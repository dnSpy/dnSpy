//
// statement.cs: Statement representation for the IL tree.
//
// Authors:
//   Miguel de Icaza (miguel@ximian.com)
//   Martin Baulig (martin@ximian.com)
//   Marek Safar (marek.safar@gmail.com)
//
// Copyright 2001, 2002, 2003 Ximian, Inc.
// Copyright 2003, 2004 Novell, Inc.
// Copyright 2011 Xamarin Inc.
//

using System;
using System.Collections.Generic;

#if STATIC
using IKVM.Reflection.Emit;
#else
using System.Reflection.Emit;
#endif

namespace Mono.CSharp {
	
	public abstract class Statement {
		public Location loc;
		protected bool reachable;

		public bool IsUnreachable {
			get {
				return !reachable;
			}
		}
		
		/// <summary>
		///   Resolves the statement, true means that all sub-statements
		///   did resolve ok.
		///  </summary>
		public virtual bool Resolve (BlockContext bc)
		{
			return true;
		}

		/// <summary>
		///   Return value indicates whether all code paths emitted return.
		/// </summary>
		protected abstract void DoEmit (EmitContext ec);

		public virtual void Emit (EmitContext ec)
		{
			ec.Mark (loc);
			DoEmit (ec);

			if (ec.StatementEpilogue != null) {
				ec.EmitEpilogue ();
			}
		}

		//
		// This routine must be overrided in derived classes and make copies
		// of all the data that might be modified if resolved
		// 
		protected abstract void CloneTo (CloneContext clonectx, Statement target);

		public Statement Clone (CloneContext clonectx)
		{
			Statement s = (Statement) this.MemberwiseClone ();
			CloneTo (clonectx, s);
			return s;
		}

		public virtual Expression CreateExpressionTree (ResolveContext ec)
		{
			ec.Report.Error (834, loc, "A lambda expression with statement body cannot be converted to an expresion tree");
			return null;
		}
		
		public virtual object Accept (StructuralVisitor visitor)
		{
			return visitor.Visit (this);
		}

		//
		// Return value indicates whether statement has unreachable end
		//
		protected abstract bool DoFlowAnalysis (FlowAnalysisContext fc);

		public bool FlowAnalysis (FlowAnalysisContext fc)
		{
			if (reachable) {
				fc.UnreachableReported = false;
				var res = DoFlowAnalysis (fc);
				return res;
			}

			//
			// Special handling cases
			//
			if (this is Block) {
				return DoFlowAnalysis (fc);
			}

			if (this is EmptyStatement || loc.IsNull)
				return true;

			if (fc.UnreachableReported)
				return true;

			fc.Report.Warning (162, 2, loc, "Unreachable code detected");
			fc.UnreachableReported = true;
			return true;
		}

		public virtual Reachability MarkReachable (Reachability rc)
		{
			if (!rc.IsUnreachable)
				reachable = true;

			return rc;
		}

		protected void CheckExitBoundaries (BlockContext bc, Block scope)
		{
			if (bc.CurrentBlock.ParametersBlock.Original != scope.ParametersBlock.Original) {
				bc.Report.Error (1632, loc, "Control cannot leave the body of an anonymous method");
				return;
			}

			for (var b = bc.CurrentBlock; b != null && b != scope; b = b.Parent) {
				if (b.IsFinallyBlock) {
					Error_FinallyClauseExit (bc);
					break;
				}
			}
		}

		protected void Error_FinallyClauseExit (BlockContext bc)
		{
			bc.Report.Error (157, loc, "Control cannot leave the body of a finally clause");
		}
	}

	public sealed class EmptyStatement : Statement
	{
		public EmptyStatement (Location loc)
		{
			this.loc = loc;
		}

		public override bool Resolve (BlockContext ec)
		{
			return true;
		}

		public override void Emit (EmitContext ec)
		{
		}

		protected override void DoEmit (EmitContext ec)
		{
			throw new NotSupportedException ();
		}

		protected override bool DoFlowAnalysis (FlowAnalysisContext fc)
		{
			return false;
		}

		protected override void CloneTo (CloneContext clonectx, Statement target)
		{
			// nothing needed.
		}
		
		public override object Accept (StructuralVisitor visitor)
		{
			return visitor.Visit (this);
		}
	}

	public class If : Statement {
		Expression expr;
		public Statement TrueStatement;
		public Statement FalseStatement;

		bool true_returns, false_returns;

		public If (Expression bool_expr, Statement true_statement, Location l)
			: this (bool_expr, true_statement, null, l)
		{
		}

		public If (Expression bool_expr,
			   Statement true_statement,
			   Statement false_statement,
			   Location l)
		{
			this.expr = bool_expr;
			TrueStatement = true_statement;
			FalseStatement = false_statement;
			loc = l;
		}

		public Expression Expr {
			get {
				return this.expr;
			}
		}
		
		public override bool Resolve (BlockContext ec)
		{
			expr = expr.Resolve (ec);

			var ok = TrueStatement.Resolve (ec);

			if (FalseStatement != null) {
				ok &= FalseStatement.Resolve (ec);
			}

			return ok;
		}
		
		protected override void DoEmit (EmitContext ec)
		{
			Label false_target = ec.DefineLabel ();
			Label end;

			//
			// If we're a boolean constant, Resolve() already
			// eliminated dead code for us.
			//
			Constant c = expr as Constant;
			if (c != null){
				c.EmitSideEffect (ec);

				if (!c.IsDefaultValue)
					TrueStatement.Emit (ec);
				else if (FalseStatement != null)
					FalseStatement.Emit (ec);

				return;
			}			
			
			expr.EmitBranchable (ec, false_target, false);
			
			TrueStatement.Emit (ec);

			if (FalseStatement != null){
				bool branch_emitted = false;
				
				end = ec.DefineLabel ();
				if (!true_returns){
					ec.Emit (OpCodes.Br, end);
					branch_emitted = true;
				}

				ec.MarkLabel (false_target);
				FalseStatement.Emit (ec);

				if (branch_emitted)
					ec.MarkLabel (end);
			} else {
				ec.MarkLabel (false_target);
			}
		}

		protected override bool DoFlowAnalysis (FlowAnalysisContext fc)
		{
			expr.FlowAnalysisConditional (fc);

			var da_false = new DefiniteAssignmentBitSet (fc.DefiniteAssignmentOnFalse);

			fc.DefiniteAssignment = fc.DefiniteAssignmentOnTrue;

			var res = TrueStatement.FlowAnalysis (fc);

			if (FalseStatement == null) {
				var c = expr as Constant;
				if (c != null && !c.IsDefaultValue)
					return true_returns;

				if (true_returns)
					fc.DefiniteAssignment = da_false;
				else
					fc.DefiniteAssignment &= da_false;
 
				return false;
			}

			if (true_returns) {
				fc.DefiniteAssignment = da_false;
				return FalseStatement.FlowAnalysis (fc);
			}

			var da_true = fc.DefiniteAssignment;

			fc.DefiniteAssignment = da_false;
			res &= FalseStatement.FlowAnalysis (fc);

			if (!TrueStatement.IsUnreachable) {
				if (false_returns || FalseStatement.IsUnreachable)
					fc.DefiniteAssignment = da_true;
				else
					fc.DefiniteAssignment &= da_true;
			}

			return res;
		}

		public override Reachability MarkReachable (Reachability rc)
		{
			if (rc.IsUnreachable)
				return rc;

			base.MarkReachable (rc);

			var c = expr as Constant;
			if (c != null) {
				bool take = !c.IsDefaultValue;
				if (take) {
					rc = TrueStatement.MarkReachable (rc);
				} else {
					if (FalseStatement != null)
						rc = FalseStatement.MarkReachable (rc);
				}

				return rc;
			}

			var true_rc = TrueStatement.MarkReachable (rc);
			true_returns = true_rc.IsUnreachable;
	
			if (FalseStatement == null)
				return rc;

			var false_rc = FalseStatement.MarkReachable (rc);
			false_returns = false_rc.IsUnreachable;

			return true_rc & false_rc;
		}

		protected override void CloneTo (CloneContext clonectx, Statement t)
		{
			If target = (If) t;

			target.expr = expr.Clone (clonectx);
			target.TrueStatement = TrueStatement.Clone (clonectx);
			if (FalseStatement != null)
				target.FalseStatement = FalseStatement.Clone (clonectx);
		}
		
		public override object Accept (StructuralVisitor visitor)
		{
			return visitor.Visit (this);
		}
	}

	public class Do : LoopStatement
	{
		public Expression expr;
		bool iterator_reachable, end_reachable;

		public Do (Statement statement, BooleanExpression bool_expr, Location doLocation, Location whileLocation)
			: base (statement)
		{
			expr = bool_expr;
			loc = doLocation;
			WhileLocation = whileLocation;
		}

		public Location WhileLocation {
			get; private set;
		}

		public override bool Resolve (BlockContext bc)
		{
			var ok = base.Resolve (bc);

			expr = expr.Resolve (bc);

			return ok;
		}
		
		protected override void DoEmit (EmitContext ec)
		{
			Label loop = ec.DefineLabel ();
			Label old_begin = ec.LoopBegin;
			Label old_end = ec.LoopEnd;
			
			ec.LoopBegin = ec.DefineLabel ();
			ec.LoopEnd = ec.DefineLabel ();
				
			ec.MarkLabel (loop);
			Statement.Emit (ec);
			ec.MarkLabel (ec.LoopBegin);

			// Mark start of while condition
			ec.Mark (WhileLocation);

			//
			// Dead code elimination
			//
			if (expr is Constant) {
				bool res = !((Constant) expr).IsDefaultValue;

				expr.EmitSideEffect (ec);
				if (res)
					ec.Emit (OpCodes.Br, loop);
			} else {
				expr.EmitBranchable (ec, loop, true);
			}
			
			ec.MarkLabel (ec.LoopEnd);

			ec.LoopBegin = old_begin;
			ec.LoopEnd = old_end;
		}

		protected override bool DoFlowAnalysis (FlowAnalysisContext fc)
		{
			var res = Statement.FlowAnalysis (fc);

			expr.FlowAnalysisConditional (fc);

			fc.DefiniteAssignment = fc.DefiniteAssignmentOnFalse;

			if (res && !iterator_reachable)
				return !end_reachable;

			if (!end_reachable) {
				var c = expr as Constant;
				if (c != null && !c.IsDefaultValue)
					return true;
			}

			return false;
		}
		
		public override Reachability MarkReachable (Reachability rc)
		{
			base.MarkReachable (rc);
			
			var body_rc = Statement.MarkReachable (rc);

			if (body_rc.IsUnreachable && !iterator_reachable) {
				expr = new UnreachableExpression (expr);
				return end_reachable ? rc : Reachability.CreateUnreachable ();
			}

			if (!end_reachable) {
				var c = expr as Constant;
				if (c != null && !c.IsDefaultValue)
					return Reachability.CreateUnreachable ();
			}

			return rc;
		}

		protected override void CloneTo (CloneContext clonectx, Statement t)
		{
			Do target = (Do) t;

			target.Statement = Statement.Clone (clonectx);
			target.expr = expr.Clone (clonectx);
		}
		
		public override object Accept (StructuralVisitor visitor)
		{
			return visitor.Visit (this);
		}

		public override void SetEndReachable ()
		{
			end_reachable = true;
		}

		public override void SetIteratorReachable ()
		{
			iterator_reachable = true;
		}
	}

	public class While : LoopStatement
	{
		public Expression expr;
		bool empty, infinite, end_reachable;
		List<DefiniteAssignmentBitSet> end_reachable_das;

		public While (BooleanExpression bool_expr, Statement statement, Location l)
			: base (statement)
		{
			this.expr = bool_expr;
			loc = l;
		}

		public override bool Resolve (BlockContext bc)
		{
			bool ok = true;

			expr = expr.Resolve (bc);
			if (expr == null)
				ok = false;

			var c = expr as Constant;
			if (c != null) {
				empty = c.IsDefaultValue;
				infinite = !empty;
			}

			ok &= base.Resolve (bc);
			return ok;
		}
		
		protected override void DoEmit (EmitContext ec)
		{
			if (empty) {
				expr.EmitSideEffect (ec);
				return;
			}

			Label old_begin = ec.LoopBegin;
			Label old_end = ec.LoopEnd;
			
			ec.LoopBegin = ec.DefineLabel ();
			ec.LoopEnd = ec.DefineLabel ();

			//
			// Inform whether we are infinite or not
			//
			if (expr is Constant) {
				// expr is 'true', since the 'empty' case above handles the 'false' case
				ec.MarkLabel (ec.LoopBegin);

				if (ec.EmitAccurateDebugInfo)
					ec.Emit (OpCodes.Nop);

				expr.EmitSideEffect (ec);
				Statement.Emit (ec);
				ec.Emit (OpCodes.Br, ec.LoopBegin);
					
				//
				// Inform that we are infinite (ie, `we return'), only
				// if we do not `break' inside the code.
				//
				ec.MarkLabel (ec.LoopEnd);
			} else {
				Label while_loop = ec.DefineLabel ();

				ec.Emit (OpCodes.Br, ec.LoopBegin);
				ec.MarkLabel (while_loop);

				Statement.Emit (ec);
			
				ec.MarkLabel (ec.LoopBegin);

				ec.Mark (loc);
				expr.EmitBranchable (ec, while_loop, true);
				
				ec.MarkLabel (ec.LoopEnd);
			}	

			ec.LoopBegin = old_begin;
			ec.LoopEnd = old_end;
		}

		protected override bool DoFlowAnalysis (FlowAnalysisContext fc)
		{
			expr.FlowAnalysisConditional (fc);

			fc.DefiniteAssignment = fc.DefiniteAssignmentOnTrue;
			var da_false = new DefiniteAssignmentBitSet (fc.DefiniteAssignmentOnFalse);

			Statement.FlowAnalysis (fc);

			//
			// Special case infinite while with breaks
			//
			if (end_reachable_das != null) {
				da_false = DefiniteAssignmentBitSet.And (end_reachable_das);
				end_reachable_das = null;
			}

			fc.DefiniteAssignment = da_false;

			if (infinite && !end_reachable)
				return true;

			return false;
		}

		public override Reachability MarkReachable (Reachability rc)
		{
			if (rc.IsUnreachable)
				return rc;

			base.MarkReachable (rc);

			//
			// Special case unreachable while body
			//
			if (empty) {
				Statement.MarkReachable (Reachability.CreateUnreachable ());
				return rc;
			}

			Statement.MarkReachable (rc);

			//
			// When infinite while end is unreachable via break anything what follows is unreachable too
			//
			if (infinite && !end_reachable)
				return Reachability.CreateUnreachable ();

			return rc;
		}

		protected override void CloneTo (CloneContext clonectx, Statement t)
		{
			While target = (While) t;

			target.expr = expr.Clone (clonectx);
			target.Statement = Statement.Clone (clonectx);
		}
		
		public override object Accept (StructuralVisitor visitor)
		{
			return visitor.Visit (this);
		}

		public override void AddEndDefiniteAssignment (FlowAnalysisContext fc)
		{
			if (!infinite)
				return;

			if (end_reachable_das == null)
				end_reachable_das = new List<DefiniteAssignmentBitSet> ();

			end_reachable_das.Add (fc.DefiniteAssignment);
		}

		public override void SetEndReachable ()
		{
			end_reachable = true;
		}
	}

	public class For : LoopStatement
	{
		bool infinite, empty, iterator_reachable, end_reachable;
		List<DefiniteAssignmentBitSet> end_reachable_das;
		
		public For (Location l)
			: base (null)
		{
			loc = l;
		}

		public Statement Initializer {
			get; set;
		}

		public Expression Condition {
			get; set;
		}

		public Statement Iterator {
			get; set;
		}

		public override bool Resolve (BlockContext bc)
		{
			Initializer.Resolve (bc);

			if (Condition != null) {
				Condition = Condition.Resolve (bc);
				var condition_constant = Condition as Constant;
				if (condition_constant != null) {
					if (condition_constant.IsDefaultValue) {
						empty = true;
					} else {
						infinite = true;
					}
				}
			} else {
				infinite = true;
			}

			return base.Resolve (bc) && Iterator.Resolve (bc);
		}

		protected override bool DoFlowAnalysis (FlowAnalysisContext fc)
		{
			Initializer.FlowAnalysis (fc);

			DefiniteAssignmentBitSet da_false;
			if (Condition != null) {
				Condition.FlowAnalysisConditional (fc);
				fc.DefiniteAssignment = fc.DefiniteAssignmentOnTrue;
				da_false = new DefiniteAssignmentBitSet (fc.DefiniteAssignmentOnFalse);
			} else {
				da_false = fc.BranchDefiniteAssignment ();
			}

			Statement.FlowAnalysis (fc);

			Iterator.FlowAnalysis (fc);

			//
			// Special case infinite for with breaks
			//
			if (end_reachable_das != null) {
				da_false = DefiniteAssignmentBitSet.And (end_reachable_das);
				end_reachable_das = null;
			}

			fc.DefiniteAssignment = da_false;

			if (infinite && !end_reachable)
				return true;

			return false;
		}

		public override Reachability MarkReachable (Reachability rc)
		{
			base.MarkReachable (rc);

			Initializer.MarkReachable (rc);

			var body_rc = Statement.MarkReachable (rc);
			if (!body_rc.IsUnreachable || iterator_reachable) {
				Iterator.MarkReachable (rc);
			}

			//
			// When infinite for end is unreachable via break anything what follows is unreachable too
			//
			if (infinite && !end_reachable) {
				return Reachability.CreateUnreachable ();
			}

			return rc;
		}

		protected override void DoEmit (EmitContext ec)
		{
			if (Initializer != null)
				Initializer.Emit (ec);

			if (empty) {
				Condition.EmitSideEffect (ec);
				return;
			}

			Label old_begin = ec.LoopBegin;
			Label old_end = ec.LoopEnd;
			Label loop = ec.DefineLabel ();
			Label test = ec.DefineLabel ();

			ec.LoopBegin = ec.DefineLabel ();
			ec.LoopEnd = ec.DefineLabel ();

			ec.Emit (OpCodes.Br, test);
			ec.MarkLabel (loop);
			Statement.Emit (ec);

			ec.MarkLabel (ec.LoopBegin);
			Iterator.Emit (ec);

			ec.MarkLabel (test);
			//
			// If test is null, there is no test, and we are just
			// an infinite loop
			//
			if (Condition != null) {
				ec.Mark (Condition.Location);

				//
				// The Resolve code already catches the case for
				// Test == Constant (false) so we know that
				// this is true
				//
				if (Condition is Constant) {
					Condition.EmitSideEffect (ec);
					ec.Emit (OpCodes.Br, loop);
				} else {
					Condition.EmitBranchable (ec, loop, true);
				}
				
			} else
				ec.Emit (OpCodes.Br, loop);
			ec.MarkLabel (ec.LoopEnd);

			ec.LoopBegin = old_begin;
			ec.LoopEnd = old_end;
		}

		protected override void CloneTo (CloneContext clonectx, Statement t)
		{
			For target = (For) t;

			if (Initializer != null)
				target.Initializer = Initializer.Clone (clonectx);
			if (Condition != null)
				target.Condition = Condition.Clone (clonectx);
			if (Iterator != null)
				target.Iterator = Iterator.Clone (clonectx);
			target.Statement = Statement.Clone (clonectx);
		}

		public override object Accept (StructuralVisitor visitor)
		{
			return visitor.Visit (this);
		}

		public override void AddEndDefiniteAssignment (FlowAnalysisContext fc)
		{
			if (!infinite)
				return;

			if (end_reachable_das == null)
				end_reachable_das = new List<DefiniteAssignmentBitSet> ();

			end_reachable_das.Add (fc.DefiniteAssignment);
		}

		public override void SetEndReachable ()
		{
			end_reachable = true;
		}

		public override void SetIteratorReachable ()
		{
			iterator_reachable = true;
		}
	}

	public abstract class LoopStatement : Statement
	{
		protected LoopStatement (Statement statement)
		{
			Statement = statement;
		}

		public Statement Statement { get; set; }

		public override bool Resolve (BlockContext bc)
		{
			var prev_loop = bc.EnclosingLoop;
			var prev_los = bc.EnclosingLoopOrSwitch;
			bc.EnclosingLoopOrSwitch = bc.EnclosingLoop = this;
			var ok = Statement.Resolve (bc);
			bc.EnclosingLoopOrSwitch = prev_los;
			bc.EnclosingLoop = prev_loop;

			return ok;
		}

		//
		// Needed by possibly infinite loops statements (for, while) and switch statment
		//
		public virtual void AddEndDefiniteAssignment (FlowAnalysisContext fc)
		{
		}

		public virtual void SetEndReachable ()
		{
		}

		public virtual void SetIteratorReachable ()
		{
		}
	}
	
	public class StatementExpression : Statement
	{
		ExpressionStatement expr;
		
		public StatementExpression (ExpressionStatement expr)
		{
			this.expr = expr;
			loc = expr.StartLocation;
		}

		public StatementExpression (ExpressionStatement expr, Location loc)
		{
			this.expr = expr;
			this.loc = loc;
		}

		public ExpressionStatement Expr {
			get {
 				return this.expr;
			}
		}
		
		protected override void CloneTo (CloneContext clonectx, Statement t)
		{
			StatementExpression target = (StatementExpression) t;
			target.expr = (ExpressionStatement) expr.Clone (clonectx);
		}
		
		protected override void DoEmit (EmitContext ec)
		{
			expr.EmitStatement (ec);
		}

		protected override bool DoFlowAnalysis (FlowAnalysisContext fc)
		{
			expr.FlowAnalysis (fc);
			return false;
		}

		public override Reachability MarkReachable (Reachability rc)
		{
			base.MarkReachable (rc);
			expr.MarkReachable (rc);
			return rc;
		}

		public override bool Resolve (BlockContext ec)
		{
			expr = expr.ResolveStatement (ec);
			return expr != null;
		}
		
		public override object Accept (StructuralVisitor visitor)
		{
			return visitor.Visit (this);
		}
	}

	public class StatementErrorExpression : Statement
	{
		Expression expr;

		public StatementErrorExpression (Expression expr)
		{
			this.expr = expr;
			this.loc = expr.StartLocation;
		}

		public Expression Expr {
			get {
				return expr;
			}
		}

		public override bool Resolve (BlockContext bc)
		{
			expr.Error_InvalidExpressionStatement (bc);
			return true;
		}

		protected override void DoEmit (EmitContext ec)
		{
			throw new NotSupportedException ();
		}

		protected override bool DoFlowAnalysis (FlowAnalysisContext fc)
		{
			return false;
		}

		protected override void CloneTo (CloneContext clonectx, Statement target)
		{
			var t = (StatementErrorExpression) target;

			t.expr = expr.Clone (clonectx);
		}
		
		public override object Accept (StructuralVisitor visitor)
		{
			return visitor.Visit (this);
		}
	}

	//
	// Simple version of statement list not requiring a block
	//
	public class StatementList : Statement
	{
		List<Statement> statements;

		public StatementList (Statement first, Statement second)
		{
			statements = new List<Statement> { first, second };
		}

		#region Properties
		public IList<Statement> Statements {
			get {
				return statements;
			}
		}
		#endregion

		public void Add (Statement statement)
		{
			statements.Add (statement);
		}

		public override bool Resolve (BlockContext ec)
		{
			foreach (var s in statements)
				s.Resolve (ec);

			return true;
		}

		protected override void DoEmit (EmitContext ec)
		{
			foreach (var s in statements)
				s.Emit (ec);
		}

		protected override bool DoFlowAnalysis (FlowAnalysisContext fc)
		{
			foreach (var s in statements)
				s.FlowAnalysis (fc);

			return false;
		}

		public override Reachability MarkReachable (Reachability rc)
		{
			base.MarkReachable (rc);

			Reachability res = rc;
			foreach (var s in statements)
				res = s.MarkReachable (rc);

			return res;
		}

		protected override void CloneTo (CloneContext clonectx, Statement target)
		{
			StatementList t = (StatementList) target;

			t.statements = new List<Statement> (statements.Count);
			foreach (Statement s in statements)
				t.statements.Add (s.Clone (clonectx));
		}
		
		public override object Accept (StructuralVisitor visitor)
		{
			return visitor.Visit (this);
		}
	}

	//
	// For statements which require special handling when inside try or catch block
	//
	public abstract class ExitStatement : Statement
	{
		protected bool unwind_protect;

		protected abstract bool DoResolve (BlockContext bc);
		protected abstract bool IsLocalExit { get; }

		public override bool Resolve (BlockContext bc)
		{
			var res = DoResolve (bc);

			if (!IsLocalExit) {
				//
				// We are inside finally scope but is it the scope we are exiting
				//
				if (bc.HasSet (ResolveContext.Options.FinallyScope)) {

					for (var b = bc.CurrentBlock; b != null; b = b.Parent) {
						if (b.IsFinallyBlock) {
							Error_FinallyClauseExit (bc);
							break;
						}

						if (b is ParametersBlock)
							break;
					}
				}
			}

			unwind_protect = bc.HasAny (ResolveContext.Options.TryScope | ResolveContext.Options.CatchScope);
			return res;
		}

		protected override bool DoFlowAnalysis (FlowAnalysisContext fc)
		{
			if (IsLocalExit)
				return true;

			if (fc.TryFinally != null) {
			    fc.TryFinally.RegisterForControlExitCheck (new DefiniteAssignmentBitSet (fc.DefiniteAssignment));
			} else {
			    fc.ParametersBlock.CheckControlExit (fc);
			}

			return true;
		}
	}

	/// <summary>
	///   Implements the return statement
	/// </summary>
	public class Return : ExitStatement
	{
		Expression expr;

		public Return (Expression expr, Location l)
		{
			this.expr = expr;
			loc = l;
		}

		#region Properties

		public Expression Expr {
			get {
				return expr;
			}
			protected set {
				expr = value;
			}
		}

		protected override bool IsLocalExit {
			get {
				return false;
			}
		}

		#endregion

