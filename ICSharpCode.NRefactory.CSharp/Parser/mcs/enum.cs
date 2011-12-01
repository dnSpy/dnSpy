//
// enum.cs: Enum handling.
//
// Author: Miguel de Icaza (miguel@gnu.org)
//         Ravi Pratap     (ravi@ximian.com)
//         Marek Safar     (marek.safar@seznam.cz)
//
// Dual licensed under the terms of the MIT X11 or GNU GPL
//
// Copyright 2001 Ximian, Inc (http://www.ximian.com)
// Copyright 2003-2003 Novell, Inc (http://www.novell.com)
// Copyright 2011 Xamarin Inc
//

using System;

#if STATIC
using MetaType = IKVM.Reflection.Type;
using IKVM.Reflection;
#else
using MetaType = System.Type;
using System.Reflection;
#endif

namespace Mono.CSharp {

	public class EnumMember : Const
	{
		class EnumTypeExpr : TypeExpr
		{
			public override TypeSpec ResolveAsType (IMemberContext ec)
			{
				type = ec.CurrentType;
				eclass = ExprClass.Type;
				return type;
			}
		}

		public EnumMember (Enum parent, MemberName name, Attributes attrs)
			: base (parent, new EnumTypeExpr (), Modifiers.PUBLIC, name, attrs)
		{
		}

		static bool IsValidEnumType (TypeSpec t)
		{
			switch (t.BuiltinType) {
			case BuiltinTypeSpec.Type.Int:
			case BuiltinTypeSpec.Type.UInt:
			case BuiltinTypeSpec.Type.Long:
			case BuiltinTypeSpec.Type.Byte:
			case BuiltinTypeSpec.Type.SByte:
			case BuiltinTypeSpec.Type.Short:
			case BuiltinTypeSpec.Type.UShort:
			case BuiltinTypeSpec.Type.ULong:
			case BuiltinTypeSpec.Type.Char:
				return true;
			default:
				return t.IsEnum;
			}
		}

		public override Constant ConvertInitializer (ResolveContext rc, Constant expr)
		{
			if (expr is EnumConstant)
				expr = ((EnumConstant) expr).Child;

			var underlying = ((Enum) Parent).UnderlyingType;
			if (expr != null) {
				expr = expr.ImplicitConversionRequired (rc, underlying, Location);
				if (expr != null && !IsValidEnumType (expr.Type)) {
					Enum.Error_1008 (Location, Report);
					expr = null;
				}
			}

			if (expr == null)
				expr = New.Constantify (underlying, Location);

			return new EnumConstant (expr, MemberType);
		}

		public override bool Define ()
		{
			if (!ResolveMemberType ())
				return false;

			const FieldAttributes attr = FieldAttributes.Public | FieldAttributes.Static | FieldAttributes.Literal;
			FieldBuilder = Parent.TypeBuilder.DefineField (Name, MemberType.GetMetaInfo (), attr);
			spec = new ConstSpec (Parent.Definition, this, MemberType, FieldBuilder, ModFlags, initializer);

			Parent.MemberCache.AddMember (spec);
			return true;
		}
		
		public override void Accept (StructuralVisitor visitor)
		{
			visitor.Visit (this);
		}

	}

	/// <summary>
	///   Enumeration container
	/// </summary>
	public class Enum : TypeContainer
	{
		//
		// Implicit enum member initializer, used when no constant value is provided
		//
		sealed class ImplicitInitializer : Expression
		{
			readonly EnumMember prev;
			readonly EnumMember current;

			public ImplicitInitializer (EnumMember current, EnumMember prev)
			{
				this.current = current;
				this.prev = prev;
			}

			public override bool ContainsEmitWithAwait ()
			{
				return false;
			}

			public override Expression CreateExpressionTree (ResolveContext ec)
			{
				throw new NotSupportedException ("Missing Resolve call");
			}

			protected override Expression DoResolve (ResolveContext rc)
			{
				// We are the first member
				if (prev == null) {
					return New.Constantify (current.Parent.Definition, Location);
				}

				var c = ((ConstSpec) prev.Spec).GetConstant (rc) as EnumConstant;
				try {
					return c.Increment ();
				} catch (OverflowException) {
					rc.Report.Error (543, current.Location,
						"The enumerator value `{0}' is outside the range of enumerator underlying type `{1}'",
						current.GetSignatureForError (), ((Enum) current.Parent).UnderlyingType.GetSignatureForError ());

					return New.Constantify (current.Parent.Definition, current.Location);
				}
			}

			public override void Emit (EmitContext ec)
			{
				throw new NotSupportedException ("Missing Resolve call");
			}
		}

		public static readonly string UnderlyingValueField = "value__";

		const Modifiers AllowedModifiers =
			Modifiers.NEW |
			Modifiers.PUBLIC |
			Modifiers.PROTECTED |
			Modifiers.INTERNAL |
			Modifiers.PRIVATE;

