// Copyright (c) 2010 AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.TypeSystem.Implementation;

namespace ICSharpCode.NRefactory.CSharp.Resolver
{
	/// <summary>
	/// Contains the main resolver logic.
	/// </summary>
	public class CSharpResolver
	{
		static readonly ResolveResult ErrorResult = new ErrorResolveResult(SharedTypes.UnknownType);
		static readonly ResolveResult DynamicResult = new ResolveResult(SharedTypes.Dynamic);
		static readonly ResolveResult NullResult = new ResolveResult(SharedTypes.Null);
		
		readonly ITypeResolveContext context;
		internal readonly CancellationToken cancellationToken;
		
		#region Constructor
		public CSharpResolver(ITypeResolveContext context)
		{
			if (context == null)
				throw new ArgumentNullException("context");
			this.context = context;
		}
		
		public CSharpResolver(ITypeResolveContext context, CancellationToken cancellationToken)
		{
			if (context == null)
				throw new ArgumentNullException("context");
			this.context = context;
			this.cancellationToken = cancellationToken;
		}
		#endregion
		
		#region Properties
		/// <summary>
		/// Gets the type resolve context used by the resolver.
		/// </summary>
		public ITypeResolveContext Context {
			get { return context; }
		}
		
		/// <summary>
		/// Gets/Sets whether the current context is <c>checked</c>.
		/// </summary>
		public bool CheckForOverflow { get; set; }
		
		/// <summary>
		/// Gets/Sets the current member definition that is used to look up identifiers as parameters
		/// or type parameters.
		/// </summary>
		/// <remarks>Don't forget to also set CurrentTypeDefinition when setting CurrentMember;
		/// setting one of the properties does not automatically set the other.</remarks>
		public IMember CurrentMember { get; set; }
		
		/// <summary>
		/// Gets/Sets the current type definition that is used to look up identifiers as simple members.
		/// </summary>
		public ITypeDefinition CurrentTypeDefinition { get; set; }
		
		/// <summary>
		/// Gets/Sets the current using scope that is used to look up identifiers as class names.
		/// </summary>
		public UsingScope UsingScope { get; set; }
		#endregion
		
		#region Local Variable Management
		sealed class LocalVariable : IVariable
		{
			// We store the local variable in a linked list
			// and provide a stack-like API.
			// The beginning of a stack frame is marked by a dummy local variable
			// with type==null and name==null.
			
			// This data structure is used to allow efficient cloning of the resolver with its local variable context.
			
			internal readonly LocalVariable prev;
			internal readonly ITypeReference type;
			internal readonly string name;
			internal readonly IConstantValue constantValue;
			
			public LocalVariable(LocalVariable prev, ITypeReference type, string name, IConstantValue constantValue)
			{
				this.prev = prev;
				this.type = type;
				this.name = name;
				this.constantValue = constantValue;
			}
			
			public string Name {
				get { return name; }
			}
			public ITypeReference Type {
				get { return type; }
			}
			public bool IsConst {
				get { return constantValue != null; }
			}
			public IConstantValue ConstantValue {
				get { return constantValue; }
			}
			
			public override string ToString()
			{
				if (name == null)
					return "<Start of Block>";
				else
					return name + ":" + type;
			}
		}
		
		LocalVariable localVariableStack;
		
		/// <summary>
		/// Opens a new scope for local variables.
		/// </summary>
		public void PushBlock()
		{
			localVariableStack = new LocalVariable(localVariableStack, null, null, null);
		}
		
		/// <summary>
		/// Closes the current scope for local variables; removing all variables in that scope.
		/// </summary>
		public void PopBlock()
		{
			LocalVariable removedVar;
			do {
				removedVar = localVariableStack;
				if (removedVar == null)
					throw new InvalidOperationException("Cannot execute PopBlock() without corresponding PushBlock()");
				localVariableStack = removedVar.prev;
			} while (removedVar.name != null);
		}
		
		/// <summary>
		/// Adds a new variable to the current block.
		/// </summary>
		public IVariable AddVariable(ITypeReference type, string name, IConstantValue constantValue = null)
		{
			if (type == null)
				throw new ArgumentNullException("type");
			if (name == null)
				throw new ArgumentNullException("name");
			return localVariableStack = new LocalVariable(localVariableStack, type, name, constantValue);
		}
		
		/// <summary>
		/// Gets all currently visible local variables.
		/// </summary>
		public IEnumerable<IVariable> LocalVariables {
			get {
				for (LocalVariable v = localVariableStack; v != null; v = v.prev) {
					if (v.name != null)
						yield return v;
				}
			}
		}
		#endregion
		
		#region Clone
		/// <summary>
		/// Creates a copy of this CSharp resolver.
		/// </summary>
		public CSharpResolver Clone()
		{
			return (CSharpResolver)MemberwiseClone();
		}
		#endregion
		
		#region class OperatorMethod
		static OperatorMethod[] Lift(params OperatorMethod[] methods)
		{
			List<OperatorMethod> result = new List<OperatorMethod>(methods);
			foreach (OperatorMethod method in methods) {
				OperatorMethod lifted = method.Lift();
				if (lifted != null)
					result.Add(lifted);
			}
			return result.ToArray();
		}
		
		class OperatorMethod : Immutable, IParameterizedMember
		{
			static readonly IParameter[] normalParameters = new IParameter[(int)(TypeCode.String + 1 - TypeCode.Object)];
			static readonly IParameter[] nullableParameters = new IParameter[(int)(TypeCode.Decimal + 1 - TypeCode.Boolean)];
			
			static OperatorMethod()
			{
				for (TypeCode i = TypeCode.Object; i <= TypeCode.String; i++) {
					normalParameters[i - TypeCode.Object] = new DefaultParameter(i.ToTypeReference(), string.Empty);
				}
				for (TypeCode i = TypeCode.Boolean; i <= TypeCode.Decimal; i++) {
					nullableParameters[i - TypeCode.Boolean] = new DefaultParameter(NullableType.Create(i.ToTypeReference()), string.Empty);
				}
			}
			
			protected static IParameter MakeParameter(TypeCode code)
			{
				return normalParameters[code - TypeCode.Object];
			}
			
			protected static IParameter MakeNullableParameter(IParameter normalParameter)
			{
				for (TypeCode i = TypeCode.Boolean; i <= TypeCode.Decimal; i++) {
					if (normalParameter == normalParameters[i - TypeCode.Object])
						return nullableParameters[i - TypeCode.Boolean];
				}
				throw new ArgumentException();
			}
			
			readonly IList<IParameter> parameters = new List<IParameter>();
			
			public IList<IParameter> Parameters {
				get { return parameters; }
			}
			
			public ITypeReference ReturnType {
				get; set;
			}
			
			public virtual OperatorMethod Lift()
			{
				return null;
			}
			
			ITypeDefinition IEntity.DeclaringTypeDefinition {
				get { throw new NotSupportedException(); }
			}
			
			IType IMember.DeclaringType {
				get { return SharedTypes.UnknownType; }
			}
			
			IMember IMember.MemberDefinition {
				get { return null; }
			}
			
			IList<IExplicitInterfaceImplementation> IMember.InterfaceImplementations {
				get { return EmptyList<IExplicitInterfaceImplementation>.Instance; }
			}
			
			bool IMember.IsVirtual {
				get { return false; }
			}
			
			bool IMember.IsOverride {
				get { return false; }
			}
			
			bool IMember.IsOverridable {
				get { return false; }
			}
			
			EntityType IEntity.EntityType {
				get { return EntityType.Operator; }
			}
			
			DomRegion IEntity.Region {
				get { return DomRegion.Empty; }
			}
			
			DomRegion IEntity.BodyRegion {
				get { return DomRegion.Empty; }
			}
			
			IList<IAttribute> IEntity.Attributes {
				get { return EmptyList<IAttribute>.Instance; }
			}
			
			string IEntity.Documentation {
				get { return null; }
			}
			
			Accessibility IEntity.Accessibility {
				get { return Accessibility.Public; }
			}
			
			bool IEntity.IsStatic {
				get { return true; }
			}
			
			bool IEntity.IsAbstract {
				get { return false; }
			}
			
			bool IEntity.IsSealed {
				get { return false; }
			}
			
			bool IEntity.IsShadowing {
				get { return false; }
			}
			
			bool IEntity.IsSynthetic {
				get { return true; }
			}
			
			IProjectContent IEntity.ProjectContent {
				get { throw new NotSupportedException(); }
			}
			
			string INamedElement.FullName {
				get { return "operator"; }
			}
			
			string INamedElement.Name {
				get { return "operator"; }
			}
			
			string INamedElement.Namespace {
				get { return string.Empty; }
			}
			
			string INamedElement.ReflectionName {
				get { return "operator"; }
			}
			
			public override string ToString()
			{
				StringBuilder b = new StringBuilder();
				b.Append(ReturnType + " operator(");
				for (int i = 0; i < parameters.Count; i++) {
					if (i > 0)
						b.Append(", ");
					b.Append(parameters[i].Type);
				}
				b.Append(')');
				return b.ToString();
			}
		}
		#endregion
		
		#region ResolveUnaryOperator
		#region ResolveUnaryOperator method
		public ResolveResult ResolveUnaryOperator(UnaryOperatorType op, ResolveResult expression)
		{
			cancellationToken.ThrowIfCancellationRequested();
			
			if (expression.Type == SharedTypes.Dynamic)
				return DynamicResult;
			
			// C# 4.0 spec: §7.3.3 Unary operator overload resolution
			string overloadableOperatorName = GetOverloadableOperatorName(op);
			if (overloadableOperatorName == null) {
				switch (op) {
					case UnaryOperatorType.Dereference:
						PointerType p = expression.Type as PointerType;
						if (p != null)
							return new ResolveResult(p.ElementType);
						else
							return ErrorResult;
					case UnaryOperatorType.AddressOf:
						return new ResolveResult(new PointerType(expression.Type));
					default:
						throw new ArgumentException("Invalid value for UnaryOperatorType", "op");
				}
			}
			// If the type is nullable, get the underlying type:
			IType type = NullableType.GetUnderlyingType(expression.Type);
			bool isNullable = NullableType.IsNullable(expression.Type);
			
			// the operator is overloadable:
			// TODO: implicit support for user operators
			//var candidateSet = GetUnaryOperatorCandidates();
			
			expression = UnaryNumericPromotion(op, ref type, isNullable, expression);
			OperatorMethod[] methodGroup;
			switch (op) {
				case UnaryOperatorType.Increment:
				case UnaryOperatorType.Decrement:
				case UnaryOperatorType.PostIncrement:
				case UnaryOperatorType.PostDecrement:
					// C# 4.0 spec: §7.6.9 Postfix increment and decrement operators
					// C# 4.0 spec: §7.7.5 Prefix increment and decrement operators
					TypeCode code = ReflectionHelper.GetTypeCode(type);
					if ((code >= TypeCode.SByte && code <= TypeCode.Decimal) || type.IsEnum() || type is PointerType)
						return new ResolveResult(expression.Type);
					else
						return new ErrorResolveResult(expression.Type);
				case UnaryOperatorType.Plus:
					methodGroup = unaryPlusOperators;
					break;
				case UnaryOperatorType.Minus:
					methodGroup = CheckForOverflow ? checkedUnaryMinusOperators : uncheckedUnaryMinusOperators;
					break;
				case UnaryOperatorType.Not:
					methodGroup = logicalNegationOperator;
					break;
				case UnaryOperatorType.BitNot:
					if (type.IsEnum()) {
						if (expression.IsCompileTimeConstant && !isNullable) {
							// evaluate as (E)(~(U)x);
							var U = expression.ConstantValue.GetType().ToTypeReference().Resolve(context);
							var unpackedEnum = new ConstantResolveResult(U, expression.ConstantValue);
							return CheckErrorAndResolveCast(expression.Type, ResolveUnaryOperator(op, unpackedEnum));
						} else {
							return new ResolveResult(expression.Type);
						}
					} else {
						methodGroup = bitwiseComplementOperators;
						break;
					}
				default:
					throw new InvalidOperationException();
			}
			OverloadResolution r = new OverloadResolution(context, new[] { expression });
			foreach (var candidate in methodGroup) {
				r.AddCandidate(candidate);
			}
			UnaryOperatorMethod m = (UnaryOperatorMethod)r.BestCandidate;
			IType resultType = m.ReturnType.Resolve(context);
			if (r.BestCandidateErrors != OverloadResolutionErrors.None) {
				return new ErrorResolveResult(resultType);
			} else if (expression.IsCompileTimeConstant && !isNullable) {
				object val;
				try {
					val = m.Invoke(this, expression.ConstantValue);
				} catch (ArithmeticException) {
					return new ErrorResolveResult(resultType);
				}
				return new ConstantResolveResult(resultType, val);
			} else {
				return new ResolveResult(resultType);
			}
		}
		#endregion
		
