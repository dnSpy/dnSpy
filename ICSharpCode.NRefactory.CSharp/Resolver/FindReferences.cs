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
		/// Gets/Sets the cancellation token.
		/// </summary>
		public CancellationToken CancellationToken { get; set; }
		
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
		abstract class SearchScope : IResolveVisitorNavigator, IFindReferenceSearchScope
		{
			protected string searchTerm;
			internal Accessibility accessibility;
			internal ITypeDefinition topLevelTypeDefinition;
			
			FoundReferenceCallback callback;
			
			IResolveVisitorNavigator IFindReferenceSearchScope.GetNavigator(FoundReferenceCallback callback)
			{
				SearchScope n = (SearchScope)MemberwiseClone();
				n.callback = callback;
				return n;
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
			
			void IResolveVisitorNavigator.ProcessConversion(Expression expression, ResolveResult result, Conversion conversion, IType targetType)
			{
				ProcessConversion(expression, result, conversion, targetType);
			}
			
			internal virtual void ProcessConversion(Expression expression, ResolveResult result, Conversion conversion, IType targetType)
			{
			}
			
			protected void ReportMatch(AstNode node, ResolveResult result)
			{
				if (callback != null)
					callback(node, result);
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
					scope = new FindTypeDefinitionReferences((ITypeDefinition)entity, this.FindTypeReferencesEvenIfAliased);
					break;
				case EntityType.Field:
					if (entity.DeclaringTypeDefinition != null && entity.DeclaringTypeDefinition.Kind == TypeKind.Enum)
						scope = new FindEnumMemberReferences((IField)entity);
					else
						scope = new FindFieldReferences((IField)entity);
					break;
				case EntityType.Property:
					scope = new FindPropertyReferences((IProperty)entity);
					break;
				case EntityType.Event:
					scope = new FindEventReferences((IEvent)entity);
					break;
				case EntityType.Method:
					scope = GetSearchScopeForMethod((IMethod)entity);
					break;
				case EntityType.Indexer:
					scope = new FindIndexerReferences((IProperty)entity);
					break;
				case EntityType.Operator:
					scope = GetSearchScopeForOperator((IMethod)entity);
					break;
				case EntityType.Constructor:
					IMethod ctor = (IMethod)entity;
					scope = new FindObjectCreateReferences(ctor);
					additionalScope = new FindChainedConstructorReferences(ctor);
					break;
				case EntityType.Destructor:
					return EmptyList<IFindReferenceSearchScope>.Instance;
				default:
					throw new ArgumentException("Unknown entity type " + entity.EntityType);
			}
			if (scope.accessibility == Accessibility.None)
				scope.accessibility = effectiveAccessibility;
			scope.topLevelTypeDefinition = topLevelTypeDefinition;
			if (additionalScope != null) {
				if (additionalScope.accessibility == Accessibility.None)
					additionalScope.accessibility = effectiveAccessibility;
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
		public IList<string> GetInterestingFileNames(IFindReferenceSearchScope searchScope, IEnumerable<ITypeDefinition> allTypes, ITypeResolveContext context)
		{
			IEnumerable<ITypeDefinition> interestingTypes;
			if (searchScope.TopLevelTypeDefinition != null) {
				switch (searchScope.Accessibility) {
					case Accessibility.None:
					case Accessibility.Private:
						interestingTypes = new [] { searchScope.TopLevelTypeDefinition.GetDefinition() };
						break;
					case Accessibility.Protected:
						interestingTypes = GetInterestingTypesProtected(allTypes, context, searchScope.TopLevelTypeDefinition);
						break;
					case Accessibility.Internal:
						interestingTypes = GetInterestingTypesInternal(allTypes, context, searchScope.TopLevelTypeDefinition.ProjectContent);
						break;
					case Accessibility.ProtectedAndInternal:
						interestingTypes = GetInterestingTypesProtected(allTypes, context, searchScope.TopLevelTypeDefinition)
							.Intersect(GetInterestingTypesInternal(allTypes, context, searchScope.TopLevelTypeDefinition.ProjectContent));
						break;
					case Accessibility.ProtectedOrInternal:
						interestingTypes = GetInterestingTypesProtected(allTypes, context, searchScope.TopLevelTypeDefinition)
							.Union(GetInterestingTypesInternal(allTypes, context, searchScope.TopLevelTypeDefinition.ProjectContent));
						break;
					default:
						interestingTypes = allTypes;
						break;
				}
			} else {
				interestingTypes = allTypes;
			}
			return (from typeDef in interestingTypes
			        from part in typeDef.GetParts()
			        where part.ParsedFile != null
			        select part.ParsedFile.FileName
			       ).Distinct(Platform.FileNameComparer).ToList();
		}
		
		IEnumerable<ITypeDefinition> GetInterestingTypesProtected(IEnumerable<ITypeDefinition> allTypes, ITypeResolveContext context, ITypeDefinition referencedTypeDefinition)
		{
			return allTypes.Where(t => t.IsDerivedFrom(referencedTypeDefinition, context));
		}
		
		IEnumerable<ITypeDefinition> GetInterestingTypesInternal(IEnumerable<ITypeDefinition> allTypes, ITypeResolveContext context, IProjectContent referencedProjectContent)
		{
			return allTypes.Where(t => referencedProjectContent.InternalsVisibleTo(t.ProjectContent, context));
		}
		#endregion
		
		#region FindReferencesInFile
		/// <summary>
		/// Finds all references in the given file.
		/// </summary>
		/// <param name="searchScope">The search scope for which to look.</param>
		/// <param name="parsedFile">The type system representation of the file being searched.</param>
		/// <param name="compilationUnit">The compilation unit of the file being searched.</param>
		/// <param name="context">The type resolve context to use for resolving the file.</param>
		/// <param name="callback">Callback used to report the references that were found.</param>
		public void FindReferencesInFile(IFindReferenceSearchScope searchScope, CSharpParsedFile parsedFile, CompilationUnit compilationUnit,
		                                 ITypeResolveContext context, FoundReferenceCallback callback)
		{
			if (searchScope == null)
				throw new ArgumentNullException("searchScope");
			FindReferencesInFile(new[] { searchScope }, parsedFile, compilationUnit, context, callback);
		}
		
		/// <summary>
		/// Finds all references in the given file.
		/// </summary>
		/// <param name="searchScopes">The search scopes for which to look.</param>
		/// <param name="parsedFile">The type system representation of the file being searched.</param>
		/// <param name="compilationUnit">The compilation unit of the file being searched.</param>
		/// <param name="context">The type resolve context to use for resolving the file.</param>
		/// <param name="callback">Callback used to report the references that were found.</param>
		public void FindReferencesInFile(IList<IFindReferenceSearchScope> searchScopes, CSharpParsedFile parsedFile, CompilationUnit compilationUnit,
		                                 ITypeResolveContext context, FoundReferenceCallback callback)
		{
			if (searchScopes == null)
				throw new ArgumentNullException("searchScopes");
			if (parsedFile == null)
				throw new ArgumentNullException("parsedFile");
			if (compilationUnit == null)
				throw new ArgumentNullException("compilationUnit");
			if (context == null)
				throw new ArgumentNullException("context");
			this.CancellationToken.ThrowIfCancellationRequested();
			if (searchScopes.Count == 0)
				return;
			using (var ctx = context.Synchronize()) {
				IResolveVisitorNavigator navigator;
				if (searchScopes.Count == 1)
					navigator = searchScopes[0].GetNavigator(callback);
				else
					navigator = new CompositeResolveVisitorNavigator(searchScopes.Select(s => s.GetNavigator(callback)).ToArray());
				navigator = new DetectSkippableNodesNavigator(navigator, compilationUnit);
				CSharpResolver resolver = new CSharpResolver(ctx, this.CancellationToken);
				ResolveVisitor v = new ResolveVisitor(resolver, parsedFile, navigator);
				v.Scan(compilationUnit);
			}
		}
		#endregion
		
		#region Find TypeDefinition References
		sealed class FindTypeDefinitionReferences : SearchScope
		{
			ITypeDefinition typeDefinition;
			
			public FindTypeDefinitionReferences(ITypeDefinition typeDefinition, bool findTypeReferencesEvenIfAliased)
			{
				this.typeDefinition = typeDefinition;
				if (!findTypeReferencesEvenIfAliased && ReflectionHelper.GetTypeCode(typeDefinition) == TypeCode.Empty) {
					// not a built-in type
					this.searchTerm = typeDefinition.Name;
				}
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
				
				return node is TypeDeclaration;
			}
			
			internal override bool IsMatch(ResolveResult rr)
			{
				TypeResolveResult trr = rr as TypeResolveResult;
				return trr != null && typeDefinition.Equals(trr.Type.GetDefinition());
			}
		}
		#endregion
		
		#region Find Member References
		class FindMemberReferences : SearchScope
		{
			readonly IMember member;
			
			public FindMemberReferences(IMember member)
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
		
		sealed class FindFieldReferences : FindMemberReferences
		{
			public FindFieldReferences(IField field) : base(field)
			{
			}
			
			internal override bool CanMatch(AstNode node)
			{
				return node is FieldDeclaration || node is VariableInitializer || base.CanMatch(node);
			}
		}
		
		sealed class FindEnumMemberReferences : FindMemberReferences
		{
			public FindEnumMemberReferences(IField field) : base(field)
			{
			}
			
			internal override bool CanMatch(AstNode node)
			{
				return node is EnumMemberDeclaration || base.CanMatch(node);
			}
		}
		
		sealed class FindPropertyReferences : FindMemberReferences
		{
			public FindPropertyReferences(IProperty property) : base(property)
			{
			}
			
			internal override bool CanMatch(AstNode node)
			{
				return node is PropertyDeclaration || base.CanMatch(node);
			}
		}
		
		sealed class FindEventReferences : FindMemberReferences
		{
			public FindEventReferences(IEvent ev) : base(ev)
			{
			}
			
			internal override bool CanMatch(AstNode node)
			{
				return node is EventDeclaration || base.CanMatch(node);
			}
		}
		#endregion
		
		#region Find Method References
		SearchScope GetSearchScopeForMethod(IMethod method)
		{
			switch (method.Name) {
				case "Add":
					return new FindMethodReferences(method, typeof(ArrayInitializerExpression));
				case "Where":
					return new FindMethodReferences(method, typeof(QueryWhereClause));
				case "Select":
					return new FindMethodReferences(method, typeof(QuerySelectClause));
				case "SelectMany":
					return new FindMethodReferences(method, typeof(QueryFromClause));
				case "Join":
				case "GroupJoin":
					return new FindMethodReferences(method, typeof(QueryJoinClause));
				case "OrderBy":
				case "OrderByDescending":
				case "ThenBy":
				case "ThenByDescending":
					return new FindMethodReferences(method, typeof(QueryOrdering));
				case "GroupBy":
					return new FindMethodReferences(method, typeof(QueryGroupClause));
				default:
					return new FindMethodReferences(method);
			}
		}
		
		sealed class FindMethodReferences : SearchScope
		{
			readonly IMethod method;
			readonly Type specialNodeType;
			
			public FindMethodReferences(IMethod method, Type specialNodeType = null)
			{
				this.method = (IMethod)method.MemberDefinition;
				this.specialNodeType = specialNodeType;
				if (specialNodeType == null)
					this.searchTerm = method.Name;
			}
			
			internal override bool CanMatch(AstNode node)
			{
				InvocationExpression ie = node as InvocationExpression;
				if (ie != null) {
					Expression target = ResolveVisitor.UnpackParenthesizedExpression(ie.Target);
					
					IdentifierExpression ident = target as IdentifierExpression;
					if (ident != null)
						return ident.Identifier == method.Name;
					
					MemberReferenceExpression mre = target as MemberReferenceExpression;
					if (mre != null)
						return mre.MemberName == method.Name;
					
					PointerReferenceExpression pre = target as PointerReferenceExpression;
					if (pre != null)
						return pre.MemberName == method.Name;
				}
				if (node is MethodDeclaration)
					return true;
				if (specialNodeType != null)
					return specialNodeType.IsInstanceOfType(node);
				else
					return false;
			}
			
			internal override bool IsMatch(ResolveResult rr)
			{
				MemberResolveResult mrr = rr as MemberResolveResult;
				return mrr != null && method == mrr.Member.MemberDefinition;
			}
		}
		#endregion
		
		#region Find Indexer References
		sealed class FindIndexerReferences : SearchScope
		{
			readonly IProperty indexer;
			
			public FindIndexerReferences(IProperty indexer)
			{
				this.indexer = (IProperty)indexer.MemberDefinition;
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
				return new FindMethodReferences(op);
			switch (opType.Value) {
				case OperatorType.LogicalNot:
					return new FindUnaryOperator(op, UnaryOperatorType.Not);
				case OperatorType.OnesComplement:
					return new FindUnaryOperator(op, UnaryOperatorType.BitNot);
				case OperatorType.UnaryPlus:
					return new FindUnaryOperator(op, UnaryOperatorType.Plus);
				case OperatorType.UnaryNegation:
					return new FindUnaryOperator(op, UnaryOperatorType.Minus);
				case OperatorType.Increment:
					return new FindUnaryOperator(op, UnaryOperatorType.Increment);
				case OperatorType.Decrement:
					return new FindUnaryOperator(op, UnaryOperatorType.Decrement);
				case OperatorType.True:
				case OperatorType.False:
					// TODO: implement search for op_True/op_False correctly
					return new FindMethodReferences(op);
				case OperatorType.Addition:
					return new FindBinaryOperator(op, BinaryOperatorType.Add);
				case OperatorType.Subtraction:
					return new FindBinaryOperator(op, BinaryOperatorType.Subtract);
				case OperatorType.Multiply:
					return new FindBinaryOperator(op, BinaryOperatorType.Multiply);
				case OperatorType.Division:
					return new FindBinaryOperator(op, BinaryOperatorType.Divide);
				case OperatorType.Modulus:
					return new FindBinaryOperator(op, BinaryOperatorType.Modulus);
				case OperatorType.BitwiseAnd:
					// TODO: an overloaded bitwise operator can also be called using the corresponding logical operator
					// (if op_True/op_False is defined)
					return new FindBinaryOperator(op, BinaryOperatorType.BitwiseAnd);
				case OperatorType.BitwiseOr:
					return new FindBinaryOperator(op, BinaryOperatorType.BitwiseOr);
				case OperatorType.ExclusiveOr:
					return new FindBinaryOperator(op, BinaryOperatorType.ExclusiveOr);
				case OperatorType.LeftShift:
					return new FindBinaryOperator(op, BinaryOperatorType.ShiftLeft);
				case OperatorType.RightShift:
					return new FindBinaryOperator(op, BinaryOperatorType.ShiftRight);
				case OperatorType.Equality:
					return new FindBinaryOperator(op, BinaryOperatorType.Equality);
				case OperatorType.Inequality:
					return new FindBinaryOperator(op, BinaryOperatorType.InEquality);
				case OperatorType.GreaterThan:
					return new FindBinaryOperator(op, BinaryOperatorType.GreaterThan);
				case OperatorType.LessThan:
					return new FindBinaryOperator(op, BinaryOperatorType.LessThan);
				case OperatorType.GreaterThanOrEqual:
					return new FindBinaryOperator(op, BinaryOperatorType.GreaterThanOrEqual);
				case OperatorType.LessThanOrEqual:
					return new FindBinaryOperator(op, BinaryOperatorType.LessThanOrEqual);
				case OperatorType.Implicit:
					return new FindImplicitOperator(op);
				case OperatorType.Explicit:
					return new FindExplicitOperator(op);
				default:
					throw new InvalidOperationException("Invalid value for OperatorType");
			}
		}
		
		sealed class FindUnaryOperator : SearchScope
		{
			readonly IMethod op;
			readonly UnaryOperatorType operatorType;
			
			public FindUnaryOperator(IMethod op, UnaryOperatorType operatorType)
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
		
		sealed class FindBinaryOperator : SearchScope
		{
			readonly IMethod op;
			readonly BinaryOperatorType operatorType;
			
			public FindBinaryOperator(IMethod op, BinaryOperatorType operatorType)
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
		
		sealed class FindImplicitOperator : SearchScope
		{
			readonly IMethod op;
			
			public FindImplicitOperator(IMethod op)
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
			
			internal override void ProcessConversion(Expression expression, ResolveResult result, Conversion conversion, IType targetType)
			{
				if (conversion.IsUserDefined && conversion.Method.MemberDefinition == op) {
					ReportMatch(expression, result);
				}
			}
		}
		
		sealed class FindExplicitOperator : SearchScope
		{
			readonly IMethod op;
			
			public FindExplicitOperator(IMethod op)
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
		sealed class FindObjectCreateReferences : SearchScope
		{
			readonly IMethod ctor;
			
			public FindObjectCreateReferences(IMethod ctor)
			{
				this.ctor = (IMethod)ctor.MemberDefinition;
				if (ReflectionHelper.GetTypeCode(ctor.DeclaringTypeDefinition) == TypeCode.Empty) {
					// not a built-in type
					this.searchTerm = ctor.DeclaringTypeDefinition.Name;
				}
			}
			
			internal override bool CanMatch(AstNode node)
			{
				return node is ObjectCreateExpression || node is ConstructorDeclaration;
			}
			
			internal override bool IsMatch(ResolveResult rr)
			{
				MemberResolveResult mrr = rr as MemberResolveResult;
				return mrr != null && ctor == mrr.Member.MemberDefinition;
			}
		}
		
		sealed class FindChainedConstructorReferences : SearchScope
		{
			readonly IMethod ctor;
			
			public FindChainedConstructorReferences(IMethod ctor)
			{
				this.ctor = (IMethod)ctor.MemberDefinition;
				if (ctor.DeclaringTypeDefinition.IsSealed)
					this.accessibility = Accessibility.Private;
				else
					this.accessibility = Accessibility.Protected;
				this.accessibility = MergeAccessibility(GetEffectiveAccessibility(ctor), this.accessibility);
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
	}
}
