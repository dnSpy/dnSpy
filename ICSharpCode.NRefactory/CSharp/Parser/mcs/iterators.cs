//
// iterators.cs: Support for implementing iterators
//
// Author:
//   Miguel de Icaza (miguel@ximian.com)
//   Marek Safar (marek.safar@gmail.com)
//
// Dual licensed under the terms of the MIT X11 or GNU GPL
// Copyright 2003 Ximian, Inc.
// Copyright 2003-2008 Novell, Inc.
//

// TODO:
//    Flow analysis for Yield.
//

using System;
using System.Collections.Generic;

#if STATIC
using IKVM.Reflection.Emit;
#else
using System.Reflection.Emit;
#endif

namespace Mono.CSharp {

	public class Yield : ResumableStatement {
		Expression expr;
		bool unwind_protect;
		Iterator iterator;
		int resume_pc;
		
		public Expression Expr {
			get { return this.expr; }
		}
		
		public Yield (Expression expr, Location l)
		{
			this.expr = expr;
			loc = l;
		}

		public static bool CheckContext (ResolveContext ec, Location loc)
		{
			if (!ec.CurrentAnonymousMethod.IsIterator) {
				ec.Report.Error (1621, loc,
					      "The yield statement cannot be used inside " +
					      "anonymous method blocks");
				return false;
			}

			return true;
		}

		public override bool Resolve (BlockContext ec)
		{
			expr = expr.Resolve (ec);
			if (expr == null)
				return false;

			Report.Debug (64, "RESOLVE YIELD #1", this, ec, expr, expr.GetType (),
				      ec.CurrentAnonymousMethod, ec.CurrentIterator);

			if (!CheckContext (ec, loc))
				return false;

			iterator = ec.CurrentIterator;
			if (expr.Type != iterator.OriginalIteratorType) {
				expr = Convert.ImplicitConversionRequired (
					ec, expr, iterator.OriginalIteratorType, loc);
				if (expr == null)
					return false;
			}

			if (!ec.CurrentBranching.CurrentUsageVector.IsUnreachable)
				unwind_protect = ec.CurrentBranching.AddResumePoint (this, loc, out resume_pc);

			return true;
		}

		protected override void DoEmit (EmitContext ec)
		{
			iterator.MarkYield (ec, expr, resume_pc, unwind_protect, resume_point);
		}

		protected override void CloneTo (CloneContext clonectx, Statement t)
		{
			Yield target = (Yield) t;

			target.expr = expr.Clone (clonectx);
		}
		
		public override object Accept (StructuralVisitor visitor)
		{
			return visitor.Visit (this);
		}
	}

	public class YieldBreak : ExitStatement
	{
		Iterator iterator;

		public YieldBreak (Location l)
		{
			loc = l;
		}

		public override void Error_FinallyClause (Report Report)
		{
			Report.Error (1625, loc, "Cannot yield in the body of a finally clause");
		}

		protected override void CloneTo (CloneContext clonectx, Statement target)
		{
			throw new NotSupportedException ();
		}

		protected override bool DoResolve (BlockContext ec)
		{
			iterator = ec.CurrentIterator;
			return Yield.CheckContext (ec, loc);
		}

		protected override void DoEmit (EmitContext ec)
		{
			iterator.EmitYieldBreak (ec, unwind_protect);
		}
		
		public override object Accept (StructuralVisitor visitor)
		{
			return visitor.Visit (this);
		}
	}

	public class IteratorStorey : AnonymousMethodStorey
	{
		class GetEnumeratorMethod : IteratorMethod
		{
			sealed class GetEnumeratorStatement : Statement
			{
				IteratorStorey host;
				IteratorMethod host_method;

				Expression new_storey;

				public GetEnumeratorStatement (IteratorStorey host, IteratorMethod host_method)
				{
					this.host = host;
					this.host_method = host_method;
					loc = host_method.Location;
				}

				protected override void CloneTo (CloneContext clonectx, Statement target)
				{
					throw new NotSupportedException ();
				}

