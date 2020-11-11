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
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using dnlib.DotNet;
using dnSpy.Analyzer.Properties;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Text;

namespace dnSpy.Analyzer.TreeNodes {
	sealed class TypeUsedByNode : SearchNode {
		readonly TypeDef analyzedType;
		Guid comGuid;
		bool isComType;
		HashSet<ITypeDefOrRef>? allTypes;

		public TypeUsedByNode(TypeDef analyzedType) =>
			this.analyzedType = analyzedType ?? throw new ArgumentNullException(nameof(analyzedType));

		protected override void Write(ITextColorWriter output, IDecompiler decompiler) =>
			output.Write(BoxedTextColor.Text, dnSpy_Analyzer_Resources.UsedByTreeNode);

		protected override IEnumerable<AnalyzerTreeNodeData> FetchChildren(CancellationToken ct) {
			allTypes = new HashSet<ITypeDefOrRef>();
			allTypes.Add(analyzedType);

			bool includeAllModules = CustomAttributesUtils.IsPseudoCustomAttributeType(analyzedType) ||
				CustomAttributesUtils.IsPseudoCustomAttributeOtherType(analyzedType);
			isComType = ComUtils.IsComType(analyzedType, out comGuid);
			includeAllModules |= isComType;
			var options = ScopedWhereUsedAnalyzerOptions.None;
			if (includeAllModules)
				options |= ScopedWhereUsedAnalyzerOptions.IncludeAllModules;
			if (isComType)
				options |= ScopedWhereUsedAnalyzerOptions.ForcePublic;
			var analyzer = new ScopedWhereUsedAnalyzer<AnalyzerTreeNodeData>(Context.DocumentService, analyzedType, FindTypeUsage, options);
			var result = analyzer.PerformAnalysis(ct)
				.Cast<EntityNode>()
				.Where(n => !allTypes.Contains(n.Member!.DeclaringType))
				.Distinct(AnalyzerEntityTreeNodeComparer.Instance)
				.Concat(FindGlobalUsage(analyzer.AllModules));
			foreach (var n in result)
				yield return n;

			allTypes = null;
		}

		IEnumerable<EntityNode> FindGlobalUsage(List<ModuleDef> allModules) {
			var analyzedAssemblies = new HashSet<AssemblyDef>();
			foreach (var module in allModules) {
				bool analyzedTypeIsExported = false;
				foreach (var et in module.ExportedTypes) {
					if (et.MovedToAnotherAssembly && et.Name == analyzedType.Name && et.Namespace == analyzedType.Namespace && et.Resolve() == analyzedType) {
						analyzedTypeIsExported = true;
						break;
					}
				}
				if (module.Assembly is AssemblyDef asm && analyzedAssemblies.Add(asm)) {
					if (analyzedTypeIsExported || IsUsedInCustomAttributes(asm))
						yield return new AssemblyNode(asm) { Context = Context };
				}
				if (IsUsedInCustomAttributes(module))
					yield return new ModuleNode(module) { Context = Context };
			}
		}

		IEnumerable<EntityNode> FindTypeUsage(TypeDef? type) {
			if (type is null)
				yield break;
			if (new SigComparer().Equals(type, analyzedType))
				yield break;
			if (isComType && ComUtils.ComEquals(type, ref comGuid)) {
				Debug2.Assert(allTypes is not null);
				lock (allTypes)
					allTypes.Add(type);
				yield break;
			}

			if (IsUsedInTypeDef(type))
				yield return new TypeNode(type) { Context = Context };

			foreach (var field in type.Fields.Where(IsUsedInFieldDef))
				yield return new FieldNode(field) { Context = Context };

			foreach (var method in type.Methods) {
				SourceRef? sourceRef = null;
				if (IsUsedInMethodDef(method, ref sourceRef))
					yield return HandleSpecialMethodNode(method, sourceRef);
			}

			foreach (var property in type.Properties) {
				if (IsUsedInCustomAttributes(property))
					yield return new PropertyNode(property) { Context = Context };
			}

			foreach (var @event in type.Events) {
				if (IsUsedInCustomAttributes(@event))
					yield return new EventNode(@event) { Context = Context };
			}
		}

