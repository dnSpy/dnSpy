// Copyright (c) 2011 AlphaSierraPapa for the SharpDevelop Team
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using System;
using System.Collections.Generic;
using ICSharpCode.Decompiler.ILAst;
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.PatternMatching;
using dnlib.DotNet;

namespace ICSharpCode.Decompiler.Ast
{
	public static class NRefactoryExtensions
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
		
		public static void AddNamedArgument(this NRefactory.CSharp.Attribute attribute, ModuleDef module, Type attrType, Type fieldType, string fieldName, Expression argument)
		{
			var ide = new IdentifierExpression(fieldName);
			if (module != null) {
				TypeSig sig = module.CorLibTypes.GetCorLibTypeSig(module.Import(fieldType));
				if (sig == null) {
					var typeRef = module.CorLibTypes.GetTypeRef(fieldType.Namespace, fieldType.Name);
					sig = fieldType.IsValueType ? (TypeSig)new ValueTypeSig(typeRef) : new ClassSig(typeRef);
				}
				var fr = new MemberRefUser(module, fieldName, new FieldSig(sig), module.CorLibTypes.GetTypeRef(attrType.Namespace, attrType.Name));
				ide.AddAnnotation(fr);
				ide.IdentifierToken.AddAnnotation(fr);
			}

			attribute.Arguments.Add(new AssignmentExpression(ide, argument));
		}
		
		public static AstType ToType(this Pattern pattern)
		{
			return pattern;
		}
		
		public static Expression ToExpression(this Pattern pattern)
		{
			return pattern;
		}
		
		public static Statement ToStatement(this Pattern pattern)
		{
			return pattern;
		}
		
		public static Statement GetNextStatement(this Statement statement)
		{
			AstNode next = statement.NextSibling;
			while (next != null && !(next is Statement))
				next = next.NextSibling;
			return (Statement)next;
		}

		public static List<ILRange> GetAllRecursiveILRanges(this AstNode node)
		{
			if (node == null)
				return new List<ILRange>();

			var ilRanges = new List<ILRange>();
			foreach (var d in node.DescendantsAndSelf) {
				foreach (var ann in d.Annotations) {
					var list = ann as IList<ILRange>;
					if (list != null)
						ilRanges.AddRange(list);
				}
			}
			return ilRanges;
		}
	}
}
