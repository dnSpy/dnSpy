//
// generic.cs: Generics support
//
// Authors: Martin Baulig (martin@ximian.com)
//          Miguel de Icaza (miguel@ximian.com)
//          Marek Safar (marek.safar@gmail.com)
//
// Dual licensed under the terms of the MIT X11 or GNU GPL
//
// Copyright 2001, 2002, 2003 Ximian, Inc (http://www.ximian.com)
// Copyright 2004-2008 Novell, Inc
// Copyright 2011 Xamarin, Inc (http://www.xamarin.com)
//

using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

#if STATIC
using MetaType = IKVM.Reflection.Type;
using IKVM.Reflection;
using IKVM.Reflection.Emit;
#else
using MetaType = System.Type;
using System.Reflection;
using System.Reflection.Emit;
#endif

namespace Mono.CSharp {
	public enum Variance
	{
		//
		// Don't add or modify internal values, they are used as -/+ calculation signs
		//
		None			= 0,
		Covariant		= 1,
		Contravariant	= -1
	}

	[Flags]
	public enum SpecialConstraint
	{
		None		= 0,
		Constructor = 1 << 2,
		Class		= 1 << 3,
		Struct		= 1 << 4
	}

	public class SpecialContraintExpr : FullNamedExpression
	{
		public SpecialContraintExpr (SpecialConstraint constraint, Location loc)
		{
			this.loc = loc;
			this.Constraint = constraint;
		}

		public SpecialConstraint Constraint { get; private set; }

		protected override Expression DoResolve (ResolveContext rc)
		{
			throw new NotImplementedException ();
		}

		public override FullNamedExpression ResolveAsTypeOrNamespace (IMemberContext ec)
		{
			throw new NotImplementedException ();
		}
	}

	//
	// A set of parsed constraints for a type parameter
	//
	public class Constraints
	{
		SimpleMemberName tparam;
		List<FullNamedExpression> constraints;
		Location loc;
		bool resolved;
		bool resolving;
		
		public IEnumerable<FullNamedExpression> ConstraintExpressions {
			get {
				return constraints;
			}
		}

		public Constraints (SimpleMemberName tparam, List<FullNamedExpression> constraints, Location loc)
		{
			this.tparam = tparam;
			this.constraints = constraints;
			this.loc = loc;
		}

		#region Properties

		public List<FullNamedExpression> TypeExpressions {
			get {
				return constraints;
			}
		}

		public Location Location {
			get {
				return loc;
			}
		}

		public SimpleMemberName TypeParameter {
			get {
				return tparam;
			}
		}

		#endregion

		public static bool CheckConflictingInheritedConstraint (TypeParameterSpec spec, TypeSpec bb, IMemberContext context, Location loc)
		{
			if (spec.HasSpecialClass && bb.IsStruct) {
				context.Module.Compiler.Report.Error (455, loc,
					"Type parameter `{0}' inherits conflicting constraints `{1}' and `{2}'",
					spec.Name, "class", bb.GetSignatureForError ());

				return false;
			}

			return CheckConflictingInheritedConstraint (spec, spec.BaseType, bb, context, loc);
		}

		static bool CheckConflictingInheritedConstraint (TypeParameterSpec spec, TypeSpec ba, TypeSpec bb, IMemberContext context, Location loc)
		{
			if (ba == bb)
				return true;

			if (TypeSpec.IsBaseClass (ba, bb, false) || TypeSpec.IsBaseClass (bb, ba, false))
				return true;

			Error_ConflictingConstraints (context, spec, ba, bb, loc);
			return false;
		}

		public static void Error_ConflictingConstraints (IMemberContext context, TypeParameterSpec tp, TypeSpec ba, TypeSpec bb, Location loc)
		{
			context.Module.Compiler.Report.Error (455, loc,
				"Type parameter `{0}' inherits conflicting constraints `{1}' and `{2}'",
				tp.Name, ba.GetSignatureForError (), bb.GetSignatureForError ());
		}

		public void CheckGenericConstraints (IMemberContext context, bool obsoleteCheck)
		{
			foreach (var c in constraints) {
				if (c == null)
					continue;

				var t = c.Type;
				if (t == null)
					continue;

				if (obsoleteCheck) {
					ObsoleteAttribute obsolete_attr = t.GetAttributeObsolete ();
					if (obsolete_attr != null)
						AttributeTester.Report_ObsoleteMessage (obsolete_attr, t.GetSignatureForError (), c.Location, context.Module.Compiler.Report);
				}

				ConstraintChecker.Check (context, t, c.Location);
			}
		}

		//
		// Resolve the constraints types with only possible early checks, return
		// value `false' is reserved for recursive failure
		//
		public bool Resolve (IMemberContext context, TypeParameter tp)
		{
			if (resolved)
				return true;

			if (resolving)
				return false;

			resolving = true;
			var spec = tp.Type;
			List<TypeParameterSpec> tparam_types = null;
			bool iface_found = false;

			spec.BaseType = context.Module.Compiler.BuiltinTypes.Object;

			for (int i = 0; i < constraints.Count; ++i) {
				var constraint = constraints[i];

				if (constraint is SpecialContraintExpr) {
					spec.SpecialConstraint |= ((SpecialContraintExpr) constraint).Constraint;
					if (spec.HasSpecialStruct)
						spec.BaseType = context.Module.Compiler.BuiltinTypes.ValueType;

					// Set to null as it does not have a type
					constraints[i] = null;
					continue;
				}

				var type = constraint.ResolveAsType (context);
				if (type == null)
					continue;

				if (type.Arity > 0 && ((InflatedTypeSpec) type).HasDynamicArgument ()) {
					context.Module.Compiler.Report.Error (1968, constraint.Location,
						"A constraint cannot be the dynamic type `{0}'", type.GetSignatureForError ());
					continue;
				}

				if (!context.CurrentMemberDefinition.IsAccessibleAs (type)) {
					context.Module.Compiler.Report.SymbolRelatedToPreviousError (type);
					context.Module.Compiler.Report.Error (703, loc,
						"Inconsistent accessibility: constraint type `{0}' is less accessible than `{1}'",
						type.GetSignatureForError (), context.GetSignatureForError ());
				}

				if (type.IsInterface) {
					if (!spec.AddInterface (type)) {
						context.Module.Compiler.Report.Error (405, constraint.Location,
							"Duplicate constraint `{0}' for type parameter `{1}'", type.GetSignatureForError (), tparam.Value);
					}

					iface_found = true;
					continue;
				}


				var constraint_tp = type as TypeParameterSpec;
				if (constraint_tp != null) {
					if (tparam_types == null) {
						tparam_types = new List<TypeParameterSpec> (2);
					} else if (tparam_types.Contains (constraint_tp)) {
						context.Module.Compiler.Report.Error (405, constraint.Location,
							"Duplicate constraint `{0}' for type parameter `{1}'", type.GetSignatureForError (), tparam.Value);
						continue;
					}

					//
					// Checks whether each generic method parameter constraint type
					// is valid with respect to T
					//
					if (tp.IsMethodTypeParameter) {
						TypeManager.CheckTypeVariance (type, Variance.Contravariant, context);
					}

					var tp_def = constraint_tp.MemberDefinition as TypeParameter;
					if (tp_def != null && !tp_def.ResolveConstraints (context)) {
						context.Module.Compiler.Report.Error (454, constraint.Location,
							"Circular constraint dependency involving `{0}' and `{1}'",
							constraint_tp.GetSignatureForError (), tp.GetSignatureForError ());
						continue;
					}

					//
					// Checks whether there are no conflicts between type parameter constraints
					//
					// class Foo<T, U>
					//      where T : A
					//      where U : B, T
					//
					// A and B are not convertible and only 1 class constraint is allowed
					//
					if (constraint_tp.HasTypeConstraint) {
						if (spec.HasTypeConstraint || spec.HasSpecialStruct) {
							if (!CheckConflictingInheritedConstraint (spec, constraint_tp.BaseType, context, constraint.Location))
								continue;
						} else {
							for (int ii = 0; ii < tparam_types.Count; ++ii) {
								if (!tparam_types[ii].HasTypeConstraint)
									continue;

								if (!CheckConflictingInheritedConstraint (spec, tparam_types[ii].BaseType, constraint_tp.BaseType, context, constraint.Location))
									break;
							}
						}
					}

					if (constraint_tp.HasSpecialStruct) {
						context.Module.Compiler.Report.Error (456, constraint.Location,
							"Type parameter `{0}' has the `struct' constraint, so it cannot be used as a constraint for `{1}'",
							constraint_tp.GetSignatureForError (), tp.GetSignatureForError ());
						continue;
					}

					tparam_types.Add (constraint_tp);
					continue;
				}

				if (iface_found || spec.HasTypeConstraint) {
					context.Module.Compiler.Report.Error (406, constraint.Location,
						"The class type constraint `{0}' must be listed before any other constraints. Consider moving type constraint to the beginning of the constraint list",
						type.GetSignatureForError ());
				}

				if (spec.HasSpecialStruct || spec.HasSpecialClass) {
					context.Module.Compiler.Report.Error (450, constraint.Location,
						"`{0}': cannot specify both a constraint class and the `class' or `struct' constraint",
						type.GetSignatureForError ());
				}

				switch (type.BuiltinType) {
				case BuiltinTypeSpec.Type.Array:
				case BuiltinTypeSpec.Type.Delegate:
				case BuiltinTypeSpec.Type.MulticastDelegate:
				case BuiltinTypeSpec.Type.Enum:
				case BuiltinTypeSpec.Type.ValueType:
				case BuiltinTypeSpec.Type.Object:
					context.Module.Compiler.Report.Error (702, constraint.Location,
						"A constraint cannot be special class `{0}'", type.GetSignatureForError ());
					continue;
				case BuiltinTypeSpec.Type.Dynamic:
					context.Module.Compiler.Report.Error (1967, constraint.Location,
						"A constraint cannot be the dynamic type");
					continue;
				}

				if (type.IsSealed || !type.IsClass) {
					context.Module.Compiler.Report.Error (701, loc,
						"`{0}' is not a valid constraint. A constraint must be an interface, a non-sealed class or a type parameter",
						TypeManager.CSharpName (type));
					continue;
				}

				if (type.IsStatic) {
					context.Module.Compiler.Report.Error (717, constraint.Location,
						"`{0}' is not a valid constraint. Static classes cannot be used as constraints",
						type.GetSignatureForError ());
				}

				spec.BaseType = type;
			}

			if (tparam_types != null)
				spec.TypeArguments = tparam_types.ToArray ();

			resolving = false;
			resolved = true;
			return true;
		}

		public void VerifyClsCompliance (Report report)
		{
			foreach (var c in constraints)
			{
				if (c == null)
					continue;

				if (!c.Type.IsCLSCompliant ()) {
					report.SymbolRelatedToPreviousError (c.Type);
					report.Warning (3024, 1, loc, "Constraint type `{0}' is not CLS-compliant",
						c.Type.GetSignatureForError ());
				}
			}
		}
	}

	//
	// A type parameter for a generic type or generic method definition
	//
	public class TypeParameter : MemberCore, ITypeDefinition
	{
		static readonly string[] attribute_target = new string [] { "type parameter" };
		
		Constraints constraints;
		GenericTypeParameterBuilder builder;
		TypeParameterSpec spec;

		public TypeParameter (int index, MemberName name, Constraints constraints, Attributes attrs, Variance variance)
			: base (null, name, attrs)
		{
			this.constraints = constraints;
			this.spec = new TypeParameterSpec (null, index, this, SpecialConstraint.None, variance, null);
		}

		//
		// Used by parser
		//
		public TypeParameter (MemberName name, Attributes attrs, Variance variance)
			: base (null, name, attrs)
		{
			this.spec = new TypeParameterSpec (null, -1, this, SpecialConstraint.None, variance, null);
		}

