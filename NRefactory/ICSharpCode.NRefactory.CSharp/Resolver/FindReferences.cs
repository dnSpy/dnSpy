// Copyright (c) 2010-2013 AlphaSierraPapa for the SharpDevelop Team
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
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.CSharp.Refactoring;
using ICSharpCode.NRefactory.CSharp.TypeSystem;
using ICSharpCode.NRefactory.Semantics;
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.Utils;

namespace ICSharpCode.NRefactory.CSharp.Resolver
{
	public delegate void FoundReferenceCallback(AstNode astNode, ResolveResult result);
	
	/// <summary>
	/// 'Find references' implementation.
	/// </summary>
	/// <remarks>
	/// This class is thread-safe.
	/// The intended multi-threaded usage is to call GetSearchScopes() once, and then
	/// call FindReferencesInFile() concurrently on multiple threads (parallel foreach over all interesting files).
	/// </remarks>
	public sealed class FindReferences
	{
		#region Properties
		/// <summary>
		/// Specifies whether to find type references even if an alias is being used.
		/// Aliases may be <c>var</c> or <c>using Alias = ...;</c>.
		/// </summary>
		public bool FindTypeReferencesEvenIfAliased { get; set; }
		
		/// <summary>
		/// Specifies whether find references should only look for specialized matches
		/// with equal type parameter substitution to the member we are searching for.
		/// </summary>
		public bool FindOnlySpecializedReferences { get; set; }
		
		/// <summary>
		/// If this option is enabled, find references on a overridden member
		/// will find calls to the base member.
		/// </summary>
		public bool FindCallsThroughVirtualBaseMethod { get; set; }
		
		/// <summary>
		/// If this option is enabled, find references on a member implementing
		/// an interface will also find calls to the interface.
		/// </summary>
		public bool FindCallsThroughInterface { get; set; }
		
		/// <summary>
		/// If this option is enabled, find references will look for all references
		/// to the virtual method slot.
		/// </summary>
		public bool WholeVirtualSlot { get; set; }
		
		//public bool FindAllOverloads { get; set; }
		
		/// <summary>
		/// Specifies whether to look for references in documentation comments.
		/// This will find entity references in <c>cref</c> attributes and
		/// parameter references in <c>&lt;param&gt;</c> and <c>&lt;paramref&gt;</c> tags.
		/// TODO: implement this feature.
		/// </summary>
		public bool SearchInDocumentationComments { get; set; }
		#endregion
		
		#region GetEffectiveAccessibility
		/// <summary>
		/// Gets the effective accessibility of the specified entity -
		/// that is, the accessibility viewed from the top level.
		/// </summary>
		/// <remarks>
		/// internal member in public class -> internal
		/// public member in internal class -> internal
		/// protected member in public class -> protected
		/// protected member in internal class -> protected and internal
		/// </remarks>
		public static Accessibility GetEffectiveAccessibility(IEntity entity)
		{
			if (entity == null)
				throw new ArgumentNullException("entity");
			Accessibility a = entity.Accessibility;
			for (ITypeDefinition declType = entity.DeclaringTypeDefinition; declType != null; declType = declType.DeclaringTypeDefinition) {
				a = MergeAccessibility(declType.Accessibility, a);
			}
			return a;
		}
		
		static Accessibility MergeAccessibility(Accessibility outer, Accessibility inner)
		{
			if (outer == inner)
				return inner;
			if (outer == Accessibility.None || inner == Accessibility.None)
				return Accessibility.None;
			if (outer == Accessibility.Private || inner == Accessibility.Private)
				return Accessibility.Private;
			if (outer == Accessibility.Public)
				return inner;
			if (inner == Accessibility.Public)
				return outer;
			// Inner and outer are both in { protected, internal, protected and internal, protected or internal }
			// (but they aren't both the same)
			if (outer == Accessibility.ProtectedOrInternal)
				return inner;
			if (inner == Accessibility.ProtectedOrInternal)
				return outer;
			// Inner and outer are both in { protected, internal, protected and internal },
			// but aren't both the same, so the result is protected and internal.
			return Accessibility.ProtectedAndInternal;
		}
		#endregion
		
		#region class SearchScope
		sealed class SearchScope : IFindReferenceSearchScope
		{
			readonly Func<ICompilation, FindReferenceNavigator> factory;
			
			public SearchScope(Func<ICompilation, FindReferenceNavigator> factory)
			{
				this.factory = factory;
			}
			
			public SearchScope(string searchTerm, Func<ICompilation, FindReferenceNavigator> factory)
			{
				this.searchTerm = searchTerm;
				this.factory = factory;
			}
			
			internal string searchTerm;
			internal FindReferences findReferences;
			internal ICompilation declarationCompilation;
			internal Accessibility accessibility;
			internal ITypeDefinition topLevelTypeDefinition;
			internal string fileName;
			
			IResolveVisitorNavigator IFindReferenceSearchScope.GetNavigator(ICompilation compilation, FoundReferenceCallback callback)
			{
				FindReferenceNavigator n = factory(compilation);
				if (n != null) {
					n.callback = callback;
					n.findReferences = findReferences;
					return n;
				} else {
					return new ConstantModeResolveVisitorNavigator(ResolveVisitorNavigationMode.Skip, null);
				}
			}
			
			ICompilation IFindReferenceSearchScope.Compilation {
				get { return declarationCompilation; }
			}
			
			string IFindReferenceSearchScope.SearchTerm {
				get { return searchTerm; }
			}
			
			Accessibility IFindReferenceSearchScope.Accessibility {
				get { return accessibility; }
			}
			
			ITypeDefinition IFindReferenceSearchScope.TopLevelTypeDefinition {
				get { return topLevelTypeDefinition; }
			}
			
			string IFindReferenceSearchScope.FileName {
				get { return fileName; }
			}
		}
		
		abstract class FindReferenceNavigator : IResolveVisitorNavigator
		{
			internal FoundReferenceCallback callback;
			internal FindReferences findReferences;
			
			internal abstract bool CanMatch(AstNode node);
			internal abstract bool IsMatch(ResolveResult rr);
			
			ResolveVisitorNavigationMode IResolveVisitorNavigator.Scan(AstNode node)
			{
				if (CanMatch(node))
					return ResolveVisitorNavigationMode.Resolve;
				else
					return ResolveVisitorNavigationMode.Scan;
			}
			
			void IResolveVisitorNavigator.Resolved(AstNode node, ResolveResult result)
			{
				if (CanMatch(node) && IsMatch(result)) {
					ReportMatch(node, result);
				}
			}
			
			public virtual void ProcessConversion(Expression expression, ResolveResult result, Conversion conversion, IType targetType)
			{
			}
			
			protected void ReportMatch(AstNode node, ResolveResult result)
			{
				if (callback != null)
					callback(node, result);
			}
			
			internal virtual void NavigatorDone(CSharpAstResolver resolver, CancellationToken cancellationToken)
			{
			}
		}
		#endregion
		
