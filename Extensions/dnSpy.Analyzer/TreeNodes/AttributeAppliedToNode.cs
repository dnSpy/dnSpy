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
using System.Diagnostics;
using System.Linq;
using System.Threading;
using dnlib.DotNet;
using dnSpy.Analyzer.Properties;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Text;

namespace dnSpy.Analyzer.TreeNodes {
	sealed class AttributeAppliedToNode : SearchNode {
		readonly TypeDef analyzedType;
		readonly List<TypeDef> analyzedTypes;
		readonly bool includeAllModules;

		readonly AttributeTargets usage;
		ConcurrentDictionary<MethodDef, int>? foundMethods;

		public static bool CanShow(TypeDef type) => type.IsClass && IsCustomAttribute(type);

		static bool IsCustomAttribute(TypeDef type) {
			while (type is not null) {
				var bt = type.BaseType.ResolveTypeDef();
				if (bt is null)
					return false;
				if (bt.FullName == "System.Attribute")
					return true;
				type = bt;
			}
			return false;
		}

		public AttributeAppliedToNode(TypeDef analyzedType) {
			this.analyzedType = analyzedType ?? throw new ArgumentNullException(nameof(analyzedType));
			analyzedTypes = new List<TypeDef> { analyzedType };
			includeAllModules = CustomAttributesUtils.IsPseudoCustomAttributeType(analyzedType);
			var ca = analyzedType.CustomAttributes.Find("System.AttributeUsageAttribute");
			if (ca is not null && ca.ConstructorArguments.Count == 1 && ca.ConstructorArguments[0].Value is int)
				usage = (AttributeTargets)ca.ConstructorArguments[0].Value;
			else
				usage = AttributeTargets.All;
		}

		protected override void Write(ITextColorWriter output, IDecompiler decompiler) =>
			output.Write(BoxedTextColor.Text, dnSpy_Analyzer_Resources.AppliedToTreeNode);

		protected override IEnumerable<AnalyzerTreeNodeData> FetchChildren(CancellationToken ct) {
			foundMethods = new ConcurrentDictionary<MethodDef, int>();

			AddTypeEquivalentTypes(Context.DocumentService, analyzedType, analyzedTypes);
			IEnumerable<(ModuleDef module, ITypeDefOrRef type)> modules;
			if (includeAllModules)
				modules = GetAllModules(analyzedType.Module, ct);
			else if (TIAHelper.IsTypeDefEquivalent(analyzedType))
				modules = GetTypeEquivalentModulesAndTypes(analyzedTypes);
			else if (IsPublic(analyzedType))
				modules = GetReferencingModules(analyzedType.Module, ct);
			else
				modules = GetModuleAndAnyFriends(analyzedType.Module, ct);

			var results = modules.AsParallel().WithCancellation(ct).SelectMany(a => FindReferencesInModule(new[] { a.Item1 }, a.Item2, ct));

			foreach (var result in results)
				yield return result;

			foundMethods = null;
		}

		static bool IsPublic(TypeDef type) {
			for (;;) {
				if (type.DeclaringType is TypeDef declType) {
					if (!(type.IsNestedFamily || type.IsNestedFamilyOrAssembly || type.IsNestedPublic))
						return false;
					type = declType;
				}
				else
					return type.IsPublic;
			}
		}

