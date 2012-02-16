//
// flowanalyis.cs: The control flow analysis code
//
// Authors:
//   Martin Baulig (martin@ximian.com)
//   Raja R Harinath (rharinath@novell.com)
//   Marek Safar (marek.safar@gmail.com)
//
// Copyright 2001, 2002, 2003 Ximian, Inc.
// Copyright 2003-2008 Novell, Inc.
// Copyright 2011 Xamarin, Inc.
//

using System;
using System.Text;
using System.Collections.Generic;

namespace Mono.CSharp
{
	// <summary>
	//   A new instance of this class is created every time a new block is resolved
	//   and if there's branching in the block's control flow.
	// </summary>
	public abstract class FlowBranching
	{
		// <summary>
		//   The type of a FlowBranching.
		// </summary>
		public enum BranchingType : byte {
			// Normal (conditional or toplevel) block.
			Block,

			// Conditional.
			Conditional,

			// A loop block.
			Loop,

			// The statement embedded inside a loop
			Embedded,

			// part of a block headed by a jump target
			Labeled,

			// TryCatch block.
			TryCatch,

			// TryFinally, Using, Lock, CollectionForeach
			Exception,

			// Switch block.
			Switch,

			// The toplevel block of a function
			Toplevel,

			// An iterator block
			Iterator
		}

		// <summary>
		//   The type of one sibling of a branching.
		// </summary>
		public enum SiblingType : byte {
			Block,
			Conditional,
			SwitchSection,
			Try,
			Catch,
			Finally
		}

		public static FlowBranching CreateBranching (FlowBranching parent, BranchingType type, Block block, Location loc)
		{
			switch (type) {
			case BranchingType.Exception:
			case BranchingType.Labeled:
			case BranchingType.Toplevel:
			case BranchingType.TryCatch:
				throw new InvalidOperationException ();

			case BranchingType.Switch:
				return new FlowBranchingBreakable (parent, type, SiblingType.SwitchSection, block, loc);

			case BranchingType.Block:
				return new FlowBranchingBlock (parent, type, SiblingType.Block, block, loc);

			case BranchingType.Loop:
				return new FlowBranchingBreakable (parent, type, SiblingType.Conditional, block, loc);

			case BranchingType.Embedded:
				return new FlowBranchingContinuable (parent, type, SiblingType.Conditional, block, loc);

			default:
				return new FlowBranchingBlock (parent, type, SiblingType.Conditional, block, loc);
			}
		}

		// <summary>
		//   The type of this flow branching.
		// </summary>
		public readonly BranchingType Type;

		// <summary>
		//   The block this branching is contained in.  This may be null if it's not
		//   a top-level block and it doesn't declare any local variables.
		// </summary>
		public readonly Block Block;

		// <summary>
		//   The parent of this branching or null if this is the top-block.
		// </summary>
		public readonly FlowBranching Parent;

		// <summary>
		//   Start-Location of this flow branching.
		// </summary>
		public readonly Location Location;

		static int next_id = 0;
		int id;

		// <summary>
		//   The vector contains a BitArray with information about which local variables
		//   and parameters are already initialized at the current code position.
		// </summary>
		public class UsageVector {
			// <summary>
			//   The type of this branching.
			// </summary>
			public readonly SiblingType Type;

			// <summary>
			//   Start location of this branching.
			// </summary>
			public Location Location;

			// <summary>
			//   This is only valid for SwitchSection, Try, Catch and Finally.
			// </summary>
			public readonly Block Block;

			// <summary>
			//   The number of locals in this block.
			// </summary>
			public readonly int CountLocals;

			// <summary>
			//   If not null, then we inherit our state from this vector and do a
			//   copy-on-write.  If null, then we're the first sibling in a top-level
			//   block and inherit from the empty vector.
			// </summary>
			public readonly UsageVector InheritsFrom;

			// <summary>
			//   This is used to construct a list of UsageVector's.
			// </summary>
			public UsageVector Next;

			//
			// Private.
			//
			MyBitVector locals;
			bool is_unreachable;

			static int next_id = 0;
			int id;

			//
			// Normally, you should not use any of these constructors.
			//
			public UsageVector (SiblingType type, UsageVector parent, Block block, Location loc, int num_locals)
			{
				this.Type = type;
				this.Block = block;
				this.Location = loc;
				this.InheritsFrom = parent;
				this.CountLocals = num_locals;

				locals = num_locals == 0 
					? MyBitVector.Empty
					: new MyBitVector (parent == null ? MyBitVector.Empty : parent.locals, num_locals);

				if (parent != null)
					is_unreachable = parent.is_unreachable;

				id = ++next_id;

			}

			public UsageVector (SiblingType type, UsageVector parent, Block block, Location loc)
				: this (type, parent, block, loc, parent.CountLocals)
			{ }

			private UsageVector (MyBitVector locals, bool is_unreachable, Block block, Location loc)
			{
				this.Type = SiblingType.Block;
				this.Location = loc;
				this.Block = block;

				this.is_unreachable = is_unreachable;

				this.locals = locals;

				id = ++next_id;

			}

			// <summary>
			//   This does a deep copy of the usage vector.
			// </summary>
			public UsageVector Clone ()
			{
				UsageVector retval = new UsageVector (Type, null, Block, Location, CountLocals);

				retval.locals = locals.Clone ();
				retval.is_unreachable = is_unreachable;

				return retval;
			}

			public bool IsAssigned (VariableInfo var, bool ignoreReachability)
			{
				if (!ignoreReachability && !var.IsParameter && IsUnreachable)
					return true;

				return var.IsAssigned (locals);
			}

			public void SetAssigned (VariableInfo var)
			{
				if (!var.IsParameter && IsUnreachable)
					return;

				var.SetAssigned (locals);
			}

			public bool IsFieldAssigned (VariableInfo var, string name)
			{
				if (/*!var.IsParameter &&*/ IsUnreachable)
					return true;

				return var.IsStructFieldAssigned (locals, name);
			}

