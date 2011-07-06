//
// async.cs: Asynchronous functions
//
// Author:
//   Marek Safar (marek.safar@gmail.com)
//
// Dual licensed under the terms of the MIT X11 or GNU GPL
//
// Copyright 2011 Novell, Inc.
//

using System;
using System.Collections.Generic;
using System.Linq;
#if STATIC
using IKVM.Reflection.Emit;
#else
using System.Reflection.Emit;
#endif

namespace Mono.CSharp
{
	class Await : ExpressionStatement
	{
		Expression expr;
		AwaitStatement stmt;

		public Await (Expression expr, Location loc)
		{
			this.expr = expr;
			this.loc = loc;
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

		protected override Expression DoResolve (ResolveContext rc)
		{
			if (rc.HasSet (ResolveContext.Options.FinallyScope)) {
				rc.Report.Error (1984, loc,
					"The `await' operator cannot be used in the body of a finally clause");
			}

			if (rc.HasSet (ResolveContext.Options.CatchScope)) {
				rc.Report.Error (1985, loc,
					"The `await' operator cannot be used in the body of a catch clause");
			}

			if (rc.HasSet (ResolveContext.Options.LockScope)) {
				rc.Report.Error (1996, loc,
					"The `await' operator cannot be used in the body of a lock statement");
			}

			if (rc.HasSet (ResolveContext.Options.ExpressionTreeConversion)) {
				rc.Report.Error (1989, loc, "An expression tree cannot contain an await operator");
				return null;
			}

			if (rc.IsUnsafe) {
				// TODO: New error code
				rc.Report.Error (-1900, loc,
					"The `await' operator cannot be used in an unsafe context");
			}

			var bc = (BlockContext) rc;

			if (!bc.CurrentBlock.ParametersBlock.IsAsync) {
				// TODO: Should check for existence of await type but
				// what to do with it
			}

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
			stmt.Emit (ec);
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
	}

	class AwaitStatement : YieldStatement<AsyncInitializer>
	{
		sealed class AwaitableMemberAccess : MemberAccess
		{
			public AwaitableMemberAccess (Expression expr)
				: base (expr, "GetAwaiter")
			{
			}

			protected override void Error_TypeDoesNotContainDefinition (ResolveContext rc, TypeSpec type, string name)
			{
				Error_WrongGetAwaiter (rc, loc, type);
			}

			protected override void Error_OperatorCannotBeApplied (ResolveContext rc, TypeSpec type)
			{
				rc.Report.Error (1991, loc, "Cannot await `{0}' expression", type.GetSignatureForError ());
			}
		}

		Field awaiter;
		PropertyExpr is_completed;
		MethodSpec on_completed;
		MethodSpec get_result;
		TypeSpec type;

		public AwaitStatement (Expression expr, Location loc)
			: base (expr, loc)
		{
		}

		#region Properties

		public TypeSpec Type {
			get {
				return type;
			}
		}

		public TypeSpec ResultType {
			get {
				return get_result.ReturnType;
			}
		}

		#endregion

		protected override void DoEmit (EmitContext ec)
		{
			var fe_awaiter = new FieldExpr (awaiter, loc);
			fe_awaiter.InstanceExpression = new CompilerGeneratedThis (ec.CurrentType, loc);

			//
			// result = awaiter.GetResult ();
			//
			var mg_result = MethodGroupExpr.CreatePredefined (get_result, fe_awaiter.Type, loc);
			mg_result.InstanceExpression = fe_awaiter;

			mg_result.EmitCall (ec, new Arguments (0));
		}

		public void EmitPrologue (EmitContext ec)
		{
			var fe_awaiter = new FieldExpr (awaiter, loc);
			fe_awaiter.InstanceExpression = new CompilerGeneratedThis (ec.CurrentType, loc);

			//
			// awaiter = expr.GetAwaiter ();
			//
			fe_awaiter.EmitAssign (ec, expr, false, false);

			is_completed.InstanceExpression = fe_awaiter;
			is_completed.EmitBranchable (ec, resume_point, true);

			var mg_completed = MethodGroupExpr.CreatePredefined (on_completed, fe_awaiter.Type, loc);
			mg_completed.InstanceExpression = fe_awaiter;

			var args = new Arguments (1);
			var storey = (AsyncTaskStorey) machine_initializer.Storey;
			var fe_cont = new FieldExpr (storey.Continuation, loc);
			fe_cont.InstanceExpression = new CompilerGeneratedThis (ec.CurrentType, loc);

			args.Add (new Argument (fe_cont));

			//
			// awaiter.OnCompleted (continuation);
			//
			mg_completed.EmitCall (ec, args);

			base.DoEmit (ec);
		}

		public void EmitStatement (EmitContext ec)
		{
			EmitPrologue (ec);
			Emit (ec);

			if (ResultType.Kind != MemberKind.Void) {
				var storey = (AsyncTaskStorey) machine_initializer.Storey;

			    if (storey.HoistedReturn != null)
			        storey.HoistedReturn.EmitAssign (ec);
				else
					ec.Emit (OpCodes.Pop);
			}
		}

		static void Error_WrongGetAwaiter (ResolveContext rc, Location loc, TypeSpec type)
		{
			rc.Report.Error (1986, loc,
				"The `await' operand type `{0}' must have suitable GetAwaiter method",
				type.GetSignatureForError ());
		}

		void Error_WrongAwaiterPattern (ResolveContext rc, TypeSpec awaiter)
		{
			rc.Report.Error (1999, loc, "The awaiter type `{0}' must have suitable IsCompleted, OnCompleted, and GetResult members",
				awaiter.GetSignatureForError ());
		}

		public override bool Resolve (BlockContext bc)
		{
			if (!base.Resolve (bc))
				return false;

			type = expr.Type;

			//
			// The task result is of dynamic type
			//
			if (expr.Type.BuiltinType == BuiltinTypeSpec.Type.Dynamic)
				throw new NotImplementedException ("dynamic await");

			//
			// Check whether the expression is awaitable
			//
			Expression ama = new AwaitableMemberAccess (expr).Resolve (bc);
			if (ama == null)
				return false;

			Arguments args = new Arguments (0);

			var errors_printer = new SessionReportPrinter ();
			var old = bc.Report.SetPrinter (errors_printer);
			ama = new Invocation (ama, args).Resolve (bc);

			if (errors_printer.ErrorsCount > 0 || !MemberAccess.IsValidDotExpression (ama.Type)) {
				bc.Report.SetPrinter (old);
				Error_WrongGetAwaiter (bc, loc, expr.Type);
				return false;
			}

			var awaiter_type = ama.Type;
			awaiter = ((AsyncTaskStorey) machine_initializer.Storey).AddAwaiter (awaiter_type, loc);
			expr = ama;

			//
			// Predefined: bool IsCompleted { get; } 
			//
			var is_completed_ma = new MemberAccess (expr, "IsCompleted").Resolve (bc);
			if (is_completed_ma != null) {
				is_completed = is_completed_ma as PropertyExpr;
				if (is_completed != null && is_completed.Type.BuiltinType == BuiltinTypeSpec.Type.Bool && is_completed.IsInstance && is_completed.Getter != null) {
					// valid
				} else {
					bc.Report.SetPrinter (old);
					Error_WrongAwaiterPattern (bc, awaiter_type);
					return false;
				}
			}

			bc.Report.SetPrinter (old);

			if (errors_printer.ErrorsCount > 0) {
				Error_WrongAwaiterPattern (bc, awaiter_type);
				return false;
			}

			//
			// Predefined: OnCompleted (Action)
			//
			if (bc.Module.PredefinedTypes.Action.Define ()) {
				on_completed = MemberCache.FindMember (awaiter_type, MemberFilter.Method ("OnCompleted", 0,
					ParametersCompiled.CreateFullyResolved (bc.Module.PredefinedTypes.Action.TypeSpec), bc.Module.Compiler.BuiltinTypes.Void),
					BindingRestriction.InstanceOnly) as MethodSpec;

				if (on_completed == null) {
					Error_WrongAwaiterPattern (bc, awaiter_type);
					return false;
				}
			}

			//
			// Predefined: GetResult ()
			//
			// The method return type is also result type of await expression
			//
			get_result = MemberCache.FindMember (awaiter_type, MemberFilter.Method ("GetResult", 0,
				ParametersCompiled.EmptyReadOnlyParameters, null),
				BindingRestriction.InstanceOnly) as MethodSpec;

			if (get_result == null) {
				Error_WrongAwaiterPattern (bc, awaiter_type);
				return false;
			}

			return true;
		}
	}

	public class AsyncInitializer : StateMachineInitializer
	{
		TypeInferenceContext return_inference;

		public AsyncInitializer (ParametersBlock block, TypeContainer host, TypeSpec returnType)
			: base (block, host, returnType)
		{
		}

		#region Properties

		public override string ContainerType {
			get {
				return "async state machine block";
			}
		}

		public override bool IsIterator {
			get {
				return false;
			}
		}

		public Block OriginalBlock {
			get {
				return block.Parent;
			}
		}

		public TypeInferenceContext ReturnTypeInference {
			get {
				return return_inference;
			}
		}

		#endregion

		public static void Create (ParametersBlock block, ParametersCompiled parameters, TypeContainer host, TypeSpec returnType, Location loc)
		{
			if (returnType != null && returnType.Kind != MemberKind.Void &&
				returnType != host.Module.PredefinedTypes.Task.TypeSpec &&
				!returnType.IsGenericTask) {
				host.Compiler.Report.Error (1983, loc, "The return type of an async method must be void, Task, or Task<T>");
			}

			for (int i = 0; i < parameters.Count; i++) {
				Parameter p = parameters[i];
				Parameter.Modifier mod = p.ModFlags;
				if ((mod & Parameter.Modifier.ISBYREF) != 0) {
					host.Compiler.Report.Error (1988, p.Location,
						"Async methods cannot have ref or out parameters");
					return;
				}

				// TODO:
				if (p is ArglistParameter) {
					host.Compiler.Report.Error (1636, p.Location,
						"__arglist is not allowed in parameter list of iterators");
					return;
				}

				// TODO:
				if (parameters.Types[i].IsPointer) {
					host.Compiler.Report.Error (1637, p.Location,
						"Iterators cannot have unsafe parameters or yield types");
					return;
				}
			}

			// TODO: Warning
			//if (!block.HasAwait) {
			//}

			block.WrapIntoAsyncTask (host, returnType);
		}

		protected override BlockContext CreateBlockContext (ResolveContext rc)
		{
			var ctx = base.CreateBlockContext (rc);
			var lambda = rc.CurrentAnonymousMethod as LambdaMethod;
			if (lambda != null)
				return_inference = lambda.ReturnTypeInference;

			return ctx;
		}

		public override Expression CreateExpressionTree (ResolveContext ec)
		{
			return base.CreateExpressionTree (ec);
		}

		public override void Emit (EmitContext ec)
		{
			throw new NotImplementedException ();
		}

		protected override void EmitMoveNextEpilogue (EmitContext ec)
		{
			var storey = (AsyncTaskStorey) Storey;
			storey.EmitSetResult (ec);
		}

		public override void EmitStatement (EmitContext ec)
		{
			var storey = (AsyncTaskStorey) Storey;
			storey.Instance.Emit (ec);
			ec.Emit (OpCodes.Call, storey.StateMachineMethod.Spec);

			if (storey.Task != null) {
				//
				// async.$builder.Task;
				//
				var pe_task = new PropertyExpr (storey.Task, loc) {
					InstanceExpression = new FieldExpr (storey.Builder, loc) {
						InstanceExpression = storey.Instance
					},
					Getter = storey.Task.Get
				};

				pe_task.Emit (ec);
			}

			ec.Emit (OpCodes.Ret);
		}

		public override void InjectYield (EmitContext ec, Expression expr, int resume_pc, bool unwind_protect, Label resume_point)
		{
			base.InjectYield (ec, expr, resume_pc, unwind_protect, resume_point);
		}
	}

	class AsyncTaskStorey : StateMachine
	{
		int awaiters;
		Field builder, continuation;
		readonly TypeSpec return_type;
		MethodSpec set_result;
		PropertySpec task;
		LocalVariable hoisted_return;

		public AsyncTaskStorey (AsyncInitializer initializer, TypeSpec type)
			: base (initializer.OriginalBlock, initializer.Host, null, null, "async")
		{
			return_type = type;
		}

		public Field AddAwaiter (TypeSpec type, Location loc)
		{
			return AddCompilerGeneratedField ("$awaiter" + awaiters++.ToString ("X"), new TypeExpression (type, loc));
		}

		#region Properties

		public Field Builder {
			get {
				return builder;
			}
		}

		public Field Continuation {
			get {
				return continuation;
			}
		}

		public LocalVariable HoistedReturn {
			get {
				return hoisted_return;
			}
		}

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

		#endregion

		protected override bool DoDefineMembers ()
		{
			var action = Module.PredefinedTypes.Action.Resolve ();
			if (action != null) {
				continuation = AddCompilerGeneratedField ("$continuation", new TypeExpression (action, Location));
				continuation.ModFlags |= Modifiers.READONLY;
			}

			PredefinedType builder_type;
			PredefinedMember<MethodSpec> bf;
			PredefinedMember<MethodSpec> sr;
			bool has_task_return_type = false;
			var pred_members = Module.PredefinedMembers;

			if (return_type.Kind == MemberKind.Void) {
				builder_type = Module.PredefinedTypes.AsyncVoidMethodBuilder;
				bf = pred_members.AsyncVoidMethodBuilderCreate;
				sr = pred_members.AsyncVoidMethodBuilderSetResult;
			} else if (return_type == Module.PredefinedTypes.Task.TypeSpec) {
				builder_type = Module.PredefinedTypes.AsyncTaskMethodBuilder;
				bf = pred_members.AsyncTaskMethodBuilderCreate;
				sr = pred_members.AsyncTaskMethodBuilderSetResult;
				task = pred_members.AsyncTaskMethodBuilderTask.Resolve (Location);
			} else {
				builder_type = Module.PredefinedTypes.AsyncTaskMethodBuilderGeneric;
				bf = pred_members.AsyncTaskMethodBuilderGenericCreate;
				sr = pred_members.AsyncTaskMethodBuilderGenericSetResult;
				task = pred_members.AsyncTaskMethodBuilderGenericTask.Resolve (Location);
				has_task_return_type = true;
			}

			set_result = sr.Resolve (Location);
			var builder_factory = bf.Resolve (Location);
			var bt = builder_type.Resolve ();
			if (bt == null || set_result == null || builder_factory == null)
				return false;

			//
			// Inflate generic Task types
			//
			if (has_task_return_type) {
				bt = bt.MakeGenericType (Module, return_type.TypeArguments);
				builder_factory = MemberCache.GetMember<MethodSpec> (bt, builder_factory);
				set_result = MemberCache.GetMember<MethodSpec> (bt, set_result);

				if (task != null)
					task = MemberCache.GetMember<PropertySpec> (bt, task);
			}

			builder = AddCompilerGeneratedField ("$builder", new TypeExpression (bt, Location));
			builder.ModFlags |= Modifiers.READONLY;

			if (!base.DoDefineMembers ())
				return false;

			MethodGroupExpr mg;
			var block = instance_constructors[0].Block;

			//
			// Initialize continuation with state machine method
			//
			if (continuation != null) {
				var args = new Arguments (1);
				mg = MethodGroupExpr.CreatePredefined (StateMachineMethod.Spec, spec, Location);
				args.Add (new Argument (mg));

				block.AddStatement (
					new StatementExpression (new SimpleAssign (
						new FieldExpr (continuation, Location),
						new NewDelegate (action, args, Location),
						Location
				)));
			}

			mg = MethodGroupExpr.CreatePredefined (builder_factory, bt, Location);
			block.AddStatement (
				new StatementExpression (new SimpleAssign (
				new FieldExpr (builder, Location),
				new Invocation (mg, new Arguments (0)),
				Location)));

			if (has_task_return_type) {
				hoisted_return = LocalVariable.CreateCompilerGenerated (bt.TypeArguments[0], block, Location);
			}

			return true;
		}

		public void EmitSetResult (EmitContext ec)
		{
			//
			// $builder.SetResult ();
			// $builder.SetResult<return-type> (value);
			//
			var mg = MethodGroupExpr.CreatePredefined (set_result, set_result.DeclaringType, Location);
			mg.InstanceExpression = new FieldExpr (Builder, Location) {
				InstanceExpression = new CompilerGeneratedThis (ec.CurrentType, Location)
			};

			Arguments args;
			if (hoisted_return == null) {
				args = new Arguments (0);
			} else {
				args = new Arguments (1);
				args.Add (new Argument (new LocalVariableReference (hoisted_return, Location)));
			}

			mg.EmitCall (ec, args);
		}
	}
}
