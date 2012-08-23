//
// codegen.cs: The code generator
//
// Authors:
//   Miguel de Icaza (miguel@ximian.com)
//   Marek Safar (marek.safar@gmail.com)
//
// Copyright 2001, 2002, 2003 Ximian, Inc.
// Copyright 2004 Novell, Inc.
// Copyright 2011 Xamarin Inc
//

using System;
using System.Collections.Generic;
using Mono.CompilerServices.SymbolWriter;

#if STATIC
using MetaType = IKVM.Reflection.Type;
using IKVM.Reflection;
using IKVM.Reflection.Emit;
#else
using MetaType = System.Type;
using System.Reflection;
using System.Reflection.Emit;
#endif

namespace Mono.CSharp
{
	/// <summary>
	///   An Emit Context is created for each body of code (from methods,
	///   properties bodies, indexer bodies or constructor bodies)
	/// </summary>
	public class EmitContext : BuilderContext
	{
		// TODO: Has to be private
		public readonly ILGenerator ig;

		/// <summary>
		///   The value that is allowed to be returned or NULL if there is no
		///   return type.
		/// </summary>
		readonly TypeSpec return_type;

		/// <summary>
		///   Keeps track of the Type to LocalBuilder temporary storage created
		///   to store structures (used to compute the address of the structure
		///   value on structure method invocations)
		/// </summary>
		Dictionary<TypeSpec, object> temporary_storage;

		/// <summary>
		///   The location where we store the return value.
		/// </summary>
		public LocalBuilder return_value;


		/// <summary>
		///   Current loop begin and end labels.
		/// </summary>
		public Label LoopBegin, LoopEnd;

		/// <summary>
		///   Default target in a switch statement.   Only valid if
		///   InSwitch is true
		/// </summary>
		public Label DefaultTarget;

		/// <summary>
		///   If this is non-null, points to the current switch statement
		/// </summary>
		public Switch Switch;

		/// <summary>
		///  Whether we are inside an anonymous method.
		/// </summary>
		public AnonymousExpression CurrentAnonymousMethod;
		
		readonly IMemberContext member_context;

		readonly SourceMethodBuilder methodSymbols;

		DynamicSiteClass dynamic_site_container;

		Label? return_label;

		List<IExpressionCleanup> epilogue_expressions;

		public EmitContext (IMemberContext rc, ILGenerator ig, TypeSpec return_type, SourceMethodBuilder methodSymbols)
		{
			this.member_context = rc;
			this.ig = ig;
			this.return_type = return_type;

			if (rc.Module.Compiler.Settings.Checked)
				flags |= Options.CheckedScope;

			if (methodSymbols != null) {
				this.methodSymbols = methodSymbols;
				if (!rc.Module.Compiler.Settings.Optimize)
					flags |= Options.AccurateDebugInfo;
			} else {
				flags |= Options.OmitDebugInfo;
			}

#if STATIC
			ig.__CleverExceptionBlockAssistance ();
#endif
		}

		#region Properties

		internal AsyncTaskStorey AsyncTaskStorey {
			get {
				return CurrentAnonymousMethod.Storey as AsyncTaskStorey;
			}
		}

		public BuiltinTypes BuiltinTypes {
			get {
				return MemberContext.Module.Compiler.BuiltinTypes;
			}
		}

		public TypeSpec CurrentType {
			get { return member_context.CurrentType; }
		}

		public TypeParameters CurrentTypeParameters {
		    get { return member_context.CurrentTypeParameters; }
		}

		public MemberCore CurrentTypeDefinition {
			get { return member_context.CurrentMemberDefinition; }
		}

		public bool EmitAccurateDebugInfo {
			get {
				return (flags & Options.AccurateDebugInfo) != 0;
			}
		}

		public bool HasMethodSymbolBuilder {
			get {
				return methodSymbols != null;
			}
		}

		public bool HasReturnLabel {
			get {
				return return_label.HasValue;
			}
		}

		public bool IsStatic {
			get { return member_context.IsStatic; }
		}

		public bool IsAnonymousStoreyMutateRequired {
			get {
				return CurrentAnonymousMethod != null &&
					CurrentAnonymousMethod.Storey != null &&
					CurrentAnonymousMethod.Storey.Mutator != null;
			}
		}