				public override bool Resolve (BlockContext ec)
				{
					TypeExpression storey_type_expr = new TypeExpression (host.Definition, loc);
					List<Expression> init = null;
					if (host.hoisted_this != null) {
						init = new List<Expression> (host.hoisted_params == null ? 1 : host.HoistedParameters.Count + 1);
						HoistedThis ht = host.hoisted_this;
						FieldExpr from = new FieldExpr (ht.Field, loc);
						from.InstanceExpression = CompilerGeneratedThis.Instance;
						init.Add (new ElementInitializer (ht.Field.Name, from, loc));
					}

					if (host.hoisted_params != null) {
						if (init == null)
							init = new List<Expression> (host.HoistedParameters.Count);

						for (int i = 0; i < host.hoisted_params.Count; ++i) {
							HoistedParameter hp = (HoistedParameter) host.hoisted_params [i];
							HoistedParameter hp_cp = (HoistedParameter) host.hoisted_params_copy [i];

							FieldExpr from = new FieldExpr (hp_cp.Field, loc);
							from.InstanceExpression = CompilerGeneratedThis.Instance;

							init.Add (new ElementInitializer (hp.Field.Name, from, loc));
						}
					}

					if (init != null) {
						new_storey = new NewInitialize (storey_type_expr, null,
							new CollectionOrObjectInitializers (init, loc), loc);
					} else {
						new_storey = new New (storey_type_expr, null, loc);
					}

					new_storey = new_storey.Resolve (ec);
					if (new_storey != null)
						new_storey = Convert.ImplicitConversionRequired (ec, new_storey, host_method.MemberType, loc);

					var t = ec.Module.PredefinedTypes.Interlocked.Resolve (loc);
					if (t != null) {
						var p = new ParametersImported (
							new[] {
									new ParameterData (null, Parameter.Modifier.REF),
									new ParameterData (null, Parameter.Modifier.NONE),
									new ParameterData (null, Parameter.Modifier.NONE)
								},
							new[] {
									TypeManager.int32_type, TypeManager.int32_type, TypeManager.int32_type
								},
							false);
						var f = new MemberFilter ("CompareExchange", 0, MemberKind.Method, p, TypeManager.int32_type);
						TypeManager.int_interlocked_compare_exchange = TypeManager.GetPredefinedMethod (t, f, loc);
					}

					ec.CurrentBranching.CurrentUsageVector.Goto ();
					return true;
				}

				protected override void DoEmit (EmitContext ec)
				{
					Label label_init = ec.DefineLabel ();

					ec.Emit (OpCodes.Ldarg_0);
					ec.Emit (OpCodes.Ldflda, host.PC.Spec);
					ec.EmitInt ((int) Iterator.State.Start);
					ec.EmitInt ((int) Iterator.State.Uninitialized);
					ec.Emit (OpCodes.Call, TypeManager.int_interlocked_compare_exchange);

					ec.EmitInt ((int) Iterator.State.Uninitialized);
					ec.Emit (OpCodes.Bne_Un_S, label_init);

					ec.Emit (OpCodes.Ldarg_0);
					ec.Emit (OpCodes.Ret);

					ec.MarkLabel (label_init);

					new_storey.Emit (ec);
					ec.Emit (OpCodes.Ret);
				}
			}

			public GetEnumeratorMethod (IteratorStorey host, FullNamedExpression returnType, MemberName name)
				: base (host, returnType, Modifiers.DEBUGGER_HIDDEN, name)
			{
				Block.AddStatement (new GetEnumeratorStatement (host, this));
			}
		}

		class DisposeMethod : IteratorMethod
		{
			sealed class DisposeMethodStatement : Statement
			{
				Iterator iterator;

				public DisposeMethodStatement (Iterator iterator)
				{
					this.iterator = iterator;
					this.loc = iterator.Location;
				}

				protected override void CloneTo (CloneContext clonectx, Statement target)
				{
					throw new NotSupportedException ();
				}

				public override bool Resolve (BlockContext ec)
				{
					return true;
				}

				protected override void DoEmit (EmitContext ec)
				{
					iterator.EmitDispose (ec);
				}
			}

			public DisposeMethod (IteratorStorey host)
				: base (host, new TypeExpression (TypeManager.void_type, host.Location), Modifiers.PUBLIC | Modifiers.DEBUGGER_HIDDEN,
					new MemberName ("Dispose", host.Location))
			{
				host.AddMethod (this);

				Block.AddStatement (new DisposeMethodStatement (host.Iterator));
			}
		}

