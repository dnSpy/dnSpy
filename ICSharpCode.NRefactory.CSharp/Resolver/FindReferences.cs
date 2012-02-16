// Copyright (c) AlphaSierraPapa for the SharpDevelop Team
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
		/// Gets/Sets whether to find type references even if an alias is being used.
		/// </summary>
		public bool FindTypeReferencesEvenIfAliased { get; set; }
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
			internal ICompilation declarationCompilation;
			internal Accessibility accessibility;
			internal ITypeDefinition topLevelTypeDefinition;
			
			IResolveVisitorNavigator IFindReferenceSearchScope.GetNavigator(ICompilation compilation, FoundReferenceCallback callback)
			{
				FindReferenceNavigator n = factory(compilation);
				if (n != null) {
					n.callback = callback;
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
		}
		
		abstract class FindReferenceNavigator : IResolveVisitorNavigator
		{
			internal FoundReferenceCallback callback;
			
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
		public IList<IFindReferenceSearchScope> GetSearchScopes(IEntity entity)
		{
			if (entity == null)
				throw new ArgumentNullException("entity");
			Accessibility effectiveAccessibility = GetEffectiveAccessibility(entity);
			ITypeDefinition topLevelTypeDefinition = entity.DeclaringTypeDefinition;
			while (topLevelTypeDefinition != null && topLevelTypeDefinition.DeclaringTypeDefinition != null)
				topLevelTypeDefinition = topLevelTypeDefinition.DeclaringTypeDefinition;
			SearchScope scope;
			SearchScope additionalScope = null;
			switch (entity.EntityType) {
				case EntityType.TypeDefinition:
					scope = FindTypeDefinitionReferences((ITypeDefinition)entity, this.FindTypeReferencesEvenIfAliased, out additionalScope);
					break;
				case EntityType.Field:
					if (entity.DeclaringTypeDefinition != null && entity.DeclaringTypeDefinition.Kind == TypeKind.Enum)
						scope = FindMemberReferences(entity, m => new FindEnumMemberReferences((IField)m));
					else
						scope = FindMemberReferences(entity, m => new FindFieldReferences((IField)m));
					break;
				case EntityType.Property:
					scope = FindMemberReferences(entity, m => new FindPropertyReferences((IProperty)m));
					break;
				case EntityType.Event:
					scope = FindMemberReferences(entity, m => new FindEventReferences((IEvent)m));
					break;
				case EntityType.Method:
					scope = GetSearchScopeForMethod((IMethod)entity);
					break;
				case EntityType.Indexer:
					scope = FindIndexerReferences((IProperty)entity);
					break;
				case EntityType.Operator:
					scope = GetSearchScopeForOperator((IMethod)entity);
					break;
				case EntityType.Constructor:
					IMethod ctor = (IMethod)((IMethod)entity).MemberDefinition;
					scope = FindObjectCreateReferences(ctor);
					additionalScope = FindChainedConstructorReferences(ctor);
					break;
				case EntityType.Destructor:
					return EmptyList<IFindReferenceSearchScope>.Instance;
				default:
					throw new ArgumentException("Unknown entity type " + entity.EntityType);
			}
			if (scope.accessibility == Accessibility.None)
				scope.accessibility = effectiveAccessibility;
			scope.declarationCompilation = entity.Compilation;
			scope.topLevelTypeDefinition = topLevelTypeDefinition;
			if (additionalScope != null) {
				if (additionalScope.accessibility == Accessibility.None)
					additionalScope.accessibility = effectiveAccessibility;
				additionalScope.declarationCompilation = entity.Compilation;
				additionalScope.topLevelTypeDefinition = topLevelTypeDefinition;
				return new[] { scope, additionalScope };
			} else {
				return new[] { scope };
			}
		}
		#endregion
		
		#region GetInterestingFileNames
		/// <summary>
		/// Gets the file names that possibly contain references to the element being searched for.
		/// </summary>
		public IEnumerable<CSharpParsedFile> GetInterestingFiles(IFindReferenceSearchScope searchScope, ICompilation compilation)
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
					return EmptyList<CSharpParsedFile>.Instance;
				}
				switch (searchScope.Accessibility) {
					case Accessibility.None:
					case Accessibility.Private:
						if (topLevelTypeDef.ParentAssembly == compilation.MainAssembly)
							return topLevelTypeDef.Parts.Select(p => p.ParsedFile).OfType<CSharpParsedFile>().Distinct();
						else
							return EmptyList<CSharpParsedFile>.Instance;
					case Accessibility.Protected:
						return GetInterestingFilesProtected(topLevelTypeDef);
					case Accessibility.Internal:
						if (topLevelTypeDef.ParentAssembly.InternalsVisibleTo(compilation.MainAssembly))
							return pc.Files.OfType<CSharpParsedFile>();
						else
							return EmptyList<CSharpParsedFile>.Instance;
					case Accessibility.ProtectedAndInternal:
						if (topLevelTypeDef.ParentAssembly.InternalsVisibleTo(compilation.MainAssembly))
							return GetInterestingFilesProtected(topLevelTypeDef);
						else
							return EmptyList<CSharpParsedFile>.Instance;
					case Accessibility.ProtectedOrInternal:
						if (topLevelTypeDef.ParentAssembly.InternalsVisibleTo(compilation.MainAssembly))
							return pc.Files.OfType<CSharpParsedFile>();
						else
							return GetInterestingFilesProtected(topLevelTypeDef);
					default:
						return pc.Files.OfType<CSharpParsedFile>();
				}
			} else {
				return pc.Files.OfType<CSharpParsedFile>();
			}
		}
		
		IEnumerable<CSharpParsedFile> GetInterestingFilesProtected(ITypeDefinition referencedTypeDefinition)
		{
			return (from typeDef in referencedTypeDefinition.Compilation.MainAssembly.GetAllTypeDefinitions()
			        where typeDef.IsDerivedFrom(referencedTypeDefinition)
			        from part in typeDef.Parts
			        select part.ParsedFile
			       ).OfType<CSharpParsedFile>().Distinct();
		}
		#endregion
		
		#region FindReferencesInFile
		/// <summary>
		/// Finds all references in the given file.
		/// </summary>
		/// <param name="searchScope">The search scope for which to look.</param>
		/// <param name="parsedFile">The type system representation of the file being searched.</param>
		/// <param name="compilationUnit">The compilation unit of the file being searched.</param>
		/// <param name="compilation">The compilation for the project that contains the file.</param>
		/// <param name="callback">Callback used to report the references that were found.</param>
		/// <param name="cancellationToken">CancellationToken that may be used to cancel the operation.</param>
		public void FindReferencesInFile(IFindReferenceSearchScope searchScope, CSharpParsedFile parsedFile, CompilationUnit compilationUnit,
		                                 ICompilation compilation, FoundReferenceCallback callback, CancellationToken cancellationToken)
		{
			if (searchScope == null)
				throw new ArgumentNullException("searchScope");
			FindReferencesInFile(new[] { searchScope }, parsedFile, compilationUnit, compilation, callback, cancellationToken);
		}
		
		/// <summary>
		/// Finds all references in the given file.
		/// </summary>
		/// <param name="searchScopes">The search scopes for which to look.</param>
		/// <param name="parsedFile">The type system representation of the file being searched.</param>
		/// <param name="compilationUnit">The compilation unit of the file being searched.</param>
		/// <param name="compilation">The compilation for the project that contains the file.</param>
		/// <param name="callback">Callback used to report the references that were found.</param>
		/// <param name="cancellationToken">CancellationToken that may be used to cancel the operation.</param>
		public void FindReferencesInFile(IList<IFindReferenceSearchScope> searchScopes, CSharpParsedFile parsedFile, CompilationUnit compilationUnit,
		                                 ICompilation compilation, FoundReferenceCallback callback, CancellationToken cancellationToken)
		{
			if (searchScopes == null)
				throw new ArgumentNullException("searchScopes");
			if (parsedFile == null)
				throw new ArgumentNullException("parsedFile");
			if (compilationUnit == null)
				throw new ArgumentNullException("compilationUnit");
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
			combinedNavigator = new DetectSkippableNodesNavigator(combinedNavigator, compilationUnit);
			cancellationToken.ThrowIfCancellationRequested();
			CSharpAstResolver resolver = new CSharpAstResolver(compilation, compilationUnit, parsedFile);
			resolver.ApplyNavigator(combinedNavigator, cancellationToken);
			foreach (var n in navigators) {
				var frn = n as FindReferenceNavigator;
				if (frn != null)
					frn.NavigatorDone(resolver, cancellationToken);
			}
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
					additionalScope = new SearchScope(
						shortForm,
						delegate (ICompilation compilation) {
							ITypeDefinition imported = compilation.Import(typeDefinition);
							if (imported != null)
								return new FindTypeDefinitionReferencesNavigator(imported, shortForm);
							else
								return null;
						});
				}
			}
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
			IMember memberDefinition = ((IMember)member).MemberDefinition;
			return new SearchScope(
				searchTerm,
				delegate(ICompilation compilation) {
					IMember imported = compilation.Import(memberDefinition);
					return imported != null ? factory(imported) : null;
				});
		}
		
		class FindMemberReferencesNavigator : FindReferenceNavigator
		{
			readonly IMember member;
			readonly string searchTerm;
			
			public FindMemberReferencesNavigator(IMember member)
			{
				this.member = member.MemberDefinition;
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
					return ne.Identifier == searchTerm;
				
				return false;
			}
			
			internal override bool IsMatch(ResolveResult rr)
			{
				MemberResolveResult mrr = rr as MemberResolveResult;
				return mrr != null && member == mrr.Member.MemberDefinition;
			}
		}
		
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
		
		#region Find Method References
		SearchScope GetSearchScopeForMethod(IMethod method)
		{
			method = (IMethod)method.MemberDefinition;
			
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
				} else if (expr.Role != InvocationExpression.Roles.TargetExpression) {
					// MemberReferences & Identifiers that aren't used in an invocation can still match the method
					// as delegate name.
					if (expr.GetChildByRole(AstNode.Roles.Identifier).Name == method.Name)
						potentialMethodGroupConversions.Add(expr);
				}
				return node is MethodDeclaration;
			}
			
			internal override bool IsMatch(ResolveResult rr)
			{
				var mrr = rr as MemberResolveResult;
				return mrr != null && method == mrr.Member.MemberDefinition;
			}
			
			internal override void NavigatorDone(CSharpAstResolver resolver, CancellationToken cancellationToken)
			{
				foreach (var expr in potentialMethodGroupConversions) {
					var conversion = resolver.GetConversion(expr, cancellationToken);
					if (conversion.IsMethodGroupConversion && conversion.Method.MemberDefinition == method) {
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
			indexer = (IProperty)indexer.MemberDefinition;
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
				return mrr != null && indexer == mrr.Member.MemberDefinition;
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
			op = (IMethod)op.MemberDefinition;
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
				return mrr != null && op == mrr.Member.MemberDefinition;
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
				return mrr != null && op == mrr.Member.MemberDefinition;
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
				return mrr != null && op == mrr.Member.MemberDefinition;
			}
			
			public override void ProcessConversion(Expression expression, ResolveResult result, Conversion conversion, IType targetType)
			{
				if (conversion.IsUserDefined && conversion.Method.MemberDefinition == op) {
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
				return crr != null && crr.Conversion.IsUserDefined && crr.Conversion.Method.MemberDefinition == op;
			}
		}
		#endregion
		
		#region Find Constructor References
		SearchScope FindObjectCreateReferences(IMethod ctor)
		{
			ctor = (IMethod)ctor.MemberDefinition;
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
				return mrr != null && ctor == mrr.Member.MemberDefinition;
			}
		}
		
		SearchScope FindChainedConstructorReferences(IMethod ctor)
		{
			ctor = (IMethod)ctor.MemberDefinition;
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
				return mrr != null && ctor == mrr.Member.MemberDefinition;
			}
		}
		#endregion
		
		#region Find Local Variable References
		/// <summary>
		/// Finds all references of a given variable.
		/// </summary>
		/// <param name="variable">The variable for which to look.</param>
		/// <param name="parsedFile">The type system representation of the file being searched.</param>
		/// <param name="compilationUnit">The compilation unit of the file being searched.</param>
		/// <param name="compilation">The compilation.</param>
		/// <param name="callback">Callback used to report the references that were found.</param>
		/// <param name="cancellationToken">Cancellation token that may be used to cancel the operation.</param>
		public void FindLocalReferences(IVariable variable, CSharpParsedFile parsedFile, CompilationUnit compilationUnit,
		                                ICompilation compilation, FoundReferenceCallback callback, CancellationToken cancellationToken)
		{
			if (variable == null)
				throw new ArgumentNullException("variable");
			var searchScope = new SearchScope(c => new FindLocalReferencesNavigator(variable));
			searchScope.declarationCompilation = compilation;
			FindReferencesInFile(searchScope, parsedFile, compilationUnit, compilation, callback, cancellationToken);
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
	}
}
