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
using System.CodeDom;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using ICSharpCode.NRefactory.CSharp.Resolver;
using ICSharpCode.NRefactory.PatternMatching;
using ICSharpCode.NRefactory.Semantics;
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.TypeSystem.Implementation;

namespace ICSharpCode.NRefactory.CSharp
{
	/// <summary>
	/// Converts from C# AST to CodeDom.
	/// </summary>
	/// <remarks>
	/// The conversion is intended for use in the SharpDevelop forms designer.
	/// </remarks>
	public class CodeDomConvertVisitor : IAstVisitor<object, CodeObject>
	{
		ITypeResolveContext context = MinimalResolveContext.Instance;
		ResolveVisitor resolveVisitor;
		bool useFullyQualifiedTypeNames;
		
		/// <summary>
		/// Gets/Sets whether the visitor should use fully-qualified type references.
		/// </summary>
		public bool UseFullyQualifiedTypeNames {
			get { return useFullyQualifiedTypeNames; }
			set { useFullyQualifiedTypeNames = value; }
		}
		
		/// <summary>
		/// Converts a compilation unit to CodeDom.
		/// </summary>
		/// <param name="compilationUnit">The input compilation unit.</param>
		/// <param name="context">Type resolve context, used for resolving type references.</param>
		/// <param name="parsedFile">CSharpParsedFile, used for resolving.</param>
		/// <returns>Converted CodeCompileUnit</returns>
		/// <remarks>
		/// This conversion process requires a resolver because it needs to distinguish field/property/event references etc.
		/// </remarks>
		public CodeCompileUnit Convert(CompilationUnit compilationUnit, ITypeResolveContext context, CSharpParsedFile parsedFile)
		{
			if (compilationUnit == null)
				throw new ArgumentNullException("compilationUnit");
			if (context == null)
				throw new ArgumentNullException("context");
			if (parsedFile == null)
				throw new ArgumentNullException("parsedFile");
			using (var ctx = context.Synchronize()) {
				ResolveVisitor resolveVisitor = new ResolveVisitor(new CSharpResolver(ctx), parsedFile);
				resolveVisitor.Scan(compilationUnit);
				return (CodeCompileUnit)Convert(compilationUnit, resolveVisitor);
			}
		}
		
		/// <summary>
		/// Converts a C# AST node to CodeDom.
		/// </summary>
		/// <param name="node">The input node.</param>
		/// <param name="resolveVisitor">The resolve visitor.
		/// The visitor must be already initialized for the file containing the given node (Scan must be called).</param>
		/// <returns>The node converted into CodeDom</returns>
		/// <remarks>
		/// This conversion process requires a resolver because it needs to distinguish field/property/event references etc.
		/// </remarks>
		public CodeObject Convert(AstNode node, ResolveVisitor resolveVisitor)
		{
			if (node == null)
				throw new ArgumentNullException("node");
			if (resolveVisitor == null)
				throw new ArgumentNullException("resolveVisitor");
			try {
				this.resolveVisitor = resolveVisitor;
				this.context = resolveVisitor.TypeResolveContext;
				return node.AcceptVisitor(this);
			} finally {
				this.resolveVisitor = null;
				this.context = MinimalResolveContext.Instance;
			}
		}
		
		ResolveResult Resolve(AstNode node)
		{
			if (resolveVisitor == null)
				return ErrorResolveResult.UnknownError;
			else
				return resolveVisitor.GetResolveResult(node);
		}
		
		CodeExpression Convert(Expression expr)
		{
			return (CodeExpression)expr.AcceptVisitor(this);
		}
		
		CodeExpression[] Convert(IEnumerable<Expression> expressions)
		{
			List<CodeExpression> result = new List<CodeExpression>();
			foreach (Expression expr in expressions) {
				CodeExpression e = Convert(expr);
				if (e != null)
					result.Add(e);
			}
			return result.ToArray();
		}
		
		CodeTypeReference Convert(AstType type)
		{
			return (CodeTypeReference)type.AcceptVisitor(this);
		}
		
		CodeTypeReference[] Convert(IEnumerable<AstType> types)
		{
			List<CodeTypeReference> result = new List<CodeTypeReference>();
			foreach (AstType type in types) {
				CodeTypeReference e = Convert(type);
				if (e != null)
					result.Add(e);
			}
			return result.ToArray();
		}
		
		CodeTypeReference Convert(IType type)
		{
			return new CodeTypeReference(type.ReflectionName);
		}
		
		CodeStatement Convert(Statement stmt)
		{
			return (CodeStatement)stmt.AcceptVisitor(this);
		}
		
		CodeStatement[] ConvertBlock(BlockStatement block)
		{
			List<CodeStatement> result = new List<CodeStatement>();
			foreach (Statement stmt in block.Statements) {
				CodeStatement s = Convert(stmt);
				if (s != null)
					result.Add(s);
			}
			return result.ToArray();
		}
		
		CodeStatement[] ConvertEmbeddedStatement(Statement embeddedStatement)
		{
			BlockStatement block = embeddedStatement as BlockStatement;
			if (block != null) {
				return ConvertBlock(block);
			}
			CodeStatement s = Convert(embeddedStatement);
			if (s != null)
				return new CodeStatement[] { s };
			else
				return new CodeStatement[0];
		}
		
		string MakeSnippet(AstNode node)
		{
			StringWriter w = new StringWriter();
			CSharpOutputVisitor v = new CSharpOutputVisitor(w, new CSharpFormattingOptions());
			node.AcceptVisitor(v);
			return w.ToString();
		}
		
		/// <summary>
		/// Converts an expression by storing it as C# snippet.
		/// This is used for expressions that cannot be represented in CodeDom.
		/// </summary>
		CodeSnippetExpression MakeSnippetExpression(Expression expr)
		{
			return new CodeSnippetExpression(MakeSnippet(expr));
		}
		
		CodeSnippetStatement MakeSnippetStatement(Statement stmt)
		{
			return new CodeSnippetStatement(MakeSnippet(stmt));
		}
		
		CodeObject IAstVisitor<object, CodeObject>.VisitAnonymousMethodExpression(AnonymousMethodExpression anonymousMethodExpression, object data)
		{
			return MakeSnippetExpression(anonymousMethodExpression);
		}
		
		CodeObject IAstVisitor<object, CodeObject>.VisitUndocumentedExpression(UndocumentedExpression undocumentedExpression, object data)
		{
			return MakeSnippetExpression(undocumentedExpression);
		}
		
		CodeObject IAstVisitor<object, CodeObject>.VisitArrayCreateExpression(ArrayCreateExpression arrayCreateExpression, object data)
		{
			CodeArrayCreateExpression ace = new CodeArrayCreateExpression();
			int dimensions = arrayCreateExpression.Arguments.Count;
			int nestingDepth = arrayCreateExpression.AdditionalArraySpecifiers.Count;
			if (dimensions > 0)
				nestingDepth++;
			if (nestingDepth > 1 || dimensions > 1) {
				// CodeDom does not support jagged or multi-dimensional arrays
				return MakeSnippetExpression(arrayCreateExpression);
			}
			if (arrayCreateExpression.Type.IsNull) {
				ace.CreateType = Convert(Resolve(arrayCreateExpression).Type);
			} else {
				ace.CreateType = Convert(arrayCreateExpression.Type);
			}
			if (arrayCreateExpression.Arguments.Count == 1) {
				ace.SizeExpression = Convert(arrayCreateExpression.Arguments.Single());
			}
			ace.Initializers.AddRange(Convert(arrayCreateExpression.Initializer.Elements));
			return ace;
		}
		
