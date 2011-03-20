// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.CSharp.PatternMatching;

namespace ICSharpCode.Decompiler.Ast
{
	static class NRefactoryExtensions
	{
		public static T WithAnnotation<T>(this T node, object annotation) where T : AstNode
		{
			if (annotation != null)
				node.AddAnnotation(annotation);
			return node;
		}
		
		public static T CopyAnnotationsFrom<T>(this T node, AstNode other) where T : AstNode
		{
			foreach (object annotation in other.Annotations) {
				node.AddAnnotation(annotation);
			}
			return node;
		}
		
		public static T Detach<T>(this T node) where T : AstNode
		{
			node.Remove();
			return node;
		}
		
		public static Expression WithName(this Expression node, string patternGroupName)
		{
			return new NamedNode(patternGroupName, node);
		}
		
		public static Statement WithName(this Statement node, string patternGroupName)
		{
			return new NamedNode(patternGroupName, node);
		}
		
		public static void AddNamedArgument(this NRefactory.CSharp.Attribute attribute, string name, Expression argument)
		{
			attribute.Arguments.Add(new AssignmentExpression(new IdentifierExpression(name), argument));
		}
	}
}
