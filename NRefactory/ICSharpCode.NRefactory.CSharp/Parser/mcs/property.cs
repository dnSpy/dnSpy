//
// property.cs: Property based handlers
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
using System.Text;
using Mono.CompilerServices.SymbolWriter;

#if NET_2_1
using XmlElement = System.Object;
#endif

#if STATIC
using IKVM.Reflection;
using IKVM.Reflection.Emit;
#else
using System.Reflection;
using System.Reflection.Emit;
#endif

namespace Mono.CSharp
{
	// It is used as a base class for all property based members
	// This includes properties, indexers, and events
	public abstract class PropertyBasedMember : InterfaceMemberBase
	{
		protected PropertyBasedMember (TypeDefinition parent, FullNamedExpression type, Modifiers mod, Modifiers allowed_mod, MemberName name, Attributes attrs)
			: base (parent, type, mod, allowed_mod, name, attrs)
		{
		}

		protected void CheckReservedNameConflict (string prefix, MethodSpec accessor)
		{
			string name;
			AParametersCollection parameters;
			if (accessor != null) {
				name = accessor.Name;
				parameters = accessor.Parameters;
			} else {
				name = prefix + ShortName;
				if (IsExplicitImpl)
					name = MemberName.Left + "." + name;

				if (this is Indexer) {
					parameters = ((Indexer) this).ParameterInfo;
					if (prefix[0] == 's') {
						var data = new IParameterData[parameters.Count + 1];
						Array.Copy (parameters.FixedParameters, data, data.Length - 1);
						data[data.Length - 1] = new ParameterData ("value", Parameter.Modifier.NONE);
						var types = new TypeSpec[data.Length];
						Array.Copy (parameters.Types, types, data.Length - 1);
						types[data.Length - 1] = member_type;

						parameters = new ParametersImported (data, types, false);
					}
				} else {
					if (prefix[0] == 's')
						parameters = ParametersCompiled.CreateFullyResolved (new[] { member_type });
					else
						parameters = ParametersCompiled.EmptyReadOnlyParameters;
				}
			}

			var conflict = MemberCache.FindMember (Parent.Definition,
				new MemberFilter (name, 0, MemberKind.Method, parameters, null),
				BindingRestriction.DeclaredOnly | BindingRestriction.NoAccessors);

			if (conflict != null) {
				Report.SymbolRelatedToPreviousError (conflict);
				Report.Error (82, Location, "A member `{0}' is already reserved", conflict.GetSignatureForError ());
			}
		}

		public abstract void PrepareEmit ();

		protected override bool VerifyClsCompliance ()
		{
			if (!base.VerifyClsCompliance ())
				return false;

			if (!MemberType.IsCLSCompliant ()) {
				Report.Warning (3003, 1, Location, "Type of `{0}' is not CLS-compliant",
					GetSignatureForError ());
			}
			return true;
		}

	}

	public class PropertySpec : MemberSpec, IInterfaceMemberSpec
	{
		PropertyInfo info;
		TypeSpec memberType;
		MethodSpec set, get;

		public PropertySpec (MemberKind kind, TypeSpec declaringType, IMemberDefinition definition, TypeSpec memberType, PropertyInfo info, Modifiers modifiers)
			: base (kind, declaringType, definition, modifiers)
		{
			this.info = info;
			this.memberType = memberType;
		}

		#region Properties

		public MethodSpec Get {
			get {
				return get;
			}
			set {
				get = value;
				get.IsAccessor = true;
			}
		}

		public MethodSpec Set { 
			get {
				return set;
			}
			set {
				set = value;
				set.IsAccessor = true;
			}
		}

		public bool HasDifferentAccessibility {
			get {
				return HasGet && HasSet && 
					(Get.Modifiers & Modifiers.AccessibilityMask) != (Set.Modifiers & Modifiers.AccessibilityMask);
			}
		}

		public bool HasGet {
			get {
				return Get != null;
			}
		}

		public bool HasSet {
			get {
				return Set != null;
			}
		}

		public PropertyInfo MetaInfo {
			get {
				if ((state & StateFlags.PendingMetaInflate) != 0)
					throw new NotSupportedException ();

				return info;
			}
		}

		public TypeSpec MemberType {
			get {
				return memberType;
			}
		}

		#endregion

		public override MemberSpec InflateMember (TypeParameterInflator inflator)
		{
			var ps = (PropertySpec) base.InflateMember (inflator);
			ps.memberType = inflator.Inflate (memberType);
			return ps;
		}

		public override List<MissingTypeSpecReference> ResolveMissingDependencies (MemberSpec caller)
		{
			return memberType.ResolveMissingDependencies (this);
		}
	}

	//
	// Properties and Indexers both generate PropertyBuilders, we use this to share 
	// their common bits.
	//
	abstract public class PropertyBase : PropertyBasedMember {

		public class GetMethod : PropertyMethod
		{
			static readonly string[] attribute_targets = new string [] { "method", "return" };

			internal const string Prefix = "get_";

			public GetMethod (PropertyBase method, Modifiers modifiers, Attributes attrs, Location loc)
				: base (method, Prefix, modifiers, attrs, loc)
			{
			}

			public override void Define (TypeContainer parent)
			{
				base.Define (parent);

				Spec = new MethodSpec (MemberKind.Method, parent.PartialContainer.Definition, this, ReturnType, ParameterInfo, ModFlags);

				method_data = new MethodData (method, ModFlags, flags, this);

				method_data.Define (parent.PartialContainer, method.GetFullName (MemberName));
			}

			public override TypeSpec ReturnType {
				get {
					return method.MemberType;
				}
			}

			public override ParametersCompiled ParameterInfo {
				get {
					return ParametersCompiled.EmptyReadOnlyParameters;
				}
			}

			public override string[] ValidAttributeTargets {
				get {
					return attribute_targets;
				}
			}
		}

		public class SetMethod : PropertyMethod {

			static readonly string[] attribute_targets = new string[] { "method", "param", "return" };

			internal const string Prefix = "set_";

			protected ParametersCompiled parameters;

			public SetMethod (PropertyBase method, Modifiers modifiers, ParametersCompiled parameters, Attributes attrs, Location loc)
				: base (method, Prefix, modifiers, attrs, loc)
			{
				this.parameters = parameters;
			}

			protected override void ApplyToExtraTarget (Attribute a, MethodSpec ctor, byte[] cdata, PredefinedAttributes pa)
			{
				if (a.Target == AttributeTargets.Parameter) {
					parameters[0].ApplyAttributeBuilder (a, ctor, cdata, pa);
					return;
				}

				base.ApplyToExtraTarget (a, ctor, cdata, pa);
			}