			public void SetFieldAssigned (VariableInfo var, string name)
			{
				if (/*!var.IsParameter &&*/ IsUnreachable)
					return;

				var.SetStructFieldAssigned (locals, name);
			}

			public bool IsUnreachable {
				get {
					return is_unreachable;
				}
				set {
					is_unreachable = value;
				}
			}

			public void ResetBarrier ()
			{
				is_unreachable = false;
			}

			public void Goto ()
			{
				is_unreachable = true;
			}

			public static UsageVector MergeSiblings (UsageVector sibling_list, Location loc)
			{
				if (sibling_list.Next == null)
					return sibling_list;

				MyBitVector locals = null;
				bool is_unreachable = sibling_list.is_unreachable;

				if (!sibling_list.IsUnreachable)
					locals &= sibling_list.locals;

				for (UsageVector child = sibling_list.Next; child != null; child = child.Next) {
					is_unreachable &= child.is_unreachable;

					if (!child.IsUnreachable)
						locals &= child.locals;
				}

				return new UsageVector (locals, is_unreachable, null, loc);
			}

			// <summary>
			//   Merges a child branching.
			// </summary>
			public UsageVector MergeChild (UsageVector child, bool overwrite)
			{
				Report.Debug (2, "    MERGING CHILD EFFECTS", this, child, Type);

				bool new_isunr = child.is_unreachable;

				//
				// We've now either reached the point after the branching or we will
				// never get there since we always return or always throw an exception.
				//
				// If we can reach the point after the branching, mark all locals and
				// parameters as initialized which have been initialized in all branches
				// we need to look at (see above).
				//

				if ((Type == SiblingType.SwitchSection) && !new_isunr) {
					Report.Error (163, Location,
						      "Control cannot fall through from one " +
						      "case label to another");
					return child;
				}

				locals |= child.locals;

				// throw away un-necessary information about variables in child blocks
				if (locals.Count != CountLocals)
					locals = new MyBitVector (locals, CountLocals);

				if (overwrite)
					is_unreachable = new_isunr;
				else
					is_unreachable |= new_isunr;

				return child;
			}

			public void MergeOrigins (UsageVector o_vectors)
			{
				Report.Debug (1, "  MERGING BREAK ORIGINS", this);

				if (o_vectors == null)
					return;

				if (IsUnreachable && locals != null)
					locals.SetAll (true);

				for (UsageVector vector = o_vectors; vector != null; vector = vector.Next) {
					Report.Debug (1, "    MERGING BREAK ORIGIN", vector);
					if (vector.IsUnreachable)
						continue;
					locals &= vector.locals;
					is_unreachable &= vector.is_unreachable;
				}

				Report.Debug (1, "  MERGING BREAK ORIGINS DONE", this);
			}

			//
			// Debugging stuff.
			//

			public override string ToString ()
			{
				return String.Format ("Vector ({0},{1},{2}-{3})", Type, id, is_unreachable, locals);
			}
		}

		// <summary>
		//   Creates a new flow branching which is contained in `parent'.
		//   You should only pass non-null for the `block' argument if this block
		//   introduces any new variables - in this case, we need to create a new
		//   usage vector with a different size than our parent's one.
		// </summary>
		protected FlowBranching (FlowBranching parent, BranchingType type, SiblingType stype,
					 Block block, Location loc)
		{
			Parent = parent;
			Block = block;
			Location = loc;
			Type = type;
			id = ++next_id;

			UsageVector vector;
			if (Block != null) {
				UsageVector parent_vector = parent != null ? parent.CurrentUsageVector : null;
				vector = new UsageVector (stype, parent_vector, Block, loc, Block.AssignableSlots);
			} else {
				vector = new UsageVector (stype, Parent.CurrentUsageVector, null, loc);
			}

			AddSibling (vector);
		}

		public abstract UsageVector CurrentUsageVector {
			get;
		}				

		// <summary>
		//   Creates a sibling of the current usage vector.
		// </summary>
		public virtual void CreateSibling (Block block, SiblingType type)
		{
			UsageVector vector = new UsageVector (
				type, Parent.CurrentUsageVector, block, Location);
			AddSibling (vector);

			Report.Debug (1, "  CREATED SIBLING", CurrentUsageVector);
		}

		public void CreateSibling ()
		{
			CreateSibling (null, SiblingType.Conditional);
		}

		protected abstract void AddSibling (UsageVector uv);

		protected abstract UsageVector Merge ();

		public UsageVector MergeChild (FlowBranching child)
		{
			return CurrentUsageVector.MergeChild (child.Merge (), true);
 		}

		public virtual bool CheckRethrow (Location loc)
		{
			return Parent.CheckRethrow (loc);
		}

		public virtual bool AddResumePoint (ResumableStatement stmt, out int pc)
		{
			return Parent.AddResumePoint (stmt, out pc);
		}

		// returns true if we crossed an unwind-protected region (try/catch/finally, lock, using, ...)
		public virtual bool AddBreakOrigin (UsageVector vector, Location loc)
		{
			return Parent.AddBreakOrigin (vector, loc);
		}

		// returns true if we crossed an unwind-protected region (try/catch/finally, lock, using, ...)
		public virtual bool AddContinueOrigin (UsageVector vector, Location loc)
		{
			return Parent.AddContinueOrigin (vector, loc);
		}

		// returns true if we crossed an unwind-protected region (try/catch/finally, lock, using, ...)
		public virtual bool AddReturnOrigin (UsageVector vector, ExitStatement stmt)
		{
			return Parent.AddReturnOrigin (vector, stmt);
		}

		// returns true if we crossed an unwind-protected region (try/catch/finally, lock, using, ...)
		public virtual bool AddGotoOrigin (UsageVector vector, Goto goto_stmt)
		{
			return Parent.AddGotoOrigin (vector, goto_stmt);
		}

		public bool IsAssigned (VariableInfo vi)
		{
			return CurrentUsageVector.IsAssigned (vi, false);
		}

		public bool IsStructFieldAssigned (VariableInfo vi, string field_name)
		{
			return CurrentUsageVector.IsAssigned (vi, false) || CurrentUsageVector.IsFieldAssigned (vi, field_name);
		}

