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
using System.Text;
using dnlib.DotNet;
using dnSpy.Decompiler.Shared;
using ICSharpCode.NRefactory.CSharp;

namespace ICSharpCode.Decompiler.Ast.Transforms {
	/// <summary>
	/// Introduces using declarations.
	/// </summary>
	public class IntroduceUsingDeclarations : IAstTransformPoolObject
	{
		DecompilerContext context;
		readonly StringBuilder stringBuilder;
		readonly Dictionary<string, List<TypeDef>> typesWithNamespace_currentModule = new Dictionary<string, List<TypeDef>>(StringComparer.Ordinal);
		readonly List<Dictionary<string, List<TypeDef>>> typesWithNamespace_allAsms_list = new List<Dictionary<string, List<TypeDef>>>();
		ModuleDef lastCheckedModule;

		public IntroduceUsingDeclarations(DecompilerContext context)
		{
			this.stringBuilder = new StringBuilder();
			Reset(context);
		}

		public void Reset(DecompilerContext context)
		{
			this.context = context;
			ambiguousTypeNames.Clear();
			availableTypeNames.Clear();
			importedNamespaces.Clear();
			importedOrDeclaredNamespaces.Clear();
			importedOrDeclaredNamespaces.Add(string.Empty);
			// Don't clear lastCheckedModule, typesWithNamespace_currentModule, typesWithNamespace_allAsms_list since they're cached between resets
		}

		public void Run(AstNode compilationUnit)
		{
			// First determine all the namespaces that need to be imported:
			compilationUnit.AcceptVisitor(new FindRequiredImports(this), null);
			
			importedNamespaces.Add("System"); // always import System, even when not necessary
			importedOrDeclaredNamespaces.Add("System");
			
			if (context.Settings.UsingDeclarations) {
				// Now add using declarations for those namespaces:
				foreach (string ns in GetNamespacesInReverseOrder()) {
					// we go backwards (OrderByDescending) through the list of namespaces because we insert them backwards
					// (always inserting at the start of the list)
					string[] parts = ns.Split(namespaceSep);
					AstType nsType = new SimpleType(parts[0]).WithAnnotation(TextTokenKind.NamespacePart);
					for (int i = 1; i < parts.Length; i++) {
						nsType = new MemberType { Target = nsType, MemberNameToken = Identifier.Create(parts[i]).WithAnnotation(TextTokenKind.NamespacePart) }.WithAnnotation(TextTokenKind.NamespacePart);
					}
					compilationUnit.InsertChildAfter(null, new UsingDeclaration { Import = nsType }, SyntaxTree.MemberRole);
				}
			}
			
			if (!context.Settings.FullyQualifyAmbiguousTypeNames && !context.Settings.FullyQualifyAllTypes)
				return;

			if (context.CurrentModule != null) {
				if (lastCheckedModule != context.CurrentModule) {
					typesWithNamespace_currentModule.Clear();
					BuildAmbiguousTypeNamesTable(typesWithNamespace_currentModule, context.CurrentModule.Types, true);
				}
				FindAmbiguousTypeNames(typesWithNamespace_currentModule, true);
				if (lastCheckedModule != context.CurrentModule) {
					var asmDict = new Dictionary<AssemblyDef, List<AssemblyDef>>(AssemblyEqualityComparer.Instance);
					lastCheckedModule = context.CurrentModule;
					foreach (var r in context.CurrentModule.GetAssemblyRefs()) {
						AssemblyDef d = context.CurrentModule.Context.AssemblyResolver.Resolve(r, context.CurrentModule);
						if (d == null)
							continue;
						List<AssemblyDef> list;
						if (!asmDict.TryGetValue(d, out list))
							asmDict.Add(d, list = new List<AssemblyDef>());
						list.Add(d);
					}
					typesWithNamespace_allAsms_list.Clear();
					foreach (var list in asmDict.Values) {
						var dict = new Dictionary<string, List<TypeDef>>(StringComparer.Ordinal);
						BuildAmbiguousTypeNamesTable(dict, GetTypes(list), false);
						typesWithNamespace_allAsms_list.Add(dict);
					}
				}
				foreach (var dict in typesWithNamespace_allAsms_list) {
					FindAmbiguousTypeNames(dict, false);
				}
			}
			
			// verify that the SimpleTypes refer to the correct type (no ambiguities)
			compilationUnit.AcceptVisitor(new FullyQualifyAmbiguousTypeNamesVisitor(this), null);
		}
		static readonly char[] namespaceSep = new char[] { '.' };

