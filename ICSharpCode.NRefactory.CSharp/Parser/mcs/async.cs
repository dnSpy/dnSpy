//
// async.cs: Asynchronous functions
//
// Author:
//   Marek Safar (marek.safar@gmail.com)
//
// Dual licensed under the terms of the MIT X11 or GNU GPL
//
// Copyright 2011 Novell, Inc.
// Copyright 2011 Xamarin Inc.
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections;

#if STATIC
using IKVM.Reflection.Emit;
#else
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

		protected override Expression DoResolve (ResolveContext rc)
		{
			if (rc.HasSet (ResolveContext.Options.LockScope)) {
				rc.Report.Error (1996, loc,
					"The `await' operator cannot be used in the body of a lock statement");
			}

			if (rc.HasSet (ResolveContext.Options.ExpressionTreeConversion)) {
				rc.Report.Error (1989, loc, "An expression tree cannot contain an await operator");
				return null;
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
			stmt.Emit (ec);
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

		public override object Accept (StructuralVisitor visitor)
		{
			return visitor.Visit (this);
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
				Error_OperatorCannotBeApplied (rc, type);
			}

			protected override void Error_OperatorCannotBeApplied (ResolveContext rc, TypeSpec type)
			{
				rc.Report.Error (4001, loc, "Cannot await `{0}' expression", type.GetSignatureForError ());
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
		PropertySpec is_completed;
		MethodSpec on_completed;
		MethodSpec get_result;
		TypeSpec type;
		TypeSpec result_type;

		public AwaitStatement (Expression expr, Location loc)
			: base (expr, loc)
		{
		}

		#region Properties

		bool IsDynamic {
			get {
				return is_completed == null;
			}
		}

		public TypeSpec Type {
			get {
				return type;
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
			GetResultExpression (ec).Emit (ec);
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
			} else {
				var mg_result = MethodGroupExpr.CreatePredefined (get_result, fe_awaiter.Type, loc);
				mg_result.InstanceExpression = fe_awaiter;

				return new GetResultInvocation (mg_result, new Arguments (0));
			}
		}

		public void EmitPrologue (EmitContext ec)
		{
			var fe_awaiter = new FieldExpr (awaiter, loc);
			fe_awaiter.InstanceExpression = new CompilerGeneratedThis (ec.CurrentType, loc);

			//
			// awaiter = expr.GetAwaiter ();
			//
			fe_awaiter.EmitAssign (ec, expr, false, false);

			Label skip_continuation = ec.DefineLabel ();

			Expression completed_expr;
			if (IsDynamic) {
				var rc = new ResolveContext (ec.MemberContext);

				Arguments dargs = new Arguments (1);
				dargs.Add (new Argument (fe_awaiter));
				completed_expr = new DynamicMemberBinder ("IsCompleted", dargs, loc).Resolve (rc);
			} else {
				var pe = PropertyExpr.CreatePredefined (is_completed, loc);
				pe.InstanceExpression = fe_awaiter;
				completed_expr = pe;
			}

			completed_expr.EmitBranchable (ec, skip_continuation, true);

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
			var cont_field = storey.EmitContinuationInitialization (ec);

			var args = new Arguments (1);
			args.Add (new Argument (cont_field));

			if (IsDynamic) {
				var rc = new ResolveContext (ec.MemberContext);
				var mg_expr = new Invocation (new MemberAccess (fe_awaiter, "OnCompleted"), args).Resolve (rc);

				ExpressionStatement es = (ExpressionStatement) mg_expr;
				es.EmitStatement (ec);
			} else {
				var mg_completed = MethodGroupExpr.CreatePredefined (on_completed, fe_awaiter.Type, loc);
				mg_completed.InstanceExpression = fe_awaiter;

				//
				// awaiter.OnCompleted (continuation);
				//
				mg_completed.EmitCall (ec, args);
			}

			// Return ok
			machine_initializer.EmitLeave (ec, unwind_protect);

			ec.MarkLabel (resume_point);
			ec.MarkLabel (skip_continuation);
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

		void Error_WrongAwaiterPattern (ResolveContext rc, TypeSpec awaiter)
		{
			rc.Report.Error (4011, loc, "The awaiter type `{0}' must have suitable IsCompleted, OnCompleted, and GetResult members",
				awaiter.GetSignatureForError ());
		}

		public override bool Resolve (BlockContext bc)
		{
			if (!base.Resolve (bc))
				return false;

			Arguments args = new Arguments (0);

			type = expr.Type;

			//
			// The await expression is of dynamic type
			//
			if (type.BuiltinType == BuiltinTypeSpec.Type.Dynamic) {
				result_type = type;

				awaiter = ((AsyncTaskStorey) machine_initializer.Storey).AddAwaiter (type, loc);

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
			awaiter = ((AsyncTaskStorey) machine_initializer.Storey).AddAwaiter (awaiter_type, loc);

			expr = ama;

			//
			// Predefined: bool IsCompleted { get; } 
			//
			is_completed = MemberCache.FindMember (awaiter_type, MemberFilter.Property ("IsCompleted", bc.Module.Compiler.BuiltinTypes.Bool),
				BindingRestriction.InstanceOnly) as PropertySpec;

			if (is_completed == null || !is_completed.HasGet) {
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

			result_type = get_result.ReturnType;

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

		public static void Create (IMemberContext context, ParametersBlock block, ParametersCompiled parameters, TypeContainer host, TypeSpec returnType, Location loc)
		{
			for (int i = 0; i < parameters.Count; i++) {
				Parameter p = parameters[i];
				Parameter.Modifier mod = p.ModFlags;
				if ((mod & Parameter.Modifier.ISBYREF) != 0) {
					host.Compiler.Report.Error (1988, p.Location,
						"Async methods cannot have ref or out parameters");
					return;
				}

				if (p is ArglistParameter) {
					host.Compiler.Report.Error (4006, p.Location,
						"__arglist is not allowed in parameter list of async methods");
					return;
				}

				if (parameters.Types[i].IsPointer) {
					host.Compiler.Report.Error (4005, p.Location,
						"Async methods cannot have unsafe parameters");
					return;
				}
			}

			if (!block.HasAwait) {
				host.Compiler.Report.Warning (1998, 1, loc,
					"Async block lacks `await' operator and will run synchronously");
			}

			block.WrapIntoAsyncTask (context, host, returnType);
		}

		protected override BlockContext CreateBlockContext (ResolveContext rc)
		{
			var ctx = base.CreateBlockContext (rc);
			var lambda = rc.CurrentAnonymousMethod as LambdaMethod;
			if (lambda != null)
				return_inference = lambda.ReturnTypeInference;

			ctx.StartFlowBranching (this, rc.CurrentBranching);
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

			var move_next_entry = storey.StateMachineMethod.Spec;
			if (storey.MemberName.Arity > 0) {
				move_next_entry = MemberCache.GetMember (storey.Instance.Type, move_next_entry);
			}

			ec.Emit (OpCodes.Call, move_next_entry);

			//
			// Emits return <async-storey-instance>.$builder.Task;
			//
			if (storey.Task != null) {
				var builder_field = storey.Builder.Spec;
				var task_get = storey.Task.Get;

				if (storey.MemberName.Arity > 0) {
					builder_field = MemberCache.GetMember (storey.Instance.Type, builder_field);
					task_get = MemberCache.GetMember (builder_field.MemberType, task_get);
				}

				var pe_task = new PropertyExpr (storey.Task, loc) {
					InstanceExpression = new FieldExpr (builder_field, loc) {
						InstanceExpression = storey.Instance
					},
					Getter = task_get
				};

				pe_task.Emit (ec);
			}

			ec.Emit (OpCodes.Ret);
		}
	}

	class AsyncTaskStorey : StateMachine
	{
		int awaiters;
		Field builder, continuation;
		readonly TypeSpec return_type;
		MethodSpec set_result;
		MethodSpec set_exception;
		PropertySpec task;
		LocalVariable hoisted_return;
		int locals_captured;
		Dictionary<TypeSpec, List<StackField>> stack_fields;
		TypeSpec action;

		public AsyncTaskStorey (IMemberContext context, AsyncInitializer initializer, TypeSpec type)
			: base (initializer.OriginalBlock, initializer.Host,context.CurrentMemberDefinition as MemberBase, context.CurrentTypeParameters, "async")
		{
			return_type = type;
		}

		#region Properties

		public Field Builder {
			get {
				return builder;
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

		public Field AddAwaiter (TypeSpec type, Location loc)
		{
			return AddCapturedVariable ("$awaiter" + awaiters++.ToString ("X"), type);
		}

		public StackField AddCapturedLocalVariable (TypeSpec type)
		{
			if (mutator != null)
				type = mutator.Mutate (type);

			List<StackField> existing_fields = null;
			if (stack_fields == null) {
				stack_fields = new Dictionary<TypeSpec, List<StackField>> ();
			} else if (stack_fields.TryGetValue (type, out existing_fields)) {
				foreach (var f in existing_fields) {
					if (f.CanBeReused) {
						f.CanBeReused = false;
						return f;
					}
				}
			}

			const Modifiers mod = Modifiers.COMPILER_GENERATED | Modifiers.PRIVATE;
			var field = new StackField (this, new TypeExpression (type, Location), mod, new MemberName ("<s>$" + locals_captured++.ToString ("X"), Location));
			AddField (field);

			field.Define ();

			if (existing_fields == null) {
				existing_fields = new List<StackField> ();
				stack_fields.Add (type, existing_fields);
			}

			existing_fields.Add (field);

			return field;
		}

		protected override bool DoDefineMembers ()
		{
			action = Module.PredefinedTypes.Action.Resolve ();

			PredefinedType builder_type;
			PredefinedMember<MethodSpec> bf;
			PredefinedMember<MethodSpec> sr;
			PredefinedMember<MethodSpec> se;
			bool has_task_return_type = false;
			var pred_members = Module.PredefinedMembers;

			if (return_type.Kind == MemberKind.Void) {
				builder_type = Module.PredefinedTypes.AsyncVoidMethodBuilder;
				bf = pred_members.AsyncVoidMethodBuilderCreate;
				sr = pred_members.AsyncVoidMethodBuilderSetResult;
				se = pred_members.AsyncVoidMethodBuilderSetException;
			} else if (return_type == Module.PredefinedTypes.Task.TypeSpec) {
				builder_type = Module.PredefinedTypes.AsyncTaskMethodBuilder;
				bf = pred_members.AsyncTaskMethodBuilderCreate;
				sr = pred_members.AsyncTaskMethodBuilderSetResult;
				se = pred_members.AsyncTaskMethodBuilderSetException;
				task = pred_members.AsyncTaskMethodBuilderTask.Get ();
			} else {
				builder_type = Module.PredefinedTypes.AsyncTaskMethodBuilderGeneric;
				bf = pred_members.AsyncTaskMethodBuilderGenericCreate;
				sr = pred_members.AsyncTaskMethodBuilderGenericSetResult;
				se = pred_members.AsyncTaskMethodBuilderGenericSetException;
				task = pred_members.AsyncTaskMethodBuilderGenericTask.Get ();
				has_task_return_type = true;
			}

			set_result = sr.Get ();
			set_exception = se.Get ();
			var builder_factory = bf.Get ();
			if (!builder_type.Define () || set_result == null || builder_factory == null || set_exception == null) {
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
				builder_factory = MemberCache.GetMember<MethodSpec> (bt, builder_factory);
				set_result = MemberCache.GetMember<MethodSpec> (bt, set_result);
				set_exception = MemberCache.GetMember<MethodSpec> (bt, set_exception);

				if (task != null)
					task = MemberCache.GetMember<PropertySpec> (bt, task);
			}

			builder = AddCompilerGeneratedField ("$builder", new TypeExpression (bt, Location));

			if (!base.DoDefineMembers ())
				return false;

			var block = instance_constructors[0].Block;

			var mg = MethodGroupExpr.CreatePredefined (builder_factory, bt, Location);
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

		public Expression EmitContinuationInitialization (EmitContext ec)
		{
			//
			// When more than 1 awaiter has been used in the block we
			// introduce class scope field to cache continuation delegate
			//
			if (awaiters > 1) {
				if (continuation == null) {
					continuation = AddCompilerGeneratedField ("$continuation", new TypeExpression (action, Location), true);
					continuation.Define ();
				}

				var fexpr = new FieldExpr (continuation, Location);
				fexpr.InstanceExpression = new CompilerGeneratedThis (CurrentType, Location);

				//
				// if ($continuation == null)
				//    $continuation = new Action (MoveNext);
				//
				fexpr.Emit (ec);

				var skip_cont_init = ec.DefineLabel ();
				ec.Emit (OpCodes.Brtrue_S, skip_cont_init);

				ec.EmitThis ();
				EmitActionLoad (ec);
				ec.Emit (OpCodes.Stfld, continuation.Spec);
				ec.MarkLabel (skip_cont_init);

				return fexpr;
			}

			//
			// Otherwise simply use temporary local variable
			//
			var field = LocalVariable.CreateCompilerGenerated (action, OriginalSourceBlock, Location);
			EmitActionLoad (ec);
			field.EmitAssign (ec);
			return new LocalVariableReference (field, Location);
		}

		void EmitActionLoad (EmitContext ec)
		{
			ec.EmitThis ();
			ec.Emit (OpCodes.Ldftn, StateMachineMethod.Spec);
			ec.Emit (OpCodes.Newobj, (MethodSpec) MemberCache.FindMember (action, MemberFilter.Constructor (null), BindingRestriction.DeclaredOnly));
		}

		public void EmitSetException (EmitContext ec, LocalVariableReference exceptionVariable)
		{
			//
			// $builder.SetException (Exception)
			//
			var mg = MethodGroupExpr.CreatePredefined (set_exception, set_exception.DeclaringType, Location);
			mg.InstanceExpression = new FieldExpr (Builder, Location) {
				InstanceExpression = new CompilerGeneratedThis (ec.CurrentType, Location)
			};

			Arguments args = new Arguments (1);
			args.Add (new Argument (exceptionVariable));

			mg.EmitCall (ec, args);
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

	class StackField : Field
	{
		public StackField (DeclSpace parent, FullNamedExpression type, Modifiers mod, MemberName name)
			: base (parent, type, mod, name, null)
		{
		}

		public bool CanBeReused { get; set; }
	}

	class StackFieldExpr : FieldExpr
	{
		public StackFieldExpr (Field field)
			: base (field, Location.Null)
		{
		}

		public override void Emit (EmitContext ec)
		{
			base.Emit (ec);

			var field = (StackField) spec.MemberDefinition;
			field.CanBeReused = true;
		}
	}
}
