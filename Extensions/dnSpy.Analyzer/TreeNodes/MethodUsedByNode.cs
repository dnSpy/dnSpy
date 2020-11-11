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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnSpy.Analyzer.Properties;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Text;

namespace dnSpy.Analyzer.TreeNodes {
	sealed class MethodUsedByNode : SearchNode {
		readonly MethodDef analyzedMethod;
		readonly bool isSetter;
		readonly UTF8String? implMapName;
		readonly string? implMapModule;
		PropertyDef? property;
		ConcurrentDictionary<MethodDef, int>? foundMethods;

		public MethodUsedByNode(MethodDef analyzedMethod, bool isSetter) {
			this.analyzedMethod = analyzedMethod ?? throw new ArgumentNullException(nameof(analyzedMethod));
			this.isSetter = isSetter;
			if (analyzedMethod.ImplMap is ImplMap implMap) {
				implMapName = GetDllImportMethodName(analyzedMethod, implMap);
				implMapModule = NormalizeModuleName(implMap.Module?.Name);
			}
		}

		static UTF8String GetDllImportMethodName(MethodDef method, ImplMap implMap) {
			if (!UTF8String.IsNullOrEmpty(implMap.Name))
				return implMap.Name;
			return method.Name;
		}

		static string NormalizeModuleName(string name) {
			if (string.IsNullOrEmpty(name))
				return string.Empty;
			if (name.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
				name = name.Substring(0, name.Length - 4);
			else {
				if (name.StartsWith("lib", StringComparison.Ordinal))
					name = name.Substring(3);

				if (name.EndsWith(".so", StringComparison.Ordinal))
					name = name.Substring(0, name.Length - 3);
				else if (name.EndsWith(".dylib", StringComparison.OrdinalIgnoreCase))
					name = name.Substring(0, name.Length - 6);
				else if (name.EndsWith(".a", StringComparison.Ordinal))
					name = name.Substring(0, name.Length - 2);
				else if (name.EndsWith(".sl", StringComparison.Ordinal))
					name = name.Substring(0, name.Length - 3);
			}
			return name;
		}

		protected override void Write(ITextColorWriter output, IDecompiler decompiler) =>
			output.Write(BoxedTextColor.Text, dnSpy_Analyzer_Resources.UsedByTreeNode);

		protected override IEnumerable<AnalyzerTreeNodeData> FetchChildren(CancellationToken ct) {
			foundMethods = new ConcurrentDictionary<MethodDef, int>();

			if (isSetter)
				property = analyzedMethod.DeclaringType.Properties.FirstOrDefault(a => a.SetMethod == analyzedMethod);

			var includeAllModules = (property is not null && CustomAttributesUtils.IsPseudoCustomAttributeType(analyzedMethod.DeclaringType)) || implMapName is not null;
			var options = ScopedWhereUsedAnalyzerOptions.None;
			if (includeAllModules)
				options |= ScopedWhereUsedAnalyzerOptions.IncludeAllModules;
			if (implMapName is not null)
				options |= ScopedWhereUsedAnalyzerOptions.ForcePublic;
			var analyzer = new ScopedWhereUsedAnalyzer<AnalyzerTreeNodeData>(Context.DocumentService, analyzedMethod, FindReferencesInType, options);
			foreach (var child in analyzer.PerformAnalysis(ct)) {
				yield return child;
			}

			if (property is not null) {
				var hash = new HashSet<AssemblyDef>();
				foreach (var module in analyzer.AllModules) {
					if (module.Assembly is AssemblyDef asm && hash.Add(asm)) {
						foreach (var node in FieldAccessNode.CheckCustomAttributeNamedArgumentWrite(Context, asm, property))
							yield return node;
					}
					foreach (var node in FieldAccessNode.CheckCustomAttributeNamedArgumentWrite(Context, module, property))
						yield return node;
				}
			}

			foundMethods = null;
		}

		IEnumerable<AnalyzerTreeNodeData> FindReferencesInType(TypeDef type) {
			string name = analyzedMethod.Name;
			foreach (MethodDef method in type.Methods) {
				if (!method.HasBody)
					continue;
				Instruction? foundInstr = null;
				if (implMapName is not null) {
					foreach (var instr in method.Body.Instructions) {
						if (instr.Operand is IMethod mr && !mr.IsField &&
							mr.ResolveMethodDef() is MethodDef md &&
							// DllImport methods are the same if module + func name are identical
							md?.ImplMap is ImplMap otherImplMap &&
							implMapName == GetDllImportMethodName(md, otherImplMap) &&
							StringComparer.OrdinalIgnoreCase.Equals(implMapModule, NormalizeModuleName(otherImplMap.Module?.Name))) {
							foundInstr = instr;
							break;
						}
					}
				}
				else {
					foreach (var instr in method.Body.Instructions) {
						if (instr.Operand is IMethod mr && !mr.IsField && mr.Name == name &&
							Helpers.IsReferencedBy(analyzedMethod.DeclaringType, mr.DeclaringType) &&
							CheckEquals(mr.ResolveMethodDef(), analyzedMethod)) {
							foundInstr = instr;
							break;
						}
					}
				}

				if (foundInstr is not null) {
					if (GetOriginalCodeLocation(method) is MethodDef codeLocation && !HasAlreadyBeenFound(codeLocation)) {
						var node = new MethodNode(codeLocation) { Context = Context };
						if (codeLocation == method)
							node.SourceRef = new SourceRef(method, foundInstr.Offset, foundInstr.Operand as IMDTokenProvider);
						yield return node;
					}
				}
			}

			if (property is not null) {
				foreach (var node in FieldAccessNode.CheckCustomAttributeNamedArgumentWrite(Context, type, property)) {
					if (node is MethodNode methodNode && methodNode.Member is MethodDef method && HasAlreadyBeenFound(method))
						continue;
					yield return node;
				}
			}
		}

		bool HasAlreadyBeenFound(MethodDef method) => !foundMethods!.TryAdd(method, 0);
	}
}
