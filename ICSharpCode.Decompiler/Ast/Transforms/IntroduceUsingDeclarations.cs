// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

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
	public class IntroduceUsingDeclarations : DepthFirstAstVisitor<object, object>, IAstTransform
	{
		DecompilerContext context;
		
		public IntroduceUsingDeclarations(DecompilerContext context)
		{
			this.context = context;
			currentNamespace = context.CurrentType != null ? context.CurrentType.Namespace : string.Empty;
		}
		
		public void Run(AstNode compilationUnit)
		{
			if (!context.Settings.UsingDeclarations)
				return;
			
			// First determine all the namespaces that need to be imported:
			compilationUnit.AcceptVisitor(this, null);
			
			importedNamespaces.Add("System"); // always import System, even when not necessary
			
			// Now add using declarations for those namespaces:
			foreach (string ns in importedNamespaces.OrderByDescending(n => n)) {
				// we go backwards (OrderByDescending) through the list of namespaces because we insert them backwards
				// (always inserting at the start of the list)
				string[] parts = ns.Split('.');
				AstType nsType = new SimpleType(parts[0]);
				for (int i = 1; i < parts.Length; i++) {
					nsType = new MemberType { Target = nsType, MemberName = parts[i] };
				}
				compilationUnit.InsertChildAfter(null, new UsingDeclaration { Import = nsType }, CompilationUnit.MemberRole);
			}
			
			FindAmbiguousTypeNames(context.CurrentModule, internalsVisible: true);
			foreach (AssemblyNameReference r in context.CurrentModule.AssemblyReferences) {
				AssemblyDefinition d = context.CurrentModule.AssemblyResolver.Resolve(r);
				if (d != null)
					FindAmbiguousTypeNames(d.MainModule, internalsVisible: false);
			}
			
			// verify that the SimpleTypes refer to the correct type (no ambiguities)
			FullyQualifyAmbiguousTypeNames(compilationUnit);
		}
		
		readonly HashSet<string> declaredNamespaces = new HashSet<string>() { string.Empty };
		readonly HashSet<string> importedNamespaces = new HashSet<string>();
		
		readonly HashSet<string> availableTypeNames = new HashSet<string>();
		readonly HashSet<string> ambiguousTypeNames = new HashSet<string>();
		string currentNamespace;
		
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
				importedNamespaces.Add(tr.Namespace);
			}
			return base.VisitSimpleType(simpleType, data); // also visit type arguments
		}
		
		public override object VisitNamespaceDeclaration(NamespaceDeclaration namespaceDeclaration, object data)
		{
			string oldNamespace = currentNamespace;
			foreach (Identifier ident in namespaceDeclaration.Identifiers) {
				currentNamespace = NamespaceDeclaration.BuildQualifiedName(currentNamespace, ident.Name);
				declaredNamespaces.Add(currentNamespace);
			}
			base.VisitNamespaceDeclaration(namespaceDeclaration, data);
			currentNamespace = oldNamespace;
			return null;
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
		
		void FullyQualifyAmbiguousTypeNames(AstNode compilationUnit)
		{
			foreach (SimpleType simpleType in compilationUnit.Descendants.OfType<SimpleType>()) {
				TypeReference tr = simpleType.Annotation<TypeReference>();
				if (tr != null && ambiguousTypeNames.Contains(tr.Name)) {
					AstType ns;
					if (string.IsNullOrEmpty(tr.Namespace)) {
						ns = new SimpleType("global");
					} else {
						string[] parts = tr.Namespace.Split('.');
						ns = new SimpleType(parts[0]);
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
			}
		}
	}
}