			public override ParametersCompiled ParameterInfo {
			    get {
			        return parameters;
			    }
			}

			public override void Define (TypeContainer parent)
			{
				parameters.Resolve (this);
				
				base.Define (parent);

				Spec = new MethodSpec (MemberKind.Method, parent.PartialContainer.Definition, this, ReturnType, ParameterInfo, ModFlags);

				method_data = new MethodData (method, ModFlags, flags, this);

				method_data.Define (parent.PartialContainer, method.GetFullName (MemberName));
			}

			public override TypeSpec ReturnType {
				get {
					return Parent.Compiler.BuiltinTypes.Void;
				}
			}

			public override string[] ValidAttributeTargets {
				get {
					return attribute_targets;
				}
			}
		}

		static readonly string[] attribute_targets = new string[] { "property" };

		public abstract class PropertyMethod : AbstractPropertyEventMethod
		{
			const Modifiers AllowedModifiers =
				Modifiers.PUBLIC |
				Modifiers.PROTECTED |
				Modifiers.INTERNAL |
				Modifiers.PRIVATE;
		
			protected readonly PropertyBase method;
			protected MethodAttributes flags;

			public PropertyMethod (PropertyBase method, string prefix, Modifiers modifiers, Attributes attrs, Location loc)
				: base (method, prefix, attrs, loc)
			{
				this.method = method;
				this.ModFlags = ModifiersExtensions.Check (AllowedModifiers, modifiers, 0, loc, Report);
				this.ModFlags |= (method.ModFlags & (Modifiers.STATIC | Modifiers.UNSAFE));
			}

			public override void ApplyAttributeBuilder (Attribute a, MethodSpec ctor, byte[] cdata, PredefinedAttributes pa)
			{
				if (a.Type == pa.MethodImpl) {
					method.is_external_implementation = a.IsInternalCall ();
				}

				base.ApplyAttributeBuilder (a, ctor, cdata, pa);
			}

			public override AttributeTargets AttributeTargets {
				get {
					return AttributeTargets.Method;
				}
			}

			public override bool IsClsComplianceRequired ()
			{
				return method.IsClsComplianceRequired ();
			}

			public virtual void Define (TypeContainer parent)
			{
				var container = parent.PartialContainer;

				//
				// Check for custom access modifier
				//
				if ((ModFlags & Modifiers.AccessibilityMask) == 0) {
					ModFlags |= method.ModFlags;
					flags = method.flags;
				} else {
					if (container.Kind == MemberKind.Interface)
						Report.Error (275, Location, "`{0}': accessibility modifiers may not be used on accessors in an interface",
							GetSignatureForError ());
					else if ((method.ModFlags & Modifiers.ABSTRACT) != 0 && (ModFlags & Modifiers.PRIVATE) != 0) {
						Report.Error (442, Location, "`{0}': abstract properties cannot have private accessors", GetSignatureForError ());
					}

					CheckModifiers (ModFlags);
					ModFlags |= (method.ModFlags & (~Modifiers.AccessibilityMask));
					ModFlags |= Modifiers.PROPERTY_CUSTOM;
					flags = ModifiersExtensions.MethodAttr (ModFlags);
					flags |= (method.flags & (~MethodAttributes.MemberAccessMask));
				}

				CheckAbstractAndExtern (block != null);
				CheckProtectedModifier ();

				if (block != null) {
					if (block.IsIterator)
						Iterator.CreateIterator (this, Parent.PartialContainer, ModFlags);

					if (Compiler.Settings.WriteMetadataOnly)
						block = null;
				}
			}

			public bool HasCustomAccessModifier {
				get {
					return (ModFlags & Modifiers.PROPERTY_CUSTOM) != 0;
				}
			}

			public PropertyBase Property {
				get {
					return method;
				}
			}

			public override ObsoleteAttribute GetAttributeObsolete ()
			{
				return method.GetAttributeObsolete ();
			}

			public override string GetSignatureForError()
			{
				return method.GetSignatureForError () + "." + prefix.Substring (0, 3);
			}

			void CheckModifiers (Modifiers modflags)
			{
				if (!ModifiersExtensions.IsRestrictedModifier (modflags & Modifiers.AccessibilityMask, method.ModFlags & Modifiers.AccessibilityMask)) {
					Report.Error (273, Location,
						"The accessibility modifier of the `{0}' accessor must be more restrictive than the modifier of the property or indexer `{1}'",
						GetSignatureForError (), method.GetSignatureForError ());
				}
			}
		}

		PropertyMethod get, set, first;
		PropertyBuilder PropertyBuilder;

		protected PropertyBase (TypeDefinition parent, FullNamedExpression type, Modifiers mod_flags, Modifiers allowed_mod, MemberName name, Attributes attrs)
			: base (parent, type, mod_flags, allowed_mod, name, attrs)
		{
		}

		#region Properties

		public override AttributeTargets AttributeTargets {
			get {
				return AttributeTargets.Property;
			}
		}

		public PropertyMethod AccessorFirst {
			get {
				return first;
			}
		}

		public PropertyMethod AccessorSecond {
			get {
				return first == get ? set : get;
			}
		}

		public override Variance ExpectedMemberTypeVariance {
			get {
				return (get != null && set != null) ?
					Variance.None : set == null ?
					Variance.Covariant :
					Variance.Contravariant;
			}
		}

		public PropertyMethod Get {
			get {
				return get;
			}
			set {
				get = value;
				if (first == null)
					first = value;

				Parent.AddNameToContainer (get, get.MemberName.Basename);
			}
		}

		public PropertyMethod Set {
			get {
				return set;
			}
			set {
				set = value;
				if (first == null)
					first = value;

				Parent.AddNameToContainer (set, set.MemberName.Basename);
			}
		}

		public override string[] ValidAttributeTargets {
			get {
				return attribute_targets;
			}
		}

		#endregion

		public override void ApplyAttributeBuilder (Attribute a, MethodSpec ctor, byte[] cdata, PredefinedAttributes pa)
		{
			if (a.HasSecurityAttribute) {
				a.Error_InvalidSecurityParent ();
				return;
			}

			if (a.Type == pa.Dynamic) {
				a.Error_MisusedDynamicAttribute ();
				return;
			}

			PropertyBuilder.SetCustomAttribute ((ConstructorInfo) ctor.GetMetaInfo (), cdata);
		}