		protected static Report Report {
			get { return RootContext.ToplevelTypes.Compiler.Report; }
		}

		public void SetAssigned (VariableInfo vi)
		{
			CurrentUsageVector.SetAssigned (vi);
		}

		public void SetFieldAssigned (VariableInfo vi, string name)
		{
			CurrentUsageVector.SetFieldAssigned (vi, name);
		}

#if DEBUG
		public override string ToString ()
		{
			StringBuilder sb = new StringBuilder ();
			sb.Append (GetType ());
			sb.Append (" (");

			sb.Append (id);
			sb.Append (",");
			sb.Append (Type);
			if (Block != null) {
				sb.Append (" - ");
				sb.Append (Block.ID);
				sb.Append (" - ");
				sb.Append (Block.StartLocation);
			}
			sb.Append (" - ");
			// sb.Append (Siblings.Length);
			// sb.Append (" - ");
			sb.Append (CurrentUsageVector);
			sb.Append (")");
			return sb.ToString ();
		}
#endif

		public string Name {
			get { return String.Format ("{0} ({1}:{2}:{3})", GetType (), id, Type, Location); }
		}
	}

	public class FlowBranchingBlock : FlowBranching
	{
		UsageVector sibling_list = null;

		public FlowBranchingBlock (FlowBranching parent, BranchingType type,
					   SiblingType stype, Block block, Location loc)
			: base (parent, type, stype, block, loc)
		{ }

		public override UsageVector CurrentUsageVector {
			get { return sibling_list; }
		}

		protected override void AddSibling (UsageVector sibling)
		{
			if (sibling_list != null && sibling_list.Type == SiblingType.Block)
				throw new InternalErrorException ("Blocks don't have sibling flow paths");
			sibling.Next = sibling_list;
			sibling_list = sibling;
		}

		public override bool AddGotoOrigin (UsageVector vector, Goto goto_stmt)
		{
			LabeledStatement stmt = Block == null ? null : Block.LookupLabel (goto_stmt.Target);
			if (stmt == null)
				return Parent.AddGotoOrigin (vector, goto_stmt);

			// forward jump
			goto_stmt.SetResolvedTarget (stmt);
			stmt.AddUsageVector (vector);
			return false;
		}
		
		public static void Error_UnknownLabel (Location loc, string label, Report Report)
		{
			Report.Error(159, loc, "The label `{0}:' could not be found within the scope of the goto statement",
				label);
		}

		protected override UsageVector Merge ()
		{
			Report.Debug (2, "  MERGING SIBLINGS", Name);
			UsageVector vector = UsageVector.MergeSiblings (sibling_list, Location);
			Report.Debug (2, "  MERGING SIBLINGS DONE", Name, vector);
			return vector;
		}
	}

	public class FlowBranchingBreakable : FlowBranchingBlock
	{
		UsageVector break_origins;

		public FlowBranchingBreakable (FlowBranching parent, BranchingType type, SiblingType stype, Block block, Location loc)
			: base (parent, type, stype, block, loc)
		{ }

		public override bool AddBreakOrigin (UsageVector vector, Location loc)
		{
			vector = vector.Clone ();
			vector.Next = break_origins;
			break_origins = vector;
			return false;
		}

		protected override UsageVector Merge ()
		{
			UsageVector vector = base.Merge ();
			vector.MergeOrigins (break_origins);
			return vector;
		}
	}

	public class FlowBranchingContinuable : FlowBranchingBlock
	{
		UsageVector continue_origins;

		public FlowBranchingContinuable (FlowBranching parent, BranchingType type, SiblingType stype, Block block, Location loc)
			: base (parent, type, stype, block, loc)
		{ }

		public override bool AddContinueOrigin (UsageVector vector, Location loc)
		{
			vector = vector.Clone ();
			vector.Next = continue_origins;
			continue_origins = vector;
			return false;
		}

		protected override UsageVector Merge ()
		{
			UsageVector vector = base.Merge ();
			vector.MergeOrigins (continue_origins);
			return vector;
		}
	}

	public class FlowBranchingLabeled : FlowBranchingBlock
	{
		LabeledStatement stmt;
		UsageVector actual;

		public FlowBranchingLabeled (FlowBranching parent, LabeledStatement stmt)
			: base (parent, BranchingType.Labeled, SiblingType.Conditional, null, stmt.loc)
		{
			this.stmt = stmt;
			CurrentUsageVector.MergeOrigins (stmt.JumpOrigins);
			actual = CurrentUsageVector.Clone ();

			// stand-in for backward jumps
			CurrentUsageVector.ResetBarrier ();
		}

		public override bool AddGotoOrigin (UsageVector vector, Goto goto_stmt)
		{
			if (goto_stmt.Target != stmt.Name)
				return Parent.AddGotoOrigin (vector, goto_stmt);

			// backward jump
			goto_stmt.SetResolvedTarget (stmt);
			actual.MergeOrigins (vector.Clone ());

			return false;
		}

		protected override UsageVector Merge ()
		{
			UsageVector vector = base.Merge ();

			if (actual.IsUnreachable)
				Report.Warning (162, 2, stmt.loc, "Unreachable code detected");

			actual.MergeChild (vector, false);
			return actual;
		}
	}

	public class FlowBranchingIterator : FlowBranchingBlock
	{
		readonly Iterator iterator;

		public FlowBranchingIterator (FlowBranching parent, Iterator iterator)
			: base (parent, BranchingType.Iterator, SiblingType.Block, iterator.Block, iterator.Location)
		{
			this.iterator = iterator;
		}

		public override bool AddResumePoint (ResumableStatement stmt, out int pc)
		{
			pc = iterator.AddResumePoint (stmt);
			return false;
		}
	}

	public class FlowBranchingToplevel : FlowBranchingBlock
	{
		UsageVector return_origins;

		public FlowBranchingToplevel (FlowBranching parent, ParametersBlock stmt)
			: base (parent, BranchingType.Toplevel, SiblingType.Conditional, stmt, stmt.loc)
		{
		}