		#region UnaryNumericPromotion
		ResolveResult UnaryNumericPromotion(UnaryOperatorType op, ref IType type, bool isNullable, ResolveResult expression)
		{
			// C# 4.0 spec: §7.3.6.1
			TypeCode code = ReflectionHelper.GetTypeCode(type);
			if (isNullable && type == SharedTypes.Null)
				code = TypeCode.SByte; // cause promotion of null to int32
			switch (op) {
				case UnaryOperatorType.Minus:
					if (code == TypeCode.UInt32) {
						IType targetType = KnownTypeReference.Int64.Resolve(context);
						type = targetType;
						if (isNullable) targetType = NullableType.Create(targetType, context);
						return ResolveCast(targetType, expression);
					}
					goto case UnaryOperatorType.Plus;
				case UnaryOperatorType.Plus:
				case UnaryOperatorType.BitNot:
					if (code >= TypeCode.Char && code <= TypeCode.UInt16) {
						IType targetType = KnownTypeReference.Int32.Resolve(context);
						type = targetType;
						if (isNullable) targetType = NullableType.Create(targetType, context);
						return ResolveCast(targetType, expression);
					}
					break;
			}
			return expression;
		}
		#endregion
		
		#region GetOverloadableOperatorName
		static string GetOverloadableOperatorName(UnaryOperatorType op)
		{
			switch (op) {
				case UnaryOperatorType.Not:
					return "op_LogicalNot";
				case UnaryOperatorType.BitNot:
					return "op_OnesComplement";
				case UnaryOperatorType.Minus:
					return "op_UnaryNegation";
				case UnaryOperatorType.Plus:
					return "op_UnaryPlus";
				case UnaryOperatorType.Increment:
				case UnaryOperatorType.PostIncrement:
					return "op_Increment";
				case UnaryOperatorType.Decrement:
				case UnaryOperatorType.PostDecrement:
					return "op_Decrement";
				default:
					return null;
			}
		}
		#endregion
		
		#region Unary operator class definitions
		abstract class UnaryOperatorMethod : OperatorMethod
		{
			public abstract object Invoke(CSharpResolver resolver, object input);
		}
		
		sealed class LambdaUnaryOperatorMethod<T> : UnaryOperatorMethod
		{
			readonly Func<T, T> func;
			
			public LambdaUnaryOperatorMethod(Func<T, T> func)
			{
				TypeCode t = Type.GetTypeCode(typeof(T));
				this.ReturnType = t.ToTypeReference();
				this.Parameters.Add(MakeParameter(t));
				this.func = func;
			}
			
			public override object Invoke(CSharpResolver resolver, object input)
			{
				return func((T)resolver.CSharpPrimitiveCast(Type.GetTypeCode(typeof(T)), input));
			}
			
			public override OperatorMethod Lift()
			{
				return new LiftedUnaryOperatorMethod(this);
			}
		}
		
		sealed class LiftedUnaryOperatorMethod : UnaryOperatorMethod, OverloadResolution.ILiftedOperator
		{
			UnaryOperatorMethod baseMethod;
			
			public LiftedUnaryOperatorMethod(UnaryOperatorMethod baseMethod)
			{
				this.baseMethod = baseMethod;
				this.ReturnType = NullableType.Create(baseMethod.ReturnType);
				this.Parameters.Add(MakeNullableParameter(baseMethod.Parameters[0]));
			}
			
			public override object Invoke(CSharpResolver resolver, object input)
			{
				if (input == null)
					return null;
				else
					return baseMethod.Invoke(resolver, input);
			}
			
			public IList<IParameter> NonLiftedParameters {
				get { return baseMethod.Parameters; }
			}
		}
		#endregion
		
		#region Unary operator definitions
		// C# 4.0 spec: §7.7.1 Unary plus operator
		static readonly OperatorMethod[] unaryPlusOperators = Lift(
			new LambdaUnaryOperatorMethod<int>(i => +i),
			new LambdaUnaryOperatorMethod<uint>(i => +i),
			new LambdaUnaryOperatorMethod<long>(i => +i),
			new LambdaUnaryOperatorMethod<ulong>(i => +i),
			new LambdaUnaryOperatorMethod<float>(i => +i),
			new LambdaUnaryOperatorMethod<double>(i => +i),
			new LambdaUnaryOperatorMethod<decimal>(i => +i)
		);
		
		// C# 4.0 spec: §7.7.2 Unary minus operator
		static readonly OperatorMethod[] uncheckedUnaryMinusOperators = Lift(
			new LambdaUnaryOperatorMethod<int>(i => unchecked(-i)),
			new LambdaUnaryOperatorMethod<long>(i => unchecked(-i)),
			new LambdaUnaryOperatorMethod<float>(i => -i),
			new LambdaUnaryOperatorMethod<double>(i => -i),
			new LambdaUnaryOperatorMethod<decimal>(i => -i)
		);
		static readonly OperatorMethod[] checkedUnaryMinusOperators = Lift(
			new LambdaUnaryOperatorMethod<int>(i => checked(-i)),
			new LambdaUnaryOperatorMethod<long>(i => checked(-i)),
			new LambdaUnaryOperatorMethod<float>(i => -i),
			new LambdaUnaryOperatorMethod<double>(i => -i),
			new LambdaUnaryOperatorMethod<decimal>(i => -i)
		);
		
		// C# 4.0 spec: §7.7.3 Logical negation operator
		static readonly OperatorMethod[] logicalNegationOperator = Lift(new LambdaUnaryOperatorMethod<bool>(b => !b));
		
		// C# 4.0 spec: §7.7.4 Bitwise complement operator
		static readonly OperatorMethod[] bitwiseComplementOperators = Lift(
			new LambdaUnaryOperatorMethod<int>(i => ~i),
			new LambdaUnaryOperatorMethod<uint>(i => ~i),
			new LambdaUnaryOperatorMethod<long>(i => ~i),
			new LambdaUnaryOperatorMethod<ulong>(i => ~i)
		);
		#endregion
		
		object GetUserUnaryOperatorCandidates()
		{
			// C# 4.0 spec: §7.3.5 Candidate user-defined operators
			// TODO: implement user-defined operators
			throw new NotImplementedException();
		}
		#endregion
		