		public TypeParameter (TypeParameterSpec spec, TypeSpec parentSpec, MemberName name, Attributes attrs)
			: base (null, name, attrs)
		{
			this.spec = new TypeParameterSpec (parentSpec, spec.DeclaredPosition, spec.MemberDefinition, spec.SpecialConstraint, spec.Variance, null) {
				BaseType = spec.BaseType,
				InterfacesDefined = spec.InterfacesDefined,
				TypeArguments = spec.TypeArguments
			};
		}

		#region Properties

		public override AttributeTargets AttributeTargets {
			get {
				return AttributeTargets.GenericParameter;
			}
		}

		public Constraints Constraints {
			get {
				return constraints;
			}
			set {
				constraints = value;
			}
		}

		public IAssemblyDefinition DeclaringAssembly {
			get	{
				return Module.DeclaringAssembly;
			}
		}

		public override string DocCommentHeader {
			get {
				throw new InvalidOperationException (
					"Unexpected attempt to get doc comment from " + this.GetType ());
			}
		}

		bool ITypeDefinition.IsPartial {
			get {
				return false;
			}
		}

		public bool IsMethodTypeParameter {
			get {
				return spec.IsMethodOwned;
			}
		}

		public string Name {
			get {
				return MemberName.Name;
			}
		}

		public string Namespace {
			get {
				return null;
			}
		}

		public TypeParameterSpec Type {
			get {
				return spec;
			}
		}

		public int TypeParametersCount {
			get {
				return 0;
			}
		}

		public TypeParameterSpec[] TypeParameters {
			get {
				return null;
			}
		}

		public override string[] ValidAttributeTargets {
			get {
				return attribute_target;
			}
		}

		public Variance Variance {
			get {
				return spec.Variance;
			}
		}

		#endregion

		//
		// This is called for each part of a partial generic type definition.
		//
		// If partial type parameters constraints are not null and we don't
		// already have constraints they become our constraints. If we already
		// have constraints, we must check that they're the same.
		//
		public bool AddPartialConstraints (TypeDefinition part, TypeParameter tp)
		{
			if (builder == null)
				throw new InvalidOperationException ();

			var new_constraints = tp.constraints;
			if (new_constraints == null)
				return true;

			// TODO: could create spec only
			//tp.Define (null, -1, part.Definition);
			tp.spec.DeclaringType = part.Definition;
			if (!tp.ResolveConstraints (part))
				return false;

			if (constraints != null)
				return spec.HasSameConstraintsDefinition (tp.Type);

			// Copy constraint from resolved part to partial container
			spec.SpecialConstraint = tp.spec.SpecialConstraint;
			spec.InterfacesDefined = tp.spec.InterfacesDefined;
			spec.TypeArguments = tp.spec.TypeArguments;
			spec.BaseType = tp.spec.BaseType;
			
			return true;
		}

		public override void ApplyAttributeBuilder (Attribute a, MethodSpec ctor, byte[] cdata, PredefinedAttributes pa)
		{
			builder.SetCustomAttribute ((ConstructorInfo) ctor.GetMetaInfo (), cdata);
		}

		public void CheckGenericConstraints (bool obsoleteCheck)
		{
			if (constraints != null)
				constraints.CheckGenericConstraints (this, obsoleteCheck);
		}

		public TypeParameter CreateHoistedCopy (TypeSpec declaringSpec)
		{
			return new TypeParameter (spec, declaringSpec, MemberName, null);
		}

		public override bool Define ()
		{
			return true;
		}

		//
		// This is the first method which is called during the resolving
		// process; we're called immediately after creating the type parameters
		// with SRE (by calling `DefineGenericParameters()' on the TypeBuilder /
		// MethodBuilder).
		//
		public void Define (GenericTypeParameterBuilder type, TypeSpec declaringType, TypeContainer parent)
		{
			if (builder != null)
				throw new InternalErrorException ();

			// Needed to get compiler reference
			this.Parent = parent;
			this.builder = type;
			spec.DeclaringType = declaringType;
			spec.SetMetaInfo (type);
		}

		public void EmitConstraints (GenericTypeParameterBuilder builder)
		{
			var attr = GenericParameterAttributes.None;
			if (spec.Variance == Variance.Contravariant)
				attr |= GenericParameterAttributes.Contravariant;
			else if (spec.Variance == Variance.Covariant)
				attr |= GenericParameterAttributes.Covariant;

			if (spec.HasSpecialClass)
				attr |= GenericParameterAttributes.ReferenceTypeConstraint;
			else if (spec.HasSpecialStruct)
				attr |= GenericParameterAttributes.NotNullableValueTypeConstraint | GenericParameterAttributes.DefaultConstructorConstraint;

			if (spec.HasSpecialConstructor)
				attr |= GenericParameterAttributes.DefaultConstructorConstraint;

			if (spec.BaseType.BuiltinType != BuiltinTypeSpec.Type.Object)
				builder.SetBaseTypeConstraint (spec.BaseType.GetMetaInfo ());

			if (spec.InterfacesDefined != null)
				builder.SetInterfaceConstraints (spec.InterfacesDefined.Select (l => l.GetMetaInfo ()).ToArray ());

			if (spec.TypeArguments != null)
				builder.SetInterfaceConstraints (spec.TypeArguments.Select (l => l.GetMetaInfo ()).ToArray ());

			builder.SetGenericParameterAttributes (attr);
		}

		public override void Emit ()
		{
			EmitConstraints (builder);

			if (OptAttributes != null)
				OptAttributes.Emit ();

			base.Emit ();
		}

		public void ErrorInvalidVariance (IMemberContext mc, Variance expected)
		{
			Report.SymbolRelatedToPreviousError (mc.CurrentMemberDefinition);
			string input_variance = Variance == Variance.Contravariant ? "contravariant" : "covariant";
			string gtype_variance;
			switch (expected) {
			case Variance.Contravariant: gtype_variance = "contravariantly"; break;
			case Variance.Covariant: gtype_variance = "covariantly"; break;
			default: gtype_variance = "invariantly"; break;
			}

			Delegate d = mc as Delegate;
			string parameters = d != null ? d.Parameters.GetSignatureForError () : "";

			Report.Error (1961, Location,
				"The {2} type parameter `{0}' must be {3} valid on `{1}{4}'",
					GetSignatureForError (), mc.GetSignatureForError (), input_variance, gtype_variance, parameters);
		}

		public TypeSpec GetAttributeCoClass ()
		{
			return null;
		}

		public string GetAttributeDefaultMember ()
		{
			throw new NotSupportedException ();
		}

		public AttributeUsageAttribute GetAttributeUsage (PredefinedAttribute pa)
		{
			throw new NotSupportedException ();
		}

		public override string GetSignatureForDocumentation ()
		{
			throw new NotImplementedException ();
		}

		public override string GetSignatureForError ()
		{
			return MemberName.Name;
		}

		bool ITypeDefinition.IsInternalAsPublic (IAssemblyDefinition assembly)
		{
			return spec.MemberDefinition.DeclaringAssembly == assembly;
		}

		public void LoadMembers (TypeSpec declaringType, bool onlyTypes, ref MemberCache cache)
		{
			throw new NotSupportedException ("Not supported for compiled definition");
		}

		//
		// Resolves all type parameter constraints
		//
		public bool ResolveConstraints (IMemberContext context)
		{
			if (constraints != null)
				return constraints.Resolve (context, this);

			if (spec.BaseType == null)
				spec.BaseType = context.Module.Compiler.BuiltinTypes.Object;

			return true;
		}

		public override bool IsClsComplianceRequired ()
		{
			return false;
		}

		public new void VerifyClsCompliance ()
		{
			if (constraints != null)
				constraints.VerifyClsCompliance (Report);
		}

		public void WarningParentNameConflict (TypeParameter conflict)
		{
			conflict.Report.SymbolRelatedToPreviousError (conflict.Location, null);
			conflict.Report.Warning (693, 3, Location,
				"Type parameter `{0}' has the same name as the type parameter from outer type `{1}'",
				GetSignatureForError (), conflict.CurrentType.GetSignatureForError ());
		}
	}

	[System.Diagnostics.DebuggerDisplay ("{DisplayDebugInfo()}")]
	public class TypeParameterSpec : TypeSpec
	{
		public static readonly new TypeParameterSpec[] EmptyTypes = new TypeParameterSpec[0];

		Variance variance;
		SpecialConstraint spec;
		int tp_pos;
		TypeSpec[] targs;
		TypeSpec[] ifaces_defined;

		//
		// Creates type owned type parameter
		//
		public TypeParameterSpec (TypeSpec declaringType, int index, ITypeDefinition definition, SpecialConstraint spec, Variance variance, MetaType info)
			: base (MemberKind.TypeParameter, declaringType, definition, info, Modifiers.PUBLIC)
		{
			this.variance = variance;
			this.spec = spec;
			state &= ~StateFlags.Obsolete_Undetected;
			tp_pos = index;
		}

		//
		// Creates method owned type parameter
		//
		public TypeParameterSpec (int index, ITypeDefinition definition, SpecialConstraint spec, Variance variance, MetaType info)
			: this (null, index, definition, spec, variance, info)
		{
		}

		#region Properties

		public int DeclaredPosition {
			get {
				return tp_pos;
			}
			set {
				tp_pos = value;
			}
		}

		public bool HasSpecialConstructor {
			get {
				return (spec & SpecialConstraint.Constructor) != 0;
			}
		}

		public bool HasSpecialClass {
			get {
				return (spec & SpecialConstraint.Class) != 0;
			}
		}

		public bool HasSpecialStruct {
			get {
				return (spec & SpecialConstraint.Struct) != 0;
			}
		}

		public bool HasTypeConstraint {
			get {
				var bt = BaseType.BuiltinType;
				return bt != BuiltinTypeSpec.Type.Object && bt != BuiltinTypeSpec.Type.ValueType;
			}
		}

		public override IList<TypeSpec> Interfaces {
			get {
				if ((state & StateFlags.InterfacesExpanded) == 0) {
					if (ifaces != null) {
						for (int i = 0; i < ifaces.Count; ++i ) {
							var iface_type = ifaces[i];
							if (iface_type.Interfaces != null) {
								if (ifaces_defined == null)
									ifaces_defined = ifaces.ToArray ();

								for (int ii = 0; ii < iface_type.Interfaces.Count; ++ii) {
									var ii_iface_type = iface_type.Interfaces [ii];

									AddInterface (ii_iface_type);
								}
							}
						}
					}

					if (ifaces_defined == null)
						ifaces_defined = ifaces == null ? TypeSpec.EmptyTypes : ifaces.ToArray ();

					state |= StateFlags.InterfacesExpanded;
				}

				return ifaces;
			}
		}

		//
		// Unexpanded interfaces list
		//
		public TypeSpec[] InterfacesDefined {
			get {
				if (ifaces_defined == null) {
					if (ifaces == null)
						return null;

					ifaces_defined = ifaces.ToArray ();
				}

				return ifaces_defined.Length == 0 ? null : ifaces_defined;
			}
			set {
				ifaces_defined = value;
				if (value != null && value.Length != 0)
					ifaces = value;
			}
		}

		public bool IsConstrained {
			get {
				return spec != SpecialConstraint.None || ifaces != null || targs != null || HasTypeConstraint;
			}
		}

		//
		// Returns whether the type parameter is known to be a reference type
		//
		public new bool IsReferenceType {
			get {
				if ((spec & (SpecialConstraint.Class | SpecialConstraint.Struct)) != 0)
					return (spec & SpecialConstraint.Class) != 0;

				//
				// Full check is needed (see IsValueType for details)
				//
				if (HasTypeConstraint && TypeSpec.IsReferenceType (BaseType))
					return true;

				if (targs != null) {
					foreach (var ta in targs) {
						//
						// Secondary special constraints are ignored (I am not sure why)
						//
						var tp = ta as TypeParameterSpec;
						if (tp != null && (tp.spec & (SpecialConstraint.Class | SpecialConstraint.Struct)) != 0)
							continue;

						if (TypeSpec.IsReferenceType (ta))
							return true;
					}
				}

				return false;
			}
		}