		public IMemberContext MemberContext {
			get {
				return member_context;
			}
		}

		public ModuleContainer Module {
			get {
				return member_context.Module;
			}
		}

		// Has to be used for specific emitter errors only any
		// possible resolver errors have to be reported during Resolve
		public Report Report {
			get {
				return member_context.Module.Compiler.Report;
			}
		}

		public TypeSpec ReturnType {
			get {
				return return_type;
			}
		}

		//
		// The label where we have to jump before leaving the context
		//
		public Label ReturnLabel {
			get {
				return return_label.Value;
			}
		}

		public List<IExpressionCleanup> StatementEpilogue {
			get {
				return epilogue_expressions;
			}
		}

		#endregion

		public void AddStatementEpilog (IExpressionCleanup cleanupExpression)
		{
			if (epilogue_expressions == null) {
				epilogue_expressions = new List<IExpressionCleanup> ();
			} else if (epilogue_expressions.Contains (cleanupExpression)) {
				return;
			}

			epilogue_expressions.Add (cleanupExpression);
		}

		public void AssertEmptyStack ()
		{
#if STATIC
			if (ig.__StackHeight != 0)
				throw new InternalErrorException ("Await yields with non-empty stack in `{0}",
					member_context.GetSignatureForError ());
#endif
		}

		/// <summary>
		///   This is called immediately before emitting an IL opcode to tell the symbol
		///   writer to which source line this opcode belongs.
		/// </summary>
		public bool Mark (Location loc)
		{
			if ((flags & Options.OmitDebugInfo) != 0)
				return false;

			if (loc.IsNull || methodSymbols == null)
				return false;

			var sf = loc.SourceFile;
			if (sf.IsHiddenLocation (loc))
				return false;

#if NET_4_0
			methodSymbols.MarkSequencePoint (ig.ILOffset, sf.SourceFileEntry, loc.Row, loc.Column, false);
#endif
			return true;
		}

		public void DefineLocalVariable (string name, LocalBuilder builder)
		{
			if ((flags & Options.OmitDebugInfo) != 0)
				return;

			methodSymbols.AddLocal (builder.LocalIndex, name);
		}

		public void BeginCatchBlock (TypeSpec type)
		{
			ig.BeginCatchBlock (type.GetMetaInfo ());
		}

		public void BeginExceptionBlock ()
		{
			ig.BeginExceptionBlock ();
		}

		public void BeginFinallyBlock ()
		{
			ig.BeginFinallyBlock ();
		}

		public void BeginScope ()
		{
			if ((flags & Options.OmitDebugInfo) != 0)
				return;

#if NET_4_0
			methodSymbols.StartBlock (CodeBlockEntry.Type.Lexical, ig.ILOffset);
#endif
		}

		public void EndExceptionBlock ()
		{
			ig.EndExceptionBlock ();
		}

		public void EndScope ()
		{
			if ((flags & Options.OmitDebugInfo) != 0)
				return;

#if NET_4_0
			methodSymbols.EndBlock (ig.ILOffset);
#endif
		}

		//
		// Creates a nested container in this context for all dynamic compiler generated stuff
		//
		internal DynamicSiteClass CreateDynamicSite ()
		{
			if (dynamic_site_container == null) {
				var mc = member_context.CurrentMemberDefinition as MemberBase;
				dynamic_site_container = new DynamicSiteClass (CurrentTypeDefinition.Parent.PartialContainer, mc, member_context.CurrentTypeParameters);

				CurrentTypeDefinition.Module.AddCompilerGeneratedClass (dynamic_site_container);
				dynamic_site_container.CreateContainer ();
				dynamic_site_container.DefineContainer ();
				dynamic_site_container.Define ();

				var inflator = new TypeParameterInflator (Module, CurrentType, TypeParameterSpec.EmptyTypes, TypeSpec.EmptyTypes);
				var inflated = dynamic_site_container.CurrentType.InflateMember (inflator);
				CurrentType.MemberCache.AddMember (inflated);
			}

			return dynamic_site_container;
		}

		public Label CreateReturnLabel ()
		{
			if (!return_label.HasValue)
				return_label = DefineLabel ();

			return return_label.Value;
		}

