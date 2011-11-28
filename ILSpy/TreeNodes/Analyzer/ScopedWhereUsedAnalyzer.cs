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
using ICSharpCode.NRefactory.Utils;
using Mono.Cecil;

namespace ICSharpCode.ILSpy.TreeNodes.Analyzer
{
	/// <summary>
	/// Determines the accessibility domain of a member for where-used analysis.
	/// </summary>
	internal class ScopedWhereUsedAnalyzer<T>
	{
		private readonly AssemblyDefinition assemblyScope;
		private TypeDefinition typeScope;

		private readonly Accessibility memberAccessibility = Accessibility.Public;
		private Accessibility typeAccessibility = Accessibility.Public;
		private readonly Func<TypeDefinition, IEnumerable<T>> typeAnalysisFunction;

		public ScopedWhereUsedAnalyzer(TypeDefinition type, Func<TypeDefinition, IEnumerable<T>> typeAnalysisFunction)
		{
			this.typeScope = type;
			this.assemblyScope = type.Module.Assembly;
			this.typeAnalysisFunction = typeAnalysisFunction;
		}

		public ScopedWhereUsedAnalyzer(MethodDefinition method, Func<TypeDefinition, IEnumerable<T>> typeAnalysisFunction)
			: this(method.DeclaringType, typeAnalysisFunction)
		{
			this.memberAccessibility = GetMethodAccessibility(method);
		}

		public ScopedWhereUsedAnalyzer(PropertyDefinition property, Func<TypeDefinition, IEnumerable<T>> typeAnalysisFunction)
			: this(property.DeclaringType, typeAnalysisFunction)
		{
			Accessibility getterAccessibility = (property.GetMethod == null) ? Accessibility.Private : GetMethodAccessibility(property.GetMethod);
			Accessibility setterAccessibility = (property.SetMethod == null) ? Accessibility.Private : GetMethodAccessibility(property.SetMethod);
			this.memberAccessibility = (Accessibility)Math.Max((int)getterAccessibility, (int)setterAccessibility);
		}

		public ScopedWhereUsedAnalyzer(EventDefinition eventDef, Func<TypeDefinition, IEnumerable<T>> typeAnalysisFunction)
			: this(eventDef.DeclaringType, typeAnalysisFunction)
		{
			// we only have to check the accessibility of the the get method
			// [CLS Rule 30: The accessibility of an event and of its accessors shall be identical.]
			this.memberAccessibility = GetMethodAccessibility(eventDef.AddMethod);
		}

