//
// constant.cs: Constants.
//
// Author:
//   Miguel de Icaza (miguel@ximian.com)
//   Marek Safar (marek.safar@seznam.cz)
//
// Copyright 2001-2003 Ximian, Inc.
// Copyright 2003-2008 Novell, Inc.
//

using System;
using System.Globalization;

#if STATIC
using IKVM.Reflection.Emit;
#else
using System.Reflection.Emit;
#endif

namespace Mono.CSharp {

	/// <summary>
	///   Base class for constants and literals.
	/// </summary>
	public abstract class Constant : Expression
	{
		static readonly NumberFormatInfo nfi = CultureInfo.InvariantCulture.NumberFormat;

		protected Constant (Location loc)
		{
			this.loc = loc;
		}

		override public string ToString ()
		{
			return this.GetType ().Name + " (" + GetValueAsLiteral () + ")";
		}

		/// <summary>
		///  This is used to obtain the actual value of the literal
		///  cast into an object.
		/// </summary>
		public abstract object GetValue ();

		public abstract long GetValueAsLong ();

		public abstract string GetValueAsLiteral ();

#if !STATIC
		//
		// Returns an object value which is typed to contant type
		//
		public virtual object GetTypedValue ()
		{
			return GetValue ();
		}
#endif

		public override void Error_ValueCannotBeConverted (ResolveContext ec, Location loc, TypeSpec target, bool expl)
		{
			if (!expl && IsLiteral && 
				BuiltinTypeSpec.IsPrimitiveTypeOrDecimal (target) &&
				BuiltinTypeSpec.IsPrimitiveTypeOrDecimal (type)) {
				ec.Report.Error (31, loc, "Constant value `{0}' cannot be converted to a `{1}'",
					GetValueAsLiteral (), TypeManager.CSharpName (target));
			} else {
				base.Error_ValueCannotBeConverted (ec, loc, target, expl);
			}
		}

		public Constant ImplicitConversionRequired (ResolveContext ec, TypeSpec type, Location loc)
		{
			Constant c = ConvertImplicitly (type);
			if (c == null)
				Error_ValueCannotBeConverted (ec, loc, type, false);

			return c;
		}

		public virtual Constant ConvertImplicitly (TypeSpec type)
		{
			if (this.type == type)
				return this;

			if (Convert.ImplicitNumericConversion (this, type) == null) 
				return null;

			bool fail;			
			object constant_value = ChangeType (GetValue (), type, out fail);
			if (fail){
				//
				// We should always catch the error before this is ever
				// reached, by calling Convert.ImplicitStandardConversionExists
				//
				throw new InternalErrorException ("Missing constant conversion between `{0}' and `{1}'",
				  TypeManager.CSharpName (Type), TypeManager.CSharpName (type));
			}

			return CreateConstant (type, constant_value, loc);
		}

		//
		//  Returns a constant instance based on Type
		//
		public static Constant CreateConstant (TypeSpec t, object v, Location loc)
		{
			return CreateConstantFromValue (t, v, loc);
		}

		public static Constant CreateConstantFromValue (TypeSpec t, object v, Location loc)
		{
			switch (t.BuiltinType) {
			case BuiltinTypeSpec.Type.Int:
				return new IntConstant (t, (int) v, loc);
			case BuiltinTypeSpec.Type.String:
				return new StringConstant (t, (string) v, loc);
			case BuiltinTypeSpec.Type.UInt:
				return new UIntConstant (t, (uint) v, loc);
			case BuiltinTypeSpec.Type.Long:
				return new LongConstant (t, (long) v, loc);
			case BuiltinTypeSpec.Type.ULong:
				return new ULongConstant (t, (ulong) v, loc);
			case BuiltinTypeSpec.Type.Float:
				return new FloatConstant (t, (float) v, loc);
			case BuiltinTypeSpec.Type.Double:
				return new DoubleConstant (t, (double) v, loc);
			case BuiltinTypeSpec.Type.Short:
				return new ShortConstant (t, (short) v, loc);
			case BuiltinTypeSpec.Type.UShort:
				return new UShortConstant (t, (ushort) v, loc);
			case BuiltinTypeSpec.Type.SByte:
				return new SByteConstant (t, (sbyte) v, loc);
			case BuiltinTypeSpec.Type.Byte:
				return new ByteConstant (t, (byte) v, loc);
			case BuiltinTypeSpec.Type.Char:
				return new CharConstant (t, (char) v, loc);
			case BuiltinTypeSpec.Type.Bool:
				return new BoolConstant (t, (bool) v, loc);
			case BuiltinTypeSpec.Type.Decimal:
				return new DecimalConstant (t, (decimal) v, loc);
			}

			if (t.IsEnum) {
				var real_type = EnumSpec.GetUnderlyingType (t);
				return new EnumConstant (CreateConstantFromValue (real_type, v, loc), t);
			}

			if (v == null) {
				if (t.IsNullableType)
					return Nullable.LiftedNull.Create (t, loc);

				if (TypeSpec.IsReferenceType (t))
					return new NullConstant (t, loc);
			}

			throw new InternalErrorException ("Constant value `{0}' has unexpected underlying type `{1}'",
				v, TypeManager.CSharpName (t));
		}

		public override Expression CreateExpressionTree (ResolveContext ec)
		{
			Arguments args = new Arguments (2);
			args.Add (new Argument (this));
			args.Add (new Argument (new TypeOf (type, loc)));

			return CreateExpressionFactoryCall (ec, "Constant", args);
		}

		/// <summary>
		/// Maybe ConvertTo name is better. It tries to convert `this' constant to target_type.
		/// It throws OverflowException 
		/// </summary>
		// DON'T CALL THIS METHOD DIRECTLY AS IT DOES NOT HANDLE ENUMS
		public abstract Constant ConvertExplicitly (bool in_checked_context, TypeSpec target_type);

		// This is a custom version of Convert.ChangeType() which works
		// with the TypeBuilder defined types when compiling corlib.
		static object ChangeType (object value, TypeSpec targetType, out bool error)
		{
			IConvertible convert_value = value as IConvertible;

			if (convert_value == null) {
				error = true;
				return null;
			}

			//
			// We cannot rely on build-in type conversions as they are
			// more limited than what C# supports.
			// See char -> float/decimal/double conversion
			//
			error = false;
			try {
				switch (targetType.BuiltinType) {
				case BuiltinTypeSpec.Type.Bool:
					return convert_value.ToBoolean (nfi);
				case BuiltinTypeSpec.Type.Byte:
					return convert_value.ToByte (nfi);
				case BuiltinTypeSpec.Type.Char:
					return convert_value.ToChar (nfi);
				case BuiltinTypeSpec.Type.Short:
					return convert_value.ToInt16 (nfi);
				case BuiltinTypeSpec.Type.Int:
					return convert_value.ToInt32 (nfi);
				case BuiltinTypeSpec.Type.Long:
					return convert_value.ToInt64 (nfi);
				case BuiltinTypeSpec.Type.SByte:
					return convert_value.ToSByte (nfi);
				case BuiltinTypeSpec.Type.Decimal:
					if (convert_value.GetType () == typeof (char))
						return (decimal) convert_value.ToInt32 (nfi);
					return convert_value.ToDecimal (nfi);
				case BuiltinTypeSpec.Type.Double:
					if (convert_value.GetType () == typeof (char))
						return (double) convert_value.ToInt32 (nfi);
					return convert_value.ToDouble (nfi);
				case BuiltinTypeSpec.Type.Float:
					if (convert_value.GetType () == typeof (char))
						return (float) convert_value.ToInt32 (nfi);
					return convert_value.ToSingle (nfi);
				case BuiltinTypeSpec.Type.String:
					return convert_value.ToString (nfi);
				case BuiltinTypeSpec.Type.UShort:
					return convert_value.ToUInt16 (nfi);
				case BuiltinTypeSpec.Type.UInt:
					return convert_value.ToUInt32 (nfi);
				case BuiltinTypeSpec.Type.ULong:
					return convert_value.ToUInt64 (nfi);
				case BuiltinTypeSpec.Type.Object:
					return value;
				}
			} catch {
			}

