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
using dnSpy.Contracts.Debugger.Engine.CallStack;
using dnSpy.Contracts.Text;
using dnSpy.Debugger.Impl;

namespace dnSpy.Debugger.CallStack {
	sealed class DbgStackFrameImpl : DbgStackFrame {
		public override DbgThread Thread => thread;
		public override DbgCodeLocation Location => engineStackFrame.Location;
		public override DbgModule Module => engineStackFrame.Module;
		public override uint FunctionOffset => engineStackFrame.FunctionOffset;
		public override uint FunctionToken => engineStackFrame.FunctionToken;
		DbgDispatcher Dispatcher => thread.Process.DbgManager.Dispatcher;

		readonly DbgThreadImpl thread;
		readonly DbgEngineStackFrame engineStackFrame;

		public DbgStackFrameImpl(DbgThreadImpl thread, DbgEngineStackFrame engineStackFrame) {
			this.thread = thread ?? throw new ArgumentNullException(nameof(thread));
			this.engineStackFrame = engineStackFrame ?? throw new ArgumentNullException(nameof(engineStackFrame));
			thread.AddAutoClose(this);
		}

		public override void Format(ITextColorWriter writer, DbgStackFrameFormatOptions options) {
			if (writer == null)
				throw new ArgumentNullException(nameof(writer));
			engineStackFrame.Format(writer, options);
		}

		public override string ToString(DbgStackFrameFormatOptions options) {
			var output = new StringBuilderTextColorOutput();
			Format(output, options);
			return output.ToString();
		}

		const DbgStackFrameFormatOptions DefaultToStringOptions =
			DbgStackFrameFormatOptions.ShowParameterTypes |
			DbgStackFrameFormatOptions.ShowFunctionOffset |
			DbgStackFrameFormatOptions.ShowDeclaringTypes |
			DbgStackFrameFormatOptions.ShowNamespaces |
			DbgStackFrameFormatOptions.ShowIntrinsicTypeKeywords;
		public override string ToString() => ToString(DefaultToStringOptions);

		protected override void CloseCore() {
			Dispatcher.VerifyAccess();
			thread.RemoveAutoClose(this);
			engineStackFrame.Close(Dispatcher);
			Location?.Close(Dispatcher);
		}
	}
}