		#region GetSearchScopes
		public IList<IFindReferenceSearchScope> GetSearchScopes(ISymbol symbol)
		{
			if (symbol == null)
				throw new ArgumentNullException("symbol");
			switch (symbol.SymbolKind) {
				case SymbolKind.Namespace:
					return new[] { GetSearchScopeForNamespace((INamespace)symbol) };
				case SymbolKind.TypeParameter:
					return new[] { GetSearchScopeForTypeParameter((ITypeParameter)symbol) };
			}
			SearchScope scope;
			SearchScope additionalScope = null;
			IEntity entity = null;

			if (symbol.SymbolKind == SymbolKind.Variable) {
				var variable = (IVariable) symbol;
				scope = GetSearchScopeForLocalVariable(variable);
			} else if (symbol.SymbolKind == SymbolKind.Parameter) {
				var par = (IParameter)symbol;
				scope = GetSearchScopeForParameter(par);
				entity = par.Owner;
			} else {
				entity = symbol as IEntity;
				if (entity == null)
					throw new NotSupportedException("Unsupported symbol type");
				if (entity is IMember)
					entity = NormalizeMember((IMember)entity);
				switch (entity.SymbolKind) {
					case SymbolKind.TypeDefinition:
						scope = FindTypeDefinitionReferences((ITypeDefinition)entity, this.FindTypeReferencesEvenIfAliased, out additionalScope);
						break;
					case SymbolKind.Field:
						if (entity.DeclaringTypeDefinition != null && entity.DeclaringTypeDefinition.Kind == TypeKind.Enum)
							scope = FindMemberReferences(entity, m => new FindEnumMemberReferences((IField)m));
						else
							scope = FindMemberReferences(entity, m => new FindFieldReferences((IField)m));
						break;
					case SymbolKind.Property:
						scope = FindMemberReferences(entity, m => new FindPropertyReferences((IProperty)m));
						if (entity.Name == "Current")
							additionalScope = FindEnumeratorCurrentReferences((IProperty)entity);
						else if (entity.Name == "IsCompleted")
							additionalScope = FindAwaiterIsCompletedReferences((IProperty)entity);
						break;
					case SymbolKind.Event:
						scope = FindMemberReferences(entity, m => new FindEventReferences((IEvent)m));
						break;
					case SymbolKind.Method:
						scope = GetSearchScopeForMethod((IMethod)entity);
						break;
					case SymbolKind.Indexer:
						scope = FindIndexerReferences((IProperty)entity);
						break;
					case SymbolKind.Operator:
						scope = GetSearchScopeForOperator((IMethod)entity);
						break;
					case SymbolKind.Constructor:
						IMethod ctor = (IMethod)entity;
						scope = FindObjectCreateReferences(ctor);
						additionalScope = FindChainedConstructorReferences(ctor);
						break;
					case SymbolKind.Destructor:
						scope = GetSearchScopeForDestructor((IMethod)entity);
						break;
					default:
						throw new ArgumentException("Unknown entity type " + entity.SymbolKind);
				}
			}
			var effectiveAccessibility = entity != null ? GetEffectiveAccessibility(entity) : Accessibility.Private;
			var topLevelTypeDefinition = GetTopLevelTypeDefinition(entity);

			if (scope.accessibility == Accessibility.None)
				scope.accessibility = effectiveAccessibility;
			scope.declarationCompilation = entity != null ? entity.Compilation : null;
			scope.topLevelTypeDefinition = topLevelTypeDefinition;
			scope.findReferences = this;
			if (additionalScope != null) {
				if (additionalScope.accessibility == Accessibility.None)
					additionalScope.accessibility = effectiveAccessibility;
				additionalScope.declarationCompilation = scope.declarationCompilation;
				additionalScope.topLevelTypeDefinition = topLevelTypeDefinition;
				additionalScope.findReferences = this;
				return new[] { scope, additionalScope };
			} else {
				return new[] { scope };
			}
		}

		public IList<IFindReferenceSearchScope> GetSearchScopes(IEnumerable<ISymbol> symbols)
		{
			if (symbols == null)
				throw new ArgumentNullException("symbols");
			return symbols.SelectMany(GetSearchScopes).ToList();
		}
		
		static ITypeDefinition GetTopLevelTypeDefinition(IEntity entity)
		{
			if (entity == null)
				return null;
			ITypeDefinition topLevelTypeDefinition = entity.DeclaringTypeDefinition;
			while (topLevelTypeDefinition != null && topLevelTypeDefinition.DeclaringTypeDefinition != null)
				topLevelTypeDefinition = topLevelTypeDefinition.DeclaringTypeDefinition;
			return topLevelTypeDefinition;
		}
		#endregion
		
		#region GetInterestingFileNames
		/// <summary>
		/// Gets the file names that possibly contain references to the element being searched for.
		/// </summary>
		public IEnumerable<CSharpUnresolvedFile> GetInterestingFiles(IFindReferenceSearchScope searchScope, ICompilation compilation)
		{
			if (searchScope == null)
				throw new ArgumentNullException("searchScope");
			if (compilation == null)
				throw new ArgumentNullException("compilation");
			var pc = compilation.MainAssembly.UnresolvedAssembly as IProjectContent;
			if (pc == null)
				throw new ArgumentException("Main assembly is not a project content");
			if (searchScope.TopLevelTypeDefinition != null) {
				ITypeDefinition topLevelTypeDef = compilation.Import(searchScope.TopLevelTypeDefinition);
				if (topLevelTypeDef == null) {
					// This compilation cannot have references to the target entity.
					return EmptyList<CSharpUnresolvedFile>.Instance;
				}
				switch (searchScope.Accessibility) {
					case Accessibility.None:
					case Accessibility.Private:
						if (topLevelTypeDef.ParentAssembly == compilation.MainAssembly)
							return topLevelTypeDef.Parts.Select(p => p.UnresolvedFile).OfType<CSharpUnresolvedFile>().Distinct();
						else
							return EmptyList<CSharpUnresolvedFile>.Instance;
					case Accessibility.Protected:
						return GetInterestingFilesProtected(topLevelTypeDef);
					case Accessibility.Internal:
						if (topLevelTypeDef.ParentAssembly.InternalsVisibleTo(compilation.MainAssembly))
							return pc.Files.OfType<CSharpUnresolvedFile>();
						else
							return EmptyList<CSharpUnresolvedFile>.Instance;
					case Accessibility.ProtectedAndInternal:
						if (topLevelTypeDef.ParentAssembly.InternalsVisibleTo(compilation.MainAssembly))
							return GetInterestingFilesProtected(topLevelTypeDef);
						else
							return EmptyList<CSharpUnresolvedFile>.Instance;
					case Accessibility.ProtectedOrInternal:
						if (topLevelTypeDef.ParentAssembly.InternalsVisibleTo(compilation.MainAssembly))
							return pc.Files.OfType<CSharpUnresolvedFile>();
						else
							return GetInterestingFilesProtected(topLevelTypeDef);
					default:
						return pc.Files.OfType<CSharpUnresolvedFile>();
				}
			} else {
				if (searchScope.FileName == null)
					return pc.Files.OfType<CSharpUnresolvedFile>();
				else
					return pc.Files.OfType<CSharpUnresolvedFile>().Where(f => f.FileName == searchScope.FileName);
			}
		}
		
		IEnumerable<CSharpUnresolvedFile> GetInterestingFilesProtected(ITypeDefinition referencedTypeDefinition)
		{
			return (from typeDef in referencedTypeDefinition.Compilation.MainAssembly.GetAllTypeDefinitions()
			        where typeDef.IsDerivedFrom(referencedTypeDefinition)
			        from part in typeDef.Parts
			        select part.UnresolvedFile
			       ).OfType<CSharpUnresolvedFile>().Distinct();
		}
		#endregion
		