		string[] GetNamespacesInReverseOrder()
		{
			var ary = new string[importedNamespaces.Count];
			int i = 0;
			foreach (var s in importedNamespaces)
				ary[i++] = s;

			if (context.Settings.SortSystemUsingStatementsFirst) {
				Array.Sort(ary, (a, b) => {
					bool sa = a == "System" || a.StartsWith("System.");
					bool sb = b == "System" || b.StartsWith("System.");
					if (sa && sb)
						return StringComparer.OrdinalIgnoreCase.Compare(b, a);
					if (sa && !sb)
						return 1;
					if (!sa && sb)
						return -1;
					return StringComparer.OrdinalIgnoreCase.Compare(b, a);
				});
			}
			else
				Array.Sort(ary, (a, b) => StringComparer.OrdinalIgnoreCase.Compare(b, a));

			return ary;
		}

		static IEnumerable<TypeDef> GetTypes(List<AssemblyDef> asms)
		{
			if (asms.Count == 0)
				return new TypeDef[0];
			if (asms.Count == 1) {
				if (asms[0].Modules.Count == 1)
					return asms[0].ManifestModule.Types;
				return asms[0].Modules.SelectMany(m => m.Types);
			}

			var types = new HashSet<TypeDef>(new TypeEqualityComparer(SigComparerOptions.DontCompareTypeScope));
			foreach (var asm in asms) {
				foreach (var mod in asm.Modules) {
					foreach (var type in mod.Types) {
						if (types.Add(type))
							continue;
						if (!type.IsPublic)
							continue;
						types.Remove(type);
						bool b = types.Add(type);
						Debug.Assert(b);
					}
				}
			}
			return types;
		}

		sealed class AssemblyEqualityComparer : IEqualityComparer<AssemblyDef>
		{
			public static readonly AssemblyEqualityComparer Instance = new AssemblyEqualityComparer();

			public bool Equals(AssemblyDef x, AssemblyDef y)
			{
				if (x == y)
					return true;
				if (x == null || y == null)
					return false;
				if (!x.Name.String.Equals(y.Name, StringComparison.InvariantCultureIgnoreCase))
					return false;
				if (x.PublicKey.IsNullOrEmpty != y.PublicKey.IsNullOrEmpty)
					return false;
				if (x.PublicKey.IsNullOrEmpty)
					return true;
				return x.PublicKey.Equals(y.PublicKey);
			}

			public int GetHashCode(AssemblyDef obj)
			{
				return unchecked(obj.Name.ToUpperInvariant().GetHashCode() +
					obj.PublicKey.GetHashCode());
			}
		}
		
		readonly HashSet<string> importedOrDeclaredNamespaces = new HashSet<string>(StringComparer.Ordinal);
		readonly HashSet<string> importedNamespaces = new HashSet<string>(StringComparer.Ordinal);
		
		// Note that we store type names with `n suffix, so we automatically disambiguate based on number of type parameters.
		readonly HashSet<string> availableTypeNames = new HashSet<string>(StringComparer.Ordinal);
		readonly HashSet<string> ambiguousTypeNames = new HashSet<string>(StringComparer.Ordinal);
		
		sealed class FindRequiredImports : DepthFirstAstVisitor<object, object>
		{
			readonly IntroduceUsingDeclarations transform;
			string currentNamespace;
			
			public FindRequiredImports(IntroduceUsingDeclarations transform)
			{
				this.transform = transform;
				this.currentNamespace = transform.context.CurrentType != null ? transform.context.CurrentType.Namespace.String : string.Empty;
			}
			
			bool IsParentOfCurrentNamespace(StringBuilder sb)
			{
				if (sb.Length == 0)
					return true;
				if (currentNamespace.StartsWith(sb)) {
					if (currentNamespace.Length == sb.Length)
						return true;
					if (currentNamespace[sb.Length] == '.')
						return true;
				}
				return false;
			}
			
