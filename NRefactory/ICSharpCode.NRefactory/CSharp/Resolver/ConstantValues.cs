// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Linq;
using ICSharpCode.NRefactory.CSharp.Analysis;
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.TypeSystem.Implementation;
using ICSharpCode.NRefactory.Utils;

namespace ICSharpCode.NRefactory.CSharp.Resolver.ConstantValues
{
	// Contains representations for constant C# expressions.
	// We use these instead of storing the full AST to reduce the memory usage.
	
	public sealed class CSharpConstantValue : Immutable, IConstantValue, ISupportsInterning
	{
		ConstantExpression expression;
		UsingScope parentUsingScope;
		ITypeDefinition parentTypeDefinition;
		
		public CSharpConstantValue(ConstantExpression expression, UsingScope parentUsingScope, ITypeDefinition parentTypeDefinition)
		{
			if (expression == null)
				throw new ArgumentNullException("expression");
			this.expression = expression;
			this.parentUsingScope = parentUsingScope;
			this.parentTypeDefinition = parentTypeDefinition;
		}
		
		CSharpResolver CreateResolver(ITypeResolveContext context)
		{
			// Because constants are evaluated by the compiler, we need to evaluate them in the resolve context
			// of the project where they are defined, not in that where the constant value is used.
			// TODO: how do we get the correct resolve context?
			return new CSharpResolver(context) {
				CheckForOverflow = false, // TODO: get project-wide overflow setting
				CurrentTypeDefinition = parentTypeDefinition,
				UsingScope = parentUsingScope
			};
		}
		
		public IType GetValueType(ITypeResolveContext context)
		{
			CSharpResolver resolver = CreateResolver(context);
			IType type = expression.Resolve(resolver).Type;
			if (resolver.Context != context) {
				// Retrieve the equivalent type in the new resolve context.
				// E.g. if the constant is defined in a .NET 2.0 project, type might be Int32 from mscorlib 2.0.
				// However, the calling project might be a .NET 4.0 project, so we need to return Int32 from mscorlib 4.0.
				return type.AcceptVisitor(new MapTypeIntoNewContext(context));
			}
			return type;
		}
		
		public object GetValue(ITypeResolveContext context)
		{
			CSharpResolver resolver = CreateResolver(context);
			object val = expression.Resolve(resolver).ConstantValue;
			if (resolver.Context != context) {
				// If 'val' is a type or an array containing types, we need to map it to the new context.
				val = MapToNewContext(val, context);
			}
			return val;
		}
		
		static object MapToNewContext(object val, ITypeResolveContext context)
		{
			IType type = val as IType;
			if (type != null) {
				return type.AcceptVisitor(new MapTypeIntoNewContext(context));
			}
			object[] arr = val as object[];
			if (arr != null) {
				object[] newArr = new object[arr.Length];
				bool modified = false;
				for (int i = 0; i < arr.Length; i++) {
					newArr[i] = MapToNewContext(arr[i], context);
					modified |= arr[i] != newArr[i];
				}
				if (modified)
					return newArr;
			}
			return val;
		}
		
		void ISupportsInterning.PrepareForInterning(IInterningProvider provider)
		{
			expression = provider.Intern(expression);
		}
		
		int ISupportsInterning.GetHashCodeForInterning()
		{
			return expression.GetHashCode()
				^ (parentUsingScope != null ? parentUsingScope.GetHashCode() : 0)
				^ (parentTypeDefinition != null ? parentTypeDefinition.GetHashCode() : 0);
		}
		
		bool ISupportsInterning.EqualsForInterning(ISupportsInterning other)
		{
			CSharpConstantValue cv = other as CSharpConstantValue;
			return cv != null
				&& expression == cv.expression
				&& parentUsingScope == cv.parentUsingScope
				&& parentTypeDefinition == cv.parentTypeDefinition;
		}
	}
	
	/// <summary>
	/// Increments an integer <see cref="IConstantValue"/> by a fixed amount without changing the type.
	/// </summary>
	public sealed class IncrementConstantValue : Immutable, IConstantValue, ISupportsInterning
	{
		IConstantValue baseValue;
		int incrementAmount;
		
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
		
		public IType GetValueType(ITypeResolveContext context)
		{
			return baseValue.GetValueType(context);
		}
		