		#region FindReferencesInFile
		/// <summary>
		/// Finds all references in the given file.
		/// </summary>
		/// <param name="searchScope">The search scope for which to look.</param>
		/// <param name="resolver">AST resolver for the file to search in.</param>
		/// <param name="callback">Callback used to report the references that were found.</param>
		/// <param name="cancellationToken">CancellationToken that may be used to cancel the operation.</param>
		public void FindReferencesInFile(IFindReferenceSearchScope searchScope, CSharpAstResolver resolver,
		                                 FoundReferenceCallback callback, CancellationToken cancellationToken)
		{
			if (resolver == null)
				throw new ArgumentNullException("resolver");
			FindReferencesInFile(searchScope, resolver.UnresolvedFile, (SyntaxTree)resolver.RootNode, resolver.Compilation, callback, cancellationToken);
		}
		
		/// <summary>
		/// Finds all references in the given file.
		/// </summary>
		/// <param name="searchScopes">The search scopes for which to look.</param>
		/// <param name="resolver">AST resolver for the file to search in.</param>
		/// <param name="callback">Callback used to report the references that were found.</param>
		/// <param name="cancellationToken">CancellationToken that may be used to cancel the operation.</param>
		public void FindReferencesInFile(IList<IFindReferenceSearchScope> searchScopes, CSharpAstResolver resolver,
		                                 FoundReferenceCallback callback, CancellationToken cancellationToken)
		{
			if (resolver == null)
				throw new ArgumentNullException("resolver");
			FindReferencesInFile(searchScopes, resolver.UnresolvedFile, (SyntaxTree)resolver.RootNode, resolver.Compilation, callback, cancellationToken);
		}
		
		/// <summary>
		/// Finds all references in the given file.
		/// </summary>
		/// <param name="searchScope">The search scope for which to look.</param>
		/// <param name="unresolvedFile">The type system representation of the file being searched.</param>
		/// <param name="syntaxTree">The syntax tree of the file being searched.</param>
		/// <param name="compilation">The compilation for the project that contains the file.</param>
		/// <param name="callback">Callback used to report the references that were found.</param>
		/// <param name="cancellationToken">CancellationToken that may be used to cancel the operation.</param>
		public void FindReferencesInFile(IFindReferenceSearchScope searchScope, CSharpUnresolvedFile unresolvedFile, SyntaxTree syntaxTree,
		                                 ICompilation compilation, FoundReferenceCallback callback, CancellationToken cancellationToken)
		{
			if (searchScope == null)
				throw new ArgumentNullException("searchScope");
			FindReferencesInFile(new[] { searchScope }, unresolvedFile, syntaxTree, compilation, callback, cancellationToken);
		}
		
		/// <summary>
		/// Finds all references in the given file.
		/// </summary>
		/// <param name="searchScopes">The search scopes for which to look.</param>
		/// <param name="unresolvedFile">The type system representation of the file being searched.</param>
		/// <param name="syntaxTree">The syntax tree of the file being searched.</param>
		/// <param name="compilation">The compilation for the project that contains the file.</param>
		/// <param name="callback">Callback used to report the references that were found.</param>
		/// <param name="cancellationToken">CancellationToken that may be used to cancel the operation.</param>
		public void FindReferencesInFile(IList<IFindReferenceSearchScope> searchScopes, CSharpUnresolvedFile unresolvedFile, SyntaxTree syntaxTree,
		                                 ICompilation compilation, FoundReferenceCallback callback, CancellationToken cancellationToken)
		{
			if (searchScopes == null)
				throw new ArgumentNullException("searchScopes");
			if (syntaxTree == null)
				throw new ArgumentNullException("syntaxTree");
			if (compilation == null)
				throw new ArgumentNullException("compilation");
			if (callback == null)
				throw new ArgumentNullException("callback");
			
			if (searchScopes.Count == 0)
				return;
			var navigators = new IResolveVisitorNavigator[searchScopes.Count];
			for (int i = 0; i < navigators.Length; i++) {
				navigators[i] = searchScopes[i].GetNavigator(compilation, callback);
			}
			IResolveVisitorNavigator combinedNavigator;
			if (searchScopes.Count == 1) {
				combinedNavigator = navigators[0];
			} else {
				combinedNavigator = new CompositeResolveVisitorNavigator(navigators);
			}
			
			cancellationToken.ThrowIfCancellationRequested();
			combinedNavigator = new DetectSkippableNodesNavigator(combinedNavigator, syntaxTree);
			cancellationToken.ThrowIfCancellationRequested();
			CSharpAstResolver resolver = new CSharpAstResolver(compilation, syntaxTree, unresolvedFile);
			resolver.ApplyNavigator(combinedNavigator, cancellationToken);
			foreach (var n in navigators) {
				var frn = n as FindReferenceNavigator;
				if (frn != null)
					frn.NavigatorDone(resolver, cancellationToken);
			}
		}
		#endregion
		
		#region RenameReferencesInFile

		public static AstNode GetNodeToReplace(AstNode node)
		{
			if (node is ConstructorInitializer)
				return null;
			if (node is ObjectCreateExpression)
				node = ((ObjectCreateExpression)node).Type;

			if (node is InvocationExpression)
				node = ((InvocationExpression)node).Target;

			if (node is MemberReferenceExpression)
				node = ((MemberReferenceExpression)node).MemberNameToken;

			if (node is SimpleType)
				node = ((SimpleType)node).IdentifierToken;

			if (node is MemberType)
				node = ((MemberType)node).MemberNameToken;

			if (node is NamespaceDeclaration) {
//				var nsd = ((NamespaceDeclaration)node);
//				node = nsd.Identifiers.LastOrDefault (n => n.Name == memberName) ?? nsd.Identifiers.FirstOrDefault ();
//				if (node == null)
				return null;
			}

			if (node is TypeDeclaration)
				node = ((TypeDeclaration)node).NameToken;
			if (node is DelegateDeclaration)
				node = ((DelegateDeclaration)node).NameToken;

			if (node is EntityDeclaration)
				node = ((EntityDeclaration)node).NameToken;

			if (node is ParameterDeclaration)
				node = ((ParameterDeclaration)node).NameToken;
			if (node is ConstructorDeclaration)
				node = ((ConstructorDeclaration)node).NameToken;
			if (node is DestructorDeclaration)
				node = ((DestructorDeclaration)node).NameToken;
			if (node is NamedArgumentExpression)
				node = ((NamedArgumentExpression)node).NameToken;
			if (node is NamedExpression)
				node = ((NamedExpression)node).NameToken;
			if (node is VariableInitializer)
				node = ((VariableInitializer)node).NameToken;

			if (node is IdentifierExpression) {
				node = ((IdentifierExpression)node).IdentifierToken;
			}
			return node;
		}

		public void RenameReferencesInFile(IList<IFindReferenceSearchScope> searchScopes, string newName, CSharpAstResolver resolver,
		                                   Action<RenameCallbackArguments> callback, Action<Error> errorCallback, CancellationToken cancellationToken = default (CancellationToken))
		{
			FindReferencesInFile(
				searchScopes,
				resolver,
				delegate(AstNode astNode, ResolveResult result) {
					var nodeToReplace = GetNodeToReplace(astNode);
					if (nodeToReplace == null) {
						errorCallback (new Error (ErrorType.Error, "no node to replace found."));
						return;
					}
					callback (new RenameCallbackArguments(nodeToReplace, Identifier.Create(newName)));
				},
				cancellationToken);
		}
		#endregion
		
