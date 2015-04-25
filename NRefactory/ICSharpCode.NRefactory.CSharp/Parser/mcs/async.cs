//
// async.cs: Asynchronous functions
//
// Author:
//   Marek Safar (marek.safar@gmail.com)
//
// Dual licensed under the terms of the MIT X11 or GNU GPL
//
// Copyright 2011 Novell, Inc.
// Copyright 2011-2012 Xamarin Inc.
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections;

#if STATIC
using IKVM.Reflection;
using IKVM.Reflection.Emit;
#else
using System.Reflection;
using System.Reflection.Emit;
#endif

namespace Mono.CSharp
{
	public class Await : ExpressionStatement
	{
		Expression expr;
		AwaitStatement stmt;
		
		public Expression Expression {
			get {
				return expr;
			}
		}
		
		public Await (Expression expr, Location loc)
		{
			this.expr = expr;
			this.loc = loc;
		}

		public Expression Expr {
			get {
				return expr;
			}
		}

		public AwaitStatement Statement {
			get {
				return stmt;
			}
		}

		protected override void CloneTo (CloneContext clonectx, Expression target)
		{
			var t = (Await) target;

			t.expr = expr.Clone (clonectx);
		}

		public override Expression CreateExpressionTree (ResolveContext ec)
		{
			throw new NotImplementedException ("ET");
		}

		public override bool ContainsEmitWithAwait ()
		{
			return true;
		}

		public override void FlowAnalysis (FlowAnalysisContext fc)
		{
			stmt.Expr.FlowAnalysis (fc);

			stmt.RegisterResumePoint ();
		}

		protected override Expression DoResolve (ResolveContext rc)
		{
			if (rc.HasSet (ResolveContext.Options.LockScope)) {
				rc.Report.Error (1996, loc,
					"The `await' operator cannot be used in the body of a lock statement");
			}

			if (rc.IsUnsafe) {
				rc.Report.Error (4004, loc,
					"The `await' operator cannot be used in an unsafe context");
			}

			var bc = (BlockContext) rc;

			stmt = new AwaitStatement (expr, loc);
			if (!stmt.Resolve (bc))
				return null;

			type = stmt.ResultType;
			eclass = ExprClass.Variable;
			return this;
		}

		public override void Emit (EmitContext ec)
		{
			stmt.EmitPrologue (ec);

			using (ec.With (BuilderContext.Options.OmitDebugInfo, true)) {
				stmt.Emit (ec);
			}
		}
		
		public override Expression EmitToField (EmitContext ec)
		{
			stmt.EmitPrologue (ec);
			return stmt.GetResultExpression (ec);
		}
		
		public void EmitAssign (EmitContext ec, FieldExpr field)
		{
			stmt.EmitPrologue (ec);
			field.InstanceExpression.Emit (ec);
			stmt.Emit (ec);
		}

		public override void EmitStatement (EmitContext ec)
		{
			stmt.EmitStatement (ec);
		}

		public override void MarkReachable (Reachability rc)
		{
			base.MarkReachable (rc);
			stmt.MarkReachable (rc);
		}

		public override object Accept (StructuralVisitor visitor)
		{
			return visitor.Visit (this);
		}
	}

	public class AwaitStatement : YieldStatement<AsyncInitializer>
	{
		public sealed class AwaitableMemberAccess : MemberAccess
		{
			public AwaitableMemberAccess (Expression expr)
				: base (expr, "GetAwaiter")
			{
			}

			public bool ProbingMode { get; set; }

			protected override void Error_TypeDoesNotContainDefinition (ResolveContext rc, TypeSpec type, string name)
			{
				Error_OperatorCannotBeApplied (rc, type);
			}