		CodeObject IAstVisitor<object, CodeObject>.VisitArrayInitializerExpression(ArrayInitializerExpression arrayInitializerExpression, object data)
		{
			// Array initializers should be handled by the parent node
			return MakeSnippetExpression(arrayInitializerExpression);
		}
		
		CodeObject IAstVisitor<object, CodeObject>.VisitAsExpression(AsExpression asExpression, object data)
		{
			return MakeSnippetExpression(asExpression);
		}
		
		CodeObject IAstVisitor<object, CodeObject>.VisitAssignmentExpression(AssignmentExpression assignmentExpression, object data)
		{
			// assignments are only supported as statements, not as expressions
			return MakeSnippetExpression(assignmentExpression);
		}
		
		CodeObject IAstVisitor<object, CodeObject>.VisitBaseReferenceExpression(BaseReferenceExpression baseReferenceExpression, object data)
		{
			return new CodeBaseReferenceExpression();
		}
		
		CodeObject IAstVisitor<object, CodeObject>.VisitBinaryOperatorExpression(BinaryOperatorExpression binaryOperatorExpression, object data)
		{
			CodeBinaryOperatorType op;
			switch (binaryOperatorExpression.Operator) {
				case BinaryOperatorType.BitwiseAnd:
					op = CodeBinaryOperatorType.BitwiseAnd;
					break;
				case BinaryOperatorType.BitwiseOr:
					op = CodeBinaryOperatorType.BitwiseOr;
					break;
				case BinaryOperatorType.ConditionalAnd:
					op = CodeBinaryOperatorType.BooleanAnd;
					break;
				case BinaryOperatorType.ConditionalOr:
					op = CodeBinaryOperatorType.BooleanOr;
					break;
				case BinaryOperatorType.GreaterThan:
					op = CodeBinaryOperatorType.GreaterThan;
					break;
				case BinaryOperatorType.GreaterThanOrEqual:
					op = CodeBinaryOperatorType.GreaterThanOrEqual;
					break;
				case BinaryOperatorType.LessThan:
					op = CodeBinaryOperatorType.LessThan;
					break;
				case BinaryOperatorType.LessThanOrEqual:
					op = CodeBinaryOperatorType.LessThanOrEqual;
					break;
				case BinaryOperatorType.Add:
					op = CodeBinaryOperatorType.Add;
					break;
				case BinaryOperatorType.Subtract:
					op = CodeBinaryOperatorType.Subtract;
					break;
				case BinaryOperatorType.Multiply:
					op = CodeBinaryOperatorType.Multiply;
					break;
				case BinaryOperatorType.Divide:
					op = CodeBinaryOperatorType.Divide;
					break;
				case BinaryOperatorType.Modulus:
					op = CodeBinaryOperatorType.Modulus;
					break;
				case BinaryOperatorType.Equality:
				case BinaryOperatorType.InEquality:
					BinaryOperatorResolveResult rr = Resolve(binaryOperatorExpression) as BinaryOperatorResolveResult;
					if (rr != null && rr.Left.Type.IsReferenceType(context) == true) {
						if (binaryOperatorExpression.Operator == BinaryOperatorType.Equality)
							op = CodeBinaryOperatorType.IdentityEquality;
						else
							op = CodeBinaryOperatorType.IdentityInequality;
					} else {
						if (binaryOperatorExpression.Operator == BinaryOperatorType.Equality) {
							op = CodeBinaryOperatorType.ValueEquality;
						} else {
							// CodeDom is retarded and does not support ValueInequality, so we'll simulate it using
							// ValueEquality and Not... but CodeDom doesn't have Not either, so we use
							// '(a == b) == false'
							return new CodeBinaryOperatorExpression(
								new CodeBinaryOperatorExpression(
									Convert(binaryOperatorExpression.Left),
									CodeBinaryOperatorType.ValueEquality,
									Convert(binaryOperatorExpression.Right)
								),
								CodeBinaryOperatorType.ValueEquality,
								new CodePrimitiveExpression(false)
							);
						}
					}
					break;
				default:
					// not supported: xor, shift, null coalescing
					return MakeSnippetExpression(binaryOperatorExpression);
			}
			return new CodeBinaryOperatorExpression(Convert(binaryOperatorExpression.Left), op, Convert(binaryOperatorExpression.Right));
		}
		
		CodeObject IAstVisitor<object, CodeObject>.VisitCastExpression(CastExpression castExpression, object data)
		{
			return new CodeCastExpression(Convert(castExpression.Type), Convert(castExpression.Expression));
		}
		
		CodeObject IAstVisitor<object, CodeObject>.VisitCheckedExpression(CheckedExpression checkedExpression, object data)
		{
			return MakeSnippetExpression(checkedExpression);
		}
		
		CodeObject IAstVisitor<object, CodeObject>.VisitConditionalExpression(ConditionalExpression conditionalExpression, object data)
		{
			return MakeSnippetExpression(conditionalExpression);
		}
		
		CodeObject IAstVisitor<object, CodeObject>.VisitDefaultValueExpression(DefaultValueExpression defaultValueExpression, object data)
		{
			return new CodeDefaultValueExpression(Convert(defaultValueExpression.Type));
		}
		
		CodeObject IAstVisitor<object, CodeObject>.VisitDirectionExpression(DirectionExpression directionExpression, object data)
		{
			System.CodeDom.FieldDirection direction;
			if (directionExpression.FieldDirection == FieldDirection.Out) {
				direction = System.CodeDom.FieldDirection.Out;
			} else {
				direction = System.CodeDom.FieldDirection.Ref;
			}
			return new CodeDirectionExpression(direction, Convert(directionExpression.Expression));
		}
		
		CodeObject IAstVisitor<object, CodeObject>.VisitIdentifierExpression(IdentifierExpression identifierExpression, object data)
		{
			ResolveResult rr = Resolve(identifierExpression);
			LocalResolveResult lrr = rr as LocalResolveResult;
			if (lrr != null && lrr.IsParameter) {
				if (lrr.Variable.Name == "value" && identifierExpression.Ancestors.Any(a => a is Accessor)) {
					return new CodePropertySetValueReferenceExpression();
				} else {
					return new CodeArgumentReferenceExpression(lrr.Variable.Name);
				}
			}
			MemberResolveResult mrr = rr as MemberResolveResult;
			if (mrr != null) {
				return HandleMemberReference(null, identifierExpression.Identifier, identifierExpression.TypeArguments, mrr);
			}
			TypeResolveResult trr = rr as TypeResolveResult;
			if (trr != null) {
				CodeTypeReference typeRef;
				if (useFullyQualifiedTypeNames) {
					typeRef = Convert(trr.Type);
				} else {
					typeRef = new CodeTypeReference(identifierExpression.Identifier);
					typeRef.TypeArguments.AddRange(Convert(identifierExpression.TypeArguments));
				}
				return new CodeTypeReferenceExpression(typeRef);
			}
			MethodGroupResolveResult mgrr = rr as MethodGroupResolveResult;
			if (mgrr != null) {
				return new CodeMethodReferenceExpression(new CodeThisReferenceExpression(), identifierExpression.Identifier, Convert(identifierExpression.TypeArguments));
			}
			return new CodeVariableReferenceExpression(identifierExpression.Identifier);
		}
		
