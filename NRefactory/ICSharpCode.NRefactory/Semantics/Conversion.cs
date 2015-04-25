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
using ICSharpCode.NRefactory.TypeSystem;

namespace ICSharpCode.NRefactory.Semantics
{
	/// <summary>
	/// Holds information about a conversion between two types.
	/// </summary>
	public abstract class Conversion : IEquatable<Conversion>
	{
		#region Conversion factory methods
		/// <summary>
		/// Not a valid conversion.
		/// </summary>
		public static readonly Conversion None = new InvalidConversion();
		
		/// <summary>
		/// Identity conversion.
		/// </summary>
		public static readonly Conversion IdentityConversion = new BuiltinConversion(true, 0);
		
		public static readonly Conversion ImplicitNumericConversion = new NumericOrEnumerationConversion(true, false);
		public static readonly Conversion ExplicitNumericConversion = new NumericOrEnumerationConversion(false, false);
		public static readonly Conversion ImplicitLiftedNumericConversion = new NumericOrEnumerationConversion(true, true);
		public static readonly Conversion ExplicitLiftedNumericConversion = new NumericOrEnumerationConversion(false, true);
		
		public static Conversion EnumerationConversion(bool isImplicit, bool isLifted)
		{
			return new NumericOrEnumerationConversion(isImplicit, isLifted, true);
		}
		
		public static readonly Conversion NullLiteralConversion = new BuiltinConversion(true, 1);
		
		/// <summary>
		/// The numeric conversion of a constant expression.
		/// </summary>
		public static readonly Conversion ImplicitConstantExpressionConversion = new BuiltinConversion(true, 2);
		
		public static readonly Conversion ImplicitReferenceConversion = new BuiltinConversion(true, 3);
		public static readonly Conversion ExplicitReferenceConversion = new BuiltinConversion(false, 3);
		
		public static readonly Conversion ImplicitDynamicConversion = new BuiltinConversion(true, 4);
		public static readonly Conversion ExplicitDynamicConversion = new BuiltinConversion(false, 4);
		
		public static readonly Conversion ImplicitNullableConversion = new BuiltinConversion(true, 5);
		public static readonly Conversion ExplicitNullableConversion = new BuiltinConversion(false, 5);
		
		public static readonly Conversion ImplicitPointerConversion = new BuiltinConversion(true, 6);
		public static readonly Conversion ExplicitPointerConversion = new BuiltinConversion(false, 6);
		
		public static readonly Conversion BoxingConversion = new BuiltinConversion(true, 7);
		public static readonly Conversion UnboxingConversion = new BuiltinConversion(false, 8);
		
		/// <summary>
		/// C# 'as' cast.
		/// </summary>
		public static readonly Conversion TryCast = new BuiltinConversion(false, 9);
		
		[Obsolete("Use UserDefinedConversion() instead")]
		public static Conversion UserDefinedImplicitConversion(IMethod operatorMethod, Conversion conversionBeforeUserDefinedOperator, Conversion conversionAfterUserDefinedOperator, bool isLifted)
		{
			if (operatorMethod == null)
				throw new ArgumentNullException("operatorMethod");
			return new UserDefinedConv(true, operatorMethod, conversionBeforeUserDefinedOperator, conversionAfterUserDefinedOperator, isLifted, false);
		}
		
		[Obsolete("Use UserDefinedConversion() instead")]
		public static Conversion UserDefinedExplicitConversion(IMethod operatorMethod, Conversion conversionBeforeUserDefinedOperator, Conversion conversionAfterUserDefinedOperator, bool isLifted)
		{
			if (operatorMethod == null)
				throw new ArgumentNullException("operatorMethod");
			return new UserDefinedConv(false, operatorMethod, conversionBeforeUserDefinedOperator, conversionAfterUserDefinedOperator, isLifted, false);
		}
		
		public static Conversion UserDefinedConversion(IMethod operatorMethod, bool isImplicit, Conversion conversionBeforeUserDefinedOperator, Conversion conversionAfterUserDefinedOperator, bool isLifted = false, bool isAmbiguous = false)
		{
			if (operatorMethod == null)
				throw new ArgumentNullException("operatorMethod");
			return new UserDefinedConv(isImplicit, operatorMethod, conversionBeforeUserDefinedOperator, conversionAfterUserDefinedOperator, isLifted, isAmbiguous);
		}
		