		//
		// Returns whether the type parameter is known to be a value type
		//
		public new bool IsValueType {
			get {
				//
				// Even if structs/enums cannot be used directly as constraints
				// they can apear as constraint type when inheriting base constraint
				// which has dependant type parameter constraint which has been
				// inflated using value type
				//
				// class A : B<int> { override void Foo<U> () {} }
				// class B<T> { virtual void Foo<U> () where U : T {} }
				//
				if (HasSpecialStruct)
					return true;

				if (targs != null) {
					foreach (var ta in targs) {
						if (TypeSpec.IsValueType (ta))
							return true;
					}
				}

				return false;
			}
		}

		public override string Name {
			get {
				return definition.Name;
			}
		}

		public bool IsMethodOwned {
			get {
				return DeclaringType == null;
			}
		}

		public SpecialConstraint SpecialConstraint {
			get {
				return spec;
			}
			set {
				spec = value;
			}
		}

		//
		// Types used to inflate the generic type
		//
		public new TypeSpec[] TypeArguments {
			get {
				return targs;
			}
			set {
				targs = value;
			}
		}

		public Variance Variance {
			get {
				return variance;
			}
		}

		#endregion

		public string DisplayDebugInfo ()
		{
			var s = GetSignatureForError ();
			return IsMethodOwned ? s + "!!" : s + "!";
		}

		//
		// Finds effective base class. The effective base class is always a class-type
		//
		public TypeSpec GetEffectiveBase ()
		{
			if (HasSpecialStruct)
				return BaseType;

			//
			// If T has a class-type constraint C but no type-parameter constraints, its effective base class is C
			//
			if (BaseType != null && targs == null) {
				//
				// If T has a constraint V that is a value-type, use instead the most specific base type of V that is a class-type.
				// 
				// LAMESPEC: Is System.ValueType always the most specific base type in this case?
				//
				// Note: This can never happen in an explicitly given constraint, but may occur when the constraints of a generic method
				// are implicitly inherited by an overriding method declaration or an explicit implementation of an interface method.
				//
				return BaseType.IsStruct ? BaseType.BaseType : BaseType;
			}

			var types = targs;
			if (HasTypeConstraint) {
				Array.Resize (ref types, types.Length + 1);

				for (int i = 0; i < types.Length - 1; ++i) {
					types[i] = types[i].BaseType;
				}

				types[types.Length - 1] = BaseType;
			} else {
				types = types.Select (l => l.BaseType).ToArray ();
			}

			if (types != null)
				return Convert.FindMostEncompassedType (types);

			return BaseType;
		}

		public override string GetSignatureForDocumentation ()
		{
			var prefix = IsMethodOwned ? "``" : "`";
			return prefix + DeclaredPosition;
		}

		public override string GetSignatureForError ()
		{
			return Name;
		}

		//
		// Constraints have to match by definition but not position, used by
		// partial classes or methods
		//
		public bool HasSameConstraintsDefinition (TypeParameterSpec other)
		{
			if (spec != other.spec)
				return false;

			if (BaseType != other.BaseType)
				return false;

			if (!TypeSpecComparer.Override.IsSame (InterfacesDefined, other.InterfacesDefined))
				return false;

			if (!TypeSpecComparer.Override.IsSame (targs, other.targs))
				return false;

			return true;
		}

		//
		// Constraints have to match by using same set of types, used by
		// implicit interface implementation
		//
		public bool HasSameConstraintsImplementation (TypeParameterSpec other)
		{
			if (spec != other.spec)
				return false;

			//
			// It can be same base type or inflated type parameter
			//
			// interface I<T> { void Foo<U> where U : T; }
			// class A : I<int> { void Foo<X> where X : int {} }
			//
			bool found;
			if (!TypeSpecComparer.Override.IsEqual (BaseType, other.BaseType)) {
				if (other.targs == null)
					return false;

				found = false;
				foreach (var otarg in other.targs) {
					if (TypeSpecComparer.Override.IsEqual (BaseType, otarg)) {
						found = true;
						break;
					}
				}

				if (!found)
					return false;
			}

			// Check interfaces implementation -> definition
			if (InterfacesDefined != null) {
				//
				// Iterate over inflated interfaces
				//
				foreach (var iface in Interfaces) {
					found = false;
					if (other.InterfacesDefined != null) {
						foreach (var oiface in other.Interfaces) {
							if (TypeSpecComparer.Override.IsEqual (iface, oiface)) {
								found = true;
								break;
							}
						}
					}

					if (found)
						continue;

					if (other.targs != null) {
						foreach (var otarg in other.targs) {
							if (TypeSpecComparer.Override.IsEqual (BaseType, otarg)) {
								found = true;
								break;
							}
						}
					}

					if (!found)
						return false;
				}
			}

			// Check interfaces implementation <- definition
			if (other.InterfacesDefined != null) {
				if (InterfacesDefined == null)
					return false;

				//
				// Iterate over inflated interfaces
				//
				foreach (var oiface in other.Interfaces) {
					found = false;
					foreach (var iface in Interfaces) {
						if (TypeSpecComparer.Override.IsEqual (iface, oiface)) {
							found = true;
							break;
						}
					}

					if (!found)
						return false;
				}
			}

			// Check type parameters implementation -> definition
			if (targs != null) {
				if (other.targs == null)
					return false;

				foreach (var targ in targs) {
					found = false;
					foreach (var otarg in other.targs) {
						if (TypeSpecComparer.Override.IsEqual (targ, otarg)) {
							found = true;
							break;
						}
					}

					if (!found)
						return false;
				}
			}

			// Check type parameters implementation <- definition
			if (other.targs != null) {
				foreach (var otarg in other.targs) {
					// Ignore inflated type arguments, were checked above
					if (!otarg.IsGenericParameter)
						continue;

					if (targs == null)
						return false;

					found = false;
					foreach (var targ in targs) {
						if (TypeSpecComparer.Override.IsEqual (targ, otarg)) {
							found = true;
							break;
						}
					}

					if (!found)
						return false;
				}				
			}

			return true;
		}

		public static TypeParameterSpec[] InflateConstraints (TypeParameterInflator inflator, TypeParameterSpec[] tparams)
		{
			return InflateConstraints (tparams, l => l, inflator);
		}

		public static TypeParameterSpec[] InflateConstraints<T> (TypeParameterSpec[] tparams, Func<T, TypeParameterInflator> inflatorFactory, T arg)
		{
			TypeParameterSpec[] constraints = null;
			TypeParameterInflator? inflator = null;

			for (int i = 0; i < tparams.Length; ++i) {
				var tp = tparams[i];
				if (tp.HasTypeConstraint || tp.InterfacesDefined != null || tp.TypeArguments != null) {
					if (constraints == null) {
						constraints = new TypeParameterSpec[tparams.Length];
						Array.Copy (tparams, constraints, constraints.Length);
					}

					//
					// Using a factory to avoid possibly expensive inflator build up
					//
					if (inflator == null)
						inflator = inflatorFactory (arg);

					constraints[i] = (TypeParameterSpec) constraints[i].InflateMember (inflator.Value);
				}
			}

			if (constraints == null)
				constraints = tparams;

			return constraints;
		}

		public void InflateConstraints (TypeParameterInflator inflator, TypeParameterSpec tps)
		{
			tps.BaseType = inflator.Inflate (BaseType);
			if (ifaces != null) {
				tps.ifaces = new List<TypeSpec> (ifaces.Count);
				for (int i = 0; i < ifaces.Count; ++i)
					tps.ifaces.Add (inflator.Inflate (ifaces[i]));
			}

			if (targs != null) {
				tps.targs = new TypeSpec[targs.Length];
				for (int i = 0; i < targs.Length; ++i)
					tps.targs[i] = inflator.Inflate (targs[i]);
			}
		}

		public override MemberSpec InflateMember (TypeParameterInflator inflator)
		{
			var tps = (TypeParameterSpec) MemberwiseClone ();
			InflateConstraints (inflator, tps);
			return tps;
		}

		//
		// Populates type parameter members using type parameter constraints
		// The trick here is to be called late enough but not too late to
		// populate member cache with all members from other types
		//
		protected override void InitializeMemberCache (bool onlyTypes)
		{
			cache = new MemberCache ();

			//
			// For a type parameter the membercache is the union of the sets of members of the types
			// specified as a primary constraint or secondary constraint
			//
			if (BaseType.BuiltinType != BuiltinTypeSpec.Type.Object && BaseType.BuiltinType != BuiltinTypeSpec.Type.ValueType)
				cache.AddBaseType (BaseType);

			if (ifaces != null) {
				foreach (var iface_type in Interfaces) {
					cache.AddInterface (iface_type);
				}
			}

			if (targs != null) {
				foreach (var ta in targs) {
					var b_type = ta.BaseType;
					if (b_type.BuiltinType != BuiltinTypeSpec.Type.Object && b_type.BuiltinType != BuiltinTypeSpec.Type.ValueType)
						cache.AddBaseType (b_type);

					if (ta.Interfaces != null) {
						foreach (var iface_type in ta.Interfaces) {
							cache.AddInterface (iface_type);
						}
					}
				}
			}
		}

		public bool IsConvertibleToInterface (TypeSpec iface)
		{
			if (Interfaces != null) {
				foreach (var t in Interfaces) {
					if (t == iface)
						return true;
				}
			}

			if (TypeArguments != null) {
				foreach (var t in TypeArguments) {
					if (((TypeParameterSpec) t).IsConvertibleToInterface (iface))
						return true;
				}
			}

			return false;
		}

		public override TypeSpec Mutate (TypeParameterMutator mutator)
		{
			return mutator.Mutate (this);
		}
	}

	public struct TypeParameterInflator
	{
		readonly TypeSpec type;
		readonly TypeParameterSpec[] tparams;
		readonly TypeSpec[] targs;
		readonly IModuleContext context;

		public TypeParameterInflator (TypeParameterInflator nested, TypeSpec type)
			: this (nested.context, type, nested.tparams, nested.targs)
		{
		}

		public TypeParameterInflator (IModuleContext context, TypeSpec type, TypeParameterSpec[] tparams, TypeSpec[] targs)
		{
			if (tparams.Length != targs.Length)
				throw new ArgumentException ("Invalid arguments");

			this.context = context;
			this.tparams = tparams;
			this.targs = targs;
			this.type = type;
		}

		#region Properties

		public IModuleContext Context {
			get {
				return context;
			}
		}

		public TypeSpec TypeInstance {
			get {
				return type;
			}
		}

		//
		// Type parameters to inflate
		//
		public TypeParameterSpec[] TypeParameters {
			get {
				return tparams;
			}
		}

		#endregion

