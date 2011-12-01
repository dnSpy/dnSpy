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
// Copyright 2011 Xamarin Inc.
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

namespace Mono.CSharp
{
	public abstract class YieldStatement<T> : ResumableStatement where T : StateMachineInitializer
	{
		protected Expression expr;
		protected bool unwind_protect;
		protected T machine_initializer;
		int resume_pc;
		
		public Expression Expr {
			get { return this.expr; }
		}
		
		protected YieldStatement (Expression expr, Location l)
		{
			this.expr = expr;
			loc = l;
		}

		protected override void CloneTo (CloneContext clonectx, Statement t)
		{
			var target = (YieldStatement<T>) t;
			target.expr = expr.Clone (clonectx);
		}

		protected override void DoEmit (EmitContext ec)
		{
			machine_initializer.InjectYield (ec, expr, resume_pc, unwind_protect, resume_point);
		}

		public override bool Resolve (BlockContext bc)
		{
			expr = expr.Resolve (bc);
			if (expr == null)
				return false;

			machine_initializer = bc.CurrentAnonymousMethod as T;

			if (!bc.CurrentBranching.CurrentUsageVector.IsUnreachable)
				unwind_protect = bc.CurrentBranching.AddResumePoint (this, out resume_pc);

			return true;
		}
	}

	public class Yield : YieldStatement<Iterator>
	{
		public Yield (Expression expr, Location loc)
			: base (expr, loc)
		{
		}

		public static bool CheckContext (ResolveContext ec, Location loc)
		{
			if (!ec.CurrentAnonymousMethod.IsIterator) {
				ec.Report.Error (1621, loc,
					"The yield statement cannot be used inside anonymous method blocks");
				return false;
			}

			return true;
		}

		public override bool Resolve (BlockContext bc)
		{
			if (!CheckContext (bc, loc))
				return false;

			if (!base.Resolve (bc))
				return false;

			var otype = bc.CurrentIterator.OriginalIteratorType;
			if (expr.Type != otype) {
				expr = Convert.ImplicitConversionRequired (bc, expr, otype, loc);
				if (expr == null)
					return false;
			}

			return true;
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

	public abstract class StateMachine : AnonymousMethodStorey
	{
		public enum State
		{
			Running = -3, // Used only in CurrentPC, never stored into $PC
			Uninitialized = -2,
			After = -1,
			Start = 0
		}

		Field pc_field;
		StateMachineMethod method;

		protected StateMachine (Block block, TypeContainer parent, MemberBase host, TypeParameter[] tparams, string name)
			: base (block, parent, host, tparams, name)
		{
		}

		#region Properties

		public StateMachineMethod StateMachineMethod {
			get {
				return method;
			}
		}

		public Field PC {
			get {
				return pc_field;
			}
		}

		#endregion

		public void AddEntryMethod (StateMachineMethod method)
		{
			if (this.method != null)
				throw new InternalErrorException ();

			this.method = method;
			AddMethod (method);
		}

		protected override bool DoDefineMembers ()
		{
			pc_field = AddCompilerGeneratedField ("$PC", new TypeExpression (Compiler.BuiltinTypes.Int, Location));

			return base.DoDefineMembers ();
		}
	}

	class IteratorStorey : StateMachine
	{
		class GetEnumeratorMethod : StateMachineMethod
		{
			sealed class GetEnumeratorStatement : Statement
			{
				readonly IteratorStorey host;
				readonly StateMachineMethod host_method;

				Expression new_storey;

				public GetEnumeratorStatement (IteratorStorey host, StateMachineMethod host_method)
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
						from.InstanceExpression = new CompilerGeneratedThis (ec.CurrentType, loc);
						init.Add (new ElementInitializer (ht.Field.Name, from, loc));
					}

					if (host.hoisted_params != null) {
						if (init == null)
							init = new List<Expression> (host.HoistedParameters.Count);

						for (int i = 0; i < host.hoisted_params.Count; ++i) {
							HoistedParameter hp = host.hoisted_params [i];
							HoistedParameter hp_cp = host.hoisted_params_copy [i];

							FieldExpr from = new FieldExpr (hp_cp.Field, loc);
							from.InstanceExpression = new CompilerGeneratedThis (ec.CurrentType, loc);

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

					ec.CurrentBranching.CurrentUsageVector.Goto ();
					return true;
				}

				protected override void DoEmit (EmitContext ec)
				{
					Label label_init = ec.DefineLabel ();

					ec.EmitThis ();
					ec.Emit (OpCodes.Ldflda, host.PC.Spec);
					ec.EmitInt ((int) State.Start);
					ec.EmitInt ((int) State.Uninitialized);

					var m = ec.Module.PredefinedMembers.InterlockedCompareExchange.Resolve (loc);
					if (m != null)
						ec.Emit (OpCodes.Call, m);

					ec.EmitInt ((int) State.Uninitialized);
					ec.Emit (OpCodes.Bne_Un_S, label_init);

					ec.EmitThis ();
					ec.Emit (OpCodes.Ret);

					ec.MarkLabel (label_init);

					new_storey.Emit (ec);
					ec.Emit (OpCodes.Ret);
				}
			}

			public GetEnumeratorMethod (IteratorStorey host, FullNamedExpression returnType, MemberName name)
				: base (host, null, returnType, Modifiers.DEBUGGER_HIDDEN, name)
			{
				Block.AddStatement (new GetEnumeratorStatement (host, this));
			}
		}

		class DisposeMethod : StateMachineMethod
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
					ec.CurrentAnonymousMethod = iterator;
					iterator.EmitDispose (ec);
				}
			}

