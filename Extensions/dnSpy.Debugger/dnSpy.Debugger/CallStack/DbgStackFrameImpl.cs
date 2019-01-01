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
using System.Globalization;
using System.Threading;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.CallStack;
using dnSpy.Contracts.Debugger.Code;
using dnSpy.Contracts.Debugger.Engine.CallStack;
using dnSpy.Contracts.Debugger.Evaluation;
using dnSpy.Contracts.Debugger.Text;
using dnSpy.Debugger.Impl;

namespace dnSpy.Debugger.CallStack {
	sealed class DbgStackFrameImpl : DbgStackFrame {
		public override DbgThread Thread => thread;
		public override DbgCodeLocation Location => engineStackFrame.Location;
		public override DbgModule Module => engineStackFrame.Module;
		public override DbgStackFrameFlags Flags => engineStackFrame.Flags;
		public override uint FunctionOffset => engineStackFrame.FunctionOffset;
		public override uint FunctionToken => engineStackFrame.FunctionToken;

		readonly DbgThreadImpl thread;
		readonly DbgEngineStackFrame engineStackFrame;

		public DbgStackFrameImpl(DbgThreadImpl thread, DbgEngineStackFrame engineStackFrame) {
			this.thread = thread ?? throw new ArgumentNullException(nameof(thread));
			this.engineStackFrame = engineStackFrame ?? throw new ArgumentNullException(nameof(engineStackFrame));
			thread.AddAutoClose(this);
			engineStackFrame.OnFrameCreated(this);
		}

		internal bool TryFormat(DbgEvaluationContext context, IDbgTextWriter output, DbgStackFrameFormatterOptions options, DbgValueFormatterOptions valueOptions, CultureInfo cultureInfo, CancellationToken cancellationToken) =>
			engineStackFrame.TryFormat(context, output, options, valueOptions, cultureInfo, cancellationToken);

		protected override void CloseCore(DbgDispatcher dispatcher) {
			thread.RemoveAutoClose(this);
			engineStackFrame.Close(dispatcher);
			Location?.Close(dispatcher);
		}
	}
}
