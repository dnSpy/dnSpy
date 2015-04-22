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
using ICSharpCode.NRefactory.CSharp;
using Mono.Cecil;

namespace ICSharpCode.Decompiler.Ast.Transforms
{
	/// <summary>
	/// Introduces using declarations.
	/// </summary>
	public class IntroduceUsingDeclarations : IAstTransform
	{
		DecompilerContext context;
		
		public IntroduceUsingDeclarations(DecompilerContext context)
		{
			this.context = context;
		}
		
		public void Run(AstNode compilationUnit)
		{
			// First determine all the namespaces that need to be imported:
			compilationUnit.AcceptVisitor(new FindRequiredImports(this), null);
			
			importedNamespaces.Add("System"); // always import System, even when not necessary
			
			if (context.Settings.UsingDeclarations) {
				// Now add using declarations for those namespaces:
				foreach (string ns in importedNamespaces.OrderByDescending(n => n)) {
					// we go backwards (OrderByDescending) through the list of namespaces because we insert them backwards
					// (always inserting at the start of the list)
					string[] parts = ns.Split('.');
					AstType nsType = new SimpleType(parts[0]);
					for (int i = 1; i < parts.Length; i++) {
						nsType = new MemberType { Target = nsType, MemberName = parts[i] };
					}
					compilationUnit.InsertChildAfter(null, new UsingDeclaration { Import = nsType }, SyntaxTree.MemberRole);
				}
			}
			
			if (!context.Settings.FullyQualifyAmbiguousTypeNames)
				return;
			
			FindAmbiguousTypeNames(context.CurrentModule, internalsVisible: true);
			foreach (AssemblyNameReference r in context.CurrentModule.AssemblyReferences) {
				AssemblyDefinition d = context.CurrentModule.AssemblyResolver.Resolve(r);
				if (d != null)
					FindAmbiguousTypeNames(d.MainModule, internalsVisible: false);
			}
			
			// verify that the SimpleTypes refer to the correct type (no ambiguities)
			compilationUnit.AcceptVisitor(new FullyQualifyAmbiguousTypeNamesVisitor(this), null);
		}
		
		readonly HashSet<string> declaredNamespaces = new HashSet<string>() { string.Empty };
		readonly HashSet<string> importedNamespaces = new HashSet<string>();
		
		// Note that we store type names with `n suffix, so we automatically disambiguate based on number of type parameters.
		readonly HashSet<string> availableTypeNames = new HashSet<string>();
		readonly HashSet<string> ambiguousTypeNames = new HashSet<string>();
		
		sealed class FindRequiredImports : DepthFirstAstVisitor<object, object>
		{
			readonly IntroduceUsingDeclarations transform;
			string currentNamespace;
			
			public FindRequiredImports(IntroduceUsingDeclarations transform)
			{
				this.transform = transform;
				this.currentNamespace = transform.context.CurrentType != null ? transform.context.CurrentType.Namespace : string.Empty;
			}
			
			bool IsParentOfCurrentNamespace(string ns)
			{
				if (ns.Length == 0)
					return true;
				if (currentNamespace.StartsWith(ns, StringComparison.Ordinal)) {
					if (currentNamespace.Length == ns.Length)
						return true;
					if (currentNamespace[ns.Length] == '.')
						return true;
				}
				return false;
			}
			
			public override object VisitSimpleType(SimpleType simpleType, object data)
			{
				TypeReference tr = simpleType.Annotation<TypeReference>();
				if (tr != null && !IsParentOfCurrentNamespace(tr.Namespace)) {
					transform.importedNamespaces.Add(tr.Namespace);
				}
				return base.VisitSimpleType(simpleType, data); // also visit type arguments
			}
			
			public override object VisitNamespaceDeclaration(NamespaceDeclaration namespaceDeclaration, object data)
			{
				string oldNamespace = currentNamespace;
				foreach (string ident in namespaceDeclaration.Identifiers) {
					currentNamespace = NamespaceDeclaration.BuildQualifiedName(currentNamespace, ident);
					transform.declaredNamespaces.Add(currentNamespace);
				}
				base.VisitNamespaceDeclaration(namespaceDeclaration, data);
				currentNamespace = oldNamespace;
				return null;
			}
		}
		
		void FindAmbiguousTypeNames(ModuleDefinition module, bool internalsVisible)
		{
			foreach (TypeDefinition type in module.Types) {
				if (internalsVisible || type.IsPublic) {
					if (importedNamespaces.Contains(type.Namespace) || declaredNamespaces.Contains(type.Namespace)) {
						if (!availableTypeNames.Add(type.Name))
							ambiguousTypeNames.Add(type.Name);
					}
				}
			}
		}
		