			protected override void Error_OperatorCannotBeApplied (ResolveContext rc, TypeSpec type)
			{
				if (ProbingMode)
					return;

				var invocation = LeftExpression as Invocation;
				if (invocation != null && invocation.MethodGroup != null && (invocation.MethodGroup.BestCandidate.Modifiers & Modifiers.ASYNC) != 0) {
					rc.Report.Error (4008, loc, "Cannot await void method `{0}'. Consider changing method return type to `Task'",
						invocation.GetSignatureForError ());
				} else {
					rc.Report.Error (4001, loc, "Cannot await `{0}' expression", type.GetSignatureForError ());
				}
			}
		}

		sealed class GetResultInvocation : Invocation
		{
			public GetResultInvocation (MethodGroupExpr mge, Arguments arguments)
				: base (null, arguments)
			{
				mg = mge;
				type = mg.BestCandidateReturnType;
			}

			public override Expression EmitToField (EmitContext ec)
			{
				return this;
			}
		}

		Field awaiter;
		AwaiterDefinition awaiter_definition;
		TypeSpec type;
		TypeSpec result_type;

		public AwaitStatement (Expression expr, Location loc)
			: base (expr, loc)
		{
			unwind_protect = true;
		}

		#region Properties

		bool IsDynamic {
			get {
				return awaiter_definition == null;
			}
		}

		public TypeSpec ResultType {
			get {
				return result_type;
			}
		}

		#endregion

		protected override void DoEmit (EmitContext ec)
		{
			using (ec.With (BuilderContext.Options.OmitDebugInfo, true)) {
				GetResultExpression (ec).Emit (ec);
			}
		}

		public Expression GetResultExpression (EmitContext ec)
		{
			var fe_awaiter = new FieldExpr (awaiter, loc);
			fe_awaiter.InstanceExpression = new CompilerGeneratedThis (ec.CurrentType, loc);

			//
			// result = awaiter.GetResult ();
			//
			if (IsDynamic) {
				var rc = new ResolveContext (ec.MemberContext);
				return new Invocation (new MemberAccess (fe_awaiter, "GetResult"), new Arguments (0)).Resolve (rc);
			}
			
			var mg_result = MethodGroupExpr.CreatePredefined (awaiter_definition.GetResult, fe_awaiter.Type, loc);
			mg_result.InstanceExpression = fe_awaiter;

			return new GetResultInvocation (mg_result, new Arguments (0));
		}

		public void EmitPrologue (EmitContext ec)
		{
			awaiter = ((AsyncTaskStorey) machine_initializer.Storey).AddAwaiter (expr.Type);

			var fe_awaiter = new FieldExpr (awaiter, loc);
			fe_awaiter.InstanceExpression = new CompilerGeneratedThis (ec.CurrentType, loc);

			Label skip_continuation = ec.DefineLabel ();

			using (ec.With (BuilderContext.Options.OmitDebugInfo, true)) {
				//
				// awaiter = expr.GetAwaiter ();
				//
				fe_awaiter.EmitAssign (ec, expr, false, false);

				Expression completed_expr;
				if (IsDynamic) {
					var rc = new ResolveContext (ec.MemberContext);

					Arguments dargs = new Arguments (1);
					dargs.Add (new Argument (fe_awaiter));
					completed_expr = new DynamicMemberBinder ("IsCompleted", dargs, loc).Resolve (rc);

					dargs = new Arguments (1);
					dargs.Add (new Argument (completed_expr));
					completed_expr = new DynamicConversion (ec.Module.Compiler.BuiltinTypes.Bool, 0, dargs, loc).Resolve (rc);
				} else {
					var pe = PropertyExpr.CreatePredefined (awaiter_definition.IsCompleted, loc);
					pe.InstanceExpression = fe_awaiter;
					completed_expr = pe;
				}

				completed_expr.EmitBranchable (ec, skip_continuation, true);
			}

			base.DoEmit (ec);

			//
			// The stack has to be empty before calling await continuation. We handle this
			// by lifting values which would be left on stack into class fields. The process
			// is quite complicated and quite hard to test because any expression can possibly
			// leave a value on the stack.
			//
			// Following assert fails when some of expression called before is missing EmitToField
			// or parent expression fails to find await in children expressions
			//
			ec.AssertEmptyStack ();

			var storey = (AsyncTaskStorey) machine_initializer.Storey;
			if (IsDynamic) {
				storey.EmitAwaitOnCompletedDynamic (ec, fe_awaiter);
			} else {
				storey.EmitAwaitOnCompleted (ec, fe_awaiter);
			}

			// Return ok
			machine_initializer.EmitLeave (ec, unwind_protect);

			ec.MarkLabel (resume_point);
			ec.MarkLabel (skip_continuation);
		}