		//
		// Uses Method as method info
		//
		class DynamicMethodGroupExpr : MethodGroupExpr
		{
			readonly Method method;

			public DynamicMethodGroupExpr (Method method, Location loc)
				: base ((IList<MemberSpec>) null, null, loc)
			{
				this.method = method;
				eclass = ExprClass.Unresolved;
			}

			protected override Expression DoResolve (ResolveContext ec)
			{
				Methods = new List<MemberSpec> (1) { method.Spec };
				type = method.Parent.Definition;
				InstanceExpression = new CompilerGeneratedThis (type, Location);
				return base.DoResolve (ec);
			}
		}

		class DynamicFieldExpr : FieldExpr
		{
			readonly Field field;

			public DynamicFieldExpr (Field field, Location loc)
				: base (loc)
			{
				this.field = field;
			}

			protected override Expression DoResolve (ResolveContext ec)
			{
				spec = field.Spec;
				type = spec.MemberType;
				InstanceExpression = new CompilerGeneratedThis (type, Location);
				return base.DoResolve (ec);
			}
		}

		public readonly Iterator Iterator;

		TypeExpr iterator_type_expr;
		Field pc_field;
		Field current_field;

		TypeExpr enumerator_type;
		TypeExpr enumerable_type;
		TypeArguments generic_args;
		TypeExpr generic_enumerator_type;
		TypeExpr generic_enumerable_type;

		List<HoistedParameter> hoisted_params_copy;
		int local_name_idx;

		public IteratorStorey (Iterator iterator)
			: base (iterator.Container.ParametersBlock, iterator.Host,
			  iterator.OriginalMethod as MemberBase, iterator.GenericMethod == null ? null : iterator.GenericMethod.CurrentTypeParameters, "Iterator")
		{
			this.Iterator = iterator;
		}

		public Field PC {
			get { return pc_field; }
		}

		public Field CurrentField {
			get { return current_field; }
		}

		public IList<HoistedParameter> HoistedParameters {
			get { return hoisted_params; }
		}

		protected override TypeExpr [] ResolveBaseTypes (out TypeExpr base_class)
		{
			var mtype = Iterator.OriginalIteratorType;
			if (Mutator != null)
				mtype = Mutator.Mutate (mtype);

			iterator_type_expr = new TypeExpression (mtype, Location);
			generic_args = new TypeArguments (iterator_type_expr);

			var list = new List<FullNamedExpression> ();
			if (Iterator.IsEnumerable) {
				enumerable_type = new TypeExpression (
					TypeManager.ienumerable_type, Location);
				list.Add (enumerable_type);

				if (TypeManager.generic_ienumerable_type != null) {
					generic_enumerable_type = new GenericTypeExpr (
						TypeManager.generic_ienumerable_type,
						generic_args, Location);
					list.Add (generic_enumerable_type);
				}
			}

			enumerator_type = new TypeExpression (
				TypeManager.ienumerator_type, Location);
			list.Add (enumerator_type);

			list.Add (new TypeExpression (TypeManager.idisposable_type, Location));

			var ienumerator_generic = Module.PredefinedTypes.IEnumeratorGeneric;
			if (ienumerator_generic.Define ()) {
				generic_enumerator_type = new GenericTypeExpr (ienumerator_generic.TypeSpec, generic_args, Location);
				list.Add (generic_enumerator_type);
			}

			type_bases = list;

			return base.ResolveBaseTypes (out base_class);
		}

		protected override string GetVariableMangledName (LocalVariable local_info)
		{
			return "<" + local_info.Name + ">__" + local_name_idx++.ToString ();
		}

		protected override bool DoDefineMembers ()
		{
			DefineIteratorMembers ();
			return base.DoDefineMembers ();
		}