		public LocalBuilder DeclareLocal (TypeSpec type, bool pinned)
		{
			if (IsAnonymousStoreyMutateRequired)
				type = CurrentAnonymousMethod.Storey.Mutator.Mutate (type);

			return ig.DeclareLocal (type.GetMetaInfo (), pinned);
		}

		public Label DefineLabel ()
		{
			return ig.DefineLabel ();
		}

		//
		// Creates temporary field in current async storey
		//
		public FieldExpr GetTemporaryField (TypeSpec type)
		{
			var f = AsyncTaskStorey.AddCapturedLocalVariable (type);
			var fexpr = new StackFieldExpr (f);
			fexpr.InstanceExpression = new CompilerGeneratedThis (CurrentType, Location.Null);
			return fexpr;
		}

		public void MarkLabel (Label label)
		{
			ig.MarkLabel (label);
		}

		public void Emit (OpCode opcode)
		{
			ig.Emit (opcode);
		}

		public void Emit (OpCode opcode, LocalBuilder local)
		{
			ig.Emit (opcode, local);
		}

		public void Emit (OpCode opcode, string arg)
		{
			ig.Emit (opcode, arg);
		}

		public void Emit (OpCode opcode, double arg)
		{
			ig.Emit (opcode, arg);
		}

		public void Emit (OpCode opcode, float arg)
		{
			ig.Emit (opcode, arg);
		}

		public void Emit (OpCode opcode, Label label)
		{
			ig.Emit (opcode, label);
		}

		public void Emit (OpCode opcode, Label[] labels)
		{
			ig.Emit (opcode, labels);
		}

		public void Emit (OpCode opcode, TypeSpec type)
		{
			if (IsAnonymousStoreyMutateRequired)
				type = CurrentAnonymousMethod.Storey.Mutator.Mutate (type);

			ig.Emit (opcode, type.GetMetaInfo ());
		}

		public void Emit (OpCode opcode, FieldSpec field)
		{
			if (IsAnonymousStoreyMutateRequired)
				field = field.Mutate (CurrentAnonymousMethod.Storey.Mutator);

			ig.Emit (opcode, field.GetMetaInfo ());
		}

		public void Emit (OpCode opcode, MethodSpec method)
		{
			if (IsAnonymousStoreyMutateRequired)
				method = method.Mutate (CurrentAnonymousMethod.Storey.Mutator);

			if (method.IsConstructor)
				ig.Emit (opcode, (ConstructorInfo) method.GetMetaInfo ());
			else
				ig.Emit (opcode, (MethodInfo) method.GetMetaInfo ());
		}

		// TODO: REMOVE breaks mutator
		public void Emit (OpCode opcode, MethodInfo method)
		{
			ig.Emit (opcode, method);
		}

		public void Emit (OpCode opcode, MethodSpec method, MetaType[] vargs)
		{
			// TODO MemberCache: This should mutate too
			ig.EmitCall (opcode, (MethodInfo) method.GetMetaInfo (), vargs);
		}

		public void EmitArrayNew (ArrayContainer ac)
		{
			if (ac.Rank == 1) {
				var type = IsAnonymousStoreyMutateRequired ?
					CurrentAnonymousMethod.Storey.Mutator.Mutate (ac.Element) :
					ac.Element;

				ig.Emit (OpCodes.Newarr, type.GetMetaInfo ());
			} else {
				if (IsAnonymousStoreyMutateRequired)
					ac = (ArrayContainer) ac.Mutate (CurrentAnonymousMethod.Storey.Mutator);

				ig.Emit (OpCodes.Newobj, ac.GetConstructor ());
			}
		}

		public void EmitArrayAddress (ArrayContainer ac)
		{
			if (ac.Rank > 1) {
				if (IsAnonymousStoreyMutateRequired)
					ac = (ArrayContainer) ac.Mutate (CurrentAnonymousMethod.Storey.Mutator);

				ig.Emit (OpCodes.Call, ac.GetAddressMethod ());
			} else {
				var type = IsAnonymousStoreyMutateRequired ?
					CurrentAnonymousMethod.Storey.Mutator.Mutate (ac.Element) :
					ac.Element;

				ig.Emit (OpCodes.Ldelema, type.GetMetaInfo ());
			}
		}