		public void EmitStatement (EmitContext ec)
		{
			EmitPrologue (ec);
			DoEmit (ec);

			awaiter.IsAvailableForReuse = true;

			if (ResultType.Kind != MemberKind.Void)
				ec.Emit (OpCodes.Pop);
		}

		void Error_WrongAwaiterPattern (ResolveContext rc, TypeSpec awaiter)
		{
			rc.Report.Error (4011, loc, "The awaiter type `{0}' must have suitable IsCompleted and GetResult members",
				awaiter.GetSignatureForError ());
		}

		public override bool Resolve (BlockContext bc)
		{
			if (bc.CurrentBlock is Linq.QueryBlock) {
				bc.Report.Error (1995, loc,
					"The `await' operator may only be used in a query expression within the first collection expression of the initial `from' clause or within the collection expression of a `join' clause");
				return false;
			}

			if (!base.Resolve (bc))
				return false;

			type = expr.Type;
			Arguments args = new Arguments (0);

			//
			// The await expression is of dynamic type
			//
			if (type.BuiltinType == BuiltinTypeSpec.Type.Dynamic) {
				result_type = type;
				expr = new Invocation (new MemberAccess (expr, "GetAwaiter"), args).Resolve (bc);
				return true;
			}

			//
			// Check whether the expression is awaitable
			//
			Expression ama = new AwaitableMemberAccess (expr).Resolve (bc);
			if (ama == null)
				return false;

			var errors_printer = new SessionReportPrinter ();
			var old = bc.Report.SetPrinter (errors_printer);
			ama = new Invocation (ama, args).Resolve (bc);
			bc.Report.SetPrinter (old);

			if (errors_printer.ErrorsCount > 0 || !MemberAccess.IsValidDotExpression (ama.Type)) {
				bc.Report.Error (1986, expr.Location,
					"The `await' operand type `{0}' must have suitable GetAwaiter method",
					expr.Type.GetSignatureForError ());

				return false;
			}

			var awaiter_type = ama.Type;

			awaiter_definition = bc.Module.GetAwaiter (awaiter_type);

			if (!awaiter_definition.IsValidPattern) {
				Error_WrongAwaiterPattern (bc, awaiter_type);
				return false;
			}

			if (!awaiter_definition.INotifyCompletion) {
				bc.Report.Error (4027, loc, "The awaiter type `{0}' must implement interface `{1}'",
					awaiter_type.GetSignatureForError (), bc.Module.PredefinedTypes.INotifyCompletion.GetSignatureForError ());
				return false;
			}

			expr = ama;
			result_type = awaiter_definition.GetResult.ReturnType;

			return true;
		}
	}

	class AsyncInitializerStatement : StatementExpression
	{
		public AsyncInitializerStatement (AsyncInitializer expr)
			: base (expr)
		{
		}

		protected override bool DoFlowAnalysis (FlowAnalysisContext fc)
		{
			base.DoFlowAnalysis (fc);

			var init = (AsyncInitializer) Expr;
			var res = !init.Block.HasReachableClosingBrace;
			var storey = (AsyncTaskStorey) init.Storey;

			if (storey.ReturnType.IsGenericTask)
				return res;

			return true;
		}

		public override Reachability MarkReachable (Reachability rc)
		{
			if (!rc.IsUnreachable)
				reachable = true;

			var init = (AsyncInitializer) Expr;
			rc = init.Block.MarkReachable (rc);

			var storey = (AsyncTaskStorey) init.Storey;

			//
			// Explicit return is required for Task<T> state machine
			//
			if (storey.ReturnType != null && storey.ReturnType.IsGenericTask)
				return rc;

		    return Reachability.CreateUnreachable ();
		}
	}

