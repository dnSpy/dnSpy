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

using System;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace ICSharpCode.Decompiler.ILAst
{
	public enum ILCode
	{
		// For convenience, the start is exactly identical to Mono.Cecil.Cil.Code
		// Instructions that should not be used are prepended by __
		Nop,
		Break,
		__Ldarg_0,
		__Ldarg_1,
		__Ldarg_2,
		__Ldarg_3,
		__Ldloc_0,
		__Ldloc_1,
		__Ldloc_2,
		__Ldloc_3,
		__Stloc_0,
		__Stloc_1,
		__Stloc_2,
		__Stloc_3,
		__Ldarg_S,
		__Ldarga_S,
		__Starg_S,
		__Ldloc_S,
		__Ldloca_S,
		__Stloc_S,
		Ldnull,
		__Ldc_I4_M1,
		__Ldc_I4_0,
		__Ldc_I4_1,
		__Ldc_I4_2,
		__Ldc_I4_3,
		__Ldc_I4_4,
		__Ldc_I4_5,
		__Ldc_I4_6,
		__Ldc_I4_7,
		__Ldc_I4_8,
		__Ldc_I4_S,
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
		__Br_S,
		__Brfalse_S,
		__Brtrue_S,
		__Beq_S,
		__Bge_S,
		__Bgt_S,
		__Ble_S,
		__Blt_S,
		__Bne_Un_S,
		__Bge_Un_S,
		__Bgt_Un_S,
		__Ble_Un_S,
		__Blt_Un_S,
		Br,
		__Brfalse,
		Brtrue,
		__Beq,
		__Bge,
		__Bgt,
		__Ble,
		__Blt,
		__Bne_Un,
		__Bge_Un,
		__Bgt_Un,
		__Ble_Un,
		__Blt_Un,
		Switch,
		__Ldind_I1,
		__Ldind_U1,
		__Ldind_I2,
		__Ldind_U2,
		__Ldind_I4,
		__Ldind_U4,
		__Ldind_I8,
		__Ldind_I,
		__Ldind_R4,
		__Ldind_R8,
		Ldind_Ref,
		Stind_Ref,
		__Stind_I1,
		__Stind_I2,
		__Stind_I4,
		__Stind_I8,
		__Stind_R4,
		__Stind_R8,
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
		Ldelem_Any,
		Stelem_Any,
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
		__Leave_S,
		__Stind_I,
		Conv_U,
		Arglist,
		Ceq,
		Cgt,
		Cgt_Un,
		Clt,
		Clt_Un,
		Ldftn,
		Ldvirtftn,
		__Ldarg,
		__Ldarga,
		__Starg,
		Ldloc,
		Ldloca,
		Stloc,
		Localloc,
		Endfilter,
		Unaligned,
		Volatile,
		Tail,
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
				case ILCode.__Brfalse_S:
				case ILCode.__Brtrue_S:
				case ILCode.__Beq_S:
				case ILCode.__Bge_S:
				case ILCode.__Bgt_S:
				case ILCode.__Ble_S:
				case ILCode.__Blt_S:
				case ILCode.__Bne_Un_S:
				case ILCode.__Bge_Un_S:
				case ILCode.__Bgt_Un_S:
				case ILCode.__Ble_Un_S:
				case ILCode.__Blt_Un_S:
				case ILCode.__Brfalse:
				case ILCode.Brtrue:
				case ILCode.__Beq:
				case ILCode.__Bge:
				case ILCode.__Bgt:
				case ILCode.__Ble:
				case ILCode.__Blt:
				case ILCode.__Bne_Un:
				case ILCode.__Bge_Un:
				case ILCode.__Bgt_Un:
				case ILCode.__Ble_Un:
				case ILCode.__Blt_Un:
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
				case ILCode.__Br_S:
				case ILCode.Leave:
				case ILCode.__Leave_S:
				case ILCode.Ret:
				case ILCode.Endfilter:
				case ILCode.Endfinally:
				case ILCode.Throw:
				case ILCode.Rethrow:
				case ILCode.LoopContinue:
				case ILCode.LoopOrSwitchBreak:
				case ILCode.YieldBreak:
					return true;
				default:
					return false;
			}
		}
		
		public static void ExpandMacro(ref ILCode code, ref object operand, MethodBody methodBody)
		{
			switch (code) {
					case ILCode.__Ldarg_0:   code = ILCode.__Ldarg; operand = methodBody.GetParameter(0); break;
					case ILCode.__Ldarg_1:   code = ILCode.__Ldarg; operand = methodBody.GetParameter(1); break;
					case ILCode.__Ldarg_2:   code = ILCode.__Ldarg; operand = methodBody.GetParameter(2); break;
					case ILCode.__Ldarg_3:   code = ILCode.__Ldarg; operand = methodBody.GetParameter(3); break;
					case ILCode.__Ldloc_0:   code = ILCode.Ldloc; operand = methodBody.Variables[0]; break;
					case ILCode.__Ldloc_1:   code = ILCode.Ldloc; operand = methodBody.Variables[1]; break;
					case ILCode.__Ldloc_2:   code = ILCode.Ldloc; operand = methodBody.Variables[2]; break;
					case ILCode.__Ldloc_3:   code = ILCode.Ldloc; operand = methodBody.Variables[3]; break;
					case ILCode.__Stloc_0:   code = ILCode.Stloc; operand = methodBody.Variables[0]; break;
					case ILCode.__Stloc_1:   code = ILCode.Stloc; operand = methodBody.Variables[1]; break;
					case ILCode.__Stloc_2:   code = ILCode.Stloc; operand = methodBody.Variables[2]; break;
					case ILCode.__Stloc_3:   code = ILCode.Stloc; operand = methodBody.Variables[3]; break;
					case ILCode.__Ldarg_S:   code = ILCode.__Ldarg; break;
					case ILCode.__Ldarga_S:  code = ILCode.__Ldarga; break;
					case ILCode.__Starg_S:   code = ILCode.__Starg; break;
					case ILCode.__Ldloc_S:   code = ILCode.Ldloc; break;
					case ILCode.__Ldloca_S:  code = ILCode.Ldloca; break;
					case ILCode.__Stloc_S:   code = ILCode.Stloc; break;
					case ILCode.__Ldc_I4_M1: code = ILCode.Ldc_I4; operand = -1; break;
					case ILCode.__Ldc_I4_0:  code = ILCode.Ldc_I4; operand = 0; break;
					case ILCode.__Ldc_I4_1:  code = ILCode.Ldc_I4; operand = 1; break;
					case ILCode.__Ldc_I4_2:  code = ILCode.Ldc_I4; operand = 2; break;
					case ILCode.__Ldc_I4_3:  code = ILCode.Ldc_I4; operand = 3; break;
					case ILCode.__Ldc_I4_4:  code = ILCode.Ldc_I4; operand = 4; break;
					case ILCode.__Ldc_I4_5:  code = ILCode.Ldc_I4; operand = 5; break;
					case ILCode.__Ldc_I4_6:  code = ILCode.Ldc_I4; operand = 6; break;
					case ILCode.__Ldc_I4_7:  code = ILCode.Ldc_I4; operand = 7; break;
					case ILCode.__Ldc_I4_8:  code = ILCode.Ldc_I4; operand = 8; break;
					case ILCode.__Ldc_I4_S:  code = ILCode.Ldc_I4; operand = (int) (sbyte) operand; break;
					case ILCode.__Br_S:      code = ILCode.Br; break;
					case ILCode.__Brfalse_S: code = ILCode.__Brfalse; break;
					case ILCode.__Brtrue_S:  code = ILCode.Brtrue; break;
					case ILCode.__Beq_S:     code = ILCode.__Beq; break;
					case ILCode.__Bge_S:     code = ILCode.__Bge; break;
					case ILCode.__Bgt_S:     code = ILCode.__Bgt; break;
					case ILCode.__Ble_S:     code = ILCode.__Ble; break;
					case ILCode.__Blt_S:     code = ILCode.__Blt; break;
					case ILCode.__Bne_Un_S:  code = ILCode.__Bne_Un; break;
					case ILCode.__Bge_Un_S:  code = ILCode.__Bge_Un; break;
					case ILCode.__Bgt_Un_S:  code = ILCode.__Bgt_Un; break;
					case ILCode.__Ble_Un_S:  code = ILCode.__Ble_Un; break;
					case ILCode.__Blt_Un_S:  code = ILCode.__Blt_Un; break;
					case ILCode.__Leave_S:   code = ILCode.Leave; break;
					case ILCode.__Ldind_I:   code = ILCode.Ldobj; operand = methodBody.Method.Module.TypeSystem.IntPtr; break;
					case ILCode.__Ldind_I1:  code = ILCode.Ldobj; operand = methodBody.Method.Module.TypeSystem.SByte; break;
					case ILCode.__Ldind_I2:  code = ILCode.Ldobj; operand = methodBody.Method.Module.TypeSystem.Int16; break;
					case ILCode.__Ldind_I4:  code = ILCode.Ldobj; operand = methodBody.Method.Module.TypeSystem.Int32; break;
					case ILCode.__Ldind_I8:  code = ILCode.Ldobj; operand = methodBody.Method.Module.TypeSystem.Int64; break;
					case ILCode.__Ldind_U1:  code = ILCode.Ldobj; operand = methodBody.Method.Module.TypeSystem.Byte; break;
					case ILCode.__Ldind_U2:  code = ILCode.Ldobj; operand = methodBody.Method.Module.TypeSystem.UInt16; break;
					case ILCode.__Ldind_U4:  code = ILCode.Ldobj; operand = methodBody.Method.Module.TypeSystem.UInt32; break;
					case ILCode.__Ldind_R4:  code = ILCode.Ldobj; operand = methodBody.Method.Module.TypeSystem.Single; break;
					case ILCode.__Ldind_R8:  code = ILCode.Ldobj; operand = methodBody.Method.Module.TypeSystem.Double; break;
					case ILCode.__Stind_I:   code = ILCode.Stobj; operand = methodBody.Method.Module.TypeSystem.IntPtr; break;
					case ILCode.__Stind_I1:  code = ILCode.Stobj; operand = methodBody.Method.Module.TypeSystem.Byte; break;
					case ILCode.__Stind_I2:  code = ILCode.Stobj; operand = methodBody.Method.Module.TypeSystem.Int16; break;
					case ILCode.__Stind_I4:  code = ILCode.Stobj; operand = methodBody.Method.Module.TypeSystem.Int32; break;
					case ILCode.__Stind_I8:  code = ILCode.Stobj; operand = methodBody.Method.Module.TypeSystem.Int64; break;
					case ILCode.__Stind_R4:  code = ILCode.Stobj; operand = methodBody.Method.Module.TypeSystem.Single; break;
					case ILCode.__Stind_R8:  code = ILCode.Stobj; operand = methodBody.Method.Module.TypeSystem.Double; break;
			}
		}
		
		public static ParameterDefinition GetParameter (this MethodBody self, int index)
		{
			var method = self.Method;

			if (method.HasThis) {
				if (index == 0)
					return self.ThisParameter;

				index--;
			}

			var parameters = method.Parameters;

			if (index < 0 || index >= parameters.Count)
				return null;

			return parameters [index];
		}
	}
}