		IEnumerable<AnalyzerTreeNodeData> FindReferencesInModule(IEnumerable<ModuleDef> modules, ITypeDefOrRef tr, CancellationToken ct) {
			var trScopeType = tr.GetScopeType();
			var checkedAsms = new HashSet<AssemblyDef>();
			foreach (var module in modules) {
				if ((usage & AttributeTargets.Assembly) != 0) {
					AssemblyDef asm = module.Assembly;
					if (asm is not null && checkedAsms.Add(asm)) {
						foreach (var attribute in asm.GetCustomAttributes()) {
							if (new SigComparer().Equals(attribute.AttributeType?.GetScopeType(), trScopeType)) {
								yield return new AssemblyNode(asm) { Context = Context };
								break;
							}
						}
					}
				}

				ct.ThrowIfCancellationRequested();

				if ((usage & AttributeTargets.Module) != 0) {
					foreach (var attribute in module.GetCustomAttributes()) {
						if (new SigComparer().Equals(attribute.AttributeType?.GetScopeType(), trScopeType)) {
							yield return new ModuleNode(module) { Context = Context };
							break;
						}
					}
				}

				ct.ThrowIfCancellationRequested();

				if ((usage & (AttributeTargets.Class | AttributeTargets.Enum | AttributeTargets.Interface | AttributeTargets.Struct | AttributeTargets.GenericParameter | AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Event | AttributeTargets.Method | AttributeTargets.Constructor | AttributeTargets.ReturnValue | AttributeTargets.Parameter)) != 0) {
					foreach (TypeDef type in TreeTraversal.PreOrder(module.Types, t => t.NestedTypes)) {
						ct.ThrowIfCancellationRequested();
						foreach (var result in FindReferencesWithinInType(type, tr)) {
							ct.ThrowIfCancellationRequested();
							yield return result;
						}
					}
				}
			}
		}

		IEnumerable<AnalyzerTreeNodeData> FindReferencesWithinInType(TypeDef type, ITypeDefOrRef attrTypeRef) {
			var attrTypeRefScopeType = attrTypeRef.GetScopeType();
			bool searchRequired = (type.IsClass && usage.HasFlag(AttributeTargets.Class))
				|| (type.IsEnum && usage.HasFlag(AttributeTargets.Enum))
				|| (type.IsInterface && usage.HasFlag(AttributeTargets.Interface))
				|| (type.IsValueType && usage.HasFlag(AttributeTargets.Struct));
			if (searchRequired) {
				foreach (var attribute in type.GetCustomAttributes()) {
					if (new SigComparer().Equals(attribute.AttributeType?.GetScopeType(), attrTypeRefScopeType)) {
						yield return new TypeNode(type) { Context = Context };
						break;
					}
				}
			}

			if ((usage & AttributeTargets.GenericParameter) != 0 && type.HasGenericParameters) {
				foreach (var parameter in type.GenericParameters) {
					foreach (var attribute in parameter.GetCustomAttributes()) {
						if (new SigComparer().Equals(attribute.AttributeType?.GetScopeType(), attrTypeRefScopeType)) {
							yield return new TypeNode(type) { Context = Context };
							break;
						}
					}
				}
			}

			if ((usage & AttributeTargets.Field) != 0 && type.HasFields) {
				foreach (var field in type.Fields) {
					foreach (var attribute in field.GetCustomAttributes()) {
						if (new SigComparer().Equals(attribute.AttributeType?.GetScopeType(), attrTypeRefScopeType)) {
							yield return new FieldNode(field) { Context = Context };
							break;
						}
					}
				}
			}

			if (((usage & AttributeTargets.Property) != 0) && type.HasProperties) {
				foreach (var property in type.Properties) {
					foreach (var attribute in property.GetCustomAttributes()) {
						if (new SigComparer().Equals(attribute.AttributeType?.GetScopeType(), attrTypeRefScopeType)) {
							yield return new PropertyNode(property) { Context = Context };
							break;
						}
					}
				}
			}
			if (((usage & AttributeTargets.Event) != 0) && type.HasEvents) {
				foreach (var @event in type.Events) {
					foreach (var attribute in @event.GetCustomAttributes()) {
						if (new SigComparer().Equals(attribute.AttributeType?.GetScopeType(), attrTypeRefScopeType)) {
							yield return new EventNode(@event) { Context = Context };
							break;
						}
					}
				}
			}

			if ((usage & (AttributeTargets.Method | AttributeTargets.Constructor | AttributeTargets.ReturnValue | AttributeTargets.Parameter)) != 0 && type.HasMethods) {
				foreach (var method in type.Methods) {
					bool found = false;
					if ((usage & (AttributeTargets.Method | AttributeTargets.Constructor)) != 0) {
						foreach (var attribute in method.GetCustomAttributes()) {
							if (new SigComparer().Equals(attribute.AttributeType?.GetScopeType(), attrTypeRefScopeType)) {
								found = true;
								break;
							}
						}
					}
					if (!found &&
						((usage & AttributeTargets.ReturnValue) != 0) &&
						method.Parameters.ReturnParameter.ParamDef is ParamDef retParamDef) {
						foreach (var attribute in retParamDef.GetCustomAttributes()) {
							if (new SigComparer().Equals(attribute.AttributeType?.GetScopeType(), attrTypeRefScopeType)) {
								found = true;
								break;
							}
						}
					}

					if (!found &&
						((usage & AttributeTargets.Parameter) != 0) &&
						method.Parameters.Count > 0) {
						foreach (var parameter in method.Parameters.Where(param => param.HasParamDef)) {
							if (parameter.IsHiddenThisParameter)
								continue;
							foreach (var attribute in parameter.ParamDef.GetCustomAttributes()) {
								if (new SigComparer().Equals(attribute.AttributeType?.GetScopeType(), attrTypeRefScopeType)) {
									found = true;
									break;
								}
							}
						}
					}

					if (found) {
						if (GetOriginalCodeLocation(method) is MethodDef codeLocation && !HasAlreadyBeenFound(codeLocation)) {
							yield return new MethodNode(codeLocation) { Context = Context };
						}
					}
				}
			}
		}

