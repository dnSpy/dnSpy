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

using dnSpy.Debugger.DotNet.Metadata;

namespace dnSpy.Debugger.DotNet.Interpreter {
	/// <summary>
	/// Interprets IL code and returns the result
	/// </summary>
	public abstract class ILVM {
		/// <summary>
		/// Creates state that can be passed in to <see cref="Execute(DebuggerRuntime, ILVMExecuteState)"/>
		/// </summary>
		/// <param name="method">Method to execute</param>
		/// <returns></returns>
		public abstract ILVMExecuteState CreateExecuteState(DmdMethodBase method);

		/// <summary>
		/// Interprets the IL instructions in the method body. All calls are handled by <paramref name="debuggerRuntime"/>
		/// </summary>
		/// <param name="debuggerRuntime">Debugger class that can call methods in the debugged process</param>
		/// <param name="state">State created by <see cref="CreateExecuteState(DmdMethodBase)"/></param>
		/// <returns></returns>
		public abstract ILValue Execute(DebuggerRuntime debuggerRuntime, ILVMExecuteState state);
	}

	/// <summary>
	/// State created by <see cref="ILVM"/> to speed up executing a method
	/// </summary>
	public abstract class ILVMExecuteState {
	}
}
