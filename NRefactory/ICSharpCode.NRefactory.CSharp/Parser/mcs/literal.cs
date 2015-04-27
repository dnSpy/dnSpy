//
// literal.cs: Literal representation for the IL tree.
//
// Author:
//   Miguel de Icaza (miguel@ximian.com)
//   Marek Safar (marek.safar@seznam.cz)
//
// Copyright 2001 Ximian, Inc.
// Copyright 2011 Xamarin Inc
//
//
// Notice that during parsing we create objects of type Literal, but the
// types are not loaded (thats why the Resolve method has to assign the
// type at that point).
//
// Literals differ from the constants in that we know we encountered them
// as a literal in the source code (and some extra rules apply there) and
// they have to be resolved (since during parsing we have not loaded the
// types yet) while constants are created only after types have been loaded
// and are fully resolved when born.
//

#if STATIC
using IKVM.Reflection.Emit;
#else
using System.Reflection.Emit;
#endif

namespace Mono.CSharp
{
	public interface ILiteralConstant
	{
#if FULL_AST
		char[] ParsedValue { get; set; }
#endif
	}

	//
	// The null literal
	//
	// Note: C# specification null-literal is NullLiteral of NullType type
	//
	public class NullLiteral : NullConstant
	{
		//
		// Default type of null is an object
		//
		public NullLiteral (Location loc)
			: base (InternalType.NullLiteral, loc)
		{
		}

		public override void Error_ValueCannotBeConverted (ResolveContext ec, TypeSpec t, bool expl)
		{
			if (t.IsGenericParameter) {
				ec.Report.Error(403, loc,
					"Cannot convert null to the type parameter `{0}' because it could be a value " +
					"type. Consider using `default ({0})' instead", t.Name);
				return;
			}

			if (TypeSpec.IsValueType (t)) {
				ec.Report.Error(37, loc, "Cannot convert null to `{0}' because it is a value type",
					t.GetSignatureForError ());
				return;
			}

			base.Error_ValueCannotBeConverted (ec, t, expl);
		}

		public override string GetValueAsLiteral ()
		{
			return "null";
		}

		public override bool IsLiteral {
			get { return true; }
		}

		public override System.Linq.Expressions.Expression MakeExpression (BuilderContext ctx)
		{
			return System.Linq.Expressions.Expression.Constant (null);
		}
	}

	public class BoolLiteral : BoolConstant, ILiteralConstant
	{
		public BoolLiteral (BuiltinTypes types, bool val, Location loc)
			: base (types, val, loc)
		{
		}

		public override bool IsLiteral {
			get { return true; }
		}

#if FULL_AST
		public char[] ParsedValue { get; set; }
#endif

		public override object Accept (StructuralVisitor visitor)
		{
			return visitor.Visit (this);
		}
	}

	public class CharLiteral : CharConstant, ILiteralConstant
	{
		public CharLiteral (BuiltinTypes types, char c, Location loc)
			: base (types, c, loc)
		{
		}

		public override bool IsLiteral {
			get { return true; }
		}

#if FULL_AST
		public char[] ParsedValue { get; set; }
#endif

		public override object Accept (StructuralVisitor visitor)
		{
			return visitor.Visit (this);
		}
	}

	public class IntLiteral : IntConstant, ILiteralConstant
	{
		public IntLiteral (BuiltinTypes types, int l, Location loc)
			: base (types, l, loc)
		{
		}

		public override Constant ConvertImplicitly (TypeSpec type)
		{
			//
			// The 0 literal can be converted to an enum value
			//
			if (Value == 0 && type.IsEnum) {
				Constant c = ConvertImplicitly (EnumSpec.GetUnderlyingType (type));
				if (c == null)
					return null;

				return new EnumConstant (c, type);
			}

			return base.ConvertImplicitly (type);
		}

		public override bool IsLiteral {
			get { return true; }
		}

#if FULL_AST
		public char[] ParsedValue { get; set; }
#endif