		public object GetValue(ITypeResolveContext context)
		{
			object val = baseValue.GetValue(context);
			if (val == null)
				return null;
			TypeCode typeCode = Type.GetTypeCode(val.GetType());
			if (!(typeCode >= TypeCode.SByte && typeCode <= TypeCode.UInt64))
				return null;
			long intVal = (long)CSharpPrimitiveCast.Cast(TypeCode.Int64, val, false);
			return CSharpPrimitiveCast.Cast(typeCode, unchecked(intVal + incrementAmount), false);
		}
		
		void ISupportsInterning.PrepareForInterning(IInterningProvider provider)
		{
			baseValue = provider.Intern(baseValue);
		}
		
		int ISupportsInterning.GetHashCodeForInterning()
		{
			return baseValue.GetHashCode() ^ incrementAmount;
		}
		
		bool ISupportsInterning.EqualsForInterning(ISupportsInterning other)
		{
			IncrementConstantValue o = other as IncrementConstantValue;
			return o != null && baseValue == o.baseValue && incrementAmount == o.incrementAmount;
		}
	}
	
	public abstract class ConstantExpression
	{
		public abstract ResolveResult Resolve(CSharpResolver resolver);
	}
	
	/// <summary>
	/// C#'s equivalent to the SimpleConstantValue.
	/// </summary>
	public sealed class PrimitiveConstantExpression : ConstantExpression, ISupportsInterning
	{
		ITypeReference type;
		object value;
		
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
			object val = value;
			if (val is ITypeReference)
				val = ((ITypeReference)val).Resolve(resolver.Context);
			return new ConstantResolveResult(type.Resolve(resolver.Context), val);
		}
		
