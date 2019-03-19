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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using dndbg.COM.CorDebug;
using dndbg.Engine;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.CallStack;
using dnSpy.Contracts.Debugger.DotNet.Disassembly;
using dnSpy.Contracts.Disassembly;
using dnSpy.Debugger.DotNet.CorDebug.CallStack;
using dnSpy.Debugger.DotNet.Metadata;

namespace dnSpy.Debugger.DotNet.CorDebug.Impl.Evaluation {
	partial class DbgCorDebugInternalRuntimeImpl {
		public bool TryGetNativeCode(DbgStackFrame frame, out DbgDotNetNativeCode nativeCode) {
			if (!ILDbgEngineStackFrame.TryGetEngineStackFrame(frame, out var ilFrame)) {
				nativeCode = default;
				return false;
			}
			if (Dispatcher.CheckAccess())
				return TryGetNativeCodeCore(ilFrame, out nativeCode);
			return TryGetNativeCode2(ilFrame, out nativeCode);

			bool TryGetNativeCode2(ILDbgEngineStackFrame ilFrame2, out DbgDotNetNativeCode nativeCode2) {
				DbgDotNetNativeCode nativeCodeTmp = default;
				Dispatcher.TryInvokeRethrow(() => TryGetNativeCodeCore(ilFrame2, out nativeCodeTmp), out var res);
				nativeCode2 = nativeCodeTmp;
				return res;
			}
		}
		bool TryGetNativeCodeCore(ILDbgEngineStackFrame ilFrame, out DbgDotNetNativeCode nativeCode) {
			Dispatcher.VerifyAccess();
			if (!engine.IsPaused) {
				nativeCode = default;
				return false;
			}
			var code = ilFrame.CorFrame.Code;
			ilFrame.GetFrameMethodInfo(out var module, out var methodMetadataToken, out var genericTypeArguments, out var genericMethodArguments);
			var reflectionMethod = TryGetMethod(module, methodMetadataToken, genericTypeArguments, genericMethodArguments);
			return TryGetNativeCodeCore(code, reflectionMethod, out nativeCode);
		}

		public bool TryGetNativeCode(DmdMethodBase method, out DbgDotNetNativeCode nativeCode) {
			if (Dispatcher.CheckAccess())
				return TryGetNativeCodeCore(method, out nativeCode);
			return TryGetNativeCode2(method, out nativeCode);

			bool TryGetNativeCode2(DmdMethodBase method2, out DbgDotNetNativeCode nativeCode2) {
				DbgDotNetNativeCode nativeCodeTmp = default;
				Dispatcher.TryInvokeRethrow(() => TryGetNativeCodeCore(method2, out nativeCodeTmp), out var res);
				nativeCode2 = nativeCodeTmp;
				return res;
			}
		}
		bool TryGetNativeCodeCore(DmdMethodBase method, out DbgDotNetNativeCode nativeCode) {
			Dispatcher.VerifyAccess();
			nativeCode = default;
			if (!engine.IsPaused)
				return false;

			var dbgModule = method.Module.GetDebuggerModule();
			if (dbgModule == null)
				return false;
			if (!engine.TryGetDnModule(dbgModule, out var dnModule))
				return false;
			var func = dnModule.CorModule.GetFunctionFromToken((uint)method.MetadataToken);
			if (func == null)
				return false;
			var code = func.NativeCode;
			if (code == null)
				return false;
			return TryGetNativeCodeCore(code, method, out nativeCode);
		}