		CodeObject IAstVisitor<object, CodeObject>.VisitIndexerExpression(IndexerExpression indexerExpression, object data)
		{
			if (Resolve(indexerExpression) is ArrayAccessResolveResult)
				return new CodeArrayIndexerExpression(Convert(indexerExpression.Target), Convert(indexerExpression.Arguments));
			else
				return new CodeIndexerExpression(Convert(indexerExpression.Target), Convert(indexerExpression.Arguments));
		}
		
		CodeObject IAstVisitor<object, CodeObject>.VisitInvocationExpression(InvocationExpression invocationExpression, object data)
		{
			MemberResolveResult rr = Resolve(invocationExpression) as MemberResolveResult;
			CSharpInvocationResolveResult csRR = rr as CSharpInvocationResolveResult;
			if (csRR != null && csRR.IsDelegateInvocation) {
				return new CodeDelegateInvokeExpression(Convert(invocationExpression.Target), Convert(invocationExpression.Arguments));
			}
			
			Expression methodExpr = invocationExpression.Target;
			while (methodExpr is ParenthesizedExpression)
				methodExpr = ((ParenthesizedExpression)methodExpr).Expression;
			CodeMethodReferenceExpression mr = null;
			MemberReferenceExpression mre = methodExpr as MemberReferenceExpression;
			if (mre != null) {
				mr = new CodeMethodReferenceExpression(Convert(mre.Target), mre.MemberName, Convert(mre.TypeArguments));
			}
			IdentifierExpression id = methodExpr as IdentifierExpression;
			if (id != null) {
				CodeExpression target;
				if (rr != null && rr.Member.IsStatic)
					target = new CodeTypeReferenceExpression(Convert(rr.Member.DeclaringType));
				else
					target = new CodeThisReferenceExpression();
				
				mr = new CodeMethodReferenceExpression(target, id.Identifier, Convert(id.TypeArguments));
			}
			if (mr != null)
				return new CodeMethodInvokeExpression(mr, Convert(invocationExpression.Arguments));
			else
				return MakeSnippetExpression(invocationExpression);
		}
		
		CodeObject IAstVisitor<object, CodeObject>.VisitIsExpression(IsExpression isExpression, object data)
		{
			return MakeSnippetExpression(isExpression);
		}
		
		CodeObject IAstVisitor<object, CodeObject>.VisitLambdaExpression(LambdaExpression lambdaExpression, object data)
		{
			return MakeSnippetExpression(lambdaExpression);
		}
		
		CodeObject IAstVisitor<object, CodeObject>.VisitMemberReferenceExpression(MemberReferenceExpression memberReferenceExpression, object data)
		{
			CodeExpression target = Convert(memberReferenceExpression.Target);
			ResolveResult rr = Resolve(memberReferenceExpression);
			MemberResolveResult mrr = rr as MemberResolveResult;
			if (mrr != null) {
				return HandleMemberReference(target, memberReferenceExpression.MemberName, memberReferenceExpression.TypeArguments, mrr);
			} else {
				if (memberReferenceExpression.TypeArguments.Any() || rr is MethodGroupResolveResult) {
					return new CodeMethodReferenceExpression(target, memberReferenceExpression.MemberName, Convert(memberReferenceExpression.TypeArguments));
				} else {
					return new CodePropertyReferenceExpression(target, memberReferenceExpression.MemberName);
				}
			}
		}
		
		CodeExpression HandleMemberReference(CodeExpression target, string identifier, AstNodeCollection<AstType> typeArguments, MemberResolveResult mrr)
		{
			if (target == null) {
				if (mrr.Member.IsStatic)
					target = new CodeTypeReferenceExpression(Convert(mrr.Member.DeclaringType));
				else
					target = new CodeThisReferenceExpression();
			}
			if (mrr.Member is IField) {
				return new CodeFieldReferenceExpression(target, identifier);
			} else if (mrr.Member is IMethod) {
				return new CodeMethodReferenceExpression(target, identifier, Convert(typeArguments));
			} else if (mrr.Member is IEvent) {
				return new CodeEventReferenceExpression(target, identifier);
			} else {
				return new CodePropertyReferenceExpression(target, identifier);
			}
		}
		
		CodeObject IAstVisitor<object, CodeObject>.VisitNamedArgumentExpression(NamedArgumentExpression namedArgumentExpression, object data)
		{
			return MakeSnippetExpression(namedArgumentExpression);
		}
		
		CodeObject IAstVisitor<object, CodeObject>.VisitNamedExpression(NamedExpression namedExpression, object data)
		{
			return MakeSnippetExpression(namedExpression);
		}
		
		CodeObject IAstVisitor<object, CodeObject>.VisitNullReferenceExpression(NullReferenceExpression nullReferenceExpression, object data)
		{
			return new CodePrimitiveExpression(null);
		}
		
		CodeObject IAstVisitor<object, CodeObject>.VisitObjectCreateExpression(ObjectCreateExpression objectCreateExpression, object data)
		{
			if (!objectCreateExpression.Initializer.IsNull)
				return MakeSnippetExpression(objectCreateExpression);
			return new CodeObjectCreateExpression(Convert(objectCreateExpression.Type), Convert(objectCreateExpression.Arguments));
		}
		
		CodeObject IAstVisitor<object, CodeObject>.VisitAnonymousTypeCreateExpression(AnonymousTypeCreateExpression anonymousTypeCreateExpression, object data)
		{
			return MakeSnippetExpression(anonymousTypeCreateExpression);
		}
		
		CodeObject IAstVisitor<object, CodeObject>.VisitParenthesizedExpression(ParenthesizedExpression parenthesizedExpression, object data)
		{
			// CodeDom generators will insert parentheses where necessary
			return Convert(parenthesizedExpression.Expression);
		}
		
		CodeObject IAstVisitor<object, CodeObject>.VisitPointerReferenceExpression(PointerReferenceExpression pointerReferenceExpression, object data)
		{
			return MakeSnippetExpression(pointerReferenceExpression);
		}
		
		CodeObject IAstVisitor<object, CodeObject>.VisitPrimitiveExpression(PrimitiveExpression primitiveExpression, object data)
		{
			return new CodePrimitiveExpression(primitiveExpression.Value);
		}
		