		void DefineIteratorMembers ()
		{
			pc_field = AddCompilerGeneratedField ("$PC", new TypeExpression (TypeManager.int32_type, Location));
			current_field = AddCompilerGeneratedField ("$current", iterator_type_expr);

			if (hoisted_params != null) {
				//
				// Iterators are independent, each GetEnumerator call has to
				// create same enumerator therefore we have to keep original values
				// around for re-initialization
				//
				// TODO: Do it for assigned/modified parameters only
				//
				hoisted_params_copy = new List<HoistedParameter> (hoisted_params.Count);
				foreach (HoistedParameter hp in hoisted_params) {
					hoisted_params_copy.Add (new HoistedParameter (hp, "<$>" + hp.Field.Name));
				}
			}

			if (generic_enumerator_type != null)
				Define_Current (true);

			Define_Current (false);
			new DisposeMethod (this);
			Define_Reset ();

			if (Iterator.IsEnumerable) {
				MemberName name = new MemberName (QualifiedAliasMember.GlobalAlias, "System", null, Location);
				name = new MemberName (name, "Collections", Location);
				name = new MemberName (name, "IEnumerable", Location);
				name = new MemberName (name, "GetEnumerator", Location);

				if (generic_enumerator_type != null) {
					Method get_enumerator = new IteratorMethod (this, enumerator_type, 0, name);

					name = new MemberName (name.Left.Left, "Generic", Location);
					name = new MemberName (name, "IEnumerable", generic_args, Location);
					name = new MemberName (name, "GetEnumerator", Location);
					Method gget_enumerator = new GetEnumeratorMethod (this, generic_enumerator_type, name);

					//
					// Just call generic GetEnumerator implementation
					//
					get_enumerator.Block.AddStatement (
						new Return (new Invocation (new DynamicMethodGroupExpr (gget_enumerator, Location), null), Location));

					AddMethod (get_enumerator);
					AddMethod (gget_enumerator);
				} else {
					AddMethod (new GetEnumeratorMethod (this, enumerator_type, name));
				}
			}
		}

		protected override void EmitHoistedParameters (EmitContext ec, IList<HoistedParameter> hoisted)
		{
			base.EmitHoistedParameters (ec, hoisted);
			base.EmitHoistedParameters (ec, hoisted_params_copy);
		}

		void Define_Current (bool is_generic)
		{
			TypeExpr type;

			MemberName name = new MemberName (QualifiedAliasMember.GlobalAlias, "System", null, Location);
			name = new MemberName (name, "Collections", Location);

			if (is_generic) {
				name = new MemberName (name, "Generic", Location);
				name = new MemberName (name, "IEnumerator", generic_args, Location);
				type = iterator_type_expr;
			} else {
				name = new MemberName (name, "IEnumerator");
				type = new TypeExpression (TypeManager.object_type, Location);
			}

			name = new MemberName (name, "Current", Location);

			ToplevelBlock get_block = new ToplevelBlock (Compiler, Location);
			get_block.AddStatement (new Return (new DynamicFieldExpr (CurrentField, Location), Location));
				
			Property current = new Property (this, type, Modifiers.DEBUGGER_HIDDEN, name, null);
			current.Get = new Property.GetMethod (current, 0, null, Location);
			current.Get.Block = get_block;

			AddProperty (current);
		}

		void Define_Reset ()
		{
			Method reset = new Method (
				this, null, new TypeExpression (TypeManager.void_type, Location),
				Modifiers.PUBLIC | Modifiers.DEBUGGER_HIDDEN,
				new MemberName ("Reset", Location),
				ParametersCompiled.EmptyReadOnlyParameters, null);
			AddMethod (reset);

			reset.Block = new ToplevelBlock (Compiler, Location);

			TypeSpec ex_type = Module.PredefinedTypes.NotSupportedException.Resolve (Location);
			if (ex_type == null)
				return;

			reset.Block.AddStatement (new Throw (new New (new TypeExpression (ex_type, Location), null, Location), Location));
		}
	}

	class IteratorMethod : Method
	{
		readonly IteratorStorey host;

		public IteratorMethod (IteratorStorey host, FullNamedExpression returnType, Modifiers mod, MemberName name)
			: base (host, null, returnType, mod | Modifiers.COMPILER_GENERATED,
			  name, ParametersCompiled.EmptyReadOnlyParameters, null)
		{
			this.host = host;

			Block = new ToplevelBlock (host.Compiler, ParametersCompiled.EmptyReadOnlyParameters, Location);
		}

		public override EmitContext CreateEmitContext (ILGenerator ig)
		{
			EmitContext ec = new EmitContext (this, ig, MemberType);

			ec.CurrentAnonymousMethod = host.Iterator;
			return ec;
		}
	}