			error = true;
			return null;
		}

		protected override Expression DoResolve (ResolveContext rc)
		{
			return this;
		}

		/// <summary>
		///   Attempts to do a compile-time folding of a constant cast.
		/// </summary>
		public Constant TryReduce (ResolveContext ec, TypeSpec target_type, Location loc)
		{
			try {
				return TryReduce (ec, target_type);
			}
			catch (OverflowException) {
				if (ec.ConstantCheckState && Type.BuiltinType != BuiltinTypeSpec.Type.Decimal) {
					ec.Report.Error (221, loc,
						"Constant value `{0}' cannot be converted to a `{1}' (use `unchecked' syntax to override)",
						GetValueAsLiteral (), target_type.GetSignatureForError ());
				} else {
					Error_ValueCannotBeConverted (ec, loc, target_type, false);
				}

				return New.Constantify (target_type, loc);
			}
		}

		Constant TryReduce (ResolveContext ec, TypeSpec target_type)
		{
			if (Type == target_type)
				return this;

			Constant c;
			if (TypeManager.IsEnumType (target_type)) {
				c = TryReduce (ec, EnumSpec.GetUnderlyingType (target_type));
				if (c == null)
					return null;

				return new EnumConstant (c, target_type);
			}

			return ConvertExplicitly (ec.ConstantCheckState, target_type);
		}

		/// <summary>
		/// Need to pass type as the constant can require a boxing
		/// and in such case no optimization is possible
		/// </summary>
		public bool IsDefaultInitializer (TypeSpec type)
		{
			if (type == Type)
				return IsDefaultValue;

			return this is NullLiteral;
		}

		public abstract bool IsDefaultValue {
			get;
		}

		public abstract bool IsNegative {
			get;
		}

		//
		// When constant is declared as literal
		//
		public virtual bool IsLiteral {
			get { return false; }
		}
		
		public virtual bool IsOneInteger {
			get { return false; }
		}		

		//
		// Returns true iff 1) the stack type of this is one of Object, 
		// int32, int64 and 2) this == 0 or this == null.
		//
		public virtual bool IsZeroInteger {
			get { return false; }
		}

		public override void EmitSideEffect (EmitContext ec)
		{
			// do nothing
		}

		public sealed override Expression Clone (CloneContext clonectx)
		{
			// No cloning is not needed for constants
			return this;
		}

		protected override void CloneTo (CloneContext clonectx, Expression target)
		{
			throw new NotSupportedException ("should not be reached");
		}

		public override System.Linq.Expressions.Expression MakeExpression (BuilderContext ctx)
		{
#if STATIC
			return base.MakeExpression (ctx);
#else
			return System.Linq.Expressions.Expression.Constant (GetTypedValue (), type.GetMetaInfo ());
#endif
		}

		public new bool Resolve (ResolveContext rc)
		{
			// It exists only as hint not to call Resolve on constants
			return true;
		}
		
		public override object Accept (StructuralVisitor visitor)
		{
			return visitor.Visit (this);
		}

	}

	public abstract class IntegralConstant : Constant
	{
		protected IntegralConstant (TypeSpec type, Location loc)
			: base (loc)
		{
			this.type = type;
			eclass = ExprClass.Value;
		}

		public override void Error_ValueCannotBeConverted (ResolveContext ec, Location loc, TypeSpec target, bool expl)
		{
			try {
				ConvertExplicitly (true, target);
				base.Error_ValueCannotBeConverted (ec, loc, target, expl);
			}
			catch
			{
				ec.Report.Error (31, loc, "Constant value `{0}' cannot be converted to a `{1}'",
					GetValue ().ToString (), TypeManager.CSharpName (target));
			}
		}

		public override string GetValueAsLiteral ()
		{
			return GetValue ().ToString ();
		}
		
		public abstract Constant Increment ();
	}
	
	public class BoolConstant : Constant {
		public readonly bool Value;

		public BoolConstant (BuiltinTypes types, bool val, Location loc)
			: this (types.Bool, val, loc)
		{
		}
		
		public BoolConstant (TypeSpec type, bool val, Location loc)
			: base (loc)
		{
			eclass = ExprClass.Value;
			this.type = type;

			Value = val;
		}

		public override object GetValue ()
		{
			return (object) Value;
		}

		public override string GetValueAsLiteral ()
		{
			return Value ? "true" : "false";
		}

		public override long GetValueAsLong ()
		{
			return Value ? 1 : 0;
		}

		public override void EncodeAttributeValue (IMemberContext rc, AttributeEncoder enc, TypeSpec targetType)
		{
			enc.Encode (Value);
		}
		
		public override void Emit (EmitContext ec)
		{
			if (Value)
				ec.Emit (OpCodes.Ldc_I4_1);
			else
				ec.Emit (OpCodes.Ldc_I4_0);
		}

		public override bool IsDefaultValue {
			get {
				return !Value;
			}
		}

		public override bool IsNegative {
			get {
				return false;
			}
		}
	
		public override bool IsZeroInteger {
			get { return Value == false; }
		}

		public override Constant ConvertExplicitly (bool in_checked_context, TypeSpec target_type)
		{
			return null;
		}

	}

	public class ByteConstant : IntegralConstant
	{
		public readonly byte Value;

		public ByteConstant (BuiltinTypes types, byte v, Location loc)
			: this (types.Byte, v, loc)
		{
		}

		public ByteConstant (TypeSpec type, byte v, Location loc)
			: base (type, loc)
		{
			Value = v;
		}

		public override void EncodeAttributeValue (IMemberContext rc, AttributeEncoder enc, TypeSpec targetType)
		{
			enc.Encode (Value);
		}

		public override void Emit (EmitContext ec)
		{
			ec.EmitInt (Value);
		}

		public override object GetValue ()
		{
			return Value;
		}

		public override long GetValueAsLong ()
		{
			return Value;
		}

		public override Constant Increment ()
		{
			return new ByteConstant (type, checked ((byte)(Value + 1)), loc);
		}

		public override bool IsDefaultValue {
			get {
				return Value == 0;
			}
		}

		public override bool IsOneInteger {
			get {
				return Value == 1;
			}
		}		

		public override bool IsNegative {
			get {
				return false;
			}
		}

		public override bool IsZeroInteger {
			get { return Value == 0; }
		}

		public override Constant ConvertExplicitly (bool in_checked_context, TypeSpec target_type)
		{
			switch (target_type.BuiltinType) {
			case BuiltinTypeSpec.Type.SByte:
				if (in_checked_context){
					if (Value > SByte.MaxValue)
						throw new OverflowException ();
				}
				return new SByteConstant (target_type, (sbyte) Value, Location);
			case BuiltinTypeSpec.Type.Short:
				return new ShortConstant (target_type, (short) Value, Location);
			case BuiltinTypeSpec.Type.UShort:
				return new UShortConstant (target_type, (ushort) Value, Location);
			case BuiltinTypeSpec.Type.Int:
				return new IntConstant (target_type, (int) Value, Location);
			case BuiltinTypeSpec.Type.UInt:
				return new UIntConstant (target_type, (uint) Value, Location);
			case BuiltinTypeSpec.Type.Long:
				return new LongConstant (target_type, (long) Value, Location);
			case BuiltinTypeSpec.Type.ULong:
				return new ULongConstant (target_type, (ulong) Value, Location);
			case BuiltinTypeSpec.Type.Float:
				return new FloatConstant (target_type, (float) Value, Location);
			case BuiltinTypeSpec.Type.Double:
				return new DoubleConstant (target_type, (double) Value, Location);
			case BuiltinTypeSpec.Type.Char:
				return new CharConstant (target_type, (char) Value, Location);
			case BuiltinTypeSpec.Type.Decimal:
				return new DecimalConstant (target_type, (decimal) Value, Location);
			}

			return null;
		}

	}

