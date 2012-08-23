//
// anonymous.cs: Support for anonymous methods and types
//
// Author:
//   Miguel de Icaza (miguel@ximain.com)
//   Marek Safar (marek.safar@gmail.com)
//
// Dual licensed under the terms of the MIT X11 or GNU GPL
// Copyright 2003-2011 Novell, Inc.
// Copyright 2011 Xamarin Inc
//

using System;
using System.Collections.Generic;
using Mono.CompilerServices.SymbolWriter;
using System.Diagnostics;

#if STATIC
using IKVM.Reflection;
using IKVM.Reflection.Emit;
using System.Diagnostics;
#else
using System.Reflection;
using System.Reflection.Emit;
#endif

namespace Mono.CSharp {

	public abstract class CompilerGeneratedContainer : ClassOrStruct
	{
		protected CompilerGeneratedContainer (TypeContainer parent, MemberName name, Modifiers mod)
			: this (parent, name, mod, MemberKind.Class)
		{
		}

		protected CompilerGeneratedContainer (TypeContainer parent, MemberName name, Modifiers mod, MemberKind kind)
			: base (parent, name, null, kind)
		{
			Debug.Assert ((mod & Modifiers.AccessibilityMask) != 0);

			ModFlags = mod | Modifiers.COMPILER_GENERATED | Modifiers.SEALED;
			spec = new TypeSpec (Kind, null, this, null, ModFlags);
		}

		protected void CheckMembersDefined ()
		{
			if (HasMembersDefined)
				throw new InternalErrorException ("Helper class already defined!");
		}

		protected override bool DoDefineMembers ()
		{
			if (Kind == MemberKind.Class && !IsStatic && !PartialContainer.HasInstanceConstructor) {
				DefineDefaultConstructor (false);
			}

			return base.DoDefineMembers ();
		}

		protected static MemberName MakeMemberName (MemberBase host, string name, int unique_id, TypeParameters tparams, Location loc)
		{
			string host_name = host == null ? null : host is InterfaceMemberBase ? ((InterfaceMemberBase)host).GetFullName (host.MemberName) : host.MemberName.Name;
			string tname = MakeName (host_name, "c", name, unique_id);
			TypeParameters args = null;
			if (tparams != null) {
				args = new TypeParameters (tparams.Count);

				// Type parameters will be filled later when we have TypeContainer
				// instance, for now we need only correct arity to create valid name
				for (int i = 0; i < tparams.Count; ++i)
					args.Add ((TypeParameter) null);
			}

			return new MemberName (tname, args, loc);
		}

		public static string MakeName (string host, string typePrefix, string name, int id)
		{
			return "<" + host + ">" + typePrefix + "__" + name + id.ToString ("X");
		}

		protected override TypeSpec[] ResolveBaseTypes (out FullNamedExpression base_class)
		{
			base_type = Compiler.BuiltinTypes.Object;

			base_class = null;
			return null;
		}
	}

	public class HoistedStoreyClass : CompilerGeneratedContainer
	{
		public sealed class HoistedField : Field
		{
			public HoistedField (HoistedStoreyClass parent, FullNamedExpression type, Modifiers mod, string name,
				  Attributes attrs, Location loc)
				: base (parent, type, mod, new MemberName (name, loc), attrs)
			{
			}

			protected override bool ResolveMemberType ()
			{
				if (!base.ResolveMemberType ())
					return false;

				HoistedStoreyClass parent = ((HoistedStoreyClass) Parent).GetGenericStorey ();
				if (parent != null && parent.Mutator != null)
					member_type = parent.Mutator.Mutate (MemberType);

				return true;
			}
		}

		protected TypeParameterMutator mutator;

		public HoistedStoreyClass (TypeDefinition parent, MemberName name, TypeParameters tparams, Modifiers mods, MemberKind kind)
			: base (parent, name, mods | Modifiers.PRIVATE, kind)
		{

			if (tparams != null) {
				var type_params = name.TypeParameters;
				var src = new TypeParameterSpec[tparams.Count];
				var dst = new TypeParameterSpec[tparams.Count];

				for (int i = 0; i < tparams.Count; ++i) {
					type_params[i] = tparams[i].CreateHoistedCopy (spec);

					src[i] = tparams[i].Type;
					dst[i] = type_params[i].Type;
				}

				// A copy is not enough, inflate any type parameter constraints
				// using a new type parameters
				var inflator = new TypeParameterInflator (this, null, src, dst);
				for (int i = 0; i < tparams.Count; ++i) {
					src[i].InflateConstraints (inflator, dst[i]);
				}

				mutator = new TypeParameterMutator (tparams, type_params);
			}
		}

		#region Properties

		public TypeParameterMutator Mutator {
			get {
				return mutator;
			}
			set {
				mutator = value;
			}
		}

		#endregion

		public HoistedStoreyClass GetGenericStorey ()
		{
			TypeContainer storey = this;
			while (storey != null && storey.CurrentTypeParameters == null)
				storey = storey.Parent;

			return storey as HoistedStoreyClass;
		}
	}


	//
	// Anonymous method storey is created when an anonymous method uses
	// variable or parameter from outer scope. They are then hoisted to
	// anonymous method storey (captured)
	//
	public class AnonymousMethodStorey : HoistedStoreyClass
	{
		struct StoreyFieldPair
		{
			public readonly AnonymousMethodStorey Storey;
			public readonly Field Field;

			public StoreyFieldPair (AnonymousMethodStorey storey, Field field)
			{
				this.Storey = storey;
				this.Field = field;
			}
		}

		//
		// Needed to delay hoisted _this_ initialization. When an anonymous
		// method is used inside ctor and _this_ is hoisted, base ctor has to
		// be called first, otherwise _this_ will be initialized with 
		// uninitialized value.
		//
		sealed class ThisInitializer : Statement
		{
			readonly HoistedThis hoisted_this;

			public ThisInitializer (HoistedThis hoisted_this)
			{
				this.hoisted_this = hoisted_this;
			}

			protected override void DoEmit (EmitContext ec)
			{
				hoisted_this.EmitAssign (ec, new CompilerGeneratedThis (ec.CurrentType, loc), false, false);
			}

			protected override void CloneTo (CloneContext clonectx, Statement target)
			{
				// Nothing to clone
			}
		}

		// Unique storey ID
		public readonly int ID;

		public readonly ExplicitBlock OriginalSourceBlock;

		// A list of StoreyFieldPair with local field keeping parent storey instance
		List<StoreyFieldPair> used_parent_storeys;
		List<ExplicitBlock> children_references;

		// A list of hoisted parameters
		protected List<HoistedParameter> hoisted_params;
		List<HoistedParameter> hoisted_local_params;
		protected List<HoistedVariable> hoisted_locals;

		// Hoisted this
		protected HoistedThis hoisted_this;

		// Local variable which holds this storey instance
		public Expression Instance;

		bool initialize_hoisted_this;

		public AnonymousMethodStorey (ExplicitBlock block, TypeDefinition parent, MemberBase host, TypeParameters tparams, string name, MemberKind kind)
			: base (parent, MakeMemberName (host, name, parent.Module.CounterAnonymousContainers, tparams, block.StartLocation),
				tparams, 0, kind)
		{
			OriginalSourceBlock = block;
			ID = parent.Module.CounterAnonymousContainers++;
		}

		public void AddCapturedThisField (EmitContext ec)
		{
			TypeExpr type_expr = new TypeExpression (ec.CurrentType, Location);
			Field f = AddCompilerGeneratedField ("$this", type_expr);
			hoisted_this = new HoistedThis (this, f);

			initialize_hoisted_this = true;
		}

		public Field AddCapturedVariable (string name, TypeSpec type)
		{
			CheckMembersDefined ();

			FullNamedExpression field_type = new TypeExpression (type, Location);
			if (!spec.IsGenericOrParentIsGeneric)
				return AddCompilerGeneratedField (name, field_type);

			const Modifiers mod = Modifiers.INTERNAL | Modifiers.COMPILER_GENERATED;
			Field f = new HoistedField (this, field_type, mod, name, null, Location);
			AddField (f);
			return f;
		}

		protected Field AddCompilerGeneratedField (string name, FullNamedExpression type)
		{
			return AddCompilerGeneratedField (name, type, false);
		}

		protected Field AddCompilerGeneratedField (string name, FullNamedExpression type, bool privateAccess)
		{
			Modifiers mod = Modifiers.COMPILER_GENERATED | (privateAccess ? Modifiers.PRIVATE : Modifiers.INTERNAL);
			Field f = new Field (this, type, mod, new MemberName (name, Location), null);
			AddField (f);
			return f;
		}

