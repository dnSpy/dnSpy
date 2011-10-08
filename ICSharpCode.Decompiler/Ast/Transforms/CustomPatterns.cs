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
using System.Linq;
using System.Reflection;
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.PatternMatching;
using Mono.Cecil;

namespace ICSharpCode.Decompiler.Ast.Transforms
{
	sealed class TypePattern : Pattern
	{
		readonly string ns;
		readonly string name;
		
		public TypePattern(Type type)
		{
			this.ns = type.Namespace;
			this.name = type.Name;
		}
		
		public override bool DoMatch(INode other, Match match)
		{
			ComposedType ct = other as ComposedType;
			AstType o;
			if (ct != null && !ct.HasNullableSpecifier && ct.PointerRank == 0 && !ct.ArraySpecifiers.Any()) {
				// Special case: ILSpy sometimes produces a ComposedType but then removed all array specifiers
				// from it. In that case, we need to look at the base type for the annotations.
				o = ct.BaseType;
			} else {
				o = other as AstType;
				if (o == null)
					return false;
			}
			TypeReference tr = o.Annotation<TypeReference>();
			return tr != null && tr.Namespace == ns && tr.Name == name;
		}
		
		public override string ToString()
		{
			return name;
		}
	}
	
	sealed class LdTokenPattern : Pattern
	{
		AnyNode childNode;
		
		public LdTokenPattern(string groupName)
		{
			this.childNode = new AnyNode(groupName);
		}
		
		public override bool DoMatch(INode other, Match match)
		{
			InvocationExpression ie = other as InvocationExpression;
			if (ie != null && ie.Annotation<LdTokenAnnotation>() != null && ie.Arguments.Count == 1) {
				return childNode.DoMatch(ie.Arguments.Single(), match);
			}
			return false;
		}
		
		public override string ToString()
		{
			return "ldtoken(...)";
		}
	}
	
	/// <summary>
	/// typeof-Pattern that applies on the expanded form of typeof (prior to ReplaceMethodCallsWithOperators)
	/// </summary>
	sealed class TypeOfPattern : Pattern
	{
		INode childNode;
		
		public TypeOfPattern(string groupName)
		{
			childNode = new TypePattern(typeof(Type)).ToType().Invoke(
				"GetTypeFromHandle", new TypeOfExpression(new AnyNode(groupName)).Member("TypeHandle"));
		}
		
		public override bool DoMatch(INode other, Match match)
		{
			return childNode.DoMatch(other, match);
		}
		
		public override string ToString()
		{
			return "typeof(...)";
		}
	}
}