		readonly TypeExpr underlying_type_expr;

		public Enum (NamespaceContainer ns, DeclSpace parent, TypeExpression type,
			     Modifiers mod_flags, MemberName name, Attributes attrs)
			: base (ns, parent, name, attrs, MemberKind.Enum)
		{
			underlying_type_expr = type;
			var accmods = IsTopLevel ? Modifiers.INTERNAL : Modifiers.PRIVATE;
			ModFlags = ModifiersExtensions.Check (AllowedModifiers, mod_flags, accmods, Location, Report);
			spec = new EnumSpec (null, this, null, null, ModFlags);
		}

		#region Properties

		public override AttributeTargets AttributeTargets {
			get {
				return AttributeTargets.Enum;
			}
		}

		public TypeExpr BaseTypeExpression {
			get {
				return underlying_type_expr;
			}
		}

		protected override TypeAttributes TypeAttr {
			get {
				return ModifiersExtensions.TypeAttr (ModFlags, IsTopLevel) |
					TypeAttributes.Class | TypeAttributes.Sealed | base.TypeAttr;
			}
		}

		public TypeSpec UnderlyingType {
			get {
				return ((EnumSpec) spec).UnderlyingType;
			}
		}

		#endregion

		public override void Accept (StructuralVisitor visitor)
		{
			visitor.Visit (this);
		}

		public void AddEnumMember (EnumMember em)
		{
			if (em.Name == UnderlyingValueField) {
				Report.Error (76, em.Location, "An item in an enumeration cannot have an identifier `{0}'",
					UnderlyingValueField);
				return;
			}

			AddConstant (em);
		}

		public static void Error_1008 (Location loc, Report Report)
		{
			Report.Error (1008, loc,
				"Type byte, sbyte, short, ushort, int, uint, long or ulong expected");
		}

		protected override bool DefineNestedTypes ()
		{
			((EnumSpec) spec).UnderlyingType = underlying_type_expr == null ? Compiler.BuiltinTypes.Int : underlying_type_expr.Type;

			TypeBuilder.DefineField (UnderlyingValueField, UnderlyingType.GetMetaInfo (),
				FieldAttributes.Public | FieldAttributes.SpecialName | FieldAttributes.RTSpecialName);

			return true;
		}

		protected override bool DoDefineMembers ()
		{
			if (constants != null) {
				for (int i = 0; i < constants.Count; ++i) {
					EnumMember em = (EnumMember) constants [i];
					if (em.Initializer == null) {
						em.Initializer = new ImplicitInitializer (em, i == 0 ? null : (EnumMember) constants[i - 1]);
					}

					em.Define ();
				}
			}

			return true;
		}

		public override bool IsUnmanagedType ()
		{
			return true;
		}

		protected override TypeSpec[] ResolveBaseTypes (out FullNamedExpression base_class)
		{
			base_type = Compiler.BuiltinTypes.Enum;
			base_class = null;
			return null;
		}
		
		protected override bool VerifyClsCompliance ()
		{
			if (!base.VerifyClsCompliance ())
				return false;

			switch (UnderlyingType.BuiltinType) {
			case BuiltinTypeSpec.Type.UInt:
			case BuiltinTypeSpec.Type.ULong:
			case BuiltinTypeSpec.Type.UShort:
				Report.Warning (3009, 1, Location, "`{0}': base type `{1}' is not CLS-compliant",
					GetSignatureForError (), TypeManager.CSharpName (UnderlyingType));
				break;
			}

			return true;
		}
	}

	class EnumSpec : TypeSpec
	{
		TypeSpec underlying;

		public EnumSpec (TypeSpec declaringType, ITypeDefinition definition, TypeSpec underlyingType, MetaType info, Modifiers modifiers)
			: base (MemberKind.Enum, declaringType, definition, info, modifiers | Modifiers.SEALED)
		{
			this.underlying = underlyingType;
		}

		public TypeSpec UnderlyingType {
			get {
				return underlying;
			}
			set {
				if (underlying != null)
					throw new InternalErrorException ("UnderlyingType reset");

				underlying = value;
			}
		}

		public static TypeSpec GetUnderlyingType (TypeSpec t)
		{
			return ((EnumSpec) t.GetDefinition ()).UnderlyingType;
		}

		public static bool IsValidUnderlyingType (TypeSpec type)
		{
			switch (type.BuiltinType) {
			case BuiltinTypeSpec.Type.Int:
			case BuiltinTypeSpec.Type.UInt:
			case BuiltinTypeSpec.Type.Long:
			case BuiltinTypeSpec.Type.Byte:
			case BuiltinTypeSpec.Type.SByte:
			case BuiltinTypeSpec.Type.Short:
			case BuiltinTypeSpec.Type.UShort:
			case BuiltinTypeSpec.Type.ULong:
				return true;
			}

			return false;
		}
	}
}