		bool TryGetNativeCodeCore(CorCode code, DmdMethodBase reflectionMethod, out DbgDotNetNativeCode nativeCode) {
			nativeCode = default;
			if (code == null)
				return false;

			var process = code.Function?.Module?.Process;
			if (process == null)
				return false;

			// The returned chunks are sorted
			var chunks = code.GetCodeChunks();
			if (chunks.Length == 0)
				return false;

			int totalLen = 0;
			foreach (var chunk in chunks)
				totalLen += (int)chunk.Length;
			var allCodeBytes = new byte[totalLen];
			int currentPos = 0;
			foreach (var chunk in chunks) {
				int hr = process.ReadMemory(chunk.StartAddr, allCodeBytes, currentPos, (int)chunk.Length, out int sizeRead);
				if (hr < 0 || sizeRead != (int)chunk.Length)
					return false;
				currentPos += (int)chunk.Length;
			}
			Debug.Assert(currentPos == totalLen);

			// We must get IL to native mappings before we get var homes, or the var
			// homes array will be empty.
			var map = code.GetILToNativeMapping();
			var varHomes = code.GetVariables();
			Array.Sort(varHomes, (a, b) => {
				int c = a.StartOffset.CompareTo(b.StartOffset);
				if (c != 0)
					return c;
				return a.Length.CompareTo(b.Length);
			});
			for (int i = 0, chunkIndex = 0, chunkOffset = 0; i < varHomes.Length; i++) {
				var startOffset = varHomes[i].StartOffset;
				while (chunkIndex < chunks.Length) {
					if (startOffset < (uint)chunkOffset + chunks[chunkIndex].Length)
						break;
					chunkOffset += (int)chunks[chunkIndex].Length;
					chunkIndex++;
				}
				Debug.Assert(chunkIndex < chunks.Length);
				if (chunkIndex >= chunks.Length) {
					varHomes = Array.Empty<VariableHome>();
					break;
				}
				varHomes[i].StartOffset += chunks[chunkIndex].StartAddr - (uint)chunkOffset;
			}
			Array.Sort(varHomes, (a, b) => {
				int c = a.SlotIndex.CompareTo(b.SlotIndex);
				if (c != 0)
					return c;
				c = a.ArgumentIndex.CompareTo(b.ArgumentIndex);
				if (c != 0)
					return c;
				c = a.StartOffset.CompareTo(b.StartOffset);
				if (c != 0)
					return c;
				return a.Length.CompareTo(b.Length);
			});

			Array.Sort(map, (a, b) => {
				int c = a.nativeStartOffset.CompareTo(b.nativeStartOffset);
				if (c != 0)
					return c;
				return a.nativeEndOffset.CompareTo(b.nativeEndOffset);
			});
			map = AddMissingMapEntries(map, (uint)totalLen);
			totalLen = 0;
			for (int i = 0; i < chunks.Length; i++) {
				chunks[i].StartAddr -= (uint)totalLen;
				totalLen += (int)chunks[i].Length;
			}
			var blocks = new DbgDotNetNativeCodeBlock[map.Length];
			ulong baseAddress = chunks[0].StartAddr;
			uint chunkByteOffset = 0;
			for (int i = 0, chunkIndex = 0; i < blocks.Length; i++) {
				var info = map[i];
				bool b = info.nativeEndOffset <= (uint)allCodeBytes.Length && info.nativeStartOffset <= info.nativeEndOffset && chunkIndex < chunks.Length;
				Debug.Assert(b);
				if (!b)
					return false;
				int codeLen = (int)(info.nativeEndOffset - info.nativeStartOffset);
				var rawCode = new ArraySegment<byte>(allCodeBytes, (int)info.nativeStartOffset, codeLen);
				ulong address = baseAddress + info.nativeStartOffset;
				if ((CorDebugIlToNativeMappingTypes)info.ilOffset == CorDebugIlToNativeMappingTypes.NO_MAPPING)
					blocks[i] = new DbgDotNetNativeCodeBlock(NativeCodeBlockKind.Unknown, address, rawCode, -1);
				else if ((CorDebugIlToNativeMappingTypes)info.ilOffset == CorDebugIlToNativeMappingTypes.PROLOG)
					blocks[i] = new DbgDotNetNativeCodeBlock(NativeCodeBlockKind.Prolog, address, rawCode, -1);
				else if ((CorDebugIlToNativeMappingTypes)info.ilOffset == CorDebugIlToNativeMappingTypes.EPILOG)
					blocks[i] = new DbgDotNetNativeCodeBlock(NativeCodeBlockKind.Epilog, address, rawCode, -1);
				else
					blocks[i] = new DbgDotNetNativeCodeBlock(NativeCodeBlockKind.Code, address, rawCode, (int)info.ilOffset);

				chunkByteOffset += (uint)codeLen;
				for (;;) {
					if (chunkIndex >= chunks.Length) {
						if (i + 1 == blocks.Length)
							break;
						Debug.Assert(false);
						return false;
					}
					if (chunkByteOffset < chunks[chunkIndex].Length)
						break;
					chunkByteOffset -= chunks[chunkIndex].Length;
					chunkIndex++;
					if (chunkIndex < chunks.Length)
						baseAddress = chunks[chunkIndex].StartAddr;
				}
			}

			NativeCodeOptimization optimization;
			switch (code.CompilerFlags) {
			case CorDebugJITCompilerFlags.CORDEBUG_JIT_DEFAULT:
				optimization = NativeCodeOptimization.Optimized;
				break;

			case CorDebugJITCompilerFlags.CORDEBUG_JIT_DISABLE_OPTIMIZATION:
			case CorDebugJITCompilerFlags.CORDEBUG_JIT_ENABLE_ENC:
				optimization = NativeCodeOptimization.Unoptimized;
				break;

			default:
				Debug.Fail($"Unknown optimization: {code.CompilerFlags}");
				optimization = NativeCodeOptimization.Unknown;
				break;
			}

			NativeCodeInfo codeInfo = null;
			NativeCodeKind codeKind;
			switch (Runtime.Process.Architecture) {
			case DbgArchitecture.X64:
			case DbgArchitecture.X86:
				codeKind = Runtime.Process.Architecture == DbgArchitecture.X86 ? NativeCodeKind.X86_32 : NativeCodeKind.X86_64;
				var x86Variables = CreateVariablesX86(varHomes) ?? Array.Empty<X86Variable>();
				if (x86Variables.Length != 0)
					codeInfo = new X86NativeCodeInfo(x86Variables);
				break;

			case DbgArchitecture.Arm:
				codeKind = NativeCodeKind.Arm;
				Debug.Fail("Create variables like x86/x64 code above");
				break;

			case DbgArchitecture.Arm64:
				codeKind = NativeCodeKind.Arm64;
				Debug.Fail("Create variables like x86/x64 code above");
				break;

			default:
				Debug.Fail($"Unsupported architecture: {Runtime.Process.Architecture}");
				return false;
			}

			var methodName = reflectionMethod == null ? null : reflectionMethod.ToString() + " (0x" + reflectionMethod.MetadataToken.ToString("X8") + ")";
			nativeCode = new DbgDotNetNativeCode(codeKind, optimization, blocks, codeInfo, methodName, reflectionMethod?.Module.FullyQualifiedName);
			return true;
		}

