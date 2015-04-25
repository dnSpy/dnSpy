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
using System.CodeDom;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ICSharpCode.NRefactory.CSharp.Refactoring;
using ICSharpCode.NRefactory.CSharp.Resolver;
using ICSharpCode.NRefactory.CSharp.TypeSystem;
using ICSharpCode.NRefactory.PatternMatching;
using ICSharpCode.NRefactory.Semantics;
using ICSharpCode.NRefactory.TypeSystem;

namespace ICSharpCode.NRefactory.CSharp
{
	/// <summary>
	/// Converts from C# AST to CodeDom.
	/// </summary>
	/// <remarks>
	/// The conversion is intended for use in the SharpDevelop forms designer.
	/// </remarks>
	public class CodeDomConvertVisitor : IAstVisitor<CodeObject>
	{
		CSharpAstResolver resolver;
		
		/// <summary>
		/// Gets/Sets whether the visitor should convert short type names into
		/// fully qualified type names.
		/// The default is <c>false</c>.
		/// </summary>
		public bool UseFullyQualifiedTypeNames { get; set; }
		
		/// <summary>
		/// Gets whether the visitor is allowed to produce snippet nodes for
		/// code that cannot be converted.
		/// The default is <c>true</c>. If this property is set to <c>false</c>,
		/// unconvertible code will throw a NotSupportedException.
		/// </summary>
		public bool AllowSnippetNodes { get; set; }
		
		public CodeDomConvertVisitor()
		{
			this.AllowSnippetNodes = true;
		}
		
		/// <summary>
		/// Converts a syntax tree to CodeDom.
		/// </summary>
		/// <param name="syntaxTree">The input syntax tree.</param>
		/// <param name="compilation">The current compilation.</param>
		/// <param name="unresolvedFile">CSharpUnresolvedFile, used for resolving.</param>
		/// <returns>Converted CodeCompileUnit</returns>
		/// <remarks>
		/// This conversion process requires a resolver because it needs to distinguish field/property/event references etc.
		/// </remarks>
		public CodeCompileUnit Convert(ICompilation compilation, SyntaxTree syntaxTree, CSharpUnresolvedFile unresolvedFile)
		{
			if (syntaxTree == null)
				throw new ArgumentNullException("syntaxTree");
			if (compilation == null)
				throw new ArgumentNullException("compilation");
			
			CSharpAstResolver resolver = new CSharpAstResolver(compilation, syntaxTree, unresolvedFile);
			return (CodeCompileUnit)Convert(syntaxTree, resolver);
		}
		
		/// <summary>
		/// Converts a C# AST node to CodeDom.
		/// </summary>
		/// <param name="node">The input node.</param>
		/// <param name="resolver">The AST resolver.</param>
		/// <returns>The node converted into CodeDom</returns>
		/// <remarks>
		/// This conversion process requires a resolver because it needs to distinguish field/property/event references etc.
		/// </remarks>
		public CodeObject Convert(AstNode node, CSharpAstResolver resolver)
		{
			if (node == null)
				throw new ArgumentNullException("node");
			if (resolver == null)
				throw new ArgumentNullException("resolver");
			try {
				this.resolver = resolver;
				return node.AcceptVisitor(this);
			} finally {
				this.resolver = null;
			}
		}
		
