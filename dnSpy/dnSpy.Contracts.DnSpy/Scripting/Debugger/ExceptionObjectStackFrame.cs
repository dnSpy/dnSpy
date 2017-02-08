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

namespace dnSpy.Contracts.Scripting.Debugger {
	/// <summary>
	/// Exception object stack frame
	/// </summary>
	public struct ExceptionObjectStackFrame {
		/// <summary>
		/// Module
		/// </summary>
		public IDebuggerModule Module;

		/// <summary>
		/// Instruction pointer
		/// </summary>
		public ulong IP;

		/// <summary>
		/// Method token
		/// </summary>
		public uint Token;

		/// <summary>
		/// true if it's last foreign exception frame
		/// </summary>
		public bool IsLastForeignExceptionFrame;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="module">Module</param>
		/// <param name="ip">IP</param>
		/// <param name="token">Method token</param>
		/// <param name="isLastForeignExceptionFrame">true if it's last foreign exception frame</param>
		public ExceptionObjectStackFrame(IDebuggerModule module, ulong ip, uint token, bool isLastForeignExceptionFrame) {
			Module = module;
			IP = ip;
			Token = token;
			IsLastForeignExceptionFrame = isLastForeignExceptionFrame;
		}
	}
}