		static ILToNativeMap[] AddMissingMapEntries(ILToNativeMap[] map, uint methodLength) {
			if (IsAllCodeBytes(map, methodLength))
				return map;
			var list = new List<ILToNativeMap>(map.Length + 4);
			uint offs = 0;
			for (int i = 0; i < map.Length; i++) {
				var m = map[i];
				if (offs < m.nativeStartOffset) {
					list.Add(new ILToNativeMap {
						ilOffset = unchecked((uint)CorDebugIlToNativeMappingTypes.NO_MAPPING),
						nativeStartOffset = offs,
						nativeEndOffset = m.nativeStartOffset,
					});
				}
				list.Add(m);
				offs = m.nativeEndOffset;
			}
			if (offs < methodLength) {
				list.Add(new ILToNativeMap {
					ilOffset = unchecked((uint)CorDebugIlToNativeMappingTypes.NO_MAPPING),
					nativeStartOffset = offs,
					nativeEndOffset = methodLength,
				});
			}
			var result = list.ToArray();
			Debug.Assert(IsAllCodeBytes(result, methodLength));
			return result;
		}

		static bool IsAllCodeBytes(ILToNativeMap[] map, uint methodLength) {
			if (map.Length == 0)
				return methodLength == 0;
			for (int i = 1; i < map.Length; i++) {
				if (map[i - 1].nativeEndOffset != map[i].nativeStartOffset)
					return false;
			}
			return map[map.Length - 1].nativeEndOffset == methodLength;
		}

