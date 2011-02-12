// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace ICSharpCode.SharpDevelop.Dom
{
	/// <summary>
	/// Provides static methods that fill a list of completion results with entries
	/// reachable from a certain calling class/member or entries that introduced
	/// by a certain Using statement.
	/// </summary>
	public class CtrlSpaceResolveHelper
	{
		static void AddTypeParametersForCtrlSpace(List<ICompletionEntry> result, IEnumerable<ITypeParameter> typeParameters)
		{
			foreach (ITypeParameter p in typeParameters) {
				DefaultClass c = DefaultTypeParameter.GetDummyClassForTypeParameter(p);
				if (p.Method != null) {
					c.Documentation = "Type parameter of " + p.Method.Name;
				} else {
					c.Documentation = "Type parameter of " + p.Class.Name;
				}
				result.Add(c);
			}
		}
		
		public static void AddContentsFromCalling(List<ICompletionEntry> result, IClass callingClass, IMember callingMember)
		{
			IMethodOrProperty methodOrProperty = callingMember as IMethodOrProperty;
			if (methodOrProperty != null) {
				foreach (IParameter p in methodOrProperty.Parameters) {
					result.Add(new DefaultField.ParameterField(p.ReturnType, p.Name, methodOrProperty.Region, callingClass));
				}
				if (callingMember is IMethod) {
					AddTypeParametersForCtrlSpace(result, ((IMethod)callingMember).TypeParameters);
				}
			}
			
			bool inStatic = false;
			if (callingMember != null)
				inStatic = callingMember.IsStatic;
			
			if (callingClass != null) {
				AddTypeParametersForCtrlSpace(result, callingClass.TypeParameters);
				
				
				List<ICompletionEntry> members = new List<ICompletionEntry>();
				IReturnType t = callingClass.DefaultReturnType;
				var language = callingClass.ProjectContent.Language;
				foreach (IMember m in MemberLookupHelper.GetAccessibleMembers(t, callingClass, language, true)) {
					if ((!inStatic || m.IsStatic) && language.ShowMember(m, m.IsStatic))
						result.Add(m);
				}
				members.Clear();
				IClass c = callingClass.DeclaringType;
				while (c != null) {
					t = c.DefaultReturnType;
					foreach (IMember m in MemberLookupHelper.GetAccessibleMembers(t, c, language, true)) {
						if (language.ShowMember(m, true))
							result.Add(m);
					}
					c = c.DeclaringType;
				}
			}
		}
		
		/// <summary>
		/// Adds contents of all assemblies referenced by <paramref name="cu" />'s project.
		/// Also adds contents of <paramref name="callingClass" />.
		/// </summary>
		public static void AddReferencedProjectsContents(List<ICompletionEntry> result, ICompilationUnit cu, IClass callingClass)
		{
			IProjectContent projectContent = cu.ProjectContent;
			projectContent.AddNamespaceContents(result, "", projectContent.Language, true);
			var allContents = projectContent.GetAllContents();
			result.Capacity = result.Count + allContents.Count;
			foreach (var entry in allContents.Where(e => !(e is NamespaceEntry))) {
				result.Add(entry);
			}
			AddUsing(result, projectContent.DefaultImports, projectContent);
			AddContentsFromCallingClass(result, projectContent, callingClass);
		}
		
		/// <summary>
		/// Adds contents of all namespaces that this <paramref name="callingClass" /> imports to the <paramref name="result" /> list.
		/// Also adds contents of <paramref name="callingClass" />.
		/// </summary>
		public static void AddImportedNamespaceContents(List<ICompletionEntry> result, ICompilationUnit cu, IClass callingClass)
		{
			IProjectContent projectContent = cu.ProjectContent;
			projectContent.AddNamespaceContents(result, "", projectContent.Language, true);
			IUsingScope scope = (callingClass != null) ? callingClass.UsingScope : cu.UsingScope;
			while (scope != null) {
				foreach (IUsing u in scope.Usings)
					AddUsing(result, u, projectContent);
				scope = scope.Parent;
			}
			AddUsing(result, projectContent.DefaultImports, projectContent);
			AddContentsFromCallingClass(result, projectContent, callingClass);
		}

		static void AddContentsFromCallingClass(List<ICompletionEntry> result, IProjectContent projectContent, IClass callingClass)
		{
			if (callingClass == null) {
				return;
			}
			// use HashSet so that Contains lookups are possible in O(1).
			HashSet<ICompletionEntry> existingResults = new HashSet<ICompletionEntry>(result);
			string[] namespaceParts = callingClass.Namespace.Split('.');
			for (int i = 1; i <= namespaceParts.Length; i++) {
				foreach (ICompletionEntry member in projectContent.GetNamespaceContents(string.Join(".", namespaceParts, 0, i))) {
					if (!existingResults.Contains(member))
						result.Add(member);
				}
			}
			IClass currentClass = callingClass;
			do {
				foreach (IClass innerClass in currentClass.GetCompoundClass().GetAccessibleTypes(currentClass)) {
					if (!existingResults.Contains(innerClass))
						result.Add(innerClass);
				}
				currentClass = currentClass.DeclaringType;
			} while (currentClass != null);
		}
		
		public static void AddUsing(List<ICompletionEntry> result, IUsing u, IProjectContent projectContent)
		{
			if (u == null) {
				return;
			}
			bool importNamespaces = projectContent.Language.ImportNamespaces;
			bool importClasses = projectContent.Language.CanImportClasses;
			foreach (string name in u.Usings) {
				if (importClasses) {
					IClass c = projectContent.GetClass(name, 0);
					if (c != null) {
						ArrayList members = new ArrayList();
						IReturnType t = c.DefaultReturnType;
						members.AddRange(t.GetMethods());
						members.AddRange(t.GetFields());
						members.AddRange(t.GetEvents());
						members.AddRange(t.GetProperties());
						foreach (IMember m in members) {
							if (m.IsStatic && m.IsPublic) {
								result.Add(m);
							}
						}
						continue;
					}
				}
				if (importNamespaces) {
					string newName = null;
					if (projectContent.DefaultImports != null) {
						newName = projectContent.DefaultImports.SearchNamespace(name);
					}
					projectContent.AddNamespaceContents(result, newName ?? name, projectContent.Language, true);
				} else {
					foreach (ICompletionEntry o in projectContent.GetNamespaceContents(name)) {
						if (!(o is NamespaceEntry))
							result.Add(o);
					}
				}
			}
			if (u.HasAliases) {
				foreach (string alias in u.Aliases.Keys) {
					result.Add(new AliasEntry(alias));
				}
			}
		}
		
		public class AliasEntry : ICompletionEntry
		{
			public string Name { get; private set; }
			
			public AliasEntry(string name)
			{
				if (name == null)
					throw new ArgumentNullException("name");
				this.Name = name;
			}
			
			public override int GetHashCode()
			{
				return Name.GetHashCode();
			}
			
			public override bool Equals(object obj)
			{
				AliasEntry e = obj as AliasEntry;
				return e != null && e.Name == this.Name;
			}
			
			public override string ToString()
			{
				return Name;
			}
		}
		
		public static ResolveResult GetResultFromDeclarationLine(IClass callingClass, IMethodOrProperty callingMember, int caretLine, int caretColumn, ExpressionResult expressionResult)
		{
			string expression = expressionResult.Expression;
			if (expression == null) return null;
			if (callingClass == null) return null;
			int pos = expression.IndexOf('(');
			if (pos >= 0) {
				expression = expression.Substring(0, pos);
			}
			expression = expression.Trim();
//			if (!callingClass.BodyRegion.IsInside(caretLine, caretColumn)
//			    && callingClass.ProjectContent.Language.NameComparer.Equals(expression, callingClass.Name))
//			{
//				return new TypeResolveResult(callingClass, callingMember, callingClass);
//			}
			if (expressionResult.Context != ExpressionContext.Type) {
				if (callingMember != null
				    && !callingMember.BodyRegion.IsInside(caretLine, caretColumn)
				    && (callingClass.ProjectContent.Language.NameComparer.Equals(expression, callingMember.Name) ||
				         // For constructor definition, the expression is the constructor name (e.g. "MyClass") but the name of the member is "#ctor"
				         (callingMember.Name == "#ctor" && callingClass.ProjectContent.Language.NameComparer.Equals(expression, callingClass.Name))
				       )
				   )
				{
					return new MemberResolveResult(callingClass, callingMember, callingMember);
				}
			}
			return null;
		}
		
		public static IList<IMethodOrProperty> FindAllExtensions(LanguageProperties language, IClass callingClass, bool searchInAllNamespaces = false)
		{
			if (language == null)
				throw new ArgumentNullException("language");
			if (callingClass == null)
				throw new ArgumentNullException("callingClass");
			
			HashSet<IMethodOrProperty> res = new HashSet<IMethodOrProperty>();
			
			bool supportsExtensionMethods = language.SupportsExtensionMethods;
			bool supportsExtensionProperties = language.SupportsExtensionProperties;
			if (supportsExtensionMethods || supportsExtensionProperties) {
				List<ICompletionEntry> list = new List<ICompletionEntry>();
				IMethod dummyMethod = new DefaultMethod("dummy", callingClass.ProjectContent.SystemTypes.Void,
				                                        ModifierEnum.Static, DomRegion.Empty, DomRegion.Empty, callingClass);
				CtrlSpaceResolveHelper.AddContentsFromCalling(list, callingClass, dummyMethod);
				if (searchInAllNamespaces) {
					// search extension methods in all referenced projects, no matter the using section
					CtrlSpaceResolveHelper.AddReferencedProjectsContents(list, callingClass.CompilationUnit, callingClass);
				} else {
					CtrlSpaceResolveHelper.AddImportedNamespaceContents(list, callingClass.CompilationUnit, callingClass);
				}
				
				bool searchExtensionsInClasses = language.SearchExtensionsInClasses;
				foreach (object o in list) {
					IMethodOrProperty mp = o as IMethodOrProperty;
					if (mp != null && mp.IsExtensionMethod &&
					    (supportsExtensionMethods && o is IMethod || supportsExtensionProperties && o is IProperty))
					{
						res.Add(mp);
					} else if (searchExtensionsInClasses && o is IClass) {
						IClass c = o as IClass;
						if (c.HasExtensionMethods) {
							if (supportsExtensionProperties) {
								foreach (IProperty p in c.Properties) {
									if (p.IsExtensionMethod)
										res.Add(p);
								}
							}
							if (supportsExtensionMethods) {
								foreach (IMethod m in c.Methods) {
									if (m.IsExtensionMethod)
										res.Add(m);
								}
							}
						}
					}
				}
			}
			return res.ToList();
		} // FindAllExtensions
	} // CtrlSpaceResolveHelper class
}