		void CheckMissingAccessor (MemberKind kind, ParametersCompiled parameters, bool get)
		{
			if (IsExplicitImpl) {
				MemberFilter filter;
				if (kind == MemberKind.Indexer)
					filter = new MemberFilter (MemberCache.IndexerNameAlias, 0, kind, parameters, null);
				else
					filter = new MemberFilter (MemberName.Name, 0, kind, null, null);

				var implementing = MemberCache.FindMember (InterfaceType, filter, BindingRestriction.DeclaredOnly) as PropertySpec;

				if (implementing == null)
					return;

				var accessor = get ? implementing.Get : implementing.Set;
				if (accessor != null) {
					Report.SymbolRelatedToPreviousError (accessor);
					Report.Error (551, Location, "Explicit interface implementation `{0}' is missing accessor `{1}'",
						GetSignatureForError (), accessor.GetSignatureForError ());
				}
			}
		}

		protected override bool CheckOverrideAgainstBase (MemberSpec base_member)
		{
			var ok = base.CheckOverrideAgainstBase (base_member);

			//
			// Check base property accessors conflict
			//
			var base_prop = (PropertySpec) base_member;
			if (Get == null) {
				if ((ModFlags & Modifiers.SEALED) != 0 && base_prop.HasGet && !base_prop.Get.IsAccessible (this)) {
					// TODO: Should be different error code but csc uses for some reason same
					Report.SymbolRelatedToPreviousError (base_prop);
					Report.Error (545, Location,
						"`{0}': cannot override because `{1}' does not have accessible get accessor",
						GetSignatureForError (), base_prop.GetSignatureForError ());
					ok = false;
				}
			} else {
				if (!base_prop.HasGet) {
					if (ok) {
						Report.SymbolRelatedToPreviousError (base_prop);
						Report.Error (545, Get.Location,
							"`{0}': cannot override because `{1}' does not have an overridable get accessor",
							Get.GetSignatureForError (), base_prop.GetSignatureForError ());
						ok = false;
					}
				} else if (Get.HasCustomAccessModifier || base_prop.HasDifferentAccessibility) {
					if (!CheckAccessModifiers (Get, base_prop.Get)) {
						Error_CannotChangeAccessModifiers (Get, base_prop.Get);
						ok = false;
					}
				}
			}

			if (Set == null) {
				if ((ModFlags & Modifiers.SEALED) != 0 && base_prop.HasSet && !base_prop.Set.IsAccessible (this)) {
					// TODO: Should be different error code but csc uses for some reason same
					Report.SymbolRelatedToPreviousError (base_prop);
					Report.Error (546, Location,
						"`{0}': cannot override because `{1}' does not have accessible set accessor",
						GetSignatureForError (), base_prop.GetSignatureForError ());
					ok = false;
				}
			} else {
				if (!base_prop.HasSet) {
					if (ok) {
						Report.SymbolRelatedToPreviousError (base_prop);
						Report.Error (546, Set.Location,
							"`{0}': cannot override because `{1}' does not have an overridable set accessor",
							Set.GetSignatureForError (), base_prop.GetSignatureForError ());
						ok = false;
					}
				} else if (Set.HasCustomAccessModifier || base_prop.HasDifferentAccessibility) {
					if (!CheckAccessModifiers (Set, base_prop.Set)) {
						Error_CannotChangeAccessModifiers (Set, base_prop.Set);
						ok = false;
					}
				}
			}

			if ((Set == null || !Set.HasCustomAccessModifier) && (Get == null || !Get.HasCustomAccessModifier)) {
				if (!CheckAccessModifiers (this, base_prop)) {
					Error_CannotChangeAccessModifiers (this, base_prop);
					ok = false;
				}
			}

			return ok;
		}

		protected override void DoMemberTypeDependentChecks ()
		{
			base.DoMemberTypeDependentChecks ();

			IsTypePermitted ();

			if (MemberType.IsStatic)
				Error_StaticReturnType ();
		}

		protected override void DoMemberTypeIndependentChecks ()
		{
			base.DoMemberTypeIndependentChecks ();

			//
			// Accessors modifiers check
			//
			if (AccessorSecond != null) {
				if ((Get.ModFlags & Modifiers.AccessibilityMask) != 0 && (Set.ModFlags & Modifiers.AccessibilityMask) != 0) {
					Report.Error (274, Location, "`{0}': Cannot specify accessibility modifiers for both accessors of the property or indexer",
						GetSignatureForError ());
				}
			} else if ((ModFlags & Modifiers.OVERRIDE) == 0 && 
				(Get == null && (Set.ModFlags & Modifiers.AccessibilityMask) != 0) ||
				(Set == null && (Get.ModFlags & Modifiers.AccessibilityMask) != 0)) {
				Report.Error (276, Location, 
					      "`{0}': accessibility modifiers on accessors may only be used if the property or indexer has both a get and a set accessor",
					      GetSignatureForError ());
			}
		}

		protected bool DefineAccessors ()
		{
			first.Define (Parent);
			if (AccessorSecond != null)
				AccessorSecond.Define (Parent);

			return true;
		}

		protected void DefineBuilders (MemberKind kind, ParametersCompiled parameters)
		{
			PropertyBuilder = Parent.TypeBuilder.DefineProperty (
				GetFullName (MemberName), PropertyAttributes.None,
#if !BOOTSTRAP_BASIC	// Requires trunk version mscorlib
				IsStatic ? 0 : CallingConventions.HasThis,
#endif
				MemberType.GetMetaInfo (), null, null,
				parameters.GetMetaInfo (), null, null);

			PropertySpec spec;
			if (kind == MemberKind.Indexer)
				spec = new IndexerSpec (Parent.Definition, this, MemberType, parameters, PropertyBuilder, ModFlags);
			else
				spec = new PropertySpec (kind, Parent.Definition, this, MemberType, PropertyBuilder, ModFlags);

			if (Get != null) {
				spec.Get = Get.Spec;
				Parent.MemberCache.AddMember (this, Get.Spec.Name, Get.Spec);
			} else {
				CheckMissingAccessor (kind, parameters, true);
			}

			if (Set != null) {
				spec.Set = Set.Spec;
				Parent.MemberCache.AddMember (this, Set.Spec.Name, Set.Spec);
			} else {
				CheckMissingAccessor (kind, parameters, false);
			}

			Parent.MemberCache.AddMember (this, PropertyBuilder.Name, spec);
		}

		public override void Emit ()
		{
			CheckReservedNameConflict (GetMethod.Prefix, get == null ? null : get.Spec);
			CheckReservedNameConflict (SetMethod.Prefix, set == null ? null : set.Spec);

			if (OptAttributes != null)
				OptAttributes.Emit ();

			if (member_type.BuiltinType == BuiltinTypeSpec.Type.Dynamic) {
				Module.PredefinedAttributes.Dynamic.EmitAttribute (PropertyBuilder);
			} else if (member_type.HasDynamicElement) {
				Module.PredefinedAttributes.Dynamic.EmitAttribute (PropertyBuilder, member_type, Location);
			}

			ConstraintChecker.Check (this, member_type, type_expr.Location);

			first.Emit (Parent);
			if (AccessorSecond != null)
				AccessorSecond.Emit (Parent);

			base.Emit ();
		}

