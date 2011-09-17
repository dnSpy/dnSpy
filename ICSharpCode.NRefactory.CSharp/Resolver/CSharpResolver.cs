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
		static readonly ResolveResult DynamicResult = new ResolveResult(SharedTypes.Dynamic);
		static readonly ResolveResult NullResult = new ResolveResult(SharedTypes.Null);
		
		readonly ITypeResolveContext context;
		internal readonly Conversions conversions;
		internal readonly CancellationToken cancellationToken;
		
		#region Constructor
		public CSharpResolver(ITypeResolveContext context) : this (context, CancellationToken.None)
		{
		}
		
		public CSharpResolver(ITypeResolveContext context, CancellationToken cancellationToken)
		{
			if (context == null)
				throw new ArgumentNullException("context");
			this.context = context;
			this.cancellationToken = cancellationToken;
			this.conversions = Conversions.Get(context);
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
		/// Gets the current project content.
		/// Returns <c>CurrentUsingScope.ProjectContent</c>.
		/// </summary>
		public IProjectContent ProjectContent {
			get {
				if (currentUsingScope != null)
					return currentUsingScope.UsingScope.ProjectContent;
				else
					return null;
			}
		}
		#endregion
		
		#region Per-CurrentTypeDefinition Cache
		TypeDefinitionCache currentTypeDefinition;
		
		/// <summary>
		/// Gets/Sets the current type definition that is used to look up identifiers as simple members.
		/// </summary>
		public ITypeDefinition CurrentTypeDefinition {
			get { return currentTypeDefinition != null ? currentTypeDefinition.TypeDefinition : null; }
			set {
				if (value == null) {
					currentTypeDefinition = null;
				} else {
					if (currentTypeDefinition == null || currentTypeDefinition.TypeDefinition != value) {
						currentTypeDefinition = new TypeDefinitionCache(value);
					}
				}
			}
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
		
		#region CurrentUsingScope
		UsingScopeCache currentUsingScope;
		
		/// <summary>
		/// Gets/Sets the current using scope that is used to look up identifiers as class names.
		/// </summary>
		public UsingScope CurrentUsingScope {
			get { return currentUsingScope != null ? currentUsingScope.UsingScope : null; }
			set {
				if (value == null) {
					currentUsingScope = null;
				} else {
					if (currentUsingScope == null || currentUsingScope.UsingScope != value) {
						currentUsingScope = new UsingScopeCache(value);
					}
				}
			}
		}
		
		/// <summary>
		/// There is one cache instance per using scope; and it might be shared between multiple resolvers
		/// that are on different threads, so it must be thread-safe.
		/// </summary>
		sealed class UsingScopeCache
		{
			public readonly UsingScope UsingScope;
			public readonly Dictionary<string, ResolveResult> ResolveCache = new Dictionary<string, ResolveResult>();
			
			public List<List<IMethod>> AllExtensionMethods;
			
			public UsingScopeCache(UsingScope usingScope)
			{
				this.UsingScope = usingScope;
			}
		}
		#endregion
		
		#region Local Variable Management
		class LocalVariable : IVariable
		{
			// We store the local variable in a linked list
			// and provide a stack-like API.
			// The beginning of a stack frame is marked by a dummy local variable
			// with type==null and name==null.
			
			// This data structure is used to allow efficient cloning of the resolver with its local variable context.
			
			internal readonly LocalVariable prev;
			internal readonly ITypeReference type;
			internal readonly DomRegion region;
			internal readonly string name;
			internal readonly IConstantValue constantValue;
			
			public LocalVariable(LocalVariable prev, ITypeReference type, DomRegion region, string name, IConstantValue constantValue)
			{
				this.prev = prev;
				this.region = region;
				this.type = type;
				this.name = name;
				this.constantValue = constantValue;
			}
			
			string IVariable.Name {
				get { return name; }
			}
			
			DomRegion IVariable.Region {
				get { return region; }
			}
			
			ITypeReference IVariable.Type {
				get { return type; }
			}
			bool IVariable.IsConst {
				get { return constantValue != null; }
			}
			IConstantValue IVariable.ConstantValue {
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
		
		sealed class LambdaParameter : LocalVariable, IParameter
		{
			readonly bool isRef;
			readonly bool isOut;
			
			public LambdaParameter(LocalVariable prev, ITypeReference type, DomRegion region, string name, bool isRef, bool isOut)
				: base(prev, type, region, name, null)
			{
				this.isRef = isRef;
				this.isOut = isOut;
			}
			
			IList<IAttribute> IParameter.Attributes {
				get { return EmptyList<IAttribute>.Instance; }
			}
			
			IConstantValue IParameter.DefaultValue {
				get { return null; }
			}
			
			bool IParameter.IsRef {
				get { return isRef; }
			}
			
			bool IParameter.IsOut {
				get { return isOut; }
			}
			
			bool IParameter.IsParams {
				get { return false; }
			}
			
			bool IParameter.IsOptional {
				get { return false; }
			}
			
			bool IFreezable.IsFrozen {
				get { return true; }
			}
			
			void IFreezable.Freeze()
			{
			}
		}
		
		LocalVariable localVariableStack;
		
		/// <summary>
		/// Opens a new scope for local variables.
		/// </summary>
		public void PushBlock()
		{
			localVariableStack = new LocalVariable(localVariableStack, null, DomRegion.Empty, null, null);
		}
		
		/// <summary>
		/// Opens a new scope for local variables.
		/// This works like <see cref="PushBlock"/>, but additionally sets <see cref="IsWithinLambdaExpression"/> to true.
		/// </summary>
		public void PushLambdaBlock()
		{
			localVariableStack = new LambdaParameter(localVariableStack, null, DomRegion.Empty, null, false, false);
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
		public IVariable AddVariable(ITypeReference type, DomRegion declarationRegion, string name, IConstantValue constantValue = null)
		{
			if (type == null)
				throw new ArgumentNullException("type");
			if (name == null)
				throw new ArgumentNullException("name");
			return localVariableStack = new LocalVariable(localVariableStack, type, declarationRegion, name, constantValue);
		}
		
		/// <summary>
		/// Adds a new lambda parameter to the current block.
		/// </summary>
		public IParameter AddLambdaParameter(ITypeReference type, DomRegion declarationRegion, string name, bool isRef = false, bool isOut = false)
		{
			if (type == null)
				throw new ArgumentNullException("type");
			if (name == null)
				throw new ArgumentNullException("name");
			LambdaParameter p = new LambdaParameter(localVariableStack, type, declarationRegion, name, isRef, isOut);
			localVariableStack = p;
			return p;
		}
		
		/// <summary>
		/// Gets all currently visible local variables and lambda parameters.
		/// </summary>
		public IEnumerable<IVariable> LocalVariables {
			get {
				for (LocalVariable v = localVariableStack; v != null; v = v.prev) {
					if (v.name != null)
						yield return v;
				}
			}
		}
		
		/// <summary>
		/// Gets whether the resolver is currently within a lambda expression.
		/// </summary>
		public bool IsWithinLambdaExpression {
			get {
				for (LocalVariable v = localVariableStack; v != null; v = v.prev) {
					if (v.name == null && v is LambdaParameter)
						return true;
				}
				return false;
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
		
		ObjectInitializerContext objectInitializerStack;
		
		/// <summary>
		/// Pushes the type of the object that is currently being initialized.
		/// </summary>
		public void PushInitializerType(IType type)
		{
			if (type == null)
				throw new ArgumentNullException("type");
			objectInitializerStack = new ObjectInitializerContext(type, objectInitializerStack);
		}
		
		public void PopInitializerType()
		{
			if (objectInitializerStack == null)
				throw new InvalidOperationException();
			objectInitializerStack = objectInitializerStack.prev;
		}
		
		/// <summary>
		/// Gets the type of the object currently being initialized.
		/// Returns SharedTypes.Unknown if no object initializer is currently open (or if the object initializer
		/// has unknown type).
		/// </summary>
		public IType CurrentObjectInitializerType {
			get { return objectInitializerStack != null ? objectInitializerStack.type : SharedTypes.UnknownType; }
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
				get { return this; }
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
			
			bool IEntity.IsPrivate {
				get { return false; }
			}
			
			bool IEntity.IsPublic {
				get { return true; }
			}
			
			bool IEntity.IsProtected {
				get { return false; }
			}
			
			bool IEntity.IsInternal {
				get { return false; }
			}
			
			bool IEntity.IsProtectedOrInternal {
				get { return false; }
			}
			
			bool IEntity.IsProtectedAndInternal {
				get { return false; }
			}
			
			IProjectContent IEntity.ProjectContent {
				get { throw new NotSupportedException(); }
			}
			
			IParsedFile IEntity.ParsedFile {
				get { return null; }
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
			
			if (SharedTypes.Dynamic.Equals(expression.Type))
				return new UnaryOperatorResolveResult(SharedTypes.Dynamic, op, expression);
			
			// C# 4.0 spec: §7.3.3 Unary operator overload resolution
			string overloadableOperatorName = GetOverloadableOperatorName(op);
			if (overloadableOperatorName == null) {
				switch (op) {
					case UnaryOperatorType.Dereference:
						PointerType p = expression.Type as PointerType;
						if (p != null)
							return new UnaryOperatorResolveResult(p.ElementType, op, expression);
						else
							return ErrorResult;
					case UnaryOperatorType.AddressOf:
						return new UnaryOperatorResolveResult(new PointerType(expression.Type), op, expression);
					case UnaryOperatorType.Await:
						ResolveResult getAwaiterMethodGroup = ResolveMemberAccess(expression, "GetAwaiter", EmptyList<IType>.Instance, true);
						ResolveResult getAwaiterInvocation = ResolveInvocation(getAwaiterMethodGroup, new ResolveResult[0]);
						var getResultMethodGroup = CreateMemberLookup().Lookup(getAwaiterInvocation, "GetResult", EmptyList<IType>.Instance, true) as MethodGroupResolveResult;
						if (getResultMethodGroup != null) {
							var or = getResultMethodGroup.PerformOverloadResolution(context, new ResolveResult[0], allowExtensionMethods: false, conversions: conversions);
							IType awaitResultType = or.GetBestCandidateWithSubstitutedTypeArguments().ReturnType.Resolve(context);
							return new UnaryOperatorResolveResult(awaitResultType, UnaryOperatorType.Await, expression);
						} else {
							return new UnaryOperatorResolveResult(SharedTypes.UnknownType, UnaryOperatorType.Await, expression);
						}
					default:
						throw new ArgumentException("Invalid value for UnaryOperatorType", "op");
				}
			}
			// If the type is nullable, get the underlying type:
			IType type = NullableType.GetUnderlyingType(expression.Type);
			bool isNullable = NullableType.IsNullable(expression.Type);
			
			// the operator is overloadable:
			OverloadResolution userDefinedOperatorOR = new OverloadResolution(context, new[] { expression }, conversions: conversions);
			foreach (var candidate in GetUserDefinedOperatorCandidates(type, overloadableOperatorName)) {
				userDefinedOperatorOR.AddCandidate(candidate);
			}
			if (userDefinedOperatorOR.FoundApplicableCandidate) {
				return CreateResolveResultForUserDefinedOperator(userDefinedOperatorOR);
			}
			
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
					if ((code >= TypeCode.SByte && code <= TypeCode.Decimal) || type.Kind == TypeKind.Enum || type.Kind == TypeKind.Pointer)
						return new UnaryOperatorResolveResult(expression.Type, op, expression);
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
					if (type.Kind == TypeKind.Enum) {
						if (expression.IsCompileTimeConstant && !isNullable) {
							// evaluate as (E)(~(U)x);
							var U = expression.ConstantValue.GetType().ToTypeReference().Resolve(context);
							var unpackedEnum = new ConstantResolveResult(U, expression.ConstantValue);
							return CheckErrorAndResolveCast(expression.Type, ResolveUnaryOperator(op, unpackedEnum));
						} else {
							return new UnaryOperatorResolveResult(expression.Type, op, expression);
						}
					} else {
						methodGroup = bitwiseComplementOperators;
						break;
					}
				default:
					throw new InvalidOperationException();
			}
			OverloadResolution builtinOperatorOR = new OverloadResolution(context, new[] { expression }, conversions: conversions);
			foreach (var candidate in methodGroup) {
				builtinOperatorOR.AddCandidate(candidate);
			}
			UnaryOperatorMethod m = (UnaryOperatorMethod)builtinOperatorOR.BestCandidate;
			IType resultType = m.ReturnType.Resolve(context);
			if (builtinOperatorOR.BestCandidateErrors != OverloadResolutionErrors.None) {
				// If there are any user-defined operators, prefer those over the built-in operators.
				// It'll be a more informative error.
				if (userDefinedOperatorOR.BestCandidate != null)
					return CreateResolveResultForUserDefinedOperator(userDefinedOperatorOR);
				else
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
				expression = Convert(expression, m.Parameters[0].Type, builtinOperatorOR.ArgumentConversions[0]);
				return new UnaryOperatorResolveResult(resultType, op, expression);
			}
		}
		#endregion
		
		#region UnaryNumericPromotion
		ResolveResult UnaryNumericPromotion(UnaryOperatorType op, ref IType type, bool isNullable, ResolveResult expression)
		{
			// C# 4.0 spec: §7.3.6.1
			TypeCode code = ReflectionHelper.GetTypeCode(type);
			if (isNullable && SharedTypes.Null.Equals(type))
				code = TypeCode.SByte; // cause promotion of null to int32
			switch (op) {
				case UnaryOperatorType.Minus:
					if (code == TypeCode.UInt32) {
						type = KnownTypeReference.Int64.Resolve(context);
						return Convert(expression, MakeNullable(type, isNullable),
						               isNullable ? Conversion.ImplicitNullableConversion : Conversion.ImplicitNumericConversion);
					}
					goto case UnaryOperatorType.Plus;
				case UnaryOperatorType.Plus:
				case UnaryOperatorType.BitNot:
					if (code >= TypeCode.Char && code <= TypeCode.UInt16) {
						type = KnownTypeReference.Int32.Resolve(context);
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
		#endregion
		
		#region ResolveBinaryOperator
		#region ResolveBinaryOperator method
		public ResolveResult ResolveBinaryOperator(BinaryOperatorType op, ResolveResult lhs, ResolveResult rhs)
		{
			cancellationToken.ThrowIfCancellationRequested();
			
			if (SharedTypes.Dynamic.Equals(lhs.Type) || SharedTypes.Dynamic.Equals(rhs.Type)) {
				lhs = Convert(lhs, SharedTypes.Dynamic);
				rhs = Convert(rhs, SharedTypes.Dynamic);
				return new BinaryOperatorResolveResult(SharedTypes.Dynamic, lhs, op, rhs);
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
			OverloadResolution userDefinedOperatorOR = new OverloadResolution(context, new[] { lhs, rhs }, conversions: conversions);
			HashSet<IParameterizedMember> userOperatorCandidates = new HashSet<IParameterizedMember>();
			userOperatorCandidates.UnionWith(GetUserDefinedOperatorCandidates(lhsType, overloadableOperatorName));
			userOperatorCandidates.UnionWith(GetUserDefinedOperatorCandidates(rhsType, overloadableOperatorName));
			foreach (var candidate in userOperatorCandidates) {
				userDefinedOperatorOR.AddCandidate(candidate);
			}
			if (userDefinedOperatorOR.FoundApplicableCandidate) {
				return CreateResolveResultForUserDefinedOperator(userDefinedOperatorOR);
			}
			
			if (SharedTypes.Null.Equals(lhsType) && rhsType.IsReferenceType(context) == false
			    || lhsType.IsReferenceType(context) == false && SharedTypes.Null.Equals(rhsType))
			{
				isNullable = true;
			}
			if (op == BinaryOperatorType.ShiftLeft || op == BinaryOperatorType.ShiftRight) {
				// special case: the shift operators allow "var x = null << null", producing int?.
				if (SharedTypes.Null.Equals(lhsType) && SharedTypes.Null.Equals(rhsType))
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
						if (lhsType.Kind == TypeKind.Enum) {
							// E operator +(E x, U y);
							IType underlyingType = MakeNullable(lhsType.GetEnumUnderlyingType(context), isNullable);
							if (TryConvert(ref rhs, underlyingType)) {
								return HandleEnumOperator(isNullable, lhsType, op, lhs, rhs);
							}
						}
						if (rhsType.Kind == TypeKind.Enum) {
							// E operator +(U x, E y);
							IType underlyingType = MakeNullable(rhsType.GetEnumUnderlyingType(context), isNullable);
							if (TryConvert(ref lhs, underlyingType)) {
								return HandleEnumOperator(isNullable, rhsType, op, lhs, rhs);
							}
						}
						
						if (lhsType.Kind == TypeKind.Delegate && TryConvert(ref rhs, lhsType)) {
							return new BinaryOperatorResolveResult(lhsType, lhs, op, rhs);
						} else if (rhsType.Kind == TypeKind.Delegate && TryConvert(ref lhs, rhsType)) {
							return new BinaryOperatorResolveResult(rhsType, lhs, op, rhs);
						}
						
						if (lhsType is PointerType) {
							methodGroup = new [] {
								new PointerArithmeticOperator(lhsType, lhsType, KnownTypeReference.Int32),
								new PointerArithmeticOperator(lhsType, lhsType, KnownTypeReference.UInt32),
								new PointerArithmeticOperator(lhsType, lhsType, KnownTypeReference.Int64),
								new PointerArithmeticOperator(lhsType, lhsType, KnownTypeReference.UInt64)
							};
						} else if (rhsType is PointerType) {
							methodGroup = new [] {
								new PointerArithmeticOperator(rhsType, KnownTypeReference.Int32, rhsType),
								new PointerArithmeticOperator(rhsType, KnownTypeReference.UInt32, rhsType),
								new PointerArithmeticOperator(rhsType, KnownTypeReference.Int64, rhsType),
								new PointerArithmeticOperator(rhsType, KnownTypeReference.UInt64, rhsType)
							};
						}
						if (SharedTypes.Null.Equals(lhsType) && SharedTypes.Null.Equals(rhsType))
							return new ErrorResolveResult(SharedTypes.Null);
					}
					break;
				case BinaryOperatorType.Subtract:
					methodGroup = CheckForOverflow ? checkedSubtractionOperators : uncheckedSubtractionOperators;
					{
						if (lhsType.Kind == TypeKind.Enum) {
							// E operator –(E x, U y);
							IType underlyingType = MakeNullable(lhsType.GetEnumUnderlyingType(context), isNullable);
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
							return new BinaryOperatorResolveResult(lhsType, lhs, op, rhs);
						} else if (rhsType.Kind == TypeKind.Delegate && TryConvert(ref lhs, rhsType)) {
							return new BinaryOperatorResolveResult(rhsType, lhs, op, rhs);
						}
						
						if (lhsType is PointerType) {
							if (rhsType is PointerType) {
								IType int64 = KnownTypeReference.Int64.Resolve(context);
								if (lhsType.Equals(rhsType)) {
									return new BinaryOperatorResolveResult(int64, lhs, op, rhs);
								} else {
									return new ErrorResolveResult(int64);
								}
							}
							methodGroup = new [] {
								new PointerArithmeticOperator(lhsType, lhsType, KnownTypeReference.Int32),
								new PointerArithmeticOperator(lhsType, lhsType, KnownTypeReference.UInt32),
								new PointerArithmeticOperator(lhsType, lhsType, KnownTypeReference.Int64),
								new PointerArithmeticOperator(lhsType, lhsType, KnownTypeReference.UInt64)
							};
						}
						
						if (SharedTypes.Null.Equals(lhsType) && SharedTypes.Null.Equals(rhsType))
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
						if (lhsType.Kind == TypeKind.Enum && TryConvert(ref rhs, lhs.Type)) {
							// bool operator op(E x, E y);
							return HandleEnumComparison(op, lhsType, isNullable, lhs, rhs);
						} else if (rhsType.Kind == TypeKind.Enum && TryConvert(ref lhs, rhs.Type)) {
							// bool operator op(E x, E y);
							return HandleEnumComparison(op, rhsType, isNullable, lhs, rhs);
						} else if (lhsType is PointerType && rhsType is PointerType) {
							return new BinaryOperatorResolveResult(KnownTypeReference.Boolean.Resolve(context), lhs, op, rhs);
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
						if (lhsType.Kind == TypeKind.Enum && TryConvert(ref rhs, lhs.Type)) {
							// bool operator op(E x, E y);
							return HandleEnumOperator(isNullable, lhsType, op, lhs, rhs);
						} else if (rhsType.Kind == TypeKind.Enum && TryConvert(ref lhs, rhs.Type)) {
							// bool operator op(E x, E y);
							return HandleEnumOperator(isNullable, rhsType, op, lhs, rhs);
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
			OverloadResolution builtinOperatorOR = new OverloadResolution(context, new[] { lhs, rhs }, conversions: conversions);
			foreach (var candidate in methodGroup) {
				builtinOperatorOR.AddCandidate(candidate);
			}
			BinaryOperatorMethod m = (BinaryOperatorMethod)builtinOperatorOR.BestCandidate;
			IType resultType = m.ReturnType.Resolve(context);
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
				return new BinaryOperatorResolveResult(resultType, lhs, op, rhs);
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
			IType resultType = KnownTypeReference.Boolean.Resolve(context);
			return new BinaryOperatorResolveResult(resultType, lhs, op, rhs);
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
			IType resultType = MakeNullable(elementType, isNullable);
			return new BinaryOperatorResolveResult(resultType, lhs, BinaryOperatorType.Subtract, rhs);
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
				IType elementType = enumType.GetEnumUnderlyingType(context);
				lhs = ResolveCast(elementType, lhs);
				if (lhs.IsError)
					return lhs;
				rhs = ResolveCast(elementType, rhs);
				if (rhs.IsError)
					return rhs;
				return CheckErrorAndResolveCast(enumType, ResolveBinaryOperator(op, lhs, rhs));
			}
			IType resultType = MakeNullable(enumType, isNullable);
			return new BinaryOperatorResolveResult(resultType, lhs, op, rhs);
		}
		
		IType MakeNullable(IType type, bool isNullable)
		{
			if (isNullable)
				return NullableType.Create(type, context);
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
			if (isNullable && SharedTypes.Null.Equals(lhs.Type)) {
				lhs = CastTo(rhsCode, isNullable, lhs, allowNullableConstants);
				lhsCode = rhsCode;
			} else if (isNullable && SharedTypes.Null.Equals(rhs.Type)) {
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
			IType elementType = targetType.ToTypeReference().Resolve(context);
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
		
		#region Binary operator class definitions
		abstract class BinaryOperatorMethod : OperatorMethod
		{
			public virtual bool CanEvaluateAtCompileTime { get { return true; } }
			public abstract object Invoke(CSharpResolver resolver, object lhs, object rhs);
		}
		
		sealed class PointerArithmeticOperator : BinaryOperatorMethod
		{
			public PointerArithmeticOperator(ITypeReference returnType, ITypeReference parameter1, ITypeReference parameter2)
			{
				this.ReturnType = returnType;
				this.Parameters.Add(new DefaultParameter(parameter1, "x"));
				this.Parameters.Add(new DefaultParameter(parameter2, "y"));
			}
			
			public override bool CanEvaluateAtCompileTime {
				get { return false; }
			}
			
			public override object Invoke(CSharpResolver resolver, object lhs, object rhs)
			{
				throw new NotSupportedException();
			}
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
			if (NullableType.IsNullable(lhs.Type)) {
				IType a0 = NullableType.GetUnderlyingType(lhs.Type);
				if (TryConvert(ref rhs, a0)) {
					return new BinaryOperatorResolveResult(a0, lhs, BinaryOperatorType.NullCoalescing, rhs);
				}
			}
			if (TryConvert(ref rhs, lhs.Type)) {
				return new BinaryOperatorResolveResult(lhs.Type, lhs, BinaryOperatorType.NullCoalescing, rhs);
			}
			if (TryConvert(ref lhs, rhs.Type)) {
				return new BinaryOperatorResolveResult(rhs.Type, lhs, BinaryOperatorType.NullCoalescing, rhs);
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
			var operators = type.GetMethods(context, m => m.IsOperator && m.Name == operatorName).ToList<IParameterizedMember>();
			LiftUserDefinedOperators(operators);
			return operators;
		}
		
		void LiftUserDefinedOperators(List<IParameterizedMember> operators)
		{
			int nonLiftedMethodCount = operators.Count;
			// Construct lifted operators
			for (int i = 0; i < nonLiftedMethodCount; i++) {
				var liftedMethod = LiftUserDefinedOperator(operators[i]);
				if (liftedMethod != null)
					operators.Add(liftedMethod);
			}
		}
		
		LiftedUserDefinedOperator LiftUserDefinedOperator(IParameterizedMember m)
		{
			IType returnType = m.ReturnType.Resolve(context);
			if (!NullableType.IsNonNullableValueType(returnType, context))
				return null; // cannot lift this operator
			LiftedUserDefinedOperator liftedOperator = new LiftedUserDefinedOperator(m);
			for (int i = 0; i < m.Parameters.Count; i++) {
				IType parameterType = m.Parameters[i].Type.Resolve(context);
				if (!NullableType.IsNonNullableValueType(parameterType, context))
					return null; // cannot lift this operator
				var p = new DefaultParameter(m.Parameters[i]);
				p.Type = NullableType.Create(parameterType, context);
				liftedOperator.Parameters.Add(p);
			}
			liftedOperator.ReturnType = NullableType.Create(returnType, context);
			return liftedOperator;
		}
		
		sealed class LiftedUserDefinedOperator : OperatorMethod, OverloadResolution.ILiftedOperator
		{
			internal readonly IParameterizedMember nonLiftedOperator;
			
			public LiftedUserDefinedOperator(IParameterizedMember nonLiftedMethod)
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
		
		ResolveResult CreateResolveResultForUserDefinedOperator(OverloadResolution r)
		{
			LiftedUserDefinedOperator lifted = r.BestCandidate as LiftedUserDefinedOperator;
			if (lifted != null) {
				return new CSharpInvocationResolveResult(
					null, lifted.nonLiftedOperator, lifted.ReturnType.Resolve(context),
					r.GetArgumentsWithConversions(), r.BestCandidateErrors,
					isLiftedOperatorInvocation: true,
					argumentToParameterMap: r.GetArgumentToParameterMap()
				);
			} else {
				return r.CreateResolveResult(null);
			}
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
		
		ResolveResult Convert(ResolveResult rr, ITypeReference targetType, Conversion c)
		{
			if (c == Conversion.IdentityConversion)
				return rr;
			else if (rr.IsCompileTimeConstant && c != Conversion.None)
				return ResolveCast(targetType.Resolve(context), rr);
			else
				return new ConversionResolveResult(targetType.Resolve(context), rr, c);
		}
		
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
				} else if (targetType.Kind == TypeKind.Enum) {
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
			Conversion c = conversions.ExplicitConversion(expression, targetType);
			if (c) {
				return new ConversionResolveResult(targetType, expression, c);
			} else {
				return new ErrorResolveResult(targetType);
			}
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
			
			cancellationToken.ThrowIfCancellationRequested();
			
			int k = typeArguments.Count;
			
			if (k == 0) {
				if (lookupMode == SimpleNameLookupMode.Expression || lookupMode == SimpleNameLookupMode.InvocationTarget) {
					// Look in local variables
					foreach (IVariable v in this.LocalVariables) {
						if (v.Name == identifier) {
							object constantValue = v.IsConst ? v.ConstantValue.Resolve(context).ConstantValue : null;
							return new LocalResolveResult(v, v.Type.Resolve(context), constantValue);
						}
					}
					// Look in parameters of current method
					IParameterizedMember parameterizedMember = this.CurrentMember as IParameterizedMember;
					if (parameterizedMember != null) {
						foreach (IParameter p in parameterizedMember.Parameters) {
							if (p.Name == identifier) {
								return new LocalResolveResult(p, p.Type.Resolve(context));
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
			
			bool parameterizeResultType = k > 0;
			if (parameterizeResultType && typeArguments.All(t => t.Kind == TypeKind.UnboundTypeArgument))
				parameterizeResultType = false;
			
			ResolveResult r = null;
			if (currentTypeDefinition != null) {
				Dictionary<string, ResolveResult> cache = null;
				bool foundInCache = false;
				if (k == 0) {
					switch (lookupMode) {
						case SimpleNameLookupMode.Expression:
							cache = currentTypeDefinition.SimpleNameLookupCacheExpression;
							break;
						case SimpleNameLookupMode.InvocationTarget:
							cache = currentTypeDefinition.SimpleNameLookupCacheInvocationTarget;
							break;
						case SimpleNameLookupMode.Type:
							cache = currentTypeDefinition.SimpleTypeLookupCache;
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
			
			if (currentUsingScope != null) {
				if (k == 0 && lookupMode != SimpleNameLookupMode.TypeInUsingDeclaration) {
					if (!currentUsingScope.ResolveCache.TryGetValue(identifier, out r)) {
						r = LookInCurrentUsingScope(identifier, typeArguments, false, false);
						currentUsingScope.ResolveCache[identifier] = r;
					}
				} else {
					r = LookInCurrentUsingScope(identifier, typeArguments, lookupMode == SimpleNameLookupMode.TypeInUsingDeclaration, parameterizeResultType);
				}
				if (r != null)
					return r;
			}
			
			if (typeArguments.Count == 0) {
				if (identifier == "dynamic")
					return new TypeResolveResult(SharedTypes.Dynamic);
				else
					return new UnknownIdentifierResolveResult(identifier);
			} else {
				return ErrorResult;
			}
		}
		
		ResolveResult LookInCurrentType(string identifier, IList<IType> typeArguments, SimpleNameLookupMode lookupMode, bool parameterizeResultType)
		{
			int k = typeArguments.Count;
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
				
				MemberLookup lookup = new MemberLookup(context, t, t.ProjectContent);
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
			UsingScope currentUsingScope = this.CurrentUsingScope;
			for (UsingScope n = currentUsingScope; n != null; n = n.Parent) {
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
				ITypeDefinition def = context.GetTypeDefinition(n.NamespaceName, identifier, k, StringComparer.Ordinal);
				if (def != null) {
					IType result = def;
					if (parameterizeResultType) {
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
					if (!(isInUsingDeclaration && n == currentUsingScope)) {
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
				if (!(isInUsingDeclaration && n == currentUsingScope)) {
					IType firstResult = null;
					foreach (var u in n.Usings) {
						NamespaceResolveResult ns = u.ResolveNamespace(context);
						if (ns != null) {
							def = context.GetTypeDefinition(ns.NamespaceName, identifier, k, StringComparer.Ordinal);
							if (def != null) {
								if (firstResult == null) {
									if (parameterizeResultType)
										firstResult = new ParameterizedType(def, typeArguments);
									else
										firstResult = def;
								} else {
									return new AmbiguousTypeResolveResult(firstResult);
								}
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
				return new NamespaceResolveResult(string.Empty);
			
			for (UsingScope n = this.CurrentUsingScope; n != null; n = n.Parent) {
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
		
		static ResolveResult ResolveExternAlias(string alias)
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
				return ResolveMemberAccessOnNamespace(nrr, identifier, typeArguments, typeArguments.Count > 0);
			}
			
			if (SharedTypes.Dynamic.Equals(target.Type))
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
					mgrr.usingScope = this.CurrentUsingScope;
					mgrr.resolver = this;
				}
			}
			return result;
		}
		
		public ResolveResult ResolveMemberType(ResolveResult target, string identifier, IList<IType> typeArguments)
		{
			cancellationToken.ThrowIfCancellationRequested();
			
			bool parameterizeResultType = typeArguments.Count > 0;
			if (parameterizeResultType && typeArguments.All(t => t.Kind == TypeKind.UnboundTypeArgument))
				parameterizeResultType = false;
			
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
				string fullName = NamespaceDeclaration.BuildQualifiedName(nrr.NamespaceName, identifier);
				if (context.GetNamespace(fullName, StringComparer.Ordinal) != null)
					return new NamespaceResolveResult(fullName);
			}
			ITypeDefinition def = context.GetTypeDefinition(nrr.NamespaceName, identifier, typeArguments.Count, StringComparer.Ordinal);
			if (def != null) {
				if (parameterizeResultType)
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
			return new MemberLookup(context, this.CurrentTypeDefinition, this.ProjectContent);
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
			if (currentUsingScope == null)
				return EmptyList<List<IMethod>>.Instance;
			List<List<IMethod>> extensionMethodGroups = currentUsingScope.AllExtensionMethods;
			if (extensionMethodGroups != null)
				return extensionMethodGroups;
			extensionMethodGroups = new List<List<IMethod>>();
			List<IMethod> m;
			for (UsingScope scope = currentUsingScope.UsingScope; scope != null; scope = scope.Parent) {
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
			currentUsingScope.AllExtensionMethods = extensionMethodGroups;
			return extensionMethodGroups;
		}
		
		IEnumerable<IMethod> GetExtensionMethods(string namespaceName)
		{
			return
				from c in context.GetTypes(namespaceName, StringComparer.Ordinal)
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
			
			cancellationToken.ThrowIfCancellationRequested();
			
			if (SharedTypes.Dynamic.Equals(target.Type))
				return DynamicResult;
			
			MethodGroupResolveResult mgrr = target as MethodGroupResolveResult;
			if (mgrr != null) {
				OverloadResolution or = mgrr.PerformOverloadResolution(context, arguments, argumentNames, conversions: conversions);
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
				OverloadResolution or = new OverloadResolution(context, arguments, argumentNames, conversions: conversions);
				or.AddCandidate(invokeMethod);
				return new CSharpInvocationResolveResult(
					target, invokeMethod, invokeMethod.ReturnType.Resolve(context),
					or.GetArgumentsWithConversions(), or.BestCandidateErrors,
					isExpandedForm: or.BestCandidateIsExpandedForm,
					isDelegateInvocation: true,
					argumentToParameterMap: or.GetArgumentToParameterMap());
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
						} while(argumentNames.Contains(newName));
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
					if (SharedTypes.Null.Equals(type) || SharedTypes.UnknownType.Equals(type)) {
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
			if (mgrr != null)
				return mgrr.MethodName;
			
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
			cancellationToken.ThrowIfCancellationRequested();
			
			switch (target.Type.Kind) {
				case TypeKind.Dynamic:
					for (int i = 0; i < arguments.Length; i++) {
						arguments[i] = Convert(arguments[i], SharedTypes.Dynamic);
					}
					return new ArrayAccessResolveResult(SharedTypes.Dynamic, target, arguments);
					
				case TypeKind.Array:
				case TypeKind.Pointer:
					// §7.6.6.1 Array access / §18.5.3 Pointer element access
					AdjustArrayAccessArguments(arguments);
					return new ArrayAccessResolveResult(((TypeWithElementType)target.Type).ElementType, target, arguments);
			}
			
			// §7.6.6.2 Indexer access
			OverloadResolution or = new OverloadResolution(context, arguments, argumentNames, conversions: conversions);
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
				if (!(TryConvert(ref arguments[i], KnownTypeReference.Int32.Resolve(context)) ||
				      TryConvert(ref arguments[i], KnownTypeReference.UInt32.Resolve(context)) ||
				      TryConvert(ref arguments[i], KnownTypeReference.Int64.Resolve(context)) ||
				      TryConvert(ref arguments[i], KnownTypeReference.UInt64.Resolve(context))))
				{
					// conversion failed
					arguments[i] = Convert(arguments[i], KnownTypeReference.Int32, Conversion.None);
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
		/// <returns>InvocationResolveResult or ErrorResolveResult</returns>
		public ResolveResult ResolveObjectCreation(IType type, ResolveResult[] arguments, string[] argumentNames = null)
		{
			cancellationToken.ThrowIfCancellationRequested();
			
			if (type.Kind == TypeKind.Delegate && arguments.Length == 1) {
				return Convert(arguments[0], type);
			}
			OverloadResolution or = new OverloadResolution(context, arguments, argumentNames, conversions: conversions);
			MemberLookup lookup = CreateMemberLookup();
			bool allowProtectedAccess = lookup.IsProtectedAccessAllowed(type);
			var constructors = type.GetConstructors(context, m => lookup.IsAccessible(m, allowProtectedAccess));
			foreach (IMethod ctor in constructors) {
				or.AddCandidate(ctor);
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
					if (baseType.Kind != TypeKind.Unknown && baseType.Kind != TypeKind.Interface) {
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
			
			bool isValid;
			IType resultType;
			if (SharedTypes.Dynamic.Equals(trueExpression.Type) || SharedTypes.Dynamic.Equals(falseExpression.Type)) {
				resultType = SharedTypes.Dynamic;
				isValid = TryConvert(ref trueExpression, resultType) & TryConvert(ref falseExpression, resultType);
			} else if (HasType(trueExpression) && HasType(falseExpression)) {
				Conversion t2f = conversions.ImplicitConversion(trueExpression.Type, falseExpression.Type);
				Conversion f2t = conversions.ImplicitConversion(falseExpression.Type, trueExpression.Type);
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
			isValid &= TryConvert(ref condition, KnownTypeReference.Boolean.Resolve(context));
			if (isValid) {
				if (condition.IsCompileTimeConstant && trueExpression.IsCompileTimeConstant && falseExpression.IsCompileTimeConstant) {
					bool? val = condition.ConstantValue as bool?;
					if (val == true)
						return trueExpression;
					else if (val == false)
						return falseExpression;
				}
				return new ConditionalOperatorResolveResult(resultType, condition, trueExpression, falseExpression);
			} else {
				return new ErrorResolveResult(resultType);
			}
		}
		
		bool HasType(ResolveResult r)
		{
			return !(SharedTypes.UnknownType.Equals(r.Type) || SharedTypes.Null.Equals(r.Type));
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
				TypeInference typeInference = new TypeInference(context, conversions);
				bool success;
				elementType = typeInference.GetBestCommonType(initializerElements, out success);
			}
			IType arrayType = new ArrayType(elementType, dimensions);
			
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
	}
}