			public DisposeMethod (IteratorStorey host)
				: base (host, null, new TypeExpression (host.Compiler.BuiltinTypes.Void, host.Location), Modifiers.PUBLIC | Modifiers.DEBUGGER_HIDDEN,
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

		List<HoistedParameter> hoisted_params_copy;

		TypeExpr iterator_type_expr;
		Field current_field;
		Field disposing_field;
		int local_name_idx;

		TypeExpr enumerator_type;
		TypeExpr enumerable_type;
		TypeArguments generic_args;
		TypeExpr generic_enumerator_type;
		TypeExpr generic_enumerable_type;

		public IteratorStorey (Iterator iterator)
			: base (iterator.Container.ParametersBlock, iterator.Host,
			  iterator.OriginalMethod as MemberBase, iterator.GenericMethod == null ? null : iterator.GenericMethod.CurrentTypeParameters, "Iterator")
		{
			this.Iterator = iterator;
		}

		public Field CurrentField {
			get {
				return current_field;
			}
		}

		public Field DisposingField {
			get {
				return disposing_field;
			}
		}

		public IList<HoistedParameter> HoistedParameters {
			get { return hoisted_params; }
		}

		protected override TypeSpec[] ResolveBaseTypes (out FullNamedExpression base_class)
		{
			var mtype = Iterator.OriginalIteratorType;
			if (Mutator != null)
				mtype = Mutator.Mutate (mtype);

			iterator_type_expr = new TypeExpression (mtype, Location);
			generic_args = new TypeArguments (iterator_type_expr);

			var list = new List<FullNamedExpression> ();
			if (Iterator.IsEnumerable) {
				enumerable_type = new TypeExpression (Compiler.BuiltinTypes.IEnumerable, Location);
				list.Add (enumerable_type);

				if (Module.PredefinedTypes.IEnumerableGeneric.Define ()) {
					generic_enumerable_type = new GenericTypeExpr (Module.PredefinedTypes.IEnumerableGeneric.TypeSpec, generic_args, Location);
					list.Add (generic_enumerable_type);
				}
			}

			enumerator_type = new TypeExpression (Compiler.BuiltinTypes.IEnumerator, Location);
			list.Add (enumerator_type);

			list.Add (new TypeExpression (Compiler.BuiltinTypes.IDisposable, Location));

			var ienumerator_generic = Module.PredefinedTypes.IEnumeratorGeneric;
			if (ienumerator_generic.Define ()) {
				generic_enumerator_type = new GenericTypeExpr (ienumerator_generic.TypeSpec, generic_args, Location);
				list.Add (generic_enumerator_type);
			}

			type_bases = list;

			return base.ResolveBaseTypes (out base_class);
		}

		protected override bool DoDefineMembers ()
		{
			current_field = AddCompilerGeneratedField ("$current", iterator_type_expr);
			disposing_field = AddCompilerGeneratedField ("$disposing", new TypeExpression (Compiler.BuiltinTypes.Bool, Location));

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
					Method get_enumerator = new StateMachineMethod (this, null, enumerator_type, 0, name);

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

			return base.DoDefineMembers ();
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
				type = new TypeExpression (Compiler.BuiltinTypes.Object, Location);
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
				this, null, new TypeExpression (Compiler.BuiltinTypes.Void, Location),
				Modifiers.PUBLIC | Modifiers.DEBUGGER_HIDDEN,
				new MemberName ("Reset", Location),
				ParametersCompiled.EmptyReadOnlyParameters, null);
			AddMethod (reset);

			reset.Block = new ToplevelBlock (Compiler, Location);

			TypeSpec ex_type = Module.PredefinedTypes.NotSupportedException.Resolve ();
			if (ex_type == null)
				return;

			reset.Block.AddStatement (new Throw (new New (new TypeExpression (ex_type, Location), null, Location), Location));
		}

