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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using ICSharpCode.NRefactory.CSharp.TypeSystem;
using ICSharpCode.NRefactory.Semantics;
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.TypeSystem.Implementation;
using ICSharpCode.NRefactory.Utils;

namespace ICSharpCode.NRefactory.CSharp.Resolver
{
	/// <summary>
	/// Contains the main resolver logic.
	/// </summary>
	public class CSharpResolver
	{
		static readonly ResolveResult ErrorResult = ErrorResolveResult.UnknownError;
		static readonly ResolveResult DynamicResult = new ResolveResult(SpecialType.Dynamic);
		static readonly ResolveResult NullResult = new ResolveResult(SpecialType.NullType);
		
		readonly ICompilation compilation;
		internal readonly Conversions conversions;
		readonly CSharpTypeResolveContext context;
		readonly bool checkForOverflow;
		readonly bool isWithinLambdaExpression;
		
		#region Constructor
		public CSharpResolver(ICompilation compilation)
		{
			if (compilation == null)
				throw new ArgumentNullException("compilation");
			this.compilation = compilation;
			this.conversions = Conversions.Get(compilation);
			this.context = new CSharpTypeResolveContext(compilation.MainAssembly);
		}
		
		public CSharpResolver(CSharpTypeResolveContext context)
		{
			if (context == null)
				throw new ArgumentNullException("context");
			this.compilation = context.Compilation;
			this.conversions = Conversions.Get(compilation);
			this.context = context;
			if (context.CurrentTypeDefinition != null)
				currentTypeDefinitionCache = new TypeDefinitionCache(context.CurrentTypeDefinition);
		}
		
		private CSharpResolver(ICompilation compilation, Conversions conversions, CSharpTypeResolveContext context, bool checkForOverflow, bool isWithinLambdaExpression, TypeDefinitionCache currentTypeDefinitionCache, ImmutableStack<IVariable> localVariableStack, ObjectInitializerContext objectInitializerStack)
		{
			this.compilation = compilation;
			this.conversions = conversions;
			this.context = context;
			this.checkForOverflow = checkForOverflow;
			this.isWithinLambdaExpression = isWithinLambdaExpression;
			this.currentTypeDefinitionCache = currentTypeDefinitionCache;
			this.localVariableStack = localVariableStack;
			this.objectInitializerStack = objectInitializerStack;
		}
		#endregion
		
		#region Properties
		/// <summary>
		/// Gets the compilation used by the resolver.
		/// </summary>
		public ICompilation Compilation {
			get { return compilation; }
		}
		
		/// <summary>
		/// Gets the current type resolve context.
		/// </summary>
		public CSharpTypeResolveContext CurrentTypeResolveContext {
			get { return context; }
		}
		
		CSharpResolver WithContext(CSharpTypeResolveContext newContext)
		{
			return new CSharpResolver(compilation, conversions, newContext, checkForOverflow, isWithinLambdaExpression, currentTypeDefinitionCache, localVariableStack, objectInitializerStack);
		}
		
		/// <summary>
		/// Gets whether the current context is <c>checked</c>.
		/// </summary>
		public bool CheckForOverflow {
			get { return checkForOverflow; }
		}
		
		/// <summary>
		/// Sets whether the current context is <c>checked</c>.
		/// </summary>
		public CSharpResolver WithCheckForOverflow(bool checkForOverflow)
		{
			return new CSharpResolver(compilation, conversions, context, checkForOverflow, isWithinLambdaExpression, currentTypeDefinitionCache, localVariableStack, objectInitializerStack);
		}
		
		/// <summary>
		/// Gets whether the resolver is currently within a lambda expression.
		/// </summary>
		public bool IsWithinLambdaExpression {
			get { return isWithinLambdaExpression; }
		}
		
		/// <summary>
		/// Sets whether the resolver is currently within a lambda expression.
		/// </summary>
		public CSharpResolver WithIsWithinLambdaExpression(bool isWithinLambdaExpression)
		{
			return new CSharpResolver(compilation, conversions, context, checkForOverflow, isWithinLambdaExpression, currentTypeDefinitionCache, localVariableStack, objectInitializerStack);
		}
		
		/// <summary>
		/// Gets the current member definition that is used to look up identifiers as parameters
		/// or type parameters.
		/// </summary>
		public IMember CurrentMember {
			get { return context.CurrentMember; }
		}
		
		/// <summary>
		/// Sets the current member definition.
		/// </summary>
		/// <remarks>Don't forget to also set CurrentTypeDefinition when setting CurrentMember;
		/// setting one of the properties does not automatically set the other.</remarks>
		public CSharpResolver WithCurrentMember(IMember member)
		{
			return WithContext(context.WithCurrentMember(member));
		}
		
		/// <summary>
		/// Gets the current using scope that is used to look up identifiers as class names.
		/// </summary>
		public ResolvedUsingScope CurrentUsingScope {
			get { return context.CurrentUsingScope; }
		}
		
		/// <summary>
		/// Sets the current using scope that is used to look up identifiers as class names.
		/// </summary>
		public CSharpResolver WithCurrentUsingScope(ResolvedUsingScope usingScope)
		{
			return WithContext(context.WithUsingScope(usingScope));
		}
		#endregion
		
		#region Per-CurrentTypeDefinition Cache
		readonly TypeDefinitionCache currentTypeDefinitionCache;
		
		/// <summary>
		/// Gets the current type definition.
		/// </summary>
		public ITypeDefinition CurrentTypeDefinition {
			get { return context.CurrentTypeDefinition; }
		}
		
		/// <summary>
		/// Sets the current type definition.
		/// </summary>
		public CSharpResolver WithCurrentTypeDefinition(ITypeDefinition typeDefinition)
		{
			if (this.CurrentTypeDefinition == typeDefinition)
				return this;
			
			TypeDefinitionCache newTypeDefinitionCache;
			if (typeDefinition != null)
				newTypeDefinitionCache = new TypeDefinitionCache(typeDefinition);
			else
				newTypeDefinitionCache = null;
			
			return new CSharpResolver(compilation, conversions, context.WithCurrentTypeDefinition(typeDefinition),
			                          checkForOverflow, isWithinLambdaExpression, newTypeDefinitionCache, localVariableStack, objectInitializerStack);
		}
		
		sealed class TypeDefinitionCache
		{
			public readonly ITypeDefinition TypeDefinition;
			public readonly Dictionary<string, ResolveResult> SimpleNameLookupCacheExpression = new Dictionary<string, ResolveResult>();
			public readonly Dictionary<string, ResolveResult> SimpleNameLookupCacheInvocationTarget = new Dictionary<string, ResolveResult>();
			public readonly Dictionary<string, ResolveResult> SimpleTypeLookupCache = new Dictionary<string, ResolveResult>();
			
			public TypeDefinitionCache(ITypeDefinition typeDefinition)
			{
				this.TypeDefinition = typeDefinition;
			}
		}
		#endregion
		
		#region Local Variable Management
		
		// We store the local variables in an immutable stack.
		// The beginning of a block is marked by a null entry.
		
		// This data structure is used to allow efficient cloning of the resolver with its local variable context.
		readonly ImmutableStack<IVariable> localVariableStack = ImmutableStack<IVariable>.Empty;
		
		CSharpResolver WithLocalVariableStack(ImmutableStack<IVariable> stack)
		{
			return new CSharpResolver(compilation, conversions, context, checkForOverflow, isWithinLambdaExpression, currentTypeDefinitionCache, stack, objectInitializerStack);
		}
		
		/// <summary>
		/// Opens a new scope for local variables.
		/// </summary>
		public CSharpResolver PushBlock()
		{
			return WithLocalVariableStack(localVariableStack.Push(null));
		}
		
		/// <summary>
		/// Closes the current scope for local variables; removing all variables in that scope.
		/// </summary>
		public CSharpResolver PopBlock()
		{
			var stack = localVariableStack;
			IVariable removedVar;
			do {
				removedVar = stack.Peek();
				stack = stack.Pop();
			} while (removedVar != null);
			return WithLocalVariableStack(stack);
		}
		
		/// <summary>
		/// Adds a new variable or lambda parameter to the current block.
		/// </summary>
		public CSharpResolver AddVariable(IVariable variable)
		{
			if (variable == null)
				throw new ArgumentNullException("variable");
			return WithLocalVariableStack(localVariableStack.Push(variable));
		}
		
		/// <summary>
		/// Gets all currently visible local variables and lambda parameters.
		/// </summary>
		public IEnumerable<IVariable> LocalVariables {
			get {
				return localVariableStack.Where(v => v != null);
			}
		}
		#endregion
		
		#region Object Initializer Context
		sealed class ObjectInitializerContext
		{
			internal readonly IType type;
			internal readonly ObjectInitializerContext prev;
			
			public ObjectInitializerContext(IType type, CSharpResolver.ObjectInitializerContext prev)
			{
				this.type = type;
				this.prev = prev;
			}
		}
		