		public override bool IsUsed {
			get {
				if (IsExplicitImpl)
					return true;

				return Get.IsUsed | Set.IsUsed;
			}
		}

		public override void PrepareEmit ()
		{
			AccessorFirst.PrepareEmit ();
			if (AccessorSecond != null)
				AccessorSecond.PrepareEmit ();

			if (get != null) {
				var method = Get.Spec.GetMetaInfo () as MethodBuilder;
				if (method != null)
					PropertyBuilder.SetGetMethod (method);
			}

			if (set != null) {
				var method = Set.Spec.GetMetaInfo () as MethodBuilder;
				if (method != null)
					PropertyBuilder.SetSetMethod (method);
			}
		}

		protected override void SetMemberName (MemberName new_name)
		{
			base.SetMemberName (new_name);

			if (Get != null)
				Get.UpdateName (this);

			if (Set != null)
				Set.UpdateName (this);
		}

		public override void WriteDebugSymbol (MonoSymbolFile file)
		{
			if (get != null)
				get.WriteDebugSymbol (file);

			if (set != null)
				set.WriteDebugSymbol (file);
		}

		//
		//   Represents header string for documentation comment.
		//
		public override string DocCommentHeader {
			get { return "P:"; }
		}
	}
			
	public class Property : PropertyBase
	{
		public sealed class BackingField : Field
		{
			readonly Property property;
			const Modifiers DefaultModifiers = Modifiers.BACKING_FIELD | Modifiers.COMPILER_GENERATED | Modifiers.PRIVATE | Modifiers.DEBUGGER_HIDDEN;

			public BackingField (Property p, bool readOnly)
				: base (p.Parent, p.type_expr, DefaultModifiers | (p.ModFlags & (Modifiers.STATIC | Modifiers.UNSAFE)),
				new MemberName ("<" + p.GetFullName (p.MemberName) + ">k__BackingField", p.Location), null)
			{
				this.property = p;
				if (readOnly)
					ModFlags |= Modifiers.READONLY;
			}

			public Property OriginalProperty {
				get {
					return property;
				}
			}

			public override string GetSignatureForError ()
			{
				return property.GetSignatureForError ();
			}
		}

		static readonly string[] attribute_target_auto = new string[] { "property", "field" };

		Field backing_field;

		public Property (TypeDefinition parent, FullNamedExpression type, Modifiers mod,
				 MemberName name, Attributes attrs)
			: base (parent, type, mod,
				parent.PartialContainer.Kind == MemberKind.Interface ? AllowedModifiersInterface :
				parent.PartialContainer.Kind == MemberKind.Struct ? AllowedModifiersStruct :
				AllowedModifiersClass,
				name, attrs)
		{
		}

		public Expression Initializer { get; set; }

		public override void Accept (StructuralVisitor visitor)
		{
			visitor.Visit (this);
		}

		public override void ApplyAttributeBuilder (Attribute a, MethodSpec ctor, byte[] cdata, PredefinedAttributes pa)
		{
			if (a.Target == AttributeTargets.Field) {
				backing_field.ApplyAttributeBuilder (a, ctor, cdata, pa);
				return;
			}

			base.ApplyAttributeBuilder (a, ctor, cdata, pa);
		}

		void CreateAutomaticProperty ()
		{
			// Create backing field
			backing_field = new BackingField (this, Initializer != null && Set == null);
			if (!backing_field.Define ())
				return;

			if (Initializer != null) {
				backing_field.Initializer = Initializer;
				Parent.RegisterFieldForInitialization (backing_field, new FieldInitializer (backing_field, Initializer, Location));
				backing_field.ModFlags |= Modifiers.READONLY;
			}

			Parent.PartialContainer.Members.Add (backing_field);

			FieldExpr fe = new FieldExpr (backing_field, Location);
			if ((backing_field.ModFlags & Modifiers.STATIC) == 0)
				fe.InstanceExpression = new CompilerGeneratedThis (Parent.CurrentType, Location);

			//
			// Create get block but we careful with location to
			// emit only single sequence point per accessor. This allow
			// to set a breakpoint on it even with no user code
			//
			Get.Block = new ToplevelBlock (Compiler, ParametersCompiled.EmptyReadOnlyParameters, Location.Null);
			Return r = new Return (fe, Get.Location);
			Get.Block.AddStatement (r);
			Get.ModFlags |= Modifiers.COMPILER_GENERATED;

			// Create set block
			if (Set != null) {
				Set.Block = new ToplevelBlock (Compiler, Set.ParameterInfo, Location.Null);
				Assign a = new SimpleAssign (fe, new SimpleName ("value", Location.Null), Location.Null);
				Set.Block.AddStatement (new StatementExpression (a, Set.Location));
				Set.ModFlags |= Modifiers.COMPILER_GENERATED;
			}
		}
		
		public override bool Define ()
		{
			if (!base.Define ())
				return false;

			flags |= MethodAttributes.HideBySig | MethodAttributes.SpecialName;

			bool auto = AccessorFirst.Block == null && (AccessorSecond == null || AccessorSecond.Block == null) &&
				(ModFlags & (Modifiers.ABSTRACT | Modifiers.EXTERN)) == 0;

			if (Initializer != null) {
				if (!auto)
					Report.Error (8050, Location, "`{0}': Only auto-implemented properties can have initializers",
						GetSignatureForError ());

				if (IsInterface)
					Report.Error (8053, Location, "`{0}': Properties inside interfaces cannot have initializers",
						GetSignatureForError ());

				if (Compiler.Settings.Version < LanguageVersion.V_6)
					Report.FeatureIsNotAvailable (Compiler, Location, "auto-implemented property initializer");
			}

			if (auto) {
				if (Get == null) {
					Report.Error (8052, Location, "Auto-implemented property `{0}' must have get accessor",
						GetSignatureForError ());
					return false;
				}

				if (Initializer == null && AccessorSecond == null) {
					Report.Error (8051, Location, "Auto-implemented property `{0}' must have set accessor or initializer",
						GetSignatureForError ());
				}

				if (Compiler.Settings.Version < LanguageVersion.V_3 && Initializer == null)
					Report.FeatureIsNotAvailable (Compiler, Location, "auto-implemented properties");

				CreateAutomaticProperty ();
			}

			if (!DefineAccessors ())
				return false;

			if (AccessorSecond == null) {
				PropertyMethod pm;
				if (AccessorFirst is GetMethod)
					pm = new SetMethod (this, 0, ParametersCompiled.EmptyReadOnlyParameters, null, Location);
				else
					pm = new GetMethod (this, 0, null, Location);

				Parent.AddNameToContainer (pm, pm.MemberName.Basename);
			}

			if (!CheckBase ())
				return false;

			DefineBuilders (MemberKind.Property, ParametersCompiled.EmptyReadOnlyParameters);
			return true;
		}