		protected override void EmitHoistedParameters (EmitContext ec, IList<HoistedParameter> hoisted)
		{
			base.EmitHoistedParameters (ec, hoisted);
			base.EmitHoistedParameters (ec, hoisted_params_copy);
		}

		protected override string GetVariableMangledName (LocalVariable local_info)
		{
			return "<" + local_info.Name + ">__" + local_name_idx++.ToString ("X");
		}
	}

	public class StateMachineMethod : Method
	{
		readonly StateMachineInitializer expr;

		public StateMachineMethod (StateMachine host, StateMachineInitializer expr, FullNamedExpression returnType, Modifiers mod, MemberName name)
			: base (host, null, returnType, mod | Modifiers.COMPILER_GENERATED,
			  name, ParametersCompiled.EmptyReadOnlyParameters, null)
		{
			this.expr = expr;
			Block = new ToplevelBlock (host.Compiler, ParametersCompiled.EmptyReadOnlyParameters, Location);
		}

		public override EmitContext CreateEmitContext (ILGenerator ig)
		{
			EmitContext ec = new EmitContext (this, ig, MemberType);
			ec.CurrentAnonymousMethod = expr;

			if (expr is AsyncInitializer)
				ec.With (BuilderContext.Options.AsyncBody, true);

			return ec;
		}
	}

	public abstract class StateMachineInitializer : AnonymousExpression
	{
		sealed class MoveNextBodyStatement : Statement
		{
			readonly StateMachineInitializer state_machine;

			public MoveNextBodyStatement (StateMachineInitializer stateMachine)
			{
				this.state_machine = stateMachine;
				this.loc = stateMachine.Location;
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
				state_machine.EmitMoveNext (ec);
			}
		}

		public readonly TypeContainer Host;
		protected StateMachine storey;

		//
		// The state as we generate the machine
		//
		Label move_next_ok;
		Label iterator_body_end;
		protected Label move_next_error;
		LocalBuilder skip_finally;
		protected LocalBuilder current_pc;
		protected List<ResumableStatement> resume_points;

		protected StateMachineInitializer (ParametersBlock block, TypeContainer host, TypeSpec returnType)
			: base (block, returnType, block.StartLocation)
		{
			this.Host = host;
		}

		#region Properties

		public Label BodyEnd {
			get {
				return iterator_body_end;
			}
		}

		public LocalBuilder CurrentPC
		{
			get {
				return current_pc;
			}
		}

		public LocalBuilder SkipFinally {
			get {
				return skip_finally;
			}
		}

		public override AnonymousMethodStorey Storey {
			get {
				return storey;
			}
		}

		#endregion

		public int AddResumePoint (ResumableStatement stmt)
		{
			if (resume_points == null)
				resume_points = new List<ResumableStatement> ();

			resume_points.Add (stmt);
			return resume_points.Count;
		}

		public override Expression CreateExpressionTree (ResolveContext ec)
		{
			throw new NotSupportedException ("ET");
		}

		protected virtual BlockContext CreateBlockContext (ResolveContext rc)
		{
			var ctx = new BlockContext (rc, block, ((BlockContext) rc).ReturnType);
			ctx.CurrentAnonymousMethod = this;
			return ctx;
		}

		protected override Expression DoResolve (ResolveContext ec)
		{
			storey = (StateMachine) block.Parent.ParametersBlock.AnonymousMethodStorey;

			var ctx = CreateBlockContext (ec);

			Block.Resolve (ctx);

			//
			// Explicit return is required for Task<T> state machine
			//
			var task_storey = storey as AsyncTaskStorey;
			if (task_storey == null || (task_storey.ReturnType != null && !task_storey.ReturnType.IsGenericTask))
				ctx.CurrentBranching.CurrentUsageVector.Goto ();

			ctx.EndFlowBranching ();

			if (!ec.IsInProbingMode) {
				var move_next = new StateMachineMethod (storey, this, new TypeExpression (ReturnType, loc), Modifiers.PUBLIC, new MemberName ("MoveNext", loc));
				move_next.Block.AddStatement (new MoveNextBodyStatement (this));
				storey.AddEntryMethod (move_next);
			}

			eclass = ExprClass.Value;
			return this;
		}

