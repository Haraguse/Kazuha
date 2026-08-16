using System;
using System.Collections.Generic;
using Avalonia.Data.Core.ExpressionNodes;
using Avalonia.Data.Core.Plugins;

namespace Avalonia.Data;

public class CompiledBindingPath
{
	private readonly ICompiledBindingPathElement[] _elements;

	internal IEnumerable<ICompiledBindingPathElement> Elements => _elements;

	public CompiledBindingPath()
	{
		_elements = Array.Empty<ICompiledBindingPathElement>();
	}

	internal CompiledBindingPath(ICompiledBindingPathElement[] elements)
	{
		_elements = elements;
	}

	internal void BuildExpression(List<ExpressionNode> result, out bool isRooted)
	{
		int num = 0;
		isRooted = false;
		ICompiledBindingPathElement[] elements = _elements;
		foreach (ICompiledBindingPathElement compiledBindingPathElement in elements)
		{
			ExpressionNode expressionNode;
			if (!(compiledBindingPathElement is NotExpressionPathElement))
			{
				if (!(compiledBindingPathElement is PropertyElement propertyElement))
				{
					if (!(compiledBindingPathElement is MethodAsCommandElement methodAsCommandElement))
					{
						if (!(compiledBindingPathElement is MethodAsDelegateElement methodAsDelegateElement))
						{
							if (!(compiledBindingPathElement is ArrayElementPathElement arrayElementPathElement))
							{
								if (!(compiledBindingPathElement is VisualAncestorPathElement visualAncestorPathElement))
								{
									if (!(compiledBindingPathElement is AncestorPathElement ancestorPathElement))
									{
										if (!(compiledBindingPathElement is SelfPathElement))
										{
											if (!(compiledBindingPathElement is ElementNameElement elementNameElement))
											{
												if (!(compiledBindingPathElement is IStronglyTypedStreamElement stronglyTypedStreamElement))
												{
													if (!(compiledBindingPathElement is ITypeCastElement typeCastElement))
													{
														if (!(compiledBindingPathElement is TemplatedParentPathElement))
														{
															throw new InvalidOperationException("Unknown binding path element type " + compiledBindingPathElement.GetType().FullName);
														}
														expressionNode = new TemplatedParentNode();
														isRooted = true;
													}
													else
													{
														expressionNode = new FuncTransformNode(typeCastElement.Cast);
													}
												}
												else
												{
													expressionNode = new StreamNode(stronglyTypedStreamElement.CreatePlugin());
												}
											}
											else
											{
												expressionNode = new NamedElementNode(elementNameElement.NameScope, elementNameElement.Name);
												isRooted = true;
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
										expressionNode = new LogicalAncestorElementNode(ancestorPathElement.AncestorType, ancestorPathElement.Level);
										isRooted = true;
									}
								}
								else
								{
									expressionNode = new VisualAncestorElementNode(visualAncestorPathElement.AncestorType, visualAncestorPathElement.Level);
									isRooted = true;
								}
							}
							else
							{
								expressionNode = new ArrayIndexerNode(arrayElementPathElement.Indices);
							}
						}
						else
						{
							expressionNode = new PropertyAccessorNode(methodAsDelegateElement.Method.Name, new MethodAccessorPlugin(methodAsDelegateElement.Method, methodAsDelegateElement.DelegateType), methodAsDelegateElement.AcceptsNull);
						}
					}
					else
					{
						expressionNode = new MethodCommandNode(methodAsCommandElement.MethodName, methodAsCommandElement.ExecuteMethod, methodAsCommandElement.CanExecuteMethod, methodAsCommandElement.DependsOnProperties);
					}
				}
				else
				{
					expressionNode = new PropertyAccessorNode(propertyElement.Property.Name, new PropertyInfoAccessorPlugin(propertyElement.Property, propertyElement.AccessorFactory), propertyElement.AcceptsNull);
				}
			}
			else
			{
				num++;
				expressionNode = null;
			}
			if (expressionNode != null)
			{
				result.Add(expressionNode);
			}
		}
		for (int j = 0; j < num; j++)
		{
			result.Add(new LogicalNotNode());
		}
	}

	/// <inheritdoc />
	public override string ToString()
	{
		return string.Concat((IEnumerable<ICompiledBindingPathElement>)_elements);
	}
}