		public static Conversion MethodGroupConversion(IMethod chosenMethod, bool isVirtualMethodLookup, bool delegateCapturesFirstArgument)
		{
			if (chosenMethod == null)
				throw new ArgumentNullException("chosenMethod");
			return new MethodGroupConv(chosenMethod, isVirtualMethodLookup, delegateCapturesFirstArgument, isValid: true);
		}
		
		public static Conversion InvalidMethodGroupConversion(IMethod chosenMethod, bool isVirtualMethodLookup, bool delegateCapturesFirstArgument)
		{
			if (chosenMethod == null)
				throw new ArgumentNullException("chosenMethod");
			return new MethodGroupConv(chosenMethod, isVirtualMethodLookup, delegateCapturesFirstArgument, isValid: false);
		}
		#endregion
		
		#region Inner classes
		sealed class InvalidConversion : Conversion
		{
			public override bool IsValid {
				get { return false; }
			}
			
			public override string ToString()
			{
				return "None";
			}
		}
		
		sealed class NumericOrEnumerationConversion : Conversion
		{
			readonly bool isImplicit;
			readonly bool isLifted;
			readonly bool isEnumeration;
			
			public NumericOrEnumerationConversion(bool isImplicit, bool isLifted, bool isEnumeration = false)
			{
				this.isImplicit = isImplicit;
				this.isLifted = isLifted;
				this.isEnumeration = isEnumeration;
			}
			
			public override bool IsImplicit {
				get { return isImplicit; }
			}
			
			public override bool IsExplicit {
				get { return !isImplicit; }
			}
			
			public override bool IsNumericConversion {
				get { return !isEnumeration; }
			}
			
			public override bool IsEnumerationConversion {
				get { return isEnumeration; }
			}
			
			public override bool IsLifted {
				get { return isLifted; }
			}
			
			public override string ToString()
			{
				return (isImplicit ? "implicit" : "explicit")
					+ (isLifted ? " lifted" : "")
					+ (isEnumeration ? " enumeration" : " numeric")
					+ " conversion";
			}
			
			public override bool Equals(Conversion other)
			{
				NumericOrEnumerationConversion o = other as NumericOrEnumerationConversion;
				return o != null && isImplicit == o.isImplicit && isLifted == o.isLifted && isEnumeration == o.isEnumeration;
			}
			
			public override int GetHashCode()
			{
				return (isImplicit ? 1 : 0) + (isLifted ? 2 : 0) + (isEnumeration ? 4 : 0);
			}
		}
		
		sealed class BuiltinConversion : Conversion
		{
			readonly bool isImplicit;
			readonly byte type;
			
			public BuiltinConversion(bool isImplicit, byte type)
			{
				this.isImplicit = isImplicit;
				this.type = type;
			}
			
			public override bool IsImplicit {
				get { return isImplicit; }
			}
			
			public override bool IsExplicit {
				get { return !isImplicit; }
			}
			
			public override bool IsIdentityConversion {
				get { return type == 0; }
			}
			
			public override bool IsNullLiteralConversion {
				get { return type == 1; }
			}
			
			public override bool IsConstantExpressionConversion {
				get { return type == 2; }
			}

			public override bool IsReferenceConversion {
				get { return type == 3; }
			}
			
			public override bool IsDynamicConversion {
				get { return type == 4; }
			}
			
			public override bool IsNullableConversion {
				get { return type == 5; }
			}
			
			public override bool IsPointerConversion {
				get { return type == 6; }
			}
			
			public override bool IsBoxingConversion {
				get { return type == 7; }
			}
			
			public override bool IsUnboxingConversion {
				get { return type == 8; }
			}
			
			public override bool IsTryCast {
				get { return type == 9; }
			}
			