		bool HasAlreadyBeenFound(MethodDef method) => !foundMethods!.TryAdd(method, 0);

		IEnumerable<(ModuleDef module, ITypeDefOrRef type)> GetAllModules(ModuleDef mod, CancellationToken ct) {
			foreach (var doc in Context.DocumentService.GetDocuments()) {
				if (!(doc.ModuleDef is ModuleDef module))
					continue;
				var typeRef = GetScopeTypeRefInModule(module) ?? module.Import(analyzedType);
				yield return (module, typeRef);
			}
		}

		IEnumerable<(ModuleDef module, ITypeDefOrRef type)> GetReferencingModules(ModuleDef mod, CancellationToken ct) {
			var asm = mod.Assembly;
			if (asm is null) {
				yield return (mod, analyzedType);
				yield break;
			}

			foreach (var m in asm.Modules)
				yield return (m, analyzedType);

			var modules = Context.DocumentService.GetDocuments().Where(a => SearchNode.CanIncludeModule(mod, a.ModuleDef));

			foreach (var module in modules) {
				Debug2.Assert(module.ModuleDef is not null);
				ct.ThrowIfCancellationRequested();
				var typeref = GetScopeTypeRefInModule(module.ModuleDef);
				if (typeref is not null)
					yield return (module.ModuleDef, typeref);
			}
		}

		IEnumerable<(ModuleDef module, ITypeDefOrRef type)> GetModuleAndAnyFriends(ModuleDef mod, CancellationToken ct) {
			var asm = mod.Assembly;
			if (asm is null) {
				yield return (mod, analyzedType);
				yield break;
			}

			foreach (var m in asm.Modules)
				yield return (m, analyzedType);

			var friendAssemblies = GetFriendAssemblies(Context.DocumentService, mod, out var modules);
			if (friendAssemblies.Count > 0) {
				foreach (var module in modules) {
					Debug2.Assert(module.ModuleDef is not null);
					ct.ThrowIfCancellationRequested();
					if (module.AssemblyDef is null || friendAssemblies.Contains(module.AssemblyDef.Name)) {
						var typeref = GetScopeTypeRefInModule(module.ModuleDef);
						if (typeref is not null)
							yield return (module.ModuleDef, typeref);
					}
				}
			}
		}

		ITypeDefOrRef? GetScopeTypeRefInModule(ModuleDef mod) {
			foreach (var typeref in mod.GetTypeRefs()) {
				if (new SigComparer().Equals(analyzedType, typeref))
					return typeref;
			}
			return null;
		}
	}
}