	//
	// Iterators are implemented as hidden anonymous block
	//
	public class Iterator : AnonymousExpression
	{
		sealed class MoveNextMethodStatement : Statement
		{
			Iterator iterator;

			public MoveNextMethodStatement (Iterator iterator)
			{
				this.iterator = iterator;
				this.loc = iterator.Location;
			}

			protected override void CloneTo (CloneContext clonectx, Statement target)
			{
				throw new NotSupportedException ();
			}

			public override bool Resolve (BlockContext ec)
			{
				return true;
			}

			protected override void DoEmit (EmitContext ec)
			{
				iterator.EmitMoveNext (ec);
			}
		}

		public readonly IMethodData OriginalMethod;
		public readonly TypeContainer Host;
		public readonly bool IsEnumerable;
		List<ResumableStatement> resume_points;

		//
		// The state as we generate the iterator
		//
		Label move_next_ok, move_next_error;
		LocalBuilder skip_finally, current_pc;

		public LocalBuilder SkipFinally {
			get { return skip_finally; }
		}

		public LocalBuilder CurrentPC {
			get { return current_pc; }
		}

		public Block Container {
			get { return OriginalMethod.Block; }
		}

		public GenericMethod GenericMethod {
			get { return OriginalMethod.GenericMethod; }
		}

		public readonly TypeSpec OriginalIteratorType;

		IteratorStorey IteratorHost;

		public enum State {
			Running = -3, // Used only in CurrentPC, never stored into $PC
			Uninitialized = -2,
			After = -1,
			Start = 0
		}

		public void EmitYieldBreak (EmitContext ec, bool unwind_protect)
		{
			ec.Emit (unwind_protect ? OpCodes.Leave : OpCodes.Br, move_next_error);
		}

		void EmitMoveNext_NoResumePoints (EmitContext ec, Block original_block)
		{
			ec.Emit (OpCodes.Ldarg_0);
			ec.Emit (OpCodes.Ldfld, IteratorHost.PC.Spec);

			ec.Emit (OpCodes.Ldarg_0);
			ec.EmitInt ((int) State.After);
			ec.Emit (OpCodes.Stfld, IteratorHost.PC.Spec);

			// We only care if the PC is zero (start executing) or non-zero (don't do anything)
			ec.Emit (OpCodes.Brtrue, move_next_error);

			SymbolWriter.StartIteratorBody (ec);
			original_block.Emit (ec);
			SymbolWriter.EndIteratorBody (ec);

			ec.MarkLabel (move_next_error);
			ec.Emit (OpCodes.Ldc_I4_0);
			ec.Emit (OpCodes.Ret);
		}

		void EmitMoveNext (EmitContext ec)
		{
			move_next_ok = ec.DefineLabel ();
			move_next_error = ec.DefineLabel ();

			if (resume_points == null) {
				EmitMoveNext_NoResumePoints (ec, block);
				return;
			}

			current_pc = ec.GetTemporaryLocal (TypeManager.uint32_type);
			ec.Emit (OpCodes.Ldarg_0);
			ec.Emit (OpCodes.Ldfld, IteratorHost.PC.Spec);
			ec.Emit (OpCodes.Stloc, current_pc);

			// We're actually in state 'running', but this is as good a PC value as any if there's an abnormal exit
			ec.Emit (OpCodes.Ldarg_0);
			ec.EmitInt ((int) State.After);
			ec.Emit (OpCodes.Stfld, IteratorHost.PC.Spec);

			Label [] labels = new Label [1 + resume_points.Count];
			labels [0] = ec.DefineLabel ();

			bool need_skip_finally = false;
			for (int i = 0; i < resume_points.Count; ++i) {
				ResumableStatement s = resume_points [i];
				need_skip_finally |= s is ExceptionStatement;
				labels [i+1] = s.PrepareForEmit (ec);
			}

			if (need_skip_finally) {
				skip_finally = ec.GetTemporaryLocal (TypeManager.bool_type);
				ec.Emit (OpCodes.Ldc_I4_0);
				ec.Emit (OpCodes.Stloc, skip_finally);
			}

			SymbolWriter.StartIteratorDispatcher (ec);
			ec.Emit (OpCodes.Ldloc, current_pc);
			ec.Emit (OpCodes.Switch, labels);

			ec.Emit (OpCodes.Br, move_next_error);
			SymbolWriter.EndIteratorDispatcher (ec);

			ec.MarkLabel (labels [0]);

			SymbolWriter.StartIteratorBody (ec);
			block.Emit (ec);
			SymbolWriter.EndIteratorBody (ec);

			SymbolWriter.StartIteratorDispatcher (ec);

			ec.Emit (OpCodes.Ldarg_0);
			ec.EmitInt ((int) State.After);
			ec.Emit (OpCodes.Stfld, IteratorHost.PC.Spec);

			ec.MarkLabel (move_next_error);
			ec.EmitInt (0);
			ec.Emit (OpCodes.Ret);

			ec.MarkLabel (move_next_ok);
			ec.Emit (OpCodes.Ldc_I4_1);
			ec.Emit (OpCodes.Ret);

			SymbolWriter.EndIteratorDispatcher (ec);
		}