		ResolveResult Resolve(AstNode node)
		{
			if (resolver == null)
				return ErrorResolveResult.UnknownError;
			else
				return resolver.Resolve(node);
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
		
		public CodeTypeReference Convert(IType type)
		{
			if (type.Kind == TypeKind.Array) {
				ArrayType a = (ArrayType)type;
				return new CodeTypeReference(Convert(a.ElementType), a.Dimensions);
			} else if (type is ParameterizedType) {
				var pt = (ParameterizedType)type;
				return new CodeTypeReference(pt.GetDefinition().ReflectionName, pt.TypeArguments.Select(Convert).ToArray());
			} else {
				return new CodeTypeReference(type.ReflectionName);
			}
		}
		
		CodeStatement Convert(Statement stmt)
		{
			return (CodeStatement)stmt.AcceptVisitor(this);
		}
		
		CodeStatement[] ConvertBlock(BlockStatement block)
		{
			List<CodeStatement> result = new List<CodeStatement>();
			foreach (Statement stmt in block.Statements) {
				if (stmt is EmptyStatement)
					continue;
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
			} else if (embeddedStatement is EmptyStatement) {
				return new CodeStatement[0];
			}
			CodeStatement s = Convert(embeddedStatement);
			if (s != null)
				return new CodeStatement[] { s };
			else
				return new CodeStatement[0];
		}
		
		string MakeSnippet(AstNode node)
		{
			if (!AllowSnippetNodes)
				throw new NotSupportedException();
			StringWriter w = new StringWriter();
			CSharpOutputVisitor v = new CSharpOutputVisitor(w, FormattingOptionsFactory.CreateMono ());
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

		CodeObject IAstVisitor<CodeObject>.VisitNullNode(AstNode nullNode)
		{
			return null;
		}

		CodeObject IAstVisitor<CodeObject>.VisitErrorNode(AstNode errorNode)
		{
			return null;
		}

		CodeObject IAstVisitor<CodeObject>.VisitAnonymousMethodExpression(AnonymousMethodExpression anonymousMethodExpression)
		{
			return MakeSnippetExpression(anonymousMethodExpression);
		}
		
		CodeObject IAstVisitor<CodeObject>.VisitUndocumentedExpression(UndocumentedExpression undocumentedExpression)
		{
			return MakeSnippetExpression(undocumentedExpression);
		}
		
		CodeObject IAstVisitor<CodeObject>.VisitArrayCreateExpression(ArrayCreateExpression arrayCreateExpression)
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
		
		CodeObject IAstVisitor<CodeObject>.VisitArrayInitializerExpression(ArrayInitializerExpression arrayInitializerExpression)
		{
			// Array initializers should be handled by the parent node
			return MakeSnippetExpression(arrayInitializerExpression);
		}
		
		CodeObject IAstVisitor<CodeObject>.VisitAsExpression(AsExpression asExpression)
		{
			return MakeSnippetExpression(asExpression);
		}
		
		CodeObject IAstVisitor<CodeObject>.VisitAssignmentExpression(AssignmentExpression assignmentExpression)
		{
			// assignments are only supported as statements, not as expressions
			return MakeSnippetExpression(assignmentExpression);
		}
		
		CodeObject IAstVisitor<CodeObject>.VisitBaseReferenceExpression(BaseReferenceExpression baseReferenceExpression)
		{
			return new CodeBaseReferenceExpression();
		}
		
		CodeObject IAstVisitor<CodeObject>.VisitBinaryOperatorExpression(BinaryOperatorExpression binaryOperatorExpression)
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
					OperatorResolveResult rr = Resolve(binaryOperatorExpression) as OperatorResolveResult;
					if (rr != null && rr.GetChildResults().Any(cr => cr.Type.IsReferenceType == true)) {
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
		
		CodeObject IAstVisitor<CodeObject>.VisitCastExpression(CastExpression castExpression)
		{
			return new CodeCastExpression(Convert(castExpression.Type), Convert(castExpression.Expression));
		}
		
		CodeObject IAstVisitor<CodeObject>.VisitCheckedExpression(CheckedExpression checkedExpression)
		{
			return MakeSnippetExpression(checkedExpression);
		}
		
		CodeObject IAstVisitor<CodeObject>.VisitConditionalExpression(ConditionalExpression conditionalExpression)
		{
			return MakeSnippetExpression(conditionalExpression);
		}
		
		CodeObject IAstVisitor<CodeObject>.VisitDefaultValueExpression(DefaultValueExpression defaultValueExpression)
		{
			return new CodeDefaultValueExpression(Convert(defaultValueExpression.Type));
		}
		
		CodeObject IAstVisitor<CodeObject>.VisitDirectionExpression(DirectionExpression directionExpression)
		{
			System.CodeDom.FieldDirection direction;
			if (directionExpression.FieldDirection == FieldDirection.Out) {
				direction = System.CodeDom.FieldDirection.Out;
			} else {
				direction = System.CodeDom.FieldDirection.Ref;
			}
			return new CodeDirectionExpression(direction, Convert(directionExpression.Expression));
		}
		
		CodeObject IAstVisitor<CodeObject>.VisitIdentifierExpression(IdentifierExpression identifierExpression)
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
				if (UseFullyQualifiedTypeNames) {
					typeRef = Convert(trr.Type);
				} else {
					typeRef = new CodeTypeReference(identifierExpression.Identifier);
					typeRef.TypeArguments.AddRange(Convert(identifierExpression.TypeArguments));
				}
				return new CodeTypeReferenceExpression(typeRef);
			}
			MethodGroupResolveResult mgrr = rr as MethodGroupResolveResult;
			if (mgrr != null || identifierExpression.TypeArguments.Any()) {
				return new CodeMethodReferenceExpression(new CodeThisReferenceExpression(), identifierExpression.Identifier, Convert(identifierExpression.TypeArguments));
			}
			return new CodeVariableReferenceExpression(identifierExpression.Identifier);
		}
		
		CodeObject IAstVisitor<CodeObject>.VisitIndexerExpression(IndexerExpression indexerExpression)
		{
			if (Resolve(indexerExpression) is ArrayAccessResolveResult)
				return new CodeArrayIndexerExpression(Convert(indexerExpression.Target), Convert(indexerExpression.Arguments));
			else
				return new CodeIndexerExpression(Convert(indexerExpression.Target), Convert(indexerExpression.Arguments));
		}
		
		CodeObject IAstVisitor<CodeObject>.VisitInvocationExpression(InvocationExpression invocationExpression)
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
		
		CodeObject IAstVisitor<CodeObject>.VisitIsExpression(IsExpression isExpression)
		{
			return MakeSnippetExpression(isExpression);
		}
		
		CodeObject IAstVisitor<CodeObject>.VisitLambdaExpression(LambdaExpression lambdaExpression)
		{
			return MakeSnippetExpression(lambdaExpression);
		}
		
		CodeObject IAstVisitor<CodeObject>.VisitMemberReferenceExpression(MemberReferenceExpression memberReferenceExpression)
		{
			CodeExpression target = Convert(memberReferenceExpression.Target);
			ResolveResult rr = Resolve(memberReferenceExpression);
			MemberResolveResult mrr = rr as MemberResolveResult;
			TypeResolveResult trr = rr as TypeResolveResult;
			if (mrr != null) {
				return HandleMemberReference(target, memberReferenceExpression.MemberName, memberReferenceExpression.TypeArguments, mrr);
			} else if (trr != null) {
				return new CodeTypeReferenceExpression(Convert(trr.Type));
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
		
		CodeObject IAstVisitor<CodeObject>.VisitNamedArgumentExpression(NamedArgumentExpression namedArgumentExpression)
		{
			return MakeSnippetExpression(namedArgumentExpression);
		}
		
		CodeObject IAstVisitor<CodeObject>.VisitNamedExpression(NamedExpression namedExpression)
		{
			return MakeSnippetExpression(namedExpression);
		}
		
		CodeObject IAstVisitor<CodeObject>.VisitNullReferenceExpression(NullReferenceExpression nullReferenceExpression)
		{
			return new CodePrimitiveExpression(null);
		}
		
		CodeObject IAstVisitor<CodeObject>.VisitObjectCreateExpression(ObjectCreateExpression objectCreateExpression)
		{
			if (!objectCreateExpression.Initializer.IsNull)
				return MakeSnippetExpression(objectCreateExpression);
			return new CodeObjectCreateExpression(Convert(objectCreateExpression.Type), Convert(objectCreateExpression.Arguments));
		}
		
		CodeObject IAstVisitor<CodeObject>.VisitAnonymousTypeCreateExpression(AnonymousTypeCreateExpression anonymousTypeCreateExpression)
		{
			return MakeSnippetExpression(anonymousTypeCreateExpression);
		}
		
		CodeObject IAstVisitor<CodeObject>.VisitParenthesizedExpression(ParenthesizedExpression parenthesizedExpression)
		{
			// CodeDom generators will insert parentheses where necessary
			return Convert(parenthesizedExpression.Expression);
		}
		
		CodeObject IAstVisitor<CodeObject>.VisitPointerReferenceExpression(PointerReferenceExpression pointerReferenceExpression)
		{
			return MakeSnippetExpression(pointerReferenceExpression);
		}
		
		CodeObject IAstVisitor<CodeObject>.VisitPrimitiveExpression(PrimitiveExpression primitiveExpression)
		{
			return new CodePrimitiveExpression(primitiveExpression.Value);
		}
		
		CodeObject IAstVisitor<CodeObject>.VisitSizeOfExpression(SizeOfExpression sizeOfExpression)
		{
			return MakeSnippetExpression(sizeOfExpression);
		}
		
		CodeObject IAstVisitor<CodeObject>.VisitStackAllocExpression(StackAllocExpression stackAllocExpression)
		{
			return MakeSnippetExpression(stackAllocExpression);
		}
		
		CodeObject IAstVisitor<CodeObject>.VisitThisReferenceExpression(ThisReferenceExpression thisReferenceExpression)
		{
			return new CodeThisReferenceExpression();
		}
		
		CodeObject IAstVisitor<CodeObject>.VisitTypeOfExpression(TypeOfExpression typeOfExpression)
		{
			return new CodeTypeOfExpression(Convert(typeOfExpression.Type));
		}
		
		CodeObject IAstVisitor<CodeObject>.VisitTypeReferenceExpression(TypeReferenceExpression typeReferenceExpression)
		{
			return new CodeTypeReferenceExpression(Convert(typeReferenceExpression.Type));
		}
		
		CodeObject IAstVisitor<CodeObject>.VisitUnaryOperatorExpression(UnaryOperatorExpression unaryOperatorExpression)
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
		
		CodeObject IAstVisitor<CodeObject>.VisitUncheckedExpression(UncheckedExpression uncheckedExpression)
		{
			return MakeSnippetExpression(uncheckedExpression);
		}
		
		CodeObject IAstVisitor<CodeObject>.VisitQueryExpression(QueryExpression queryExpression)
		{
			return MakeSnippetExpression(queryExpression);
		}
		
		CodeObject IAstVisitor<CodeObject>.VisitQueryContinuationClause(QueryContinuationClause queryContinuationClause)
		{
			throw new NotSupportedException();
		}
		
		CodeObject IAstVisitor<CodeObject>.VisitQueryFromClause(QueryFromClause queryFromClause)
		{
			throw new NotSupportedException();
		}
		
		CodeObject IAstVisitor<CodeObject>.VisitQueryLetClause(QueryLetClause queryLetClause)
		{
			throw new NotSupportedException();
		}
		
		CodeObject IAstVisitor<CodeObject>.VisitQueryWhereClause(QueryWhereClause queryWhereClause)
		{
			throw new NotSupportedException();
		}
		
		CodeObject IAstVisitor<CodeObject>.VisitQueryJoinClause(QueryJoinClause queryJoinClause)
		{
			throw new NotSupportedException();
		}
		
		CodeObject IAstVisitor<CodeObject>.VisitQueryOrderClause(QueryOrderClause queryOrderClause)
		{
			throw new NotSupportedException();
		}
		
		CodeObject IAstVisitor<CodeObject>.VisitQueryOrdering(QueryOrdering queryOrdering)
		{
			throw new NotSupportedException();
		}
		
		CodeObject IAstVisitor<CodeObject>.VisitQuerySelectClause(QuerySelectClause querySelectClause)
		{
			throw new NotSupportedException();
		}
		
		CodeObject IAstVisitor<CodeObject>.VisitQueryGroupClause(QueryGroupClause queryGroupClause)
		{
			throw new NotSupportedException();
		}
		
		CodeObject IAstVisitor<CodeObject>.VisitAttribute(Attribute attribute)
		{
			throw new NotSupportedException();
		}
		
		CodeObject IAstVisitor<CodeObject>.VisitAttributeSection(AttributeSection attributeSection)
		{
			throw new NotSupportedException();
		}
		
		CodeAttributeDeclaration Convert(Attribute attribute)
		{
			var attr = new CodeAttributeDeclaration(Convert(attribute.Type));
			foreach (Expression expr in attribute.Arguments) {
				NamedExpression ne = expr as NamedExpression;
				if (ne != null)
					attr.Arguments.Add(new CodeAttributeArgument(ne.Name, Convert(ne.Expression)));
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
		
		CodeObject IAstVisitor<CodeObject>.VisitDelegateDeclaration(DelegateDeclaration delegateDeclaration)
		{
			CodeTypeDelegate d = new CodeTypeDelegate(delegateDeclaration.Name);
			d.Attributes = ConvertMemberAttributes(delegateDeclaration.Modifiers, SymbolKind.TypeDefinition);
			d.CustomAttributes.AddRange(Convert(delegateDeclaration.Attributes));
			d.ReturnType = Convert(delegateDeclaration.ReturnType);
			d.Parameters.AddRange(Convert(delegateDeclaration.Parameters));
			d.TypeParameters.AddRange(ConvertTypeParameters(delegateDeclaration.TypeParameters, delegateDeclaration.Constraints));
			return d;
		}
		
		MemberAttributes ConvertMemberAttributes(Modifiers modifiers, SymbolKind symbolKind)
		{
			MemberAttributes a = 0;
			if ((modifiers & Modifiers.Abstract) != 0)
				a |= MemberAttributes.Abstract;
			if ((modifiers & Modifiers.Sealed) != 0)
				a |= MemberAttributes.Final;
			if (symbolKind != SymbolKind.TypeDefinition && (modifiers & (Modifiers.Abstract | Modifiers.Override | Modifiers.Virtual)) == 0)
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
		
		CodeObject IAstVisitor<CodeObject>.VisitNamespaceDeclaration(NamespaceDeclaration namespaceDeclaration)
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
		
		CodeObject IAstVisitor<CodeObject>.VisitTypeDeclaration(TypeDeclaration typeDeclaration)
		{
			//bool isNestedType = typeStack.Count > 0;
			CodeTypeDeclaration typeDecl = new CodeTypeDeclaration(typeDeclaration.Name);
			typeDecl.Attributes = ConvertMemberAttributes(typeDeclaration.Modifiers, SymbolKind.TypeDefinition);
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
		
		CodeObject IAstVisitor<CodeObject>.VisitUsingAliasDeclaration(UsingAliasDeclaration usingAliasDeclaration)
		{
			return new CodeSnippetTypeMember(MakeSnippet(usingAliasDeclaration));
		}
		
		CodeObject IAstVisitor<CodeObject>.VisitUsingDeclaration(UsingDeclaration usingDeclaration)
		{
			return new CodeNamespaceImport(usingDeclaration.Namespace);
		}
		
		CodeObject IAstVisitor<CodeObject>.VisitExternAliasDeclaration(ExternAliasDeclaration externAliasDeclaration)
		{
			return new CodeSnippetTypeMember(MakeSnippet(externAliasDeclaration));
		}
		
		CodeObject IAstVisitor<CodeObject>.VisitBlockStatement(BlockStatement blockStatement)
		{
			return new CodeConditionStatement(new CodePrimitiveExpression(true), ConvertBlock(blockStatement));
		}
		
		CodeObject IAstVisitor<CodeObject>.VisitBreakStatement(BreakStatement breakStatement)
		{
			return MakeSnippetStatement(breakStatement);
		}
		
		CodeObject IAstVisitor<CodeObject>.VisitCheckedStatement(CheckedStatement checkedStatement)
		{
			return MakeSnippetStatement(checkedStatement);
		}
		
		CodeObject IAstVisitor<CodeObject>.VisitContinueStatement(ContinueStatement continueStatement)
		{
			return MakeSnippetStatement(continueStatement);
		}
		
		CodeObject IAstVisitor<CodeObject>.VisitDoWhileStatement(DoWhileStatement doWhileStatement)
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
		
		CodeObject IAstVisitor<CodeObject>.VisitEmptyStatement(EmptyStatement emptyStatement)
		{
			return EmptyStatement();
		}
		
		CodeStatement EmptyStatement()
		{
			return new CodeExpressionStatement(new CodeObjectCreateExpression(new CodeTypeReference(typeof(object))));
		}
		
		CodeObject IAstVisitor<CodeObject>.VisitExpressionStatement(ExpressionStatement expressionStatement)
		{
			AssignmentExpression assignment = expressionStatement.Expression as AssignmentExpression;
			if (assignment != null && assignment.Operator == AssignmentOperatorType.Assign) {
				return new CodeAssignStatement(Convert(assignment.Left), Convert(assignment.Right));
			} else if (assignment != null && CanBeDuplicatedForCompoundAssignment(assignment.Left)) {
				CodeBinaryOperatorType op;
				switch (assignment.Operator) {
					case AssignmentOperatorType.Add:
						op = CodeBinaryOperatorType.Add;
						break;
					case AssignmentOperatorType.Subtract:
						op = CodeBinaryOperatorType.Subtract;
						break;
					case AssignmentOperatorType.Multiply:
						op = CodeBinaryOperatorType.Multiply;
						break;
					case AssignmentOperatorType.Divide:
						op = CodeBinaryOperatorType.Divide;
						break;
					case AssignmentOperatorType.Modulus:
						op = CodeBinaryOperatorType.Modulus;
						break;
					case AssignmentOperatorType.BitwiseAnd:
						op = CodeBinaryOperatorType.BitwiseAnd;
						break;
					case AssignmentOperatorType.BitwiseOr:
						op = CodeBinaryOperatorType.BitwiseOr;
						break;
					default:
						return MakeSnippetStatement(expressionStatement);
				}
				var cboe = new CodeBinaryOperatorExpression(Convert(assignment.Left), op, Convert(assignment.Right));
				return new CodeAssignStatement(Convert(assignment.Left), cboe);
			}
			UnaryOperatorExpression unary = expressionStatement.Expression as UnaryOperatorExpression;
			if (unary != null && CanBeDuplicatedForCompoundAssignment(unary.Expression)) {
				var op = unary.Operator;
				if (op == UnaryOperatorType.Increment || op == UnaryOperatorType.PostIncrement) {
					var cboe = new CodeBinaryOperatorExpression(Convert(unary.Expression), CodeBinaryOperatorType.Add, new CodePrimitiveExpression(1));
					return new CodeAssignStatement(Convert(unary.Expression), cboe);
				} else if (op == UnaryOperatorType.Decrement || op == UnaryOperatorType.PostDecrement) {
					var cboe = new CodeBinaryOperatorExpression(Convert(unary.Expression), CodeBinaryOperatorType.Subtract, new CodePrimitiveExpression(1));
					return new CodeAssignStatement(Convert(unary.Expression), cboe);
				}
			}
			if (assignment != null && assignment.Operator == AssignmentOperatorType.Add) {
				var rr = Resolve(assignment.Left);
				if (!rr.IsError && rr.Type.Kind == TypeKind.Delegate) {
					var expr = (MemberReferenceExpression)assignment.Left;
					var memberRef = (CodeEventReferenceExpression)HandleMemberReference(Convert(expr.Target), expr.MemberName, expr.TypeArguments, (MemberResolveResult)rr);
					return new CodeAttachEventStatement(memberRef, Convert(assignment.Right));
				}
			}
			return new CodeExpressionStatement(Convert(expressionStatement.Expression));
		}
		
		bool CanBeDuplicatedForCompoundAssignment(Expression expr)
		{
			return expr is IdentifierExpression;
		}
		
		CodeObject IAstVisitor<CodeObject>.VisitFixedStatement(FixedStatement fixedStatement)
		{
			return MakeSnippetStatement(fixedStatement);
		}
		
		CodeObject IAstVisitor<CodeObject>.VisitForeachStatement(ForeachStatement foreachStatement)
		{
			return MakeSnippetStatement(foreachStatement);
		}
		
		CodeObject IAstVisitor<CodeObject>.VisitForStatement(ForStatement forStatement)
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
		
		CodeObject IAstVisitor<CodeObject>.VisitGotoCaseStatement(GotoCaseStatement gotoCaseStatement)
		{
			return MakeSnippetStatement(gotoCaseStatement);
		}
		
		CodeObject IAstVisitor<CodeObject>.VisitGotoDefaultStatement(GotoDefaultStatement gotoDefaultStatement)
		{
			return MakeSnippetStatement(gotoDefaultStatement);
		}
		
		CodeObject IAstVisitor<CodeObject>.VisitGotoStatement(GotoStatement gotoStatement)
		{
			return new CodeGotoStatement(gotoStatement.Label);
		}
		
		CodeObject IAstVisitor<CodeObject>.VisitIfElseStatement(IfElseStatement ifElseStatement)
		{
			return new CodeConditionStatement(
				Convert(ifElseStatement.Condition),
				ConvertEmbeddedStatement(ifElseStatement.TrueStatement),
				ConvertEmbeddedStatement(ifElseStatement.FalseStatement));
		}
		
		CodeObject IAstVisitor<CodeObject>.VisitLabelStatement(LabelStatement labelStatement)
		{
			return new CodeLabeledStatement(labelStatement.Label);
		}
		
		CodeObject IAstVisitor<CodeObject>.VisitLockStatement(LockStatement lockStatement)
		{
			return MakeSnippetStatement(lockStatement);
		}
		
		CodeObject IAstVisitor<CodeObject>.VisitReturnStatement(ReturnStatement returnStatement)
		{
			return new CodeMethodReturnStatement(Convert(returnStatement.Expression));
		}
		
		CodeObject IAstVisitor<CodeObject>.VisitSwitchStatement(SwitchStatement switchStatement)
		{
			return MakeSnippetStatement(switchStatement);
		}
		
		CodeObject IAstVisitor<CodeObject>.VisitSwitchSection(SwitchSection switchSection)
		{
			throw new NotSupportedException();
		}
		
		CodeObject IAstVisitor<CodeObject>.VisitCaseLabel(CaseLabel caseLabel)
		{
			throw new NotSupportedException();
		}
		
		CodeObject IAstVisitor<CodeObject>.VisitThrowStatement(ThrowStatement throwStatement)
		{
			return new CodeThrowExceptionStatement(Convert(throwStatement.Expression));
		}
		
		CodeObject IAstVisitor<CodeObject>.VisitTryCatchStatement(TryCatchStatement tryCatchStatement)
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
		
		CodeObject IAstVisitor<CodeObject>.VisitCatchClause(CatchClause catchClause)
		{
			throw new NotSupportedException();
		}
		
		CodeObject IAstVisitor<CodeObject>.VisitUncheckedStatement(UncheckedStatement uncheckedStatement)
		{
			return MakeSnippetStatement(uncheckedStatement);
		}
		
		CodeObject IAstVisitor<CodeObject>.VisitUnsafeStatement(UnsafeStatement unsafeStatement)
		{
			return MakeSnippetStatement(unsafeStatement);
		}
		
		CodeObject IAstVisitor<CodeObject>.VisitUsingStatement(UsingStatement usingStatement)
		{
			return MakeSnippetStatement(usingStatement);
		}
		
		CodeObject IAstVisitor<CodeObject>.VisitVariableDeclarationStatement(VariableDeclarationStatement variableDeclarationStatement)
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
		
		CodeObject IAstVisitor<CodeObject>.VisitWhileStatement(WhileStatement whileStatement)
		{
			return new CodeIterationStatement(EmptyStatement(), Convert(whileStatement.Condition), EmptyStatement(), ConvertEmbeddedStatement(whileStatement.EmbeddedStatement));
		}
		
		CodeObject IAstVisitor<CodeObject>.VisitYieldBreakStatement(YieldBreakStatement yieldBreakStatement)
		{
			return MakeSnippetStatement(yieldBreakStatement);
		}
		
		CodeObject IAstVisitor<CodeObject>.VisitYieldReturnStatement(YieldReturnStatement yieldStatement)
		{
			return MakeSnippetStatement(yieldStatement);
		}
		
		CodeObject IAstVisitor<CodeObject>.VisitAccessor(Accessor accessor)
		{
			throw new NotSupportedException();
		}
		
		CodeObject IAstVisitor<CodeObject>.VisitConstructorDeclaration(ConstructorDeclaration constructorDeclaration)
		{
			CodeConstructor ctor = new CodeConstructor();
			ctor.Attributes = ConvertMemberAttributes(constructorDeclaration.Modifiers, SymbolKind.Constructor);
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
		
		CodeObject IAstVisitor<CodeObject>.VisitConstructorInitializer(ConstructorInitializer constructorInitializer)
		{
			throw new NotSupportedException();
		}
		
		CodeObject IAstVisitor<CodeObject>.VisitDestructorDeclaration(DestructorDeclaration destructorDeclaration)
		{
			return new CodeSnippetTypeMember(MakeSnippet(destructorDeclaration));
		}
		
		CodeObject IAstVisitor<CodeObject>.VisitEnumMemberDeclaration(EnumMemberDeclaration enumMemberDeclaration)
		{
			TypeDeclaration td = enumMemberDeclaration.Parent as TypeDeclaration;
			CodeMemberField f = new CodeMemberField(td != null ? td.Name : "Enum", enumMemberDeclaration.Name);
			f.Attributes = MemberAttributes.Public | MemberAttributes.Static;
			f.CustomAttributes.AddRange(Convert(enumMemberDeclaration.Attributes));
			f.InitExpression = Convert(enumMemberDeclaration.Initializer);
			return f;
		}
		
		CodeObject IAstVisitor<CodeObject>.VisitEventDeclaration(EventDeclaration eventDeclaration)
		{
			foreach (VariableInitializer vi in eventDeclaration.Variables) {
				if (!vi.Initializer.IsNull) {
					AddTypeMember(new CodeSnippetTypeMember(MakeSnippet(eventDeclaration)));
					continue;
				}
				
				CodeMemberEvent e = new CodeMemberEvent();
				e.Attributes = ConvertMemberAttributes(eventDeclaration.Modifiers, SymbolKind.Event);
				e.CustomAttributes.AddRange(Convert(eventDeclaration.Attributes));
				e.Name = vi.Name;
				e.Type = Convert(eventDeclaration.ReturnType);
				AddTypeMember(e);
			}
			return null;
		}
		
		CodeObject IAstVisitor<CodeObject>.VisitCustomEventDeclaration(CustomEventDeclaration customEventDeclaration)
		{
			return new CodeSnippetTypeMember(MakeSnippet(customEventDeclaration));
		}
		
		CodeObject IAstVisitor<CodeObject>.VisitFieldDeclaration(FieldDeclaration fieldDeclaration)
		{
			foreach (VariableInitializer vi in fieldDeclaration.Variables) {
				CodeMemberField f = new CodeMemberField(Convert(fieldDeclaration.ReturnType), vi.Name);
				f.Attributes = ConvertMemberAttributes(fieldDeclaration.Modifiers, SymbolKind.Field);
				f.CustomAttributes.AddRange(Convert(fieldDeclaration.Attributes));
				f.InitExpression = ConvertVariableInitializer(vi.Initializer, fieldDeclaration.ReturnType);
				AddTypeMember(f);
			}
			return null;
		}
		
		CodeObject IAstVisitor<CodeObject>.VisitIndexerDeclaration(IndexerDeclaration indexerDeclaration)
		{
			CodeMemberProperty p = new CodeMemberProperty();
			p.Attributes = ConvertMemberAttributes(indexerDeclaration.Modifiers, SymbolKind.Indexer);
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
		
		CodeObject IAstVisitor<CodeObject>.VisitMethodDeclaration(MethodDeclaration methodDeclaration)
		{
			CodeMemberMethod m = new CodeMemberMethod();
			m.Attributes = ConvertMemberAttributes(methodDeclaration.Modifiers, SymbolKind.Method);
			
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
		
		CodeObject IAstVisitor<CodeObject>.VisitOperatorDeclaration(OperatorDeclaration operatorDeclaration)
		{
			CodeMemberMethod m = new CodeMemberMethod();
			m.Attributes = ConvertMemberAttributes(operatorDeclaration.Modifiers, SymbolKind.Method);
			
			m.CustomAttributes.AddRange(Convert(operatorDeclaration.Attributes.Where(a => a.AttributeTarget != "return")));
			m.ReturnTypeCustomAttributes.AddRange(Convert(operatorDeclaration.Attributes.Where(a => a.AttributeTarget == "return")));
			
			m.ReturnType = Convert(operatorDeclaration.ReturnType);
			m.Name = operatorDeclaration.Name;
			m.Parameters.AddRange(Convert(operatorDeclaration.Parameters));
			
			m.Statements.AddRange(ConvertBlock(operatorDeclaration.Body));
			return m;
		}
		
		CodeObject IAstVisitor<CodeObject>.VisitParameterDeclaration(ParameterDeclaration parameterDeclaration)
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
		
		CodeObject IAstVisitor<CodeObject>.VisitPropertyDeclaration(PropertyDeclaration propertyDeclaration)
		{
			CodeMemberProperty p = new CodeMemberProperty();
			p.Attributes = ConvertMemberAttributes(propertyDeclaration.Modifiers, SymbolKind.Property);
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
		
		CodeObject IAstVisitor<CodeObject>.VisitVariableInitializer(VariableInitializer variableInitializer)
		{
			throw new NotSupportedException(); // should be handled by the parent node
		}
		
		CodeObject IAstVisitor<CodeObject>.VisitFixedFieldDeclaration(FixedFieldDeclaration fixedFieldDeclaration)
		{
			return new CodeSnippetTypeMember(MakeSnippet(fixedFieldDeclaration));
		}
		
		CodeObject IAstVisitor<CodeObject>.VisitFixedVariableInitializer(FixedVariableInitializer fixedVariableInitializer)
		{
			throw new NotSupportedException(); // should be handled by the parent node
		}
		
		CodeObject IAstVisitor<CodeObject>.VisitSyntaxTree(SyntaxTree syntaxTree)
		{
			CodeCompileUnit cu = new CodeCompileUnit();
			var globalImports = new List<CodeNamespaceImport> ();
			foreach (AstNode node in syntaxTree.Children) {
				CodeObject o = node.AcceptVisitor(this);
				
				CodeNamespace ns = o as CodeNamespace;
				if (ns != null) {
					cu.Namespaces.Add(ns);
				}
				CodeTypeDeclaration td = o as CodeTypeDeclaration;
				if (td != null) {
					cu.Namespaces.Add(new CodeNamespace() { Types = { td } });
				}
				
				var import = o as CodeNamespaceImport;
				if (import != null)
					globalImports.Add (import);
			}
			foreach (var gi in globalImports) {
				for (int j = 0; j < cu.Namespaces.Count; j++) {
					var cn = cu.Namespaces [j];
					bool found = cn.Imports
						.Cast<CodeNamespaceImport> ()
						.Any (ns => ns.Namespace == gi.Namespace);
					if (!found)
						cn.Imports.Add (gi);
				}
			}
			return cu;
		}
		
		CodeObject IAstVisitor<CodeObject>.VisitSimpleType(SimpleType simpleType)
		{
			if (UseFullyQualifiedTypeNames) {
				IType type = Resolve(simpleType).Type;
				if (type.Kind != TypeKind.Unknown)
					return Convert(type);
			}
			var tr = new CodeTypeReference(simpleType.Identifier);
			tr.TypeArguments.AddRange(Convert(simpleType.TypeArguments));
			return tr;
		}
		
		CodeObject IAstVisitor<CodeObject>.VisitMemberType(MemberType memberType)
		{
			if (memberType.IsDoubleColon && new SimpleType("global").IsMatch(memberType.Target)) {
				var tr = new CodeTypeReference(memberType.MemberName, CodeTypeReferenceOptions.GlobalReference);
				tr.TypeArguments.AddRange(Convert(memberType.TypeArguments));
				return tr;
			}
			if (UseFullyQualifiedTypeNames || memberType.IsDoubleColon) {
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
		
		CodeObject IAstVisitor<CodeObject>.VisitComposedType(ComposedType composedType)
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
		
		CodeObject IAstVisitor<CodeObject>.VisitArraySpecifier(ArraySpecifier arraySpecifier)
		{
			throw new NotSupportedException(); // handled by parent node
		}
		
		CodeObject IAstVisitor<CodeObject>.VisitPrimitiveType(PrimitiveType primitiveType)
		{
			KnownTypeCode typeCode = primitiveType.KnownTypeCode;
			if (typeCode != KnownTypeCode.None) {
				KnownTypeReference ktr = KnownTypeReference.Get(typeCode);
				return new CodeTypeReference(ktr.Namespace + "." + ktr.Name);
			}
			return new CodeTypeReference(primitiveType.Keyword);
		}
		
		CodeObject IAstVisitor<CodeObject>.VisitComment (Comment comment)
		{
			return new CodeComment (comment.Content, comment.CommentType == CommentType.Documentation);
		}

		CodeObject IAstVisitor<CodeObject>.VisitNewLine(NewLineNode newLineNode)
		{
			return null;
		}

		CodeObject IAstVisitor<CodeObject>.VisitWhitespace(WhitespaceNode whitespaceNode)
		{
			return null;
		}

		CodeObject IAstVisitor<CodeObject>.VisitText(TextNode textNode)
		{
			throw new NotSupportedException();
		}

		CodeObject IAstVisitor<CodeObject>.VisitPreProcessorDirective (PreProcessorDirective preProcessorDirective)
		{
			return new CodeComment ("#" + preProcessorDirective.Type.ToString ().ToLowerInvariant ());
		}
		
		CodeObject IAstVisitor<CodeObject>.VisitTypeParameterDeclaration(TypeParameterDeclaration typeParameterDeclaration)
		{
			throw new NotSupportedException(); // type parameters and constraints are handled together
		}
		
		CodeObject IAstVisitor<CodeObject>.VisitConstraint(Constraint constraint)
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
					if (constraint.TypeParameter.Identifier == tp.Name) {
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
		
		CodeObject IAstVisitor<CodeObject>.VisitCSharpTokenNode(CSharpTokenNode cSharpTokenNode)
		{
			return null;
		}
		
		CodeObject IAstVisitor<CodeObject>.VisitIdentifier(Identifier identifier)
		{
			return null;
		}
		
		CodeObject IAstVisitor<CodeObject>.VisitPatternPlaceholder(AstNode placeholder, Pattern pattern)
		{
			return null;
		}
		
		CodeObject IAstVisitor<CodeObject>.VisitDocumentationReference(DocumentationReference documentationReference)
		{
			return null;
		}
	}
}
