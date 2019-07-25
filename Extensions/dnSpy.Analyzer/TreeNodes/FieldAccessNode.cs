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
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnSpy.Analyzer.Properties;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Text;

namespace dnSpy.Analyzer.TreeNodes {
	sealed class FieldAccessNode : SearchNode {
		readonly bool showWrites; // true: show writes; false: show read access
		readonly FieldDef analyzedField;
		Lazy<Hashtable>? foundMethods;
		readonly object hashLock = new object();

		public FieldAccessNode(FieldDef analyzedField, bool showWrites) {
			this.analyzedField = analyzedField ?? throw new ArgumentNullException(nameof(analyzedField));
			this.showWrites = showWrites;
		}

		protected override void Write(ITextColorWriter output, IDecompiler decompiler) =>
			output.Write(BoxedTextColor.Text, showWrites ? dnSpy_Analyzer_Resources.AssignedByTreeNode : dnSpy_Analyzer_Resources.ReadByTreeNode);

		protected override IEnumerable<AnalyzerTreeNodeData> FetchChildren(CancellationToken ct) {
			foundMethods = new Lazy<Hashtable>(LazyThreadSafetyMode.ExecutionAndPublication);

			var includeAllModules = showWrites && CustomAttributesUtils.IsPseudoCustomAttributeType(analyzedField.DeclaringType);
			var options = ScopedWhereUsedAnalyzerOptions.None;
			if (includeAllModules)
				options |= ScopedWhereUsedAnalyzerOptions.IncludeAllModules;
			var analyzer = new ScopedWhereUsedAnalyzer<AnalyzerTreeNodeData>(Context.DocumentService, analyzedField, FindReferencesInType, options);
			foreach (var child in analyzer.PerformAnalysis(ct)) {
				yield return child;
			}

			foundMethods = null;

			if (showWrites) {
				var hash = new HashSet<AssemblyDef>();
				foreach (var module in analyzer.AllModules) {
					if (module.Assembly is AssemblyDef asm && hash.Add(asm)) {
						foreach (var node in CheckCustomAttributeNamedArgumentWrite(Context, asm, analyzedField))
							yield return node;
					}
					foreach (var node in CheckCustomAttributeNamedArgumentWrite(Context, module, analyzedField))
						yield return node;
				}
			}
		}

		IEnumerable<AnalyzerTreeNodeData> FindReferencesInType(TypeDef type) {
			foreach (MethodDef method in type.Methods) {
				Instruction? foundInstr = null;
				if (method.HasBody) {
					foreach (Instruction instr in method.Body.Instructions) {
						if (CanBeReference(instr.OpCode.Code)) {
							IField? fr = instr.Operand as IField;
							if (CheckEquals(fr.ResolveFieldDef(), analyzedField) &&
								Helpers.IsReferencedBy(analyzedField.DeclaringType, fr!.DeclaringType)) {
								foundInstr = instr;
								break;
							}
						}
					}
				}

				if (!(foundInstr is null)) {
					if (GetOriginalCodeLocation(method) is MethodDef codeLocation && !HasAlreadyBeenFound(codeLocation)) {
						var node = new MethodNode(codeLocation) { Context = Context };
						if (codeLocation == method)
							node.SourceRef = new SourceRef(method, foundInstr.Offset, foundInstr.Operand as IMDTokenProvider);
						yield return node;
					}
				}
			}

			if (showWrites) {
				foreach (var node in CheckCustomAttributeNamedArgumentWrite(Context, type, analyzedField)) {
					if (node is MethodNode methodNode && methodNode.Member is MethodDef method && HasAlreadyBeenFound(method))
						continue;
					yield return node;
				}
			}
		}

		internal static IEnumerable<AnalyzerTreeNodeData> CheckCustomAttributeNamedArgumentWrite(IAnalyzerTreeNodeDataContext context, AssemblyDef asm, IMemberRef member) {
			if (TryGetCAWrite(asm, member, member.IsField, out var ca))
				yield return new AssemblyNode(asm) { Context = context };
		}

		internal static IEnumerable<AnalyzerTreeNodeData> CheckCustomAttributeNamedArgumentWrite(IAnalyzerTreeNodeDataContext context, ModuleDef module, IMemberRef member) {
			if (HasWrite(module, member, member.IsField))
				yield return new ModuleNode(module) { Context = context };
		}

		internal static IEnumerable<AnalyzerTreeNodeData> CheckCustomAttributeNamedArgumentWrite(IAnalyzerTreeNodeDataContext context, TypeDef type, IMemberRef member) {
			bool isField = member.IsField;
			if (HasWrite(type, member, isField))
				yield return new TypeNode(type) { Context = context };
			foreach (var method in type.Methods) {
				if (HasWrite(method, member, isField))
					yield return new MethodNode(method) { Context = context };
				foreach (var pd in method.ParamDefs) {
					if (HasWrite(pd, member, isField))
						yield return new MethodNode(method) { Context = context };
				}
			}
			foreach (var field in type.Fields) {
				if (HasWrite(field, member, isField))
					yield return new FieldNode(field) { Context = context };
			}
			foreach (var property in type.Properties) {
				if (HasWrite(property, member, isField))
					yield return new PropertyNode(property) { Context = context };
			}
			foreach (var @event in type.Events) {
				if (HasWrite(@event, member, isField))
					yield return new EventNode(@event) { Context = context };
			}
		}

		static bool HasWrite(IHasCustomAttribute hca, IMemberRef member, bool isField) =>
			TryGetCAWrite(hca, member, isField, out _);

		static bool TryGetCAWrite(IHasCustomAttribute hca, IMemberRef member, bool isField, [NotNullWhen(true)] out CustomAttribute? customAttribute) {
			customAttribute = null;
			TypeSig? memberType;
			if (isField)
				memberType = ((IField)member).FieldSig?.GetFieldType();
			else {
				var property = (PropertyDef)member;
				var propSig = property.PropertySig;
				if (propSig is null || propSig.Params.Count != 0)
					return false;
				memberType = propSig?.GetRetType();
			}
			foreach (var ca in hca.GetCustomAttributes()) {
				if (!new SigComparer().Equals(ca.AttributeType.GetScopeType(), member.DeclaringType))
					continue;
				var namedArgs = ca.NamedArguments;
				for (int i = 0; i < namedArgs.Count; i++) {
					var namedArg = namedArgs[i];
					if (namedArg.IsField != isField)
						continue;
					if (namedArg.Name != member.Name)
						continue;
					if (!new SigComparer().Equals(namedArg.Type, memberType))
						continue;

					customAttribute = ca;
					return true;
				}
			}

			return false;
		}

		bool CanBeReference(Code code) {
			switch (code) {
			case Code.Ldfld:
			case Code.Ldsfld:
				return !showWrites;
			case Code.Stfld:
			case Code.Stsfld:
				return showWrites;
			case Code.Ldflda:
			case Code.Ldsflda:
			case Code.Ldtoken:
				return true; // always show address-loading
			default:
				return false;
			}
		}

		bool HasAlreadyBeenFound(MethodDef method) {
			Hashtable hashtable = foundMethods!.Value;
			lock (hashLock) {
				if (hashtable.Contains(method)) {
					return true;
				}
				else {
					hashtable.Add(method, null);
					return false;
				}
			}
		}
	}
}