		//
		// Creates a link between hoisted variable block and the anonymous method storey
		//
		// An anonymous method can reference variables from any outer block, but they are
		// hoisted in their own ExplicitBlock. When more than one block is referenced we
		// need to create another link between those variable storeys
		//
		public void AddReferenceFromChildrenBlock (ExplicitBlock block)
		{
			if (children_references == null)
				children_references = new List<ExplicitBlock> ();

			if (!children_references.Contains (block))
				children_references.Add (block);
		}

		public void AddParentStoreyReference (EmitContext ec, AnonymousMethodStorey storey)
		{
			CheckMembersDefined ();

			if (used_parent_storeys == null)
				used_parent_storeys = new List<StoreyFieldPair> ();
			else if (used_parent_storeys.Exists (i => i.Storey == storey))
				return;

			TypeExpr type_expr = storey.CreateStoreyTypeExpression (ec);
			Field f = AddCompilerGeneratedField ("<>f__ref$" + storey.ID, type_expr);
			used_parent_storeys.Add (new StoreyFieldPair (storey, f));
		}

		public void CaptureLocalVariable (ResolveContext ec, LocalVariable localVariable)
		{
			if (this is StateMachine) {
				if (ec.CurrentBlock.ParametersBlock != localVariable.Block.ParametersBlock)
					ec.CurrentBlock.Explicit.HasCapturedVariable = true;
			} else {
				ec.CurrentBlock.Explicit.HasCapturedVariable = true;
			}

			var hoisted = localVariable.HoistedVariant;
			if (hoisted != null && hoisted.Storey != this && hoisted.Storey is StateMachine) {
				// TODO: It's too late the field is defined in HoistedLocalVariable ctor
				hoisted.Storey.hoisted_locals.Remove (hoisted);
				hoisted = null;
			}

			if (hoisted == null) {
				hoisted = new HoistedLocalVariable (this, localVariable, GetVariableMangledName (localVariable));
				localVariable.HoistedVariant = hoisted;

				if (hoisted_locals == null)
					hoisted_locals = new List<HoistedVariable> ();

				hoisted_locals.Add (hoisted);
			}

			if (ec.CurrentBlock.Explicit != localVariable.Block.Explicit && !(hoisted.Storey is StateMachine))
				hoisted.Storey.AddReferenceFromChildrenBlock (ec.CurrentBlock.Explicit);
		}

		public void CaptureParameter (ResolveContext ec, ParametersBlock.ParameterInfo parameterInfo, ParameterReference parameterReference)
		{
			if (!(this is StateMachine)) {
				ec.CurrentBlock.Explicit.HasCapturedVariable = true;
			}

			var hoisted = parameterInfo.Parameter.HoistedVariant;

			if (parameterInfo.Block.StateMachine != null) {
				//
				// Another storey in same block exists but state machine does not
				// have parameter captured. We need to add it there as well to
				// proxy parameter value correctly.
				//
				if (hoisted == null && parameterInfo.Block.StateMachine != this) {
					var storey = parameterInfo.Block.StateMachine;

					hoisted = new HoistedParameter (storey, parameterReference);
					parameterInfo.Parameter.HoistedVariant = hoisted;

					if (storey.hoisted_params == null)
						storey.hoisted_params = new List<HoistedParameter> ();

					storey.hoisted_params.Add (hoisted);
				}

				//
				// Lift captured parameter from value type storey to reference type one. Otherwise
				// any side effects would be done on a copy
				//
				if (hoisted != null && hoisted.Storey != this && hoisted.Storey is StateMachine) {
					if (hoisted_local_params == null)
						hoisted_local_params = new List<HoistedParameter> ();

					hoisted_local_params.Add (hoisted);
					hoisted = null;
				}
			}

			if (hoisted == null) {
				hoisted = new HoistedParameter (this, parameterReference);
				parameterInfo.Parameter.HoistedVariant = hoisted;

				if (hoisted_params == null)
					hoisted_params = new List<HoistedParameter> ();

				hoisted_params.Add (hoisted);
			}

			//
			// Register link between current block and parameter storey. It will
			// be used when setting up storey definition to deploy storey reference
			// when parameters are used from multiple blocks
			//
			if (ec.CurrentBlock.Explicit != parameterInfo.Block) {
				hoisted.Storey.AddReferenceFromChildrenBlock (ec.CurrentBlock.Explicit);
			}
		}

		TypeExpr CreateStoreyTypeExpression (EmitContext ec)
		{
			//
			// Create an instance of storey type
			//
			TypeExpr storey_type_expr;
			if (CurrentTypeParameters != null) {
				//
				// Use current method type parameter (MVAR) for top level storey only. All
				// nested storeys use class type parameter (VAR)
				//
				var tparams = ec.CurrentAnonymousMethod != null && ec.CurrentAnonymousMethod.Storey != null ?
					ec.CurrentAnonymousMethod.Storey.CurrentTypeParameters :
					ec.CurrentTypeParameters;

				TypeArguments targs = new TypeArguments ();

				//
				// Use type parameter name instead of resolved type parameter
				// specification to resolve to correctly nested type parameters
				//
				for (int i = 0; i < tparams.Count; ++i)
					targs.Add (new SimpleName (tparams [i].Name, Location)); //  new TypeParameterExpr (tparams[i], Location));

				storey_type_expr = new GenericTypeExpr (Definition, targs, Location);
			} else {
				storey_type_expr = new TypeExpression (CurrentType, Location);
			}

			return storey_type_expr;
		}

		public void SetNestedStoryParent (AnonymousMethodStorey parentStorey)
		{
			Parent = parentStorey;
			spec.IsGeneric = false;
			spec.DeclaringType = parentStorey.CurrentType;
			MemberName.TypeParameters = null;
		}

		protected override bool DoResolveTypeParameters ()
		{
			// Although any storey can have type parameters they are all clones of method type
			// parameters therefore have to mutate MVAR references in any of cloned constraints
			if (CurrentTypeParameters != null) {
				for (int i = 0; i < CurrentTypeParameters.Count; ++i) {
					var spec = CurrentTypeParameters[i].Type;
					spec.BaseType = mutator.Mutate (spec.BaseType);
					if (spec.InterfacesDefined != null) {
						var mutated = new TypeSpec[spec.InterfacesDefined.Length];
						for (int ii = 0; ii < mutated.Length; ++ii) {
							mutated[ii] = mutator.Mutate (spec.InterfacesDefined[ii]);
						}

						spec.InterfacesDefined = mutated;
					}

					if (spec.TypeArguments != null) {
						spec.TypeArguments = mutator.Mutate (spec.TypeArguments);
					}
				}
			}

			//
			// Update parent cache as we most likely passed the point
			// where the cache was constructed
			//
			Parent.CurrentType.MemberCache.AddMember (this.spec);

			return true;
		}

		//
		// Initializes all hoisted variables
		//
		public void EmitStoreyInstantiation (EmitContext ec, ExplicitBlock block)
		{
			// There can be only one instance variable for each storey type
			if (Instance != null)
				throw new InternalErrorException ();

			//
			// Create an instance of this storey
			//
			ResolveContext rc = new ResolveContext (ec.MemberContext);
			rc.CurrentBlock = block;

			var storey_type_expr = CreateStoreyTypeExpression (ec);
			var source = new New (storey_type_expr, null, Location).Resolve (rc);

			//
			// When the current context is async (or iterator) lift local storey
			// instantiation to the currect storey
			//
			if (ec.CurrentAnonymousMethod is StateMachineInitializer && (block.HasYield || block.HasAwait)) {
				//
				// Unfortunately, normal capture mechanism could not be used because we are
				// too late in the pipeline and standart assign cannot be used either due to
				// recursive nature of GetStoreyInstanceExpression
				//
				var field = ec.CurrentAnonymousMethod.Storey.AddCompilerGeneratedField (
					LocalVariable.GetCompilerGeneratedName (block), storey_type_expr, true);

				field.Define ();
				field.Emit ();

				var fexpr = new FieldExpr (field, Location);
				fexpr.InstanceExpression = new CompilerGeneratedThis (ec.CurrentType, Location);
				fexpr.EmitAssign (ec, source, false, false);
				Instance = fexpr;
			} else {
				var local = TemporaryVariableReference.Create (source.Type, block, Location);
				if (source.Type.IsStruct) {
					local.LocalInfo.CreateBuilder (ec);
				} else {
					local.EmitAssign (ec, source);
				}

				Instance = local;
			}

			EmitHoistedFieldsInitialization (rc, ec);

			// TODO: Implement properly
			//SymbolWriter.DefineScopeVariable (ID, Instance.Builder);
		}

