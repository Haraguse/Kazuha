using System;
using System.Collections.Generic;
using System.Linq;

namespace FluentAvalonia.UI.Controls;

internal static class SelectionTreeHelper
{
	public struct TreeWalkNodeInfo
	{
		public SelectionNode Node { get; }

		public IndexPath Path { get; }

		public SelectionNode ParentNode { get; }

		public TreeWalkNodeInfo(SelectionNode node, IndexPath indexPath, SelectionNode parent)
		{
			node = node ?? throw new ArgumentNullException("node");
			Node = node;
			Path = indexPath;
			ParentNode = parent;
		}

		public TreeWalkNodeInfo(SelectionNode node, IndexPath indexPath)
		{
			node = node ?? throw new ArgumentNullException("node");
			Node = node;
			Path = indexPath;
			ParentNode = null;
		}
	}

	public static void TraverseIndexPath(SelectionNode root, IndexPath path, bool realizeChildren, Action<SelectionNode, IndexPath, int, int> nodeAction)
	{
		SelectionNode selectionNode = root;
		for (int i = 0; i < path.GetSize(); i++)
		{
			int at = path.GetAt(i);
			nodeAction(selectionNode, path, i, at);
			if (i < path.GetSize() - 1)
			{
				selectionNode = selectionNode.GetAt(at, realizeChildren);
			}
		}
	}

	public static void Traverse(SelectionNode root, bool realizeChildren, Action<TreeWalkNodeInfo> nodeAction)
	{
		List<TreeWalkNodeInfo> list = new List<TreeWalkNodeInfo>();
		IndexPath indexPath = new IndexPath(null);
		list.Add(new TreeWalkNodeInfo(root, indexPath));
		while (list.Count > 0)
		{
			TreeWalkNodeInfo obj = list.Last();
			list.RemoveAt(list.Count - 1);
			for (int num = (realizeChildren ? obj.Node.DataCount : obj.Node.ChildrenNodeCount) - 1; num >= 0; num--)
			{
				SelectionNode at = obj.Node.GetAt(num, realizeChildren);
				IndexPath indexPath2 = obj.Path.CloneWithChildIndex(num);
				if (at != null)
				{
					list.Add(new TreeWalkNodeInfo(at, indexPath2, obj.Node));
				}
			}
			nodeAction(obj);
		}
	}

	public static void TraverseRangeRealizeChildren(SelectionNode root, IndexPath start, IndexPath end, Action<TreeWalkNodeInfo> nodeAction)
	{
		List<TreeWalkNodeInfo> pendingNodes = new List<TreeWalkNodeInfo>();
		_ = start;
		TraverseIndexPath(root, start, realizeChildren: true, delegate(SelectionNode node, IndexPath path, int depth, int childIndex)
		{
			IndexPath subset = StartPath(path, depth);
			bool flag3 = IsSubSet(start, subset);
			bool flag4 = IsSubSet(end, subset);
			int num3 = (((depth < start.GetSize()) & flag3) ? Math.Max(0, start.GetAt(depth)) : 0);
			for (int num4 = (((depth < end.GetSize()) & flag4) ? Math.Min(node.DataCount - 1, end.GetAt(depth)) : (node.DataCount - 1)); num4 >= num3; num4--)
			{
				SelectionNode at2 = node.GetAt(num4, realizeChild: true);
				if (at2 != null)
				{
					IndexPath indexPath2 = subset.CloneWithChildIndex(num4);
					pendingNodes.Add(new TreeWalkNodeInfo(at2, indexPath2, node));
				}
			}
		});
		while (pendingNodes.Count > 0)
		{
			TreeWalkNodeInfo obj = pendingNodes.Last();
			pendingNodes.RemoveAt(pendingNodes.Count - 1);
			int size = obj.Path.GetSize();
			bool flag = IsSubSet(start, obj.Path);
			bool flag2 = IsSubSet(end, obj.Path);
			int num = (((size < start.GetSize()) & flag) ? start.GetAt(size) : 0);
			for (int num2 = (((size < end.GetSize()) & flag2) ? end.GetAt(size) : (obj.Node.DataCount - 1)); num2 >= num; num2--)
			{
				SelectionNode at = obj.Node.GetAt(num2, realizeChild: true);
				if (at != null)
				{
					IndexPath indexPath = obj.Path.CloneWithChildIndex(num2);
					pendingNodes.Add(new TreeWalkNodeInfo(at, indexPath, obj.Node));
				}
			}
			nodeAction(obj);
			if (obj.Path.CompareTo(end) == 0)
			{
				break;
			}
		}
	}

	private static bool IsSubSet(IndexPath path, IndexPath subset)
	{
		int size = subset.GetSize();
		if (path.GetSize() < size)
		{
			return false;
		}
		for (int i = 0; i < size; i++)
		{
			if (path.GetAt(i) != subset.GetAt(i))
			{
				return false;
			}
		}
		return true;
	}

	private static IndexPath StartPath(IndexPath path, int length)
	{
		List<int> list = new List<int>();
		for (int i = 0; i < length; i++)
		{
			list.Add(path.GetAt(i));
		}
		return new IndexPath(list);
	}
}
