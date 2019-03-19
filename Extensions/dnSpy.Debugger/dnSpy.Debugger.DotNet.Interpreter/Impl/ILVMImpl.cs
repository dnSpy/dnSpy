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
using dnSpy.Debugger.DotNet.Metadata;

namespace dnSpy.Debugger.DotNet.Interpreter.Impl {
	sealed class ILVMImpl : ILVM {
		readonly DebuggerILInterpreter debuggerILInterpreter;

		public ILVMImpl() => debuggerILInterpreter = new DebuggerILInterpreter();

		public override ILVMExecuteState CreateExecuteState(DmdMethodBase method) {
			if ((object)method == null)
				throw new ArgumentNullException(nameof(method));
			return new ILVMExecuteStateImpl(method);
		}

		public override ILValue Execute(DebuggerRuntime debuggerRuntime, ILVMExecuteState state) {
			if (debuggerRuntime == null)
				throw new ArgumentNullException(nameof(debuggerRuntime));
			var stateImpl = state as ILVMExecuteStateImpl;
			if (stateImpl == null)
				throw new ArgumentException();
			return debuggerILInterpreter.Execute(debuggerRuntime, stateImpl);
		}
	}

	sealed class ILVMExecuteStateImpl : ILVMExecuteState {
		public DmdMethodBase Method { get; }
		public DmdMethodBody Body { get; }
		public byte[] ILBytes { get; }

		public ILVMExecuteStateImpl(DmdMethodBase method) {
			Method = method;
			// Don't throw here, do it later
			Body = method.GetMethodBody();
			ILBytes = Body?.GetILAsByteArray();
		}
	}
}