		#region Find TypeDefinition References
		SearchScope FindTypeDefinitionReferences(ITypeDefinition typeDefinition, bool findTypeReferencesEvenIfAliased, out SearchScope additionalScope)
		{
			string searchTerm = null;
			additionalScope = null;
			if (!findTypeReferencesEvenIfAliased && KnownTypeReference.GetCSharpNameByTypeCode(typeDefinition.KnownTypeCode) == null) {
				// We can optimize the search by looking only for the type references with the right identifier,
				// but only if it's not a primitive type and we're not looking for indirect references (through an alias)
				searchTerm = typeDefinition.Name;
				if (searchTerm.Length > 9 && searchTerm.EndsWith("Attribute", StringComparison.Ordinal)) {
					// The type might be an attribute, so we also need to look for the short form:
					string shortForm = searchTerm.Substring(0, searchTerm.Length - 9);
					additionalScope = FindTypeDefinitionReferences(typeDefinition, shortForm);
				}
			}
			return FindTypeDefinitionReferences(typeDefinition, searchTerm);
		}
		
		SearchScope FindTypeDefinitionReferences(ITypeDefinition typeDefinition, string searchTerm)
		{
			return new SearchScope(
				searchTerm,
				delegate (ICompilation compilation) {
					ITypeDefinition imported = compilation.Import(typeDefinition);
					if (imported != null)
						return new FindTypeDefinitionReferencesNavigator(imported, searchTerm);
					else
						return null;
				});
		}
		
		sealed class FindTypeDefinitionReferencesNavigator : FindReferenceNavigator
		{
			readonly ITypeDefinition typeDefinition;
			readonly string searchTerm;
			
			public FindTypeDefinitionReferencesNavigator(ITypeDefinition typeDefinition, string searchTerm)
			{
				this.typeDefinition = typeDefinition;
				this.searchTerm = searchTerm;
			}
			
			internal override bool CanMatch(AstNode node)
			{
				IdentifierExpression ident = node as IdentifierExpression;
				if (ident != null)
					return searchTerm == null || ident.Identifier == searchTerm;
				
				MemberReferenceExpression mre = node as MemberReferenceExpression;
				if (mre != null)
					return searchTerm == null || mre.MemberName == searchTerm;
				
				SimpleType st = node as SimpleType;
				if (st != null)
					return searchTerm == null || st.Identifier == searchTerm;
				
				MemberType mt = node as MemberType;
				if (mt != null)
					return searchTerm == null || mt.MemberName == searchTerm;
				
				if (searchTerm == null && node is PrimitiveType)
					return true;
				
				TypeDeclaration typeDecl = node as TypeDeclaration;
				if (typeDecl != null)
					return searchTerm == null || typeDecl.Name == searchTerm;
				
				DelegateDeclaration delegateDecl = node as DelegateDeclaration;
				if (delegateDecl != null)
					return searchTerm == null || delegateDecl.Name == searchTerm;
				
				return false;
			}
			
			internal override bool IsMatch(ResolveResult rr)
			{
				TypeResolveResult trr = rr as TypeResolveResult;
				return trr != null && typeDefinition.Equals(trr.Type.GetDefinition());
			}
		}
		#endregion
		
		#region Find Member References
		SearchScope FindMemberReferences(IEntity member, Func<IMember, FindMemberReferencesNavigator> factory)
		{
			string searchTerm = member.Name;
			return new SearchScope(
				searchTerm,
				delegate(ICompilation compilation) {
					IMember imported = compilation.Import((IMember)member);
					return imported != null ? factory(imported) : null;
				});
		}
		
		class FindMemberReferencesNavigator : FindReferenceNavigator
		{
			readonly IMember member;
			readonly string searchTerm;
			
			public FindMemberReferencesNavigator(IMember member)
			{
				this.member = member;
				this.searchTerm = member.Name;
			}
			
			internal override bool CanMatch(AstNode node)
			{
				IdentifierExpression ident = node as IdentifierExpression;
				if (ident != null)
					return ident.Identifier == searchTerm;
				
				MemberReferenceExpression mre = node as MemberReferenceExpression;
				if (mre != null)
					return mre.MemberName == searchTerm;
				
				PointerReferenceExpression pre = node as PointerReferenceExpression;
				if (pre != null)
					return pre.MemberName == searchTerm;
				
				NamedExpression ne = node as NamedExpression;
				if (ne != null)
					return ne.Name == searchTerm;
				
				return false;
			}
			
			internal override bool IsMatch(ResolveResult rr)
			{
				MemberResolveResult mrr = rr as MemberResolveResult;
				return mrr != null && findReferences.IsMemberMatch(member, mrr.Member, mrr.IsVirtualCall);
			}
		}
		
		IMember NormalizeMember(IMember member)
		{
			if (WholeVirtualSlot && member.IsOverride)
				member = InheritanceHelper.GetBaseMembers(member, false).FirstOrDefault(m => !m.IsOverride) ?? member;
			if (!FindOnlySpecializedReferences)
				member = member.MemberDefinition;
			return member;
		}
		
		bool IsMemberMatch(IMember member, IMember referencedMember, bool isVirtualCall)
		{
			referencedMember = NormalizeMember(referencedMember);
			if (member.Equals(referencedMember))
				return true;
			if (FindCallsThroughInterface && member.DeclaringTypeDefinition != null && member.DeclaringTypeDefinition.Kind == TypeKind.Interface) {
				if (FindOnlySpecializedReferences) {
					return referencedMember.ImplementedInterfaceMembers.Contains(member);
				} else {
					return referencedMember.ImplementedInterfaceMembers.Any(m => m.MemberDefinition.Equals(member));
				}
			}
			if (!isVirtualCall)
				return false;
			bool isInterfaceCall = referencedMember.DeclaringTypeDefinition != null && referencedMember.DeclaringTypeDefinition.Kind == TypeKind.Interface;
			if (FindCallsThroughVirtualBaseMethod && member.IsOverride && !WholeVirtualSlot && !isInterfaceCall) {
				// Test if 'member' overrides 'referencedMember':
				foreach (var baseMember in InheritanceHelper.GetBaseMembers(member, false)) {
					if (FindOnlySpecializedReferences) {
						if (baseMember.Equals(referencedMember))
							return true;
					} else {
						if (baseMember.MemberDefinition.Equals(referencedMember))
							return true;
					}
					if (!baseMember.IsOverride)
						break;
				}
				return false;
			} else if (FindCallsThroughInterface && isInterfaceCall) {
				// Test if 'member' implements 'referencedMember':
				if (FindOnlySpecializedReferences) {
					return member.ImplementedInterfaceMembers.Contains(referencedMember);
				} else {
					return member.ImplementedInterfaceMembers.Any(m => m.MemberDefinition.Equals(referencedMember));
				}
			}
			return false;
		}
		
		/*
		bool PerformVirtualLookup(IMember member, IMember referencedMember)
		{
			if (FindCallsThroughVirtualBaseMethod && member.IsOverride && !WholeVirtualSlot)
				return true;
			var typeDef = referencedMember.DeclaringTypeDefinition;
			return FindCallsThroughInterface && typeDef != null && typeDef.Kind == TypeKind.Interface;
		}*/
		