		//
		// Emits the right opcode to load from an array
		//
		public void EmitArrayLoad (ArrayContainer ac)
		{
			if (ac.Rank > 1) {
				if (IsAnonymousStoreyMutateRequired)
					ac = (ArrayContainer) ac.Mutate (CurrentAnonymousMethod.Storey.Mutator);

				ig.Emit (OpCodes.Call, ac.GetGetMethod ());
				return;
			}


			var type = ac.Element;
			if (type.Kind == MemberKind.Enum)
				type = EnumSpec.GetUnderlyingType (type);

			switch (type.BuiltinType) {
			case BuiltinTypeSpec.Type.Byte:
			case BuiltinTypeSpec.Type.Bool:
				ig.Emit (OpCodes.Ldelem_U1);
				break;
			case BuiltinTypeSpec.Type.SByte:
				ig.Emit (OpCodes.Ldelem_I1);
				break;
			case BuiltinTypeSpec.Type.Short:
				ig.Emit (OpCodes.Ldelem_I2);
				break;
			case BuiltinTypeSpec.Type.UShort:
			case BuiltinTypeSpec.Type.Char:
				ig.Emit (OpCodes.Ldelem_U2);
				break;
			case BuiltinTypeSpec.Type.Int:
				ig.Emit (OpCodes.Ldelem_I4);
				break;
			case BuiltinTypeSpec.Type.UInt:
				ig.Emit (OpCodes.Ldelem_U4);
				break;
			case BuiltinTypeSpec.Type.ULong:
			case BuiltinTypeSpec.Type.Long:
				ig.Emit (OpCodes.Ldelem_I8);
				break;
			case BuiltinTypeSpec.Type.Float:
				ig.Emit (OpCodes.Ldelem_R4);
				break;
			case BuiltinTypeSpec.Type.Double:
				ig.Emit (OpCodes.Ldelem_R8);
				break;
			case BuiltinTypeSpec.Type.IntPtr:
				ig.Emit (OpCodes.Ldelem_I);
				break;
			default:
				switch (type.Kind) {
				case MemberKind.Struct:
					if (IsAnonymousStoreyMutateRequired)
						type = CurrentAnonymousMethod.Storey.Mutator.Mutate (type);

					ig.Emit (OpCodes.Ldelema, type.GetMetaInfo ());
					ig.Emit (OpCodes.Ldobj, type.GetMetaInfo ());
					break;
				case MemberKind.TypeParameter:
					if (IsAnonymousStoreyMutateRequired)
						type = CurrentAnonymousMethod.Storey.Mutator.Mutate (type);

					ig.Emit (OpCodes.Ldelem, type.GetMetaInfo ());
					break;
				case MemberKind.PointerType:
					ig.Emit (OpCodes.Ldelem_I);
					break;
				default:
					ig.Emit (OpCodes.Ldelem_Ref);
					break;
				}
				break;
			}
		}

		//
		// Emits the right opcode to store to an array
		//
		public void EmitArrayStore (ArrayContainer ac)
		{
			if (ac.Rank > 1) {
				if (IsAnonymousStoreyMutateRequired)
					ac = (ArrayContainer) ac.Mutate (CurrentAnonymousMethod.Storey.Mutator);

				ig.Emit (OpCodes.Call, ac.GetSetMethod ());
				return;
			}

			var type = ac.Element;

			if (type.Kind == MemberKind.Enum)
				type = EnumSpec.GetUnderlyingType (type);

			switch (type.BuiltinType) {
			case BuiltinTypeSpec.Type.Byte:
			case BuiltinTypeSpec.Type.SByte:
			case BuiltinTypeSpec.Type.Bool:
				Emit (OpCodes.Stelem_I1);
				return;
			case BuiltinTypeSpec.Type.Short:
			case BuiltinTypeSpec.Type.UShort:
			case BuiltinTypeSpec.Type.Char:
				Emit (OpCodes.Stelem_I2);
				return;
			case BuiltinTypeSpec.Type.Int:
			case BuiltinTypeSpec.Type.UInt:
				Emit (OpCodes.Stelem_I4);
				return;
			case BuiltinTypeSpec.Type.Long:
			case BuiltinTypeSpec.Type.ULong:
				Emit (OpCodes.Stelem_I8);
				return;
			case BuiltinTypeSpec.Type.Float:
				Emit (OpCodes.Stelem_R4);
				return;
			case BuiltinTypeSpec.Type.Double:
				Emit (OpCodes.Stelem_R8);
				return;
			}

			switch (type.Kind) {
			case MemberKind.Struct:
				Emit (OpCodes.Stobj, type);
				break;
			case MemberKind.TypeParameter:
				Emit (OpCodes.Stelem, type);
				break;
			case MemberKind.PointerType:
				Emit (OpCodes.Stelem_I);
				break;
			default:
				Emit (OpCodes.Stelem_Ref);
				break;
			}
		}