		public override void Emit ()
		{
			if ((AccessorFirst.ModFlags & (Modifiers.STATIC | Modifiers.COMPILER_GENERATED)) == Modifiers.COMPILER_GENERATED && Parent.PartialContainer.HasExplicitLayout) {
				Report.Error (842, Location,
					"Automatically implemented property `{0}' cannot be used inside a type with an explicit StructLayout attribute",
					GetSignatureForError ());
			}

			base.Emit ();
		}

		public override string[] ValidAttributeTargets {
			get {
				return Get != null && ((Get.ModFlags & Modifiers.COMPILER_GENERATED) != 0) ?
					attribute_target_auto : base.ValidAttributeTargets;
			}
		}
	}

	/// <summary>
	/// For case when event is declared like property (with add and remove accessors).
	/// </summary>
	public class EventProperty: Event {
		public abstract class AEventPropertyAccessor : AEventAccessor
		{
			protected AEventPropertyAccessor (EventProperty method, string prefix, Attributes attrs, Location loc)
				: base (method, prefix, attrs, loc)
			{
			}

			public override void Define (TypeContainer ds)
			{
				CheckAbstractAndExtern (block != null);
				base.Define (ds);
			}
			
			public override string GetSignatureForError ()
			{
				return method.GetSignatureForError () + "." + prefix.Substring (0, prefix.Length - 1);
			}
		}

		public sealed class AddDelegateMethod: AEventPropertyAccessor
		{
			public AddDelegateMethod (EventProperty method, Attributes attrs, Location loc)
				: base (method, AddPrefix, attrs, loc)
			{
			}
		}

		public sealed class RemoveDelegateMethod: AEventPropertyAccessor
		{
			public RemoveDelegateMethod (EventProperty method, Attributes attrs, Location loc)
				: base (method, RemovePrefix, attrs, loc)
			{
			}
		}

		static readonly string[] attribute_targets = new string [] { "event" };

		public EventProperty (TypeDefinition parent, FullNamedExpression type, Modifiers mod_flags, MemberName name, Attributes attrs)
			: base (parent, type, mod_flags, name, attrs)
		{
		}

		public override void Accept (StructuralVisitor visitor)
		{
			visitor.Visit (this);
		}
		
		public override bool Define()
		{
			if (!base.Define ())
				return false;

			SetIsUsed ();
			return true;
		}

		public override string[] ValidAttributeTargets {
			get {
				return attribute_targets;
			}
		}
	}

	/// <summary>
	/// Event is declared like field.
	/// </summary>
	public class EventField : Event
	{
		abstract class EventFieldAccessor : AEventAccessor
		{
			protected EventFieldAccessor (EventField method, string prefix)
				: base (method, prefix, null, method.Location)
			{
			}

			protected abstract MethodSpec GetOperation (Location loc);

			public override void Emit (TypeDefinition parent)
			{
				if ((method.ModFlags & (Modifiers.ABSTRACT | Modifiers.EXTERN)) == 0 && !Compiler.Settings.WriteMetadataOnly) {
					block = new ToplevelBlock (Compiler, ParameterInfo, Location) {
						IsCompilerGenerated = true
					};
					FabricateBodyStatement ();
				}

				base.Emit (parent);
			}

			void FabricateBodyStatement ()
			{
				//
				// Delegate obj1 = backing_field
				// do {
				//   Delegate obj2 = obj1;
				//   obj1 = Interlocked.CompareExchange (ref backing_field, Delegate.Combine|Remove(obj2, value), obj1);
				// } while ((object)obj1 != (object)obj2)
				//

				var field_info = ((EventField) method).backing_field;
				FieldExpr f_expr = new FieldExpr (field_info, Location);
				if (!IsStatic)
					f_expr.InstanceExpression = new CompilerGeneratedThis (Parent.CurrentType, Location);

				var obj1 = LocalVariable.CreateCompilerGenerated (field_info.MemberType, block, Location);
				var obj2 = LocalVariable.CreateCompilerGenerated (field_info.MemberType, block, Location);

				block.AddStatement (new StatementExpression (new SimpleAssign (new LocalVariableReference (obj1, Location), f_expr)));

				var cond = new BooleanExpression (new Binary (Binary.Operator.Inequality,
					new Cast (new TypeExpression (Module.Compiler.BuiltinTypes.Object, Location), new LocalVariableReference (obj1, Location), Location),
					new Cast (new TypeExpression (Module.Compiler.BuiltinTypes.Object, Location), new LocalVariableReference (obj2, Location), Location)));

				var body = new ExplicitBlock (block, Location, Location);
				block.AddStatement (new Do (body, cond, Location, Location));

				body.AddStatement (new StatementExpression (
					new SimpleAssign (new LocalVariableReference (obj2, Location), new LocalVariableReference (obj1, Location))));

				var args_oper = new Arguments (2);
				args_oper.Add (new Argument (new LocalVariableReference (obj2, Location)));
				args_oper.Add (new Argument (block.GetParameterReference (0, Location)));

				var op_method = GetOperation (Location);

				var args = new Arguments (3);
				args.Add (new Argument (f_expr, Argument.AType.Ref));
				args.Add (new Argument (new Cast (
					new TypeExpression (field_info.MemberType, Location),
					new Invocation (MethodGroupExpr.CreatePredefined (op_method, op_method.DeclaringType, Location), args_oper),
					Location)));
				args.Add (new Argument (new LocalVariableReference (obj1, Location)));

				var cas = Module.PredefinedMembers.InterlockedCompareExchange_T.Get ();
				if (cas == null) {
					if (Module.PredefinedMembers.MonitorEnter_v4.Get () != null || Module.PredefinedMembers.MonitorEnter.Get () != null) {
						// Workaround for cripled (e.g. microframework) mscorlib without CompareExchange
						body.AddStatement (new Lock (
							block.GetParameterReference (0, Location),
							new StatementExpression (new SimpleAssign (
								f_expr, args [1].Expr, Location), Location), Location));
					} else {
						Module.PredefinedMembers.InterlockedCompareExchange_T.Resolve (Location);
					}
				} else {
					body.AddStatement (new StatementExpression (new SimpleAssign (
						new LocalVariableReference (obj1, Location),
						new Invocation (MethodGroupExpr.CreatePredefined (cas, cas.DeclaringType, Location), args))));
				}
			}
		}

		sealed class AddDelegateMethod: EventFieldAccessor
		{
			public AddDelegateMethod (EventField method):
				base (method, AddPrefix)
			{
			}

