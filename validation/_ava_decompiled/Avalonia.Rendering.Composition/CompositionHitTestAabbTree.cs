using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Avalonia.Collections.Pooled;
using Avalonia.Platform;
using Avalonia.Rendering.Composition.Server;

namespace Avalonia.Rendering.Composition;

internal sealed class CompositionHitTestAabbTree
{
	private enum BoundsState
	{
		Empty,
		Bounded,
		Unbounded
	}

	private struct Node
	{
		public LtrbRect Bounds;

		public CompositionVisual? Visual;

		public int Parent;

		public int Child1;

		public int Child2;

		public int Next;

		public int Height;

		public int Order;

		public int Bucket;

		public readonly bool IsLeaf => Child1 == -1;
	}

	private struct Bucket(int root)
	{
		public int Root = root;

		public List<CompositionVisual>? Unbounded = null;

		public ulong ReadbackRevision = 0uL;

		public readonly bool IsEmpty
		{
			get
			{
				if (Root == -1)
				{
					if (Unbounded != null)
					{
						return Unbounded.Count == 0;
					}
					return true;
				}
				return false;
			}
		}
	}

	private struct Entry(int order)
	{
		public int Order = order;

		public int Leaf = -1;

		public ulong Revision = 0uL;

		public bool IsUnbounded = false;
	}

	private readonly struct Candidate(CompositionVisual visual, int order)
	{
		public CompositionVisual Visual { get; } = visual;

		public int Order { get; } = order;
	}

	private sealed class CandidateComparer : IComparer<Candidate>
	{
		public int Compare(Candidate left, Candidate right)
		{
			return right.Order.CompareTo(left.Order);
		}
	}

	private const int Null = -1;

	private const int OrderBucketSize = 32;

	private const double FatBoundsPadding = 1.0;

	private static readonly CandidateComparer s_candidateComparer = new CandidateComparer();

	private readonly CompositionVisualCollection _children;

	private readonly Dictionary<CompositionVisual, Entry> _entries = new Dictionary<CompositionVisual, Entry>();

	private readonly List<Bucket> _buckets = new List<Bucket>();

	private readonly List<Node> _nodes = new List<Node>();

	private int _freeList = -1;

	public CompositionHitTestAabbTree(CompositionVisualCollection children)
	{
		_children = children;
		for (int i = 0; i < children.Count; i++)
		{
			Update(children[i], i);
		}
	}

	public void Clear()
	{
		_entries.Clear();
		_buckets.Clear();
		_nodes.Clear();
		_freeList = -1;
	}

	public void Update(CompositionVisual visual, int order)
	{
		ref Entry valueRefOrAddDefault = ref CollectionsMarshal.GetValueRefOrAddDefault(_entries, visual, out var exists);
		if (!exists)
		{
			int bucketIndex = GetBucketIndex(order);
			valueRefOrAddDefault = new Entry(order);
			GetOrCreateBucket(bucketIndex);
		}
		else if (valueRefOrAddDefault.Order != order)
		{
			UpdateOrderCore(visual, ref valueRefOrAddDefault, order);
		}
		BoundsState boundsState = GetBoundsState(visual, out var bounds, out var revision);
		UpdateBounds(visual, ref valueRefOrAddDefault, boundsState, bounds, revision);
	}

	public void Remove(CompositionVisual visual)
	{
		if (_entries.Remove(visual, out var value))
		{
			if (value.Leaf != -1)
			{
				DestroyLeaf(value.Leaf);
			}
			else if (value.IsUnbounded)
			{
				RemoveUnbounded(visual, value.Order);
			}
			RemoveBucketIfEmpty(GetBucketIndex(value.Order));
		}
	}

	public void UpdateOrder(CompositionVisual visual, int order)
	{
		ref Entry valueRefOrAddDefault = ref CollectionsMarshal.GetValueRefOrAddDefault(_entries, visual, out var exists);
		if (!exists)
		{
			valueRefOrAddDefault = new Entry(order);
		}
		else if (valueRefOrAddDefault.Order != order)
		{
			UpdateOrderCore(visual, ref valueRefOrAddDefault, order);
		}
	}

	private void UpdateOrderCore(CompositionVisual visual, ref Entry entry, int order)
	{
		int order2 = entry.Order;
		int bucketIndex = GetBucketIndex(order2);
		int bucketIndex2 = GetBucketIndex(order);
		entry.Order = order;
		if (entry.Leaf != -1)
		{
			MoveLeaf(entry.Leaf, order);
		}
		else if (entry.IsUnbounded)
		{
			MoveUnbounded(visual, order2, order);
		}
		else if (bucketIndex != bucketIndex2)
		{
			RemoveBucketIfEmpty(bucketIndex);
		}
	}