	public class CharConstant : Constant {
		public readonly char Value;

		public CharConstant (BuiltinTypes types, char v, Location loc)
			: this (types.Char, v, loc)
		{
		}

		public CharConstant (TypeSpec type, char v, Location loc)
			: base (loc)
		{
			this.type = type;
			eclass = ExprClass.Value;

			Value = v;
		}

		public override void EncodeAttributeValue (IMemberContext rc, AttributeEncoder enc, TypeSpec targetType)
		{
			enc.Encode ((ushort) Value);
		}

		public override void Emit (EmitContext ec)
		{
			ec.EmitInt (Value);
		}

		static string descape (char c)
		{
			switch (c){
			case '\a':
				return "\\a"; 
			case '\b':
				return "\\b"; 
			case '\n':
				return "\\n"; 
			case '\t':
				return "\\t"; 
			case '\v':
				return "\\v"; 
			case '\r':
				return "\\r"; 
			case '\\':
				return "\\\\";
			case '\f':
				return "\\f"; 
			case '\0':
				return "\\0"; 
			case '"':
				return "\\\""; 
			case '\'':
				return "\\\'"; 
			}
			return c.ToString ();
		}

		public override object GetValue ()
		{
			return Value;
		}

		public override long GetValueAsLong ()
		{
			return Value;
		}

		public override string GetValueAsLiteral ()
		{
			return "\"" + descape (Value) + "\"";
		}

		public override bool IsDefaultValue {
			get {
				return Value == 0;
			}
		}

		public override bool IsNegative {
			get {
				return false;
			}
		}

		public override bool IsZeroInteger {
			get { return Value == '\0'; }
		}

		public override Constant ConvertExplicitly (bool in_checked_context, TypeSpec target_type)
		{
			switch (target_type.BuiltinType) {
			case BuiltinTypeSpec.Type.Byte:
				if (in_checked_context) {
					if (Value < Byte.MinValue || Value > Byte.MaxValue)
						throw new OverflowException ();
				}
				return new ByteConstant (target_type, (byte) Value, Location);
			case BuiltinTypeSpec.Type.SByte:
				if (in_checked_context) {
					if (Value > SByte.MaxValue)
						throw new OverflowException ();
				}
				return new SByteConstant (target_type, (sbyte) Value, Location);

			case BuiltinTypeSpec.Type.Short:
				if (in_checked_context) {
					if (Value > Int16.MaxValue)
						throw new OverflowException ();
				}
				return new ShortConstant (target_type, (short) Value, Location);
			case BuiltinTypeSpec.Type.Int:
				return new IntConstant (target_type, (int) Value, Location);
			case BuiltinTypeSpec.Type.UInt:
				return new UIntConstant (target_type, (uint) Value, Location);
			case BuiltinTypeSpec.Type.Long:
				return new LongConstant (target_type, (long) Value, Location);
			case BuiltinTypeSpec.Type.ULong:
				return new ULongConstant (target_type, (ulong) Value, Location);
			case BuiltinTypeSpec.Type.Float:
				return new FloatConstant (target_type, (float) Value, Location);
			case BuiltinTypeSpec.Type.Double:
				return new DoubleConstant (target_type, (double) Value, Location);
			case BuiltinTypeSpec.Type.Decimal:
				return new DecimalConstant (target_type, (decimal) Value, Location);
			}

			return null;
		}

	}

	public class SByteConstant : IntegralConstant
	{
		public readonly sbyte Value;

		public SByteConstant (BuiltinTypes types, sbyte v, Location loc)
			: this (types.SByte, v, loc)
		{
		}

		public SByteConstant (TypeSpec type, sbyte v, Location loc)
			: base (type, loc)
		{
			Value = v;
		}

		public override void EncodeAttributeValue (IMemberContext rc, AttributeEncoder enc, TypeSpec targetType)
		{
			enc.Encode (Value);
		}

		public override void Emit (EmitContext ec)
		{
			ec.EmitInt (Value);
		}

		public override object GetValue ()
		{
			return Value;
		}

		public override long GetValueAsLong ()
		{
			return Value;
		}

		public override Constant Increment ()
		{
		    return new SByteConstant (type, checked((sbyte)(Value + 1)), loc);
		}

		public override bool IsDefaultValue {
			get {
				return Value == 0;
			}
		}

		public override bool IsNegative {
			get {
				return Value < 0;
			}
		}
		
		public override bool IsOneInteger {
			get {
				return Value == 1;
			}
		}		
		
		public override bool IsZeroInteger {
			get { return Value == 0; }
		}

		public override Constant ConvertExplicitly (bool in_checked_context, TypeSpec target_type)
		{
			switch (target_type.BuiltinType) {
			case BuiltinTypeSpec.Type.Byte:
				if (in_checked_context && Value < 0)
					throw new OverflowException ();
				return new ByteConstant (target_type, (byte) Value, Location);
			case BuiltinTypeSpec.Type.Short:
				return new ShortConstant (target_type, (short) Value, Location);
			case BuiltinTypeSpec.Type.UShort:
				if (in_checked_context && Value < 0)
					throw new OverflowException ();
				return new UShortConstant (target_type, (ushort) Value, Location);
			case BuiltinTypeSpec.Type.Int:
				return new IntConstant (target_type, (int) Value, Location);
			case BuiltinTypeSpec.Type.UInt:
				if (in_checked_context && Value < 0)
					throw new OverflowException ();
				return new UIntConstant (target_type, (uint) Value, Location);
			case BuiltinTypeSpec.Type.Long:
				return new LongConstant (target_type, (long) Value, Location);
			case BuiltinTypeSpec.Type.ULong:
				if (in_checked_context && Value < 0)
					throw new OverflowException ();
				return new ULongConstant (target_type, (ulong) Value, Location);
			case BuiltinTypeSpec.Type.Float:
				return new FloatConstant (target_type, (float) Value, Location);
			case BuiltinTypeSpec.Type.Double:
				return new DoubleConstant (target_type, (double) Value, Location);
			case BuiltinTypeSpec.Type.Char:
				if (in_checked_context && Value < 0)
					throw new OverflowException ();
				return new CharConstant (target_type, (char) Value, Location);
			case BuiltinTypeSpec.Type.Decimal:
				return new DecimalConstant (target_type, (decimal) Value, Location);
			}

			return null;
		}

	}

	public class ShortConstant : IntegralConstant {
		public readonly short Value;

		public ShortConstant (BuiltinTypes types, short v, Location loc)
			: this (types.Short, v, loc)
		{
		}

		public ShortConstant (TypeSpec type, short v, Location loc)
			: base (type, loc)
		{
			Value = v;
		}

		public override void EncodeAttributeValue (IMemberContext rc, AttributeEncoder enc, TypeSpec targetType)
		{
			enc.Encode (Value);
		}

		public override void Emit (EmitContext ec)
		{
			ec.EmitInt (Value);
		}

		public override object GetValue ()
		{
			return Value;
		}

		public override long GetValueAsLong ()
		{
			return Value;
		}

		public override Constant Increment ()
		{
			return new ShortConstant (type, checked((short)(Value + 1)), loc);
		}

		public override bool IsDefaultValue {
			get {
				return Value == 0;
			}
		}
		
		public override bool IsZeroInteger {
			get { return Value == 0; }
		}

		public override bool IsNegative {
			get {
				return Value < 0;
			}
		}
		
		public override bool IsOneInteger {
			get {
				return Value == 1;
			}
		}		

