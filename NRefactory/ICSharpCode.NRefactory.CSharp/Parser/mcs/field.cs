//
// field.cs: All field handlers
//
// Authors: Miguel de Icaza (miguel@gnu.org)
//          Martin Baulig (martin@ximian.com)
//          Marek Safar (marek.safar@seznam.cz)
//
// Dual licensed under the terms of the MIT X11 or GNU GPL
//
// Copyright 2001, 2002, 2003 Ximian, Inc (http://www.ximian.com)
// Copyright 2004-2008 Novell, Inc
// Copyright 2011 Xamarin Inc
//

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

#if STATIC
using MetaType = IKVM.Reflection.Type;
using IKVM.Reflection;
using IKVM.Reflection.Emit;
#else
using MetaType = System.Type;
using System.Reflection;
using System.Reflection.Emit;
#endif

namespace Mono.CSharp
{
	public class FieldDeclarator
	{
		public FieldDeclarator (SimpleMemberName name, Expression initializer)
		{
			this.Name = name;
			this.Initializer = initializer;
		}

		#region Properties

		public SimpleMemberName Name { get; private set; }
		public Expression Initializer { get; private set; }

		#endregion
	}

	//
	// Abstract class for all fields
	//
	abstract public class FieldBase : MemberBase
	{
		protected FieldBuilder FieldBuilder;
		protected FieldSpec spec;
		public Status status;
		protected Expression initializer;
		protected List<FieldDeclarator> declarators;

		[Flags]
		public enum Status : byte {
			HAS_OFFSET = 4		// Used by FieldMember.
		}

		static readonly string[] attribute_targets = new string [] { "field" };

		protected FieldBase (DeclSpace parent, FullNamedExpression type, Modifiers mod,
				     Modifiers allowed_mod, MemberName name, Attributes attrs)
			: base (parent, null, type, mod, allowed_mod | Modifiers.ABSTRACT, Modifiers.PRIVATE,
				name, attrs)
		{
			if ((mod & Modifiers.ABSTRACT) != 0)
				Report.Error (681, Location, "The modifier 'abstract' is not valid on fields. Try using a property instead");
		}

		#region Properties

		public override AttributeTargets AttributeTargets {
			get {
				return AttributeTargets.Field;
			}
		}

		public Expression Initializer {
			get {
				return initializer;
			}
			set {
				this.initializer = value;
			}
		}

		public FieldSpec Spec {
			get {
				return spec;
			}
		}

		public override string[] ValidAttributeTargets  {
			get {
				return attribute_targets;
			}
		}
		
		public List<FieldDeclarator> Declarators {
			get {
				return this.declarators;
			}
		}
		#endregion

		public void AddDeclarator (FieldDeclarator declarator)
		{
			if (declarators == null)
				declarators = new List<FieldDeclarator> (2);

			declarators.Add (declarator);

			// TODO: This will probably break
			Parent.AddMember (this, declarator.Name.Value);
		}

		public override void ApplyAttributeBuilder (Attribute a, MethodSpec ctor, byte[] cdata, PredefinedAttributes pa)
		{
			if (a.Type == pa.FieldOffset) {
				status |= Status.HAS_OFFSET;

				if (!Parent.PartialContainer.HasExplicitLayout) {
					Report.Error (636, Location, "The FieldOffset attribute can only be placed on members of types marked with the StructLayout(LayoutKind.Explicit)");
					return;
				}

				if ((ModFlags & Modifiers.STATIC) != 0 || this is Const) {
					Report.Error (637, Location, "The FieldOffset attribute is not allowed on static or const fields");
					return;
				}
			}

			if (a.Type == pa.FixedBuffer) {
				Report.Error (1716, Location, "Do not use 'System.Runtime.CompilerServices.FixedBuffer' attribute. Use the 'fixed' field modifier instead");
				return;
			}

#if false
			if (a.Type == pa.MarshalAs) {
				UnmanagedMarshal marshal = a.GetMarshal (this);
				if (marshal != null) {
					FieldBuilder.SetMarshal (marshal);
				}
				return;
			}
#endif
			if ((a.HasSecurityAttribute)) {
				a.Error_InvalidSecurityParent ();
				return;
			}

			if (a.Type == pa.Dynamic) {
				a.Error_MisusedDynamicAttribute ();
				return;
			}

			FieldBuilder.SetCustomAttribute ((ConstructorInfo) ctor.GetMetaInfo (), cdata);
		}

