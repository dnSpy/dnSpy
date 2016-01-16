// Copyright (c) 2011 AlphaSierraPapa for the SharpDevelop Team
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using dnlib.DotNet;

namespace ICSharpCode.Decompiler.ILAst {
	public enum ILCode
	{
		Nop,
		Break,
		Ldarg_0,
		Ldarg_1,
		Ldarg_2,
		Ldarg_3,
		Ldloc_0,
		Ldloc_1,
		Ldloc_2,
		Ldloc_3,
		Stloc_0,
		Stloc_1,
		Stloc_2,
		Stloc_3,
		Ldarg_S,
		Ldarga_S,
		Starg_S,
		Ldloc_S,
		Ldloca_S,
		Stloc_S,
		Ldnull,
		Ldc_I4_M1,
		Ldc_I4_0,
		Ldc_I4_1,
		Ldc_I4_2,
		Ldc_I4_3,
		Ldc_I4_4,
		Ldc_I4_5,
		Ldc_I4_6,
		Ldc_I4_7,
		Ldc_I4_8,
		Ldc_I4_S,
		Ldc_I4,
		Ldc_I8,
		Ldc_R4,
		Ldc_R8,
		Dup,
		Pop,
		Jmp,
		Call,
		Calli,
		Ret,
		Br_S,
		Brfalse_S,
		Brtrue_S,
		Beq_S,
		Bge_S,
		Bgt_S,
		Ble_S,
		Blt_S,
		Bne_Un_S,
		Bge_Un_S,
		Bgt_Un_S,
		Ble_Un_S,
		Blt_Un_S,
		Br,
		Brfalse,
		Brtrue,
		Beq,
		Bge,
		Bgt,
		Ble,
		Blt,
		Bne_Un,
		Bge_Un,
		Bgt_Un,
		Ble_Un,
		Blt_Un,
		Switch,
		Ldind_I1,
		Ldind_U1,
		Ldind_I2,
		Ldind_U2,
		Ldind_I4,
		Ldind_U4,
		Ldind_I8,
		Ldind_I,
		Ldind_R4,
		Ldind_R8,
		Ldind_Ref,
		Stind_Ref,
		Stind_I1,
		Stind_I2,
		Stind_I4,
		Stind_I8,
		Stind_R4,
		Stind_R8,
		Add,
		Sub,
		Mul,
		Div,
		Div_Un,
		Rem,
		Rem_Un,
		And,
		Or,
		Xor,
		Shl,
		Shr,
		Shr_Un,
		Neg,
		Not,
		Conv_I1,
		Conv_I2,
		Conv_I4,
		Conv_I8,
		Conv_R4,
		Conv_R8,
		Conv_U4,
		Conv_U8,
		Callvirt,
		Cpobj,
		Ldobj,
		Ldstr,
		Newobj,
		Castclass,
		Isinst,
		Conv_R_Un,
		Unbox,
		Throw,
		Ldfld,
		Ldflda,
		Stfld,
		Ldsfld,
		Ldsflda,
		Stsfld,
		Stobj,
		Conv_Ovf_I1_Un,
		Conv_Ovf_I2_Un,
		Conv_Ovf_I4_Un,
		Conv_Ovf_I8_Un,
		Conv_Ovf_U1_Un,
		Conv_Ovf_U2_Un,
		Conv_Ovf_U4_Un,
		Conv_Ovf_U8_Un,
		Conv_Ovf_I_Un,
		Conv_Ovf_U_Un,
		Box,
		Newarr,
		Ldlen,
		Ldelema,
		Ldelem_I1,
		Ldelem_U1,
		Ldelem_I2,
		Ldelem_U2,
		Ldelem_I4,
		Ldelem_U4,
		Ldelem_I8,
		Ldelem_I,
		Ldelem_R4,
		Ldelem_R8,
		Ldelem_Ref,
		Stelem_I,
		Stelem_I1,
		Stelem_I2,
		Stelem_I4,
		Stelem_I8,
		Stelem_R4,
		Stelem_R8,
		Stelem_Ref,
		Ldelem,
		Stelem,
		Unbox_Any,
		Conv_Ovf_I1,
		Conv_Ovf_U1,
		Conv_Ovf_I2,
		Conv_Ovf_U2,
		Conv_Ovf_I4,
		Conv_Ovf_U4,
		Conv_Ovf_I8,
		Conv_Ovf_U8,
		Refanyval,
		Ckfinite,
		Mkrefany,
		Ldtoken,
		Conv_U2,
		Conv_U1,
		Conv_I,
		Conv_Ovf_I,
		Conv_Ovf_U,
		Add_Ovf,
		Add_Ovf_Un,
		Mul_Ovf,
		Mul_Ovf_Un,
		Sub_Ovf,
		Sub_Ovf_Un,
		Endfinally,
		Leave,
		Leave_S,
		Stind_I,
		Conv_U,
		Arglist,
		Ceq,
		Cgt,
		Cgt_Un,
		Clt,
		Clt_Un,
		Ldftn,
		Ldvirtftn,
		Ldarg,
		Ldarga,
		Starg,
		Ldloc,
		Ldloca,
		Stloc,
		Localloc,
		Endfilter,
		Unaligned,
		Volatile,
		Tailcall,
		Initobj,
		Constrained,
		Cpblk,
		Initblk,
		No,
		Rethrow,
		Sizeof,
		Refanytype,
		Readonly,
		