		public override Constant ConvertExplicitly (bool in_checked_context, TypeSpec target_type)
		{
			switch (target_type.BuiltinType) {
			case BuiltinTypeSpec.Type.Byte:
				if (in_checked_context) {
					if (Value < Byte.MinValue || Value > Byte.MaxValue)
						throw new OverflowException ();
				}
				return new ByteConstant (target_type, (byte) Value, Location);
			case BuiltinTypeSpec.Type.SByte:
				if (in_checked_context) {
					if (Value < SByte.MinValue || Value > SByte.MaxValue)
						throw new OverflowException ();
				}
				return new SByteConstant (target_type, (sbyte) Value, Location);
			case BuiltinTypeSpec.Type.UShort:
				if (in_checked_context && Value < 0)
					throw new OverflowException ();

				return new UShortConstant (target_type, (ushort) Value, Location);
			case BuiltinTypeSpec.Type.Int:
				return new IntConstant (target_type, (int) Value, Location);
			case BuiltinTypeSpec.Type.UInt:
				if (in_checked_context && Value < 0)
					throw new OverflowException ();
				return new UIntConstant (target_type, (uint) Value, Location);
			case BuiltinTypeSpec.Type.Long:
				return new LongConstant (target_type, (long) Value, Location);
			case BuiltinTypeSpec.Type.ULong:
				if (in_checked_context && Value < 0)
					throw new OverflowException ();
				return new ULongConstant (target_type, (ulong) Value, Location);
			case BuiltinTypeSpec.Type.Float:
				return new FloatConstant (target_type, (float) Value, Location);
			case BuiltinTypeSpec.Type.Double:
				return new DoubleConstant (target_type, (double) Value, Location);
			case BuiltinTypeSpec.Type.Char:
				if (in_checked_context) {
					if (Value < Char.MinValue)
						throw new OverflowException ();
				}
				return new CharConstant (target_type, (char) Value, Location);
			case BuiltinTypeSpec.Type.Decimal:
				return new DecimalConstant (target_type, (decimal) Value, Location);
			}

			return null;
		}

	}

	public class UShortConstant : IntegralConstant
	{
		public readonly ushort Value;

		public UShortConstant (BuiltinTypes types, ushort v, Location loc)
			: this (types.UShort, v, loc)
		{
		}

		public UShortConstant (TypeSpec type, ushort v, Location loc)
			: base (type, loc)
		{
			Value = v;
		}

		public override void EncodeAttributeValue (IMemberContext rc, AttributeEncoder enc, TypeSpec targetType)
		{
			enc.Encode (Value);
		}

		public override void Emit (EmitContext ec)
		{
			ec.EmitInt (Value);
		}

		public override object GetValue ()
		{
			return Value;
		}

		public override long GetValueAsLong ()
		{
			return Value;
		}
	
		public override Constant Increment ()
		{
			return new UShortConstant (type, checked((ushort)(Value + 1)), loc);
		}

		public override bool IsDefaultValue {
			get {
				return Value == 0;
			}
		}

		public override bool IsNegative {
			get {
				return false;
			}
		}
		
		public override bool IsOneInteger {
			get {
				return Value == 1;
			}
		}		
	
		public override bool IsZeroInteger {
			get { return Value == 0; }
		}

		public override Constant ConvertExplicitly (bool in_checked_context, TypeSpec target_type)
		{
			switch (target_type.BuiltinType) {
			case BuiltinTypeSpec.Type.Byte:
				if (in_checked_context) {
					if (Value > Byte.MaxValue)
						throw new OverflowException ();
				}
				return new ByteConstant (target_type, (byte) Value, Location);
			case BuiltinTypeSpec.Type.SByte:
				if (in_checked_context) {
					if (Value > SByte.MaxValue)
						throw new OverflowException ();
				}
				return new SByteConstant (target_type, (sbyte) Value, Location);
			case BuiltinTypeSpec.Type.Short:
				if (in_checked_context) {
					if (Value > Int16.MaxValue)
						throw new OverflowException ();
				}
				return new ShortConstant (target_type, (short) Value, Location);
			case BuiltinTypeSpec.Type.Int:
				return new IntConstant (target_type, (int) Value, Location);
			case BuiltinTypeSpec.Type.UInt:
				return new UIntConstant (target_type, (uint) Value, Location);
			case BuiltinTypeSpec.Type.Long:
				return new LongConstant (target_type, (long) Value, Location);
			case BuiltinTypeSpec.Type.ULong:
				return new ULongConstant (target_type, (ulong) Value, Location);
			case BuiltinTypeSpec.Type.Float:
				return new FloatConstant (target_type, (float) Value, Location);
			case BuiltinTypeSpec.Type.Double:
				return new DoubleConstant (target_type, (double) Value, Location);
			case BuiltinTypeSpec.Type.Char:
				if (in_checked_context) {
					if (Value > Char.MaxValue)
						throw new OverflowException ();
				}
				return new CharConstant (target_type, (char) Value, Location);
			case BuiltinTypeSpec.Type.Decimal:
				return new DecimalConstant (target_type, (decimal) Value, Location);
			}

			return null;
		}
	}

	public class IntConstant : IntegralConstant
	{
		public readonly int Value;

		public IntConstant (BuiltinTypes types, int v, Location loc)
			: this (types.Int, v, loc)
		{
		}

		public IntConstant (TypeSpec type, int v, Location loc)
			: base (type, loc)
		{
			Value = v;
		}

		public override void EncodeAttributeValue (IMemberContext rc, AttributeEncoder enc, TypeSpec targetType)
		{
			enc.Encode (Value);
		}

		public override void Emit (EmitContext ec)
		{
			ec.EmitInt (Value);
		}

		public override object GetValue ()
		{
			return Value;
		}

		public override long GetValueAsLong ()
		{
			return Value;
		}

		public override Constant Increment ()
		{
			return new IntConstant (type, checked(Value + 1), loc);
		}

		public override bool IsDefaultValue {
			get {
				return Value == 0;
			}
		}
		
		public override bool IsNegative {
			get {
				return Value < 0;
			}
		}
		
		public override bool IsOneInteger {
			get {
				return Value == 1;
			}
		}		

		public override bool IsZeroInteger {
			get { return Value == 0; }
		}

		public override Constant ConvertExplicitly (bool in_checked_context, TypeSpec target_type)
		{
			switch (target_type.BuiltinType) {
			case BuiltinTypeSpec.Type.Byte:
				if (in_checked_context) {
					if (Value < Byte.MinValue || Value > Byte.MaxValue)
						throw new OverflowException ();
				}
				return new ByteConstant (target_type, (byte) Value, Location);
			case BuiltinTypeSpec.Type.SByte:
				if (in_checked_context) {
					if (Value < SByte.MinValue || Value > SByte.MaxValue)
						throw new OverflowException ();
				}
				return new SByteConstant (target_type, (sbyte) Value, Location);
			case BuiltinTypeSpec.Type.Short:
				if (in_checked_context) {
					if (Value < Int16.MinValue || Value > Int16.MaxValue)
						throw new OverflowException ();
				}
				return new ShortConstant (target_type, (short) Value, Location);
			case BuiltinTypeSpec.Type.UShort:
				if (in_checked_context) {
					if (Value < UInt16.MinValue || Value > UInt16.MaxValue)
						throw new OverflowException ();
				}
				return new UShortConstant (target_type, (ushort) Value, Location);
			case BuiltinTypeSpec.Type.UInt:
				if (in_checked_context) {
					if (Value < UInt32.MinValue)
						throw new OverflowException ();
				}
				return new UIntConstant (target_type, (uint) Value, Location);
			case BuiltinTypeSpec.Type.Long:
				return new LongConstant (target_type, (long) Value, Location);
			case BuiltinTypeSpec.Type.ULong:
				if (in_checked_context && Value < 0)
					throw new OverflowException ();
				return new ULongConstant (target_type, (ulong) Value, Location);
			case BuiltinTypeSpec.Type.Float:
				return new FloatConstant (target_type, (float) Value, Location);
			case BuiltinTypeSpec.Type.Double:
				return new DoubleConstant (target_type, (double) Value, Location);
			case BuiltinTypeSpec.Type.Char:
				if (in_checked_context) {
					if (Value < Char.MinValue || Value > Char.MaxValue)
						throw new OverflowException ();
				}
				return new CharConstant (target_type, (char) Value, Location);
			case BuiltinTypeSpec.Type.Decimal:
				return new DecimalConstant (target_type, (decimal) Value, Location);
			}

			return null;
		}