		void EmitHoistedFieldsInitialization (ResolveContext rc, EmitContext ec)
		{
			//
			// Initialize all storey reference fields by using local or hoisted variables
			//
			if (used_parent_storeys != null) {
				foreach (StoreyFieldPair sf in used_parent_storeys) {
					//
					// Get instance expression of storey field
					//
					Expression instace_expr = GetStoreyInstanceExpression (ec);
					var fs = sf.Field.Spec;
					if (TypeManager.IsGenericType (instace_expr.Type))
						fs = MemberCache.GetMember (instace_expr.Type, fs);

					FieldExpr f_set_expr = new FieldExpr (fs, Location);
					f_set_expr.InstanceExpression = instace_expr;

					// TODO: CompilerAssign expression
					SimpleAssign a = new SimpleAssign (f_set_expr, sf.Storey.GetStoreyInstanceExpression (ec));
					if (a.Resolve (rc) != null)
						a.EmitStatement (ec);
				}
			}

			//
			// Initialize hoisted `this' only once, everywhere else will be
			// referenced indirectly
			//
			if (initialize_hoisted_this) {
				rc.CurrentBlock.AddScopeStatement (new ThisInitializer (hoisted_this));
			}

			//
			// Setting currect anonymous method to null blocks any further variable hoisting
			//
			AnonymousExpression ae = ec.CurrentAnonymousMethod;
			ec.CurrentAnonymousMethod = null;

			if (hoisted_params != null) {
				EmitHoistedParameters (ec, hoisted_params);
			}

			ec.CurrentAnonymousMethod = ae;
		}

		protected virtual void EmitHoistedParameters (EmitContext ec, List<HoistedParameter> hoisted)
		{
			foreach (HoistedParameter hp in hoisted) {
				//
				// Parameters could be proxied via local fields for value type storey
				//
				if (hoisted_local_params != null) {
					var local_param = hoisted_local_params.Find (l => l.Parameter.Parameter == hp.Parameter.Parameter);
					var source = new FieldExpr (local_param.Field, Location);
					source.InstanceExpression = new CompilerGeneratedThis (CurrentType, Location);
					hp.EmitAssign (ec, source, false, false);
					continue;
				}

				hp.EmitHoistingAssignment (ec);
			}
		}

		//
		// Returns a field which holds referenced storey instance
		//
		Field GetReferencedStoreyField (AnonymousMethodStorey storey)
		{
			if (used_parent_storeys == null)
				return null;

			foreach (StoreyFieldPair sf in used_parent_storeys) {
				if (sf.Storey == storey)
					return sf.Field;
			}

			return null;
		}

		//
		// Creates storey instance expression regardless of currect IP
		//
		public Expression GetStoreyInstanceExpression (EmitContext ec)
		{
			AnonymousExpression am = ec.CurrentAnonymousMethod;

			//
			// Access from original block -> storey
			//
			if (am == null)
				return Instance;

			//
			// Access from anonymous method implemented as a static -> storey
			//
			if (am.Storey == null)
				return Instance;

			Field f = am.Storey.GetReferencedStoreyField (this);
			if (f == null) {
				if (am.Storey == this) {
					//
					// Access from inside of same storey (S -> S)
					//
					return new CompilerGeneratedThis (CurrentType, Location);
				}

				//
				// External field access
				//
				return Instance;
			}

			//
			// Storey was cached to local field
			//
			FieldExpr f_ind = new FieldExpr (f, Location);
			f_ind.InstanceExpression = new CompilerGeneratedThis (CurrentType, Location);
			return f_ind;
		}

		protected virtual string GetVariableMangledName (LocalVariable local_info)
		{
			//
			// No need to mangle anonymous method hoisted variables cause they
			// are hoisted in their own scopes
			//
			return local_info.Name;
		}

		public HoistedThis HoistedThis {
			get {
				return hoisted_this;
			}
			set {
				hoisted_this = value;
			}
		}

		public IList<ExplicitBlock> ReferencesFromChildrenBlock {
			get { return children_references; }
		}
	}

	public abstract class HoistedVariable
	{
		//
		// Hoisted version of variable references used in expression
		// tree has to be delayed until we know its location. The variable
		// doesn't know its location until all stories are calculated
		//
		class ExpressionTreeVariableReference : Expression
		{
			readonly HoistedVariable hv;

			public ExpressionTreeVariableReference (HoistedVariable hv)
			{
				this.hv = hv;
			}

			public override bool ContainsEmitWithAwait ()
			{
				return false;
			}

			public override Expression CreateExpressionTree (ResolveContext ec)
			{
				return hv.CreateExpressionTree ();
			}

			protected override Expression DoResolve (ResolveContext ec)
			{
				eclass = ExprClass.Value;
				type = ec.Module.PredefinedTypes.Expression.Resolve ();
				return this;
			}

			public override void Emit (EmitContext ec)
			{
				ResolveContext rc = new ResolveContext (ec.MemberContext);
				Expression e = hv.GetFieldExpression (ec).CreateExpressionTree (rc, false);
				// This should never fail
				e = e.Resolve (rc);
				if (e != null)
					e.Emit (ec);
			}
		}
	
		protected readonly AnonymousMethodStorey storey;
		protected Field field;
		Dictionary<AnonymousExpression, FieldExpr> cached_inner_access; // TODO: Hashtable is too heavyweight
		FieldExpr cached_outer_access;

		protected HoistedVariable (AnonymousMethodStorey storey, string name, TypeSpec type)
			: this (storey, storey.AddCapturedVariable (name, type))
		{
		}

		protected HoistedVariable (AnonymousMethodStorey storey, Field field)
		{
			this.storey = storey;
			this.field = field;
		}

		public AnonymousMethodStorey Storey {
			get {
				return storey;
			}
		}

		public void AddressOf (EmitContext ec, AddressOp mode)
		{
			GetFieldExpression (ec).AddressOf (ec, mode);
		}

		public Expression CreateExpressionTree ()
		{
			return new ExpressionTreeVariableReference (this);
		}

		public void Emit (EmitContext ec)
		{
			GetFieldExpression (ec).Emit (ec);
		}

		public Expression EmitToField (EmitContext ec)
		{
			return GetFieldExpression (ec);
		}

		//
		// Creates field access expression for hoisted variable
		//
		protected virtual FieldExpr GetFieldExpression (EmitContext ec)
		{
			if (ec.CurrentAnonymousMethod == null || ec.CurrentAnonymousMethod.Storey == null) {
				if (cached_outer_access != null)
					return cached_outer_access;

				//
				// When setting top-level hoisted variable in generic storey
				// change storey generic types to method generic types (VAR -> MVAR)
				//
				if (storey.Instance.Type.IsGenericOrParentIsGeneric) {
					var fs = MemberCache.GetMember (storey.Instance.Type, field.Spec);
					cached_outer_access = new FieldExpr (fs, field.Location);
				} else {
					cached_outer_access = new FieldExpr (field, field.Location);
				}

				cached_outer_access.InstanceExpression = storey.GetStoreyInstanceExpression (ec);
				return cached_outer_access;
			}

			FieldExpr inner_access;
			if (cached_inner_access != null) {
				if (!cached_inner_access.TryGetValue (ec.CurrentAnonymousMethod, out inner_access))
					inner_access = null;
			} else {
				inner_access = null;
				cached_inner_access = new Dictionary<AnonymousExpression, FieldExpr> (4);
			}

			if (inner_access == null) {
				if (field.Parent.IsGenericOrParentIsGeneric) {
					var fs = MemberCache.GetMember (field.Parent.CurrentType, field.Spec);
					inner_access = new FieldExpr (fs, field.Location);
				} else {
					inner_access = new FieldExpr (field, field.Location);
				}

				inner_access.InstanceExpression = storey.GetStoreyInstanceExpression (ec);
				cached_inner_access.Add (ec.CurrentAnonymousMethod, inner_access);
			}

			return inner_access;
		}

		public void Emit (EmitContext ec, bool leave_copy)
		{
			GetFieldExpression (ec).Emit (ec, leave_copy);
		}

		public void EmitAssign (EmitContext ec, Expression source, bool leave_copy, bool isCompound)
		{
			GetFieldExpression (ec).EmitAssign (ec, source, leave_copy, false);
		}
	}

	public class HoistedParameter : HoistedVariable
	{
		sealed class HoistedFieldAssign : CompilerAssign
		{
			public HoistedFieldAssign (Expression target, Expression source)
				: base (target, source, target.Location)
			{
			}

			protected override Expression ResolveConversions (ResolveContext ec)
			{
				//
				// Implicit conversion check fails for hoisted type arguments
				// as they are of different types (!!0 x !0)
				//
				return this;
			}
		}

