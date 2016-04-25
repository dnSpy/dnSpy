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
using dnlib.Threading;
using dnSpy.Contracts.Files;

namespace dnSpy.Analyzer.TreeNodes {
	/// <summary>
	/// Determines the accessibility domain of a member for where-used analysis.
	/// </summary>
	sealed class ScopedWhereUsedAnalyzer<T> {
		readonly ModuleDef moduleScope;
		readonly IFileManager fileManager;
		TypeDef typeScope;

		readonly Accessibility memberAccessibility = Accessibility.Public;
		Accessibility typeAccessibility = Accessibility.Public;
		readonly Func<TypeDef, IEnumerable<T>> typeAnalysisFunction;

		public ScopedWhereUsedAnalyzer(IFileManager fileManager, TypeDef type, Func<TypeDef, IEnumerable<T>> typeAnalysisFunction) {
			this.typeScope = type;
			this.moduleScope = type.Module;
			this.typeAnalysisFunction = typeAnalysisFunction;
			this.fileManager = fileManager;
		}

		public ScopedWhereUsedAnalyzer(IFileManager fileManager, MethodDef method, Func<TypeDef, IEnumerable<T>> typeAnalysisFunction)
			: this(fileManager, method.DeclaringType, typeAnalysisFunction) {
			this.memberAccessibility = GetMethodAccessibility(method);
		}

		public ScopedWhereUsedAnalyzer(IFileManager fileManager, PropertyDef property, Func<TypeDef, IEnumerable<T>> typeAnalysisFunction)
			: this(fileManager, property.DeclaringType, typeAnalysisFunction) {
			Accessibility getterAccessibility = (property.GetMethod == null) ? Accessibility.Private : GetMethodAccessibility(property.GetMethod);
			Accessibility setterAccessibility = (property.SetMethod == null) ? Accessibility.Private : GetMethodAccessibility(property.SetMethod);
			this.memberAccessibility = (Accessibility)Math.Max((int)getterAccessibility, (int)setterAccessibility);
		}

		public ScopedWhereUsedAnalyzer(IFileManager fileManager, EventDef eventDef, Func<TypeDef, IEnumerable<T>> typeAnalysisFunction)
			: this(fileManager, eventDef.DeclaringType, typeAnalysisFunction) {
			// we only have to check the accessibility of the the get method
			// [CLS Rule 30: The accessibility of an event and of its accessors shall be identical.]
			this.memberAccessibility = GetMethodAccessibility(eventDef.AddMethod);
		}

		public ScopedWhereUsedAnalyzer(IFileManager fileManager, FieldDef field, Func<TypeDef, IEnumerable<T>> typeAnalysisFunction)
			: this(fileManager, field.DeclaringType, typeAnalysisFunction) {
			switch (field.Attributes & FieldAttributes.FieldAccessMask) {
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

		Accessibility GetMethodAccessibility(MethodDef method) {
			if (method == null)
				return 0;
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

			if (typeScope.IsNotPublic &&
				((int)typeAccessibility > (int)Accessibility.Internal)) {
				typeAccessibility = Accessibility.Internal;
			}
		}

		static Accessibility GetNestedTypeAccessibility(TypeDef type) {
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
			var modules = GetModuleAndAnyFriends(moduleScope, ct);
			return modules.AsParallel().WithCancellation(ct).SelectMany(a => FindReferencesInModule(a, ct));
		}

		IEnumerable<T> FindReferencesGlobal(CancellationToken ct) {
			var modules = GetReferencingModules(moduleScope, ct);
			return modules.AsParallel().WithCancellation(ct).SelectMany(a => FindReferencesInModule(a, ct));
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
			if (asm == null) {
				yield return mod;
				yield break;
			}
			foreach (var m in mod.Assembly.Modules.GetSafeEnumerable())
				yield return m;

			var assemblies = fileManager.GetFiles().Where(a => a.AssemblyDef != null);

			foreach (var assembly in assemblies) {
				ct.ThrowIfCancellationRequested();
				bool found = false;
				foreach (var reference in assembly.AssemblyDef.Modules.GetSafeEnumerable().SelectMany(module => module.GetAssemblyRefs())) {
					if (AssemblyNameComparer.CompareAll.CompareTo(asm, reference) == 0) {
						found = true;
						break;
					}
				}
				if (found && AssemblyReferencesScopeType(assembly.AssemblyDef)) {
					foreach (var m in assembly.AssemblyDef.Modules.GetSafeEnumerable())
						yield return m;
				}
			}
		}

		IEnumerable<ModuleDef> GetModuleAndAnyFriends(ModuleDef mod, CancellationToken ct) {
			var asm = mod.Assembly;
			if (asm == null) {
				yield return mod;
				yield break;
			}
			foreach (var m in mod.Assembly.Modules.GetSafeEnumerable())
				yield return m;

			if (asm.HasCustomAttributes) {
				var attributes = asm.CustomAttributes
					.Where(attr => attr.TypeFullName == "System.Runtime.CompilerServices.InternalsVisibleToAttribute");
				var friendAssemblies = new HashSet<string>();
				foreach (var attribute in attributes) {
					if (attribute.ConstructorArguments.Count == 0)
						continue;
					string assemblyName = attribute.ConstructorArguments[0].Value as UTF8String;
					if (assemblyName == null)
						continue;
					assemblyName = assemblyName.Split(',')[0]; // strip off any public key info
					friendAssemblies.Add(assemblyName);
				}

				if (friendAssemblies.Count > 0) {
					var assemblies = fileManager.GetFiles().Where(a => a.AssemblyDef != null);

					foreach (var assembly in assemblies) {
						ct.ThrowIfCancellationRequested();
						if (friendAssemblies.Contains(assembly.AssemblyDef.Name) && AssemblyReferencesScopeType(assembly.AssemblyDef)) {
							foreach (var m in assembly.AssemblyDef.Modules.GetSafeEnumerable())
								yield return m;
						}
					}
				}
			}
		}

		bool AssemblyReferencesScopeType(AssemblyDef asm) {
			foreach (var mod in asm.Modules.GetSafeEnumerable()) {
				foreach (var typeref in mod.GetTypeRefs()) {
					if (new SigComparer().Equals(typeScope, typeref))
						return true;
				}
			}
			return false;
		}
	}
}
