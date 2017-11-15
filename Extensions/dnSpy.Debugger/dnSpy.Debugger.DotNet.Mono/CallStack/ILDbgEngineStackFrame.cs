/*
    Copyright (C) 2014-2017 de4dot@gmail.com

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
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.CallStack;
using dnSpy.Contracts.Debugger.Code;
using dnSpy.Contracts.Debugger.DotNet.Code;
using dnSpy.Contracts.Debugger.Engine.CallStack;
using dnSpy.Contracts.Text;
using dnSpy.Debugger.DotNet.Mono.Impl;
using Mono.Debugger.Soft;

namespace dnSpy.Debugger.DotNet.Mono.CallStack {
	sealed class ILDbgEngineStackFrame : DbgEngineStackFrame {
		public override DbgCodeLocation Location { get; }
		public override DbgModule Module { get; }
		public override uint FunctionOffset { get; }
		public override uint FunctionToken { get; }

		readonly DbgEngineImpl engine;

		public ILDbgEngineStackFrame(DbgEngineImpl engine, DbgModule module, StackFrame monoFrame, Lazy<DbgDotNetCodeLocationFactory> dbgDotNetCodeLocationFactory) {
			if (monoFrame == null)
				throw new ArgumentNullException(nameof(monoFrame));
			this.engine = engine ?? throw new ArgumentNullException(nameof(engine));
			Module = module ?? throw new ArgumentNullException(nameof(module));
			FunctionToken = (uint)monoFrame.Method.MetadataToken;
			// Native transitions have no IL offset so -1 is used by mono, but we should use 0 instead
			var ilOffset = (uint)monoFrame.ILOffset;
			FunctionOffset = ilOffset == uint.MaxValue ? 0 : ilOffset;
			var moduleId = DbgEngineImpl.TryGetModuleId(module) ?? default;
			Location = dbgDotNetCodeLocationFactory.Value.Create(moduleId, FunctionToken, FunctionOffset);
		}

		public override void Format(ITextColorWriter writer, DbgStackFrameFormatOptions options) =>
			engine.DebuggerThread.Invoke(() => Format_MonoDebug(writer, options));

		void Format_MonoDebug(ITextColorWriter writer, DbgStackFrameFormatOptions options) {
			if (Module.IsClosed)
				return;
			writer.Write(BoxedTextColor.Error, "NYI");//TODO:
		}

		public override void OnFrameCreated(DbgStackFrame frame) { }
		protected override void CloseCore(DbgDispatcher dispatcher) { }
	}
}