		public void SetCustomAttribute (MethodSpec ctor, byte[] data)
		{
			FieldBuilder.SetCustomAttribute ((ConstructorInfo) ctor.GetMetaInfo (), data);
		}

 		protected override bool CheckBase ()
		{
 			if (!base.CheckBase ())
 				return false;

			MemberSpec candidate;
			bool overrides = false;
			var conflict_symbol = MemberCache.FindBaseMember (this, out candidate, ref overrides);
			if (conflict_symbol == null)
				conflict_symbol = candidate;

 			if (conflict_symbol == null) {
 				if ((ModFlags & Modifiers.NEW) != 0) {
 					Report.Warning (109, 4, Location, "The member `{0}' does not hide an inherited member. The new keyword is not required",
						GetSignatureForError ());
 				}
 			} else {
				if ((ModFlags & (Modifiers.NEW | Modifiers.OVERRIDE | Modifiers.BACKING_FIELD)) == 0) {
					Report.SymbolRelatedToPreviousError (conflict_symbol);
					Report.Warning (108, 2, Location, "`{0}' hides inherited member `{1}'. Use the new keyword if hiding was intended",
						GetSignatureForError (), conflict_symbol.GetSignatureForError ());
				}

				if (conflict_symbol.IsAbstract) {
					Report.SymbolRelatedToPreviousError (conflict_symbol);
					Report.Error (533, Location, "`{0}' hides inherited abstract member `{1}'",
						GetSignatureForError (), conflict_symbol.GetSignatureForError ());
				}
			}
 
 			return true;
 		}

		public virtual Constant ConvertInitializer (ResolveContext rc, Constant expr)
		{
			return expr.ConvertImplicitly (MemberType);
		}

		protected override void DoMemberTypeDependentChecks ()
		{
			base.DoMemberTypeDependentChecks ();

			if (MemberType.IsGenericParameter)
				return;

			if (MemberType.IsStatic)
				Error_VariableOfStaticClass (Location, GetSignatureForError (), MemberType, Report);

			CheckBase ();
			IsTypePermitted ();
		}

		//
		//   Represents header string for documentation comment.
		//
		public override string DocCommentHeader {
			get { return "F:"; }
		}

		public override void Emit ()
		{
			if (member_type.BuiltinType == BuiltinTypeSpec.Type.Dynamic) {
				Module.PredefinedAttributes.Dynamic.EmitAttribute (FieldBuilder);
			} else if (!(Parent is CompilerGeneratedClass) && member_type.HasDynamicElement) {
				Module.PredefinedAttributes.Dynamic.EmitAttribute (FieldBuilder, member_type, Location);
			}

			if ((ModFlags & Modifiers.COMPILER_GENERATED) != 0 && !Parent.IsCompilerGenerated)
				Module.PredefinedAttributes.CompilerGenerated.EmitAttribute (FieldBuilder);

			if (OptAttributes != null) {
				OptAttributes.Emit ();
			}

			if (((status & Status.HAS_OFFSET) == 0) && (ModFlags & (Modifiers.STATIC | Modifiers.BACKING_FIELD)) == 0 && Parent.PartialContainer.HasExplicitLayout) {
				Report.Error (625, Location, "`{0}': Instance field types marked with StructLayout(LayoutKind.Explicit) must have a FieldOffset attribute", GetSignatureForError ());
			}

			ConstraintChecker.Check (this, member_type, type_expr.Location);

			base.Emit ();
		}

		public static void Error_VariableOfStaticClass (Location loc, string variable_name, TypeSpec static_class, Report Report)
		{
			Report.SymbolRelatedToPreviousError (static_class);
			Report.Error (723, loc, "`{0}': cannot declare variables of static types",
				variable_name);
		}

		protected override bool VerifyClsCompliance ()
		{
			if (!base.VerifyClsCompliance ())
				return false;

			if (!MemberType.IsCLSCompliant () || this is FixedField) {
				Report.Warning (3003, 1, Location, "Type of `{0}' is not CLS-compliant",
					GetSignatureForError ());
			}
			return true;
		}
	}

	//
	// Field specification
	//
	public class FieldSpec : MemberSpec, IInterfaceMemberSpec
	{
		FieldInfo metaInfo;
		TypeSpec memberType;

		public FieldSpec (TypeSpec declaringType, IMemberDefinition definition, TypeSpec memberType, FieldInfo info, Modifiers modifiers)
			: base (MemberKind.Field, declaringType, definition, modifiers)
		{
			this.metaInfo = info;
			this.memberType = memberType;
		}

		#region Properties