		#region ResolveBinaryOperator
		#region ResolveBinaryOperator method
		public ResolveResult ResolveBinaryOperator(BinaryOperatorType op, ResolveResult lhs, ResolveResult rhs)
		{
			cancellationToken.ThrowIfCancellationRequested();
			
			if (lhs.Type == SharedTypes.Dynamic || rhs.Type == SharedTypes.Dynamic)
				return DynamicResult;
			
			// C# 4.0 spec: §7.3.4 Binary operator overload resolution
			string overloadableOperatorName = GetOverloadableOperatorName(op);
			if (overloadableOperatorName == null) {
				
				// Handle logical and/or exactly as bitwise and/or:
				// - If the user overloads a bitwise operator, that implicitly creates the corresponding logical operator.
				// - If both inputs are compile-time constants, it doesn't matter that we don't short-circuit.
				// - If inputs aren't compile-time constants, we don't evaluate anything, so again it doesn't matter that we don't short-circuit
				if (op == BinaryOperatorType.ConditionalAnd) {
					overloadableOperatorName = GetOverloadableOperatorName(BinaryOperatorType.BitwiseAnd);
				} else if (op == BinaryOperatorType.ConditionalOr) {
					overloadableOperatorName = GetOverloadableOperatorName(BinaryOperatorType.BitwiseOr);
				} else if (op == BinaryOperatorType.NullCoalescing) {
					// null coalescing operator is not overloadable and needs to be handled separately
					return ResolveNullCoalescingOperator(lhs, rhs);
				} else {
					throw new ArgumentException("Invalid value for BinaryOperatorType", "op");
				}
			}
			
			// If the type is nullable, get the underlying type:
			bool isNullable = NullableType.IsNullable(lhs.Type) || NullableType.IsNullable(rhs.Type);
			IType lhsType = NullableType.GetUnderlyingType(lhs.Type);
			IType rhsType = NullableType.GetUnderlyingType(rhs.Type);
			
			// TODO: find user-defined operators
			
			if (lhsType == SharedTypes.Null && rhsType.IsReferenceType == false
			    || lhsType.IsReferenceType == false && rhsType == SharedTypes.Null)
			{
				isNullable = true;
			}
			if (op == BinaryOperatorType.ShiftLeft || op == BinaryOperatorType.ShiftRight) {
				// special case: the shift operators allow "var x = null << null", producing int?.
				if (lhsType == SharedTypes.Null && rhsType == SharedTypes.Null)
					isNullable = true;
				// for shift operators, do unary promotion independently on both arguments
				lhs = UnaryNumericPromotion(UnaryOperatorType.Plus, ref lhsType, isNullable, lhs);
				rhs = UnaryNumericPromotion(UnaryOperatorType.Plus, ref rhsType, isNullable, rhs);
			} else {
				bool allowNullableConstants = op == BinaryOperatorType.Equality || op == BinaryOperatorType.InEquality;
				if (!BinaryNumericPromotion(isNullable, ref lhs, ref rhs, allowNullableConstants))
					return new ErrorResolveResult(lhs.Type);
			}
			// re-read underlying types after numeric promotion
			lhsType = NullableType.GetUnderlyingType(lhs.Type);
			rhsType = NullableType.GetUnderlyingType(rhs.Type);
			
			IEnumerable<OperatorMethod> methodGroup;
			switch (op) {
				case BinaryOperatorType.Multiply:
					methodGroup = CheckForOverflow ? checkedMultiplicationOperators : uncheckedMultiplicationOperators;
					break;
				case BinaryOperatorType.Divide:
					methodGroup = CheckForOverflow ? checkedDivisionOperators : uncheckedDivisionOperators;
					break;
				case BinaryOperatorType.Modulus:
					methodGroup = CheckForOverflow ? checkedRemainderOperators : uncheckedRemainderOperators;
					break;
				case BinaryOperatorType.Add:
					methodGroup = CheckForOverflow ? checkedAdditionOperators : uncheckedAdditionOperators;
					{
						Conversions conversions = new Conversions(context);
						if (lhsType.IsEnum() && conversions.ImplicitConversion(rhsType, lhsType.GetEnumUnderlyingType(context))) {
							// E operator +(E x, U y);
							return HandleEnumAdditionOrSubtraction(isNullable, lhsType, op, lhs, rhs);
						} else if (rhsType.IsEnum() && conversions.ImplicitConversion(lhsType, rhsType.GetEnumUnderlyingType(context))) {
							// E operator +(U x, E y);
							return ResolveBinaryOperator(op, rhs, lhs); // swap arguments
						}
						if (lhsType.IsDelegate() && conversions.ImplicitConversion(rhsType, lhsType)) {
							return new ResolveResult(lhsType);
						} else if (rhsType.IsDelegate() && conversions.ImplicitConversion(lhsType, rhsType)) {
							return new ResolveResult(rhsType);
						}
						if (lhsType is PointerType && IsInteger(ReflectionHelper.GetTypeCode(rhsType))) {
							return new ResolveResult(lhsType);
						} else if (rhsType is PointerType && IsInteger(ReflectionHelper.GetTypeCode(lhsType))) {
							return new ResolveResult(rhsType);
						}
						if (lhsType == SharedTypes.Null && rhsType == SharedTypes.Null)
							return new ErrorResolveResult(SharedTypes.Null);
					}
					break;
				case BinaryOperatorType.Subtract:
					methodGroup = CheckForOverflow ? checkedSubtractionOperators : uncheckedSubtractionOperators;
					{
						Conversions conversions = new Conversions(context);
						if (lhsType.IsEnum() && conversions.ImplicitConversion(rhsType, lhsType.GetEnumUnderlyingType(context))) {
							// E operator –(E x, U y);
							return HandleEnumAdditionOrSubtraction(isNullable, lhsType, op, lhs, rhs);
						} else if (lhsType.IsEnum() && conversions.ImplicitConversion(rhs, lhs.Type)) {
							// U operator –(E x, E y);
							return HandleEnumSubtraction(isNullable, lhsType, lhs, rhs);
						} else if (rhsType.IsEnum() && conversions.ImplicitConversion(lhs, rhs.Type)) {
							// U operator –(E x, E y);
							return HandleEnumSubtraction(isNullable, lhsType, lhs, rhs);
						}
						if (lhsType.IsDelegate() && conversions.ImplicitConversion(rhsType, lhsType)) {
							return new ResolveResult(lhsType);
						} else if (rhsType.IsDelegate() && conversions.ImplicitConversion(lhsType, rhsType)) {
							return new ResolveResult(rhsType);
						}
						if (lhsType is PointerType && IsInteger(ReflectionHelper.GetTypeCode(rhsType))) {
							return new ResolveResult(lhsType);
						} else if (lhsType is PointerType && lhsType.Equals(rhsType)) {
							return new ResolveResult(KnownTypeReference.Int64.Resolve(context));
						}
						if (lhsType == SharedTypes.Null && rhsType == SharedTypes.Null)
							return new ErrorResolveResult(SharedTypes.Null);
					}
					break;
				case BinaryOperatorType.ShiftLeft:
					methodGroup = shiftLeftOperators;
					break;
				case BinaryOperatorType.ShiftRight:
					methodGroup = shiftRightOperators;
					break;
				case BinaryOperatorType.Equality:
				case BinaryOperatorType.InEquality:
				case BinaryOperatorType.LessThan:
				case BinaryOperatorType.GreaterThan:
				case BinaryOperatorType.LessThanOrEqual:
				case BinaryOperatorType.GreaterThanOrEqual:
					{
						Conversions conversions = new Conversions(context);
						if (lhsType.IsEnum() && conversions.ImplicitConversion(rhs, lhs.Type)) {
							// bool operator op(E x, E y);
							return HandleEnumComparison(op, lhsType, isNullable, lhs, rhs);
						} else if (rhsType.IsEnum() && conversions.ImplicitConversion(lhs, rhs.Type)) {
							// bool operator op(E x, E y);
							return HandleEnumComparison(op, rhsType, isNullable, lhs, rhs);
						} else if (lhsType is PointerType && rhsType is PointerType) {
							return new ResolveResult(KnownTypeReference.Boolean.Resolve(context));
						}
						switch (op) {
							case BinaryOperatorType.Equality:
								methodGroup = equalityOperators;
								break;
							case BinaryOperatorType.InEquality:
								methodGroup = inequalityOperators;
								break;
							case BinaryOperatorType.LessThan:
								methodGroup = lessThanOperators;
								break;
							case BinaryOperatorType.GreaterThan:
								methodGroup = greaterThanOperators;
								break;
							case BinaryOperatorType.LessThanOrEqual:
								methodGroup = lessThanOrEqualOperators;
								break;
							case BinaryOperatorType.GreaterThanOrEqual:
								methodGroup = greaterThanOrEqualOperators;
								break;
							default:
								throw new InvalidOperationException();
						}
					}
					break;
				case BinaryOperatorType.BitwiseAnd:
				case BinaryOperatorType.BitwiseOr:
				case BinaryOperatorType.ExclusiveOr:
					{
						Conversions conversions = new Conversions(context);
						if (lhsType.IsEnum() && conversions.ImplicitConversion(rhs, lhs.Type)) {
							// E operator op(E x, E y);
							return HandleEnumAdditionOrSubtraction(isNullable, lhsType, op, lhs, rhs);
						} else if (rhsType.IsEnum() && conversions.ImplicitConversion(lhs, rhs.Type)) {
							// E operator op(E x, E y);
							return HandleEnumAdditionOrSubtraction(isNullable, rhsType, op, lhs, rhs);
						}
						switch (op) {
							case BinaryOperatorType.BitwiseAnd:
								methodGroup = bitwiseAndOperators;
								break;
							case BinaryOperatorType.BitwiseOr:
								methodGroup = bitwiseOrOperators;
								break;
							case BinaryOperatorType.ExclusiveOr:
								methodGroup = bitwiseXorOperators;
								break;
							default:
								throw new InvalidOperationException();
						}
					}
					break;
				case BinaryOperatorType.ConditionalAnd:
					methodGroup = logicalAndOperator;
					break;
				case BinaryOperatorType.ConditionalOr:
					methodGroup = logicalOrOperator;
					break;
				default:
					throw new InvalidOperationException();
			}
			OverloadResolution r = new OverloadResolution(context, new[] { lhs, rhs });
			foreach (var candidate in methodGroup) {
				r.AddCandidate(candidate);
			}
			BinaryOperatorMethod m = (BinaryOperatorMethod)r.BestCandidate;
			IType resultType = m.ReturnType.Resolve(context);
			if (r.BestCandidateErrors != OverloadResolutionErrors.None) {
				return new ErrorResolveResult(resultType);
			} else if (lhs.IsCompileTimeConstant && rhs.IsCompileTimeConstant && m.CanEvaluateAtCompileTime) {
				object val;
				try {
					val = m.Invoke(this, lhs.ConstantValue, rhs.ConstantValue);
				} catch (ArithmeticException) {
					return new ErrorResolveResult(resultType);
				}
				return new ConstantResolveResult(resultType, val);
			} else {
				return new ResolveResult(resultType);
			}
		}
		#endregion
		
		#region Enum helper methods
		/// <summary>
		/// Handle the case where an enum value is compared with another enum value
		/// bool operator op(E x, E y);
		/// </summary>
		ResolveResult HandleEnumComparison(BinaryOperatorType op, IType enumType, bool isNullable, ResolveResult lhs, ResolveResult rhs)
		{
			// evaluate as ((U)x op (U)y)
			IType elementType = enumType.GetEnumUnderlyingType(context);
			if (lhs.IsCompileTimeConstant && rhs.IsCompileTimeConstant && !isNullable) {
				lhs = ResolveCast(elementType, lhs);
				if (lhs.IsError)
					return lhs;
				rhs = ResolveCast(elementType, rhs);
				if (rhs.IsError)
					return rhs;
				return ResolveBinaryOperator(op, lhs, rhs);
			}
			return new ResolveResult(KnownTypeReference.Boolean.Resolve(context));
		}
		
		/// <summary>
		/// Handle the case where an enum value is subtracted from another enum value
		/// U operator –(E x, E y);
		/// </summary>
		ResolveResult HandleEnumSubtraction(bool isNullable, IType enumType, ResolveResult lhs, ResolveResult rhs)
		{
			// evaluate as (U)((U)x – (U)y)
			IType elementType = enumType.GetEnumUnderlyingType(context);
			if (lhs.IsCompileTimeConstant && rhs.IsCompileTimeConstant && !isNullable) {
				lhs = ResolveCast(elementType, lhs);
				if (lhs.IsError)
					return lhs;
				rhs = ResolveCast(elementType, rhs);
				if (rhs.IsError)
					return rhs;
				return CheckErrorAndResolveCast(elementType, ResolveBinaryOperator(BinaryOperatorType.Subtract, lhs, rhs));
			}
			return new ResolveResult(isNullable ? NullableType.Create(elementType, context) : elementType);
		}
		
		/// <summary>
		/// Handle the case where an integral value is added to or subtracted from an enum value,
		/// or when two enum values of the same type are combined using a bitwise operator.
		/// E operator +(E x, U y);
		/// E operator –(E x, U y);
		/// E operator &amp;(E x, E y);
		/// E operator |(E x, E y);
		/// E operator ^(E x, E y);
		/// </summary>
		ResolveResult HandleEnumAdditionOrSubtraction(bool isNullable, IType enumType, BinaryOperatorType op, ResolveResult lhs, ResolveResult rhs)
		{
			// evaluate as (E)((U)x op (U)y)
			if (lhs.IsCompileTimeConstant && rhs.IsCompileTimeConstant && !isNullable) {
				IType elementType = enumType.GetEnumUnderlyingType(context);
				lhs = ResolveCast(elementType, lhs);
				if (lhs.IsError)
					return lhs;
				rhs = ResolveCast(elementType, rhs);
				if (rhs.IsError)
					return rhs;
				return CheckErrorAndResolveCast(enumType, ResolveBinaryOperator(op, lhs, rhs));
			}
			return new ResolveResult(isNullable ? NullableType.Create(enumType, context) : enumType);
		}
		#endregion
		