	public class AsyncInitializer : StateMachineInitializer
	{
		TypeInferenceContext return_inference;

		public AsyncInitializer (ParametersBlock block, TypeDefinition host, TypeSpec returnType)
			: base (block, host, returnType)
		{
		}

		#region Properties

		public override string ContainerType {
			get {
				return "async state machine block";
			}
		}

		public TypeSpec DelegateType {
			get; set;
		}

		public StackFieldExpr HoistedReturnState {
			get; set;
		}

		public override bool IsIterator {
			get {
				return false;
			}
		}

		public TypeInferenceContext ReturnTypeInference {
			get {
				return return_inference;
			}
		}

		#endregion

		protected override BlockContext CreateBlockContext (BlockContext bc)
		{
			var ctx = base.CreateBlockContext (bc);
			var am = bc.CurrentAnonymousMethod as AnonymousMethodBody;
			if (am != null)
				return_inference = am.ReturnTypeInference;

			ctx.Set (ResolveContext.Options.TryScope);

			return ctx;
		}

		public override void Emit (EmitContext ec)
		{
			throw new NotImplementedException ();
		}

		public void EmitCatchBlock (EmitContext ec)
		{
			var catch_value = LocalVariable.CreateCompilerGenerated (ec.Module.Compiler.BuiltinTypes.Exception, block, Location);

			ec.BeginCatchBlock (catch_value.Type);
			catch_value.EmitAssign (ec);

			ec.EmitThis ();
			ec.EmitInt ((int) IteratorStorey.State.After);
			ec.Emit (OpCodes.Stfld, storey.PC.Spec);

			((AsyncTaskStorey) Storey).EmitSetException (ec, new LocalVariableReference (catch_value, Location));

			ec.Emit (OpCodes.Leave, move_next_ok);
			ec.EndExceptionBlock ();

		}

		protected override void EmitMoveNextEpilogue (EmitContext ec)
		{
			var storey = (AsyncTaskStorey) Storey;
			storey.EmitSetResult (ec);
		}

		public override void EmitStatement (EmitContext ec)
		{
			var storey = (AsyncTaskStorey) Storey;
			storey.EmitInitializer (ec);
			ec.Emit (OpCodes.Ret);
		}

		public override void MarkReachable (Reachability rc)
		{
			//
			// Reachability has been done in AsyncInitializerStatement
			//
		}
	}

	class AsyncTaskStorey : StateMachine
	{
		int awaiters;
		Field builder;
		readonly TypeSpec return_type;
		MethodSpec set_result;
		MethodSpec set_exception;
		MethodSpec builder_factory;
		MethodSpec builder_start;
		PropertySpec task;
		int locals_captured;
		Dictionary<TypeSpec, List<Field>> stack_fields;
		Dictionary<TypeSpec, List<Field>> awaiter_fields;

		public AsyncTaskStorey (ParametersBlock block, IMemberContext context, AsyncInitializer initializer, TypeSpec type)
			: base (block, initializer.Host, context.CurrentMemberDefinition as MemberBase, context.CurrentTypeParameters, "async", MemberKind.Struct)
		{
			return_type = type;
			awaiter_fields = new Dictionary<TypeSpec, List<Field>> ();
		}

		#region Properties

		public Expression HoistedReturnValue { get; set; }

		public TypeSpec ReturnType {
			get {
				return return_type;
			}
		}

		public PropertySpec Task {
			get {
				return task;
			}
		}

		protected override TypeAttributes TypeAttr {
			get {
				return base.TypeAttr & ~TypeAttributes.SequentialLayout;
			}
		}

		#endregion