		readonly ParameterReference parameter;

		public HoistedParameter (AnonymousMethodStorey scope, ParameterReference par)
			: base (scope, par.Name, par.Type)
		{
			this.parameter = par;
		}

		public HoistedParameter (HoistedParameter hp, string name)
			: base (hp.storey, name, hp.parameter.Type)
		{
			this.parameter = hp.parameter;
		}

		#region Properties

		public Field Field {
			get {
				return field;
			}
		}

		public ParameterReference Parameter {
			get {
				return parameter;
			}
		}

		#endregion

		public void EmitHoistingAssignment (EmitContext ec)
		{
			//
			// Remove hoisted redirection to emit assignment from original parameter
			//
			var temp = parameter.Parameter.HoistedVariant;
			parameter.Parameter.HoistedVariant = null;

			var a = new HoistedFieldAssign (GetFieldExpression (ec), parameter);
			a.EmitStatement (ec);

			parameter.Parameter.HoistedVariant = temp;
		}
	}

	class HoistedLocalVariable : HoistedVariable
	{
		public HoistedLocalVariable (AnonymousMethodStorey storey, LocalVariable local, string name)
			: base (storey, name, local.Type)
		{
		}
	}

	public class HoistedThis : HoistedVariable
	{
		public HoistedThis (AnonymousMethodStorey storey, Field field)
			: base (storey, field)
		{
		}

		public Field Field {
			get {
				return field;
			}
		}
	}

	//
	// Anonymous method expression as created by parser
	//
	public class AnonymousMethodExpression : Expression
	{
		//
		// Special conversion for nested expression tree lambdas
		//
		class Quote : ShimExpression
		{
			public Quote (Expression expr)
				: base (expr)
			{
			}

			public override Expression CreateExpressionTree (ResolveContext ec)
			{
				var args = new Arguments (1);
				args.Add (new Argument (expr.CreateExpressionTree (ec)));
				return CreateExpressionFactoryCall (ec, "Quote", args);
			}

			protected override Expression DoResolve (ResolveContext rc)
			{
				expr = expr.Resolve (rc);
				if (expr == null)
					return null;

				eclass = expr.eclass;
				type = expr.Type;
				return this;
			}
		}

		readonly Dictionary<TypeSpec, Expression> compatibles;

		public ParametersBlock Block;

		public AnonymousMethodExpression (Location loc)
		{
			this.loc = loc;
			this.compatibles = new Dictionary<TypeSpec, Expression> ();
		}

		#region Properties

		public override string ExprClassName {
			get {
				return "anonymous method";
			}
		}

		public virtual bool HasExplicitParameters {
			get {
				return Parameters != ParametersCompiled.Undefined;
			}
		}

		public ParametersCompiled Parameters {
			get {
				return Block.Parameters;
			}
		}

		public bool IsAsync {
			get;
			internal set;
		}

		public ReportPrinter TypeInferenceReportPrinter {
			get; set;
		}

		#endregion

		//
		// Returns true if the body of lambda expression can be implicitly
		// converted to the delegate of type `delegate_type'
		//
		public bool ImplicitStandardConversionExists (ResolveContext ec, TypeSpec delegate_type)
		{
			using (ec.With (ResolveContext.Options.InferReturnType, false)) {
				using (ec.Set (ResolveContext.Options.ProbingMode)) {
					var prev = ec.Report.SetPrinter (TypeInferenceReportPrinter ?? new NullReportPrinter ());

					var res = Compatible (ec, delegate_type) != null;

					ec.Report.SetPrinter (prev);

					return res;
				}
			}
		}

		TypeSpec CompatibleChecks (ResolveContext ec, TypeSpec delegate_type)
		{
			if (delegate_type.IsDelegate)
				return delegate_type;

			if (delegate_type.IsExpressionTreeType) {
				delegate_type = delegate_type.TypeArguments [0];
				if (delegate_type.IsDelegate)
					return delegate_type;

				ec.Report.Error (835, loc, "Cannot convert `{0}' to an expression tree of non-delegate type `{1}'",
					GetSignatureForError (), TypeManager.CSharpName (delegate_type));
				return null;
			}

			ec.Report.Error (1660, loc, "Cannot convert `{0}' to non-delegate type `{1}'",
				      GetSignatureForError (), TypeManager.CSharpName (delegate_type));
			return null;
		}

		protected bool VerifyExplicitParameters (ResolveContext ec, TypeSpec delegate_type, AParametersCollection parameters)
		{
			if (VerifyParameterCompatibility (ec, delegate_type, parameters, ec.IsInProbingMode))
				return true;

			if (!ec.IsInProbingMode)
				ec.Report.Error (1661, loc,
					"Cannot convert `{0}' to delegate type `{1}' since there is a parameter mismatch",
					GetSignatureForError (), TypeManager.CSharpName (delegate_type));

			return false;
		}

		protected bool VerifyParameterCompatibility (ResolveContext ec, TypeSpec delegate_type, AParametersCollection invoke_pd, bool ignore_errors)
		{
			if (Parameters.Count != invoke_pd.Count) {
				if (ignore_errors)
					return false;
				
				ec.Report.Error (1593, loc, "Delegate `{0}' does not take `{1}' arguments",
					      TypeManager.CSharpName (delegate_type), Parameters.Count.ToString ());
				return false;
			}

			bool has_implicit_parameters = !HasExplicitParameters;
			bool error = false;

			for (int i = 0; i < Parameters.Count; ++i) {
				Parameter.Modifier p_mod = invoke_pd.FixedParameters [i].ModFlags;
				if (Parameters.FixedParameters [i].ModFlags != p_mod && p_mod != Parameter.Modifier.PARAMS) {
					if (ignore_errors)
						return false;
					
					if (p_mod == Parameter.Modifier.NONE)
						ec.Report.Error (1677, loc, "Parameter `{0}' should not be declared with the `{1}' keyword",
							      (i + 1).ToString (), Parameter.GetModifierSignature (Parameters.FixedParameters [i].ModFlags));
					else
						ec.Report.Error (1676, loc, "Parameter `{0}' must be declared with the `{1}' keyword",
							      (i+1).ToString (), Parameter.GetModifierSignature (p_mod));
					error = true;
				}

				if (has_implicit_parameters)
					continue;

				TypeSpec type = invoke_pd.Types [i];
				
				// We assume that generic parameters are always inflated
				if (TypeManager.IsGenericParameter (type))
					continue;
				
				if (TypeManager.HasElementType (type) && TypeManager.IsGenericParameter (TypeManager.GetElementType (type)))
					continue;
				
				if (!TypeSpecComparer.IsEqual (invoke_pd.Types [i], Parameters.Types [i])) {
					if (ignore_errors)
						return false;
					
					ec.Report.Error (1678, loc, "Parameter `{0}' is declared as type `{1}' but should be `{2}'",
						      (i+1).ToString (),
						      TypeManager.CSharpName (Parameters.Types [i]),
						      TypeManager.CSharpName (invoke_pd.Types [i]));
					error = true;
				}
			}

			return !error;
		}

		//
		// Infers type arguments based on explicit arguments
		//
		public bool ExplicitTypeInference (ResolveContext ec, TypeInferenceContext type_inference, TypeSpec delegate_type)
		{
			if (!HasExplicitParameters)
				return false;

			if (!delegate_type.IsDelegate) {
				if (!delegate_type.IsExpressionTreeType)
					return false;

				delegate_type = TypeManager.GetTypeArguments (delegate_type) [0];
				if (!delegate_type.IsDelegate)
					return false;
			}
			
			AParametersCollection d_params = Delegate.GetParameters (delegate_type);
			if (d_params.Count != Parameters.Count)
				return false;

			for (int i = 0; i < Parameters.Count; ++i) {
				if (type_inference.ExactInference (Parameters.Types[i], d_params.Types[i]) == 0)
					return false;
			}

			return true;
		}

		public TypeSpec InferReturnType (ResolveContext ec, TypeInferenceContext tic, TypeSpec delegate_type)
		{
			Expression expr;
			AnonymousExpression am;

			if (compatibles.TryGetValue (delegate_type, out expr)) {
				am = expr as AnonymousExpression;
				return am == null ? null : am.ReturnType;
			}

			using (ec.Set (ResolveContext.Options.ProbingMode | ResolveContext.Options.InferReturnType)) {
				ReportPrinter prev;
				if (TypeInferenceReportPrinter != null) {
					prev = ec.Report.SetPrinter (TypeInferenceReportPrinter);
				} else {
					prev = null;
				}

				var body = CompatibleMethodBody (ec, tic, null, delegate_type);
				if (body != null) {
					am = body.Compatible (ec, body);
				} else {
					am = null;
				}

				if (TypeInferenceReportPrinter != null) {
					ec.Report.SetPrinter (prev);
				}
			}

			if (am == null)
				return null;

//			compatibles.Add (delegate_type, am);
			return am.ReturnType;
		}