		public bool IsReadOnly {
			get {
				return (Modifiers & Modifiers.READONLY) != 0;
			}
		}

		public TypeSpec MemberType {
			get {
				return memberType;
			}
		}

#endregion

		public FieldInfo GetMetaInfo ()
		{
			if ((state & StateFlags.PendingMetaInflate) != 0) {
				var decl_meta = DeclaringType.GetMetaInfo ();
				if (DeclaringType.IsTypeBuilder) {
					metaInfo = TypeBuilder.GetField (decl_meta, metaInfo);
				} else {
					var orig_token = metaInfo.MetadataToken;
					metaInfo = decl_meta.GetField (Name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
					if (metaInfo.MetadataToken != orig_token)
						throw new NotImplementedException ("Resolved to wrong meta token");

					// What a stupid API, does not work because field handle is imported
					// metaInfo = FieldInfo.GetFieldFromHandle (metaInfo.FieldHandle, DeclaringType.MetaInfo.TypeHandle);
				}

				state &= ~StateFlags.PendingMetaInflate;
			}

			return metaInfo;
		}

		public override MemberSpec InflateMember (TypeParameterInflator inflator)
		{
			var fs = (FieldSpec) base.InflateMember (inflator);
			fs.memberType = inflator.Inflate (memberType);
			return fs;
		}

		public FieldSpec Mutate (TypeParameterMutator mutator)
		{
			var decl = DeclaringType;
			if (DeclaringType.IsGenericOrParentIsGeneric)
				decl = mutator.Mutate (decl);

			if (decl == DeclaringType)
				return this;

			var fs = (FieldSpec) MemberwiseClone ();
			fs.declaringType = decl;
			fs.state |= StateFlags.PendingMetaInflate;

			// Gets back FieldInfo in case of metaInfo was inflated
			fs.metaInfo = MemberCache.GetMember (TypeParameterMutator.GetMemberDeclaringType (DeclaringType), this).metaInfo;
			return fs;
		}

		public override List<TypeSpec> ResolveMissingDependencies ()
		{
			return memberType.ResolveMissingDependencies ();
		}
	}

	/// <summary>
	/// Fixed buffer implementation
	/// </summary>
	public class FixedField : FieldBase
	{
		public const string FixedElementName = "FixedElementField";
		static int GlobalCounter = 0;

		TypeBuilder fixed_buffer_type;

		const Modifiers AllowedModifiers =
			Modifiers.NEW |
			Modifiers.PUBLIC |
			Modifiers.PROTECTED |
			Modifiers.INTERNAL |
			Modifiers.PRIVATE |
			Modifiers.UNSAFE;

		public FixedField (DeclSpace parent, FullNamedExpression type, Modifiers mod, MemberName name, Attributes attrs)
			: base (parent, type, mod, AllowedModifiers, name, attrs)
		{
		}

		#region Properties

		//
		// Explicit struct layout set by parent
		//
		public CharSet? CharSet {
			get; set;
		}		

		#endregion

		public override Constant ConvertInitializer (ResolveContext rc, Constant expr)
		{
			return expr.ImplicitConversionRequired (rc, rc.BuiltinTypes.Int, Location);
		}

		public override bool Define ()
		{
			if (!base.Define ())
				return false;

			if (!BuiltinTypeSpec.IsPrimitiveType (MemberType)) {
				Report.Error (1663, Location,
					"`{0}': Fixed size buffers type must be one of the following: bool, byte, short, int, long, char, sbyte, ushort, uint, ulong, float or double",
					GetSignatureForError ());
			} else if (declarators != null) {
				var t = new TypeExpression (MemberType, TypeExpression.Location);
				int index = Parent.PartialContainer.Fields.IndexOf (this);
				foreach (var d in declarators) {
					var f = new FixedField (Parent, t, ModFlags, new MemberName (d.Name.Value, d.Name.Location), OptAttributes);
					f.initializer = d.Initializer;
					((ConstInitializer) f.initializer).Name = d.Name.Value;
					Parent.PartialContainer.Fields.Insert (++index, f);
				}
			}
			
			// Create nested fixed buffer container
			string name = String.Format ("<{0}>__FixedBuffer{1}", Name, GlobalCounter++);
			fixed_buffer_type = Parent.TypeBuilder.DefineNestedType (name,
				TypeAttributes.NestedPublic | TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit,
				Compiler.BuiltinTypes.ValueType.GetMetaInfo ());

			var ffield = fixed_buffer_type.DefineField (FixedElementName, MemberType.GetMetaInfo (), FieldAttributes.Public);
			
			FieldBuilder = Parent.TypeBuilder.DefineField (Name, fixed_buffer_type, ModifiersExtensions.FieldAttr (ModFlags));

			var element_spec = new FieldSpec (null, this, MemberType, ffield, ModFlags);
			spec = new FixedFieldSpec (Parent.Definition, this, FieldBuilder, element_spec, ModFlags);

			Parent.MemberCache.AddMember (spec);
			return true;
		}

