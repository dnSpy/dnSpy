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
using System.Linq;
using System.Threading;
using dnlib.DotNet;
using dnSpy.Contracts.Documents;

namespace dnSpy.Analyzer.TreeNodes {
	[Flags]
	enum ScopedWhereUsedAnalyzerOptions {
		None						= 0,

		/// <summary>
		/// Search all modules, eg. used if it's a type opted in to type equivalence, or DllImport methods, or COM types/members.
		/// </summary>
		IncludeAllModules			= 0x00000001,

		/// <summary>
		/// Force accessibility to public, eg. used by DllImport methods since DllImport methods with
		/// the same name and module are considered to be the same method.
		/// </summary>
		ForcePublic					= 0x00000002,
	}

	/// <summary>
	/// Determines the accessibility domain of a member for where-used analysis.
	/// </summary>
	sealed class ScopedWhereUsedAnalyzer<T> {
		readonly IDsDocumentService documentService;
		readonly TypeDef analyzedType;
		readonly List<ModuleDef> allModules;
		readonly ScopedWhereUsedAnalyzerOptions options;
		TypeDef typeScope;

		internal List<ModuleDef> AllModules => allModules;

		readonly Accessibility memberAccessibility = Accessibility.Public;
		Accessibility typeAccessibility = Accessibility.Public;
		readonly Func<TypeDef, IEnumerable<T>> typeAnalysisFunction;

		public ScopedWhereUsedAnalyzer(IDsDocumentService documentService, TypeDef analyzedType, Func<TypeDef, IEnumerable<T>> typeAnalysisFunction, ScopedWhereUsedAnalyzerOptions options = ScopedWhereUsedAnalyzerOptions.None) {
			this.analyzedType = analyzedType;
			typeScope = analyzedType;
			this.typeAnalysisFunction = typeAnalysisFunction;
			this.documentService = documentService;
			allModules = new List<ModuleDef>();
			this.options = options;
		}

		public ScopedWhereUsedAnalyzer(IDsDocumentService documentService, MethodDef method, Func<TypeDef, IEnumerable<T>> typeAnalysisFunction, ScopedWhereUsedAnalyzerOptions options = ScopedWhereUsedAnalyzerOptions.None)
			: this(documentService, method.DeclaringType, typeAnalysisFunction, options) => memberAccessibility = GetMethodAccessibility(method, options);

		public ScopedWhereUsedAnalyzer(IDsDocumentService documentService, PropertyDef property, Func<TypeDef, IEnumerable<T>> typeAnalysisFunction, ScopedWhereUsedAnalyzerOptions options = ScopedWhereUsedAnalyzerOptions.None)
			: this(documentService, property.DeclaringType, typeAnalysisFunction, options) {
			var getterAccessibility = property.GetMethod is null ? Accessibility.Private : GetMethodAccessibility(property.GetMethod, options);
			var setterAccessibility = property.SetMethod is null ? Accessibility.Private : GetMethodAccessibility(property.SetMethod, options);
			memberAccessibility = (Accessibility)Math.Max((int)getterAccessibility, (int)setterAccessibility);
		}

		public ScopedWhereUsedAnalyzer(IDsDocumentService documentService, EventDef eventDef, Func<TypeDef, IEnumerable<T>> typeAnalysisFunction, ScopedWhereUsedAnalyzerOptions options = ScopedWhereUsedAnalyzerOptions.None)
			: this(documentService, eventDef.DeclaringType, typeAnalysisFunction, options) {
			var adderAccessibility = eventDef.AddMethod is null ? Accessibility.Private : GetMethodAccessibility(eventDef.AddMethod, options);
			var removerAccessibility = eventDef.RemoveMethod is null ? Accessibility.Private : GetMethodAccessibility(eventDef.RemoveMethod, options);
			var invokerAccessibility = eventDef.InvokeMethod is null ? Accessibility.Private : GetMethodAccessibility(eventDef.InvokeMethod, options);
			memberAccessibility = (Accessibility)Math.Max(Math.Max((int)adderAccessibility, (int)removerAccessibility), (int)invokerAccessibility);
		}

		public ScopedWhereUsedAnalyzer(IDsDocumentService documentService, FieldDef field, Func<TypeDef, IEnumerable<T>> typeAnalysisFunction, ScopedWhereUsedAnalyzerOptions options = ScopedWhereUsedAnalyzerOptions.None)
			: this(documentService, field.DeclaringType, typeAnalysisFunction, options) {
			switch ((options & ScopedWhereUsedAnalyzerOptions.ForcePublic) != 0 ? FieldAttributes.Public : field.Attributes & FieldAttributes.FieldAccessMask) {
			case FieldAttributes.Private:
			default:
				memberAccessibility = Accessibility.Private;
				break;
			case FieldAttributes.FamANDAssem:
				memberAccessibility = Accessibility.FamilyAndInternal;
				break;
			case FieldAttributes.Assembly:
				memberAccessibility = Accessibility.Internal;
				break;
			case FieldAttributes.PrivateScope:
			case FieldAttributes.Family:
				memberAccessibility = Accessibility.Family;
				break;
			case FieldAttributes.FamORAssem:
				memberAccessibility = Accessibility.FamilyOrInternal;
				break;
			case FieldAttributes.Public:
				memberAccessibility = Accessibility.Public;
				break;
			}
		}

