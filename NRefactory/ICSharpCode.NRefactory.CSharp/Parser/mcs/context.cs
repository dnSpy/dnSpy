//
// context.cs: Various compiler contexts.
//
// Author:
//   Marek Safar (marek.safar@gmail.com)
//   Miguel de Icaza (miguel@ximian.com)
//
// Copyright 2001, 2002, 2003 Ximian, Inc.
// Copyright 2004-2009 Novell, Inc.
// Copyright 2011 Xamarin Inc.
//

using System;
using System.Collections.Generic;
using System.IO;

namespace Mono.CSharp
{
	public enum LookupMode
	{
		Normal = 0,
		Probing = 1,
		IgnoreAccessibility = 2
	}

	//
	// Implemented by elements which can act as independent contexts
	// during resolve phase. Used mostly for lookups.
	//
	public interface IMemberContext : IModuleContext
	{
		//
		// A scope type context, it can be inflated for generic types
		//
		TypeSpec CurrentType { get; }

		//
		// A scope type parameters either VAR or MVAR
		//
		TypeParameter[] CurrentTypeParameters { get; }

		//
		// A member definition of the context. For partial types definition use
		// CurrentTypeDefinition.PartialContainer otherwise the context is local
		//
		// TODO: Obsolete it in this context, dynamic context cannot guarantee sensible value
		//
		MemberCore CurrentMemberDefinition { get; }

		bool IsObsolete { get; }
		bool IsUnsafe { get; }
		bool IsStatic { get; }

		string GetSignatureForError ();

		ExtensionMethodCandidates LookupExtensionMethod (TypeSpec extensionType, string name, int arity);
		FullNamedExpression LookupNamespaceOrType (string name, int arity, LookupMode mode, Location loc);
		FullNamedExpression LookupNamespaceAlias (string name);
	}

	public interface IModuleContext
	{
		ModuleContainer Module { get; }
	}

	//
	// Block or statement resolving context
	//
	public class BlockContext : ResolveContext
	{
		FlowBranching current_flow_branching;

		readonly TypeSpec return_type;

		public int FlowOffset;

		public BlockContext (IMemberContext mc, ExplicitBlock block, TypeSpec returnType)
			: base (mc)
		{
			if (returnType == null)
				throw new ArgumentNullException ("returnType");

			this.return_type = returnType;

			// TODO: check for null value
			CurrentBlock = block;
		}

		public BlockContext (ResolveContext rc, ExplicitBlock block, TypeSpec returnType)
			: this (rc.MemberContext, block, returnType)
		{
			if (rc.IsUnsafe)
				flags |= ResolveContext.Options.UnsafeScope;

			if (rc.HasSet (ResolveContext.Options.CheckedScope))
				flags |= ResolveContext.Options.CheckedScope;
		}

		public override FlowBranching CurrentBranching {
			get { return current_flow_branching; }
		}

		public TypeSpec ReturnType {
			get { return return_type; }
		}

		// <summary>
		//   Starts a new code branching.  This inherits the state of all local
		//   variables and parameters from the current branching.
		// </summary>
		public FlowBranching StartFlowBranching (FlowBranching.BranchingType type, Location loc)
		{
			current_flow_branching = FlowBranching.CreateBranching (CurrentBranching, type, null, loc);
			return current_flow_branching;
		}

		// <summary>
		//   Starts a new code branching for block `block'.
		// </summary>
		public FlowBranching StartFlowBranching (Block block)
		{
			Set (Options.DoFlowAnalysis);

			current_flow_branching = FlowBranching.CreateBranching (
				CurrentBranching, FlowBranching.BranchingType.Block, block, block.StartLocation);
			return current_flow_branching;
		}

		public FlowBranchingTryCatch StartFlowBranching (TryCatch stmt)
		{
			FlowBranchingTryCatch branching = new FlowBranchingTryCatch (CurrentBranching, stmt);
			current_flow_branching = branching;
			return branching;
		}

		public FlowBranchingTryFinally StartFlowBranching (TryFinallyBlock stmt)
		{
			FlowBranchingTryFinally branching = new FlowBranchingTryFinally (CurrentBranching, stmt);
			current_flow_branching = branching;
			return branching;
		}

		public FlowBranchingLabeled StartFlowBranching (LabeledStatement stmt)
		{
			FlowBranchingLabeled branching = new FlowBranchingLabeled (CurrentBranching, stmt);
			current_flow_branching = branching;
			return branching;
		}

		public FlowBranchingIterator StartFlowBranching (Iterator iterator, FlowBranching parent)
		{
			FlowBranchingIterator branching = new FlowBranchingIterator (parent, iterator);
			current_flow_branching = branching;
			return branching;
		}