		sealed class FullyQualifyAmbiguousTypeNamesVisitor : DepthFirstAstVisitor<object, object>
		{
			readonly IntroduceUsingDeclarations transform;
			string currentNamespace;
			HashSet<string> currentMemberTypes;
			Dictionary<string, MemberReference> currentMembers;
			bool isWithinTypeReferenceExpression;
			
			public FullyQualifyAmbiguousTypeNamesVisitor(IntroduceUsingDeclarations transform)
			{
				this.transform = transform;
				this.currentNamespace = transform.context.CurrentType != null ? transform.context.CurrentType.Namespace : string.Empty;
			}
			
			public override object VisitNamespaceDeclaration(NamespaceDeclaration namespaceDeclaration, object data)
			{
				string oldNamespace = currentNamespace;
				foreach (string ident in namespaceDeclaration.Identifiers) {
					currentNamespace = NamespaceDeclaration.BuildQualifiedName(currentNamespace, ident);
				}
				base.VisitNamespaceDeclaration(namespaceDeclaration, data);
				currentNamespace = oldNamespace;
				return null;
			}
			
			public override object VisitTypeDeclaration(TypeDeclaration typeDeclaration, object data)
			{
				HashSet<string> oldMemberTypes = currentMemberTypes;
				currentMemberTypes = currentMemberTypes != null ? new HashSet<string>(currentMemberTypes) : new HashSet<string>();
				
				Dictionary<string, MemberReference> oldMembers = currentMembers;
				currentMembers = new Dictionary<string, MemberReference>();
				
				TypeDefinition typeDef = typeDeclaration.Annotation<TypeDefinition>();
				bool privateMembersVisible = true;
				ModuleDefinition internalMembersVisibleInModule = typeDef.Module;
				while (typeDef != null) {
					foreach (GenericParameter gp in typeDef.GenericParameters) {
						currentMemberTypes.Add(gp.Name);
					}
					foreach (TypeDefinition t in typeDef.NestedTypes) {
						if (privateMembersVisible || IsVisible(t, internalMembersVisibleInModule))
							currentMemberTypes.Add(t.Name.Substring(t.Name.LastIndexOf('+') + 1));
					}
					
					foreach (MethodDefinition method in typeDef.Methods) {
						if (privateMembersVisible || IsVisible(method, internalMembersVisibleInModule))
							AddCurrentMember(method);
					}
					foreach (PropertyDefinition property in typeDef.Properties) {
						if (privateMembersVisible || IsVisible(property.GetMethod, internalMembersVisibleInModule) || IsVisible(property.SetMethod, internalMembersVisibleInModule))
							AddCurrentMember(property);
					}
					foreach (EventDefinition ev in typeDef.Events) {
						if (privateMembersVisible || IsVisible(ev.AddMethod, internalMembersVisibleInModule) || IsVisible(ev.RemoveMethod, internalMembersVisibleInModule))
							AddCurrentMember(ev);
					}
					foreach (FieldDefinition f in typeDef.Fields) {
						if (privateMembersVisible || IsVisible(f, internalMembersVisibleInModule))
							AddCurrentMember(f);
					}
					// repeat with base class:
					if (typeDef.BaseType != null)
						typeDef = typeDef.BaseType.Resolve();
					else
						typeDef = null;
					privateMembersVisible = false;
				}
				
				// Now add current members from outer classes:
				if (oldMembers != null) {
					foreach (var pair in oldMembers) {
						// add members from outer classes only if the inner class doesn't define the member
						if (!currentMembers.ContainsKey(pair.Key))
							currentMembers.Add(pair.Key, pair.Value);
					}
				}
				
				base.VisitTypeDeclaration(typeDeclaration, data);
				currentMembers = oldMembers;
				return null;
			}
			
			void AddCurrentMember(MemberReference m)
			{
				MemberReference existingMember;
				if (currentMembers.TryGetValue(m.Name, out existingMember)) {
					// We keep the existing member assignment if it was from another class (=from a derived class),
					// because members in derived classes have precedence over members in base classes.
					if (existingMember != null && existingMember.DeclaringType == m.DeclaringType) {
						// Use null as value to signalize multiple members with the same name
						currentMembers[m.Name] = null;
					}
				} else {
					currentMembers.Add(m.Name, m);
				}
			}
			