		sealed class FindFieldReferences : FindMemberReferencesNavigator
		{
			public FindFieldReferences(IField field) : base(field)
			{
			}
			
			internal override bool CanMatch(AstNode node)
			{
				if (node is VariableInitializer) {
					return node.Parent is FieldDeclaration;
				}
				return base.CanMatch(node);
			}
		}
		
		sealed class FindEnumMemberReferences : FindMemberReferencesNavigator
		{
			public FindEnumMemberReferences(IField field) : base(field)
			{
			}
			
			internal override bool CanMatch(AstNode node)
			{
				return node is EnumMemberDeclaration || base.CanMatch(node);
			}
		}
		
		sealed class FindPropertyReferences : FindMemberReferencesNavigator
		{
			public FindPropertyReferences(IProperty property) : base(property)
			{
			}
			
			internal override bool CanMatch(AstNode node)
			{
				return node is PropertyDeclaration || base.CanMatch(node);
			}
		}
		
		sealed class FindEventReferences : FindMemberReferencesNavigator
		{
			public FindEventReferences(IEvent ev) : base(ev)
			{
			}
			
			internal override bool CanMatch(AstNode node)
			{
				if (node is VariableInitializer) {
					return node.Parent is EventDeclaration;
				}
				return node is CustomEventDeclaration || base.CanMatch(node);
			}
		}
		#endregion
		
		#region Find References to IEnumerator.Current
		SearchScope FindEnumeratorCurrentReferences(IProperty property)
		{
			return new SearchScope(
				delegate(ICompilation compilation) {
					IProperty imported = compilation.Import(property);
					return imported != null ? new FindEnumeratorCurrentReferencesNavigator(imported) : null;
				});
		}

		SearchScope FindAwaiterIsCompletedReferences(IProperty property)
		{
			return new SearchScope(
				delegate(ICompilation compilation) {
					IProperty imported = compilation.Import(property);
					return imported != null ? new FindAwaiterIsCompletedReferencesNavigator(imported) : null;
				});
		}
		
		sealed class FindEnumeratorCurrentReferencesNavigator : FindReferenceNavigator
		{
			IProperty property;
			
			public FindEnumeratorCurrentReferencesNavigator(IProperty property)
			{
				this.property = property;
			}
			
			internal override bool CanMatch(AstNode node)
			{
				return node is ForeachStatement;
			}
			
			internal override bool IsMatch(ResolveResult rr)
			{
				ForEachResolveResult ferr = rr as ForEachResolveResult;
				return ferr != null && ferr.CurrentProperty != null && findReferences.IsMemberMatch(property, ferr.CurrentProperty, true);
			}
		}

		sealed class FindAwaiterIsCompletedReferencesNavigator : FindReferenceNavigator
		{
			IProperty property;
			
			public FindAwaiterIsCompletedReferencesNavigator(IProperty property)
			{
				this.property = property;
			}
			
			internal override bool CanMatch(AstNode node)
			{
				return node is UnaryOperatorExpression;
			}
			
			internal override bool IsMatch(ResolveResult rr)
			{
				AwaitResolveResult arr = rr as AwaitResolveResult;
				return arr != null && arr.IsCompletedProperty != null && findReferences.IsMemberMatch(property, arr.IsCompletedProperty, true);
			}
		}
		#endregion
		
		#region Find Method References
		SearchScope GetSearchScopeForMethod(IMethod method)
		{
			Type specialNodeType;
			switch (method.Name) {
				case "Add":
					specialNodeType = typeof(ArrayInitializerExpression);
					break;
				case "Where":
					specialNodeType = typeof(QueryWhereClause);
					break;
				case "Select":
					specialNodeType = typeof(QuerySelectClause);
					break;
				case "SelectMany":
					specialNodeType = typeof(QueryFromClause);
					break;
				case "Join":
				case "GroupJoin":
					specialNodeType = typeof(QueryJoinClause);
					break;
				case "OrderBy":
				case "OrderByDescending":
				case "ThenBy":
				case "ThenByDescending":
					specialNodeType = typeof(QueryOrdering);
					break;
				case "GroupBy":
					specialNodeType = typeof(QueryGroupClause);
					break;
				case "Invoke":
					if (method.DeclaringTypeDefinition != null && method.DeclaringTypeDefinition.Kind == TypeKind.Delegate)
						specialNodeType = typeof(InvocationExpression);
					else
						specialNodeType = null;
					break;
				case "GetEnumerator":
				case "MoveNext":
					specialNodeType = typeof(ForeachStatement);
					break;
				case "GetAwaiter":
				case "GetResult":
				case "OnCompleted":
				case "UnsafeOnCompleted":
					specialNodeType = typeof(UnaryOperatorExpression);
					break;
				default:
					specialNodeType = null;
					break;
			}
			// Use searchTerm only if specialNodeType==null
			string searchTerm = (specialNodeType == null) ? method.Name : null;
			return new SearchScope(
				searchTerm,
				delegate (ICompilation compilation) {
					IMethod imported = compilation.Import(method);
					if (imported != null)
						return new FindMethodReferences(imported, specialNodeType);
					else
						return null;
				});
		}
		
		sealed class FindMethodReferences : FindReferenceNavigator
		{
			readonly IMethod method;
			readonly Type specialNodeType;
			HashSet<Expression> potentialMethodGroupConversions = new HashSet<Expression>();
			
			public FindMethodReferences(IMethod method, Type specialNodeType)
			{
				this.method = method;
				this.specialNodeType = specialNodeType;
			}
			
			internal override bool CanMatch(AstNode node)
			{
				if (specialNodeType != null && node.GetType() == specialNodeType)
					return true;
				
				Expression expr = node as Expression;
				if (expr == null)
					return node is MethodDeclaration;
				
				InvocationExpression ie = node as InvocationExpression;
				if (ie != null) {
					Expression target = ParenthesizedExpression.UnpackParenthesizedExpression(ie.Target);
					
					IdentifierExpression ident = target as IdentifierExpression;
					if (ident != null)
						return ident.Identifier == method.Name;
					
					MemberReferenceExpression mre = target as MemberReferenceExpression;
					if (mre != null)
						return mre.MemberName == method.Name;
					
					PointerReferenceExpression pre = target as PointerReferenceExpression;
					if (pre != null)
						return pre.MemberName == method.Name;
				} else if (expr.Role != Roles.TargetExpression) {
					// MemberReferences & Identifiers that aren't used in an invocation can still match the method
					// as delegate name.
					if (expr.GetChildByRole(Roles.Identifier).Name == method.Name)
						potentialMethodGroupConversions.Add(expr);
				}
				return node is MethodDeclaration;
			}
			
			internal override bool IsMatch(ResolveResult rr)
			{
				if (specialNodeType != null) {
					var ferr = rr as ForEachResolveResult;
					if (ferr != null) {
						return IsMatch(ferr.GetEnumeratorCall)
							|| (ferr.MoveNextMethod != null && findReferences.IsMemberMatch(method, ferr.MoveNextMethod, true));
					}
					var arr = rr as AwaitResolveResult;
					if (arr != null) {
						return IsMatch(arr.GetAwaiterInvocation)
							|| (arr.GetResultMethod != null && findReferences.IsMemberMatch(method, arr.GetResultMethod, true))
							|| (arr.OnCompletedMethod != null && findReferences.IsMemberMatch(method, arr.OnCompletedMethod, true));
					}
				}
				var mrr = rr as MemberResolveResult;
				return mrr != null && findReferences.IsMemberMatch(method, mrr.Member, mrr.IsVirtualCall);
			}
			