		public override void Emit (EmitContext ec)
		{
			//
			// Load Iterator storey instance
			//
			storey.Instance.Emit (ec);
		}

		void EmitMoveNext_NoResumePoints (EmitContext ec, Block original_block)
		{
			ec.EmitThis ();
			ec.Emit (OpCodes.Ldfld, storey.PC.Spec);

			ec.EmitThis ();
			ec.EmitInt ((int) IteratorStorey.State.After);
			ec.Emit (OpCodes.Stfld, storey.PC.Spec);

			// We only care if the PC is zero (start executing) or non-zero (don't do anything)
			ec.Emit (OpCodes.Brtrue, move_next_error);

			iterator_body_end = ec.DefineLabel ();

			SymbolWriter.StartIteratorBody (ec);
			original_block.Emit (ec);
			SymbolWriter.EndIteratorBody (ec);

			ec.MarkLabel (iterator_body_end);

			EmitMoveNextEpilogue (ec);

			ec.MarkLabel (move_next_error);

			if (ReturnType.Kind != MemberKind.Void) {
				ec.EmitInt (0);
				ec.Emit (OpCodes.Ret);
			}
		}

		void EmitMoveNext (EmitContext ec)
		{
			move_next_ok = ec.DefineLabel ();
			move_next_error = ec.DefineLabel ();

			if (resume_points == null) {
				EmitMoveNext_NoResumePoints (ec, block);
				return;
			}
			
			current_pc = ec.GetTemporaryLocal (ec.BuiltinTypes.UInt);
			ec.EmitThis ();
			ec.Emit (OpCodes.Ldfld, storey.PC.Spec);
			ec.Emit (OpCodes.Stloc, current_pc);

			// We're actually in state 'running', but this is as good a PC value as any if there's an abnormal exit
			ec.EmitThis ();
			ec.EmitInt ((int) IteratorStorey.State.After);
			ec.Emit (OpCodes.Stfld, storey.PC.Spec);

			Label[] labels = new Label[1 + resume_points.Count];
			labels[0] = ec.DefineLabel ();

			bool need_skip_finally = false;
			for (int i = 0; i < resume_points.Count; ++i) {
				ResumableStatement s = resume_points[i];
				need_skip_finally |= s is ExceptionStatement;
				labels[i + 1] = s.PrepareForEmit (ec);
			}

			if (need_skip_finally) {
				skip_finally = ec.GetTemporaryLocal (ec.BuiltinTypes.Bool);
				ec.EmitInt (0);
				ec.Emit (OpCodes.Stloc, skip_finally);
			}

			var async_init = this as AsyncInitializer;
			if (async_init != null)
				ec.BeginExceptionBlock ();

			SymbolWriter.StartIteratorDispatcher (ec);
			ec.Emit (OpCodes.Ldloc, current_pc);
			ec.Emit (OpCodes.Switch, labels);

			ec.Emit (async_init != null ? OpCodes.Leave : OpCodes.Br, move_next_error);
			SymbolWriter.EndIteratorDispatcher (ec);

			ec.MarkLabel (labels[0]);

			iterator_body_end = ec.DefineLabel ();

			SymbolWriter.StartIteratorBody (ec);
			block.Emit (ec);
			SymbolWriter.EndIteratorBody (ec);

			SymbolWriter.StartIteratorDispatcher (ec);

			ec.MarkLabel (iterator_body_end);

			if (async_init != null) {
				var catch_value = LocalVariable.CreateCompilerGenerated (ec.Module.Compiler.BuiltinTypes.Exception, block, Location);

				ec.BeginCatchBlock (catch_value.Type);
				catch_value.EmitAssign (ec);

				ec.EmitThis ();
				ec.EmitInt ((int) IteratorStorey.State.After);
				ec.Emit (OpCodes.Stfld, storey.PC.Spec);

				((AsyncTaskStorey) async_init.Storey).EmitSetException (ec, new LocalVariableReference (catch_value, Location));

				ec.Emit (OpCodes.Leave, move_next_ok);
				ec.EndExceptionBlock ();
			}

			ec.EmitThis ();
			ec.EmitInt ((int) IteratorStorey.State.After);
			ec.Emit (OpCodes.Stfld, storey.PC.Spec);

			EmitMoveNextEpilogue (ec);

			ec.MarkLabel (move_next_error);
			
			if (ReturnType.Kind != MemberKind.Void) {
				ec.EmitInt (0);
				ec.Emit (OpCodes.Ret);
			}

			ec.MarkLabel (move_next_ok);

			if (ReturnType.Kind != MemberKind.Void) {
				ec.EmitInt (1);
				ec.Emit (OpCodes.Ret);
			}

			SymbolWriter.EndIteratorDispatcher (ec);
		}