		void ISupportsInterning.PrepareForInterning(IInterningProvider provider)
		{
			type = provider.Intern(type);
			value = provider.Intern(value);
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
	
	public sealed class ConstantCast : ConstantExpression, ISupportsInterning
	{
		ITypeReference targetType;
		ConstantExpression expression;
		
		public ConstantCast(ITypeReference targetType, ConstantExpression expression)
		{
			if (targetType == null)
				throw new ArgumentNullException("targetType");
			if (expression == null)
				throw new ArgumentNullException("expression");
			this.targetType = targetType;
			this.expression = expression;
		}
		
		public override ResolveResult Resolve(CSharpResolver resolver)
		{
			return resolver.ResolveCast(targetType.Resolve(resolver.Context), expression.Resolve(resolver));
		}
		
		void ISupportsInterning.PrepareForInterning(IInterningProvider provider)
		{
			targetType = provider.Intern(targetType);
			expression = provider.Intern(expression);
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
				&& this.targetType == cast.targetType && this.expression == cast.expression;
		}
	}
	
	public sealed class ConstantIdentifierReference : ConstantExpression, ISupportsInterning
	{
		string identifier;
		IList<ITypeReference> typeArguments;
		
		public ConstantIdentifierReference(string identifier, IList<ITypeReference> typeArguments = null)
		{
			if (identifier == null)
				throw new ArgumentNullException("identifier");
			this.identifier = identifier;
			this.typeArguments = typeArguments;
		}
		
		public override ResolveResult Resolve(CSharpResolver resolver)
		{
			return resolver.ResolveSimpleName(identifier, ResolveTypes(resolver, typeArguments));
		}
		
		internal static IList<IType> ResolveTypes(CSharpResolver resolver, IList<ITypeReference> typeArguments)
		{
			if (typeArguments == null)
				return EmptyList<IType>.Instance;
			IType[] types = new IType[typeArguments.Count];
			for (int i = 0; i < types.Length; i++) {
				types[i] = typeArguments[i].Resolve(resolver.Context);
			}
			return types;
		}
		
		void ISupportsInterning.PrepareForInterning(IInterningProvider provider)
		{
			identifier = provider.Intern(identifier);
			typeArguments = provider.InternList(typeArguments);
		}
		
		int ISupportsInterning.GetHashCodeForInterning()
		{
			unchecked {
				int hashCode = identifier.GetHashCode();
				if (typeArguments != null)
					hashCode ^= typeArguments.GetHashCode();
				return hashCode;
			}
		}
		
		bool ISupportsInterning.EqualsForInterning(ISupportsInterning other)
		{
			ConstantIdentifierReference cir = other as ConstantIdentifierReference;
			return cir != null &&
				this.identifier == cir.identifier && this.typeArguments == cir.typeArguments;
		}
	}
	
	public sealed class ConstantMemberReference : ConstantExpression, ISupportsInterning
	{
		ITypeReference targetType;
		ConstantExpression targetExpression;
		string memberName;
		IList<ITypeReference> typeArguments;
		
		public ConstantMemberReference(ITypeReference targetType, string memberName, IList<ITypeReference> typeArguments = null)
		{
			if (targetType == null)
				throw new ArgumentNullException("targetType");
			if (memberName == null)
				throw new ArgumentNullException("memberName");
			this.targetType = targetType;
			this.memberName = memberName;
			this.typeArguments = typeArguments;
		}
		
		public ConstantMemberReference(ConstantExpression targetExpression, string memberName, IList<ITypeReference> typeArguments = null)
		{
			if (targetExpression == null)
				throw new ArgumentNullException("targetExpression");
			if (memberName == null)
				throw new ArgumentNullException("memberName");
			this.targetExpression = targetExpression;
			this.memberName = memberName;
			this.typeArguments = typeArguments;
		}
		
		public override ResolveResult Resolve(CSharpResolver resolver)
		{
			ResolveResult rr;
			if (targetType != null)
				rr = new TypeResolveResult(targetType.Resolve(resolver.Context));
			else
				rr = targetExpression.Resolve(resolver);
			return resolver.ResolveMemberAccess(rr, memberName, ConstantIdentifierReference.ResolveTypes(resolver, typeArguments));
		}
		
		void ISupportsInterning.PrepareForInterning(IInterningProvider provider)
		{
			targetType = provider.Intern(targetType);
			targetExpression = provider.Intern(targetExpression);
			memberName = provider.Intern(memberName);
			typeArguments = provider.InternList(typeArguments);
		}
		
		int ISupportsInterning.GetHashCodeForInterning()
		{
			unchecked {
				int hashCode;
				if (targetType != null)
					hashCode = targetType.GetHashCode();
				else
					hashCode = targetExpression.GetHashCode();
				hashCode ^= memberName.GetHashCode();
				if (typeArguments != null)
					hashCode ^= typeArguments.GetHashCode();
				return hashCode;
			}
		}
		
		bool ISupportsInterning.EqualsForInterning(ISupportsInterning other)
		{
			ConstantMemberReference cmr = other as ConstantMemberReference;
			return cmr != null
				&& this.targetType == cmr.targetType && this.targetExpression == cmr.targetExpression
				&& this.memberName == cmr.memberName && this.typeArguments == cmr.typeArguments;
		}
	}
	
	public sealed class ConstantCheckedExpression : ConstantExpression, ISupportsInterning
	{
		bool checkForOverflow;
		ConstantExpression expression;
		
		public ConstantCheckedExpression(bool checkForOverflow, ConstantExpression expression)
		{
			if (expression == null)
				throw new ArgumentNullException("expression");
			this.checkForOverflow = checkForOverflow;
			this.expression = expression;
		}
		
		public override ResolveResult Resolve(CSharpResolver resolver)
		{
			bool oldCheckForOverflow = resolver.CheckForOverflow;
			try {
				resolver.CheckForOverflow = this.checkForOverflow;
				return expression.Resolve(resolver);
			} finally {
				resolver.CheckForOverflow = oldCheckForOverflow;
			}
		}
		
		void ISupportsInterning.PrepareForInterning(IInterningProvider provider)
		{
			expression = provider.Intern(expression);
		}
		
		int ISupportsInterning.GetHashCodeForInterning()
		{
			return expression.GetHashCode() ^ (checkForOverflow ? 161851612 : 75163517);
		}
		
		bool ISupportsInterning.EqualsForInterning(ISupportsInterning other)
		{
			ConstantCheckedExpression cce = other as ConstantCheckedExpression;
			return cce != null
				&& this.expression == cce.expression
				&& this.checkForOverflow == cce.checkForOverflow;
		}
	}
	
	public sealed class ConstantDefaultValue : ConstantExpression, ISupportsInterning
	{
		ITypeReference type;
		
		public ConstantDefaultValue(ITypeReference type)
		{
			if (type == null)
				throw new ArgumentNullException("type");
			this.type = type;
		}
		
		public override ResolveResult Resolve(CSharpResolver resolver)
		{
			return resolver.ResolveDefaultValue(type.Resolve(resolver.Context));
		}
		
		void ISupportsInterning.PrepareForInterning(IInterningProvider provider)
		{
			type = provider.Intern(type);
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
	
	public sealed class ConstantUnaryOperator : ConstantExpression, ISupportsInterning
	{
		UnaryOperatorType operatorType;
		ConstantExpression expression;
		
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
		
		void ISupportsInterning.PrepareForInterning(IInterningProvider provider)
		{
			expression = provider.Intern(expression);
		}
		
		int ISupportsInterning.GetHashCodeForInterning()
		{
			unchecked {
				return expression.GetHashCode() * 811 + operatorType.GetHashCode();
			}
		}
		
		bool ISupportsInterning.EqualsForInterning(ISupportsInterning other)
		{
			ConstantUnaryOperator uop = other as ConstantUnaryOperator;
			return uop != null
				&& this.operatorType == uop.operatorType
				&& this.expression == uop.expression;
		}
	}

	public sealed class ConstantBinaryOperator : ConstantExpression, ISupportsInterning
	{
		ConstantExpression left;
		BinaryOperatorType operatorType;
		ConstantExpression right;
		
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
		
		void ISupportsInterning.PrepareForInterning(IInterningProvider provider)
		{
			left = provider.Intern(left);
			right = provider.Intern(right);
		}
		
		int ISupportsInterning.GetHashCodeForInterning()
		{
			unchecked {
				return left.GetHashCode() * 811 + operatorType.GetHashCode() + right.GetHashCode() * 91781;
			}
		}
		
		bool ISupportsInterning.EqualsForInterning(ISupportsInterning other)
		{
			ConstantBinaryOperator bop = other as ConstantBinaryOperator;
			return bop != null
				&& this.operatorType == bop.operatorType
				&& this.left == bop.left && this.right == bop.right;
		}
	}
	
	public sealed class ConstantConditionalOperator : ConstantExpression, ISupportsInterning
	{
		ConstantExpression condition, trueExpr, falseExpr;
		
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
		
		void ISupportsInterning.PrepareForInterning(IInterningProvider provider)
		{
			condition = provider.Intern(condition);
			trueExpr = provider.Intern(trueExpr);
			falseExpr = provider.Intern(falseExpr);
		}
		
		int ISupportsInterning.GetHashCodeForInterning()
		{
			unchecked {
				return condition.GetHashCode() * 182981713
					+ trueExpr.GetHashCode() * 917517169
					+ falseExpr.GetHashCode() * 611651;
			}
		}
		
		bool ISupportsInterning.EqualsForInterning(ISupportsInterning other)
		{
			ConstantConditionalOperator coo = other as ConstantConditionalOperator;
			return coo != null
				&& this.condition == coo.condition
				&& this.trueExpr == coo.trueExpr
				&& this.falseExpr == coo.falseExpr;
		}
	}
	
	/// <summary>
	/// Represents an array creation (as used within an attribute argument)
	/// </summary>
	public sealed class ConstantArrayCreation : ConstantExpression, ISupportsInterning
	{
		// type may be null when the element is being inferred
		ITypeReference type;
		IList<ConstantExpression> arrayElements;
		
		public ConstantArrayCreation(ITypeReference type, IList<ConstantExpression> arrayElements)
		{
			if (arrayElements == null)
				throw new ArgumentNullException("arrayElements");
			this.type = type;
			this.arrayElements = arrayElements;
		}
		
		public override ResolveResult Resolve(CSharpResolver resolver)
		{
			throw new NotImplementedException();
		}
		
		void ISupportsInterning.PrepareForInterning(IInterningProvider provider)
		{
			type = provider.Intern(type);
			arrayElements = provider.InternList(arrayElements);
		}
		
		int ISupportsInterning.GetHashCodeForInterning()
		{
			return (type != null ? type.GetHashCode() : 0) ^ arrayElements.GetHashCode();
		}
		
		bool ISupportsInterning.EqualsForInterning(ISupportsInterning other)
		{
			ConstantArrayCreation cac = other as ConstantArrayCreation;
			return cac != null && this.type == cac.type && this.arrayElements == cac.arrayElements;
		}
	}
}