		protected override bool DoResolve (BlockContext ec)
		{
			var block_return_type = ec.ReturnType;

			if (expr == null) {
				if (block_return_type.Kind == MemberKind.Void)
					return true;

				//
				// Return must not be followed by an expression when
				// the method return type is Task
				//
				if (ec.CurrentAnonymousMethod is AsyncInitializer) {
					var storey = (AsyncTaskStorey) ec.CurrentAnonymousMethod.Storey;
					if (storey.ReturnType == ec.Module.PredefinedTypes.Task.TypeSpec) {
						//
						// Extra trick not to emit ret/leave inside awaiter body
						//
						expr = EmptyExpression.Null;
						return true;
					}

					if (storey.ReturnType.IsGenericTask)
						block_return_type = storey.ReturnType.TypeArguments[0];
				}

				if (ec.CurrentIterator != null) {
					Error_ReturnFromIterator (ec);
				} else if (block_return_type != InternalType.ErrorType) {
					ec.Report.Error (126, loc,
						"An object of a type convertible to `{0}' is required for the return statement",
						block_return_type.GetSignatureForError ());
				}

				return false;
			}

			expr = expr.Resolve (ec);

			AnonymousExpression am = ec.CurrentAnonymousMethod;
			if (am == null) {
				if (block_return_type.Kind == MemberKind.Void) {
					ec.Report.Error (127, loc,
						"`{0}': A return keyword must not be followed by any expression when method returns void",
						ec.GetSignatureForError ());

					return false;
				}
			} else {
				if (am.IsIterator) {
					Error_ReturnFromIterator (ec);
					return false;
				}

				var async_block = am as AsyncInitializer;
				if (async_block != null) {
					if (expr != null) {
						var storey = (AsyncTaskStorey) am.Storey;
						var async_type = storey.ReturnType;

						if (async_type == null && async_block.ReturnTypeInference != null) {
							if (expr.Type.Kind == MemberKind.Void && !(this is ContextualReturn))
								ec.Report.Error (4029, loc, "Cannot return an expression of type `void'");
							else
								async_block.ReturnTypeInference.AddCommonTypeBoundAsync (expr.Type);
							return true;
						}

						if (async_type.Kind == MemberKind.Void) {
							ec.Report.Error (8030, loc,
								"Anonymous function or lambda expression converted to a void returning delegate cannot return a value");
							return false;
						}

						if (!async_type.IsGenericTask) {
							if (this is ContextualReturn)
								return true;

							if (async_block.DelegateType != null) {
								ec.Report.Error (8031, loc,
									"Async lambda expression or anonymous method converted to a `Task' cannot return a value. Consider returning `Task<T>'");
							} else {
								ec.Report.Error (1997, loc,
									"`{0}': A return keyword must not be followed by an expression when async method returns `Task'. Consider using `Task<T>' return type",
									ec.GetSignatureForError ());
							}
							return false;
						}

						//
						// The return type is actually Task<T> type argument
						//
						if (expr.Type == async_type) {
							ec.Report.Error (4016, loc,
								"`{0}': The return expression type of async method must be `{1}' rather than `Task<{1}>'",
								ec.GetSignatureForError (), async_type.TypeArguments[0].GetSignatureForError ());
						} else {
							block_return_type = async_type.TypeArguments[0];
						}
					}
				} else {
					if (block_return_type.Kind == MemberKind.Void) {
						ec.Report.Error (8030, loc,
							"Anonymous function or lambda expression converted to a void returning delegate cannot return a value");
						return false;
					}

					var l = am as AnonymousMethodBody;
					if (l != null && expr != null) {
						if (l.ReturnTypeInference != null) {
							l.ReturnTypeInference.AddCommonTypeBound (expr.Type);
							return true;
						}

						//
						// Try to optimize simple lambda. Only when optimizations are enabled not to cause
						// unexpected debugging experience
						//
						if (this is ContextualReturn && !ec.IsInProbingMode && ec.Module.Compiler.Settings.Optimize) {
							l.DirectMethodGroupConversion = expr.CanReduceLambda (l);
						}
					}
				}
			}

			if (expr == null)
				return false;

			if (expr.Type != block_return_type && expr.Type != InternalType.ErrorType) {
				expr = Convert.ImplicitConversionRequired (ec, expr, block_return_type, loc);

				if (expr == null) {
					if (am != null && block_return_type == ec.ReturnType) {
						ec.Report.Error (1662, loc,
							"Cannot convert `{0}' to delegate type `{1}' because some of the return types in the block are not implicitly convertible to the delegate return type",
							am.ContainerType, am.GetSignatureForError ());
					}
					return false;
				}
			}

			return true;			
		}
		
		protected override void DoEmit (EmitContext ec)
		{
			if (expr != null) {

				var async_body = ec.CurrentAnonymousMethod as AsyncInitializer;
				if (async_body != null) {
					var storey = (AsyncTaskStorey)async_body.Storey;
					Label exit_label = async_body.BodyEnd;

					//
					// It's null for await without async
					//
					if (storey.HoistedReturnValue != null) {
						//
						// Special case hoisted return value (happens in try/finally scenario)
						//
						if (ec.TryFinallyUnwind != null) {
							if (storey.HoistedReturnValue is VariableReference) {
								storey.HoistedReturnValue = ec.GetTemporaryField (storey.HoistedReturnValue.Type);
							}

							exit_label = TryFinally.EmitRedirectedReturn (ec, async_body);
						}

						var async_return = (IAssignMethod)storey.HoistedReturnValue;
						async_return.EmitAssign (ec, expr, false, false);
						ec.EmitEpilogue ();
					} else {
						expr.Emit (ec);

						if (ec.TryFinallyUnwind != null)
							exit_label = TryFinally.EmitRedirectedReturn (ec, async_body);
					}

					ec.Emit (OpCodes.Leave, exit_label);
					return;
				}

				expr.Emit (ec);
				ec.EmitEpilogue ();

				if (unwind_protect || ec.EmitAccurateDebugInfo)
					ec.Emit (OpCodes.Stloc, ec.TemporaryReturn ());
			}

			if (unwind_protect) {
				ec.Emit (OpCodes.Leave, ec.CreateReturnLabel ());
			} else if (ec.EmitAccurateDebugInfo) {
				ec.Emit (OpCodes.Br, ec.CreateReturnLabel ());
			} else {
				ec.Emit (OpCodes.Ret);
			}
		}

		protected override bool DoFlowAnalysis (FlowAnalysisContext fc)
		{
			if (expr != null)
				expr.FlowAnalysis (fc);

			base.DoFlowAnalysis (fc);
			return true;
		}

		void Error_ReturnFromIterator (ResolveContext rc)
		{
			rc.Report.Error (1622, loc,
				"Cannot return a value from iterators. Use the yield return statement to return a value, or yield break to end the iteration");
		}

		public override Reachability MarkReachable (Reachability rc)
		{
			base.MarkReachable (rc);
			return Reachability.CreateUnreachable ();
		}

		protected override void CloneTo (CloneContext clonectx, Statement t)
		{
			Return target = (Return) t;
			// It's null for simple return;
			if (expr != null)
				target.expr = expr.Clone (clonectx);
		}

		public override object Accept (StructuralVisitor visitor)
		{
			return visitor.Visit (this);
		}
	}

	public class Goto : ExitStatement
	{
		string target;
		LabeledStatement label;
		TryFinally try_finally;

		public Goto (string label, Location l)
		{
			loc = l;
			target = label;
		}

		public string Target {
			get { return target; }
		}

		protected override bool IsLocalExit {
			get {
				return true;
			}
		}

		protected override bool DoResolve (BlockContext bc)
		{
			label = bc.CurrentBlock.LookupLabel (target);
			if (label == null) {
				Error_UnknownLabel (bc, target, loc);
				return false;
			}

			try_finally = bc.CurrentTryBlock as TryFinally;

			CheckExitBoundaries (bc, label.Block);

			return true;
		}

		public static void Error_UnknownLabel (BlockContext bc, string label, Location loc)
		{
			bc.Report.Error (159, loc, "The label `{0}:' could not be found within the scope of the goto statement",
				label);
		}

		protected override bool DoFlowAnalysis (FlowAnalysisContext fc)
		{
			if (fc.LabelStack == null) {
				fc.LabelStack = new List<LabeledStatement> ();
			} else if (fc.LabelStack.Contains (label)) {
				return true;
			}

			fc.LabelStack.Add (label);
			label.Block.ScanGotoJump (label, fc);
			fc.LabelStack.Remove (label);
			return true;
		}

		public override Reachability MarkReachable (Reachability rc)
		{
			if (rc.IsUnreachable)
				return rc;

			base.MarkReachable (rc);

			if (try_finally != null) {
				if (try_finally.FinallyBlock.HasReachableClosingBrace) {
					label.AddGotoReference (rc, false);
				} else {
					label.AddGotoReference (rc, true);
				}
			} else {
				label.AddGotoReference (rc, false);
			}

			return Reachability.CreateUnreachable ();
		}

		protected override void CloneTo (CloneContext clonectx, Statement target)
		{
			// Nothing to clone
		}

		protected override void DoEmit (EmitContext ec)
		{
			if (label == null)
				throw new InternalErrorException ("goto emitted before target resolved");

			Label l = label.LabelTarget (ec);

			if (ec.TryFinallyUnwind != null && IsLeavingFinally (label.Block)) {
				var async_body = (AsyncInitializer) ec.CurrentAnonymousMethod;
				l = TryFinally.EmitRedirectedJump (ec, async_body, l, label.Block);
			}

			ec.Emit (unwind_protect ? OpCodes.Leave : OpCodes.Br, l);
		}

		bool IsLeavingFinally (Block labelBlock)
		{
			var b = try_finally.Statement as Block;
			while (b != null) {
				if (b == labelBlock)
					return true;

				b = b.Parent;
			}

			return false;
		}
		
		public override object Accept (StructuralVisitor visitor)
		{
			return visitor.Visit (this);
		}
	}

	public class LabeledStatement : Statement {
		string name;
		bool defined;
		bool referenced;
		bool finalTarget;
		Label label;
		Block block;
		
		public LabeledStatement (string name, Block block, Location l)
		{
			this.name = name;
			this.block = block;
			this.loc = l;
		}

		public Label LabelTarget (EmitContext ec)
		{
			if (defined)
				return label;

			label = ec.DefineLabel ();
			defined = true;
			return label;
		}

		public Block Block {
			get {
				return block;
			}
		}

		public string Name {
			get { return name; }
		}

		protected override void CloneTo (CloneContext clonectx, Statement target)
		{
			var t = (LabeledStatement) target;

			t.block = clonectx.RemapBlockCopy (block);
		}

		public override bool Resolve (BlockContext bc)
		{
			return true;
		}

		protected override void DoEmit (EmitContext ec)
		{
			LabelTarget (ec);
			ec.MarkLabel (label);

			if (finalTarget)
				ec.Emit (OpCodes.Br_S, label);
		}

		protected override bool DoFlowAnalysis (FlowAnalysisContext fc)
		{
			if (!referenced) {
				fc.Report.Warning (164, 2, loc, "This label has not been referenced");
			}

			return false;
		}

		public override Reachability MarkReachable (Reachability rc)
		{
			base.MarkReachable (rc);

			if (referenced)
				rc = new Reachability ();

			return rc;
		}

		public void AddGotoReference (Reachability rc, bool finalTarget)
		{
			if (referenced)
				return;

			referenced = true;
			MarkReachable (rc);

			//
			// Label is final target when goto jumps out of try block with
			// finally clause. In that case we need leave with target but in C#
			// terms the label is unreachable. Using finalTarget we emit
			// explicit label not just marker
			//
			if (finalTarget) {
				this.finalTarget = true;
				return;
			}

			block.ScanGotoJump (this);
		}

		public override object Accept (StructuralVisitor visitor)
		{
			return visitor.Visit (this);
		}
	}
	

	/// <summary>
	///   `goto default' statement
	/// </summary>
	public class GotoDefault : SwitchGoto
	{		
		public GotoDefault (Location l)
			: base (l)
		{
		}

		public override bool Resolve (BlockContext bc)
		{
			if (bc.Switch == null) {
				Error_GotoCaseRequiresSwitchBlock (bc);
				return false;
			}

			bc.Switch.RegisterGotoCase (null, null);
			base.Resolve (bc);

			return true;
		}

		protected override void DoEmit (EmitContext ec)
		{
			ec.Emit (unwind_protect ? OpCodes.Leave : OpCodes.Br, ec.Switch.DefaultLabel.GetILLabel (ec));
		}

		public override Reachability MarkReachable (Reachability rc)
		{
			if (!rc.IsUnreachable) {
				var label = switch_statement.DefaultLabel;
				if (label.IsUnreachable) {
					label.MarkReachable (rc);
					switch_statement.Block.ScanGotoJump (label);
				}
			}

			return base.MarkReachable (rc);
		}

		public override object Accept (StructuralVisitor visitor)
		{
			return visitor.Visit (this);
		}
	}

	/// <summary>
	///   `goto case' statement
	/// </summary>
	public class GotoCase : SwitchGoto
	{
		Expression expr;
		
		public GotoCase (Expression e, Location l)
			: base (l)
		{
			expr = e;
		}

		public Expression Expr {
			get {
 				return expr;
			}
		}

		public SwitchLabel Label { get; set; }

		public override bool Resolve (BlockContext ec)
		{
			if (ec.Switch == null) {
				Error_GotoCaseRequiresSwitchBlock (ec);
				return false;
			}

			Constant c = expr.ResolveLabelConstant (ec);
			if (c == null) {
				return false;
			}

			Constant res;
			if (ec.Switch.IsNullable && c is NullLiteral) {
				res = c;
			} else {
				TypeSpec type = ec.Switch.SwitchType;
				res = c.Reduce (ec, type);
				if (res == null) {
					c.Error_ValueCannotBeConverted (ec, type, true);
					return false;
				}

				if (!Convert.ImplicitStandardConversionExists (c, type))
					ec.Report.Warning (469, 2, loc,
						"The `goto case' value is not implicitly convertible to type `{0}'",
						type.GetSignatureForError ());

			}

			ec.Switch.RegisterGotoCase (this, res);
			base.Resolve (ec);
			expr = res;

			return true;
		}

		protected override void DoEmit (EmitContext ec)
		{
			ec.Emit (unwind_protect ? OpCodes.Leave : OpCodes.Br, Label.GetILLabel (ec));
		}

		protected override void CloneTo (CloneContext clonectx, Statement t)
		{
			GotoCase target = (GotoCase) t;

			target.expr = expr.Clone (clonectx);
		}

		public override Reachability MarkReachable (Reachability rc)
		{
			if (!rc.IsUnreachable) {
				var label = switch_statement.FindLabel ((Constant) expr);
				if (label.IsUnreachable) {
					label.MarkReachable (rc);
					switch_statement.Block.ScanGotoJump (label);
				}
			}

			return base.MarkReachable (rc);
		}
		
		public override object Accept (StructuralVisitor visitor)
		{
			return visitor.Visit (this);
		}
	}

	public abstract class SwitchGoto : Statement
	{
		protected bool unwind_protect;
		protected Switch switch_statement;

		protected SwitchGoto (Location loc)
		{
			this.loc = loc;
		}

		protected override void CloneTo (CloneContext clonectx, Statement target)
		{
			// Nothing to clone
		}

		public override bool Resolve (BlockContext bc)
		{
			CheckExitBoundaries (bc, bc.Switch.Block);

			unwind_protect = bc.HasAny (ResolveContext.Options.TryScope | ResolveContext.Options.CatchScope);
			switch_statement = bc.Switch;

			return true;
		}

		protected override bool DoFlowAnalysis (FlowAnalysisContext fc)
		{
			return true;
		}

		public override Reachability MarkReachable (Reachability rc)
		{
			base.MarkReachable (rc);
			return Reachability.CreateUnreachable ();
		}

		protected void Error_GotoCaseRequiresSwitchBlock (BlockContext bc)
		{
			bc.Report.Error (153, loc, "A goto case is only valid inside a switch statement");
		}
	}
	
	public class Throw : Statement {
		Expression expr;
		
		public Throw (Expression expr, Location l)
		{
			this.expr = expr;
			loc = l;
		}

		public Expression Expr {
			get {
 				return this.expr;
			}
		}

		public override bool Resolve (BlockContext ec)
		{
			if (expr == null) {
				if (!ec.HasSet (ResolveContext.Options.CatchScope)) {
					ec.Report.Error (156, loc, "A throw statement with no arguments is not allowed outside of a catch clause");
				} else if (ec.HasSet (ResolveContext.Options.FinallyScope)) {
					for (var b = ec.CurrentBlock; b != null && !b.IsCatchBlock; b = b.Parent) {
						if (b.IsFinallyBlock) {
							ec.Report.Error (724, loc,
								"A throw statement with no arguments is not allowed inside of a finally clause nested inside of the innermost catch clause");
							break;
						}
					}
				}

				return true;
			}

			expr = expr.Resolve (ec, ResolveFlags.Type | ResolveFlags.VariableOrValue);

			if (expr == null)
				return false;

			var et = ec.BuiltinTypes.Exception;
			if (Convert.ImplicitConversionExists (ec, expr, et))
				expr = Convert.ImplicitConversion (ec, expr, et, loc);
			else
				ec.Report.Error (155, expr.Location, "The type caught or thrown must be derived from System.Exception");

			return true;
		}
			
		protected override void DoEmit (EmitContext ec)
		{
			if (expr == null) {
				var atv = ec.AsyncThrowVariable;
				if (atv != null) {
					if (atv.HoistedVariant != null) {
						atv.HoistedVariant.Emit (ec);
					} else {
						atv.Emit (ec);
					}

					ec.Emit (OpCodes.Throw);
				} else {
					ec.Emit (OpCodes.Rethrow);
				}
			} else {
				expr.Emit (ec);

				ec.Emit (OpCodes.Throw);
			}
		}

		protected override bool DoFlowAnalysis (FlowAnalysisContext fc)
		{
			if (expr != null)
				expr.FlowAnalysis (fc);

			return true;
		}

		public override Reachability MarkReachable (Reachability rc)
		{
			base.MarkReachable (rc);
			return Reachability.CreateUnreachable ();
		}

		protected override void CloneTo (CloneContext clonectx, Statement t)
		{
			Throw target = (Throw) t;

			if (expr != null)
				target.expr = expr.Clone (clonectx);
		}
		
		public override object Accept (StructuralVisitor visitor)
		{
			return visitor.Visit (this);
		}
	}

	public class Break : LocalExitStatement
	{		
		public Break (Location l)
			: base (l)
		{
		}
		
		public override object Accept (StructuralVisitor visitor)
		{
			return visitor.Visit (this);
		}

		protected override void DoEmit (EmitContext ec)
		{
			var l = ec.LoopEnd;

			if (ec.TryFinallyUnwind != null) {
				var async_body = (AsyncInitializer) ec.CurrentAnonymousMethod;
				l = TryFinally.EmitRedirectedJump (ec, async_body, l, enclosing_loop.Statement as Block);
			}

			ec.Emit (unwind_protect ? OpCodes.Leave : OpCodes.Br, l);
		}

		protected override bool DoFlowAnalysis (FlowAnalysisContext fc)
		{
			enclosing_loop.AddEndDefiniteAssignment (fc);
			return true;
		}

		protected override bool DoResolve (BlockContext bc)
		{
			enclosing_loop = bc.EnclosingLoopOrSwitch;
			return base.DoResolve (bc);
		}

		public override Reachability MarkReachable (Reachability rc)
		{
			base.MarkReachable (rc);

			if (!rc.IsUnreachable)
				enclosing_loop.SetEndReachable ();

			return Reachability.CreateUnreachable ();
		}
	}

	public class Continue : LocalExitStatement
	{		
		public Continue (Location l)
			: base (l)
		{
		}

		public override object Accept (StructuralVisitor visitor)
		{
			return visitor.Visit (this);
		}


		protected override void DoEmit (EmitContext ec)
		{
			var l = ec.LoopBegin;

			if (ec.TryFinallyUnwind != null) {
				var async_body = (AsyncInitializer) ec.CurrentAnonymousMethod;
				l = TryFinally.EmitRedirectedJump (ec, async_body, l, enclosing_loop.Statement as Block);
			}

			ec.Emit (unwind_protect ? OpCodes.Leave : OpCodes.Br, l);
		}

		protected override bool DoResolve (BlockContext bc)
		{
			enclosing_loop = bc.EnclosingLoop;
			return base.DoResolve (bc);
		}

		public override Reachability MarkReachable (Reachability rc)
		{
			base.MarkReachable (rc);

			if (!rc.IsUnreachable)
				enclosing_loop.SetIteratorReachable ();

			return Reachability.CreateUnreachable ();
		}
	}

	public abstract class LocalExitStatement : ExitStatement
	{
		protected LoopStatement enclosing_loop;

		protected LocalExitStatement (Location loc)
		{
			this.loc = loc;
		}

		protected override bool IsLocalExit {
			get {
				return true;
			}
		}

		protected override void CloneTo (CloneContext clonectx, Statement t)
		{
			// nothing needed.
		}

		protected override bool DoResolve (BlockContext bc)
		{
			if (enclosing_loop == null) {
				bc.Report.Error (139, loc, "No enclosing loop out of which to break or continue");
				return false;
			}

			var block = enclosing_loop.Statement as Block;

			// Don't need to do extra checks for simple statements loops
			if (block != null) {
				CheckExitBoundaries (bc, block);
			}

			return true;
		}
	}

	public interface ILocalVariable
	{
		void Emit (EmitContext ec);
		void EmitAssign (EmitContext ec);
		void EmitAddressOf (EmitContext ec);
	}

	public interface INamedBlockVariable
	{
		Block Block { get; }
		Expression CreateReferenceExpression (ResolveContext rc, Location loc);
		bool IsDeclared { get; }
		bool IsParameter { get; }
		Location Location { get; }
	}

	public class BlockVariableDeclarator
	{
		LocalVariable li;
		Expression initializer;

		public BlockVariableDeclarator (LocalVariable li, Expression initializer)
		{
			if (li.Type != null)
				throw new ArgumentException ("Expected null variable type");

			this.li = li;
			this.initializer = initializer;
		}

		#region Properties