		public TypeSpec Inflate (TypeSpec type)
		{
			var tp = type as TypeParameterSpec;
			if (tp != null)
				return Inflate (tp);

			var ac = type as ArrayContainer;
			if (ac != null) {
				var et = Inflate (ac.Element);
				if (et != ac.Element)
					return ArrayContainer.MakeType (context.Module, et, ac.Rank);

				return ac;
			}

			//
			// When inflating a nested type, inflate its parent first
			// in case it's using same type parameters (was inflated within the type)
			//
			TypeSpec[] targs;
			int i = 0;
			if (type.IsNested) {
				var parent = Inflate (type.DeclaringType);

				//
				// Keep the inflated type arguments
				// 
				targs = type.TypeArguments;

				//
				// When inflating imported nested type used inside same declaring type, we get TypeSpec
				// because the import cache helps us to catch it. However, that means we have to look at
				// type definition to get type argument (they are in fact type parameter in this case)
				//
				if (targs.Length == 0 && type.Arity > 0)
					targs = type.MemberDefinition.TypeParameters;

				//
				// Parent was inflated, find the same type on inflated type
				// to use same cache for nested types on same generic parent
				//
				type = MemberCache.FindNestedType (parent, type.Name, type.Arity);

				//
				// Handle the tricky case where parent shares local type arguments
				// which means inflating inflated type
				//
				// class Test<T> {
				//		public static Nested<T> Foo () { return null; }
				//
				//		public class Nested<U> {}
				//	}
				//
				//  return type of Test<string>.Foo() has to be Test<string>.Nested<string> 
				//
				if (targs.Length > 0) {
					var inflated_targs = new TypeSpec[targs.Length];
					for (; i < targs.Length; ++i)
						inflated_targs[i] = Inflate (targs[i]);

					type = type.MakeGenericType (context, inflated_targs);
				}

				return type;
			}

			// Nothing to do for non-generic type
			if (type.Arity == 0)
				return type;

			targs = new TypeSpec[type.Arity];

			//
			// Inflating using outside type arguments, var v = new Foo<int> (), class Foo<T> {}
			//
			if (type is InflatedTypeSpec) {
				for (; i < targs.Length; ++i)
					targs[i] = Inflate (type.TypeArguments[i]);

				type = type.GetDefinition ();
			} else {
				//
				// Inflating parent using inside type arguments, class Foo<T> { ITest<T> foo; }
				//
				var args = type.MemberDefinition.TypeParameters;
				foreach (var ds_tp in args)
					targs[i++] = Inflate (ds_tp);
			}

			return type.MakeGenericType (context, targs);
		}

		public TypeSpec Inflate (TypeParameterSpec tp)
		{
			for (int i = 0; i < tparams.Length; ++i)
				if (tparams [i] == tp)
					return targs[i];

			// This can happen when inflating nested types
			// without type arguments specified
			return tp;
		}
	}

	//
	// Before emitting any code we have to change all MVAR references to VAR
	// when the method is of generic type and has hoisted variables
	//
	public class TypeParameterMutator
	{
		readonly TypeParameters mvar;
		readonly TypeParameters var;
		Dictionary<TypeSpec, TypeSpec> mutated_typespec;

		public TypeParameterMutator (TypeParameters mvar, TypeParameters var)
		{
			if (mvar.Count != var.Count)
				throw new ArgumentException ();

			this.mvar = mvar;
			this.var = var;
		}

		#region Properties

		public TypeParameters MethodTypeParameters {
			get {
				return mvar;
			}
		}

		#endregion

		public static TypeSpec GetMemberDeclaringType (TypeSpec type)
		{
			if (type is InflatedTypeSpec) {
				if (type.DeclaringType == null)
					return type.GetDefinition ();

				var parent = GetMemberDeclaringType (type.DeclaringType);
				type = MemberCache.GetMember<TypeSpec> (parent, type);
			}

			return type;
		}

		public TypeSpec Mutate (TypeSpec ts)
		{
			TypeSpec value;
			if (mutated_typespec != null && mutated_typespec.TryGetValue (ts, out value))
				return value;

			value = ts.Mutate (this);
			if (mutated_typespec == null)
				mutated_typespec = new Dictionary<TypeSpec, TypeSpec> ();

			mutated_typespec.Add (ts, value);
			return value;
		}

		public TypeParameterSpec Mutate (TypeParameterSpec tp)
		{
			for (int i = 0; i < mvar.Count; ++i) {
				if (mvar[i].Type == tp)
					return var[i].Type;
			}

			return tp;
		}

		public TypeSpec[] Mutate (TypeSpec[] targs)
		{
			TypeSpec[] mutated = new TypeSpec[targs.Length];
			bool changed = false;
			for (int i = 0; i < targs.Length; ++i) {
				mutated[i] = Mutate (targs[i]);
				changed |= targs[i] != mutated[i];
			}

			return changed ? mutated : targs;
		}
	}

	/// <summary>
	///   A TypeExpr which already resolved to a type parameter.
	/// </summary>
	public class TypeParameterExpr : TypeExpression
	{
		public TypeParameterExpr (TypeParameter type_parameter, Location loc)
			: base (type_parameter.Type, loc)
		{
			this.eclass = ExprClass.TypeParameter;
		}
	}

	public class InflatedTypeSpec : TypeSpec
	{
		TypeSpec[] targs;
		TypeParameterSpec[] constraints;
		readonly TypeSpec open_type;
		readonly IModuleContext context;

		public InflatedTypeSpec (IModuleContext context, TypeSpec openType, TypeSpec declaringType, TypeSpec[] targs)
			: base (openType.Kind, declaringType, openType.MemberDefinition, null, openType.Modifiers)
		{
			if (targs == null)
				throw new ArgumentNullException ("targs");

//			this.state = openType.state;
			this.context = context;
			this.open_type = openType;
			this.targs = targs;

			foreach (var arg in targs) {
				if (arg.HasDynamicElement || arg.BuiltinType == BuiltinTypeSpec.Type.Dynamic) {
					state |= StateFlags.HasDynamicElement;
					break;
				}
			}

			if (open_type.Kind == MemberKind.MissingType)
				MemberCache = MemberCache.Empty;

			if ((open_type.Modifiers & Modifiers.COMPILER_GENERATED) != 0)
				state |= StateFlags.ConstraintsChecked;
		}

		#region Properties

		public override TypeSpec BaseType {
			get {
				if (cache == null || (state & StateFlags.PendingBaseTypeInflate) != 0)
					InitializeMemberCache (true);

				return base.BaseType;
			}
		}

		//
		// Inflated type parameters with constraints array, mapping with type arguments is based on index
		//
		public TypeParameterSpec[] Constraints {
			get {
				if (constraints == null) {
					constraints = TypeParameterSpec.InflateConstraints (MemberDefinition.TypeParameters, l => l.CreateLocalInflator (context), this);
				}

				return constraints;
			}
		}

		//
		// Used to cache expensive constraints validation on constructed types
		//
		public bool HasConstraintsChecked {
			get {
				return (state & StateFlags.ConstraintsChecked) != 0;
			}
			set {
				state = value ? state | StateFlags.ConstraintsChecked : state & ~StateFlags.ConstraintsChecked;
			}
		}

		public override IList<TypeSpec> Interfaces {
			get {
				if (cache == null)
					InitializeMemberCache (true);

				return base.Interfaces;
			}
		}

		public override bool IsExpressionTreeType {
			get {
				return (open_type.state & StateFlags.InflatedExpressionType) != 0;
			}
		}

		public override bool IsGenericIterateInterface {
			get {
				return (open_type.state & StateFlags.GenericIterateInterface) != 0;
			}
		}

		public override bool IsGenericTask {
			get {
				return (open_type.state & StateFlags.GenericTask) != 0;
			}
		}

		public override bool IsNullableType {
			get {
				return (open_type.state & StateFlags.InflatedNullableType) != 0;
			}
		}

		//
		// Types used to inflate the generic  type
		//
		public override TypeSpec[] TypeArguments {
			get {
				return targs;
			}
		}

		#endregion

		public static bool ContainsTypeParameter (TypeSpec type)
		{
			if (type.Kind == MemberKind.TypeParameter)
				return true;

			var element_container = type as ElementTypeSpec;
			if (element_container != null)
				return ContainsTypeParameter (element_container.Element);

			foreach (var t in type.TypeArguments) {
				if (ContainsTypeParameter (t)) {
					return true;
				}
			}

			return false;
		}

		TypeParameterInflator CreateLocalInflator (IModuleContext context)
		{
			TypeParameterSpec[] tparams_full;
			TypeSpec[] targs_full = targs;
			if (IsNested) {
				//
				// Special case is needed when we are inflating an open type (nested type definition)
				// on inflated parent. Consider following case
				//
				// Foo<T>.Bar<U> => Foo<string>.Bar<U>
				//
				// Any later inflation of Foo<string>.Bar<U> has to also inflate T if used inside Bar<U>
				//
				List<TypeSpec> merged_targs = null;
				List<TypeParameterSpec> merged_tparams = null;

				var type = DeclaringType;

				do {
					if (type.TypeArguments.Length > 0) {
						if (merged_targs == null) {
							merged_targs = new List<TypeSpec> ();
							merged_tparams = new List<TypeParameterSpec> ();
							if (targs.Length > 0) {
								merged_targs.AddRange (targs);
								merged_tparams.AddRange (open_type.MemberDefinition.TypeParameters);
							}
						}
						merged_tparams.AddRange (type.MemberDefinition.TypeParameters);
						merged_targs.AddRange (type.TypeArguments);
					}
					type = type.DeclaringType;
				} while (type != null);

				if (merged_targs != null) {
					// Type arguments are not in the right order but it should not matter in this case
					targs_full = merged_targs.ToArray ();
					tparams_full = merged_tparams.ToArray ();
				} else if (targs.Length == 0) {
					tparams_full = TypeParameterSpec.EmptyTypes;
				} else {
					tparams_full = open_type.MemberDefinition.TypeParameters;
				}
			} else if (targs.Length == 0) {
				tparams_full = TypeParameterSpec.EmptyTypes;
			} else {
				tparams_full = open_type.MemberDefinition.TypeParameters;
			}

			return new TypeParameterInflator (context, this, tparams_full, targs_full);
		}

		MetaType CreateMetaInfo (TypeParameterMutator mutator)
		{
			//
			// Converts nested type arguments into right order
			// Foo<string, bool>.Bar<int> => string, bool, int
			//
			var all = new List<MetaType> ();
			TypeSpec type = this;
			TypeSpec definition = type;
			do {
				if (type.GetDefinition().IsGeneric) {
					all.InsertRange (0,
						type.TypeArguments != TypeSpec.EmptyTypes ?
						type.TypeArguments.Select (l => l.GetMetaInfo ()) :
						type.MemberDefinition.TypeParameters.Select (l => l.GetMetaInfo ()));
				}

				definition = definition.GetDefinition ();
				type = type.DeclaringType;
			} while (type != null);

			return definition.GetMetaInfo ().MakeGenericType (all.ToArray ());
		}

		public override ObsoleteAttribute GetAttributeObsolete ()
		{
			return open_type.GetAttributeObsolete ();
		}

		protected override bool IsNotCLSCompliant (out bool attrValue)
		{
			if (base.IsNotCLSCompliant (out attrValue))
				return true;

			foreach (var ta in TypeArguments) {
				if (ta.MemberDefinition.CLSAttributeValue == false)
					return true;
			}

			return false;
		}

		public override TypeSpec GetDefinition ()
		{
			return open_type;
		}

		public override MetaType GetMetaInfo ()
		{
			if (info == null)
				info = CreateMetaInfo (null);

			return info;
		}

		public override string GetSignatureForError ()
		{
			if (IsNullableType)
				return targs[0].GetSignatureForError () + "?";

			return base.GetSignatureForError ();
		}

		protected override string GetTypeNameSignature ()
		{
			if (targs.Length == 0 || MemberDefinition is AnonymousTypeClass)
				return null;

			return "<" + TypeManager.CSharpName (targs) + ">";
		}

		public bool HasDynamicArgument ()
		{
			for (int i = 0; i < targs.Length; ++i) {
				var item = targs[i];

				if (item.BuiltinType == BuiltinTypeSpec.Type.Dynamic)
					return true;

				if (item is InflatedTypeSpec) {
					if (((InflatedTypeSpec) item).HasDynamicArgument ())
						return true;

					continue;
				}

				if (item.IsArray) {
					while (item.IsArray) {
						item = ((ArrayContainer) item).Element;
					}

					if (item.BuiltinType == BuiltinTypeSpec.Type.Dynamic)
						return true;
				}
			}

			return false;
		}