		EntityNode HandleSpecialMethodNode(MethodDef method, SourceRef? sourceRef) {
			var property = method.DeclaringType.Properties.FirstOrDefault(p => (object?)p.GetMethod == method || (object?)p.SetMethod == method);
			if (property is not null)
				return new PropertyNode(property) { Context = Context, SourceRef = sourceRef };

			var @event = method.DeclaringType.Events.FirstOrDefault(p => (object?)p.AddMethod == method || (object?)p.RemoveMethod == method || (object?)p.InvokeMethod == method);
			if (@event is not null)
				return new EventNode(@event) { Context = Context, SourceRef = sourceRef };

			return new MethodNode(method) { Context = Context, SourceRef = sourceRef };
		}

		bool IsUsedInTypeRefs(IEnumerable<ITypeDefOrRef> types) => types.Any(IsUsedInTypeRef);

		bool IsUsedInTypeRef(ITypeDefOrRef? type) {
			if (type is null)
				return false;

			return TypeMatches(type.DeclaringType)
				|| TypeMatches(type);
		}

		bool IsUsedInTypeDef(TypeDef? type) {
			if (type is null)
				return false;

			return IsUsedInTypeRef(type)
				   || TypeMatches(type.BaseType)
				   || IsUsedInTypeRefs(type.Interfaces.Select(ii => ii.Interface))
				   || IsUsedInCustomAttributes(type);
		}

		bool IsUsedInCustomAttributes(IHasCustomAttribute? hca) {
			if (hca is null)
				return false;
			foreach (var ca in hca.GetCustomAttributes()) {
				if (IsUsedInMethodRef(ca.Constructor))
					return true;
				foreach (var arg in ca.ConstructorArguments) {
					if (IsUsed(arg, 0))
						return true;
				}
				foreach (var arg in ca.NamedArguments) {
					if (IsUsed(arg.Argument, 0))
						return true;
					if (TypeMatches(arg.Type))
						return true;
				}
			}
			return false;
		}

		const int maxRecursion = 20;
		bool IsUsed(CAArgument arg, int recursionCounter) {
			if (recursionCounter > maxRecursion)
				return false;
			if (TypeMatches(arg.Type))
				return true;
			return ValueMatches(arg.Value, recursionCounter + 1);
		}

		bool ValueMatches(object? value, int recursionCounter) {
			if (recursionCounter > maxRecursion)
				return false;
			if (value is null)
				return false;
			if (value is TypeSig ts)
				return TypeMatches(ts);
			if (value is CAArgument arg)
				return IsUsed(arg, recursionCounter + 1);
			if (value is IList<CAArgument> args) {
				for (int i = 0; i < args.Count; i++) {
					if (IsUsed(args[i], recursionCounter + 1))
						return true;
				}
				return false;
			}
			return false;
		}

		bool IsUsedInFieldDef(FieldDef field) =>
			IsUsedInFieldRef(field) || IsUsedInCustomAttributes(field);

		bool IsUsedInFieldRef(IField? field) {
			if (field is null || !field.IsField)
				return false;

			return TypeMatches(field.DeclaringType)
				|| TypeMatches(field.FieldSig.GetFieldType());
		}

		bool IsUsedInMethodRef(IMethod? method) {
			if (method is null || !method.IsMethod)
				return false;

			if (method is MethodSpec ms && ms.Instantiation is GenericInstMethodSig gims) {
				foreach (var ga in gims.GenericArguments) {
					if (TypeMatches(ga))
						return true;
				}
			}

			return TypeMatches(method.DeclaringType)
				   || TypeMatches(method.MethodSig.GetRetType())
				   || IsUsedInMethodParameters(method.GetParameters());
		}

		bool IsUsedInMethodDef(MethodDef method, ref SourceRef? sourceRef) {
			if (IsUsedInMethodRef(method) || IsUsedInMethodBody(method, ref sourceRef) || IsUsedInCustomAttributes(method))
				return true;
			foreach (var pd in method.ParamDefs) {
				if (IsUsedInCustomAttributes(pd))
					return true;
			}
			return false;
		}