			bool IsVisible(MethodDefinition m, ModuleDefinition internalMembersVisibleInModule)
			{
				if (m == null)
					return false;
				switch (m.Attributes & MethodAttributes.MemberAccessMask) {
					case MethodAttributes.FamANDAssem:
					case MethodAttributes.Assembly:
						return m.Module == internalMembersVisibleInModule;
					case MethodAttributes.Family:
					case MethodAttributes.FamORAssem:
					case MethodAttributes.Public:
						return true;
					default:
						return false;
				}
			}
			
			bool IsVisible(FieldDefinition f, ModuleDefinition internalMembersVisibleInModule)
			{
				if (f == null)
					return false;
				switch (f.Attributes & FieldAttributes.FieldAccessMask) {
					case FieldAttributes.FamANDAssem:
					case FieldAttributes.Assembly:
						return f.Module == internalMembersVisibleInModule;
					case FieldAttributes.Family:
					case FieldAttributes.FamORAssem:
					case FieldAttributes.Public:
						return true;
					default:
						return false;
				}
			}
			
			bool IsVisible(TypeDefinition t, ModuleDefinition internalMembersVisibleInModule)
			{
				if (t == null)
					return false;
				switch (t.Attributes & TypeAttributes.VisibilityMask) {
					case TypeAttributes.NotPublic:
					case TypeAttributes.NestedAssembly:
					case TypeAttributes.NestedFamANDAssem:
						return t.Module == internalMembersVisibleInModule;
					case TypeAttributes.NestedFamily:
					case TypeAttributes.NestedFamORAssem:
					case TypeAttributes.NestedPublic:
					case TypeAttributes.Public:
						return true;
					default:
						return false;
				}
			}
			
			public override object VisitSimpleType(SimpleType simpleType, object data)
			{
				// Handle type arguments first, so that the fixed-up type arguments get moved over to the MemberType,
				// if we're also creating one here.
				base.VisitSimpleType(simpleType, data);
				TypeReference tr = simpleType.Annotation<TypeReference>();
				// Fully qualify any ambiguous type names.
				if (tr != null && IsAmbiguous(tr.Namespace, tr.Name)) {
					AstType ns;
					if (string.IsNullOrEmpty(tr.Namespace)) {
						ns = new SimpleType("global");
					} else {
						string[] parts = tr.Namespace.Split('.');
						if (IsAmbiguous(string.Empty, parts[0])) {
							// conflict between namespace and type name/member name
							ns = new MemberType { Target = new SimpleType("global"), IsDoubleColon = true, MemberName = parts[0] };
						} else {
							ns = new SimpleType(parts[0]);
						}
						for (int i = 1; i < parts.Length; i++) {
							ns = new MemberType { Target = ns, MemberName = parts[i] };
						}
					}
					MemberType mt = new MemberType();
					mt.Target = ns;
					mt.IsDoubleColon = string.IsNullOrEmpty(tr.Namespace);
					mt.MemberName = simpleType.Identifier;
					mt.CopyAnnotationsFrom(simpleType);
					simpleType.TypeArguments.MoveTo(mt.TypeArguments);
					simpleType.ReplaceWith(mt);
				}
				return null;
			}
			
			public override object VisitTypeReferenceExpression(TypeReferenceExpression typeReferenceExpression, object data)
			{
				isWithinTypeReferenceExpression = true;
				base.VisitTypeReferenceExpression(typeReferenceExpression, data);
				isWithinTypeReferenceExpression = false;
				return null;
			}
			
			bool IsAmbiguous(string ns, string name)
			{
				// If the type name conflicts with an inner class/type parameter, we need to fully-qualify it:
				if (currentMemberTypes != null && currentMemberTypes.Contains(name))
					return true;
				// If the type name conflicts with a field/property etc. on the current class, we need to fully-qualify it,
				// if we're inside an expression.
				if (isWithinTypeReferenceExpression && currentMembers != null) {
					MemberReference mr;
					if (currentMembers.TryGetValue(name, out mr)) {
						// However, in the special case where the member is a field or property with the same type
						// as is requested, then we can use the short name (if it's not otherwise ambiguous)
						PropertyDefinition prop = mr as PropertyDefinition;
						FieldDefinition field = mr as FieldDefinition;
						if (!(prop != null && prop.PropertyType.Namespace == ns && prop.PropertyType.Name == name)
						    && !(field != null && field.FieldType.Namespace == ns && field.FieldType.Name == name))
							return true;
					}
				}
				// If the type is defined in the current namespace,
				// then we can use the short name even if we imported type with same name from another namespace.
				if (ns == currentNamespace && !string.IsNullOrEmpty(ns))
					return false;
				return transform.ambiguousTypeNames.Contains(name);
			}
		}
	}
}