		public override Constant ConvertImplicitly (TypeSpec type)
		{
			if (this.type == type)
				return this;

			Constant c = TryImplicitIntConversion (type);
			if (c != null)
				return c; //.Resolve (rc);

			return base.ConvertImplicitly (type);
		}

		/// <summary>
		///   Attempts to perform an implicit constant conversion of the IntConstant
		///   into a different data type using casts (See Implicit Constant
		///   Expression Conversions)
		/// </summary>
		Constant TryImplicitIntConversion (TypeSpec target_type)
		{
			switch (target_type.BuiltinType) {
			case BuiltinTypeSpec.Type.SByte:
				if (Value >= SByte.MinValue && Value <= SByte.MaxValue)
					return new SByteConstant (target_type, (sbyte) Value, loc);
				break;
			case BuiltinTypeSpec.Type.Byte:
				if (Value >= Byte.MinValue && Value <= Byte.MaxValue)
					return new ByteConstant (target_type, (byte) Value, loc);
				break;
			case BuiltinTypeSpec.Type.Short:
				if (Value >= Int16.MinValue && Value <= Int16.MaxValue)
					return new ShortConstant (target_type, (short) Value, loc);
				break;
			case BuiltinTypeSpec.Type.UShort:
				if (Value >= UInt16.MinValue && Value <= UInt16.MaxValue)
					return new UShortConstant (target_type, (ushort) Value, loc);
				break;
			case BuiltinTypeSpec.Type.UInt:
				if (Value >= 0)
					return new UIntConstant (target_type, (uint) Value, loc);
				break;
			case BuiltinTypeSpec.Type.ULong:
				//
				// we can optimize this case: a positive int32
				// always fits on a uint64.  But we need an opcode
				// to do it.
				//
				if (Value >= 0)
					return new ULongConstant (target_type, (ulong) Value, loc);
				break;
			case BuiltinTypeSpec.Type.Double:
				return new DoubleConstant (target_type, (double) Value, loc);
			case BuiltinTypeSpec.Type.Float:
				return new FloatConstant (target_type, (float) Value, loc);
			}

			return null;
		}
	}

	public class UIntConstant : IntegralConstant {
		public readonly uint Value;

		public UIntConstant (BuiltinTypes types, uint v, Location loc)
			: this (types.UInt, v, loc)
		{
		}

		public UIntConstant (TypeSpec type, uint v, Location loc)
			: base (type, loc)
		{
			Value = v;
		}

		public override void EncodeAttributeValue (IMemberContext rc, AttributeEncoder enc, TypeSpec targetType)
		{
			enc.Encode (Value);
		}

		public override void Emit (EmitContext ec)
		{
			ec.EmitInt (unchecked ((int) Value));
		}

		public override object GetValue ()
		{
			return Value;
		}

		public override long GetValueAsLong ()
		{
			return Value;
		}

		public override Constant Increment ()
		{
			return new UIntConstant (type, checked(Value + 1), loc);
		}
	
		public override bool IsDefaultValue {
			get {
				return Value == 0;
			}
		}

		public override bool IsNegative {
			get {
				return false;
			}
		}
		
		public override bool IsOneInteger {
			get {
				return Value == 1;
			}
		}		

		public override bool IsZeroInteger {
			get { return Value == 0; }
		}

		public override Constant ConvertExplicitly (bool in_checked_context, TypeSpec target_type)
		{
			switch (target_type.BuiltinType) {
			case BuiltinTypeSpec.Type.Byte:
				if (in_checked_context) {
					if (Value < 0 || Value > byte.MaxValue)
						throw new OverflowException ();
				}
				return new ByteConstant (target_type, (byte) Value, Location);
			case BuiltinTypeSpec.Type.SByte:
				if (in_checked_context) {
					if (Value > SByte.MaxValue)
						throw new OverflowException ();
				}
				return new SByteConstant (target_type, (sbyte) Value, Location);
			case BuiltinTypeSpec.Type.Short:
				if (in_checked_context) {
					if (Value > Int16.MaxValue)
						throw new OverflowException ();
				}
				return new ShortConstant (target_type, (short) Value, Location);
			case BuiltinTypeSpec.Type.UShort:
				if (in_checked_context) {
					if (Value < UInt16.MinValue || Value > UInt16.MaxValue)
						throw new OverflowException ();
				}
				return new UShortConstant (target_type, (ushort) Value, Location);
			case BuiltinTypeSpec.Type.Int:
				if (in_checked_context) {
					if (Value > Int32.MaxValue)
						throw new OverflowException ();
				}
				return new IntConstant (target_type, (int) Value, Location);
			case BuiltinTypeSpec.Type.Long:
				return new LongConstant (target_type, (long) Value, Location);
			case BuiltinTypeSpec.Type.ULong:
				return new ULongConstant (target_type, (ulong) Value, Location);
			case BuiltinTypeSpec.Type.Float:
				return new FloatConstant (target_type, (float) Value, Location);
			case BuiltinTypeSpec.Type.Double:
				return new DoubleConstant (target_type, (double) Value, Location);
			case BuiltinTypeSpec.Type.Char:
				if (in_checked_context) {
					if (Value < Char.MinValue || Value > Char.MaxValue)
						throw new OverflowException ();
				}
				return new CharConstant (target_type, (char) Value, Location);
			case BuiltinTypeSpec.Type.Decimal:
				return new DecimalConstant (target_type, (decimal) Value, Location);
			}

			return null;
		}

	}

	public class LongConstant : IntegralConstant {
		public readonly long Value;

		public LongConstant (BuiltinTypes types, long v, Location loc)
			: this (types.Long, v, loc)
		{
		}

		public LongConstant (TypeSpec type, long v, Location loc)
			: base (type, loc)
		{
			Value = v;
		}

		public override void EncodeAttributeValue (IMemberContext rc, AttributeEncoder enc, TypeSpec targetType)
		{
			enc.Encode (Value);
		}

		public override void Emit (EmitContext ec)
		{
			ec.EmitLong (Value);
		}

		public override object GetValue ()
		{
			return Value;
		}

		public override long GetValueAsLong ()
		{
			return Value;
		}

		public override Constant Increment ()
		{
			return new LongConstant (type, checked(Value + 1), loc);
		}
		
		public override bool IsDefaultValue {
			get {
				return Value == 0;
			}
		}

		public override bool IsNegative {
			get {
				return Value < 0;
			}
		}
		
		public override bool IsOneInteger {
			get {
				return Value == 1;
			}
		}		

		public override bool IsZeroInteger {
			get { return Value == 0; }
		}

		public override Constant ConvertExplicitly (bool in_checked_context, TypeSpec target_type)
		{
			switch (target_type.BuiltinType) {
			case BuiltinTypeSpec.Type.Byte:
				if (in_checked_context) {
					if (Value < Byte.MinValue || Value > Byte.MaxValue)
						throw new OverflowException ();
				}
				return new ByteConstant (target_type, (byte) Value, Location);
			case BuiltinTypeSpec.Type.SByte:
				if (in_checked_context) {
					if (Value < SByte.MinValue || Value > SByte.MaxValue)
						throw new OverflowException ();
				}
				return new SByteConstant (target_type, (sbyte) Value, Location);
			case BuiltinTypeSpec.Type.Short:
				if (in_checked_context) {
					if (Value < Int16.MinValue || Value > Int16.MaxValue)
						throw new OverflowException ();
				}
				return new ShortConstant (target_type, (short) Value, Location);
			case BuiltinTypeSpec.Type.UShort:
				if (in_checked_context) {
					if (Value < UInt16.MinValue || Value > UInt16.MaxValue)
						throw new OverflowException ();
				}
				return new UShortConstant (target_type, (ushort) Value, Location);
			case BuiltinTypeSpec.Type.Int:
				if (in_checked_context) {
					if (Value < Int32.MinValue || Value > Int32.MaxValue)
						throw new OverflowException ();
				}
				return new IntConstant (target_type, (int) Value, Location);
			case BuiltinTypeSpec.Type.UInt:
				if (in_checked_context) {
					if (Value < UInt32.MinValue || Value > UInt32.MaxValue)
						throw new OverflowException ();
				}
				return new UIntConstant (target_type, (uint) Value, Location);
			case BuiltinTypeSpec.Type.ULong:
				if (in_checked_context && Value < 0)
					throw new OverflowException ();
				return new ULongConstant (target_type, (ulong) Value, Location);
			case BuiltinTypeSpec.Type.Float:
				return new FloatConstant (target_type, (float) Value, Location);
			case BuiltinTypeSpec.Type.Double:
				return new DoubleConstant (target_type, (double) Value, Location);
			case BuiltinTypeSpec.Type.Char:
				if (in_checked_context) {
					if (Value < Char.MinValue || Value > Char.MaxValue)
						throw new OverflowException ();
				}
				return new CharConstant (target_type, (char) Value, Location);
			case BuiltinTypeSpec.Type.Decimal:
				return new DecimalConstant (target_type, (decimal) Value, Location);
			}

			return null;
		}