		CodeObject IAstVisitor<object, CodeObject>.VisitSizeOfExpression(SizeOfExpression sizeOfExpression, object data)
		{
			return MakeSnippetExpression(sizeOfExpression);
		}
		
		CodeObject IAstVisitor<object, CodeObject>.VisitStackAllocExpression(StackAllocExpression stackAllocExpression, object data)
		{
			return MakeSnippetExpression(stackAllocExpression);
		}
		
		CodeObject IAstVisitor<object, CodeObject>.VisitThisReferenceExpression(ThisReferenceExpression thisReferenceExpression, object data)
		{
			return new CodeThisReferenceExpression();
		}
		
		CodeObject IAstVisitor<object, CodeObject>.VisitTypeOfExpression(TypeOfExpression typeOfExpression, object data)
		{
			return new CodeTypeOfExpression(Convert(typeOfExpression.Type));
		}
		
		CodeObject IAstVisitor<object, CodeObject>.VisitTypeReferenceExpression(TypeReferenceExpression typeReferenceExpression, object data)
		{
			return new CodeTypeReferenceExpression(Convert(typeReferenceExpression.Type));
		}
		
		CodeObject IAstVisitor<object, CodeObject>.VisitUnaryOperatorExpression(UnaryOperatorExpression unaryOperatorExpression, object data)
		{
			switch (unaryOperatorExpression.Operator) {
				case UnaryOperatorType.Not:
					return new CodeBinaryOperatorExpression(
						Convert(unaryOperatorExpression.Expression),
						CodeBinaryOperatorType.ValueEquality,
						new CodePrimitiveExpression(false));
				case UnaryOperatorType.Minus:
					return new CodeBinaryOperatorExpression(
						new CodePrimitiveExpression(0),
						CodeBinaryOperatorType.Subtract,
						Convert(unaryOperatorExpression.Expression));
				case UnaryOperatorType.Plus:
					return Convert(unaryOperatorExpression.Expression);
				default:
					return MakeSnippetExpression(unaryOperatorExpression);
			}
		}
		
		CodeObject IAstVisitor<object, CodeObject>.VisitUncheckedExpression(UncheckedExpression uncheckedExpression, object data)
		{
			return MakeSnippetExpression(uncheckedExpression);
		}
		
		CodeObject IAstVisitor<object, CodeObject>.VisitEmptyExpression(EmptyExpression emptyExpression, object data)
		{
			return null;
		}
		
		CodeObject IAstVisitor<object, CodeObject>.VisitQueryExpression(QueryExpression queryExpression, object data)
		{
			return MakeSnippetExpression(queryExpression);
		}
		
		CodeObject IAstVisitor<object, CodeObject>.VisitQueryContinuationClause(QueryContinuationClause queryContinuationClause, object data)
		{
			throw new NotSupportedException();
		}
		
		CodeObject IAstVisitor<object, CodeObject>.VisitQueryFromClause(QueryFromClause queryFromClause, object data)
		{
			throw new NotSupportedException();
		}
		
		CodeObject IAstVisitor<object, CodeObject>.VisitQueryLetClause(QueryLetClause queryLetClause, object data)
		{
			throw new NotSupportedException();
		}
		
		CodeObject IAstVisitor<object, CodeObject>.VisitQueryWhereClause(QueryWhereClause queryWhereClause, object data)
		{
			throw new NotSupportedException();
		}
		
		CodeObject IAstVisitor<object, CodeObject>.VisitQueryJoinClause(QueryJoinClause queryJoinClause, object data)
		{
			throw new NotSupportedException();
		}
		
		CodeObject IAstVisitor<object, CodeObject>.VisitQueryOrderClause(QueryOrderClause queryOrderClause, object data)
		{
			throw new NotSupportedException();
		}
		
		CodeObject IAstVisitor<object, CodeObject>.VisitQueryOrdering(QueryOrdering queryOrdering, object data)
		{
			throw new NotSupportedException();
		}
		
		CodeObject IAstVisitor<object, CodeObject>.VisitQuerySelectClause(QuerySelectClause querySelectClause, object data)
		{
			throw new NotSupportedException();
		}
		
		CodeObject IAstVisitor<object, CodeObject>.VisitQueryGroupClause(QueryGroupClause queryGroupClause, object data)
		{
			throw new NotSupportedException();
		}
		
		CodeObject IAstVisitor<object, CodeObject>.VisitAttribute(Attribute attribute, object data)
		{
			throw new NotSupportedException();
		}
		
		CodeObject IAstVisitor<object, CodeObject>.VisitAttributeSection(AttributeSection attributeSection, object data)
		{
			throw new NotSupportedException();
		}
		
		CodeAttributeDeclaration Convert(Attribute attribute)
		{
			var attr = new CodeAttributeDeclaration(Convert(attribute.Type));
			foreach (Expression expr in attribute.Arguments) {
				NamedExpression ne = expr as NamedExpression;
				if (ne != null)
					attr.Arguments.Add(new CodeAttributeArgument(ne.Identifier, Convert(ne.Expression)));
				else
					attr.Arguments.Add(new CodeAttributeArgument(Convert(expr)));
			}
			return attr;
		}
		
		CodeAttributeDeclaration[] Convert(IEnumerable<AttributeSection> attributeSections)
		{
			List<CodeAttributeDeclaration> result = new List<CodeAttributeDeclaration>();
			foreach (AttributeSection section in attributeSections) {
				foreach (Attribute attr in section.Attributes) {
					CodeAttributeDeclaration attrDecl = Convert(attr);
					if (attrDecl != null)
						result.Add(attrDecl);
				}
			}
			return result.ToArray();
		}
		
		CodeObject IAstVisitor<object, CodeObject>.VisitDelegateDeclaration(DelegateDeclaration delegateDeclaration, object data)
		{
			CodeTypeDelegate d = new CodeTypeDelegate(delegateDeclaration.Name);
			d.Attributes = ConvertMemberAttributes(delegateDeclaration.Modifiers);
			d.CustomAttributes.AddRange(Convert(delegateDeclaration.Attributes));
			d.ReturnType = Convert(delegateDeclaration.ReturnType);
			d.Parameters.AddRange(Convert(delegateDeclaration.Parameters));
			d.TypeParameters.AddRange(ConvertTypeParameters(delegateDeclaration.TypeParameters, delegateDeclaration.Constraints));
			return d;
		}
		
		static MemberAttributes ConvertMemberAttributes(Modifiers modifiers)
		{
			MemberAttributes a = 0;
			if ((modifiers & Modifiers.Abstract) != 0)
				a |= MemberAttributes.Abstract;
			if ((modifiers & Modifiers.Sealed) != 0)
				a |= MemberAttributes.Final;
			if ((modifiers & Modifiers.Static) != 0)
				a |= MemberAttributes.Static;
			if ((modifiers & Modifiers.Override) != 0)
				a |= MemberAttributes.Override;
			if ((modifiers & Modifiers.Const) != 0)
				a |= MemberAttributes.Const;
			if ((modifiers & Modifiers.New) != 0)
				a |= MemberAttributes.New;
			
			if ((modifiers & Modifiers.Public) != 0)
				a |= MemberAttributes.Public;
			else if ((modifiers & (Modifiers.Protected | Modifiers.Internal)) == (Modifiers.Protected | Modifiers.Internal))
				a |= MemberAttributes.FamilyOrAssembly;
			else if ((modifiers & Modifiers.Protected) != 0)
				a |= MemberAttributes.Family;
			else if ((modifiers & Modifiers.Internal) != 0)
				a |= MemberAttributes.Assembly;
			else if ((modifiers & Modifiers.Private) != 0)
				a |= MemberAttributes.Private;
			
			return a;
		}
		