		protected override void DoMemberTypeIndependentChecks ()
		{
			base.DoMemberTypeIndependentChecks ();

			if (!IsUnsafe)
				Expression.UnsafeError (Report, Location);

			if (Parent.PartialContainer.Kind != MemberKind.Struct) {
				Report.Error (1642, Location, "`{0}': Fixed size buffer fields may only be members of structs",
					GetSignatureForError ());
			}
		}
		
		public override void Accept (StructuralVisitor visitor)
		{
			visitor.Visit (this);
		}

		public override void Emit()
		{
			ResolveContext rc = new ResolveContext (this);
			IntConstant buffer_size_const = initializer.Resolve (rc) as IntConstant;
			if (buffer_size_const == null)
				return;

			int buffer_size = buffer_size_const.Value;

			if (buffer_size <= 0) {
				Report.Error (1665, Location, "`{0}': Fixed size buffers must have a length greater than zero", GetSignatureForError ());
				return;
			}

			EmitFieldSize (buffer_size);

#if STATIC
			if (Module.HasDefaultCharSet)
				fixed_buffer_type.__SetAttributes (fixed_buffer_type.Attributes | Module.DefaultCharSetType);
#endif

			Module.PredefinedAttributes.UnsafeValueType.EmitAttribute (fixed_buffer_type);
			Module.PredefinedAttributes.CompilerGenerated.EmitAttribute (fixed_buffer_type);
			fixed_buffer_type.CreateType ();

			base.Emit ();
		}

		void EmitFieldSize (int buffer_size)
		{
			int type_size = BuiltinTypeSpec.GetSize (MemberType);

			if (buffer_size > int.MaxValue / type_size) {
				Report.Error (1664, Location, "Fixed size buffer `{0}' of length `{1}' and type `{2}' exceeded 2^31 limit",
					GetSignatureForError (), buffer_size.ToString (), TypeManager.CSharpName (MemberType));
				return;
			}

			AttributeEncoder encoder;

			var ctor = Module.PredefinedMembers.StructLayoutAttributeCtor.Resolve (Location);
			if (ctor == null)
				return;

			var field_size = Module.PredefinedMembers.StructLayoutSize.Resolve (Location);
			var field_charset = Module.PredefinedMembers.StructLayoutCharSet.Resolve (Location);
			if (field_size == null || field_charset == null)
				return;

			var char_set = CharSet ?? Module.DefaultCharSet ?? 0;

			encoder = new AttributeEncoder ();
			encoder.Encode ((short)LayoutKind.Sequential);
			encoder.EncodeNamedArguments (
				new [] { field_size, field_charset },
				new Constant [] { 
					new IntConstant (Compiler.BuiltinTypes, buffer_size * type_size, Location),
					new IntConstant (Compiler.BuiltinTypes, (int) char_set, Location)
				}
			);

			fixed_buffer_type.SetCustomAttribute ((ConstructorInfo) ctor.GetMetaInfo (), encoder.ToArray ());

			//
			// Don't emit FixedBufferAttribute attribute for private types
			//
			if ((ModFlags & Modifiers.PRIVATE) != 0)
				return;

			ctor = Module.PredefinedMembers.FixedBufferAttributeCtor.Resolve (Location);
			if (ctor == null)
				return;

			encoder = new AttributeEncoder ();
			encoder.EncodeTypeName (MemberType);
			encoder.Encode (buffer_size);
			encoder.EncodeEmptyNamedArguments ();

			FieldBuilder.SetCustomAttribute ((ConstructorInfo) ctor.GetMetaInfo (), encoder.ToArray ());
		}
	}

	class FixedFieldSpec : FieldSpec
	{
		readonly FieldSpec element;

		public FixedFieldSpec (TypeSpec declaringType, IMemberDefinition definition, FieldInfo info, FieldSpec element, Modifiers modifiers)
			: base (declaringType, definition, element.MemberType, info, modifiers)
		{
			this.element = element;

			// It's never CLS-Compliant
			state &= ~StateFlags.CLSCompliant_Undetected;
		}

		public FieldSpec Element {
			get {
				return element;
			}
		}

