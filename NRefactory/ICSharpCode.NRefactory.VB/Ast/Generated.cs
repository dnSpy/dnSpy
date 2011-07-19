//// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
//// This code is distributed under MIT X11 license (for details please see \doc\license.txt)
//using System;
//using System.Collections.Generic;
//
//namespace ICSharpCode.NRefactory.VB.Ast {
//	public class AddHandlerStatement : Statement {
//		
//		Expression eventExpression;
//		
//		Expression handlerExpression;
//		
//		public Expression EventExpression {
//			get {
//				return eventExpression;
//			}
//			set {
//				eventExpression = value ?? Expression.Null;
//				if (!eventExpression.IsNull) eventExpression.Parent = this;
//			}
//		}
//		
//		public Expression HandlerExpression {
//			get {
//				return handlerExpression;
//			}
//			set {
//				handlerExpression = value ?? Expression.Null;
//				if (!handlerExpression.IsNull) handlerExpression.Parent = this;
//			}
//		}
//		
//		public AddHandlerStatement(Expression eventExpression, Expression handlerExpression) {
//			EventExpression = eventExpression;
//			HandlerExpression = handlerExpression;
//		}
//		
//		public override object AcceptVisitor(IAstVisitor visitor, object data) {
//			return visitor.VisitAddHandlerStatement(this, data);
//		}
//		
//		public override string ToString() {
//			return string.Format("[AddHandlerStatement EventExpression={0} HandlerExpression={1}]", EventExpression, HandlerExpression);
//		}
//	}
//	
//	public class AddressOfExpression : Expression {
//		
//		Expression expression;
//		
//		public Expression Expression {
//			get {
//				return expression;
//			}
//			set {
//				expression = value ?? Expression.Null;
//				if (!expression.IsNull) expression.Parent = this;
//			}
//		}
//		
//		public AddressOfExpression(Expression expression) {
//			Expression = expression;
//		}
//		
//		public override object AcceptVisitor(IAstVisitor visitor, object data) {
//			return visitor.VisitAddressOfExpression(this, data);
//		}
//		
//		public override string ToString() {
//			return string.Format("[AddressOfExpression Expression={0}]", Expression);
//		}
//	}
//	
//	public class ArrayCreateExpression : Expression {
//		
//		TypeReference createType;
//		
//		List<Expression> arguments;
//		
//		CollectionInitializerExpression arrayInitializer;
//		
//		public TypeReference CreateType {
//			get {
//				return createType;
//			}
//			set {
//				createType = value ?? TypeReference.Null;
//				if (!createType.IsNull) createType.Parent = this;
//			}
//		}
//		
//		public List<Expression> Arguments {
//			get {
//				return arguments;
//			}
//			set {
//				arguments = value ?? new List<Expression>();
//			}
//		}
//		
//		public CollectionInitializerExpression ArrayInitializer {
//			get {
//				return arrayInitializer;
//			}
//			set {
//				arrayInitializer = value ?? CollectionInitializerExpression.Null;
//				if (!arrayInitializer.IsNull) arrayInitializer.Parent = this;
//			}
//		}
//		
//		public ArrayCreateExpression(TypeReference createType) {
//			CreateType = createType;
//			arguments = new List<Expression>();
//			arrayInitializer = CollectionInitializerExpression.Null;
//		}
//		
//		public ArrayCreateExpression(TypeReference createType, List<Expression> arguments) {
//			CreateType = createType;
//			Arguments = arguments;
//			arrayInitializer = CollectionInitializerExpression.Null;
//		}
//		
//		public ArrayCreateExpression(TypeReference createType, CollectionInitializerExpression arrayInitializer) {
//			CreateType = createType;
//			ArrayInitializer = arrayInitializer;
//			arguments = new List<Expression>();
//		}
//		
//		public bool IsImplicitlyTyped {
//			get {
//				return createType.IsNull || string.IsNullOrEmpty(createType.Type);
//			}
//		}
//		
//		public override object AcceptVisitor(IAstVisitor visitor, object data) {
//			return visitor.VisitArrayCreateExpression(this, data);
//		}
//		
//		public override string ToString() {
//			return string.Format("[ArrayCreateExpression CreateType={0} Arguments={1} ArrayInitializer={2}]", CreateType, GetCollectionString(Arguments), ArrayInitializer);
//		}
//	}
//	
//	public class AssignmentExpression : Expression {
//		
//		Expression left;
//		
//		AssignmentOperatorType op;
//		
//		Expression right;
//		
//		public Expression Left {
//			get {
//				return left;
//			}
//			set {
//				left = value ?? Expression.Null;
//				if (!left.IsNull) left.Parent = this;
//			}
//		}
//		
//		public AssignmentOperatorType Op {
//			get {
//				return op;
//			}
//			set {
//				op = value;
//			}
//		}
//		
//		public Expression Right {
//			get {
//				return right;
//			}
//			set {
//				right = value ?? Expression.Null;
//				if (!right.IsNull) right.Parent = this;
//			}
//		}
//		
//		public AssignmentExpression(Expression left, AssignmentOperatorType op, Expression right) {
//			Left = left;
//			Op = op;
//			Right = right;
//		}
//		
//		public override object AcceptVisitor(IAstVisitor visitor, object data) {
//			return visitor.VisitAssignmentExpression(this, data);
//		}
//		
//		public override string ToString() {
//			return string.Format("[AssignmentExpression Left={0} Op={1} Right={2}]", Left, Op, Right);
//		}
//	}
//	
//	public abstract class AttributedNode : AbstractNode {
//		
//		List<AttributeSection> attributes;
//		
//		Modifiers modifier;
//		
//		public List<AttributeSection> Attributes {
//			get {
//				return attributes;
//			}
//			set {
//				attributes = value ?? new List<AttributeSection>();
//			}
//		}
//		
//		public Modifiers Modifier {
//			get {
//				return modifier;
//			}
//			set {
//				modifier = value;
//			}
//		}
//		
//		protected AttributedNode() {
//			attributes = new List<AttributeSection>();
//		}
//		
//		protected AttributedNode(List<AttributeSection> attributes) {
//			Attributes = attributes;
//		}
//		
//		protected AttributedNode(Modifiers modifier, List<AttributeSection> attributes) {
//			Modifier = modifier;
//			Attributes = attributes;
//		}
//	}
//	
//	public class AttributeSection : AbstractNode {
//		
//		string attributeTarget;
//		
//		List<ICSharpCode.NRefactory.VB.Ast.Attribute> attributes;
//		
//		public string AttributeTarget {
//			get {
//				return attributeTarget;
//			}
//			set {
//				attributeTarget = value ?? "";
//			}
//		}
//		
//		public List<ICSharpCode.NRefactory.VB.Ast.Attribute> Attributes {
//			get {
//				return attributes;
//			}
//			set {
//				attributes = value ?? new List<Attribute>();
//			}
//		}
//		
//		public AttributeSection() {
//			attributeTarget = "";
//			attributes = new List<Attribute>();
//		}
//		
//		public override object AcceptVisitor(IAstVisitor visitor, object data) {
//			return visitor.VisitAttributeSection(this, data);
//		}
//		
//		public override string ToString() {
//			return string.Format("[AttributeSection AttributeTarget={0} Attributes={1}]", AttributeTarget, GetCollectionString(Attributes));
//		}
//	}
//	
//	public class BaseReferenceExpression : Expression {
//		
//		public BaseReferenceExpression() {
//		}
//		
//		public override object AcceptVisitor(IAstVisitor visitor, object data) {
//			return visitor.VisitBaseReferenceExpression(this, data);
//		}
//		
//		public override string ToString() {
//			return "[BaseReferenceExpression]";
//		}
//	}
//	
//	public class BinaryOperatorExpression : Expression {
//		
//		Expression left;
//		
//		BinaryOperatorType op;
//		
//		Expression right;
//		
//		public Expression Left {
//			get {
//				return left;
//			}
//			set {
//				left = value ?? Expression.Null;
//				if (!left.IsNull) left.Parent = this;
//			}
//		}
//		
//		public BinaryOperatorType Op {
//			get {
//				return op;
//			}
//			set {
//				op = value;
//			}
//		}
//		
//		public Expression Right {
//			get {
//				return right;
//			}
//			set {
//				right = value ?? Expression.Null;
//				if (!right.IsNull) right.Parent = this;
//			}
//		}
//		
//		public BinaryOperatorExpression() {
//			left = Expression.Null;
//			right = Expression.Null;
//		}
//		
//		public BinaryOperatorExpression(Expression left, BinaryOperatorType op, Expression right) {
//			Left = left;
//			Op = op;
//			Right = right;
//		}
//		
//		public override object AcceptVisitor(IAstVisitor visitor, object data) {
//			return visitor.VisitBinaryOperatorExpression(this, data);
//		}
//		
//		public override string ToString() {
//			return string.Format("[BinaryOperatorExpression Left={0} Op={1} Right={2}]", Left, Op, Right);
//		}
//	}
//	
//	public class CaseLabel : AbstractNode {
//		
//		Expression label;
//		
//		BinaryOperatorType binaryOperatorType;
//		
//		Expression toExpression;
//		
//		public Expression Label {
//			get {
//				return label;
//			}
//			set {
//				label = value ?? Expression.Null;
//				if (!label.IsNull) label.Parent = this;
//			}
//		}
//		
//		public BinaryOperatorType BinaryOperatorType {
//			get {
//				return binaryOperatorType;
//			}
//			set {
//				binaryOperatorType = value;
//			}
//		}
//		
//		public Expression ToExpression {
//			get {
//				return toExpression;
//			}
//			set {
//				toExpression = value ?? Expression.Null;
//				if (!toExpression.IsNull) toExpression.Parent = this;
//			}
//		}
//		
//		public CaseLabel() {
//			label = Expression.Null;
//			toExpression = Expression.Null;
//		}
//		
//		public CaseLabel(Expression label) {
//			Label = label;
//			toExpression = Expression.Null;
//		}
//		
//		public CaseLabel(Expression label, Expression toExpression) {
//			Label = label;
//			ToExpression = toExpression;
//		}
//		
//		public CaseLabel(BinaryOperatorType binaryOperatorType, Expression label) {
//			BinaryOperatorType = binaryOperatorType;
//			Label = label;
//			toExpression = Expression.Null;
//		}
//		
//		public bool IsDefault {
//			get {
//				return label.IsNull;
//			}
//		}
//		
//		public override object AcceptVisitor(IAstVisitor visitor, object data) {
//			return visitor.VisitCaseLabel(this, data);
//		}
//		
//		public override string ToString() {
//			return string.Format("[CaseLabel Label={0} BinaryOperatorType={1} ToExpression={2}]", Label, BinaryOperatorType, ToExpression);
//		}
//	}
//	
//	public class CastExpression : Expression {
//		
//		TypeReference castTo;
//		
//		Expression expression;
//		
//		CastType castType;
//		
//		public TypeReference CastTo {
//			get {
//				return castTo;
//			}
//			set {
//				castTo = value ?? TypeReference.Null;
//				if (!castTo.IsNull) castTo.Parent = this;
//			}
//		}
//		
//		public Expression Expression {
//			get {
//				return expression;
//			}
//			set {
//				expression = value ?? Expression.Null;
//				if (!expression.IsNull) expression.Parent = this;
//			}
//		}
//		
//		public CastType CastType {
//			get {
//				return castType;
//			}
//			set {
//				castType = value;
//			}
//		}
//		
//		public CastExpression(TypeReference castTo) {
//			CastTo = castTo;
//			expression = Expression.Null;
//		}
//		
//		public CastExpression(TypeReference castTo, Expression expression, CastType castType) {
//			CastTo = castTo;
//			Expression = expression;
//			CastType = castType;
//		}
//		
//		public override object AcceptVisitor(IAstVisitor visitor, object data) {
//			return visitor.VisitCastExpression(this, data);
//		}
//		
//		public override string ToString() {
//			return string.Format("[CastExpression CastTo={0} Expression={1} CastType={2}]", CastTo, Expression, CastType);
//		}
//	}
//	
//	public class CatchClause : AbstractNode {
//		
//		TypeReference typeReference;
//		
//		string variableName;
//		
//		Statement statementBlock;
//		
//		Expression condition;
//		
//		public TypeReference TypeReference {
//			get {
//				return typeReference;
//			}
//			set {
//				typeReference = value ?? TypeReference.Null;
//				if (!typeReference.IsNull) typeReference.Parent = this;
//			}
//		}
//		
//		public string VariableName {
//			get {
//				return variableName;
//			}
//			set {
//				variableName = value ?? "";
//			}
//		}
//		
//		public Statement StatementBlock {
//			get {
//				return statementBlock;
//			}
//			set {
//				statementBlock = value ?? Statement.Null;
//				if (!statementBlock.IsNull) statementBlock.Parent = this;
//			}
//		}
//		
//		public Expression Condition {
//			get {
//				return condition;
//			}
//			set {
//				condition = value ?? Expression.Null;
//				if (!condition.IsNull) condition.Parent = this;
//			}
//		}
//		
//		public CatchClause(TypeReference typeReference, string variableName, Statement statementBlock) {
//			TypeReference = typeReference;
//			VariableName = variableName;
//			StatementBlock = statementBlock;
//			condition = Expression.Null;
//		}
//		
//		public CatchClause(TypeReference typeReference, string variableName, Statement statementBlock, Expression condition) {
//			TypeReference = typeReference;
//			VariableName = variableName;
//			StatementBlock = statementBlock;
//			Condition = condition;
//		}
//		
//		public CatchClause(Statement statementBlock) {
//			StatementBlock = statementBlock;
//			typeReference = TypeReference.Null;
//			variableName = "";
//			condition = Expression.Null;
//		}
//		
//		public override object AcceptVisitor(IAstVisitor visitor, object data) {
//			return visitor.VisitCatchClause(this, data);
//		}
//		
//		public override string ToString() {
//			return string.Format("[CatchClause TypeReference={0} VariableName={1} StatementBlock={2} Condition={3}]" +
//			                     "", TypeReference, VariableName, StatementBlock, Condition);
//		}
//	}
//	
//	public class ClassReferenceExpression : Expression {
//		
//		public ClassReferenceExpression() {
//		}
//		
//		public override object AcceptVisitor(IAstVisitor visitor, object data) {
//			return visitor.VisitClassReferenceExpression(this, data);
//		}
//		
//		public override string ToString() {
//			return "[ClassReferenceExpression]";
//		}
//	}
//	
//	public class CollectionInitializerExpression : Expression {
//		
//		List<Expression> createExpressions;
//		
//		public List<Expression> CreateExpressions {
//			get {
//				return createExpressions;
//			}
//			set {
//				createExpressions = value ?? new List<Expression>();
//			}
//		}
//		
//		public CollectionInitializerExpression() {
//			createExpressions = new List<Expression>();
//		}
//		
//		public CollectionInitializerExpression(List<Expression> createExpressions) {
//			CreateExpressions = createExpressions;
//		}
//		
//		public new static CollectionInitializerExpression Null {
//			get {
//				return NullCollectionInitializerExpression.Instance;
//			}
//		}
//		
//		public override object AcceptVisitor(IAstVisitor visitor, object data) {
//			return visitor.VisitCollectionInitializerExpression(this, data);
//		}
//		
//		public override string ToString() {
//			return string.Format("[CollectionInitializerExpression CreateExpressions={0}]", GetCollectionString(CreateExpressions));
//		}
//	}
//	
//	internal sealed class NullCollectionInitializerExpression : CollectionInitializerExpression {
//		
//		internal static NullCollectionInitializerExpression Instance = new NullCollectionInitializerExpression();
//		
//		public override bool IsNull {
//			get {
//				return true;
//			}
//		}
//		
//		public override object AcceptVisitor(IAstVisitor visitor, object data) {
//			return null;
//		}
//		
//		public override string ToString() {
//			return "[NullCollectionInitializerExpression]";
//		}
//	}
//	
//	public class CollectionRangeVariable : AbstractNode, INullable {
//		
//		string identifier;
//		
//		Expression expression;
//		
//		TypeReference type;
//		
//		public string Identifier {
//			get {
//				return identifier;
//			}
//			set {
//				identifier = value ?? "";
//			}
//		}
//		
//		public Expression Expression {
//			get {
//				return expression;
//			}
//			set {
//				expression = value ?? Expression.Null;
//				if (!expression.IsNull) expression.Parent = this;
//			}
//		}
//		
//		public TypeReference Type {
//			get {
//				return type;
//			}
//			set {
//				type = value ?? TypeReference.Null;
//				if (!type.IsNull) type.Parent = this;
//			}
//		}
//		
//		public CollectionRangeVariable() {
//			identifier = "";
//			expression = Expression.Null;
//			type = TypeReference.Null;
//		}
//		
//		public virtual bool IsNull {
//			get {
//				return false;
//			}
//		}
//		
//		public static CollectionRangeVariable Null {
//			get {
//				return NullCollectionRangeVariable.Instance;
//			}
//		}
//		
//		public override object AcceptVisitor(IAstVisitor visitor, object data) {
//			return visitor.VisitCollectionRangeVariable(this, data);
//		}
//		
//		public override string ToString() {
//			return string.Format("[CollectionRangeVariable Identifier={0} Expression={1} Type={2}]", Identifier, Expression, Type);
//		}
//	}
//	
//	internal sealed class NullCollectionRangeVariable : CollectionRangeVariable {
//		
//		internal static NullCollectionRangeVariable Instance = new NullCollectionRangeVariable();
//		
//		public override bool IsNull {
//			get {
//				return true;
//			}
//		}
//		
//		public override object AcceptVisitor(IAstVisitor visitor, object data) {
//			return null;
//		}
//		
//		public override string ToString() {
//			return "[NullCollectionRangeVariable]";
//		}
//	}
//	
//	public class ConditionalExpression : Expression {
//		
//		Expression condition;
//		
//		Expression trueExpression;
//		
//		Expression falseExpression;
//		
//		public Expression Condition {
//			get {
//				return condition;
//			}
//			set {
//				condition = value ?? Expression.Null;
//				if (!condition.IsNull) condition.Parent = this;
//			}
//		}
//		
//		public Expression TrueExpression {
//			get {
//				return trueExpression;
//			}
//			set {
//				trueExpression = value ?? Expression.Null;
//				if (!trueExpression.IsNull) trueExpression.Parent = this;
//			}
//		}
//		
//		public Expression FalseExpression {
//			get {
//				return falseExpression;
//			}
//			set {
//				falseExpression = value ?? Expression.Null;
//				if (!falseExpression.IsNull) falseExpression.Parent = this;
//			}
//		}
//		
//		public ConditionalExpression() {
//			condition = Expression.Null;
//			trueExpression = Expression.Null;
//			falseExpression = Expression.Null;
//		}
//		
//		public ConditionalExpression(Expression condition, Expression trueExpression, Expression falseExpression) {
//			Condition = condition;
//			TrueExpression = trueExpression;
//			FalseExpression = falseExpression;
//		}
//		
//		public override object AcceptVisitor(IAstVisitor visitor, object data) {
//			return visitor.VisitConditionalExpression(this, data);
//		}
//		
//		public override string ToString() {
//			return string.Format("[ConditionalExpression Condition={0} TrueExpression={1} FalseExpression={2}]", Condition, TrueExpression, FalseExpression);
//		}
//	}
//	
//	public class ConstructorDeclaration : ParametrizedNode {
//		
//		ConstructorInitializer constructorInitializer;
//		
//		BlockStatement body;
//		
//		public ConstructorInitializer ConstructorInitializer {
//			get {
//				return constructorInitializer;
//			}
//			set {
//				constructorInitializer = value ?? ConstructorInitializer.Null;
//				if (!constructorInitializer.IsNull) constructorInitializer.Parent = this;
//			}
//		}
//		
//		public BlockStatement Body {
//			get {
//				return body;
//			}
//			set {
//				body = value ?? BlockStatement.Null;
//				if (!body.IsNull) body.Parent = this;
//			}
//		}
//		
//		public ConstructorDeclaration(string name, Modifiers modifier, List<ParameterDeclarationExpression> parameters, List<AttributeSection> attributes) {
//			Name = name;
//			Modifier = modifier;
//			Parameters = parameters;
//			Attributes = attributes;
//			constructorInitializer = ConstructorInitializer.Null;
//			body = BlockStatement.Null;
//		}
//		
//		public ConstructorDeclaration(string name, Modifiers modifier, List<ParameterDeclarationExpression> parameters, ConstructorInitializer constructorInitializer, List<AttributeSection> attributes) {
//			Name = name;
//			Modifier = modifier;
//			Parameters = parameters;
//			ConstructorInitializer = constructorInitializer;
//			Attributes = attributes;
//			body = BlockStatement.Null;
//		}
//		
//		public override object AcceptVisitor(IAstVisitor visitor, object data) {
//			return visitor.VisitConstructorDeclaration(this, data);
//		}
//		
//		public override string ToString() {
//			return string.Format("[ConstructorDeclaration ConstructorInitializer={0} Body={1} Name={2} Parameters={" +
//			                     "3} Attributes={4} Modifier={5}]", ConstructorInitializer, Body, Name, GetCollectionString(Parameters), GetCollectionString(Attributes), Modifier);
//		}
//	}
//	
//	public class ConstructorInitializer : AbstractNode, INullable {
//		
//		ConstructorInitializerType constructorInitializerType;
//		
//		List<Expression> arguments;
//		
//		public ConstructorInitializerType ConstructorInitializerType {
//			get {
//				return constructorInitializerType;
//			}
//			set {
//				constructorInitializerType = value;
//			}
//		}
//		
//		public List<Expression> Arguments {
//			get {
//				return arguments;
//			}
//			set {
//				arguments = value ?? new List<Expression>();
//			}
//		}
//		
//		public ConstructorInitializer() {
//			arguments = new List<Expression>();
//		}
//		
//		public virtual bool IsNull {
//			get {
//				return false;
//			}
//		}
//		
//		public static ConstructorInitializer Null {
//			get {
//				return NullConstructorInitializer.Instance;
//			}
//		}
//		
//		public override object AcceptVisitor(IAstVisitor visitor, object data) {
//			return visitor.VisitConstructorInitializer(this, data);
//		}
//		
//		public override string ToString() {
//			return string.Format("[ConstructorInitializer ConstructorInitializerType={0} Arguments={1}]", ConstructorInitializerType, GetCollectionString(Arguments));
//		}
//	}
//	
//	internal sealed class NullConstructorInitializer : ConstructorInitializer {
//		
//		internal static NullConstructorInitializer Instance = new NullConstructorInitializer();
//		
//		public override bool IsNull {
//			get {
//				return true;
//			}
//		}
//		
//		public override object AcceptVisitor(IAstVisitor visitor, object data) {
//			return null;
//		}
//		
//		public override string ToString() {
//			return "[NullConstructorInitializer]";
//		}
//	}
//	
//	public class ContinueStatement : Statement {
//		
//		ContinueType continueType;
//		
//		public ContinueType ContinueType {
//			get {
//				return continueType;
//			}
//			set {
//				continueType = value;
//			}
//		}
//		
//		public ContinueStatement() {
//		}
//		
//		public ContinueStatement(ContinueType continueType) {
//			ContinueType = continueType;
//		}
//		
//		public override object AcceptVisitor(IAstVisitor visitor, object data) {
//			return visitor.VisitContinueStatement(this, data);
//		}
//		
//		public override string ToString() {
//			return string.Format("[ContinueStatement ContinueType={0}]", ContinueType);
//		}
//	}
//	
//	public class DeclareDeclaration : ParametrizedNode {
//		
//		string alias;
//		
//		string library;
//		
//		CharsetModifier charset;
//		
//		TypeReference typeReference;
//		
//		public string Alias {
//			get {
//				return alias;
//			}
//			set {
//				alias = value ?? "";
//			}
//		}
//		
//		public string Library {
//			get {
//				return library;
//			}
//			set {
//				library = value ?? "";
//			}
//		}
//		
//		public CharsetModifier Charset {
//			get {
//				return charset;
//			}
//			set {
//				charset = value;
//			}
//		}
//		
//		public TypeReference TypeReference {
//			get {
//				return typeReference;
//			}
//			set {
//				typeReference = value ?? TypeReference.Null;
//				if (!typeReference.IsNull) typeReference.Parent = this;
//			}
//		}
//		
//		public DeclareDeclaration(string name, Modifiers modifier, TypeReference typeReference, List<ParameterDeclarationExpression> parameters, List<AttributeSection> attributes, string library, string alias, CharsetModifier charset) {
//			Name = name;
//			Modifier = modifier;
//			TypeReference = typeReference;
//			Parameters = parameters;
//			Attributes = attributes;
//			Library = library;
//			Alias = alias;
//			Charset = charset;
//		}
//		
//		public override object AcceptVisitor(IAstVisitor visitor, object data) {
//			return visitor.VisitDeclareDeclaration(this, data);
//		}
//		
//		public override string ToString() {
//			return string.Format("[DeclareDeclaration Alias={0} Library={1} Charset={2} TypeReference={3} Name={4} " +
//			                     "Parameters={5} Attributes={6} Modifier={7}]", Alias, Library, Charset, TypeReference, Name, GetCollectionString(Parameters), GetCollectionString(Attributes), Modifier);
//		}
//	}
//	
//	public class DefaultValueExpression : Expression {
//		
//		TypeReference typeReference;
//		
//		public TypeReference TypeReference {
//			get {
//				return typeReference;
//			}
//			set {
//				typeReference = value ?? TypeReference.Null;
//				if (!typeReference.IsNull) typeReference.Parent = this;
//			}
//		}
//		
//		public DefaultValueExpression(TypeReference typeReference) {
//			TypeReference = typeReference;
//		}
//		
//		public override object AcceptVisitor(IAstVisitor visitor, object data) {
//			return visitor.VisitDefaultValueExpression(this, data);
//		}
//		
//		public override string ToString() {
//			return string.Format("[DefaultValueExpression TypeReference={0}]", TypeReference);
//		}
//	}
//	
//	public class DelegateDeclaration : AttributedNode {
//		
//		string name;
//		
//		TypeReference returnType;
//		
//		List<ParameterDeclarationExpression> parameters;
//		
//		List<TemplateDefinition> templates;
//		
//		public string Name {
//			get {
//				return name;
//			}
//			set {
//				name = string.IsNullOrEmpty(value) ? "?" : value;
//			}
//		}
//		
//		public TypeReference ReturnType {
//			get {
//				return returnType;
//			}
//			set {
//				returnType = value ?? TypeReference.Null;
//				if (!returnType.IsNull) returnType.Parent = this;
//			}
//		}
//		
//		public List<ParameterDeclarationExpression> Parameters {
//			get {
//				return parameters;
//			}
//			set {
//				parameters = value ?? new List<ParameterDeclarationExpression>();
//			}
//		}
//		
//		public List<TemplateDefinition> Templates {
//			get {
//				return templates;
//			}
//			set {
//				templates = value ?? new List<TemplateDefinition>();
//			}
//		}
//		
//		public DelegateDeclaration(Modifiers modifier, List<AttributeSection> attributes) {
//			Modifier = modifier;
//			Attributes = attributes;
//			name = "?";
//			returnType = TypeReference.Null;
//			parameters = new List<ParameterDeclarationExpression>();
//			templates = new List<TemplateDefinition>();
//		}
//		
//		public override object AcceptVisitor(IAstVisitor visitor, object data) {
//			return visitor.VisitDelegateDeclaration(this, data);
//		}
//		
//		public override string ToString() {
//			return string.Format("[DelegateDeclaration Name={0} ReturnType={1} Parameters={2} Templates={3} Attribu" +
//			                     "tes={4} Modifier={5}]", Name, ReturnType, GetCollectionString(Parameters), GetCollectionString(Templates), GetCollectionString(Attributes), Modifier);
//		}
//	}
//	
//	public class DirectionExpression : Expression {
//		
//		FieldDirection fieldDirection;
//		
//		Expression expression;
//		
//		public FieldDirection FieldDirection {
//			get {
//				return fieldDirection;
//			}
//			set {
//				fieldDirection = value;
//			}
//		}
//		
//		public Expression Expression {
//			get {
//				return expression;
//			}
//			set {
//				expression = value ?? Expression.Null;
//				if (!expression.IsNull) expression.Parent = this;
//			}
//		}
//		
//		public DirectionExpression(FieldDirection fieldDirection, Expression expression) {
//			FieldDirection = fieldDirection;
//			Expression = expression;
//		}
//		
//		public override object AcceptVisitor(IAstVisitor visitor, object data) {
//			return visitor.VisitDirectionExpression(this, data);
//		}
//		
//		public override string ToString() {
//			return string.Format("[DirectionExpression FieldDirection={0} Expression={1}]", FieldDirection, Expression);
//		}
//	}
//	
//	public class DoLoopStatement : StatementWithEmbeddedStatement {
//		
//		Expression condition;
//		
//		ConditionType conditionType;
//		
//		ConditionPosition conditionPosition;
//		
//		public Expression Condition {
//			get {
//				return condition;
//			}
//			set {
//				condition = value ?? Expression.Null;
//				if (!condition.IsNull) condition.Parent = this;
//			}
//		}
//		
//		public ConditionType ConditionType {
//			get {
//				return conditionType;
//			}
//			set {
//				conditionType = value;
//			}
//		}
//		
//		public ConditionPosition ConditionPosition {
//			get {
//				return conditionPosition;
//			}
//			set {
//				conditionPosition = value;
//			}
//		}
//		
//		public DoLoopStatement(Expression condition, Statement embeddedStatement, ConditionType conditionType, ConditionPosition conditionPosition) {
//			Condition = condition;
//			EmbeddedStatement = embeddedStatement;
//			ConditionType = conditionType;
//			ConditionPosition = conditionPosition;
//		}
//		
//		public override object AcceptVisitor(IAstVisitor visitor, object data) {
//			return visitor.VisitDoLoopStatement(this, data);
//		}
//		
//		public override string ToString() {
//			return string.Format("[DoLoopStatement Condition={0} ConditionType={1} ConditionPosition={2} EmbeddedSt" +
//			                     "atement={3}]", Condition, ConditionType, ConditionPosition, EmbeddedStatement);
//		}
//	}
//	
//	public class ElseIfSection : StatementWithEmbeddedStatement {
//		
//		Expression condition;
//		
//		public Expression Condition {
//			get {
//				return condition;
//			}
//			set {
//				condition = value ?? Expression.Null;
//				if (!condition.IsNull) condition.Parent = this;
//			}
//		}
//		
//		public ElseIfSection(Expression condition, Statement embeddedStatement) {
//			Condition = condition;
//			EmbeddedStatement = embeddedStatement;
//		}
//		
//		public override object AcceptVisitor(IAstVisitor visitor, object data) {
//			return visitor.VisitElseIfSection(this, data);
//		}
//		
//		public override string ToString() {
//			return string.Format("[ElseIfSection Condition={0} EmbeddedStatement={1}]", Condition, EmbeddedStatement);
//		}
//	}
//	
//	public class EndStatement : Statement {
//		
//		public EndStatement() {
//		}
//		
//		public override object AcceptVisitor(IAstVisitor visitor, object data) {
//			return visitor.VisitEndStatement(this, data);
//		}
//		
//		public override string ToString() {
//			return "[EndStatement]";
//		}
//	}
//	
//	public class EraseStatement : Statement {
//		
//		List<Expression> expressions;
//		
//		public List<Expression> Expressions {
//			get {
//				return expressions;
//			}
//			set {
//				expressions = value ?? new List<Expression>();
//			}
//		}
//		
//		public EraseStatement() {
//			expressions = new List<Expression>();
//		}
//		
//		public EraseStatement(List<Expression> expressions) {
//			Expressions = expressions;
//		}
//		
//		public override object AcceptVisitor(IAstVisitor visitor, object data) {
//			return visitor.VisitEraseStatement(this, data);
//		}
//		
//		public override string ToString() {
//			return string.Format("[EraseStatement Expressions={0}]", GetCollectionString(Expressions));
//		}
//	}
//	
//	public class ErrorStatement : Statement {
//		
//		Expression expression;
//		
//		public Expression Expression {
//			get {
//				return expression;
//			}
//			set {
//				expression = value ?? Expression.Null;
//				if (!expression.IsNull) expression.Parent = this;
//			}
//		}
//		
//		public ErrorStatement(Expression expression) {
//			Expression = expression;
//		}
//		
//		public override object AcceptVisitor(IAstVisitor visitor, object data) {
//			return visitor.VisitErrorStatement(this, data);
//		}
//		
//		public override string ToString() {
//			return string.Format("[ErrorStatement Expression={0}]", Expression);
//		}
//	}
//	
//	public class EventAddRegion : EventAddRemoveRegion {
//		
//		public EventAddRegion(List<AttributeSection> attributes) :
//			base(attributes) {
//		}
//		
//		public static EventAddRegion Null {
//			get {
//				return NullEventAddRegion.Instance;
//			}
//		}
//		
//		public override object AcceptVisitor(IAstVisitor visitor, object data) {
//			return visitor.VisitEventAddRegion(this, data);
//		}
//		
//		public override string ToString() {
//			return string.Format("[EventAddRegion Block={0} Parameters={1} Attributes={2} Modifier={3}]", Block, GetCollectionString(Parameters), GetCollectionString(Attributes), Modifier);
//		}
//	}
//	
//	internal sealed class NullEventAddRegion : EventAddRegion {
//		
//		private NullEventAddRegion() :
//			base(null) {
//		}
//		
//		internal static NullEventAddRegion Instance = new NullEventAddRegion();
//		
//		public override bool IsNull {
//			get {
//				return true;
//			}
//		}
//		
//		public override object AcceptVisitor(IAstVisitor visitor, object data) {
//			return null;
//		}
//		
//		public override string ToString() {
//			return "[NullEventAddRegion]";
//		}
//	}
//	
//	public abstract class EventAddRemoveRegion : AttributedNode, INullable {
//		
//		BlockStatement block;
//		
//		List<ParameterDeclarationExpression> parameters;
//		
//		public BlockStatement Block {
//			get {
//				return block;
//			}
//			set {
//				block = value ?? BlockStatement.Null;
//				if (!block.IsNull) block.Parent = this;
//			}
//		}
//		
//		public List<ParameterDeclarationExpression> Parameters {
//			get {
//				return parameters;
//			}
//			set {
//				parameters = value ?? new List<ParameterDeclarationExpression>();
//			}
//		}
//		
//		protected EventAddRemoveRegion(List<AttributeSection> attributes) {
//			Attributes = attributes;
//			block = BlockStatement.Null;
//			parameters = new List<ParameterDeclarationExpression>();
//		}
//		
//		public virtual bool IsNull {
//			get {
//				return false;
//			}
//		}
//	}
//	
//	public class EventDeclaration : MemberNode {
//		
//		EventAddRegion addRegion;
//		
//		EventRemoveRegion removeRegion;
//		
//		EventRaiseRegion raiseRegion;
//		
//		Location bodyStart;
//		
//		Location bodyEnd;
//		
//		Expression initializer;
//		
//		public EventAddRegion AddRegion {
//			get {
//				return addRegion;
//			}
//			set {
//				addRegion = value ?? EventAddRegion.Null;
//				if (!addRegion.IsNull) addRegion.Parent = this;
//			}
//		}
//		
//		public EventRemoveRegion RemoveRegion {
//			get {
//				return removeRegion;
//			}
//			set {
//				removeRegion = value ?? EventRemoveRegion.Null;
//				if (!removeRegion.IsNull) removeRegion.Parent = this;
//			}
//		}
//		
//		public EventRaiseRegion RaiseRegion {
//			get {
//				return raiseRegion;
//			}
//			set {
//				raiseRegion = value ?? EventRaiseRegion.Null;
//				if (!raiseRegion.IsNull) raiseRegion.Parent = this;
//			}
//		}
//		
//		public Location BodyStart {
//			get {
//				return bodyStart;
//			}
//			set {
//				bodyStart = value;
//			}
//		}
//		
//		public Location BodyEnd {
//			get {
//				return bodyEnd;
//			}
//			set {
//				bodyEnd = value;
//			}
//		}
//		
//		public Expression Initializer {
//			get {
//				return initializer;
//			}
//			set {
//				initializer = value ?? Expression.Null;
//				if (!initializer.IsNull) initializer.Parent = this;
//			}
//		}
//		
//		public EventDeclaration() {
//			addRegion = EventAddRegion.Null;
//			removeRegion = EventRemoveRegion.Null;
//			raiseRegion = EventRaiseRegion.Null;
//			bodyStart = Location.Empty;
//			bodyEnd = Location.Empty;
//			initializer = Expression.Null;
//		}
//		
//		public bool HasAddRegion {
//			get {
//				return !addRegion.IsNull;
//			}
//		}
//		
//		public bool HasRemoveRegion {
//			get {
//				return !removeRegion.IsNull;
//			}
//		}
//		
//		public bool HasRaiseRegion {
//			get {
//				return !raiseRegion.IsNull;
//			}
//		}
//		
//		public override object AcceptVisitor(IAstVisitor visitor, object data) {
//			return visitor.VisitEventDeclaration(this, data);
//		}
//		
//		public override string ToString() {
//			return string.Format("[EventDeclaration AddRegion={0} RemoveRegion={1} RaiseRegion={2} BodyStart={3} Bo" +
//			                     "dyEnd={4} Initializer={5} InterfaceImplementations={6} TypeReference={7} Name={8" +
//			                     "} Parameters={9} Attributes={10} Modifier={11}]", AddRegion, RemoveRegion, RaiseRegion, BodyStart, BodyEnd, Initializer, GetCollectionString(InterfaceImplementations), TypeReference, Name, GetCollectionString(Parameters), GetCollectionString(Attributes), Modifier);
//		}
//	}
//	
//	public class EventRaiseRegion : EventAddRemoveRegion {
//		
//		public EventRaiseRegion(List<AttributeSection> attributes) :
//			base(attributes) {
//		}
//		
//		public static EventRaiseRegion Null {
//			get {
//				return NullEventRaiseRegion.Instance;
//			}
//		}
//		
//		public override object AcceptVisitor(IAstVisitor visitor, object data) {
//			return visitor.VisitEventRaiseRegion(this, data);
//		}
//		
//		public override string ToString() {
//			return string.Format("[EventRaiseRegion Block={0} Parameters={1} Attributes={2} Modifier={3}]", Block, GetCollectionString(Parameters), GetCollectionString(Attributes), Modifier);
//		}
//	}
//	
//	internal sealed class NullEventRaiseRegion : EventRaiseRegion {
//		
//		private NullEventRaiseRegion() :
//			base(null) {
//		}
//		
//		internal static NullEventRaiseRegion Instance = new NullEventRaiseRegion();
//		
//		public override bool IsNull {
//			get {
//				return true;
//			}
//		}
//		
//		public override object AcceptVisitor(IAstVisitor visitor, object data) {
//			return null;
//		}
//		
//		public override string ToString() {
//			return "[NullEventRaiseRegion]";
//		}
//	}
//	
//	public class EventRemoveRegion : EventAddRemoveRegion {
//		
//		public EventRemoveRegion(List<AttributeSection> attributes) :
//			base(attributes) {
//		}
//		
//		public static EventRemoveRegion Null {
//			get {
//				return NullEventRemoveRegion.Instance;
//			}
//		}
//		
//		public override object AcceptVisitor(IAstVisitor visitor, object data) {
//			return visitor.VisitEventRemoveRegion(this, data);
//		}
//		
//		public override string ToString() {
//			return string.Format("[EventRemoveRegion Block={0} Parameters={1} Attributes={2} Modifier={3}]", Block, GetCollectionString(Parameters), GetCollectionString(Attributes), Modifier);
//		}
//	}
//	
//	internal sealed class NullEventRemoveRegion : EventRemoveRegion {
//		
//		private NullEventRemoveRegion() :
//			base(null) {
//		}
//		
//		internal static NullEventRemoveRegion Instance = new NullEventRemoveRegion();
//		
//		public override bool IsNull {
//			get {
//				return true;
//			}
//		}
//		
//		public override object AcceptVisitor(IAstVisitor visitor, object data) {
//			return null;
//		}
//		
//		public override string ToString() {
//			return "[NullEventRemoveRegion]";
//		}
//	}
//	
//	public class ExitStatement : Statement {
//		
//		ExitType exitType;
//		
//		public ExitType ExitType {
//			get {
//				return exitType;
//			}
//			set {
//				exitType = value;
//			}
//		}
//		
//		public ExitStatement(ExitType exitType) {
//			ExitType = exitType;
//		}
//		
//		public override object AcceptVisitor(IAstVisitor visitor, object data) {
//			return visitor.VisitExitStatement(this, data);
//		}
//		
//		public override string ToString() {
//			return string.Format("[ExitStatement ExitType={0}]", ExitType);
//		}
//	}
//	
//	public class ExpressionRangeVariable : AbstractNode, INullable {
//		
//		string identifier;
//		
//		Expression expression;
//		
//		TypeReference type;
//		
//		public string Identifier {
//			get {
//				return identifier;
//			}
//			set {
//				identifier = value ?? "";
//			}
//		}
//		
//		public Expression Expression {
//			get {
//				return expression;
//			}
//			set {
//				expression = value ?? Expression.Null;
//				if (!expression.IsNull) expression.Parent = this;
//			}
//		}
//		
//		public TypeReference Type {
//			get {
//				return type;
//			}
//			set {
//				type = value ?? TypeReference.Null;
//				if (!type.IsNull) type.Parent = this;
//			}
//		}
//		
//		public ExpressionRangeVariable() {
//			identifier = "";
//			expression = Expression.Null;
//			type = TypeReference.Null;
//		}
//		
//		public virtual bool IsNull {
//			get {
//				return false;
//			}
//		}
//		
//		public static ExpressionRangeVariable Null {
//			get {
//				return NullExpressionRangeVariable.Instance;
//			}
//		}
//		
//		public override object AcceptVisitor(IAstVisitor visitor, object data) {
//			return visitor.VisitExpressionRangeVariable(this, data);
//		}
//		
//		public override string ToString() {
//			return string.Format("[ExpressionRangeVariable Identifier={0} Expression={1} Type={2}]", Identifier, Expression, Type);
//		}
//	}
//	
//	internal sealed class NullExpressionRangeVariable : ExpressionRangeVariable {
//		
//		internal static NullExpressionRangeVariable Instance = new NullExpressionRangeVariable();
//		
//		public override bool IsNull {
//			get {
//				return true;
//			}
//		}
//		
//		public override object AcceptVisitor(IAstVisitor visitor, object data) {
//			return null;
//		}
//		
//		public override string ToString() {
//			return "[NullExpressionRangeVariable]";
//		}
//	}
//	
//	public class ExpressionStatement : Statement {
//		
//		Expression expression;
//		
//		public Expression Expression {
//			get {
//				return expression;
//			}
//			set {
//				expression = value ?? Expression.Null;
//				if (!expression.IsNull) expression.Parent = this;
//			}
//		}
//		
//		public ExpressionStatement(Expression expression) {
//			Expression = expression;
//		}
//		
//		public override object AcceptVisitor(IAstVisitor visitor, object data) {
//			return visitor.VisitExpressionStatement(this, data);
//		}
//		
//		public override string ToString() {
//			return string.Format("[ExpressionStatement Expression={0}]", Expression);
//		}
//	}
//	
//	public class ExternAliasDirective : AbstractNode {
//		
//		string name;
//		
//		public string Name {
//			get {
//				return name;
//			}
//			set {
//				name = value ?? "";
//			}
//		}
//		
//		public ExternAliasDirective() {
//			name = "";
//		}
//		
//		public override object AcceptVisitor(IAstVisitor visitor, object data) {
//			return visitor.VisitExternAliasDirective(this, data);
//		}
//		
//		public override string ToString() {
//			return string.Format("[ExternAliasDirective Name={0}]", Name);
//		}
//	}
//	
//	public class FieldDeclaration : AttributedNode {
//		
//		TypeReference typeReference;
//		
//		List<VariableDeclaration> fields;
//		
//		public TypeReference TypeReference {
//			get {
//				return typeReference;
//			}
//			set {
//				typeReference = value ?? TypeReference.Null;
//				if (!typeReference.IsNull) typeReference.Parent = this;
//			}
//		}
//		
//		public List<VariableDeclaration> Fields {
//			get {
//				return fields;
//			}
//			set {
//				fields = value ?? new List<VariableDeclaration>();
//			}
//		}
//		
//		public FieldDeclaration(List<AttributeSection> attributes) {
//			Attributes = attributes;
//			typeReference = TypeReference.Null;
//			fields = new List<VariableDeclaration>();
//		}
//		
//		public FieldDeclaration(List<AttributeSection> attributes, TypeReference typeReference, Modifiers modifier) {
//			Attributes = attributes;
//			TypeReference = typeReference;
//			Modifier = modifier;
//			fields = new List<VariableDeclaration>();
//		}
//		
//
//		public TypeReference GetTypeForField(int fieldIndex)
//		{
//			if (!typeReference.IsNull) {
//				return typeReference;
//			}
//			return ((VariableDeclaration)Fields[fieldIndex]).TypeReference;
//		}
//		
//
//		public VariableDeclaration GetVariableDeclaration(string variableName)
//		{
//			foreach (VariableDeclaration variableDeclaration in Fields) {
//				if (variableDeclaration.Name == variableName) {
//					return variableDeclaration;
//				}
//			}
//			return null;
//		}
//		
//		public override object AcceptVisitor(IAstVisitor visitor, object data) {
//			return visitor.VisitFieldDeclaration(this, data);
//		}
//		
//		public override string ToString() {
//			return string.Format("[FieldDeclaration TypeReference={0} Fields={1} Attributes={2} Modifier={3}]", TypeReference, GetCollectionString(Fields), GetCollectionString(Attributes), Modifier);
//		}
//	}
//	
//	public class ForeachStatement : StatementWithEmbeddedStatement {
//		
//		TypeReference typeReference;
//		
//		string variableName;
//		
//		Expression expression;
//		
//		Expression nextExpression;
//		
//		public TypeReference TypeReference {
//			get {
//				return typeReference;
//			}
//			set {
//				typeReference = value ?? TypeReference.Null;
//				if (!typeReference.IsNull) typeReference.Parent = this;
//			}
//		}
//		
//		public string VariableName {
//			get {
//				return variableName;
//			}
//			set {
//				variableName = value ?? "";
//			}
//		}
//		
//		public Expression Expression {
//			get {
//				return expression;
//			}
//			set {
//				expression = value ?? Expression.Null;
//				if (!expression.IsNull) expression.Parent = this;
//			}
//		}
//		
//		public Expression NextExpression {
//			get {
//				return nextExpression;
//			}
//			set {
//				nextExpression = value ?? Expression.Null;
//				if (!nextExpression.IsNull) nextExpression.Parent = this;
//			}
//		}
//		
//		public ForeachStatement(TypeReference typeReference, string variableName, Expression expression, Statement embeddedStatement) {
//			TypeReference = typeReference;
//			VariableName = variableName;
//			Expression = expression;
//			EmbeddedStatement = embeddedStatement;
//			nextExpression = Expression.Null;
//		}
//		
//		public ForeachStatement(TypeReference typeReference, string variableName, Expression expression, Statement embeddedStatement, Expression nextExpression) {
//			TypeReference = typeReference;
//			VariableName = variableName;
//			Expression = expression;
//			EmbeddedStatement = embeddedStatement;
//			NextExpression = nextExpression;
//		}
//		
//		public override object AcceptVisitor(IAstVisitor visitor, object data) {
//			return visitor.VisitForeachStatement(this, data);
//		}
//		
//		public override string ToString() {
//			return string.Format("[ForeachStatement TypeReference={0} VariableName={1} Expression={2} NextExpressio" +
//			                     "n={3} EmbeddedStatement={4}]", TypeReference, VariableName, Expression, NextExpression, EmbeddedStatement);
//		}
//	}
//	
//	public class ForNextStatement : StatementWithEmbeddedStatement {
//		
//		Expression start;
//		
//		Expression end;
//		
//		Expression step;
//		
//		List<Expression> nextExpressions;
//		
//		TypeReference typeReference;
//		
//		string variableName;
//		
//		Expression loopVariableExpression;
//		
//		public Expression Start {
//			get {
//				return start;
//			}
//			set {
//				start = value ?? Expression.Null;
//				if (!start.IsNull) start.Parent = this;
//			}
//		}
//		
//		public Expression End {
//			get {
//				return end;
//			}
//			set {
//				end = value ?? Expression.Null;
//				if (!end.IsNull) end.Parent = this;
//			}
//		}
//		
//		public Expression Step {
//			get {
//				return step;
//			}
//			set {
//				step = value ?? Expression.Null;
//				if (!step.IsNull) step.Parent = this;
//			}
//		}
//		
//		public List<Expression> NextExpressions {
//			get {
//				return nextExpressions;
//			}
//			set {
//				nextExpressions = value ?? new List<Expression>();
//			}
//		}
//		
//		public TypeReference TypeReference {
//			get {
//				return typeReference;
//			}
//			set {
//				typeReference = value ?? TypeReference.Null;
//				if (!typeReference.IsNull) typeReference.Parent = this;
//			}
//		}
//		
//		public string VariableName {
//			get {
//				return variableName;
//			}
//			set {
//				variableName = value ?? "";
//			}
//		}
//		
//		public Expression LoopVariableExpression {
//			get {
//				return loopVariableExpression;
//			}
//			set {
//				loopVariableExpression = value ?? Expression.Null;
//				if (!loopVariableExpression.IsNull) loopVariableExpression.Parent = this;
//			}
//		}
//		
//		public ForNextStatement() {
//			start = Expression.Null;
//			end = Expression.Null;
//			step = Expression.Null;
//			nextExpressions = new List<Expression>();
//			typeReference = TypeReference.Null;
//			variableName = "";
//			loopVariableExpression = Expression.Null;
//		}
//		
//		public override object AcceptVisitor(IAstVisitor visitor, object data) {
//			return visitor.VisitForNextStatement(this, data);
//		}
//		
//		public override string ToString() {
//			return string.Format("[ForNextStatement Start={0} End={1} Step={2} NextExpressions={3} TypeReference={4" +
//			                     "} VariableName={5} LoopVariableExpression={6} EmbeddedStatement={7}]", Start, End, Step, GetCollectionString(NextExpressions), TypeReference, VariableName, LoopVariableExpression, EmbeddedStatement);
//		}
//	}
//	
//	public class GotoStatement : Statement {
//		
//		string label;
//		
//		public string Label {
//			get {
//				return label;
//			}
//			set {
//				label = value ?? "";
//			}
//		}
//		
//		public GotoStatement(string label) {
//			Label = label;
//		}
//		
//		public override object AcceptVisitor(IAstVisitor visitor, object data) {
//			return visitor.VisitGotoStatement(this, data);
//		}
//		
//		public override string ToString() {
//			return string.Format("[GotoStatement Label={0}]", Label);
//		}
//	}
//	
//	public class IfElseStatement : Statement {
//		
//		Expression condition;
//		
//		List<Statement> trueStatement;
//		
//		List<Statement> falseStatement;
//		
//		List<ElseIfSection> elseIfSections;
//		
//		public Expression Condition {
//			get {
//				return condition;
//			}
//			set {
//				condition = value ?? Expression.Null;
//				if (!condition.IsNull) condition.Parent = this;
//			}
//		}
//		
//		public List<Statement> TrueStatement {
//			get {
//				return trueStatement;
//			}
//			set {
//				trueStatement = value ?? new List<Statement>();
//			}
//		}
//		
//		public List<Statement> FalseStatement {
//			get {
//				return falseStatement;
//			}
//			set {
//				falseStatement = value ?? new List<Statement>();
//			}
//		}
//		
//		public List<ElseIfSection> ElseIfSections {
//			get {
//				return elseIfSections;
//			}
//			set {
//				elseIfSections = value ?? new List<ElseIfSection>();
//			}
//		}
//		
//		public IfElseStatement(Expression condition) {
//			Condition = condition;
//			trueStatement = new List<Statement>();
//			falseStatement = new List<Statement>();
//			elseIfSections = new List<ElseIfSection>();
//		}
//		
//
//		public IfElseStatement(Expression condition, Statement trueStatement)
//			: this(condition) {
//			this.trueStatement.Add(Statement.CheckNull(trueStatement));
//			if (trueStatement != null) trueStatement.Parent = this;
//		}
//		
//		public bool HasElseStatements {
//			get {
//				return falseStatement.Count > 0;
//			}
//		}
//		
//		public bool HasElseIfSections {
//			get {
//				return elseIfSections.Count > 0;
//			}
//		}
//		
//
//		public IfElseStatement(Expression condition, Statement trueStatement, Statement falseStatement)
//			: this(condition) {
//			this.trueStatement.Add(Statement.CheckNull(trueStatement));
//			this.falseStatement.Add(Statement.CheckNull(falseStatement));
//			if (trueStatement != null) trueStatement.Parent = this;
//			if (falseStatement != null) falseStatement.Parent = this;
//		}
//		
//		public override object AcceptVisitor(IAstVisitor visitor, object data) {
//			return visitor.VisitIfElseStatement(this, data);
//		}
//		
//		public override string ToString() {
//			return string.Format("[IfElseStatement Condition={0} TrueStatement={1} FalseStatement={2} ElseIfSection" +
//			                     "s={3}]", Condition, GetCollectionString(TrueStatement), GetCollectionString(FalseStatement), GetCollectionString(ElseIfSections));
//		}
//	}
//	
//	public class InterfaceImplementation : AbstractNode {
//		
//		TypeReference interfaceType;
//		
//		string memberName;
//		
//		public TypeReference InterfaceType {
//			get {
//				return interfaceType;
//			}
//			set {
//				interfaceType = value ?? TypeReference.Null;
//				if (!interfaceType.IsNull) interfaceType.Parent = this;
//			}
//		}
//		
//		public string MemberName {
//			get {
//				return memberName;
//			}
//			set {
//				memberName = string.IsNullOrEmpty(value) ? "?" : value;
//			}
//		}
//		
//		public InterfaceImplementation(TypeReference interfaceType, string memberName) {
//			InterfaceType = interfaceType;
//			MemberName = memberName;
//		}
//		
//		public override object AcceptVisitor(IAstVisitor visitor, object data) {
//			return visitor.VisitInterfaceImplementation(this, data);
//		}
//		
//		public override string ToString() {
//			return string.Format("[InterfaceImplementation InterfaceType={0} MemberName={1}]", InterfaceType, MemberName);
//		}
//	}
//	
//	public class InvocationExpression : Expression {
//		
//		Expression targetObject;
//		
//		List<Expression> arguments;
//		
//		public Expression TargetObject {
//			get {
//				return targetObject;
//			}
//			set {
//				targetObject = value ?? Expression.Null;
//				if (!targetObject.IsNull) targetObject.Parent = this;
//			}
//		}
//		
//		public List<Expression> Arguments {
//			get {
//				return arguments;
//			}
//			set {
//				arguments = value ?? new List<Expression>();
//			}
//		}
//		
//		public InvocationExpression(Expression targetObject) {
//			TargetObject = targetObject;
//			arguments = new List<Expression>();
//		}
//		
//		public InvocationExpression(Expression targetObject, List<Expression> arguments) {
//			TargetObject = targetObject;
//			Arguments = arguments;
//		}
//		
//		public override object AcceptVisitor(IAstVisitor visitor, object data) {
//			return visitor.VisitInvocationExpression(this, data);
//		}
//		
//		public override string ToString() {
//			return string.Format("[InvocationExpression TargetObject={0} Arguments={1}]", TargetObject, GetCollectionString(Arguments));
//		}
//	}
//	
//	public class LabelStatement : Statement {
//		
//		string label;
//		
//		public string Label {
//			get {
//				return label;
//			}
//			set {
//				label = value ?? "";
//			}
//		}
//		
//		public LabelStatement(string label) {
//			Label = label;
//		}
//		
//		public override object AcceptVisitor(IAstVisitor visitor, object data) {
//			return visitor.VisitLabelStatement(this, data);
//		}
//		
//		public override string ToString() {
//			return string.Format("[LabelStatement Label={0}]", Label);
//		}
//	}
//	
//	public class LambdaExpression : Expression {
//		
//		List<ParameterDeclarationExpression> parameters;
//		
//		Statement statementBody;
//		
//		Expression expressionBody;
//		
//		TypeReference returnType;
//		
//		public List<ParameterDeclarationExpression> Parameters {
//			get {
//				return parameters;
//			}
//			set {
//				parameters = value ?? new List<ParameterDeclarationExpression>();
//			}
//		}
//		
//		public Statement StatementBody {
//			get {
//				return statementBody;
//			}
//			set {
//				statementBody = value ?? Statement.Null;
//				if (!statementBody.IsNull) statementBody.Parent = this;
//			}
//		}
//		
//		public Expression ExpressionBody {
//			get {
//				return expressionBody;
//			}
//			set {
//				expressionBody = value ?? Expression.Null;
//				if (!expressionBody.IsNull) expressionBody.Parent = this;
//			}
//		}
//		
//		public TypeReference ReturnType {
//			get {
//				return returnType;
//			}
//			set {
//				returnType = value ?? TypeReference.Null;
//				if (!returnType.IsNull) returnType.Parent = this;
//			}
//		}
//		
//		public LambdaExpression() {
//			parameters = new List<ParameterDeclarationExpression>();
//			statementBody = Statement.Null;
//			expressionBody = Expression.Null;
//			returnType = TypeReference.Null;
//		}
//		
//		public Location ExtendedEndLocation { get; set; }
//		
//		public override object AcceptVisitor(IAstVisitor visitor, object data) {
//			return visitor.VisitLambdaExpression(this, data);
//		}
//		
//		public override string ToString() {
//			return string.Format("[LambdaExpression Parameters={0} StatementBody={1} ExpressionBody={2} ReturnType=" +
//			                     "{3}]", GetCollectionString(Parameters), StatementBody, ExpressionBody, ReturnType);
//		}
//	}
//	
//	public class LockStatement : StatementWithEmbeddedStatement {
//		
//		Expression lockExpression;
//		
//		public Expression LockExpression {
//			get {
//				return lockExpression;
//			}
//			set {
//				lockExpression = value ?? Expression.Null;
//				if (!lockExpression.IsNull) lockExpression.Parent = this;
//			}
//		}
//		
//		public LockStatement(Expression lockExpression, Statement embeddedStatement) {
//			LockExpression = lockExpression;
//			EmbeddedStatement = embeddedStatement;
//		}
//		
//		public override object AcceptVisitor(IAstVisitor visitor, object data) {
//			return visitor.VisitLockStatement(this, data);
//		}
//		
//		public override string ToString() {
//			return string.Format("[LockStatement LockExpression={0} EmbeddedStatement={1}]", LockExpression, EmbeddedStatement);
//		}
//	}
//	
//	public class MemberInitializerExpression : Expression {
//		
//		string name;
//		
//		bool isKey;
//		
//		Expression expression;
//		
//		public string Name {
//			get {
//				return name;
//			}
//			set {
//				name = value ?? "";
//			}
//		}
//		
//		public bool IsKey {
//			get {
//				return isKey;
//			}
//			set {
//				isKey = value;
//			}
//		}
//		
//		public Expression Expression {
//			get {
//				return expression;
//			}
//			set {
//				expression = value ?? Expression.Null;
//				if (!expression.IsNull) expression.Parent = this;
//			}
//		}
//		
//		public MemberInitializerExpression() {
//			name = "";
//			expression = Expression.Null;
//		}
//		
//		public MemberInitializerExpression(string name, Expression expression) {
//			Name = name;
//			Expression = expression;
//		}
//		
//		public override object AcceptVisitor(IAstVisitor visitor, object data) {
//			return visitor.VisitMemberInitializerExpression(this, data);
//		}
//		
//		public override string ToString() {
//			return string.Format("[MemberInitializerExpression Name={0} IsKey={1} Expression={2}]", Name, IsKey, Expression);
//		}
//	}
//	
//	public abstract class MemberNode : ParametrizedNode {
//		
//		List<InterfaceImplementation> interfaceImplementations;
//		
//		TypeReference typeReference;
//		
//		public List<InterfaceImplementation> InterfaceImplementations {
//			get {
//				return interfaceImplementations;
//			}
//			set {
//				interfaceImplementations = value ?? new List<InterfaceImplementation>();
//			}
//		}
//		
//		public TypeReference TypeReference {
//			get {
//				return typeReference;
//			}
//			set {
//				typeReference = value ?? TypeReference.Null;
//				if (!typeReference.IsNull) typeReference.Parent = this;
//			}
//		}
//		
//		protected MemberNode() {
//			interfaceImplementations = new List<InterfaceImplementation>();
//			typeReference = TypeReference.Null;
//		}
//		
//		protected MemberNode(Modifiers modifier, List<AttributeSection> attributes, string name, List<ParameterDeclarationExpression> parameters) {
//			Modifier = modifier;
//			Attributes = attributes;
//			Name = name;
//			Parameters = parameters;
//			interfaceImplementations = new List<InterfaceImplementation>();
//			typeReference = TypeReference.Null;
//		}
//	}
//	
//	public class MemberReferenceExpression : Expression {
//		
//		Expression targetObject;
//		
//		string memberName;
//		
//		List<TypeReference> typeArguments;
//		
//		public Expression TargetObject {
//			get {
//				return targetObject;
//			}
//			set {
//				targetObject = value ?? Expression.Null;
//				if (!targetObject.IsNull) targetObject.Parent = this;
//			}
//		}
//		
//		public string MemberName {
//			get {
//				return memberName;
//			}
//			set {
//				memberName = value ?? "";
//			}
//		}
//		
//		public List<TypeReference> TypeArguments {
//			get {
//				return typeArguments;
//			}
//			set {
//				typeArguments = value ?? new List<TypeReference>();
//			}
//		}
//		
//		public MemberReferenceExpression(Expression targetObject, string memberName) {
//			TargetObject = targetObject;
//			MemberName = memberName;
//			typeArguments = new List<TypeReference>();
//		}
//		
//		public override object AcceptVisitor(IAstVisitor visitor, object data) {
//			return visitor.VisitMemberReferenceExpression(this, data);
//		}
//		
//		public override string ToString() {
//			return string.Format("[MemberReferenceExpression TargetObject={0} MemberName={1} TypeArguments={2}]", TargetObject, MemberName, GetCollectionString(TypeArguments));
//		}
//	}
//	
//	public class MethodDeclaration : MemberNode {
//		
//		BlockStatement body;
//		
//		List<string> handlesClause;
//		
//		List<TemplateDefinition> templates;
//		
//		bool isExtensionMethod;
//		
//		public BlockStatement Body {
//			get {
//				return body;
//			}
//			set {
//				body = value ?? BlockStatement.Null;
//				if (!body.IsNull) body.Parent = this;
//			}
//		}
//		
//		public List<string> HandlesClause {
//			get {
//				return handlesClause;
//			}
//			set {
//				handlesClause = value ?? new List<String>();
//			}
//		}
//		
//		public List<TemplateDefinition> Templates {
//			get {
//				return templates;
//			}
//			set {
//				templates = value ?? new List<TemplateDefinition>();
//			}
//		}
//		
//		public bool IsExtensionMethod {
//			get {
//				return isExtensionMethod;
//			}
//			set {
//				isExtensionMethod = value;
//			}
//		}
//		
//		public MethodDeclaration() {
//			body = BlockStatement.Null;
//			handlesClause = new List<String>();
//			templates = new List<TemplateDefinition>();
//		}
//		
//		public override object AcceptVisitor(IAstVisitor visitor, object data) {
//			return visitor.VisitMethodDeclaration(this, data);
//		}
//		
//		public override string ToString() {
//			return string.Format("[MethodDeclaration Body={0} HandlesClause={1} Templates={2} IsExtensionMethod={3}" +
//			                     " InterfaceImplementations={4} TypeReference={5} Name={6} Parameters={7} Attribut" +
//			                     "es={8} Modifier={9}]", Body, GetCollectionString(HandlesClause), GetCollectionString(Templates), IsExtensionMethod, GetCollectionString(InterfaceImplementations), TypeReference, Name, GetCollectionString(Parameters), GetCollectionString(Attributes), Modifier);
//		}
//	}
//	
//	public class NamedArgumentExpression : Expression {
//		
//		string name;
//		
//		Expression expression;
//		
//		public string Name {
//			get {
//				return name;
//			}
//			set {
//				name = value ?? "";
//			}
//		}
//		
//		public Expression Expression {
//			get {
//				return expression;
//			}
//			set {
//				expression = value ?? Expression.Null;
//				if (!expression.IsNull) expression.Parent = this;
//			}
//		}
//		
//		public NamedArgumentExpression() {
//			name = "";
//			expression = Expression.Null;
//		}
//		
//		public NamedArgumentExpression(string name, Expression expression) {
//			Name = name;
//			Expression = expression;
//		}
//		
//		public override object AcceptVisitor(IAstVisitor visitor, object data) {
//			return visitor.VisitNamedArgumentExpression(this, data);
//		}
//		
//		public override string ToString() {
//			return string.Format("[NamedArgumentExpression Name={0} Expression={1}]", Name, Expression);
//		}
//	}
//	
//	public class NamespaceDeclaration : AbstractNode {
//		
//		string name;
//		
//		public string Name {
//			get {
//				return name;
//			}
//			set {
//				name = value ?? "";
//			}
//		}
//		
//		public NamespaceDeclaration(string name) {
//			Name = name;
//		}
//		
//		public override object AcceptVisitor(IAstVisitor visitor, object data) {
//			return visitor.VisitNamespaceDeclaration(this, data);
//		}
//		
//		public override string ToString() {
//			return string.Format("[NamespaceDeclaration Name={0}]", Name);
//		}
//	}
//	
//	public class ObjectCreateExpression : Expression {
//		
//		TypeReference createType;
//		
//		List<Expression> parameters;
//		
//		CollectionInitializerExpression objectInitializer;
//		
//		public TypeReference CreateType {
//			get {
//				return createType;
//			}
//			set {
//				createType = value ?? TypeReference.Null;
//				if (!createType.IsNull) createType.Parent = this;
//			}
//		}
//		
//		public List<Expression> Parameters {
//			get {
//				return parameters;
//			}
//			set {
//				parameters = value ?? new List<Expression>();
//			}
//		}
//		
//		public CollectionInitializerExpression ObjectInitializer {
//			get {
//				return objectInitializer;
//			}
//			set {
//				objectInitializer = value ?? CollectionInitializerExpression.Null;
//				if (!objectInitializer.IsNull) objectInitializer.Parent = this;
//			}
//		}
//		
//		public ObjectCreateExpression(TypeReference createType, List<Expression> parameters) {
//			CreateType = createType;
//			Parameters = parameters;
//			objectInitializer = CollectionInitializerExpression.Null;
//		}
//		
//		public bool IsAnonymousType {
//			get {
//				return createType.IsNull || string.IsNullOrEmpty(createType.Type);
//			}
//		}
//		
//		public override object AcceptVisitor(IAstVisitor visitor, object data) {
//			return visitor.VisitObjectCreateExpression(this, data);
//		}
//		
//		public override string ToString() {
//			return string.Format("[ObjectCreateExpression CreateType={0} Parameters={1} ObjectInitializer={2}]", CreateType, GetCollectionString(Parameters), ObjectInitializer);
//		}
//	}
//	
//	public class OnErrorStatement : StatementWithEmbeddedStatement {
//		
//		public OnErrorStatement(Statement embeddedStatement) {
//			EmbeddedStatement = embeddedStatement;
//		}
//		
//		public override object AcceptVisitor(IAstVisitor visitor, object data) {
//			return visitor.VisitOnErrorStatement(this, data);
//		}
//		
//		public override string ToString() {
//			return string.Format("[OnErrorStatement EmbeddedStatement={0}]", EmbeddedStatement);
//		}
//	}
//	
//	public class OperatorDeclaration : MethodDeclaration {
//		
//		ConversionType conversionType;
//		
//		OverloadableOperatorType overloadableOperator;
//		
//		public ConversionType ConversionType {
//			get {
//				return conversionType;
//			}
//			set {
//				conversionType = value;
//			}
//		}
//		
//		public OverloadableOperatorType OverloadableOperator {
//			get {
//				return overloadableOperator;
//			}
//			set {
//				overloadableOperator = value;
//			}
//		}
//		
//		public OperatorDeclaration() {
//		}
//		
//		public bool IsConversionOperator {
//			get {
//				return conversionType != ConversionType.None;
//			}
//		}
//		
//		public override object AcceptVisitor(IAstVisitor visitor, object data) {
//			return visitor.VisitOperatorDeclaration(this, data);
//		}
//		
//		public override string ToString() {
//			return string.Format("[OperatorDeclaration ConversionType={0} OverloadableOperator={1} Body={2} Handles" +
//			                     "Clause={3} Templates={4} IsExtensionMethod={5} InterfaceImplementations={6} Type" +
//			                     "Reference={7} Name={8} Parameters={9} Attributes={10} Modifier={11}]", ConversionType, OverloadableOperator, Body, GetCollectionString(HandlesClause), GetCollectionString(Templates), IsExtensionMethod, GetCollectionString(InterfaceImplementations), TypeReference, Name, GetCollectionString(Parameters), GetCollectionString(Attributes), Modifier);
//		}
//	}
//	
//	public class ParameterDeclarationExpression : Expression {
//		
//		List<AttributeSection> attributes;
//		
//		string parameterName;
//		
//		TypeReference typeReference;
//		
//		ParameterModifiers paramModifier;
//		
//		Expression defaultValue;
//		
//		public List<AttributeSection> Attributes {
//			get {
//				return attributes;
//			}
//			set {
//				attributes = value ?? new List<AttributeSection>();
//			}
//		}
//		
//		public string ParameterName {
//			get {
//				return parameterName;
//			}
//			set {
//				parameterName = string.IsNullOrEmpty(value) ? "?" : value;
//			}
//		}
//		
//		public TypeReference TypeReference {
//			get {
//				return typeReference;
//			}
//			set {
//				typeReference = value ?? TypeReference.Null;
//				if (!typeReference.IsNull) typeReference.Parent = this;
//			}
//		}
//		
//		public ParameterModifiers ParamModifier {
//			get {
//				return paramModifier;
//			}
//			set {
//				paramModifier = value;
//			}
//		}
//		
//		public Expression DefaultValue {
//			get {
//				return defaultValue;
//			}
//			set {
//				defaultValue = value ?? Expression.Null;
//				if (!defaultValue.IsNull) defaultValue.Parent = this;
//			}
//		}
//		
//		public ParameterDeclarationExpression(TypeReference typeReference, string parameterName) {
//			TypeReference = typeReference;
//			ParameterName = parameterName;
//			attributes = new List<AttributeSection>();
//			defaultValue = Expression.Null;
//		}
//		
//		public ParameterDeclarationExpression(TypeReference typeReference, string parameterName, ParameterModifiers paramModifier) {
//			TypeReference = typeReference;
//			ParameterName = parameterName;
//			ParamModifier = paramModifier;
//			attributes = new List<AttributeSection>();
//			defaultValue = Expression.Null;
//		}
//		
//		public ParameterDeclarationExpression(TypeReference typeReference, string parameterName, ParameterModifiers paramModifier, Expression defaultValue) {
//			TypeReference = typeReference;
//			ParameterName = parameterName;
//			ParamModifier = paramModifier;
//			DefaultValue = defaultValue;
//			attributes = new List<AttributeSection>();
//		}
//		
//		public override object AcceptVisitor(IAstVisitor visitor, object data) {
//			return visitor.VisitParameterDeclarationExpression(this, data);
//		}
//		
//		public override string ToString() {
//			return string.Format("[ParameterDeclarationExpression Attributes={0} ParameterName={1} TypeReference={2" +
//			                     "} ParamModifier={3} DefaultValue={4}]", GetCollectionString(Attributes), ParameterName, TypeReference, ParamModifier, DefaultValue);
//		}
//	}
//	
//	public abstract class ParametrizedNode : AttributedNode {
//		
//		string name;
//		
//		List<ParameterDeclarationExpression> parameters;
//		
//		public string Name {
//			get {
//				return name;
//			}
//			set {
//				name = value ?? "";
//			}
//		}
//		
//		public List<ParameterDeclarationExpression> Parameters {
//			get {
//				return parameters;
//			}
//			set {
//				parameters = value ?? new List<ParameterDeclarationExpression>();
//			}
//		}
//		
//		protected ParametrizedNode() {
//			name = "";
//			parameters = new List<ParameterDeclarationExpression>();
//		}
//		
//		protected ParametrizedNode(Modifiers modifier, List<AttributeSection> attributes, string name, List<ParameterDeclarationExpression> parameters) {
//			Modifier = modifier;
//			Attributes = attributes;
//			Name = name;
//			Parameters = parameters;
//		}
//	}
//	
//	public class ParenthesizedExpression : Expression {
//		
//		Expression expression;
//		
//		public Expression Expression {
//			get {
//				return expression;
//			}
//			set {
//				expression = value ?? Expression.Null;
//				if (!expression.IsNull) expression.Parent = this;
//			}
//		}
//		
//		public ParenthesizedExpression(Expression expression) {
//			Expression = expression;
//		}
//		
//		public override object AcceptVisitor(IAstVisitor visitor, object data) {
//			return visitor.VisitParenthesizedExpression(this, data);
//		}
//		
//		public override string ToString() {
//			return string.Format("[ParenthesizedExpression Expression={0}]", Expression);
//		}
//	}
//	
//	public class PropertyDeclaration : MemberNode {
//		
//		Location bodyStart;
//		
//		Location bodyEnd;
//		
//		PropertyGetRegion getRegion;
//		
//		PropertySetRegion setRegion;
//		
//		Expression initializer;
//		
//		public Location BodyStart {
//			get {
//				return bodyStart;
//			}
//			set {
//				bodyStart = value;
//			}
//		}
//		
//		public Location BodyEnd {
//			get {
//				return bodyEnd;
//			}
//			set {
//				bodyEnd = value;
//			}
//		}
//		
//		public PropertyGetRegion GetRegion {
//			get {
//				return getRegion;
//			}
//			set {
//				getRegion = value ?? PropertyGetRegion.Null;
//				if (!getRegion.IsNull) getRegion.Parent = this;
//			}
//		}
//		
//		public PropertySetRegion SetRegion {
//			get {
//				return setRegion;
//			}
//			set {
//				setRegion = value ?? PropertySetRegion.Null;
//				if (!setRegion.IsNull) setRegion.Parent = this;
//			}
//		}
//		
//		public Expression Initializer {
//			get {
//				return initializer;
//			}
//			set {
//				initializer = value ?? Expression.Null;
//				if (!initializer.IsNull) initializer.Parent = this;
//			}
//		}
//		
//		public PropertyDeclaration(Modifiers modifier, List<AttributeSection> attributes, string name, List<ParameterDeclarationExpression> parameters) {
//			Modifier = modifier;
//			Attributes = attributes;
//			Name = name;
//			Parameters = parameters;
//			bodyStart = Location.Empty;
//			bodyEnd = Location.Empty;
//			getRegion = PropertyGetRegion.Null;
//			setRegion = PropertySetRegion.Null;
//			initializer = Expression.Null;
//		}
//		
//		public bool IsReadOnly {
//			get {
//				return HasGetRegion && !HasSetRegion;
//			}
//		}
//		
//		public bool HasGetRegion {
//			get {
//				return !getRegion.IsNull;
//			}
//		}
//		
//		public bool IsWriteOnly {
//			get {
//				return !HasGetRegion && HasSetRegion;
//			}
//		}
//		
//		public bool IsIndexer {
//			get {
//				return (Modifier & Modifiers.Default) != 0;
//			}
//		}
//		
//
//		internal PropertyDeclaration(string name, TypeReference typeReference, Modifiers modifier, List<AttributeSection> attributes) : this(modifier, attributes, name, null)
//		{
//			this.TypeReference = typeReference;
//			if ((modifier & Modifiers.ReadOnly) != Modifiers.ReadOnly) {
//				this.SetRegion = new PropertySetRegion(null, null);
//			}
//			if ((modifier & Modifiers.WriteOnly) != Modifiers.WriteOnly) {
//				this.GetRegion = new PropertyGetRegion(null, null);
//			}
//		}
//		
//		public bool HasSetRegion {
//			get {
//				return !setRegion.IsNull;
//			}
//		}
//		
//		public override object AcceptVisitor(IAstVisitor visitor, object data) {
//			return visitor.VisitPropertyDeclaration(this, data);
//		}
//		
//		public override string ToString() {
//			return string.Format("[PropertyDeclaration BodyStart={0} BodyEnd={1} GetRegion={2} SetRegion={3} Initia" +
//			                     "lizer={4} InterfaceImplementations={5} TypeReference={6} Name={7} Parameters={8}" +
//			                     " Attributes={9} Modifier={10}]", BodyStart, BodyEnd, GetRegion, SetRegion, Initializer, GetCollectionString(InterfaceImplementations), TypeReference, Name, GetCollectionString(Parameters), GetCollectionString(Attributes), Modifier);
//		}
//	}
//	
//	public class PropertyGetRegion : PropertyGetSetRegion {
//		
//		public PropertyGetRegion(BlockStatement block, List<AttributeSection> attributes) :
//			base(block, attributes) {
//		}
//		
//		public static PropertyGetRegion Null {
//			get {
//				return NullPropertyGetRegion.Instance;
//			}
//		}
//		
//		public override object AcceptVisitor(IAstVisitor visitor, object data) {
//			return visitor.VisitPropertyGetRegion(this, data);
//		}
//		
//		public override string ToString() {
//			return string.Format("[PropertyGetRegion Block={0} Attributes={1} Modifier={2}]", Block, GetCollectionString(Attributes), Modifier);
//		}
//	}
//	
//	internal sealed class NullPropertyGetRegion : PropertyGetRegion {
//		
//		private NullPropertyGetRegion() :
//			base(null, null) {
//		}
//		
//		internal static NullPropertyGetRegion Instance = new NullPropertyGetRegion();
//		
//		public override bool IsNull {
//			get {
//				return true;
//			}
//		}
//		
//		public override object AcceptVisitor(IAstVisitor visitor, object data) {
//			return null;
//		}
//		
//		public override string ToString() {
//			return "[NullPropertyGetRegion]";
//		}
//	}
//	
//	public abstract class PropertyGetSetRegion : AttributedNode, INullable {
//		
//		BlockStatement block;
//		
//		public BlockStatement Block {
//			get {
//				return block;
//			}
//			set {
//				block = value ?? BlockStatement.Null;
//				if (!block.IsNull) block.Parent = this;
//			}
//		}
//		
//		protected PropertyGetSetRegion(BlockStatement block, List<AttributeSection> attributes) {
//			Block = block;
//			Attributes = attributes;
//		}
//		
//		public virtual bool IsNull {
//			get {
//				return false;
//			}
//		}
//	}
//	
//	public class PropertySetRegion : PropertyGetSetRegion {
//		
//		List<ParameterDeclarationExpression> parameters;
//		
//		public List<ParameterDeclarationExpression> Parameters {
//			get {
//				return parameters;
//			}
//			set {
//				parameters = value ?? new List<ParameterDeclarationExpression>();
//			}
//		}
//		
//		public PropertySetRegion(BlockStatement block, List<AttributeSection> attributes) :
//			base(block, attributes) {
//			parameters = new List<ParameterDeclarationExpression>();
//		}
//		
//		public static PropertySetRegion Null {
//			get {
//				return NullPropertySetRegion.Instance;
//			}
//		}
//		
//		public override object AcceptVisitor(IAstVisitor visitor, object data) {
//			return visitor.VisitPropertySetRegion(this, data);
//		}
//		
//		public override string ToString() {
//			return string.Format("[PropertySetRegion Parameters={0} Block={1} Attributes={2} Modifier={3}]", GetCollectionString(Parameters), Block, GetCollectionString(Attributes), Modifier);
//		}
//	}
//	
//	internal sealed class NullPropertySetRegion : PropertySetRegion {
//		
//		private NullPropertySetRegion() :
//			base(null, null) {
//		}
//		
//		internal static NullPropertySetRegion Instance = new NullPropertySetRegion();
//		
//		public override bool IsNull {
//			get {
//				return true;
//			}
//		}
//		
//		public override object AcceptVisitor(IAstVisitor visitor, object data) {
//			return null;
//		}
//		
//		public override string ToString() {
//			return "[NullPropertySetRegion]";
//		}
//	}
//	
//	public class QueryExpression : Expression {
//		
//		List<QueryExpressionClause> clauses;
//		
//		public List<QueryExpressionClause> Clauses {
//			get {
//				return clauses;
//			}
//			set {
//				clauses = value ?? new List<QueryExpressionClause>();
//			}
//		}
//		
//		public QueryExpression() {
//			clauses = new List<QueryExpressionClause>();
//		}
//		
//		public override object AcceptVisitor(IAstVisitor visitor, object data) {
//			return visitor.VisitQueryExpression(this, data);
//		}
//		
//		public override string ToString() {
//			return string.Format("[QueryExpression Clauses={0}]", GetCollectionString(Clauses));
//		}
//	}
//	
//	public class QueryExpressionAggregateClause : QueryExpressionClause {
//		
//		CollectionRangeVariable source;
//		
//		List<QueryExpressionClause> middleClauses;
//		
//		List<ExpressionRangeVariable> intoVariables;
//		
//		public CollectionRangeVariable Source {
//			get {
//				return source;
//			}
//			set {
//				source = value ?? CollectionRangeVariable.Null;
//				if (!source.IsNull) source.Parent = this;
//			}
//		}
//		
//		public List<QueryExpressionClause> MiddleClauses {
//			get {
//				return middleClauses;
//			}
//			set {
//				middleClauses = value ?? new List<QueryExpressionClause>();
//			}
//		}
//		
//		public List<ExpressionRangeVariable> IntoVariables {
//			get {
//				return intoVariables;
//			}
//			set {
//				intoVariables = value ?? new List<ExpressionRangeVariable>();
//			}
//		}
//		
//		public QueryExpressionAggregateClause() {
//			source = CollectionRangeVariable.Null;
//			middleClauses = new List<QueryExpressionClause>();
//			intoVariables = new List<ExpressionRangeVariable>();
//		}
//		
//		public override object AcceptVisitor(IAstVisitor visitor, object data) {
//			return visitor.VisitQueryExpressionAggregateClause(this, data);
//		}
//		
//		public override string ToString() {
//			return string.Format("[QueryExpressionAggregateClause Source={0} MiddleClauses={1} IntoVariables={2}]", Source, GetCollectionString(MiddleClauses), GetCollectionString(IntoVariables));
//		}
//	}
//	
//	public abstract class QueryExpressionClause : AbstractNode, INullable {
//		
//		protected QueryExpressionClause() {
//		}
//		
//		public virtual bool IsNull {
//			get {
//				return false;
//			}
//		}
//		
//		public static QueryExpressionClause Null {
//			get {
//				return NullQueryExpressionClause.Instance;
//			}
//		}
//	}
//	
//	internal sealed class NullQueryExpressionClause : QueryExpressionClause {
//		
//		internal static NullQueryExpressionClause Instance = new NullQueryExpressionClause();
//		
//		public override bool IsNull {
//			get {
//				return true;
//			}
//		}
//		
//		public override object AcceptVisitor(IAstVisitor visitor, object data) {
//			return null;
//		}
//		
//		public override string ToString() {
//			return "[NullQueryExpressionClause]";
//		}
//	}
//	
//	public class QueryExpressionDistinctClause : QueryExpressionClause {
//		
//		public QueryExpressionDistinctClause() {
//		}
//		
//		public override object AcceptVisitor(IAstVisitor visitor, object data) {
//			return visitor.VisitQueryExpressionDistinctClause(this, data);
//		}
//		
//		public override string ToString() {
//			return "[QueryExpressionDistinctClause]";
//		}
//	}
//	
//	public class QueryExpressionFromClause : QueryExpressionClause {
//		
//		List<CollectionRangeVariable> sources;
//		
//		public List<CollectionRangeVariable> Sources {
//			get {
//				return sources;
//			}
//			set {
//				sources = value ?? new List<CollectionRangeVariable>();
//			}
//		}
//		
//		public QueryExpressionFromClause() {
//			sources = new List<CollectionRangeVariable>();
//		}
//		
//		public new static QueryExpressionFromClause Null {
//			get {
//				return NullQueryExpressionFromClause.Instance;
//			}
//		}
//		
//		public override object AcceptVisitor(IAstVisitor visitor, object data) {
//			return visitor.VisitQueryExpressionFromClause(this, data);
//		}
//		
//		public override string ToString() {
//			return string.Format("[QueryExpressionFromClause Sources={0}]", GetCollectionString(Sources));
//		}
//	}
//	
//	internal sealed class NullQueryExpressionFromClause : QueryExpressionFromClause {
//		
//		internal static NullQueryExpressionFromClause Instance = new NullQueryExpressionFromClause();
//		
//		public override bool IsNull {
//			get {
//				return true;
//			}
//		}
//		
//		public override object AcceptVisitor(IAstVisitor visitor, object data) {
//			return null;
//		}
//		
//		public override string ToString() {
//			return "[NullQueryExpressionFromClause]";
//		}
//	}
//	
//	public class QueryExpressionGroupClause : QueryExpressionClause {
//		
//		Expression projection;
//		
//		Expression groupBy;
//		
//		public Expression Projection {
//			get {
//				return projection;
//			}
//			set {
//				projection = value ?? Expression.Null;
//				if (!projection.IsNull) projection.Parent = this;
//			}
//		}
//		
//		public Expression GroupBy {
//			get {
//				return groupBy;
//			}
//			set {
//				groupBy = value ?? Expression.Null;
//				if (!groupBy.IsNull) groupBy.Parent = this;
//			}
//		}
//		
//		public QueryExpressionGroupClause() {
//			projection = Expression.Null;
//			groupBy = Expression.Null;
//		}
//		
//		public override object AcceptVisitor(IAstVisitor visitor, object data) {
//			return visitor.VisitQueryExpressionGroupClause(this, data);
//		}
//		
//		public override string ToString() {
//			return string.Format("[QueryExpressionGroupClause Projection={0} GroupBy={1}]", Projection, GroupBy);
//		}
//	}
//	
//	public class QueryExpressionGroupJoinVBClause : QueryExpressionClause {
//		
//		QueryExpressionJoinVBClause joinClause;
//		
//		List<ExpressionRangeVariable> intoVariables;
//		
//		public QueryExpressionJoinVBClause JoinClause {
//			get {
//				return joinClause;
//			}
//			set {
//				joinClause = value ?? QueryExpressionJoinVBClause.Null;
//				if (!joinClause.IsNull) joinClause.Parent = this;
//			}
//		}
//		
//		public List<ExpressionRangeVariable> IntoVariables {
//			get {
//				return intoVariables;
//			}
//			set {
//				intoVariables = value ?? new List<ExpressionRangeVariable>();
//			}
//		}
//		
//		public QueryExpressionGroupJoinVBClause() {
//			joinClause = QueryExpressionJoinVBClause.Null;
//			intoVariables = new List<ExpressionRangeVariable>();
//		}
//		
//		public override object AcceptVisitor(IAstVisitor visitor, object data) {
//			return visitor.VisitQueryExpressionGroupJoinVBClause(this, data);
//		}
//		
//		public override string ToString() {
//			return string.Format("[QueryExpressionGroupJoinVBClause JoinClause={0} IntoVariables={1}]", JoinClause, GetCollectionString(IntoVariables));
//		}
//	}
//	
//	public class QueryExpressionGroupVBClause : QueryExpressionClause {
//		
//		List<ExpressionRangeVariable> groupVariables;
//		
//		List<ExpressionRangeVariable> byVariables;
//		
//		List<ExpressionRangeVariable> intoVariables;
//		
//		public List<ExpressionRangeVariable> GroupVariables {
//			get {
//				return groupVariables;
//			}
//			set {
//				groupVariables = value ?? new List<ExpressionRangeVariable>();
//			}
//		}
//		
//		public List<ExpressionRangeVariable> ByVariables {
//			get {
//				return byVariables;
//			}
//			set {
//				byVariables = value ?? new List<ExpressionRangeVariable>();
//			}
//		}
//		
//		public List<ExpressionRangeVariable> IntoVariables {
//			get {
//				return intoVariables;
//			}
//			set {
//				intoVariables = value ?? new List<ExpressionRangeVariable>();
//			}
//		}
//		
//		public QueryExpressionGroupVBClause() {
//			groupVariables = new List<ExpressionRangeVariable>();
//			byVariables = new List<ExpressionRangeVariable>();
//			intoVariables = new List<ExpressionRangeVariable>();
//		}
//		
//		public override object AcceptVisitor(IAstVisitor visitor, object data) {
//			return visitor.VisitQueryExpressionGroupVBClause(this, data);
//		}
//		
//		public override string ToString() {
//			return string.Format("[QueryExpressionGroupVBClause GroupVariables={0} ByVariables={1} IntoVariables={2" +
//			                     "}]", GetCollectionString(GroupVariables), GetCollectionString(ByVariables), GetCollectionString(IntoVariables));
//		}
//	}
//	
//	public class QueryExpressionJoinClause : QueryExpressionClause {
//		
//		Expression onExpression;
//		
//		Expression equalsExpression;
//		
//		CollectionRangeVariable source;
//		
//		string intoIdentifier;
//		
//		public Expression OnExpression {
//			get {
//				return onExpression;
//			}
//			set {
//				onExpression = value ?? Expression.Null;
//				if (!onExpression.IsNull) onExpression.Parent = this;
//			}
//		}
//		
//		public Expression EqualsExpression {
//			get {
//				return equalsExpression;
//			}
//			set {
//				equalsExpression = value ?? Expression.Null;
//				if (!equalsExpression.IsNull) equalsExpression.Parent = this;
//			}
//		}
//		
//		public CollectionRangeVariable Source {
//			get {
//				return source;
//			}
//			set {
//				source = value ?? CollectionRangeVariable.Null;
//				if (!source.IsNull) source.Parent = this;
//			}
//		}
//		
//		public string IntoIdentifier {
//			get {
//				return intoIdentifier;
//			}
//			set {
//				intoIdentifier = value ?? "";
//			}
//		}
//		
//		public QueryExpressionJoinClause() {
//			onExpression = Expression.Null;
//			equalsExpression = Expression.Null;
//			source = CollectionRangeVariable.Null;
//			intoIdentifier = "";
//		}
//		
//		public override object AcceptVisitor(IAstVisitor visitor, object data) {
//			return visitor.VisitQueryExpressionJoinClause(this, data);
//		}
//		
//		public override string ToString() {
//			return string.Format("[QueryExpressionJoinClause OnExpression={0} EqualsExpression={1} Source={2} IntoI" +
//			                     "dentifier={3}]", OnExpression, EqualsExpression, Source, IntoIdentifier);
//		}
//	}
//	
//	public class QueryExpressionJoinConditionVB : AbstractNode {
//		
//		Expression leftSide;
//		
//		Expression rightSide;
//		
//		public Expression LeftSide {
//			get {
//				return leftSide;
//			}
//			set {
//				leftSide = value ?? Expression.Null;
//				if (!leftSide.IsNull) leftSide.Parent = this;
//			}
//		}
//		
//		public Expression RightSide {
//			get {
//				return rightSide;
//			}
//			set {
//				rightSide = value ?? Expression.Null;
//				if (!rightSide.IsNull) rightSide.Parent = this;
//			}
//		}
//		
//		public QueryExpressionJoinConditionVB() {
//			leftSide = Expression.Null;
//			rightSide = Expression.Null;
//		}
//		
//		public override object AcceptVisitor(IAstVisitor visitor, object data) {
//			return visitor.VisitQueryExpressionJoinConditionVB(this, data);
//		}
//		
//		public override string ToString() {
//			return string.Format("[QueryExpressionJoinConditionVB LeftSide={0} RightSide={1}]", LeftSide, RightSide);
//		}
//	}
//	
//	public class QueryExpressionJoinVBClause : QueryExpressionClause {
//		
//		CollectionRangeVariable joinVariable;
//		
//		QueryExpressionJoinVBClause subJoin;
//		
//		List<QueryExpressionJoinConditionVB> conditions;
//		
//		public CollectionRangeVariable JoinVariable {
//			get {
//				return joinVariable;
//			}
//			set {
//				joinVariable = value ?? CollectionRangeVariable.Null;
//				if (!joinVariable.IsNull) joinVariable.Parent = this;
//			}
//		}
//		
//		public QueryExpressionJoinVBClause SubJoin {
//			get {
//				return subJoin;
//			}
//			set {
//				subJoin = value ?? QueryExpressionJoinVBClause.Null;
//				if (!subJoin.IsNull) subJoin.Parent = this;
//			}
//		}
//		
//		public List<QueryExpressionJoinConditionVB> Conditions {
//			get {
//				return conditions;
//			}
//			set {
//				conditions = value ?? new List<QueryExpressionJoinConditionVB>();
//			}
//		}
//		
//		public QueryExpressionJoinVBClause() {
//			joinVariable = CollectionRangeVariable.Null;
//			subJoin = QueryExpressionJoinVBClause.Null;
//			conditions = new List<QueryExpressionJoinConditionVB>();
//		}
//		
//		public new static QueryExpressionJoinVBClause Null {
//			get {
//				return NullQueryExpressionJoinVBClause.Instance;
//			}
//		}
//		
//		public override object AcceptVisitor(IAstVisitor visitor, object data) {
//			return visitor.VisitQueryExpressionJoinVBClause(this, data);
//		}
//		
//		public override string ToString() {
//			return string.Format("[QueryExpressionJoinVBClause JoinVariable={0} SubJoin={1} Conditions={2}]", JoinVariable, SubJoin, GetCollectionString(Conditions));
//		}
//	}
//	
//	internal sealed class NullQueryExpressionJoinVBClause : QueryExpressionJoinVBClause {
//		
//		internal static NullQueryExpressionJoinVBClause Instance = new NullQueryExpressionJoinVBClause();
//		
//		public override bool IsNull {
//			get {
//				return true;
//			}
//		}
//		
//		public override object AcceptVisitor(IAstVisitor visitor, object data) {
//			return null;
//		}
//		
//		public override string ToString() {
//			return "[NullQueryExpressionJoinVBClause]";
//		}
//	}
//	
//	public class QueryExpressionLetClause : QueryExpressionClause {
//		
//		List<ExpressionRangeVariable> variables;
//		
//		public List<ExpressionRangeVariable> Variables {
//			get {
//				return variables;
//			}
//			set {
//				variables = value ?? new List<ExpressionRangeVariable>();
//			}
//		}
//		
//		public QueryExpressionLetClause() {
//			variables = new List<ExpressionRangeVariable>();
//		}
//		
//		public override object AcceptVisitor(IAstVisitor visitor, object data) {
//			return visitor.VisitQueryExpressionLetVBClause(this, data);
//		}
//		
//		public override string ToString() {
//			return string.Format("[QueryExpressionLetVBClause Variables={0}]", GetCollectionString(Variables));
//		}
//	}
//	
//	public class QueryExpressionOrderClause : QueryExpressionClause {
//		
//		List<QueryExpressionOrdering> orderings;
//		
//		public List<QueryExpressionOrdering> Orderings {
//			get {
//				return orderings;
//			}
//			set {
//				orderings = value ?? new List<QueryExpressionOrdering>();
//			}
//		}
//		
//		public QueryExpressionOrderClause() {
//			orderings = new List<QueryExpressionOrdering>();
//		}
//		
//		public override object AcceptVisitor(IAstVisitor visitor, object data) {
//			return visitor.VisitQueryExpressionOrderClause(this, data);
//		}
//		
//		public override string ToString() {
//			return string.Format("[QueryExpressionOrderClause Orderings={0}]", GetCollectionString(Orderings));
//		}
//	}
//	
//	public class QueryExpressionOrdering : AbstractNode {
//		
//		Expression criteria;
//		
//		QueryExpressionOrderingDirection direction;
//		
//		public Expression Criteria {
//			get {
//				return criteria;
//			}
//			set {
//				criteria = value ?? Expression.Null;
//				if (!criteria.IsNull) criteria.Parent = this;
//			}
//		}
//		
//		public QueryExpressionOrderingDirection Direction {
//			get {
//				return direction;
//			}
//			set {
//				direction = value;
//			}
//		}
//		
//		public QueryExpressionOrdering() {
//			criteria = Expression.Null;
//		}
//		
//		public override object AcceptVisitor(IAstVisitor visitor, object data) {
//			return visitor.VisitQueryExpressionOrdering(this, data);
//		}
//		
//		public override string ToString() {
//			return string.Format("[QueryExpressionOrdering Criteria={0} Direction={1}]", Criteria, Direction);
//		}
//	}
//	
//	public class QueryExpressionPartitionVBClause : QueryExpressionClause {
//		
//		Expression expression;
//		
//		QueryExpressionPartitionType partitionType;
//		
//		public Expression Expression {
//			get {
//				return expression;
//			}
//			set {
//				expression = value ?? Expression.Null;
//				if (!expression.IsNull) expression.Parent = this;
//			}
//		}
//		
//		public QueryExpressionPartitionType PartitionType {
//			get {
//				return partitionType;
//			}
//			set {
//				partitionType = value;
//			}
//		}
//		
//		public QueryExpressionPartitionVBClause() {
//			expression = Expression.Null;
//		}
//		
//		public override object AcceptVisitor(IAstVisitor visitor, object data) {
//			return visitor.VisitQueryExpressionPartitionVBClause(this, data);
//		}
//		
//		public override string ToString() {
//			return string.Format("[QueryExpressionPartitionVBClause Expression={0} PartitionType={1}]", Expression, PartitionType);
//		}
//	}
//	
//	public class QueryExpressionSelectClause : QueryExpressionClause {
//		
//		Expression projection;
//		
//		public Expression Projection {
//			get {
//				return projection;
//			}
//			set {
//				projection = value ?? Expression.Null;
//				if (!projection.IsNull) projection.Parent = this;
//			}
//		}
//		
//		public QueryExpressionSelectClause() {
//			projection = Expression.Null;
//		}
//		
//		public override object AcceptVisitor(IAstVisitor visitor, object data) {
//			return visitor.VisitQueryExpressionSelectClause(this, data);
//		}
//		
//		public override string ToString() {
//			return string.Format("[QueryExpressionSelectClause Projection={0}]", Projection);
//		}
//	}
//	
//	public class QueryExpressionSelectVBClause : QueryExpressionClause {
//		
//		List<ExpressionRangeVariable> variables;
//		
//		public List<ExpressionRangeVariable> Variables {
//			get {
//				return variables;
//			}
//			set {
//				variables = value ?? new List<ExpressionRangeVariable>();
//			}
//		}
//		
//		public QueryExpressionSelectVBClause() {
//			variables = new List<ExpressionRangeVariable>();
//		}
//		
//		public override object AcceptVisitor(IAstVisitor visitor, object data) {
//			return visitor.VisitQueryExpressionSelectVBClause(this, data);
//		}
//		
//		public override string ToString() {
//			return string.Format("[QueryExpressionSelectVBClause Variables={0}]", GetCollectionString(Variables));
//		}
//	}
//	
//	public class QueryExpressionWhereClause : QueryExpressionClause {
//		
//		Expression condition;
//		
//		public Expression Condition {
//			get {
//				return condition;
//			}
//			set {
//				condition = value ?? Expression.Null;
//				if (!condition.IsNull) condition.Parent = this;
//			}
//		}
//		
//		public QueryExpressionWhereClause() {
//			condition = Expression.Null;
//		}
//		
//		public override object AcceptVisitor(IAstVisitor visitor, object data) {
//			return visitor.VisitQueryExpressionWhereClause(this, data);
//		}
//		
//		public override string ToString() {
//			return string.Format("[QueryExpressionWhereClause Condition={0}]", Condition);
//		}
//	}
//	
//	public class RaiseEventStatement : Statement {
//		
//		string eventName;
//		
//		List<Expression> arguments;
//		
//		public string EventName {
//			get {
//				return eventName;
//			}
//			set {
//				eventName = value ?? "";
//			}
//		}
//		
//		public List<Expression> Arguments {
//			get {
//				return arguments;
//			}
//			set {
//				arguments = value ?? new List<Expression>();
//			}
//		}
//		
//		public RaiseEventStatement(string eventName, List<Expression> arguments) {
//			EventName = eventName;
//			Arguments = arguments;
//		}
//		
//		public override object AcceptVisitor(IAstVisitor visitor, object data) {
//			return visitor.VisitRaiseEventStatement(this, data);
//		}
//		
//		public override string ToString() {
//			return string.Format("[RaiseEventStatement EventName={0} Arguments={1}]", EventName, GetCollectionString(Arguments));
//		}
//	}
//	
//	public class ReDimStatement : Statement {
//		
//		List<InvocationExpression> reDimClauses;
//		
//		bool isPreserve;
//		
//		public List<InvocationExpression> ReDimClauses {
//			get {
//				return reDimClauses;
//			}
//			set {
//				reDimClauses = value ?? new List<InvocationExpression>();
//			}
//		}
//		
//		public bool IsPreserve {
//			get {
//				return isPreserve;
//			}
//			set {
//				isPreserve = value;
//			}
//		}
//		
//		public ReDimStatement(bool isPreserve) {
//			IsPreserve = isPreserve;
//			reDimClauses = new List<InvocationExpression>();
//		}
//		
//		public override object AcceptVisitor(IAstVisitor visitor, object data) {
//			return visitor.VisitReDimStatement(this, data);
//		}
//		
//		public override string ToString() {
//			return string.Format("[ReDimStatement ReDimClauses={0} IsPreserve={1}]", GetCollectionString(ReDimClauses), IsPreserve);
//		}
//	}
//	
//	public class RemoveHandlerStatement : Statement {
//		
//		Expression eventExpression;
//		
//		Expression handlerExpression;
//		
//		public Expression EventExpression {
//			get {
//				return eventExpression;
//			}
//			set {
//				eventExpression = value ?? Expression.Null;
//				if (!eventExpression.IsNull) eventExpression.Parent = this;
//			}
//		}
//		
//		public Expression HandlerExpression {
//			get {
//				return handlerExpression;
//			}
//			set {
//				handlerExpression = value ?? Expression.Null;
//				if (!handlerExpression.IsNull) handlerExpression.Parent = this;
//			}
//		}
//		
//		public RemoveHandlerStatement(Expression eventExpression, Expression handlerExpression) {
//			EventExpression = eventExpression;
//			HandlerExpression = handlerExpression;
//		}
//		
//		public override object AcceptVisitor(IAstVisitor visitor, object data) {
//			return visitor.VisitRemoveHandlerStatement(this, data);
//		}
//		
//		public override string ToString() {
//			return string.Format("[RemoveHandlerStatement EventExpression={0} HandlerExpression={1}]", EventExpression, HandlerExpression);
//		}
//	}
//	
//	public class ResumeStatement : Statement {
//		
//		string labelName;
//		
//		bool isResumeNext;
//		
//		public string LabelName {
//			get {
//				return labelName;
//			}
//			set {
//				labelName = value ?? "";
//			}
//		}
//		
//		public bool IsResumeNext {
//			get {
//				return isResumeNext;
//			}
//			set {
//				isResumeNext = value;
//			}
//		}
//		
//		public ResumeStatement(bool isResumeNext) {
//			IsResumeNext = isResumeNext;
//			labelName = "";
//		}
//		
//		public ResumeStatement(string labelName) {
//			LabelName = labelName;
//		}
//		
//		public override object AcceptVisitor(IAstVisitor visitor, object data) {
//			return visitor.VisitResumeStatement(this, data);
//		}
//		
//		public override string ToString() {
//			return string.Format("[ResumeStatement LabelName={0} IsResumeNext={1}]", LabelName, IsResumeNext);
//		}
//	}
//	
//	public class ReturnStatement : Statement {
//		
//		Expression expression;
//		
//		public Expression Expression {
//			get {
//				return expression;
//			}
//			set {
//				expression = value ?? Expression.Null;
//				if (!expression.IsNull) expression.Parent = this;
//			}
//		}
//		
//		public ReturnStatement(Expression expression) {
//			Expression = expression;
//		}
//		
//		public override object AcceptVisitor(IAstVisitor visitor, object data) {
//			return visitor.VisitReturnStatement(this, data);
//		}
//		
//		public override string ToString() {
//			return string.Format("[ReturnStatement Expression={0}]", Expression);
//		}
//	}
//	
//	public class StopStatement : Statement {
//		
//		public StopStatement() {
//		}
//		
//		public override object AcceptVisitor(IAstVisitor visitor, object data) {
//			return visitor.VisitStopStatement(this, data);
//		}
//		
//		public override string ToString() {
//			return "[StopStatement]";
//		}
//	}
//	
//	public class SwitchSection : BlockStatement {
//		
//		List<CaseLabel> switchLabels;
//		
//		public List<CaseLabel> SwitchLabels {
//			get {
//				return switchLabels;
//			}
//			set {
//				switchLabels = value ?? new List<CaseLabel>();
//			}
//		}
//		
//		public SwitchSection() {
//			switchLabels = new List<CaseLabel>();
//		}
//		
//		public SwitchSection(List<CaseLabel> switchLabels) {
//			SwitchLabels = switchLabels;
//		}
//		
//		public override object AcceptVisitor(IAstVisitor visitor, object data) {
//			return visitor.VisitSwitchSection(this, data);
//		}
//		
//		public override string ToString() {
//			return string.Format("[SwitchSection SwitchLabels={0}]", GetCollectionString(SwitchLabels));
//		}
//	}
//	
//	public class SwitchStatement : Statement {
//		
//		Expression switchExpression;
//		
//		List<SwitchSection> switchSections;
//		
//		public Expression SwitchExpression {
//			get {
//				return switchExpression;
//			}
//			set {
//				switchExpression = value ?? Expression.Null;
//				if (!switchExpression.IsNull) switchExpression.Parent = this;
//			}
//		}
//		
//		public List<SwitchSection> SwitchSections {
//			get {
//				return switchSections;
//			}
//			set {
//				switchSections = value ?? new List<SwitchSection>();
//			}
//		}
//		
//		public SwitchStatement(Expression switchExpression, List<SwitchSection> switchSections) {
//			SwitchExpression = switchExpression;
//			SwitchSections = switchSections;
//		}
//		
//		public override object AcceptVisitor(IAstVisitor visitor, object data) {
//			return visitor.VisitSwitchStatement(this, data);
//		}
//		
//		public override string ToString() {
//			return string.Format("[SwitchStatement SwitchExpression={0} SwitchSections={1}]", SwitchExpression, GetCollectionString(SwitchSections));
//		}
//	}
//	
//	public class TemplateDefinition : AttributedNode {
//		
//		string name;
//		
//		VarianceModifier varianceModifier;
//		
//		List<TypeReference> bases;
//		
//		public string Name {
//			get {
//				return name;
//			}
//			set {
//				name = string.IsNullOrEmpty(value) ? "?" : value;
//			}
//		}
//		
//		public VarianceModifier VarianceModifier {
//			get {
//				return varianceModifier;
//			}
//			set {
//				varianceModifier = value;
//			}
//		}
//		
//		public List<TypeReference> Bases {
//			get {
//				return bases;
//			}
//			set {
//				bases = value ?? new List<TypeReference>();
//			}
//		}
//		
//		public TemplateDefinition() {
//			name = "?";
//			bases = new List<TypeReference>();
//		}
//		
//		public TemplateDefinition(string name, List<AttributeSection> attributes) {
//			Name = name;
//			Attributes = attributes;
//			bases = new List<TypeReference>();
//		}
//		
//		public override object AcceptVisitor(IAstVisitor visitor, object data) {
//			return visitor.VisitTemplateDefinition(this, data);
//		}
//		
//		public override string ToString() {
//			return string.Format("[TemplateDefinition Name={0} VarianceModifier={1} Bases={2} Attributes={3} Modifi" +
//			                     "er={4}]", Name, VarianceModifier, GetCollectionString(Bases), GetCollectionString(Attributes), Modifier);
//		}
//	}
//	
//	public class ThisReferenceExpression : Expression {
//		
//		public ThisReferenceExpression() {
//		}
//		
//		public override object AcceptVisitor(IAstVisitor visitor, object data) {
//			return visitor.VisitThisReferenceExpression(this, data);
//		}
//		
//		public override string ToString() {
//			return "[ThisReferenceExpression]";
//		}
//	}
//	
//	public class ThrowStatement : Statement {
//		
//		Expression expression;
//		
//		public Expression Expression {
//			get {
//				return expression;
//			}
//			set {
//				expression = value ?? Expression.Null;
//				if (!expression.IsNull) expression.Parent = this;
//			}
//		}
//		
//		public ThrowStatement(Expression expression) {
//			Expression = expression;
//		}
//		
//		public override object AcceptVisitor(IAstVisitor visitor, object data) {
//			return visitor.VisitThrowStatement(this, data);
//		}
//		
//		public override string ToString() {
//			return string.Format("[ThrowStatement Expression={0}]", Expression);
//		}
//	}
//	
//	public class TryCatchStatement : Statement {
//		
//		Statement statementBlock;
//		
//		List<CatchClause> catchClauses;
//		
//		Statement finallyBlock;
//		
//		public Statement StatementBlock {
//			get {
//				return statementBlock;
//			}
//			set {
//				statementBlock = value ?? Statement.Null;
//				if (!statementBlock.IsNull) statementBlock.Parent = this;
//			}
//		}
//		
//		public List<CatchClause> CatchClauses {
//			get {
//				return catchClauses;
//			}
//			set {
//				catchClauses = value ?? new List<CatchClause>();
//			}
//		}
//		
//		public Statement FinallyBlock {
//			get {
//				return finallyBlock;
//			}
//			set {
//				finallyBlock = value ?? Statement.Null;
//				if (!finallyBlock.IsNull) finallyBlock.Parent = this;
//			}
//		}
//		
//		public TryCatchStatement(Statement statementBlock, List<CatchClause> catchClauses, Statement finallyBlock) {
//			StatementBlock = statementBlock;
//			CatchClauses = catchClauses;
//			FinallyBlock = finallyBlock;
//		}
//		
//		public override object AcceptVisitor(IAstVisitor visitor, object data) {
//			return visitor.VisitTryCatchStatement(this, data);
//		}
//		
//		public override string ToString() {
//			return string.Format("[TryCatchStatement StatementBlock={0} CatchClauses={1} FinallyBlock={2}]", StatementBlock, GetCollectionString(CatchClauses), FinallyBlock);
//		}
//	}
//	
//	public class TypeDeclaration : AttributedNode {
//		
//		string name;
//		
//		ClassType type;
//		
//		List<TypeReference> baseTypes;
//		
//		List<TemplateDefinition> templates;
//		
//		Location bodyStartLocation;
//		
//		public string Name {
//			get {
//				return name;
//			}
//			set {
//				name = value ?? "";
//			}
//		}
//		
//		public ClassType Type {
//			get {
//				return type;
//			}
//			set {
//				type = value;
//			}
//		}
//		
//		public List<TypeReference> BaseTypes {
//			get {
//				return baseTypes;
//			}
//			set {
//				baseTypes = value ?? new List<TypeReference>();
//			}
//		}
//		
//		public List<TemplateDefinition> Templates {
//			get {
//				return templates;
//			}
//			set {
//				templates = value ?? new List<TemplateDefinition>();
//			}
//		}
//		
//		public Location BodyStartLocation {
//			get {
//				return bodyStartLocation;
//			}
//			set {
//				bodyStartLocation = value;
//			}
//		}
//		
//		public TypeDeclaration(Modifiers modifier, List<AttributeSection> attributes) {
//			Modifier = modifier;
//			Attributes = attributes;
//			name = "";
//			baseTypes = new List<TypeReference>();
//			templates = new List<TemplateDefinition>();
//			bodyStartLocation = Location.Empty;
//		}
//		
//		public override object AcceptVisitor(IAstVisitor visitor, object data) {
//			return visitor.VisitTypeDeclaration(this, data);
//		}
//		
//		public override string ToString() {
//			return string.Format("[TypeDeclaration Name={0} Type={1} BaseTypes={2} Templates={3} BodyStartLocation=" +
//			                     "{4} Attributes={5} Modifier={6}]", Name, Type, GetCollectionString(BaseTypes), GetCollectionString(Templates), BodyStartLocation, GetCollectionString(Attributes), Modifier);
//		}
//	}
//	
//	public class TypeOfExpression : Expression {
//		
//		TypeReference typeReference;
//		
//		public TypeReference TypeReference {
//			get {
//				return typeReference;
//			}
//			set {
//				typeReference = value ?? TypeReference.Null;
//				if (!typeReference.IsNull) typeReference.Parent = this;
//			}
//		}
//		
//		public TypeOfExpression(TypeReference typeReference) {
//			TypeReference = typeReference;
//		}
//		
//		public override object AcceptVisitor(IAstVisitor visitor, object data) {
//			return visitor.VisitTypeOfExpression(this, data);
//		}
//		
//		public override string ToString() {
//			return string.Format("[TypeOfExpression TypeReference={0}]", TypeReference);
//		}
//	}
//	
//	public class TypeOfIsExpression : Expression {
//		
//		Expression expression;
//		
//		TypeReference typeReference;
//		
//		public Expression Expression {
//			get {
//				return expression;
//			}
//			set {
//				expression = value ?? Expression.Null;
//				if (!expression.IsNull) expression.Parent = this;
//			}
//		}
//		
//		public TypeReference TypeReference {
//			get {
//				return typeReference;
//			}
//			set {
//				typeReference = value ?? TypeReference.Null;
//				if (!typeReference.IsNull) typeReference.Parent = this;
//			}
//		}
//		
//		public TypeOfIsExpression(Expression expression, TypeReference typeReference) {
//			Expression = expression;
//			TypeReference = typeReference;
//		}
//		
//		public override object AcceptVisitor(IAstVisitor visitor, object data) {
//			return visitor.VisitTypeOfIsExpression(this, data);
//		}
//		
//		public override string ToString() {
//			return string.Format("[TypeOfIsExpression Expression={0} TypeReference={1}]", Expression, TypeReference);
//		}
//	}
//	
//	public class TypeReferenceExpression : Expression {
//		
//		TypeReference typeReference;
//		
//		public TypeReference TypeReference {
//			get {
//				return typeReference;
//			}
//			set {
//				typeReference = value ?? TypeReference.Null;
//				if (!typeReference.IsNull) typeReference.Parent = this;
//			}
//		}
//		
//		public TypeReferenceExpression(TypeReference typeReference) {
//			TypeReference = typeReference;
//		}
//		
//		public override object AcceptVisitor(IAstVisitor visitor, object data) {
//			return visitor.VisitTypeReferenceExpression(this, data);
//		}
//		
//		public override string ToString() {
//			return string.Format("[TypeReferenceExpression TypeReference={0}]", TypeReference);
//		}
//	}
//	
//	public class UnaryOperatorExpression : Expression {
//		
//		UnaryOperatorType op;
//		
//		Expression expression;
//		
//		public UnaryOperatorType Op {
//			get {
//				return op;
//			}
//			set {
//				op = value;
//			}
//		}
//		
//		public Expression Expression {
//			get {
//				return expression;
//			}
//			set {
//				expression = value ?? Expression.Null;
//				if (!expression.IsNull) expression.Parent = this;
//			}
//		}
//		
//		public UnaryOperatorExpression(UnaryOperatorType op) {
//			Op = op;
//			expression = Expression.Null;
//		}
//		
//		public UnaryOperatorExpression(Expression expression, UnaryOperatorType op) {
//			Expression = expression;
//			Op = op;
//		}
//		
//		public override object AcceptVisitor(IAstVisitor visitor, object data) {
//			return visitor.VisitUnaryOperatorExpression(this, data);
//		}
//		
//		public override string ToString() {
//			return string.Format("[UnaryOperatorExpression Op={0} Expression={1}]", Op, Expression);
//		}
//	}
//	
//	public class UsingStatement : StatementWithEmbeddedStatement {
//		
//		Statement resourceAcquisition;
//		
//		public Statement ResourceAcquisition {
//			get {
//				return resourceAcquisition;
//			}
//			set {
//				resourceAcquisition = value ?? Statement.Null;
//				if (!resourceAcquisition.IsNull) resourceAcquisition.Parent = this;
//			}
//		}
//		
//		public UsingStatement(Statement resourceAcquisition, Statement embeddedStatement) {
//			ResourceAcquisition = resourceAcquisition;
//			EmbeddedStatement = embeddedStatement;
//		}
//		
//		public override object AcceptVisitor(IAstVisitor visitor, object data) {
//			return visitor.VisitUsingStatement(this, data);
//		}
//		
//		public override string ToString() {
//			return string.Format("[UsingStatement ResourceAcquisition={0} EmbeddedStatement={1}]", ResourceAcquisition, EmbeddedStatement);
//		}
//	}
//	
//	public class VariableDeclaration : AbstractNode {
//		
//		string name;
//		
//		Expression initializer;
//		
//		TypeReference typeReference;
//		
//		Expression fixedArrayInitialization;
//		
//		public string Name {
//			get {
//				return name;
//			}
//			set {
//				name = value ?? "";
//			}
//		}
//		
//		public Expression Initializer {
//			get {
//				return initializer;
//			}
//			set {
//				initializer = value ?? Expression.Null;
//				if (!initializer.IsNull) initializer.Parent = this;
//			}
//		}
//		
//		public TypeReference TypeReference {
//			get {
//				return typeReference;
//			}
//			set {
//				typeReference = value ?? TypeReference.Null;
//				if (!typeReference.IsNull) typeReference.Parent = this;
//			}
//		}
//		
//		public Expression FixedArrayInitialization {
//			get {
//				return fixedArrayInitialization;
//			}
//			set {
//				fixedArrayInitialization = value ?? Expression.Null;
//				if (!fixedArrayInitialization.IsNull) fixedArrayInitialization.Parent = this;
//			}
//		}
//		
//		public VariableDeclaration(string name) {
//			Name = name;
//			initializer = Expression.Null;
//			typeReference = TypeReference.Null;
//			fixedArrayInitialization = Expression.Null;
//		}
//		
//		public VariableDeclaration(string name, Expression initializer) {
//			Name = name;
//			Initializer = initializer;
//			typeReference = TypeReference.Null;
//			fixedArrayInitialization = Expression.Null;
//		}
//		
//		public VariableDeclaration(string name, Expression initializer, TypeReference typeReference) {
//			Name = name;
//			Initializer = initializer;
//			TypeReference = typeReference;
//			fixedArrayInitialization = Expression.Null;
//		}
//		
//		public override object AcceptVisitor(IAstVisitor visitor, object data) {
//			return visitor.VisitVariableDeclaration(this, data);
//		}
//		
//		public override string ToString() {
//			return string.Format("[VariableDeclaration Name={0} Initializer={1} TypeReference={2} FixedArrayInitial" +
//			                     "ization={3}]", Name, Initializer, TypeReference, FixedArrayInitialization);
//		}
//	}
//	
//	public class WithStatement : Statement {
//		
//		Expression expression;
//		
//		BlockStatement body;
//		
//		public Expression Expression {
//			get {
//				return expression;
//			}
//			set {
//				expression = value ?? Expression.Null;
//				if (!expression.IsNull) expression.Parent = this;
//			}
//		}
//		
//		public BlockStatement Body {
//			get {
//				return body;
//			}
//			set {
//				body = value ?? BlockStatement.Null;
//				if (!body.IsNull) body.Parent = this;
//			}
//		}
//		
//		public WithStatement(Expression expression) {
//			Expression = expression;
//			body = BlockStatement.Null;
//		}
//		
//		public override object AcceptVisitor(IAstVisitor visitor, object data) {
//			return visitor.VisitWithStatement(this, data);
//		}
//		
//		public override string ToString() {
//			return string.Format("[WithStatement Expression={0} Body={1}]", Expression, Body);
//		}
//	}
//	
//	public class XmlAttributeExpression : XmlExpression {
//		
//		string name;
//		
//		string literalValue;
//		
//		bool useDoubleQuotes;
//		
//		Expression expressionValue;
//		
//		public string Name {
//			get {
//				return name;
//			}
//			set {
//				name = value ?? "";
//			}
//		}
//		
//		public string LiteralValue {
//			get {
//				return literalValue;
//			}
//			set {
//				literalValue = value ?? "";
//			}
//		}
//		
//		public bool UseDoubleQuotes {
//			get {
//				return useDoubleQuotes;
//			}
//			set {
//				useDoubleQuotes = value;
//			}
//		}
//		
//		public Expression ExpressionValue {
//			get {
//				return expressionValue;
//			}
//			set {
//				expressionValue = value ?? Expression.Null;
//				if (!expressionValue.IsNull) expressionValue.Parent = this;
//			}
//		}
//		
//		public XmlAttributeExpression() {
//			name = "";
//			literalValue = "";
//			expressionValue = Expression.Null;
//		}
//		
//		public bool IsLiteralValue {
//			get {
//				return expressionValue.IsNull;
//			}
//		}
//		
//		public override object AcceptVisitor(IAstVisitor visitor, object data) {
//			return visitor.VisitXmlAttributeExpression(this, data);
//		}
//		
//		public override string ToString() {
//			return string.Format("[XmlAttributeExpression Name={0} LiteralValue={1} UseDoubleQuotes={2} ExpressionV" +
//			                     "alue={3}]", Name, LiteralValue, UseDoubleQuotes, ExpressionValue);
//		}
//	}
//	
//	public class XmlContentExpression : XmlExpression {
//		
//		string content;
//		
//		XmlContentType type;
//		
//		public string Content {
//			get {
//				return content;
//			}
//			set {
//				content = value ?? "";
//			}
//		}
//		
//		public XmlContentType Type {
//			get {
//				return type;
//			}
//			set {
//				type = value;
//			}
//		}
//		
//		public XmlContentExpression(string content, XmlContentType type) {
//			Content = content;
//			Type = type;
//		}
//		
//		public override object AcceptVisitor(IAstVisitor visitor, object data) {
//			return visitor.VisitXmlContentExpression(this, data);
//		}
//		
//		public override string ToString() {
//			return string.Format("[XmlContentExpression Content={0} Type={1}]", Content, Type);
//		}
//	}
//	
//	public class XmlDocumentExpression : XmlExpression {
//		
//		List<XmlExpression> expressions;
//		
//		public List<XmlExpression> Expressions {
//			get {
//				return expressions;
//			}
//			set {
//				expressions = value ?? new List<XmlExpression>();
//			}
//		}
//		
//		public XmlDocumentExpression() {
//			expressions = new List<XmlExpression>();
//		}
//		
//		public override object AcceptVisitor(IAstVisitor visitor, object data) {
//			return visitor.VisitXmlDocumentExpression(this, data);
//		}
//		
//		public override string ToString() {
//			return string.Format("[XmlDocumentExpression Expressions={0}]", GetCollectionString(Expressions));
//		}
//	}
//	
//	public class XmlElementExpression : XmlExpression {
//		
//		Expression content;
//		
//		Expression nameExpression;
//		
//		string xmlName;
//		
//		List<XmlExpression> attributes;
//		
//		public Expression Content {
//			get {
//				return content;
//			}
//			set {
//				content = value ?? Expression.Null;
//				if (!content.IsNull) content.Parent = this;
//			}
//		}
//		
//		public Expression NameExpression {
//			get {
//				return nameExpression;
//			}
//			set {
//				nameExpression = value ?? Expression.Null;
//				if (!nameExpression.IsNull) nameExpression.Parent = this;
//			}
//		}
//		
//		public string XmlName {
//			get {
//				return xmlName;
//			}
//			set {
//				xmlName = value ?? "";
//			}
//		}
//		
//		public List<XmlExpression> Attributes {
//			get {
//				return attributes;
//			}
//			set {
//				attributes = value ?? new List<XmlExpression>();
//			}
//		}
//		
//		public XmlElementExpression() {
//			content = Expression.Null;
//			nameExpression = Expression.Null;
//			xmlName = "";
//			attributes = new List<XmlExpression>();
//		}
//		
//		public bool IsExpression {
//			get {
//				return !content.IsNull;
//			}
//		}
//		
//		public bool NameIsExpression {
//			get {
//				return !nameExpression.IsNull;
//			}
//		}
//		
//		public override object AcceptVisitor(IAstVisitor visitor, object data) {
//			return visitor.VisitXmlElementExpression(this, data);
//		}
//		
//		public override string ToString() {
//			return string.Format("[XmlElementExpression Content={0} NameExpression={1} XmlName={2} Attributes={3}]", Content, NameExpression, XmlName, GetCollectionString(Attributes));
//		}
//	}
//	
//	public class XmlEmbeddedExpression : XmlExpression {
//		
//		Expression inlineVBExpression;
//		
//		public Expression InlineVBExpression {
//			get {
//				return inlineVBExpression;
//			}
//			set {
//				inlineVBExpression = value ?? Expression.Null;
//				if (!inlineVBExpression.IsNull) inlineVBExpression.Parent = this;
//			}
//		}
//		
//		public XmlEmbeddedExpression() {
//			inlineVBExpression = Expression.Null;
//		}
//		
//		public override object AcceptVisitor(IAstVisitor visitor, object data) {
//			return visitor.VisitXmlEmbeddedExpression(this, data);
//		}
//		
//		public override string ToString() {
//			return string.Format("[XmlEmbeddedExpression InlineVBExpression={0}]", InlineVBExpression);
//		}
//	}
//	
//	public abstract class XmlExpression : Expression {
//		
//		protected XmlExpression() {
//		}
//	}
//	
//	public class XmlMemberAccessExpression : Expression {
//		
//		Expression targetObject;
//		
//		XmlAxisType axisType;
//		
//		bool isXmlIdentifier;
//		
//		string identifier;
//		
//		public Expression TargetObject {
//			get {
//				return targetObject;
//			}
//			set {
//				targetObject = value ?? Expression.Null;
//				if (!targetObject.IsNull) targetObject.Parent = this;
//			}
//		}
//		
//		public XmlAxisType AxisType {
//			get {
//				return axisType;
//			}
//			set {
//				axisType = value;
//			}
//		}
//		
//		public bool IsXmlIdentifier {
//			get {
//				return isXmlIdentifier;
//			}
//			set {
//				isXmlIdentifier = value;
//			}
//		}
//		
//		public string Identifier {
//			get {
//				return identifier;
//			}
//			set {
//				identifier = value ?? "";
//			}
//		}
//		
//		public XmlMemberAccessExpression(Expression targetObject, XmlAxisType axisType, string identifier, bool isXmlIdentifier) {
//			TargetObject = targetObject;
//			AxisType = axisType;
//			Identifier = identifier;
//			IsXmlIdentifier = isXmlIdentifier;
//		}
//		
//		public override object AcceptVisitor(IAstVisitor visitor, object data) {
//			return visitor.VisitXmlMemberAccessExpression(this, data);
//		}
//		
//		public override string ToString() {
//			return string.Format("[XmlMemberAccessExpression TargetObject={0} AxisType={1} IsXmlIdentifier={2} Iden" +
//			                     "tifier={3}]", TargetObject, AxisType, IsXmlIdentifier, Identifier);
//		}
//	}
//}
