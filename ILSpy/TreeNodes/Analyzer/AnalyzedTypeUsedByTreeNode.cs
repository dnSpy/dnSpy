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
using System.Linq;
using System.Threading;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace ICSharpCode.ILSpy.TreeNodes.Analyzer
{
	internal sealed class AnalyzedTypeUsedByTreeNode : AnalyzerSearchTreeNode
	{
		private readonly TypeDefinition analyzedType;

		public AnalyzedTypeUsedByTreeNode(TypeDefinition analyzedType)
		{
			if (analyzedType == null)
				throw new ArgumentNullException("analyzedType");

			this.analyzedType = analyzedType;
		}

		public override object Text
		{
			get { return "Used By"; }
		}

		protected override IEnumerable<AnalyzerTreeNode> FetchChildren(CancellationToken ct)
		{
			var analyzer = new ScopedWhereUsedAnalyzer<AnalyzerTreeNode>(analyzedType, FindTypeUsage);
			return analyzer.PerformAnalysis(ct)
				.Cast<AnalyzerEntityTreeNode>()
				.Where(n => n.Member.DeclaringType != analyzedType)
				.Distinct(new AnalyzerEntityTreeNodeComparer())
				.OrderBy(n => n.Text);
		}

		private IEnumerable<AnalyzerEntityTreeNode> FindTypeUsage(TypeDefinition type)
		{
			if (type == analyzedType)
				yield break;

			if (IsUsedInTypeDefinition(type))
				yield return new AnalyzedTypeTreeNode(type) { Language = Language };

			foreach (var field in type.Fields.Where(IsUsedInFieldReference))
				yield return new AnalyzedFieldTreeNode(field) { Language = Language };

			foreach (var method in type.Methods.Where(IsUsedInMethodDefinition))
				yield return HandleSpecialMethodNode(method);
		}

		private AnalyzerEntityTreeNode HandleSpecialMethodNode(MethodDefinition method)
		{
			var property = method.DeclaringType.Properties.FirstOrDefault(p => p.GetMethod == method || p.SetMethod == method);
			if (property != null)
				return new AnalyzedPropertyTreeNode(property) { Language = Language };

			return new AnalyzedMethodTreeNode(method) { Language = Language };
		}

		private bool IsUsedInTypeReferences(IEnumerable<TypeReference> types)
		{
			return types.Any(IsUsedInTypeReference);
		}

		private bool IsUsedInTypeReference(TypeReference type)
		{
			if (type == null)
				return false;

			return TypeMatches(type.DeclaringType)
				|| TypeMatches(type);
		}

		private bool IsUsedInTypeDefinition(TypeDefinition type)
		{
			return IsUsedInTypeReference(type)
				   || TypeMatches(type.BaseType)
				   || IsUsedInTypeReferences(type.Interfaces);
		}

		private bool IsUsedInFieldReference(FieldReference field)
		{
			if (field == null)
				return false;

			return TypeMatches(field.DeclaringType)
				|| TypeMatches(field.FieldType);
		}

		private bool IsUsedInMethodReference(MethodReference method)
		{
			if (method == null)
				return false;

			return TypeMatches(method.DeclaringType)
				   || TypeMatches(method.ReturnType)
				   || IsUsedInMethodParameters(method.Parameters);
		}

		private bool IsUsedInMethodDefinition(MethodDefinition method)
		{
			return IsUsedInMethodReference(method)
				   || IsUsedInMethodBody(method);
		}

		private bool IsUsedInMethodBody(MethodDefinition method)
		{
			if (method.Body == null)
				return false;

			bool found = false;

			foreach (var instruction in method.Body.Instructions) {
				TypeReference tr = instruction.Operand as TypeReference;
				if (IsUsedInTypeReference(tr)) {
					found = true;
					break;
				}
				FieldReference fr = instruction.Operand as FieldReference;
				if (IsUsedInFieldReference(fr)) {
					found = true;
					break;
				}
				MethodReference mr = instruction.Operand as MethodReference;
				if (IsUsedInMethodReference(mr)) {
					found = true;
					break;
				}
			}

			method.Body = null; // discard body to reduce memory pressure & higher GC gen collections

			return found;
		}

		private bool IsUsedInMethodParameters(IEnumerable<ParameterDefinition> parameters)
		{
			return parameters.Any(IsUsedInMethodParameter);
		}

		private bool IsUsedInMethodParameter(ParameterDefinition parameter)
		{
			return TypeMatches(parameter.ParameterType);
		}

		private bool TypeMatches(TypeReference tref)
		{
			if (tref != null && tref.Name == analyzedType.Name) {
				var tdef = tref.Resolve();
				if (tdef != null) {
					return (tdef == analyzedType);
				}
			}
			return false;
		}

		public static bool CanShow(TypeDefinition type)
		{
			return type != null;
		}
	}

	internal class AnalyzerEntityTreeNodeComparer : IEqualityComparer<AnalyzerEntityTreeNode>
	{
		public bool Equals(AnalyzerEntityTreeNode x, AnalyzerEntityTreeNode y)
		{
			return x.Member == y.Member;
		}

		public int GetHashCode(AnalyzerEntityTreeNode node)
		{
			return node.Member.GetHashCode();
		}
	}

}