	public void Query(Point point, PooledList<CompositionVisual> results, ulong readbackRevision)
	{
		Candidate[] candidates = ArrayPool<Candidate>.Shared.Rent(32);
		int[] stack = ArrayPool<int>.Shared.Rent(16);
		int candidateCount = 0;
		try
		{
			for (int num = _buckets.Count - 1; num >= 0; num--)
			{
				UpdateBucket(num, readbackRevision);
				Bucket bucket = _buckets[num];
				if (bucket.Root == -1)
				{
					List<CompositionVisual> unbounded = bucket.Unbounded;
					if (unbounded == null || unbounded.Count <= 0)
					{
						continue;
					}
				}
				candidateCount = 0;
				int stackCount = 0;
				QueryBucket(bucket, point, ref candidates, ref candidateCount, ref stack, ref stackCount);
				candidates.AsSpan(0, candidateCount).Sort(s_candidateComparer);
				for (int i = 0; i < candidateCount; i++)
				{
					results.Add(candidates[i].Visual);
				}
				candidates.AsSpan(0, candidateCount).Clear();
			}
		}
		finally
		{
			candidates.AsSpan(0, candidateCount).Clear();
			ArrayPool<Candidate>.Shared.Return(candidates);
			ArrayPool<int>.Shared.Return(stack);
		}
	}

	public CompositionVisual? QueryFirst(CompositionTarget target, Point point, Func<CompositionVisual, bool>? filter, Func<CompositionVisual, bool>? resultFilter, ulong readbackRevision)
	{
		Candidate[] candidates = ArrayPool<Candidate>.Shared.Rent(32);
		int[] stack = ArrayPool<int>.Shared.Rent(16);
		int candidateCount = 0;
		try
		{
			for (int num = _buckets.Count - 1; num >= 0; num--)
			{
				UpdateBucket(num, readbackRevision);
				Bucket bucket = _buckets[num];
				if (bucket.Root == -1)
				{
					List<CompositionVisual> unbounded = bucket.Unbounded;
					if (unbounded == null || unbounded.Count <= 0)
					{
						continue;
					}
				}
				candidateCount = 0;
				int stackCount = 0;
				QueryBucket(bucket, point, ref candidates, ref candidateCount, ref stack, ref stackCount);
				candidates.AsSpan(0, candidateCount).Sort(s_candidateComparer);
				for (int i = 0; i < candidateCount; i++)
				{
					CompositionVisual compositionVisual = target.HitTestFirstCore(candidates[i].Visual, point, filter, resultFilter);
					if (compositionVisual != null)
					{
						return compositionVisual;
					}
				}
				candidates.AsSpan(0, candidateCount).Clear();
			}
		}
		finally
		{
			candidates.AsSpan(0, candidateCount).Clear();
			ArrayPool<Candidate>.Shared.Return(candidates);
			ArrayPool<int>.Shared.Return(stack);
		}
		return null;
	}

	private void UpdateBucket(int bucketIndex, ulong readbackRevision)
	{
		ref Bucket reference = ref GetRef(_buckets, bucketIndex);
		if (reference.ReadbackRevision == readbackRevision)
		{
			return;
		}
		int num = Math.Min(_children.Count, (bucketIndex + 1) * 32);
		for (int i = bucketIndex * 32; i < num; i++)
		{
			CompositionVisual compositionVisual = _children[i];
			ref Entry valueRefOrNullRef = ref CollectionsMarshal.GetValueRefOrNullRef(_entries, compositionVisual);
			BoundsState boundsState = GetBoundsState(compositionVisual, out var bounds, out var revision);
			if (valueRefOrNullRef.Revision != revision)
			{
				UpdateBounds(compositionVisual, ref valueRefOrNullRef, boundsState, bounds, revision);
			}
		}
		reference.ReadbackRevision = readbackRevision;
	}