		readonly ObjectInitializerContext objectInitializerStack;
		
		CSharpResolver WithObjectInitializerStack(ObjectInitializerContext stack)
		{
			return new CSharpResolver(compilation, conversions, context, checkForOverflow, isWithinLambdaExpression, currentTypeDefinitionCache, localVariableStack, stack);
		}
		
		/// <summary>
		/// Pushes the type of the object that is currently being initialized.
		/// </summary>
		public CSharpResolver PushInitializerType(IType type)
		{
			if (type == null)
				throw new ArgumentNullException("type");
			return WithObjectInitializerStack(new ObjectInitializerContext(type, objectInitializerStack));
		}
		
		public CSharpResolver PopInitializerType()
		{
			if (objectInitializerStack == null)
				throw new InvalidOperationException();
			return WithObjectInitializerStack(objectInitializerStack.prev);
		}
		
		/// <summary>
		/// Gets the type of the object currently being initialized.
		/// Returns SharedTypes.Unknown if no object initializer is currently open (or if the object initializer
		/// has unknown type).
		/// </summary>
		public IType CurrentObjectInitializerType {
			get { return objectInitializerStack != null ? objectInitializerStack.type : SpecialType.UnknownType; }
		}
		#endregion
		
		#region Clone
		/// <summary>
		/// Creates a copy of this CSharp resolver.
		/// </summary>
		[Obsolete("CSharpResolver is immutable, cloning is no longer necessary")]
		public CSharpResolver Clone()
		{
			return this;
		}
		#endregion
		
		#region ResolveUnaryOperator
		#region ResolveUnaryOperator method
		public ResolveResult ResolveUnaryOperator(UnaryOperatorType op, ResolveResult expression)
		{
			if (SpecialType.Dynamic.Equals(expression.Type))
				return UnaryOperatorResolveResult(SpecialType.Dynamic, op, expression);
			
			// C# 4.0 spec: §7.3.3 Unary operator overload resolution
			string overloadableOperatorName = GetOverloadableOperatorName(op);
			if (overloadableOperatorName == null) {
				switch (op) {
					case UnaryOperatorType.Dereference:
						PointerType p = expression.Type as PointerType;
						if (p != null)
							return UnaryOperatorResolveResult(p.ElementType, op, expression);
						else
							return ErrorResult;
					case UnaryOperatorType.AddressOf:
						return UnaryOperatorResolveResult(new PointerType(expression.Type), op, expression);
					case UnaryOperatorType.Await:
						ResolveResult getAwaiterMethodGroup = ResolveMemberAccess(expression, "GetAwaiter", EmptyList<IType>.Instance, true);
						ResolveResult getAwaiterInvocation = ResolveInvocation(getAwaiterMethodGroup, new ResolveResult[0]);
						var getResultMethodGroup = CreateMemberLookup().Lookup(getAwaiterInvocation, "GetResult", EmptyList<IType>.Instance, true) as MethodGroupResolveResult;
						if (getResultMethodGroup != null) {
							var or = getResultMethodGroup.PerformOverloadResolution(compilation, new ResolveResult[0], allowExtensionMethods: false, conversions: conversions);
							IType awaitResultType = or.GetBestCandidateWithSubstitutedTypeArguments().ReturnType;
							return UnaryOperatorResolveResult(awaitResultType, UnaryOperatorType.Await, expression);
						} else {
							return UnaryOperatorResolveResult(SpecialType.UnknownType, UnaryOperatorType.Await, expression);
						}
					default:
						throw new ArgumentException("Invalid value for UnaryOperatorType", "op");
				}
			}
			// If the type is nullable, get the underlying type:
			IType type = NullableType.GetUnderlyingType(expression.Type);
			bool isNullable = NullableType.IsNullable(expression.Type);
			
			// the operator is overloadable:
			OverloadResolution userDefinedOperatorOR = new OverloadResolution(compilation, new[] { expression }, conversions: conversions);
			foreach (var candidate in GetUserDefinedOperatorCandidates(type, overloadableOperatorName)) {
				userDefinedOperatorOR.AddCandidate(candidate);
			}
			if (userDefinedOperatorOR.FoundApplicableCandidate) {
				return CreateResolveResultForUserDefinedOperator(userDefinedOperatorOR);
			}
			
			expression = UnaryNumericPromotion(op, ref type, isNullable, expression);
			CSharpOperators.OperatorMethod[] methodGroup;
			CSharpOperators operators = CSharpOperators.Get(compilation);
			switch (op) {
				case UnaryOperatorType.Increment:
				case UnaryOperatorType.Decrement:
				case UnaryOperatorType.PostIncrement:
				case UnaryOperatorType.PostDecrement:
					// C# 4.0 spec: §7.6.9 Postfix increment and decrement operators
					// C# 4.0 spec: §7.7.5 Prefix increment and decrement operators
					TypeCode code = ReflectionHelper.GetTypeCode(type);
					if ((code >= TypeCode.SByte && code <= TypeCode.Decimal) || type.Kind == TypeKind.Enum || type.Kind == TypeKind.Pointer)
						return UnaryOperatorResolveResult(expression.Type, op, expression);
					else
						return new ErrorResolveResult(expression.Type);
				case UnaryOperatorType.Plus:
					methodGroup = operators.UnaryPlusOperators;
					break;
				case UnaryOperatorType.Minus:
					methodGroup = CheckForOverflow ? operators.CheckedUnaryMinusOperators : operators.UncheckedUnaryMinusOperators;
					break;
				case UnaryOperatorType.Not:
					methodGroup = operators.LogicalNegationOperators;
					break;
				case UnaryOperatorType.BitNot:
					if (type.Kind == TypeKind.Enum) {
						if (expression.IsCompileTimeConstant && !isNullable && expression.ConstantValue != null) {
							// evaluate as (E)(~(U)x);
							var U = compilation.FindType(expression.ConstantValue.GetType());
							var unpackedEnum = new ConstantResolveResult(U, expression.ConstantValue);
							return CheckErrorAndResolveCast(expression.Type, ResolveUnaryOperator(op, unpackedEnum));
						} else {
							return UnaryOperatorResolveResult(expression.Type, op, expression);
						}
					} else {
						methodGroup = operators.BitwiseComplementOperators;
						break;
					}
				default:
					throw new InvalidOperationException();
			}
			OverloadResolution builtinOperatorOR = new OverloadResolution(compilation, new[] { expression }, conversions: conversions);
			foreach (var candidate in methodGroup) {
				builtinOperatorOR.AddCandidate(candidate);
			}
			CSharpOperators.UnaryOperatorMethod m = (CSharpOperators.UnaryOperatorMethod)builtinOperatorOR.BestCandidate;
			IType resultType = m.ReturnType;
			if (builtinOperatorOR.BestCandidateErrors != OverloadResolutionErrors.None) {
				if (userDefinedOperatorOR.BestCandidate != null) {
					// If there are any user-defined operators, prefer those over the built-in operators.
					// It'll be a more informative error.
					return CreateResolveResultForUserDefinedOperator(userDefinedOperatorOR);
				} else if (builtinOperatorOR.BestCandidateAmbiguousWith != null) {
					// If the best candidate is ambiguous, just use the input type instead
					// of picking one of the ambiguous overloads.
					return new ErrorResolveResult(expression.Type);
				} else {
					return new ErrorResolveResult(resultType);
				}
			} else if (expression.IsCompileTimeConstant && m.CanEvaluateAtCompileTime) {
				object val;
				try {
					val = m.Invoke(this, expression.ConstantValue);
				} catch (ArithmeticException) {
					return new ErrorResolveResult(resultType);
				}
				return new ConstantResolveResult(resultType, val);
			} else {
				expression = Convert(expression, m.Parameters[0].Type, builtinOperatorOR.ArgumentConversions[0]);
				return UnaryOperatorResolveResult(resultType, op, expression);
			}
		}
		
		OperatorResolveResult UnaryOperatorResolveResult(IType resultType, UnaryOperatorType op, ResolveResult expression)
		{
			return new OperatorResolveResult(resultType, UnaryOperatorExpression.GetLinqNodeType(op, this.CheckForOverflow), expression);
		}
		#endregion
		