		public TypeSpec ElementType {
			get {
				return MemberType;
			}
		}
	}

	//
	// The Field class is used to represents class/struct fields during parsing.
	//
	public class Field : FieldBase {
		// <summary>
		//   Modifiers allowed in a class declaration
		// </summary>
		const Modifiers AllowedModifiers =
			Modifiers.NEW |
			Modifiers.PUBLIC |
			Modifiers.PROTECTED |
			Modifiers.INTERNAL |
			Modifiers.PRIVATE |
			Modifiers.STATIC |
			Modifiers.VOLATILE |
			Modifiers.UNSAFE |
			Modifiers.READONLY;

		public Field (DeclSpace parent, FullNamedExpression type, Modifiers mod, MemberName name,
			      Attributes attrs)
			: base (parent, type, mod, AllowedModifiers, name, attrs)
		{
		}

		bool CanBeVolatile ()
		{
			switch (MemberType.BuiltinType) {
			case BuiltinTypeSpec.Type.Bool:
			case BuiltinTypeSpec.Type.Char:
			case BuiltinTypeSpec.Type.SByte:
			case BuiltinTypeSpec.Type.Byte:
			case BuiltinTypeSpec.Type.Short:
			case BuiltinTypeSpec.Type.UShort:
			case BuiltinTypeSpec.Type.Int:
			case BuiltinTypeSpec.Type.UInt:
			case BuiltinTypeSpec.Type.Float:
			case BuiltinTypeSpec.Type.UIntPtr:
			case BuiltinTypeSpec.Type.IntPtr:
				return true;
			}

			if (TypeSpec.IsReferenceType (MemberType))
				return true;

			if (MemberType.IsEnum)
				return true;

			return false;
		}

		public override void Accept (StructuralVisitor visitor)
		{
			visitor.Visit (this);
		}
		
		public override bool Define ()
		{
			if (!base.Define ())
				return false;

			MetaType[] required_modifier = null;
			if ((ModFlags & Modifiers.VOLATILE) != 0) {
				var mod = Module.PredefinedTypes.IsVolatile.Resolve ();
				if (mod != null)
					required_modifier = new MetaType[] { mod.GetMetaInfo () };
			}

			FieldBuilder = Parent.TypeBuilder.DefineField (
				Name, member_type.GetMetaInfo (), required_modifier, null, ModifiersExtensions.FieldAttr (ModFlags));

			spec = new FieldSpec (Parent.Definition, this, MemberType, FieldBuilder, ModFlags);

			//
			// Don't cache inaccessible fields except for struct where we
			// need them for definitive assignment checks
			//
			if ((ModFlags & Modifiers.BACKING_FIELD) == 0 || Parent.Kind == MemberKind.Struct) {
				Parent.MemberCache.AddMember (spec);
			}

			if (initializer != null) {
				((TypeContainer) Parent).RegisterFieldForInitialization (this,
					new FieldInitializer (spec, initializer, this));
			}

			if (declarators != null) {
				var t = new TypeExpression (MemberType, TypeExpression.Location);
				int index = Parent.PartialContainer.Fields.IndexOf (this);
				foreach (var d in declarators) {
					var f = new Field (Parent, t, ModFlags, new MemberName (d.Name.Value, d.Name.Location), OptAttributes);
					if (d.Initializer != null)
						f.initializer = d.Initializer;

					Parent.PartialContainer.Fields.Insert (++index, f);
				}
			}

			return true;
		}

		protected override void DoMemberTypeDependentChecks ()
		{
			if ((ModFlags & Modifiers.BACKING_FIELD) != 0)
				return;

			base.DoMemberTypeDependentChecks ();

			if ((ModFlags & Modifiers.VOLATILE) != 0) {
				if (!CanBeVolatile ()) {
					Report.Error (677, Location, "`{0}': A volatile field cannot be of the type `{1}'",
						GetSignatureForError (), TypeManager.CSharpName (MemberType));
				}

				if ((ModFlags & Modifiers.READONLY) != 0) {
					Report.Error (678, Location, "`{0}': A field cannot be both volatile and readonly",
						GetSignatureForError ());
				}
			}
		}

		protected override bool VerifyClsCompliance ()
		{
			if (!base.VerifyClsCompliance ())
				return false;

			if ((ModFlags & Modifiers.VOLATILE) != 0) {
				Report.Warning (3026, 1, Location, "CLS-compliant field `{0}' cannot be volatile", GetSignatureForError ());
			}

			return true;
		}
	}
}
