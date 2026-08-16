using System;
using System.Collections.Generic;
using Avalonia.Utilities;

namespace Avalonia.Data.Core.Parsers;

internal static class BindingExpressionGrammar
{
	private enum State
	{
		Start,
		RelativeSource,
		ElementName,
		AfterMember,
		BeforeMember,
		BeforeMemberNullable,
		AttachedProperty,
		AttachedPropertyNullable,
		Indexer,
		TypeCast,
		End
	}

	private readonly ref struct TypeName(ReadOnlySpan<char> ns, ReadOnlySpan<char> typeName)
	{
		public readonly ReadOnlySpan<char> Namespace = ns;

		public readonly ReadOnlySpan<char> Type = typeName;

		public void Deconstruct(out string ns, out string typeName)
		{
			ns = Namespace.ToString();
			typeName = Type.ToString();
		}
	}

	public interface INode
	{
	}

	public interface ITransformNode
	{
	}

	public class EmptyExpressionNode : INode
	{
	}

	public class PropertyNameNode : INode
	{
		public bool AcceptsNull { get; set; }

		public string PropertyName { get; set; } = string.Empty;
	}

	public class AttachedPropertyNameNode : INode
	{
		public bool AcceptsNull { get; set; }

		public string Namespace { get; set; } = string.Empty;

		public string TypeName { get; set; } = string.Empty;

		public string PropertyName { get; set; } = string.Empty;
	}

	public class IndexerNode : INode
	{
		public IList<string> Arguments { get; set; } = Array.Empty<string>();
	}

	public class NotNode : INode, ITransformNode
	{
	}

	public class StreamNode : INode
	{
	}

	public class SelfNode : INode
	{
	}

	public class NameNode : INode
	{
		public string Name { get; set; } = string.Empty;
	}

	public class AncestorNode : INode
	{
		public string? Namespace { get; set; }

		public string? TypeName { get; set; }

		public int Level { get; set; }
	}

	public class TypeCastNode : INode
	{
		public string Namespace { get; set; } = string.Empty;

		public string TypeName { get; set; } = string.Empty;
	}

	private static readonly List<INode> s_pool = new List<INode>();

	public static (List<INode> Nodes, SourceMode Mode) Parse(string text)
	{
		CharacterReader r = new CharacterReader(text.AsSpan());
		return Parse(ref r);
	}

	public static (List<INode> Nodes, SourceMode Mode) Parse(ref CharacterReader r)
	{
		List<INode> list = new List<INode>();
		SourceMode item = Parse(ref r, list);
		return (Nodes: list, Mode: item);
	}

	public static (List<INode> Nodes, SourceMode Mode) ParseToPooledList(ref CharacterReader r)
	{
		s_pool.Clear();
		SourceMode item = Parse(ref r, s_pool);
		return (Nodes: s_pool, Mode: item);
	}

	private static SourceMode Parse(ref CharacterReader r, List<INode> nodes)
	{
		State state = State.Start;
		SourceMode result = SourceMode.Data;
		while (!r.End)
		{
			switch (state)
			{
			case State.Start:
				state = ParseStart(ref r, nodes);
				continue;
			case State.AfterMember:
				state = ParseAfterMember(ref r, nodes);
				continue;
			case State.BeforeMember:
				state = ParseBeforeMember(ref r, nodes, acceptsNull: false);
				continue;
			case State.BeforeMemberNullable:
				state = ParseBeforeMember(ref r, nodes, acceptsNull: true);
				continue;
			case State.AttachedProperty:
				state = ParseAttachedProperty(ref r, nodes, acceptsNull: false);
				continue;
			case State.AttachedPropertyNullable:
				state = ParseAttachedProperty(ref r, nodes, acceptsNull: true);
				continue;
			case State.Indexer:
				state = ParseIndexer(ref r, nodes);
				continue;
			case State.TypeCast:
				state = ParseTypeCast(ref r, nodes);
				continue;
			case State.ElementName:
				state = ParseElementName(ref r, nodes);
				result = SourceMode.Control;
				continue;
			case State.RelativeSource:
				state = ParseRelativeSource(ref r, nodes);
				result = SourceMode.Control;
				continue;
			default:
				continue;
			case State.End:
				break;
			}
			break;
		}
		if (!r.End)
		{
			throw new ExpressionParseException(r.Position, "Expected end of expression.");
		}
		if ((uint)(state - 4) <= 1u)
		{
			throw new ExpressionParseException(r.Position, "Unexpected end of expression.");
		}
		return result;
	}

