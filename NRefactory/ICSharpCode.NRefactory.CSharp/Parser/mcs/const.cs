//
// const.cs: Constant declarations.
//
// Author:
//   Miguel de Icaza (miguel@ximian.com)
//   Marek Safar (marek.safar@seznam.cz)
//
// Copyright 2001-2003 Ximian, Inc.
// Copyright 2003-2008 Novell, Inc.
//

#if STATIC
using IKVM.Reflection;
#else
using System.Reflection;
#endif

namespace Mono.CSharp {

	public class Const : FieldBase
	{
		const Modifiers AllowedModifiers =
			Modifiers.NEW |
			Modifiers.PUBLIC |
			Modifiers.PROTECTED |
			Modifiers.INTERNAL |
			Modifiers.PRIVATE;

		public Const (TypeDefinition parent, FullNamedExpression type, Modifiers mod_flags, MemberName name, Attributes attrs)
			: base (parent, type, mod_flags, AllowedModifiers, name, attrs)
		{
			ModFlags |= Modifiers.STATIC;
		}

		/// <summary>
		///   Defines the constant in the @parent
		/// </summary>
		public override bool Define ()
		{
			if (!base.Define ())
				return false;

			if (!member_type.IsConstantCompatible) {
				Error_InvalidConstantType (member_type, Location, Report);
			}

			FieldAttributes field_attr = FieldAttributes.Static | ModifiersExtensions.FieldAttr (ModFlags);
			// Decimals cannot be emitted into the constant blob.  So, convert to 'readonly'.
			if (member_type.BuiltinType == BuiltinTypeSpec.Type.Decimal) {
				field_attr |= FieldAttributes.InitOnly;
			} else {
				field_attr |= FieldAttributes.Literal;
			}

			FieldBuilder = Parent.TypeBuilder.DefineField (Name, MemberType.GetMetaInfo (), field_attr);
			spec = new ConstSpec (Parent.Definition, this, MemberType, FieldBuilder, ModFlags, initializer);

			Parent.MemberCache.AddMember (spec);

			if ((field_attr & FieldAttributes.InitOnly) != 0)
				Parent.PartialContainer.RegisterFieldForInitialization (this,
					new FieldInitializer (this, initializer, Location));

			if (declarators != null) {
				var t = new TypeExpression (MemberType, TypeExpression.Location);
				foreach (var d in declarators) {
					var c = new Const (Parent, t, ModFlags & ~Modifiers.STATIC, new MemberName (d.Name.Value, d.Name.Location), OptAttributes);
					c.initializer = d.Initializer;
					((ConstInitializer) c.initializer).Name = d.Name.Value;
					c.Define ();
					Parent.PartialContainer.Members.Add (c);
				}
			}

			return true;
		}

		public void DefineValue ()
		{
			var rc = new ResolveContext (this);
			((ConstSpec) spec).GetConstant (rc);
		}

		/// <summary>
		///  Emits the field value by evaluating the expression
		/// </summary>
		public override void Emit ()
		{
			var c = ((ConstSpec) spec).Value as Constant;
			if (c.Type.BuiltinType == BuiltinTypeSpec.Type.Decimal) {
				Module.PredefinedAttributes.DecimalConstant.EmitAttribute (FieldBuilder, (decimal) c.GetValue (), c.Location);
			} else {
				FieldBuilder.SetConstant (c.GetValue ());
			}

			base.Emit ();
		}

		public static void Error_InvalidConstantType (TypeSpec t, Location loc, Report Report)
		{
			if (t.IsGenericParameter) {
				Report.Error (1959, loc,
					"Type parameter `{0}' cannot be declared const", t.GetSignatureForError ());
			} else {
				Report.Error (283, loc,
					"The type `{0}' cannot be declared const", t.GetSignatureForError ());
			}
		}

		public override void Accept (StructuralVisitor visitor)
		{
			visitor.Visit (this);
		}
	}

	public class ConstSpec : FieldSpec
	{
		Expression value;

		public ConstSpec (TypeSpec declaringType, IMemberDefinition definition, TypeSpec memberType, FieldInfo fi, Modifiers mod, Expression value)
			: base (declaringType, definition, memberType, fi, mod)
		{
			this.value = value;
		}

		//
		// This expresion is guarantee to be a constant at emit phase only
		//
		public Expression Value {
			get {
				return value;
			}
		}

		//
		// For compiled constants we have to resolve the value as there could be constant dependecies. This
		// is needed for imported constants too to get the right context type
		//
		public Constant GetConstant (ResolveContext rc)
		{
			if (value.eclass != ExprClass.Value)
				value = value.Resolve (rc);

			return (Constant) value;
		}
	}

	public class ConstInitializer : ShimExpression
	{
		bool in_transit;
		readonly FieldBase field;

		public ConstInitializer (FieldBase field, Expression value, Location loc)
			: base (value)
		{
			this.loc = loc;
			this.field = field;
		}

		public string Name { get; set; }

		protected override Expression DoResolve (ResolveContext unused)
		{
			if (type != null)
				return expr;

			var opt = ResolveContext.Options.ConstantScope;
			if (field is EnumMember)
				opt |= ResolveContext.Options.EnumScope;

			//
			// Use a context in which the constant was declared and
			// not the one in which is referenced
			//
			var rc = new ResolveContext (field, opt);
			expr = DoResolveInitializer (rc);
			type = expr.Type;

			return expr;
		}

		protected virtual Expression DoResolveInitializer (ResolveContext rc)
		{
			if (in_transit) {
				field.Compiler.Report.Error (110, expr.Location,
					"The evaluation of the constant value for `{0}' involves a circular definition",
					GetSignatureForError ());

				expr = null;
			} else {
				in_transit = true;
				expr = expr.Resolve (rc);
			}

			in_transit = false;

			if (expr != null) {
				Constant c = expr as Constant;
				if (c != null)
					c = field.ConvertInitializer (rc, c);

				if (c == null) {
					if (TypeSpec.IsReferenceType (field.MemberType))
						Error_ConstantCanBeInitializedWithNullOnly (rc, field.MemberType, expr.Location, GetSignatureForError ());
					else if (!(expr is Constant))
						Error_ExpressionMustBeConstant (rc, expr.Location, GetSignatureForError ());
					else
						expr.Error_ValueCannotBeConverted (rc, field.MemberType, false);
				}

				expr = c;
			}

			if (expr == null) {
				expr = New.Constantify (field.MemberType, Location);
				if (expr == null)
					expr = Constant.CreateConstantFromValue (field.MemberType, null, Location);
				expr = expr.Resolve (rc);
			}

			return expr;
		}

		public override string GetSignatureForError ()
		{
			if (Name == null)
				return field.GetSignatureForError ();

			return field.Parent.GetSignatureForError () + "." + Name;
		}

		public override object Accept (StructuralVisitor visitor)
		{
			return visitor.Visit (this);
		}
	}
}