		public override bool CheckRethrow (Location loc)
		{
			Report.Error (156, loc, "A throw statement with no arguments is not allowed outside of a catch clause");
			return false;
		}

		public override bool AddResumePoint (ResumableStatement stmt, out int pc)
		{
			throw new InternalErrorException ("A yield in a non-iterator block");
		}

		public override bool AddBreakOrigin (UsageVector vector, Location loc)
		{
			Report.Error (139, loc, "No enclosing loop out of which to break or continue");
			return false;
		}

		public override bool AddContinueOrigin (UsageVector vector, Location loc)
		{
			Report.Error (139, loc, "No enclosing loop out of which to break or continue");
			return false;
		}

		public override bool AddReturnOrigin (UsageVector vector, ExitStatement stmt)
		{
			vector = vector.Clone ();
			vector.Location = stmt.loc;
			vector.Next = return_origins;
			return_origins = vector;
			return false;
		}

		public override bool AddGotoOrigin (UsageVector vector, Goto goto_stmt)
		{
			string name = goto_stmt.Target;
			LabeledStatement s = Block.LookupLabel (name);
			if (s != null)
				throw new InternalErrorException ("Shouldn't get here");

			if (Parent == null) {
				Error_UnknownLabel (goto_stmt.loc, name, Report);
				return false;
			}

			int errors = Report.Errors;
			Parent.AddGotoOrigin (vector, goto_stmt);
			if (errors == Report.Errors)
				Report.Error (1632, goto_stmt.loc, "Control cannot leave the body of an anonymous method");
			return false;
		}

		protected override UsageVector Merge ()
		{
			for (UsageVector origin = return_origins; origin != null; origin = origin.Next)
				Block.ParametersBlock.CheckOutParameters (origin);

			UsageVector vector = base.Merge ();
			Block.ParametersBlock.CheckOutParameters (vector);
			// Note: we _do_not_ merge in the return origins
			return vector;
		}

		public bool End ()
		{
			return Merge ().IsUnreachable;
		}
	}

	public class FlowBranchingTryCatch : FlowBranchingBlock
	{
		readonly TryCatch tc;

		public FlowBranchingTryCatch (FlowBranching parent, TryCatch stmt)
			: base (parent, BranchingType.Block, SiblingType.Try, null, stmt.loc)
		{
			this.tc = stmt;
		}

		public override bool CheckRethrow (Location loc)
		{
			return CurrentUsageVector.Next != null || Parent.CheckRethrow (loc);
		}

		public override bool AddResumePoint (ResumableStatement stmt, out int pc)
		{
			int errors = Report.Errors;
			Parent.AddResumePoint (tc.IsTryCatchFinally ? stmt : tc, out pc);
			if (errors == Report.Errors) {
				if (stmt is AwaitStatement) {
					if (CurrentUsageVector.Next != null) {
						Report.Error (1985, stmt.loc, "The `await' operator cannot be used in the body of a catch clause");
					} else {
						this.tc.AddResumePoint (stmt, pc);
					}
				} else {
					if (CurrentUsageVector.Next == null)
						Report.Error (1626, stmt.loc, "Cannot yield a value in the body of a try block with a catch clause");
					else
						Report.Error (1631, stmt.loc, "Cannot yield a value in the body of a catch clause");
				}
			}

			return true;
		}

		public override bool AddBreakOrigin (UsageVector vector, Location loc)
		{
			Parent.AddBreakOrigin (vector, loc);
			tc.SomeCodeFollows ();
			return true;
		}

		public override bool AddContinueOrigin (UsageVector vector, Location loc)
		{
			Parent.AddContinueOrigin (vector, loc);
			tc.SomeCodeFollows ();
			return true;
		}

		public override bool AddReturnOrigin (UsageVector vector, ExitStatement exit_stmt)
		{
			Parent.AddReturnOrigin (vector, exit_stmt);
			tc.SomeCodeFollows ();
			return true;
		}

		public override bool AddGotoOrigin (UsageVector vector, Goto goto_stmt)
		{
			Parent.AddGotoOrigin (vector, goto_stmt);
			return true;
		}
	}

	public  class FlowBranchingAsync : FlowBranchingBlock
	{
		readonly AsyncInitializer async_init;

		public FlowBranchingAsync (FlowBranching parent, AsyncInitializer async_init)
			: base (parent, BranchingType.Block, SiblingType.Try, null, async_init.Location)
		{
			this.async_init = async_init;
		}
/*
		public override bool CheckRethrow (Location loc)
		{
			return CurrentUsageVector.Next != null || Parent.CheckRethrow (loc);
		}
*/
		public override bool AddResumePoint (ResumableStatement stmt, out int pc)
		{
			pc = async_init.AddResumePoint (stmt);
			return true;
		}

		public override bool AddBreakOrigin (UsageVector vector, Location loc)
		{
			Parent.AddBreakOrigin (vector, loc);
			return true;
		}

		public override bool AddContinueOrigin (UsageVector vector, Location loc)
		{
			Parent.AddContinueOrigin (vector, loc);
			return true;
		}

		public override bool AddReturnOrigin (UsageVector vector, ExitStatement exit_stmt)
		{
			Parent.AddReturnOrigin (vector, exit_stmt);
			return true;
		}

		public override bool AddGotoOrigin (UsageVector vector, Goto goto_stmt)
		{
			Parent.AddGotoOrigin (vector, goto_stmt);
			return true;
		}
	}

	public class FlowBranchingTryFinally : FlowBranching
	{
		ExceptionStatement stmt;
		UsageVector current_vector;
		UsageVector try_vector;
		UsageVector finally_vector;

		abstract class SavedOrigin {
			public readonly SavedOrigin Next;
			public readonly UsageVector Vector;

			protected SavedOrigin (SavedOrigin next, UsageVector vector)
			{
				Next = next;
				Vector = vector.Clone ();
			}