			protected override MethodSpec GetOperation (Location loc)
			{
				return Module.PredefinedMembers.DelegateCombine.Resolve (loc);
			}
		}

		sealed class RemoveDelegateMethod: EventFieldAccessor
		{
			public RemoveDelegateMethod (EventField method):
				base (method, RemovePrefix)
			{
			}

			protected override MethodSpec GetOperation (Location loc)
			{
				return Module.PredefinedMembers.DelegateRemove.Resolve (loc);
			}
		}


		static readonly string[] attribute_targets = new string [] { "event", "field", "method" };
		static readonly string[] attribute_targets_interface = new string[] { "event", "method" };

		Expression initializer;
		Field backing_field;
		List<FieldDeclarator> declarators;

		public EventField (TypeDefinition parent, FullNamedExpression type, Modifiers mod_flags, MemberName name, Attributes attrs)
			: base (parent, type, mod_flags, name, attrs)
		{
			Add = new AddDelegateMethod (this);
			Remove = new RemoveDelegateMethod (this);
		}

		#region Properties

		public List<FieldDeclarator> Declarators {
			get {
				return this.declarators;
			}
		}

		bool HasBackingField {
			get {
				return !IsInterface && (ModFlags & Modifiers.ABSTRACT) == 0;
			}
		}

		public Expression Initializer {
			get {
				return initializer;
			}
			set {
				initializer = value;
			}
		}

		public override string[] ValidAttributeTargets {
			get {
				return HasBackingField ? attribute_targets : attribute_targets_interface;
			}
		}

		#endregion

		
		public override void Accept (StructuralVisitor visitor)
		{
			visitor.Visit (this);
		}

		public void AddDeclarator (FieldDeclarator declarator)
		{
			if (declarators == null)
				declarators = new List<FieldDeclarator> (2);

			declarators.Add (declarator);

			Parent.AddNameToContainer (this, declarator.Name.Value);
		}

		public override void ApplyAttributeBuilder (Attribute a, MethodSpec ctor, byte[] cdata, PredefinedAttributes pa)
		{
			if (a.Target == AttributeTargets.Field) {
				backing_field.ApplyAttributeBuilder (a, ctor, cdata, pa);
				return;
			}

			if (a.Target == AttributeTargets.Method) {
				int errors = Report.Errors;
				Add.ApplyAttributeBuilder (a, ctor, cdata, pa);
				if (errors == Report.Errors)
					Remove.ApplyAttributeBuilder (a, ctor, cdata, pa);
				return;
			}

			base.ApplyAttributeBuilder (a, ctor, cdata, pa);
		}

		public override bool Define()
		{
			var mod_flags_src = ModFlags;

			if (!base.Define ())
				return false;

			if (declarators != null) {
				if ((mod_flags_src & Modifiers.DEFAULT_ACCESS_MODIFIER) != 0)
					mod_flags_src &= ~(Modifiers.AccessibilityMask | Modifiers.DEFAULT_ACCESS_MODIFIER);

				var t = new TypeExpression (MemberType, TypeExpression.Location);
				foreach (var d in declarators) {
					var ef = new EventField (Parent, t, mod_flags_src, new MemberName (d.Name.Value, d.Name.Location), OptAttributes);

					if (d.Initializer != null)
						ef.initializer = d.Initializer;

					ef.Define ();
					Parent.PartialContainer.Members.Add (ef);
				}
			}

			if (!HasBackingField) {
				SetIsUsed ();
				return true;
			}

			backing_field = new Field (Parent,
				new TypeExpression (MemberType, Location),
				Modifiers.BACKING_FIELD | Modifiers.COMPILER_GENERATED | Modifiers.PRIVATE | (ModFlags & (Modifiers.STATIC | Modifiers.UNSAFE)),
				MemberName, null);

			Parent.PartialContainer.Members.Add (backing_field);
			backing_field.Initializer = Initializer;
			backing_field.ModFlags &= ~Modifiers.COMPILER_GENERATED;

			// Call define because we passed fields definition
			backing_field.Define ();

			// Set backing field for event fields
			spec.BackingField = backing_field.Spec;

			return true;
		}
	}

	public abstract class Event : PropertyBasedMember
	{
		public abstract class AEventAccessor : AbstractPropertyEventMethod
		{
			protected readonly Event method;
			readonly ParametersCompiled parameters;

			static readonly string[] attribute_targets = new string [] { "method", "param", "return" };

			public const string AddPrefix = "add_";
			public const string RemovePrefix = "remove_";

			protected AEventAccessor (Event method, string prefix, Attributes attrs, Location loc)
				: base (method, prefix, attrs, loc)
			{
				this.method = method;
				this.ModFlags = method.ModFlags;
				this.parameters = ParametersCompiled.CreateImplicitParameter (method.TypeExpression, loc);
			}

			public bool IsInterfaceImplementation {
				get { return method_data.implementing != null; }
			}

			public override void ApplyAttributeBuilder (Attribute a, MethodSpec ctor, byte[] cdata, PredefinedAttributes pa)
			{
				if (a.Type == pa.MethodImpl) {
					method.is_external_implementation = a.IsInternalCall ();
				}

				base.ApplyAttributeBuilder (a, ctor, cdata, pa);
			}

			protected override void ApplyToExtraTarget (Attribute a, MethodSpec ctor, byte[] cdata, PredefinedAttributes pa)
			{
				if (a.Target == AttributeTargets.Parameter) {
					parameters[0].ApplyAttributeBuilder (a, ctor, cdata, pa);
					return;
				}

				base.ApplyToExtraTarget (a, ctor, cdata, pa);
			}

			public override AttributeTargets AttributeTargets {
				get {
					return AttributeTargets.Method;
				}
			}

			public override bool IsClsComplianceRequired ()
			{
				return method.IsClsComplianceRequired ();
			}

			public virtual void Define (TypeContainer parent)
			{
				// Fill in already resolved event type to speed things up and
				// avoid confusing duplicate errors
				((Parameter) parameters.FixedParameters[0]).Type = method.member_type;
				parameters.Types = new TypeSpec[] { method.member_type };

				method_data = new MethodData (method, method.ModFlags,
					method.flags | MethodAttributes.HideBySig | MethodAttributes.SpecialName, this);

				if (!method_data.Define (parent.PartialContainer, method.GetFullName (MemberName)))
					return;

				if (Compiler.Settings.WriteMetadataOnly)
					block = null;

				Spec = new MethodSpec (MemberKind.Method, parent.PartialContainer.Definition, this, ReturnType, ParameterInfo, method.ModFlags);
				Spec.IsAccessor = true;
			}

