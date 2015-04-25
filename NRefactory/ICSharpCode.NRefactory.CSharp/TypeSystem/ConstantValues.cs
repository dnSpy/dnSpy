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
using System.Linq;
using ICSharpCode.NRefactory.CSharp.Analysis;
using ICSharpCode.NRefactory.CSharp.Resolver;
using ICSharpCode.NRefactory.Semantics;
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.TypeSystem.Implementation;
using ICSharpCode.NRefactory.Utils;

namespace ICSharpCode.NRefactory.CSharp.TypeSystem.ConstantValues
{
	// Contains representations for constant C# expressions.
	// We use these instead of storing the full AST to reduce the memory usage.
	
	[Serializable]
	public abstract class ConstantExpression : IConstantValue
	{
		public abstract ResolveResult Resolve(CSharpResolver resolver);
		
		public ResolveResult Resolve(ITypeResolveContext context)
		{
			var csContext = (CSharpTypeResolveContext)context;
			if (context.CurrentAssembly != context.Compilation.MainAssembly) {
				// The constant needs to be resolved in a different compilation.
				IProjectContent pc = context.CurrentAssembly as IProjectContent;
				if (pc != null) {
					ICompilation nestedCompilation = context.Compilation.SolutionSnapshot.GetCompilation(pc);
					if (nestedCompilation != null) {
						var nestedContext = MapToNestedCompilation(csContext, nestedCompilation);
						ResolveResult rr = Resolve(new CSharpResolver(nestedContext));
						return MapToNewContext(rr, context);
					}
				}
			}
			// Resolve in current context.
			return Resolve(new CSharpResolver(csContext));
		}
		
		CSharpTypeResolveContext MapToNestedCompilation(CSharpTypeResolveContext context, ICompilation nestedCompilation)
		{
			var nestedContext = new CSharpTypeResolveContext(nestedCompilation.MainAssembly);
			if (context.CurrentUsingScope != null) {
				nestedContext = nestedContext.WithUsingScope(context.CurrentUsingScope.UnresolvedUsingScope.Resolve(nestedCompilation));
			}
			if (context.CurrentTypeDefinition != null) {
				nestedContext = nestedContext.WithCurrentTypeDefinition(nestedCompilation.Import(context.CurrentTypeDefinition));
			}
			return nestedContext;
		}
		
		static ResolveResult MapToNewContext(ResolveResult rr, ITypeResolveContext newContext)
		{
			if (rr is TypeOfResolveResult) {
				return new TypeOfResolveResult(
					rr.Type.ToTypeReference().Resolve(newContext),
					((TypeOfResolveResult)rr).ReferencedType.ToTypeReference().Resolve(newContext));
			} else if (rr is ArrayCreateResolveResult) {
				ArrayCreateResolveResult acrr = (ArrayCreateResolveResult)rr;
				return new ArrayCreateResolveResult(
					acrr.Type.ToTypeReference().Resolve(newContext),
					MapToNewContext(acrr.SizeArguments, newContext),
					MapToNewContext(acrr.InitializerElements, newContext));
			} else if (rr.IsCompileTimeConstant) {
				return new ConstantResolveResult(
					rr.Type.ToTypeReference().Resolve(newContext),
					rr.ConstantValue
				);
			} else {
				return new ErrorResolveResult(rr.Type.ToTypeReference().Resolve(newContext));
			}
		}
		
		static ResolveResult[] MapToNewContext(IList<ResolveResult> input, ITypeResolveContext newContext)
		{
			if (input == null)
				return null;
			ResolveResult[] output = new ResolveResult[input.Count];
			for (int i = 0; i < output.Length; i++) {
				output[i] = MapToNewContext(input[i], newContext);
			}
			return output;
		}
	}
	
	/// <summary>
	/// Used for constants that could not be converted to IConstantValue.
	/// </summary>
	[Serializable]
	public sealed class ErrorConstantValue : IConstantValue
	{
		readonly ITypeReference type;
		
		public ErrorConstantValue(ITypeReference type)
		{
			if (type == null)
				throw new ArgumentNullException("type");
			this.type = type;
		}
		
		public ResolveResult Resolve(ITypeResolveContext context)
		{
			return new ErrorResolveResult(type.Resolve(context));
		}
	}
	
