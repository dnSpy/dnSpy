// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <author name="Daniel Grunwald"/>
//     <version>$Revision: 776 $</version>
// </file>

using System;
using System.Collections.Generic;
using System.Linq;

using Mono.Cecil.Cil;

namespace ICSharpCode.Decompiler
{
	/// <summary>
	/// Additional info about opcodes.
	/// </summary>
	sealed class OpCodeInfo
	{
		static readonly OpCodeInfo[] knownOpCodes = {
			#region Base Instructions
			new OpCodeInfo(OpCodes.Add)        { CanThrow = false },
			new OpCodeInfo(OpCodes.Add_Ovf)    { CanThrow = true  },
			new OpCodeInfo(OpCodes.Add_Ovf_Un) { CanThrow = true  },
			new OpCodeInfo(OpCodes.And)        { CanThrow = false },
			new OpCodeInfo(OpCodes.Arglist)    { CanThrow = false },
			new OpCodeInfo(OpCodes.Beq)        { CanThrow = false },
			new OpCodeInfo(OpCodes.Bge)        { CanThrow = false },
			new OpCodeInfo(OpCodes.Bge_Un)     { CanThrow = false },
			new OpCodeInfo(OpCodes.Bgt)        { CanThrow = false },
			new OpCodeInfo(OpCodes.Bgt_Un)     { CanThrow = false },
			new OpCodeInfo(OpCodes.Ble)        { CanThrow = false },
			new OpCodeInfo(OpCodes.Ble_Un)     { CanThrow = false },
			new OpCodeInfo(OpCodes.Blt)        { CanThrow = false },
			new OpCodeInfo(OpCodes.Blt_Un)     { CanThrow = false },
			new OpCodeInfo(OpCodes.Bne_Un)     { CanThrow = false },
			new OpCodeInfo(OpCodes.Br)         { CanThrow = false },
			new OpCodeInfo(OpCodes.Break)      { CanThrow = true  },
			new OpCodeInfo(OpCodes.Brfalse)    { CanThrow = false },
			new OpCodeInfo(OpCodes.Brtrue)     { CanThrow = false },
			new OpCodeInfo(OpCodes.Call)       { CanThrow = true  },
			new OpCodeInfo(OpCodes.Calli)      { CanThrow = true  },
			new OpCodeInfo(OpCodes.Ceq)        { CanThrow = false },
			new OpCodeInfo(OpCodes.Cgt)        { CanThrow = false },
			new OpCodeInfo(OpCodes.Cgt_Un)     { CanThrow = false },
			new OpCodeInfo(OpCodes.Ckfinite)   { CanThrow = true  },
			new OpCodeInfo(OpCodes.Clt)        { CanThrow = false },
			new OpCodeInfo(OpCodes.Clt_Un)     { CanThrow = false },
			// conv.<to type>
			new OpCodeInfo(OpCodes.Conv_I1)    { CanThrow = false },
			new OpCodeInfo(OpCodes.Conv_I2)    { CanThrow = false },
			new OpCodeInfo(OpCodes.Conv_I4)    { CanThrow = false },
			new OpCodeInfo(OpCodes.Conv_I8)    { CanThrow = false },
			new OpCodeInfo(OpCodes.Conv_R4)    { CanThrow = false },
			new OpCodeInfo(OpCodes.Conv_R8)    { CanThrow = false },
			new OpCodeInfo(OpCodes.Conv_U1)    { CanThrow = false },
			new OpCodeInfo(OpCodes.Conv_U2)    { CanThrow = false },
			new OpCodeInfo(OpCodes.Conv_U4)    { CanThrow = false },
			new OpCodeInfo(OpCodes.Conv_U8)    { CanThrow = false },
			new OpCodeInfo(OpCodes.Conv_I)     { CanThrow = false },
			new OpCodeInfo(OpCodes.Conv_U)     { CanThrow = false },
			new OpCodeInfo(OpCodes.Conv_R_Un)  { CanThrow = false },
			// conv.ovf.<to type>
			new OpCodeInfo(OpCodes.Conv_Ovf_I1) { CanThrow = true},
			new OpCodeInfo(OpCodes.Conv_Ovf_I2) { CanThrow = true},
			new OpCodeInfo(OpCodes.Conv_Ovf_I4) { CanThrow = true},
			new OpCodeInfo(OpCodes.Conv_Ovf_I8) { CanThrow = true},
			new OpCodeInfo(OpCodes.Conv_Ovf_U1) { CanThrow = true},
			new OpCodeInfo(OpCodes.Conv_Ovf_U2) { CanThrow = true},
			new OpCodeInfo(OpCodes.Conv_Ovf_U4) { CanThrow = true},
			new OpCodeInfo(OpCodes.Conv_Ovf_U8) { CanThrow = true},
			new OpCodeInfo(OpCodes.Conv_Ovf_I)  { CanThrow = true},
			new OpCodeInfo(OpCodes.Conv_Ovf_U)  { CanThrow = true},
			// conv.ovf.<to type>.un
			new OpCodeInfo(OpCodes.Conv_Ovf_I1_Un) { CanThrow = true },
			new OpCodeInfo(OpCodes.Conv_Ovf_I2_Un) { CanThrow = true },
			new OpCodeInfo(OpCodes.Conv_Ovf_I4_Un) { CanThrow = true },
			new OpCodeInfo(OpCodes.Conv_Ovf_I8_Un) { CanThrow = true },
			new OpCodeInfo(OpCodes.Conv_Ovf_U1_Un) { CanThrow = true },
			new OpCodeInfo(OpCodes.Conv_Ovf_U2_Un) { CanThrow = true },
			new OpCodeInfo(OpCodes.Conv_Ovf_U4_Un) { CanThrow = true },
			new OpCodeInfo(OpCodes.Conv_Ovf_U8_Un) { CanThrow = true },
			new OpCodeInfo(OpCodes.Conv_Ovf_I_Un)  { CanThrow = true },
			new OpCodeInfo(OpCodes.Conv_Ovf_U_Un)  { CanThrow = true },
			
			//new OpCodeInfo(OpCodes.Cpblk)      { CanThrow = true }, - no idea whether this might cause trouble for the type system, C# shouldn't use it so I'll disable it
			new OpCodeInfo(OpCodes.Div)        { CanThrow = true },
			new OpCodeInfo(OpCodes.Div_Un)     { CanThrow = true },
			new OpCodeInfo(OpCodes.Dup)        { CanThrow = true, IsMoveInstruction = true },
			new OpCodeInfo(OpCodes.Endfilter)  { CanThrow = false },
			new OpCodeInfo(OpCodes.Endfinally) { CanThrow = false },
			//new OpCodeInfo(OpCodes.Initblk)    { CanThrow = true }, - no idea whether this might cause trouble for the type system, C# shouldn't use it so I'll disable it
			//new OpCodeInfo(OpCodes.Jmp)        { CanThrow = true } - We don't support non-local control transfers.
			new OpCodeInfo(OpCodes.Ldarg)  { CanThrow = false, IsMoveInstruction = true },
			new OpCodeInfo(OpCodes.Ldarga) { CanThrow = false },
			new OpCodeInfo(OpCodes.Ldc_I4) { CanThrow = false },
			new OpCodeInfo(OpCodes.Ldc_I8) { CanThrow = false },
			new OpCodeInfo(OpCodes.Ldc_R4) { CanThrow = false },
			new OpCodeInfo(OpCodes.Ldc_R8) { CanThrow = false },
			new OpCodeInfo(OpCodes.Ldftn)  { CanThrow = false },
			// ldind.<type>
			new OpCodeInfo(OpCodes.Ldind_I1)  { CanThrow = true },
			new OpCodeInfo(OpCodes.Ldind_I2)  { CanThrow = true },
			new OpCodeInfo(OpCodes.Ldind_I4)  { CanThrow = true },
			new OpCodeInfo(OpCodes.Ldind_I8)  { CanThrow = true },
			new OpCodeInfo(OpCodes.Ldind_U1)  { CanThrow = true },
			new OpCodeInfo(OpCodes.Ldind_U2)  { CanThrow = true },
			new OpCodeInfo(OpCodes.Ldind_U4)  { CanThrow = true },
			new OpCodeInfo(OpCodes.Ldind_R4)  { CanThrow = true },
			new OpCodeInfo(OpCodes.Ldind_R8)  { CanThrow = true },
			new OpCodeInfo(OpCodes.Ldind_I)   { CanThrow = true },
			new OpCodeInfo(OpCodes.Ldind_Ref) { CanThrow = true },
			// the ldloc exceptions described in the spec can only occur on methods without .localsinit - but csc always sets that flag
			new OpCodeInfo(OpCodes.Ldloc)      { CanThrow = false, IsMoveInstruction = true },
			new OpCodeInfo(OpCodes.Ldloca)     { CanThrow = false },
			new OpCodeInfo(OpCodes.Ldnull)     { CanThrow = false },
			new OpCodeInfo(OpCodes.Leave)      { CanThrow = false },
			new OpCodeInfo(OpCodes.Localloc)   { CanThrow = true  },
			new OpCodeInfo(OpCodes.Mul)        { CanThrow = false },
			new OpCodeInfo(OpCodes.Mul_Ovf)    { CanThrow = true  },
			new OpCodeInfo(OpCodes.Mul_Ovf_Un) { CanThrow = true  },
			new OpCodeInfo(OpCodes.Neg)        { CanThrow = false },
			new OpCodeInfo(OpCodes.Nop)        { CanThrow = false },
			new OpCodeInfo(OpCodes.Not)        { CanThrow = false },
			new OpCodeInfo(OpCodes.Or)         { CanThrow = false },
			new OpCodeInfo(OpCodes.Pop)        { CanThrow = false },
			new OpCodeInfo(OpCodes.Rem)        { CanThrow = true  },
			new OpCodeInfo(OpCodes.Rem_Un)     { CanThrow = true  },
			new OpCodeInfo(OpCodes.Ret)        { CanThrow = false },
			new OpCodeInfo(OpCodes.Shl)        { CanThrow = false },
			new OpCodeInfo(OpCodes.Shr)        { CanThrow = false },
			new OpCodeInfo(OpCodes.Shr_Un)     { CanThrow = false },
			new OpCodeInfo(OpCodes.Starg)      { CanThrow = false, IsMoveInstruction = true },
			new OpCodeInfo(OpCodes.Stind_I1)   { CanThrow = true },
			new OpCodeInfo(OpCodes.Stind_I2)   { CanThrow = true },
			new OpCodeInfo(OpCodes.Stind_I4)   { CanThrow = true },
			new OpCodeInfo(OpCodes.Stind_I8)   { CanThrow = true },
			new OpCodeInfo(OpCodes.Stind_R4)   { CanThrow = true },
			new OpCodeInfo(OpCodes.Stind_R8)   { CanThrow = true },
			new OpCodeInfo(OpCodes.Stind_I)    { CanThrow = true },
			new OpCodeInfo(OpCodes.Stind_Ref)  { CanThrow = true },
			new OpCodeInfo(OpCodes.Stloc)      { CanThrow = false, IsMoveInstruction = true },
			new OpCodeInfo(OpCodes.Sub)        { CanThrow = false },
			new OpCodeInfo(OpCodes.Sub_Ovf)    { CanThrow = true  },
			new OpCodeInfo(OpCodes.Sub_Ovf_Un) { CanThrow = true  },
			new OpCodeInfo(OpCodes.Switch)     { CanThrow = false },
			new OpCodeInfo(OpCodes.Xor)        { CanThrow = false },
			#endregion
			#region Object model instructions
			// CanThrow is true by default - most OO instructions can throw, so we don't specify CanThrow all of the time
			new OpCodeInfo(OpCodes.Box),
			new OpCodeInfo(OpCodes.Callvirt),
			new OpCodeInfo(OpCodes.Castclass), // (output type not Input0 as type arguments may change)
			new OpCodeInfo(OpCodes.Cpobj),
			new OpCodeInfo(OpCodes.Initobj) { CanThrow = false },
			new OpCodeInfo(OpCodes.Isinst)  { CanThrow = false },
			new OpCodeInfo(OpCodes.Ldelem_Any),
			// ldelem.<type>
			new OpCodeInfo(OpCodes.Ldelem_I) ,
			new OpCodeInfo(OpCodes.Ldelem_I1),
			new OpCodeInfo(OpCodes.Ldelem_I2),
			new OpCodeInfo(OpCodes.Ldelem_I4),
			new OpCodeInfo(OpCodes.Ldelem_I8),
			new OpCodeInfo(OpCodes.Ldelem_R4),
			new OpCodeInfo(OpCodes.Ldelem_R8),
			new OpCodeInfo(OpCodes.Ldelem_Ref),
			new OpCodeInfo(OpCodes.Ldelem_U1),
			new OpCodeInfo(OpCodes.Ldelem_U2),
			new OpCodeInfo(OpCodes.Ldelem_U4),
			new OpCodeInfo(OpCodes.Ldelema)  ,
			new OpCodeInfo(OpCodes.Ldfld) ,
			new OpCodeInfo(OpCodes.Ldflda),
			new OpCodeInfo(OpCodes.Ldlen) ,
			new OpCodeInfo(OpCodes.Ldobj) ,
			new OpCodeInfo(OpCodes.Ldsfld),
			new OpCodeInfo(OpCodes.Ldsflda),
			new OpCodeInfo(OpCodes.Ldstr) { CanThrow = false },
			new OpCodeInfo(OpCodes.Ldtoken) { CanThrow = false },
			new OpCodeInfo(OpCodes.Ldvirtftn),
			//new OpCodeInfo(OpCodes.Mkrefany), don't enable this without checking what it is
			new OpCodeInfo(OpCodes.Newarr),
			new OpCodeInfo(OpCodes.Newobj),
			//new OpCodeInfo(OpCodes.Mkrefany), don't enable this without checking what it is
			//new OpCodeInfo(OpCodes.Refanyval), don't enable this without checking what it is
			new OpCodeInfo(OpCodes.Rethrow),
			new OpCodeInfo(OpCodes.Sizeof) { CanThrow = false },
			new OpCodeInfo(OpCodes.Stelem_Any),
			new OpCodeInfo(OpCodes.Stelem_I1),
			new OpCodeInfo(OpCodes.Stelem_I2),
			new OpCodeInfo(OpCodes.Stelem_I4),
			new OpCodeInfo(OpCodes.Stelem_I8),
			new OpCodeInfo(OpCodes.Stelem_R4),
			new OpCodeInfo(OpCodes.Stelem_R8),
			new OpCodeInfo(OpCodes.Stelem_Ref),
			new OpCodeInfo(OpCodes.Stfld),
			new OpCodeInfo(OpCodes.Stobj),
			new OpCodeInfo(OpCodes.Stsfld),
			new OpCodeInfo(OpCodes.Throw),
			new OpCodeInfo(OpCodes.Unbox),
			new OpCodeInfo(OpCodes.Unbox_Any),
			#endregion
		};
		static readonly Dictionary<Code, OpCodeInfo> knownOpCodeDict = knownOpCodes.ToDictionary(info => info.OpCode.Code);
		
		public static OpCodeInfo Get(OpCode opCode)
		{
			return Get(opCode.Code);
		}
		
		public static OpCodeInfo Get(Code code)
		{
			OpCodeInfo info;
			if (knownOpCodeDict.TryGetValue(code, out info))
				return info;
			else
				throw new NotSupportedException(code.ToString());
		}
		
		OpCode opcode;
		
		private OpCodeInfo(OpCode opcode)
		{
			this.opcode = opcode;
			this.CanThrow = true;
		}
		
		public OpCode OpCode { get { return opcode; } }
		
		/// <summary>
		/// 'Move' kind of instructions have one input (may be stack or local variable) and copy that value to all outputs (again stack or local variable).
		/// </summary>
		public bool IsMoveInstruction { get; private set; }
		
		/// <summary>
		/// Specifies whether this opcode is capable of throwing exceptions.
		/// </summary>
		public bool CanThrow { get; private set; }
	}
}