			internal override void NavigatorDone(CSharpAstResolver resolver, CancellationToken cancellationToken)
			{
				foreach (var expr in potentialMethodGroupConversions) {
					var conversion = resolver.GetConversion(expr, cancellationToken);
					if (conversion.IsMethodGroupConversion && findReferences.IsMemberMatch(method, conversion.Method, conversion.IsVirtualMethodLookup)) {
						IType targetType = resolver.GetExpectedType(expr, cancellationToken);
						ResolveResult result = resolver.Resolve(expr, cancellationToken);
						ReportMatch(expr, new ConversionResolveResult(targetType, result, conversion));
					}
				}
				base.NavigatorDone(resolver, cancellationToken);
			}
		}
		#endregion
		
		#region Find Indexer References
		SearchScope FindIndexerReferences(IProperty indexer)
		{
			return new SearchScope(
				delegate (ICompilation compilation) {
					IProperty imported = compilation.Import(indexer);
					if (imported != null)
						return new FindIndexerReferencesNavigator(imported);
					else
						return null;
				});
		}
		
		sealed class FindIndexerReferencesNavigator : FindReferenceNavigator
		{
			readonly IProperty indexer;
			
			public FindIndexerReferencesNavigator(IProperty indexer)
			{
				this.indexer = indexer;
			}
			
			internal override bool CanMatch(AstNode node)
			{
				return node is IndexerExpression || node is IndexerDeclaration;
			}
			
			internal override bool IsMatch(ResolveResult rr)
			{
				MemberResolveResult mrr = rr as MemberResolveResult;
				return mrr != null && findReferences.IsMemberMatch(indexer, mrr.Member, mrr.IsVirtualCall);
			}
		}
		#endregion
		
		#region Find Operator References
		SearchScope GetSearchScopeForOperator(IMethod op)
		{
			OperatorType? opType = OperatorDeclaration.GetOperatorType(op.Name);
			if (opType == null)
				return GetSearchScopeForMethod(op);
			switch (opType.Value) {
				case OperatorType.LogicalNot:
					return FindUnaryOperator(op, UnaryOperatorType.Not);
				case OperatorType.OnesComplement:
					return FindUnaryOperator(op, UnaryOperatorType.BitNot);
				case OperatorType.UnaryPlus:
					return FindUnaryOperator(op, UnaryOperatorType.Plus);
				case OperatorType.UnaryNegation:
					return FindUnaryOperator(op, UnaryOperatorType.Minus);
				case OperatorType.Increment:
					return FindUnaryOperator(op, UnaryOperatorType.Increment);
				case OperatorType.Decrement:
					return FindUnaryOperator(op, UnaryOperatorType.Decrement);
				case OperatorType.True:
				case OperatorType.False:
					// TODO: implement search for op_True/op_False correctly
					return GetSearchScopeForMethod(op);
				case OperatorType.Addition:
					return FindBinaryOperator(op, BinaryOperatorType.Add);
				case OperatorType.Subtraction:
					return FindBinaryOperator(op, BinaryOperatorType.Subtract);
				case OperatorType.Multiply:
					return FindBinaryOperator(op, BinaryOperatorType.Multiply);
				case OperatorType.Division:
					return FindBinaryOperator(op, BinaryOperatorType.Divide);
				case OperatorType.Modulus:
					return FindBinaryOperator(op, BinaryOperatorType.Modulus);
				case OperatorType.BitwiseAnd:
					// TODO: an overloaded bitwise operator can also be called using the corresponding logical operator
					// (if op_True/op_False is defined)
					return FindBinaryOperator(op, BinaryOperatorType.BitwiseAnd);
				case OperatorType.BitwiseOr:
					return FindBinaryOperator(op, BinaryOperatorType.BitwiseOr);
				case OperatorType.ExclusiveOr:
					return FindBinaryOperator(op, BinaryOperatorType.ExclusiveOr);
				case OperatorType.LeftShift:
					return FindBinaryOperator(op, BinaryOperatorType.ShiftLeft);
				case OperatorType.RightShift:
					return FindBinaryOperator(op, BinaryOperatorType.ShiftRight);
				case OperatorType.Equality:
					return FindBinaryOperator(op, BinaryOperatorType.Equality);
				case OperatorType.Inequality:
					return FindBinaryOperator(op, BinaryOperatorType.InEquality);
				case OperatorType.GreaterThan:
					return FindBinaryOperator(op, BinaryOperatorType.GreaterThan);
				case OperatorType.LessThan:
					return FindBinaryOperator(op, BinaryOperatorType.LessThan);
				case OperatorType.GreaterThanOrEqual:
					return FindBinaryOperator(op, BinaryOperatorType.GreaterThanOrEqual);
				case OperatorType.LessThanOrEqual:
					return FindBinaryOperator(op, BinaryOperatorType.LessThanOrEqual);
				case OperatorType.Implicit:
					return FindOperator(op, m => new FindImplicitOperatorNavigator(m));
				case OperatorType.Explicit:
					return FindOperator(op, m => new FindExplicitOperatorNavigator(m));
				default:
					throw new InvalidOperationException("Invalid value for OperatorType");
			}
		}
		
		SearchScope FindOperator(IMethod op, Func<IMethod, FindReferenceNavigator> factory)
		{
			return new SearchScope(
				delegate (ICompilation compilation) {
					IMethod imported = compilation.Import(op);
					return imported != null ? factory(imported) : null;
				});
		}
		
		SearchScope FindUnaryOperator(IMethod op, UnaryOperatorType operatorType)
		{
			return FindOperator(op, m => new FindUnaryOperatorNavigator(m, operatorType));
		}
		
		sealed class FindUnaryOperatorNavigator : FindReferenceNavigator
		{
			readonly IMethod op;
			readonly UnaryOperatorType operatorType;
			
			public FindUnaryOperatorNavigator(IMethod op, UnaryOperatorType operatorType)
			{
				this.op = op;
				this.operatorType = operatorType;
			}
			
			internal override bool CanMatch(AstNode node)
			{
				UnaryOperatorExpression uoe = node as UnaryOperatorExpression;
				if (uoe != null) {
					if (operatorType == UnaryOperatorType.Increment)
						return uoe.Operator == UnaryOperatorType.Increment || uoe.Operator == UnaryOperatorType.PostIncrement;
					else if (operatorType == UnaryOperatorType.Decrement)
						return uoe.Operator == UnaryOperatorType.Decrement || uoe.Operator == UnaryOperatorType.PostDecrement;
					else
						return uoe.Operator == operatorType;
				}
				return node is OperatorDeclaration;
			}
			
			internal override bool IsMatch(ResolveResult rr)
			{
				MemberResolveResult mrr = rr as MemberResolveResult;
				return mrr != null && findReferences.IsMemberMatch(op, mrr.Member, mrr.IsVirtualCall);
			}
		}
		
		SearchScope FindBinaryOperator(IMethod op, BinaryOperatorType operatorType)
		{
			return FindOperator(op, m => new FindBinaryOperatorNavigator(m, operatorType));
		}
		
		sealed class FindBinaryOperatorNavigator : FindReferenceNavigator
		{
			readonly IMethod op;
			readonly BinaryOperatorType operatorType;
			