		public override bool ContainsEmitWithAwait ()
		{
			return false;
		}

		//
		// Returns AnonymousMethod container if this anonymous method
		// expression can be implicitly converted to the delegate type `delegate_type'
		//
		public Expression Compatible (ResolveContext ec, TypeSpec type)
		{
			Expression am;
			if (compatibles.TryGetValue (type, out am))
				return am;

			TypeSpec delegate_type = CompatibleChecks (ec, type);
			if (delegate_type == null)
				return null;

			//
			// At this point its the first time we know the return type that is 
			// needed for the anonymous method.  We create the method here.
			//

			var invoke_mb = Delegate.GetInvokeMethod (delegate_type);
			TypeSpec return_type = invoke_mb.ReturnType;

			//
			// Second: the return type of the delegate must be compatible with 
			// the anonymous type.   Instead of doing a pass to examine the block
			// we satisfy the rule by setting the return type on the EmitContext
			// to be the delegate type return type.
			//

			var body = CompatibleMethodBody (ec, null, return_type, delegate_type);
			if (body == null)
				return null;

			bool etree_conversion = delegate_type != type;

			try {
				if (etree_conversion) {
					if (ec.HasSet (ResolveContext.Options.ExpressionTreeConversion)) {
						//
						// Nested expression tree lambda use same scope as parent
						// lambda, this also means no variable capturing between this
						// and parent scope
						//
						am = body.Compatible (ec, ec.CurrentAnonymousMethod);

						//
						// Quote nested expression tree
						//
						if (am != null)
							am = new Quote (am);
					} else {
						int errors = ec.Report.Errors;

						if (Block.IsAsync) {
							ec.Report.Error (1989, loc, "Async lambda expressions cannot be converted to expression trees");
						}

						using (ec.Set (ResolveContext.Options.ExpressionTreeConversion)) {
							am = body.Compatible (ec);
						}

						//
						// Rewrite expressions into expression tree when targeting Expression<T>
						//
						if (am != null && errors == ec.Report.Errors)
							am = CreateExpressionTree (ec, delegate_type);
					}
				} else {
					am = body.Compatible (ec);
				}
			} catch (CompletionResult) {
				throw;
			} catch (FatalException) {
				throw;
			} catch (Exception e) {
				throw new InternalErrorException (e, loc);
			}

			if (!ec.IsInProbingMode) {
				compatibles.Add (type, am ?? EmptyExpression.Null);
			}

			return am;
		}

		protected virtual Expression CreateExpressionTree (ResolveContext ec, TypeSpec delegate_type)
		{
			return CreateExpressionTree (ec);
		}

		public override Expression CreateExpressionTree (ResolveContext ec)
		{
			ec.Report.Error (1946, loc, "An anonymous method cannot be converted to an expression tree");
			return null;
		}

		protected virtual ParametersCompiled ResolveParameters (ResolveContext ec, TypeInferenceContext tic, TypeSpec delegate_type)
		{
			var delegate_parameters = Delegate.GetParameters (delegate_type);

			if (Parameters == ParametersCompiled.Undefined) {
				//
				// We provide a set of inaccessible parameters
				//
				Parameter[] fixedpars = new Parameter[delegate_parameters.Count];

				for (int i = 0; i < delegate_parameters.Count; i++) {
					Parameter.Modifier i_mod = delegate_parameters.FixedParameters [i].ModFlags;
					if ((i_mod & Parameter.Modifier.OUT) != 0) {
						if (!ec.IsInProbingMode) {
							ec.Report.Error (1688, loc,
								"Cannot convert anonymous method block without a parameter list to delegate type `{0}' because it has one or more `out' parameters",
								delegate_type.GetSignatureForError ());
						}

						return null;
					}
					fixedpars[i] = new Parameter (
						new TypeExpression (delegate_parameters.Types [i], loc), null,
						delegate_parameters.FixedParameters [i].ModFlags, null, loc);
				}

				return ParametersCompiled.CreateFullyResolved (fixedpars, delegate_parameters.Types);
			}

			if (!VerifyExplicitParameters (ec, delegate_type, delegate_parameters)) {
				return null;
			}

			return Parameters;
		}

		protected override Expression DoResolve (ResolveContext ec)
		{
			if (ec.HasSet (ResolveContext.Options.ConstantScope)) {
				ec.Report.Error (1706, loc, "Anonymous methods and lambda expressions cannot be used in the current context");
				return null;
			}

			//
			// Set class type, set type
			//

			eclass = ExprClass.Value;

			//
			// This hack means `The type is not accessible
			// anywhere', we depend on special conversion
			// rules.
			// 
			type = InternalType.AnonymousMethod;

			if (!DoResolveParameters (ec))
				return null;

#if !STATIC
			// FIXME: The emitted code isn't very careful about reachability
			// so, ensure we have a 'ret' at the end
			BlockContext bc = ec as BlockContext;
			if (bc != null && bc.CurrentBranching != null && bc.CurrentBranching.CurrentUsageVector.IsUnreachable)
				bc.NeedReturnLabel ();
#endif
			return this;
		}

		protected virtual bool DoResolveParameters (ResolveContext rc)
		{
			return Parameters.Resolve (rc);
		}

		public override void Emit (EmitContext ec)
		{
			// nothing, as we only exist to not do anything.
		}

		public static void Error_AddressOfCapturedVar (ResolveContext ec, IVariableReference var, Location loc)
		{
			ec.Report.Error (1686, loc,
				"Local variable or parameter `{0}' cannot have their address taken and be used inside an anonymous method, lambda expression or query expression",
				var.Name);
		}

		public override string GetSignatureForError ()
		{
			return ExprClassName;
		}

		AnonymousMethodBody CompatibleMethodBody (ResolveContext ec, TypeInferenceContext tic, TypeSpec return_type, TypeSpec delegate_type)
		{
			ParametersCompiled p = ResolveParameters (ec, tic, delegate_type);
			if (p == null)
				return null;

			ParametersBlock b = ec.IsInProbingMode ? (ParametersBlock) Block.PerformClone () : Block;

			if (b.IsAsync) {
				var rt = return_type;
				if (rt != null && rt.Kind != MemberKind.Void && rt != ec.Module.PredefinedTypes.Task.TypeSpec && !rt.IsGenericTask) {
					ec.Report.Error (4010, loc, "Cannot convert async {0} to delegate type `{1}'",
						GetSignatureForError (), delegate_type.GetSignatureForError ());

					return null;
				}

				b = b.ConvertToAsyncTask (ec, ec.CurrentMemberDefinition.Parent.PartialContainer, p, return_type, loc);
			}

			return CompatibleMethodFactory (return_type ?? InternalType.Arglist, delegate_type, p, b);
		}

		protected virtual AnonymousMethodBody CompatibleMethodFactory (TypeSpec return_type, TypeSpec delegate_type, ParametersCompiled p, ParametersBlock b)
		{
			return new AnonymousMethodBody (p, b, return_type, delegate_type, loc);
		}

		protected override void CloneTo (CloneContext clonectx, Expression t)
		{
			AnonymousMethodExpression target = (AnonymousMethodExpression) t;

			target.Block = (ParametersBlock) clonectx.LookupBlock (Block);
		}
		
		public override object Accept (StructuralVisitor visitor)
		{
			return visitor.Visit (this);
		}
	}

	//
	// Abstract expression for any block which requires variables hoisting
	//
	public abstract class AnonymousExpression : ExpressionStatement
	{
		protected class AnonymousMethodMethod : Method
		{
			public readonly AnonymousExpression AnonymousMethod;
			public readonly AnonymousMethodStorey Storey;

			public AnonymousMethodMethod (TypeDefinition parent, AnonymousExpression am, AnonymousMethodStorey storey,
							  TypeExpr return_type,
							  Modifiers mod, MemberName name,
							  ParametersCompiled parameters)
				: base (parent, return_type, mod | Modifiers.COMPILER_GENERATED,
						name, parameters, null)
			{
				this.AnonymousMethod = am;
				this.Storey = storey;

				Parent.PartialContainer.Members.Add (this);
				Block = new ToplevelBlock (am.block, parameters);
			}

