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

namespace ICSharpCode.ILSpy.TreeNodes.Analyzer
{
	internal sealed class AnalyzedTypeExposedByTreeNode : AnalyzerSearchTreeNode
	{
		private readonly TypeDef analyzedType;

		public AnalyzedTypeExposedByTreeNode(TypeDef analyzedType)
		{
			if (analyzedType == null)
				throw new ArgumentNullException("analyzedType");

			this.analyzedType = analyzedType;
		}

		public override object Text
		{
			get { return "Exposed By"; }
		}

		protected override IEnumerable<AnalyzerTreeNode> FetchChildren(CancellationToken ct)
		{
			var analyzer = new ScopedWhereUsedAnalyzer<AnalyzerTreeNode>(analyzedType, FindReferencesInType);
			return analyzer.PerformAnalysis(ct).OrderBy(n => n.Text);
		}

		private IEnumerable<AnalyzerTreeNode> FindReferencesInType(TypeDef type)
		{
			if (analyzedType.IsEnum && type == analyzedType)
				yield break;

			if (!this.Language.ShowMember(type))
				yield break;

			foreach (FieldDef field in type.Fields) {
				if (TypeIsExposedBy(field)) {
					var node = new AnalyzedFieldTreeNode(field);
					node.Language = this.Language;
					yield return node;
				}
			}

			foreach (PropertyDef property in type.Properties) {
				if (TypeIsExposedBy(property)) {
					var node = new AnalyzedPropertyTreeNode(property);
					node.Language = this.Language;
					yield return node;
				}
			}

			foreach (EventDef eventDef in type.Events) {
				if (TypeIsExposedBy(eventDef)) {
					var node = new AnalyzedEventTreeNode(eventDef);
					node.Language = this.Language;
					yield return node;
				}
			}

			foreach (MethodDef method in type.Methods) {
				if (TypeIsExposedBy(method)) {
					var node = new AnalyzedMethodTreeNode(method);
					node.Language = this.Language;
					yield return node;
				}
			}
		}

		private bool TypeIsExposedBy(FieldDef field)
		{
			if (field.IsPrivate)
				return false;

			if (Decompiler.DnlibExtensions.Resolve(field.FieldType) == analyzedType)
				return true;

			return false;
		}

		private bool TypeIsExposedBy(PropertyDef property)
		{
			if (IsPrivate(property))
				return false;

			if (Decompiler.DnlibExtensions.Resolve(property.PropertySig.RetType) == analyzedType)
				return true;

			return false;
		}

		private bool TypeIsExposedBy(EventDef eventDef)
		{
			if (IsPrivate(eventDef))
				return false;

			if (eventDef.EventType.ResolveTypeDef() == analyzedType)
				return true;

			return false;
		}

		private bool TypeIsExposedBy(MethodDef method)
		{
			// if the method has overrides, it is probably an explicit interface member
			// and should be considered part of the public API even though it is marked private.
			if (method.IsPrivate) {
				if (!method.HasOverrides)
					return false;
				var typeDefinition = method.Overrides[0].MethodDeclaration.DeclaringType.ResolveTypeDef();
				if (typeDefinition != null && !typeDefinition.IsInterface)
					return false;
			}

			// exclude methods with 'semantics'. for example, property getters & setters.
			// HACK: this is a potentially fragile implementation, as the MethodSemantics may be extended to other uses at a later date.
			if (Decompiler.DnlibExtensions.HasSemantics(method))
				return false;

			if (Decompiler.DnlibExtensions.Resolve(method.ReturnType) == analyzedType)
				return true;

			foreach (var parameter in method.Parameters) {
				if (parameter.IsHiddenThisParameter)
					continue;
				if (Decompiler.DnlibExtensions.Resolve(parameter.Type) == analyzedType)
					return true;
			}

			return false;
		}

		private static bool IsPrivate(PropertyDef property)
		{
			bool isGetterPublic = (property.GetMethod != null && !property.GetMethod.IsPrivate);
			bool isSetterPublic = (property.SetMethod != null && !property.SetMethod.IsPrivate);
			return !(isGetterPublic || isSetterPublic);
		}

		private static bool IsPrivate(EventDef eventDef)
		{
			bool isAdderPublic = (eventDef.AddMethod != null && !eventDef.AddMethod.IsPrivate);
			bool isRemoverPublic = (eventDef.RemoveMethod != null && !eventDef.RemoveMethod.IsPrivate);
			return !(isAdderPublic || isRemoverPublic);
		}

		public static bool CanShow(TypeDef type)
		{
			return true;
		}
	}
}