		public override Constant ConvertImplicitly (TypeSpec type)
		{
			if (Value >= 0 && type.BuiltinType == BuiltinTypeSpec.Type.ULong) {
				return new ULongConstant (type, (ulong) Value, loc);
			}

			return base.ConvertImplicitly (type);
		}
	}

	public class ULongConstant : IntegralConstant {
		public readonly ulong Value;

		public ULongConstant (BuiltinTypes types, ulong v, Location loc)
			: this (types.ULong, v, loc)
		{
		}

		public ULongConstant (TypeSpec type, ulong v, Location loc)
			: base (type, loc)
		{
			Value = v;
		}

		public override void EncodeAttributeValue (IMemberContext rc, AttributeEncoder enc, TypeSpec targetType)
		{
			enc.Encode (Value);
		}

		public override void Emit (EmitContext ec)
		{
			ec.EmitLong (unchecked ((long) Value));
		}

		public override object GetValue ()
		{
			return Value;
		}

		public override long GetValueAsLong ()
		{
			return (long) Value;
		}

		public override Constant Increment ()
		{
			return new ULongConstant (type, checked(Value + 1), loc);
		}

		public override bool IsDefaultValue {
			get {
				return Value == 0;
			}
		}

		public override bool IsNegative {
			get {
				return false;
			}
		}
		
		public override bool IsOneInteger {
			get {
				return Value == 1;
			}
		}		

		public override bool IsZeroInteger {
			get { return Value == 0; }
		}

		public override Constant ConvertExplicitly (bool in_checked_context, TypeSpec target_type)
		{
			switch (target_type.BuiltinType) {
			case BuiltinTypeSpec.Type.Byte:
				if (in_checked_context && Value > Byte.MaxValue)
					throw new OverflowException ();
				return new ByteConstant (target_type, (byte) Value, Location);
			case BuiltinTypeSpec.Type.SByte:
				if (in_checked_context && Value > ((ulong) SByte.MaxValue))
					throw new OverflowException ();
				return new SByteConstant (target_type, (sbyte) Value, Location);
			case BuiltinTypeSpec.Type.Short:
				if (in_checked_context && Value > ((ulong) Int16.MaxValue))
					throw new OverflowException ();
				return new ShortConstant (target_type, (short) Value, Location);
			case BuiltinTypeSpec.Type.UShort:
				if (in_checked_context && Value > UInt16.MaxValue)
					throw new OverflowException ();
				return new UShortConstant (target_type, (ushort) Value, Location);
			case BuiltinTypeSpec.Type.Int:
				if (in_checked_context && Value > UInt32.MaxValue)
					throw new OverflowException ();
				return new IntConstant (target_type, (int) Value, Location);
			case BuiltinTypeSpec.Type.UInt:
				if (in_checked_context && Value > UInt32.MaxValue)
					throw new OverflowException ();
				return new UIntConstant (target_type, (uint) Value, Location);
			case BuiltinTypeSpec.Type.Long:
				if (in_checked_context && Value > Int64.MaxValue)
					throw new OverflowException ();
				return new LongConstant (target_type, (long) Value, Location);
			case BuiltinTypeSpec.Type.Float:
				return new FloatConstant (target_type, (float) Value, Location);
			case BuiltinTypeSpec.Type.Double:
				return new DoubleConstant (target_type, (double) Value, Location);
			case BuiltinTypeSpec.Type.Char:
				if (in_checked_context && Value > Char.MaxValue)
					throw new OverflowException ();
				return new CharConstant (target_type, (char) Value, Location);
			case BuiltinTypeSpec.Type.Decimal:
				return new DecimalConstant (target_type, (decimal) Value, Location);
			}

			return null;
		}

	}

	public class FloatConstant : Constant {
		public readonly float Value;

		public FloatConstant (BuiltinTypes types, float v, Location loc)
			: this (types.Float, v, loc)
		{
		}

		public FloatConstant (TypeSpec type, float v, Location loc)
			: base (loc)
		{
			this.type = type;
			eclass = ExprClass.Value;

			Value = v;
		}

		public override void EncodeAttributeValue (IMemberContext rc, AttributeEncoder enc, TypeSpec targetType)
		{
			enc.Encode (Value);
		}

		public override void Emit (EmitContext ec)
		{
			ec.Emit (OpCodes.Ldc_R4, Value);
		}

		public override object GetValue ()
		{
			return Value;
		}

		public override string GetValueAsLiteral ()
		{
			return Value.ToString ();
		}

		public override long GetValueAsLong ()
		{
			throw new NotSupportedException ();
		}

		public override bool IsDefaultValue {
			get {
				return Value == 0;
			}
		}

		public override bool IsNegative {
			get {
				return Value < 0;
			}
		}

		public override Constant ConvertExplicitly (bool in_checked_context, TypeSpec target_type)
		{
			switch (target_type.BuiltinType) {
			case BuiltinTypeSpec.Type.Byte:
				if (in_checked_context) {
					if (Value < byte.MinValue || Value > byte.MaxValue || float.IsNaN (Value))
						throw new OverflowException ();
				}
				return new ByteConstant (target_type, (byte) Value, Location);
			case BuiltinTypeSpec.Type.SByte:
				if (in_checked_context) {
					if (Value < sbyte.MinValue || Value > sbyte.MaxValue || float.IsNaN (Value))
						throw new OverflowException ();
				}
				return new SByteConstant (target_type, (sbyte) Value, Location);
			case BuiltinTypeSpec.Type.Short:
				if (in_checked_context) {
					if (Value < short.MinValue || Value > short.MaxValue || float.IsNaN (Value))
						throw new OverflowException ();
				}
				return new ShortConstant (target_type, (short) Value, Location);
			case BuiltinTypeSpec.Type.UShort:
				if (in_checked_context) {
					if (Value < ushort.MinValue || Value > ushort.MaxValue || float.IsNaN (Value))
						throw new OverflowException ();
				}
				return new UShortConstant (target_type, (ushort) Value, Location);
			case BuiltinTypeSpec.Type.Int:
				if (in_checked_context) {
					if (Value < int.MinValue || Value > int.MaxValue || float.IsNaN (Value))
						throw new OverflowException ();
				}
				return new IntConstant (target_type, (int) Value, Location);
			case BuiltinTypeSpec.Type.UInt:
				if (in_checked_context) {
					if (Value < uint.MinValue || Value > uint.MaxValue || float.IsNaN (Value))
						throw new OverflowException ();
				}
				return new UIntConstant (target_type, (uint) Value, Location);
			case BuiltinTypeSpec.Type.Long:
				if (in_checked_context) {
					if (Value < long.MinValue || Value > long.MaxValue || float.IsNaN (Value))
						throw new OverflowException ();
				}
				return new LongConstant (target_type, (long) Value, Location);
			case BuiltinTypeSpec.Type.ULong:
				if (in_checked_context) {
					if (Value < ulong.MinValue || Value > ulong.MaxValue || float.IsNaN (Value))
						throw new OverflowException ();
				}
				return new ULongConstant (target_type, (ulong) Value, Location);
			case BuiltinTypeSpec.Type.Double:
				return new DoubleConstant (target_type, (double) Value, Location);
			case BuiltinTypeSpec.Type.Char:
				if (in_checked_context) {
					if (Value < (float) char.MinValue || Value > (float) char.MaxValue || float.IsNaN (Value))
						throw new OverflowException ();
				}
				return new CharConstant (target_type, (char) Value, Location);
			case BuiltinTypeSpec.Type.Decimal:
				return new DecimalConstant (target_type, (decimal) Value, Location);
			}

			return null;
		}

	}