			public override EmitContext CreateEmitContext (ILGenerator ig, SourceMethodBuilder sourceMethod)
			{
				EmitContext ec = new EmitContext (this, ig, ReturnType, sourceMethod);
				ec.CurrentAnonymousMethod = AnonymousMethod;
				return ec;
			}

			protected override void DefineTypeParameters ()
			{
				// Type parameters were cloned
			}

			protected override bool ResolveMemberType ()
			{
				if (!base.ResolveMemberType ())
					return false;

				if (Storey != null && Storey.Mutator != null) {
					if (!parameters.IsEmpty) {
						var mutated = Storey.Mutator.Mutate (parameters.Types);
						if (mutated != parameters.Types)
							parameters = ParametersCompiled.CreateFullyResolved ((Parameter[]) parameters.FixedParameters, mutated);
					}

					member_type = Storey.Mutator.Mutate (member_type);
				}

				return true;
			}

			public override void Emit ()
			{
				if (MethodBuilder == null) {
					Define ();
				}

				base.Emit ();
			}
		}

		protected ParametersBlock block;

		public TypeSpec ReturnType;

		protected AnonymousExpression (ParametersBlock block, TypeSpec return_type, Location loc)
		{
			this.ReturnType = return_type;
			this.block = block;
			this.loc = loc;
		}

		public abstract string ContainerType { get; }
		public abstract bool IsIterator { get; }
		public abstract AnonymousMethodStorey Storey { get; }

		//
		// The block that makes up the body for the anonymous method
		//
		public ParametersBlock Block {
			get {
				return block;
			}
		}

		public AnonymousExpression Compatible (ResolveContext ec)
		{
			return Compatible (ec, this);
		}

		public AnonymousExpression Compatible (ResolveContext ec, AnonymousExpression ae)
		{
			if (block.Resolved)
				return this;

			// TODO: Implement clone
			BlockContext aec = new BlockContext (ec, block, ReturnType);
			aec.CurrentAnonymousMethod = ae;

			ResolveContext.Options flags = 0;

			var am = this as AnonymousMethodBody;

			if (ec.HasSet (ResolveContext.Options.InferReturnType) && am != null) {
				am.ReturnTypeInference = new TypeInferenceContext ();
			}

			if (ec.IsInProbingMode)
				flags |= ResolveContext.Options.ProbingMode;

			if (ec.HasSet (ResolveContext.Options.FieldInitializerScope))
				flags |= ResolveContext.Options.FieldInitializerScope;

			if (ec.HasSet (ResolveContext.Options.ExpressionTreeConversion))
				flags |= ResolveContext.Options.ExpressionTreeConversion;

			if (ec.HasSet (ResolveContext.Options.BaseInitializer))
				flags |= ResolveContext.Options.BaseInitializer;

			aec.Set (flags);

			var bc = ec as BlockContext;
			if (bc != null)
				aec.FlowOffset = bc.FlowOffset;

			var errors = ec.Report.Errors;

			bool res = Block.Resolve (ec.CurrentBranching, aec, null);

			if (am != null && am.ReturnTypeInference != null) {
				am.ReturnTypeInference.FixAllTypes (ec);
				ReturnType = am.ReturnTypeInference.InferredTypeArguments [0];
				am.ReturnTypeInference = null;

				//
				// If e is synchronous the inferred return type is T
				// If e is asynchronous the inferred return type is Task<T>
				//
				if (block.IsAsync && ReturnType != null) {
					ReturnType = ec.Module.PredefinedTypes.TaskGeneric.TypeSpec.MakeGenericType (ec, new [] { ReturnType });
				}
			}

			if (res && errors != ec.Report.Errors)
				return null;

			return res ? this : null;
		}

		public override bool ContainsEmitWithAwait ()
		{
			return false;
		}

		public void SetHasThisAccess ()
		{
			ExplicitBlock b = block;
			do {
				if (b.HasCapturedThis)
					return;

				b.HasCapturedThis = true;
				b = b.Parent == null ? null : b.Parent.Explicit;
			} while (b != null);
		}
	}

	public class AnonymousMethodBody : AnonymousExpression
	{
		protected readonly ParametersCompiled parameters;
		AnonymousMethodStorey storey;

		AnonymousMethodMethod method;
		Field am_cache;
		string block_name;
		TypeInferenceContext return_inference;

		public AnonymousMethodBody (ParametersCompiled parameters,
					ParametersBlock block, TypeSpec return_type, TypeSpec delegate_type,
					Location loc)
			: base (block, return_type, loc)
		{
			this.type = delegate_type;
			this.parameters = parameters;
		}

		#region Properties

		public override string ContainerType {
			get { return "anonymous method"; }
		}

		public override bool IsIterator {
			get {
				return false;
			}
		}

		public ParametersCompiled Parameters {
			get {
				return parameters;
			}
		}

		public TypeInferenceContext ReturnTypeInference {
			get {
				return return_inference;
			}
			set {
				return_inference = value;
			}
		}

		public override AnonymousMethodStorey Storey {
			get { return storey; }
		}

		#endregion

		public override Expression CreateExpressionTree (ResolveContext ec)
		{
			ec.Report.Error (1945, loc, "An expression tree cannot contain an anonymous method expression");
			return null;
		}

		bool Define (ResolveContext ec)
		{
			if (!Block.Resolved && Compatible (ec) == null)
				return false;

			if (block_name == null) {
				MemberCore mc = (MemberCore) ec.MemberContext;
				block_name = mc.MemberName.Basename;
			}

			return true;
		}

		//
		// Creates a host for the anonymous method
		//
		AnonymousMethodMethod DoCreateMethodHost (EmitContext ec)
		{
			//
			// Anonymous method body can be converted to
			//
			// 1, an instance method in current scope when only `this' is hoisted
			// 2, a static method in current scope when neither `this' nor any variable is hoisted
			// 3, an instance method in compiler generated storey when any hoisted variable exists
			//

			Modifiers modifiers;
			TypeDefinition parent = null;

			var src_block = Block.Original.Explicit;
			if (src_block.HasCapturedVariable || src_block.HasCapturedThis) {
				parent = storey = FindBestMethodStorey ();

				if (storey == null) {
					var sm = src_block.ParametersBlock.TopBlock.StateMachine;

					//
					// Remove hoisted this demand when simple instance method is enough
					//
					if (src_block.HasCapturedThis) {
						src_block.ParametersBlock.TopBlock.RemoveThisReferenceFromChildrenBlock (src_block);

						//
						// Special case where parent class is used to emit instance method
						// because currect storey is of value type (async host) and we don't
						// want to create another childer storey to host this reference only
						//
						if (sm != null && sm.Kind == MemberKind.Struct)
							parent = sm.Parent.PartialContainer;
					}

					//
					// For iterators we can host everything in one class
					//
					if (sm is IteratorStorey)
						parent = storey = sm;
				}

				modifiers = storey != null ? Modifiers.INTERNAL : Modifiers.PRIVATE;
			} else {
				if (ec.CurrentAnonymousMethod != null)
					parent = storey = ec.CurrentAnonymousMethod.Storey;

				modifiers = Modifiers.STATIC | Modifiers.PRIVATE;
			}

			if (parent == null)
				parent = ec.CurrentTypeDefinition.Parent.PartialContainer;

			string name = CompilerGeneratedContainer.MakeName (parent != storey ? block_name : null,
				"m", null, ec.Module.CounterAnonymousMethods++);

			MemberName member_name;
			if (storey == null && ec.CurrentTypeParameters != null) {

				var hoisted_tparams = ec.CurrentTypeParameters;
				var type_params = new TypeParameters (hoisted_tparams.Count);
				for (int i = 0; i < hoisted_tparams.Count; ++i) {
				    type_params.Add (hoisted_tparams[i].CreateHoistedCopy (null));
				}

				member_name = new MemberName (name, type_params, Location);
			} else {
				member_name = new MemberName (name, Location);
			}

			return new AnonymousMethodMethod (parent,
				this, storey, new TypeExpression (ReturnType, Location), modifiers,
				member_name, parameters);
		}

		protected override Expression DoResolve (ResolveContext ec)
		{
			if (!Define (ec))
				return null;

			eclass = ExprClass.Value;
			return this;
		}

