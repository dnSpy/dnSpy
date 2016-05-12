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
using dnlib.DotNet;
using dnSpy.Analyzer.Properties;
using dnSpy.Contracts.Languages;
using dnSpy.Contracts.Text;
using dnSpy.Decompiler.Shared;

namespace dnSpy.Analyzer.TreeNodes {
	sealed class TypeUsedByNode : SearchNode {
		readonly TypeDef analyzedType;

		public TypeUsedByNode(TypeDef analyzedType) {
			if (analyzedType == null)
				throw new ArgumentNullException(nameof(analyzedType));

			this.analyzedType = analyzedType;
		}

		protected override void Write(IOutputColorWriter output, ILanguage language) =>
			output.Write(BoxedOutputColor.Text, dnSpy_Analyzer_Resources.UsedByTreeNode);

		protected override IEnumerable<IAnalyzerTreeNodeData> FetchChildren(CancellationToken ct) {
			var analyzer = new ScopedWhereUsedAnalyzer<IAnalyzerTreeNodeData>(Context.FileManager, analyzedType, FindTypeUsage);
			return analyzer.PerformAnalysis(ct)
				.Cast<EntityNode>()
				.Where(n => n.Member.DeclaringType != analyzedType)
				.Distinct(new AnalyzerEntityTreeNodeComparer());
		}

		IEnumerable<EntityNode> FindTypeUsage(TypeDef type) {
			if (type == null)
				yield break;
			if (type == analyzedType)
				yield break;

			if (IsUsedInTypeDef(type))
				yield return new TypeNode(type) { Context = Context };

			foreach (var field in type.Fields.Where(IsUsedInFieldRef))
				yield return new FieldNode(field) { Context = Context };

			foreach (var method in type.Methods) {
				SourceRef? sourceRef = null;
				if (IsUsedInMethodDef(method, ref sourceRef))
					yield return HandleSpecialMethodNode(method, sourceRef);
			}
		}

		EntityNode HandleSpecialMethodNode(MethodDef method, SourceRef? sourceRef) {
			var property = method.DeclaringType.Properties.FirstOrDefault(p => p.GetMethod == method || p.SetMethod == method);
			if (property != null)
				return new PropertyNode(property) { Context = Context, SourceRef = sourceRef };

			return new MethodNode(method) { Context = Context, SourceRef = sourceRef };
		}

		bool IsUsedInTypeRefs(IEnumerable<ITypeDefOrRef> types) => types.Any(IsUsedInTypeRef);

		bool IsUsedInTypeRef(ITypeDefOrRef type) {
			if (type == null)
				return false;

			return TypeMatches(type.DeclaringType)
				|| TypeMatches(type);
		}

		bool IsUsedInTypeDef(TypeDef type) {
			if (type == null)
				return false;

			return IsUsedInTypeRef(type)
				   || TypeMatches(type.BaseType)
				   || IsUsedInTypeRefs(type.Interfaces.Select(ii => ii.Interface));
		}

		bool IsUsedInFieldRef(IField field) {
			if (field == null || !field.IsField)
				return false;

			return TypeMatches(field.DeclaringType)
				|| TypeMatches(field.FieldSig.GetFieldType());
		}

		bool IsUsedInMethodRef(IMethod method) {
			if (method == null || !method.IsMethod)
				return false;

			return TypeMatches(method.DeclaringType)
				   || TypeMatches(method.MethodSig.GetRetType())
				   || IsUsedInMethodParameters(method.GetParameters());
		}

		bool IsUsedInMethodDef(MethodDef method, ref SourceRef? sourceRef) => IsUsedInMethodRef(method)
	   || IsUsedInMethodBody(method, ref sourceRef);

		bool IsUsedInMethodBody(MethodDef method, ref SourceRef? sourceRef) {
			if (method == null)
				return false;
			if (method.Body == null)
				return false;

			foreach (var instruction in method.Body.Instructions) {
				ITypeDefOrRef tr = instruction.Operand as ITypeDefOrRef;
				if (IsUsedInTypeRef(tr)) {
					sourceRef = new SourceRef(method, instruction.Offset, instruction.Operand as IMDTokenProvider);
					return true;
				}
				IField fr = instruction.Operand as IField;
				if (IsUsedInFieldRef(fr)) {
					sourceRef = new SourceRef(method, instruction.Offset, instruction.Operand as IMDTokenProvider);
					return true;
				}
				IMethod mr = instruction.Operand as IMethod;
				if (IsUsedInMethodRef(mr)) {
					sourceRef = new SourceRef(method, instruction.Offset, instruction.Operand as IMDTokenProvider);
					return true;
				}
			}

			return false;
		}

		bool IsUsedInMethodParameters(IEnumerable<Parameter> parameters) => parameters.Any(IsUsedInMethodParameter);
		bool IsUsedInMethodParameter(Parameter parameter) => !parameter.IsHiddenThisParameter && TypeMatches(parameter.Type);
		bool TypeMatches(IType tref) => tref != null && new SigComparer().Equals(analyzedType, tref);
		public static bool CanShow(TypeDef type) => type != null;
	}

	sealed class AnalyzerEntityTreeNodeComparer : IEqualityComparer<EntityNode> {
		public bool Equals(EntityNode x, EntityNode y) => x.Member == y.Member;
		public int GetHashCode(EntityNode node) => node.Member.GetHashCode();
	}
}
