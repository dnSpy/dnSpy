/*
    Copyright (C) 2014-2019 de4dot@gmail.com

    This file is part of dnSpy

    dnSpy is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    dnSpy is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with dnSpy.  If not, see <http://www.gnu.org/licenses/>.
*/

using System.Diagnostics;
using dnSpy.Contracts.Disassembly;
using Iced.Intel;

namespace dnSpy.Disassembly.Viewer.X86 {
	static class RegisterUtils {
		public static Register ToIcedRegister(this X86Register register) {
			switch (register) {
			case X86Register.None: return Register.None;
			case X86Register.AL: return Register.AL;
			case X86Register.CL: return Register.CL;
			case X86Register.DL: return Register.DL;
			case X86Register.BL: return Register.BL;
			case X86Register.AH: return Register.AH;
			case X86Register.CH: return Register.CH;
			case X86Register.DH: return Register.DH;
			case X86Register.BH: return Register.BH;
			case X86Register.SPL: return Register.SPL;
			case X86Register.BPL: return Register.BPL;
			case X86Register.SIL: return Register.SIL;
			case X86Register.DIL: return Register.DIL;
			case X86Register.R8L: return Register.R8L;
			case X86Register.R9L: return Register.R9L;
			case X86Register.R10L: return Register.R10L;
			case X86Register.R11L: return Register.R11L;
			case X86Register.R12L: return Register.R12L;
			case X86Register.R13L: return Register.R13L;
			case X86Register.R14L: return Register.R14L;
			case X86Register.R15L: return Register.R15L;
			case X86Register.AX: return Register.AX;
			case X86Register.CX: return Register.CX;
			case X86Register.DX: return Register.DX;
			case X86Register.BX: return Register.BX;
			case X86Register.SP: return Register.SP;
			case X86Register.BP: return Register.BP;
			case X86Register.SI: return Register.SI;
			case X86Register.DI: return Register.DI;
			case X86Register.R8W: return Register.R8W;
			case X86Register.R9W: return Register.R9W;
			case X86Register.R10W: return Register.R10W;
			case X86Register.R11W: return Register.R11W;
			case X86Register.R12W: return Register.R12W;
			case X86Register.R13W: return Register.R13W;
			case X86Register.R14W: return Register.R14W;
			case X86Register.R15W: return Register.R15W;
			case X86Register.EAX: return Register.EAX;
			case X86Register.ECX: return Register.ECX;
			case X86Register.EDX: return Register.EDX;
			case X86Register.EBX: return Register.EBX;
			case X86Register.ESP: return Register.ESP;
			case X86Register.EBP: return Register.EBP;
			case X86Register.ESI: return Register.ESI;
			case X86Register.EDI: return Register.EDI;
			case X86Register.R8D: return Register.R8D;
			case X86Register.R9D: return Register.R9D;
			case X86Register.R10D: return Register.R10D;
			case X86Register.R11D: return Register.R11D;
			case X86Register.R12D: return Register.R12D;
			case X86Register.R13D: return Register.R13D;
			case X86Register.R14D: return Register.R14D;
			case X86Register.R15D: return Register.R15D;
			case X86Register.RAX: return Register.RAX;
			case X86Register.RCX: return Register.RCX;
			case X86Register.RDX: return Register.RDX;
			case X86Register.RBX: return Register.RBX;
			case X86Register.RSP: return Register.RSP;
			case X86Register.RBP: return Register.RBP;
			case X86Register.RSI: return Register.RSI;
			case X86Register.RDI: return Register.RDI;
			case X86Register.R8: return Register.R8;
			case X86Register.R9: return Register.R9;
			case X86Register.R10: return Register.R10;
			case X86Register.R11: return Register.R11;
			case X86Register.R12: return Register.R12;
			case X86Register.R13: return Register.R13;
			case X86Register.R14: return Register.R14;
			case X86Register.R15: return Register.R15;
			case X86Register.EIP: return Register.EIP;
			case X86Register.RIP: return Register.RIP;
			case X86Register.ST0: return Register.ST0;
			case X86Register.ST1: return Register.ST1;
			case X86Register.ST2: return Register.ST2;
			case X86Register.ST3: return Register.ST3;
			case X86Register.ST4: return Register.ST4;
			case X86Register.ST5: return Register.ST5;
			case X86Register.ST6: return Register.ST6;
			case X86Register.ST7: return Register.ST7;
			case X86Register.MM0: return Register.MM0;
			case X86Register.MM1: return Register.MM1;
			case X86Register.MM2: return Register.MM2;
			case X86Register.MM3: return Register.MM3;
			case X86Register.MM4: return Register.MM4;
			case X86Register.MM5: return Register.MM5;
			case X86Register.MM6: return Register.MM6;
			case X86Register.MM7: return Register.MM7;
			case X86Register.XMM0: return Register.XMM0;
			case X86Register.XMM1: return Register.XMM1;
			case X86Register.XMM2: return Register.XMM2;
			case X86Register.XMM3: return Register.XMM3;
			case X86Register.XMM4: return Register.XMM4;
			case X86Register.XMM5: return Register.XMM5;
			case X86Register.XMM6: return Register.XMM6;
			case X86Register.XMM7: return Register.XMM7;
			case X86Register.XMM8: return Register.XMM8;
			case X86Register.XMM9: return Register.XMM9;
			case X86Register.XMM10: return Register.XMM10;
			case X86Register.XMM11: return Register.XMM11;
			case X86Register.XMM12: return Register.XMM12;
			case X86Register.XMM13: return Register.XMM13;
			case X86Register.XMM14: return Register.XMM14;
			case X86Register.XMM15: return Register.XMM15;
			case X86Register.XMM16: return Register.XMM16;
			case X86Register.XMM17: return Register.XMM17;
			case X86Register.XMM18: return Register.XMM18;
			case X86Register.XMM19: return Register.XMM19;
			case X86Register.XMM20: return Register.XMM20;
			case X86Register.XMM21: return Register.XMM21;
			case X86Register.XMM22: return Register.XMM22;
			case X86Register.XMM23: return Register.XMM23;
			case X86Register.XMM24: return Register.XMM24;
			case X86Register.XMM25: return Register.XMM25;
			case X86Register.XMM26: return Register.XMM26;
			case X86Register.XMM27: return Register.XMM27;
			case X86Register.XMM28: return Register.XMM28;
			case X86Register.XMM29: return Register.XMM29;
			case X86Register.XMM30: return Register.XMM30;
			case X86Register.XMM31: return Register.XMM31;
			case X86Register.YMM0: return Register.YMM0;
			case X86Register.YMM1: return Register.YMM1;
			case X86Register.YMM2: return Register.YMM2;
			case X86Register.YMM3: return Register.YMM3;
			case X86Register.YMM4: return Register.YMM4;
			case X86Register.YMM5: return Register.YMM5;
			case X86Register.YMM6: return Register.YMM6;
			case X86Register.YMM7: return Register.YMM7;
			case X86Register.YMM8: return Register.YMM8;
			case X86Register.YMM9: return Register.YMM9;
			case X86Register.YMM10: return Register.YMM10;
			case X86Register.YMM11: return Register.YMM11;
			case X86Register.YMM12: return Register.YMM12;
			case X86Register.YMM13: return Register.YMM13;
			case X86Register.YMM14: return Register.YMM14;
			case X86Register.YMM15: return Register.YMM15;
			case X86Register.YMM16: return Register.YMM16;
			case X86Register.YMM17: return Register.YMM17;
			case X86Register.YMM18: return Register.YMM18;
			case X86Register.YMM19: return Register.YMM19;
			case X86Register.YMM20: return Register.YMM20;
			case X86Register.YMM21: return Register.YMM21;
			case X86Register.YMM22: return Register.YMM22;
			case X86Register.YMM23: return Register.YMM23;
			case X86Register.YMM24: return Register.YMM24;
			case X86Register.YMM25: return Register.YMM25;
			case X86Register.YMM26: return Register.YMM26;
			case X86Register.YMM27: return Register.YMM27;
			case X86Register.YMM28: return Register.YMM28;
			case X86Register.YMM29: return Register.YMM29;
			case X86Register.YMM30: return Register.YMM30;
			case X86Register.YMM31: return Register.YMM31;
			case X86Register.ZMM0: return Register.ZMM0;
			case X86Register.ZMM1: return Register.ZMM1;
			case X86Register.ZMM2: return Register.ZMM2;
			case X86Register.ZMM3: return Register.ZMM3;
			case X86Register.ZMM4: return Register.ZMM4;
			case X86Register.ZMM5: return Register.ZMM5;
			case X86Register.ZMM6: return Register.ZMM6;
			case X86Register.ZMM7: return Register.ZMM7;
			case X86Register.ZMM8: return Register.ZMM8;
			case X86Register.ZMM9: return Register.ZMM9;
			case X86Register.ZMM10: return Register.ZMM10;
			case X86Register.ZMM11: return Register.ZMM11;
			case X86Register.ZMM12: return Register.ZMM12;
			case X86Register.ZMM13: return Register.ZMM13;
			case X86Register.ZMM14: return Register.ZMM14;
			case X86Register.ZMM15: return Register.ZMM15;
			case X86Register.ZMM16: return Register.ZMM16;
			case X86Register.ZMM17: return Register.ZMM17;
			case X86Register.ZMM18: return Register.ZMM18;
			case X86Register.ZMM19: return Register.ZMM19;
			case X86Register.ZMM20: return Register.ZMM20;
			case X86Register.ZMM21: return Register.ZMM21;
			case X86Register.ZMM22: return Register.ZMM22;
			case X86Register.ZMM23: return Register.ZMM23;
			case X86Register.ZMM24: return Register.ZMM24;
			case X86Register.ZMM25: return Register.ZMM25;
			case X86Register.ZMM26: return Register.ZMM26;
			case X86Register.ZMM27: return Register.ZMM27;
			case X86Register.ZMM28: return Register.ZMM28;
			case X86Register.ZMM29: return Register.ZMM29;
			case X86Register.ZMM30: return Register.ZMM30;
			case X86Register.ZMM31: return Register.ZMM31;
			default:
				Debug.Fail($"Unknown register: {register}");
				return Register.None;
			}
		}
	}
}
