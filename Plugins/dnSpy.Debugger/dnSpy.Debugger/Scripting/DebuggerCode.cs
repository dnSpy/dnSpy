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
using dnSpy.Contracts.Scripting;
using dnSpy.Contracts.Scripting.Debugger;

namespace dnSpy.Debugger.Scripting {
	sealed class DebuggerCode : IDebuggerCode {
		public IDebuggerMethod Method => debugger.Dispatcher.UI(() => {
			var func = this.CorCode.Function;
			return func == null ? null : new DebuggerMethod(debugger, func);
		});

		public bool IsIL => isIL;
		public ulong Address => address;
		public uint Size => size;
		public uint VersionNumber => debugger.Dispatcher.UI(() => this.CorCode.VersionNumber);
		internal CorCode CorCode { get; }

		readonly Debugger debugger;
		readonly int hashCode;
		readonly ulong address;
		readonly uint size;
		readonly bool isIL;

		public DebuggerCode(Debugger debugger, CorCode code) {
			debugger.Dispatcher.VerifyAccess();
			this.debugger = debugger;
			this.CorCode = code;
			this.hashCode = code.GetHashCode();
			this.address = code.Address;
			this.size = code.Size;
			this.isIL = code.IsIL;
		}

		public CodeChunkInfo[] GetCodeChunks() => debugger.Dispatcher.UI(() => {
			var chunks = this.CorCode.GetCodeChunks();
			var res = new CodeChunkInfo[chunks.Length];
			for (int i = 0; i < chunks.Length; i++)
				res[i] = new CodeChunkInfo(chunks[i].StartAddr, chunks[i].Length);
			return res;
		});

		public byte[] ReadCode() => debugger.Read(Address, Size);

		public IILBreakpoint CreateBreakpoint(uint offset, Func<IILBreakpoint, bool> cond) => debugger.Dispatcher.UI(() => {
			var func = this.CorCode.Function;
			var mod = func?.Module;
			uint token = func?.Token ?? 0;
			var module = mod == null ? new ModuleName() : Utils.ToModuleName(mod.SerializedDnModule);
			return debugger.CreateBreakpoint(module, token, offset, cond);
		});

		public IILBreakpoint CreateBreakpoint(int offset, Func<IILBreakpoint, bool> cond) => CreateBreakpoint((uint)offset, cond);
		public INativeBreakpoint CreateNativeBreakpoint(uint offset, Func<INativeBreakpoint, bool> cond) => debugger.CreateNativeBreakpoint(this, offset, cond);
		public INativeBreakpoint CreateNativeBreakpoint(int offset, Func<INativeBreakpoint, bool> cond) => CreateNativeBreakpoint((uint)offset, cond);

		public override bool Equals(object obj) => (obj as DebuggerCode)?.CorCode == CorCode;
		public override int GetHashCode() => hashCode;
		const TypePrinterFlags DEFAULT_FLAGS = TypePrinterFlags.Default;
		public void WriteTo(IOutputWriter output) => Write(output, (TypeFormatFlags)DEFAULT_FLAGS);
		public void Write(IOutputWriter output, TypeFormatFlags flags) =>
			debugger.Dispatcher.UI(() => CorCode.Write(new OutputWriterConverter(output), (TypePrinterFlags)flags));
		public string ToString(TypeFormatFlags flags) => debugger.Dispatcher.UI(() => this.CorCode.ToString((TypePrinterFlags)flags));
		public override string ToString() => debugger.Dispatcher.UI(() => this.CorCode.ToString(DEFAULT_FLAGS));
	}
}