	private static State ParseStart(ref CharacterReader r, IList<INode> nodes)
	{
		if (ParseNot(ref r))
		{
			nodes.Add(new NotNode());
			return State.Start;
		}
		if (ParseSharp(ref r))
		{
			return State.ElementName;
		}
		if (ParseDollarSign(ref r))
		{
			return State.RelativeSource;
		}
		if (ParseOpenBrace(ref r))
		{
			if (PeekOpenBrace(ref r))
			{
				return State.TypeCast;
			}
			return State.AttachedProperty;
		}
		if (PeekOpenBracket(ref r))
		{
			return State.Indexer;
		}
		if (ParseDot(ref r))
		{
			nodes.Add(new EmptyExpressionNode());
			return State.AfterMember;
		}
		if (ParseStreamOperator(ref r))
		{
			nodes.Add(new EmptyExpressionNode());
			nodes.Add(new StreamNode());
			return State.AfterMember;
		}
		ReadOnlySpan<char> readOnlySpan = r.ParseIdentifier();
		if (!readOnlySpan.IsEmpty)
		{
			nodes.Add(new PropertyNameNode
			{
				PropertyName = readOnlySpan.ToString()
			});
			return State.AfterMember;
		}
		return State.End;
	}

	private static State ParseAfterMember(ref CharacterReader r, IList<INode> nodes)
	{
		if (ParseMemberAccessor(ref r, out var acceptsNull))
		{
			if (!acceptsNull)
			{
				return State.BeforeMember;
			}
			return State.BeforeMemberNullable;
		}
		if (ParseStreamOperator(ref r))
		{
			nodes.Add(new StreamNode());
			return State.AfterMember;
		}
		if (PeekOpenBracket(ref r))
		{
			return State.Indexer;
		}
		if (ParseOpenBrace(ref r))
		{
			return State.TypeCast;
		}
		return State.End;
	}

	private static State ParseBeforeMember(ref CharacterReader r, IList<INode> nodes, bool acceptsNull)
	{
		if (ParseOpenBrace(ref r))
		{
			if (PeekOpenBrace(ref r))
			{
				return State.TypeCast;
			}
			if (!acceptsNull)
			{
				return State.AttachedProperty;
			}
			return State.AttachedPropertyNullable;
		}
		ReadOnlySpan<char> readOnlySpan = r.ParseIdentifier();
		if (!readOnlySpan.IsEmpty)
		{
			nodes.Add(new PropertyNameNode
			{
				AcceptsNull = acceptsNull,
				PropertyName = readOnlySpan.ToString()
			});
			return State.AfterMember;
		}
		return State.End;
	}

	private static State ParseAttachedProperty(scoped ref CharacterReader r, List<INode> nodes, bool acceptsNull)
	{
		var (text3, typeName2) = ParseTypeName(ref r);
		if (!r.End && r.TakeIf(')'))
		{
			nodes.Add(new TypeCastNode
			{
				Namespace = text3,
				TypeName = typeName2
			});
			return State.AfterMember;
		}
		if (r.End || !r.TakeIf('.'))
		{
			throw new ExpressionParseException(r.Position, "Invalid attached property name.");
		}
		ReadOnlySpan<char> readOnlySpan = r.ParseIdentifier();
		if (readOnlySpan.Length == 0)
		{
			throw new ExpressionParseException(r.Position, "Attached Property name expected after '.'.");
		}
		if (r.End || !r.TakeIf(')'))
		{
			throw new ExpressionParseException(r.Position, "Expected ')'.");
		}
		nodes.Add(new AttachedPropertyNameNode
		{
			AcceptsNull = acceptsNull,
			Namespace = text3,
			TypeName = typeName2,
			PropertyName = readOnlySpan.ToString()
		});
		return State.AfterMember;
	}

	private static State ParseIndexer(ref CharacterReader r, List<INode> nodes)
	{
		IList<string> list = r.ParseArguments('[', ']');
		if (list.Count == 0)
		{
			throw new ExpressionParseException(r.Position, "Indexer may not be empty.");
		}
		nodes.Add(new IndexerNode
		{
			Arguments = list
		});
		return State.AfterMember;
	}

	private static State ParseTypeCast(ref CharacterReader r, List<INode> nodes)
	{
		bool num = ParseOpenBrace(ref r);
		var (text3, typeName2) = ParseTypeName(ref r);
		State state = State.AfterMember;
		if (num)
		{
			if (!ParseCloseBrace(ref r))
			{
				throw new ExpressionParseException(r.Position, "Expected ')'.");
			}
			state = ParseBeforeMember(ref r, nodes, acceptsNull: false);
			if (state == State.AttachedProperty)
			{
				state = ParseAttachedProperty(ref r, nodes, acceptsNull: false);
			}
			if (r.Peek == '[')
			{
				state = ParseIndexer(ref r, nodes);
			}
		}
		nodes.Add(new TypeCastNode
		{
			Namespace = text3,
			TypeName = typeName2
		});
		if (r.End || !r.TakeIf(')'))
		{
			throw new ExpressionParseException(r.Position, "Expected ')'.");
		}
		return state;
	}