			public override object VisitSimpleType(SimpleType simpleType, object data)
			{
				ITypeDefOrRef tr = simpleType.Annotation<ITypeDefOrRef>();
				if (tr != null) {
					var sb = GetNamespace(tr);
					if (!IsParentOfCurrentNamespace(sb)) {
						string ns = sb.ToString();
						transform.importedNamespaces.Add(ns);
						transform.importedOrDeclaredNamespaces.Add(ns);
					}
				}
				return base.VisitSimpleType(simpleType, data); // also visit type arguments
			}

			StringBuilder GetNamespace(IType type)
			{
				this.transform.stringBuilder.Clear();
				if (type == null)
					return this.transform.stringBuilder;
				return FullNameCreator.NamespaceSB(type, false, this.transform.stringBuilder);
			}
			
			public override object VisitNamespaceDeclaration(NamespaceDeclaration namespaceDeclaration, object data)
			{
				string oldNamespace = currentNamespace;
				foreach (string ident in namespaceDeclaration.Identifiers) {
					currentNamespace = NamespaceDeclaration.BuildQualifiedName(currentNamespace, ident);
					transform.importedOrDeclaredNamespaces.Add(currentNamespace);
				}
				base.VisitNamespaceDeclaration(namespaceDeclaration, data);
				currentNamespace = oldNamespace;
				return null;
			}
		}

		void BuildAmbiguousTypeNamesTable(Dictionary<string, List<TypeDef>> dict, IEnumerable<TypeDef> types, bool internalsVisible)
		{
			foreach (TypeDef type in types) {
				if (!internalsVisible && !type.IsPublic)
					continue;
				List<TypeDef> list;
				string ns = type.Namespace;
				if (!dict.TryGetValue(ns, out list))
					dict.Add(ns, list = new List<TypeDef>());
				list.Add(type);
			}
		}
		
		void FindAmbiguousTypeNames(Dictionary<string, List<TypeDef>> dict, bool internalsVisible)
		{
			foreach (var ns in importedOrDeclaredNamespaces) {
				List<TypeDef> list;
				if (!dict.TryGetValue(ns, out list))
					continue;
				foreach (var type in list) {
					if (!internalsVisible && !type.IsPublic)
						continue;
					string name = type.Name;
					if (!availableTypeNames.Add(name))
						ambiguousTypeNames.Add(name);
				}
			}
		}
		
		sealed class FullyQualifyAmbiguousTypeNamesVisitor : DepthFirstAstVisitor<object, object>
		{
			readonly IntroduceUsingDeclarations transform;
			string currentNamespace;
			HashSet<string> currentMemberTypes;
			Dictionary<string, IMemberRef> currentMembers;
			bool isWithinTypeReferenceExpression;
			