		CodeObject IAstVisitor<object, CodeObject>.VisitNamespaceDeclaration(NamespaceDeclaration namespaceDeclaration, object data)
		{
			CodeNamespace ns = new CodeNamespace(namespaceDeclaration.Name);
			foreach (AstNode node in namespaceDeclaration.Members) {
				CodeObject r = node.AcceptVisitor(this);
				
				CodeNamespaceImport import = r as CodeNamespaceImport;
				if (import != null)
					ns.Imports.Add(import);
				
				CodeTypeDeclaration typeDecl = r as CodeTypeDeclaration;
				if (typeDecl != null)
					ns.Types.Add(typeDecl);
			}
			return ns;
		}
		
		Stack<CodeTypeDeclaration> typeStack = new Stack<CodeTypeDeclaration>();
		
		CodeObject IAstVisitor<object, CodeObject>.VisitTypeDeclaration(TypeDeclaration typeDeclaration, object data)
		{
			bool isNestedType = typeStack.Count > 0;
			CodeTypeDeclaration typeDecl = new CodeTypeDeclaration(typeDeclaration.Name);
			typeDecl.Attributes = ConvertMemberAttributes(typeDeclaration.Modifiers);
			typeDecl.CustomAttributes.AddRange(Convert(typeDeclaration.Attributes));
			
			switch (typeDeclaration.ClassType) {
				case ClassType.Struct:
					typeDecl.IsStruct = true;
					break;
				case ClassType.Interface:
					typeDecl.IsInterface = true;
					break;
				case ClassType.Enum:
					typeDecl.IsEnum = true;
					break;
				default:
					typeDecl.IsClass = true;
					break;
			}
			typeDecl.IsPartial = (typeDeclaration.Modifiers & Modifiers.Partial) == Modifiers.Partial;
			
			typeDecl.BaseTypes.AddRange(Convert(typeDeclaration.BaseTypes));
			typeDecl.TypeParameters.AddRange(ConvertTypeParameters(typeDeclaration.TypeParameters, typeDeclaration.Constraints));
			
			typeStack.Push(typeDecl);
			foreach (var member in typeDeclaration.Members) {
				CodeTypeMember m = member.AcceptVisitor(this) as CodeTypeMember;
				if (m != null)
					typeDecl.Members.Add(m);
			}
			typeStack.Pop();
			return typeDecl;
		}
		
		void AddTypeMember(CodeTypeMember member)
		{
			if (typeStack.Count != 0)
				typeStack.Peek().Members.Add(member);
		}
		
		CodeObject IAstVisitor<object, CodeObject>.VisitUsingAliasDeclaration(UsingAliasDeclaration usingAliasDeclaration, object data)
		{
			return new CodeSnippetTypeMember(MakeSnippet(usingAliasDeclaration));
		}
		
		CodeObject IAstVisitor<object, CodeObject>.VisitUsingDeclaration(UsingDeclaration usingDeclaration, object data)
		{
			return new CodeNamespaceImport(usingDeclaration.Namespace);
		}
		
		CodeObject IAstVisitor<object, CodeObject>.VisitExternAliasDeclaration(ExternAliasDeclaration externAliasDeclaration, object data)
		{
			return new CodeSnippetTypeMember(MakeSnippet(externAliasDeclaration));
		}
		
		CodeObject IAstVisitor<object, CodeObject>.VisitBlockStatement(BlockStatement blockStatement, object data)
		{
			return new CodeConditionStatement(new CodePrimitiveExpression(true), ConvertBlock(blockStatement));
		}
		
		CodeObject IAstVisitor<object, CodeObject>.VisitBreakStatement(BreakStatement breakStatement, object data)
		{
			return MakeSnippetStatement(breakStatement);
		}
		
		CodeObject IAstVisitor<object, CodeObject>.VisitCheckedStatement(CheckedStatement checkedStatement, object data)
		{
			return MakeSnippetStatement(checkedStatement);
		}
		
		CodeObject IAstVisitor<object, CodeObject>.VisitContinueStatement(ContinueStatement continueStatement, object data)
		{
			return MakeSnippetStatement(continueStatement);
		}
		
		CodeObject IAstVisitor<object, CodeObject>.VisitDoWhileStatement(DoWhileStatement doWhileStatement, object data)
		{
			// do { } while (expr);
			//
			// emulate with:
			//  for (bool _do = true; _do; _do = expr) {}
			string varName = "_do" + doWhileStatement.Ancestors.OfType<DoWhileStatement>().Count();
			return new CodeIterationStatement(
				new CodeVariableDeclarationStatement(typeof(bool), varName, new CodePrimitiveExpression(true)),
				new CodeVariableReferenceExpression(varName),
				new CodeAssignStatement(new CodeVariableReferenceExpression(varName), Convert(doWhileStatement.Condition)),
				ConvertEmbeddedStatement(doWhileStatement.EmbeddedStatement)
			);
		}
		
		CodeObject IAstVisitor<object, CodeObject>.VisitEmptyStatement(EmptyStatement emptyStatement, object data)
		{
			return null;
		}
		
		CodeObject IAstVisitor<object, CodeObject>.VisitExpressionStatement(ExpressionStatement expressionStatement, object data)
		{
			AssignmentExpression assignment = expressionStatement.Expression as AssignmentExpression;
			if (assignment != null && assignment.Operator == AssignmentOperatorType.Assign) {
				return new CodeAssignStatement(Convert(assignment.Left), Convert(assignment.Right));
			}
			return new CodeExpressionStatement(Convert(expressionStatement.Expression));
		}
		
		CodeObject IAstVisitor<object, CodeObject>.VisitFixedStatement(FixedStatement fixedStatement, object data)
		{
			return MakeSnippetStatement(fixedStatement);
		}
		
		CodeObject IAstVisitor<object, CodeObject>.VisitForeachStatement(ForeachStatement foreachStatement, object data)
		{
			return MakeSnippetStatement(foreachStatement);
		}
		
		CodeObject IAstVisitor<object, CodeObject>.VisitForStatement(ForStatement forStatement, object data)
		{
			if (forStatement.Initializers.Count != 1 || forStatement.Iterators.Count != 1)
				return MakeSnippetStatement(forStatement);
			return new CodeIterationStatement(
				Convert(forStatement.Initializers.Single()),
				Convert(forStatement.Condition),
				Convert(forStatement.Iterators.Single()),
				ConvertEmbeddedStatement(forStatement.EmbeddedStatement)
			);
		}
		