		X86Variable[] CreateVariablesX86(VariableHome[] varHomes) {
			var x86Variables = varHomes.Length == 0 ? Array.Empty<X86Variable>() : new X86Variable[varHomes.Length];
			var architecture = Runtime.Process.Architecture;
			for (int i = 0; i < varHomes.Length; i++) {
				var varHome = varHomes[i];
				bool isLocal;
				int varIndex;
				if (varHome.SlotIndex >= 0) {
					isLocal = true;
					varIndex = varHome.SlotIndex;
				}
				else if (varHome.ArgumentIndex >= 0) {
					isLocal = false;
					varIndex = varHome.ArgumentIndex;
				}
				else
					return null;

				X86VariableLocationKind locationKind;
				X86Register register;
				int memoryOffset;
				switch (varHome.LocationType) {
				case VariableLocationType.VLT_REGISTER:
					locationKind = X86VariableLocationKind.Register;
					// Ignore errors, use register None
					TryGetRegisterX86(architecture, varHome.Register, out register);
					memoryOffset = 0;
					break;

				case VariableLocationType.VLT_REGISTER_RELATIVE:
					locationKind = X86VariableLocationKind.Memory;
					// Ignore errors, the register is very rarely invalid (RyuJIT bug)
					TryGetRegisterX86(architecture, varHome.Register, out register);
					memoryOffset = varHome.Offset;
					break;

				case VariableLocationType.VLT_INVALID:
					// eg. local is a ulong stored on the stack and it's 32-bit code
					locationKind = X86VariableLocationKind.Other;
					register = X86Register.None;
					memoryOffset = 0;
					break;

				default:
					return null;
				}

				const string varName = null;
				x86Variables[i] = new X86Variable(varName, varIndex, isLocal, varHome.StartOffset, varHome.Length, locationKind, register, memoryOffset);
			}
			return x86Variables;
		}