		Accessibility GetMethodAccessibility(MethodDef method, ScopedWhereUsedAnalyzerOptions options) {
			if (method is null)
				return 0;
			if ((options & ScopedWhereUsedAnalyzerOptions.ForcePublic) != 0)
				return Accessibility.Public;
			Accessibility accessibility;
			switch (method.Attributes & MethodAttributes.MemberAccessMask) {
			case MethodAttributes.Private:
			default:
				accessibility = Accessibility.Private;
				break;
			case MethodAttributes.FamANDAssem:
				accessibility = Accessibility.FamilyAndInternal;
				break;
			case MethodAttributes.PrivateScope:
			case MethodAttributes.Family:
				accessibility = Accessibility.Family;
				break;
			case MethodAttributes.Assembly:
				accessibility = Accessibility.Internal;
				break;
			case MethodAttributes.FamORAssem:
				accessibility = Accessibility.FamilyOrInternal;
				break;
			case MethodAttributes.Public:
				accessibility = Accessibility.Public;
				break;
			}
			return accessibility;
		}

		public IEnumerable<T> PerformAnalysis(CancellationToken ct) {
			if (memberAccessibility == Accessibility.Private) {
				return FindReferencesInTypeScope(ct);
			}

			DetermineTypeAccessibility();

			if (typeAccessibility == Accessibility.Private) {
				return FindReferencesInEnclosingTypeScope(ct);
			}

			if (memberAccessibility == Accessibility.Internal ||
				memberAccessibility == Accessibility.FamilyAndInternal ||
				typeAccessibility == Accessibility.Internal ||
				typeAccessibility == Accessibility.FamilyAndInternal)
				return FindReferencesInAssemblyAndFriends(ct);

			return FindReferencesGlobal(ct);
		}

		void DetermineTypeAccessibility() {
			while (typeScope.IsNested) {
				Accessibility accessibility = GetNestedTypeAccessibility(typeScope);
				if ((int)typeAccessibility > (int)accessibility) {
					typeAccessibility = accessibility;
					if (typeAccessibility == Accessibility.Private)
						return;
				}
				typeScope = typeScope.DeclaringType;
			}

			if (GetTypeAccessibility(typeScope) == Accessibility.Internal &&
				((int)typeAccessibility > (int)Accessibility.Internal)) {
				typeAccessibility = Accessibility.Internal;
			}
		}

		Accessibility GetTypeAccessibility(TypeDef type) {
			if ((options & ScopedWhereUsedAnalyzerOptions.ForcePublic) != 0)
				return Accessibility.Public;
			return type.IsNotPublic ? Accessibility.Internal : Accessibility.Public;
		}

		Accessibility GetNestedTypeAccessibility(TypeDef type) {
			if ((options & ScopedWhereUsedAnalyzerOptions.ForcePublic) != 0)
				return Accessibility.Public;
			Accessibility result;
			switch (type.Attributes & TypeAttributes.VisibilityMask) {
			case TypeAttributes.NestedPublic:
				result = Accessibility.Public;
				break;
			case TypeAttributes.NestedPrivate:
				result = Accessibility.Private;
				break;
			case TypeAttributes.NestedFamily:
				result = Accessibility.Family;
				break;
			case TypeAttributes.NestedAssembly:
				result = Accessibility.Internal;
				break;
			case TypeAttributes.NestedFamANDAssem:
				result = Accessibility.FamilyAndInternal;
				break;
			case TypeAttributes.NestedFamORAssem:
				result = Accessibility.FamilyOrInternal;
				break;
			default:
				throw new InvalidOperationException();
			}
			return result;
		}

		/// <summary>
		/// The effective accessibility of a member
		/// </summary>
		enum Accessibility {
			Private,
			FamilyAndInternal,
			Internal,
			Family,
			FamilyOrInternal,
			Public
		}

