// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;

namespace NRefactoryASTGenerator.Ast
{
	[CustomImplementation]
	abstract class Expression : AbstractNode, INullable {}
	
	[CustomImplementation]
	class PrimitiveExpression : Expression {}
	
	enum ParameterModifiers { In }
	enum QueryExpressionPartitionType { }
	
	class ParameterDeclarationExpression : Expression {
		List<AttributeSection> attributes;
		[QuestionMarkDefault]
		string         parameterName;
		TypeReference  typeReference;
		ParameterModifiers  paramModifier;
		Expression     defaultValue;
		
		public ParameterDeclarationExpression(TypeReference typeReference, string parameterName) {}
		public ParameterDeclarationExpression(TypeReference typeReference, string parameterName, ParameterModifiers paramModifier) {}
		public ParameterDeclarationExpression(TypeReference typeReference, string parameterName, ParameterModifiers paramModifier, Expression defaultValue) {}
	}
	
	class NamedArgumentExpression : Expression {
		string     name;
		Expression expression;
		
		public NamedArgumentExpression() { }
		public NamedArgumentExpression(string name, Expression expression) {}
	}
	
	class MemberInitializerExpression : Expression {
		string     name;
		bool       isKey;
		Expression expression;
		
		public MemberInitializerExpression() { }
		public MemberInitializerExpression(string name, Expression expression) {}
	}
	
	[IncludeBoolProperty("IsAnonymousType", "return createType.IsNull || string.IsNullOrEmpty(createType.Type);")]
	class ObjectCreateExpression : Expression {
		TypeReference    createType;
		List<Expression> parameters;
		CollectionInitializerExpression objectInitializer;
		
		public ObjectCreateExpression(TypeReference createType, List<Expression> parameters) {}
	}
	
	[IncludeBoolProperty("IsImplicitlyTyped", "return createType.IsNull || string.IsNullOrEmpty(createType.Type);")]
	class ArrayCreateExpression : Expression {
		TypeReference              createType;
		List<Expression>           arguments;
		CollectionInitializerExpression arrayInitializer;
		
		public ArrayCreateExpression(TypeReference createType) {}
		public ArrayCreateExpression(TypeReference createType, List<Expression> arguments) {}
		public ArrayCreateExpression(TypeReference createType, CollectionInitializerExpression arrayInitializer) {}
	}
	
	[ImplementNullable(NullableImplementation.Shadow)]
	class CollectionInitializerExpression : Expression {
		List<Expression> createExpressions;
		
		public CollectionInitializerExpression() {}
		public CollectionInitializerExpression(List<Expression> createExpressions) {}
	}
	
	enum AssignmentOperatorType {}
	
	class AssignmentExpression : Expression {
		Expression             left;
		AssignmentOperatorType op;
		Expression             right;
		
		public AssignmentExpression(Expression left, AssignmentOperatorType op, Expression right) {}
	}
	
	class BaseReferenceExpression : Expression {}
	
	enum BinaryOperatorType {}
	
	class BinaryOperatorExpression : Expression
	{
		Expression         left;
		BinaryOperatorType op;
		Expression         right;
		
		public BinaryOperatorExpression() { }
		public BinaryOperatorExpression(Expression left, BinaryOperatorType op, Expression right) {}
	}
	
	enum CastType {}
	
	class CastExpression : Expression
	{
		TypeReference castTo;
		Expression    expression;
		CastType      castType;
		
		public CastExpression(TypeReference castTo) {}
		public CastExpression(TypeReference castTo, Expression expression, CastType castType) {}
	}
	
	class MemberReferenceExpression : Expression
	{
		Expression targetObject;
		string     memberName;
		List<TypeReference> typeArguments;
		
		public MemberReferenceExpression(Expression targetObject, string memberName) {}
	}
	
	class PointerReferenceExpression : Expression {
		Expression targetObject;
		string     memberName;
		List<TypeReference> typeArguments;
		
		public PointerReferenceExpression(Expression targetObject, string memberName) {}
	}
	
	class IdentifierExpression : Expression {
		string identifier;
		List<TypeReference> typeArguments;
		
		public IdentifierExpression(string identifier) {}
	}
	
	class InvocationExpression : Expression {
		Expression          targetObject;
		List<Expression>    arguments;
		