		public override void Emit (EmitContext ec)
		{
			//
			// Use same anonymous method implementation for scenarios where same
			// code is used from multiple blocks, e.g. field initializers
			//
			if (method == null) {
				//
				// Delay an anonymous method definition to avoid emitting unused code
				// for unreachable blocks or expression trees
				//
				method = DoCreateMethodHost (ec);
				method.Define ();
			}

			bool is_static = (method.ModFlags & Modifiers.STATIC) != 0;
			if (is_static && am_cache == null) {
				//
				// Creates a field cache to store delegate instance if it's not generic
				//
				if (!method.MemberName.IsGeneric) {
					var parent = method.Parent.PartialContainer;
					int id = parent.AnonymousMethodsCounter++;
					var cache_type = storey != null && storey.Mutator != null ? storey.Mutator.Mutate (type) : type;

					am_cache = new Field (parent, new TypeExpression (cache_type, loc),
						Modifiers.STATIC | Modifiers.PRIVATE | Modifiers.COMPILER_GENERATED,
						new MemberName (CompilerGeneratedContainer.MakeName (null, "f", "am$cache", id), loc), null);
					am_cache.Define ();
					parent.AddField (am_cache);
				} else {
					// TODO: Implement caching of generated generic static methods
					//
					// Idea:
					//
					// Some extra class is needed to capture variable generic type
					// arguments. Maybe we could re-use anonymous types, with a unique
					// anonymous method id, but they are quite heavy.
					//
					// Consider : "() => typeof(T);"
					//
					// We need something like
					// static class Wrap<Tn, Tm, DelegateType> {
					//		public static DelegateType cache;
					// }
					//
					// We then specialize local variable to capture all generic parameters
					// and delegate type, e.g. "Wrap<Ta, Tb, DelegateTypeInst> cache;"
					//
				}
			}

			Label l_initialized = ec.DefineLabel ();

			if (am_cache != null) {
				ec.Emit (OpCodes.Ldsfld, am_cache.Spec);
				ec.Emit (OpCodes.Brtrue_S, l_initialized);
			}

			//
			// Load method delegate implementation
			//

			if (is_static) {
				ec.EmitNull ();
			} else if (storey != null) {
				Expression e = storey.GetStoreyInstanceExpression (ec).Resolve (new ResolveContext (ec.MemberContext));
				if (e != null) {
					e.Emit (ec);
				}
			} else {
				ec.EmitThis ();

				//
				// Special case for value type storey where this is not lifted but
				// droped off to parent class
				//
				for (var b = Block.Parent; b != null; b = b.Parent) {
					if (b.ParametersBlock.StateMachine != null) {
						ec.Emit (OpCodes.Ldfld, b.ParametersBlock.StateMachine.HoistedThis.Field.Spec);
						break;
					}
				}
			}

			var delegate_method = method.Spec;
			if (storey != null && storey.MemberName.IsGeneric) {
				TypeSpec t = storey.Instance.Type;

				//
				// Mutate anonymous method instance type if we are in nested
				// hoisted generic anonymous method storey
				//
				if (ec.IsAnonymousStoreyMutateRequired) {
					t = storey.Mutator.Mutate (t);
				}

				ec.Emit (OpCodes.Ldftn, TypeBuilder.GetMethod (t.GetMetaInfo (), (MethodInfo) delegate_method.GetMetaInfo ()));
			} else {
				if (delegate_method.IsGeneric)
					delegate_method = delegate_method.MakeGenericMethod (ec.MemberContext, method.TypeParameters);

				ec.Emit (OpCodes.Ldftn, delegate_method);
			}

			var constructor_method = Delegate.GetConstructor (type);
			ec.Emit (OpCodes.Newobj, constructor_method);

			if (am_cache != null) {
				ec.Emit (OpCodes.Stsfld, am_cache.Spec);
				ec.MarkLabel (l_initialized);
				ec.Emit (OpCodes.Ldsfld, am_cache.Spec);
			}
		}

		public override void EmitStatement (EmitContext ec)
		{
			throw new NotImplementedException ();
		}

		//
		// Look for the best storey for this anonymous method
		//
		AnonymousMethodStorey FindBestMethodStorey ()
		{
			//
			// Use the nearest parent block which has a storey
			//
			for (Block b = Block.Parent; b != null; b = b.Parent) {
				AnonymousMethodStorey s = b.Explicit.AnonymousMethodStorey;
				if (s != null)
					return s;
			}
					
			return null;
		}

		public override string GetSignatureForError ()
		{
			return TypeManager.CSharpName (type);
		}
	}

	//
	// Anonymous type container
	//
	public class AnonymousTypeClass : CompilerGeneratedContainer
	{
		public const string ClassNamePrefix = "<>__AnonType";
		public const string SignatureForError = "anonymous type";
		
		readonly IList<AnonymousTypeParameter> parameters;

		private AnonymousTypeClass (ModuleContainer parent, MemberName name, IList<AnonymousTypeParameter> parameters, Location loc)
			: base (parent, name, parent.Evaluator != null ? Modifiers.PUBLIC : Modifiers.INTERNAL)
		{
			this.parameters = parameters;
		}

		public static AnonymousTypeClass Create (TypeContainer parent, IList<AnonymousTypeParameter> parameters, Location loc)
		{
			string name = ClassNamePrefix + parent.Module.CounterAnonymousTypes++;

			ParametersCompiled all_parameters;
			TypeParameters tparams = null;
			SimpleName[] t_args;

			if (parameters.Count == 0) {
				all_parameters = ParametersCompiled.EmptyReadOnlyParameters;
				t_args = null;
			} else {
				t_args = new SimpleName[parameters.Count];
				tparams = new TypeParameters ();
				Parameter[] ctor_params = new Parameter[parameters.Count];
				for (int i = 0; i < parameters.Count; ++i) {
					AnonymousTypeParameter p = parameters[i];
					for (int ii = 0; ii < i; ++ii) {
						if (parameters[ii].Name == p.Name) {
							parent.Compiler.Report.Error (833, parameters[ii].Location,
								"`{0}': An anonymous type cannot have multiple properties with the same name",
									p.Name);

							p = new AnonymousTypeParameter (null, "$" + i.ToString (), p.Location);
							parameters[i] = p;
							break;
						}
					}

					t_args[i] = new SimpleName ("<" + p.Name + ">__T", p.Location);
					tparams.Add (new TypeParameter (i, new MemberName (t_args[i].Name, p.Location), null, null, Variance.None));
					ctor_params[i] = new Parameter (t_args[i], p.Name, Parameter.Modifier.NONE, null, p.Location);
				}

				all_parameters = new ParametersCompiled (ctor_params);
			}

			//
			// Create generic anonymous type host with generic arguments
			// named upon properties names
			//
			AnonymousTypeClass a_type = new AnonymousTypeClass (parent.Module, new MemberName (name, tparams, loc), parameters, loc);

			Constructor c = new Constructor (a_type, name, Modifiers.PUBLIC | Modifiers.DEBUGGER_HIDDEN,
				null, all_parameters, loc);
			c.Block = new ToplevelBlock (parent.Module.Compiler, c.ParameterInfo, loc);

			// 
			// Create fields and constructor body with field initialization
			//
			bool error = false;
			for (int i = 0; i < parameters.Count; ++i) {
				AnonymousTypeParameter p = parameters [i];

				Field f = new Field (a_type, t_args [i], Modifiers.PRIVATE | Modifiers.READONLY | Modifiers.DEBUGGER_HIDDEN,
					new MemberName ("<" + p.Name + ">", p.Location), null);

				if (!a_type.AddField (f)) {
					error = true;
					continue;
				}

				c.Block.AddStatement (new StatementExpression (
					new SimpleAssign (new MemberAccess (new This (p.Location), f.Name),
						c.Block.GetParameterReference (i, p.Location))));

				ToplevelBlock get_block = new ToplevelBlock (parent.Module.Compiler, p.Location);
				get_block.AddStatement (new Return (
					new MemberAccess (new This (p.Location), f.Name), p.Location));

				Property prop = new Property (a_type, t_args [i], Modifiers.PUBLIC,
					new MemberName (p.Name, p.Location), null);
				prop.Get = new Property.GetMethod (prop, 0, null, p.Location);
				prop.Get.Block = get_block;
				a_type.AddMember (prop);
			}

			if (error)
				return null;

			a_type.AddConstructor (c);
			return a_type;
		}
		