	/// <summary>
	/// Increments an integer <see cref="IConstantValue"/> by a fixed amount without changing the type.
	/// </summary>
	[Serializable]
	public sealed class IncrementConstantValue : IConstantValue, ISupportsInterning
	{
		readonly IConstantValue baseValue;
		readonly int incrementAmount;
		
		public IncrementConstantValue(IConstantValue baseValue, int incrementAmount = 1)
		{
			if (baseValue == null)
				throw new ArgumentNullException("baseValue");
			IncrementConstantValue icv = baseValue as IncrementConstantValue;
			if (icv != null) {
				this.baseValue = icv.baseValue;
				this.incrementAmount = icv.incrementAmount + incrementAmount;
			} else {
				this.baseValue = baseValue;
				this.incrementAmount = incrementAmount;
			}
		}
		
		public ResolveResult Resolve(ITypeResolveContext context)
		{
			ResolveResult rr = baseValue.Resolve(context);
			if (rr.IsCompileTimeConstant && rr.ConstantValue != null) {
				object val = rr.ConstantValue;
				TypeCode typeCode = Type.GetTypeCode(val.GetType());
				if (typeCode >= TypeCode.SByte && typeCode <= TypeCode.UInt64) {
					long intVal = (long)CSharpPrimitiveCast.Cast(TypeCode.Int64, val, false);
					object newVal = CSharpPrimitiveCast.Cast(typeCode, unchecked(intVal + incrementAmount), false);
					return new ConstantResolveResult(rr.Type, newVal);
				}
			}
			return new ErrorResolveResult(rr.Type);
		}
		
		int ISupportsInterning.GetHashCodeForInterning()
		{
			unchecked {
				return baseValue.GetHashCode() * 33 ^ incrementAmount;
			}
		}
		
		bool ISupportsInterning.EqualsForInterning(ISupportsInterning other)
		{
			IncrementConstantValue o = other as IncrementConstantValue;
			return o != null && baseValue == o.baseValue && incrementAmount == o.incrementAmount;
		}
	}
	
	/// <summary>
	/// C#'s equivalent to the SimpleConstantValue.
	/// </summary>
	[Serializable]
	public sealed class PrimitiveConstantExpression : ConstantExpression, ISupportsInterning
	{
		readonly ITypeReference type;
		readonly object value;
		
		public ITypeReference Type {
			get { return type; }
		}
		
		public object Value {
			get { return value; }
		}
		
		public PrimitiveConstantExpression(ITypeReference type, object value)
		{
			if (type == null)
				throw new ArgumentNullException("type");
			this.type = type;
			this.value = value;
		}
		
		public override ResolveResult Resolve(CSharpResolver resolver)
		{
			return new ConstantResolveResult(type.Resolve(resolver.CurrentTypeResolveContext), value);
		}
		
		int ISupportsInterning.GetHashCodeForInterning()
		{
			return type.GetHashCode() ^ (value != null ? value.GetHashCode() : 0);
		}
		
		bool ISupportsInterning.EqualsForInterning(ISupportsInterning other)
		{
			PrimitiveConstantExpression scv = other as PrimitiveConstantExpression;
			return scv != null && type == scv.type && value == scv.value;
		}
	}
	
	[Serializable]
	public sealed class TypeOfConstantExpression : ConstantExpression
	{
		readonly ITypeReference type;
		
		public ITypeReference Type {
			get { return type; }
		}
		
		public TypeOfConstantExpression(ITypeReference type)
		{
			this.type = type;
		}
		
		public override ResolveResult Resolve(CSharpResolver resolver)
		{
			return resolver.ResolveTypeOf(type.Resolve(resolver.CurrentTypeResolveContext));
		}
	}
	
	[Serializable]
	public sealed class ConstantCast : ConstantExpression, ISupportsInterning
	{
		readonly ITypeReference targetType;
		readonly ConstantExpression expression;
		readonly bool allowNullableConstants;
		
		public ConstantCast(ITypeReference targetType, ConstantExpression expression, bool allowNullableConstants)
		{
			if (targetType == null)
				throw new ArgumentNullException("targetType");
			if (expression == null)
				throw new ArgumentNullException("expression");
			this.targetType = targetType;
			this.expression = expression;
			this.allowNullableConstants = allowNullableConstants;
		}
		