			public FindBinaryOperatorNavigator(IMethod op, BinaryOperatorType operatorType)
			{
				this.op = op;
				this.operatorType = operatorType;
			}
			
			internal override bool CanMatch(AstNode node)
			{
				BinaryOperatorExpression boe = node as BinaryOperatorExpression;
				if (boe != null) {
					return boe.Operator == operatorType;
				}
				return node is OperatorDeclaration;
			}
			
			internal override bool IsMatch(ResolveResult rr)
			{
				MemberResolveResult mrr = rr as MemberResolveResult;
				return mrr != null && findReferences.IsMemberMatch(op, mrr.Member, mrr.IsVirtualCall);
			}
		}
		
		sealed class FindImplicitOperatorNavigator : FindReferenceNavigator
		{
			readonly IMethod op;
			
			public FindImplicitOperatorNavigator(IMethod op)
			{
				this.op = op;
			}
			
			internal override bool CanMatch(AstNode node)
			{
				return true;
			}
			
			internal override bool IsMatch(ResolveResult rr)
			{
				MemberResolveResult mrr = rr as MemberResolveResult;
				return mrr != null && findReferences.IsMemberMatch(op, mrr.Member, mrr.IsVirtualCall);
			}
			
			public override void ProcessConversion(Expression expression, ResolveResult result, Conversion conversion, IType targetType)
			{
				if (conversion.IsUserDefined && findReferences.IsMemberMatch(op, conversion.Method, conversion.IsVirtualMethodLookup)) {
					ReportMatch(expression, result);
				}
			}
		}
		
		sealed class FindExplicitOperatorNavigator : FindReferenceNavigator
		{
			readonly IMethod op;
			
			public FindExplicitOperatorNavigator(IMethod op)
			{
				this.op = op;
			}
			
			internal override bool CanMatch(AstNode node)
			{
				return node is CastExpression;
			}
			
			internal override bool IsMatch(ResolveResult rr)
			{
				ConversionResolveResult crr = rr as ConversionResolveResult;
				return crr != null && crr.Conversion.IsUserDefined
					&& findReferences.IsMemberMatch(op, crr.Conversion.Method, crr.Conversion.IsVirtualMethodLookup);
			}
		}
		#endregion
		
		#region Find Constructor References
		SearchScope FindObjectCreateReferences(IMethod ctor)
		{
			string searchTerm = null;
			if (KnownTypeReference.GetCSharpNameByTypeCode(ctor.DeclaringTypeDefinition.KnownTypeCode) == null) {
				// not a built-in type
				searchTerm = ctor.DeclaringTypeDefinition.Name;
				if (searchTerm.Length > 9 && searchTerm.EndsWith("Attribute", StringComparison.Ordinal)) {
					// we also need to look for the short form
					searchTerm = null;
				}
			}
			return new SearchScope(
				searchTerm,
				delegate (ICompilation compilation) {
					IMethod imported = compilation.Import(ctor);
					if (imported != null)
						return new FindObjectCreateReferencesNavigator(imported);
					else
						return null;
				});
		}
		
		sealed class FindObjectCreateReferencesNavigator : FindReferenceNavigator
		{
			readonly IMethod ctor;
			
			public FindObjectCreateReferencesNavigator(IMethod ctor)
			{
				this.ctor = ctor;
			}
			
			internal override bool CanMatch(AstNode node)
			{
				return node is ObjectCreateExpression || node is ConstructorDeclaration || node is Attribute;
			}
			
			internal override bool IsMatch(ResolveResult rr)
			{
				MemberResolveResult mrr = rr as MemberResolveResult;
				return mrr != null && findReferences.IsMemberMatch(ctor, mrr.Member, mrr.IsVirtualCall);
			}
		}
		
		SearchScope FindChainedConstructorReferences(IMethod ctor)
		{
			SearchScope searchScope = new SearchScope(
				delegate (ICompilation compilation) {
					IMethod imported = compilation.Import(ctor);
					if (imported != null)
						return new FindChainedConstructorReferencesNavigator(imported);
					else
						return null;
				});
			if (ctor.DeclaringTypeDefinition.IsSealed)
				searchScope.accessibility = Accessibility.Private;
			else
				searchScope.accessibility = Accessibility.Protected;
			searchScope.accessibility = MergeAccessibility(GetEffectiveAccessibility(ctor), searchScope.accessibility);
			return searchScope;
		}
		
		sealed class FindChainedConstructorReferencesNavigator : FindReferenceNavigator
		{
			readonly IMethod ctor;
			
			public FindChainedConstructorReferencesNavigator(IMethod ctor)
			{
				this.ctor = ctor;
			}
			
			internal override bool CanMatch(AstNode node)
			{
				return node is ConstructorInitializer;
			}
			
			internal override bool IsMatch(ResolveResult rr)
			{
				MemberResolveResult mrr = rr as MemberResolveResult;
				return mrr != null && findReferences.IsMemberMatch(ctor, mrr.Member, mrr.IsVirtualCall);
			}
		}
		#endregion
		
		#region Find Destructor References
		SearchScope GetSearchScopeForDestructor(IMethod dtor)
		{
			var scope = new SearchScope (
				delegate (ICompilation compilation) {
					IMethod imported = compilation.Import(dtor);
					if (imported != null) {
						return new FindDestructorReferencesNavigator (imported);
					} else {
						return null;
					}
				});
			scope.accessibility = Accessibility.Private;
			return scope;
		}
		
		sealed class FindDestructorReferencesNavigator : FindReferenceNavigator
		{
			readonly IMethod dtor;
			
			public FindDestructorReferencesNavigator (IMethod dtor)
			{
				this.dtor = dtor;
			}
			
			internal override bool CanMatch(AstNode node)
			{
				return node is DestructorDeclaration;
			}
			
			internal override bool IsMatch(ResolveResult rr)
			{
				MemberResolveResult mrr = rr as MemberResolveResult;
				return mrr != null && findReferences.IsMemberMatch(dtor, mrr.Member, mrr.IsVirtualCall);
			}
		}
		#endregion

		#region Find Local Variable References
		/// <summary>
		/// Finds all references of a given variable.
		/// </summary>
		/// <param name="variable">The variable for which to look.</param>
		/// <param name="unresolvedFile">The type system representation of the file being searched.</param>
		/// <param name="syntaxTree">The syntax tree of the file being searched.</param>
		/// <param name="compilation">The compilation.</param>
		/// <param name="callback">Callback used to report the references that were found.</param>
		/// <param name="cancellationToken">Cancellation token that may be used to cancel the operation.</param>
		public void FindLocalReferences(IVariable variable, CSharpUnresolvedFile unresolvedFile, SyntaxTree syntaxTree,
		                                ICompilation compilation, FoundReferenceCallback callback, CancellationToken cancellationToken)
		{
			if (variable == null)
				throw new ArgumentNullException("variable");
			var searchScope = new SearchScope(c => new FindLocalReferencesNavigator(variable));
			searchScope.declarationCompilation = compilation;
			FindReferencesInFile(searchScope, unresolvedFile, syntaxTree, compilation, callback, cancellationToken);
		}
		
		SearchScope GetSearchScopeForLocalVariable(IVariable variable)
		{
			var scope = new SearchScope (
				delegate {
					return new FindLocalReferencesNavigator(variable);
				}
			);
			scope.fileName = variable.Region.FileName;
			return scope;
		}
		
