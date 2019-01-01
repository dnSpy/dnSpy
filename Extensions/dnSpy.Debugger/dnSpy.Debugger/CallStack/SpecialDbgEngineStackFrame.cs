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

namespace dnSpy.Debugger.CallStack {
	sealed class SpecialDbgEngineStackFrame : DbgEngineStackFrame {
		public override DbgCodeLocation Location { get; }
		public override DbgModule Module { get; }
		public override DbgStackFrameFlags Flags => DbgStackFrameFlags.None;
		public override uint FunctionOffset { get; }
		public override uint FunctionToken { get; }

		readonly string name;

		public SpecialDbgEngineStackFrame(string name, DbgCodeLocation location, DbgModule module, uint functionOffset, uint functionToken) {
			this.name = name ?? throw new ArgumentNullException(nameof(name));
			Location = location;
			Module = module;
			FunctionOffset = functionOffset;
			FunctionToken = functionToken;
		}

		public override bool TryFormat(DbgEvaluationContext context, IDbgTextWriter output, DbgStackFrameFormatterOptions options, DbgValueFormatterOptions valueOptions, CultureInfo cultureInfo, CancellationToken cancellationToken) {
			output.Write(DbgTextColor.Punctuation, "[");
			output.Write(DbgTextColor.Text, name);
			output.Write(DbgTextColor.Punctuation, "]");
			return true;
		}

		public override void OnFrameCreated(DbgStackFrame frame) { }
		protected override void CloseCore(DbgDispatcher dispatcher) { }
	}
}