		public override ResolveResult Resolve(CSharpResolver resolver)
		{
			var type = targetType.Resolve(resolver.CurrentTypeResolveContext);
			var resolveResult = expression.Resolve(resolver);
			if (allowNullableConstants && NullableType.IsNullable(type)) {
				resolveResult = resolver.ResolveCast(NullableType.GetUnderlyingType(type), resolveResult);
				if (resolveResult.IsCompileTimeConstant)
					return new ConstantResolveResult(type, resolveResult.ConstantValue);
			}
			return resolver.ResolveCast(type, resolveResult);
		}
		
		int ISupportsInterning.GetHashCodeForInterning()
		{
			unchecked {
				return targetType.GetHashCode() + expression.GetHashCode() * 1018829;
			}
		}
		
		bool ISupportsInterning.EqualsForInterning(ISupportsInterning other)
		{
			ConstantCast cast = other as ConstantCast;
			return cast != null
				&& this.targetType == cast.targetType && this.expression == cast.expression && this.allowNullableConstants == cast.allowNullableConstants;
		}
	}
	
	[Serializable]
	public sealed class ConstantIdentifierReference : ConstantExpression
	{
		readonly string identifier;
		readonly IList<ITypeReference> typeArguments;
		
		public ConstantIdentifierReference(string identifier, IList<ITypeReference> typeArguments = null)
		{
			if (identifier == null)
				throw new ArgumentNullException("identifier");
			this.identifier = identifier;
			this.typeArguments = typeArguments ?? EmptyList<ITypeReference>.Instance;
		}
		
		public override ResolveResult Resolve(CSharpResolver resolver)
		{
			return resolver.ResolveSimpleName(identifier, typeArguments.Resolve(resolver.CurrentTypeResolveContext));
		}
	}
	
	[Serializable]
	public sealed class ConstantMemberReference : ConstantExpression
	{
		readonly ITypeReference targetType;
		readonly ConstantExpression targetExpression;
		readonly string memberName;
		readonly IList<ITypeReference> typeArguments;
		
		public ConstantMemberReference(ITypeReference targetType, string memberName, IList<ITypeReference> typeArguments = null)
		{
			if (targetType == null)
				throw new ArgumentNullException("targetType");
			if (memberName == null)
				throw new ArgumentNullException("memberName");
			this.targetType = targetType;
			this.memberName = memberName;
			this.typeArguments = typeArguments ?? EmptyList<ITypeReference>.Instance;
		}
		
		public ConstantMemberReference(ConstantExpression targetExpression, string memberName, IList<ITypeReference> typeArguments = null)
		{
			if (targetExpression == null)
				throw new ArgumentNullException("targetExpression");
			if (memberName == null)
				throw new ArgumentNullException("memberName");
			this.targetExpression = targetExpression;
			this.memberName = memberName;
			this.typeArguments = typeArguments ?? EmptyList<ITypeReference>.Instance;
		}
		
		public override ResolveResult Resolve(CSharpResolver resolver)
		{
			ResolveResult rr;
			if (targetType != null)
				rr = new TypeResolveResult(targetType.Resolve(resolver.CurrentTypeResolveContext));
			else
				rr = targetExpression.Resolve(resolver);
			return resolver.ResolveMemberAccess(rr, memberName, typeArguments.Resolve(resolver.CurrentTypeResolveContext));
		}
	}
	
	[Serializable]
	public sealed class ConstantCheckedExpression : ConstantExpression
	{
		readonly bool checkForOverflow;
		readonly ConstantExpression expression;
		
		public ConstantCheckedExpression(bool checkForOverflow, ConstantExpression expression)
		{
			if (expression == null)
				throw new ArgumentNullException("expression");
			this.checkForOverflow = checkForOverflow;
			this.expression = expression;
		}
		
		public override ResolveResult Resolve(CSharpResolver resolver)
		{
			return expression.Resolve(resolver.WithCheckForOverflow(checkForOverflow));
		}
	}
	
	[Serializable]
	public sealed class ConstantDefaultValue : ConstantExpression, ISupportsInterning
	{
		readonly ITypeReference type;
		
		public ConstantDefaultValue(ITypeReference type)
		{
			if (type == null)
				throw new ArgumentNullException("type");
			this.type = type;
		}
		
		public override ResolveResult Resolve(CSharpResolver resolver)
		{
			return resolver.ResolveDefaultValue(type.Resolve(resolver.CurrentTypeResolveContext));
		}
		
		int ISupportsInterning.GetHashCodeForInterning()
		{
			return type.GetHashCode();
		}
		