			protected abstract void DoPropagateFinally (FlowBranching parent);
			public void PropagateFinally (UsageVector finally_vector, FlowBranching parent)
			{
				if (finally_vector != null)
					Vector.MergeChild (finally_vector, false);
				DoPropagateFinally (parent);
			}
		}

		class BreakOrigin : SavedOrigin {
			Location Loc;
			public BreakOrigin (SavedOrigin next, UsageVector vector, Location loc)
				: base (next, vector)
			{
				Loc = loc;
			}

			protected override void DoPropagateFinally (FlowBranching parent)
			{
				parent.AddBreakOrigin (Vector, Loc);
			}
		}

		class ContinueOrigin : SavedOrigin {
			Location Loc;
			public ContinueOrigin (SavedOrigin next, UsageVector vector, Location loc)
				: base (next, vector)
			{
				Loc = loc;
			}

			protected override void DoPropagateFinally (FlowBranching parent)
			{
				parent.AddContinueOrigin (Vector, Loc);
			}
		}

		class ReturnOrigin : SavedOrigin {
			public ExitStatement Stmt;

			public ReturnOrigin (SavedOrigin next, UsageVector vector, ExitStatement stmt)
				: base (next, vector)
			{
				Stmt = stmt;
			}

			protected override void DoPropagateFinally (FlowBranching parent)
			{
				parent.AddReturnOrigin (Vector, Stmt);
			}
		}

		class GotoOrigin : SavedOrigin {
			public Goto Stmt;

			public GotoOrigin (SavedOrigin next, UsageVector vector, Goto stmt)
				: base (next, vector)
			{
				Stmt = stmt;
			}

			protected override void DoPropagateFinally (FlowBranching parent)
			{
				parent.AddGotoOrigin (Vector, Stmt);
			}
		}

		SavedOrigin saved_origins;

		public FlowBranchingTryFinally (FlowBranching parent,
					       ExceptionStatement stmt)
			: base (parent, BranchingType.Exception, SiblingType.Try,
				null, stmt.loc)
		{
			this.stmt = stmt;
		}

		protected override void AddSibling (UsageVector sibling)
		{
			switch (sibling.Type) {
			case SiblingType.Try:
				try_vector = sibling;
				break;
			case SiblingType.Finally:
				finally_vector = sibling;
				break;
			default:
				throw new InvalidOperationException ();
			}
			current_vector = sibling;
		}

		public override UsageVector CurrentUsageVector {
			get { return current_vector; }
		}

		public override bool CheckRethrow (Location loc)
		{
			if (!Parent.CheckRethrow (loc))
				return false;
			if (finally_vector == null)
				return true;
			Report.Error (724, loc, "A throw statement with no arguments is not allowed inside of a finally clause nested inside of the innermost catch clause");
			return false;
		}

		public override bool AddResumePoint (ResumableStatement stmt, out int pc)
		{
			int errors = Report.Errors;
			Parent.AddResumePoint (this.stmt, out pc);
			if (errors == Report.Errors) {
				if (finally_vector == null)
					this.stmt.AddResumePoint (stmt, pc);
				else {
					if (stmt is AwaitStatement) {
						Report.Error (1984, stmt.loc, "The `await' operator cannot be used in the body of a finally clause");
					} else {
						Report.Error (1625, stmt.loc, "Cannot yield in the body of a finally clause");
					}
				}
			}
			return true;
		}

		public override bool AddBreakOrigin (UsageVector vector, Location loc)
		{
			if (finally_vector != null) {
				int errors = Report.Errors;
				Parent.AddBreakOrigin (vector, loc);
				if (errors == Report.Errors)
					Report.Error (157, loc, "Control cannot leave the body of a finally clause");
			} else {
				saved_origins = new BreakOrigin (saved_origins, vector, loc);
			}

			// either the loop test or a back jump will follow code
			stmt.SomeCodeFollows ();
			return true;
		}

		public override bool AddContinueOrigin (UsageVector vector, Location loc)
		{
			if (finally_vector != null) {
				int errors = Report.Errors;
				Parent.AddContinueOrigin (vector, loc);
				if (errors == Report.Errors)
					Report.Error (157, loc, "Control cannot leave the body of a finally clause");
			} else {
				saved_origins = new ContinueOrigin (saved_origins, vector, loc);
			}

			// either the loop test or a back jump will follow code
			stmt.SomeCodeFollows ();
			return true;
		}

		public override bool AddReturnOrigin (UsageVector vector, ExitStatement exit_stmt)
		{
			if (finally_vector != null) {
				int errors = Report.Errors;
				Parent.AddReturnOrigin (vector, exit_stmt);
				if (errors == Report.Errors)
					exit_stmt.Error_FinallyClause (Report);
			} else {
				saved_origins = new ReturnOrigin (saved_origins, vector, exit_stmt);
			}

			// sets ec.NeedReturnLabel()
			stmt.SomeCodeFollows ();
			return true;
		}

		public override bool AddGotoOrigin (UsageVector vector, Goto goto_stmt)
		{
			LabeledStatement s = current_vector.Block == null ? null : current_vector.Block.LookupLabel (goto_stmt.Target);
			if (s != null)
				throw new InternalErrorException ("Shouldn't get here");

			if (finally_vector != null) {
				int errors = Report.Errors;
				Parent.AddGotoOrigin (vector, goto_stmt);
				if (errors == Report.Errors)
					Report.Error (157, goto_stmt.loc, "Control cannot leave the body of a finally clause");
			} else {
				saved_origins = new GotoOrigin (saved_origins, vector, goto_stmt);
			}
			return true;
		}

		protected override UsageVector Merge ()
		{
			UsageVector vector = try_vector.Clone ();

			if (finally_vector != null)
				vector.MergeChild (finally_vector, false);

			for (SavedOrigin origin = saved_origins; origin != null; origin = origin.Next)
				origin.PropagateFinally (finally_vector, Parent);

			return vector;
		}
	}