		// Virtual codes - defined for convenience
		Cne,
		Cge,
		Cge_Un,
		Cle,
		Cle_Un,
		Ldexception,  // Operand holds the CatchType for catch handler, null for filter
		LogicNot,
		LogicAnd,
		LogicOr,
		NullCoalescing,
		InitArray, // Array Initializer

		/// <summary>
		/// Defines a barrier between the parent expression and the argument expression that prevents combining them
		/// </summary>
		Wrap,
		
		// new Class { Prop = 1, Collection = { { 2, 3 }, {4, 5} }}
		// is represented as:
		// InitObject(newobj Class,
		//            CallSetter(Prop, InitializedObject, 1),
		//            InitCollection(CallGetter(Collection, InitializedObject))),
		//                           Call(Add, InitializedObject, 2, 3),
		//                           Call(Add, InitializedObject, 4, 5)))
		InitObject, // Object initializer: first arg is newobj/defaultvalue, remaining args are the initializing statements
		InitCollection, // Collection initializer: first arg is newobj/defaultvalue, remaining args are the initializing statements
		InitializedObject, // Refers the the object being initialized (refers to first arg in parent InitObject or InitCollection instruction)
		
		TernaryOp, // ?:
		LoopOrSwitchBreak,
		LoopContinue,
		Ldc_Decimal,
		YieldBreak,
		YieldReturn,
		/// <summary>
		/// Represents the 'default(T)' instruction.
		/// </summary>
		/// <remarks>Introduced by SimplifyLdObjAndStObj step</remarks>
		DefaultValue,
		/// <summary>
		/// ILExpression with a single child: binary operator.
		/// This expression means that the binary operator will also assign the new value to its left-hand side.
		/// 'CompoundAssignment' must not be used for local variables, as inlining (and other) optimizations don't know that it modifies the variable.
		/// </summary>
		/// <remarks>Introduced by MakeCompoundAssignments step</remarks>
		CompoundAssignment,
		/// <summary>
		/// Represents the post-increment operator.
		/// The first argument is the address of the variable to increment (ldloca instruction).
		/// The second arugment is the amount the variable is incremented by (ldc.i4 instruction)
		/// </summary>
		/// <remarks>Introduced by IntroducePostIncrement step</remarks>
		PostIncrement,
		PostIncrement_Ovf, // checked variant of PostIncrement
		PostIncrement_Ovf_Un, // checked variant of PostIncrement, for unsigned integers
		/// <summary>Calls the getter of a static property (or indexer), or of an instance property on 'base'</summary>
		CallGetter,
		/// <summary>Calls the getter of an instance property (or indexer)</summary>
		CallvirtGetter,
		/// <summary>Calls the setter of a static property (or indexer), or of an instance property on 'base'</summary>
		/// <remarks>This allows us to represent "while ((SomeProperty = val) != null) {}"</remarks>
		CallSetter,
		/// <summary>Calls the setter of a instance property (or indexer)</summary>
		CallvirtSetter,
		/// <summary>Simulates getting the address of the argument instruction.</summary>
		/// <remarks>
		/// Used for postincrement for properties, and to represent the Address() method on multi-dimensional arrays.
		/// Also used when inlining a method call on a value type: "stloc(v, ...); call(M, ldloca(v));" becomes "call(M, AddressOf(...))"
		/// </remarks>
		AddressOf,
		/// <summary>Simulates getting the value of a lifted operator's nullable argument</summary>
		/// <remarks>
		/// For example "stloc(v1, ...); stloc(v2, ...); logicand(ceq(call(Nullable`1::GetValueOrDefault, ldloca(v1)), ldloc(v2)), callgetter(Nullable`1::get_HasValue, ldloca(v1)))" becomes "wrap(ceq(ValueOf(...), ...))"
		/// </remarks>
		ValueOf,
		/// <summary>Simulates creating a new nullable value from a value type argument</summary>
		/// <remarks>
		/// For example "stloc(v1, ...); stloc(v2, ...); ternaryop(callgetter(Nullable`1::get_HasValue, ldloca(v1)), newobj(Nullable`1::.ctor, add(call(Nullable`1::GetValueOrDefault, ldloca(v1)), ldloc(v2))), defaultvalue(Nullable`1))"
		/// becomes "NullableOf(add(valueof(...), ...))"
		/// </remarks>
		NullableOf,
		/// <summary>
		/// Declares parameters that are used in an expression tree.
		/// The last child of this node is the call constructing the expression tree, all other children are the
		/// assignments to the ParameterExpression variables.
		/// </summary>
		ExpressionTreeParameterDeclarations,
		/// <summary>
		/// C# 5 await
		/// </summary>
		Await
	}
	