	private void UpdateBounds(CompositionVisual visual, ref Entry entry, BoundsState state, LtrbRect bounds, ulong revision)
	{
		entry.Revision = revision;
		if (entry.Leaf != -1)
		{
			if (state == BoundsState.Bounded)
			{
				UpdateLeaf(entry.Leaf, bounds, entry.Order);
				return;
			}
			DestroyLeaf(entry.Leaf);
			entry.Leaf = -1;
			if (state == BoundsState.Unbounded)
			{
				AddUnbounded(visual, entry.Order, ref entry);
			}
		}
		else if (entry.IsUnbounded)
		{
			if (state != BoundsState.Unbounded)
			{
				RemoveUnbounded(visual, entry.Order);
				entry.IsUnbounded = false;
				if (state == BoundsState.Bounded)
				{
					entry.Leaf = CreateLeaf(visual, bounds, entry.Order);
				}
			}
		}
		else
		{
			switch (state)
			{
			case BoundsState.Bounded:
				entry.Leaf = CreateLeaf(visual, bounds, entry.Order);
				break;
			case BoundsState.Unbounded:
				AddUnbounded(visual, entry.Order, ref entry);
				break;
			}
		}
	}

	private void QueryBucket(Bucket bucket, Point point, ref Candidate[] candidates, ref int candidateCount, ref int[] stack, ref int stackCount)
	{
		PushQueryNode(ref stack, ref stackCount, bucket.Root);
		while (stackCount > 0)
		{
			int index = stack[--stackCount];
			Node node = _nodes[index];
			if (!node.Bounds.Contains(point))
			{
				continue;
			}
			if (node.IsLeaf)
			{
				if (node.Visual != null)
				{
					AddCandidate(ref candidates, ref candidateCount, new Candidate(node.Visual, node.Order));
				}
			}
			else
			{
				PushQueryNode(ref stack, ref stackCount, node.Child1);
				PushQueryNode(ref stack, ref stackCount, node.Child2);
			}
		}
		if (bucket.Unbounded == null)
		{
			return;
		}
		foreach (CompositionVisual item in bucket.Unbounded)
		{
			if (_entries.TryGetValue(item, out var value))
			{
				AddCandidate(ref candidates, ref candidateCount, new Candidate(item, value.Order));
			}
		}
	}

	private static void AddCandidate(ref Candidate[] candidates, ref int count, Candidate candidate)
	{
		if (count == candidates.Length)
		{
			Resize(ref candidates, count);
		}
		candidates[count++] = candidate;
	}

	private static void PushQueryNode(ref int[] stack, ref int count, int nodeIndex)
	{
		if (nodeIndex != -1)
		{
			if (count == stack.Length)
			{
				Resize(ref stack, count);
			}
			stack[count++] = nodeIndex;
		}
	}

	private static void Resize<T>(ref T[] buffer, int count)
	{
		T[] array = ArrayPool<T>.Shared.Rent(buffer.Length * 2);
		buffer.AsSpan(0, count).CopyTo(array);
		ArrayPool<T>.Shared.Return(buffer, RuntimeHelpers.IsReferenceOrContainsReferences<T>());
		buffer = array;
	}

	private static int GetBucketIndex(int order)
	{
		return order / 32;
	}

	private ref Bucket GetOrCreateBucket(int bucketIndex)
	{
		while (_buckets.Count <= bucketIndex)
		{
			_buckets.Add(new Bucket(-1));
		}
		return ref GetRef(_buckets, bucketIndex);
	}

	private void RemoveBucketIfEmpty(int bucketIndex)
	{
		if (bucketIndex >= _buckets.Count)
		{
			return;
		}
		int num = ((_children.Count == 0) ? (-1) : GetBucketIndex(_children.Count - 1));
		ref Bucket reference = ref GetRef(_buckets, bucketIndex);
		List<CompositionVisual>? unbounded = reference.Unbounded;
		if (unbounded != null && unbounded.Count == 0)
		{
			reference.Unbounded = null;
		}
		if (!reference.IsEmpty || bucketIndex != _buckets.Count - 1 || bucketIndex <= num)
		{
			return;
		}
		List<Bucket> buckets;
		do
		{
			_buckets.RemoveAt(_buckets.Count - 1);
			if (_buckets.Count > 0 && _buckets.Count - 1 > num)
			{
				buckets = _buckets;
				continue;
			}
			break;
		}
		while (buckets[buckets.Count - 1].IsEmpty);
	}

	private static ref T GetRef<T>(List<T> items, int index)
	{
		return ref CollectionsMarshal.AsSpan(items)[index];
	}

	private int CreateLeaf(CompositionVisual visual, LtrbRect bounds, int order)
	{
		int num = AllocateNode();
		ref Node reference = ref GetRef(_nodes, num);
		reference.Bounds = Fatten(bounds);
		reference.Visual = visual;
		reference.Order = order;
		reference.Bucket = GetBucketIndex(order);
		reference.Height = 0;
		InsertLeaf(num);
		return num;
	}