		public void EmitInt (int i)
		{
			EmitIntConstant (i);
		}

		void EmitIntConstant (int i)
		{
			switch (i) {
			case -1:
				ig.Emit (OpCodes.Ldc_I4_M1);
				break;

			case 0:
				ig.Emit (OpCodes.Ldc_I4_0);
				break;

			case 1:
				ig.Emit (OpCodes.Ldc_I4_1);
				break;

			case 2:
				ig.Emit (OpCodes.Ldc_I4_2);
				break;

			case 3:
				ig.Emit (OpCodes.Ldc_I4_3);
				break;

			case 4:
				ig.Emit (OpCodes.Ldc_I4_4);
				break;

			case 5:
				ig.Emit (OpCodes.Ldc_I4_5);
				break;

			case 6:
				ig.Emit (OpCodes.Ldc_I4_6);
				break;

			case 7:
				ig.Emit (OpCodes.Ldc_I4_7);
				break;

			case 8:
				ig.Emit (OpCodes.Ldc_I4_8);
				break;

			default:
				if (i >= -128 && i <= 127) {
					ig.Emit (OpCodes.Ldc_I4_S, (sbyte) i);
				} else
					ig.Emit (OpCodes.Ldc_I4, i);
				break;
			}
		}

		public void EmitLong (long l)
		{
			if (l >= int.MinValue && l <= int.MaxValue) {
				EmitIntConstant (unchecked ((int) l));
				ig.Emit (OpCodes.Conv_I8);
			} else if (l >= 0 && l <= uint.MaxValue) {
				EmitIntConstant (unchecked ((int) l));
				ig.Emit (OpCodes.Conv_U8);
			} else {
				ig.Emit (OpCodes.Ldc_I8, l);
			}
		}

		//
		// Load the object from the pointer.  
		//
		public void EmitLoadFromPtr (TypeSpec type)
		{
			if (type.Kind == MemberKind.Enum)
				type = EnumSpec.GetUnderlyingType (type);

			switch (type.BuiltinType) {
			case BuiltinTypeSpec.Type.Int:
				ig.Emit (OpCodes.Ldind_I4);
				break;
			case BuiltinTypeSpec.Type.UInt:
				ig.Emit (OpCodes.Ldind_U4);
				break;
			case BuiltinTypeSpec.Type.Short:
				ig.Emit (OpCodes.Ldind_I2);
				break;
			case BuiltinTypeSpec.Type.UShort:
			case BuiltinTypeSpec.Type.Char:
				ig.Emit (OpCodes.Ldind_U2);
				break;
			case BuiltinTypeSpec.Type.Byte:
				ig.Emit (OpCodes.Ldind_U1);
				break;
			case BuiltinTypeSpec.Type.SByte:
			case BuiltinTypeSpec.Type.Bool:
				ig.Emit (OpCodes.Ldind_I1);
				break;
			case BuiltinTypeSpec.Type.ULong:
			case BuiltinTypeSpec.Type.Long:
				ig.Emit (OpCodes.Ldind_I8);
				break;
			case BuiltinTypeSpec.Type.Float:
				ig.Emit (OpCodes.Ldind_R4);
				break;
			case BuiltinTypeSpec.Type.Double:
				ig.Emit (OpCodes.Ldind_R8);
				break;
			case BuiltinTypeSpec.Type.IntPtr:
				ig.Emit (OpCodes.Ldind_I);
				break;
			default:
				switch (type.Kind) {
				case MemberKind.Struct:
				case MemberKind.TypeParameter:
					if (IsAnonymousStoreyMutateRequired)
						type = CurrentAnonymousMethod.Storey.Mutator.Mutate (type);

					ig.Emit (OpCodes.Ldobj, type.GetMetaInfo ());
					break;
				case MemberKind.PointerType:
					ig.Emit (OpCodes.Ldind_I);
					break;
				default:
					ig.Emit (OpCodes.Ldind_Ref);
					break;
				}
				break;
			}
		}

