//
// codegen.cs: The code generator
//
// Authors:
//   Miguel de Icaza (miguel@ximian.com)
//   Marek Safar (marek.safar@gmail.com)
//
// Copyright 2001, 2002, 2003 Ximian, Inc.
// Copyright 2004 Novell, Inc.
//

using System;
using System.Collections.Generic;

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
		///   The location where return has to jump to return the
		///   value
		/// </summary>
		public Label ReturnLabel;

		/// <summary>
		///   If we already defined the ReturnLabel
		/// </summary>
		public bool HasReturnLabel;

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

		DynamicSiteClass dynamic_site_container;

		public EmitContext (IMemberContext rc, ILGenerator ig, TypeSpec return_type)
		{
			this.member_context = rc;
			this.ig = ig;

			this.return_type = return_type;

#if STATIC
			ig.__CleverExceptionBlockAssistance ();
#endif
		}

		#region Properties

		public BuiltinTypes BuiltinTypes {
			get {
				return MemberContext.Module.Compiler.BuiltinTypes;
			}
		}

		public TypeSpec CurrentType {
			get { return member_context.CurrentType; }
		}

		public TypeParameter[] CurrentTypeParameters {
			get { return member_context.CurrentTypeParameters; }
		}

		public MemberCore CurrentTypeDefinition {
			get { return member_context.CurrentMemberDefinition; }
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
		#endregion

		/// <summary>
		///   This is called immediately before emitting an IL opcode to tell the symbol
		///   writer to which source line this opcode belongs.
		/// </summary>
		public void Mark (Location loc)
		{
			if (!SymbolWriter.HasSymbolWriter || HasSet (Options.OmitDebugInfo) || loc.IsNull)
				return;

			SymbolWriter.MarkSequencePoint (ig, loc);
		}

		public void DefineLocalVariable (string name, LocalBuilder builder)
		{
			SymbolWriter.DefineLocalVariable (name, builder);
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
			SymbolWriter.OpenScope(ig);
		}

		public void EndExceptionBlock ()
		{
			ig.EndExceptionBlock ();
		}

		public void EndScope ()
		{
			SymbolWriter.CloseScope(ig);
		}

		//
		// Creates a nested container in this context for all dynamic compiler generated stuff
		//
		internal DynamicSiteClass CreateDynamicSite ()
		{
			if (dynamic_site_container == null) {
				var mc = member_context.CurrentMemberDefinition as MemberBase;
				dynamic_site_container = new DynamicSiteClass (CurrentTypeDefinition.Parent.PartialContainer, mc, CurrentTypeParameters);

				CurrentTypeDefinition.Module.AddCompilerGeneratedClass (dynamic_site_container);
				dynamic_site_container.CreateType ();
				dynamic_site_container.DefineType ();
				dynamic_site_container.ResolveTypeParameters ();
				dynamic_site_container.Define ();

				var inflator = new TypeParameterInflator (Module, CurrentType, TypeParameterSpec.EmptyTypes, TypeSpec.EmptyTypes);
				var inflated = dynamic_site_container.CurrentType.InflateMember (inflator);
				CurrentType.MemberCache.AddMember (inflated);
			}

			return dynamic_site_container;
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

		public void Emit (OpCode opcode, int arg)
		{
			ig.Emit (opcode, arg);
		}

		public void Emit (OpCode opcode, byte arg)
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
				Emit (OpCodes.Newarr, ac.Element);
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
				Emit (OpCodes.Ldelema, ac.Element);
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
				Emit (OpCodes.Ldelem_U1);
				return;
			case BuiltinTypeSpec.Type.SByte:
				Emit (OpCodes.Ldelem_I1);
				return;
			case BuiltinTypeSpec.Type.Short:
				Emit (OpCodes.Ldelem_I2);
				return;
			case BuiltinTypeSpec.Type.UShort:
			case BuiltinTypeSpec.Type.Char:
				Emit (OpCodes.Ldelem_U2);
				return;
			case BuiltinTypeSpec.Type.Int:
				Emit (OpCodes.Ldelem_I4);
				return;
			case BuiltinTypeSpec.Type.UInt:
				Emit (OpCodes.Ldelem_U4);
				return;
			case BuiltinTypeSpec.Type.ULong:
			case BuiltinTypeSpec.Type.Long:
				Emit (OpCodes.Ldelem_I8);
				return;
			case BuiltinTypeSpec.Type.Float:
				Emit (OpCodes.Ldelem_R4);
				return;
			case BuiltinTypeSpec.Type.Double:
				Emit (OpCodes.Ldelem_R8);
				return;
			case BuiltinTypeSpec.Type.IntPtr:
				Emit (OpCodes.Ldelem_I);
				return;
			}

			switch (type.Kind) {
			case MemberKind.Struct:
				Emit (OpCodes.Ldelema, type);
				Emit (OpCodes.Ldobj, type);
				break;
			case MemberKind.TypeParameter:
				Emit (OpCodes.Ldelem, type);
				break;
			case MemberKind.PointerType:
				Emit (OpCodes.Ldelem_I);
				break;
			default:
				Emit (OpCodes.Ldelem_Ref);
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
				EmitInt (unchecked ((int) l));
				ig.Emit (OpCodes.Conv_I8);
				return;
			}

			if (l >= 0 && l <= uint.MaxValue) {
				EmitInt (unchecked ((int) l));
				ig.Emit (OpCodes.Conv_U8);
				return;
			}

			ig.Emit (OpCodes.Ldc_I8, l);
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
				return;
			case BuiltinTypeSpec.Type.UInt:
				ig.Emit (OpCodes.Ldind_U4);
				return;
			case BuiltinTypeSpec.Type.Short:
				ig.Emit (OpCodes.Ldind_I2);
				return;
			case BuiltinTypeSpec.Type.UShort:
			case BuiltinTypeSpec.Type.Char:
				ig.Emit (OpCodes.Ldind_U2);
				return;
			case BuiltinTypeSpec.Type.Byte:
				ig.Emit (OpCodes.Ldind_U1);
				return;
			case BuiltinTypeSpec.Type.SByte:
			case BuiltinTypeSpec.Type.Bool:
				ig.Emit (OpCodes.Ldind_I1);
				return;
			case BuiltinTypeSpec.Type.ULong:
			case BuiltinTypeSpec.Type.Long:
				ig.Emit (OpCodes.Ldind_I8);
				return;
			case BuiltinTypeSpec.Type.Float:
				ig.Emit (OpCodes.Ldind_R4);
				return;
			case BuiltinTypeSpec.Type.Double:
				ig.Emit (OpCodes.Ldind_R8);
				return;
			case BuiltinTypeSpec.Type.IntPtr:
				ig.Emit (OpCodes.Ldind_I);
				return;
			}

			switch (type.Kind) {
			case MemberKind.Struct:
			case MemberKind.TypeParameter:
				Emit (OpCodes.Ldobj, type);
				break;
			case MemberKind.PointerType:
				ig.Emit (OpCodes.Ldind_I);
				break;
			default:
				ig.Emit (OpCodes.Ldind_Ref);
				break;
			}
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

			if (type.IsStruct || TypeManager.IsGenericParameter (type))
				Emit (OpCodes.Stobj, type);
			else
				ig.Emit (OpCodes.Stind_Ref);
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
				if (!HasReturnLabel){
					ReturnLabel = DefineLabel ();
					HasReturnLabel = true;
				}
			}

			return return_value;
		}
	}
}