		public override object Accept (StructuralVisitor visitor)
		{
			return visitor.Visit (this);
		}
	}

	public class UIntLiteral : UIntConstant, ILiteralConstant
	{
		public UIntLiteral (BuiltinTypes types, uint l, Location loc)
			: base (types, l, loc)
		{
		}

		public override bool IsLiteral {
			get { return true; }
		}

#if FULL_AST
		public char[] ParsedValue { get; set; }
#endif

		public override object Accept (StructuralVisitor visitor)
		{
			return visitor.Visit (this);
		}
	}

	public class LongLiteral : LongConstant, ILiteralConstant
	{
		public LongLiteral (BuiltinTypes types, long l, Location loc)
			: base (types, l, loc)
		{
		}

		public override bool IsLiteral {
			get { return true; }
		}

#if FULL_AST
		public char[] ParsedValue { get; set; }
#endif

		public override object Accept (StructuralVisitor visitor)
		{
			return visitor.Visit (this);
		}
	}

	public class ULongLiteral : ULongConstant, ILiteralConstant
	{
		public ULongLiteral (BuiltinTypes types, ulong l, Location loc)
			: base (types, l, loc)
		{
		}

		public override bool IsLiteral {
			get { return true; }
		}

#if FULL_AST
		public char[] ParsedValue { get; set; }
#endif

		public override object Accept (StructuralVisitor visitor)
		{
			return visitor.Visit (this);
		}
	}

	public class FloatLiteral : FloatConstant, ILiteralConstant
	{
		public FloatLiteral (BuiltinTypes types, float f, Location loc)
			: base (types, f, loc)
		{
		}

		public override bool IsLiteral {
			get { return true; }
		}

#if FULL_AST
		public char[] ParsedValue { get; set; }
#endif

		public override object Accept (StructuralVisitor visitor)
		{
			return visitor.Visit (this);
		}
	}

	public class DoubleLiteral : DoubleConstant, ILiteralConstant
	{
		public DoubleLiteral (BuiltinTypes types, double d, Location loc)
			: base (types, d, loc)
		{
		}

		public override void Error_ValueCannotBeConverted (ResolveContext ec, TypeSpec target, bool expl)
		{
			if (target.BuiltinType == BuiltinTypeSpec.Type.Float) {
				Error_664 (ec, loc, "float", "f");
				return;
			}

			if (target.BuiltinType == BuiltinTypeSpec.Type.Decimal) {
				Error_664 (ec, loc, "decimal", "m");
				return;
			}

			base.Error_ValueCannotBeConverted (ec, target, expl);
		}

		static void Error_664 (ResolveContext ec, Location loc, string type, string suffix)
		{
			ec.Report.Error (664, loc,
				"Literal of type double cannot be implicitly converted to type `{0}'. Add suffix `{1}' to create a literal of this type",
				type, suffix);
		}

		public override bool IsLiteral {
			get { return true; }
		}

#if FULL_AST
		public char[] ParsedValue { get; set; }
#endif

		public override object Accept (StructuralVisitor visitor)
		{
			return visitor.Visit (this);
		}
	}

	public class DecimalLiteral : DecimalConstant, ILiteralConstant
	{
		public DecimalLiteral (BuiltinTypes types, decimal d, Location loc)
			: base (types, d, loc)
		{
		}

		public override bool IsLiteral {
			get { return true; }
		}

#if FULL_AST
		public char[] ParsedValue { get; set; }
#endif

		public override object Accept (StructuralVisitor visitor)
		{
			return visitor.Visit (this);
		}
	}

	public class StringLiteral : StringConstant, ILiteralConstant
	{
		public StringLiteral (BuiltinTypes types, string s, Location loc)
			: base (types, s, loc)
		{
		}

		public override bool IsLiteral {
			get { return true; }
		}

#if FULL_AST
		public char[] ParsedValue { get; set; }
#endif

		public override object Accept (StructuralVisitor visitor)
		{
			return visitor.Visit (this);
		}
	}
}