		public Field AddAwaiter (TypeSpec type)
		{
			if (mutator != null)
				type = mutator.Mutate (type);

			List<Field> existing_fields;
			if (awaiter_fields.TryGetValue (type, out existing_fields)) {
				foreach (var f in existing_fields) {
					if (f.IsAvailableForReuse) {
						f.IsAvailableForReuse = false;
						return f;
					}
				}
			}

			var field = AddCompilerGeneratedField ("$awaiter" + awaiters++.ToString ("X"), new TypeExpression (type, Location), true);
			field.Define ();

			if (existing_fields == null) {
				existing_fields = new List<Field> ();
				awaiter_fields.Add (type, existing_fields);
			}

			existing_fields.Add (field);
			return field;
		}

		public Field AddCapturedLocalVariable (TypeSpec type, bool requiresUninitialized = false)
		{
			if (mutator != null)
				type = mutator.Mutate (type);

			List<Field> existing_fields = null;
			if (stack_fields == null) {
				stack_fields = new Dictionary<TypeSpec, List<Field>> ();
			} else if (stack_fields.TryGetValue (type, out existing_fields) && !requiresUninitialized) {
				foreach (var f in existing_fields) {
					if (f.IsAvailableForReuse) {
						f.IsAvailableForReuse = false;
						return f;
					}
				}
			}

			var field = AddCompilerGeneratedField ("$stack" + locals_captured++.ToString ("X"), new TypeExpression (type, Location), true);
			field.Define ();

			if (existing_fields == null) {
				existing_fields = new List<Field> ();
				stack_fields.Add (type, existing_fields);
			}

			existing_fields.Add (field);

			return field;
		}