		public InvocationExpression(Expression targetObject) {}
		public InvocationExpression(Expression targetObject, List<Expression> arguments) {}
	}
	
	class ParenthesizedExpression : Expression {
		Expression expression;
		
		public ParenthesizedExpression(Expression expression) {}
	}
	
	class ThisReferenceExpression : Expression {}
	
	class TypeOfExpression : Expression {
		TypeReference typeReference;
		
		public TypeOfExpression(TypeReference typeReference) {}
	}
	
	class TypeReferenceExpression : Expression {
		TypeReference typeReference;
		
		public TypeReferenceExpression(TypeReference typeReference) {}
	}
	
	enum UnaryOperatorType {}
	
	class UnaryOperatorExpression : Expression {
		UnaryOperatorType op;
		Expression        expression;
		
		public UnaryOperatorExpression(UnaryOperatorType op) {}
		public UnaryOperatorExpression(Expression expression, UnaryOperatorType op) {}
	}
	
	class AnonymousMethodExpression : Expression {
		List<ParameterDeclarationExpression> parameters;
		BlockStatement body;
		bool hasParameterList;
	}
	
	[IncludeMember("public Location ExtendedEndLocation { get; set; }")]
	class LambdaExpression : Expression {
		List<ParameterDeclarationExpression> parameters;
		Statement statementBody;
		Expression expressionBody;
		TypeReference returnType;
	}
	
	class CheckedExpression : Expression {
		Expression expression;
		
		public CheckedExpression(Expression expression) {}
	}
	
	class ConditionalExpression : Expression {
		Expression condition;
		Expression trueExpression;
		Expression falseExpression;
		
		public ConditionalExpression() { }
		public ConditionalExpression(Expression condition, Expression trueExpression, Expression falseExpression) {}
	}
	
	class DefaultValueExpression : Expression {
		TypeReference typeReference;
		
		public DefaultValueExpression(TypeReference typeReference) {}
	}
	
	enum FieldDirection {}
	
	class DirectionExpression : Expression {
		FieldDirection fieldDirection;
		Expression     expression;
		
		public DirectionExpression(FieldDirection fieldDirection, Expression expression) {}
	}
	
	class IndexerExpression : Expression {
		Expression       targetObject;
		List<Expression> indexes;
		
		public IndexerExpression(Expression targetObject, List<Expression> indexes) {}
	}
	
	class SizeOfExpression : Expression {
		TypeReference typeReference;
		
		public SizeOfExpression(TypeReference typeReference) {}
	}
	
	class StackAllocExpression : Expression {
		TypeReference typeReference;
		Expression    expression;
		
		public StackAllocExpression(TypeReference typeReference, Expression expression) {}
	}
	
	class UncheckedExpression : Expression {
		Expression expression;
		
		public UncheckedExpression(Expression expression) {}
	}
	
	class AddressOfExpression : Expression {
		Expression expression;
		
		public AddressOfExpression(Expression expression) {}
	}
	
	class ClassReferenceExpression : Expression {}
	
	class TypeOfIsExpression : Expression {
		Expression    expression;
		TypeReference typeReference;
		
		public TypeOfIsExpression(Expression expression, TypeReference typeReference) {}
	}
	
	[ImplementNullable(NullableImplementation.Shadow)]
	class QueryExpression : Expression {
		
		/// <remarks>
		/// Either from or aggregate clause.
		/// </remarks>
		QueryExpressionFromClause fromClause;
		
		bool isQueryContinuation;
		
		List<QueryExpressionClause> middleClauses;
		
		/// <remarks>
		/// C# only.
		/// </remarks>
		QueryExpressionClause selectOrGroupClause;
	}
	
	class QueryExpressionVB : Expression {
		List<QueryExpressionClause> clauses;
	}
	
	[ImplementNullable]
	abstract class QueryExpressionClause : AbstractNode, INullable { }
	
	class QueryExpressionWhereClause : QueryExpressionClause {
		Expression condition;
	}
	
	class QueryExpressionLetClause : QueryExpressionClause {
		[QuestionMarkDefault]
		string identifier;
		Expression expression;
	}
	