			public override string ToString()
			{
				string name = null;
				switch (type) {
					case 0:
						return "identity conversion";
					case 1:
						return "null-literal conversion";
					case 2:
						name = "constant-expression";
						break;
					case 3:
						name = "reference";
						break;
					case 4:
						name = "dynamic";
						break;
					case 5:
						name = "nullable";
						break;
					case 6:
						name = "pointer";
						break;
					case 7:
						return "boxing conversion";
					case 8:
						return "unboxing conversion";
					case 9:
						return "try cast";
				}
				return (isImplicit ? "implicit " : "explicit ") + name + " conversion";
			}
		}
		
		sealed class UserDefinedConv : Conversion
		{
			readonly IMethod method;
			readonly bool isLifted;
			readonly Conversion conversionBeforeUserDefinedOperator;
			readonly Conversion conversionAfterUserDefinedOperator;
			readonly bool isImplicit;
			readonly bool isValid;
			
			public UserDefinedConv(bool isImplicit, IMethod method, Conversion conversionBeforeUserDefinedOperator, Conversion conversionAfterUserDefinedOperator, bool isLifted, bool isAmbiguous)
			{
				this.method = method;
				this.isLifted = isLifted;
				this.conversionBeforeUserDefinedOperator = conversionBeforeUserDefinedOperator;
				this.conversionAfterUserDefinedOperator = conversionAfterUserDefinedOperator;
				this.isImplicit = isImplicit;
				this.isValid = !isAmbiguous;
			}
			
			public override bool IsValid {
				get { return isValid; }
			}
			
			public override bool IsImplicit {
				get { return isImplicit; }
			}
			
			public override bool IsExplicit {
				get { return !isImplicit; }
			}
			
			public override bool IsLifted {
				get { return isLifted; }
			}
			
			public override bool IsUserDefined {
				get { return true; }
			}
			
			public override Conversion ConversionBeforeUserDefinedOperator {
				get { return conversionBeforeUserDefinedOperator; }
			}
		
			public override Conversion ConversionAfterUserDefinedOperator {
				get { return conversionAfterUserDefinedOperator; }
			}

			public override IMethod Method {
				get { return method; }
			}
			
			public override bool Equals(Conversion other)
			{
				UserDefinedConv o = other as UserDefinedConv;
				return o != null && isLifted == o.isLifted && isImplicit == o.isImplicit && isValid == o.isValid && method.Equals(o.method);
			}
			
			public override int GetHashCode()
			{
				return unchecked(method.GetHashCode() + (isLifted ? 31 : 27) + (isImplicit ? 71 : 61) + (isValid ? 107 : 109));
			}
			
			public override string ToString()
			{
				return (isImplicit ? "implicit" : "explicit")
					+ (isLifted ? " lifted" : "")
					+ (isValid ? "" : " ambiguous")
					+ "user-defined conversion (" + method + ")";
			}
		}
		
		sealed class MethodGroupConv : Conversion
		{
			readonly IMethod method;
			readonly bool isVirtualMethodLookup;
			readonly bool delegateCapturesFirstArgument;
			readonly bool isValid;
			
			public MethodGroupConv(IMethod method, bool isVirtualMethodLookup, bool delegateCapturesFirstArgument, bool isValid)
			{
				this.method = method;
				this.isVirtualMethodLookup = isVirtualMethodLookup;
				this.delegateCapturesFirstArgument = delegateCapturesFirstArgument;
				this.isValid = isValid;
			}
			
			public override bool IsValid {
				get { return isValid; }
			}
			
			public override bool IsImplicit {
				get { return true; }
			}
			
			public override bool IsMethodGroupConversion {
				get { return true; }
			}
			
			public override bool IsVirtualMethodLookup {
				get { return isVirtualMethodLookup; }
			}
			
			public override bool DelegateCapturesFirstArgument {
				get { return delegateCapturesFirstArgument; }
			}

			public override IMethod Method {
				get { return method; }
			}
			
			public override bool Equals(Conversion other)
			{
				MethodGroupConv o = other as MethodGroupConv;
				return o != null && method.Equals(o.method);
			}
			
			public override int GetHashCode()
			{
				return method.GetHashCode();
			}
		}
		#endregion
		
		/// <summary>
		/// Gets whether the conversion is valid.
		/// </summary>
		public virtual bool IsValid {
			get { return true; }
		}
		
		public virtual bool IsImplicit {
			get { return false; }
		}
		