		protected override bool DoDefineMembers ()
		{
			PredefinedType builder_type;
			PredefinedMember<MethodSpec> bf;
			PredefinedMember<MethodSpec> bs;
			PredefinedMember<MethodSpec> sr;
			PredefinedMember<MethodSpec> se;
			PredefinedMember<MethodSpec> sm;
			bool has_task_return_type = false;
			var pred_members = Module.PredefinedMembers;

			if (return_type.Kind == MemberKind.Void) {
				builder_type = Module.PredefinedTypes.AsyncVoidMethodBuilder;
				bf = pred_members.AsyncVoidMethodBuilderCreate;
				bs = pred_members.AsyncVoidMethodBuilderStart;
				sr = pred_members.AsyncVoidMethodBuilderSetResult;
				se = pred_members.AsyncVoidMethodBuilderSetException;
				sm = pred_members.AsyncVoidMethodBuilderSetStateMachine;
			} else if (return_type == Module.PredefinedTypes.Task.TypeSpec) {
				builder_type = Module.PredefinedTypes.AsyncTaskMethodBuilder;
				bf = pred_members.AsyncTaskMethodBuilderCreate;
				bs = pred_members.AsyncTaskMethodBuilderStart;
				sr = pred_members.AsyncTaskMethodBuilderSetResult;
				se = pred_members.AsyncTaskMethodBuilderSetException;
				sm = pred_members.AsyncTaskMethodBuilderSetStateMachine;
				task = pred_members.AsyncTaskMethodBuilderTask.Get ();
			} else {
				builder_type = Module.PredefinedTypes.AsyncTaskMethodBuilderGeneric;
				bf = pred_members.AsyncTaskMethodBuilderGenericCreate;
				bs = pred_members.AsyncTaskMethodBuilderGenericStart;
				sr = pred_members.AsyncTaskMethodBuilderGenericSetResult;
				se = pred_members.AsyncTaskMethodBuilderGenericSetException;
				sm = pred_members.AsyncTaskMethodBuilderGenericSetStateMachine;
				task = pred_members.AsyncTaskMethodBuilderGenericTask.Get ();
				has_task_return_type = true;
			}

			set_result = sr.Get ();
			set_exception = se.Get ();
			builder_factory = bf.Get ();
			builder_start = bs.Get ();

			var istate_machine = Module.PredefinedTypes.IAsyncStateMachine;
			var set_statemachine = sm.Get ();

			if (!builder_type.Define () || !istate_machine.Define () || set_result == null || builder_factory == null ||
				set_exception == null || set_statemachine == null || builder_start == null ||
				!Module.PredefinedTypes.INotifyCompletion.Define ()) {
				Report.Error (1993, Location,
					"Cannot find compiler required types for asynchronous functions support. Are you targeting the wrong framework version?");
				return base.DoDefineMembers ();
			}

			var bt = builder_type.TypeSpec;

			//
			// Inflate generic Task types
			//
			if (has_task_return_type) {
				var task_return_type = return_type.TypeArguments;
				if (mutator != null)
					task_return_type = mutator.Mutate (task_return_type);

				bt = bt.MakeGenericType (Module, task_return_type);
				set_result = MemberCache.GetMember (bt, set_result);
				set_exception = MemberCache.GetMember (bt, set_exception);
				set_statemachine = MemberCache.GetMember (bt, set_statemachine);

				if (task != null)
					task = MemberCache.GetMember (bt, task);
			}

			builder = AddCompilerGeneratedField ("$builder", new TypeExpression (bt, Location));

			var set_state_machine = new Method (this, new TypeExpression (Compiler.BuiltinTypes.Void, Location),
				Modifiers.COMPILER_GENERATED | Modifiers.DEBUGGER_HIDDEN | Modifiers.PUBLIC,
				new MemberName ("SetStateMachine"),
				ParametersCompiled.CreateFullyResolved (
					new Parameter (new TypeExpression (istate_machine.TypeSpec, Location), "stateMachine", Parameter.Modifier.NONE, null, Location),
					istate_machine.TypeSpec),
				null);

			ToplevelBlock block = new ToplevelBlock (Compiler, set_state_machine.ParameterInfo, Location);
			block.IsCompilerGenerated = true;
			set_state_machine.Block = block;

			Members.Add (set_state_machine);

			if (!base.DoDefineMembers ())
				return false;

			//
			// Fabricates SetStateMachine method
			//
			// public void SetStateMachine (IAsyncStateMachine stateMachine)
			// {
			//    $builder.SetStateMachine (stateMachine);
			// }
			//
			var mg = MethodGroupExpr.CreatePredefined (set_statemachine, bt, Location);
			mg.InstanceExpression = new FieldExpr (builder, Location);

			var param_reference = block.GetParameterReference (0, Location);
			param_reference.Type = istate_machine.TypeSpec;
			param_reference.eclass = ExprClass.Variable;

			var args = new Arguments (1);
			args.Add (new Argument (param_reference));
			set_state_machine.Block.AddStatement (new StatementExpression (new Invocation (mg, args)));

			if (has_task_return_type) {
				HoistedReturnValue = TemporaryVariableReference.Create (bt.TypeArguments [0], StateMachineMethod.Block, Location);
			}

			return true;
		}

		public void EmitAwaitOnCompletedDynamic (EmitContext ec, FieldExpr awaiter)
		{
			var critical = Module.PredefinedTypes.ICriticalNotifyCompletion;
			if (!critical.Define ()) {
				throw new NotImplementedException ();
			}

			var temp_critical = new LocalTemporary (critical.TypeSpec);
			var label_critical = ec.DefineLabel ();
			var label_end = ec.DefineLabel ();

			//
			// Special path for dynamic awaiters
			//
			// var awaiter = this.$awaiter as ICriticalNotifyCompletion;
			// if (awaiter == null) {
			//    var completion = (INotifyCompletion) this.$awaiter;
			//    this.$builder.AwaitOnCompleted (ref completion, ref this);
			// } else {
			//    this.$builder.AwaitUnsafeOnCompleted (ref awaiter, ref this);
			// }
			//
			awaiter.Emit (ec);
			ec.Emit (OpCodes.Isinst, critical.TypeSpec);
			temp_critical.Store (ec);
			temp_critical.Emit (ec);
			ec.Emit (OpCodes.Brtrue_S, label_critical);

			var temp = new LocalTemporary (Module.PredefinedTypes.INotifyCompletion.TypeSpec);
			awaiter.Emit (ec);
			ec.Emit (OpCodes.Castclass, temp.Type);
			temp.Store (ec);
			EmitOnCompleted (ec, temp, false);
			temp.Release (ec);
			ec.Emit (OpCodes.Br_S, label_end);

			ec.MarkLabel (label_critical);

			EmitOnCompleted (ec, temp_critical, true);

			ec.MarkLabel (label_end);

			temp_critical.Release (ec);
		}