		#region UnaryNumericPromotion
		ResolveResult UnaryNumericPromotion(UnaryOperatorType op, ref IType type, bool isNullable, ResolveResult expression)
		{
			// C# 4.0 spec: §7.3.6.1
			TypeCode code = ReflectionHelper.GetTypeCode(type);
			if (isNullable && SpecialType.NullType.Equals(type))
				code = TypeCode.SByte; // cause promotion of null to int32
			switch (op) {
				case UnaryOperatorType.Minus:
					if (code == TypeCode.UInt32) {
						type = compilation.FindType(KnownTypeCode.Int64);
						return Convert(expression, MakeNullable(type, isNullable),
						               isNullable ? Conversion.ImplicitNullableConversion : Conversion.ImplicitNumericConversion);
					}
					goto case UnaryOperatorType.Plus;
				case UnaryOperatorType.Plus:
				case UnaryOperatorType.BitNot:
					if (code >= TypeCode.Char && code <= TypeCode.UInt16) {
						type = compilation.FindType(KnownTypeCode.Int32);
						return Convert(expression, MakeNullable(type, isNullable),
						               isNullable ? Conversion.ImplicitNullableConversion : Conversion.ImplicitNumericConversion);
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
		#endregion
		
		#region ResolveBinaryOperator
		#region ResolveBinaryOperator method
		public ResolveResult ResolveBinaryOperator(BinaryOperatorType op, ResolveResult lhs, ResolveResult rhs)
		{
			if (SpecialType.Dynamic.Equals(lhs.Type) || SpecialType.Dynamic.Equals(rhs.Type)) {
				lhs = Convert(lhs, SpecialType.Dynamic);
				rhs = Convert(rhs, SpecialType.Dynamic);
				return BinaryOperatorResolveResult(SpecialType.Dynamic, lhs, op, rhs);
			}
			
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
			
			// the operator is overloadable:
			OverloadResolution userDefinedOperatorOR = new OverloadResolution(compilation, new[] { lhs, rhs }, conversions: conversions);
			HashSet<IParameterizedMember> userOperatorCandidates = new HashSet<IParameterizedMember>();
			userOperatorCandidates.UnionWith(GetUserDefinedOperatorCandidates(lhsType, overloadableOperatorName));
			userOperatorCandidates.UnionWith(GetUserDefinedOperatorCandidates(rhsType, overloadableOperatorName));
			foreach (var candidate in userOperatorCandidates) {
				userDefinedOperatorOR.AddCandidate(candidate);
			}
			if (userDefinedOperatorOR.FoundApplicableCandidate) {
				return CreateResolveResultForUserDefinedOperator(userDefinedOperatorOR);
			}
			
			if (SpecialType.NullType.Equals(lhsType) && rhsType.IsReferenceType == false
			    || lhsType.IsReferenceType == false && SpecialType.NullType.Equals(rhsType))
			{
				isNullable = true;
			}
			if (op == BinaryOperatorType.ShiftLeft || op == BinaryOperatorType.ShiftRight) {
				// special case: the shift operators allow "var x = null << null", producing int?.
				if (SpecialType.NullType.Equals(lhsType) && SpecialType.NullType.Equals(rhsType))
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
			
			IEnumerable<CSharpOperators.OperatorMethod> methodGroup;
			CSharpOperators operators = CSharpOperators.Get(compilation);
			switch (op) {
				case BinaryOperatorType.Multiply:
					methodGroup = operators.MultiplicationOperators;
					break;
				case BinaryOperatorType.Divide:
					methodGroup = operators.DivisionOperators;
					break;
				case BinaryOperatorType.Modulus:
					methodGroup = operators.RemainderOperators;
					break;
				case BinaryOperatorType.Add:
					methodGroup = operators.AdditionOperators;
					{
						if (lhsType.Kind == TypeKind.Enum) {
							// E operator +(E x, U y);
							IType underlyingType = MakeNullable(GetEnumUnderlyingType(lhsType), isNullable);
							if (TryConvert(ref rhs, underlyingType)) {
								return HandleEnumOperator(isNullable, lhsType, op, lhs, rhs);
							}
						}
						if (rhsType.Kind == TypeKind.Enum) {
							// E operator +(U x, E y);
							IType underlyingType = MakeNullable(GetEnumUnderlyingType(rhsType), isNullable);
							if (TryConvert(ref lhs, underlyingType)) {
								return HandleEnumOperator(isNullable, rhsType, op, lhs, rhs);
							}
						}
						
						if (lhsType.Kind == TypeKind.Delegate && TryConvert(ref rhs, lhsType)) {
							return BinaryOperatorResolveResult(lhsType, lhs, op, rhs);
						} else if (rhsType.Kind == TypeKind.Delegate && TryConvert(ref lhs, rhsType)) {
							return BinaryOperatorResolveResult(rhsType, lhs, op, rhs);
						}
						
						if (lhsType is PointerType) {
							methodGroup = new [] {
								PointerArithmeticOperator(lhsType, lhsType, KnownTypeCode.Int32),
								PointerArithmeticOperator(lhsType, lhsType, KnownTypeCode.UInt32),
								PointerArithmeticOperator(lhsType, lhsType, KnownTypeCode.Int64),
								PointerArithmeticOperator(lhsType, lhsType, KnownTypeCode.UInt64)
							};
						} else if (rhsType is PointerType) {
							methodGroup = new [] {
								PointerArithmeticOperator(rhsType, KnownTypeCode.Int32, rhsType),
								PointerArithmeticOperator(rhsType, KnownTypeCode.UInt32, rhsType),
								PointerArithmeticOperator(rhsType, KnownTypeCode.Int64, rhsType),
								PointerArithmeticOperator(rhsType, KnownTypeCode.UInt64, rhsType)
							};
						}
						if (SpecialType.NullType.Equals(lhsType) && SpecialType.NullType.Equals(rhsType))
							return new ErrorResolveResult(SpecialType.NullType);
					}
					break;
				case BinaryOperatorType.Subtract:
					methodGroup = operators.SubtractionOperators;
					{
						if (lhsType.Kind == TypeKind.Enum) {
							// E operator –(E x, U y);
							IType underlyingType = MakeNullable(GetEnumUnderlyingType(lhsType), isNullable);
							if (TryConvert(ref rhs, underlyingType)) {
								return HandleEnumOperator(isNullable, lhsType, op, lhs, rhs);
							}
							// U operator –(E x, E y);
							if (TryConvert(ref rhs, lhs.Type)) {
								return HandleEnumSubtraction(isNullable, lhsType, lhs, rhs);
							}
						}
						if (rhsType.Kind == TypeKind.Enum) {
							// U operator –(E x, E y);
							if (TryConvert(ref lhs, rhs.Type)) {
								return HandleEnumSubtraction(isNullable, rhsType, lhs, rhs);
							}
						}
						
						if (lhsType.Kind == TypeKind.Delegate && TryConvert(ref rhs, lhsType)) {
							return BinaryOperatorResolveResult(lhsType, lhs, op, rhs);
						} else if (rhsType.Kind == TypeKind.Delegate && TryConvert(ref lhs, rhsType)) {
							return BinaryOperatorResolveResult(rhsType, lhs, op, rhs);
						}
						
						if (lhsType is PointerType) {
							if (rhsType is PointerType) {
								IType int64 = compilation.FindType(KnownTypeCode.Int64);
								if (lhsType.Equals(rhsType)) {
									return BinaryOperatorResolveResult(int64, lhs, op, rhs);
								} else {
									return new ErrorResolveResult(int64);
								}
							}
							methodGroup = new [] {
								PointerArithmeticOperator(lhsType, lhsType, KnownTypeCode.Int32),
								PointerArithmeticOperator(lhsType, lhsType, KnownTypeCode.UInt32),
								PointerArithmeticOperator(lhsType, lhsType, KnownTypeCode.Int64),
								PointerArithmeticOperator(lhsType, lhsType, KnownTypeCode.UInt64)
							};
						}
						
						if (SpecialType.NullType.Equals(lhsType) && SpecialType.NullType.Equals(rhsType))
							return new ErrorResolveResult(SpecialType.NullType);
					}
					break;
				case BinaryOperatorType.ShiftLeft:
					methodGroup = operators.ShiftLeftOperators;
					break;
				case BinaryOperatorType.ShiftRight:
					methodGroup = operators.ShiftRightOperators;
					break;
				case BinaryOperatorType.Equality:
				case BinaryOperatorType.InEquality:
				case BinaryOperatorType.LessThan:
				case BinaryOperatorType.GreaterThan:
				case BinaryOperatorType.LessThanOrEqual:
				case BinaryOperatorType.GreaterThanOrEqual:
					{
						if (lhsType.Kind == TypeKind.Enum && TryConvert(ref rhs, lhs.Type)) {
							// bool operator op(E x, E y);
							return HandleEnumComparison(op, lhsType, isNullable, lhs, rhs);
						} else if (rhsType.Kind == TypeKind.Enum && TryConvert(ref lhs, rhs.Type)) {
							// bool operator op(E x, E y);
							return HandleEnumComparison(op, rhsType, isNullable, lhs, rhs);
						} else if (lhsType is PointerType && rhsType is PointerType) {
							return BinaryOperatorResolveResult(compilation.FindType(KnownTypeCode.Boolean), lhs, op, rhs);
						}
						switch (op) {
							case BinaryOperatorType.Equality:
								methodGroup = operators.EqualityOperators;
								break;
							case BinaryOperatorType.InEquality:
								methodGroup = operators.InequalityOperators;
								break;
							case BinaryOperatorType.LessThan:
								methodGroup = operators.LessThanOperators;
								break;
							case BinaryOperatorType.GreaterThan:
								methodGroup = operators.GreaterThanOperators;
								break;
							case BinaryOperatorType.LessThanOrEqual:
								methodGroup = operators.LessThanOrEqualOperators;
								break;
							case BinaryOperatorType.GreaterThanOrEqual:
								methodGroup = operators.GreaterThanOrEqualOperators;
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
						if (lhsType.Kind == TypeKind.Enum && TryConvert(ref rhs, lhs.Type)) {
							// bool operator op(E x, E y);
							return HandleEnumOperator(isNullable, lhsType, op, lhs, rhs);
						} else if (rhsType.Kind == TypeKind.Enum && TryConvert(ref lhs, rhs.Type)) {
							// bool operator op(E x, E y);
							return HandleEnumOperator(isNullable, rhsType, op, lhs, rhs);
						}
						
						switch (op) {
							case BinaryOperatorType.BitwiseAnd:
								methodGroup = operators.BitwiseAndOperators;
								break;
							case BinaryOperatorType.BitwiseOr:
								methodGroup = operators.BitwiseOrOperators;
								break;
							case BinaryOperatorType.ExclusiveOr:
								methodGroup = operators.BitwiseXorOperators;
								break;
							default:
								throw new InvalidOperationException();
						}
					}
					break;
				case BinaryOperatorType.ConditionalAnd:
					methodGroup = operators.LogicalAndOperators;
					break;
				case BinaryOperatorType.ConditionalOr:
					methodGroup = operators.LogicalOrOperators;
					break;
				default:
					throw new InvalidOperationException();
			}
			OverloadResolution builtinOperatorOR = new OverloadResolution(compilation, new[] { lhs, rhs }, conversions: conversions);
			foreach (var candidate in methodGroup) {
				builtinOperatorOR.AddCandidate(candidate);
			}
			CSharpOperators.BinaryOperatorMethod m = (CSharpOperators.BinaryOperatorMethod)builtinOperatorOR.BestCandidate;
			IType resultType = m.ReturnType;
			if (builtinOperatorOR.BestCandidateErrors != OverloadResolutionErrors.None) {
				// If there are any user-defined operators, prefer those over the built-in operators.
				// It'll be a more informative error.
				if (userDefinedOperatorOR.BestCandidate != null)
					return CreateResolveResultForUserDefinedOperator(userDefinedOperatorOR);
				else
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
				lhs = Convert(lhs, m.Parameters[0].Type, builtinOperatorOR.ArgumentConversions[0]);
				rhs = Convert(rhs, m.Parameters[1].Type, builtinOperatorOR.ArgumentConversions[1]);
				return BinaryOperatorResolveResult(resultType, lhs, op, rhs);
			}
		}
		
		ResolveResult BinaryOperatorResolveResult(IType resultType, ResolveResult lhs, BinaryOperatorType op, ResolveResult rhs)
		{
			return new OperatorResolveResult(resultType, BinaryOperatorExpression.GetLinqNodeType(op, this.CheckForOverflow), lhs, rhs);
		}
		#endregion
		
		#region Pointer arithmetic
		CSharpOperators.BinaryOperatorMethod PointerArithmeticOperator(IType resultType, IType inputType1, KnownTypeCode inputType2)
		{
			return PointerArithmeticOperator(resultType, inputType1, compilation.FindType(inputType2));
		}
		
		CSharpOperators.BinaryOperatorMethod PointerArithmeticOperator(IType resultType, KnownTypeCode inputType1, IType inputType2)
		{
			return PointerArithmeticOperator(resultType, compilation.FindType(inputType1), inputType2);
		}
		
		CSharpOperators.BinaryOperatorMethod PointerArithmeticOperator(IType resultType, IType inputType1, IType inputType2)
		{
			return new CSharpOperators.BinaryOperatorMethod(compilation) {
				ReturnType = resultType,
				Parameters = {
					new DefaultParameter(inputType1, string.Empty),
					new DefaultParameter(inputType2, string.Empty)
				}
			};
		}
		#endregion
		
		#region Enum helper methods
		IType GetEnumUnderlyingType(IType enumType)
		{
			ITypeDefinition def = enumType.GetDefinition();
			return def != null ? def.EnumUnderlyingType : SpecialType.UnknownType;
		}
		
		/// <summary>
		/// Handle the case where an enum value is compared with another enum value
		/// bool operator op(E x, E y);
		/// </summary>
		ResolveResult HandleEnumComparison(BinaryOperatorType op, IType enumType, bool isNullable, ResolveResult lhs, ResolveResult rhs)
		{
			// evaluate as ((U)x op (U)y)
			IType elementType = GetEnumUnderlyingType(enumType);
			if (lhs.IsCompileTimeConstant && rhs.IsCompileTimeConstant && !isNullable) {
				lhs = ResolveCast(elementType, lhs);
				if (lhs.IsError)
					return lhs;
				rhs = ResolveCast(elementType, rhs);
				if (rhs.IsError)
					return rhs;
				return ResolveBinaryOperator(op, lhs, rhs);
			}
			IType resultType = compilation.FindType(KnownTypeCode.Boolean);
			return BinaryOperatorResolveResult(resultType, lhs, op, rhs);
		}
		
		/// <summary>
		/// Handle the case where an enum value is subtracted from another enum value
		/// U operator –(E x, E y);
		/// </summary>
		ResolveResult HandleEnumSubtraction(bool isNullable, IType enumType, ResolveResult lhs, ResolveResult rhs)
		{
			// evaluate as (U)((U)x – (U)y)
			IType elementType = GetEnumUnderlyingType(enumType);
			if (lhs.IsCompileTimeConstant && rhs.IsCompileTimeConstant && !isNullable) {
				lhs = ResolveCast(elementType, lhs);
				if (lhs.IsError)
					return lhs;
				rhs = ResolveCast(elementType, rhs);
				if (rhs.IsError)
					return rhs;
				return CheckErrorAndResolveCast(elementType, ResolveBinaryOperator(BinaryOperatorType.Subtract, lhs, rhs));
			}
			IType resultType = MakeNullable(elementType, isNullable);
			return BinaryOperatorResolveResult(resultType, lhs, BinaryOperatorType.Subtract, rhs);
		}
		
		/// <summary>
		/// Handle the following enum operators:
		/// E operator +(E x, U y);
		/// E operator +(U x, E y);
		/// E operator –(E x, U y);
		/// E operator &amp;(E x, E y);
		/// E operator |(E x, E y);
		/// E operator ^(E x, E y);
		/// </summary>
		ResolveResult HandleEnumOperator(bool isNullable, IType enumType, BinaryOperatorType op, ResolveResult lhs, ResolveResult rhs)
		{
			// evaluate as (E)((U)x op (U)y)
			if (lhs.IsCompileTimeConstant && rhs.IsCompileTimeConstant && !isNullable) {
				IType elementType = GetEnumUnderlyingType(enumType);
				lhs = ResolveCast(elementType, lhs);
				if (lhs.IsError)
					return lhs;
				rhs = ResolveCast(elementType, rhs);
				if (rhs.IsError)
					return rhs;
				return CheckErrorAndResolveCast(enumType, ResolveBinaryOperator(op, lhs, rhs));
			}
			IType resultType = MakeNullable(enumType, isNullable);
			return BinaryOperatorResolveResult(resultType, lhs, op, rhs);
		}
		
		IType MakeNullable(IType type, bool isNullable)
		{
			if (isNullable)
				return NullableType.Create(compilation, type);
			else
				return type;
		}
		#endregion
		
		#region BinaryNumericPromotion
		bool BinaryNumericPromotion(bool isNullable, ref ResolveResult lhs, ref ResolveResult rhs, bool allowNullableConstants)
		{
			// C# 4.0 spec: §7.3.6.2
			TypeCode lhsCode = ReflectionHelper.GetTypeCode(NullableType.GetUnderlyingType(lhs.Type));
			TypeCode rhsCode = ReflectionHelper.GetTypeCode(NullableType.GetUnderlyingType(rhs.Type));
			// if one of the inputs is the null literal, promote that to the type of the other operand
			if (isNullable && SpecialType.NullType.Equals(lhs.Type)) {
				lhs = CastTo(rhsCode, isNullable, lhs, allowNullableConstants);
				lhsCode = rhsCode;
			} else if (isNullable && SpecialType.NullType.Equals(rhs.Type)) {
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
		
		ResolveResult CastTo(TypeCode targetType, bool isNullable, ResolveResult expression, bool allowNullableConstants)
		{
			IType elementType = compilation.FindType(targetType);
			IType nullableType = MakeNullable(elementType, isNullable);
			if (nullableType.Equals(expression.Type))
				return expression;
			if (allowNullableConstants && expression.IsCompileTimeConstant) {
				if (expression.ConstantValue == null)
					return new ConstantResolveResult(nullableType, null);
				ResolveResult rr = ResolveCast(elementType, expression);
				if (rr.IsError)
					return rr;
				Debug.Assert(rr.IsCompileTimeConstant);
				return new ConstantResolveResult(nullableType, rr.ConstantValue);
			} else {
				return Convert(expression, nullableType,
				               isNullable ? Conversion.ImplicitNullableConversion : Conversion.ImplicitNumericConversion);
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
		
		#region Null coalescing operator
		ResolveResult ResolveNullCoalescingOperator(ResolveResult lhs, ResolveResult rhs)
		{
			if (NullableType.IsNullable(lhs.Type)) {
				IType a0 = NullableType.GetUnderlyingType(lhs.Type);
				if (TryConvert(ref rhs, a0)) {
					return BinaryOperatorResolveResult(a0, lhs, BinaryOperatorType.NullCoalescing, rhs);
				}
			}
			if (TryConvert(ref rhs, lhs.Type)) {
				return BinaryOperatorResolveResult(lhs.Type, lhs, BinaryOperatorType.NullCoalescing, rhs);
			}
			if (TryConvert(ref lhs, rhs.Type)) {
				return BinaryOperatorResolveResult(rhs.Type, lhs, BinaryOperatorType.NullCoalescing, rhs);
			} else {
				return new ErrorResolveResult(lhs.Type);
			}
		}
		#endregion
		#endregion
		
		#region Get user-defined operator candidates
		IEnumerable<IParameterizedMember> GetUserDefinedOperatorCandidates(IType type, string operatorName)
		{
			if (operatorName == null)
				return EmptyList<IMethod>.Instance;
			TypeCode c = ReflectionHelper.GetTypeCode(type);
			if (TypeCode.Boolean <= c && c <= TypeCode.Decimal || c == TypeCode.String) {
				// The .NET framework contains some of C#'s built-in operators as user-defined operators.
				// However, we must not use those as user-defined operators (we would skip numeric promotion).
				return EmptyList<IMethod>.Instance;
			}
			// C# 4.0 spec: §7.3.5 Candidate user-defined operators
			var operators = type.GetMethods(m => m.IsOperator && m.Name == operatorName).ToList();
			LiftUserDefinedOperators(operators);
			return operators;
		}
		
		void LiftUserDefinedOperators(List<IMethod> operators)
		{
			int nonLiftedMethodCount = operators.Count;
			// Construct lifted operators
			for (int i = 0; i < nonLiftedMethodCount; i++) {
				var liftedMethod = LiftUserDefinedOperator(operators[i]);
				if (liftedMethod != null)
					operators.Add(liftedMethod);
			}
		}
		
		LiftedUserDefinedOperator LiftUserDefinedOperator(IMethod m)
		{
			IType returnType = m.ReturnType;
			if (!NullableType.IsNonNullableValueType(returnType))
				return null; // cannot lift this operator
			for (int i = 0; i < m.Parameters.Count; i++) {
				if (!NullableType.IsNonNullableValueType(m.Parameters[i].Type))
					return null; // cannot lift this operator
			}
			return new LiftedUserDefinedOperator(m);
		}
		
		sealed class LiftedUserDefinedOperator : SpecializedMethod, OverloadResolution.ILiftedOperator
		{
			internal readonly IParameterizedMember nonLiftedOperator;
			
			public LiftedUserDefinedOperator(IMethod nonLiftedMethod)
				: base(nonLiftedMethod.DeclaringType, (IMethod)nonLiftedMethod.MemberDefinition,
				       EmptyList<IType>.Instance, new MakeNullableVisitor(nonLiftedMethod.Compilation))
			{
				this.nonLiftedOperator = nonLiftedMethod;
			}
			
			public IList<IParameter> NonLiftedParameters {
				get { return nonLiftedOperator.Parameters; }
			}
			
			public override bool Equals(object obj)
			{
				LiftedUserDefinedOperator op = obj as LiftedUserDefinedOperator;
				return op != null && this.nonLiftedOperator.Equals(op.nonLiftedOperator);
			}
			
			public override int GetHashCode()
			{
				return nonLiftedOperator.GetHashCode() ^ 0x7191254;
			}
		}
		
		sealed class MakeNullableVisitor : TypeVisitor
		{
			readonly ICompilation compilation;
			
			public MakeNullableVisitor(ICompilation compilation)
			{
				this.compilation = compilation;
			}
			
			public override IType VisitTypeDefinition(ITypeDefinition type)
			{
				return NullableType.Create(compilation, type);
			}
			
			public override IType VisitTypeParameter(ITypeParameter type)
			{
				return NullableType.Create(compilation, type);
			}
			
			public override IType VisitParameterizedType(ParameterizedType type)
			{
				return NullableType.Create(compilation, type);
			}
			
			public override IType VisitOtherType(IType type)
			{
				return NullableType.Create(compilation, type);
			}
		}
		
		ResolveResult CreateResolveResultForUserDefinedOperator(OverloadResolution r)
		{
			return r.CreateResolveResult(null);
		}
		#endregion
		
		#region ResolveCast
		bool TryConvert(ref ResolveResult rr, IType targetType)
		{
			Conversion c = conversions.ImplicitConversion(rr, targetType);
			if (c) {
				rr = Convert(rr, targetType, c);
				return true;
			} else {
				return false;
			}
		}
		
		ResolveResult Convert(ResolveResult rr, IType targetType)
		{
			return Convert(rr, targetType, conversions.ImplicitConversion(rr, targetType));
		}
		
		ResolveResult Convert(ResolveResult rr, IType targetType, Conversion c)
		{
			if (c == Conversion.IdentityConversion)
				return rr;
			else if (rr.IsCompileTimeConstant && c != Conversion.None)
				return ResolveCast(targetType, rr);
			else
				return new ConversionResolveResult(targetType, rr, c);
		}
		
		public ResolveResult ResolveCast(IType targetType, ResolveResult expression)
		{
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
				} else if (targetType.Kind == TypeKind.Enum) {
					code = ReflectionHelper.GetTypeCode(GetEnumUnderlyingType(targetType));
					if (code >= TypeCode.SByte && code <= TypeCode.UInt64 && expression.ConstantValue != null) {
						try {
							return new ConstantResolveResult(targetType, CSharpPrimitiveCast(code, expression.ConstantValue));
						} catch (OverflowException) {
							return new ErrorResolveResult(targetType);
						}
					}
				}
			}
			Conversion c = conversions.ExplicitConversion(expression, targetType);
			if (c) {
				return new ConversionResolveResult(targetType, expression, c);
			} else {
				return new ErrorResolveResult(targetType);
			}
		}
		
		internal object CSharpPrimitiveCast(TypeCode targetType, object input)
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
		public ResolveResult ResolveSimpleName(string identifier, IList<IType> typeArguments, bool isInvocationTarget = false)
		{
			// C# 4.0 spec: §7.6.2 Simple Names
			
			return LookupSimpleNameOrTypeName(
				identifier, typeArguments,
				isInvocationTarget ? SimpleNameLookupMode.InvocationTarget : SimpleNameLookupMode.Expression);
		}
		
		public ResolveResult LookupSimpleNameOrTypeName(string identifier, IList<IType> typeArguments, SimpleNameLookupMode lookupMode)
		{
			// C# 4.0 spec: §3.8 Namespace and type names; §7.6.2 Simple Names
			
			if (identifier == null)
				throw new ArgumentNullException("identifier");
			if (typeArguments == null)
				throw new ArgumentNullException("typeArguments");
			
			int k = typeArguments.Count;
			
			if (k == 0) {
				if (lookupMode == SimpleNameLookupMode.Expression || lookupMode == SimpleNameLookupMode.InvocationTarget) {
					// Look in local variables
					foreach (IVariable v in this.LocalVariables) {
						if (v.Name == identifier) {
							return new LocalResolveResult(v);
						}
					}
					// Look in parameters of current method
					IParameterizedMember parameterizedMember = this.CurrentMember as IParameterizedMember;
					if (parameterizedMember != null) {
						foreach (IParameter p in parameterizedMember.Parameters) {
							if (p.Name == identifier) {
								return new LocalResolveResult(p);
							}
						}
					}
				}
				
				// look in type parameters of current method
				IMethod m = this.CurrentMember as IMethod;
				if (m != null) {
					foreach (ITypeParameter tp in m.TypeParameters) {
						if (tp.Name == identifier)
							return new TypeResolveResult(tp);
					}
				}
			}
			
			bool parameterizeResultType = !(typeArguments.Count != 0 && typeArguments.All(t => t.Kind == TypeKind.UnboundTypeArgument));
			
			ResolveResult r = null;
			if (currentTypeDefinitionCache != null) {
				Dictionary<string, ResolveResult> cache = null;
				bool foundInCache = false;
				if (k == 0) {
					switch (lookupMode) {
						case SimpleNameLookupMode.Expression:
							cache = currentTypeDefinitionCache.SimpleNameLookupCacheExpression;
							break;
						case SimpleNameLookupMode.InvocationTarget:
							cache = currentTypeDefinitionCache.SimpleNameLookupCacheInvocationTarget;
							break;
						case SimpleNameLookupMode.Type:
							cache = currentTypeDefinitionCache.SimpleTypeLookupCache;
							break;
					}
					if (cache != null) {
						foundInCache = cache.TryGetValue(identifier, out r);
					}
				}
				if (!foundInCache) {
					r = LookInCurrentType(identifier, typeArguments, lookupMode, parameterizeResultType);
					if (cache != null) {
						// also cache missing members (r==null)
						cache[identifier] = r;
					}
				}
				if (r != null)
					return r;
			}
			
			if (context.CurrentUsingScope != null) {
				if (k == 0 && lookupMode != SimpleNameLookupMode.TypeInUsingDeclaration) {
					if (!context.CurrentUsingScope.ResolveCache.TryGetValue(identifier, out r)) {
						r = LookInCurrentUsingScope(identifier, typeArguments, false, false);
						r = context.CurrentUsingScope.ResolveCache.GetOrAdd(identifier, r);
					}
				} else {
					r = LookInCurrentUsingScope(identifier, typeArguments, lookupMode == SimpleNameLookupMode.TypeInUsingDeclaration, parameterizeResultType);
				}
				if (r != null)
					return r;
			}
			
			if (typeArguments.Count == 0 && identifier == "dynamic") {
				return new TypeResolveResult(SpecialType.Dynamic);
			} else {
				return new UnknownIdentifierResolveResult(identifier, typeArguments.Count);
			}
		}
		
		ResolveResult LookInCurrentType(string identifier, IList<IType> typeArguments, SimpleNameLookupMode lookupMode, bool parameterizeResultType)
		{
			int k = typeArguments.Count;
			MemberLookup lookup;
			if (lookupMode == SimpleNameLookupMode.BaseTypeReference && this.CurrentTypeDefinition != null) {
				// When looking up a base type reference, treat us as being outside the current type definition
				// for accessibility purposes.
				// This avoids a stack overflow when referencing a protected class nested inside the base class
				// of a parent class. (NameLookupTests.InnerClassInheritingFromProtectedBaseInnerClassShouldNotCauseStackOverflow)
				lookup = new MemberLookup(this.CurrentTypeDefinition.DeclaringTypeDefinition, this.Compilation.MainAssembly, false);
			} else {
				lookup = CreateMemberLookup();
			}
			// look in current type definitions
			for (ITypeDefinition t = this.CurrentTypeDefinition; t != null; t = t.DeclaringTypeDefinition) {
				if (k == 0) {
					// look for type parameter with that name
					var typeParameters = t.TypeParameters;
					// only look at type parameters defined directly on this type, not at those copied from outer classes
					for (int i = (t.DeclaringTypeDefinition != null ? t.DeclaringTypeDefinition.TypeParameterCount : 0); i < typeParameters.Count; i++) {
						if (typeParameters[i].Name == identifier)
							return new TypeResolveResult(typeParameters[i]);
					}
				}
				
				if (lookupMode == SimpleNameLookupMode.BaseTypeReference && t == this.CurrentTypeDefinition) {
					// don't look in current type when resolving a base type reference
					continue;
				}
				
				ResolveResult r;
				if (lookupMode == SimpleNameLookupMode.Expression || lookupMode == SimpleNameLookupMode.InvocationTarget) {
					r = lookup.Lookup(new TypeResolveResult(t), identifier, typeArguments, lookupMode == SimpleNameLookupMode.InvocationTarget);
				} else {
					r = lookup.LookupType(t, identifier, typeArguments, parameterizeResultType);
				}
				if (!(r is UnknownMemberResolveResult)) // but do return AmbiguousMemberResolveResult
					return r;
			}
			return null;
		}
		
		ResolveResult LookInCurrentUsingScope(string identifier, IList<IType> typeArguments, bool isInUsingDeclaration, bool parameterizeResultType)
		{
			int k = typeArguments.Count;
			// look in current namespace definitions
			ResolvedUsingScope currentUsingScope = this.CurrentUsingScope;
			for (ResolvedUsingScope u = currentUsingScope; u != null; u = u.Parent) {
				INamespace n = u.Namespace;
				// first look for a namespace
				if (k == 0 && n != null) {
					INamespace childNamespace = n.GetChildNamespace(identifier);
					if (childNamespace != null) {
						if (u.HasAlias(identifier))
							return new AmbiguousTypeResolveResult(new UnknownType(null, identifier));
						return new NamespaceResolveResult(childNamespace);
					}
				}
				// then look for a type
				if (n != null) {
					ITypeDefinition def = n.GetTypeDefinition(identifier, k);
					if (def != null) {
						IType result = def;
						if (parameterizeResultType && k > 0) {
							result = new ParameterizedType(def, typeArguments);
						}
						if (u.HasAlias(identifier))
							return new AmbiguousTypeResolveResult(result);
						else
							return new TypeResolveResult(result);
					}
				}
				// then look for aliases:
				if (k == 0) {
					if (u.ExternAliases.Contains(identifier)) {
						return ResolveExternAlias(identifier);
					}
					if (!(isInUsingDeclaration && u == currentUsingScope)) {
						foreach (var pair in u.UsingAliases) {
							if (pair.Key == identifier) {
								return pair.Value;
							}
						}
					}
				}
				// finally, look in the imported namespaces:
				if (!(isInUsingDeclaration && u == currentUsingScope)) {
					IType firstResult = null;
					foreach (var importedNamespace in u.Usings) {
						ITypeDefinition def = importedNamespace.GetTypeDefinition(identifier, k);
						if (def != null) {
							if (firstResult == null) {
								if (parameterizeResultType && k > 0)
									firstResult = new ParameterizedType(def, typeArguments);
								else
									firstResult = def;
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
			return null;
		}
		
		/// <summary>
		/// Looks up an alias (identifier in front of :: operator)
		/// </summary>
		public ResolveResult ResolveAlias(string identifier)
		{
			if (identifier == "global")
				return new NamespaceResolveResult(compilation.RootNamespace);
			
			for (ResolvedUsingScope n = this.CurrentUsingScope; n != null; n = n.Parent) {
				if (n.ExternAliases.Contains(identifier)) {
					return ResolveExternAlias(identifier);
				}
				foreach (var pair in n.UsingAliases) {
					if (pair.Key == identifier) {
						return (pair.Value as NamespaceResolveResult) ?? ErrorResult;
					}
				}
			}
			return ErrorResult;
		}
		
		ResolveResult ResolveExternAlias(string alias)
		{
			INamespace ns = compilation.GetNamespaceForExternAlias(alias);
			if (ns != null)
				return new NamespaceResolveResult(ns);
			else
				return ErrorResult;
		}
		#endregion
		
		#region ResolveMemberAccess
		public ResolveResult ResolveMemberAccess(ResolveResult target, string identifier, IList<IType> typeArguments, bool isInvocationTarget = false)
		{
			// C# 4.0 spec: §7.6.4
			
			NamespaceResolveResult nrr = target as NamespaceResolveResult;
			if (nrr != null) {
				return ResolveMemberAccessOnNamespace(nrr, identifier, typeArguments, typeArguments.Count > 0);
			}
			
			if (SpecialType.Dynamic.Equals(target.Type))
				return DynamicResult;
			
			MemberLookup lookup = CreateMemberLookup();
			ResolveResult result = lookup.Lookup(target, identifier, typeArguments, isInvocationTarget);
			if (result is UnknownMemberResolveResult) {
				var extensionMethods = GetExtensionMethods(target.Type, identifier, typeArguments);
				if (extensionMethods.Count > 0) {
					return new MethodGroupResolveResult(target, identifier, EmptyList<MethodListWithDeclaringType>.Instance, typeArguments) {
						extensionMethods = extensionMethods
					};
				}
			} else {
				MethodGroupResolveResult mgrr = result as MethodGroupResolveResult;
				if (mgrr != null) {
					Debug.Assert(mgrr.extensionMethods == null);
					// set the values that are necessary to make MethodGroupResolveResult.GetExtensionMethods() work
					mgrr.resolver = this;
				}
			}
			return result;
		}
		
		public ResolveResult ResolveMemberType(ResolveResult target, string identifier, IList<IType> typeArguments)
		{
			bool parameterizeResultType = !(typeArguments.Count != 0 && typeArguments.All(t => t.Kind == TypeKind.UnboundTypeArgument));
			
			NamespaceResolveResult nrr = target as NamespaceResolveResult;
			if (nrr != null) {
				return ResolveMemberAccessOnNamespace(nrr, identifier, typeArguments, parameterizeResultType);
			}
			
			MemberLookup lookup = CreateMemberLookup();
			return lookup.LookupType(target.Type, identifier, typeArguments, parameterizeResultType);
		}
		
		ResolveResult ResolveMemberAccessOnNamespace(NamespaceResolveResult nrr, string identifier, IList<IType> typeArguments, bool parameterizeResultType)
		{
			if (typeArguments.Count == 0) {
				INamespace childNamespace = nrr.Namespace.GetChildNamespace(identifier);
				if (childNamespace != null)
					return new NamespaceResolveResult(childNamespace);
			}
			ITypeDefinition def = nrr.Namespace.GetTypeDefinition(identifier, typeArguments.Count);
			if (def != null) {
				if (parameterizeResultType && typeArguments.Count > 0)
					return new TypeResolveResult(new ParameterizedType(def, typeArguments));
				else
					return new TypeResolveResult(def);
			}
			return ErrorResult;
		}
		
		/// <summary>
		/// Creates a MemberLookup instance using this resolver's settings.
		/// </summary>
		public MemberLookup CreateMemberLookup()
		{
			ITypeDefinition currentTypeDefinition = this.CurrentTypeDefinition;
			bool isInEnumMemberInitializer = this.CurrentMember != null && this.CurrentMember.EntityType == EntityType.Field
				&& currentTypeDefinition != null && currentTypeDefinition.Kind == TypeKind.Enum;
			return new MemberLookup(currentTypeDefinition, this.Compilation.MainAssembly, isInEnumMemberInitializer);
		}
		#endregion
		
		#region ResolveIdentifierInObjectInitializer
		public ResolveResult ResolveIdentifierInObjectInitializer(string identifier)
		{
			MemberLookup memberLookup = CreateMemberLookup();
			ResolveResult target = new ResolveResult(this.CurrentObjectInitializerType);
			return memberLookup.Lookup(target, identifier, EmptyList<IType>.Instance, false);
		}
		#endregion
		
		#region GetExtensionMethods
		/// <summary>
		/// Gets the extension methods that are called 'name'
		/// and are applicable with a first argument type of 'targetType'.
		/// </summary>
		/// <param name="targetType">Type of the 'this' argument</param>
		/// <param name="name">Name of the extension method</param>
		/// <param name="typeArguments">Explicitly provided type arguments.
		/// An empty list will return all matching extension method definitions;
		/// a non-empty list will return <see cref="SpecializedMethod"/>s for all extension methods
		/// with the matching number of type parameters.</param>
		/// <remarks>
		/// The results are stored in nested lists because they are grouped by using scope.
		/// That is, for "using SomeExtensions; namespace X { using MoreExtensions; ... }",
		/// the return value will be
		/// new List {
		///    new List { all extensions from MoreExtensions },
		///    new List { all extensions from SomeExtensions }
		/// }
		/// </remarks>
		public List<List<IMethod>> GetExtensionMethods(IType targetType, string name, IList<IType> typeArguments = null)
		{
			List<List<IMethod>> extensionMethodGroups = new List<List<IMethod>>();
			foreach (var inputGroup in GetAllExtensionMethods()) {
				List<IMethod> outputGroup = new List<IMethod>();
				foreach (var method in inputGroup) {
					if (method.Name != name)
						continue;
					
					if (typeArguments != null && typeArguments.Count > 0) {
						if (method.TypeParameters.Count != typeArguments.Count)
							continue;
						SpecializedMethod sm = new SpecializedMethod(method.DeclaringType, method, typeArguments);
						// TODO: verify targetType
						outputGroup.Add(sm);
					} else {
						// TODO: verify targetType
						outputGroup.Add(method);
					}
				}
				if (outputGroup.Count > 0)
					extensionMethodGroups.Add(outputGroup);
			}
			return extensionMethodGroups;
		}
		
		/// <summary>
		/// Gets all extension methods available in the current using scope.
		/// This list includes unaccessible
		/// </summary>
		IList<List<IMethod>> GetAllExtensionMethods()
		{
			var currentUsingScope = context.CurrentUsingScope;
			if (currentUsingScope == null)
				return EmptyList<List<IMethod>>.Instance;
			List<List<IMethod>> extensionMethodGroups = currentUsingScope.AllExtensionMethods;
			if (extensionMethodGroups != null) {
				LazyInit.ReadBarrier();
				return extensionMethodGroups;
			}
			extensionMethodGroups = new List<List<IMethod>>();
			List<IMethod> m;
			for (ResolvedUsingScope scope = currentUsingScope; scope != null; scope = scope.Parent) {
				INamespace ns = scope.Namespace;
				if (ns != null) {
					m = GetExtensionMethods(ns).ToList();
					if (m.Count > 0)
						extensionMethodGroups.Add(m);
				}
				
				m = scope.Usings
					.Distinct()
					.SelectMany(importedNamespace => GetExtensionMethods(importedNamespace))
					.ToList();
				if (m.Count > 0)
					extensionMethodGroups.Add(m);
			}
			return LazyInit.GetOrSet(ref currentUsingScope.AllExtensionMethods, extensionMethodGroups);
		}
		
		IEnumerable<IMethod> GetExtensionMethods(INamespace ns)
		{
			// TODO: maybe make this a property on INamespace?
			return
				from c in ns.Types
				where c.IsStatic && c.HasExtensionMethods && c.TypeParameters.Count == 0
				from m in c.Methods
				where m.IsExtensionMethod
				select m;
		}
		#endregion
		
		#region ResolveInvocation
		/// <summary>
		/// Resolves an invocation.
		/// </summary>
		/// <param name="target">The target of the invocation. Usually a MethodGroupResolveResult.</param>
		/// <param name="arguments">
		/// Arguments passed to the method.
		/// The resolver may mutate this array to wrap elements in <see cref="ConversionResolveResult"/>s!
		/// </param>
		/// <param name="argumentNames">
		/// The argument names. Pass the null string for positional arguments.
		/// </param>
		/// <returns>InvocationResolveResult or UnknownMethodResolveResult</returns>
		public ResolveResult ResolveInvocation(ResolveResult target, ResolveResult[] arguments, string[] argumentNames = null)
		{
			// C# 4.0 spec: §7.6.5
			
			if (SpecialType.Dynamic.Equals(target.Type))
				return DynamicResult;
			
			MethodGroupResolveResult mgrr = target as MethodGroupResolveResult;
			if (mgrr != null) {
				OverloadResolution or = mgrr.PerformOverloadResolution(compilation, arguments, argumentNames, conversions: conversions);
				if (or.BestCandidate != null) {
					return or.CreateResolveResult(mgrr.TargetResult);
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
				OverloadResolution or = new OverloadResolution(compilation, arguments, argumentNames, conversions: conversions);
				or.AddCandidate(invokeMethod);
				return new CSharpInvocationResolveResult(
					target, invokeMethod, //invokeMethod.ReturnType.Resolve(context),
					or.GetArgumentsWithConversions(), or.BestCandidateErrors,
					isExpandedForm: or.BestCandidateIsExpandedForm,
					isDelegateInvocation: true,
					argumentToParameterMap: or.GetArgumentToParameterMap());
			}
			return ErrorResult;
		}
		
		List<IParameter> CreateParameters(ResolveResult[] arguments, string[] argumentNames)
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
						} while(argumentNames.Contains(newName));
						newArgumentName = newName;
					}
					argumentNames[i] = newArgumentName;
				}
				
				// create the parameter:
				ByReferenceResolveResult brrr = arguments[i] as ByReferenceResolveResult;
				if (brrr != null) {
					list.Add(new DefaultParameter(arguments[i].Type, argumentNames[i], isRef: brrr.IsRef, isOut: brrr.IsOut));
				} else {
					// argument might be a lambda or delegate type, so we have to try to guess the delegate type
					IType type = arguments[i].Type;
					if (type.Kind == TypeKind.Null || type.Kind == TypeKind.Unknown) {
						list.Add(new DefaultParameter(compilation.FindType(KnownTypeCode.Object), argumentNames[i]));
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
			if (mgrr != null)
				return mgrr.MethodName;
			
			LocalResolveResult vrr = rr as LocalResolveResult;
			if (vrr != null)
				return MakeParameterName(vrr.Variable.Name);
			
			if (rr.Type.Kind != TypeKind.Unknown && !string.IsNullOrEmpty(rr.Type.Name)) {
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
		/// <summary>
		/// Resolves an indexer access.
		/// </summary>
		/// <param name="target">Target expression.</param>
		/// <param name="arguments">
		/// Arguments passed to the indexer.
		/// The resolver may mutate this array to wrap elements in <see cref="ConversionResolveResult"/>s!
		/// </param>
		/// <param name="argumentNames">
		/// The argument names. Pass the null string for positional arguments.
		/// </param>
		/// <returns>ArrayAccessResolveResult, InvocationResolveResult, or ErrorResolveResult</returns>
		public ResolveResult ResolveIndexer(ResolveResult target, ResolveResult[] arguments, string[] argumentNames = null)
		{
			switch (target.Type.Kind) {
				case TypeKind.Dynamic:
					for (int i = 0; i < arguments.Length; i++) {
						arguments[i] = Convert(arguments[i], SpecialType.Dynamic);
					}
					return new ArrayAccessResolveResult(SpecialType.Dynamic, target, arguments);
					
				case TypeKind.Array:
				case TypeKind.Pointer:
					// §7.6.6.1 Array access / §18.5.3 Pointer element access
					AdjustArrayAccessArguments(arguments);
					return new ArrayAccessResolveResult(((TypeWithElementType)target.Type).ElementType, target, arguments);
			}
			
			// §7.6.6.2 Indexer access
			OverloadResolution or = new OverloadResolution(compilation, arguments, argumentNames, conversions: conversions);
			MemberLookup lookup = CreateMemberLookup();
			var indexers = lookup.LookupIndexers(target.Type);
			or.AddMethodLists(indexers);
			if (or.BestCandidate != null) {
				return or.CreateResolveResult(target);
			} else {
				return ErrorResult;
			}
		}
		
		/// <summary>
		/// Converts all arguments to int,uint,long or ulong.
		/// </summary>
		void AdjustArrayAccessArguments(ResolveResult[] arguments)
		{
			for (int i = 0; i < arguments.Length; i++) {
				if (!(TryConvert(ref arguments[i], compilation.FindType(KnownTypeCode.Int32)) ||
				      TryConvert(ref arguments[i], compilation.FindType(KnownTypeCode.UInt32)) ||
				      TryConvert(ref arguments[i], compilation.FindType(KnownTypeCode.Int64)) ||
				      TryConvert(ref arguments[i], compilation.FindType(KnownTypeCode.UInt64))))
				{
					// conversion failed
					arguments[i] = Convert(arguments[i], compilation.FindType(KnownTypeCode.Int32), Conversion.None);
				}
			}
		}
		#endregion
		
		#region ResolveObjectCreation
		/// <summary>
		/// Resolves an object creation.
		/// </summary>
		/// <param name="type">Type of the object to create.</param>
		/// <param name="arguments">
		/// Arguments passed to the constructor.
		/// The resolver may mutate this array to wrap elements in <see cref="ConversionResolveResult"/>s!
		/// </param>
		/// <param name="argumentNames">
		/// The argument names. Pass the null string for positional arguments.
		/// </param>
		/// <param name="allowProtectedAccess">
		/// Whether to allow calling protected constructors.
		/// This should be false except when resolving constructor initializers.
		/// </param>
		/// <returns>InvocationResolveResult or ErrorResolveResult</returns>
		public ResolveResult ResolveObjectCreation(IType type, ResolveResult[] arguments, string[] argumentNames = null, bool allowProtectedAccess = false)
		{
			if (type.Kind == TypeKind.Delegate && arguments.Length == 1) {
				return Convert(arguments[0], type);
			}
			OverloadResolution or = new OverloadResolution(compilation, arguments, argumentNames, conversions: conversions);
			MemberLookup lookup = CreateMemberLookup();
			foreach (IMethod ctor in type.GetConstructors()) {
				if (lookup.IsAccessible(ctor, allowProtectedAccess))
					or.AddCandidate(ctor);
				else
					or.AddCandidate(ctor, OverloadResolutionErrors.Inaccessible);
			}
			if (or.BestCandidate != null) {
				return or.CreateResolveResult(null);
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
			IType int32 = compilation.FindType(KnownTypeCode.Int32);
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
				if (t.TypeParameterCount != 0) {
					// Self-parameterize the type
					return new ThisResolveResult(new ParameterizedType(t, t.TypeParameters));
				} else {
					return new ThisResolveResult(t);
				}
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
				foreach (IType baseType in t.DirectBaseTypes) {
					if (baseType.Kind != TypeKind.Unknown && baseType.Kind != TypeKind.Interface) {
						return new ThisResolveResult(baseType);
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
			
			bool isValid;
			IType resultType;
			if (SpecialType.Dynamic.Equals(trueExpression.Type) || SpecialType.Dynamic.Equals(falseExpression.Type)) {
				resultType = SpecialType.Dynamic;
				isValid = TryConvert(ref trueExpression, resultType) & TryConvert(ref falseExpression, resultType);
			} else if (HasType(trueExpression) && HasType(falseExpression)) {
				Conversion t2f = conversions.ImplicitConversion(trueExpression, falseExpression.Type);
				Conversion f2t = conversions.ImplicitConversion(falseExpression, trueExpression.Type);
				// The operator is valid:
				// a) if there's a conversion in one direction but not the other
				// b) if there are conversions in both directions, and the types are equivalent
				if (t2f && !f2t) {
					resultType = falseExpression.Type;
					isValid = true;
					trueExpression = Convert(trueExpression, resultType, t2f);
				} else if (f2t && !t2f) {
					resultType = trueExpression.Type;
					isValid = true;
					falseExpression = Convert(falseExpression, resultType, f2t);
				} else {
					resultType = trueExpression.Type;
					isValid = trueExpression.Type.Equals(falseExpression.Type);
				}
			} else if (HasType(trueExpression)) {
				resultType = trueExpression.Type;
				isValid = TryConvert(ref falseExpression, resultType);
			} else if (HasType(falseExpression)) {
				resultType = falseExpression.Type;
				isValid = TryConvert(ref trueExpression, resultType);
			} else {
				return ErrorResult;
			}
			isValid &= TryConvert(ref condition, compilation.FindType(KnownTypeCode.Boolean));
			if (isValid) {
				if (condition.IsCompileTimeConstant && trueExpression.IsCompileTimeConstant && falseExpression.IsCompileTimeConstant) {
					bool? val = condition.ConstantValue as bool?;
					if (val == true)
						return trueExpression;
					else if (val == false)
						return falseExpression;
				}
				return new OperatorResolveResult(resultType, System.Linq.Expressions.ExpressionType.Conditional,
				                                 condition, trueExpression, falseExpression);
			} else {
				return new ErrorResolveResult(resultType);
			}
		}
		
		bool HasType(ResolveResult r)
		{
			return r.Type.Kind != TypeKind.Unknown && r.Type.Kind != TypeKind.Null;
		}
		#endregion
		
		#region ResolvePrimitive
		public ResolveResult ResolvePrimitive(object value)
		{
			if (value == null) {
				return NullResult;
			} else {
				TypeCode typeCode = Type.GetTypeCode(value.GetType());
				IType type = compilation.FindType(typeCode);
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
		
		#region ResolveArrayCreation
		/// <summary>
		/// Resolves an array creation.
		/// </summary>
		/// <param name="elementType">
		/// The array element type.
		/// Pass null to resolve an implicitly-typed array creation.
		/// </param>
		/// <param name="dimensions">
		/// The number of array dimensions.
		/// </param>
		/// <param name="sizeArguments">
		/// The size arguments. May be null if no explicit size was given.
		/// The resolver may mutate this array to wrap elements in <see cref="ConversionResolveResult"/>s!
		/// </param>
		/// <param name="initializerElements">
		/// The initializer elements. May be null if no array initializer was specified.
		/// The resolver may mutate this array to wrap elements in <see cref="ConversionResolveResult"/>s!
		/// </param>
		/// <param name="allowArrayConstants">
		/// Specifies whether to allow treating single-dimensional arrays like compile-time constants.
		/// This is used for attribute arguments.
		/// </param>
		public ArrayCreateResolveResult ResolveArrayCreation(IType elementType, int dimensions = 1, ResolveResult[] sizeArguments = null, ResolveResult[] initializerElements = null)
		{
			if (sizeArguments != null && dimensions != Math.Max(1, sizeArguments.Length))
				throw new ArgumentException("dimensions and sizeArguments.Length don't match");
			if (elementType == null) {
				TypeInference typeInference = new TypeInference(compilation, conversions);
				bool success;
				elementType = typeInference.GetBestCommonType(initializerElements, out success);
			}
			IType arrayType = new ArrayType(compilation, elementType, dimensions);
			
			if (sizeArguments != null)
				AdjustArrayAccessArguments(sizeArguments);
			
			if (initializerElements != null) {
				for (int i = 0; i < initializerElements.Length; i++) {
					initializerElements[i] = Convert(initializerElements[i], elementType);
				}
			}
			return new ArrayCreateResolveResult(arrayType, sizeArguments, initializerElements);
		}
		#endregion
		
		public ResolveResult ResolveTypeOf(IType referencedType)
		{
			return new TypeOfResolveResult(compilation.FindType(KnownTypeCode.Type), referencedType);
		}
	}
}