		public void EmitDispose (EmitContext ec)
		{
			Label end = ec.DefineLabel ();

			Label [] labels = null;
			int n_resume_points = resume_points == null ? 0 : resume_points.Count;
			for (int i = 0; i < n_resume_points; ++i) {
				ResumableStatement s = (ResumableStatement) resume_points [i];
				Label ret = s.PrepareForDispose (ec, end);
				if (ret.Equals (end) && labels == null)
					continue;
				if (labels == null) {
					labels = new Label [resume_points.Count + 1];
					for (int j = 0; j <= i; ++j)
						labels [j] = end;
				}
				labels [i+1] = ret;
			}

			if (labels != null) {
				current_pc = ec.GetTemporaryLocal (TypeManager.uint32_type);
				ec.Emit (OpCodes.Ldarg_0);
				ec.Emit (OpCodes.Ldfld, IteratorHost.PC.Spec);
				ec.Emit (OpCodes.Stloc, current_pc);
			}

			ec.Emit (OpCodes.Ldarg_0);
			ec.EmitInt ((int) State.After);
			ec.Emit (OpCodes.Stfld, IteratorHost.PC.Spec);

			if (labels != null) {
				//SymbolWriter.StartIteratorDispatcher (ec.ig);
				ec.Emit (OpCodes.Ldloc, current_pc);
				ec.Emit (OpCodes.Switch, labels);
				//SymbolWriter.EndIteratorDispatcher (ec.ig);

				foreach (ResumableStatement s in resume_points)
					s.EmitForDispose (ec, this, end, true);
			}

			ec.MarkLabel (end);
		}

		public int AddResumePoint (ResumableStatement stmt)
		{
			if (resume_points == null)
				resume_points = new List<ResumableStatement> ();

			resume_points.Add (stmt);
			return resume_points.Count;
		}

		//
		// Called back from Yield
		//
		public void MarkYield (EmitContext ec, Expression expr, int resume_pc, bool unwind_protect, Label resume_point)
		{
			// Store the new current
			ec.Emit (OpCodes.Ldarg_0);
			expr.Emit (ec);
			ec.Emit (OpCodes.Stfld, IteratorHost.CurrentField.Spec);

			// store resume program-counter
			ec.Emit (OpCodes.Ldarg_0);
			ec.EmitInt (resume_pc);
			ec.Emit (OpCodes.Stfld, IteratorHost.PC.Spec);

			// mark finally blocks as disabled
			if (unwind_protect && skip_finally != null) {
				ec.EmitInt (1);
				ec.Emit (OpCodes.Stloc, skip_finally);
			}

			// Return ok
			ec.Emit (unwind_protect ? OpCodes.Leave : OpCodes.Br, move_next_ok);

			ec.MarkLabel (resume_point);
		}

		//
		// Our constructor
		//
		public Iterator (ParametersBlock block, IMethodData method, TypeContainer host, TypeSpec iterator_type, bool is_enumerable)
			: base (block, TypeManager.bool_type, block.StartLocation)
		{
			this.OriginalMethod = method;
			this.OriginalIteratorType = iterator_type;
			this.IsEnumerable = is_enumerable;
			this.Host = host;
			this.type = method.ReturnType;
		}

		public override string ContainerType {
			get { return "iterator"; }
		}

		public override bool IsIterator {
			get { return true; }
		}

		public override AnonymousMethodStorey Storey {
			get { return IteratorHost; }
		}

