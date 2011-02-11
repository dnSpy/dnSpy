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

#if STATIC
using IKVM.Reflection.Emit;
#else
using System.Reflection.Emit;
#endif

namespace Mono.CSharp {

	/// <summary>
	///   Base class for constants and literals.
	/// </summary>
	public abstract class Constant : Expression {

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
				(TypeManager.IsPrimitiveType (target) || type == TypeManager.decimal_type) &&
				(TypeManager.IsPrimitiveType (type) || type == TypeManager.decimal_type)) {
				ec.Report.Error (31, loc, "Constant value `{0}' cannot be converted to a `{1}'",
					GetValueAsLiteral (), TypeManager.CSharpName (target));
			} else {
				base.Error_ValueCannotBeConverted (ec, loc, target, expl);
			}
		}

		public Constant ImplicitConversionRequired (ResolveContext ec, TypeSpec type, Location loc)
		{
			Constant c = ConvertImplicitly (ec, type);
			if (c == null)
				Error_ValueCannotBeConverted (ec, loc, type, false);

			return c;
		}

		public virtual Constant ConvertImplicitly (ResolveContext rc, TypeSpec type)
		{
			if (this.type == type)
				return this;

			if (Convert.ImplicitNumericConversion (this, type) == null) 
				return null;

			bool fail;			
			object constant_value = TypeManager.ChangeType (GetValue (), type, out fail);
			if (fail){
				//
				// We should always catch the error before this is ever
				// reached, by calling Convert.ImplicitStandardConversionExists
				//
				throw new InternalErrorException ("Missing constant conversion between `{0}' and `{1}'",
				  TypeManager.CSharpName (Type), TypeManager.CSharpName (type));
			}

			return CreateConstant (rc, type, constant_value, loc);
		}

		//
		//  Returns a constant instance based on Type
		//
		public static Constant CreateConstant (ResolveContext rc, TypeSpec t, object v, Location loc)
		{
			return CreateConstantFromValue (t, v, loc).Resolve (rc);
		}

		public static Constant CreateConstantFromValue (TypeSpec t, object v, Location loc)
		{
			if (t == TypeManager.int32_type)
				return new IntConstant ((int) v, loc);
			if (t == TypeManager.string_type)
				return new StringConstant ((string) v, loc);
			if (t == TypeManager.uint32_type)
				return new UIntConstant ((uint) v, loc);
			if (t == TypeManager.int64_type)
				return new LongConstant ((long) v, loc);
			if (t == TypeManager.uint64_type)
				return new ULongConstant ((ulong) v, loc);
			if (t == TypeManager.float_type)
				return new FloatConstant ((float) v, loc);
			if (t == TypeManager.double_type)
				return new DoubleConstant ((double) v, loc);
			if (t == TypeManager.short_type)
				return new ShortConstant ((short)v, loc);
			if (t == TypeManager.ushort_type)
				return new UShortConstant ((ushort)v, loc);
			if (t == TypeManager.sbyte_type)
				return new SByteConstant ((sbyte)v, loc);
			if (t == TypeManager.byte_type)
				return new ByteConstant ((byte)v, loc);
			if (t == TypeManager.char_type)
				return new CharConstant ((char)v, loc);
			if (t == TypeManager.bool_type)
				return new BoolConstant ((bool) v, loc);
			if (t == TypeManager.decimal_type)
				return new DecimalConstant ((decimal) v, loc);
			if (TypeManager.IsEnumType (t)) {
				var real_type = EnumSpec.GetUnderlyingType (t);
				return new EnumConstant (CreateConstantFromValue (real_type, v, loc).Resolve (null), t);
			}
			if (v == null) {
				if (TypeManager.IsNullableType (t))
					return Nullable.LiftedNull.Create (t, loc);

				if (TypeManager.IsReferenceType (t))
					return new NullConstant (t, loc);
			}

			throw new InternalErrorException ("Constant value `{0}' has unexpected underlying type `{1}'",
				v, TypeManager.CSharpName (t));
		}

		public override Expression CreateExpressionTree (ResolveContext ec)
		{
			Arguments args = new Arguments (2);
			args.Add (new Argument (this));
			args.Add (new Argument (new TypeOf (new TypeExpression (type, loc), loc)));

			return CreateExpressionFactoryCall (ec, "Constant", args);
		}


		/// <summary>
		/// Maybe ConvertTo name is better. It tries to convert `this' constant to target_type.
		/// It throws OverflowException 
		/// </summary>
		// DON'T CALL THIS METHOD DIRECTLY AS IT DOES NOT HANDLE ENUMS
		public abstract Constant ConvertExplicitly (bool in_checked_context, TypeSpec target_type);

		/// <summary>
		///   Attempts to do a compile-time folding of a constant cast.
		/// </summary>
		public Constant TryReduce (ResolveContext ec, TypeSpec target_type, Location loc)
		{
			try {
				return TryReduce (ec, target_type);
			}
			catch (OverflowException) {
				if (ec.ConstantCheckState && Type.BuildinType != BuildinTypeSpec.Type.Decimal) {
					ec.Report.Error (221, loc,
						"Constant value `{0}' cannot be converted to a `{1}' (use `unchecked' syntax to override)",
						GetValueAsLiteral (), target_type.GetSignatureForError ());
				} else {
					Error_ValueCannotBeConverted (ec, loc, target_type, false);
				}

				return New.Constantify (target_type, loc).Resolve (ec);
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

				return new EnumConstant (c, target_type).Resolve (ec);
			}

			c = ConvertExplicitly (ec.ConstantCheckState, target_type);
			if (c != null)
				c = c.Resolve (ec);

			return c;
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

		public new Constant Resolve (ResolveContext rc)
		{
			if (eclass != ExprClass.Unresolved)
				return this;

			// Resolved constant has to be still a constant
			Constant c = (Constant) DoResolve (rc);
			if (c == null)
				return null;

			if ((c.eclass & ExprClass.Value) == 0) {
				c.Error_UnexpectedKind (rc, ResolveFlags.VariableOrValue, loc);
				return null;
			}

			if (c.type == null)
				throw new InternalErrorException ("Expression `{0}' did not set its type after Resolve", c.GetType ());

			return c;
		}
		
		public override object Accept (StructuralVisitor visitor)
		{
			return visitor.Visit (this);
		}

	}

	public abstract class IntegralConstant : Constant
	{
		protected IntegralConstant (Location loc) :
			base (loc)
		{
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
		
		public BoolConstant (bool val, Location loc):
			base (loc)
		{
			Value = val;
		}

		protected override Expression DoResolve (ResolveContext ec)
		{
			type = TypeManager.bool_type;
			eclass = ExprClass.Value;
			return this;
		}

		public override object GetValue ()
		{
			return (object) Value;
		}

		public override string GetValueAsLiteral ()
		{
			return Value ? "true" : "false";
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

	public class ByteConstant : IntegralConstant {
		public readonly byte Value;

		public ByteConstant (byte v, Location loc):
			base (loc)
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

		protected override Expression DoResolve (ResolveContext ec)
		{
			type = TypeManager.byte_type;
			eclass = ExprClass.Value;
			return this;
		}

		public override object GetValue ()
		{
			return Value;
		}

		public override Constant Increment ()
		{
			return new ByteConstant (checked ((byte)(Value + 1)), loc);
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
			if (target_type == TypeManager.sbyte_type) {
				if (in_checked_context){
					if (Value > SByte.MaxValue)
						throw new OverflowException ();
				}
				return new SByteConstant ((sbyte) Value, Location);
			}
			if (target_type == TypeManager.short_type)
				return new ShortConstant ((short) Value, Location);
			if (target_type == TypeManager.ushort_type)
				return new UShortConstant ((ushort) Value, Location);
			if (target_type == TypeManager.int32_type)
				return new IntConstant ((int) Value, Location);
			if (target_type == TypeManager.uint32_type)
				return new UIntConstant ((uint) Value, Location);
			if (target_type == TypeManager.int64_type)
				return new LongConstant ((long) Value, Location);
			if (target_type == TypeManager.uint64_type)
				return new ULongConstant ((ulong) Value, Location);
			if (target_type == TypeManager.float_type)
				return new FloatConstant ((float) Value, Location);
			if (target_type == TypeManager.double_type)
				return new DoubleConstant ((double) Value, Location);
			if (target_type == TypeManager.char_type)
				return new CharConstant ((char) Value, Location);
			if (target_type == TypeManager.decimal_type)
				return new DecimalConstant ((decimal) Value, Location);

			return null;
		}

	}

	public class CharConstant : Constant {
		public readonly char Value;

		public CharConstant (char v, Location loc):
			base (loc)
		{
			Value = v;
		}

		protected override Expression DoResolve (ResolveContext rc)
		{
			type = TypeManager.char_type;
			eclass = ExprClass.Value;
			return this;
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
			if (target_type == TypeManager.byte_type) {
				if (in_checked_context){
					if (Value < Byte.MinValue || Value > Byte.MaxValue)
						throw new OverflowException ();
				}
				return new ByteConstant ((byte) Value, Location);
			}
			if (target_type == TypeManager.sbyte_type) {
				if (in_checked_context){
					if (Value > SByte.MaxValue)
						throw new OverflowException ();
				}
				return new SByteConstant ((sbyte) Value, Location);
			}
			if (target_type == TypeManager.short_type) {
				if (in_checked_context){
					if (Value > Int16.MaxValue)
						throw new OverflowException ();
				}					
				return new ShortConstant ((short) Value, Location);
			}
			if (target_type == TypeManager.int32_type)
				return new IntConstant ((int) Value, Location);
			if (target_type == TypeManager.uint32_type)
				return new UIntConstant ((uint) Value, Location);
			if (target_type == TypeManager.int64_type)
				return new LongConstant ((long) Value, Location);
			if (target_type == TypeManager.uint64_type)
				return new ULongConstant ((ulong) Value, Location);
			if (target_type == TypeManager.float_type)
				return new FloatConstant ((float) Value, Location);
			if (target_type == TypeManager.double_type)
				return new DoubleConstant ((double) Value, Location);
			if (target_type == TypeManager.decimal_type)
				return new DecimalConstant ((decimal) Value, Location);

			return null;
		}

	}

	public class SByteConstant : IntegralConstant {
		public readonly sbyte Value;

		public SByteConstant (sbyte v, Location loc):
			base (loc)
		{
			Value = v;
		}

		protected override Expression DoResolve (ResolveContext rc)
		{
			type = TypeManager.sbyte_type;
			eclass = ExprClass.Value;
			return this;
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

		public override Constant Increment ()
		{
		    return new SByteConstant (checked((sbyte)(Value + 1)), loc);
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
			if (target_type == TypeManager.byte_type) {
				if (in_checked_context && Value < 0)
					throw new OverflowException ();
				return new ByteConstant ((byte) Value, Location);
			}
			if (target_type == TypeManager.short_type)
				return new ShortConstant ((short) Value, Location);
			if (target_type == TypeManager.ushort_type) {
				if (in_checked_context && Value < 0)
					throw new OverflowException ();
				return new UShortConstant ((ushort) Value, Location);
			} if (target_type == TypeManager.int32_type)
				  return new IntConstant ((int) Value, Location);
			if (target_type == TypeManager.uint32_type) {
				if (in_checked_context && Value < 0)
					throw new OverflowException ();
				return new UIntConstant ((uint) Value, Location);
			} if (target_type == TypeManager.int64_type)
				  return new LongConstant ((long) Value, Location);
			if (target_type == TypeManager.uint64_type) {
				if (in_checked_context && Value < 0)
					throw new OverflowException ();
				return new ULongConstant ((ulong) Value, Location);
			}
			if (target_type == TypeManager.float_type)
				return new FloatConstant ((float) Value, Location);
			if (target_type == TypeManager.double_type)
				return new DoubleConstant ((double) Value, Location);
			if (target_type == TypeManager.char_type) {
				if (in_checked_context && Value < 0)
					throw new OverflowException ();
				return new CharConstant ((char) Value, Location);
			}
			if (target_type == TypeManager.decimal_type)
				return new DecimalConstant ((decimal) Value, Location);

			return null;
		}

	}

	public class ShortConstant : IntegralConstant {
		public readonly short Value;

		public ShortConstant (short v, Location loc):
			base (loc)
		{
			Value = v;
		}

		protected override Expression DoResolve (ResolveContext rc)
		{
			type = TypeManager.short_type;
			eclass = ExprClass.Value;
			return this;
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

		public override Constant Increment ()
		{
			return new ShortConstant (checked((short)(Value + 1)), loc);
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
			if (target_type == TypeManager.byte_type) {
				if (in_checked_context){
					if (Value < Byte.MinValue || Value > Byte.MaxValue)
						throw new OverflowException ();
				}
				return new ByteConstant ((byte) Value, Location);
			}
			if (target_type == TypeManager.sbyte_type) {
				if (in_checked_context){
					if (Value < SByte.MinValue || Value > SByte.MaxValue)
						throw new OverflowException ();
				}
				return new SByteConstant ((sbyte) Value, Location);
			}
			if (target_type == TypeManager.ushort_type) {
				if (in_checked_context && Value < 0)
					throw new OverflowException ();
				
				return new UShortConstant ((ushort) Value, Location);
			}
			if (target_type == TypeManager.int32_type)
				return new IntConstant ((int) Value, Location);
			if (target_type == TypeManager.uint32_type) {
				if (in_checked_context && Value < 0)
					throw new OverflowException ();
				return new UIntConstant ((uint) Value, Location);
			}
			if (target_type == TypeManager.int64_type)
				return new LongConstant ((long) Value, Location);
			if (target_type == TypeManager.uint64_type) {
				if (in_checked_context && Value < 0)
					throw new OverflowException ();
				return new ULongConstant ((ulong) Value, Location);
			}
			if (target_type == TypeManager.float_type)
				return new FloatConstant ((float) Value, Location);
			if (target_type == TypeManager.double_type)
				return new DoubleConstant ((double) Value, Location);
			if (target_type == TypeManager.char_type) {
				if (in_checked_context){
					if (Value < Char.MinValue)
						throw new OverflowException ();
				}
				return new CharConstant ((char) Value, Location);
			}
			if (target_type == TypeManager.decimal_type)
				return new DecimalConstant ((decimal) Value, Location);

			return null;
		}

	}

	public class UShortConstant : IntegralConstant {
		public readonly ushort Value;

		public UShortConstant (ushort v, Location loc):
			base (loc)
		{
			Value = v;
		}

		protected override Expression DoResolve (ResolveContext rc)
		{
			type = TypeManager.ushort_type;
			eclass = ExprClass.Value;
			return this;
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
	
		public override Constant Increment ()
		{
			return new UShortConstant (checked((ushort)(Value + 1)), loc);
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
			if (target_type == TypeManager.byte_type) {
				if (in_checked_context){
					if (Value > Byte.MaxValue)
						throw new OverflowException ();
				}
				return new ByteConstant ((byte) Value, Location);
			}
			if (target_type == TypeManager.sbyte_type) {
				if (in_checked_context){
					if (Value > SByte.MaxValue)
						throw new OverflowException ();
				}
				return new SByteConstant ((sbyte) Value, Location);
			}
			if (target_type == TypeManager.short_type) {
				if (in_checked_context){
					if (Value > Int16.MaxValue)
						throw new OverflowException ();
				}
				return new ShortConstant ((short) Value, Location);
			}
			if (target_type == TypeManager.int32_type)
				return new IntConstant ((int) Value, Location);
			if (target_type == TypeManager.uint32_type)
				return new UIntConstant ((uint) Value, Location);
			if (target_type == TypeManager.int64_type)
				return new LongConstant ((long) Value, Location);
			if (target_type == TypeManager.uint64_type)
				return new ULongConstant ((ulong) Value, Location);
			if (target_type == TypeManager.float_type)
				return new FloatConstant ((float) Value, Location);
			if (target_type == TypeManager.double_type)
				return new DoubleConstant ((double) Value, Location);
			if (target_type == TypeManager.char_type) {
				if (in_checked_context){
					if (Value > Char.MaxValue)
						throw new OverflowException ();
				}
				return new CharConstant ((char) Value, Location);
			}
			if (target_type == TypeManager.decimal_type)
				return new DecimalConstant ((decimal) Value, Location);

			return null;
		}
	}

	public class IntConstant : IntegralConstant {
		public readonly int Value;

		public IntConstant (int v, Location loc):
			base (loc)
		{
			Value = v;
		}

		protected override Expression DoResolve (ResolveContext rc)
		{
			type = TypeManager.int32_type;
			eclass = ExprClass.Value;
			return this;
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

		public override Constant Increment ()
		{
			return new IntConstant (checked(Value + 1), loc);
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
			if (target_type == TypeManager.byte_type) {
				if (in_checked_context){
					if (Value < Byte.MinValue || Value > Byte.MaxValue)
						throw new OverflowException ();
				}
				return new ByteConstant ((byte) Value, Location);
			}
			if (target_type == TypeManager.sbyte_type) {
				if (in_checked_context){
					if (Value < SByte.MinValue || Value > SByte.MaxValue)
						throw new OverflowException ();
				}
				return new SByteConstant ((sbyte) Value, Location);
			}
			if (target_type == TypeManager.short_type) {
				if (in_checked_context){
					if (Value < Int16.MinValue || Value > Int16.MaxValue)
						throw new OverflowException ();
				}
				return new ShortConstant ((short) Value, Location);
			}
			if (target_type == TypeManager.ushort_type) {
				if (in_checked_context){
					if (Value < UInt16.MinValue || Value > UInt16.MaxValue)
						throw new OverflowException ();
				}
				return new UShortConstant ((ushort) Value, Location);
			}
			if (target_type == TypeManager.uint32_type) {
				if (in_checked_context){
					if (Value < UInt32.MinValue)
						throw new OverflowException ();
				}
				return new UIntConstant ((uint) Value, Location);
			}
			if (target_type == TypeManager.int64_type)
				return new LongConstant ((long) Value, Location);
			if (target_type == TypeManager.uint64_type) {
				if (in_checked_context && Value < 0)
					throw new OverflowException ();
				return new ULongConstant ((ulong) Value, Location);
			}
			if (target_type == TypeManager.float_type)
				return new FloatConstant ((float) Value, Location);
			if (target_type == TypeManager.double_type)
				return new DoubleConstant ((double) Value, Location);
			if (target_type == TypeManager.char_type) {
				if (in_checked_context){
					if (Value < Char.MinValue || Value > Char.MaxValue)
						throw new OverflowException ();
				}
				return new CharConstant ((char) Value, Location);
			}
			if (target_type == TypeManager.decimal_type)
				return new DecimalConstant ((decimal) Value, Location);

			return null;
		}

		public override Constant ConvertImplicitly (ResolveContext rc, TypeSpec type)
		{
			if (this.type == type)
				return this;

			Constant c = TryImplicitIntConversion (type);
			if (c != null)
				return c.Resolve (rc);

			return base.ConvertImplicitly (rc, type);
		}

		/// <summary>
		///   Attempts to perform an implicit constant conversion of the IntConstant
		///   into a different data type using casts (See Implicit Constant
		///   Expression Conversions)
		/// </summary>
		Constant TryImplicitIntConversion (TypeSpec target_type)
		{
			if (target_type == TypeManager.sbyte_type) {
				if (Value >= SByte.MinValue && Value <= SByte.MaxValue)
					return new SByteConstant ((sbyte) Value, loc);
			} 
			else if (target_type == TypeManager.byte_type) {
				if (Value >= Byte.MinValue && Value <= Byte.MaxValue)
					return new ByteConstant ((byte) Value, loc);
			} 
			else if (target_type == TypeManager.short_type) {
				if (Value >= Int16.MinValue && Value <= Int16.MaxValue)
					return new ShortConstant ((short) Value, loc);
			} 
			else if (target_type == TypeManager.ushort_type) {
				if (Value >= UInt16.MinValue && Value <= UInt16.MaxValue)
					return new UShortConstant ((ushort) Value, loc);
			} 
			else if (target_type == TypeManager.uint32_type) {
				if (Value >= 0)
					return new UIntConstant ((uint) Value, loc);
			} 
			else if (target_type == TypeManager.uint64_type) {
				//
				// we can optimize this case: a positive int32
				// always fits on a uint64.  But we need an opcode
				// to do it.
				//
				if (Value >= 0)
					return new ULongConstant ((ulong) Value, loc);
			} 
			else if (target_type == TypeManager.double_type)
				return new DoubleConstant ((double) Value, loc);
			else if (target_type == TypeManager.float_type)
				return new FloatConstant ((float) Value, loc);

			return null;
		}
	}

	public class UIntConstant : IntegralConstant {
		public readonly uint Value;

		public UIntConstant (uint v, Location loc):
			base (loc)
		{
			Value = v;
		}

		protected override Expression DoResolve (ResolveContext rc)
		{
			type = TypeManager.uint32_type;
			eclass = ExprClass.Value;
			return this;
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

		public override Constant Increment ()
		{
			return new UIntConstant (checked(Value + 1), loc);
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
			if (target_type == TypeManager.byte_type) {
				if (in_checked_context){
					if (Value < 0 || Value > byte.MaxValue)
						throw new OverflowException ();
				}
				return new ByteConstant ((byte) Value, Location);
			}
			if (target_type == TypeManager.sbyte_type) {
				if (in_checked_context){
					if (Value > SByte.MaxValue)
						throw new OverflowException ();
				}
				return new SByteConstant ((sbyte) Value, Location);
			}
			if (target_type == TypeManager.short_type) {
				if (in_checked_context){
					if (Value > Int16.MaxValue)
						throw new OverflowException ();
				}
				return new ShortConstant ((short) Value, Location);
			}
			if (target_type == TypeManager.ushort_type) {
				if (in_checked_context){
					if (Value < UInt16.MinValue || Value > UInt16.MaxValue)
						throw new OverflowException ();
				}
				return new UShortConstant ((ushort) Value, Location);
			}
			if (target_type == TypeManager.int32_type) {
				if (in_checked_context){
					if (Value > Int32.MaxValue)
						throw new OverflowException ();
				}
				return new IntConstant ((int) Value, Location);
			}
			if (target_type == TypeManager.int64_type)
				return new LongConstant ((long) Value, Location);
			if (target_type == TypeManager.uint64_type)
				return new ULongConstant ((ulong) Value, Location);
			if (target_type == TypeManager.float_type)
				return new FloatConstant ((float) Value, Location);
			if (target_type == TypeManager.double_type)
				return new DoubleConstant ((double) Value, Location);
			if (target_type == TypeManager.char_type) {
				if (in_checked_context){
					if (Value < Char.MinValue || Value > Char.MaxValue)
						throw new OverflowException ();
				}
				return new CharConstant ((char) Value, Location);
			}
			if (target_type == TypeManager.decimal_type)
				return new DecimalConstant ((decimal) Value, Location);

			return null;
		}

	}

	public class LongConstant : IntegralConstant {
		public readonly long Value;

		public LongConstant (long v, Location loc):
			base (loc)
		{
			Value = v;
		}

		protected override Expression DoResolve (ResolveContext rc)
		{
			type = TypeManager.int64_type;
			eclass = ExprClass.Value;
			return this;
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

		public override Constant Increment ()
		{
			return new LongConstant (checked(Value + 1), loc);
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
			if (target_type == TypeManager.byte_type) {
				if (in_checked_context){
					if (Value < Byte.MinValue || Value > Byte.MaxValue)
						throw new OverflowException ();
				}
				return new ByteConstant ((byte) Value, Location);
			}
			if (target_type == TypeManager.sbyte_type) {
				if (in_checked_context){
					if (Value < SByte.MinValue || Value > SByte.MaxValue)
						throw new OverflowException ();
				}
				return new SByteConstant ((sbyte) Value, Location);
			}
			if (target_type == TypeManager.short_type) {
				if (in_checked_context){
					if (Value < Int16.MinValue || Value > Int16.MaxValue)
						throw new OverflowException ();
				}
				return new ShortConstant ((short) Value, Location);
			}
			if (target_type == TypeManager.ushort_type) {
				if (in_checked_context){
					if (Value < UInt16.MinValue || Value > UInt16.MaxValue)
						throw new OverflowException ();
				}
				return new UShortConstant ((ushort) Value, Location);
			}
			if (target_type == TypeManager.int32_type) {
				if (in_checked_context){
					if (Value < Int32.MinValue || Value > Int32.MaxValue)
						throw new OverflowException ();
				}
				return new IntConstant ((int) Value, Location);
			}
			if (target_type == TypeManager.uint32_type) {
				if (in_checked_context){
					if (Value < UInt32.MinValue || Value > UInt32.MaxValue)
						throw new OverflowException ();
				}
				return new UIntConstant ((uint) Value, Location);
			}
			if (target_type == TypeManager.uint64_type) {
				if (in_checked_context && Value < 0)
					throw new OverflowException ();
				return new ULongConstant ((ulong) Value, Location);
			}
			if (target_type == TypeManager.float_type)
				return new FloatConstant ((float) Value, Location);
			if (target_type == TypeManager.double_type)
				return new DoubleConstant ((double) Value, Location);
			if (target_type == TypeManager.char_type) {
				if (in_checked_context){
					if (Value < Char.MinValue || Value > Char.MaxValue)
						throw new OverflowException ();
				}
				return new CharConstant ((char) Value, Location);
			}
			if (target_type == TypeManager.decimal_type)
				return new DecimalConstant ((decimal) Value, Location);

			return null;
		}

		public override Constant ConvertImplicitly (ResolveContext rc, TypeSpec type)
		{
			if (Value >= 0 && type == TypeManager.uint64_type) {
				return new ULongConstant ((ulong) Value, loc).Resolve (rc);
			}

			return base.ConvertImplicitly (rc, type);
		}
	}

	public class ULongConstant : IntegralConstant {
		public readonly ulong Value;

		public ULongConstant (ulong v, Location loc):
			base (loc)
		{
			Value = v;
		}

		protected override Expression DoResolve (ResolveContext rc)
		{
			type = TypeManager.uint64_type;
			eclass = ExprClass.Value;
			return this;
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

		public override Constant Increment ()
		{
			return new ULongConstant (checked(Value + 1), loc);
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
			if (target_type == TypeManager.byte_type) {
				if (in_checked_context && Value > Byte.MaxValue)
					throw new OverflowException ();
				return new ByteConstant ((byte) Value, Location);
			}
			if (target_type == TypeManager.sbyte_type) {
				if (in_checked_context && Value > ((ulong) SByte.MaxValue))
					throw new OverflowException ();
				return new SByteConstant ((sbyte) Value, Location);
			}
			if (target_type == TypeManager.short_type) {
				if (in_checked_context && Value > ((ulong) Int16.MaxValue))
					throw new OverflowException ();
				return new ShortConstant ((short) Value, Location);
			}
			if (target_type == TypeManager.ushort_type) {
				if (in_checked_context && Value > UInt16.MaxValue)
					throw new OverflowException ();
				return new UShortConstant ((ushort) Value, Location);
			}
			if (target_type == TypeManager.int32_type) {
				if (in_checked_context && Value > UInt32.MaxValue)
					throw new OverflowException ();
				return new IntConstant ((int) Value, Location);
			}
			if (target_type == TypeManager.uint32_type) {
				if  (in_checked_context && Value > UInt32.MaxValue)
					throw new OverflowException ();
				return new UIntConstant ((uint) Value, Location);
			}
			if (target_type == TypeManager.int64_type) {
				if (in_checked_context && Value > Int64.MaxValue)
					throw new OverflowException ();
				return new LongConstant ((long) Value, Location);
			}
			if (target_type == TypeManager.float_type)
				return new FloatConstant ((float) Value, Location);
			if (target_type == TypeManager.double_type)
				return new DoubleConstant ((double) Value, Location);
			if (target_type == TypeManager.char_type) {
				if (in_checked_context && Value > Char.MaxValue)
					throw new OverflowException ();
				return new CharConstant ((char) Value, Location);
			}
			if (target_type == TypeManager.decimal_type)
				return new DecimalConstant ((decimal) Value, Location);

			return null;
		}

	}

	public class FloatConstant : Constant {
		public float Value;

		public FloatConstant (float v, Location loc):
			base (loc)
		{
			Value = v;
		}

		protected override Expression DoResolve (ResolveContext rc)
		{
			type = TypeManager.float_type;
			eclass = ExprClass.Value;
			return this;
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
			if (target_type == TypeManager.byte_type) {
				if (in_checked_context){
					if (Value < byte.MinValue || Value > byte.MaxValue || float.IsNaN (Value))
						throw new OverflowException ();
				}
				return new ByteConstant ((byte) Value, Location);
			}
			if (target_type == TypeManager.sbyte_type) {
				if (in_checked_context){
					if (Value < sbyte.MinValue || Value > sbyte.MaxValue || float.IsNaN (Value))
						throw new OverflowException ();
				}
				return new SByteConstant ((sbyte) Value, Location);
			}
			if (target_type == TypeManager.short_type) {
				if (in_checked_context){
					if (Value < short.MinValue || Value > short.MaxValue || float.IsNaN (Value))
						throw new OverflowException ();
				}
				return new ShortConstant ((short) Value, Location);
			}
			if (target_type == TypeManager.ushort_type) {
				if (in_checked_context){
					if (Value < ushort.MinValue || Value > ushort.MaxValue || float.IsNaN (Value))
						throw new OverflowException ();
				}
				return new UShortConstant ((ushort) Value, Location);
			}
			if (target_type == TypeManager.int32_type) {
				if (in_checked_context){
					if (Value < int.MinValue || Value > int.MaxValue || float.IsNaN (Value))
						throw new OverflowException ();
				}
				return new IntConstant ((int) Value, Location);
			}
			if (target_type == TypeManager.uint32_type) {
				if (in_checked_context){
					if (Value < uint.MinValue || Value > uint.MaxValue || float.IsNaN (Value))
						throw new OverflowException ();
				}
				return new UIntConstant ((uint) Value, Location);
			}
			if (target_type == TypeManager.int64_type) {
				if (in_checked_context){
					if (Value < long.MinValue || Value > long.MaxValue || float.IsNaN (Value))
						throw new OverflowException ();
				}
				return new LongConstant ((long) Value, Location);
			}
			if (target_type == TypeManager.uint64_type) {
				if (in_checked_context){
					if (Value < ulong.MinValue || Value > ulong.MaxValue || float.IsNaN (Value))
						throw new OverflowException ();
				}
				return new ULongConstant ((ulong) Value, Location);
			}
			if (target_type == TypeManager.double_type)
				return new DoubleConstant ((double) Value, Location);
			if (target_type == TypeManager.char_type) {
				if (in_checked_context){
					if (Value < (float) char.MinValue || Value > (float) char.MaxValue || float.IsNaN (Value))
						throw new OverflowException ();
				}
				return new CharConstant ((char) Value, Location);
			}
			if (target_type == TypeManager.decimal_type)
				return new DecimalConstant ((decimal) Value, Location);

			return null;
		}

	}

	public class DoubleConstant : Constant {
		public double Value;

		public DoubleConstant (double v, Location loc):
			base (loc)
		{
			Value = v;
		}

		protected override Expression DoResolve (ResolveContext rc)
		{
			type = TypeManager.double_type;
			eclass = ExprClass.Value;
			return this;
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
			if (target_type == TypeManager.byte_type) {
				if (in_checked_context){
					if (Value < Byte.MinValue || Value > Byte.MaxValue || double.IsNaN (Value))
						throw new OverflowException ();
				}
				return new ByteConstant ((byte) Value, Location);
			}
			if (target_type == TypeManager.sbyte_type) {
				if (in_checked_context){
					if (Value < SByte.MinValue || Value > SByte.MaxValue || double.IsNaN (Value))
						throw new OverflowException ();
				}
				return new SByteConstant ((sbyte) Value, Location);
			}
			if (target_type == TypeManager.short_type) {
				if (in_checked_context){
					if (Value < short.MinValue || Value > short.MaxValue || double.IsNaN (Value))
						throw new OverflowException ();
				}
				return new ShortConstant ((short) Value, Location);
			}
			if (target_type == TypeManager.ushort_type) {
				if (in_checked_context){
					if (Value < ushort.MinValue || Value > ushort.MaxValue || double.IsNaN (Value))
						throw new OverflowException ();
				}
				return new UShortConstant ((ushort) Value, Location);
			}
			if (target_type == TypeManager.int32_type) {
				if (in_checked_context){
					if (Value < int.MinValue || Value > int.MaxValue || double.IsNaN (Value))
						throw new OverflowException ();
				}
				return new IntConstant ((int) Value, Location);
			}
			if (target_type == TypeManager.uint32_type) {
				if (in_checked_context){
					if (Value < uint.MinValue || Value > uint.MaxValue || double.IsNaN (Value))
						throw new OverflowException ();
				}
				return new UIntConstant ((uint) Value, Location);
			}
			if (target_type == TypeManager.int64_type) {
				if (in_checked_context){
					if (Value < long.MinValue || Value > long.MaxValue || double.IsNaN (Value))
						throw new OverflowException ();
				}
				return new LongConstant ((long) Value, Location);
			}
			if (target_type == TypeManager.uint64_type) {
				if (in_checked_context){
					if (Value < ulong.MinValue || Value > ulong.MaxValue || double.IsNaN (Value))
						throw new OverflowException ();
				}
				return new ULongConstant ((ulong) Value, Location);
			}
			if (target_type == TypeManager.float_type)
				return new FloatConstant ((float) Value, Location);
			if (target_type == TypeManager.char_type) {
				if (in_checked_context){
					if (Value < (double) char.MinValue || Value > (double) char.MaxValue || double.IsNaN (Value))
						throw new OverflowException ();
				}
				return new CharConstant ((char) Value, Location);
			}
			if (target_type == TypeManager.decimal_type)
				return new DecimalConstant ((decimal) Value, Location);

			return null;
		}

	}

	public class DecimalConstant : Constant {
		public readonly decimal Value;

		public DecimalConstant (decimal d, Location loc):
			base (loc)
		{
			Value = d;
		}

		protected override Expression DoResolve (ResolveContext rc)
		{
			type = TypeManager.decimal_type;
			eclass = ExprClass.Value;
			return this;
		}

		public override object GetValue ()
		{
			return Value;
		}

		public override string GetValueAsLiteral ()
		{
			return Value.ToString () + "M";
		}

		public override void Emit (EmitContext ec)
		{
			int [] words = decimal.GetBits (Value);
			int power = (words [3] >> 16) & 0xff;

			if (power == 0) {
				if (Value <= int.MaxValue && Value >= int.MinValue) {
					if (TypeManager.void_decimal_ctor_int_arg == null) {
						TypeManager.void_decimal_ctor_int_arg = TypeManager.GetPredefinedConstructor (
							TypeManager.decimal_type, loc, TypeManager.int32_type);

						if (TypeManager.void_decimal_ctor_int_arg == null)
							return;
					}

					ec.EmitInt ((int) Value);
					ec.Emit (OpCodes.Newobj, TypeManager.void_decimal_ctor_int_arg);
					return;
				}

				if (Value <= long.MaxValue && Value >= long.MinValue) {
					if (TypeManager.void_decimal_ctor_long_arg == null) {
						TypeManager.void_decimal_ctor_long_arg = TypeManager.GetPredefinedConstructor (
							TypeManager.decimal_type, loc, TypeManager.int64_type);

						if (TypeManager.void_decimal_ctor_long_arg == null)
							return;
					}

					ec.EmitLong ((long) Value);
					ec.Emit (OpCodes.Newobj, TypeManager.void_decimal_ctor_long_arg);
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

			if (TypeManager.void_decimal_ctor_five_args == null) {
				TypeManager.void_decimal_ctor_five_args = TypeManager.GetPredefinedConstructor (
					TypeManager.decimal_type, loc, TypeManager.int32_type, TypeManager.int32_type,
					TypeManager.int32_type, TypeManager.bool_type, TypeManager.byte_type);

				if (TypeManager.void_decimal_ctor_five_args == null)
					return;
			}

			ec.Emit (OpCodes.Newobj, TypeManager.void_decimal_ctor_five_args);
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
			if (target_type == TypeManager.sbyte_type)
				return new SByteConstant ((sbyte)Value, loc);
			if (target_type == TypeManager.byte_type)
				return new ByteConstant ((byte)Value, loc);
			if (target_type == TypeManager.short_type)
				return new ShortConstant ((short)Value, loc);
			if (target_type == TypeManager.ushort_type)
				return new UShortConstant ((ushort)Value, loc);
			if (target_type == TypeManager.int32_type)
				return new IntConstant ((int)Value, loc);
			if (target_type == TypeManager.uint32_type)
				return new UIntConstant ((uint)Value, loc);
			if (target_type == TypeManager.int64_type)
				return new LongConstant ((long)Value, loc);
			if (target_type == TypeManager.uint64_type)
				return new ULongConstant ((ulong)Value, loc);
			if (target_type == TypeManager.char_type)
				return new CharConstant ((char)Value, loc);
			if (target_type == TypeManager.float_type)
				return new FloatConstant ((float)Value, loc);
			if (target_type == TypeManager.double_type)
				return new DoubleConstant ((double)Value, loc);

			return null;
		}

	}

	public class StringConstant : Constant {
		public readonly string Value;

		public StringConstant (string s, Location loc):
			base (loc)
		{
			Value = s;
		}

		protected override Expression DoResolve (ResolveContext rc)
		{
			type = TypeManager.string_type;
			eclass = ExprClass.Value;
			return this;
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
			if (Value.Length == 0 && RootContext.Optimize && ec.CurrentType != TypeManager.string_type) {
				if (TypeManager.string_empty == null)
					TypeManager.string_empty = TypeManager.GetPredefinedField (TypeManager.string_type, "Empty", loc, TypeManager.string_type);

				if (TypeManager.string_empty != null) {
					ec.Emit (OpCodes.Ldsfld, TypeManager.string_empty);
					return;
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
			if (type == InternalType.Null || type == TypeManager.object_type) {
				// Optimized version, also avoids referencing literal internal type
				Arguments args = new Arguments (1);
				args.Add (new Argument (this));
				return CreateExpressionFactoryCall (ec, "Constant", args);
			}

			return base.CreateExpressionTree (ec);
		}

		protected override Expression DoResolve (ResolveContext ec)
		{
			return this;
		}

		public override void EncodeAttributeValue (IMemberContext rc, AttributeEncoder enc, TypeSpec targetType)
		{
			// Type it as string cast
			if (targetType == TypeManager.object_type || targetType == InternalType.Null)
				enc.Encode (TypeManager.string_type);

			var ac = targetType as ArrayContainer;
			if (ac != null) {
				if (ac.Rank != 1 || ac.Element.IsArray)
					base.EncodeAttributeValue (rc, enc, targetType);
				else
					enc.Encode (uint.MaxValue);
			} else {
				enc.Encode (byte.MaxValue);
			}
		}

		public override void Emit (EmitContext ec)
		{
			ec.Emit (OpCodes.Ldnull);

			// Only to make verifier happy
			if (TypeManager.IsGenericParameter (type))
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
					return new EmptyConstantCast (new NullPointer (loc), targetType);

				return null;
			}

			// Exlude internal compiler types
			if (targetType.Kind == MemberKind.InternalCompilerType && targetType != InternalType.Dynamic && targetType != InternalType.Null)
				return null;

			if (!IsLiteral && !Convert.ImplicitStandardConversionExists (this, targetType))
				return null;

			if (TypeManager.IsReferenceType (targetType))
				return new NullConstant (targetType, loc);

			if (TypeManager.IsNullableType (targetType))
				return Nullable.LiftedNull.Create (targetType, loc);

			return null;
		}

		public override Constant ConvertImplicitly (ResolveContext rc, TypeSpec targetType)
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

	/// <summary>
	///   The value is constant, but when emitted has a side effect.  This is
	///   used by BitwiseAnd to ensure that the second expression is invoked
	///   regardless of the value of the left side.  
	/// </summary>
	public class SideEffectConstant : Constant {
		public Constant value;
		Expression side_effect;
		
		public SideEffectConstant (Constant value, Expression side_effect, Location loc) : base (loc)
		{
			this.value = value;
			while (side_effect is SideEffectConstant)
				side_effect = ((SideEffectConstant) side_effect).side_effect;
			this.side_effect = side_effect;
		}

		protected override Expression DoResolve (ResolveContext rc)
		{
			value = value.Resolve (rc);

			type = value.Type;
			eclass = ExprClass.Value;
			return this;
		}

		public override object GetValue ()
		{
			return value.GetValue ();
		}

		public override string GetValueAsLiteral ()
		{
			return value.GetValueAsLiteral ();
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
			return new_value == null ? null : new SideEffectConstant (new_value, side_effect, new_value.Location);
		}
	}
}