		public ScopedWhereUsedAnalyzer(FieldDefinition field, Func<TypeDefinition, IEnumerable<T>> typeAnalysisFunction)
			: this(field.DeclaringType, typeAnalysisFunction)
		{
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

		private Accessibility GetMethodAccessibility(MethodDefinition method)
		{
			Accessibility accessibility;
			switch (method.Attributes & MethodAttributes.MemberAccessMask) {
				case MethodAttributes.Private:
				default:
					accessibility = Accessibility.Private;
					break;
				case MethodAttributes.FamANDAssem:
					accessibility = Accessibility.FamilyAndInternal;
					break;
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

		public IEnumerable<T> PerformAnalysis(CancellationToken ct)
		{
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
		
		private void DetermineTypeAccessibility()
		{
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

		private static Accessibility GetNestedTypeAccessibility(TypeDefinition type)
		{
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
		private enum Accessibility
		{
			Private,
			FamilyAndInternal,
			Internal,
			Family,
			FamilyOrInternal,
			Public
		}

		private IEnumerable<T> FindReferencesInAssemblyAndFriends(CancellationToken ct)
		{
			var assemblies = GetAssemblyAndAnyFriends(assemblyScope, ct);

			// use parallelism only on the assembly level (avoid locks within Cecil)
			return assemblies.AsParallel().WithCancellation(ct).SelectMany(a => FindReferencesInAssembly(a, ct));
		}

		private IEnumerable<T> FindReferencesGlobal(CancellationToken ct)
		{
			var assemblies = GetReferencingAssemblies(assemblyScope, ct);

			// use parallelism only on the assembly level (avoid locks within Cecil)
			return assemblies.AsParallel().WithCancellation(ct).SelectMany(asm => FindReferencesInAssembly(asm, ct));
		}

		private IEnumerable<T> FindReferencesInAssembly(AssemblyDefinition asm, CancellationToken ct)
		{
			foreach (TypeDefinition type in TreeTraversal.PreOrder(asm.MainModule.Types, t => t.NestedTypes)) {
				ct.ThrowIfCancellationRequested();
				foreach (var result in typeAnalysisFunction(type)) {
					ct.ThrowIfCancellationRequested();
					yield return result;
				}
			}
		}

		private IEnumerable<T> FindReferencesInTypeScope(CancellationToken ct)
		{
			foreach (TypeDefinition type in TreeTraversal.PreOrder(typeScope, t => t.NestedTypes)) {
				ct.ThrowIfCancellationRequested();
				foreach (var result in typeAnalysisFunction(type)) {
					ct.ThrowIfCancellationRequested();
					yield return result;
				}
			}
		}

		private IEnumerable<T> FindReferencesInEnclosingTypeScope(CancellationToken ct)
		{
			foreach (TypeDefinition type in TreeTraversal.PreOrder(typeScope.DeclaringType, t => t.NestedTypes)) {
				ct.ThrowIfCancellationRequested();
				foreach (var result in typeAnalysisFunction(type)) {
					ct.ThrowIfCancellationRequested();
					yield return result;
				}
			}
		}

		private IEnumerable<AssemblyDefinition> GetReferencingAssemblies(AssemblyDefinition asm, CancellationToken ct)
		{
			yield return asm;

			string requiredAssemblyFullName = asm.FullName;

			IEnumerable<LoadedAssembly> assemblies = MainWindow.Instance.CurrentAssemblyList.GetAssemblies().Where(assy => assy.AssemblyDefinition != null);

			foreach (var assembly in assemblies) {
				ct.ThrowIfCancellationRequested();
				bool found = false;
				foreach (var reference in assembly.AssemblyDefinition.MainModule.AssemblyReferences) {
					if (requiredAssemblyFullName == reference.FullName) {
						found = true;
						break;
					}
				}
				if (found && AssemblyReferencesScopeType(assembly.AssemblyDefinition))
					yield return assembly.AssemblyDefinition;
			}
		}

		private IEnumerable<AssemblyDefinition> GetAssemblyAndAnyFriends(AssemblyDefinition asm, CancellationToken ct)
		{
			yield return asm;

			if (asm.HasCustomAttributes) {
				var attributes = asm.CustomAttributes
					.Where(attr => attr.AttributeType.FullName == "System.Runtime.CompilerServices.InternalsVisibleToAttribute");
				var friendAssemblies = new HashSet<string>();
				foreach (var attribute in attributes) {
					string assemblyName = attribute.ConstructorArguments[0].Value as string;
					assemblyName = assemblyName.Split(',')[0]; // strip off any public key info
					friendAssemblies.Add(assemblyName);
				}

				if (friendAssemblies.Count > 0) {
					IEnumerable<LoadedAssembly> assemblies = MainWindow.Instance.CurrentAssemblyList.GetAssemblies();

					foreach (var assembly in assemblies) {
						ct.ThrowIfCancellationRequested();
						if (friendAssemblies.Contains(assembly.ShortName) && AssemblyReferencesScopeType(assembly.AssemblyDefinition)) {
							yield return assembly.AssemblyDefinition;
						}
					}
				}
			}
		}

		private bool AssemblyReferencesScopeType(AssemblyDefinition asm)
		{
			bool hasRef = false;
			foreach (var typeref in asm.MainModule.GetTypeReferences()) {
				if (typeref.Name == typeScope.Name && typeref.Namespace == typeScope.Namespace) {
					hasRef = true;
					break;
				}
			}
			return hasRef;
		}
	}
}