		protected virtual void EmitMoveNextEpilogue (EmitContext ec)
		{
		}

		public void EmitLeave (EmitContext ec, bool unwind_protect)
		{
			// Return ok
			ec.Emit (unwind_protect ? OpCodes.Leave : OpCodes.Br, move_next_ok);
		}

		//
		// Called back from YieldStatement
		//
		public virtual void InjectYield (EmitContext ec, Expression expr, int resume_pc, bool unwind_protect, Label resume_point)
		{
			//
			// Guard against being disposed meantime
			//
			Label disposed = ec.DefineLabel ();
			var iterator = storey as IteratorStorey;
			if (iterator != null) {
				ec.EmitThis ();
				ec.Emit (OpCodes.Ldfld, iterator.DisposingField.Spec);
				ec.Emit (OpCodes.Brtrue_S, disposed);
			}

			//
			// store resume program-counter
			//
			ec.EmitThis ();
			ec.EmitInt (resume_pc);
			ec.Emit (OpCodes.Stfld, storey.PC.Spec);

			if (iterator != null) {
				ec.MarkLabel (disposed);
			}

			// mark finally blocks as disabled
			if (unwind_protect && skip_finally != null) {
				ec.EmitInt (1);
				ec.Emit (OpCodes.Stloc, skip_finally);
			}
		}
	}

	//
	// Iterators are implemented as hidden anonymous block
	//
	public class Iterator : StateMachineInitializer
	{
		public readonly IMethodData OriginalMethod;
		public readonly bool IsEnumerable;
		public readonly TypeSpec OriginalIteratorType;

		public Iterator (ParametersBlock block, IMethodData method, TypeContainer host, TypeSpec iterator_type, bool is_enumerable)
			: base (block, host, host.Compiler.BuiltinTypes.Bool)
		{
			this.OriginalMethod = method;
			this.OriginalIteratorType = iterator_type;
			this.IsEnumerable = is_enumerable;
			this.type = method.ReturnType;
		}

		public Block Container {
			get { return OriginalMethod.Block; }
		}

		public GenericMethod GenericMethod {
			get { return OriginalMethod.GenericMethod; }
		}

		public override string ContainerType {
			get { return "iterator"; }
		}

		public override bool IsIterator {
			get { return true; }
		}

		public void EmitYieldBreak (EmitContext ec, bool unwind_protect)
		{
			ec.Emit (unwind_protect ? OpCodes.Leave : OpCodes.Br, move_next_error);
		}

		public override string GetSignatureForError ()
		{
			return OriginalMethod.GetSignatureForError ();
		}

		public override void Emit (EmitContext ec)
		{
			//
			// Load Iterator storey instance
			//
			storey.Instance.Emit (ec);

			//
			// Initialize iterator PC when it's unitialized
			//
			if (IsEnumerable) {
				ec.Emit (OpCodes.Dup);
				ec.EmitInt ((int)IteratorStorey.State.Uninitialized);

				var field = storey.PC.Spec;
				if (storey.MemberName.IsGeneric) {
					field = MemberCache.GetMember (Storey.Instance.Type, field);
				}

				ec.Emit (OpCodes.Stfld, field);
			}
		}

