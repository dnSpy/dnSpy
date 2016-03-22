/*
    Copyright (C) 2014-2016 de4dot@gmail.com

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
using dndbg.Engine;
using dnSpy.Contracts.Highlighting;
using dnSpy.Contracts.Scripting.Debugger;
using dnSpy.Shared.Scripting;

namespace dnSpy.Debugger.Scripting {
	sealed class DebuggerCode : IDebuggerCode {
		public IDebuggerMethod Method {
			get {
				return debugger.Dispatcher.UI(() => {
					var func = code.Function;
					return func == null ? null : new DebuggerMethod(debugger, func);
				});
			}
		}

		public bool IsIL {
			get { return isIL; }
		}

		public ulong Address {
			get { return address; }
		}

		public uint Size {
			get { return size; }
		}

		public uint VersionNumber {
			get { return debugger.Dispatcher.UI(() => code.VersionNumber); }
		}

		internal CorCode CorCode {
			get { return code; }
		}
		readonly CorCode code;

		readonly Debugger debugger;
		readonly int hashCode;
		readonly ulong address;
		readonly uint size;
		readonly bool isIL;

		public DebuggerCode(Debugger debugger, CorCode code) {
			debugger.Dispatcher.VerifyAccess();
			this.debugger = debugger;
			this.code = code;
			this.hashCode = code.GetHashCode();
			this.address = code.Address;
			this.size = code.Size;
			this.isIL = code.IsIL;
		}

		public CodeChunkInfo[] GetCodeChunks() {
			return debugger.Dispatcher.UI(() => {
				var chunks = code.GetCodeChunks();
				var res = new CodeChunkInfo[chunks.Length];
				for (int i = 0; i < chunks.Length; i++)
					res[i] = new CodeChunkInfo(chunks[i].StartAddr, chunks[i].Length);
				return res;
			});
		}

		public byte[] ReadCode() {
			return debugger.ReadMemory(Address, Size);
		}

		public IILBreakpoint CreateBreakpoint(uint offset, Func<IILBreakpoint, bool> cond) {
			return debugger.Dispatcher.UI(() => {
				var func = code.Function;
				var mod = func == null ? null : func.Module;
				uint token = func == null ? 0 : func.Token;
				var module = mod == null ? new ModuleName() : mod.SerializedDnModule.ToModuleName();
				return debugger.CreateBreakpoint(module, token, offset, cond);
			});
		}

		public IILBreakpoint CreateBreakpoint(int offset, Func<IILBreakpoint, bool> cond) {
			return CreateBreakpoint((uint)offset, cond);
		}

		public INativeBreakpoint CreateNativeBreakpoint(uint offset, Func<INativeBreakpoint, bool> cond) {
			return debugger.CreateNativeBreakpoint(this, offset, cond);
		}

		public INativeBreakpoint CreateNativeBreakpoint(int offset, Func<INativeBreakpoint, bool> cond) {
			return CreateNativeBreakpoint((uint)offset, cond);
		}

		public override bool Equals(object obj) {
			var other = obj as DebuggerCode;
			return other != null && other.code == code;
		}

		public override int GetHashCode() {
			return hashCode;
		}

		public void Write(ISyntaxHighlightOutput output, TypeFormatFlags flags) {
			debugger.Dispatcher.UI(() => code.Write(new OutputConverter(output), (TypePrinterFlags)flags));
		}

		public string ToString(TypeFormatFlags flags) {
			return debugger.Dispatcher.UI(() => code.ToString((TypePrinterFlags)flags));
		}

		public override string ToString() {
			return debugger.Dispatcher.UI(() => code.ToString());
		}
	}
}