		#region BinaryNumericPromotion
		bool BinaryNumericPromotion(bool isNullable, ref ResolveResult lhs, ref ResolveResult rhs, bool allowNullableConstants)
		{
			// C# 4.0 spec: §7.3.6.2
			TypeCode lhsCode = ReflectionHelper.GetTypeCode(NullableType.GetUnderlyingType(lhs.Type));
			TypeCode rhsCode = ReflectionHelper.GetTypeCode(NullableType.GetUnderlyingType(rhs.Type));
			// if one of the inputs is the null literal, promote that to the type of the other operand
			if (isNullable && lhs.Type == SharedTypes.Null) {
				lhs = CastTo(rhsCode, isNullable, lhs, allowNullableConstants);
				lhsCode = rhsCode;
			} else if (isNullable && rhs.Type == SharedTypes.Null) {
				rhs = CastTo(lhsCode, isNullable, rhs, allowNullableConstants);
				rhsCode = lhsCode;
			}
			bool bindingError = false;
			if (lhsCode >= TypeCode.Char && lhsCode <= TypeCode.Decimal
			    && rhsCode >= TypeCode.Char && rhsCode <= TypeCode.Decimal)
			{
				TypeCode targetType;
				if (lhsCode == TypeCode.Decimal || rhsCode == TypeCode.Decimal) {
					targetType = TypeCode.Decimal;
					bindingError = (lhsCode == TypeCode.Single || lhsCode == TypeCode.Double
					                || rhsCode == TypeCode.Single || rhsCode == TypeCode.Double);
				} else if (lhsCode == TypeCode.Double || rhsCode == TypeCode.Double) {
					targetType = TypeCode.Double;
				} else if (lhsCode == TypeCode.Single || rhsCode == TypeCode.Single) {
					targetType = TypeCode.Single;
				} else if (lhsCode == TypeCode.UInt64 || rhsCode == TypeCode.UInt64) {
					targetType = TypeCode.UInt64;
					bindingError = IsSigned(lhsCode, lhs) || IsSigned(rhsCode, rhs);
				} else if (lhsCode == TypeCode.Int64 || rhsCode == TypeCode.Int64) {
					targetType = TypeCode.Int64;
				} else if (lhsCode == TypeCode.UInt32 || rhsCode == TypeCode.UInt32) {
					targetType = (IsSigned(lhsCode, lhs) || IsSigned(rhsCode, rhs)) ? TypeCode.Int64 : TypeCode.UInt32;
				} else {
					targetType = TypeCode.Int32;
				}
				lhs = CastTo(targetType, isNullable, lhs, allowNullableConstants);
				rhs = CastTo(targetType, isNullable, rhs, allowNullableConstants);
			}
			return !bindingError;
		}
		
		bool IsSigned(TypeCode code, ResolveResult rr)
		{
			// Determine whether the rr with code==ReflectionHelper.GetTypeCode(NullableType.GetUnderlyingType(rr.Type))
			// is a signed primitive type.
			switch (code) {
				case TypeCode.SByte:
				case TypeCode.Int16:
					return true;
				case TypeCode.Int32:
					// for int, consider implicit constant expression conversion
					if (rr.IsCompileTimeConstant && rr.ConstantValue != null && (int)rr.ConstantValue >= 0)
						return false;
					else
						return true;
				case TypeCode.Int64:
					// for long, consider implicit constant expression conversion
					if (rr.IsCompileTimeConstant && rr.ConstantValue != null && (long)rr.ConstantValue >= 0)
						return false;
					else
						return true;
				default:
					return false;
			}
		}
		
		static bool IsInteger(TypeCode code)
		{
			return code >= TypeCode.SByte && code <= TypeCode.UInt64;
		}
		
		ResolveResult CastTo(TypeCode targetType, bool isNullable, ResolveResult expression, bool allowNullableConstants)
		{
			IType elementType = targetType.ToTypeReference().Resolve(context);
			IType nullableType = isNullable ? NullableType.Create(elementType, context) : elementType;
			if (allowNullableConstants && expression.IsCompileTimeConstant) {
				if (expression.ConstantValue == null)
					return new ConstantResolveResult(nullableType, null);
				ResolveResult rr = ResolveCast(elementType, expression);
				if (rr.IsError)
					return rr;
				Debug.Assert(rr.IsCompileTimeConstant);
				return new ConstantResolveResult(nullableType, rr.ConstantValue);
			} else {
				return ResolveCast(nullableType, expression);
			}
		}
		#endregion
		
		#region GetOverloadableOperatorName
		static string GetOverloadableOperatorName(BinaryOperatorType op)
		{
			switch (op) {
				case BinaryOperatorType.Add:
					return "op_Addition";
				case BinaryOperatorType.Subtract:
					return "op_Subtraction";
				case BinaryOperatorType.Multiply:
					return "op_Multiply";
				case BinaryOperatorType.Divide:
					return "op_Division";
				case BinaryOperatorType.Modulus:
					return "op_Modulus";
				case BinaryOperatorType.BitwiseAnd:
					return "op_BitwiseAnd";
				case BinaryOperatorType.BitwiseOr:
					return "op_BitwiseOr";
				case BinaryOperatorType.ExclusiveOr:
					return "op_ExclusiveOr";
				case BinaryOperatorType.ShiftLeft:
					return "op_LeftShift";
				case BinaryOperatorType.ShiftRight:
					return "op_RightShift";
				case BinaryOperatorType.Equality:
					return "op_Equality";
				case BinaryOperatorType.InEquality:
					return "op_Inequality";
				case BinaryOperatorType.GreaterThan:
					return "op_GreaterThan";
				case BinaryOperatorType.LessThan:
					return "op_LessThan";
				case BinaryOperatorType.GreaterThanOrEqual:
					return "op_GreaterThanOrEqual";
				case BinaryOperatorType.LessThanOrEqual:
					return "op_LessThanOrEqual";
				default:
					return null;
			}
		}
		#endregion
		
		#region Binary operator class definitions
		abstract class BinaryOperatorMethod : OperatorMethod
		{
			public virtual bool CanEvaluateAtCompileTime { get { return true; } }
			public abstract object Invoke(CSharpResolver resolver, object lhs, object rhs);
		}
		
		sealed class LambdaBinaryOperatorMethod<T1, T2> : BinaryOperatorMethod
		{
			readonly Func<T1, T2, T1> func;
			
			public LambdaBinaryOperatorMethod(Func<T1, T2, T1> func)
			{
				TypeCode t1 = Type.GetTypeCode(typeof(T1));
				this.ReturnType = t1.ToTypeReference();
				this.Parameters.Add(MakeParameter(t1));
				this.Parameters.Add(MakeParameter(Type.GetTypeCode(typeof(T2))));
				this.func = func;
			}
			
			public override object Invoke(CSharpResolver resolver, object lhs, object rhs)
			{
				return func((T1)resolver.CSharpPrimitiveCast(Type.GetTypeCode(typeof(T1)), lhs),
				            (T2)resolver.CSharpPrimitiveCast(Type.GetTypeCode(typeof(T2)), rhs));
			}
			
			public override OperatorMethod Lift()
			{
				return new LiftedBinaryOperatorMethod(this);
			}
		}
		
		sealed class LiftedBinaryOperatorMethod : BinaryOperatorMethod, OverloadResolution.ILiftedOperator
		{
			readonly BinaryOperatorMethod baseMethod;
			
			public LiftedBinaryOperatorMethod(BinaryOperatorMethod baseMethod)
			{
				this.baseMethod = baseMethod;
				this.ReturnType = NullableType.Create(baseMethod.ReturnType);
				this.Parameters.Add(MakeNullableParameter(baseMethod.Parameters[0]));
				this.Parameters.Add(MakeNullableParameter(baseMethod.Parameters[1]));
			}
			
			public override bool CanEvaluateAtCompileTime {
				get { return false; }
			}
			
			public override object Invoke(CSharpResolver resolver, object lhs, object rhs)
			{
				throw new NotSupportedException(); // cannot use nullables at compile time
			}
			
			public IList<IParameter> NonLiftedParameters {
				get { return baseMethod.Parameters; }
			}
		}
		#endregion
		
		#region Arithmetic operators
		// C# 4.0 spec: §7.8.1 Multiplication operator
		static readonly OperatorMethod[] checkedMultiplicationOperators = Lift(
			new LambdaBinaryOperatorMethod<int,     int>    ((a, b) => checked(a * b)),
			new LambdaBinaryOperatorMethod<uint,    uint>   ((a, b) => checked(a * b)),
			new LambdaBinaryOperatorMethod<long,    long>   ((a, b) => checked(a * b)),
			new LambdaBinaryOperatorMethod<ulong,   ulong>  ((a, b) => checked(a * b)),
			new LambdaBinaryOperatorMethod<float,   float>  ((a, b) => checked(a * b)),
			new LambdaBinaryOperatorMethod<double,  double> ((a, b) => checked(a * b)),
			new LambdaBinaryOperatorMethod<decimal, decimal>((a, b) => checked(a * b))
		);
		static readonly OperatorMethod[] uncheckedMultiplicationOperators = Lift(
			new LambdaBinaryOperatorMethod<int,     int>    ((a, b) => unchecked(a * b)),
			new LambdaBinaryOperatorMethod<uint,    uint>   ((a, b) => unchecked(a * b)),
			new LambdaBinaryOperatorMethod<long,    long>   ((a, b) => unchecked(a * b)),
			new LambdaBinaryOperatorMethod<ulong,   ulong>  ((a, b) => unchecked(a * b)),
			new LambdaBinaryOperatorMethod<float,   float>  ((a, b) => unchecked(a * b)),
			new LambdaBinaryOperatorMethod<double,  double> ((a, b) => unchecked(a * b)),
			new LambdaBinaryOperatorMethod<decimal, decimal>((a, b) => unchecked(a * b))
		);
		
		// C# 4.0 spec: §7.8.2 Division operator
		static readonly OperatorMethod[] checkedDivisionOperators = Lift(
			new LambdaBinaryOperatorMethod<int,     int>    ((a, b) => checked(a / b)),
			new LambdaBinaryOperatorMethod<uint,    uint>   ((a, b) => checked(a / b)),
			new LambdaBinaryOperatorMethod<long,    long>   ((a, b) => checked(a / b)),
			new LambdaBinaryOperatorMethod<ulong,   ulong>  ((a, b) => checked(a / b)),
			new LambdaBinaryOperatorMethod<float,   float>  ((a, b) => checked(a / b)),
			new LambdaBinaryOperatorMethod<double,  double> ((a, b) => checked(a / b)),
			new LambdaBinaryOperatorMethod<decimal, decimal>((a, b) => checked(a / b))
		);
		static readonly OperatorMethod[] uncheckedDivisionOperators = Lift(
			new LambdaBinaryOperatorMethod<int,     int>    ((a, b) => unchecked(a / b)),
			new LambdaBinaryOperatorMethod<uint,    uint>   ((a, b) => unchecked(a / b)),
			new LambdaBinaryOperatorMethod<long,    long>   ((a, b) => unchecked(a / b)),
			new LambdaBinaryOperatorMethod<ulong,   ulong>  ((a, b) => unchecked(a / b)),
			new LambdaBinaryOperatorMethod<float,   float>  ((a, b) => unchecked(a / b)),
			new LambdaBinaryOperatorMethod<double,  double> ((a, b) => unchecked(a / b)),
			new LambdaBinaryOperatorMethod<decimal, decimal>((a, b) => unchecked(a / b))
		);
		