	public class DoubleConstant : Constant
	{
		public readonly double Value;

		public DoubleConstant (BuiltinTypes types, double v, Location loc)
			: this (types.Double, v, loc)
		{
		}

		public DoubleConstant (TypeSpec type, double v, Location loc)
			: base (loc)
		{
			this.type = type;
			eclass = ExprClass.Value;

			Value = v;
		}

		public override void EncodeAttributeValue (IMemberContext rc, AttributeEncoder enc, TypeSpec targetType)
		{
			enc.Encode (Value);
		}

		public override void Emit (EmitContext ec)
		{
			ec.Emit (OpCodes.Ldc_R8, Value);
		}

		public override object GetValue ()
		{
			return Value;
		}

		public override string GetValueAsLiteral ()
		{
			return Value.ToString ();
		}

		public override long GetValueAsLong ()
		{
			throw new NotSupportedException ();
		}

		public override bool IsDefaultValue {
			get {
				return Value == 0;
			}
		}

		public override bool IsNegative {
			get {
				return Value < 0;
			}
		}

		public override Constant ConvertExplicitly (bool in_checked_context, TypeSpec target_type)
		{
			switch (target_type.BuiltinType) {
			case BuiltinTypeSpec.Type.Byte:
				if (in_checked_context) {
					if (Value < Byte.MinValue || Value > Byte.MaxValue || double.IsNaN (Value))
						throw new OverflowException ();
				}
				return new ByteConstant (target_type, (byte) Value, Location);
			case BuiltinTypeSpec.Type.SByte:
				if (in_checked_context) {
					if (Value < SByte.MinValue || Value > SByte.MaxValue || double.IsNaN (Value))
						throw new OverflowException ();
				}
				return new SByteConstant (target_type, (sbyte) Value, Location);
			case BuiltinTypeSpec.Type.Short:
				if (in_checked_context) {
					if (Value < short.MinValue || Value > short.MaxValue || double.IsNaN (Value))
						throw new OverflowException ();
				}
				return new ShortConstant (target_type, (short) Value, Location);
			case BuiltinTypeSpec.Type.UShort:
				if (in_checked_context) {
					if (Value < ushort.MinValue || Value > ushort.MaxValue || double.IsNaN (Value))
						throw new OverflowException ();
				}
				return new UShortConstant (target_type, (ushort) Value, Location);
			case BuiltinTypeSpec.Type.Int:
				if (in_checked_context) {
					if (Value < int.MinValue || Value > int.MaxValue || double.IsNaN (Value))
						throw new OverflowException ();
				}
				return new IntConstant (target_type, (int) Value, Location);
			case BuiltinTypeSpec.Type.UInt:
				if (in_checked_context) {
					if (Value < uint.MinValue || Value > uint.MaxValue || double.IsNaN (Value))
						throw new OverflowException ();
				}
				return new UIntConstant (target_type, (uint) Value, Location);
			case BuiltinTypeSpec.Type.Long:
				if (in_checked_context) {
					if (Value < long.MinValue || Value > long.MaxValue || double.IsNaN (Value))
						throw new OverflowException ();
				}
				return new LongConstant (target_type, (long) Value, Location);
			case BuiltinTypeSpec.Type.ULong:
				if (in_checked_context) {
					if (Value < ulong.MinValue || Value > ulong.MaxValue || double.IsNaN (Value))
						throw new OverflowException ();
				}
				return new ULongConstant (target_type, (ulong) Value, Location);
			case BuiltinTypeSpec.Type.Float:
				return new FloatConstant (target_type, (float) Value, Location);
			case BuiltinTypeSpec.Type.Char:
				if (in_checked_context) {
					if (Value < (double) char.MinValue || Value > (double) char.MaxValue || double.IsNaN (Value))
						throw new OverflowException ();
				}
				return new CharConstant (target_type, (char) Value, Location);
			case BuiltinTypeSpec.Type.Decimal:
				return new DecimalConstant (target_type, (decimal) Value, Location);
			}

			return null;
		}

	}

	public class DecimalConstant : Constant {
		public readonly decimal Value;

		public DecimalConstant (BuiltinTypes types, decimal d, Location loc)
			: this (types.Decimal, d, loc)
		{
		}

		public DecimalConstant (TypeSpec type, decimal d, Location loc)
			: base (loc)
		{
			this.type = type;
			eclass = ExprClass.Value;

			Value = d;
		}

		public override void Emit (EmitContext ec)
		{
			MethodSpec m;

			int [] words = decimal.GetBits (Value);
			int power = (words [3] >> 16) & 0xff;

			if (power == 0) {
				if (Value <= int.MaxValue && Value >= int.MinValue) {
					m = ec.Module.PredefinedMembers.DecimalCtorInt.Resolve (loc);
					if (m == null) {
						return;
					}

					ec.EmitInt ((int) Value);
					ec.Emit (OpCodes.Newobj, m);
					return;
				}

				if (Value <= long.MaxValue && Value >= long.MinValue) {
					m = ec.Module.PredefinedMembers.DecimalCtorLong.Resolve (loc);
					if (m == null) {
						return;
					}

					ec.EmitLong ((long) Value);
					ec.Emit (OpCodes.Newobj, m);
					return;
				}
			}

			ec.EmitInt (words [0]);
			ec.EmitInt (words [1]);
			ec.EmitInt (words [2]);

			// sign
			ec.EmitInt (words [3] >> 31);

			// power
			ec.EmitInt (power);

			m = ec.Module.PredefinedMembers.DecimalCtor.Resolve (loc);
			if (m != null) {
				ec.Emit (OpCodes.Newobj, m);
			}
		}

		public override bool IsDefaultValue {
			get {
				return Value == 0;
			}
		}

		public override bool IsNegative {
			get {
				return Value < 0;
			}
		}

		public override Constant ConvertExplicitly (bool in_checked_context, TypeSpec target_type)
		{
			switch (target_type.BuiltinType) {
			case BuiltinTypeSpec.Type.SByte:
				return new SByteConstant (target_type, (sbyte) Value, loc);
			case BuiltinTypeSpec.Type.Byte:
				return new ByteConstant (target_type, (byte) Value, loc);
			case BuiltinTypeSpec.Type.Short:
				return new ShortConstant (target_type, (short) Value, loc);
			case BuiltinTypeSpec.Type.UShort:
				return new UShortConstant (target_type, (ushort) Value, loc);
			case BuiltinTypeSpec.Type.Int:
				return new IntConstant (target_type, (int) Value, loc);
			case BuiltinTypeSpec.Type.UInt:
				return new UIntConstant (target_type, (uint) Value, loc);
			case BuiltinTypeSpec.Type.Long:
				return new LongConstant (target_type, (long) Value, loc);
			case BuiltinTypeSpec.Type.ULong:
				return new ULongConstant (target_type, (ulong) Value, loc);
			case BuiltinTypeSpec.Type.Char:
				return new CharConstant (target_type, (char) Value, loc);
			case BuiltinTypeSpec.Type.Float:
				return new FloatConstant (target_type, (float) Value, loc);
			case BuiltinTypeSpec.Type.Double:
				return new DoubleConstant (target_type, (double) Value, loc);
			}

			return null;
		}