		CodeObject IAstVisitor<object, CodeObject>.VisitGotoCaseStatement(GotoCaseStatement gotoCaseStatement, object data)
		{
			return MakeSnippetStatement(gotoCaseStatement);
		}
		
		CodeObject IAstVisitor<object, CodeObject>.VisitGotoDefaultStatement(GotoDefaultStatement gotoDefaultStatement, object data)
		{
			return MakeSnippetStatement(gotoDefaultStatement);
		}
		
		CodeObject IAstVisitor<object, CodeObject>.VisitGotoStatement(GotoStatement gotoStatement, object data)
		{
			return new CodeGotoStatement(gotoStatement.Label);
		}
		
		CodeObject IAstVisitor<object, CodeObject>.VisitIfElseStatement(IfElseStatement ifElseStatement, object data)
		{
			return new CodeConditionStatement(
				Convert(ifElseStatement.Condition),
				ConvertEmbeddedStatement(ifElseStatement.TrueStatement),
				ConvertEmbeddedStatement(ifElseStatement.FalseStatement));
		}
		
		CodeObject IAstVisitor<object, CodeObject>.VisitLabelStatement(LabelStatement labelStatement, object data)
		{
			return new CodeLabeledStatement(labelStatement.Label);
		}
		
		CodeObject IAstVisitor<object, CodeObject>.VisitLockStatement(LockStatement lockStatement, object data)
		{
			return MakeSnippetStatement(lockStatement);
		}
		
		CodeObject IAstVisitor<object, CodeObject>.VisitReturnStatement(ReturnStatement returnStatement, object data)
		{
			return new CodeMethodReturnStatement(Convert(returnStatement.Expression));
		}
		
		CodeObject IAstVisitor<object, CodeObject>.VisitSwitchStatement(SwitchStatement switchStatement, object data)
		{
			return MakeSnippetStatement(switchStatement);
		}
		
		CodeObject IAstVisitor<object, CodeObject>.VisitSwitchSection(SwitchSection switchSection, object data)
		{
			throw new NotSupportedException();
		}
		
		CodeObject IAstVisitor<object, CodeObject>.VisitCaseLabel(CaseLabel caseLabel, object data)
		{
			throw new NotSupportedException();
		}
		
		CodeObject IAstVisitor<object, CodeObject>.VisitThrowStatement(ThrowStatement throwStatement, object data)
		{
			return new CodeThrowExceptionStatement(Convert(throwStatement.Expression));
		}
		
		CodeObject IAstVisitor<object, CodeObject>.VisitTryCatchStatement(TryCatchStatement tryCatchStatement, object data)
		{
			List<CodeCatchClause> catchClauses = new List<CodeCatchClause>();
			foreach (var catchClause in tryCatchStatement.CatchClauses) {
				catchClauses.Add(new CodeCatchClause(catchClause.VariableName, Convert(catchClause.Type), ConvertBlock(catchClause.Body)));
			}
			return new CodeTryCatchFinallyStatement(
				ConvertBlock(tryCatchStatement.TryBlock),
				catchClauses.ToArray(),
				ConvertBlock(tryCatchStatement.FinallyBlock));
		}
		
		CodeObject IAstVisitor<object, CodeObject>.VisitCatchClause(CatchClause catchClause, object data)
		{
			throw new NotSupportedException();
		}
		
		CodeObject IAstVisitor<object, CodeObject>.VisitUncheckedStatement(UncheckedStatement uncheckedStatement, object data)
		{
			return MakeSnippetStatement(uncheckedStatement);
		}
		
		CodeObject IAstVisitor<object, CodeObject>.VisitUnsafeStatement(UnsafeStatement unsafeStatement, object data)
		{
			return MakeSnippetStatement(unsafeStatement);
		}
		
		CodeObject IAstVisitor<object, CodeObject>.VisitUsingStatement(UsingStatement usingStatement, object data)
		{
			return MakeSnippetStatement(usingStatement);
		}
		
		CodeObject IAstVisitor<object, CodeObject>.VisitVariableDeclarationStatement(VariableDeclarationStatement variableDeclarationStatement, object data)
		{
			if (variableDeclarationStatement.Variables.Count != 1)
				return MakeSnippetStatement(variableDeclarationStatement);
			VariableInitializer vi = variableDeclarationStatement.Variables.Single();
			return new CodeVariableDeclarationStatement(
				Convert(variableDeclarationStatement.Type),
				vi.Name,
				ConvertVariableInitializer(vi.Initializer, variableDeclarationStatement.Type));
		}
		
		CodeExpression ConvertVariableInitializer(Expression expr, AstType type)
		{
			ArrayInitializerExpression aie = expr as ArrayInitializerExpression;
			if (aie != null) {
				return new CodeArrayCreateExpression(Convert(type), Convert(aie.Elements));
			} else {
				return Convert(expr);
			}
		}
		
		CodeObject IAstVisitor<object, CodeObject>.VisitWhileStatement(WhileStatement whileStatement, object data)
		{
			return new CodeIterationStatement(null, Convert(whileStatement.Condition), null, ConvertEmbeddedStatement(whileStatement.EmbeddedStatement));
		}
		
		CodeObject IAstVisitor<object, CodeObject>.VisitYieldBreakStatement(YieldBreakStatement yieldBreakStatement, object data)
		{
			return MakeSnippetStatement(yieldBreakStatement);
		}
		
		CodeObject IAstVisitor<object, CodeObject>.VisitYieldReturnStatement(YieldReturnStatement yieldStatement, object data)
		{
			return MakeSnippetStatement(yieldStatement);
		}
		
		CodeObject IAstVisitor<object, CodeObject>.VisitAccessor(Accessor accessor, object data)
		{
			throw new NotSupportedException();
		}
		
		CodeObject IAstVisitor<object, CodeObject>.VisitConstructorDeclaration(ConstructorDeclaration constructorDeclaration, object data)
		{
			CodeConstructor ctor = new CodeConstructor();
			ctor.Attributes = ConvertMemberAttributes(constructorDeclaration.Modifiers);
			ctor.CustomAttributes.AddRange(Convert(constructorDeclaration.Attributes));
			if (constructorDeclaration.Initializer.ConstructorInitializerType == ConstructorInitializerType.This) {
				ctor.ChainedConstructorArgs.AddRange(Convert(constructorDeclaration.Initializer.Arguments));
			} else {
				ctor.BaseConstructorArgs.AddRange(Convert(constructorDeclaration.Initializer.Arguments));
			}
			ctor.Parameters.AddRange(Convert(constructorDeclaration.Parameters));
			
			ctor.Statements.AddRange(ConvertBlock(constructorDeclaration.Body));
			return ctor;
		}
		
		CodeObject IAstVisitor<object, CodeObject>.VisitConstructorInitializer(ConstructorInitializer constructorInitializer, object data)
		{
			throw new NotSupportedException();
		}
		