		public void EmitNull ()
		{
			ig.Emit (OpCodes.Ldnull);
		}

		public void EmitArgumentAddress (int pos)
		{
			if (!IsStatic)
				++pos;

			if (pos > byte.MaxValue)
				ig.Emit (OpCodes.Ldarga, pos);
			else
				ig.Emit (OpCodes.Ldarga_S, (byte) pos);
		}

		public void EmitArgumentLoad (int pos)
		{
			if (!IsStatic)
				++pos;

			switch (pos) {
			case 0: ig.Emit (OpCodes.Ldarg_0); break;
			case 1: ig.Emit (OpCodes.Ldarg_1); break;
			case 2: ig.Emit (OpCodes.Ldarg_2); break;
			case 3: ig.Emit (OpCodes.Ldarg_3); break;
			default:
				if (pos > byte.MaxValue)
					ig.Emit (OpCodes.Ldarg, pos);
				else
					ig.Emit (OpCodes.Ldarg_S, (byte) pos);
				break;
			}
		}

		public void EmitArgumentStore (int pos)
		{
			if (!IsStatic)
				++pos;

			if (pos > byte.MaxValue)
				ig.Emit (OpCodes.Starg, pos);
			else
				ig.Emit (OpCodes.Starg_S, (byte) pos);
		}

		//
		// The stack contains the pointer and the value of type `type'
		//
		public void EmitStoreFromPtr (TypeSpec type)
		{
			if (type.IsEnum)
				type = EnumSpec.GetUnderlyingType (type);

			switch (type.BuiltinType) {
			case BuiltinTypeSpec.Type.Int:
			case BuiltinTypeSpec.Type.UInt:
				ig.Emit (OpCodes.Stind_I4);
				return;
			case BuiltinTypeSpec.Type.Long:
			case BuiltinTypeSpec.Type.ULong:
				ig.Emit (OpCodes.Stind_I8);
				return;
			case BuiltinTypeSpec.Type.Char:
			case BuiltinTypeSpec.Type.Short:
			case BuiltinTypeSpec.Type.UShort:
				ig.Emit (OpCodes.Stind_I2);
				return;
			case BuiltinTypeSpec.Type.Float:
				ig.Emit (OpCodes.Stind_R4);
				return;
			case BuiltinTypeSpec.Type.Double:
				ig.Emit (OpCodes.Stind_R8);
				return;
			case BuiltinTypeSpec.Type.Byte:
			case BuiltinTypeSpec.Type.SByte:
			case BuiltinTypeSpec.Type.Bool:
				ig.Emit (OpCodes.Stind_I1);
				return;
			case BuiltinTypeSpec.Type.IntPtr:
				ig.Emit (OpCodes.Stind_I);
				return;
			}

			switch (type.Kind) {
			case MemberKind.Struct:
			case MemberKind.TypeParameter:
				if (IsAnonymousStoreyMutateRequired)
					type = CurrentAnonymousMethod.Storey.Mutator.Mutate (type);

				ig.Emit (OpCodes.Stobj, type.GetMetaInfo ());
				break;
			default:
				ig.Emit (OpCodes.Stind_Ref);
				break;
			}
		}

		public void EmitThis ()
		{
			ig.Emit (OpCodes.Ldarg_0);
		}

		public void EmitEpilogue ()
		{
			if (epilogue_expressions == null)
				return;

			foreach (var e in epilogue_expressions)
				e.EmitCleanup (this);

			epilogue_expressions = null;
		}