		public virtual bool IsExplicit {
			get { return false; }
		}
		
		/// <summary>
		/// Gets whether the conversion is an '<c>as</c>' cast.
		/// </summary>
		public virtual bool IsTryCast {
			get { return false; }
		}
		
		public virtual bool IsIdentityConversion {
			get { return false; }
		}
		
		public virtual bool IsNullLiteralConversion {
			get { return false; }
		}
		
		public virtual bool IsConstantExpressionConversion {
			get { return false; }
		}

		public virtual bool IsNumericConversion {
			get { return false; }
		}
		
		/// <summary>
		/// Gets whether this conversion is a lifted version of another conversion.
		/// </summary>
		public virtual bool IsLifted {
			get { return false; }
		}
		
		/// <summary>
		/// Gets whether the conversion is dynamic.
		/// </summary>
		public virtual bool IsDynamicConversion {
			get { return false; }
		}
		
		/// <summary>
		/// Gets whether the conversion is a reference conversion.
		/// </summary>
		public virtual bool IsReferenceConversion {
			get { return false; }
		}
		
		/// <summary>
		/// Gets whether the conversion is an enumeration conversion.
		/// </summary>
		public virtual bool IsEnumerationConversion {
			get { return false; }
		}
		
		/// <summary>
		/// Gets whether the conversion is a nullable conversion
		/// (conversion between a nullable type and the regular type).
		/// </summary>
		public virtual bool IsNullableConversion {
			get { return false; }
		}
		
		/// <summary>
		/// Gets whether this conversion is user-defined (op_Implicit or op_Explicit).
		/// </summary>
		public virtual bool IsUserDefined {
			get { return false; }
		}

		/// <summary>
		/// The conversion that is applied to the input before the user-defined conversion operator is invoked.
		/// </summary>
		public virtual Conversion ConversionBeforeUserDefinedOperator {
			get { return null; }
		}
		
		/// <summary>
		/// The conversion that is applied to the result of the user-defined conversion operator.
		/// </summary>
		public virtual Conversion ConversionAfterUserDefinedOperator {
			get { return null; }
		}

		/// <summary>
		/// Gets whether this conversion is a boxing conversion.
		/// </summary>
		public virtual bool IsBoxingConversion {
			get { return false; }
		}
		
		/// <summary>
		/// Gets whether this conversion is an unboxing conversion.
		/// </summary>
		public virtual bool IsUnboxingConversion {
			get { return false; }
		}
		
		/// <summary>
		/// Gets whether this conversion is a pointer conversion.
		/// </summary>
		public virtual bool IsPointerConversion {
			get { return false; }
		}
		
		/// <summary>
		/// Gets whether this conversion is a method group conversion.
		/// </summary>
		public virtual bool IsMethodGroupConversion {
			get { return false; }
		}
		
		/// <summary>
		/// For method-group conversions, gets whether to perform a virtual method lookup at runtime.
		/// </summary>
		public virtual bool IsVirtualMethodLookup {
			get { return false; }
		}
		
		/// <summary>
		/// For method-group conversions, gets whether the conversion captures the first argument.
		/// 
		/// For instance methods, this property always returns true for C# method-group conversions.
		/// For static methods, this property returns true for method-group conversions of an extension method performed on an instance (eg. <c>Func&lt;int&gt; f = myEnumerable.Single</c>).
		/// </summary>
		public virtual bool DelegateCapturesFirstArgument {
			get { return false; }
		}

		/// <summary>
		/// Gets whether this conversion is an anonymous function conversion.
		/// </summary>
		public virtual bool IsAnonymousFunctionConversion {
			get { return false; }
		}
		
		/// <summary>
		/// Gets the method associated with this conversion.
		/// For user-defined conversions, this is the method being called.
		/// For method-group conversions, this is the method that was chosen from the group.
		/// </summary>
		public virtual IMethod Method {
			get { return null; }
		}
		
		public override sealed bool Equals(object obj)
		{
			return Equals(obj as Conversion);
		}
		
		public override int GetHashCode()
		{
			return base.GetHashCode();
		}
		
		public virtual bool Equals(Conversion other)
		{
			return this == other;
		}
	}
}