			public override TypeSpec ReturnType {
				get {
					return Parent.Compiler.BuiltinTypes.Void;
				}
			}

			public override ObsoleteAttribute GetAttributeObsolete ()
			{
				return method.GetAttributeObsolete ();
			}

			public MethodData MethodData {
				get {
					return method_data;
				}
			}

			public override string[] ValidAttributeTargets {
				get {
					return attribute_targets;
				}
			}

			public override ParametersCompiled ParameterInfo {
				get {
					return parameters;
				}
			}
		}

		AEventAccessor add, remove;
		EventBuilder EventBuilder;
		protected EventSpec spec;

		protected Event (TypeDefinition parent, FullNamedExpression type, Modifiers mod_flags, MemberName name, Attributes attrs)
			: base (parent, type, mod_flags,
				parent.PartialContainer.Kind == MemberKind.Interface ? AllowedModifiersInterface :
				parent.PartialContainer.Kind == MemberKind.Struct ? AllowedModifiersStruct :
				AllowedModifiersClass,
				name, attrs)
		{
		}

		#region Properties

		public override AttributeTargets AttributeTargets {
			get {
				return AttributeTargets.Event;
			}
		}

		public AEventAccessor Add {
			get {
				return this.add;
			}
			set {
				add = value;
				Parent.AddNameToContainer (value, value.MemberName.Basename);
			}
		}

		public override Variance ExpectedMemberTypeVariance {
			get {
				return Variance.Contravariant;
			}
		}

		public AEventAccessor Remove {
			get {
				return this.remove;
			}
			set {
				remove = value;
				Parent.AddNameToContainer (value, value.MemberName.Basename);
			}
		}
		#endregion

		public override void ApplyAttributeBuilder (Attribute a, MethodSpec ctor, byte[] cdata, PredefinedAttributes pa)
		{
			if ((a.HasSecurityAttribute)) {
				a.Error_InvalidSecurityParent ();
				return;
			}

			EventBuilder.SetCustomAttribute ((ConstructorInfo) ctor.GetMetaInfo (), cdata);
		}

		protected override bool CheckOverrideAgainstBase (MemberSpec base_member)
		{
			var ok = base.CheckOverrideAgainstBase (base_member);

			if (!CheckAccessModifiers (this, base_member)) {
				Error_CannotChangeAccessModifiers (this, base_member);
				ok = false;
			}

			return ok;
		}

		public override bool Define ()
		{
			if (!base.Define ())
				return false;

			if (!MemberType.IsDelegate) {
				Report.Error (66, Location, "`{0}': event must be of a delegate type", GetSignatureForError ());
			}

			if (!CheckBase ())
				return false;

			//
			// Now define the accessors
			//
			add.Define (Parent);
			remove.Define (Parent);

			EventBuilder = Parent.TypeBuilder.DefineEvent (GetFullName (MemberName), EventAttributes.None, MemberType.GetMetaInfo ());

			spec = new EventSpec (Parent.Definition, this, MemberType, ModFlags, Add.Spec, remove.Spec);

			Parent.MemberCache.AddMember (this, GetFullName (MemberName), spec);
			Parent.MemberCache.AddMember (this, Add.Spec.Name, Add.Spec);
			Parent.MemberCache.AddMember (this, Remove.Spec.Name, remove.Spec);

			return true;
		}

		public override void Emit ()
		{
			CheckReservedNameConflict (null, add.Spec);
			CheckReservedNameConflict (null, remove.Spec);

			if (OptAttributes != null) {
				OptAttributes.Emit ();
			}

			ConstraintChecker.Check (this, member_type, type_expr.Location);

			Add.Emit (Parent);
			Remove.Emit (Parent);

			base.Emit ();
		}

		public override void PrepareEmit ()
		{
			add.PrepareEmit ();
			remove.PrepareEmit ();

			EventBuilder.SetAddOnMethod (add.MethodData.MethodBuilder);
			EventBuilder.SetRemoveOnMethod (remove.MethodData.MethodBuilder);
		}

		public override void WriteDebugSymbol (MonoSymbolFile file)
		{
			add.WriteDebugSymbol (file);
			remove.WriteDebugSymbol (file);
		}

		//
		//   Represents header string for documentation comment.
		//
		public override string DocCommentHeader {
			get { return "E:"; }
		}
	}

	public class EventSpec : MemberSpec, IInterfaceMemberSpec
	{
		MethodSpec add, remove;
		FieldSpec backing_field;

		public EventSpec (TypeSpec declaringType, IMemberDefinition definition, TypeSpec eventType, Modifiers modifiers, MethodSpec add, MethodSpec remove)
			: base (MemberKind.Event, declaringType, definition, modifiers)
		{
			this.AccessorAdd = add;
			this.AccessorRemove = remove;
			this.MemberType = eventType;
		}

		#region Properties

		public MethodSpec AccessorAdd { 
			get {
				return add;
			}
			set {
				add = value;
			}
		}

		public MethodSpec AccessorRemove {
			get {
				return remove;
			}
			set {
				remove = value;
			}
		}

		public FieldSpec BackingField {
			get {
				return backing_field;
			}
			set {
				backing_field = value;
			}
		}

		public TypeSpec MemberType { get; private set; }

		#endregion

		public override MemberSpec InflateMember (TypeParameterInflator inflator)
		{
			var es = (EventSpec) base.InflateMember (inflator);
			es.MemberType = inflator.Inflate (MemberType);

			if (backing_field != null)
				es.backing_field = (FieldSpec) backing_field.InflateMember (inflator);

			return es;
		}

		public override List<MissingTypeSpecReference> ResolveMissingDependencies (MemberSpec caller)
		{
			return MemberType.ResolveMissingDependencies (this);
		}
	}
 
	public class Indexer : PropertyBase, IParametersMember
	{
		public class GetIndexerMethod : GetMethod, IParametersMember
		{
			ParametersCompiled parameters;

			public GetIndexerMethod (PropertyBase property, Modifiers modifiers, ParametersCompiled parameters, Attributes attrs, Location loc)
				: base (property, modifiers, attrs, loc)
			{
				this.parameters = parameters;
			}

			public override void Define (TypeContainer parent)
			{
				// Disable reporting, parameters are resolved twice
				Report.DisableReporting ();
				try {
					parameters.Resolve (this);
				} finally {
					Report.EnableReporting ();
				}

				base.Define (parent);
			}

			public override ParametersCompiled ParameterInfo {
				get {
					return parameters;
				}
			}

			#region IParametersMember Members

			AParametersCollection IParametersMember.Parameters {
				get {
					return parameters;
				}
			}

			TypeSpec IInterfaceMemberSpec.MemberType {
				get {
					return ReturnType;
				}
			}