	public static class ILCodeUtil
	{
		public static string GetName(this ILCode code)
		{
			return code.ToString().ToLowerInvariant().TrimStart('_').Replace('_','.');
		}
		
		public static bool IsConditionalControlFlow(this ILCode code)
		{
			switch(code) {
				case ILCode.Brfalse_S:
				case ILCode.Brtrue_S:
				case ILCode.Beq_S:
				case ILCode.Bge_S:
				case ILCode.Bgt_S:
				case ILCode.Ble_S:
				case ILCode.Blt_S:
				case ILCode.Bne_Un_S:
				case ILCode.Bge_Un_S:
				case ILCode.Bgt_Un_S:
				case ILCode.Ble_Un_S:
				case ILCode.Blt_Un_S:
				case ILCode.Brfalse:
				case ILCode.Brtrue:
				case ILCode.Beq:
				case ILCode.Bge:
				case ILCode.Bgt:
				case ILCode.Ble:
				case ILCode.Blt:
				case ILCode.Bne_Un:
				case ILCode.Bge_Un:
				case ILCode.Bgt_Un:
				case ILCode.Ble_Un:
				case ILCode.Blt_Un:
				case ILCode.Switch:
					return true;
				default:
					return false;
			}
		}
		
		public static bool IsUnconditionalControlFlow(this ILCode code)
		{
			switch(code) {
				case ILCode.Br:
				case ILCode.Br_S:
				case ILCode.Leave:
				case ILCode.Leave_S:
				case ILCode.Ret:
				case ILCode.Endfilter:
				case ILCode.Endfinally:
				case ILCode.Throw:
				case ILCode.Rethrow:
				case ILCode.LoopContinue:
				case ILCode.LoopOrSwitchBreak:
				case ILCode.YieldBreak:
				case ILCode.Jmp:
					return true;
				default:
					return false;
			}
		}
		