		// C# 4.0 spec: §7.8.3 Remainder operator
		static readonly OperatorMethod[] checkedRemainderOperators = Lift(
			new LambdaBinaryOperatorMethod<int,     int>    ((a, b) => checked(a % b)),
			new LambdaBinaryOperatorMethod<uint,    uint>   ((a, b) => checked(a % b)),
			new LambdaBinaryOperatorMethod<long,    long>   ((a, b) => checked(a % b)),
			new LambdaBinaryOperatorMethod<ulong,   ulong>  ((a, b) => checked(a % b)),
			new LambdaBinaryOperatorMethod<float,   float>  ((a, b) => checked(a % b)),
			new LambdaBinaryOperatorMethod<double,  double> ((a, b) => checked(a % b)),
			new LambdaBinaryOperatorMethod<decimal, decimal>((a, b) => checked(a % b))
		);
		static readonly OperatorMethod[] uncheckedRemainderOperators = Lift(
			new LambdaBinaryOperatorMethod<int,     int>    ((a, b) => unchecked(a % b)),
			new LambdaBinaryOperatorMethod<uint,    uint>   ((a, b) => unchecked(a % b)),
			new LambdaBinaryOperatorMethod<long,    long>   ((a, b) => unchecked(a % b)),
			new LambdaBinaryOperatorMethod<ulong,   ulong>  ((a, b) => unchecked(a % b)),
			new LambdaBinaryOperatorMethod<float,   float>  ((a, b) => unchecked(a % b)),
			new LambdaBinaryOperatorMethod<double,  double> ((a, b) => unchecked(a % b)),
			new LambdaBinaryOperatorMethod<decimal, decimal>((a, b) => unchecked(a % b))
		);
		
		// C# 4.0 spec: §7.8.3 Addition operator
		static readonly OperatorMethod[] checkedAdditionOperators = Lift(
			new LambdaBinaryOperatorMethod<int,     int>    ((a, b) => checked(a + b)),
			new LambdaBinaryOperatorMethod<uint,    uint>   ((a, b) => checked(a + b)),
			new LambdaBinaryOperatorMethod<long,    long>   ((a, b) => checked(a + b)),
			new LambdaBinaryOperatorMethod<ulong,   ulong>  ((a, b) => checked(a + b)),
			new LambdaBinaryOperatorMethod<float,   float>  ((a, b) => checked(a + b)),
			new LambdaBinaryOperatorMethod<double,  double> ((a, b) => checked(a + b)),
			new LambdaBinaryOperatorMethod<decimal, decimal>((a, b) => checked(a + b)),
			new StringConcatenation(TypeCode.String, TypeCode.String),
			new StringConcatenation(TypeCode.String, TypeCode.Object),
			new StringConcatenation(TypeCode.Object, TypeCode.String)
		);
		static readonly OperatorMethod[] uncheckedAdditionOperators = Lift(
			new LambdaBinaryOperatorMethod<int,     int>    ((a, b) => unchecked(a + b)),
			new LambdaBinaryOperatorMethod<uint,    uint>   ((a, b) => unchecked(a + b)),
			new LambdaBinaryOperatorMethod<long,    long>   ((a, b) => unchecked(a + b)),
			new LambdaBinaryOperatorMethod<ulong,   ulong>  ((a, b) => unchecked(a + b)),
			new LambdaBinaryOperatorMethod<float,   float>  ((a, b) => unchecked(a + b)),
			new LambdaBinaryOperatorMethod<double,  double> ((a, b) => unchecked(a + b)),
			new LambdaBinaryOperatorMethod<decimal, decimal>((a, b) => unchecked(a + b)),
			new StringConcatenation(TypeCode.String, TypeCode.String),
			new StringConcatenation(TypeCode.String, TypeCode.Object),
			new StringConcatenation(TypeCode.Object, TypeCode.String)
		);
		// not in this list, but handled manually: enum addition, delegate combination
		sealed class StringConcatenation : BinaryOperatorMethod
		{
			bool canEvaluateAtCompileTime;
			
			public StringConcatenation(TypeCode p1, TypeCode p2)
			{
				this.canEvaluateAtCompileTime = p1 == TypeCode.String && p2 == TypeCode.String;
				this.ReturnType = KnownTypeReference.String;
				this.Parameters.Add(MakeParameter(p1));
				this.Parameters.Add(MakeParameter(p2));
			}
			
			public override bool CanEvaluateAtCompileTime {
				get { return canEvaluateAtCompileTime; }
			}
			
			public override object Invoke(CSharpResolver resolver, object lhs, object rhs)
			{
				return string.Concat(lhs, rhs);
			}
		}
		
		// C# 4.0 spec: §7.8.4 Subtraction operator
		static readonly OperatorMethod[] checkedSubtractionOperators = Lift(
			new LambdaBinaryOperatorMethod<int,     int>    ((a, b) => checked(a - b)),
			new LambdaBinaryOperatorMethod<uint,    uint>   ((a, b) => checked(a - b)),
			new LambdaBinaryOperatorMethod<long,    long>   ((a, b) => checked(a - b)),
			new LambdaBinaryOperatorMethod<ulong,   ulong>  ((a, b) => checked(a - b)),
			new LambdaBinaryOperatorMethod<float,   float>  ((a, b) => checked(a - b)),
			new LambdaBinaryOperatorMethod<double,  double> ((a, b) => checked(a - b)),
			new LambdaBinaryOperatorMethod<decimal, decimal>((a, b) => checked(a - b))
		);
		static readonly OperatorMethod[] uncheckedSubtractionOperators = Lift(
			new LambdaBinaryOperatorMethod<int,     int>    ((a, b) => unchecked(a - b)),
			new LambdaBinaryOperatorMethod<uint,    uint>   ((a, b) => unchecked(a - b)),
			new LambdaBinaryOperatorMethod<long,    long>   ((a, b) => unchecked(a - b)),
			new LambdaBinaryOperatorMethod<ulong,   ulong>  ((a, b) => unchecked(a - b)),
			new LambdaBinaryOperatorMethod<float,   float>  ((a, b) => unchecked(a - b)),
			new LambdaBinaryOperatorMethod<double,  double> ((a, b) => unchecked(a - b)),
			new LambdaBinaryOperatorMethod<decimal, decimal>((a, b) => unchecked(a - b))
		);
		
		// C# 4.0 spec: §7.8.5 Shift operators
		static readonly OperatorMethod[] shiftLeftOperators = Lift(
			new LambdaBinaryOperatorMethod<int,   int>((a, b) => a << b),
			new LambdaBinaryOperatorMethod<uint,  int>((a, b) => a << b),
			new LambdaBinaryOperatorMethod<long,  int>((a, b) => a << b),
			new LambdaBinaryOperatorMethod<ulong, int>((a, b) => a << b)
		);
		static readonly OperatorMethod[] shiftRightOperators = Lift(
			new LambdaBinaryOperatorMethod<int,   int>((a, b) => a >> b),
			new LambdaBinaryOperatorMethod<uint,  int>((a, b) => a >> b),
			new LambdaBinaryOperatorMethod<long,  int>((a, b) => a >> b),
			new LambdaBinaryOperatorMethod<ulong, int>((a, b) => a >> b)
		);
		#endregion
		
		#region Equality operators
		sealed class EqualityOperatorMethod : BinaryOperatorMethod
		{
			public readonly TypeCode Type;
			public readonly bool Negate;
			
			public EqualityOperatorMethod(TypeCode type, bool negate)
			{
				this.Negate = negate;
				this.Type = type;
				this.ReturnType = KnownTypeReference.Boolean;
				this.Parameters.Add(MakeParameter(type));
				this.Parameters.Add(MakeParameter(type));
			}
			
			public override bool CanEvaluateAtCompileTime {
				get { return Type != TypeCode.Object; }
			}
			
			public override object Invoke(CSharpResolver resolver, object lhs, object rhs)
			{
				lhs = resolver.CSharpPrimitiveCast(Type, lhs);
				rhs = resolver.CSharpPrimitiveCast(Type, rhs);
				bool equal;
				if (Type == TypeCode.Single) {
					equal = (float)lhs == (float)rhs;
				} else if (Type == TypeCode.Double) {
					equal = (double)lhs == (double)rhs;
				} else {
					equal = object.Equals(lhs, rhs);
				}
				return equal ^ Negate;
			}
			
			public override OperatorMethod Lift()
			{
				if (Type == TypeCode.Object || Type == TypeCode.String)
					return null;
				else
					return new LiftedEqualityOperatorMethod(this);
			}
		}
		
		sealed class LiftedEqualityOperatorMethod : BinaryOperatorMethod, OverloadResolution.ILiftedOperator
		{
			readonly EqualityOperatorMethod baseMethod;
			
			public LiftedEqualityOperatorMethod(EqualityOperatorMethod baseMethod)
			{
				this.baseMethod = baseMethod;
				this.ReturnType = baseMethod.ReturnType;
				IParameter p = MakeNullableParameter(baseMethod.Parameters[0]);
				this.Parameters.Add(p);
				this.Parameters.Add(p);
			}
			
			public override bool CanEvaluateAtCompileTime {
				get { return baseMethod.CanEvaluateAtCompileTime; }
			}
			
			public override object Invoke(CSharpResolver resolver, object lhs, object rhs)
			{
				if (lhs == null && rhs == null)
					return !baseMethod.Negate; // ==: true; !=: false
				if (lhs == null || rhs == null)
					return baseMethod.Negate; // ==: false; !=: true
				return baseMethod.Invoke(resolver, lhs, rhs);
			}
			
			public IList<IParameter> NonLiftedParameters {
				get { return baseMethod.Parameters; }
			}
		}
		
		// C# 4.0 spec: §7.10 Relational and type-testing operators
		static readonly TypeCode[] equalityOperatorsFor = {
			TypeCode.Int32, TypeCode.UInt32,
			TypeCode.Int64, TypeCode.UInt64,
			TypeCode.Single, TypeCode.Double,
			TypeCode.Decimal,
			TypeCode.Boolean,
			TypeCode.String, TypeCode.Object
		};
		
		static readonly OperatorMethod[] equalityOperators = Lift(equalityOperatorsFor.Select(c => new EqualityOperatorMethod(c, false)).ToArray());
		static readonly OperatorMethod[] inequalityOperators = Lift(equalityOperatorsFor.Select(c => new EqualityOperatorMethod(c, true)).ToArray());
		#endregion
		