			#endregion
		}

		public class SetIndexerMethod : SetMethod, IParametersMember
		{
			public SetIndexerMethod (PropertyBase property, Modifiers modifiers, ParametersCompiled parameters, Attributes attrs, Location loc)
				: base (property, modifiers, parameters, attrs, loc)
			{
			}

			#region IParametersMember Members

			AParametersCollection IParametersMember.Parameters {
				get {
					return parameters;
				}
			}

			TypeSpec IInterfaceMemberSpec.MemberType {
				get {
					return ReturnType;
				}
			}

			#endregion
		}

		const Modifiers AllowedModifiers =
			Modifiers.NEW |
			Modifiers.PUBLIC |
			Modifiers.PROTECTED |
			Modifiers.INTERNAL |
			Modifiers.PRIVATE |
			Modifiers.VIRTUAL |
			Modifiers.SEALED |
			Modifiers.OVERRIDE |
			Modifiers.UNSAFE |
			Modifiers.EXTERN |
			Modifiers.ABSTRACT;

		const Modifiers AllowedInterfaceModifiers =
			Modifiers.NEW;

		readonly ParametersCompiled parameters;

		public Indexer (TypeDefinition parent, FullNamedExpression type, MemberName name, Modifiers mod, ParametersCompiled parameters, Attributes attrs)
			: base (parent, type, mod,
				parent.PartialContainer.Kind == MemberKind.Interface ? AllowedInterfaceModifiers : AllowedModifiers,
				name, attrs)
		{
			this.parameters = parameters;
		}

		#region Properties

		AParametersCollection IParametersMember.Parameters {
			get {
				return parameters;
			}
		}

		public ParametersCompiled ParameterInfo {
			get {
				return parameters;
			}
		}

		#endregion

		
		public override void Accept (StructuralVisitor visitor)
		{
			visitor.Visit (this);
		}

		public override void ApplyAttributeBuilder (Attribute a, MethodSpec ctor, byte[] cdata, PredefinedAttributes pa)
		{
			if (a.Type == pa.IndexerName) {
				// Attribute was copied to container
				return;
			}

			base.ApplyAttributeBuilder (a, ctor, cdata, pa);
		}

		protected override bool CheckForDuplications ()
		{
			return Parent.MemberCache.CheckExistingMembersOverloads (this, parameters);
		}
		
		public override bool Define ()
		{
			if (!base.Define ())
				return false;

			if (!DefineParameters (parameters))
				return false;

			if (OptAttributes != null) {
				Attribute indexer_attr = OptAttributes.Search (Module.PredefinedAttributes.IndexerName);
				if (indexer_attr != null) {
					var compiling = indexer_attr.Type.MemberDefinition as TypeContainer;
					if (compiling != null)
						compiling.Define ();

					if (IsExplicitImpl) {
						Report.Error (415, indexer_attr.Location,
							"The `{0}' attribute is valid only on an indexer that is not an explicit interface member declaration",
							indexer_attr.Type.GetSignatureForError ());
					} else if ((ModFlags & Modifiers.OVERRIDE) != 0) {
						Report.Error (609, indexer_attr.Location,
							"Cannot set the `IndexerName' attribute on an indexer marked override");
					} else {
						string name = indexer_attr.GetIndexerAttributeValue ();

						if (!string.IsNullOrEmpty (name)) {
							SetMemberName (new MemberName (MemberName.Left, name, Location));
						}
					}
				}
			}

			if (InterfaceType != null) {
				string base_IndexerName = InterfaceType.MemberDefinition.GetAttributeDefaultMember ();
				if (base_IndexerName != ShortName) {
					SetMemberName (new MemberName (MemberName.Left, base_IndexerName, new TypeExpression (InterfaceType, Location), Location));
				}
			}

			Parent.AddNameToContainer (this, MemberName.Basename);

			flags |= MethodAttributes.HideBySig | MethodAttributes.SpecialName;
			
			if (!DefineAccessors ())
				return false;

			if (!CheckBase ())
				return false;

			DefineBuilders (MemberKind.Indexer, parameters);
			return true;
		}

		public override bool EnableOverloadChecks (MemberCore overload)
		{
			if (overload is Indexer) {
				caching_flags |= Flags.MethodOverloadsExist;
				return true;
			}

			return base.EnableOverloadChecks (overload);
		}

		public override void Emit ()
		{
			parameters.CheckConstraints (this);

			base.Emit ();
		}

		public override string GetSignatureForError ()
		{
			StringBuilder sb = new StringBuilder (Parent.GetSignatureForError ());
			if (MemberName.ExplicitInterface != null) {
				sb.Append (".");
				sb.Append (MemberName.ExplicitInterface.GetSignatureForError ());
			}

			sb.Append (".this");
			sb.Append (parameters.GetSignatureForError ("[", "]", parameters.Count));
			return sb.ToString ();
		}

		public override string GetSignatureForDocumentation ()
		{
			return base.GetSignatureForDocumentation () + parameters.GetSignatureForDocumentation ();
		}

		protected override bool VerifyClsCompliance ()
		{
			if (!base.VerifyClsCompliance ())
				return false;

			parameters.VerifyClsCompliance (this);
			return true;
		}
	}

	public class IndexerSpec : PropertySpec, IParametersMember
	{
		AParametersCollection parameters;

		public IndexerSpec (TypeSpec declaringType, IMemberDefinition definition, TypeSpec memberType, AParametersCollection parameters, PropertyInfo info, Modifiers modifiers)
			: base (MemberKind.Indexer, declaringType, definition, memberType, info, modifiers)
		{
			this.parameters = parameters;
		}

		#region Properties
		public AParametersCollection Parameters {
			get {
				return parameters;
			}
		}
		#endregion

		public override string GetSignatureForDocumentation ()
		{
			return base.GetSignatureForDocumentation () + parameters.GetSignatureForDocumentation ();
		}

		public override string GetSignatureForError ()
		{
			return DeclaringType.GetSignatureForError () + ".this" + parameters.GetSignatureForError ("[", "]", parameters.Count);
		}

		public override MemberSpec InflateMember (TypeParameterInflator inflator)
		{
			var spec = (IndexerSpec) base.InflateMember (inflator);
			spec.parameters = parameters.Inflate (inflator);
			return spec;
		}

		public override List<MissingTypeSpecReference> ResolveMissingDependencies (MemberSpec caller)
		{
			var missing = base.ResolveMissingDependencies (caller);

			foreach (var pt in parameters.Types) {
				var m = pt.GetMissingDependencies (caller);
				if (m == null)
					continue;

				if (missing == null)
					missing = new List<MissingTypeSpecReference> ();

				missing.AddRange (m);
			}

			return missing;
		}
	}
}