		public static void ExpandMacro(ref ILCode code, ref object operand, MethodDef method)
		{
			var methodBody = method.Body;
			switch (code) {
					case ILCode.Ldarg_0:   code = ILCode.Ldarg; operand = method.Parameters[0]; break;
					case ILCode.Ldarg_1:   code = ILCode.Ldarg; operand = method.Parameters[1]; break;
					case ILCode.Ldarg_2:   code = ILCode.Ldarg; operand = method.Parameters[2]; break;
					case ILCode.Ldarg_3:   code = ILCode.Ldarg; operand = method.Parameters[3]; break;
					case ILCode.Ldloc_0:   code = ILCode.Ldloc; operand = methodBody.Variables[0]; break;
					case ILCode.Ldloc_1:   code = ILCode.Ldloc; operand = methodBody.Variables[1]; break;
					case ILCode.Ldloc_2:   code = ILCode.Ldloc; operand = methodBody.Variables[2]; break;
					case ILCode.Ldloc_3:   code = ILCode.Ldloc; operand = methodBody.Variables[3]; break;
					case ILCode.Stloc_0:   code = ILCode.Stloc; operand = methodBody.Variables[0]; break;
					case ILCode.Stloc_1:   code = ILCode.Stloc; operand = methodBody.Variables[1]; break;
					case ILCode.Stloc_2:   code = ILCode.Stloc; operand = methodBody.Variables[2]; break;
					case ILCode.Stloc_3:   code = ILCode.Stloc; operand = methodBody.Variables[3]; break;
					case ILCode.Ldarg_S:   code = ILCode.Ldarg; break;
					case ILCode.Ldarga_S:  code = ILCode.Ldarga; break;
					case ILCode.Starg_S:   code = ILCode.Starg; break;
					case ILCode.Ldloc_S:   code = ILCode.Ldloc; break;
					case ILCode.Ldloca_S:  code = ILCode.Ldloca; break;
					case ILCode.Stloc_S:   code = ILCode.Stloc; break;
					case ILCode.Ldc_I4_M1: code = ILCode.Ldc_I4; operand = -1; break;
					case ILCode.Ldc_I4_0:  code = ILCode.Ldc_I4; operand = 0; break;
					case ILCode.Ldc_I4_1:  code = ILCode.Ldc_I4; operand = 1; break;
					case ILCode.Ldc_I4_2:  code = ILCode.Ldc_I4; operand = 2; break;
					case ILCode.Ldc_I4_3:  code = ILCode.Ldc_I4; operand = 3; break;
					case ILCode.Ldc_I4_4:  code = ILCode.Ldc_I4; operand = 4; break;
					case ILCode.Ldc_I4_5:  code = ILCode.Ldc_I4; operand = 5; break;
					case ILCode.Ldc_I4_6:  code = ILCode.Ldc_I4; operand = 6; break;
					case ILCode.Ldc_I4_7:  code = ILCode.Ldc_I4; operand = 7; break;
					case ILCode.Ldc_I4_8:  code = ILCode.Ldc_I4; operand = 8; break;
					case ILCode.Ldc_I4_S:  code = ILCode.Ldc_I4; operand = (int) (sbyte) operand; break;
					case ILCode.Br_S:      code = ILCode.Br; break;
					case ILCode.Brfalse_S: code = ILCode.Brfalse; break;
					case ILCode.Brtrue_S:  code = ILCode.Brtrue; break;
					case ILCode.Beq_S:     code = ILCode.Beq; break;
					case ILCode.Bge_S:     code = ILCode.Bge; break;
					case ILCode.Bgt_S:     code = ILCode.Bgt; break;
					case ILCode.Ble_S:     code = ILCode.Ble; break;
					case ILCode.Blt_S:     code = ILCode.Blt; break;
					case ILCode.Bne_Un_S:  code = ILCode.Bne_Un; break;
					case ILCode.Bge_Un_S:  code = ILCode.Bge_Un; break;
					case ILCode.Bgt_Un_S:  code = ILCode.Bgt_Un; break;
					case ILCode.Ble_Un_S:  code = ILCode.Ble_Un; break;
					case ILCode.Blt_Un_S:  code = ILCode.Blt_Un; break;
					case ILCode.Leave_S:   code = ILCode.Leave; break;
					case ILCode.Ldind_I:   code = ILCode.Ldobj; operand = method.Module.CorLibTypes.IntPtr.TypeDefOrRef; break;
					case ILCode.Ldind_I1:  code = ILCode.Ldobj; operand = method.Module.CorLibTypes.SByte.TypeDefOrRef; break;
					case ILCode.Ldind_I2:  code = ILCode.Ldobj; operand = method.Module.CorLibTypes.Int16.TypeDefOrRef; break;
					case ILCode.Ldind_I4:  code = ILCode.Ldobj; operand = method.Module.CorLibTypes.Int32.TypeDefOrRef; break;
					case ILCode.Ldind_I8:  code = ILCode.Ldobj; operand = method.Module.CorLibTypes.Int64.TypeDefOrRef; break;
					case ILCode.Ldind_U1:  code = ILCode.Ldobj; operand = method.Module.CorLibTypes.Byte.TypeDefOrRef; break;
					case ILCode.Ldind_U2:  code = ILCode.Ldobj; operand = method.Module.CorLibTypes.UInt16.TypeDefOrRef; break;
					case ILCode.Ldind_U4:  code = ILCode.Ldobj; operand = method.Module.CorLibTypes.UInt32.TypeDefOrRef; break;
					case ILCode.Ldind_R4:  code = ILCode.Ldobj; operand = method.Module.CorLibTypes.Single.TypeDefOrRef; break;
					case ILCode.Ldind_R8:  code = ILCode.Ldobj; operand = method.Module.CorLibTypes.Double.TypeDefOrRef; break;
					case ILCode.Stind_I:   code = ILCode.Stobj; operand = method.Module.CorLibTypes.IntPtr.TypeDefOrRef; break;
					case ILCode.Stind_I1:  code = ILCode.Stobj; operand = method.Module.CorLibTypes.Byte.TypeDefOrRef; break;
					case ILCode.Stind_I2:  code = ILCode.Stobj; operand = method.Module.CorLibTypes.Int16.TypeDefOrRef; break;
					case ILCode.Stind_I4:  code = ILCode.Stobj; operand = method.Module.CorLibTypes.Int32.TypeDefOrRef; break;
					case ILCode.Stind_I8:  code = ILCode.Stobj; operand = method.Module.CorLibTypes.Int64.TypeDefOrRef; break;
					case ILCode.Stind_R4:  code = ILCode.Stobj; operand = method.Module.CorLibTypes.Single.TypeDefOrRef; break;
					case ILCode.Stind_R8:  code = ILCode.Stobj; operand = method.Module.CorLibTypes.Double.TypeDefOrRef; break;
			}
		}
	}
}