		public void EmitDispose (EmitContext ec)
		{
			Label end = ec.DefineLabel ();

			Label[] labels = null;
			int n_resume_points = resume_points == null ? 0 : resume_points.Count;
			for (int i = 0; i < n_resume_points; ++i) {
				ResumableStatement s = resume_points[i];
				Label ret = s.PrepareForDispose (ec, end);
				if (ret.Equals (end) && labels == null)
					continue;
				if (labels == null) {
					labels = new Label[resume_points.Count + 1];
					for (int j = 0; j <= i; ++j)
						labels[j] = end;
				}

				labels[i + 1] = ret;
			}

			if (labels != null) {
				current_pc = ec.GetTemporaryLocal (ec.BuiltinTypes.UInt);
				ec.EmitThis ();
				ec.Emit (OpCodes.Ldfld, storey.PC.Spec);
				ec.Emit (OpCodes.Stloc, current_pc);
			}

			ec.EmitThis ();
			ec.EmitInt (1);
			ec.Emit (OpCodes.Stfld, ((IteratorStorey) storey).DisposingField.Spec);

			ec.EmitThis ();
			ec.EmitInt ((int) IteratorStorey.State.After);
			ec.Emit (OpCodes.Stfld, storey.PC.Spec);

			if (labels != null) {
				//SymbolWriter.StartIteratorDispatcher (ec.ig);
				ec.Emit (OpCodes.Ldloc, current_pc);
				ec.Emit (OpCodes.Switch, labels);
				//SymbolWriter.EndIteratorDispatcher (ec.ig);

				foreach (ResumableStatement s in resume_points)
					s.EmitForDispose (ec, current_pc, end, true);
			}

			ec.MarkLabel (end);
		}

		public override void EmitStatement (EmitContext ec)
		{
			throw new NotImplementedException ();
		}

		public override void InjectYield (EmitContext ec, Expression expr, int resume_pc, bool unwind_protect, Label resume_point)
		{
			// Store the new value into current
			var fe = new FieldExpr (((IteratorStorey) storey).CurrentField, loc);
			fe.InstanceExpression = new CompilerGeneratedThis (storey.CurrentType, loc);
			fe.EmitAssign (ec, expr, false, false);

			base.InjectYield (ec, expr, resume_pc, unwind_protect, resume_point);

			EmitLeave (ec, unwind_protect);

			ec.MarkLabel (resume_point);
		}

		protected override BlockContext CreateBlockContext (ResolveContext rc)
		{
			var bc = base.CreateBlockContext (rc);
			bc.StartFlowBranching (this, rc.CurrentBranching);
			return bc;
		}

		public static void CreateIterator (IMethodData method, TypeContainer parent, Modifiers modifiers)
		{
			bool is_enumerable;
			TypeSpec iterator_type;

			TypeSpec ret = method.ReturnType;
			if (ret == null)
				return;

			if (!CheckType (ret, parent, out iterator_type, out is_enumerable)) {
				parent.Compiler.Report.Error (1624, method.Location,
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
					parent.Compiler.Report.Error (1623, p.Location,
						"Iterators cannot have ref or out parameters");
					return;
				}

				if (p is ArglistParameter) {
					parent.Compiler.Report.Error (1636, method.Location,
						"__arglist is not allowed in parameter list of iterators");
					return;
				}

				if (parameters.Types [i].IsPointer) {
					parent.Compiler.Report.Error (1637, p.Location,
						"Iterators cannot have unsafe parameters or yield types");
					return;
				}
			}

			if ((modifiers & Modifiers.UNSAFE) != 0) {
				parent.Compiler.Report.Error (1629, method.Location, "Unsafe code may not appear in iterators");
			}

			method.Block.WrapIntoIterator (method, parent, iterator_type, is_enumerable);
		}

		static bool CheckType (TypeSpec ret, TypeContainer parent, out TypeSpec original_iterator_type, out bool is_enumerable)
		{
			original_iterator_type = null;
			is_enumerable = false;

			if (ret.BuiltinType == BuiltinTypeSpec.Type.IEnumerable) {
				original_iterator_type = parent.Compiler.BuiltinTypes.Object;
				is_enumerable = true;
				return true;
			}
			if (ret.BuiltinType == BuiltinTypeSpec.Type.IEnumerator) {
				original_iterator_type = parent.Compiler.BuiltinTypes.Object;
				is_enumerable = false;
				return true;
			}

			InflatedTypeSpec inflated = ret as InflatedTypeSpec;
			if (inflated == null)
				return false;

			var member_definition = inflated.MemberDefinition;
			PredefinedType ptype = parent.Module.PredefinedTypes.IEnumerableGeneric;

			if (ptype.Define () && ptype.TypeSpec.MemberDefinition == member_definition) {
				original_iterator_type = inflated.TypeArguments[0];
				is_enumerable = true;
				return true;
			}

			ptype = parent.Module.PredefinedTypes.IEnumeratorGeneric;
			if (ptype.Define () && ptype.TypeSpec.MemberDefinition == member_definition) {
				original_iterator_type = inflated.TypeArguments[0];
				is_enumerable = false;
				return true;
			}

			return false;
		}
	}
}