	[ImplementNullable(NullableImplementation.Shadow)]
	class QueryExpressionFromClause : QueryExpressionClause {
		List<CollectionRangeVariable> sources;
	}

	class QueryExpressionAggregateClause : 	QueryExpressionClause {
		CollectionRangeVariable source;
		List<QueryExpressionClause> middleClauses;
		List<ExpressionRangeVariable> intoVariables;
	}
	
	[ImplementNullable]
	class ExpressionRangeVariable : AbstractNode, INullable {
		string identifier;
		Expression expression;
		TypeReference type;
	}
	
	[ImplementNullable]
	class CollectionRangeVariable : AbstractNode, INullable {
		string identifier;
		Expression expression;
		TypeReference type;
	}
	
	class QueryExpressionJoinClause : QueryExpressionClause {
		Expression onExpression;
		Expression equalsExpression;
		CollectionRangeVariable source;
		
		string intoIdentifier;
	}
	
	[ImplementNullable(NullableImplementation.Shadow)]
	class QueryExpressionJoinVBClause : QueryExpressionClause {
		CollectionRangeVariable joinVariable;
		QueryExpressionJoinVBClause subJoin;
		List<QueryExpressionJoinConditionVB> conditions;
	}

	class QueryExpressionPartitionVBClause : QueryExpressionClause {
		Expression expression;
		QueryExpressionPartitionType partitionType;
	}
	
	class QueryExpressionJoinConditionVB : AbstractNode {
		Expression leftSide;
		Expression rightSide;
	}
	
	class QueryExpressionOrderClause : QueryExpressionClause {
		List<QueryExpressionOrdering> orderings;
	}
	
	class QueryExpressionOrdering : AbstractNode {
		Expression criteria;
		QueryExpressionOrderingDirection direction;
	}
	
	enum QueryExpressionOrderingDirection {
		None, Ascending, Descending
	}
	
	class QueryExpressionSelectClause : QueryExpressionClause {
		Expression projection;
	}

	class QueryExpressionSelectVBClause : QueryExpressionClause {
		List<ExpressionRangeVariable> variables;
	}
	
	class QueryExpressionLetVBClause : QueryExpressionClause {
		List<ExpressionRangeVariable> variables;
	}
	
	class QueryExpressionDistinctClause : QueryExpressionClause {
		
	}
	
	class QueryExpressionGroupClause : QueryExpressionClause {
		Expression projection;
		Expression groupBy;
	}
	
	class QueryExpressionGroupVBClause : QueryExpressionClause  {
		List<ExpressionRangeVariable> groupVariables;
		List<ExpressionRangeVariable> byVariables;
		List<ExpressionRangeVariable> intoVariables;
	}
	
	class QueryExpressionGroupJoinVBClause : QueryExpressionClause {
		QueryExpressionJoinVBClause joinClause;
		List<ExpressionRangeVariable> intoVariables;
	}
	
	enum XmlAxisType { }
	
	class XmlMemberAccessExpression : Expression {
		Expression targetObject;
		XmlAxisType axisType;
		bool isXmlIdentifier;
		string identifier;
		
		public XmlMemberAccessExpression(Expression targetObject, XmlAxisType axisType, string identifier, bool isXmlIdentifier) {}
	}

	abstract class XmlExpression : Expression { }

	class XmlDocumentExpression : XmlExpression {
		List<XmlExpression> expressions;
	}
	
	enum XmlContentType { }
	
	class XmlContentExpression : XmlExpression {
		string content;
		XmlContentType type;
		
		public XmlContentExpression(string content, XmlContentType type) {}
	}
	
	class XmlEmbeddedExpression : XmlExpression {
		Expression inlineVBExpression;
	}

	[IncludeBoolProperty("IsExpression", "return !content.IsNull;")]
	[IncludeBoolProperty("NameIsExpression", "return !nameExpression.IsNull;")]
	[HasChildren]
	class XmlElementExpression : XmlExpression {
		Expression content;
		Expression nameExpression;
		string xmlName;
		List<XmlExpression> attributes;
	}
	
	[IncludeBoolProperty("IsLiteralValue", "return expressionValue.IsNull;")]
	class XmlAttributeExpression : XmlExpression {
		string name;
		string literalValue;
		bool useDoubleQuotes;
		Expression expressionValue;
	}
}