		public void EmitAwaitOnCompleted (EmitContext ec, FieldExpr awaiter)
		{
			bool unsafe_version = false;
			if (Module.PredefinedTypes.ICriticalNotifyCompletion.Define ()) {
				unsafe_version = awaiter.Type.ImplementsInterface (Module.PredefinedTypes.ICriticalNotifyCompletion.TypeSpec, false);
			}

			EmitOnCompleted (ec, awaiter, unsafe_version);
		}

		void EmitOnCompleted (EmitContext ec, Expression awaiter, bool unsafeVersion)
		{
			var pm = Module.PredefinedMembers;
			PredefinedMember<MethodSpec> predefined;
			bool has_task_return_type = false;
			if (return_type.Kind == MemberKind.Void) {
				predefined = unsafeVersion ? pm.AsyncVoidMethodBuilderOnCompletedUnsafe : pm.AsyncVoidMethodBuilderOnCompleted;
			} else if (return_type == Module.PredefinedTypes.Task.TypeSpec) {
				predefined = unsafeVersion ? pm.AsyncTaskMethodBuilderOnCompletedUnsafe : pm.AsyncTaskMethodBuilderOnCompleted;
			} else {
				predefined = unsafeVersion ? pm.AsyncTaskMethodBuilderGenericOnCompletedUnsafe : pm.AsyncTaskMethodBuilderGenericOnCompleted;
				has_task_return_type = true;
			}

			var on_completed = predefined.Resolve (Location);
			if (on_completed == null)
				return;

			if (has_task_return_type)
				on_completed = MemberCache.GetMember<MethodSpec> (set_result.DeclaringType, on_completed);

			on_completed = on_completed.MakeGenericMethod (this, awaiter.Type, ec.CurrentType);

			var mg = MethodGroupExpr.CreatePredefined (on_completed, on_completed.DeclaringType, Location);
			mg.InstanceExpression = new FieldExpr (builder, Location) {
				InstanceExpression = new CompilerGeneratedThis (ec.CurrentType, Location)
			};

			var args = new Arguments (2);
			args.Add (new Argument (awaiter, Argument.AType.Ref));
			args.Add (new Argument (new CompilerGeneratedThis (CurrentType, Location), Argument.AType.Ref));
			using (ec.With (BuilderContext.Options.OmitDebugInfo, true)) {
				mg.EmitCall (ec, args, true);
			}
		}

		public void EmitInitializer (EmitContext ec)
		{
			//
			// Some predefined types are missing
			//
			if (builder == null)
				return;

			var instance = (TemporaryVariableReference) Instance;
			var builder_field = builder.Spec;
			if (MemberName.Arity > 0) {
				builder_field = MemberCache.GetMember (instance.Type, builder_field);
			}

			//
			// Inflated factory method when task is of generic type
			//
			if (builder_factory.DeclaringType.IsGeneric) {
				var task_return_type = return_type.TypeArguments;
				var bt = builder_factory.DeclaringType.MakeGenericType (Module, task_return_type);
				builder_factory = MemberCache.GetMember (bt, builder_factory);
				builder_start = MemberCache.GetMember (bt, builder_start);
			}

			//
			// stateMachine.$builder = AsyncTaskMethodBuilder<{task-type}>.Create();
			//
			instance.AddressOf (ec, AddressOp.Store);
			ec.Emit (OpCodes.Call, builder_factory);
			ec.Emit (OpCodes.Stfld, builder_field);

			//
			// stateMachine.$builder.Start<{storey-type}>(ref stateMachine);
			//
			instance.AddressOf (ec, AddressOp.Store);
			ec.Emit (OpCodes.Ldflda, builder_field);
			if (Task != null)
				ec.Emit (OpCodes.Dup);
			instance.AddressOf (ec, AddressOp.Store);
			ec.Emit (OpCodes.Call, builder_start.MakeGenericMethod (Module, instance.Type));

			//
			// Emits return stateMachine.$builder.Task;
			//
			if (Task != null) {
				var task_get = Task.Get;

				if (MemberName.Arity > 0) {
					task_get = MemberCache.GetMember (builder_field.MemberType, task_get);
				}

				var pe_task = new PropertyExpr (Task, Location) {
					InstanceExpression = EmptyExpression.Null,	// Comes from the dup above
					Getter = task_get
				};

				pe_task.Emit (ec);
			}
		}

