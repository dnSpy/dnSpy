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
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Text;

namespace dnSpy.Analyzer.TreeNodes {
	sealed class InterfaceMethodImplementedByNode : SearchNode {
		readonly MethodDef analyzedMethod;
		Guid comGuid;
		bool isComType;
		int vtblIndex;

		public InterfaceMethodImplementedByNode(MethodDef analyzedMethod) => this.analyzedMethod = analyzedMethod ?? throw new ArgumentNullException(nameof(analyzedMethod));

		protected override void Write(ITextColorWriter output, IDecompiler decompiler) =>
			output.Write(BoxedTextColor.Text, dnSpy_Analyzer_Resources.ImplementedByTreeNode);

		protected override IEnumerable<AnalyzerTreeNodeData> FetchChildren(CancellationToken ct) {
			ComUtils.GetMemberInfo(analyzedMethod, out isComType, out comGuid, out vtblIndex);
			bool includeAllModules = isComType;
			var options = ScopedWhereUsedAnalyzerOptions.None;
			if (includeAllModules)
				options |= ScopedWhereUsedAnalyzerOptions.IncludeAllModules;
			if (isComType)
				options |= ScopedWhereUsedAnalyzerOptions.ForcePublic;
			var analyzer = new ScopedWhereUsedAnalyzer<AnalyzerTreeNodeData>(Context.DocumentService, analyzedMethod, FindReferencesInType, options);
			return analyzer.PerformAnalysis(ct);
		}

		IEnumerable<AnalyzerTreeNodeData> FindReferencesInType(TypeDef type) {
			if (type.IsInterface)
				yield break;
			var implementedInterfaceRef = GetInterface(type, analyzedMethod.DeclaringType);
			var comIface = isComType ? GetComInterface(type, ref comGuid) : null;

			foreach (var method in type.Methods) {
				// Don't include abstract methods, they don't implement anything
				if (!method.IsVirtual || method.IsAbstract)
					continue;
				if (method.HasOverrides && method.Overrides.Any(m => CheckOverride(m))) {
					yield return new MethodNode(method) { Context = Context };
					yield break;
				}
			}

			if (comIface is not null && ComUtils.GetMethod(comIface, vtblIndex) is MethodDef comIfaceMethod) {
				foreach (var method in type.Methods) {
					// Don't include abstract methods, they don't implement anything
					if (!method.IsVirtual || method.IsAbstract)
						continue;
					if (TypesHierarchyHelpers.MatchInterfaceMethod(method, comIfaceMethod, comIface)) {
						yield return new MethodNode(method) { Context = Context };
						yield break;
					}
				}
			}

			if (implementedInterfaceRef is not null) {
				foreach (var method in type.Methods) {
					// Don't include abstract methods, they don't implement anything
					if (!method.IsVirtual || method.IsAbstract)
						continue;
					if (method.Name != analyzedMethod.Name)
						continue;
					if (TypesHierarchyHelpers.MatchInterfaceMethod(method, analyzedMethod, implementedInterfaceRef)) {
						yield return new MethodNode(method) { Context = Context };
						yield break;
					}
				}
			}
		}

		bool CheckOverride(MethodOverride m) {
			if (!(m.MethodDeclaration.ResolveMethodDef() is MethodDef method))
				return false;
			if (isComType) {
				ComUtils.GetMemberInfo(method, out bool otherIsComType, out var otherComGuid, out int otherVtblIndex);
				if (otherIsComType && otherComGuid == comGuid && otherVtblIndex == vtblIndex)
					return true;
			}
			return CheckEquals(method, analyzedMethod);
		}

		internal static ITypeDefOrRef? GetInterface(TypeDef type, TypeDef interfaceType) {
			foreach (var t in TypesHierarchyHelpers.GetTypeAndBaseTypes(type)) {
				var td = t.Resolve();
				if (td is null)
					break;
				foreach (var ii in td.Interfaces) {
					var genericArgs = t is GenericInstSig ? ((GenericInstSig)t).GenericArguments : null;
					var iface = GenericArgumentResolver.Resolve(ii.Interface.ToTypeSig(), genericArgs, null);
					if (iface is null)
						continue;
					if (new SigComparer().Equals(ii.Interface.GetScopeType(), interfaceType))
						return iface.ToTypeDefOrRef();
				}
			}
			return null;
		}

		static TypeDef? GetComInterface(TypeDef type, ref Guid comGuid) {
			foreach (var t in TypesHierarchyHelpers.GetTypeAndBaseTypes(type)) {
				var td = t.Resolve();
				if (td is null)
					break;
				foreach (var ii in td.Interfaces) {
					if (ii.Interface.GetScopeType().ResolveTypeDef() is TypeDef iface && ComUtils.ComEquals(iface, ref comGuid))
						return iface;
				}
			}
			return null;
		}

		public static bool CanShow(MethodDef method) => method.DeclaringType.IsInterface && (method.IsVirtual || method.IsAbstract);
	}
}