		IEnumerable<T> FindReferencesInAssemblyAndFriends(CancellationToken ct) {
			IEnumerable<ModuleDef> modules;
			if ((options & ScopedWhereUsedAnalyzerOptions.IncludeAllModules) != 0)
				modules = documentService.GetDocuments().Select(a => a.ModuleDef).OfType<ModuleDef>();
			else if (TIAHelper.IsTypeDefEquivalent(analyzedType)) {
				var analyzedTypes = new List<TypeDef> { analyzedType };
				SearchNode.AddTypeEquivalentTypes(documentService, analyzedType, analyzedTypes);
				modules = SearchNode.GetTypeEquivalentModules(analyzedTypes);
			}
			else
				modules = GetModuleAndAnyFriends(analyzedType.Module, ct);
			allModules.AddRange(modules);
			return allModules.AsParallel().WithCancellation(ct).SelectMany(a => FindReferencesInModule(a, ct));
		}

		IEnumerable<T> FindReferencesGlobal(CancellationToken ct) {
			IEnumerable<ModuleDef> modules;
			if ((options & ScopedWhereUsedAnalyzerOptions.IncludeAllModules) != 0)
				modules = documentService.GetDocuments().Select(a => a.ModuleDef).OfType<ModuleDef>();
			else if (TIAHelper.IsTypeDefEquivalent(analyzedType)) {
				var analyzedTypes = new List<TypeDef> { analyzedType };
				SearchNode.AddTypeEquivalentTypes(documentService, analyzedType, analyzedTypes);
				modules = SearchNode.GetTypeEquivalentModules(analyzedTypes);
			}
			else
				modules = GetReferencingModules(analyzedType.Module, ct);
			allModules.AddRange(modules);
			return allModules.AsParallel().WithCancellation(ct).SelectMany(a => FindReferencesInModule(a, ct));
		}

		IEnumerable<T> FindReferencesInModule(ModuleDef mod, CancellationToken ct) {
			foreach (TypeDef type in TreeTraversal.PreOrder(mod.Types, t => t.NestedTypes)) {
				ct.ThrowIfCancellationRequested();
				foreach (var result in typeAnalysisFunction(type)) {
					ct.ThrowIfCancellationRequested();
					yield return result;
				}
			}
		}

		IEnumerable<T> FindReferencesInTypeScope(CancellationToken ct) {
			foreach (TypeDef type in TreeTraversal.PreOrder(typeScope, t => t.NestedTypes)) {
				ct.ThrowIfCancellationRequested();
				foreach (var result in typeAnalysisFunction(type)) {
					ct.ThrowIfCancellationRequested();
					yield return result;
				}
			}
		}

		IEnumerable<T> FindReferencesInEnclosingTypeScope(CancellationToken ct) {
			foreach (TypeDef type in TreeTraversal.PreOrder(typeScope.DeclaringType, t => t.NestedTypes)) {
				ct.ThrowIfCancellationRequested();
				foreach (var result in typeAnalysisFunction(type)) {
					ct.ThrowIfCancellationRequested();
					yield return result;
				}
			}
		}

		IEnumerable<ModuleDef> GetReferencingModules(ModuleDef mod, CancellationToken ct) {
			var asm = mod.Assembly;
			if (asm is null) {
				yield return mod;
				yield break;
			}
			foreach (var m in mod.Assembly.Modules)
				yield return m;

			var modules = documentService.GetDocuments().Where(a => SearchNode.CanIncludeModule(mod, a.ModuleDef));

			foreach (var module in modules) {
				Debug2.Assert(!(module.ModuleDef is null));
				ct.ThrowIfCancellationRequested();
				if (ModuleReferencesScopeType(module.ModuleDef))
					yield return module.ModuleDef;
			}
		}

		IEnumerable<ModuleDef> GetModuleAndAnyFriends(ModuleDef mod, CancellationToken ct) {
			var asm = mod.Assembly;
			if (asm is null) {
				yield return mod;
				yield break;
			}
			foreach (var m in mod.Assembly.Modules)
				yield return m;

			var friendAssemblies = SearchNode.GetFriendAssemblies(documentService, mod, out var modules);
			if (friendAssemblies.Count > 0) {
				foreach (var module in modules) {
					Debug2.Assert(!(module.ModuleDef is null));
					ct.ThrowIfCancellationRequested();
					if ((module.AssemblyDef is null || friendAssemblies.Contains(module.AssemblyDef.Name)) && ModuleReferencesScopeType(module.ModuleDef))
						yield return module.ModuleDef;
				}
			}
		}

		bool ModuleReferencesScopeType(ModuleDef mod) {
			foreach (var typeRef in mod.GetTypeRefs()) {
				if (new SigComparer().Equals(typeScope, typeRef))
					return true;
			}
			foreach (var exportedType in mod.ExportedTypes) {
				if (!exportedType.MovedToAnotherAssembly)
					continue;
				if (new SigComparer().Equals(typeScope, exportedType))
					return true;
			}
			return false;
		}
	}
}