		public FlowBranchingAsync StartFlowBranching (AsyncInitializer asyncBody, FlowBranching parent)
		{
			var branching = new FlowBranchingAsync (parent, asyncBody);
			current_flow_branching = branching;
			return branching;
		}

		public FlowBranchingToplevel StartFlowBranching (ParametersBlock stmt, FlowBranching parent)
		{
			FlowBranchingToplevel branching = new FlowBranchingToplevel (parent, stmt);
			current_flow_branching = branching;
			return branching;
		}

		// <summary>
		//   Ends a code branching.  Merges the state of locals and parameters
		//   from all the children of the ending branching.
		// </summary>
		public bool EndFlowBranching ()
		{
			FlowBranching old = current_flow_branching;
			current_flow_branching = current_flow_branching.Parent;

			FlowBranching.UsageVector vector = current_flow_branching.MergeChild (old);
			return vector.IsUnreachable;
		}

		// <summary>
		//   Kills the current code branching.  This throws away any changed state
		//   information and should only be used in case of an error.
		// </summary>
		// FIXME: this is evil
		public void KillFlowBranching ()
		{
			current_flow_branching = current_flow_branching.Parent;
		}

#if !STATIC
		public void NeedReturnLabel ()
		{
		}
#endif
	}

	//
	// Expression resolving context
	//
	public class ResolveContext : IMemberContext
	{
		[Flags]
		public enum Options
		{
			/// <summary>
			///   This flag tracks the `checked' state of the compilation,
			///   it controls whether we should generate code that does overflow
			///   checking, or if we generate code that ignores overflows.
			///
			///   The default setting comes from the command line option to generate
			///   checked or unchecked code plus any source code changes using the
			///   checked/unchecked statements or expressions.   Contrast this with
			///   the ConstantCheckState flag.
			/// </summary>
			CheckedScope = 1 << 0,

			/// <summary>
			///   The constant check state is always set to `true' and cant be changed
			///   from the command line.  The source code can change this setting with
			///   the `checked' and `unchecked' statements and expressions. 
			/// </summary>
			ConstantCheckState = 1 << 1,

			AllCheckStateFlags = CheckedScope | ConstantCheckState,

			//
			// unsafe { ... } scope
			//
			UnsafeScope = 1 << 2,
			CatchScope = 1 << 3,
			FinallyScope = 1 << 4,
			FieldInitializerScope = 1 << 5,
			CompoundAssignmentScope = 1 << 6,
			FixedInitializerScope = 1 << 7,
			BaseInitializer = 1 << 8,

			//
			// Inside an enum definition, we do not resolve enumeration values
			// to their enumerations, but rather to the underlying type/value
			// This is so EnumVal + EnumValB can be evaluated.
			//
			// There is no "E operator + (E x, E y)", so during an enum evaluation
			// we relax the rules
			//
			EnumScope = 1 << 9,

			ConstantScope = 1 << 10,

			ConstructorScope = 1 << 11,

			UsingInitializerScope = 1 << 12,

			LockScope = 1 << 13,

			/// <summary>
			///   Whether control flow analysis is enabled
			/// </summary>
			DoFlowAnalysis = 1 << 20,

			/// <summary>
			///   Whether control flow analysis is disabled on structs
			///   (only meaningful when DoFlowAnalysis is set)
			/// </summary>
			OmitStructFlowAnalysis = 1 << 21,

			///
			/// Indicates the current context is in probing mode, no errors are reported. 
			///
			ProbingMode = 1 << 22,

			//
			// Return and ContextualReturn statements will set the ReturnType
			// value based on the expression types of each return statement
			// instead of the method return type which is initially null.
			//
			InferReturnType = 1 << 23,

			OmitDebuggingInfo = 1 << 24,

			ExpressionTreeConversion = 1 << 25,

			InvokeSpecialName = 1 << 26
		}

		// utility helper for CheckExpr, UnCheckExpr, Checked and Unchecked statements
		// it's public so that we can use a struct at the callsite
		public struct FlagsHandle : IDisposable
		{
			ResolveContext ec;
			readonly Options invmask, oldval;

			public FlagsHandle (ResolveContext ec, Options flagsToSet)
				: this (ec, flagsToSet, flagsToSet)
			{
			}

			internal FlagsHandle (ResolveContext ec, Options mask, Options val)
			{
				this.ec = ec;
				invmask = ~mask;
				oldval = ec.flags & mask;
				ec.flags = (ec.flags & invmask) | (val & mask);

//				if ((mask & Options.ProbingMode) != 0)
//					ec.Report.DisableReporting ();
			}

			public void Dispose ()
			{
//				if ((invmask & Options.ProbingMode) == 0)
//					ec.Report.EnableReporting ();

				ec.flags = (ec.flags & invmask) | oldval;
			}
		}

		protected Options flags;