		public LocalVariable Variable {
			get {
				return li;
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

		#endregion

		public virtual BlockVariableDeclarator Clone (CloneContext cloneCtx)
		{
			var t = (BlockVariableDeclarator) MemberwiseClone ();
			if (initializer != null)
				t.initializer = initializer.Clone (cloneCtx);

			return t;
		}
	}

	public class BlockVariable : Statement
	{
		Expression initializer;
		protected FullNamedExpression type_expr;
		protected LocalVariable li;
		protected List<BlockVariableDeclarator> declarators;
		TypeSpec type;

		public BlockVariable (FullNamedExpression type, LocalVariable li)
		{
			this.type_expr = type;
			this.li = li;
			this.loc = type_expr.Location;
		}

		protected BlockVariable (LocalVariable li)
		{
			this.li = li;
		}

		#region Properties

		public List<BlockVariableDeclarator> Declarators {
			get {
				return declarators;
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

		public FullNamedExpression TypeExpression {
			get {
				return type_expr;
			}
		}

		public LocalVariable Variable {
			get {
				return li;
			}
		}

		#endregion

		public void AddDeclarator (BlockVariableDeclarator decl)
		{
			if (declarators == null)
				declarators = new List<BlockVariableDeclarator> ();

			declarators.Add (decl);
		}

		static void CreateEvaluatorVariable (BlockContext bc, LocalVariable li)
		{
			if (bc.Report.Errors != 0)
				return;

			var container = bc.CurrentMemberDefinition.Parent.PartialContainer;

			Field f = new Field (container, new TypeExpression (li.Type, li.Location), Modifiers.PUBLIC | Modifiers.STATIC,
				new MemberName (li.Name, li.Location), null);

			container.AddField (f);
			f.Define ();

			li.HoistedVariant = new HoistedEvaluatorVariable (f);
			li.SetIsUsed ();
		}

		public override bool Resolve (BlockContext bc)
		{
			return Resolve (bc, true);
		}

		public bool Resolve (BlockContext bc, bool resolveDeclaratorInitializers)
		{
			if (type == null && !li.IsCompilerGenerated) {
				var vexpr = type_expr as VarExpr;

				//
				// C# 3.0 introduced contextual keywords (var) which behaves like a type if type with
				// same name exists or as a keyword when no type was found
				//
				if (vexpr != null && !vexpr.IsPossibleTypeOrNamespace (bc)) {
					if (bc.Module.Compiler.Settings.Version < LanguageVersion.V_3)
						bc.Report.FeatureIsNotAvailable (bc.Module.Compiler, loc, "implicitly typed local variable");

					if (li.IsFixed) {
						bc.Report.Error (821, loc, "A fixed statement cannot use an implicitly typed local variable");
						return false;
					}

					if (li.IsConstant) {
						bc.Report.Error (822, loc, "An implicitly typed local variable cannot be a constant");
						return false;
					}

					if (Initializer == null) {
						bc.Report.Error (818, loc, "An implicitly typed local variable declarator must include an initializer");
						return false;
					}

					if (declarators != null) {
						bc.Report.Error (819, loc, "An implicitly typed local variable declaration cannot include multiple declarators");
						declarators = null;
					}

					Initializer = Initializer.Resolve (bc);
					if (Initializer != null) {
						((VarExpr) type_expr).InferType (bc, Initializer);
						type = type_expr.Type;
					} else {
						// Set error type to indicate the var was placed correctly but could
						// not be infered
						//
						// var a = missing ();
						//
						type = InternalType.ErrorType;
					}
				}

				if (type == null) {
					type = type_expr.ResolveAsType (bc);
					if (type == null)
						return false;

					if (li.IsConstant && !type.IsConstantCompatible) {
						Const.Error_InvalidConstantType (type, loc, bc.Report);
					}
				}

				if (type.IsStatic)
					FieldBase.Error_VariableOfStaticClass (loc, li.Name, type, bc.Report);

				li.Type = type;
			}

			bool eval_global = bc.Module.Compiler.Settings.StatementMode && bc.CurrentBlock is ToplevelBlock;
			if (eval_global) {
				CreateEvaluatorVariable (bc, li);
			} else if (type != InternalType.ErrorType) {
				li.PrepareAssignmentAnalysis (bc);
			}

			if (initializer != null) {
				initializer = ResolveInitializer (bc, li, initializer);
				// li.Variable.DefinitelyAssigned 
			}

			if (declarators != null) {
				foreach (var d in declarators) {
					d.Variable.Type = li.Type;
					if (eval_global) {
						CreateEvaluatorVariable (bc, d.Variable);
					} else if (type != InternalType.ErrorType) {
						d.Variable.PrepareAssignmentAnalysis (bc);
					}

					if (d.Initializer != null && resolveDeclaratorInitializers) {
						d.Initializer = ResolveInitializer (bc, d.Variable, d.Initializer);
						// d.Variable.DefinitelyAssigned 
					} 
				}
			}

			return true;
		}

		protected virtual Expression ResolveInitializer (BlockContext bc, LocalVariable li, Expression initializer)
		{
			var a = new SimpleAssign (li.CreateReferenceExpression (bc, li.Location), initializer, li.Location);
			return a.ResolveStatement (bc);
		}

		protected override void DoEmit (EmitContext ec)
		{
			li.CreateBuilder (ec);

			if (Initializer != null && !IsUnreachable)
				((ExpressionStatement) Initializer).EmitStatement (ec);

			if (declarators != null) {
				foreach (var d in declarators) {
					d.Variable.CreateBuilder (ec);
					if (d.Initializer != null && !IsUnreachable) {
						ec.Mark (d.Variable.Location);
						((ExpressionStatement) d.Initializer).EmitStatement (ec);
					}
				}
			}
		}

		protected override bool DoFlowAnalysis (FlowAnalysisContext fc)
		{
			if (Initializer != null)
				Initializer.FlowAnalysis (fc);

			if (declarators != null) {
				foreach (var d in declarators) {
					if (d.Initializer != null)
						d.Initializer.FlowAnalysis (fc);
				}
			}

			return false;
		}

		public override Reachability MarkReachable (Reachability rc)
		{
			var init = initializer as ExpressionStatement;
			if (init != null)
				init.MarkReachable (rc);

			return base.MarkReachable (rc);
		}

		protected override void CloneTo (CloneContext clonectx, Statement target)
		{
			BlockVariable t = (BlockVariable) target;

			if (type_expr != null)
				t.type_expr = (FullNamedExpression) type_expr.Clone (clonectx);

			if (initializer != null)
				t.initializer = initializer.Clone (clonectx);

			if (declarators != null) {
				t.declarators = null;
				foreach (var d in declarators)
					t.AddDeclarator (d.Clone (clonectx));
			}
		}

		public override object Accept (StructuralVisitor visitor)
		{
			return visitor.Visit (this);
		}
	}

	public class BlockConstant : BlockVariable
	{
		public BlockConstant (FullNamedExpression type, LocalVariable li)
			: base (type, li)
		{
		}

		public override void Emit (EmitContext ec)
		{
			// Nothing to emit, not even sequence point
		}

		protected override Expression ResolveInitializer (BlockContext bc, LocalVariable li, Expression initializer)
		{
			initializer = initializer.Resolve (bc);
			if (initializer == null)
				return null;

			var c = initializer as Constant;
			if (c == null) {
				initializer.Error_ExpressionMustBeConstant (bc, initializer.Location, li.Name);
				return null;
			}

			c = c.ConvertImplicitly (li.Type);
			if (c == null) {
				if (TypeSpec.IsReferenceType (li.Type))
					initializer.Error_ConstantCanBeInitializedWithNullOnly (bc, li.Type, initializer.Location, li.Name);
				else
					initializer.Error_ValueCannotBeConverted (bc, li.Type, false);

				return null;
			}

			li.ConstantValue = c;
			return initializer;
		}
		
		public override object Accept (StructuralVisitor visitor)
		{
			return visitor.Visit (this);
		}
	}

	//
	// The information about a user-perceived local variable
	//
	public sealed class LocalVariable : INamedBlockVariable, ILocalVariable
	{
		[Flags]
		public enum Flags
		{
			Used = 1,
			IsThis = 1 << 1,
			AddressTaken = 1 << 2,
			CompilerGenerated = 1 << 3,
			Constant = 1 << 4,
			ForeachVariable = 1 << 5,
			FixedVariable = 1 << 6,
			UsingVariable = 1 << 7,
			IsLocked = 1 << 8,

			ReadonlyMask = ForeachVariable | FixedVariable | UsingVariable
		}

		TypeSpec type;
		readonly string name;
		readonly Location loc;
		readonly Block block;
		Flags flags;
		Constant const_value;

		public VariableInfo VariableInfo;
		HoistedVariable hoisted_variant;

		LocalBuilder builder;

		public LocalVariable (Block block, string name, Location loc)
		{
			this.block = block;
			this.name = name;
			this.loc = loc;
		}

		public LocalVariable (Block block, string name, Flags flags, Location loc)
			: this (block, name, loc)
		{
			this.flags = flags;
		}

		//
		// Used by variable declarators
		//
		public LocalVariable (LocalVariable li, string name, Location loc)
			: this (li.block, name, li.flags, loc)
		{
		}

		#region Properties

		public bool AddressTaken {
			get {
				return (flags & Flags.AddressTaken) != 0;
			}
		}

		public Block Block {
			get {
				return block;
			}
		}

		public Constant ConstantValue {
			get {
				return const_value;
			}
			set {
				const_value = value;
			}
		}

		//
		// Hoisted local variable variant
		//
		public HoistedVariable HoistedVariant {
			get {
				return hoisted_variant;
			}
			set {
				hoisted_variant = value;
			}
		}

		public bool IsDeclared {
			get {
				return type != null;
			}
		}

		public bool IsCompilerGenerated {
			get {
				return (flags & Flags.CompilerGenerated) != 0;
			}
		}

		public bool IsConstant {
			get {
				return (flags & Flags.Constant) != 0;
			}
		}

		public bool IsLocked {
			get {
				return (flags & Flags.IsLocked) != 0;
			}
			set {
				flags = value ? flags | Flags.IsLocked : flags & ~Flags.IsLocked;
			}
		}

		public bool IsThis {
			get {
				return (flags & Flags.IsThis) != 0;
			}
		}

		public bool IsFixed {
			get {
				return (flags & Flags.FixedVariable) != 0;
			}
		}

		bool INamedBlockVariable.IsParameter {
			get {
				return false;
			}
		}

		public bool IsReadonly {
			get {
				return (flags & Flags.ReadonlyMask) != 0;
			}
		}

		public Location Location {
			get {
				return loc;
			}
		}

		public string Name {
			get {
				return name;
			}
		}

		public TypeSpec Type {
		    get {
				return type;
			}
		    set {
				type = value;
			}
		}

		#endregion

		public void CreateBuilder (EmitContext ec)
		{
			if ((flags & Flags.Used) == 0) {
				if (VariableInfo == null) {
					// Missing flow analysis or wrong variable flags
					throw new InternalErrorException ("VariableInfo is null and the variable `{0}' is not used", name);
				}

				if (VariableInfo.IsEverAssigned)
					ec.Report.Warning (219, 3, Location, "The variable `{0}' is assigned but its value is never used", Name);
				else
					ec.Report.Warning (168, 3, Location, "The variable `{0}' is declared but never used", Name);
			}

			if (HoistedVariant != null)
				return;

			if (builder != null) {
				if ((flags & Flags.CompilerGenerated) != 0)
					return;

				// To avoid Used warning duplicates
				throw new InternalErrorException ("Already created variable `{0}'", name);
			}

			//
			// All fixed variabled are pinned, a slot has to be alocated
			//
			builder = ec.DeclareLocal (Type, IsFixed);
			if (!ec.HasSet (BuilderContext.Options.OmitDebugInfo) && (flags & Flags.CompilerGenerated) == 0)
				ec.DefineLocalVariable (name, builder);
		}

		public static LocalVariable CreateCompilerGenerated (TypeSpec type, Block block, Location loc)
		{
			LocalVariable li = new LocalVariable (block, GetCompilerGeneratedName (block), Flags.CompilerGenerated | Flags.Used, loc);
			li.Type = type;
			return li;
		}

		public Expression CreateReferenceExpression (ResolveContext rc, Location loc)
		{
			if (IsConstant && const_value != null)
				return Constant.CreateConstantFromValue (Type, const_value.GetValue (), loc);

			return new LocalVariableReference (this, loc);
		}

		public void Emit (EmitContext ec)
		{
			// TODO: Need something better for temporary variables
			if ((flags & Flags.CompilerGenerated) != 0)
				CreateBuilder (ec);

			ec.Emit (OpCodes.Ldloc, builder);
		}

		public void EmitAssign (EmitContext ec)
		{
			// TODO: Need something better for temporary variables
			if ((flags & Flags.CompilerGenerated) != 0)
				CreateBuilder (ec);

			ec.Emit (OpCodes.Stloc, builder);
		}

		public void EmitAddressOf (EmitContext ec)
		{
			// TODO: Need something better for temporary variables
			if ((flags & Flags.CompilerGenerated) != 0)
				CreateBuilder (ec);

			ec.Emit (OpCodes.Ldloca, builder);
		}

		public static string GetCompilerGeneratedName (Block block)
		{
			// HACK: Debugger depends on the name semantics
			return "$locvar" + block.ParametersBlock.TemporaryLocalsCount++.ToString ("X");
		}

		public string GetReadOnlyContext ()
		{
			switch (flags & Flags.ReadonlyMask) {
			case Flags.FixedVariable:
				return "fixed variable";
			case Flags.ForeachVariable:
				return "foreach iteration variable";
			case Flags.UsingVariable:
				return "using variable";
			}

			throw new InternalErrorException ("Variable is not readonly");
		}

		public bool IsThisAssigned (FlowAnalysisContext fc, Block block)
		{
			if (VariableInfo == null)
				throw new Exception ();

			if (IsAssigned (fc))
				return true;

			return VariableInfo.IsFullyInitialized (fc, block.StartLocation);
		}

		public bool IsAssigned (FlowAnalysisContext fc)
		{
			return fc.IsDefinitelyAssigned (VariableInfo);
		}

		public void PrepareAssignmentAnalysis (BlockContext bc)
		{
			//
			// No need to run assignment analysis for these guys
			//
			if ((flags & (Flags.Constant | Flags.ReadonlyMask | Flags.CompilerGenerated)) != 0)
				return;

			VariableInfo = VariableInfo.Create (bc, this);
		}

		//
		// Mark the variables as referenced in the user code
		//
		public void SetIsUsed ()
		{
			flags |= Flags.Used;
		}

		public void SetHasAddressTaken ()
		{
			flags |= (Flags.AddressTaken | Flags.Used);
		}

		public override string ToString ()
		{
			return string.Format ("LocalInfo ({0},{1},{2},{3})", name, type, VariableInfo, Location);
		}
	}

	/// <summary>
	///   Block represents a C# block.
	/// </summary>
	///
	/// <remarks>
	///   This class is used in a number of places: either to represent
	///   explicit blocks that the programmer places or implicit blocks.
	///
	///   Implicit blocks are used as labels or to introduce variable
	///   declarations.
	///
	///   Top-level blocks derive from Block, and they are called ToplevelBlock
	///   they contain extra information that is not necessary on normal blocks.
	/// </remarks>
	public class Block : Statement {
		[Flags]
		public enum Flags
		{
			Unchecked = 1,
			ReachableEnd = 8,
			Unsafe = 16,
			HasCapturedVariable = 64,
			HasCapturedThis = 1 << 7,
			IsExpressionTree = 1 << 8,
			CompilerGenerated = 1 << 9,
			HasAsyncModifier = 1 << 10,
			Resolved = 1 << 11,
			YieldBlock = 1 << 12,
			AwaitBlock = 1 << 13,
			FinallyBlock = 1 << 14,
			CatchBlock = 1 << 15,
			Iterator = 1 << 20,
			NoFlowAnalysis = 1 << 21,
			InitializationEmitted = 1 << 22
		}

		public Block Parent;
		public Location StartLocation;
		public Location EndLocation;

		public ExplicitBlock Explicit;
		public ParametersBlock ParametersBlock;

		protected Flags flags;

		//
		// The statements in this block
		//
		protected List<Statement> statements;

		protected List<Statement> scope_initializers;

		int? resolving_init_idx;

		Block original;

#if DEBUG
		static int id;
		public int ID = id++;

		static int clone_id_counter;
		int clone_id;
#endif

//		int assignable_slots;

		public Block (Block parent, Location start, Location end)
			: this (parent, 0, start, end)
		{
		}

		public Block (Block parent, Flags flags, Location start, Location end)
		{
			if (parent != null) {
				// the appropriate constructors will fixup these fields
				ParametersBlock = parent.ParametersBlock;
				Explicit = parent.Explicit;
			}
			
			this.Parent = parent;
			this.flags = flags;
			this.StartLocation = start;
			this.EndLocation = end;
			this.loc = start;
			statements = new List<Statement> (4);

			this.original = this;
		}

		#region Properties

		public Block Original {
			get {
				return original;
			}
			protected set {
				original = value;
			}
		}

		public bool IsCompilerGenerated {
			get { return (flags & Flags.CompilerGenerated) != 0; }
			set { flags = value ? flags | Flags.CompilerGenerated : flags & ~Flags.CompilerGenerated; }
		}


		public bool IsCatchBlock {
			get {
				return (flags & Flags.CatchBlock) != 0;
			}
		}

		public bool IsFinallyBlock {
			get {
				return (flags & Flags.FinallyBlock) != 0;
			}
		}

		public bool Unchecked {
			get { return (flags & Flags.Unchecked) != 0; }
			set { flags = value ? flags | Flags.Unchecked : flags & ~Flags.Unchecked; }
		}

		public bool Unsafe {
			get { return (flags & Flags.Unsafe) != 0; }
			set { flags |= Flags.Unsafe; }
		}

		public List<Statement> Statements {
			get { return statements; }
		}

		#endregion

		public void SetEndLocation (Location loc)
		{
			EndLocation = loc;
		}

		public void AddLabel (LabeledStatement target)
		{
			ParametersBlock.TopBlock.AddLabel (target.Name, target);
		}

		public void AddLocalName (LocalVariable li)
		{
			AddLocalName (li.Name, li);
		}

		public void AddLocalName (string name, INamedBlockVariable li)
		{
			ParametersBlock.TopBlock.AddLocalName (name, li, false);
		}

		public virtual void Error_AlreadyDeclared (string name, INamedBlockVariable variable, string reason)
		{
			if (reason == null) {
				Error_AlreadyDeclared (name, variable);
				return;
			}

			ParametersBlock.TopBlock.Report.Error (136, variable.Location,
				"A local variable named `{0}' cannot be declared in this scope because it would give a different meaning " +
				"to `{0}', which is already used in a `{1}' scope to denote something else",
				name, reason);
		}

		public virtual void Error_AlreadyDeclared (string name, INamedBlockVariable variable)
		{
			var pi = variable as ParametersBlock.ParameterInfo;
			if (pi != null) {
				pi.Parameter.Error_DuplicateName (ParametersBlock.TopBlock.Report);
			} else {
				ParametersBlock.TopBlock.Report.Error (128, variable.Location,
					"A local variable named `{0}' is already defined in this scope", name);
			}
		}
					
		public virtual void Error_AlreadyDeclaredTypeParameter (string name, Location loc)
		{
			ParametersBlock.TopBlock.Report.Error (412, loc,
				"The type parameter name `{0}' is the same as local variable or parameter name",
				name);
		}

		//
		// It should be used by expressions which require to
		// register a statement during resolve process.
		//
		public void AddScopeStatement (Statement s)
		{
			if (scope_initializers == null)
				scope_initializers = new List<Statement> ();

			//
			// Simple recursive helper, when resolve scope initializer another
			// new scope initializer can be added, this ensures it's initialized
			// before existing one. For now this can happen with expression trees
			// in base ctor initializer only
			//
			if (resolving_init_idx.HasValue) {
				scope_initializers.Insert (resolving_init_idx.Value, s);
				++resolving_init_idx;
			} else {
				scope_initializers.Add (s);
			}
		}

		public void InsertStatement (int index, Statement s)
		{
			statements.Insert (index, s);
		}
		
		public void AddStatement (Statement s)
		{
			statements.Add (s);
		}

		public LabeledStatement LookupLabel (string name)
		{
			return ParametersBlock.GetLabel (name, this);
		}

		public override Reachability MarkReachable (Reachability rc)
		{
			if (rc.IsUnreachable)
				return rc;

			MarkReachableScope (rc);

			foreach (var s in statements) {
				rc = s.MarkReachable (rc);
				if (rc.IsUnreachable) {
					if ((flags & Flags.ReachableEnd) != 0)
						return new Reachability ();

					return rc;
				}
			}

			flags |= Flags.ReachableEnd;

			return rc;
		}

		public void MarkReachableScope (Reachability rc)
		{
			base.MarkReachable (rc);

			if (scope_initializers != null) {
				foreach (var si in scope_initializers)
					si.MarkReachable (rc);
			}
		}

		public override bool Resolve (BlockContext bc)
		{
			if ((flags & Flags.Resolved) != 0)
				return true;

			Block prev_block = bc.CurrentBlock;
			bc.CurrentBlock = this;

			//
			// Compiler generated scope statements
			//
			if (scope_initializers != null) {
				for (resolving_init_idx = 0; resolving_init_idx < scope_initializers.Count; ++resolving_init_idx) {
					scope_initializers[resolving_init_idx.Value].Resolve (bc);
				}

				resolving_init_idx = null;
			}

			bool ok = true;
			int statement_count = statements.Count;
			for (int ix = 0; ix < statement_count; ix++){
				Statement s = statements [ix];

				if (!s.Resolve (bc)) {
					ok = false;
					if (!bc.IsInProbingMode)
						statements [ix] = new EmptyStatement (s.loc);

					continue;
				}
			}

			bc.CurrentBlock = prev_block;

			flags |= Flags.Resolved;
			return ok;
		}

		protected override void DoEmit (EmitContext ec)
		{
			for (int ix = 0; ix < statements.Count; ix++){
				statements [ix].Emit (ec);
			}
		}

		public override void Emit (EmitContext ec)
		{
			if (scope_initializers != null)
				EmitScopeInitializers (ec);

			DoEmit (ec);
		}

		protected void EmitScopeInitializers (EmitContext ec)
		{
			foreach (Statement s in scope_initializers)
				s.Emit (ec);
		}

		protected override bool DoFlowAnalysis (FlowAnalysisContext fc)
		{
			if (scope_initializers != null) {
				foreach (var si in scope_initializers)
					si.FlowAnalysis (fc);
			}

			return DoFlowAnalysis (fc, 0);	
		}

		bool DoFlowAnalysis (FlowAnalysisContext fc, int startIndex)
		{
			bool end_unreachable = !reachable;
			for (; startIndex < statements.Count; ++startIndex) {
				var s = statements[startIndex];

				end_unreachable = s.FlowAnalysis (fc);
				if (s.IsUnreachable) {
					statements [startIndex] = RewriteUnreachableStatement (s);
					continue;
				}

				//
				// Statement end reachability is needed mostly due to goto support. Consider
				//
				// if (cond) {
				//    goto X;
				// } else {
				//    goto Y;
				// }
				// X:
				//
				// X label is reachable only via goto not as another statement after if. We need
				// this for flow-analysis only to carry variable info correctly.
				//
				if (end_unreachable) {
					for (++startIndex; startIndex < statements.Count; ++startIndex) {
						s = statements[startIndex];
						if (s is SwitchLabel) {
							s.FlowAnalysis (fc);
							break;
						}

						if (s.IsUnreachable) {
							s.FlowAnalysis (fc);
							statements [startIndex] = RewriteUnreachableStatement (s);
						}
					}
				}
			}

			//
			// The condition should be true unless there is forward jumping goto
			// 
			// if (this is ExplicitBlock && end_unreachable != Explicit.HasReachableClosingBrace)
			//	Debug.Fail ();

			return !Explicit.HasReachableClosingBrace;
		}

		static Statement RewriteUnreachableStatement (Statement s)
		{
			// LAMESPEC: It's not clear whether declararion statement should be part of reachability
			// analysis. Even csc report unreachable warning for it but it's actually used hence
			// we try to emulate this behaviour
			//
			// Consider:
			// 	goto L;
			//	int v;
			// L:
			//	v = 1;

			if (s is BlockVariable)
				return s;

			return new EmptyStatement (s.loc);
		}

		public void ScanGotoJump (Statement label)
		{
			int i;
			for (i = 0; i < statements.Count; ++i) {
				if (statements[i] == label)
					break;
			}

			var rc = new Reachability ();
			for (++i; i < statements.Count; ++i) {
				var s = statements[i];
				rc = s.MarkReachable (rc);
				if (rc.IsUnreachable)
					return;
			}

			flags |= Flags.ReachableEnd;
		}

		public void ScanGotoJump (Statement label, FlowAnalysisContext fc)
		{
			int i;
			for (i = 0; i < statements.Count; ++i) {
				if (statements[i] == label)
					break;
			}

			DoFlowAnalysis (fc, ++i);
		}

#if DEBUG
		public override string ToString ()
		{
			return String.Format ("{0}: ID={1} Clone={2} Location={3}", GetType (), ID, clone_id != 0, StartLocation);
		}
#endif

		protected override void CloneTo (CloneContext clonectx, Statement t)
		{
			Block target = (Block) t;
#if DEBUG
			target.clone_id = ++clone_id_counter;
#endif

			clonectx.AddBlockMap (this, target);
			if (original != this)
				clonectx.AddBlockMap (original, target);

			target.ParametersBlock = (ParametersBlock) (ParametersBlock == this ? target : clonectx.RemapBlockCopy (ParametersBlock));
			target.Explicit = (ExplicitBlock) (Explicit == this ? target : clonectx.LookupBlock (Explicit));

			if (Parent != null)
				target.Parent = clonectx.RemapBlockCopy (Parent);

			target.statements = new List<Statement> (statements.Count);
			foreach (Statement s in statements)
				target.statements.Add (s.Clone (clonectx));
		}

		public override object Accept (StructuralVisitor visitor)
		{
			return visitor.Visit (this);
		}
	}

	public class ExplicitBlock : Block
	{
		protected AnonymousMethodStorey am_storey;

		public ExplicitBlock (Block parent, Location start, Location end)
			: this (parent, (Flags) 0, start, end)
		{
		}

		public ExplicitBlock (Block parent, Flags flags, Location start, Location end)
			: base (parent, flags, start, end)
		{
			this.Explicit = this;
		}

		#region Properties

		public AnonymousMethodStorey AnonymousMethodStorey {
			get {
				return am_storey;
			}
		}

		public bool HasAwait {
			get {
				return (flags & Flags.AwaitBlock) != 0;
			}
		}

		public bool HasCapturedThis {
			set {
				flags = value ? flags | Flags.HasCapturedThis : flags & ~Flags.HasCapturedThis;
			}
			get {
				return (flags & Flags.HasCapturedThis) != 0;
			}
		}

		//
		// Used to indicate that the block has reference to parent
		// block and cannot be made static when defining anonymous method
		//
		public bool HasCapturedVariable {
			set {
				flags = value ? flags | Flags.HasCapturedVariable : flags & ~Flags.HasCapturedVariable;
			}
			get {
				return (flags & Flags.HasCapturedVariable) != 0;
			}
		}

		public bool HasReachableClosingBrace {
		    get {
		        return (flags & Flags.ReachableEnd) != 0;
		    }
			set {
				flags = value ? flags | Flags.ReachableEnd : flags & ~Flags.ReachableEnd;
			}
		}

		public bool HasYield {
			get {
				return (flags & Flags.YieldBlock) != 0;
			}
		}

		#endregion

		//
		// Creates anonymous method storey in current block
		//
		public AnonymousMethodStorey CreateAnonymousMethodStorey (ResolveContext ec)
		{
			//
			// Return same story for iterator and async blocks unless we are
			// in nested anonymous method
			//
			if (ec.CurrentAnonymousMethod is StateMachineInitializer && ParametersBlock.Original == ec.CurrentAnonymousMethod.Block.Original)
				return ec.CurrentAnonymousMethod.Storey;

			if (am_storey == null) {
				MemberBase mc = ec.MemberContext as MemberBase;

				//
				// Creates anonymous method storey for this block
				//
				am_storey = new AnonymousMethodStorey (this, ec.CurrentMemberDefinition.Parent.PartialContainer, mc, ec.CurrentTypeParameters, "AnonStorey", MemberKind.Class);
			}

			return am_storey;
		}

		public void EmitScopeInitialization (EmitContext ec)
		{
			if ((flags & Flags.InitializationEmitted) != 0)
				return;

			if (am_storey != null) {
				DefineStoreyContainer (ec, am_storey);
				am_storey.EmitStoreyInstantiation (ec, this);
			}

			if (scope_initializers != null)
				EmitScopeInitializers (ec);

			flags |= Flags.InitializationEmitted;
		}

		public override void Emit (EmitContext ec)
		{
			EmitScopeInitialization (ec);

			if (ec.EmitAccurateDebugInfo && !IsCompilerGenerated && ec.Mark (StartLocation)) {
				ec.Emit (OpCodes.Nop);
			}

			if (Parent != null)
				ec.BeginScope ();

			DoEmit (ec);

			if (Parent != null)
				ec.EndScope ();

			if (ec.EmitAccurateDebugInfo && HasReachableClosingBrace && !(this is ParametersBlock) &&
				!IsCompilerGenerated && ec.Mark (EndLocation)) {
				ec.Emit (OpCodes.Nop);
			}
		}

		protected void DefineStoreyContainer (EmitContext ec, AnonymousMethodStorey storey)
		{
			if (ec.CurrentAnonymousMethod != null && ec.CurrentAnonymousMethod.Storey != null) {
				storey.SetNestedStoryParent (ec.CurrentAnonymousMethod.Storey);
				storey.Mutator = ec.CurrentAnonymousMethod.Storey.Mutator;
			}

			//
			// Creates anonymous method storey
			//
			storey.CreateContainer ();
			storey.DefineContainer ();

			if (Original.Explicit.HasCapturedThis && Original.ParametersBlock.TopBlock.ThisReferencesFromChildrenBlock != null) {

				//
				// Only first storey in path will hold this reference. All children blocks will
				// reference it indirectly using $ref field
				//
				for (Block b = Original.Explicit; b != null; b = b.Parent) {
					if (b.Parent != null) {
						var s = b.Parent.Explicit.AnonymousMethodStorey;
						if (s != null) {
							storey.HoistedThis = s.HoistedThis;
							break;
						}
					}

					if (b.Explicit == b.Explicit.ParametersBlock && b.Explicit.ParametersBlock.StateMachine != null) {
						if (storey.HoistedThis == null)
							storey.HoistedThis = b.Explicit.ParametersBlock.StateMachine.HoistedThis;

						if (storey.HoistedThis != null)
							break;
					}
				}
				
				//
				// We are the first storey on path and 'this' has to be hoisted
				//
				if (storey.HoistedThis == null) {
					foreach (ExplicitBlock ref_block in Original.ParametersBlock.TopBlock.ThisReferencesFromChildrenBlock) {
						//
						// ThisReferencesFromChildrenBlock holds all reference even if they
						// are not on this path. It saves some memory otherwise it'd have to
						// be in every explicit block. We run this check to see if the reference
						// is valid for this storey
						//
						Block block_on_path = ref_block;
						for (; block_on_path != null && block_on_path != Original; block_on_path = block_on_path.Parent);

						if (block_on_path == null)
							continue;

						if (storey.HoistedThis == null) {
							storey.AddCapturedThisField (ec, null);
						}

						for (ExplicitBlock b = ref_block; b.AnonymousMethodStorey != storey; b = b.Parent.Explicit) {
							ParametersBlock pb;
							AnonymousMethodStorey b_storey = b.AnonymousMethodStorey;

							if (b_storey != null) {
								//
								// Don't add storey cross reference for `this' when the storey ends up not
								// beeing attached to any parent
								//
								if (b.ParametersBlock.StateMachine == null) {
									AnonymousMethodStorey s = null;
									for (Block ab = b.AnonymousMethodStorey.OriginalSourceBlock.Parent; ab != null; ab = ab.Parent) {
										s = ab.Explicit.AnonymousMethodStorey;
										if (s != null)
											break;
									}

									// Needs to be in sync with AnonymousMethodBody::DoCreateMethodHost
									if (s == null) {
										var parent = storey == null || storey.Kind == MemberKind.Struct ? null : storey;
										b.AnonymousMethodStorey.AddCapturedThisField (ec, parent);
										break;
									}

								}

								//
								// Stop propagation inside same top block
								//
								if (b.ParametersBlock == ParametersBlock.Original) {
									b_storey.AddParentStoreyReference (ec, storey);
//									b_storey.HoistedThis = storey.HoistedThis;
									break;
								}

								b = pb = b.ParametersBlock;
							} else {
								pb = b as ParametersBlock;
							}

							if (pb != null && pb.StateMachine != null) {
								if (pb.StateMachine == storey)
									break;

								//
								// If we are state machine with no parent. We can hook into parent without additional
 								// reference and capture this directly
								//
								ExplicitBlock parent_storey_block = pb;
								while (parent_storey_block.Parent != null) {
									parent_storey_block = parent_storey_block.Parent.Explicit;
									if (parent_storey_block.AnonymousMethodStorey != null) {
										break;
									}
								}

								if (parent_storey_block.AnonymousMethodStorey == null) {
									pb.StateMachine.AddCapturedThisField (ec, null);
									b.HasCapturedThis = true;
									continue;
								}

								pb.StateMachine.AddParentStoreyReference (ec, storey);
							}

							//
							// Add parent storey reference only when this is not captured directly
							//
							if (b_storey != null) {
								b_storey.AddParentStoreyReference (ec, storey);
								b_storey.HoistedThis = storey.HoistedThis;
							}
						}
					}
				}
			}

			var ref_blocks = storey.ReferencesFromChildrenBlock;
			if (ref_blocks != null) {
				foreach (ExplicitBlock ref_block in ref_blocks) {
					for (ExplicitBlock b = ref_block; b.AnonymousMethodStorey != storey; b = b.Parent.Explicit) {
						if (b.AnonymousMethodStorey != null) {
							b.AnonymousMethodStorey.AddParentStoreyReference (ec, storey);

							//
							// Stop propagation inside same top block
							//
							if (b.ParametersBlock == ParametersBlock.Original)
								break;

							b = b.ParametersBlock;
						}

						var pb = b as ParametersBlock;
						if (pb != null && pb.StateMachine != null) {
							if (pb.StateMachine == storey)
								break;

							pb.StateMachine.AddParentStoreyReference (ec, storey);
						}

						b.HasCapturedVariable = true;
					}
				}
			}

			storey.Define ();
			storey.PrepareEmit ();
			storey.Parent.PartialContainer.AddCompilerGeneratedClass (storey);
		}

		public void RegisterAsyncAwait ()
		{
			var block = this;
			while ((block.flags & Flags.AwaitBlock) == 0) {
				block.flags |= Flags.AwaitBlock;

				if (block is ParametersBlock)
					return;

				block = block.Parent.Explicit;
			}
		}

		public void RegisterIteratorYield ()
		{
			ParametersBlock.TopBlock.IsIterator = true;

			var block = this;
			while ((block.flags & Flags.YieldBlock) == 0) {
				block.flags |= Flags.YieldBlock;

				if (block.Parent == null)
					return;

				block = block.Parent.Explicit;
			}
		}

		public void SetCatchBlock ()
		{
			flags |= Flags.CatchBlock;
		}

		public void SetFinallyBlock ()
		{
			flags |= Flags.FinallyBlock;
		}

		public void WrapIntoDestructor (TryFinally tf, ExplicitBlock tryBlock)
		{
			tryBlock.statements = statements;
			statements = new List<Statement> (1);
			statements.Add (tf);
		}
	}

	//
	// ParametersBlock was introduced to support anonymous methods
	// and lambda expressions
	// 
	public class ParametersBlock : ExplicitBlock
	{
		public class ParameterInfo : INamedBlockVariable
		{
			readonly ParametersBlock block;
			readonly int index;
			public VariableInfo VariableInfo;
			bool is_locked;

			public ParameterInfo (ParametersBlock block, int index)
			{
				this.block = block;
				this.index = index;
			}

			#region Properties

			public ParametersBlock Block {
				get {
					return block;
				}
			}

			Block INamedBlockVariable.Block {
				get {
					return block;
				}
			}

			public bool IsDeclared {
				get {
					return true;
				}
			}

			public bool IsParameter {
				get {
					return true;
				}
			}

			public bool IsLocked {
				get {
					return is_locked;
				}
				set {
					is_locked = value;
				}
			}

			public Location Location {
				get {
					return Parameter.Location;
				}
			}

			public Parameter Parameter {
				get {
					return block.Parameters [index];
				}
			}

			public TypeSpec ParameterType {
				get {
					return Parameter.Type;
				}
			}

			#endregion

			public Expression CreateReferenceExpression (ResolveContext rc, Location loc)
			{
				return new ParameterReference (this, loc);
			}
		}

		// 
		// Block is converted into an expression
		//
		sealed class BlockScopeExpression : Expression
		{
			Expression child;
			readonly ParametersBlock block;

			public BlockScopeExpression (Expression child, ParametersBlock block)
			{
				this.child = child;
				this.block = block;
			}

			public override bool ContainsEmitWithAwait ()
			{
				return child.ContainsEmitWithAwait ();
			}

			public override Expression CreateExpressionTree (ResolveContext ec)
			{
				throw new NotSupportedException ();
			}

			protected override Expression DoResolve (ResolveContext ec)
			{
				if (child == null)
					return null;

				child = child.Resolve (ec);
				if (child == null)
					return null;

				eclass = child.eclass;
				type = child.Type;
				return this;
			}

			public override void Emit (EmitContext ec)
			{
				block.EmitScopeInitializers (ec);
				child.Emit (ec);
			}
		}

		protected ParametersCompiled parameters;
		protected ParameterInfo[] parameter_info;
		protected bool resolved;
		protected ToplevelBlock top_block;
		protected StateMachine state_machine;
		protected Dictionary<string, object> labels;

		public ParametersBlock (Block parent, ParametersCompiled parameters, Location start, Flags flags = 0)
			: base (parent, 0, start, start)
		{
			if (parameters == null)
				throw new ArgumentNullException ("parameters");

			this.parameters = parameters;
			ParametersBlock = this;

			this.flags |= flags | (parent.ParametersBlock.flags & (Flags.YieldBlock | Flags.AwaitBlock));

			this.top_block = parent.ParametersBlock.top_block;
			ProcessParameters ();
		}

		protected ParametersBlock (ParametersCompiled parameters, Location start)
			: base (null, 0, start, start)
		{
			if (parameters == null)
				throw new ArgumentNullException ("parameters");

			this.parameters = parameters;
			ParametersBlock = this;
		}

		//
		// It's supposed to be used by method body implementation of anonymous methods
		//
		protected ParametersBlock (ParametersBlock source, ParametersCompiled parameters)
			: base (null, 0, source.StartLocation, source.EndLocation)
		{
			this.parameters = parameters;
			this.statements = source.statements;
			this.scope_initializers = source.scope_initializers;

			this.resolved = true;
			this.reachable = source.reachable;
			this.am_storey = source.am_storey;
			this.state_machine = source.state_machine;
			this.flags = source.flags & Flags.ReachableEnd;

			ParametersBlock = this;

			//
			// Overwrite original for comparison purposes when linking cross references
			// between anonymous methods
			//
			Original = source.Original;
		}

		#region Properties

		public bool IsAsync {
			get {
				return (flags & Flags.HasAsyncModifier) != 0;
			}
			set {
				flags = value ? flags | Flags.HasAsyncModifier : flags & ~Flags.HasAsyncModifier;
			}
		}

		//
		// Block has been converted to expression tree
		//
		public bool IsExpressionTree {
			get {
				return (flags & Flags.IsExpressionTree) != 0;
			}
		}

		//
		// The parameters for the block.
		//
		public ParametersCompiled Parameters {
			get {
				return parameters;
			}
		}

		public StateMachine StateMachine {
			get {
				return state_machine;
			}
		}

		public ToplevelBlock TopBlock {
			get {
				return top_block;
			}
			set {
				top_block = value;
			}
		}

		public bool Resolved {
			get {
				return (flags & Flags.Resolved) != 0;
			}
		}

		public int TemporaryLocalsCount { get; set; }

		#endregion

		//
		// Checks whether all `out' parameters have been assigned.
		//
		public void CheckControlExit (FlowAnalysisContext fc)
		{
			CheckControlExit (fc, fc.DefiniteAssignment);
		}

		public virtual void CheckControlExit (FlowAnalysisContext fc, DefiniteAssignmentBitSet dat)
		{
			if (parameter_info == null)
				return;

			foreach (var p in parameter_info) {
				if (p.VariableInfo == null)
					continue;

				if (p.VariableInfo.IsAssigned (dat))
					continue;

				fc.Report.Error (177, p.Location,
					"The out parameter `{0}' must be assigned to before control leaves the current method",
					p.Parameter.Name);
			}					
		}

		protected override void CloneTo (CloneContext clonectx, Statement t)
		{
			base.CloneTo (clonectx, t);

			var target = (ParametersBlock) t;

			//
			// Clone label statements as well as they contain block reference
			//
			var pb = this;
			while (true) {
				if (pb.labels != null) {
					target.labels = new Dictionary<string, object> ();

					foreach (var entry in pb.labels) {
						var list = entry.Value as List<LabeledStatement>;

						if (list != null) {
							var list_clone = new List<LabeledStatement> ();
							foreach (var lentry in list) {
								list_clone.Add (RemapLabeledStatement (lentry, lentry.Block, clonectx.RemapBlockCopy (lentry.Block)));
							}

							target.labels.Add (entry.Key, list_clone);
						} else {
							var labeled = (LabeledStatement) entry.Value;
							target.labels.Add (entry.Key, RemapLabeledStatement (labeled, labeled.Block, clonectx.RemapBlockCopy (labeled.Block)));
						}
					}

					break;
				}

				if (pb.Parent == null)
					break;

				pb = pb.Parent.ParametersBlock;
			}
		}

		public override Expression CreateExpressionTree (ResolveContext ec)
		{
			if (statements.Count == 1) {
				Expression expr = statements[0].CreateExpressionTree (ec);
				if (scope_initializers != null)
					expr = new BlockScopeExpression (expr, this);

				return expr;
			}

			return base.CreateExpressionTree (ec);
		}

		public override void Emit (EmitContext ec)
		{
			if (state_machine != null && state_machine.OriginalSourceBlock != this) {
				DefineStoreyContainer (ec, state_machine);
				state_machine.EmitStoreyInstantiation (ec, this);
			}

			base.Emit (ec);
		}

		public void EmitEmbedded (EmitContext ec)
		{
			if (state_machine != null && state_machine.OriginalSourceBlock != this) {
				DefineStoreyContainer (ec, state_machine);
				state_machine.EmitStoreyInstantiation (ec, this);
			}

			base.Emit (ec);
		}

		protected override bool DoFlowAnalysis (FlowAnalysisContext fc)
		{
			var res = base.DoFlowAnalysis (fc);

			if (HasReachableClosingBrace)
				CheckControlExit (fc);

			return res;
		}

		public LabeledStatement GetLabel (string name, Block block)
		{
			//
			// Cloned parameters blocks can have their own cloned version of top-level labels
			//
			if (labels == null) {
				if (Parent != null)
					return Parent.ParametersBlock.GetLabel (name, block);

				return null;
			}

			object value;
			if (!labels.TryGetValue (name, out value)) {
				return null;
			}

			var label = value as LabeledStatement;
			Block b = block;
			if (label != null) {
				if (IsLabelVisible (label, b))
					return label;

			} else {
				List<LabeledStatement> list = (List<LabeledStatement>) value;
				for (int i = 0; i < list.Count; ++i) {
					label = list[i];
					if (IsLabelVisible (label, b))
						return label;
				}
			}

			return null;
		}

		static bool IsLabelVisible (LabeledStatement label, Block b)
		{
			do {
				if (label.Block == b)
					return true;
				b = b.Parent;
			} while (b != null);

			return false;
		}

		public ParameterInfo GetParameterInfo (Parameter p)
		{
			for (int i = 0; i < parameters.Count; ++i) {
				if (parameters[i] == p)
					return parameter_info[i];
			}

			throw new ArgumentException ("Invalid parameter");
		}

		public ParameterReference GetParameterReference (int index, Location loc)
		{
			return new ParameterReference (parameter_info[index], loc);
		}

		public Statement PerformClone ()
		{
			CloneContext clonectx = new CloneContext ();
			return Clone (clonectx);
		}

		protected void ProcessParameters ()
		{
			if (parameters.Count == 0)
				return;

			parameter_info = new ParameterInfo[parameters.Count];
			for (int i = 0; i < parameter_info.Length; ++i) {
				var p = parameters.FixedParameters[i];
				if (p == null)
					continue;

				// TODO: Should use Parameter only and more block there
				parameter_info[i] = new ParameterInfo (this, i);
				if (p.Name != null)
					AddLocalName (p.Name, parameter_info[i]);
			}
		}

		static LabeledStatement RemapLabeledStatement (LabeledStatement stmt, Block src, Block dst)
		{
			var src_stmts = src.Statements;
			for (int i = 0; i < src_stmts.Count; ++i) {
				if (src_stmts[i] == stmt)
					return (LabeledStatement) dst.Statements[i];
			}

			throw new InternalErrorException ("Should never be reached");
		}

		public override bool Resolve (BlockContext bc)
		{
			// TODO: if ((flags & Flags.Resolved) != 0)

			if (resolved)
				return true;

			resolved = true;

			if (bc.HasSet (ResolveContext.Options.ExpressionTreeConversion))
				flags |= Flags.IsExpressionTree;

			try {
				PrepareAssignmentAnalysis (bc);

				if (!base.Resolve (bc))
					return false;

			} catch (Exception e) {
				if (e is CompletionResult || bc.Report.IsDisabled || e is FatalException || bc.Report.Printer is NullReportPrinter || bc.Module.Compiler.Settings.BreakOnInternalError)
					throw;

				if (bc.CurrentBlock != null) {
					bc.Report.Error (584, bc.CurrentBlock.StartLocation, "Internal compiler error: {0}", e.Message);
				} else {
					bc.Report.Error (587, "Internal compiler error: {0}", e.Message);
				}
			}

			//
			// If an asynchronous body of F is either an expression classified as nothing, or a 
			// statement block where no return statements have expressions, the inferred return type is Task
			//
			if (IsAsync) {
				var am = bc.CurrentAnonymousMethod as AnonymousMethodBody;
				if (am != null && am.ReturnTypeInference != null && !am.ReturnTypeInference.HasBounds (0)) {
					am.ReturnTypeInference = null;
					am.ReturnType = bc.Module.PredefinedTypes.Task.TypeSpec;
					return true;
				}
			}

			return true;
		}

		void PrepareAssignmentAnalysis (BlockContext bc)
		{
			for (int i = 0; i < parameters.Count; ++i) {
				var par = parameters.FixedParameters[i];

				if ((par.ModFlags & Parameter.Modifier.OUT) == 0)
					continue;

				parameter_info [i].VariableInfo = VariableInfo.Create (bc, (Parameter) par);
			}
		}

		public ToplevelBlock ConvertToIterator (IMethodData method, TypeDefinition host, TypeSpec iterator_type, bool is_enumerable)
		{
			var iterator = new Iterator (this, method, host, iterator_type, is_enumerable);
			var stateMachine = new IteratorStorey (iterator);

			state_machine = stateMachine;
			iterator.SetStateMachine (stateMachine);

			var tlb = new ToplevelBlock (host.Compiler, Parameters, Location.Null, Flags.CompilerGenerated);
			tlb.Original = this;
			tlb.state_machine = stateMachine;
			tlb.AddStatement (new Return (iterator, iterator.Location));
			return tlb;
		}

		public ParametersBlock ConvertToAsyncTask (IMemberContext context, TypeDefinition host, ParametersCompiled parameters, TypeSpec returnType, TypeSpec delegateType, Location loc)
		{
			for (int i = 0; i < parameters.Count; i++) {
				Parameter p = parameters[i];
				Parameter.Modifier mod = p.ModFlags;
				if ((mod & Parameter.Modifier.RefOutMask) != 0) {
					host.Compiler.Report.Error (1988, p.Location,
						"Async methods cannot have ref or out parameters");
					return this;
				}

				if (p is ArglistParameter) {
					host.Compiler.Report.Error (4006, p.Location,
						"__arglist is not allowed in parameter list of async methods");
					return this;
				}

				if (parameters.Types[i].IsPointer) {
					host.Compiler.Report.Error (4005, p.Location,
						"Async methods cannot have unsafe parameters");
					return this;
				}
			}

			if (!HasAwait) {
				host.Compiler.Report.Warning (1998, 1, loc,
					"Async block lacks `await' operator and will run synchronously");
			}

			var block_type = host.Module.Compiler.BuiltinTypes.Void;
			var initializer = new AsyncInitializer (this, host, block_type);
			initializer.Type = block_type;
			initializer.DelegateType = delegateType;

			var stateMachine = new AsyncTaskStorey (this, context, initializer, returnType);

			state_machine = stateMachine;
			initializer.SetStateMachine (stateMachine);

			const Flags flags = Flags.CompilerGenerated;

			var b = this is ToplevelBlock ?
				new ToplevelBlock (host.Compiler, Parameters, Location.Null, flags) :
				new ParametersBlock (Parent, parameters, Location.Null, flags | Flags.HasAsyncModifier);

			b.Original = this;
			b.state_machine = stateMachine;
			b.AddStatement (new AsyncInitializerStatement (initializer));
			return b;
		}
	}

	//
	//
	//
	public class ToplevelBlock : ParametersBlock
	{
		LocalVariable this_variable;
		CompilerContext compiler;
		Dictionary<string, object> names;

		List<ExplicitBlock> this_references;

		public ToplevelBlock (CompilerContext ctx, Location loc)
			: this (ctx, ParametersCompiled.EmptyReadOnlyParameters, loc)
		{
		}

		public ToplevelBlock (CompilerContext ctx, ParametersCompiled parameters, Location start, Flags flags = 0)
			: base (parameters, start)
		{
			this.compiler = ctx;
			this.flags = flags;
			top_block = this;

			ProcessParameters ();
		}

		//
		// Recreates a top level block from parameters block. Used for
		// compiler generated methods where the original block comes from
		// explicit child block. This works for already resolved blocks
		// only to ensure we resolve them in the correct flow order
		//
		public ToplevelBlock (ParametersBlock source, ParametersCompiled parameters)
			: base (source, parameters)
		{
			this.compiler = source.TopBlock.compiler;
			top_block = this;
		}

		public bool IsIterator {
			get {
				return (flags & Flags.Iterator) != 0;
			}
			set {
				flags = value ? flags | Flags.Iterator : flags & ~Flags.Iterator;
			}
		}

		public Report Report {
			get {
				return compiler.Report;
			}
		}

		//
		// Used by anonymous blocks to track references of `this' variable
		//
		public List<ExplicitBlock> ThisReferencesFromChildrenBlock {
			get {
				return this_references;
			}
		}

		//
		// Returns the "this" instance variable of this block.
		// See AddThisVariable() for more information.
		//
		public LocalVariable ThisVariable {
			get {
				return this_variable;
			}
		}

		public void AddLocalName (string name, INamedBlockVariable li, bool ignoreChildrenBlocks)
		{
			if (names == null)
				names = new Dictionary<string, object> ();

			object value;
			if (!names.TryGetValue (name, out value)) {
				names.Add (name, li);
				return;
			}

			INamedBlockVariable existing = value as INamedBlockVariable;
			List<INamedBlockVariable> existing_list;
			if (existing != null) {
				existing_list = new List<INamedBlockVariable> ();
				existing_list.Add (existing);
				names[name] = existing_list;
			} else {
				existing_list = (List<INamedBlockVariable>) value;
			}

			//
			// A collision checking between local names
			//
			var variable_block = li.Block.Explicit;
			for (int i = 0; i < existing_list.Count; ++i) {
				existing = existing_list[i];
				Block b = existing.Block.Explicit;

				// Collision at same level
				if (variable_block == b) {
					li.Block.Error_AlreadyDeclared (name, li);
					break;
				}

				// Collision with parent
				Block parent = variable_block;
				while ((parent = parent.Parent) != null) {
					if (parent == b) {
						li.Block.Error_AlreadyDeclared (name, li, "parent or current");
						i = existing_list.Count;
						break;
					}
				}

				if (!ignoreChildrenBlocks && variable_block.Parent != b.Parent) {
					// Collision with children
					while ((b = b.Parent) != null) {
						if (variable_block == b) {
							li.Block.Error_AlreadyDeclared (name, li, "child");
							i = existing_list.Count;
							break;
						}
					}
				}
			}

			existing_list.Add (li);
		}

		public void AddLabel (string name, LabeledStatement label)
		{
			if (labels == null)
				labels = new Dictionary<string, object> ();

			object value;
			if (!labels.TryGetValue (name, out value)) {
				labels.Add (name, label);
				return;
			}

			LabeledStatement existing = value as LabeledStatement;
			List<LabeledStatement> existing_list;
			if (existing != null) {
				existing_list = new List<LabeledStatement> ();
				existing_list.Add (existing);
				labels[name] = existing_list;
			} else {
				existing_list = (List<LabeledStatement>) value;
			}

			//
			// A collision checking between labels
			//
			for (int i = 0; i < existing_list.Count; ++i) {
				existing = existing_list[i];
				Block b = existing.Block;

				// Collision at same level
				if (label.Block == b) {
					Report.SymbolRelatedToPreviousError (existing.loc, name);
					Report.Error (140, label.loc, "The label `{0}' is a duplicate", name);
					break;
				}

				// Collision with parent
				b = label.Block;
				while ((b = b.Parent) != null) {
					if (existing.Block == b) {
						Report.Error (158, label.loc,
							"The label `{0}' shadows another label by the same name in a contained scope", name);
						i = existing_list.Count;
						break;
					}
				}

				// Collision with with children
				b = existing.Block;
				while ((b = b.Parent) != null) {
					if (label.Block == b) {
						Report.Error (158, label.loc,
							"The label `{0}' shadows another label by the same name in a contained scope", name);
						i = existing_list.Count;
						break;
					}
				}
			}

			existing_list.Add (label);
		}

		public void AddThisReferenceFromChildrenBlock (ExplicitBlock block)
		{
			if (this_references == null)
				this_references = new List<ExplicitBlock> ();

			if (!this_references.Contains (block))
				this_references.Add (block);
		}

		public void RemoveThisReferenceFromChildrenBlock (ExplicitBlock block)
		{
			this_references.Remove (block);
		}

		//
		// Creates an arguments set from all parameters, useful for method proxy calls
		//
		public Arguments GetAllParametersArguments ()
		{
			int count = parameters.Count;
			Arguments args = new Arguments (count);
			for (int i = 0; i < count; ++i) {
				var pi = parameter_info[i];
				var arg_expr = GetParameterReference (i, pi.Location);

				Argument.AType atype_modifier;
				switch (pi.Parameter.ParameterModifier & Parameter.Modifier.RefOutMask) {
				case Parameter.Modifier.REF:
					atype_modifier = Argument.AType.Ref;
					break;
				case Parameter.Modifier.OUT:
					atype_modifier = Argument.AType.Out;
					break;
				default:
					atype_modifier = 0;
					break;
				}

				args.Add (new Argument (arg_expr, atype_modifier));
			}

			return args;
		}

		//
		// Lookup inside a block, the returned value can represent 3 states
		//
		// true+variable: A local name was found and it's valid
		// false+variable: A local name was found in a child block only
		// false+null: No local name was found
		//
		public bool GetLocalName (string name, Block block, ref INamedBlockVariable variable)
		{
			if (names == null)
				return false;

			object value;
			if (!names.TryGetValue (name, out value))
				return false;

			variable = value as INamedBlockVariable;
			Block b = block;
			if (variable != null) {
				do {
					if (variable.Block == b.Original)
						return true;

					b = b.Parent;
				} while (b != null);

				b = variable.Block;
				do {
					if (block == b)
						return false;

					b = b.Parent;
				} while (b != null);
			} else {
				List<INamedBlockVariable> list = (List<INamedBlockVariable>) value;
				for (int i = 0; i < list.Count; ++i) {
					variable = list[i];
					do {
						if (variable.Block == b.Original)
							return true;

						b = b.Parent;
					} while (b != null);

					b = variable.Block;
					do {
						if (block == b)
							return false;

						b = b.Parent;
					} while (b != null);

					b = block;
				}
			}

			variable = null;
			return false;
		}

		public void IncludeBlock (ParametersBlock pb, ToplevelBlock block)
		{
			if (block.names != null) {
				foreach (var n in block.names) {
					var variable = n.Value as INamedBlockVariable;
					if (variable != null) {
						if (variable.Block.ParametersBlock == pb)
							AddLocalName (n.Key, variable, false);
						continue;
					}

					foreach (var v in (List<INamedBlockVariable>) n.Value)
						if (v.Block.ParametersBlock == pb)
							AddLocalName (n.Key, v, false);
				}
			}
		}

		// <summary>
		//   This is used by non-static `struct' constructors which do not have an
		//   initializer - in this case, the constructor must initialize all of the
		//   struct's fields.  To do this, we add a "this" variable and use the flow
		//   analysis code to ensure that it's been fully initialized before control
		//   leaves the constructor.
		// </summary>
		public void AddThisVariable (BlockContext bc)
		{
			if (this_variable != null)
				throw new InternalErrorException (StartLocation.ToString ());

			this_variable = new LocalVariable (this, "this", LocalVariable.Flags.IsThis | LocalVariable.Flags.Used, StartLocation);
			this_variable.Type = bc.CurrentType;
			this_variable.PrepareAssignmentAnalysis (bc);
		}

		public override void CheckControlExit (FlowAnalysisContext fc, DefiniteAssignmentBitSet dat)
		{
			//
			// If we're a non-static struct constructor which doesn't have an
			// initializer, then we must initialize all of the struct's fields.
			//
			if (this_variable != null)
				this_variable.IsThisAssigned (fc, this);

			base.CheckControlExit (fc, dat);
		}

		public override void Emit (EmitContext ec)
		{
			if (Report.Errors > 0)
				return;

			try {
			if (IsCompilerGenerated) {
				using (ec.With (BuilderContext.Options.OmitDebugInfo, true)) {
					base.Emit (ec);
				}
			} else {
				base.Emit (ec);
			}

			//
			// If `HasReturnLabel' is set, then we already emitted a
			// jump to the end of the method, so we must emit a `ret'
			// there.
			//
			// Unfortunately, System.Reflection.Emit automatically emits
			// a leave to the end of a finally block.  This is a problem
			// if no code is following the try/finally block since we may
			// jump to a point after the end of the method.
			// As a workaround, we're always creating a return label in
			// this case.
			//
			if (ec.HasReturnLabel || HasReachableClosingBrace) {
				if (ec.HasReturnLabel)
					ec.MarkLabel (ec.ReturnLabel);

				if (ec.EmitAccurateDebugInfo && !IsCompilerGenerated)
					ec.Mark (EndLocation);

				if (ec.ReturnType.Kind != MemberKind.Void)
					ec.Emit (OpCodes.Ldloc, ec.TemporaryReturn ());

				ec.Emit (OpCodes.Ret);
			}

			} catch (Exception e) {
				throw new InternalErrorException (e, StartLocation);
			}
		}

		public bool Resolve (BlockContext bc, IMethodData md)
		{
			if (resolved)
				return true;

			var errors = bc.Report.Errors;

			base.Resolve (bc);

			if (bc.Report.Errors > errors)
				return false;

			MarkReachable (new Reachability ());

			if (HasReachableClosingBrace && bc.ReturnType.Kind != MemberKind.Void) {
				// TODO: var md = bc.CurrentMemberDefinition;
				bc.Report.Error (161, md.Location, "`{0}': not all code paths return a value", md.GetSignatureForError ());
			}

			if ((flags & Flags.NoFlowAnalysis) != 0)
				return true;

			var fc = new FlowAnalysisContext (bc.Module.Compiler, this, bc.AssignmentInfoOffset);
			try {
				FlowAnalysis (fc);
			} catch (Exception e) {
				throw new InternalErrorException (e, StartLocation);
			}

			return true;
		}
	}
	
	public class SwitchLabel : Statement
	{
		Constant converted;
		Expression label;

		Label? il_label;

		//
		// if expr == null, then it is the default case.
		//
		public SwitchLabel (Expression expr, Location l)
		{
			label = expr;
			loc = l;
		}

		public bool IsDefault {
			get {
				return label == null;
			}
		}

		public Expression Label {
			get {
				return label;
			}
		}

		public Location Location {
			get {
				return loc;
			}
		}

		public Constant Converted {
			get {
				return converted;
			}
			set {
				converted = value; 
			}
		}

		public bool SectionStart { get; set; }

		public Label GetILLabel (EmitContext ec)
		{
			if (il_label == null){
				il_label = ec.DefineLabel ();
			}

			return il_label.Value;
		}

		protected override void DoEmit (EmitContext ec)
		{
			ec.MarkLabel (GetILLabel (ec));
		}

		protected override bool DoFlowAnalysis (FlowAnalysisContext fc)
		{
			if (!SectionStart)
				return false;

			fc.DefiniteAssignment = new DefiniteAssignmentBitSet (fc.SwitchInitialDefinitiveAssignment);
			return false;
		}

		public override bool Resolve (BlockContext bc)
		{
			if (ResolveAndReduce (bc))
				bc.Switch.RegisterLabel (bc, this);

			return true;
		}

		//
		// Resolves the expression, reduces it to a literal if possible
		// and then converts it to the requested type.
		//
		bool ResolveAndReduce (BlockContext rc)
		{
			if (IsDefault)
				return true;

			var c = label.ResolveLabelConstant (rc);
			if (c == null)
				return false;

			if (rc.Switch.IsNullable && c is NullLiteral) {
				converted = c;
				return true;
			}

			converted = c.ImplicitConversionRequired (rc, rc.Switch.SwitchType);
			return converted != null;
		}

		public void Error_AlreadyOccurs (ResolveContext ec, SwitchLabel collision_with)
		{
			ec.Report.SymbolRelatedToPreviousError (collision_with.loc, null);
			ec.Report.Error (152, loc, "The label `{0}' already occurs in this switch statement", GetSignatureForError ());
		}

		protected override void CloneTo (CloneContext clonectx, Statement target)
		{
			var t = (SwitchLabel) target;
			if (label != null)
				t.label = label.Clone (clonectx);
		}

		public override object Accept (StructuralVisitor visitor)
		{
			return visitor.Visit (this);
		}

		public string GetSignatureForError ()
		{
			string label;
			if (converted == null)
				label = "default";
			else
				label = converted.GetValueAsLiteral ();

			return string.Format ("case {0}:", label);
		}
	}

	public class Switch : LoopStatement
	{
		// structure used to hold blocks of keys while calculating table switch
		sealed class LabelsRange : IComparable<LabelsRange>
		{
			public readonly long min;
			public long max;
			public readonly List<long> label_values;

			public LabelsRange (long value)
			{
				min = max = value;
				label_values = new List<long> ();
				label_values.Add (value);
			}

			public LabelsRange (long min, long max, ICollection<long> values)
			{
				this.min = min;
				this.max = max;
				this.label_values = new List<long> (values);
			}

			public long Range {
				get {
					return max - min + 1;
				}
			}

			public bool AddValue (long value)
			{
				var gap = value - min + 1;
				// Ensure the range has > 50% occupancy
				if (gap > 2 * (label_values.Count + 1) || gap <= 0)
					return false;

				max = value;
				label_values.Add (value);
				return true;
			}

			public int CompareTo (LabelsRange other)
			{
				int nLength = label_values.Count;
				int nLengthOther = other.label_values.Count;
				if (nLengthOther == nLength)
					return (int) (other.min - min);

				return nLength - nLengthOther;
			}
		}

		sealed class DispatchStatement : Statement
		{
			readonly Switch body;

			public DispatchStatement (Switch body)
			{
				this.body = body;
			}

			protected override void CloneTo (CloneContext clonectx, Statement target)
			{
				throw new NotImplementedException ();
			}

			protected override bool DoFlowAnalysis (FlowAnalysisContext fc)
			{
				return false;
			}

			protected override void DoEmit (EmitContext ec)
			{
				body.EmitDispatch (ec);
			}
		}

		class MissingBreak : Statement
		{
			readonly SwitchLabel label;

			public MissingBreak (SwitchLabel sl)
			{
				this.label = sl;
				this.loc = sl.loc;
			}

			public bool FallOut { get; set; }

			protected override void DoEmit (EmitContext ec)
			{
			}

			protected override void CloneTo (CloneContext clonectx, Statement target)
			{
			}

			protected override bool DoFlowAnalysis (FlowAnalysisContext fc)
			{
				if (FallOut) {
					fc.Report.Error (8070, loc, "Control cannot fall out of switch statement through final case label `{0}'",
						label.GetSignatureForError ());
				} else {
					fc.Report.Error (163, loc, "Control cannot fall through from one case label `{0}' to another",
						label.GetSignatureForError ());
				}
				return true;
			}
		}

		public Expression Expr;

		//
		// Mapping of all labels to their SwitchLabels
		//
		Dictionary<long, SwitchLabel> labels;
		Dictionary<string, SwitchLabel> string_labels;
		List<SwitchLabel> case_labels;

		List<Tuple<GotoCase, Constant>> goto_cases;
		List<DefiniteAssignmentBitSet> end_reachable_das;

		/// <summary>
		///   The governing switch type
		/// </summary>
		public TypeSpec SwitchType;

		Expression new_expr;

		SwitchLabel case_null;
		SwitchLabel case_default;

		Label defaultLabel, nullLabel;
		VariableReference value;
		ExpressionStatement string_dictionary;
		FieldExpr switch_cache_field;
		ExplicitBlock block;
		bool end_reachable;

		//
		// Nullable Types support
		//
		Nullable.Unwrap unwrap;

		public Switch (Expression e, ExplicitBlock block, Location l)
			: base (block)
		{
			Expr = e;
			this.block = block;
			loc = l;
		}

		public SwitchLabel ActiveLabel { get; set; }

		public ExplicitBlock Block {
			get {
				return block;
			}
		}

		public SwitchLabel DefaultLabel {
			get {
				return case_default;
			}
		}

		public bool IsNullable {
			get {
				return unwrap != null;
			}
		}

		public List<SwitchLabel> RegisteredLabels {
			get {
				return case_labels;
			}
		}

		//
		// Determines the governing type for a switch.  The returned
		// expression might be the expression from the switch, or an
		// expression that includes any potential conversions to
		//
		static Expression SwitchGoverningType (ResolveContext rc, Expression expr, bool unwrapExpr)
		{
			switch (expr.Type.BuiltinType) {
			case BuiltinTypeSpec.Type.Byte:
			case BuiltinTypeSpec.Type.SByte:
			case BuiltinTypeSpec.Type.UShort:
			case BuiltinTypeSpec.Type.Short:
			case BuiltinTypeSpec.Type.UInt:
			case BuiltinTypeSpec.Type.Int:
			case BuiltinTypeSpec.Type.ULong:
			case BuiltinTypeSpec.Type.Long:
			case BuiltinTypeSpec.Type.Char:
			case BuiltinTypeSpec.Type.String:
			case BuiltinTypeSpec.Type.Bool:
				return expr;
			}

			if (expr.Type.IsEnum)
				return expr;

			//
			// Try to find a *user* defined implicit conversion.
			//
			// If there is no implicit conversion, or if there are multiple
			// conversions, we have to report an error
			//
			Expression converted = null;
			foreach (TypeSpec tt in rc.Module.PredefinedTypes.SwitchUserTypes) {

				if (!unwrapExpr && tt.IsNullableType && expr.Type.IsNullableType)
					break;

				var restr = Convert.UserConversionRestriction.ImplicitOnly |
					Convert.UserConversionRestriction.ProbingOnly;

				if (unwrapExpr)
					restr |= Convert.UserConversionRestriction.NullableSourceOnly;

				var e = Convert.UserDefinedConversion (rc, expr, tt, restr, Location.Null);
				if (e == null)
					continue;

				//
				// Ignore over-worked ImplicitUserConversions that do
				// an implicit conversion in addition to the user conversion.
				// 
				var uc = e as UserCast;
				if (uc == null)
					continue;

				if (converted != null){
//					rc.Report.ExtraInformation (loc, "(Ambiguous implicit user defined conversion in previous ");
					return null;
				}

				converted = e;
			}
			return converted;
		}

		public static TypeSpec[] CreateSwitchUserTypes (ModuleContainer module, TypeSpec nullable)
		{
			var types = module.Compiler.BuiltinTypes;

			// LAMESPEC: For some reason it does not contain bool which looks like csc bug
			TypeSpec[] stypes = new[] {
				types.SByte,
				types.Byte,
				types.Short,
				types.UShort,
				types.Int,
				types.UInt,
				types.Long,
				types.ULong,
				types.Char,
				types.String
			};

			if (nullable != null) {

				Array.Resize (ref stypes, stypes.Length + 9);

				for (int i = 0; i < 9; ++i) {
					stypes [10 + i] = nullable.MakeGenericType (module, new [] { stypes [i] });
				}
			}

			return stypes;
		}

		public void RegisterLabel (BlockContext rc, SwitchLabel sl)
		{
			case_labels.Add (sl);

			if (sl.IsDefault) {
				if (case_default != null) {
					sl.Error_AlreadyOccurs (rc, case_default);
				} else {
					case_default = sl;
				}

				return;
			}

			try {
				if (string_labels != null) {
					string string_value = sl.Converted.GetValue () as string;
					if (string_value == null)
						case_null = sl;
					else
						string_labels.Add (string_value, sl);
				} else {
					if (sl.Converted is NullLiteral) {
						case_null = sl;
					} else {
						labels.Add (sl.Converted.GetValueAsLong (), sl);
					}
				}
			} catch (ArgumentException) {
				if (string_labels != null)
					sl.Error_AlreadyOccurs (rc, string_labels[(string) sl.Converted.GetValue ()]);
				else
					sl.Error_AlreadyOccurs (rc, labels[sl.Converted.GetValueAsLong ()]);
			}
		}
		
		//
		// This method emits code for a lookup-based switch statement (non-string)
		// Basically it groups the cases into blocks that are at least half full,
		// and then spits out individual lookup opcodes for each block.
		// It emits the longest blocks first, and short blocks are just
		// handled with direct compares.
		//
		void EmitTableSwitch (EmitContext ec, Expression val)
		{
			if (labels != null && labels.Count > 0) {
				List<LabelsRange> ranges;
				if (string_labels != null) {
					// We have done all hard work for string already
					// setup single range only
					ranges = new List<LabelsRange> (1);
					ranges.Add (new LabelsRange (0, labels.Count - 1, labels.Keys));
				} else {
					var element_keys = new long[labels.Count];
					labels.Keys.CopyTo (element_keys, 0);
					Array.Sort (element_keys);

					//
					// Build possible ranges of switch labes to reduce number
					// of comparisons
					//
					ranges = new List<LabelsRange> (element_keys.Length);
					var range = new LabelsRange (element_keys[0]);
					ranges.Add (range);
					for (int i = 1; i < element_keys.Length; ++i) {
						var l = element_keys[i];
						if (range.AddValue (l))
							continue;

						range = new LabelsRange (l);
						ranges.Add (range);
					}

					// sort the blocks so we can tackle the largest ones first
					ranges.Sort ();
				}

				Label lbl_default = defaultLabel;
				TypeSpec compare_type = SwitchType.IsEnum ? EnumSpec.GetUnderlyingType (SwitchType) : SwitchType;

				for (int range_index = ranges.Count - 1; range_index >= 0; --range_index) {
					LabelsRange kb = ranges[range_index];
					lbl_default = (range_index == 0) ? defaultLabel : ec.DefineLabel ();

					// Optimize small ranges using simple equality check
					if (kb.Range <= 2) {
						foreach (var key in kb.label_values) {
							SwitchLabel sl = labels[key];
							if (sl == case_default || sl == case_null)
								continue;

							if (sl.Converted.IsZeroInteger) {
								val.EmitBranchable (ec, sl.GetILLabel (ec), false);
							} else {
								val.Emit (ec);
								sl.Converted.Emit (ec);
								ec.Emit (OpCodes.Beq, sl.GetILLabel (ec));
							}
						}
					} else {
						// TODO: if all the keys in the block are the same and there are
						//       no gaps/defaults then just use a range-check.
						if (compare_type.BuiltinType == BuiltinTypeSpec.Type.Long || compare_type.BuiltinType == BuiltinTypeSpec.Type.ULong) {
							// TODO: optimize constant/I4 cases

							// check block range (could be > 2^31)
							val.Emit (ec);
							ec.EmitLong (kb.min);
							ec.Emit (OpCodes.Blt, lbl_default);

							val.Emit (ec);
							ec.EmitLong (kb.max);
							ec.Emit (OpCodes.Bgt, lbl_default);

							// normalize range
							val.Emit (ec);
							if (kb.min != 0) {
								ec.EmitLong (kb.min);
								ec.Emit (OpCodes.Sub);
							}

							ec.Emit (OpCodes.Conv_I4);	// assumes < 2^31 labels!
						} else {
							// normalize range
							val.Emit (ec);
							int first = (int) kb.min;
							if (first > 0) {
								ec.EmitInt (first);
								ec.Emit (OpCodes.Sub);
							} else if (first < 0) {
								ec.EmitInt (-first);
								ec.Emit (OpCodes.Add);
							}
						}

						// first, build the list of labels for the switch
						int iKey = 0;
						long cJumps = kb.Range;
						Label[] switch_labels = new Label[cJumps];
						for (int iJump = 0; iJump < cJumps; iJump++) {
							var key = kb.label_values[iKey];
							if (key == kb.min + iJump) {
								switch_labels[iJump] = labels[key].GetILLabel (ec);
								iKey++;
							} else {
								switch_labels[iJump] = lbl_default;
							}
						}

						// emit the switch opcode
						ec.Emit (OpCodes.Switch, switch_labels);
					}

					// mark the default for this block
					if (range_index != 0)
						ec.MarkLabel (lbl_default);
				}

				// the last default just goes to the end
				if (ranges.Count > 0)
					ec.Emit (OpCodes.Br, lbl_default);
			}
		}
		
		public SwitchLabel FindLabel (Constant value)
		{
			SwitchLabel sl = null;

			if (string_labels != null) {
				string s = value.GetValue () as string;
				if (s == null) {
					if (case_null != null)
						sl = case_null;
					else if (case_default != null)
						sl = case_default;
				} else {
					string_labels.TryGetValue (s, out sl);
				}
			} else {
				if (value is NullLiteral) {
					sl = case_null;
				} else {
					labels.TryGetValue (value.GetValueAsLong (), out sl);
				}
			}

			return sl;
		}

		protected override bool DoFlowAnalysis (FlowAnalysisContext fc)
		{
			Expr.FlowAnalysis (fc);

			var prev_switch = fc.SwitchInitialDefinitiveAssignment;
			var InitialDefinitiveAssignment = fc.DefiniteAssignment;
			fc.SwitchInitialDefinitiveAssignment = InitialDefinitiveAssignment;

			block.FlowAnalysis (fc);

			fc.SwitchInitialDefinitiveAssignment = prev_switch;

			if (end_reachable_das != null) {
				var sections_das = DefiniteAssignmentBitSet.And (end_reachable_das);
				InitialDefinitiveAssignment |= sections_das;
				end_reachable_das = null;
			}

			fc.DefiniteAssignment = InitialDefinitiveAssignment;

			return case_default != null && !end_reachable;
		}

		public override bool Resolve (BlockContext ec)
		{
			Expr = Expr.Resolve (ec);
			if (Expr == null)
				return false;

			//
			// LAMESPEC: User conversion from non-nullable governing type has a priority
			//
			new_expr = SwitchGoverningType (ec, Expr, false);

			if (new_expr == null) {
				if (Expr.Type.IsNullableType) {
					unwrap = Nullable.Unwrap.Create (Expr, false);
					if (unwrap == null)
						return false;

					//
					// Unwrap + user conversion using non-nullable type is not allowed but user operator
					// involving nullable Expr and nullable governing type is
					//
					new_expr = SwitchGoverningType (ec, unwrap, true);
				}
			}

			if (new_expr == null) {
				if (Expr.Type != InternalType.ErrorType) {
					ec.Report.Error (151, loc,
						"A switch expression of type `{0}' cannot be converted to an integral type, bool, char, string, enum or nullable type",
						Expr.Type.GetSignatureForError ());
				}

				return false;
			}

			SwitchType = new_expr.Type;
			if (SwitchType.IsNullableType) {
				new_expr = unwrap = Nullable.Unwrap.Create (new_expr, true);
				SwitchType = Nullable.NullableInfo.GetUnderlyingType (SwitchType);
			}

			if (SwitchType.BuiltinType == BuiltinTypeSpec.Type.Bool && ec.Module.Compiler.Settings.Version == LanguageVersion.ISO_1) {
				ec.Report.FeatureIsNotAvailable (ec.Module.Compiler, loc, "switch expression of boolean type");
				return false;
			}

			if (block.Statements.Count == 0)
				return true;

			if (SwitchType.BuiltinType == BuiltinTypeSpec.Type.String) {
				string_labels = new Dictionary<string, SwitchLabel> ();
			} else {
				labels = new Dictionary<long, SwitchLabel> ();
			}

			case_labels = new List<SwitchLabel> ();

			var constant = new_expr as Constant;

			//
			// Don't need extra variable for constant switch or switch with
			// only default case
			//
			if (constant == null) {
				//
				// Store switch expression for comparison purposes
				//
				value = new_expr as VariableReference;
				if (value == null && !HasOnlyDefaultSection ()) {
					var current_block = ec.CurrentBlock;
					ec.CurrentBlock = Block;
					// Create temporary variable inside switch scope
					value = TemporaryVariableReference.Create (SwitchType, ec.CurrentBlock, loc);
					value.Resolve (ec);
					ec.CurrentBlock = current_block;
				}
			}

			Switch old_switch = ec.Switch;
			ec.Switch = this;
			var parent_los = ec.EnclosingLoopOrSwitch;
			ec.EnclosingLoopOrSwitch = this;

			var ok = Statement.Resolve (ec);

			ec.EnclosingLoopOrSwitch = parent_los;
			ec.Switch = old_switch;

			//
			// Check if all goto cases are valid. Needs to be done after switch
			// is resolved because goto can jump forward in the scope.
			//
			if (goto_cases != null) {
				foreach (var gc in goto_cases) {
					if (gc.Item1 == null) {
						if (DefaultLabel == null) {
							Goto.Error_UnknownLabel (ec, "default", loc);
						}

						continue;
					}

					var sl = FindLabel (gc.Item2);
					if (sl == null) {
						Goto.Error_UnknownLabel (ec, "case " + gc.Item2.GetValueAsLiteral (), loc);
					} else {
						gc.Item1.Label = sl;
					}
				}
			}

			if (!ok)
				return false;

			if (constant == null && SwitchType.BuiltinType == BuiltinTypeSpec.Type.String && string_labels.Count > 6) {
				ResolveStringSwitchMap (ec);
			}

			//
			// Anonymous storey initialization has to happen before
			// any generated switch dispatch
			//
			block.InsertStatement (0, new DispatchStatement (this));

			return true;
		}

		bool HasOnlyDefaultSection ()
		{
			for (int i = 0; i < block.Statements.Count; ++i) {
				var s = block.Statements[i] as SwitchLabel;

				if (s == null || s.IsDefault)
					continue;

				return false;
			}

			return true;
		}

		public override Reachability MarkReachable (Reachability rc)
		{
			if (rc.IsUnreachable)
				return rc;

			base.MarkReachable (rc);

			block.MarkReachableScope (rc);

			if (block.Statements.Count == 0)
				return rc;

			SwitchLabel constant_label = null;
			var constant = new_expr as Constant;

			if (constant != null) {
				constant_label = FindLabel (constant) ?? case_default;
				if (constant_label == null) {
					block.Statements.RemoveAt (0);
					return rc;
				}
			}

			var section_rc = new Reachability ();
			SwitchLabel prev_label = null;

			for (int i = 0; i < block.Statements.Count; ++i) {
				var s = block.Statements[i];
				var sl = s as SwitchLabel;

				if (sl != null && sl.SectionStart) {
					//
					// Section is marked already via goto case
					//
					if (!sl.IsUnreachable) {
						section_rc = new Reachability ();
						continue;
					}

					if (constant_label != null && constant_label != sl)
						section_rc = Reachability.CreateUnreachable ();
					else if (section_rc.IsUnreachable) {
						section_rc = new Reachability ();
					} else {
						if (prev_label != null) {
							sl.SectionStart = false;
							s = new MissingBreak (prev_label);
							s.MarkReachable (rc);
							block.Statements.Insert (i - 1, s);
							++i;
						}
					}

					prev_label = sl;
				}

				section_rc = s.MarkReachable (section_rc);
			}

			if (!section_rc.IsUnreachable && prev_label != null) {
				prev_label.SectionStart = false;
				var s = new MissingBreak (prev_label) {
					FallOut = true
				};

				s.MarkReachable (rc);
				block.Statements.Add (s);
			}

			//
			// Reachability can affect parent only when all possible paths are handled but
			// we still need to run reachability check on switch body to check for fall-through
			//
			if (case_default == null && constant_label == null)
				return rc;

			//
			// We have at least one local exit from the switch
			//
			if (end_reachable)
				return rc;

			return Reachability.CreateUnreachable ();
		}

		public void RegisterGotoCase (GotoCase gotoCase, Constant value)
		{
			if (goto_cases == null)
				goto_cases = new List<Tuple<GotoCase, Constant>> ();

			goto_cases.Add (Tuple.Create (gotoCase, value));
		}

		//
		// Converts string switch into string hashtable
		//
		void ResolveStringSwitchMap (ResolveContext ec)
		{
			FullNamedExpression string_dictionary_type;
			if (ec.Module.PredefinedTypes.Dictionary.Define ()) {
				string_dictionary_type = new TypeExpression (
					ec.Module.PredefinedTypes.Dictionary.TypeSpec.MakeGenericType (ec,
						new [] { ec.BuiltinTypes.String, ec.BuiltinTypes.Int }),
					loc);
			} else if (ec.Module.PredefinedTypes.Hashtable.Define ()) {
				string_dictionary_type = new TypeExpression (ec.Module.PredefinedTypes.Hashtable.TypeSpec, loc);
			} else {
				ec.Module.PredefinedTypes.Dictionary.Resolve ();
				return;
			}

			var ctype = ec.CurrentMemberDefinition.Parent.PartialContainer;
			Field field = new Field (ctype, string_dictionary_type,
				Modifiers.STATIC | Modifiers.PRIVATE | Modifiers.COMPILER_GENERATED,
				new MemberName (CompilerGeneratedContainer.MakeName (null, "f", "switch$map", ec.Module.CounterSwitchTypes++), loc), null);
			if (!field.Define ())
				return;
			ctype.AddField (field);

			var init = new List<Expression> ();
			int counter = -1;
			labels = new Dictionary<long, SwitchLabel> (string_labels.Count);
			string value = null;

			foreach (SwitchLabel sl in case_labels) {

				if (sl.SectionStart)
					labels.Add (++counter, sl);

				if (sl == case_default || sl == case_null)
					continue;

				value = (string) sl.Converted.GetValue ();
				var init_args = new List<Expression> (2);
				init_args.Add (new StringLiteral (ec.BuiltinTypes, value, sl.Location));

				sl.Converted = new IntConstant (ec.BuiltinTypes, counter, loc);
				init_args.Add (sl.Converted);

				init.Add (new CollectionElementInitializer (init_args, loc));
			}
	
			Arguments args = new Arguments (1);
			args.Add (new Argument (new IntConstant (ec.BuiltinTypes, init.Count, loc)));
			Expression initializer = new NewInitialize (string_dictionary_type, args,
				new CollectionOrObjectInitializers (init, loc), loc);

			switch_cache_field = new FieldExpr (field, loc);
			string_dictionary = new SimpleAssign (switch_cache_field, initializer.Resolve (ec));
		}

		void DoEmitStringSwitch (EmitContext ec)
		{
			Label l_initialized = ec.DefineLabel ();

			//
			// Skip initialization when value is null
			//
			value.EmitBranchable (ec, nullLabel, false);

			//
			// Check if string dictionary is initialized and initialize
			//
			switch_cache_field.EmitBranchable (ec, l_initialized, true);
			using (ec.With (BuilderContext.Options.OmitDebugInfo, true)) {
				string_dictionary.EmitStatement (ec);
			}
			ec.MarkLabel (l_initialized);

			LocalTemporary string_switch_variable = new LocalTemporary (ec.BuiltinTypes.Int);

			ResolveContext rc = new ResolveContext (ec.MemberContext);

			if (switch_cache_field.Type.IsGeneric) {
				Arguments get_value_args = new Arguments (2);
				get_value_args.Add (new Argument (value));
				get_value_args.Add (new Argument (string_switch_variable, Argument.AType.Out));
				Expression get_item = new Invocation (new MemberAccess (switch_cache_field, "TryGetValue", loc), get_value_args).Resolve (rc);
				if (get_item == null)
					return;

				//
				// A value was not found, go to default case
				//
				get_item.EmitBranchable (ec, defaultLabel, false);
			} else {
				Arguments get_value_args = new Arguments (1);
				get_value_args.Add (new Argument (value));

				Expression get_item = new ElementAccess (switch_cache_field, get_value_args, loc).Resolve (rc);
				if (get_item == null)
					return;

				LocalTemporary get_item_object = new LocalTemporary (ec.BuiltinTypes.Object);
				get_item_object.EmitAssign (ec, get_item, true, false);
				ec.Emit (OpCodes.Brfalse, defaultLabel);

				ExpressionStatement get_item_int = (ExpressionStatement) new SimpleAssign (string_switch_variable,
					new Cast (new TypeExpression (ec.BuiltinTypes.Int, loc), get_item_object, loc)).Resolve (rc);

				get_item_int.EmitStatement (ec);
				get_item_object.Release (ec);
			}

			EmitTableSwitch (ec, string_switch_variable);
			string_switch_variable.Release (ec);
		}

		//
		// Emits switch using simple if/else comparison for small label count (4 + optional default)
		//
		void EmitShortSwitch (EmitContext ec)
		{
			MethodSpec equal_method = null;
			if (SwitchType.BuiltinType == BuiltinTypeSpec.Type.String) {
				equal_method = ec.Module.PredefinedMembers.StringEqual.Resolve (loc);
			}

			if (equal_method != null) {
				value.EmitBranchable (ec, nullLabel, false);
			}

			for (int i = 0; i < case_labels.Count; ++i) {
				var label = case_labels [i];
				if (label == case_default || label == case_null)
					continue;

				var constant = label.Converted;

				if (equal_method != null) {
					value.Emit (ec);
					constant.Emit (ec);

					var call = new CallEmitter ();
					call.EmitPredefined (ec, equal_method, new Arguments (0));
					ec.Emit (OpCodes.Brtrue, label.GetILLabel (ec));
					continue;
				}

				if (constant.IsZeroInteger && constant.Type.BuiltinType != BuiltinTypeSpec.Type.Long && constant.Type.BuiltinType != BuiltinTypeSpec.Type.ULong) {
					value.EmitBranchable (ec, label.GetILLabel (ec), false);
					continue;
				}

				value.Emit (ec);
				constant.Emit (ec);
				ec.Emit (OpCodes.Beq, label.GetILLabel (ec));
			}

			ec.Emit (OpCodes.Br, defaultLabel);
		}

		void EmitDispatch (EmitContext ec)
		{
			if (value == null) {
				//
				// Constant switch, we've already done the work if there is only 1 label
				// referenced
				//
				int reachable = 0;
				foreach (var sl in case_labels) {
					if (sl.IsUnreachable)
						continue;

					if (reachable++ > 0) {
						var constant = (Constant) new_expr;
						var constant_label = FindLabel (constant) ?? case_default;

						ec.Emit (OpCodes.Br, constant_label.GetILLabel (ec));
						break;
					}
				}

				return;
			}

			if (string_dictionary != null) {
				DoEmitStringSwitch (ec);
			} else if (case_labels.Count < 4 || string_labels != null) {
				EmitShortSwitch (ec);
			} else {
				EmitTableSwitch (ec, value);
			}
		}

		protected override void DoEmit (EmitContext ec)
		{
			//
			// Setup the codegen context
			//
			Label old_end = ec.LoopEnd;
			Switch old_switch = ec.Switch;

			ec.LoopEnd = ec.DefineLabel ();
			ec.Switch = this;

			defaultLabel = case_default == null ? ec.LoopEnd : case_default.GetILLabel (ec);
			nullLabel = case_null == null ? defaultLabel : case_null.GetILLabel (ec);

			if (value != null) {
				ec.Mark (loc);
				if (IsNullable) {
					unwrap.EmitCheck (ec);
					ec.Emit (OpCodes.Brfalse, nullLabel);
					value.EmitAssign (ec, new_expr, false, false);
				} else if (new_expr != value) {
					value.EmitAssign (ec, new_expr, false, false);
				}


				//
				// Next statement is compiler generated we don't need extra
				// nop when we can use the statement for sequence point
				//
				ec.Mark (block.StartLocation);
				block.IsCompilerGenerated = true;
			} else {
				new_expr.EmitSideEffect (ec);
			}

			block.Emit (ec);

			// Restore context state. 
			ec.MarkLabel (ec.LoopEnd);

			//
			// Restore the previous context
			//
			ec.LoopEnd = old_end;
			ec.Switch = old_switch;
		}

		protected override void CloneTo (CloneContext clonectx, Statement t)
		{
			Switch target = (Switch) t;

			target.Expr = Expr.Clone (clonectx);
			target.Statement = target.block = (ExplicitBlock) block.Clone (clonectx);
		}
		
		public override object Accept (StructuralVisitor visitor)
		{
			return visitor.Visit (this);
		}

		public override void AddEndDefiniteAssignment (FlowAnalysisContext fc)
		{
			if (case_default == null && !(new_expr is Constant))
				return;

			if (end_reachable_das == null)
				end_reachable_das = new List<DefiniteAssignmentBitSet> ();

			end_reachable_das.Add (fc.DefiniteAssignment);
		}

		public override void SetEndReachable ()
		{
			end_reachable = true;
		}
	}

	// A place where execution can restart in a state machine
	public abstract class ResumableStatement : Statement
	{
		bool prepared;
		protected Label resume_point;

		public Label PrepareForEmit (EmitContext ec)
		{
			if (!prepared) {
				prepared = true;
				resume_point = ec.DefineLabel ();
			}
			return resume_point;
		}

		public virtual Label PrepareForDispose (EmitContext ec, Label end)
		{
			return end;
		}

		public virtual void EmitForDispose (EmitContext ec, LocalBuilder pc, Label end, bool have_dispatcher)
		{
		}
	}

	public abstract class TryFinallyBlock : ExceptionStatement
	{
		protected Statement stmt;
		Label dispose_try_block;
		bool prepared_for_dispose, emitted_dispose;
		Method finally_host;

		protected TryFinallyBlock (Statement stmt, Location loc)
			: base (loc)
		{
			this.stmt = stmt;
		}

		#region Properties

		public Statement Statement {
			get {
				return stmt;
			}
		}

		#endregion

		protected abstract void EmitTryBody (EmitContext ec);
		public abstract void EmitFinallyBody (EmitContext ec);

		public override Label PrepareForDispose (EmitContext ec, Label end)
		{
			if (!prepared_for_dispose) {
				prepared_for_dispose = true;
				dispose_try_block = ec.DefineLabel ();
			}
			return dispose_try_block;
		}

		protected sealed override void DoEmit (EmitContext ec)
		{
			EmitTryBodyPrepare (ec);
			EmitTryBody (ec);

			bool beginFinally = EmitBeginFinallyBlock (ec);

			Label start_finally = ec.DefineLabel ();
			if (resume_points != null && beginFinally) {
				var state_machine = (StateMachineInitializer) ec.CurrentAnonymousMethod;

				ec.Emit (OpCodes.Ldloc, state_machine.SkipFinally);
				ec.Emit (OpCodes.Brfalse_S, start_finally);
				ec.Emit (OpCodes.Endfinally);
			}

			ec.MarkLabel (start_finally);

			if (finally_host != null) {
				finally_host.Define ();
				finally_host.PrepareEmit ();
				finally_host.Emit ();

				// Now it's safe to add, to close it properly and emit sequence points
				finally_host.Parent.AddMember (finally_host);

				var ce = new CallEmitter ();
				ce.InstanceExpression = new CompilerGeneratedThis (ec.CurrentType, loc);
				ce.EmitPredefined (ec, finally_host.Spec, new Arguments (0), true);
			} else {
				EmitFinallyBody (ec);
			}

			if (beginFinally)
				ec.EndExceptionBlock ();
		}

		public override void EmitForDispose (EmitContext ec, LocalBuilder pc, Label end, bool have_dispatcher)
		{
			if (emitted_dispose)
				return;

			emitted_dispose = true;

			Label end_of_try = ec.DefineLabel ();

			// Ensure that the only way we can get into this code is through a dispatcher
			if (have_dispatcher)
				ec.Emit (OpCodes.Br, end);

			ec.BeginExceptionBlock ();

			ec.MarkLabel (dispose_try_block);

			Label[] labels = null;
			for (int i = 0; i < resume_points.Count; ++i) {
				ResumableStatement s = resume_points[i];
				Label ret = s.PrepareForDispose (ec, end_of_try);
				if (ret.Equals (end_of_try) && labels == null)
					continue;
				if (labels == null) {
					labels = new Label[resume_points.Count];
					for (int j = 0; j < i; ++j)
						labels[j] = end_of_try;
				}
				labels[i] = ret;
			}

			if (labels != null) {
				int j;
				for (j = 1; j < labels.Length; ++j)
					if (!labels[0].Equals (labels[j]))
						break;
				bool emit_dispatcher = j < labels.Length;

				if (emit_dispatcher) {
					ec.Emit (OpCodes.Ldloc, pc);
					ec.EmitInt (first_resume_pc);
					ec.Emit (OpCodes.Sub);
					ec.Emit (OpCodes.Switch, labels);
				}

				foreach (ResumableStatement s in resume_points)
					s.EmitForDispose (ec, pc, end_of_try, emit_dispatcher);
			}

			ec.MarkLabel (end_of_try);

			ec.BeginFinallyBlock ();

			if (finally_host != null) {
				var ce = new CallEmitter ();
				ce.InstanceExpression = new CompilerGeneratedThis (ec.CurrentType, loc);
				ce.EmitPredefined (ec, finally_host.Spec, new Arguments (0), true);
			} else {
				EmitFinallyBody (ec);
			}

			ec.EndExceptionBlock ();
		}

		protected override bool DoFlowAnalysis (FlowAnalysisContext fc)
		{
			var res = stmt.FlowAnalysis (fc);
			parent = null;
			return res;
		}

		protected virtual bool EmitBeginFinallyBlock (EmitContext ec)
		{
			ec.BeginFinallyBlock ();
			return true;
		}

		public override Reachability MarkReachable (Reachability rc)
		{
			base.MarkReachable (rc);
			return Statement.MarkReachable (rc);
		}

		public override bool Resolve (BlockContext bc)
		{
			bool ok;

			parent = bc.CurrentTryBlock;
			bc.CurrentTryBlock = this;

			using (bc.Set (ResolveContext.Options.TryScope)) {
				ok = stmt.Resolve (bc);
			}

			bc.CurrentTryBlock = parent;

			//
			// Finally block inside iterator is called from MoveNext and
			// Dispose methods that means we need to lift the block into
			// newly created host method to emit the body only once. The
			// original block then simply calls the newly generated method.
			//
			if (bc.CurrentIterator != null && !bc.IsInProbingMode) {
				var b = stmt as Block;
				if (b != null && b.Explicit.HasYield) {
					finally_host = bc.CurrentIterator.CreateFinallyHost (this);
				}
			}

			return base.Resolve (bc) && ok;
		}
	}

	//
	// Base class for blocks using exception handling
	//
	public abstract class ExceptionStatement : ResumableStatement
	{
		protected List<ResumableStatement> resume_points;
		protected int first_resume_pc;
		protected ExceptionStatement parent;

		protected ExceptionStatement (Location loc)
		{
			this.loc = loc;
		}

		protected virtual void EmitTryBodyPrepare (EmitContext ec)
		{
			StateMachineInitializer state_machine = null;
			if (resume_points != null) {
				state_machine = (StateMachineInitializer) ec.CurrentAnonymousMethod;

				ec.EmitInt ((int) IteratorStorey.State.Running);
				ec.Emit (OpCodes.Stloc, state_machine.CurrentPC);
			}

			ec.BeginExceptionBlock ();

			if (resume_points != null) {
				ec.MarkLabel (resume_point);

				// For normal control flow, we want to fall-through the Switch
				// So, we use CurrentPC rather than the $PC field, and initialize it to an outside value above
				ec.Emit (OpCodes.Ldloc, state_machine.CurrentPC);
				ec.EmitInt (first_resume_pc);
				ec.Emit (OpCodes.Sub);

				Label[] labels = new Label[resume_points.Count];
				for (int i = 0; i < resume_points.Count; ++i)
					labels[i] = resume_points[i].PrepareForEmit (ec);
				ec.Emit (OpCodes.Switch, labels);
			}
		}

		public virtual int AddResumePoint (ResumableStatement stmt, int pc, StateMachineInitializer stateMachine)
		{
			if (parent != null) {
				// TODO: MOVE to virtual TryCatch
				var tc = this as TryCatch;
				var s = tc != null && tc.IsTryCatchFinally ? stmt : this;

				pc = parent.AddResumePoint (s, pc, stateMachine);
			} else {
				pc = stateMachine.AddResumePoint (this);
			}

			if (resume_points == null) {
				resume_points = new List<ResumableStatement> ();
				first_resume_pc = pc;
			}

			if (pc != first_resume_pc + resume_points.Count)
				throw new InternalErrorException ("missed an intervening AddResumePoint?");

			resume_points.Add (stmt);
			return pc;
		}
	}

	public class Lock : TryFinallyBlock
	{
		Expression expr;
		TemporaryVariableReference expr_copy;
		TemporaryVariableReference lock_taken;
			
		public Lock (Expression expr, Statement stmt, Location loc)
			: base (stmt, loc)
		{
			this.expr = expr;
		}

		public Expression Expr {
			get {
 				return this.expr;
			}
		}

		protected override bool DoFlowAnalysis (FlowAnalysisContext fc)
		{
			expr.FlowAnalysis (fc);
			return base.DoFlowAnalysis (fc);
		}

		public override bool Resolve (BlockContext ec)
		{
			expr = expr.Resolve (ec);
			if (expr == null)
				return false;

			if (!TypeSpec.IsReferenceType (expr.Type)) {
				ec.Report.Error (185, loc,
					"`{0}' is not a reference type as required by the lock statement",
					expr.Type.GetSignatureForError ());
			}

			if (expr.Type.IsGenericParameter) {
				expr = Convert.ImplicitTypeParameterConversion (expr, (TypeParameterSpec)expr.Type, ec.BuiltinTypes.Object);
			}

			VariableReference lv = expr as VariableReference;
			bool locked;
			if (lv != null) {
				locked = lv.IsLockedByStatement;
				lv.IsLockedByStatement = true;
			} else {
				lv = null;
				locked = false;
			}

			//
			// Have to keep original lock value around to unlock same location
			// in the case of original value has changed or is null
			//
			expr_copy = TemporaryVariableReference.Create (ec.BuiltinTypes.Object, ec.CurrentBlock, loc);
			expr_copy.Resolve (ec);

			//
			// Ensure Monitor methods are available
			//
			if (ResolvePredefinedMethods (ec) > 1) {
				lock_taken = TemporaryVariableReference.Create (ec.BuiltinTypes.Bool, ec.CurrentBlock, loc);
				lock_taken.Resolve (ec);
			}

			using (ec.Set (ResolveContext.Options.LockScope)) {
				base.Resolve (ec);
			}

			if (lv != null) {
				lv.IsLockedByStatement = locked;
			}

			return true;
		}
		
		protected override void EmitTryBodyPrepare (EmitContext ec)
		{
			expr_copy.EmitAssign (ec, expr);

			if (lock_taken != null) {
				//
				// Initialize ref variable
				//
				lock_taken.EmitAssign (ec, new BoolLiteral (ec.BuiltinTypes, false, loc));
			} else {
				//
				// Monitor.Enter (expr_copy)
				//
				expr_copy.Emit (ec);
				ec.Emit (OpCodes.Call, ec.Module.PredefinedMembers.MonitorEnter.Get ());
			}

			base.EmitTryBodyPrepare (ec);
		}

		protected override void EmitTryBody (EmitContext ec)
		{
			//
			// Monitor.Enter (expr_copy, ref lock_taken)
			//
			if (lock_taken != null) {
				expr_copy.Emit (ec);
				lock_taken.LocalInfo.CreateBuilder (ec);
				lock_taken.AddressOf (ec, AddressOp.Load);
				ec.Emit (OpCodes.Call, ec.Module.PredefinedMembers.MonitorEnter_v4.Get ());
			}

			Statement.Emit (ec);
		}

		public override void EmitFinallyBody (EmitContext ec)
		{
			//
			// if (lock_taken) Monitor.Exit (expr_copy)
			//
			Label skip = ec.DefineLabel ();

			if (lock_taken != null) {
				lock_taken.Emit (ec);
				ec.Emit (OpCodes.Brfalse_S, skip);
			}

			expr_copy.Emit (ec);
			var m = ec.Module.PredefinedMembers.MonitorExit.Resolve (loc);
			if (m != null)
				ec.Emit (OpCodes.Call, m);

			ec.MarkLabel (skip);
		}

		int ResolvePredefinedMethods (ResolveContext rc)
		{
			// Try 4.0 Monitor.Enter (object, ref bool) overload first
			var m = rc.Module.PredefinedMembers.MonitorEnter_v4.Get ();
			if (m != null)
				return 4;

			m = rc.Module.PredefinedMembers.MonitorEnter.Get ();
			if (m != null)
				return 1;

			rc.Module.PredefinedMembers.MonitorEnter_v4.Resolve (loc);
			return 0;
		}

		protected override void CloneTo (CloneContext clonectx, Statement t)
		{
			Lock target = (Lock) t;

			target.expr = expr.Clone (clonectx);
			target.stmt = Statement.Clone (clonectx);
		}
		
		public override object Accept (StructuralVisitor visitor)
		{
			return visitor.Visit (this);
		}

	}

	public class Unchecked : Statement {
		public Block Block;
		
		public Unchecked (Block b, Location loc)
		{
			Block = b;
			b.Unchecked = true;
			this.loc = loc;
		}

		public override bool Resolve (BlockContext ec)
		{
			using (ec.With (ResolveContext.Options.AllCheckStateFlags, false))
				return Block.Resolve (ec);
		}
		
		protected override void DoEmit (EmitContext ec)
		{
			using (ec.With (EmitContext.Options.CheckedScope, false))
				Block.Emit (ec);
		}

		protected override bool DoFlowAnalysis (FlowAnalysisContext fc)
		{
			return Block.FlowAnalysis (fc);
		}

		public override Reachability MarkReachable (Reachability rc)
		{
			base.MarkReachable (rc);
			return Block.MarkReachable (rc);
		}

		protected override void CloneTo (CloneContext clonectx, Statement t)
		{
			Unchecked target = (Unchecked) t;

			target.Block = clonectx.LookupBlock (Block);
		}
		
		public override object Accept (StructuralVisitor visitor)
		{
			return visitor.Visit (this);
		}
	}

	public class Checked : Statement {
		public Block Block;
		
		public Checked (Block b, Location loc)
		{
			Block = b;
			b.Unchecked = false;
			this.loc = loc;
		}

		public override bool Resolve (BlockContext ec)
		{
			using (ec.With (ResolveContext.Options.AllCheckStateFlags, true))
				return Block.Resolve (ec);
		}

		protected override void DoEmit (EmitContext ec)
		{
			using (ec.With (EmitContext.Options.CheckedScope, true))
				Block.Emit (ec);
		}

		protected override bool DoFlowAnalysis (FlowAnalysisContext fc)
		{
			return Block.FlowAnalysis (fc);
		}

		public override Reachability MarkReachable (Reachability rc)
		{
			base.MarkReachable (rc);
			return Block.MarkReachable (rc);
		}

		protected override void CloneTo (CloneContext clonectx, Statement t)
		{
			Checked target = (Checked) t;

			target.Block = clonectx.LookupBlock (Block);
		}
		
		public override object Accept (StructuralVisitor visitor)
		{
			return visitor.Visit (this);
		}
	}

	public class Unsafe : Statement {
		public Block Block;

		public Unsafe (Block b, Location loc)
		{
			Block = b;
			Block.Unsafe = true;
			this.loc = loc;
		}

		public override bool Resolve (BlockContext ec)
		{
			if (ec.CurrentIterator != null)
				ec.Report.Error (1629, loc, "Unsafe code may not appear in iterators");

			using (ec.Set (ResolveContext.Options.UnsafeScope))
				return Block.Resolve (ec);
		}
		
		protected override void DoEmit (EmitContext ec)
		{
			Block.Emit (ec);
		}

		protected override bool DoFlowAnalysis (FlowAnalysisContext fc)
		{
			return Block.FlowAnalysis (fc);
		}

		public override Reachability MarkReachable (Reachability rc)
		{
			base.MarkReachable (rc);
			return Block.MarkReachable (rc);
		}

		protected override void CloneTo (CloneContext clonectx, Statement t)
		{
			Unsafe target = (Unsafe) t;

			target.Block = clonectx.LookupBlock (Block);
		}
		
		public override object Accept (StructuralVisitor visitor)
		{
			return visitor.Visit (this);
		}
	}

	// 
	// Fixed statement
	//
	public class Fixed : Statement
	{
		abstract class Emitter : ShimExpression
		{
			protected LocalVariable vi;

			protected Emitter (Expression expr, LocalVariable li)
				: base (expr)
			{
				vi = li;
			}

			public abstract void EmitExit (EmitContext ec);

			public override void FlowAnalysis (FlowAnalysisContext fc)
			{
				expr.FlowAnalysis (fc);
			}
		}

		class ExpressionEmitter : Emitter {
			public ExpressionEmitter (Expression converted, LocalVariable li) :
				base (converted, li)
			{
			}

			protected override Expression DoResolve (ResolveContext rc)
			{
				throw new NotImplementedException ();
			}

			public override void Emit (EmitContext ec) {
				//
				// Store pointer in pinned location
				//
				expr.Emit (ec);
				vi.EmitAssign (ec);
			}

			public override void EmitExit (EmitContext ec)
			{
				ec.EmitInt (0);
				ec.Emit (OpCodes.Conv_U);
				vi.EmitAssign (ec);
			}
		}

		class StringEmitter : Emitter
		{
			LocalVariable pinned_string;

			public StringEmitter (Expression expr, LocalVariable li)
				: base (expr, li)
			{
			}

			protected override Expression DoResolve (ResolveContext rc)
			{
				pinned_string = new LocalVariable (vi.Block, "$pinned",
					LocalVariable.Flags.FixedVariable | LocalVariable.Flags.CompilerGenerated | LocalVariable.Flags.Used,
					vi.Location);
				pinned_string.Type = rc.BuiltinTypes.String;

				eclass = ExprClass.Variable;
				type = rc.BuiltinTypes.Int;
				return this;
			}

			public override void Emit (EmitContext ec)
			{
				pinned_string.CreateBuilder (ec);

				expr.Emit (ec);
				pinned_string.EmitAssign (ec);

				// TODO: Should use Binary::Add
				pinned_string.Emit (ec);
				ec.Emit (OpCodes.Conv_I);

				var m = ec.Module.PredefinedMembers.RuntimeHelpersOffsetToStringData.Resolve (loc);
				if (m == null)
					return;

				PropertyExpr pe = new PropertyExpr (m, pinned_string.Location);
				//pe.InstanceExpression = pinned_string;
				pe.Resolve (new ResolveContext (ec.MemberContext)).Emit (ec);

				ec.Emit (OpCodes.Add);
				vi.EmitAssign (ec);
			}

			public override void EmitExit (EmitContext ec)
			{
				ec.EmitNull ();
				pinned_string.EmitAssign (ec);
			}
		}

		public class VariableDeclaration : BlockVariable
		{
			public VariableDeclaration (FullNamedExpression type, LocalVariable li)
				: base (type, li)
			{
			}

			protected override Expression ResolveInitializer (BlockContext bc, LocalVariable li, Expression initializer)
			{
				if (!Variable.Type.IsPointer && li == Variable) {
					bc.Report.Error (209, TypeExpression.Location,
						"The type of locals declared in a fixed statement must be a pointer type");
					return null;
				}

				//
				// The rules for the possible declarators are pretty wise,
				// but the production on the grammar is more concise.
				//
				// So we have to enforce these rules here.
				//
				// We do not resolve before doing the case 1 test,
				// because the grammar is explicit in that the token &
				// is present, so we need to test for this particular case.
				//

				if (initializer is Cast) {
					bc.Report.Error (254, initializer.Location, "The right hand side of a fixed statement assignment may not be a cast expression");
					return null;
				}

				initializer = initializer.Resolve (bc);

				if (initializer == null)
					return null;

				//
				// Case 1: Array
				//
				if (initializer.Type.IsArray) {
					TypeSpec array_type = TypeManager.GetElementType (initializer.Type);

					//
					// Provided that array_type is unmanaged,
					//
					if (!TypeManager.VerifyUnmanaged (bc.Module, array_type, loc))
						return null;

					//
					// and T* is implicitly convertible to the
					// pointer type given in the fixed statement.
					//
					ArrayPtr array_ptr = new ArrayPtr (initializer, array_type, loc);

					Expression converted = Convert.ImplicitConversionRequired (bc, array_ptr.Resolve (bc), li.Type, loc);
					if (converted == null)
						return null;

					//
					// fixed (T* e_ptr = (e == null || e.Length == 0) ? null : converted [0])
					//
					converted = new Conditional (new BooleanExpression (new Binary (Binary.Operator.LogicalOr,
						new Binary (Binary.Operator.Equality, initializer, new NullLiteral (loc)),
						new Binary (Binary.Operator.Equality, new MemberAccess (initializer, "Length"), new IntConstant (bc.BuiltinTypes, 0, loc)))),
							new NullLiteral (loc),
							converted, loc);

					converted = converted.Resolve (bc);

					return new ExpressionEmitter (converted, li);
				}

				//
				// Case 2: string
				//
				if (initializer.Type.BuiltinType == BuiltinTypeSpec.Type.String) {
					return new StringEmitter (initializer, li).Resolve (bc);
				}

				// Case 3: fixed buffer
				if (initializer is FixedBufferPtr) {
					return new ExpressionEmitter (initializer, li);
				}

				//
				// Case 4: & object.
				//
				bool already_fixed = true;
				Unary u = initializer as Unary;
				if (u != null && u.Oper == Unary.Operator.AddressOf) {
					IVariableReference vr = u.Expr as IVariableReference;
					if (vr == null || !vr.IsFixed) {
						already_fixed = false;
					}
				}

				if (already_fixed) {
					bc.Report.Error (213, loc, "You cannot use the fixed statement to take the address of an already fixed expression");
				}

				initializer = Convert.ImplicitConversionRequired (bc, initializer, li.Type, loc);
				return new ExpressionEmitter (initializer, li);
			}
		}


		VariableDeclaration decl;
		Statement statement;
		bool has_ret;

		public Fixed (VariableDeclaration decl, Statement stmt, Location l)
		{
			this.decl = decl;
			statement = stmt;
			loc = l;
		}

		#region Properties

		public Statement Statement {
			get {
				return statement;
			}
		}

		public BlockVariable Variables {
			get {
				return decl;
			}
		}

		#endregion

		public override bool Resolve (BlockContext bc)
		{
			using (bc.Set (ResolveContext.Options.FixedInitializerScope)) {
				if (!decl.Resolve (bc))
					return false;
			}

			return statement.Resolve (bc);
		}

		protected override bool DoFlowAnalysis (FlowAnalysisContext fc)
		{
			decl.FlowAnalysis (fc);
			return statement.FlowAnalysis (fc);
		}
		
		protected override void DoEmit (EmitContext ec)
		{
			decl.Variable.CreateBuilder (ec);
			decl.Initializer.Emit (ec);
			if (decl.Declarators != null) {
				foreach (var d in decl.Declarators) {
					d.Variable.CreateBuilder (ec);
					d.Initializer.Emit (ec);
				}
			}

			statement.Emit (ec);

			if (has_ret)
				return;

			//
			// Clear the pinned variable
			//
			((Emitter) decl.Initializer).EmitExit (ec);
			if (decl.Declarators != null) {
				foreach (var d in decl.Declarators) {
					((Emitter)d.Initializer).EmitExit (ec);
				}
			}
		}

		public override Reachability MarkReachable (Reachability rc)
		{
			base.MarkReachable (rc);

			decl.MarkReachable (rc);

			rc = statement.MarkReachable (rc);

			// TODO: What if there is local exit?
			has_ret = rc.IsUnreachable;
			return rc;
		}

		protected override void CloneTo (CloneContext clonectx, Statement t)
		{
			Fixed target = (Fixed) t;

			target.decl = (VariableDeclaration) decl.Clone (clonectx);
			target.statement = statement.Clone (clonectx);
		}
		
		public override object Accept (StructuralVisitor visitor)
		{
			return visitor.Visit (this);
		}
	}

	public class Catch : Statement
	{
		class FilterStatement : Statement
		{
			readonly Catch ctch;

			public FilterStatement (Catch ctch)
			{
				this.ctch = ctch;
			}

			protected override void CloneTo (CloneContext clonectx, Statement target)
			{
			}

			protected override void DoEmit (EmitContext ec)
			{
				if (ctch.li != null) {
					if (ctch.hoisted_temp != null)
						ctch.hoisted_temp.Emit (ec);
					else
						ctch.li.Emit (ec);

					if (!ctch.IsGeneral && ctch.type.Kind == MemberKind.TypeParameter)
						ec.Emit (OpCodes.Box, ctch.type);
				}

				var expr_start = ec.DefineLabel ();
				var end = ec.DefineLabel ();

				ec.Emit (OpCodes.Brtrue_S, expr_start);
				ec.EmitInt (0);
				ec.Emit (OpCodes.Br, end);
				ec.MarkLabel (expr_start);

				ctch.Filter.Emit (ec);

				ec.MarkLabel (end);
				ec.Emit (OpCodes.Endfilter);
				ec.BeginFilterHandler ();
				ec.Emit (OpCodes.Pop);
			}

			protected override bool DoFlowAnalysis (FlowAnalysisContext fc)
			{
				ctch.Filter.FlowAnalysis (fc);
				return true;
			}

			public override bool Resolve (BlockContext bc)
			{
				ctch.Filter = ctch.Filter.Resolve (bc);

				if (ctch.Filter != null) {
					if (ctch.Filter.ContainsEmitWithAwait ()) {
						bc.Report.Error (7094, ctch.Filter.Location, "The `await' operator cannot be used in the filter expression of a catch clause");
					}

					var c = ctch.Filter as Constant;
					if (c != null && !c.IsDefaultValue) {
						bc.Report.Warning (7095, 1, ctch.Filter.Location, "Exception filter expression is a constant");
					}
				}

				return true;
			}
		}

		ExplicitBlock block;
		LocalVariable li;
		FullNamedExpression type_expr;
		CompilerAssign assign;
		TypeSpec type;
		LocalTemporary hoisted_temp;

		public Catch (ExplicitBlock block, Location loc)
		{
			this.block = block;
			this.loc = loc;
		}

		#region Properties

		public ExplicitBlock Block {
			get {
				return block;
			}
		}

		public TypeSpec CatchType {
			get {
				return type;
			}
		}

		public Expression Filter {
			get; set;
		}

		public bool IsGeneral {
			get {
				return type_expr == null;
			}
		}

		public FullNamedExpression TypeExpression {
			get {
				return type_expr;
			}
			set {
				type_expr = value;
			}
		}

		public LocalVariable Variable {
			get {
				return li;
			}
			set {
				li = value;
			}
		}

		#endregion

		protected override void DoEmit (EmitContext ec)
		{
			if (Filter != null) {
				ec.BeginExceptionFilterBlock ();
				ec.Emit (OpCodes.Isinst, IsGeneral ? ec.BuiltinTypes.Object : CatchType);

				if (li != null)
					EmitCatchVariableStore (ec);

				if (Block.HasAwait) {
					Block.EmitScopeInitialization (ec);
				} else {
					Block.Emit (ec);
				}

				return;
			}

			if (IsGeneral)
				ec.BeginCatchBlock (ec.BuiltinTypes.Object);
			else
				ec.BeginCatchBlock (CatchType);

			if (li != null) {
				EmitCatchVariableStore (ec);
			} else {
				ec.Emit (OpCodes.Pop);
			}

			if (!Block.HasAwait)
				Block.Emit (ec);
		}

		void EmitCatchVariableStore (EmitContext ec)
		{
			li.CreateBuilder (ec);

			//
			// Special case hoisted catch variable, we have to use a temporary variable
			// to pass via anonymous storey initialization with the value still on top
			// of the stack
			//
			if (li.HoistedVariant != null) {
				hoisted_temp = new LocalTemporary (li.Type);
				hoisted_temp.Store (ec);

				// switch to assignment from temporary variable and not from top of the stack
				assign.UpdateSource (hoisted_temp);
			}
		}

		public override bool Resolve (BlockContext bc)
		{
			using (bc.Set (ResolveContext.Options.CatchScope)) {
				if (type_expr == null) {
					if (CreateExceptionVariable (bc.Module.Compiler.BuiltinTypes.Object)) {
						Expression source = new EmptyExpression (li.Type);
						assign = new CompilerAssign (new LocalVariableReference (li, Location.Null), source, Location.Null);
						Block.AddScopeStatement (new StatementExpression (assign, Location.Null));
					}
				} else {
					type = type_expr.ResolveAsType (bc);
					if (type == null)
						return false;

					if (li == null)
						CreateExceptionVariable (type);

					if (type.BuiltinType != BuiltinTypeSpec.Type.Exception && !TypeSpec.IsBaseClass (type, bc.BuiltinTypes.Exception, false)) {
						bc.Report.Error (155, loc, "The type caught or thrown must be derived from System.Exception");
					} else if (li != null) {
						li.Type = type;
						li.PrepareAssignmentAnalysis (bc);

						// source variable is at the top of the stack
						Expression source = new EmptyExpression (li.Type);
						if (li.Type.IsGenericParameter)
							source = new UnboxCast (source, li.Type);

						//
						// Uses Location.Null to hide from symbol file
						//
						assign = new CompilerAssign (new LocalVariableReference (li, Location.Null), source, Location.Null);
						Block.AddScopeStatement (new StatementExpression (assign, Location.Null));
					}
				}

				if (Filter != null) {
					Block.AddScopeStatement (new FilterStatement (this));
				}

				Block.SetCatchBlock ();
				return Block.Resolve (bc);
			}
		}

		bool CreateExceptionVariable (TypeSpec type)
		{
			if (!Block.HasAwait)
				return false;

			// TODO: Scan the block for rethrow expression
			//if (!Block.HasRethrow)
			//	return;

			li = LocalVariable.CreateCompilerGenerated (type, block, Location.Null);
			return true;
		}

		protected override bool DoFlowAnalysis (FlowAnalysisContext fc)
		{
			if (li != null && !li.IsCompilerGenerated) {
				fc.SetVariableAssigned (li.VariableInfo, true);
			}

			return block.FlowAnalysis (fc);
		}

		public override Reachability MarkReachable (Reachability rc)
		{
			base.MarkReachable (rc);

			var c = Filter as Constant;
			if (c != null && c.IsDefaultValue)
				return Reachability.CreateUnreachable ();

			return block.MarkReachable (rc);
		}

		protected override void CloneTo (CloneContext clonectx, Statement t)
		{
			Catch target = (Catch) t;

			if (type_expr != null)
				target.type_expr = (FullNamedExpression) type_expr.Clone (clonectx);

			if (Filter != null)
				target.Filter = Filter.Clone (clonectx);

			target.block = (ExplicitBlock) clonectx.LookupBlock (block);
		}
	}

	public class TryFinally : TryFinallyBlock
	{
		ExplicitBlock fini;
		List<DefiniteAssignmentBitSet> try_exit_dat;
		List<Label> redirected_jumps;
		Label? start_fin_label;

		public Statement Stmt {
			get { return this.stmt; }
		}

		public ExplicitBlock Fini {
			get { return this.fini; }
		}

		public TryFinally (Statement stmt, ExplicitBlock fini, Location loc)
			 : base (stmt, loc)
		{
			this.fini = fini;
		}

		public ExplicitBlock FinallyBlock {
			get {
 				return fini;
			}
		}

		public void RegisterForControlExitCheck (DefiniteAssignmentBitSet vector)
		{
			if (try_exit_dat == null)
				try_exit_dat = new List<DefiniteAssignmentBitSet> ();

			try_exit_dat.Add (vector);
		}

		public override bool Resolve (BlockContext bc)
		{
			bool ok = base.Resolve (bc);

			fini.SetFinallyBlock ();
			using (bc.Set (ResolveContext.Options.FinallyScope)) {
				ok &= fini.Resolve (bc);
			}

			return ok;
		}

		protected override void EmitTryBody (EmitContext ec)
		{
			if (fini.HasAwait) {
				if (ec.TryFinallyUnwind == null)
					ec.TryFinallyUnwind = new List<TryFinally> ();

				ec.TryFinallyUnwind.Add (this);
				stmt.Emit (ec);
				ec.TryFinallyUnwind.Remove (this);

				if (start_fin_label != null)
					ec.MarkLabel (start_fin_label.Value);

				return;
			}

			stmt.Emit (ec);
		}

		protected override bool EmitBeginFinallyBlock (EmitContext ec)
		{
			if (fini.HasAwait)
				return false;

			return base.EmitBeginFinallyBlock (ec);
		}

		public override void EmitFinallyBody (EmitContext ec)
		{
			if (!fini.HasAwait) {
				fini.Emit (ec);
				return;
			}

			//
			// Emits catch block like
			//
			// catch (object temp) {
			//	this.exception_field = temp;
			// }
			//
			var type = ec.BuiltinTypes.Object;
			ec.BeginCatchBlock (type);

			var temp = ec.GetTemporaryLocal (type);
			ec.Emit (OpCodes.Stloc, temp);

			var exception_field = ec.GetTemporaryField (type);
			ec.EmitThis ();
			ec.Emit (OpCodes.Ldloc, temp);
			exception_field.EmitAssignFromStack (ec);

			ec.EndExceptionBlock ();

			ec.FreeTemporaryLocal (temp, type);

			fini.Emit (ec);

			//
			// Emits exception rethrow
			//
			// if (this.exception_field != null)
			//	throw this.exception_field;
			//
			exception_field.Emit (ec);
			var skip_throw = ec.DefineLabel ();
			ec.Emit (OpCodes.Brfalse_S, skip_throw);
			exception_field.Emit (ec);
			ec.Emit (OpCodes.Throw);
			ec.MarkLabel (skip_throw);

			exception_field.IsAvailableForReuse = true;

			EmitUnwindFinallyTable (ec);
		}

		bool IsParentBlock (Block block)
		{
			for (Block b = fini; b != null; b = b.Parent) {
				if (b == block)
					return true;
			}

			return false;
		}

		public static Label EmitRedirectedJump (EmitContext ec, AsyncInitializer initializer, Label label, Block labelBlock)
		{
			int idx;
			if (labelBlock != null) {
				for (idx = ec.TryFinallyUnwind.Count; idx != 0; --idx) {
					var fin = ec.TryFinallyUnwind [idx - 1];
					if (!fin.IsParentBlock (labelBlock))
						break;
				}
			} else {
				idx = 0;
			}

			bool set_return_state = true;

			for (; idx < ec.TryFinallyUnwind.Count; ++idx) {
				var fin = ec.TryFinallyUnwind [idx];
				if (labelBlock != null && !fin.IsParentBlock (labelBlock))
					break;

				fin.EmitRedirectedExit (ec, label, initializer, set_return_state);
				set_return_state = false;

				if (fin.start_fin_label == null) {
					fin.start_fin_label = ec.DefineLabel ();
				}

				label = fin.start_fin_label.Value;
			}

			return label;
		}

		public static Label EmitRedirectedReturn (EmitContext ec, AsyncInitializer initializer)
		{
			return EmitRedirectedJump (ec, initializer, initializer.BodyEnd, null);
		}

		void EmitRedirectedExit (EmitContext ec, Label label, AsyncInitializer initializer, bool setReturnState)
		{
			if (redirected_jumps == null) {
				redirected_jumps = new List<Label> ();

				// Add fallthrough label
				redirected_jumps.Add (ec.DefineLabel ());

				if (setReturnState)
					initializer.HoistedReturnState = ec.GetTemporaryField (ec.Module.Compiler.BuiltinTypes.Int, true);
			}

			int index = redirected_jumps.IndexOf (label);
			if (index < 0) {
				redirected_jumps.Add (label);
				index = redirected_jumps.Count - 1;
			}

			//
			// Indicates we have captured exit jump
			//
			if (setReturnState) {
				var value = new IntConstant (initializer.HoistedReturnState.Type, index, Location.Null);
				initializer.HoistedReturnState.EmitAssign (ec, value, false, false);
			}
		}

		//
		// Emits state table of jumps outside of try block and reload of return
		// value when try block returns value
		//
		void EmitUnwindFinallyTable (EmitContext ec)
		{
			if (redirected_jumps == null)
				return;

			var initializer = (AsyncInitializer)ec.CurrentAnonymousMethod;
			initializer.HoistedReturnState.EmitLoad (ec);
			ec.Emit (OpCodes.Switch, redirected_jumps.ToArray ());

			// Mark fallthrough label
			ec.MarkLabel (redirected_jumps [0]);
		}

		protected override bool DoFlowAnalysis (FlowAnalysisContext fc)
		{
			var da = fc.BranchDefiniteAssignment ();

			var tf = fc.TryFinally;
			fc.TryFinally = this;

			var res_stmt = Statement.FlowAnalysis (fc);

			fc.TryFinally = tf;

			var try_da = fc.DefiniteAssignment;
			fc.DefiniteAssignment = da;

			var res_fin = fini.FlowAnalysis (fc);

			if (try_exit_dat != null) {
				//
				// try block has global exit but we need to run definite assignment check
				// for parameter block out parameter after finally block because it's always
				// executed before exit
				//
				foreach (var try_da_part in try_exit_dat)
					fc.ParametersBlock.CheckControlExit (fc, fc.DefiniteAssignment | try_da_part);

				try_exit_dat = null;
			}

			fc.DefiniteAssignment |= try_da;
			return res_stmt | res_fin;
		}

		public override Reachability MarkReachable (Reachability rc)
		{
			//
			// Mark finally block first for any exit statement in try block
			// to know whether the code which follows finally is reachable
			//
			return fini.MarkReachable (rc) | base.MarkReachable (rc);
		}

		protected override void CloneTo (CloneContext clonectx, Statement t)
		{
			TryFinally target = (TryFinally) t;

			target.stmt = stmt.Clone (clonectx);
			if (fini != null)
				target.fini = (ExplicitBlock) clonectx.LookupBlock (fini);
		}
		
		public override object Accept (StructuralVisitor visitor)
		{
			return visitor.Visit (this);
		}
	}

	public class TryCatch : ExceptionStatement
	{
		public Block Block;
		List<Catch> clauses;
		readonly bool inside_try_finally;
		List<Catch> catch_sm;

		public TryCatch (Block block, List<Catch> catch_clauses, Location l, bool inside_try_finally)
			: base (l)
		{
			this.Block = block;
			this.clauses = catch_clauses;
			this.inside_try_finally = inside_try_finally;
		}

		public List<Catch> Clauses {
			get {
				return clauses;
			}
		}

		public bool IsTryCatchFinally {
			get {
				return inside_try_finally;
			}
		}

		public override bool Resolve (BlockContext bc)
		{
			bool ok;

			using (bc.Set (ResolveContext.Options.TryScope)) {
				parent = bc.CurrentTryBlock;

				if (IsTryCatchFinally) {
					ok = Block.Resolve (bc);
				} else {
					using (bc.Set (ResolveContext.Options.TryWithCatchScope)) {
						bc.CurrentTryBlock = this;
						ok = Block.Resolve (bc);
						bc.CurrentTryBlock = parent;
					}
				}
			}

			for (int i = 0; i < clauses.Count; ++i) {
				var c = clauses[i];

				ok &= c.Resolve (bc);

				if (c.Block.HasAwait) {
					if (catch_sm == null)
						catch_sm = new List<Catch> ();

					catch_sm.Add (c);
				}

				if (c.Filter != null)
					continue;

				TypeSpec resolved_type = c.CatchType;
				if (resolved_type == null)
					continue;

				for (int ii = 0; ii < clauses.Count; ++ii) {
					if (ii == i)
						continue;

					if (clauses[ii].Filter != null)
						continue;

					if (clauses[ii].IsGeneral) {
						if (resolved_type.BuiltinType != BuiltinTypeSpec.Type.Exception)
							continue;

						if (!bc.Module.DeclaringAssembly.WrapNonExceptionThrows)
							continue;

						if (!bc.Module.PredefinedAttributes.RuntimeCompatibility.IsDefined)
							continue;

						bc.Report.Warning (1058, 1, c.loc,
							"A previous catch clause already catches all exceptions. All non-exceptions thrown will be wrapped in a `System.Runtime.CompilerServices.RuntimeWrappedException'");

						continue;
					}

					if (ii >= i)
						continue;

					var ct = clauses[ii].CatchType;
					if (ct == null)
						continue;

					if (resolved_type == ct || TypeSpec.IsBaseClass (resolved_type, ct, true)) {
						bc.Report.Error (160, c.loc,
							"A previous catch clause already catches all exceptions of this or a super type `{0}'",
							ct.GetSignatureForError ());
						ok = false;
					}
				}
			}

			return base.Resolve (bc) && ok;
		}

		protected sealed override void DoEmit (EmitContext ec)
		{
			if (!inside_try_finally)
				EmitTryBodyPrepare (ec);

			Block.Emit (ec);

			LocalBuilder state_variable = null;
			foreach (Catch c in clauses) {
				c.Emit (ec);

				if (catch_sm != null) {
					if (state_variable == null) {
						//
						// Cannot reuse temp variable because non-catch path assumes the value is 0
						// which may not be true for reused local variable
						//
						state_variable = ec.DeclareLocal (ec.Module.Compiler.BuiltinTypes.Int, false);
					}

					var index = catch_sm.IndexOf (c);
					if (index < 0)
						continue;

					ec.EmitInt (index + 1);
					ec.Emit (OpCodes.Stloc, state_variable);
				}
			}

			if (!inside_try_finally)
				ec.EndExceptionBlock ();

			if (state_variable != null) {
				ec.Emit (OpCodes.Ldloc, state_variable);

				var labels = new Label [catch_sm.Count + 1];
				for (int i = 0; i < labels.Length; ++i) {
					labels [i] = ec.DefineLabel ();
				}

				var end = ec.DefineLabel ();
				ec.Emit (OpCodes.Switch, labels);

				// 0 value is default label
				ec.MarkLabel (labels [0]);
				ec.Emit (OpCodes.Br, end);

				var atv = ec.AsyncThrowVariable;
				Catch c = null;
				for (int i = 0; i < catch_sm.Count; ++i) {
					if (c != null && c.Block.HasReachableClosingBrace)
						ec.Emit (OpCodes.Br, end);

					ec.MarkLabel (labels [i + 1]);
					c = catch_sm [i];
					ec.AsyncThrowVariable = c.Variable;
					c.Block.Emit (ec);
				}
				ec.AsyncThrowVariable = atv;

				ec.MarkLabel (end);
			}
		}

		protected override bool DoFlowAnalysis (FlowAnalysisContext fc)
		{
			var start_fc = fc.BranchDefiniteAssignment ();
			var res = Block.FlowAnalysis (fc);

			DefiniteAssignmentBitSet try_fc = res ? null : fc.DefiniteAssignment;

			foreach (var c in clauses) {
				fc.DefiniteAssignment = new DefiniteAssignmentBitSet (start_fc);
				if (!c.FlowAnalysis (fc)) {
					if (try_fc == null)
						try_fc = fc.DefiniteAssignment;
					else
						try_fc &= fc.DefiniteAssignment;

					res = false;
				}
			}

			fc.DefiniteAssignment = try_fc ?? start_fc;
			parent = null;
			return res;
		}

		public override Reachability MarkReachable (Reachability rc)
		{
			if (rc.IsUnreachable)
				return rc;

			base.MarkReachable (rc);

			var tc_rc = Block.MarkReachable (rc);

			foreach (var c in clauses)
				tc_rc &= c.MarkReachable (rc);

			return tc_rc;
		}

		protected override void CloneTo (CloneContext clonectx, Statement t)
		{
			TryCatch target = (TryCatch) t;

			target.Block = clonectx.LookupBlock (Block);
			if (clauses != null){
				target.clauses = new List<Catch> ();
				foreach (Catch c in clauses)
					target.clauses.Add ((Catch) c.Clone (clonectx));
			}
		}

		public override object Accept (StructuralVisitor visitor)
		{
			return visitor.Visit (this);
		}
	}

	public class Using : TryFinallyBlock
	{
		public class VariableDeclaration : BlockVariable
		{
			Statement dispose_call;

			public VariableDeclaration (FullNamedExpression type, LocalVariable li)
				: base (type, li)
			{
			}

			public VariableDeclaration (LocalVariable li, Location loc)
				: base (li)
			{
				reachable = true;
				this.loc = loc;
			}

			public VariableDeclaration (Expression expr)
				: base (null)
			{
				loc = expr.Location;
				Initializer = expr;
			}

			#region Properties

			public bool IsNested { get; private set; }

			#endregion

			public void EmitDispose (EmitContext ec)
			{
				dispose_call.Emit (ec);
			}

			public override bool Resolve (BlockContext bc)
			{
				if (IsNested)
					return true;

				return base.Resolve (bc, false);
			}

			public Expression ResolveExpression (BlockContext bc)
			{
				var e = Initializer.Resolve (bc);
				if (e == null)
					return null;

				li = LocalVariable.CreateCompilerGenerated (e.Type, bc.CurrentBlock, loc);
				Initializer = ResolveInitializer (bc, Variable, e);
				return e;
			}

			protected override Expression ResolveInitializer (BlockContext bc, LocalVariable li, Expression initializer)
			{
				if (li.Type.BuiltinType == BuiltinTypeSpec.Type.Dynamic) {
					initializer = initializer.Resolve (bc);
					if (initializer == null)
						return null;

					// Once there is dynamic used defer conversion to runtime even if we know it will never succeed
					Arguments args = new Arguments (1);
					args.Add (new Argument (initializer));
					initializer = new DynamicConversion (bc.BuiltinTypes.IDisposable, 0, args, initializer.Location).Resolve (bc);
					if (initializer == null)
						return null;

					var var = LocalVariable.CreateCompilerGenerated (initializer.Type, bc.CurrentBlock, loc);
					dispose_call = CreateDisposeCall (bc, var);
					dispose_call.Resolve (bc);

					return base.ResolveInitializer (bc, li, new SimpleAssign (var.CreateReferenceExpression (bc, loc), initializer, loc));
				}

				if (li == Variable) {
					CheckIDiposableConversion (bc, li, initializer);
					dispose_call = CreateDisposeCall (bc, li);
					dispose_call.Resolve (bc);
				}

				return base.ResolveInitializer (bc, li, initializer);
			}

			protected virtual void CheckIDiposableConversion (BlockContext bc, LocalVariable li, Expression initializer)
			{
				var type = li.Type;

				if (type.BuiltinType != BuiltinTypeSpec.Type.IDisposable && !CanConvertToIDisposable (bc, type)) {
					if (type.IsNullableType) {
						// it's handled in CreateDisposeCall
						return;
					}

					if (type != InternalType.ErrorType) {
						bc.Report.SymbolRelatedToPreviousError (type);
						var loc = type_expr == null ? initializer.Location : type_expr.Location;
						bc.Report.Error (1674, loc, "`{0}': type used in a using statement must be implicitly convertible to `System.IDisposable'",
							type.GetSignatureForError ());
					}

					return;
				}
			}

			static bool CanConvertToIDisposable (BlockContext bc, TypeSpec type)
			{
				var target = bc.BuiltinTypes.IDisposable;
				var tp = type as TypeParameterSpec;
				if (tp != null)
					return Convert.ImplicitTypeParameterConversion (null, tp, target) != null;

				return type.ImplementsInterface (target, false);
			}

			protected virtual Statement CreateDisposeCall (BlockContext bc, LocalVariable lv)
			{
				var lvr = lv.CreateReferenceExpression (bc, lv.Location);
				var type = lv.Type;
				var loc = lv.Location;

				var idt = bc.BuiltinTypes.IDisposable;
				var m = bc.Module.PredefinedMembers.IDisposableDispose.Resolve (loc);

				var dispose_mg = MethodGroupExpr.CreatePredefined (m, idt, loc);
				dispose_mg.InstanceExpression = type.IsNullableType ?
					new Cast (new TypeExpression (idt, loc), lvr, loc).Resolve (bc) :
					lvr;

				//
				// Hide it from symbol file via null location
				//
				Statement dispose = new StatementExpression (new Invocation (dispose_mg, null), Location.Null);

				// Add conditional call when disposing possible null variable
				if (!TypeSpec.IsValueType (type) || type.IsNullableType)
					dispose = new If (new Binary (Binary.Operator.Inequality, lvr, new NullLiteral (loc)), dispose, dispose.loc);

				return dispose;
			}

			public void ResolveDeclaratorInitializer (BlockContext bc)
			{
				Initializer = base.ResolveInitializer (bc, Variable, Initializer);
			}

			public Statement RewriteUsingDeclarators (BlockContext bc, Statement stmt)
			{
				for (int i = declarators.Count - 1; i >= 0; --i) {
					var d = declarators [i];
					var vd = new VariableDeclaration (d.Variable, d.Variable.Location);
					vd.Initializer = d.Initializer;
					vd.IsNested = true;
					vd.dispose_call = CreateDisposeCall (bc, d.Variable);
					vd.dispose_call.Resolve (bc);

					stmt = new Using (vd, stmt, d.Variable.Location);
				}

				declarators = null;
				return stmt;
			}	

			public override object Accept (StructuralVisitor visitor)
			{
				return visitor.Visit (this);
			}	
		}

		VariableDeclaration decl;

		public Using (VariableDeclaration decl, Statement stmt, Location loc)
			: base (stmt, loc)
		{
			this.decl = decl;
		}

		public Using (Expression expr, Statement stmt, Location loc)
			: base (stmt, loc)
		{
			this.decl = new VariableDeclaration (expr);
		}

		#region Properties

		public Expression Expr {
			get {
				return decl.Variable == null ? decl.Initializer : null;
			}
		}

		public BlockVariable Variables {
			get {
				return decl;
			}
		}

		#endregion

		public override void Emit (EmitContext ec)
		{
			//
			// Don't emit sequence point it will be set on variable declaration
			//
			DoEmit (ec);
		}

		protected override void EmitTryBodyPrepare (EmitContext ec)
		{
			decl.Emit (ec);
			base.EmitTryBodyPrepare (ec);
		}

		protected override void EmitTryBody (EmitContext ec)
		{
			stmt.Emit (ec);
		}

		public override void EmitFinallyBody (EmitContext ec)
		{
			decl.EmitDispose (ec);
		}

		protected override bool DoFlowAnalysis (FlowAnalysisContext fc)
		{
			decl.FlowAnalysis (fc);
			return stmt.FlowAnalysis (fc);
		}

		public override Reachability MarkReachable (Reachability rc)
		{
			decl.MarkReachable (rc);
			return base.MarkReachable (rc);
		}

		public override bool Resolve (BlockContext ec)
		{
			VariableReference vr;
			bool vr_locked = false;

			using (ec.Set (ResolveContext.Options.UsingInitializerScope)) {
				if (decl.Variable == null) {
					vr = decl.ResolveExpression (ec) as VariableReference;
					if (vr != null) {
						vr_locked = vr.IsLockedByStatement;
						vr.IsLockedByStatement = true;
					}
				} else {
					if (decl.IsNested) {
						decl.ResolveDeclaratorInitializer (ec);
					} else {
						if (!decl.Resolve (ec))
							return false;

						if (decl.Declarators != null) {
							stmt = decl.RewriteUsingDeclarators (ec, stmt);
						}
					}

					vr = null;
				}
			}

			var ok = base.Resolve (ec);

			if (vr != null)
				vr.IsLockedByStatement = vr_locked;

			return ok;
		}

		protected override void CloneTo (CloneContext clonectx, Statement t)
		{
			Using target = (Using) t;

			target.decl = (VariableDeclaration) decl.Clone (clonectx);
			target.stmt = stmt.Clone (clonectx);
		}

		public override object Accept (StructuralVisitor visitor)
		{
			return visitor.Visit (this);
		}
	}

	/// <summary>
	///   Implementation of the foreach C# statement
	/// </summary>
	public class Foreach : LoopStatement
	{
		abstract class IteratorStatement : Statement
		{
			protected readonly Foreach for_each;

			protected IteratorStatement (Foreach @foreach)
			{
				this.for_each = @foreach;
				this.loc = @foreach.expr.Location;
			}

			protected override void CloneTo (CloneContext clonectx, Statement target)
			{
				throw new NotImplementedException ();
			}

			public override void Emit (EmitContext ec)
			{
				if (ec.EmitAccurateDebugInfo) {
					ec.Emit (OpCodes.Nop);
				}

				base.Emit (ec);
			}

			protected override bool DoFlowAnalysis (FlowAnalysisContext fc)
			{
				throw new NotImplementedException ();
			}
		}

		sealed class ArrayForeach : IteratorStatement
		{
			TemporaryVariableReference[] lengths;
			Expression [] length_exprs;
			StatementExpression[] counter;
			TemporaryVariableReference[] variables;

			TemporaryVariableReference copy;

			public ArrayForeach (Foreach @foreach, int rank)
				: base (@foreach)
			{
				counter = new StatementExpression[rank];
				variables = new TemporaryVariableReference[rank];
				length_exprs = new Expression [rank];

				//
				// Only use temporary length variables when dealing with
				// multi-dimensional arrays
				//
				if (rank > 1)
					lengths = new TemporaryVariableReference [rank];
			}

			public override bool Resolve (BlockContext ec)
			{
				Block variables_block = for_each.variable.Block;
				copy = TemporaryVariableReference.Create (for_each.expr.Type, variables_block, loc);
				copy.Resolve (ec);

				int rank = length_exprs.Length;
				Arguments list = new Arguments (rank);
				for (int i = 0; i < rank; i++) {
					var v = TemporaryVariableReference.Create (ec.BuiltinTypes.Int, variables_block, loc);
					variables[i] = v;
					counter[i] = new StatementExpression (new UnaryMutator (UnaryMutator.Mode.PostIncrement, v, Location.Null));
					counter[i].Resolve (ec);

					if (rank == 1) {
						length_exprs [i] = new MemberAccess (copy, "Length").Resolve (ec);
					} else {
						lengths[i] = TemporaryVariableReference.Create (ec.BuiltinTypes.Int, variables_block, loc);
						lengths[i].Resolve (ec);

						Arguments args = new Arguments (1);
						args.Add (new Argument (new IntConstant (ec.BuiltinTypes, i, loc)));
						length_exprs [i] = new Invocation (new MemberAccess (copy, "GetLength"), args).Resolve (ec);
					}

					list.Add (new Argument (v));
				}

				var access = new ElementAccess (copy, list, loc).Resolve (ec);
				if (access == null)
					return false;

				TypeSpec var_type;
				if (for_each.type is VarExpr) {
					// Infer implicitly typed local variable from foreach array type
					var_type = access.Type;
				} else {
					var_type = for_each.type.ResolveAsType (ec);

					if (var_type == null)
						return false;

					access = Convert.ExplicitConversion (ec, access, var_type, loc);
					if (access == null)
						return false;
				}

				for_each.variable.Type = var_type;

				var variable_ref = new LocalVariableReference (for_each.variable, loc).Resolve (ec);
				if (variable_ref == null)
					return false;

				for_each.body.AddScopeStatement (new StatementExpression (new CompilerAssign (variable_ref, access, Location.Null), for_each.type.Location));

				return for_each.body.Resolve (ec);
			}

			protected override void DoEmit (EmitContext ec)
			{
				copy.EmitAssign (ec, for_each.expr);

				int rank = length_exprs.Length;
				Label[] test = new Label [rank];
				Label[] loop = new Label [rank];

				for (int i = 0; i < rank; i++) {
					test [i] = ec.DefineLabel ();
					loop [i] = ec.DefineLabel ();

					if (lengths != null)
						lengths [i].EmitAssign (ec, length_exprs [i]);
				}

				IntConstant zero = new IntConstant (ec.BuiltinTypes, 0, loc);
				for (int i = 0; i < rank; i++) {
					variables [i].EmitAssign (ec, zero);

					ec.Emit (OpCodes.Br, test [i]);
					ec.MarkLabel (loop [i]);
				}

				for_each.body.Emit (ec);

				ec.MarkLabel (ec.LoopBegin);
				ec.Mark (for_each.expr.Location);

				for (int i = rank - 1; i >= 0; i--){
					counter [i].Emit (ec);

					ec.MarkLabel (test [i]);
					variables [i].Emit (ec);

					if (lengths != null)
						lengths [i].Emit (ec);
					else
						length_exprs [i].Emit (ec);

					ec.Emit (OpCodes.Blt, loop [i]);
				}

				ec.MarkLabel (ec.LoopEnd);
			}
		}

		sealed class CollectionForeach : IteratorStatement, OverloadResolver.IErrorHandler
		{
			class RuntimeDispose : Using.VariableDeclaration
			{
				public RuntimeDispose (LocalVariable lv, Location loc)
					: base (lv, loc)
				{
					reachable = true;
				}

				protected override void CheckIDiposableConversion (BlockContext bc, LocalVariable li, Expression initializer)
				{
					// Defered to runtime check
				}

				protected override Statement CreateDisposeCall (BlockContext bc, LocalVariable lv)
				{
					var idt = bc.BuiltinTypes.IDisposable;

					//
					// Fabricates code like
					//
					// if ((temp = vr as IDisposable) != null) temp.Dispose ();
					//

					var dispose_variable = LocalVariable.CreateCompilerGenerated (idt, bc.CurrentBlock, loc);

					var idisaposable_test = new Binary (Binary.Operator.Inequality, new CompilerAssign (
						dispose_variable.CreateReferenceExpression (bc, loc),
						new As (lv.CreateReferenceExpression (bc, loc), new TypeExpression (dispose_variable.Type, loc), loc),
						loc), new NullLiteral (loc));

					var m = bc.Module.PredefinedMembers.IDisposableDispose.Resolve (loc);

					var dispose_mg = MethodGroupExpr.CreatePredefined (m, idt, loc);
					dispose_mg.InstanceExpression = dispose_variable.CreateReferenceExpression (bc, loc);

					Statement dispose = new StatementExpression (new Invocation (dispose_mg, null));
					return new If (idisaposable_test, dispose, loc);
				}
			}

			LocalVariable variable;
			Expression expr;
			Statement statement;
			ExpressionStatement init;
			TemporaryVariableReference enumerator_variable;
			bool ambiguous_getenumerator_name;

			public CollectionForeach (Foreach @foreach, LocalVariable var, Expression expr)
				: base (@foreach)
			{
				this.variable = var;
				this.expr = expr;
			}

			void Error_WrongEnumerator (ResolveContext rc, MethodSpec enumerator)
			{
				rc.Report.SymbolRelatedToPreviousError (enumerator);
				rc.Report.Error (202, loc,
					"foreach statement requires that the return type `{0}' of `{1}' must have a suitable public MoveNext method and public Current property",
						enumerator.ReturnType.GetSignatureForError (), enumerator.GetSignatureForError ());
			}

			MethodGroupExpr ResolveGetEnumerator (ResolveContext rc)
			{
				//
				// Option 1: Try to match by name GetEnumerator first
				//
				var mexpr = Expression.MemberLookup (rc, false, expr.Type,
					"GetEnumerator", 0, Expression.MemberLookupRestrictions.ExactArity, loc);		// TODO: What if CS0229 ?

				var mg = mexpr as MethodGroupExpr;
				if (mg != null) {
					mg.InstanceExpression = expr;
					Arguments args = new Arguments (0);
					mg = mg.OverloadResolve (rc, ref args, this, OverloadResolver.Restrictions.ProbingOnly | OverloadResolver.Restrictions.GetEnumeratorLookup);

					// For ambiguous GetEnumerator name warning CS0278 was reported, but Option 2 could still apply
					if (ambiguous_getenumerator_name)
						mg = null;

					if (mg != null && !mg.BestCandidate.IsStatic && mg.BestCandidate.IsPublic) {
						return mg;
					}
				}

				//
				// Option 2: Try to match using IEnumerable interfaces with preference of generic version
				//
				var t = expr.Type;
				PredefinedMember<MethodSpec> iface_candidate = null;
				var ptypes = rc.Module.PredefinedTypes;
				var gen_ienumerable = ptypes.IEnumerableGeneric;
				if (!gen_ienumerable.Define ())
					gen_ienumerable = null;

				var ifaces = t.Interfaces;
				if (ifaces != null) {
					foreach (var iface in ifaces) {
						if (gen_ienumerable != null && iface.MemberDefinition == gen_ienumerable.TypeSpec.MemberDefinition) {
							if (iface_candidate != null && iface_candidate != rc.Module.PredefinedMembers.IEnumerableGetEnumerator) {
								rc.Report.SymbolRelatedToPreviousError (expr.Type);
								rc.Report.Error (1640, loc,
									"foreach statement cannot operate on variables of type `{0}' because it contains multiple implementation of `{1}'. Try casting to a specific implementation",
									expr.Type.GetSignatureForError (), gen_ienumerable.TypeSpec.GetSignatureForError ());

								return null;
							}

							// TODO: Cache this somehow
							iface_candidate = new PredefinedMember<MethodSpec> (rc.Module, iface,
								MemberFilter.Method ("GetEnumerator", 0, ParametersCompiled.EmptyReadOnlyParameters, null));

							continue;
						}

						if (iface.BuiltinType == BuiltinTypeSpec.Type.IEnumerable && iface_candidate == null) {
							iface_candidate = rc.Module.PredefinedMembers.IEnumerableGetEnumerator;
						}
					}
				}

				if (iface_candidate == null) {
					if (expr.Type != InternalType.ErrorType) {
						rc.Report.Error (1579, loc,
							"foreach statement cannot operate on variables of type `{0}' because it does not contain a definition for `{1}' or is inaccessible",
							expr.Type.GetSignatureForError (), "GetEnumerator");
					}

					return null;
				}

				var method = iface_candidate.Resolve (loc);
				if (method == null)
					return null;

				mg = MethodGroupExpr.CreatePredefined (method, expr.Type, loc);
				mg.InstanceExpression = expr;
				return mg;
			}

			MethodGroupExpr ResolveMoveNext (ResolveContext rc, MethodSpec enumerator)
			{
				var ms = MemberCache.FindMember (enumerator.ReturnType,
					MemberFilter.Method ("MoveNext", 0, ParametersCompiled.EmptyReadOnlyParameters, rc.BuiltinTypes.Bool),
					BindingRestriction.InstanceOnly) as MethodSpec;

				if (ms == null || !ms.IsPublic) {
					Error_WrongEnumerator (rc, enumerator);
					return null;
				}

				return MethodGroupExpr.CreatePredefined (ms, enumerator.ReturnType, expr.Location);
			}

			PropertySpec ResolveCurrent (ResolveContext rc, MethodSpec enumerator)
			{
				var ps = MemberCache.FindMember (enumerator.ReturnType,
					MemberFilter.Property ("Current", null),
					BindingRestriction.InstanceOnly) as PropertySpec;

				if (ps == null || !ps.IsPublic) {
					Error_WrongEnumerator (rc, enumerator);
					return null;
				}

				return ps;
			}

			public override bool Resolve (BlockContext ec)
			{
				bool is_dynamic = expr.Type.BuiltinType == BuiltinTypeSpec.Type.Dynamic;

				if (is_dynamic) {
					expr = Convert.ImplicitConversionRequired (ec, expr, ec.BuiltinTypes.IEnumerable, loc);
				} else if (expr.Type.IsNullableType) {
					expr = new Nullable.UnwrapCall (expr).Resolve (ec);
				}

				var get_enumerator_mg = ResolveGetEnumerator (ec);
				if (get_enumerator_mg == null) {
					return false;
				}

				var get_enumerator = get_enumerator_mg.BestCandidate;
				enumerator_variable = TemporaryVariableReference.Create (get_enumerator.ReturnType, variable.Block, loc);
				enumerator_variable.Resolve (ec);

				// Prepare bool MoveNext ()
				var move_next_mg = ResolveMoveNext (ec, get_enumerator);
				if (move_next_mg == null) {
					return false;
				}

				move_next_mg.InstanceExpression = enumerator_variable;

				// Prepare ~T~ Current { get; }
				var current_prop = ResolveCurrent (ec, get_enumerator);
				if (current_prop == null) {
					return false;
				}

				var current_pe = new PropertyExpr (current_prop, loc) { InstanceExpression = enumerator_variable }.Resolve (ec);
				if (current_pe == null)
					return false;

				VarExpr ve = for_each.type as VarExpr;

				if (ve != null) {
					if (is_dynamic) {
						// Source type is dynamic, set element type to dynamic too
						variable.Type = ec.BuiltinTypes.Dynamic;
					} else {
						// Infer implicitly typed local variable from foreach enumerable type
						variable.Type = current_pe.Type;
					}
				} else {
					if (is_dynamic) {
						// Explicit cast of dynamic collection elements has to be done at runtime
						current_pe = EmptyCast.Create (current_pe, ec.BuiltinTypes.Dynamic);
					}

					variable.Type = for_each.type.ResolveAsType (ec);

					if (variable.Type == null)
						return false;

					current_pe = Convert.ExplicitConversion (ec, current_pe, variable.Type, loc);
					if (current_pe == null)
						return false;
				}

				var variable_ref = new LocalVariableReference (variable, loc).Resolve (ec);
				if (variable_ref == null)
					return false;

				for_each.body.AddScopeStatement (new StatementExpression (new CompilerAssign (variable_ref, current_pe, Location.Null), for_each.type.Location));

				var init = new Invocation.Predefined (get_enumerator_mg, null);

				statement = new While (new BooleanExpression (new Invocation (move_next_mg, null)),
					 for_each.body, Location.Null);

				var enum_type = enumerator_variable.Type;

				//
				// Add Dispose method call when enumerator can be IDisposable
				//
				if (!enum_type.ImplementsInterface (ec.BuiltinTypes.IDisposable, false)) {
					if (!enum_type.IsSealed && !TypeSpec.IsValueType (enum_type)) {
						//
						// Runtime Dispose check
						//
						var vd = new RuntimeDispose (enumerator_variable.LocalInfo, Location.Null);
						vd.Initializer = init;
						statement = new Using (vd, statement, Location.Null);
					} else {
						//
						// No Dispose call needed
						//
						this.init = new SimpleAssign (enumerator_variable, init, Location.Null);
						this.init.Resolve (ec);
					}
				} else {
					//
					// Static Dispose check
					//
					var vd = new Using.VariableDeclaration (enumerator_variable.LocalInfo, Location.Null);
					vd.Initializer = init;
					statement = new Using (vd, statement, Location.Null);
				}

				return statement.Resolve (ec);
			}

			protected override void DoEmit (EmitContext ec)
			{
				enumerator_variable.LocalInfo.CreateBuilder (ec);

				if (init != null)
					init.EmitStatement (ec);

				statement.Emit (ec);
			}

			#region IErrorHandler Members

			bool OverloadResolver.IErrorHandler.AmbiguousCandidates (ResolveContext ec, MemberSpec best, MemberSpec ambiguous)
			{
				ec.Report.SymbolRelatedToPreviousError (best);
				ec.Report.Warning (278, 2, expr.Location,
					"`{0}' contains ambiguous implementation of `{1}' pattern. Method `{2}' is ambiguous with method `{3}'",
					expr.Type.GetSignatureForError (), "enumerable",
					best.GetSignatureForError (), ambiguous.GetSignatureForError ());

				ambiguous_getenumerator_name = true;
				return true;
			}

			bool OverloadResolver.IErrorHandler.ArgumentMismatch (ResolveContext rc, MemberSpec best, Argument arg, int index)
			{
				return false;
			}

			bool OverloadResolver.IErrorHandler.NoArgumentMatch (ResolveContext rc, MemberSpec best)
			{
				return false;
			}

			bool OverloadResolver.IErrorHandler.TypeInferenceFailed (ResolveContext rc, MemberSpec best)
			{
				return false;
			}

			#endregion
		}

		Expression type;
		LocalVariable variable;
		Expression expr;
		Block body;

		public Foreach (Expression type, LocalVariable var, Expression expr, Statement stmt, Block body, Location l)
			: base (stmt)
		{
			this.type = type;
			this.variable = var;
			this.expr = expr;
			this.body = body;
			loc = l;
		}

		public Expression Expr {
			get { return expr; }
		}

		public Expression TypeExpression {
			get { return type; }
		}

		public LocalVariable Variable {
			get { return variable; }
		}

		public override Reachability MarkReachable (Reachability rc)
		{
			base.MarkReachable (rc);

			body.MarkReachable (rc);

			return rc;
		}

		public override bool Resolve (BlockContext ec)
		{
			expr = expr.Resolve (ec);
			if (expr == null)
				return false;

			if (expr.IsNull) {
				ec.Report.Error (186, loc, "Use of null is not valid in this context");
				return false;
			}

			body.AddStatement (Statement);

			if (expr.Type.BuiltinType == BuiltinTypeSpec.Type.String) {
				Statement = new ArrayForeach (this, 1);
			} else if (expr.Type is ArrayContainer) {
				Statement = new ArrayForeach (this, ((ArrayContainer) expr.Type).Rank);
			} else {
				if (expr.eclass == ExprClass.MethodGroup || expr is AnonymousMethodExpression) {
					ec.Report.Error (446, expr.Location, "Foreach statement cannot operate on a `{0}'",
						expr.ExprClassName);
					return false;
				}

				Statement = new CollectionForeach (this, variable, expr);
			}

			return base.Resolve (ec);
		}

		protected override void DoEmit (EmitContext ec)
		{
			Label old_begin = ec.LoopBegin, old_end = ec.LoopEnd;
			ec.LoopBegin = ec.DefineLabel ();
			ec.LoopEnd = ec.DefineLabel ();

			if (!(Statement is Block))
				ec.BeginCompilerScope ();

			variable.CreateBuilder (ec);

			Statement.Emit (ec);

			if (!(Statement is Block))
				ec.EndScope ();

			ec.LoopBegin = old_begin;
			ec.LoopEnd = old_end;
		}

		protected override bool DoFlowAnalysis (FlowAnalysisContext fc)
		{
			expr.FlowAnalysis (fc);

			var da = fc.BranchDefiniteAssignment ();
			body.FlowAnalysis (fc);
			fc.DefiniteAssignment = da;
			return false;
		}

		protected override void CloneTo (CloneContext clonectx, Statement t)
		{
			Foreach target = (Foreach) t;

			target.type = type.Clone (clonectx);
			target.expr = expr.Clone (clonectx);
			target.body = (Block) body.Clone (clonectx);
			target.Statement = Statement.Clone (clonectx);
		}
		
		public override object Accept (StructuralVisitor visitor)
		{
			return visitor.Visit (this);
		}
	}
}