		/// <summary>
		///   Returns a temporary storage for a variable of type t as 
		///   a local variable in the current body.
		/// </summary>
		public LocalBuilder GetTemporaryLocal (TypeSpec t)
		{
			if (temporary_storage != null) {
				object o;
				if (temporary_storage.TryGetValue (t, out o)) {
					if (o is Stack<LocalBuilder>) {
						var s = (Stack<LocalBuilder>) o;
						o = s.Count == 0 ? null : s.Pop ();
					} else {
						temporary_storage.Remove (t);
					}
				}
				if (o != null)
					return (LocalBuilder) o;
			}
			return DeclareLocal (t, false);
		}

		public void FreeTemporaryLocal (LocalBuilder b, TypeSpec t)
		{
			if (temporary_storage == null) {
				temporary_storage = new Dictionary<TypeSpec, object> (ReferenceEquality<TypeSpec>.Default);
				temporary_storage.Add (t, b);
				return;
			}
			object o;
			
			if (!temporary_storage.TryGetValue (t, out o)) {
				temporary_storage.Add (t, b);
				return;
			}
			var s = o as Stack<LocalBuilder>;
			if (s == null) {
				s = new Stack<LocalBuilder> ();
				s.Push ((LocalBuilder)o);
				temporary_storage [t] = s;
			}
			s.Push (b);
		}

		/// <summary>
		///   ReturnValue creates on demand the LocalBuilder for the
		///   return value from the function.  By default this is not
		///   used.  This is only required when returns are found inside
		///   Try or Catch statements.
		///
		///   This method is typically invoked from the Emit phase, so
		///   we allow the creation of a return label if it was not
		///   requested during the resolution phase.   Could be cleaned
		///   up, but it would replicate a lot of logic in the Emit phase
		///   of the code that uses it.
		/// </summary>
		public LocalBuilder TemporaryReturn ()
		{
			if (return_value == null){
				return_value = DeclareLocal (return_type, false);
			}

			return return_value;
		}
	}

	struct CallEmitter
	{
		public Expression InstanceExpression;

		//
		// When set leaves an extra copy of all arguments on the stack
		//
		public bool DuplicateArguments;

		//
		// Does not emit InstanceExpression load when InstanceExpressionOnStack
		// is set. Used by compound assignments.
		//
		public bool InstanceExpressionOnStack;

		//
		// Any of arguments contains await expression
		//
		public bool HasAwaitArguments;

		//
		// When dealing with await arguments the original arguments are converted
		// into a new set with hoisted stack results
		//
		public Arguments EmittedArguments;

		public void Emit (EmitContext ec, MethodSpec method, Arguments Arguments, Location loc)
		{
			// Speed up the check by not doing it on not allowed targets
			if (method.ReturnType.Kind == MemberKind.Void && method.IsConditionallyExcluded (ec.MemberContext, loc))
				return;

			EmitPredefined (ec, method, Arguments, loc);
		}