		CodeObject IAstVisitor<object, CodeObject>.VisitDestructorDeclaration(DestructorDeclaration destructorDeclaration, object data)
		{
			return new CodeSnippetTypeMember(MakeSnippet(destructorDeclaration));
		}
		
		CodeObject IAstVisitor<object, CodeObject>.VisitEnumMemberDeclaration(EnumMemberDeclaration enumMemberDeclaration, object data)
		{
			TypeDeclaration td = enumMemberDeclaration.Parent as TypeDeclaration;
			CodeMemberField f = new CodeMemberField(td != null ? td.Name : "Enum", enumMemberDeclaration.Name);
			f.Attributes = MemberAttributes.Public | MemberAttributes.Static;
			f.CustomAttributes.AddRange(Convert(enumMemberDeclaration.Attributes));
			f.InitExpression = Convert(enumMemberDeclaration.Initializer);
			return f;
		}
		
		CodeObject IAstVisitor<object, CodeObject>.VisitEventDeclaration(EventDeclaration eventDeclaration, object data)
		{
			foreach (VariableInitializer vi in eventDeclaration.Variables) {
				if (!vi.Initializer.IsNull) {
					AddTypeMember(new CodeSnippetTypeMember(MakeSnippet(eventDeclaration)));
					continue;
				}
				
				CodeMemberEvent e = new CodeMemberEvent();
				e.Attributes = ConvertMemberAttributes(eventDeclaration.Modifiers);
				e.CustomAttributes.AddRange(Convert(eventDeclaration.Attributes));
				e.Name = vi.Name;
				e.Type = Convert(eventDeclaration.ReturnType);
				AddTypeMember(e);
			}
			return null;
		}
		
		CodeObject IAstVisitor<object, CodeObject>.VisitCustomEventDeclaration(CustomEventDeclaration customEventDeclaration, object data)
		{
			return new CodeSnippetTypeMember(MakeSnippet(customEventDeclaration));
		}
		
		CodeObject IAstVisitor<object, CodeObject>.VisitFieldDeclaration(FieldDeclaration fieldDeclaration, object data)
		{
			foreach (VariableInitializer vi in fieldDeclaration.Variables) {
				CodeMemberField f = new CodeMemberField(Convert(fieldDeclaration.ReturnType), vi.Name);
				f.Attributes = ConvertMemberAttributes(fieldDeclaration.Modifiers);
				f.CustomAttributes.AddRange(Convert(fieldDeclaration.Attributes));
				f.InitExpression = ConvertVariableInitializer(vi.Initializer, fieldDeclaration.ReturnType);
				AddTypeMember(f);
			}
			return null;
		}
		
		CodeObject IAstVisitor<object, CodeObject>.VisitIndexerDeclaration(IndexerDeclaration indexerDeclaration, object data)
		{
			CodeMemberProperty p = new CodeMemberProperty();
			p.Attributes = ConvertMemberAttributes(indexerDeclaration.Modifiers);
			p.CustomAttributes.AddRange(Convert(indexerDeclaration.Attributes));
			p.Name = "Items";
			p.PrivateImplementationType = Convert(indexerDeclaration.PrivateImplementationType);
			p.Parameters.AddRange(Convert(indexerDeclaration.Parameters));
			p.Type = Convert(indexerDeclaration.ReturnType);
			
			if (!indexerDeclaration.Getter.IsNull) {
				p.HasGet = true;
				p.GetStatements.AddRange(ConvertBlock(indexerDeclaration.Getter.Body));
			}
			if (!indexerDeclaration.Setter.IsNull) {
				p.HasSet = true;
				p.SetStatements.AddRange(ConvertBlock(indexerDeclaration.Setter.Body));
			}
			return p;
		}
		
		CodeObject IAstVisitor<object, CodeObject>.VisitMethodDeclaration(MethodDeclaration methodDeclaration, object data)
		{
			CodeMemberMethod m = new CodeMemberMethod();
			m.Attributes = ConvertMemberAttributes(methodDeclaration.Modifiers);
			
			m.CustomAttributes.AddRange(Convert(methodDeclaration.Attributes.Where(a => a.AttributeTarget != "return")));
			m.ReturnTypeCustomAttributes.AddRange(Convert(methodDeclaration.Attributes.Where(a => a.AttributeTarget == "return")));
			
			m.ReturnType = Convert(methodDeclaration.ReturnType);
			m.PrivateImplementationType = Convert(methodDeclaration.PrivateImplementationType);
			m.Name = methodDeclaration.Name;
			m.TypeParameters.AddRange(ConvertTypeParameters(methodDeclaration.TypeParameters, methodDeclaration.Constraints));
			m.Parameters.AddRange(Convert(methodDeclaration.Parameters));
			
			m.Statements.AddRange(ConvertBlock(methodDeclaration.Body));
			return m;
		}
		
		CodeObject IAstVisitor<object, CodeObject>.VisitOperatorDeclaration(OperatorDeclaration operatorDeclaration, object data)
		{
			CodeMemberMethod m = new CodeMemberMethod();
			m.Attributes = ConvertMemberAttributes(operatorDeclaration.Modifiers);
			
			m.CustomAttributes.AddRange(Convert(operatorDeclaration.Attributes.Where(a => a.AttributeTarget != "return")));
			m.ReturnTypeCustomAttributes.AddRange(Convert(operatorDeclaration.Attributes.Where(a => a.AttributeTarget == "return")));
			
			m.ReturnType = Convert(operatorDeclaration.ReturnType);
			m.Name = operatorDeclaration.Name;
			m.Parameters.AddRange(Convert(operatorDeclaration.Parameters));
			
			m.Statements.AddRange(ConvertBlock(operatorDeclaration.Body));
			return m;
		}
		
		CodeObject IAstVisitor<object, CodeObject>.VisitParameterDeclaration(ParameterDeclaration parameterDeclaration, object data)
		{
			var p = new CodeParameterDeclarationExpression(Convert(parameterDeclaration.Type), parameterDeclaration.Name);
			p.CustomAttributes.AddRange(Convert(parameterDeclaration.Attributes));
			switch (parameterDeclaration.ParameterModifier) {
				case ParameterModifier.Ref:
					p.Direction = System.CodeDom.FieldDirection.Ref;
					break;
				case ParameterModifier.Out:
					p.Direction = System.CodeDom.FieldDirection.Out;
					break;
			}
			return p;
		}
		
		CodeParameterDeclarationExpression[] Convert(IEnumerable<ParameterDeclaration> parameters)
		{
			List<CodeParameterDeclarationExpression> result = new List<CodeParameterDeclarationExpression>();
			foreach (ParameterDeclaration pd in parameters) {
				CodeParameterDeclarationExpression pde = pd.AcceptVisitor(this) as CodeParameterDeclarationExpression;
				if (pde != null)
					result.Add(pde);
			}
			return result.ToArray();
		}
		