			public FullyQualifyAmbiguousTypeNamesVisitor(IntroduceUsingDeclarations transform)
			{
				this.transform = transform;
				this.currentNamespace = transform.context.CurrentType != null ? transform.context.CurrentType.Namespace.String : string.Empty;
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
				
				Dictionary<string, IMemberRef> oldMembers = currentMembers;
				currentMembers = new Dictionary<string, IMemberRef>();
				
				TypeDef typeDef = typeDeclaration.Annotation<TypeDef>();
				bool privateMembersVisible = true;
				ModuleDef internalMembersVisibleInModule = typeDef == null ? null : typeDef.Module;
				while (typeDef != null) {
					foreach (GenericParam gp in typeDef.GenericParameters) {
						currentMemberTypes.Add(gp.Name);
					}
					foreach (TypeDef t in typeDef.NestedTypes) {
						if (privateMembersVisible || IsVisible(t, internalMembersVisibleInModule))
							currentMemberTypes.Add(t.Name.Substring(t.Name.LastIndexOf('+') + 1));
					}
					
					foreach (MethodDef method in typeDef.Methods) {
						if (privateMembersVisible || IsVisible(method, internalMembersVisibleInModule))
							AddCurrentMember(method);
					}
					foreach (PropertyDef property in typeDef.Properties) {
						if (privateMembersVisible || IsVisible(property.GetMethod, internalMembersVisibleInModule) || IsVisible(property.SetMethod, internalMembersVisibleInModule))
							AddCurrentMember(property);
					}
					foreach (EventDef ev in typeDef.Events) {
						if (privateMembersVisible || IsVisible(ev.AddMethod, internalMembersVisibleInModule) || IsVisible(ev.RemoveMethod, internalMembersVisibleInModule))
							AddCurrentMember(ev);
					}
					foreach (FieldDef f in typeDef.Fields) {
						if (privateMembersVisible || IsVisible(f, internalMembersVisibleInModule))
							AddCurrentMember(f);
					}
					// repeat with base class:
					if (typeDef.BaseType != null)
						typeDef = typeDef.BaseType.ResolveTypeDef();
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
			
			void AddCurrentMember(IMemberRef m)
			{
				IMemberRef existingMember;
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
			
			bool IsVisible(MethodDef m, ModuleDef internalMembersVisibleInModule)
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
			
			bool IsVisible(FieldDef f, ModuleDef internalMembersVisibleInModule)
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
			
			bool IsVisible(TypeDef t, ModuleDef internalMembersVisibleInModule)
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
				ITypeDefOrRef tr = simpleType.Annotation<ITypeDefOrRef>();
				// Fully qualify any ambiguous type names.
				if (tr == null)
					return null;
				var nss = GetNamespace(tr).ToString();
				if (IsAmbiguous(nss, null, GetName(tr))) {
					AstType ns;
					if (string.IsNullOrEmpty(nss)) {
						ns = new SimpleType("global").WithAnnotation(TextTokenKind.Keyword);
					} else {
						string[] parts = nss.Split('.');
						if (IsAmbiguous(string.Empty, parts[0], null)) {
							// conflict between namespace and type name/member name
							ns = new MemberType { Target = new SimpleType("global").WithAnnotation(TextTokenKind.Keyword), IsDoubleColon = true, MemberNameToken = Identifier.Create(parts[0]).WithAnnotation(TextTokenKind.NamespacePart) }.WithAnnotation(TextTokenKind.NamespacePart);
						} else {
							ns = new SimpleType(parts[0]).WithAnnotation(TextTokenKind.NamespacePart);
						}
						for (int i = 1; i < parts.Length; i++) {
							ns = new MemberType { Target = ns, MemberNameToken = Identifier.Create(parts[i]).WithAnnotation(TextTokenKind.NamespacePart) }.WithAnnotation(TextTokenKind.NamespacePart);
						}
					}
					MemberType mt = new MemberType();
					mt.Target = ns;
					mt.IsDoubleColon = string.IsNullOrEmpty(nss);
					mt.MemberNameToken = (Identifier)simpleType.IdentifierToken.Clone();
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

			bool IsAmbiguous(string ns, string name, StringBuilder sbName)
			{
				if (transform.context.Settings.FullyQualifyAllTypes)
					return true;
				// If the type name conflicts with an inner class/type parameter, we need to fully-qualify it:
				if (currentMemberTypes != null && currentMemberTypes.Contains(name ?? (name = sbName.ToString())))
					return true;
				// If the type name conflicts with a field/property etc. on the current class, we need to fully-qualify it,
				// if we're inside an expression.
				if (isWithinTypeReferenceExpression && currentMembers != null) {
					IMemberRef mr;
					if (name == null)
						name = sbName.ToString();
					if (currentMembers.TryGetValue(name, out mr)) {
						// However, in the special case where the member is a field or property with the same type
						// as is requested, then we can use the short name (if it's not otherwise ambiguous)
						PropertyDef prop = mr as PropertyDef;
						FieldDef field = mr as FieldDef;
						if (!(prop != null && GetNamespace(prop.PropertySig.GetRetType()).CheckEquals(ns) && GetName(prop.PropertySig.GetRetType()).CheckEquals(name))
							&& !(field != null && field.FieldType != null && GetNamespace(field.FieldType).CheckEquals(ns) && GetName(field.FieldType).CheckEquals(name)))
							return true;
					}
				}
				// If the type is defined in the current namespace,
				// then we can use the short name even if we imported type with same name from another namespace.
				if (ns == currentNamespace && !string.IsNullOrEmpty(ns))
					return false;
				return transform.ambiguousTypeNames.Contains(name ?? (name = sbName.ToString()));
			}

			StringBuilder GetNamespace(IType type)
			{
				this.transform.stringBuilder.Clear();
				if (type == null)
					return this.transform.stringBuilder;
				return FullNameCreator.NamespaceSB(type, false, this.transform.stringBuilder);
			}

			StringBuilder GetName(IType type)
			{
				this.transform.stringBuilder.Clear();
				if (type == null)
					return this.transform.stringBuilder;
				return FullNameCreator.NameSB(type, false, this.transform.stringBuilder);
			}
		}
	}
}