		public override object GetValue ()
		{
			return Value;
		}

		public override string GetValueAsLiteral ()
		{
			return Value.ToString () + "M";
		}

		public override long GetValueAsLong ()
		{
			throw new NotSupportedException ();
		}
	}

	public class StringConstant : Constant {
		public readonly string Value;

		public StringConstant (BuiltinTypes types, string s, Location loc)
			: this (types.String, s, loc)
		{
		}

		public StringConstant (TypeSpec type, string s, Location loc)
			: base (loc)
		{
			this.type = type;
			eclass = ExprClass.Value;

			Value = s;
		}

		public override object GetValue ()
		{
			return Value;
		}

		public override string GetValueAsLiteral ()
		{
			// FIXME: Escape the string.
			return "\"" + Value + "\"";
		}

		public override long GetValueAsLong ()
		{
			throw new NotSupportedException ();
		}
		
		public override void Emit (EmitContext ec)
		{
			if (Value == null) {
				ec.Emit (OpCodes.Ldnull);
				return;
			}

			//
			// Use string.Empty for both literals and constants even if
			// it's not allowed at language level
			//
			if (Value.Length == 0 && ec.Module.Compiler.Settings.Optimize) {
				var string_type = ec.BuiltinTypes.String;
				if (ec.CurrentType != string_type) {
					var m = ec.Module.PredefinedMembers.StringEmpty.Get ();
					if (m != null) {
						ec.Emit (OpCodes.Ldsfld, m);
						return;
					}
				}
			}

			ec.Emit (OpCodes.Ldstr, Value);
		}

		public override void EncodeAttributeValue (IMemberContext rc, AttributeEncoder enc, TypeSpec targetType)
		{
			// cast to object
			if (type != targetType)
				enc.Encode (type);

			enc.Encode (Value);
		}

		public override bool IsDefaultValue {
			get {
				return Value == null;
			}
		}

		public override bool IsNegative {
			get {
				return false;
			}
		}

		public override bool IsNull {
			get {
				return IsDefaultValue;
			}
		}

		public override Constant ConvertExplicitly (bool in_checked_context, TypeSpec target_type)
		{
			return null;
		}
	}

	//
	// Null constant can have its own type, think of `default (Foo)'
	//
	public class NullConstant : Constant
	{
		public NullConstant (TypeSpec type, Location loc)
			: base (loc)
		{
			eclass = ExprClass.Value;
			this.type = type;
		}

		public override Expression CreateExpressionTree (ResolveContext ec)
		{
			if (type == InternalType.NullLiteral || type.BuiltinType == BuiltinTypeSpec.Type.Object) {
				// Optimized version, also avoids referencing literal internal type
				Arguments args = new Arguments (1);
				args.Add (new Argument (this));
				return CreateExpressionFactoryCall (ec, "Constant", args);
			}

			return base.CreateExpressionTree (ec);
		}

		public override void EncodeAttributeValue (IMemberContext rc, AttributeEncoder enc, TypeSpec targetType)
		{
			switch (targetType.BuiltinType) {
			case BuiltinTypeSpec.Type.Object:
				// Type it as string cast
				enc.Encode (rc.Module.Compiler.BuiltinTypes.String);
				goto case BuiltinTypeSpec.Type.String;
			case BuiltinTypeSpec.Type.String:
			case BuiltinTypeSpec.Type.Type:
				enc.Encode (byte.MaxValue);
				return;
			default:
				var ac = targetType as ArrayContainer;
				if (ac != null && ac.Rank == 1 && !ac.Element.IsArray) {
					enc.Encode (uint.MaxValue);
					return;
				}

				break;
			}

			base.EncodeAttributeValue (rc, enc, targetType);
		}

		public override void Emit (EmitContext ec)
		{
			ec.Emit (OpCodes.Ldnull);

			// Only to make verifier happy
			if (type.IsGenericParameter)
				ec.Emit (OpCodes.Unbox_Any, type);
		}

		public override string ExprClassName {
			get {
				return GetSignatureForError ();
			}
		}

		public override Constant ConvertExplicitly (bool inCheckedContext, TypeSpec targetType)
		{
			if (targetType.IsPointer) {
				if (IsLiteral || this is NullPointer)
					return new NullPointer (targetType, loc);

				return null;
			}

			// Exlude internal compiler types
			if (targetType.Kind == MemberKind.InternalCompilerType && targetType.BuiltinType != BuiltinTypeSpec.Type.Dynamic)
				return null;

			if (!IsLiteral && !Convert.ImplicitStandardConversionExists (this, targetType))
				return null;

			if (TypeSpec.IsReferenceType (targetType))
				return new NullConstant (targetType, loc);

			if (targetType.IsNullableType)
				return Nullable.LiftedNull.Create (targetType, loc);

			return null;
		}

		public override Constant ConvertImplicitly (TypeSpec targetType)
		{
			return ConvertExplicitly (false, targetType);
		}

		public override string GetSignatureForError ()
		{
			return "null";
		}

		public override object GetValue ()
		{
			return null;
		}

		public override string GetValueAsLiteral ()
		{
			return GetSignatureForError ();
		}

		public override long GetValueAsLong ()
		{
			throw new NotSupportedException ();
		}

		public override bool IsDefaultValue {
			get { return true; }
		}

		public override bool IsNegative {
			get { return false; }
		}

		public override bool IsNull {
			get { return true; }
		}

		public override bool IsZeroInteger {
			get { return true; }
		}
	}


	//
	// A null constant in a pointer context
	//
	class NullPointer : NullConstant
	{
		public NullPointer (TypeSpec type, Location loc)
			: base (type, loc)
		{
		}

		public override Expression CreateExpressionTree (ResolveContext ec)
		{
			Error_PointerInsideExpressionTree (ec);
			return base.CreateExpressionTree (ec);
		}

		public override void Emit (EmitContext ec)
		{
			//
			// Emits null pointer
			//
			ec.Emit (OpCodes.Ldc_I4_0);
			ec.Emit (OpCodes.Conv_U);
		}
	}

	/// <summary>
	///   The value is constant, but when emitted has a side effect.  This is
	///   used by BitwiseAnd to ensure that the second expression is invoked
	///   regardless of the value of the left side.  
	/// </summary>
	public class SideEffectConstant : Constant {
		public readonly Constant value;
		Expression side_effect;
		
		public SideEffectConstant (Constant value, Expression side_effect, Location loc)
			: base (loc)
		{
			this.value = value;
			type = value.Type;
			eclass = ExprClass.Value;

			while (side_effect is SideEffectConstant)
				side_effect = ((SideEffectConstant) side_effect).side_effect;
			this.side_effect = side_effect;
		}

		public override object GetValue ()
		{
			return value.GetValue ();
		}

		public override string GetValueAsLiteral ()
		{
			return value.GetValueAsLiteral ();
		}

		public override long GetValueAsLong ()
		{
			return value.GetValueAsLong ();
		}

		public override void Emit (EmitContext ec)
		{
			side_effect.EmitSideEffect (ec);
			value.Emit (ec);
		}

		public override void EmitSideEffect (EmitContext ec)
		{
			side_effect.EmitSideEffect (ec);
			value.EmitSideEffect (ec);
		}

		public override bool IsDefaultValue {
			get { return value.IsDefaultValue; }
		}

		public override bool IsNegative {
			get { return value.IsNegative; }
		}

		public override bool IsZeroInteger {
			get { return value.IsZeroInteger; }
		}

		public override Constant ConvertExplicitly (bool in_checked_context, TypeSpec target_type)
		{
			Constant new_value = value.ConvertExplicitly (in_checked_context, target_type);
			if (new_value == null)
				return null;

			var c = new SideEffectConstant (new_value, side_effect, new_value.Location);
			c.type = target_type;
			c.eclass = eclass;
			return c;
		}
	}
}