		//
		// Whether we are inside an anonymous method.
		//
		public AnonymousExpression CurrentAnonymousMethod;

		//
		// Holds a varible used during collection or object initialization.
		//
		public Expression CurrentInitializerVariable;

		public Block CurrentBlock;

		public readonly IMemberContext MemberContext;

		/// <summary>
		///   If this is non-null, points to the current switch statement
		/// </summary>
		public Switch Switch;

		public ResolveContext (IMemberContext mc)
		{
			if (mc == null)
				throw new ArgumentNullException ();

			MemberContext = mc;

			//
			// The default setting comes from the command line option
			//
			if (mc.Module.Compiler.Settings.Checked)
				flags |= Options.CheckedScope;

			//
			// The constant check state is always set to true
			//
			flags |= Options.ConstantCheckState;
		}

		public ResolveContext (IMemberContext mc, Options options)
			: this (mc)
		{
			flags |= options;
		}

		#region Properties

		public BuiltinTypes BuiltinTypes {
			get {
				return MemberContext.Module.Compiler.BuiltinTypes;
			}
		}

		public virtual ExplicitBlock ConstructorBlock {
			get {
				return CurrentBlock.Explicit;
			}
		}

		public virtual FlowBranching CurrentBranching {
			get { return null; }
		}

		//
		// The current iterator
		//
		public Iterator CurrentIterator {
			get { return CurrentAnonymousMethod as Iterator; }
		}

		public TypeSpec CurrentType {
			get { return MemberContext.CurrentType; }
		}

		public TypeParameter[] CurrentTypeParameters {
			get { return MemberContext.CurrentTypeParameters; }
		}

		public MemberCore CurrentMemberDefinition {
			get { return MemberContext.CurrentMemberDefinition; }
		}

		public bool ConstantCheckState {
			get { return (flags & Options.ConstantCheckState) != 0; }
		}

		public bool DoFlowAnalysis {
			get { return (flags & Options.DoFlowAnalysis) != 0; }
		}

		public bool IsInProbingMode {
			get {
				return (flags & Options.ProbingMode) != 0;
			}
		}

		public bool IsObsolete {
			get {
				// Disables obsolete checks when probing is on
				return MemberContext.IsObsolete;
			}
		}

		public bool IsStatic {
			get {
				return MemberContext.IsStatic;
			}
		}

		public bool IsUnsafe {
			get {
				return HasSet (Options.UnsafeScope) || MemberContext.IsUnsafe;
			}
		}

		public bool IsRuntimeBinder {
			get {
				return Module.Compiler.IsRuntimeBinder;
			}
		}

		public bool IsVariableCapturingRequired {
			get {
				return !IsInProbingMode && (CurrentBranching == null || !CurrentBranching.CurrentUsageVector.IsUnreachable);
			}
		}

		public ModuleContainer Module {
			get {
				return MemberContext.Module;
			}
		}

		public bool OmitStructFlowAnalysis {
			get { return (flags & Options.OmitStructFlowAnalysis) != 0; }
		}

		public Report Report {
			get {
				return Module.Compiler.Report;
			}
		}

		#endregion

		public bool MustCaptureVariable (INamedBlockVariable local)
		{
			if (CurrentAnonymousMethod == null)
				return false;

			//
			// Capture only if this or any of child blocks contain yield
			// or it's a parameter
			//
			if (CurrentAnonymousMethod.IsIterator)
				return local.IsParameter || CurrentBlock.Explicit.HasYield;

			//
			// Capture only if this or any of child blocks contain await
			// or it's a parameter
			//
			if (CurrentAnonymousMethod is AsyncInitializer)
				return CurrentBlock.Explicit.HasAwait;

			return local.Block.ParametersBlock != CurrentBlock.ParametersBlock.Original;
		}

		public bool HasSet (Options options)
		{
			return (this.flags & options) == options;
		}

		public bool HasAny (Options options)
		{
			return (this.flags & options) != 0;
		}


		// Temporarily set all the given flags to the given value.  Should be used in an 'using' statement
		public FlagsHandle Set (Options options)
		{
			return new FlagsHandle (this, options);
		}

		public FlagsHandle With (Options options, bool enable)
		{
			return new FlagsHandle (this, options, enable ? options : 0);
		}

		#region IMemberContext Members

		public string GetSignatureForError ()
		{
			return MemberContext.GetSignatureForError ();
		}

		public ExtensionMethodCandidates LookupExtensionMethod (TypeSpec extensionType, string name, int arity)
		{
			return MemberContext.LookupExtensionMethod (extensionType, name, arity);
		}

		public FullNamedExpression LookupNamespaceOrType (string name, int arity, LookupMode mode, Location loc)
		{
			return MemberContext.LookupNamespaceOrType (name, arity, mode, loc);
		}