		protected override void InitializeMemberCache (bool onlyTypes)
		{
			if (cache == null) {
				var open_cache = onlyTypes ? open_type.MemberCacheTypes : open_type.MemberCache;

				// Surprisingly, calling MemberCache on open type could meantime create cache on this type
				// for imported type parameter constraints referencing nested type of this declaration
				if (cache == null)
					cache = new MemberCache (open_cache);
			}

			var inflator = CreateLocalInflator (context);

			//
			// Two stage inflate due to possible nested types recursive
			// references
			//
			// class A<T> {
			//    B b;
			//    class B {
			//      T Value;
			//    }
			// }
			//
			// When resolving type of `b' members of `B' cannot be 
			// inflated because are not yet available in membercache
			//
			if ((state & StateFlags.PendingMemberCacheMembers) == 0) {
				open_type.MemberCacheTypes.InflateTypes (cache, inflator);

				//
				// Inflate any implemented interfaces
				//
				if (open_type.Interfaces != null) {
					ifaces = new List<TypeSpec> (open_type.Interfaces.Count);
					foreach (var iface in open_type.Interfaces) {
						var iface_inflated = inflator.Inflate (iface);
						if (iface_inflated == null)
							continue;

						AddInterface (iface_inflated);
					}
				}

				//
				// Handles the tricky case of recursive nested base generic type
				//
				// class A<T> : Base<A<T>.Nested> {
				//    class Nested {}
				// }
				//
				// When inflating A<T>. base type is not yet known, secondary
				// inflation is required (not common case) once base scope
				// is known
				//
				if (open_type.BaseType == null) {
					if (IsClass)
						state |= StateFlags.PendingBaseTypeInflate;
				} else {
					BaseType = inflator.Inflate (open_type.BaseType);
				}
			} else if ((state & StateFlags.PendingBaseTypeInflate) != 0) {
				//
				// It can happen when resolving base type without being defined
				// which is not allowed to happen and will always lead to an error
				//
				// class B { class N {} }
				// class A<T> : A<B.N> {}
				//
				if (open_type.BaseType == null)
					return;

				BaseType = inflator.Inflate (open_type.BaseType);
				state &= ~StateFlags.PendingBaseTypeInflate;
			}

			if (onlyTypes) {
				state |= StateFlags.PendingMemberCacheMembers;
				return;
			}

			var tc = open_type.MemberDefinition as TypeDefinition;
			if (tc != null && !tc.HasMembersDefined) {
				//
				// Inflating MemberCache with undefined members
				//
				return;
			}

			if ((state & StateFlags.PendingBaseTypeInflate) != 0) {
				BaseType = inflator.Inflate (open_type.BaseType);
				state &= ~StateFlags.PendingBaseTypeInflate;
			}

			state &= ~StateFlags.PendingMemberCacheMembers;
			open_type.MemberCache.InflateMembers (cache, open_type, inflator);
		}

		public override TypeSpec Mutate (TypeParameterMutator mutator)
		{
			var targs = TypeArguments;
			if (targs != null)
				targs = mutator.Mutate (targs);

			var decl = DeclaringType;
			if (IsNested && DeclaringType.IsGenericOrParentIsGeneric)
				decl = mutator.Mutate (decl);

			if (targs == TypeArguments && decl == DeclaringType)
				return this;

			var mutated = (InflatedTypeSpec) MemberwiseClone ();
			if (decl != DeclaringType) {
				// Gets back MethodInfo in case of metaInfo was inflated
				//mutated.info = MemberCache.GetMember<TypeSpec> (DeclaringType.GetDefinition (), this).info;

				mutated.declaringType = decl;
				mutated.state |= StateFlags.PendingMetaInflate;
			}

			if (targs != null) {
				mutated.targs = targs;
				mutated.info = null;
			}

			return mutated;
		}
	}


	//
	// Tracks the type arguments when instantiating a generic type. It's used
	// by both type arguments and type parameters
	//
	public class TypeArguments
	{
		List<FullNamedExpression> args;
		TypeSpec[] atypes;

		public List<FullNamedExpression> Args {
			get { return this.args; }
		}

		public TypeArguments (params FullNamedExpression[] types)
		{
			this.args = new List<FullNamedExpression> (types);
		}

		public void Add (FullNamedExpression type)
		{
			args.Add (type);
		}

		/// <summary>
		///   We may only be used after Resolve() is called and return the fully
		///   resolved types.
		/// </summary>
		// TODO: Not needed, just return type from resolve
		public TypeSpec[] Arguments {
			get {
				return atypes;
			}
			set {
				atypes = value;
			}
		}

		public int Count {
			get {
				return args.Count;
			}
		}

		public virtual bool IsEmpty {
			get {
				return false;
			}
		}

		public List<FullNamedExpression> TypeExpressions {
			get {
				return this.args;
 			}
		}

		public string GetSignatureForError()
		{
			StringBuilder sb = new StringBuilder ();
			for (int i = 0; i < Count; ++i) {
				var expr = args[i];
				if (expr != null)
					sb.Append (expr.GetSignatureForError ());

				if (i + 1 < Count)
					sb.Append (',');
			}

			return sb.ToString ();
		}

		/// <summary>
		///   Resolve the type arguments.
		/// </summary>
		public virtual bool Resolve (IMemberContext ec)
		{
			if (atypes != null)
			    return atypes.Length != 0;

			int count = args.Count;
			bool ok = true;

			atypes = new TypeSpec [count];

			for (int i = 0; i < count; i++){
				var te = args[i].ResolveAsType (ec);
				if (te == null) {
					ok = false;
					continue;
				}

				atypes[i] = te;

				if (te.IsStatic) {
					ec.Module.Compiler.Report.Error (718, args[i].Location, "`{0}': static classes cannot be used as generic arguments",
						te.GetSignatureForError ());
					ok = false;
				}

				if (te.IsPointer || te.IsSpecialRuntimeType) {
					ec.Module.Compiler.Report.Error (306, args[i].Location,
						"The type `{0}' may not be used as a type argument",
						te.GetSignatureForError ());
					ok = false;
				}
			}

			if (!ok)
				atypes = TypeSpec.EmptyTypes;

			return ok;
		}

		public TypeArguments Clone ()
		{
			TypeArguments copy = new TypeArguments ();
			foreach (var ta in args)
				copy.args.Add (ta);

			return copy;
		}
	}

	public class UnboundTypeArguments : TypeArguments
	{
		public UnboundTypeArguments (int arity)
			: base (new FullNamedExpression[arity])
		{
		}

		public override bool IsEmpty {
			get {
				return true;
			}
		}

		public override bool Resolve (IMemberContext ec)
		{
			// Nothing to be resolved
			return true;
		}
	}

	public class TypeParameters
	{
		List<TypeParameter> names;
		TypeParameterSpec[] types;

		public TypeParameters ()
		{
			names = new List<TypeParameter> ();
		}

		public TypeParameters (int count)
		{
			names = new List<TypeParameter> (count);
		}

		#region Properties

		public int Count {
			get {
				return names.Count;
			}
		}

		public TypeParameterSpec[] Types {
			get {
				return types;
			}
		}

		#endregion

		public void Add (TypeParameter tparam)
		{
			names.Add (tparam);
		}

		public void Add (TypeParameters tparams)
		{
			names.AddRange (tparams.names);
		}

		public void Define (GenericTypeParameterBuilder[] buiders, TypeSpec declaringType, int parentOffset, TypeContainer parent)
		{
			types = new TypeParameterSpec[Count];
			for (int i = 0; i < types.Length; ++i) {
				var tp = names[i];

				tp.Define (buiders[i + parentOffset], declaringType, parent);
				types[i] = tp.Type;
				types[i].DeclaredPosition = i + parentOffset;

				if (tp.Variance != Variance.None && !(declaringType != null && (declaringType.Kind == MemberKind.Interface || declaringType.Kind == MemberKind.Delegate))) {
					parent.Compiler.Report.Error (1960, tp.Location, "Variant type parameters can only be used with interfaces and delegates");
				}
			}
		}

		public TypeParameter this[int index] {
			get {
				return names [index];
			}
			set {
				names[index] = value;
			}
		}

		public TypeParameter Find (string name)
		{
			foreach (var tp in names) {
				if (tp.Name == name)
					return tp;
			}

			return null;
		}

		public string[] GetAllNames ()
		{
			return names.Select (l => l.Name).ToArray ();
		}

		public string GetSignatureForError ()
		{
			StringBuilder sb = new StringBuilder ();
			for (int i = 0; i < Count; ++i) {
				if (i > 0)
					sb.Append (',');

				var name = names[i];
				if (name != null)
					sb.Append (name.GetSignatureForError ());
			}

			return sb.ToString ();
		}

		public void VerifyClsCompliance ()
		{
			foreach (var tp in names) {
				tp.VerifyClsCompliance ();
			}
		}
	}

	//
	// A type expression of generic type with type arguments
	//
	class GenericTypeExpr : TypeExpr
	{
		TypeArguments args;
		TypeSpec open_type;

		/// <summary>
		///   Instantiate the generic type `t' with the type arguments `args'.
		///   Use this constructor if you already know the fully resolved
		///   generic type.
		/// </summary>		
		public GenericTypeExpr (TypeSpec open_type, TypeArguments args, Location l)
		{
			this.open_type = open_type;
			loc = l;
			this.args = args;
		}

		public override string GetSignatureForError ()
		{
			return TypeManager.CSharpName (type);
		}

		public override TypeSpec ResolveAsType (IMemberContext mc)
		{
			if (eclass != ExprClass.Unresolved)
				return type;

			if (!args.Resolve (mc))
				return null;

			TypeSpec[] atypes = args.Arguments;

			//
			// Now bind the parameters
			//
			var inflated = open_type.MakeGenericType (mc, atypes);
			type = inflated;
			eclass = ExprClass.Type;

			//
			// The constraints can be checked only when full type hierarchy is known
			//
			if (!inflated.HasConstraintsChecked && mc.Module.HasTypesFullyDefined) {
				var constraints = inflated.Constraints;
				if (constraints != null) {
					var cc = new ConstraintChecker (mc);
					if (cc.CheckAll (open_type, atypes, constraints, loc)) {
						inflated.HasConstraintsChecked = true;
					}
				}
			}

			return type;
		}

		public override bool Equals (object obj)
		{
			GenericTypeExpr cobj = obj as GenericTypeExpr;
			if (cobj == null)
				return false;

			if ((type == null) || (cobj.type == null))
				return false;

			return type == cobj.type;
		}

		public override int GetHashCode ()
		{
			return base.GetHashCode ();
		}
	}

	//
	// Generic type with unbound type arguments, used for typeof (G<,,>)
	//
	class GenericOpenTypeExpr : TypeExpression
	{
		public GenericOpenTypeExpr (TypeSpec type, /*UnboundTypeArguments args,*/ Location loc)
			: base (type.GetDefinition (), loc)
		{
		}
	}

	struct ConstraintChecker
	{
		IMemberContext mc;
		bool ignore_inferred_dynamic;
		bool recursive_checks;

		public ConstraintChecker (IMemberContext ctx)
		{
			this.mc = ctx;
			ignore_inferred_dynamic = false;
			recursive_checks = false;
		}

		#region Properties

		public bool IgnoreInferredDynamic {
			get {
				return ignore_inferred_dynamic;
			}
			set {
				ignore_inferred_dynamic = value;
			}
		}

		#endregion