	private static State ParseElementName(ref CharacterReader r, List<INode> nodes)
	{
		ReadOnlySpan<char> readOnlySpan = r.ParseIdentifier();
		if (readOnlySpan.IsEmpty)
		{
			throw new ExpressionParseException(r.Position, "Element name expected after '#'.");
		}
		nodes.Add(new NameNode
		{
			Name = readOnlySpan.ToString()
		});
		return State.AfterMember;
	}

	private static State ParseRelativeSource(ref CharacterReader r, List<INode> nodes)
	{
		ReadOnlySpan<char> span = r.ParseIdentifier();
		if (span.SequenceEqual("self".AsSpan()))
		{
			nodes.Add(new SelfNode());
		}
		else
		{
			if (!span.SequenceEqual("parent".AsSpan()))
			{
				throw new ExpressionParseException(r.Position, "Unknown RelativeSource mode.");
			}
			string text = null;
			string typeName = null;
			int level = 0;
			if (PeekOpenBracket(ref r))
			{
				IList<string> list = r.ParseArguments('[', ']', ';');
				if (list.Count > 2 || list.Count == 0)
				{
					throw new ExpressionParseException(r.Position, "Too many arguments in RelativeSource syntax sugar");
				}
				string typeName2;
				string ns;
				if (list.Count == 1)
				{
					if (int.TryParse(list[0], out var result))
					{
						typeName = null;
						level = result;
					}
					else
					{
						CharacterReader r2 = new CharacterReader(list[0].AsSpan());
						ParseTypeName(ref r2).Deconstruct(out typeName2, out ns);
						text = typeName2;
						typeName = ns;
					}
				}
				else
				{
					CharacterReader r3 = new CharacterReader(list[0].AsSpan());
					ParseTypeName(ref r3).Deconstruct(out ns, out typeName2);
					text = ns;
					typeName = typeName2;
					level = int.Parse(list[1]);
				}
			}
			nodes.Add(new AncestorNode
			{
				Namespace = text,
				TypeName = typeName,
				Level = level
			});
		}
		return State.AfterMember;
	}

	private static TypeName ParseTypeName(scoped ref CharacterReader r)
	{
		ReadOnlySpan<char> ns = ReadOnlySpan<char>.Empty;
		ReadOnlySpan<char> readOnlySpan = r.ParseIdentifier();
		ReadOnlySpan<char> typeName;
		if (!r.End && r.TakeIf(':'))
		{
			ns = readOnlySpan;
			typeName = r.ParseIdentifier();
		}
		else
		{
			typeName = readOnlySpan;
		}
		return new TypeName(ns, typeName);
	}

	private static bool ParseNot(ref CharacterReader r)
	{
		if (!r.End)
		{
			return r.TakeIf('!');
		}
		return false;
	}

	private static bool ParseMemberAccessor(ref CharacterReader r, out bool acceptsNull)
	{
		if (r.End)
		{
			acceptsNull = false;
			return false;
		}
		if (r.TakeIf('.'))
		{
			acceptsNull = false;
			return true;
		}
		if (r.TakeIf("?."))
		{
			acceptsNull = true;
			return true;
		}
		acceptsNull = false;
		return false;
	}

	private static bool ParseOpenBrace(ref CharacterReader r)
	{
		if (!r.End)
		{
			return r.TakeIf('(');
		}
		return false;
	}

	private static bool ParseCloseBrace(ref CharacterReader r)
	{
		if (!r.End)
		{
			return r.TakeIf(')');
		}
		return false;
	}

	private static bool PeekOpenBracket(ref CharacterReader r)
	{
		if (!r.End)
		{
			return r.Peek == '[';
		}
		return false;
	}

	private static bool PeekOpenBrace(ref CharacterReader r)
	{
		if (!r.End)
		{
			return r.Peek == '(';
		}
		return false;
	}

	private static bool ParseStreamOperator(ref CharacterReader r)
	{
		if (!r.End)
		{
			return r.TakeIf('^');
		}
		return false;
	}

	private static bool ParseDollarSign(ref CharacterReader r)
	{
		if (!r.End)
		{
			return r.TakeIf('$');
		}
		return false;
	}

	private static bool ParseSharp(ref CharacterReader r)
	{
		if (!r.End)
		{
			return r.TakeIf('#');
		}
		return false;
	}

	private static bool ParseDot(ref CharacterReader r)
	{
		if (!r.End)
		{
			return r.TakeIf('.');
		}
		return false;
	}
}