		public void EmitSetException (EmitContext ec, LocalVariableReference exceptionVariable)
		{
			//
			// $builder.SetException (Exception)
			//
			var mg = MethodGroupExpr.CreatePredefined (set_exception, set_exception.DeclaringType, Location);
			mg.InstanceExpression = new FieldExpr (builder, Location) {
				InstanceExpression = new CompilerGeneratedThis (ec.CurrentType, Location)
			};

			Arguments args = new Arguments (1);
			args.Add (new Argument (exceptionVariable));

			using (ec.With (BuilderContext.Options.OmitDebugInfo, true)) {
				mg.EmitCall (ec, args, true);
			}
		}

		public void EmitSetResult (EmitContext ec)
		{
			//
			// $builder.SetResult ();
			// $builder.SetResult<return-type> (value);
			//
			var mg = MethodGroupExpr.CreatePredefined (set_result, set_result.DeclaringType, Location);
			mg.InstanceExpression = new FieldExpr (builder, Location) {
				InstanceExpression = new CompilerGeneratedThis (ec.CurrentType, Location)
			};

			Arguments args;
			if (HoistedReturnValue == null) {
				args = new Arguments (0);
			} else {
				args = new Arguments (1);
				args.Add (new Argument (HoistedReturnValue));
			}

			using (ec.With (BuilderContext.Options.OmitDebugInfo, true)) {
				mg.EmitCall (ec, args, true);
			}
		}

		protected override TypeSpec[] ResolveBaseTypes (out FullNamedExpression base_class)
		{
			base_type = Compiler.BuiltinTypes.ValueType;
			base_class = null;

			var istate_machine = Module.PredefinedTypes.IAsyncStateMachine;
			if (istate_machine.Define ()) {
				return new[] { istate_machine.TypeSpec };
			}

			return null;
		}
	}

	public class StackFieldExpr : FieldExpr, IExpressionCleanup
	{
		public StackFieldExpr (Field field)
			: base (field, Location.Null)
		{
		}

		public bool IsAvailableForReuse {
			get {
				var field = (Field) spec.MemberDefinition;
				return field.IsAvailableForReuse;
			}
			set {
				var field = (Field) spec.MemberDefinition;
				field.IsAvailableForReuse = value;
			}
		}

		public override void AddressOf (EmitContext ec, AddressOp mode)
		{
			base.AddressOf (ec, mode);

			if (mode == AddressOp.Load) {
				IsAvailableForReuse = true;
			}
		}

		public override void Emit (EmitContext ec)
		{
			base.Emit (ec);

			PrepareCleanup (ec);
		}

		public void EmitLoad (EmitContext ec)
		{
			base.Emit (ec);
		}

		public void PrepareCleanup (EmitContext ec)
		{
			IsAvailableForReuse = true;

			//
			// Release any captured reference type stack variables
			// to imitate real stack behavour and help GC stuff early
			//
			if (TypeSpec.IsReferenceType (type)) {
				ec.AddStatementEpilog (this);
			}
		}

		void IExpressionCleanup.EmitCleanup (EmitContext ec)
		{
			EmitAssign (ec, new NullConstant (type, loc), false, false);
		}
	}
}