	private void DestroyLeaf(int leaf)
	{
		RemoveLeaf(leaf);
		FreeNode(leaf);
	}

	private void UpdateLeaf(int leaf, LtrbRect bounds, int order)
	{
		int bucketIndex = GetBucketIndex(order);
		ref Node reference = ref GetRef(_nodes, leaf);
		if (reference.Bucket == bucketIndex && reference.Bounds.Contains(bounds))
		{
			reference.Order = order;
			return;
		}
		RemoveLeaf(leaf);
		ref Node reference2 = ref GetRef(_nodes, leaf);
		reference2.Bounds = Fatten(bounds);
		reference2.Bucket = bucketIndex;
		reference2.Order = order;
		InsertLeaf(leaf);
	}

	private void MoveLeaf(int leaf, int order)
	{
		int bucketIndex = GetBucketIndex(order);
		ref Node reference = ref GetRef(_nodes, leaf);
		if (reference.Bucket == bucketIndex)
		{
			reference.Order = order;
			return;
		}
		RemoveLeaf(leaf);
		ref Node reference2 = ref GetRef(_nodes, leaf);
		reference2.Order = order;
		reference2.Bucket = bucketIndex;
		InsertLeaf(leaf);
	}

	private int AllocateNode()
	{
		if (_freeList == -1)
		{
			_nodes.Add(new Node
			{
				Parent = -1,
				Child1 = -1,
				Child2 = -1,
				Next = -1
			});
			return _nodes.Count - 1;
		}
		int freeList = _freeList;
		ref Node reference = ref GetRef(_nodes, freeList);
		_freeList = reference.Next;
		reference.Parent = -1;
		reference.Child1 = -1;
		reference.Child2 = -1;
		reference.Next = -1;
		reference.Height = 0;
		reference.Visual = null;
		reference.Order = 0;
		reference.Bucket = 0;
		return freeList;
	}

	private void FreeNode(int index)
	{
		ref Node reference = ref GetRef(_nodes, index);
		reference.Next = _freeList;
		reference.Parent = -1;
		reference.Child1 = -1;
		reference.Child2 = -1;
		reference.Height = -1;
		reference.Visual = null;
		reference.Order = 0;
		reference.Bucket = 0;
		_freeList = index;
	}

	private void InsertLeaf(int leaf)
	{
		int bucket = GetRef(_nodes, leaf).Bucket;
		ref Bucket orCreateBucket = ref GetOrCreateBucket(bucket);
		if (orCreateBucket.Root == -1)
		{
			orCreateBucket.Root = leaf;
			GetRef(_nodes, leaf).Parent = -1;
			return;
		}
		LtrbRect bounds = GetRef(_nodes, leaf).Bounds;
		int num = FindBestSibling(orCreateBucket.Root, bounds);
		int parent = GetRef(_nodes, num).Parent;
		int num2 = AllocateNode();
		ref Node reference = ref GetRef(_nodes, num2);
		reference.Parent = parent;
		reference.Bounds = bounds.Union(GetRef(_nodes, num).Bounds);
		reference.Height = GetRef(_nodes, num).Height + 1;
		reference.Child1 = num;
		reference.Child2 = leaf;
		reference.Visual = null;
		reference.Bucket = bucket;
		GetRef(_nodes, num).Parent = num2;
		GetRef(_nodes, leaf).Parent = num2;
		if (parent == -1)
		{
			orCreateBucket.Root = num2;
		}
		else
		{
			ref Node reference2 = ref GetRef(_nodes, parent);
			if (reference2.Child1 == num)
			{
				reference2.Child1 = num2;
			}
			else
			{
				reference2.Child2 = num2;
			}
		}
		FixAncestors(num2);
	}

	private int FindBestSibling(int root, LtrbRect leafBounds)
	{
		int num = root;
		while (!_nodes[num].IsLeaf)
		{
			Node node = _nodes[num];
			int child = node.Child1;
			int child2 = node.Child2;
			double num2 = Perimeter(node.Bounds);
			double num3 = Perimeter(node.Bounds.Union(leafBounds));
			double num4 = 2.0 * num3;
			double inheritanceCost = 2.0 * (num3 - num2);
			double insertionCost = GetInsertionCost(child, leafBounds, inheritanceCost);
			double insertionCost2 = GetInsertionCost(child2, leafBounds, inheritanceCost);
			if (num4 < insertionCost && num4 < insertionCost2)
			{
				break;
			}
			num = ((insertionCost < insertionCost2) ? child : child2);
		}
		return num;
	}