		//
		// Checks the constraints of open generic type against type
		// arguments. This version is used for types which could not be
		// checked immediatelly during construction because the type
		// hierarchy was not yet fully setup (before Emit phase)
		//
		public static bool Check (IMemberContext mc, TypeSpec type, Location loc)
		{
			//
			// Check declaring type first if there is any
			//
			if (type.DeclaringType != null && !Check (mc, type.DeclaringType, loc))
				return false;

			while (type is ElementTypeSpec)
				type = ((ElementTypeSpec) type).Element;

			if (type.Arity == 0)
				return true;

			var gtype = type as InflatedTypeSpec;
			if (gtype == null)
				return true;

			var constraints = gtype.Constraints;
			if (constraints == null)
				return true;

			if (gtype.HasConstraintsChecked)
				return true;

			var cc = new ConstraintChecker (mc);
			cc.recursive_checks = true;

			if (cc.CheckAll (gtype.GetDefinition (), type.TypeArguments, constraints, loc)) {
				gtype.HasConstraintsChecked = true;
				return true;
			}

			return false;
		}

		//
		// Checks all type arguments againts type parameters constraints
		// NOTE: It can run in probing mode when `mc' is null
		//
		public bool CheckAll (MemberSpec context, TypeSpec[] targs, TypeParameterSpec[] tparams, Location loc)
		{
			for (int i = 0; i < tparams.Length; i++) {
				if (ignore_inferred_dynamic && targs[i].BuiltinType == BuiltinTypeSpec.Type.Dynamic)
					continue;

				var targ = targs[i];
				if (!CheckConstraint (context, targ, tparams [i], loc))
					return false;

				if (!recursive_checks)
					continue;

				if (!Check (mc, targ, loc))
					return false;
			}

			return true;
		}

		bool CheckConstraint (MemberSpec context, TypeSpec atype, TypeParameterSpec tparam, Location loc)
		{
			//
			// First, check the `class' and `struct' constraints.
			//
			if (tparam.HasSpecialClass && !TypeSpec.IsReferenceType (atype)) {
				if (mc != null) {
					mc.Module.Compiler.Report.Error (452, loc,
						"The type `{0}' must be a reference type in order to use it as type parameter `{1}' in the generic type or method `{2}'",
						TypeManager.CSharpName (atype), tparam.GetSignatureForError (), context.GetSignatureForError ());
				}

				return false;
			}

			if (tparam.HasSpecialStruct && (!TypeSpec.IsValueType (atype) || atype.IsNullableType)) {
				if (mc != null) {
					mc.Module.Compiler.Report.Error (453, loc,
						"The type `{0}' must be a non-nullable value type in order to use it as type parameter `{1}' in the generic type or method `{2}'",
						TypeManager.CSharpName (atype), tparam.GetSignatureForError (), context.GetSignatureForError ());
				}

				return false;
			}

			bool ok = true;

			//
			// Check the class constraint
			//
			if (tparam.HasTypeConstraint) {
				var dep = tparam.BaseType.GetMissingDependencies ();
				if (dep != null) {
					if (mc == null)
						return false;

					ImportedTypeDefinition.Error_MissingDependency (mc, dep, loc);
					ok = false;
				}

				if (!CheckConversion (mc, context, atype, tparam, tparam.BaseType, loc)) {
					if (mc == null)
						return false;

					ok = false;
				}
			}

			//
			// Check the interfaces constraints
			//
			if (tparam.Interfaces != null) {
				foreach (TypeSpec iface in tparam.Interfaces) {
					var dep = iface.GetMissingDependencies ();
					if (dep != null) {
						if (mc == null)
							return false;

						ImportedTypeDefinition.Error_MissingDependency (mc, dep, loc);
						ok = false;

						// return immediately to avoid duplicate errors because we are scanning
						// expanded interface list
						return false;
					}

					if (!CheckConversion (mc, context, atype, tparam, iface, loc)) {
						if (mc == null)
							return false;

						ok = false;
					}
				}
			}

			//
			// Check the type parameter constraint
			//
			if (tparam.TypeArguments != null) {
				foreach (var ta in tparam.TypeArguments) {
					if (!CheckConversion (mc, context, atype, tparam, ta, loc)) {
						if (mc == null)
							return false;

						ok = false;
					}
				}
			}

			//
			// Finally, check the constructor constraint.
			//
			if (!tparam.HasSpecialConstructor)
				return ok;

			if (!HasDefaultConstructor (atype)) {
				if (mc != null) {
					mc.Module.Compiler.Report.SymbolRelatedToPreviousError (atype);
					mc.Module.Compiler.Report.Error (310, loc,
						"The type `{0}' must have a public parameterless constructor in order to use it as parameter `{1}' in the generic type or method `{2}'",
						TypeManager.CSharpName (atype), tparam.GetSignatureForError (), context.GetSignatureForError ());
				}
				return false;
			}

			return ok;
		}

		static bool HasDynamicTypeArgument (TypeSpec[] targs)
		{
			for (int i = 0; i < targs.Length; ++i) {
				var targ = targs [i];
				if (targ.BuiltinType == BuiltinTypeSpec.Type.Dynamic)
					return true;

				if (HasDynamicTypeArgument (targ.TypeArguments))
					return true;
			}

			return false;
		}

		bool CheckConversion (IMemberContext mc, MemberSpec context, TypeSpec atype, TypeParameterSpec tparam, TypeSpec ttype, Location loc)
		{
			if (atype == ttype)
				return true;

			if (atype.IsGenericParameter) {
				var tps = (TypeParameterSpec) atype;
				if (tps.TypeArguments != null) {
					foreach (var targ in tps.TypeArguments) {
						if (TypeSpecComparer.Override.IsEqual (targ, ttype))
							return true;
					}
				}

				if (Convert.ImplicitTypeParameterConversion (null, tps, ttype) != null)
					return true;

			} else if (TypeSpec.IsValueType (atype)) {
				if (atype.IsNullableType) {
					//
					// LAMESPEC: Only identity or base type ValueType or Object satisfy nullable type
					//
					if (TypeSpec.IsBaseClass (atype, ttype, false))
						return true;
				} else {
					if (Convert.ImplicitBoxingConversion (null, atype, ttype) != null)
						return true;
				}
			} else {
				if (Convert.ImplicitReferenceConversionExists (atype, ttype) || Convert.ImplicitBoxingConversion (null, atype, ttype) != null)
					return true;
			}

			//
			// When partial/full type inference finds a dynamic type argument delay
			// the constraint check to runtime, it can succeed for real underlying
			// dynamic type
			//
			if (ignore_inferred_dynamic && HasDynamicTypeArgument (ttype.TypeArguments))
				return true;

			if (mc != null) {
				mc.Module.Compiler.Report.SymbolRelatedToPreviousError (tparam);
				if (atype.IsGenericParameter) {
					mc.Module.Compiler.Report.Error (314, loc,
						"The type `{0}' cannot be used as type parameter `{1}' in the generic type or method `{2}'. There is no boxing or type parameter conversion from `{0}' to `{3}'",
						atype.GetSignatureForError (), tparam.GetSignatureForError (), context.GetSignatureForError (), ttype.GetSignatureForError ());
				} else if (TypeSpec.IsValueType (atype)) {
					if (atype.IsNullableType) {
						if (ttype.IsInterface) {
							mc.Module.Compiler.Report.Error (313, loc,
								"The type `{0}' cannot be used as type parameter `{1}' in the generic type or method `{2}'. The nullable type `{0}' never satisfies interface constraint `{3}'",
								atype.GetSignatureForError (), tparam.GetSignatureForError (), context.GetSignatureForError (), ttype.GetSignatureForError ());
						} else {
							mc.Module.Compiler.Report.Error (312, loc,
								"The type `{0}' cannot be used as type parameter `{1}' in the generic type or method `{2}'. The nullable type `{0}' does not satisfy constraint `{3}'",
								atype.GetSignatureForError (), tparam.GetSignatureForError (), context.GetSignatureForError (), ttype.GetSignatureForError ());
						}
					} else {
						mc.Module.Compiler.Report.Error (315, loc,
							"The type `{0}' cannot be used as type parameter `{1}' in the generic type or method `{2}'. There is no boxing conversion from `{0}' to `{3}'",
							atype.GetSignatureForError (), tparam.GetSignatureForError (), context.GetSignatureForError (), ttype.GetSignatureForError ());
					}
				} else {
					mc.Module.Compiler.Report.Error (311, loc,
						"The type `{0}' cannot be used as type parameter `{1}' in the generic type or method `{2}'. There is no implicit reference conversion from `{0}' to `{3}'",
						atype.GetSignatureForError (), tparam.GetSignatureForError (), context.GetSignatureForError (), ttype.GetSignatureForError ());
				}
			}

			return false;
		}

		static bool HasDefaultConstructor (TypeSpec atype)
		{
			var tp = atype as TypeParameterSpec;
			if (tp != null) {
				return tp.HasSpecialConstructor || tp.HasSpecialStruct;
			}

			if (atype.IsStruct || atype.IsEnum)
				return true;

			if (atype.IsAbstract)
				return false;

			var tdef = atype.GetDefinition ();

			var found = MemberCache.FindMember (tdef,
				MemberFilter.Constructor (ParametersCompiled.EmptyReadOnlyParameters),
				BindingRestriction.DeclaredOnly | BindingRestriction.InstanceOnly);

			return found != null && (found.Modifiers & Modifiers.PUBLIC) != 0;
		}
	}

	public partial class TypeManager
	{
		public static Variance CheckTypeVariance (TypeSpec t, Variance expected, IMemberContext member)
		{
			var tp = t as TypeParameterSpec;
			if (tp != null) {
				Variance v = tp.Variance;
				if (expected == Variance.None && v != expected ||
					expected == Variance.Covariant && v == Variance.Contravariant ||
					expected == Variance.Contravariant && v == Variance.Covariant) {
					((TypeParameter)tp.MemberDefinition).ErrorInvalidVariance (member, expected);
				}

				return expected;
			}

			if (t.TypeArguments.Length > 0) {
				var targs_definition = t.MemberDefinition.TypeParameters;
				TypeSpec[] targs = GetTypeArguments (t);
				for (int i = 0; i < targs.Length; ++i) {
					Variance v = targs_definition[i].Variance;
					CheckTypeVariance (targs[i], (Variance) ((int)v * (int)expected), member);
				}

				return expected;
			}

			if (t.IsArray)
				return CheckTypeVariance (GetElementType (t), expected, member);

			return Variance.None;
		}
	}

	//
	// Implements C# type inference
	//
	class TypeInference
	{
		//
		// Tracks successful rate of type inference
		//
		int score = int.MaxValue;
		readonly Arguments arguments;
		readonly int arg_count;

		public TypeInference (Arguments arguments)
		{
			this.arguments = arguments;
			if (arguments != null)
				arg_count = arguments.Count;
		}

		public int InferenceScore {
			get {
				return score;
			}
		}

		public TypeSpec[] InferMethodArguments (ResolveContext ec, MethodSpec method)
		{
			var method_generic_args = method.GenericDefinition.TypeParameters;
			TypeInferenceContext context = new TypeInferenceContext (method_generic_args);
			if (!context.UnfixedVariableExists)
				return TypeSpec.EmptyTypes;

			AParametersCollection pd = method.Parameters;
			if (!InferInPhases (ec, context, pd))
				return null;

			return context.InferredTypeArguments;
		}