		#region Relational Operators
		sealed class RelationalOperatorMethod<T1, T2> : BinaryOperatorMethod
		{
			readonly Func<T1, T2, bool> func;
			
			public RelationalOperatorMethod(Func<T1, T2, bool> func)
			{
				this.ReturnType = KnownTypeReference.Boolean;
				this.Parameters.Add(MakeParameter(Type.GetTypeCode(typeof(T1))));
				this.Parameters.Add(MakeParameter(Type.GetTypeCode(typeof(T2))));
				this.func = func;
			}
			
			public override object Invoke(CSharpResolver resolver, object lhs, object rhs)
			{
				return func((T1)resolver.CSharpPrimitiveCast(Type.GetTypeCode(typeof(T1)), lhs),
				            (T2)resolver.CSharpPrimitiveCast(Type.GetTypeCode(typeof(T2)), rhs));
			}
			
			public override OperatorMethod Lift()
			{
				return new LiftedBinaryOperatorMethod(this);
			}
		}
		
		static readonly OperatorMethod[] lessThanOperators = Lift(
			new RelationalOperatorMethod<int, int>        ((a, b) => a < b),
			new RelationalOperatorMethod<uint, uint>      ((a, b) => a < b),
			new RelationalOperatorMethod<long, long>      ((a, b) => a < b),
			new RelationalOperatorMethod<ulong, ulong>    ((a, b) => a < b),
			new RelationalOperatorMethod<float, float>    ((a, b) => a < b),
			new RelationalOperatorMethod<double, double>  ((a, b) => a < b),
			new RelationalOperatorMethod<decimal, decimal>((a, b) => a < b)
		);
		
		static readonly OperatorMethod[] lessThanOrEqualOperators = Lift(
			new RelationalOperatorMethod<int, int>        ((a, b) => a <= b),
			new RelationalOperatorMethod<uint, uint>      ((a, b) => a <= b),
			new RelationalOperatorMethod<long, long>      ((a, b) => a <= b),
			new RelationalOperatorMethod<ulong, ulong>    ((a, b) => a <= b),
			new RelationalOperatorMethod<float, float>    ((a, b) => a <= b),
			new RelationalOperatorMethod<double, double>  ((a, b) => a <= b),
			new RelationalOperatorMethod<decimal, decimal>((a, b) => a <= b)
		);
		
		static readonly OperatorMethod[] greaterThanOperators = Lift(
			new RelationalOperatorMethod<int, int>        ((a, b) => a > b),
			new RelationalOperatorMethod<uint, uint>      ((a, b) => a > b),
			new RelationalOperatorMethod<long, long>      ((a, b) => a > b),
			new RelationalOperatorMethod<ulong, ulong>    ((a, b) => a > b),
			new RelationalOperatorMethod<float, float>    ((a, b) => a > b),
			new RelationalOperatorMethod<double, double>  ((a, b) => a > b),
			new RelationalOperatorMethod<decimal, decimal>((a, b) => a > b)
		);
		
		static readonly OperatorMethod[] greaterThanOrEqualOperators = Lift(
			new RelationalOperatorMethod<int, int>        ((a, b) => a >= b),
			new RelationalOperatorMethod<uint, uint>      ((a, b) => a >= b),
			new RelationalOperatorMethod<long, long>      ((a, b) => a >= b),
			new RelationalOperatorMethod<ulong, ulong>    ((a, b) => a >= b),
			new RelationalOperatorMethod<float, float>    ((a, b) => a >= b),
			new RelationalOperatorMethod<double, double>  ((a, b) => a >= b),
			new RelationalOperatorMethod<decimal, decimal>((a, b) => a >= b)
		);
		#endregion
		
		#region Bitwise operators
		static readonly OperatorMethod[] logicalAndOperator = {
			new LambdaBinaryOperatorMethod<bool, bool>  ((a, b) => a & b)
		};
		
		static readonly OperatorMethod[] bitwiseAndOperators = Lift(
			new LambdaBinaryOperatorMethod<int, int>    ((a, b) => a & b),
			new LambdaBinaryOperatorMethod<uint, uint>  ((a, b) => a & b),
			new LambdaBinaryOperatorMethod<long, long>  ((a, b) => a & b),
			new LambdaBinaryOperatorMethod<ulong, ulong>((a, b) => a & b),
			logicalAndOperator[0]
		);
		
		static readonly OperatorMethod[] logicalOrOperator = {
			new LambdaBinaryOperatorMethod<bool, bool>  ((a, b) => a | b)
		};
		
		static readonly OperatorMethod[] bitwiseOrOperators = Lift(
			new LambdaBinaryOperatorMethod<int, int>    ((a, b) => a | b),
			new LambdaBinaryOperatorMethod<uint, uint>  ((a, b) => a | b),
			new LambdaBinaryOperatorMethod<long, long>  ((a, b) => a | b),
			new LambdaBinaryOperatorMethod<ulong, ulong>((a, b) => a | b),
			logicalOrOperator[0]
		);
		// Note: the logic for the lifted bool? bitwise operators is wrong;
		// we produce "true | null" = "null" when it should be true. However, this is irrelevant
		// because bool? cannot be a compile-time type.
		
		static readonly OperatorMethod[] bitwiseXorOperators = Lift(
			new LambdaBinaryOperatorMethod<int, int>    ((a, b) => a ^ b),
			new LambdaBinaryOperatorMethod<uint, uint>  ((a, b) => a ^ b),
			new LambdaBinaryOperatorMethod<long, long>  ((a, b) => a ^ b),
			new LambdaBinaryOperatorMethod<ulong, ulong>((a, b) => a ^ b),
			new LambdaBinaryOperatorMethod<bool, bool>  ((a, b) => a ^ b)
		);
		#endregion
		
		#region Null coalescing operator
		ResolveResult ResolveNullCoalescingOperator(ResolveResult lhs, ResolveResult rhs)
		{
			Conversions conversions = new Conversions(context);
			if (NullableType.IsNullable(lhs.Type)) {
				IType a0 = NullableType.GetUnderlyingType(lhs.Type);
				if (conversions.ImplicitConversion(rhs, a0))
					return new ResolveResult(a0);
			}
			if (conversions.ImplicitConversion(rhs, lhs.Type))
				return new ResolveResult(lhs.Type);
			if (conversions.ImplicitConversion(lhs, rhs.Type))
				return new ResolveResult(rhs.Type);
			else
				return new ErrorResolveResult(lhs.Type);
		}
		#endregion
		
		object GetUserBinaryOperatorCandidates()
		{
			// C# 4.0 spec: §7.3.5 Candidate user-defined operators
			// TODO: implement user-defined operators
			throw new NotImplementedException();
		}
		#endregion
		
		#region ResolveCast
		public ResolveResult ResolveCast(IType targetType, ResolveResult expression)
		{
			cancellationToken.ThrowIfCancellationRequested();
			
			// C# 4.0 spec: §7.7.6 Cast expressions
			if (expression.IsCompileTimeConstant) {
				TypeCode code = ReflectionHelper.GetTypeCode(targetType);
				if (code >= TypeCode.Boolean && code <= TypeCode.Decimal && expression.ConstantValue != null) {
					try {
						return new ConstantResolveResult(targetType, CSharpPrimitiveCast(code, expression.ConstantValue));
					} catch (OverflowException) {
						return new ErrorResolveResult(targetType);
					}
				} else if (code == TypeCode.String) {
					if (expression.ConstantValue == null || expression.ConstantValue is string)
						return new ConstantResolveResult(targetType, expression.ConstantValue);
					else
						return new ErrorResolveResult(targetType);
				} else if (targetType.IsEnum()) {
					code = ReflectionHelper.GetTypeCode(targetType.GetEnumUnderlyingType(context));
					if (code >= TypeCode.SByte && code <= TypeCode.UInt64 && expression.ConstantValue != null) {
						try {
							return new ConstantResolveResult(targetType, CSharpPrimitiveCast(code, expression.ConstantValue));
						} catch (OverflowException) {
							return new ErrorResolveResult(targetType);
						}
					}
				}
			}
			return new ResolveResult(targetType);
		}
		
		object CSharpPrimitiveCast(TypeCode targetType, object input)
		{
			return Utils.CSharpPrimitiveCast.Cast(targetType, input, this.CheckForOverflow);
		}
		
		ResolveResult CheckErrorAndResolveCast(IType targetType, ResolveResult expression)
		{
			if (expression.IsError)
				return expression;
			else
				return ResolveCast(targetType, expression);
		}
		#endregion
		
		#region ResolveSimpleName
		enum SimpleNameLookupMode
		{
			Expression,
			InvocationTarget,
			Type,
			TypeInUsingDeclaration
		}
		
		public ResolveResult ResolveSimpleName(string identifier, IList<IType> typeArguments, bool isInvocationTarget = false)
		{
			// C# 4.0 spec: §7.6.2 Simple Names
			
			if (identifier == null)
				throw new ArgumentNullException("identifier");
			if (typeArguments == null)
				throw new ArgumentNullException("typeArguments");
			
			if (typeArguments.Count == 0) {
				foreach (IVariable v in this.LocalVariables) {
					if (v.Name == identifier) {
						object constantValue = v.IsConst ? v.ConstantValue.GetValue(context) : null;
						return new LocalResolveResult(v, v.Type.Resolve(context), constantValue);
					}
				}
				IParameterizedMember parameterizedMember = this.CurrentMember as IParameterizedMember;
				if (parameterizedMember != null) {
					foreach (IParameter p in parameterizedMember.Parameters) {
						if (p.Name == identifier) {
							return new LocalResolveResult(p, p.Type.Resolve(context));
						}
					}
				}
			}
			
			return LookupSimpleNameOrTypeName(
				identifier, typeArguments,
				isInvocationTarget ? SimpleNameLookupMode.InvocationTarget : SimpleNameLookupMode.Expression);
		}
		
		public ResolveResult LookupSimpleNamespaceOrTypeName(string identifier, IList<IType> typeArguments, bool isUsingDeclaration = false)
		{
			if (identifier == null)
				throw new ArgumentNullException("identifier");
			if (typeArguments == null)
				throw new ArgumentNullException("typeArguments");
			
			return LookupSimpleNameOrTypeName(identifier, typeArguments,
			                                  isUsingDeclaration ? SimpleNameLookupMode.TypeInUsingDeclaration : SimpleNameLookupMode.Type);
		}
		