		bool IsUsedInMethodBody(MethodDef? method, ref SourceRef? sourceRef) {
			if (method is null)
				return false;
			if (method.Body is null)
				return false;

			foreach (var instruction in method.Body.Instructions) {
				ITypeDefOrRef? tr = instruction.Operand as ITypeDefOrRef;
				if (IsUsedInTypeRef(tr)) {
					sourceRef = new SourceRef(method, instruction.Offset, instruction.Operand as IMDTokenProvider);
					return true;
				}
				IField? fr = instruction.Operand as IField;
				if (IsUsedInFieldRef(fr)) {
					sourceRef = new SourceRef(method, instruction.Offset, instruction.Operand as IMDTokenProvider);
					return true;
				}
				IMethod? mr = instruction.Operand as IMethod;
				if (IsUsedInMethodRef(mr)) {
					sourceRef = new SourceRef(method, instruction.Offset, instruction.Operand as IMDTokenProvider);
					return true;
				}
			}
			foreach (var local in method.Body.Variables) {
				if (TypeMatches(local.Type)) {
					sourceRef = new SourceRef(method, null, null);
					return true;
				}
			}
			foreach (var eh in method.Body.ExceptionHandlers) {
				if (TypeMatches(eh.CatchType)) {
					sourceRef = new SourceRef(method, null, null);
					return true;
				}
			}

			return false;
		}

		bool IsUsedInMethodParameters(IEnumerable<Parameter> parameters) => parameters.Any(IsUsedInMethodParameter);
		bool IsUsedInMethodParameter(Parameter parameter) => !parameter.IsHiddenThisParameter && TypeMatches(parameter.Type);

		bool TypeMatches(IType? tref) => TypeMatches(tref, 0);
		bool TypeMatches(IType? tref, int level) {
			if (level >= 100)
				return false;
			if (isComType && tref.Resolve() is TypeDef td && ComUtils.ComEquals(td, ref comGuid))
				return true;
			if (tref is not null) {
				if (new SigComparer().Equals(analyzedType, tref.GetScopeType()))
					return true;
				if (tref is TypeSig ts) {
					switch (ts) {
					case TypeDefOrRefSig tdr:
						if (TypeMatches(tdr.TypeDefOrRef, level + 1))
							return true;
						break;
					case FnPtrSig fnptr:
						if (fnptr.MethodSig is MethodSig msig) {
							if (TypeMatches(msig.RetType, level + 1))
								return true;
							foreach (var p in msig.Params) {
								if (TypeMatches(p, level + 1))
									return true;
							}
							if (msig.ParamsAfterSentinel is not null) {
								foreach (var p in msig.ParamsAfterSentinel) {
									if (TypeMatches(p, level + 1))
										return true;
								}
							}
						}
						break;
					case GenericInstSig gis:
						if (TypeMatches(gis.GenericType, level + 1))
							return true;
						foreach (var ga in gis.GenericArguments) {
							if (TypeMatches(ga, level + 1))
								return true;
						}
						break;
					case PtrSig ps:
						if (TypeMatches(ps.Next, level + 1))
							return true;
						break;
					case ByRefSig brs:
						if (TypeMatches(brs.Next, level + 1))
							return true;
						break;
					case ArraySigBase asb:
						if (TypeMatches(asb.Next, level + 1))
							return true;
						break;
					case ModifierSig ms:
						if (TypeMatches(ms.Modifier, level + 1))
							return true;
						if (TypeMatches(ms.Next, level + 1))
							return true;
						break;
					case PinnedSig ps:
						if (TypeMatches(ps.Next, level + 1))
							return true;
						break;
					}
				}
				else if (tref is TypeSpec typeSpec) {
					if (TypeMatches(typeSpec.TypeSig, level + 1))
						return true;
				}
			}
			return false;
		}

		public static bool CanShow(TypeDef? type) => type is not null;
	}

	sealed class AnalyzerEntityTreeNodeComparer : IEqualityComparer<EntityNode> {
		public static readonly AnalyzerEntityTreeNodeComparer Instance = new AnalyzerEntityTreeNodeComparer();
		AnalyzerEntityTreeNodeComparer() { }
		public bool Equals([AllowNull] EntityNode x, [AllowNull] EntityNode y) => (object?)x?.Member == y?.Member;
		public int GetHashCode([DisallowNull] EntityNode node) => node.Member?.GetHashCode() ?? 0;
	}
}