		CodeObject IAstVisitor<object, CodeObject>.VisitPropertyDeclaration(PropertyDeclaration propertyDeclaration, object data)
		{
			CodeMemberProperty p = new CodeMemberProperty();
			p.Attributes = ConvertMemberAttributes(propertyDeclaration.Modifiers);
			p.CustomAttributes.AddRange(Convert(propertyDeclaration.Attributes));
			p.Name = propertyDeclaration.Name;
			p.PrivateImplementationType = Convert(propertyDeclaration.PrivateImplementationType);
			p.Type = Convert(propertyDeclaration.ReturnType);
			
			if (!propertyDeclaration.Getter.IsNull) {
				p.HasGet = true;
				p.GetStatements.AddRange(ConvertBlock(propertyDeclaration.Getter.Body));
			}
			if (!propertyDeclaration.Setter.IsNull) {
				p.HasSet = true;
				p.SetStatements.AddRange(ConvertBlock(propertyDeclaration.Setter.Body));
			}
			return p;
		}
		
		CodeObject IAstVisitor<object, CodeObject>.VisitVariableInitializer(VariableInitializer variableInitializer, object data)
		{
			throw new NotSupportedException(); // should be handled by the parent node
		}
		
		CodeObject IAstVisitor<object, CodeObject>.VisitFixedFieldDeclaration(FixedFieldDeclaration fixedFieldDeclaration, object data)
		{
			return new CodeSnippetTypeMember(MakeSnippet(fixedFieldDeclaration));
		}
		
		CodeObject IAstVisitor<object, CodeObject>.VisitFixedVariableInitializer(FixedVariableInitializer fixedVariableInitializer, object data)
		{
			throw new NotSupportedException(); // should be handled by the parent node
		}
		
		CodeObject IAstVisitor<object, CodeObject>.VisitCompilationUnit(CompilationUnit compilationUnit, object data)
		{
			CodeCompileUnit cu = new CodeCompileUnit();
			foreach (AstNode node in compilationUnit.Children) {
				CodeObject o = node.AcceptVisitor(this);
				
				CodeNamespace ns = o as CodeNamespace;
				if (ns != null) {
					cu.Namespaces.Add(ns);
				}
				CodeTypeDeclaration td = o as CodeTypeDeclaration;
				if (td != null) {
					cu.Namespaces.Add(new CodeNamespace() { Types = { td } });
				}
			}
			return cu;
		}
		
		CodeObject IAstVisitor<object, CodeObject>.VisitSimpleType(SimpleType simpleType, object data)
		{
			if (useFullyQualifiedTypeNames) {
				IType type = Resolve(simpleType).Type;
				if (type.Kind != TypeKind.Unknown)
					return Convert(type);
			}
			var tr = new CodeTypeReference(simpleType.Identifier);
			tr.TypeArguments.AddRange(Convert(simpleType.TypeArguments));
			return tr;
		}
		
		CodeObject IAstVisitor<object, CodeObject>.VisitMemberType(MemberType memberType, object data)
		{
			if (memberType.IsDoubleColon && new SimpleType("global").IsMatch(memberType.Target)) {
				var tr = new CodeTypeReference(memberType.MemberName, CodeTypeReferenceOptions.GlobalReference);
				tr.TypeArguments.AddRange(Convert(memberType.TypeArguments));
				return tr;
			}
			if (useFullyQualifiedTypeNames || memberType.IsDoubleColon) {
				IType type = Resolve(memberType).Type;
				if (type.Kind != TypeKind.Unknown)
					return Convert(type);
			}
			CodeTypeReference target = Convert(memberType.Target);
			if (target == null)
				return null;
			target.BaseType = target.BaseType + "." + memberType.MemberName;
			target.TypeArguments.AddRange(Convert(memberType.TypeArguments));
			return target;
		}
		
		CodeObject IAstVisitor<object, CodeObject>.VisitComposedType(ComposedType composedType, object data)
		{
			CodeTypeReference typeRef = Convert(composedType.BaseType);
			if (typeRef == null)
				return null;
			if (composedType.HasNullableSpecifier) {
				typeRef = new CodeTypeReference("System.Nullable") { TypeArguments = { typeRef } };
			}
			foreach (ArraySpecifier s in composedType.ArraySpecifiers.Reverse()) {
				typeRef = new CodeTypeReference(typeRef, s.Dimensions);
			}
			return typeRef;
		}
		
		CodeObject IAstVisitor<object, CodeObject>.VisitArraySpecifier(ArraySpecifier arraySpecifier, object data)
		{
			throw new NotSupportedException(); // handled by parent node
		}
		
		CodeObject IAstVisitor<object, CodeObject>.VisitPrimitiveType(PrimitiveType primitiveType, object data)
		{
			string keyword = primitiveType.Keyword;
			for (TypeCode c = TypeCode.Empty; c <= TypeCode.String; c++) {
				if (ReflectionHelper.GetCSharpNameByTypeCode(c) == keyword)
					return new CodeTypeReference("System." + ReflectionHelper.GetShortNameByTypeCode(c));
			}
			return new CodeTypeReference(primitiveType.Keyword);
		}
		
		CodeObject IAstVisitor<object, CodeObject>.VisitComment(Comment comment, object data)
		{
			return new CodeComment(comment.Content, comment.CommentType == CommentType.Documentation);
		}
		
		CodeObject IAstVisitor<object, CodeObject>.VisitTypeParameterDeclaration(TypeParameterDeclaration typeParameterDeclaration, object data)
		{
			throw new NotSupportedException(); // type parameters and constraints are handled together
		}
		
		CodeObject IAstVisitor<object, CodeObject>.VisitConstraint(Constraint constraint, object data)
		{
			throw new NotSupportedException();
		}
		
		CodeTypeParameter[] ConvertTypeParameters(IEnumerable<TypeParameterDeclaration> typeParameters, IEnumerable<Constraint> constraints)
		{
			List<CodeTypeParameter> result = new List<CodeTypeParameter>();
			foreach (TypeParameterDeclaration tpDecl in typeParameters) {
				CodeTypeParameter tp = new CodeTypeParameter(tpDecl.Name);
				tp.CustomAttributes.AddRange(Convert(tpDecl.Attributes));
				foreach (Constraint constraint in constraints) {
					if (constraint.TypeParameter == tp.Name) {
						foreach (AstType baseType in constraint.BaseTypes) {
							if (baseType is PrimitiveType && ((PrimitiveType)baseType).Keyword == "new") {
								tp.HasConstructorConstraint = true;
							} else {
								CodeTypeReference tr = Convert(baseType);
								if (tr != null)
									tp.Constraints.Add(tr);
							}
						}
					}
				}
				result.Add(tp);
			}
			return result.ToArray();
		}
		
		CodeObject IAstVisitor<object, CodeObject>.VisitCSharpTokenNode(CSharpTokenNode cSharpTokenNode, object data)
		{
			return null;
		}
		
		CodeObject IAstVisitor<object, CodeObject>.VisitIdentifier(Identifier identifier, object data)
		{
			return null;
		}
		
		CodeObject IAstVisitor<object, CodeObject>.VisitPatternPlaceholder(AstNode placeholder, ICSharpCode.NRefactory.PatternMatching.Pattern pattern, object data)
		{
			return null;
		}
	}
}