	// <summary>
	//   This is used by the flow analysis code to keep track of the type of local variables.
	//
	//   The flow code uses a BitVector to keep track of whether a variable has been assigned
	//   or not.  This is easy for fundamental types (int, char etc.) or reference types since
	//   you can only assign the whole variable as such.
	//
	//   For structs, we also need to keep track of all its fields.  To do this, we allocate one
	//   bit for the struct itself (it's used if you assign/access the whole struct) followed by
	//   one bit for each of its fields.
	//
	//   This class computes this `layout' for each type.
	// </summary>
	public class TypeInfo
	{
		// <summary>
		//   Total number of bits a variable of this type consumes in the flow vector.
		// </summary>
		public readonly int TotalLength;

		// <summary>
		//   Number of bits the simple fields of a variable of this type consume
		//   in the flow vector.
		// </summary>
		public readonly int Length;

		// <summary>
		//   This is only used by sub-structs.
		// </summary>
		public readonly int Offset;

		// <summary>
		//   If this is a struct.
		// </summary>
		public readonly bool IsStruct;

		// <summary>
		//   If this is a struct, all fields which are structs theirselves.
		// </summary>
		public TypeInfo[] SubStructInfo;

		readonly StructInfo struct_info;
		private static Dictionary<TypeSpec, TypeInfo> type_hash;

		static readonly TypeInfo simple_type = new TypeInfo (1);
		
		static TypeInfo ()
		{
			Reset ();
		}
		
		public static void Reset ()
		{
			type_hash = new Dictionary<TypeSpec, TypeInfo> ();
			StructInfo.field_type_hash = new Dictionary<TypeSpec, StructInfo> ();
		}

		TypeInfo (int totalLength)
		{
			this.TotalLength = totalLength;
		}
		
		TypeInfo (StructInfo struct_info, int offset)
		{
			this.struct_info = struct_info;
			this.Offset = offset;
			this.Length = struct_info.Length;
			this.TotalLength = struct_info.TotalLength;
			this.SubStructInfo = struct_info.StructFields;
			this.IsStruct = true;
		}
		
		public int GetFieldIndex (string name)
		{
			if (struct_info == null)
				return 0;

			return struct_info [name];
		}

		public TypeInfo GetStructField (string name)
		{
			if (struct_info == null)
				return null;

			return struct_info.GetStructField (name);
		}

		public static TypeInfo GetTypeInfo (TypeSpec type)
		{
			if (!type.IsStruct)
				return simple_type;

			TypeInfo info;
			if (type_hash.TryGetValue (type, out info))
				return info;

			var struct_info = StructInfo.GetStructInfo (type);
			if (struct_info != null) {
				info = new TypeInfo (struct_info, 0);
			} else {
				info = simple_type;
			}

			type_hash.Add (type, info);
			return info;
		}

		// <summary>
		//   A struct's constructor must always assign all fields.
		//   This method checks whether it actually does so.
		// </summary>
		public bool IsFullyInitialized (BlockContext ec, VariableInfo vi, Location loc)
		{
			if (struct_info == null)
				return true;

			bool ok = true;
			FlowBranching branching = ec.CurrentBranching;
			for (int i = 0; i < struct_info.Count; i++) {
				var field = struct_info.Fields [i];

				if (!branching.IsStructFieldAssigned (vi, field.Name)) {
					if (field.MemberDefinition is Property.BackingField) {
						ec.Report.Error (843, loc,
							"An automatically implemented property `{0}' must be fully assigned before control leaves the constructor. Consider calling the default struct contructor from a constructor initializer",
							field.GetSignatureForError ());
					} else {
						ec.Report.Error (171, loc,
							"Field `{0}' must be fully assigned before control leaves the constructor",
							field.GetSignatureForError ());
					}
					ok = false;
				}
			}

			return ok;
		}

		public override string ToString ()
		{
			return String.Format ("TypeInfo ({0}:{1}:{2})",
					      Offset, Length, TotalLength);
		}

		class StructInfo
		{
			readonly List<FieldSpec> fields;
			public readonly TypeInfo[] StructFields;
			public readonly int Length;
			public readonly int TotalLength;

			public static Dictionary<TypeSpec, StructInfo> field_type_hash;
			private Dictionary<string, TypeInfo> struct_field_hash;
			private Dictionary<string, int> field_hash;

			bool InTransit;

			//
			// We only need one instance per type
			//
			StructInfo (TypeSpec type)
			{
				field_type_hash.Add (type, this);

				fields = MemberCache.GetAllFieldsForDefiniteAssignment (type);

				struct_field_hash = new Dictionary<string, TypeInfo> ();
				field_hash = new Dictionary<string, int> (fields.Count);

				StructFields = new TypeInfo[fields.Count];
				StructInfo[] sinfo = new StructInfo[fields.Count];

				InTransit = true;

				for (int i = 0; i < fields.Count; i++) {
					var field = fields [i];

					if (field.MemberType.IsStruct)
						sinfo [i] = GetStructInfo (field.MemberType);

					if (sinfo [i] == null)
						field_hash.Add (field.Name, ++Length);
					else if (sinfo [i].InTransit) {
						sinfo [i] = null;
						return;
					}
				}

				InTransit = false;

				TotalLength = Length + 1;
				for (int i = 0; i < fields.Count; i++) {
					var field = fields [i];

					if (sinfo [i] == null)
						continue;

					field_hash.Add (field.Name, TotalLength);

					StructFields [i] = new TypeInfo (sinfo [i], TotalLength);
					struct_field_hash.Add (field.Name, StructFields [i]);
					TotalLength += sinfo [i].TotalLength;
				}
			}

			public int Count {
				get {
					return fields.Count;
				}
			}

			public List<FieldSpec> Fields {
				get {
					return fields;
				}
			}

			public int this [string name] {
				get {
					int val;
					if (!field_hash.TryGetValue (name, out val))
						return 0;

					return val;
				}
			}

			public TypeInfo GetStructField (string name)
			{
				TypeInfo ti;
				if (struct_field_hash.TryGetValue (name, out ti))
					return ti;

				return null;
			}

			public static StructInfo GetStructInfo (TypeSpec type)
			{
				if (type.BuiltinType > 0)
					return null;

				StructInfo info;
				if (field_type_hash.TryGetValue (type, out info))
					return info;

				return new StructInfo (type);
			}
		}
	}