		protected override bool DoDefineMembers ()
		{
			if (!base.DoDefineMembers ())
				return false;

			Location loc = Location;

			var equals_parameters = ParametersCompiled.CreateFullyResolved (
				new Parameter (new TypeExpression (Compiler.BuiltinTypes.Object, loc), "obj", 0, null, loc), Compiler.BuiltinTypes.Object);

			Method equals = new Method (this, new TypeExpression (Compiler.BuiltinTypes.Bool, loc),
				Modifiers.PUBLIC | Modifiers.OVERRIDE | Modifiers.DEBUGGER_HIDDEN, new MemberName ("Equals", loc),
				equals_parameters, null);

			equals_parameters[0].Resolve (equals, 0);

			Method tostring = new Method (this, new TypeExpression (Compiler.BuiltinTypes.String, loc),
				Modifiers.PUBLIC | Modifiers.OVERRIDE | Modifiers.DEBUGGER_HIDDEN, new MemberName ("ToString", loc),
				Mono.CSharp.ParametersCompiled.EmptyReadOnlyParameters, null);

			ToplevelBlock equals_block = new ToplevelBlock (Compiler, equals.ParameterInfo, loc);

			TypeExpr current_type;
			if (CurrentTypeParameters != null) {
				var targs = new TypeArguments ();
				for (int i = 0; i < CurrentTypeParameters.Count; ++i) {
					targs.Add (new TypeParameterExpr (CurrentTypeParameters[i], Location));
				}

				current_type = new GenericTypeExpr (Definition, targs, loc);
			} else {
				current_type = new TypeExpression (Definition, loc);
			}

			var li_other = LocalVariable.CreateCompilerGenerated (CurrentType, equals_block, loc);
			equals_block.AddStatement (new BlockVariableDeclaration (new TypeExpression (li_other.Type, loc), li_other));
			var other_variable = new LocalVariableReference (li_other, loc);

			MemberAccess system_collections_generic = new MemberAccess (new MemberAccess (
				new QualifiedAliasMember ("global", "System", loc), "Collections", loc), "Generic", loc);

			Expression rs_equals = null;
			Expression string_concat = new StringConstant (Compiler.BuiltinTypes, "{", loc);
			Expression rs_hashcode = new IntConstant (Compiler.BuiltinTypes, -2128831035, loc);
			for (int i = 0; i < parameters.Count; ++i) {
				var p = parameters [i];
				var f = (Field) Members [i * 2];

				MemberAccess equality_comparer = new MemberAccess (new MemberAccess (
					system_collections_generic, "EqualityComparer",
						new TypeArguments (new SimpleName (CurrentTypeParameters [i].Name, loc)), loc),
						"Default", loc);

				Arguments arguments_equal = new Arguments (2);
				arguments_equal.Add (new Argument (new MemberAccess (new This (f.Location), f.Name)));
				arguments_equal.Add (new Argument (new MemberAccess (other_variable, f.Name)));

				Expression field_equal = new Invocation (new MemberAccess (equality_comparer,
					"Equals", loc), arguments_equal);

				Arguments arguments_hashcode = new Arguments (1);
				arguments_hashcode.Add (new Argument (new MemberAccess (new This (f.Location), f.Name)));
				Expression field_hashcode = new Invocation (new MemberAccess (equality_comparer,
					"GetHashCode", loc), arguments_hashcode);

				IntConstant FNV_prime = new IntConstant (Compiler.BuiltinTypes, 16777619, loc);				
				rs_hashcode = new Binary (Binary.Operator.Multiply,
					new Binary (Binary.Operator.ExclusiveOr, rs_hashcode, field_hashcode),
					FNV_prime);

				Expression field_to_string = new Conditional (new BooleanExpression (new Binary (Binary.Operator.Inequality,
					new MemberAccess (new This (f.Location), f.Name), new NullLiteral (loc))),
					new Invocation (new MemberAccess (
						new MemberAccess (new This (f.Location), f.Name), "ToString"), null),
					new StringConstant (Compiler.BuiltinTypes, string.Empty, loc), loc);

				if (rs_equals == null) {
					rs_equals = field_equal;
					string_concat = new Binary (Binary.Operator.Addition,
						string_concat,
						new Binary (Binary.Operator.Addition,
							new StringConstant (Compiler.BuiltinTypes, " " + p.Name + " = ", loc),
							field_to_string));
					continue;
				}

				//
				// Implementation of ToString () body using string concatenation
				//				
				string_concat = new Binary (Binary.Operator.Addition,
					new Binary (Binary.Operator.Addition,
						string_concat,
						new StringConstant (Compiler.BuiltinTypes, ", " + p.Name + " = ", loc)),
					field_to_string);

				rs_equals = new Binary (Binary.Operator.LogicalAnd, rs_equals, field_equal);
			}

			string_concat = new Binary (Binary.Operator.Addition,
				string_concat,
				new StringConstant (Compiler.BuiltinTypes, " }", loc));

			//
			// Equals (object obj) override
			//		
			var other_variable_assign = new TemporaryVariableReference (li_other, loc);
			equals_block.AddStatement (new StatementExpression (
				new SimpleAssign (other_variable_assign,
					new As (equals_block.GetParameterReference (0, loc),
						current_type, loc), loc)));

			Expression equals_test = new Binary (Binary.Operator.Inequality, other_variable, new NullLiteral (loc));
			if (rs_equals != null)
				equals_test = new Binary (Binary.Operator.LogicalAnd, equals_test, rs_equals);
			equals_block.AddStatement (new Return (equals_test, loc));

			equals.Block = equals_block;
			equals.Define ();
			Members.Add (equals);

			//
			// GetHashCode () override
			//
			Method hashcode = new Method (this, new TypeExpression (Compiler.BuiltinTypes.Int, loc),
				Modifiers.PUBLIC | Modifiers.OVERRIDE | Modifiers.DEBUGGER_HIDDEN,
				new MemberName ("GetHashCode", loc),
				Mono.CSharp.ParametersCompiled.EmptyReadOnlyParameters, null);

			//
			// Modified FNV with good avalanche behavior and uniform
			// distribution with larger hash sizes.
			//
			// const int FNV_prime = 16777619;
			// int hash = (int) 2166136261;
			// foreach (int d in data)
			//     hash = (hash ^ d) * FNV_prime;
			// hash += hash << 13;
			// hash ^= hash >> 7;
			// hash += hash << 3;
			// hash ^= hash >> 17;
			// hash += hash << 5;

			ToplevelBlock hashcode_top = new ToplevelBlock (Compiler, loc);
			Block hashcode_block = new Block (hashcode_top, loc, loc);
			hashcode_top.AddStatement (new Unchecked (hashcode_block, loc));

			var li_hash = LocalVariable.CreateCompilerGenerated (Compiler.BuiltinTypes.Int, hashcode_top, loc);
			hashcode_block.AddStatement (new BlockVariableDeclaration (new TypeExpression (li_hash.Type, loc), li_hash));
			LocalVariableReference hash_variable_assign = new LocalVariableReference (li_hash, loc);
			hashcode_block.AddStatement (new StatementExpression (
				new SimpleAssign (hash_variable_assign, rs_hashcode)));

			var hash_variable = new LocalVariableReference (li_hash, loc);
			hashcode_block.AddStatement (new StatementExpression (
				new CompoundAssign (Binary.Operator.Addition, hash_variable,
					new Binary (Binary.Operator.LeftShift, hash_variable, new IntConstant (Compiler.BuiltinTypes, 13, loc)))));
			hashcode_block.AddStatement (new StatementExpression (
				new CompoundAssign (Binary.Operator.ExclusiveOr, hash_variable,
					new Binary (Binary.Operator.RightShift, hash_variable, new IntConstant (Compiler.BuiltinTypes, 7, loc)))));
			hashcode_block.AddStatement (new StatementExpression (
				new CompoundAssign (Binary.Operator.Addition, hash_variable,
					new Binary (Binary.Operator.LeftShift, hash_variable, new IntConstant (Compiler.BuiltinTypes, 3, loc)))));
			hashcode_block.AddStatement (new StatementExpression (
				new CompoundAssign (Binary.Operator.ExclusiveOr, hash_variable,
					new Binary (Binary.Operator.RightShift, hash_variable, new IntConstant (Compiler.BuiltinTypes, 17, loc)))));
			hashcode_block.AddStatement (new StatementExpression (
				new CompoundAssign (Binary.Operator.Addition, hash_variable,
					new Binary (Binary.Operator.LeftShift, hash_variable, new IntConstant (Compiler.BuiltinTypes, 5, loc)))));

			hashcode_block.AddStatement (new Return (hash_variable, loc));
			hashcode.Block = hashcode_top;
			hashcode.Define ();
			Members.Add (hashcode);

			//
			// ToString () override
			//

			ToplevelBlock tostring_block = new ToplevelBlock (Compiler, loc);
			tostring_block.AddStatement (new Return (string_concat, loc));
			tostring.Block = tostring_block;
			tostring.Define ();
			Members.Add (tostring);

			return true;
		}

		public override string GetSignatureForError ()
		{
			return SignatureForError;
		}

		public override CompilationSourceFile GetCompilationSourceFile ()
		{
			return null;
		}

		public IList<AnonymousTypeParameter> Parameters {
			get {
				return parameters;
			}
		}
	}
}