		ResolveResult LookupSimpleNameOrTypeName(string identifier, IList<IType> typeArguments, SimpleNameLookupMode lookupMode)
		{
			// C# 4.0 spec: §3.8 Namespace and type names; §7.6.2 Simple Names
			
			cancellationToken.ThrowIfCancellationRequested();
			
			int k = typeArguments.Count;
			
			// look in type parameters of current method
			if (k == 0) {
				IMethod m = this.CurrentMember as IMethod;
				if (m != null) {
					foreach (ITypeParameter tp in m.TypeParameters) {
						if (tp.Name == identifier)
							return new TypeResolveResult(tp);
					}
				}
			}
			
			// look in current type definitions
			for (ITypeDefinition t = this.CurrentTypeDefinition; t != null; t = t.DeclaringTypeDefinition) {
				if (k == 0) {
					// look for type parameter with that name
					foreach (ITypeParameter tp in t.TypeParameters) {
						if (tp.Name == identifier)
							return new TypeResolveResult(tp);
					}
				}
				
				MemberLookup lookup = new MemberLookup(context, t, t.ProjectContent);
				ResolveResult r;
				if (lookupMode == SimpleNameLookupMode.Expression || lookupMode == SimpleNameLookupMode.InvocationTarget) {
					r = lookup.Lookup(t, identifier, typeArguments, lookupMode == SimpleNameLookupMode.InvocationTarget);
				} else {
					r = lookup.LookupType(t, identifier, typeArguments);
				}
				if (!(r is UnknownMemberResolveResult)) // but do return AmbiguousMemberResolveResult
					return r;
			}
			// look in current namespace definitions
			for (UsingScope n = this.UsingScope; n != null; n = n.Parent) {
				// first look for a namespace
				if (k == 0) {
					string fullName = NamespaceDeclaration.BuildQualifiedName(n.NamespaceName, identifier);
					if (context.GetNamespace(fullName, StringComparer.Ordinal) != null) {
						if (n.HasAlias(identifier))
							return new AmbiguousTypeResolveResult(SharedTypes.UnknownType);
						return new NamespaceResolveResult(fullName);
					}
				}
				// then look for a type
				ITypeDefinition def = context.GetClass(n.NamespaceName, identifier, k, StringComparer.Ordinal);
				if (def != null) {
					IType result = def;
					if (k != 0) {
						result = new ParameterizedType(def, typeArguments);
					}
					if (n.HasAlias(identifier))
						return new AmbiguousTypeResolveResult(result);
					else
						return new TypeResolveResult(result);
				}
				// then look for aliases:
				if (k == 0) {
					if (n.ExternAliases.Contains(identifier)) {
						return ResolveExternAlias(identifier);
					}
					if (lookupMode != SimpleNameLookupMode.TypeInUsingDeclaration || n != this.UsingScope) {
						foreach (var pair in n.UsingAliases) {
							if (pair.Key == identifier) {
								NamespaceResolveResult ns = pair.Value.ResolveNamespace(context);
								if (ns != null)
									return ns;
								else
									return new TypeResolveResult(pair.Value.Resolve(context));
							}
						}
					}
				}
				// finally, look in the imported namespaces:
				if (lookupMode != SimpleNameLookupMode.TypeInUsingDeclaration || n != this.UsingScope) {
					IType firstResult = null;
					foreach (var u in n.Usings) {
						NamespaceResolveResult ns = u.ResolveNamespace(context);
						if (ns != null) {
							def = context.GetClass(ns.NamespaceName, identifier, k, StringComparer.Ordinal);
							if (firstResult == null) {
								if (k == 0)
									firstResult = def;
								else
									firstResult = new ParameterizedType(def, typeArguments);
							} else {
								return new AmbiguousTypeResolveResult(firstResult);
							}
						}
					}
					if (firstResult != null)
						return new TypeResolveResult(firstResult);
				}
				// if we didn't find anything: repeat lookup with parent namespace
			}
			if (typeArguments.Count == 0)
				return new UnknownIdentifierResolveResult(identifier);
			else
				return ErrorResult;
		}
		
		/// <summary>
		/// Looks up an alias (identifier in front of :: operator)
		/// </summary>
		public ResolveResult ResolveAlias(string identifier)
		{
			if (identifier == "global")
				return new NamespaceResolveResult(string.Empty);
			
			for (UsingScope n = this.UsingScope; n != null; n = n.Parent) {
				if (n.ExternAliases.Contains(identifier)) {
					return ResolveExternAlias(identifier);
				}
				foreach (var pair in n.UsingAliases) {
					if (pair.Key == identifier) {
						return pair.Value.ResolveNamespace(context) ?? ErrorResult;
					}
				}
			}
			return ErrorResult;
		}
		
		ResolveResult ResolveExternAlias(string alias)
		{
			// TODO: implement extern alias support
			return new NamespaceResolveResult(string.Empty);
		}
		#endregion
		
		#region ResolveMemberAccess
		public ResolveResult ResolveMemberAccess(ResolveResult target, string identifier, IList<IType> typeArguments, bool isInvocationTarget = false)
		{
			// C# 4.0 spec: §7.6.4
			
			cancellationToken.ThrowIfCancellationRequested();
			
			NamespaceResolveResult nrr = target as NamespaceResolveResult;
			if (nrr != null) {
				if (typeArguments.Count == 0) {
					string fullName = NamespaceDeclaration.BuildQualifiedName(nrr.NamespaceName, identifier);
					if (context.GetNamespace(fullName, StringComparer.Ordinal) != null)
						return new NamespaceResolveResult(fullName);
				}
				ITypeDefinition def = context.GetClass(nrr.NamespaceName, identifier, typeArguments.Count, StringComparer.Ordinal);
				if (def != null)
					return new TypeResolveResult(def);
				return ErrorResult;
			}
			
			if (target.Type == SharedTypes.Dynamic)
				return DynamicResult;
			
			MemberLookup lookup = CreateMemberLookup();
			ResolveResult result = lookup.Lookup(target.Type, identifier, typeArguments, isInvocationTarget);
			if (result is UnknownMemberResolveResult) {
				var extensionMethods = GetExtensionMethods(target.Type, identifier, typeArguments.Count);
				if (extensionMethods.Count > 0) {
					return new MethodGroupResolveResult(target.Type, identifier, EmptyList<IMethod>.Instance, typeArguments) {
						ExtensionMethods = extensionMethods
					};
				}
			}
			return result;
		}
		
		MemberLookup CreateMemberLookup()
		{
			return new MemberLookup(context, this.CurrentTypeDefinition, this.UsingScope != null ? this.UsingScope.ProjectContent : null);
		}
		#endregion
		
		#region GetExtensionMethods
		/// <summary>
		/// Gets the extension methods that are called 'name', and can be called with 'typeArgumentCount' explicit type arguments;
		/// and are applicable with a first argument type of 'targetType'.
		/// </summary>
		List<List<IMethod>> GetExtensionMethods(IType targetType, string name, int typeArgumentCount)
		{
			List<List<IMethod>> extensionMethodGroups = new List<List<IMethod>>();
			foreach (var inputGroup in GetAllExtensionMethods()) {
				List<IMethod> outputGroup = new List<IMethod>();
				foreach (var method in inputGroup) {
					if (method.Name == name && (typeArgumentCount == 0 || method.TypeParameters.Count == typeArgumentCount)) {
						// TODO: verify targetType
						outputGroup.Add(method);
					}
				}
				if (outputGroup.Count > 0)
					extensionMethodGroups.Add(outputGroup);
			}
			return extensionMethodGroups;
		}
		
		List<List<IMethod>> GetAllExtensionMethods()
		{
			// TODO: maybe cache the result?
			List<List<IMethod>> extensionMethodGroups = new List<List<IMethod>>();
			List<IMethod> m;
			for (UsingScope scope = this.UsingScope; scope != null; scope = scope.Parent) {
				m = GetExtensionMethods(scope.NamespaceName).ToList();
				if (m.Count > 0)
					extensionMethodGroups.Add(m);
				
				m = (
					from u in scope.Usings
					select u.ResolveNamespace(context) into ns
					where ns != null
					select ns.NamespaceName
				).Distinct().SelectMany(ns => GetExtensionMethods(ns)).ToList();
				if (m.Count > 0)
					extensionMethodGroups.Add(m);
			}
			return extensionMethodGroups;
		}
		
		IEnumerable<IMethod> GetExtensionMethods(string namespaceName)
		{
			return
				from c in context.GetClasses(namespaceName, StringComparer.Ordinal)
				where c.IsStatic && c.HasExtensionMethods
				from m in c.Methods
				where m.IsExtensionMethod
				select m;
		}
		#endregion
		
		#region ResolveInvocation
		public ResolveResult ResolveInvocation(ResolveResult target, ResolveResult[] arguments, string[] argumentNames = null)
		{
			// C# 4.0 spec: §7.6.5
			
			cancellationToken.ThrowIfCancellationRequested();
			
			if (target.Type == SharedTypes.Dynamic)
				return DynamicResult;
			
			MethodGroupResolveResult mgrr = target as MethodGroupResolveResult;
			if (mgrr != null) {
				var typeArgumentArray = mgrr.TypeArguments.ToArray();
				OverloadResolution or = new OverloadResolution(context, arguments, argumentNames, typeArgumentArray);
				foreach (IMethod method in mgrr.Methods) {
					// TODO: grouping by class definition?
					or.AddCandidate(method);
				}
				if (!or.FoundApplicableCandidate) {
					// No applicable match found, so let's try extension methods.
					
					var extensionMethods = mgrr.ExtensionMethods;
					// Look in extension methods pre-calcalculated by ResolveMemberAccess if possible;
					// otherwise call GetExtensionMethods().
					if (extensionMethods == null)
						extensionMethods = GetExtensionMethods(mgrr.TargetType, mgrr.MethodName, mgrr.TypeArguments.Count);
					
					if (extensionMethods.Count > 0) {
						ResolveResult[] extArguments = new ResolveResult[arguments.Length + 1];
						extArguments[0] = new ResolveResult(mgrr.TargetType);
						arguments.CopyTo(extArguments, 1);
						string[] extArgumentNames = null;
						if (argumentNames != null) {
							extArgumentNames = new string[argumentNames.Length + 1];
							argumentNames.CopyTo(extArgumentNames, 1);
						}
						var extOr = new OverloadResolution(context, extArguments, extArgumentNames, typeArgumentArray);
						
						foreach (var g in extensionMethods) {
							foreach (var m in g) {
								extOr.AddCandidate(m);
							}
							if (extOr.FoundApplicableCandidate)
								break;
						}
						// For the lack of a better comparison function (the one within OverloadResolution
						// cannot be used as it depends on the argument set):
						if (extOr.FoundApplicableCandidate || or.BestCandidate == null) {
							// Consider an extension method result better than the normal result only
							// if it's applicable; or if there is no normal result.
							or = extOr;
						}
					}
				}
				if (or.BestCandidate != null) {
					IType returnType = or.BestCandidate.ReturnType.Resolve(context);
					returnType = returnType.AcceptVisitor(new MethodTypeParameterSubstitution(or.InferredTypeArguments));
					return new MemberResolveResult(or.BestCandidate, returnType);
				} else {
					// No candidate found at all (not even an inapplicable one).
					// This can happen with empty method groups (as sometimes used with extension methods)
					return new UnknownMethodResolveResult(
						mgrr.TargetType, mgrr.MethodName, mgrr.TypeArguments, CreateParameters(arguments, argumentNames));
				}
			}
			UnknownMemberResolveResult umrr = target as UnknownMemberResolveResult;
			if (umrr != null) {
				return new UnknownMethodResolveResult(umrr.TargetType, umrr.MemberName, umrr.TypeArguments, CreateParameters(arguments, argumentNames));
			}
			UnknownIdentifierResolveResult uirr = target as UnknownIdentifierResolveResult;
			if (uirr != null && CurrentTypeDefinition != null) {
				return new UnknownMethodResolveResult(CurrentTypeDefinition, uirr.Identifier, EmptyList<IType>.Instance, CreateParameters(arguments, argumentNames));
			}
			IMethod invokeMethod = target.Type.GetDelegateInvokeMethod();
			if (invokeMethod != null) {
				return new ResolveResult(invokeMethod.ReturnType.Resolve(context));
			}
			return ErrorResult;
		}
		