	private double GetInsertionCost(int nodeIndex, LtrbRect leafBounds, double inheritanceCost)
	{
		Node node = _nodes[nodeIndex];
		LtrbRect bounds = node.Bounds.Union(leafBounds);
		if (node.IsLeaf)
		{
			return Perimeter(bounds) + inheritanceCost;
		}
		return Perimeter(bounds) - Perimeter(node.Bounds) + inheritanceCost;
	}

	private void RemoveLeaf(int leaf)
	{
		int bucket = GetRef(_nodes, leaf).Bucket;
		bool flag = false;
		ref Bucket reference = ref GetRef(_buckets, bucket);
		if (leaf == reference.Root)
		{
			reference.Root = -1;
			flag = true;
		}
		if (flag)
		{
			RemoveBucketIfEmpty(bucket);
			return;
		}
		int parent = GetRef(_nodes, leaf).Parent;
		Node node = GetRef(_nodes, parent);
		int parent2 = node.Parent;
		int num = ((node.Child1 == leaf) ? node.Child2 : node.Child1);
		if (parent2 != -1)
		{
			ref Node reference2 = ref GetRef(_nodes, parent2);
			if (reference2.Child1 == parent)
			{
				reference2.Child1 = num;
			}
			else
			{
				reference2.Child2 = num;
			}
			GetRef(_nodes, num).Parent = parent2;
			FreeNode(parent);
			FixAncestors(parent2);
		}
		else
		{
			GetRef(_buckets, bucket).Root = num;
			GetRef(_nodes, num).Parent = -1;
			FreeNode(parent);
		}
		GetRef(_nodes, leaf).Parent = -1;
		RemoveBucketIfEmpty(bucket);
	}

	private void FixAncestors(int index)
	{
		while (index != -1)
		{
			index = Balance(index);
			ref Node reference = ref GetRef(_nodes, index);
			Node node = GetRef(_nodes, reference.Child1);
			Node node2 = GetRef(_nodes, reference.Child2);
			reference.Bounds = node.Bounds.Union(node2.Bounds);
			reference.Height = 1 + Math.Max(node.Height, node2.Height);
			index = reference.Parent;
		}
	}

	private int Balance(int indexA)
	{
		Node node = GetRef(_nodes, indexA);
		if (node.IsLeaf || node.Height < 2)
		{
			return indexA;
		}
		int child = node.Child1;
		int child2 = node.Child2;
		Node node2 = GetRef(_nodes, child);
		int num = GetRef(_nodes, child2).Height - node2.Height;
		if (num > 1)
		{
			return RotateCUp(indexA, child, child2);
		}
		if (num < -1)
		{
			return RotateBUp(indexA, child, child2);
		}
		return indexA;
	}

	private int RotateCUp(int indexA, int indexB, int indexC)
	{
		ref Node reference = ref GetRef(_nodes, indexA);
		ref Node reference2 = ref GetRef(_nodes, indexC);
		int child = reference2.Child1;
		int child2 = reference2.Child2;
		ref Node reference3 = ref GetRef(_nodes, child);
		ref Node reference4 = ref GetRef(_nodes, child2);
		reference2.Child1 = indexA;
		reference2.Parent = reference.Parent;
		reference.Parent = indexC;
		ReplaceParentChild(indexA, indexC, reference2.Parent);
		if (reference3.Height > reference4.Height)
		{
			reference2.Child2 = child;
			reference.Child2 = child2;
			reference4.Parent = indexA;
			reference.Bounds = GetRef(_nodes, indexB).Bounds.Union(reference4.Bounds);
			reference2.Bounds = reference.Bounds.Union(reference3.Bounds);
			reference.Height = 1 + Math.Max(GetRef(_nodes, indexB).Height, reference4.Height);
			reference2.Height = 1 + Math.Max(reference.Height, reference3.Height);
		}
		else
		{
			reference2.Child2 = child2;
			reference.Child2 = child;
			reference3.Parent = indexA;
			reference.Bounds = GetRef(_nodes, indexB).Bounds.Union(reference3.Bounds);
			reference2.Bounds = reference.Bounds.Union(reference4.Bounds);
			reference.Height = 1 + Math.Max(GetRef(_nodes, indexB).Height, reference3.Height);
			reference2.Height = 1 + Math.Max(reference.Height, reference4.Height);
		}
		return indexC;
	}