		public override string GetSignatureForError ()
		{
			return OriginalMethod.GetSignatureForError ();
		}

		protected override Expression DoResolve (ResolveContext ec)
		{
			IteratorHost = (IteratorStorey) block.TopBlock.AnonymousMethodStorey;

			BlockContext ctx = new BlockContext (ec, block, ReturnType);
			ctx.CurrentAnonymousMethod = this;

			ctx.StartFlowBranching (this, ec.CurrentBranching);
			Block.Resolve (ctx);
			ctx.EndFlowBranching ();

			var move_next = new IteratorMethod (IteratorHost, new TypeExpression (TypeManager.bool_type, loc),
				Modifiers.PUBLIC, new MemberName ("MoveNext", Location));
			move_next.Block.AddStatement (new MoveNextMethodStatement (this));
			IteratorHost.AddMethod (move_next);

			eclass = ExprClass.Value;
			return this;
		}

		public override void Emit (EmitContext ec)
		{
			//
			// Load Iterator storey instance
			//
			IteratorHost.Instance.Emit (ec);

			//
			// Initialize iterator PC when it's unitialized
			//
			if (IsEnumerable) {
				ec.Emit (OpCodes.Dup);
				ec.EmitInt ((int)State.Uninitialized);

				var field = IteratorHost.PC.Spec;
				if (Storey.MemberName.IsGeneric) {
					field = MemberCache.GetMember (Storey.Instance.Type, field);
				}

				ec.Emit (OpCodes.Stfld, field);
			}
		}

		public override Expression CreateExpressionTree (ResolveContext ec)
		{
			throw new NotSupportedException ("ET");
		}

		public static void CreateIterator (IMethodData method, TypeContainer parent, Modifiers modifiers, CompilerContext ctx)
		{
			bool is_enumerable;
			TypeSpec iterator_type;

			TypeSpec ret = method.ReturnType;
			if (ret == null)
				return;

			if (!CheckType (ret, out iterator_type, out is_enumerable)) {
				ctx.Report.Error (1624, method.Location,
					      "The body of `{0}' cannot be an iterator block " +
					      "because `{1}' is not an iterator interface type",
					      method.GetSignatureForError (),
					      TypeManager.CSharpName (ret));
				return;
			}

			ParametersCompiled parameters = method.ParameterInfo;
			for (int i = 0; i < parameters.Count; i++) {
				Parameter p = parameters [i];
				Parameter.Modifier mod = p.ModFlags;
				if ((mod & Parameter.Modifier.ISBYREF) != 0) {
					ctx.Report.Error (1623, p.Location,
						"Iterators cannot have ref or out parameters");
					return;
				}

				if (p is ArglistParameter) {
					ctx.Report.Error (1636, method.Location,
						"__arglist is not allowed in parameter list of iterators");
					return;
				}

				if (parameters.Types [i].IsPointer) {
					ctx.Report.Error (1637, p.Location,
							  "Iterators cannot have unsafe parameters or " +
							  "yield types");
					return;
				}
			}

			if ((modifiers & Modifiers.UNSAFE) != 0) {
				ctx.Report.Error (1629, method.Location, "Unsafe code may not appear in iterators");
			}

			method.Block.WrapIntoIterator (method, parent, iterator_type, is_enumerable);
		}

		static bool CheckType (TypeSpec ret, out TypeSpec original_iterator_type, out bool is_enumerable)
		{
			original_iterator_type = null;
			is_enumerable = false;

			if (ret == TypeManager.ienumerable_type) {
				original_iterator_type = TypeManager.object_type;
				is_enumerable = true;
				return true;
			}
			if (ret == TypeManager.ienumerator_type) {
				original_iterator_type = TypeManager.object_type;
				is_enumerable = false;
				return true;
			}

			InflatedTypeSpec inflated = ret as InflatedTypeSpec;
			if (inflated == null)
				return false;

			ret = inflated.GetDefinition ();
			if (ret == TypeManager.generic_ienumerable_type) {
				original_iterator_type = inflated.TypeArguments[0];
				is_enumerable = true;
				return true;
			}
			
			if (ret == TypeManager.generic_ienumerator_type) {
				original_iterator_type = inflated.TypeArguments[0];
				is_enumerable = false;
				return true;
			}

			return false;
		}
	}
}