		public FullNamedExpression LookupNamespaceAlias (string name)
		{
			return MemberContext.LookupNamespaceAlias (name);
		}

		#endregion
	}

	//
	// This class is used during the Statement.Clone operation
	// to remap objects that have been cloned.
	//
	// Since blocks are cloned by Block.Clone, we need a way for
	// expressions that must reference the block to be cloned
	// pointing to the new cloned block.
	//
	public class CloneContext
	{
		Dictionary<Block, Block> block_map = new Dictionary<Block, Block> ();

		public void AddBlockMap (Block from, Block to)
		{
			block_map.Add (from, to);
		}

		public Block LookupBlock (Block from)
		{
			Block result;
			if (!block_map.TryGetValue (from, out result)) {
				result = (Block) from.Clone (this);
			}

			return result;
		}

		///
		/// Remaps block to cloned copy if one exists.
		///
		public Block RemapBlockCopy (Block from)
		{
			Block mapped_to;
			if (!block_map.TryGetValue (from, out mapped_to))
				return from;

			return mapped_to;
		}
	}

	//
	// Main compiler context
	//
	public class CompilerContext
	{
		static readonly TimeReporter DisabledTimeReporter = new TimeReporter (false);

		readonly Report report;
		readonly BuiltinTypes builtin_types;
		readonly CompilerSettings settings;

		Dictionary<string, SourceFile> all_source_files;

		public CompilerContext (CompilerSettings settings, Report report)
		{
			this.settings = settings;
			this.report = report;
			this.builtin_types = new BuiltinTypes ();
			this.TimeReporter = DisabledTimeReporter;
		}

		#region Properties

		public BuiltinTypes BuiltinTypes {
			get {
				return builtin_types;
			}
		}

		// Used for special handling of runtime dynamic context mostly
		// by error reporting but also by member accessibility checks
		public bool IsRuntimeBinder {
			get; set;
		}

		public Report Report {
			get {
				return report;
			}
		}

		public CompilerSettings Settings {
			get {
				return settings;
			}
		}

		public List<CompilationSourceFile> SourceFiles {
			get {
				return settings.SourceFiles;
			}
		}

		internal TimeReporter TimeReporter {
			get; set;
		}

		#endregion

		//
		// This is used when we encounter a #line preprocessing directive during parsing
		// to register additional source file names
		//
		public SourceFile LookupFile (CompilationSourceFile comp_unit, string name)
		{
			if (all_source_files == null) {
				all_source_files = new Dictionary<string, SourceFile> ();
				foreach (var source in SourceFiles)
					all_source_files[source.FullPathName] = source;
			}

			string path;
			if (!Path.IsPathRooted (name)) {
				string root = Path.GetDirectoryName (comp_unit.FullPathName);
				path = Path.Combine (root, name);
			} else
				path = name;

			SourceFile retval;
			if (all_source_files.TryGetValue (path, out retval))
				return retval;

			retval = Location.AddFile (name, path);
			all_source_files.Add (path, retval);
			return retval;
		}
	}

	//
	// Generic code emitter context
	//
	public class BuilderContext
	{
		[Flags]
		public enum Options
		{
			/// <summary>
			///   This flag tracks the `checked' state of the compilation,
			///   it controls whether we should generate code that does overflow
			///   checking, or if we generate code that ignores overflows.
			///
			///   The default setting comes from the command line option to generate
			///   checked or unchecked code plus any source code changes using the
			///   checked/unchecked statements or expressions.   Contrast this with
			///   the ConstantCheckState flag.
			/// </summary>
			CheckedScope = 1 << 0,

			OmitDebugInfo = 1 << 2,

			ConstructorScope = 1 << 3,

			AsyncBody = 1 << 4
		}

		// utility helper for CheckExpr, UnCheckExpr, Checked and Unchecked statements
		// it's public so that we can use a struct at the callsite
		public struct FlagsHandle : IDisposable
		{
			BuilderContext ec;
			readonly Options invmask, oldval;

			public FlagsHandle (BuilderContext ec, Options flagsToSet)
				: this (ec, flagsToSet, flagsToSet)
			{
			}

			internal FlagsHandle (BuilderContext ec, Options mask, Options val)
			{
				this.ec = ec;
				invmask = ~mask;
				oldval = ec.flags & mask;
				ec.flags = (ec.flags & invmask) | (val & mask);
			}

			public void Dispose ()
			{
				ec.flags = (ec.flags & invmask) | oldval;
			}
		}

		protected Options flags;

		public bool HasSet (Options options)
		{
			return (this.flags & options) == options;
		}

		// Temporarily set all the given flags to the given value.  Should be used in an 'using' statement
		public FlagsHandle With (Options options, bool enable)
		{
			return new FlagsHandle (this, options, enable ? options : 0);
		}
	}
}