		static List<IParameter> CreateParameters(ResolveResult[] arguments, string[] argumentNames)
		{
			List<IParameter> list = new List<IParameter>();
			if (argumentNames == null) {
				argumentNames = new string[arguments.Length];
			} else {
				if (argumentNames.Length != arguments.Length)
					throw new ArgumentException();
				argumentNames = (string[])argumentNames.Clone();
			}
			for (int i = 0; i < arguments.Length; i++) {
				// invent argument names where necessary:
				if (argumentNames[i] == null) {
					string newArgumentName = GuessParameterName(arguments[i]);
					if (argumentNames.Contains(newArgumentName)) {
						// disambiguate argument name (e.g. add a number)
						int num = 1;
						string newName;
						do {
							newName = newArgumentName + num.ToString();
							num++;
						} while(argumentNames.Contains(newArgumentName));
						newArgumentName = newName;
					}
					argumentNames[i] = newArgumentName;
				}
				
				// create the parameter:
				ByReferenceResolveResult brrr = arguments[i] as ByReferenceResolveResult;
				if (brrr != null) {
					list.Add(new DefaultParameter(arguments[i].Type, argumentNames[i]) {
					         	IsRef = brrr.IsRef,
					         	IsOut = brrr.IsOut
					         });
				} else {
					// argument might be a lambda or delegate type, so we have to try to guess the delegate type
					IType type = arguments[i].Type;
					if (type == SharedTypes.Null || type == SharedTypes.UnknownType) {
						list.Add(new DefaultParameter(KnownTypeReference.Object, argumentNames[i]));
					} else {
						list.Add(new DefaultParameter(type, argumentNames[i]));
					}
				}
			}
			return list;
		}
		
		static string GuessParameterName(ResolveResult rr)
		{
			MemberResolveResult mrr = rr as MemberResolveResult;
			if (mrr != null)
				return mrr.Member.Name;
			
			UnknownMemberResolveResult umrr = rr as UnknownMemberResolveResult;
			if (umrr != null)
				return umrr.MemberName;
			
			MethodGroupResolveResult mgrr = rr as MethodGroupResolveResult;
			if (mgrr != null && mgrr.Methods.Count > 0)
				return mgrr.Methods[0].Name;
			
			LocalResolveResult vrr = rr as LocalResolveResult;
			if (vrr != null)
				return MakeParameterName(vrr.Variable.Name);
			
			if (rr.Type != SharedTypes.UnknownType && !string.IsNullOrEmpty(rr.Type.Name)) {
				return MakeParameterName(rr.Type.Name);
			} else {
				return "parameter";
			}
		}
		
		static string MakeParameterName(string variableName)
		{
			if (string.IsNullOrEmpty(variableName))
				return "parameter";
			if (variableName.Length > 1 && variableName[0] == '_')
				variableName = variableName.Substring(1);
			return char.ToLower(variableName[0]) + variableName.Substring(1);
		}
		#endregion
		
		#region ResolveIndexer
		public ResolveResult ResolveIndexer(ResolveResult target, ResolveResult[] arguments, string[] argumentNames = null)
		{
			cancellationToken.ThrowIfCancellationRequested();
			
			if (target.Type == SharedTypes.Dynamic)
				return DynamicResult;
			
			OverloadResolution or = new OverloadResolution(context, arguments, argumentNames, new IType[0]);
			MemberLookup lookup = CreateMemberLookup();
			bool allowProtectedAccess = lookup.IsProtectedAccessAllowed(target.Type);
			var indexers = target.Type.GetProperties(context, p => p.IsIndexer && lookup.IsAccessible(p, allowProtectedAccess));
			// TODO: filter indexers hiding other indexers?
			foreach (IProperty p in indexers) {
				// TODO: grouping by class definition?
				or.AddCandidate(p);
			}
			if (or.BestCandidate != null) {
				return new MemberResolveResult(or.BestCandidate, or.BestCandidate.ReturnType.Resolve(context));
			} else {
				return ErrorResult;
			}
		}
		#endregion
		
		#region ResolveObjectCreation
		public ResolveResult ResolveObjectCreation(IType type, ResolveResult[] arguments, string[] argumentNames = null)
		{
			cancellationToken.ThrowIfCancellationRequested();
			
			OverloadResolution or = new OverloadResolution(context, arguments, argumentNames, new IType[0]);
			MemberLookup lookup = CreateMemberLookup();
			bool allowProtectedAccess = lookup.IsProtectedAccessAllowed(type);
			var constructors = type.GetConstructors(context, m => lookup.IsAccessible(m, allowProtectedAccess));
			foreach (IMethod ctor in constructors) {
				or.AddCandidate(ctor);
			}
			if (or.BestCandidate != null) {
				return new MemberResolveResult(or.BestCandidate, type);
			} else {
				return new ErrorResolveResult(type);
			}
		}
		#endregion
		
		#region ResolveSizeOf
		/// <summary>
		/// Resolves 'sizeof(type)'.
		/// </summary>
		public ResolveResult ResolveSizeOf(IType type)
		{
			IType int32 = KnownTypeReference.Int32.Resolve(context);
			int size;
			switch (ReflectionHelper.GetTypeCode(type)) {
				case TypeCode.Boolean:
				case TypeCode.SByte:
				case TypeCode.Byte:
					size = 1;
					break;
				case TypeCode.Char:
				case TypeCode.Int16:
				case TypeCode.UInt16:
					size = 2;
					break;
				case TypeCode.Int32:
				case TypeCode.UInt32:
				case TypeCode.Single:
					size = 4;
					break;
				case TypeCode.Int64:
				case TypeCode.UInt64:
				case TypeCode.Double:
					size = 8;
					break;
				default:
					return new ResolveResult(int32);
			}
			return new ConstantResolveResult(int32, size);
		}
		#endregion
		
		#region Resolve This/Base Reference
		/// <summary>
		/// Resolves 'this'.
		/// </summary>
		public ResolveResult ResolveThisReference()
		{
			ITypeDefinition t = CurrentTypeDefinition;
			if (t != null) {
				return new ResolveResult(t);
			}
			return ErrorResult;
		}
		
		/// <summary>
		/// Resolves 'base'.
		/// </summary>
		public ResolveResult ResolveBaseReference()
		{
			ITypeDefinition t = CurrentTypeDefinition;
			if (t != null) {
				foreach (IType baseType in t.GetBaseTypes(context)) {
					ITypeDefinition baseTypeDef = baseType.GetDefinition();
					if (baseTypeDef != null && baseTypeDef.ClassType != ClassType.Interface) {
						return new ResolveResult(baseType);
					}
				}
			}
			return ErrorResult;
		}
		#endregion
		
		#region ResolveConditional
		public ResolveResult ResolveConditional(ResolveResult condition, ResolveResult trueExpression, ResolveResult falseExpression)
		{
			// C# 4.0 spec §7.14: Conditional operator
			
			cancellationToken.ThrowIfCancellationRequested();
			
			Conversions c = new Conversions(context);
			bool isValid;
			IType resultType;
			if (trueExpression.Type == SharedTypes.Dynamic || falseExpression.Type == SharedTypes.Dynamic) {
				resultType = SharedTypes.Dynamic;
				isValid = true;
			} else if (HasType(trueExpression) && HasType(falseExpression)) {
				bool t2f = c.ImplicitConversion(trueExpression.Type, falseExpression.Type);
				bool f2t = c.ImplicitConversion(falseExpression.Type, trueExpression.Type);
				resultType = (f2t && !t2f) ? trueExpression.Type : falseExpression.Type;
				// The operator is valid:
				// a) if there's a conversion in one direction but not the other
				// b) if there are conversions in both directions, and the types are equivalent
				isValid = (t2f != f2t) || (t2f && f2t && trueExpression.Type.Equals(falseExpression.Type));
			} else if (HasType(trueExpression)) {
				resultType = trueExpression.Type;
				isValid = c.ImplicitConversion(falseExpression, resultType);
			} else if (HasType(falseExpression)) {
				resultType = falseExpression.Type;
				isValid = c.ImplicitConversion(trueExpression, resultType);
			} else {
				return ErrorResult;
			}
			if (isValid) {
				if (condition.IsCompileTimeConstant && trueExpression.IsCompileTimeConstant && falseExpression.IsCompileTimeConstant) {
					bool? val = condition.ConstantValue as bool?;
					if (val == true)
						return ResolveCast(resultType, trueExpression);
					else if (val == false)
						return ResolveCast(resultType, falseExpression);
				}
				return new ResolveResult(resultType);
			} else {
				return new ErrorResolveResult(resultType);
			}
		}
		
		bool HasType(ResolveResult r)
		{
			return r.Type != SharedTypes.UnknownType && r.Type != SharedTypes.Null;
		}
		#endregion
		
		#region ResolvePrimitive
		public ResolveResult ResolvePrimitive(object value)
		{
			if (value == null) {
				return NullResult;
			} else {
				TypeCode typeCode = Type.GetTypeCode(value.GetType());
				IType type = typeCode.ToTypeReference().Resolve(context);
				return new ConstantResolveResult(type, value);
			}
		}
		#endregion
		
		#region ResolveDefaultValue
		public ResolveResult ResolveDefaultValue(IType type)
		{
			return new ConstantResolveResult(type, GetDefaultValue(type));
		}
		
		public static object GetDefaultValue(IType type)
		{
			switch (ReflectionHelper.GetTypeCode(type)) {
				case TypeCode.Boolean:
					return false;
				case TypeCode.Char:
					return '\0';
				case TypeCode.SByte:
					return (sbyte)0;
				case TypeCode.Byte:
					return (byte)0;
				case TypeCode.Int16:
					return (short)0;
				case TypeCode.UInt16:
					return (ushort)0;
				case TypeCode.Int32:
					return 0;
				case TypeCode.UInt32:
					return 0U;
				case TypeCode.Int64:
					return 0L;
				case TypeCode.UInt64:
					return 0UL;
				case TypeCode.Single:
					return 0f;
				case TypeCode.Double:
					return 0.0;
				case TypeCode.Decimal:
					return 0m;
				default:
					return null;
			}
		}
		#endregion
	}
}
