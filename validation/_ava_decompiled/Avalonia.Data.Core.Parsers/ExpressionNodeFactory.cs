using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Avalonia.Controls;
using Avalonia.Data.Core.ExpressionNodes;
using Avalonia.Data.Core.ExpressionNodes.Reflection;

namespace Avalonia.Data.Core.Parsers;

/// <summary>
/// Creates <see cref="T:Avalonia.Data.Core.ExpressionNodes.ExpressionNode" />s from a <see cref="T:Avalonia.Data.Core.Parsers.BindingExpressionGrammar" />.
/// </summary>
internal static class ExpressionNodeFactory
{
	[RequiresUnreferencedCode("BindingExpression and ReflectionBinding heavily use reflection. Consider using CompiledBindings instead.")]
	[RequiresDynamicCode("BindingExpression and ReflectionBinding require dynamic code. Consider using CompiledBindings instead.")]
	public static List<ExpressionNode>? CreateFromAst(List<BindingExpressionGrammar.INode> astNodes, Func<string?, string, Type?>? typeResolver, INameScope? nameScope, out bool isRooted)
	{
		int num = 0;
		List<ExpressionNode> list = null;
		ExpressionNode expressionNode = null;
		isRooted = false;
		foreach (BindingExpressionGrammar.INode astNode in astNodes)
		{
			if (!(astNode is BindingExpressionGrammar.AncestorNode ancestor))
			{
				if (!(astNode is BindingExpressionGrammar.AttachedPropertyNameNode attached))
				{
					if (!(astNode is BindingExpressionGrammar.EmptyExpressionNode))
					{
						if (!(astNode is BindingExpressionGrammar.IndexerNode indexerNode))
						{
							if (!(astNode is BindingExpressionGrammar.NameNode nameNode))
							{
								if (!(astNode is BindingExpressionGrammar.NotNode))
								{
									if (!(astNode is BindingExpressionGrammar.PropertyNameNode propertyNameNode))
									{
										if (!(astNode is BindingExpressionGrammar.SelfNode))
										{
											if (!(astNode is BindingExpressionGrammar.StreamNode))
											{
												if (!(astNode is BindingExpressionGrammar.TypeCastNode typeCastNode))
												{
													throw new Exception($"Unexpected node type '{astNode}'.");
												}
												expressionNode = new ReflectionTypeCastNode(LookupType(typeResolver, typeCastNode.Namespace, typeCastNode.TypeName));
											}
											else
											{
												expressionNode = new DynamicPluginStreamNode();
											}
										}
										else
										{
											expressionNode = null;
											isRooted = true;
										}
									}
									else
									{
										expressionNode = new DynamicPluginPropertyAccessorNode(propertyNameNode.PropertyName, propertyNameNode.AcceptsNull);
									}
								}
								else
								{
									num++;
								}
							}
							else
							{
								expressionNode = new NamedElementNode(nameScope, nameNode.Name);
								isRooted = true;
							}
						}
						else
						{
							expressionNode = new ReflectionIndexerNode((IList)indexerNode.Arguments);
						}
					}
					else
					{
						expressionNode = null;
					}
				}
				else
				{
					expressionNode = AttachedPropertyNode(typeResolver, attached);
				}
			}
			else
			{
				expressionNode = LogicalAncestorNode(typeResolver, ancestor);
				isRooted = true;
			}
			if (expressionNode != null)
			{
				if (list == null)
				{
					list = new List<ExpressionNode>(astNodes.Count);
				}
				list.Add(expressionNode);
			}
		}
		if (num != 0)
		{
			if (list == null)
			{
				list = new List<ExpressionNode>(num);
			}
			for (int i = 0; i < num; i++)
			{
				list.Add(new LogicalNotNode());
			}
		}
		return list;
	}

	public static ExpressionNode? CreateRelativeSource(RelativeSource source)
	{
		switch (source.Mode)
		{
		case RelativeSourceMode.DataContext:
			return new DataContextNode();
		case RelativeSourceMode.TemplatedParent:
			return new TemplatedParentNode();
		case RelativeSourceMode.Self:
			return null;
		case RelativeSourceMode.FindAncestor:
			if (source.Tree == TreeType.Logical)
			{
				return new LogicalAncestorElementNode(source.AncestorType, source.AncestorLevel - 1);
			}
			if (source.Tree == TreeType.Visual)
			{
				return new VisualAncestorElementNode(source.AncestorType, source.AncestorLevel - 1);
			}
			break;
		}
		throw new NotSupportedException("Unsupported RelativeSource mode.");
	}

	public static ExpressionNode CreateDataContext(AvaloniaProperty? targetProperty)
	{
		if (!(targetProperty == StyledElement.DataContextProperty))
		{
			return new DataContextNode();
		}
		return new ParentDataContextNode();
	}

	private static AvaloniaPropertyAccessorNode AttachedPropertyNode(Func<string?, string, Type?>? typeResolver, BindingExpressionGrammar.AttachedPropertyNameNode attached)
	{
		Type type = LookupType(typeResolver, attached.Namespace, attached.TypeName);
		return new AvaloniaPropertyAccessorNode(AvaloniaPropertyRegistry.Instance.FindRegistered(type, attached.PropertyName) ?? throw new InvalidOperationException($"Cannot find property {type}.{attached.PropertyName}."));
	}

	private static LogicalAncestorElementNode LogicalAncestorNode(Func<string?, string, Type?>? typeResolver, BindingExpressionGrammar.AncestorNode ancestor)
	{
		Type ancestorType = null;
		if (!string.IsNullOrEmpty(ancestor.TypeName))
		{
			ancestorType = LookupType(typeResolver, ancestor.Namespace, ancestor.TypeName);
		}
		return new LogicalAncestorElementNode(ancestorType, ancestor.Level);
	}

	private static Type LookupType(Func<string?, string, Type?>? typeResolver, string? @namespace, string? name)
	{
		if (name == null)
		{
			throw new InvalidOperationException("Unable to resolve unnamed type from namespace '" + @namespace + "'.");
		}
		return typeResolver?.Invoke(@namespace, name) ?? throw new InvalidOperationException($"Unable to resolve type '{@namespace}:{name}'.");
	}
}