		static bool TryGetRegisterX86(DbgArchitecture architecture, CorDebugRegister corReg, out X86Register register) {
			switch (architecture) {
			case DbgArchitecture.X86:
				switch (corReg) {
				case CorDebugRegister.REGISTER_X86_EIP:
					register = X86Register.EIP;
					return true;
				case CorDebugRegister.REGISTER_X86_ESP:
					register = X86Register.ESP;
					return true;
				case CorDebugRegister.REGISTER_X86_EBP:
					register = X86Register.EBP;
					return true;
				case CorDebugRegister.REGISTER_X86_EAX:
					register = X86Register.EAX;
					return true;
				case CorDebugRegister.REGISTER_X86_ECX:
					register = X86Register.ECX;
					return true;
				case CorDebugRegister.REGISTER_X86_EDX:
					register = X86Register.EDX;
					return true;
				case CorDebugRegister.REGISTER_X86_EBX:
					register = X86Register.EBX;
					return true;
				case CorDebugRegister.REGISTER_X86_ESI:
					register = X86Register.ESI;
					return true;
				case CorDebugRegister.REGISTER_X86_EDI:
					register = X86Register.EDI;
					return true;
				case CorDebugRegister.REGISTER_X86_FPSTACK_0:
					register = X86Register.ST0;
					return true;
				case CorDebugRegister.REGISTER_X86_FPSTACK_1:
					register = X86Register.ST1;
					return true;
				case CorDebugRegister.REGISTER_X86_FPSTACK_2:
					register = X86Register.ST2;
					return true;
				case CorDebugRegister.REGISTER_X86_FPSTACK_3:
					register = X86Register.ST3;
					return true;
				case CorDebugRegister.REGISTER_X86_FPSTACK_4:
					register = X86Register.ST4;
					return true;
				case CorDebugRegister.REGISTER_X86_FPSTACK_5:
					register = X86Register.ST5;
					return true;
				case CorDebugRegister.REGISTER_X86_FPSTACK_6:
					register = X86Register.ST6;
					return true;
				case CorDebugRegister.REGISTER_X86_FPSTACK_7:
					register = X86Register.ST7;
					return true;
				default:
					Debug.Fail($"Unknown register number {(int)corReg}");
					register = X86Register.None;
					return false;
				}

			case DbgArchitecture.X64:
				switch (corReg) {
				case CorDebugRegister.REGISTER_AMD64_RIP:
					register = X86Register.RIP;
					return true;
				case CorDebugRegister.REGISTER_AMD64_RSP:
					register = X86Register.RSP;
					return true;
				case CorDebugRegister.REGISTER_AMD64_RBP:
					register = X86Register.RBP;
					return true;
				case CorDebugRegister.REGISTER_AMD64_RAX:
					register = X86Register.RAX;
					return true;
				case CorDebugRegister.REGISTER_AMD64_RCX:
					register = X86Register.RCX;
					return true;
				case CorDebugRegister.REGISTER_AMD64_RDX:
					register = X86Register.RDX;
					return true;
				case CorDebugRegister.REGISTER_AMD64_RBX:
					register = X86Register.RBX;
					return true;
				case CorDebugRegister.REGISTER_AMD64_RSI:
					register = X86Register.RSI;
					return true;
				case CorDebugRegister.REGISTER_AMD64_RDI:
					register = X86Register.RDI;
					return true;
				case CorDebugRegister.REGISTER_AMD64_R8:
					register = X86Register.R8;
					return true;
				case CorDebugRegister.REGISTER_AMD64_R9:
					register = X86Register.R9;
					return true;
				case CorDebugRegister.REGISTER_AMD64_R10:
					register = X86Register.R10;
					return true;
				case CorDebugRegister.REGISTER_AMD64_R11:
					register = X86Register.R11;
					return true;
				case CorDebugRegister.REGISTER_AMD64_R12:
					register = X86Register.R12;
					return true;
				case CorDebugRegister.REGISTER_AMD64_R13:
					register = X86Register.R13;
					return true;
				case CorDebugRegister.REGISTER_AMD64_R14:
					register = X86Register.R14;
					return true;
				case CorDebugRegister.REGISTER_AMD64_R15:
					register = X86Register.R15;
					return true;
				case CorDebugRegister.REGISTER_AMD64_XMM0:
					register = X86Register.XMM0;
					return true;
				case CorDebugRegister.REGISTER_AMD64_XMM1:
					register = X86Register.XMM1;
					return true;
				case CorDebugRegister.REGISTER_AMD64_XMM2:
					register = X86Register.XMM2;
					return true;
				case CorDebugRegister.REGISTER_AMD64_XMM3:
					register = X86Register.XMM3;
					return true;
				case CorDebugRegister.REGISTER_AMD64_XMM4:
					register = X86Register.XMM4;
					return true;
				case CorDebugRegister.REGISTER_AMD64_XMM5:
					register = X86Register.XMM5;
					return true;
				case CorDebugRegister.REGISTER_AMD64_XMM6:
					register = X86Register.XMM6;
					return true;
				case CorDebugRegister.REGISTER_AMD64_XMM7:
					register = X86Register.XMM7;
					return true;
				case CorDebugRegister.REGISTER_AMD64_XMM8:
					register = X86Register.XMM8;
					return true;
				case CorDebugRegister.REGISTER_AMD64_XMM9:
					register = X86Register.XMM9;
					return true;
				case CorDebugRegister.REGISTER_AMD64_XMM10:
					register = X86Register.XMM10;
					return true;
				case CorDebugRegister.REGISTER_AMD64_XMM11:
					register = X86Register.XMM11;
					return true;
				case CorDebugRegister.REGISTER_AMD64_XMM12:
					register = X86Register.XMM12;
					return true;
				case CorDebugRegister.REGISTER_AMD64_XMM13:
					register = X86Register.XMM13;
					return true;
				case CorDebugRegister.REGISTER_AMD64_XMM14:
					register = X86Register.XMM14;
					return true;
				case CorDebugRegister.REGISTER_AMD64_XMM15:
					register = X86Register.XMM15;
					return true;
				default:
					Debug.Fail($"Unknown register number {(int)corReg}");
					register = X86Register.None;
					return false;
				}

			case DbgArchitecture.Arm:
			case DbgArchitecture.Arm64:
			default:
				Debug.Fail($"Unsupported architecture: {architecture}");
				register = default;
				return false;
			}
		}

		public bool TryGetSymbol(ulong address, out SymbolResolverResult result) {
			if (Dispatcher.CheckAccess())
				return TryGetSymbolCore(address, out result);
			return TryGetSymbolCore2(address, out result);

			bool TryGetSymbolCore2(ulong address2, out SymbolResolverResult result2) {
				SymbolResolverResult resultTmp = default;
				Dispatcher.TryInvokeRethrow(() => TryGetSymbolCore(address2, out resultTmp), out var res);
				result2 = resultTmp;
				return res;
			}
		}
		bool TryGetSymbolCore(ulong address, out SymbolResolverResult result) {
			Dispatcher.VerifyAccess();
			if (!engine.IsPaused) {
				result = default;
				return false;
			}
			return engine.clrDac.TryGetSymbolCore(address, out result);
		}
	}
}