		//
		// Implements method type arguments inference
		//
		bool InferInPhases (ResolveContext ec, TypeInferenceContext tic, AParametersCollection methodParameters)
		{
			int params_arguments_start;
			if (methodParameters.HasParams) {
				params_arguments_start = methodParameters.Count - 1;
			} else {
				params_arguments_start = arg_count;
			}

			TypeSpec [] ptypes = methodParameters.Types;
			
			//
			// The first inference phase
			//
			TypeSpec method_parameter = null;
			for (int i = 0; i < arg_count; i++) {
				Argument a = arguments [i];
				if (a == null)
					continue;
				
				if (i < params_arguments_start) {
					method_parameter = methodParameters.Types [i];
				} else if (i == params_arguments_start) {
					if (arg_count == params_arguments_start + 1 && TypeManager.HasElementType (a.Type))
						method_parameter = methodParameters.Types [params_arguments_start];
					else
						method_parameter = TypeManager.GetElementType (methodParameters.Types [params_arguments_start]);

					ptypes = (TypeSpec[]) ptypes.Clone ();
					ptypes [i] = method_parameter;
				}

				//
				// When a lambda expression, an anonymous method
				// is used an explicit argument type inference takes a place
				//
				AnonymousMethodExpression am = a.Expr as AnonymousMethodExpression;
				if (am != null) {
					if (am.ExplicitTypeInference (ec, tic, method_parameter))
						--score; 
					continue;
				}

				if (a.IsByRef) {
					score -= tic.ExactInference (a.Type, method_parameter);
					continue;
				}

				if (a.Expr.Type == InternalType.NullLiteral)
					continue;

				if (TypeSpec.IsValueType (method_parameter)) {
					score -= tic.LowerBoundInference (a.Type, method_parameter);
					continue;
				}

				//
				// Otherwise an output type inference is made
				//
				score -= tic.OutputTypeInference (ec, a.Expr, method_parameter);
			}

			//
			// Part of the second phase but because it happens only once
			// we don't need to call it in cycle
			//
			bool fixed_any = false;
			if (!tic.FixIndependentTypeArguments (ec, ptypes, ref fixed_any))
				return false;

			return DoSecondPhase (ec, tic, ptypes, !fixed_any);
		}

		bool DoSecondPhase (ResolveContext ec, TypeInferenceContext tic, TypeSpec[] methodParameters, bool fixDependent)
		{
			bool fixed_any = false;
			if (fixDependent && !tic.FixDependentTypes (ec, ref fixed_any))
				return false;

			// If no further unfixed type variables exist, type inference succeeds
			if (!tic.UnfixedVariableExists)
				return true;

			if (!fixed_any && fixDependent)
				return false;
			
			// For all arguments where the corresponding argument output types
			// contain unfixed type variables but the input types do not,
			// an output type inference is made
			for (int i = 0; i < arg_count; i++) {
				
				// Align params arguments
				TypeSpec t_i = methodParameters [i >= methodParameters.Length ? methodParameters.Length - 1: i];
				
				if (!t_i.IsDelegate) {
					if (!t_i.IsExpressionTreeType)
						continue;

					t_i = TypeManager.GetTypeArguments (t_i) [0];
				}

				var mi = Delegate.GetInvokeMethod (t_i);
				TypeSpec rtype = mi.ReturnType;

				if (tic.IsReturnTypeNonDependent (ec, mi, rtype)) {
					// It can be null for default arguments
					if (arguments[i] == null)
						continue;

					score -= tic.OutputTypeInference (ec, arguments[i].Expr, t_i);
				}
			}


			return DoSecondPhase (ec, tic, methodParameters, true);
		}
	}

	public class TypeInferenceContext
	{
		protected enum BoundKind
		{
			Exact	= 0,
			Lower	= 1,
			Upper	= 2
		}

		protected class BoundInfo : IEquatable<BoundInfo>
		{
			public readonly TypeSpec Type;
			public readonly BoundKind Kind;

			public BoundInfo (TypeSpec type, BoundKind kind)
			{
				this.Type = type;
				this.Kind = kind;
			}
			
			public override int GetHashCode ()
			{
				return Type.GetHashCode ();
			}

			public virtual Expression GetTypeExpression ()
			{
				return new TypeExpression (Type, Location.Null);
			}

			#region IEquatable<BoundInfo> Members

			public virtual bool Equals (BoundInfo other)
			{
				return Type == other.Type && Kind == other.Kind;
			}

			#endregion
		}

		readonly TypeSpec[] tp_args;
		readonly TypeSpec[] fixed_types;
		readonly List<BoundInfo>[] bounds;
		bool failed;

		// TODO MemberCache: Could it be TypeParameterSpec[] ??
		public TypeInferenceContext (TypeSpec[] typeArguments)
		{
			if (typeArguments.Length == 0)
				throw new ArgumentException ("Empty generic arguments");

			fixed_types = new TypeSpec [typeArguments.Length];
			for (int i = 0; i < typeArguments.Length; ++i) {
				if (typeArguments [i].IsGenericParameter) {
					if (bounds == null) {
						bounds = new List<BoundInfo> [typeArguments.Length];
						tp_args = new TypeSpec [typeArguments.Length];
					}
					tp_args [i] = typeArguments [i];
				} else {
					fixed_types [i] = typeArguments [i];
				}
			}
		}

		// 
		// Used together with AddCommonTypeBound fo implement
		// 7.4.2.13 Finding the best common type of a set of expressions
		//
		public TypeInferenceContext ()
		{
			fixed_types = new TypeSpec [1];
			tp_args = new TypeSpec [1];
			tp_args[0] = InternalType.Arglist; // it can be any internal type
			bounds = new List<BoundInfo> [1];
		}

		public TypeSpec[] InferredTypeArguments {
			get {
				return fixed_types;
			}
		}

		public void AddCommonTypeBound (TypeSpec type)
		{
			AddToBounds (new BoundInfo (type, BoundKind.Lower), 0);
		}

		protected void AddToBounds (BoundInfo bound, int index)
		{
			//
			// Some types cannot be used as type arguments
			//
			if (bound.Type.Kind == MemberKind.Void || bound.Type.IsPointer || bound.Type.IsSpecialRuntimeType ||
				bound.Type == InternalType.MethodGroup || bound.Type == InternalType.AnonymousMethod)
				return;

			var a = bounds [index];
			if (a == null) {
				a = new List<BoundInfo> (2);
				a.Add (bound);
				bounds [index] = a;
				return;
			}

			if (a.Contains (bound))
				return;

			a.Add (bound);
		}
		
		bool AllTypesAreFixed (TypeSpec[] types)
		{
			foreach (TypeSpec t in types) {
				if (t.IsGenericParameter) {
					if (!IsFixed (t))
						return false;
					continue;
				}

				if (TypeManager.IsGenericType (t))
					return AllTypesAreFixed (TypeManager.GetTypeArguments (t));
			}
			
			return true;
		}		

		//
		// 26.3.3.8 Exact Inference
		//
		public int ExactInference (TypeSpec u, TypeSpec v)
		{
			// If V is an array type
			if (v.IsArray) {
				if (!u.IsArray)
					return 0;

				var ac_u = (ArrayContainer) u;
				var ac_v = (ArrayContainer) v;
				if (ac_u.Rank != ac_v.Rank)
					return 0;

				return ExactInference (ac_u.Element, ac_v.Element);
			}

			// If V is constructed type and U is constructed type
			if (TypeManager.IsGenericType (v)) {
				if (!TypeManager.IsGenericType (u) || v.MemberDefinition != u.MemberDefinition)
					return 0;

				TypeSpec [] ga_u = TypeManager.GetTypeArguments (u);
				TypeSpec [] ga_v = TypeManager.GetTypeArguments (v);
				if (ga_u.Length != ga_v.Length)
					return 0;

				int score = 0;
				for (int i = 0; i < ga_u.Length; ++i)
					score += ExactInference (ga_u [i], ga_v [i]);

				return score > 0 ? 1 : 0;
			}

			// If V is one of the unfixed type arguments
			int pos = IsUnfixed (v);
			if (pos == -1)
				return 0;

			AddToBounds (new BoundInfo (u, BoundKind.Exact), pos);
			return 1;
		}

		public bool FixAllTypes (ResolveContext ec)
		{
			for (int i = 0; i < tp_args.Length; ++i) {
				if (!FixType (ec, i))
					return false;
			}
			return true;
		}

		//
		// All unfixed type variables Xi are fixed for which all of the following hold:
		// a, There is at least one type variable Xj that depends on Xi
		// b, Xi has a non-empty set of bounds
		// 
		public bool FixDependentTypes (ResolveContext ec, ref bool fixed_any)
		{
			for (int i = 0; i < tp_args.Length; ++i) {
				if (fixed_types[i] != null)
					continue;

				if (bounds[i] == null)
					continue;

				if (!FixType (ec, i))
					return false;
				
				fixed_any = true;
			}

			return true;
		}

		//
		// All unfixed type variables Xi which depend on no Xj are fixed
		//
		public bool FixIndependentTypeArguments (ResolveContext ec, TypeSpec[] methodParameters, ref bool fixed_any)
		{
			var types_to_fix = new List<TypeSpec> (tp_args);
			for (int i = 0; i < methodParameters.Length; ++i) {
				TypeSpec t = methodParameters[i];

				if (!t.IsDelegate) {
					if (!t.IsExpressionTreeType)
						continue;

					t =  TypeManager.GetTypeArguments (t) [0];
				}

				if (t.IsGenericParameter)
					continue;

				var invoke = Delegate.GetInvokeMethod (t);
				TypeSpec rtype = invoke.ReturnType;
				while (rtype.IsArray)
					rtype = ((ArrayContainer) rtype).Element;

				if (!rtype.IsGenericParameter && !TypeManager.IsGenericType (rtype))
					continue;

				// Remove dependent types, they cannot be fixed yet
				RemoveDependentTypes (types_to_fix, rtype);
			}

			foreach (TypeSpec t in types_to_fix) {
				if (t == null)
					continue;

				int idx = IsUnfixed (t);
				if (idx >= 0 && !FixType (ec, idx)) {
					return false;
				}
			}

			fixed_any = types_to_fix.Count > 0;
			return true;
		}

		//
		// 26.3.3.10 Fixing
		//
		public bool FixType (ResolveContext ec, int i)
		{
			// It's already fixed
			if (fixed_types[i] != null)
				throw new InternalErrorException ("Type argument has been already fixed");

			if (failed)
				return false;

			var candidates = bounds [i];
			if (candidates == null)
				return false;

			if (candidates.Count == 1) {
				TypeSpec t = candidates[0].Type;
				if (t == InternalType.NullLiteral)
					return false;

				fixed_types [i] = t;
				return true;
			}

			//
			// Determines a unique type from which there is
			// a standard implicit conversion to all the other
			// candidate types.
			//
			TypeSpec best_candidate = null;
			int cii;
			int candidates_count = candidates.Count;
			for (int ci = 0; ci < candidates_count; ++ci) {
				BoundInfo bound = candidates [ci];
				for (cii = 0; cii < candidates_count; ++cii) {
					if (cii == ci)
						continue;

					BoundInfo cbound = candidates[cii];
					
					// Same type parameters with different bounds
					if (cbound.Type == bound.Type) {
						if (bound.Kind != BoundKind.Exact)
							bound = cbound;

						continue;
					}

					if (bound.Kind == BoundKind.Exact || cbound.Kind == BoundKind.Exact) {
						if (cbound.Kind == BoundKind.Lower) {
							if (!Convert.ImplicitConversionExists (ec, cbound.GetTypeExpression (), bound.Type)) {
								break;
							}

							continue;
						}
						if (cbound.Kind == BoundKind.Upper) {
							if (!Convert.ImplicitConversionExists (ec, bound.GetTypeExpression (), cbound.Type)) {
								break;
							}

							continue;
						}
						
						if (bound.Kind != BoundKind.Exact) {
							if (!Convert.ImplicitConversionExists (ec, bound.GetTypeExpression (), cbound.Type)) {
								break;
							}

							bound = cbound;
							continue;
						}
						
						break;
					}

					if (bound.Kind == BoundKind.Lower) {
						if (cbound.Kind == BoundKind.Lower) {
							if (!Convert.ImplicitConversionExists (ec, cbound.GetTypeExpression (), bound.Type)) {
								break;
							}
						} else {
							if (!Convert.ImplicitConversionExists (ec, bound.GetTypeExpression (), cbound.Type)) {
								break;
							}

							bound = cbound;
						}

						continue;
					}

					if (bound.Kind == BoundKind.Upper) {
						if (!Convert.ImplicitConversionExists (ec, bound.GetTypeExpression (), cbound.Type)) {
							break;
						}
					} else {
						throw new NotImplementedException ("variance conversion");
					}
				}

				if (cii != candidates_count)
					continue;

				//
				// We already have the best candidate, break if thet are different
				//
				// Dynamic is never ambiguous as we prefer dynamic over other best candidate types
				//
				if (best_candidate != null) {

					if (best_candidate.BuiltinType == BuiltinTypeSpec.Type.Dynamic)
						continue;

					if (bound.Type.BuiltinType != BuiltinTypeSpec.Type.Dynamic && best_candidate != bound.Type)
						return false;
				}

				best_candidate = bound.Type;
			}

			if (best_candidate == null)
				return false;

			fixed_types[i] = best_candidate;
			return true;
		}