		public void EmitPredefined (EmitContext ec, MethodSpec method, Arguments Arguments, Location? loc = null)
		{
			Expression instance_copy = null;

			if (!HasAwaitArguments && ec.HasSet (BuilderContext.Options.AsyncBody)) {
				HasAwaitArguments = Arguments != null && Arguments.ContainsEmitWithAwait ();
				if (HasAwaitArguments && InstanceExpressionOnStack) {
					throw new NotSupportedException ();
				}
			}

			OpCode call_op;
			LocalTemporary lt = null;

			if (method.IsStatic) {
				call_op = OpCodes.Call;
			} else {
				if (IsVirtualCallRequired (InstanceExpression, method)) {
					call_op = OpCodes.Callvirt;
				} else {
					call_op = OpCodes.Call;
				}

				if (HasAwaitArguments) {
					instance_copy = InstanceExpression.EmitToField (ec);
					if (Arguments == null)
						EmitCallInstance (ec, instance_copy, method.DeclaringType, call_op);
				} else if (!InstanceExpressionOnStack) {
					var instance_on_stack_type = EmitCallInstance (ec, InstanceExpression, method.DeclaringType, call_op);

					if (DuplicateArguments) {
						ec.Emit (OpCodes.Dup);
						if (Arguments != null && Arguments.Count != 0) {
							lt = new LocalTemporary (instance_on_stack_type);
							lt.Store (ec);
							instance_copy = lt;
						}
					}
				}
			}

			if (Arguments != null && !InstanceExpressionOnStack) {
				EmittedArguments = Arguments.Emit (ec, DuplicateArguments, HasAwaitArguments);
				if (EmittedArguments != null) {
					if (instance_copy != null) {
						EmitCallInstance (ec, instance_copy, method.DeclaringType, call_op);

						if (lt != null)
							lt.Release (ec);
					}

					EmittedArguments.Emit (ec);
				}
			}

			if (call_op == OpCodes.Callvirt && (InstanceExpression.Type.IsGenericParameter || InstanceExpression.Type.IsStruct)) {
				ec.Emit (OpCodes.Constrained, InstanceExpression.Type);
			}

			if (loc != null) {
				//
				// Emit explicit sequence point for expressions like Foo.Bar () to help debugger to
				// break at right place when LHS expression can be stepped-into
				//
				// TODO: The list is probably not comprehensive, need to do more testing
				//
				if (InstanceExpression is PropertyExpr || InstanceExpression is Invocation || InstanceExpression is IndexerExpr ||
					InstanceExpression is New || InstanceExpression is DelegateInvocation)
					ec.Mark (loc.Value);
			}

			//
			// Set instance expression to actual result expression. When it contains await it can be
			// picked up by caller
			//
			InstanceExpression = instance_copy;

			if (method.Parameters.HasArglist) {
				var varargs_types = GetVarargsTypes (method, Arguments);
				ec.Emit (call_op, method, varargs_types);
				return;
			}

			//
			// If you have:
			// this.DoFoo ();
			// and DoFoo is not virtual, you can omit the callvirt,
			// because you don't need the null checking behavior.
			//
			ec.Emit (call_op, method);
		}

		static TypeSpec EmitCallInstance (EmitContext ec, Expression instance, TypeSpec declaringType, OpCode callOpcode)
		{
			var instance_type = instance.Type;

			//
			// Push the instance expression
			//
			if ((instance_type.IsStruct && (callOpcode == OpCodes.Callvirt || (callOpcode == OpCodes.Call && declaringType.IsStruct))) ||
				instance_type.IsGenericParameter || declaringType.IsNullableType) {
				//
				// If the expression implements IMemoryLocation, then
				// we can optimize and use AddressOf on the
				// return.
				//
				// If not we have to use some temporary storage for
				// it.
				var iml = instance as IMemoryLocation;
				if (iml != null) {
					iml.AddressOf (ec, AddressOp.Load);
				} else {
					LocalTemporary temp = new LocalTemporary (instance_type);
					instance.Emit (ec);
					temp.Store (ec);
					temp.AddressOf (ec, AddressOp.Load);
				}

				return ReferenceContainer.MakeType (ec.Module, instance_type);
			}

			if (instance_type.IsEnum || instance_type.IsStruct) {
				instance.Emit (ec);
				ec.Emit (OpCodes.Box, instance_type);
				return ec.BuiltinTypes.Object;
			}

			instance.Emit (ec);
			return instance_type;
		}

		static MetaType[] GetVarargsTypes (MethodSpec method, Arguments arguments)
		{
			AParametersCollection pd = method.Parameters;

			Argument a = arguments[pd.Count - 1];
			Arglist list = (Arglist) a.Expr;

			return list.ArgumentTypes;
		}

		//
		// Used to decide whether call or callvirt is needed
		//
		static bool IsVirtualCallRequired (Expression instance, MethodSpec method)
		{
			//
			// There are 2 scenarious where we emit callvirt
			//
			// Case 1: A method is virtual and it's not used to call base
			// Case 2: A method instance expression can be null. In this casen callvirt ensures
			// correct NRE exception when the method is called
			//
			var decl_type = method.DeclaringType;
			if (decl_type.IsStruct || decl_type.IsEnum)
				return false;

			if (instance is BaseThis)
				return false;

			//
			// It's non-virtual and will never be null and it can be determined
			// whether it's known value or reference type by verifier
			//
			if (!method.IsVirtual && (instance is This || instance is New || instance is ArrayCreation || instance is DelegateCreation) &&
				!instance.Type.IsGenericParameter)
				return false;

			return true;
		}
	}
}