		bool ISupportsInterning.EqualsForInterning(ISupportsInterning other)
		{
			ConstantDefaultValue o = other as ConstantDefaultValue;
			return o != null && this.type == o.type;
		}
	}
	
	[Serializable]
	public sealed class ConstantUnaryOperator : ConstantExpression
	{
		readonly UnaryOperatorType operatorType;
		readonly ConstantExpression expression;
		
		public ConstantUnaryOperator(UnaryOperatorType operatorType, ConstantExpression expression)
		{
			if (expression == null)
				throw new ArgumentNullException("expression");
			this.operatorType = operatorType;
			this.expression = expression;
		}
		
		public override ResolveResult Resolve(CSharpResolver resolver)
		{
			return resolver.ResolveUnaryOperator(operatorType, expression.Resolve(resolver));
		}
	}

	[Serializable]
	public sealed class ConstantBinaryOperator : ConstantExpression
	{
		readonly ConstantExpression left;
		readonly BinaryOperatorType operatorType;
		readonly ConstantExpression right;
		
		public ConstantBinaryOperator(ConstantExpression left, BinaryOperatorType operatorType, ConstantExpression right)
		{
			if (left == null)
				throw new ArgumentNullException("left");
			if (right == null)
				throw new ArgumentNullException("right");
			this.left = left;
			this.operatorType = operatorType;
			this.right = right;
		}
		
		public override ResolveResult Resolve(CSharpResolver resolver)
		{
			ResolveResult lhs = left.Resolve(resolver);
			ResolveResult rhs = right.Resolve(resolver);
			return resolver.ResolveBinaryOperator(operatorType, lhs, rhs);
		}
	}
	
	[Serializable]
	public sealed class ConstantConditionalOperator : ConstantExpression
	{
		readonly ConstantExpression condition, trueExpr, falseExpr;
		
		public ConstantConditionalOperator(ConstantExpression condition, ConstantExpression trueExpr, ConstantExpression falseExpr)
		{
			if (condition == null)
				throw new ArgumentNullException("condition");
			if (trueExpr == null)
				throw new ArgumentNullException("trueExpr");
			if (falseExpr == null)
				throw new ArgumentNullException("falseExpr");
			this.condition = condition;
			this.trueExpr = trueExpr;
			this.falseExpr = falseExpr;
		}
		
		public override ResolveResult Resolve(CSharpResolver resolver)
		{
			return resolver.ResolveConditional(
				condition.Resolve(resolver),
				trueExpr.Resolve(resolver),
				falseExpr.Resolve(resolver)
			);
		}
	}
	
	/// <summary>
	/// Represents an array creation (as used within an attribute argument)
	/// </summary>
	[Serializable]
	public sealed class ConstantArrayCreation : ConstantExpression
	{
		// type may be null when the element is being inferred
		readonly ITypeReference elementType;
		readonly IList<ConstantExpression> arrayElements;
		
		public ConstantArrayCreation(ITypeReference type, IList<ConstantExpression> arrayElements)
		{
			if (arrayElements == null)
				throw new ArgumentNullException("arrayElements");
			this.elementType = type;
			this.arrayElements = arrayElements;
		}
		
		public override ResolveResult Resolve(CSharpResolver resolver)
		{
			ResolveResult[] elements = new ResolveResult[arrayElements.Count];
			for (int i = 0; i < elements.Length; i++) {
				elements[i] = arrayElements[i].Resolve(resolver);
			}
			int[] sizeArguments = { elements.Length };
			if (elementType != null) {
				return resolver.ResolveArrayCreation(elementType.Resolve(resolver.CurrentTypeResolveContext), sizeArguments, elements);
			} else {
				return resolver.ResolveArrayCreation(null, sizeArguments, elements);
			}
		}
	}

	/// <summary>
	/// Used for sizeof() expressions in constants.
	/// </summary>
	[Serializable]
	public sealed class SizeOfConstantValue : ConstantExpression
	{
		readonly ITypeReference type;
		
		public SizeOfConstantValue(ITypeReference type)
		{
			if (type == null)
				throw new ArgumentNullException("type");
			this.type = type;
		}
		
		public override ResolveResult Resolve(CSharpResolver resolver)
		{
			return resolver.ResolveSizeOf(type.Resolve(resolver.CurrentTypeResolveContext));
		}
	}
}