		public bool HasBounds (int pos)
		{
			return bounds[pos] != null;
		}
		
		//
		// Uses inferred or partially infered types to inflate delegate type argument. Returns
		// null when type parameter has not been fixed
		//
		public TypeSpec InflateGenericArgument (IModuleContext context, TypeSpec parameter)
		{
			var tp = parameter as TypeParameterSpec;
			if (tp != null) {
				//
				// Type inference works on generic arguments (MVAR) only
				//
				if (!tp.IsMethodOwned)
					return parameter;

				//
				// Ensure the type parameter belongs to same container
				//
				if (tp.DeclaredPosition < tp_args.Length && tp_args[tp.DeclaredPosition] == parameter)
					return fixed_types[tp.DeclaredPosition] ?? parameter;

				return parameter;
			}

			var gt = parameter as InflatedTypeSpec;
			if (gt != null) {
				var inflated_targs = new TypeSpec [gt.TypeArguments.Length];
				for (int ii = 0; ii < inflated_targs.Length; ++ii) {
					var inflated = InflateGenericArgument (context, gt.TypeArguments [ii]);
					if (inflated == null)
						return null;

					inflated_targs[ii] = inflated;
				}

				return gt.GetDefinition ().MakeGenericType (context, inflated_targs);
			}

			var ac = parameter as ArrayContainer;
			if (ac != null) {
				var inflated = InflateGenericArgument (context, ac.Element);
				if (inflated != ac.Element)
					return ArrayContainer.MakeType (context.Module, inflated);
			}

			return parameter;
		}
		
		//
		// Tests whether all delegate input arguments are fixed and generic output type
		// requires output type inference 
		//
		public bool IsReturnTypeNonDependent (ResolveContext ec, MethodSpec invoke, TypeSpec returnType)
		{
			while (returnType.IsArray)
				returnType = ((ArrayContainer) returnType).Element;

			if (returnType.IsGenericParameter) {
				if (IsFixed (returnType))
				    return false;
			} else if (TypeManager.IsGenericType (returnType)) {
				if (returnType.IsDelegate) {
					invoke = Delegate.GetInvokeMethod (returnType);
					return IsReturnTypeNonDependent (ec, invoke, invoke.ReturnType);
				}
					
				TypeSpec[] g_args = TypeManager.GetTypeArguments (returnType);
				
				// At least one unfixed return type has to exist 
				if (AllTypesAreFixed (g_args))
					return false;
			} else {
				return false;
			}

			// All generic input arguments have to be fixed
			AParametersCollection d_parameters = invoke.Parameters;
			return AllTypesAreFixed (d_parameters.Types);
		}
		
		bool IsFixed (TypeSpec type)
		{
			return IsUnfixed (type) == -1;
		}		

		int IsUnfixed (TypeSpec type)
		{
			if (!type.IsGenericParameter)
				return -1;

			for (int i = 0; i < tp_args.Length; ++i) {
				if (tp_args[i] == type) {
					if (fixed_types[i] != null)
						break;

					return i;
				}
			}

			return -1;
		}

		//
		// 26.3.3.9 Lower-bound Inference
		//
		public int LowerBoundInference (TypeSpec u, TypeSpec v)
		{
			return LowerBoundInference (u, v, false);
		}

		//
		// Lower-bound (false) or Upper-bound (true) inference based on inversed argument
		//
		int LowerBoundInference (TypeSpec u, TypeSpec v, bool inversed)
		{
			// If V is one of the unfixed type arguments
			int pos = IsUnfixed (v);
			if (pos != -1) {
				AddToBounds (new BoundInfo (u, inversed ? BoundKind.Upper : BoundKind.Lower), pos);
				return 1;
			}			

			// If U is an array type
			var u_ac = u as ArrayContainer;
			if (u_ac != null) {
				var v_ac = v as ArrayContainer;
				if (v_ac != null) {
					if (u_ac.Rank != v_ac.Rank)
						return 0;

					if (TypeSpec.IsValueType (u_ac.Element))
						return ExactInference (u_ac.Element, v_ac.Element);

					return LowerBoundInference (u_ac.Element, v_ac.Element, inversed);
				}

				if (u_ac.Rank != 1 || !v.IsGenericIterateInterface)
					return 0;

				var v_i = TypeManager.GetTypeArguments (v) [0];
				if (TypeSpec.IsValueType (u_ac.Element))
					return ExactInference (u_ac.Element, v_i);

				return LowerBoundInference (u_ac.Element, v_i);
			}
			
			if (v.IsGenericOrParentIsGeneric) {
				//
				// if V is a constructed type C<V1..Vk> and there is a unique type C<U1..Uk>
				// such that U is identical to, inherits from (directly or indirectly),
				// or implements (directly or indirectly) C<U1..Uk>
				//
				var u_candidates = new List<TypeSpec> ();
				var open_v = v.MemberDefinition;

				for (TypeSpec t = u; t != null; t = t.BaseType) {
					if (open_v == t.MemberDefinition)
						u_candidates.Add (t);

					//
					// Using this trick for dynamic type inference, the spec says the type arguments are "unknown" but
					// that would complicate the process a lot, instead I treat them as dynamic
					//
					if (t.BuiltinType == BuiltinTypeSpec.Type.Dynamic)
						u_candidates.Add (t);

					if (t.Interfaces != null) {
						foreach (var iface in t.Interfaces) {
							if (open_v == iface.MemberDefinition)
								u_candidates.Add (iface);
						}
					}
				}

				TypeSpec[] unique_candidate_targs = null;
				var ga_v = TypeSpec.GetAllTypeArguments (v);
				foreach (TypeSpec u_candidate in u_candidates) {
					//
					// The unique set of types U1..Uk means that if we have an interface I<T>,
					// class U : I<int>, I<long> then no type inference is made when inferring
					// type I<T> by applying type U because T could be int or long
					//
					if (unique_candidate_targs != null) {
						TypeSpec[] second_unique_candidate_targs = TypeSpec.GetAllTypeArguments (u_candidate);
						if (TypeSpecComparer.Equals (unique_candidate_targs, second_unique_candidate_targs)) {
							unique_candidate_targs = second_unique_candidate_targs;
							continue;
						}

						//
						// This should always cause type inference failure
						//
						failed = true;
						return 1;
					}

					//
					// A candidate is dynamic type expression, to simplify things use dynamic
					// for all type parameter of this type. For methods like this one
					// 
					// void M<T, U> (IList<T>, IList<U[]>)
					//
					// dynamic becomes both T and U when the arguments are of dynamic type
					//
					if (u_candidate.BuiltinType == BuiltinTypeSpec.Type.Dynamic) {
						unique_candidate_targs = new TypeSpec[ga_v.Length];
						for (int i = 0; i < unique_candidate_targs.Length; ++i)
							unique_candidate_targs[i] = u_candidate;
					} else {
						unique_candidate_targs = TypeSpec.GetAllTypeArguments (u_candidate);
					}
				}

				if (unique_candidate_targs != null) {
					int score = 0;
					int tp_index = -1;
					TypeParameterSpec[] tps = null;

					for (int i = 0; i < unique_candidate_targs.Length; ++i) {
						if (tp_index < 0) {
							while (v.Arity == 0)
								v = v.DeclaringType;

							tps = v.MemberDefinition.TypeParameters;
							tp_index = tps.Length - 1;
						}

						Variance variance = tps [tp_index--].Variance;

						TypeSpec u_i = unique_candidate_targs [i];
						if (variance == Variance.None || TypeSpec.IsValueType (u_i)) {
							if (ExactInference (u_i, ga_v [i]) == 0)
								++score;
						} else {
							bool upper_bound = (variance == Variance.Contravariant && !inversed) ||
								(variance == Variance.Covariant && inversed);

							if (LowerBoundInference (u_i, ga_v [i], upper_bound) == 0)
								++score;
						}
					}

					return score;
				}
			}

			return 0;
		}

		//
		// 26.3.3.6 Output Type Inference
		//
		public int OutputTypeInference (ResolveContext ec, Expression e, TypeSpec t)
		{
			// If e is a lambda or anonymous method with inferred return type
			AnonymousMethodExpression ame = e as AnonymousMethodExpression;
			if (ame != null) {
				TypeSpec rt = ame.InferReturnType (ec, this, t);
				var invoke = Delegate.GetInvokeMethod (t);

				if (rt == null) {
					AParametersCollection pd = invoke.Parameters;
					return ame.Parameters.Count == pd.Count ? 1 : 0;
				}

				TypeSpec rtype = invoke.ReturnType;
				return LowerBoundInference (rt, rtype) + 1;
			}

			//
			// if E is a method group and T is a delegate type or expression tree type
			// return type Tb with parameter types T1..Tk and return type Tb, and overload
			// resolution of E with the types T1..Tk yields a single method with return type U,
			// then a lower-bound inference is made from U for Tb.
			//
			if (e is MethodGroupExpr) {
				if (!t.IsDelegate) {
					if (!t.IsExpressionTreeType)
						return 0;

					t = TypeManager.GetTypeArguments (t)[0];
				}

				var invoke = Delegate.GetInvokeMethod (t);
				TypeSpec rtype = invoke.ReturnType;

				if (!rtype.IsGenericParameter && !TypeManager.IsGenericType (rtype))
					return 0;

				// LAMESPEC: Standard does not specify that all methodgroup arguments
				// has to be fixed but it does not specify how to do recursive type inference
				// either. We choose the simple option and infer return type only
				// if all delegate generic arguments are fixed.
				TypeSpec[] param_types = new TypeSpec [invoke.Parameters.Count];
				for (int i = 0; i < param_types.Length; ++i) {
					var inflated = InflateGenericArgument (ec, invoke.Parameters.Types[i]);
					if (inflated == null)
						return 0;

					if (IsUnfixed (inflated) >= 0)
						return 0;

					param_types[i] = inflated;
				}

				MethodGroupExpr mg = (MethodGroupExpr) e;
				Arguments args = DelegateCreation.CreateDelegateMethodArguments (invoke.Parameters, param_types, e.Location);
				mg = mg.OverloadResolve (ec, ref args, null, OverloadResolver.Restrictions.CovariantDelegate | OverloadResolver.Restrictions.ProbingOnly);
				if (mg == null)
					return 0;

				return LowerBoundInference (mg.BestCandidateReturnType, rtype) + 1;
			}

			//
			// if e is an expression with type U, then
			// a lower-bound inference is made from U for T
			//
			return LowerBoundInference (e.Type, t) * 2;
		}

		void RemoveDependentTypes (List<TypeSpec> types, TypeSpec returnType)
		{
			int idx = IsUnfixed (returnType);
			if (idx >= 0) {
				types [idx] = null;
				return;
			}

			if (TypeManager.IsGenericType (returnType)) {
				foreach (TypeSpec t in TypeManager.GetTypeArguments (returnType)) {
					RemoveDependentTypes (types, t);
				}
			}
		}

		public bool UnfixedVariableExists {
			get {
				foreach (TypeSpec ut in fixed_types) {
					if (ut == null)
						return true;
				}

				return false;
			}
		}
	}
}