	// <summary>
	//   This is used by the flow analysis code to store information about a single local variable
	//   or parameter.  Depending on the variable's type, we need to allocate one or more elements
	//   in the BitVector - if it's a fundamental or reference type, we just need to know whether
	//   it has been assigned or not, but for structs, we need this information for each of its fields.
	// </summary>
	public class VariableInfo {
		readonly string Name;
		readonly TypeInfo TypeInfo;

		// <summary>
		//   The bit offset of this variable in the flow vector.
		// </summary>
		readonly int Offset;

		// <summary>
		//   The number of bits this variable needs in the flow vector.
		//   The first bit always specifies whether the variable as such has been assigned while
		//   the remaining bits contain this information for each of a struct's fields.
		// </summary>
		public readonly int Length;

		// <summary>
		//   If this is a parameter of local variable.
		// </summary>
		public readonly bool IsParameter;

		VariableInfo[] sub_info;

		VariableInfo (string name, TypeSpec type, int offset)
		{
			this.Name = name;
			this.Offset = offset;
			this.TypeInfo = TypeInfo.GetTypeInfo (type);

			Length = TypeInfo.TotalLength;

			Initialize ();
		}

		VariableInfo (VariableInfo parent, TypeInfo type)
		{
			this.Name = parent.Name;
			this.TypeInfo = type;
			this.Offset = parent.Offset + type.Offset;
			this.Length = type.TotalLength;

			this.IsParameter = parent.IsParameter;

			Initialize ();
		}

		protected void Initialize ()
		{
			TypeInfo[] sub_fields = TypeInfo.SubStructInfo;
			if (sub_fields != null) {
				sub_info = new VariableInfo [sub_fields.Length];
				for (int i = 0; i < sub_fields.Length; i++) {
					if (sub_fields [i] != null)
						sub_info [i] = new VariableInfo (this, sub_fields [i]);
				}
			} else
				sub_info = new VariableInfo [0];
		}

		public VariableInfo (LocalVariable local_info, int offset)
			: this (local_info.Name, local_info.Type, offset)
		{
			this.IsParameter = false;
		}

		public VariableInfo (ParametersCompiled ip, int i, int offset)
			: this (ip.FixedParameters [i].Name, ip.Types [i], offset)
		{
			this.IsParameter = true;
		}

		public bool IsAssigned (ResolveContext ec)
		{
			return !ec.DoFlowAnalysis || ec.CurrentBranching.IsAssigned (this);
		}

		public bool IsAssigned (MyBitVector vector)
		{
			if (vector == null)
				return true;

			if (vector [Offset])
				return true;

			// Unless this is a struct
			if (!TypeInfo.IsStruct)
				return false;

			//
			// Following case cannot be handled fully by SetStructFieldAssigned
			// because we may encounter following case
			// 
			// struct A { B b }
			// struct B { int value; }
			//
			// setting a.b.value is propagated only to B's vector and not upwards to possible parents
			//
			//
			// Each field must be assigned
			//
			for (int i = Offset + 1; i <= TypeInfo.Length + Offset; i++) {
				if (!vector[i])
					return false;
			}

			// Ok, now check all fields which are structs.
			for (int i = 0; i < sub_info.Length; i++) {
				VariableInfo sinfo = sub_info[i];
				if (sinfo == null)
					continue;

				if (!sinfo.IsAssigned (vector))
					return false;
			}
			
			vector [Offset] = true;
			return true;
		}

		public bool IsEverAssigned { get; set; }

		public bool IsStructFieldAssigned (ResolveContext ec, string name)
		{
			return !ec.DoFlowAnalysis || ec.CurrentBranching.IsStructFieldAssigned (this, name);
		}

		public bool IsFullyInitialized (BlockContext bc, Location loc)
		{
			return TypeInfo.IsFullyInitialized (bc, this, loc);
		}

		public bool IsStructFieldAssigned (MyBitVector vector, string field_name)
		{
			int field_idx = TypeInfo.GetFieldIndex (field_name);

			if (field_idx == 0)
				return true;

			return vector [Offset + field_idx];
		}

		public void SetStructFieldAssigned (ResolveContext ec, string name)
		{
			if (ec.DoFlowAnalysis)
				ec.CurrentBranching.SetFieldAssigned (this, name);
		}

		public void SetAssigned (ResolveContext ec)
		{
			if (ec.DoFlowAnalysis)
				ec.CurrentBranching.SetAssigned (this);
		}

		public void SetAssigned (MyBitVector vector)
		{
			if (Length == 1)
				vector[Offset] = true;
			else
				vector.SetRange (Offset, Length);

			IsEverAssigned = true;
		}

		public void SetStructFieldAssigned (MyBitVector vector, string field_name)
		{
			if (vector[Offset])
				return;

			int field_idx = TypeInfo.GetFieldIndex (field_name);

			if (field_idx == 0)
				return;

			var complex_field = TypeInfo.GetStructField (field_name);
			if (complex_field != null) {
				vector.SetRange (Offset + complex_field.Offset, complex_field.TotalLength);
			} else {
				vector[Offset + field_idx] = true;
			}

			IsEverAssigned = true;

			//
			// Each field must be assigned
			//
			for (int i = Offset + 1; i < TypeInfo.TotalLength + Offset; i++) {
				if (!vector[i])
					return;
			}

			//
			// Set master struct flag to assigned when all tested struct
			// fields have been assigned
			//
			vector[Offset] = true;
		}

		public VariableInfo GetStructFieldInfo (string fieldName)
		{
			TypeInfo type = TypeInfo.GetStructField (fieldName);

			if (type == null)
				return null;

			return new VariableInfo (this, type);
		}

		public override string ToString ()
		{
			return String.Format ("VariableInfo ({0}:{1}:{2}:{3}:{4})",
					      Name, TypeInfo, Offset, Length, IsParameter);
		}
	}