		class FindLocalReferencesNavigator : FindReferenceNavigator
		{
			readonly IVariable variable;
			
			public FindLocalReferencesNavigator(IVariable variable)
			{
				this.variable = variable;
			}
			
			internal override bool CanMatch(AstNode node)
			{
				var expr = node as IdentifierExpression;
				if (expr != null)
					return expr.TypeArguments.Count == 0 && variable.Name == expr.Identifier;
				var vi = node as VariableInitializer;
				if (vi != null)
					return vi.Name == variable.Name;
				var pd = node as ParameterDeclaration;
				if (pd != null)
					return pd.Name == variable.Name;
				var id = node as Identifier;
				if (id != null)
					return id.Name == variable.Name;
				return false;
			}
			
			internal override bool IsMatch(ResolveResult rr)
			{
				var lrr = rr as LocalResolveResult;
				return lrr != null && lrr.Variable.Name == variable.Name && lrr.Variable.Region == variable.Region;
			}
		}
		#endregion

		#region Find Type Parameter References
		/// <summary>
		/// Finds all references of a given type parameter.
		/// </summary>
		/// <param name="typeParameter">The type parameter for which to look.</param>
		/// <param name="unresolvedFile">The type system representation of the file being searched.</param>
		/// <param name="syntaxTree">The syntax tree of the file being searched.</param>
		/// <param name="compilation">The compilation.</param>
		/// <param name="callback">Callback used to report the references that were found.</param>
		/// <param name="cancellationToken">Cancellation token that may be used to cancel the operation.</param>
		[Obsolete("Use GetSearchScopes(typeParameter) instead")]
		public void FindTypeParameterReferences(IType typeParameter, CSharpUnresolvedFile unresolvedFile, SyntaxTree syntaxTree,
		                                        ICompilation compilation, FoundReferenceCallback callback, CancellationToken cancellationToken)
		{
			if (typeParameter == null)
				throw new ArgumentNullException("typeParameter");
			if (typeParameter.Kind != TypeKind.TypeParameter)
				throw new ArgumentOutOfRangeException("typeParameter", "Only type parameters are allowed");
			var searchScope = new SearchScope(c => new FindTypeParameterReferencesNavigator((ITypeParameter)typeParameter));
			searchScope.declarationCompilation = compilation;
			searchScope.accessibility = Accessibility.Private;
			FindReferencesInFile(searchScope, unresolvedFile, syntaxTree, compilation, callback, cancellationToken);
		}
		
		SearchScope GetSearchScopeForTypeParameter(ITypeParameter tp)
		{
			var searchScope = new SearchScope(c => new FindTypeParameterReferencesNavigator(tp));
			var compilationProvider = tp as ICompilationProvider;
			if (compilationProvider != null)
				searchScope.declarationCompilation = compilationProvider.Compilation;
			searchScope.topLevelTypeDefinition = GetTopLevelTypeDefinition(tp.Owner);
			searchScope.accessibility = Accessibility.Private;
			return searchScope;
		}

		class FindTypeParameterReferencesNavigator : FindReferenceNavigator
		{
			readonly ITypeParameter typeParameter;
			
			public FindTypeParameterReferencesNavigator(ITypeParameter typeParameter)
			{
				this.typeParameter = typeParameter;
			}
			
			internal override bool CanMatch(AstNode node)
			{
				var type = node as SimpleType;
				if (type != null)
					return type.Identifier == typeParameter.Name;
				var declaration = node as TypeParameterDeclaration;
				if (declaration != null)
					return declaration.Name == typeParameter.Name;
				return false;
			}
			
			internal override bool IsMatch(ResolveResult rr)
			{
				var lrr = rr as TypeResolveResult;
				return lrr != null && lrr.Type.Kind == TypeKind.TypeParameter && ((ITypeParameter)lrr.Type).Region == typeParameter.Region;
			}
		}
		#endregion
		
		#region Find Namespace References
		SearchScope GetSearchScopeForNamespace(INamespace ns)
		{
			var scope = new SearchScope (
				delegate (ICompilation compilation) {
					return new FindNamespaceNavigator (ns);
				}
			);
			return scope;
		}

		sealed class FindNamespaceNavigator : FindReferenceNavigator
		{
			readonly INamespace ns;

			public FindNamespaceNavigator (INamespace ns)
			{
				this.ns = ns;
			}

			internal override bool CanMatch(AstNode node)
			{
				var nsd = node as NamespaceDeclaration;
				if (nsd != null && nsd.FullName.StartsWith(ns.FullName, StringComparison.Ordinal))
					return true;

				var ud = node as UsingDeclaration;
				if (ud != null && ud.Namespace == ns.FullName)
					return true;

				var st = node as SimpleType;
				if (st != null && st.Identifier == ns.Name)
					return !st.AncestorsAndSelf.TakeWhile (n => n is AstType).Any (m => m.Role == NamespaceDeclaration.NamespaceNameRole);

				var mt = node as MemberType;
				if (mt != null && mt.MemberName == ns.Name)
					return !mt.AncestorsAndSelf.TakeWhile (n => n is AstType).Any (m => m.Role == NamespaceDeclaration.NamespaceNameRole);

				var identifer = node as IdentifierExpression;
				if (identifer != null && identifer.Identifier == ns.Name)
					return true;

				var mrr = node as MemberReferenceExpression;
				if (mrr != null && mrr.MemberName == ns.Name)
					return true;


				return false;
			}

			internal override bool IsMatch(ResolveResult rr)
			{
				var nsrr = rr as NamespaceResolveResult;
				return nsrr != null && nsrr.NamespaceName.StartsWith(ns.FullName, StringComparison.Ordinal);
			}
		}
		#endregion
		
		#region Find Parameter References

		SearchScope GetSearchScopeForParameter(IParameter parameter)
		{
			var scope = new SearchScope (
				delegate {
					return new FindParameterReferencesNavigator (parameter);
				}
			);
			if (parameter.Owner == null) {
				scope.fileName = parameter.Region.FileName;
			}
			return scope;
		}

		class FindParameterReferencesNavigator : FindReferenceNavigator
		{
			readonly IParameter parameter;

			public FindParameterReferencesNavigator(IParameter parameter)
			{
				this.parameter = parameter;
			}

			internal override bool CanMatch(AstNode node)
			{
				var expr = node as IdentifierExpression;
				if (expr != null)
					return expr.TypeArguments.Count == 0 && parameter.Name == expr.Identifier;
				var vi = node as VariableInitializer;
				if (vi != null)
					return vi.Name == parameter.Name;
				var pd = node as ParameterDeclaration;
				if (pd != null)
					return pd.Name == parameter.Name;
				var id = node as Identifier;
				if (id != null)
					return id.Name == parameter.Name;
				var nae = node as NamedArgumentExpression;
				if (nae != null)
					return nae.Name == parameter.Name;
				return false;
			}

			internal override bool IsMatch(ResolveResult rr)
			{
				var lrr = rr as LocalResolveResult;
				if (lrr != null)
					return lrr.Variable.Name == parameter.Name && lrr.Variable.Region == parameter.Region;

				var nar = rr as NamedArgumentResolveResult;
				return nar != null && nar.Parameter.Name == parameter.Name && nar.Parameter.Region == parameter.Region;
			}
		}
		#endregion
	}
}