	private int RotateBUp(int indexA, int indexB, int indexC)
	{
		ref Node reference = ref GetRef(_nodes, indexA);
		ref Node reference2 = ref GetRef(_nodes, indexB);
		int child = reference2.Child1;
		int child2 = reference2.Child2;
		ref Node reference3 = ref GetRef(_nodes, child);
		ref Node reference4 = ref GetRef(_nodes, child2);
		reference2.Child1 = indexA;
		reference2.Parent = reference.Parent;
		reference.Parent = indexB;
		ReplaceParentChild(indexA, indexB, reference2.Parent);
		if (reference3.Height > reference4.Height)
		{
			reference2.Child2 = child;
			reference.Child1 = child2;
			reference4.Parent = indexA;
			reference.Bounds = GetRef(_nodes, indexC).Bounds.Union(reference4.Bounds);
			reference2.Bounds = reference.Bounds.Union(reference3.Bounds);
			reference.Height = 1 + Math.Max(GetRef(_nodes, indexC).Height, reference4.Height);
			reference2.Height = 1 + Math.Max(reference.Height, reference3.Height);
		}
		else
		{
			reference2.Child2 = child2;
			reference.Child1 = child;
			reference3.Parent = indexA;
			reference.Bounds = GetRef(_nodes, indexC).Bounds.Union(reference3.Bounds);
			reference2.Bounds = reference.Bounds.Union(reference4.Bounds);
			reference.Height = 1 + Math.Max(GetRef(_nodes, indexC).Height, reference3.Height);
			reference2.Height = 1 + Math.Max(reference.Height, reference4.Height);
		}
		return indexB;
	}

	private void ReplaceParentChild(int oldChild, int newChild, int parent)
	{
		if (parent == -1)
		{
			GetRef(_buckets, GetRef(_nodes, newChild).Bucket).Root = newChild;
			return;
		}
		ref Node reference = ref GetRef(_nodes, parent);
		if (reference.Child1 == oldChild)
		{
			reference.Child1 = newChild;
		}
		else
		{
			reference.Child2 = newChild;
		}
	}

	private void AddUnbounded(CompositionVisual visual, int order, ref Entry entry)
	{
		int bucketIndex = GetBucketIndex(order);
		ref List<CompositionVisual> unbounded = ref GetOrCreateBucket(bucketIndex).Unbounded;
		(unbounded ?? (unbounded = new List<CompositionVisual>())).Add(visual);
		entry.IsUnbounded = true;
	}

	private void MoveUnbounded(CompositionVisual visual, int oldOrder, int order)
	{
		int bucketIndex = GetBucketIndex(oldOrder);
		int bucketIndex2 = GetBucketIndex(order);
		if (bucketIndex != bucketIndex2)
		{
			RemoveUnbounded(visual, oldOrder);
			ref List<CompositionVisual> unbounded = ref GetOrCreateBucket(bucketIndex2).Unbounded;
			(unbounded ?? (unbounded = new List<CompositionVisual>())).Add(visual);
		}
	}

	private void RemoveUnbounded(CompositionVisual visual, int order)
	{
		int bucketIndex = GetBucketIndex(order);
		GetRef(_buckets, bucketIndex).Unbounded.Remove(visual);
		RemoveBucketIfEmpty(bucketIndex);
	}

	private static BoundsState GetBoundsState(CompositionVisual visual, out LtrbRect bounds, out ulong revision)
	{
		bounds = default(LtrbRect);
		revision = 0uL;
		ServerCompositionVisual.ReadbackData readbackData = visual.TryGetValidReadback();
		if (readbackData == null)
		{
			return BoundsState.Empty;
		}
		revision = readbackData.Revision;
		if (visual.DisableSubTreeBoundsHitTestOptimization)
		{
			return BoundsState.Unbounded;
		}
		LtrbRect? transformedSubtreeBounds = readbackData.TransformedSubtreeBounds;
		if (transformedSubtreeBounds.HasValue)
		{
			LtrbRect valueOrDefault = transformedSubtreeBounds.GetValueOrDefault();
			if (!valueOrDefault.IsZeroSize)
			{
				bounds = valueOrDefault;
				return BoundsState.Bounded;
			}
		}
		return BoundsState.Empty;
	}

	private static LtrbRect Fatten(LtrbRect bounds)
	{
		return new LtrbRect(bounds.Left - 1.0, bounds.Top - 1.0, bounds.Right + 1.0, bounds.Bottom + 1.0);
	}

	private static double Perimeter(LtrbRect bounds)
	{
		return 2.0 * (bounds.Width + bounds.Height);
	}
}