	// <summary>
	//   This is a special bit vector which can inherit from another bit vector doing a
	//   copy-on-write strategy.  The inherited vector may have a smaller size than the
	//   current one.
	// </summary>
	public class MyBitVector {
		public readonly int Count;
		public static readonly MyBitVector Empty = new MyBitVector ();

		// Invariant: vector != null => vector.Count == Count
		// Invariant: vector == null || shared == null
		//            i.e., at most one of 'vector' and 'shared' can be non-null.  They can both be null -- that means all-ones
		// The object in 'shared' cannot be modified, while 'vector' can be freely modified
		System.Collections.BitArray vector, shared;

		MyBitVector ()
		{
			shared = new System.Collections.BitArray (0, false);
		}

		public MyBitVector (MyBitVector InheritsFrom, int Count)
		{
			if (InheritsFrom != null)
				shared = InheritsFrom.MakeShared (Count);

			this.Count = Count;
		}

		System.Collections.BitArray MakeShared (int new_count)
		{
			// Post-condition: vector == null

			// ensure we don't leak out dirty bits from the BitVector we inherited from
			if (new_count > Count &&
			    ((shared != null && shared.Count > Count) ||
			     (shared == null && vector == null)))
				initialize_vector ();

			if (vector != null) {
				shared = vector;
				vector = null;
			}

			return shared;
		}

		// <summary>
		//   Get/set bit `index' in the bit vector.
		// </summary>
		public bool this [int index] {
			get {
				if (index >= Count)
					// FIXME: Disabled due to missing anonymous method flow analysis
					// throw new ArgumentOutOfRangeException ();
					return true; 

				if (vector != null)
					return vector [index];
				if (shared == null)
					return true;
				if (index < shared.Count)
					return shared [index];
				return false;
			}

			set {
				// Only copy the vector if we're actually modifying it.
				if (this [index] != value) {
					if (vector == null)
						initialize_vector ();
					vector [index] = value;
				}
			}
		}

		// <summary>
		//   Performs an `or' operation on the bit vector.  The `new_vector' may have a
		//   different size than the current one.
		// </summary>
		private MyBitVector Or (MyBitVector new_vector)
		{
			if (Count == 0 || new_vector.Count == 0)
				return this;

			var o = new_vector.vector != null ? new_vector.vector : new_vector.shared;

			if (o == null) {
				int n = new_vector.Count;
				if (n < Count) {
					for (int i = 0; i < n; ++i)
						this [i] = true;
				} else {
					SetAll (true);
				}
				return this;
			}

			if (Count == o.Count) {
				if (vector == null) {
					if (shared == null)
						return this;
					initialize_vector ();
				}
				vector.Or (o);
				return this;
			}

			int min = o.Count;
			if (Count < min)
				min = Count;

			for (int i = 0; i < min; i++) {
				if (o [i])
					this [i] = true;
			}

			return this;
		}

		// <summary>
		//   Performs an `and' operation on the bit vector.  The `new_vector' may have
		//   a different size than the current one.
		// </summary>
		private MyBitVector And (MyBitVector new_vector)
		{
			if (Count == 0)
				return this;

			var o = new_vector.vector != null ? new_vector.vector : new_vector.shared;

			if (o == null) {
				for (int i = new_vector.Count; i < Count; ++i)
					this [i] = false;
				return this;
			}

			if (o.Count == 0) {
				SetAll (false);
				return this;
			}

			if (Count == o.Count) {
				if (vector == null) {
					if (shared == null) {
						shared = new_vector.MakeShared (Count);
						return this;
					}
					initialize_vector ();
				}
				vector.And (o);
				return this;
			}

			int min = o.Count;
			if (Count < min)
				min = Count;

			for (int i = 0; i < min; i++) {
				if (! o [i])
					this [i] = false;
			}

			for (int i = min; i < Count; i++)
				this [i] = false;

			return this;
		}

		public static MyBitVector operator & (MyBitVector a, MyBitVector b)
		{
			if (a == b)
				return a;
			if (a == null)
				return b.Clone ();
			if (b == null)
				return a.Clone ();
			if (a.Count > b.Count)
				return a.Clone ().And (b);
			else
				return b.Clone ().And (a);					
		}

		public static MyBitVector operator | (MyBitVector a, MyBitVector b)
		{
			if (a == b)
				return a;
			if (a == null)
				return new MyBitVector (null, b.Count);
			if (b == null)
				return new MyBitVector (null, a.Count);
			if (a.Count > b.Count)
				return a.Clone ().Or (b);
			else
				return b.Clone ().Or (a);
		}

		public MyBitVector Clone ()
		{
			return Count == 0 ? Empty : new MyBitVector (this, Count);
		}

		public void SetRange (int offset, int length)
		{
			if (offset > Count || offset + length > Count)
				throw new ArgumentOutOfRangeException ("flow-analysis");

			if (shared == null && vector == null)
				return;

			int i = 0;
			if (shared != null) {
				if (offset + length <= shared.Count) {
					for (; i < length; ++i)
						if (!shared [i+offset])
						    break;
					if (i == length)
						return;
				}
				initialize_vector ();
			}
			for (; i < length; ++i)
				vector [i+offset] = true;

		}

		public void SetAll (bool value)
		{
			// Don't clobber Empty
			if (Count == 0)
				return;
			shared = value ? null : Empty.MakeShared (Count);
			vector = null;
		}

		void initialize_vector ()
		{
			// Post-condition: vector != null
			if (shared == null) {
				vector = new System.Collections.BitArray (Count, true);
				return;
			}

			vector = new System.Collections.BitArray (shared);
			if (Count != vector.Count)
				vector.Length = Count;
			shared = null;
		}

		StringBuilder Dump (StringBuilder sb)
		{
			var dump = vector == null ? shared : vector;
			if (dump == null)
				return sb.Append ("/");
			if (dump == shared)
				sb.Append ("=");
			for (int i = 0; i < dump.Count; i++)
				sb.Append (dump [i] ? "1" : "0");
			return sb;
		}

		public override string ToString ()
		{
			return Dump (new StringBuilder ("{")).Append ("}").ToString ();
		}
	}
}
