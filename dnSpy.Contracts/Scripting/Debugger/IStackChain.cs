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

using System.Collections.Generic;

namespace dnSpy.Contracts.Scripting.Debugger {
	/// <summary>
	/// Stack chain. This stack frame is only valid until the debugged process continues.
	/// </summary>
	public interface IStackChain {
		/// <summary>
		/// Gets the thread
		/// </summary>
		IDebuggerThread Thread { get; }

		/// <summary>
		/// true if this is a managed chain
		/// </summary>
		bool IsManaged { get; }

		/// <summary>
		/// Gets the reason
		/// </summary>
		ChainReason Reason { get; }

		/// <summary>
		/// Start address of the stack segment
		/// </summary>
		ulong StackStart { get; }

		/// <summary>
		/// End address of the stack segment
		/// </summary>
		ulong StackEnd { get; }

		/// <summary>
		/// Gets the active frame or null
		/// </summary>
		IStackFrame ActiveFrame { get; }

		/// <summary>
		/// Gets the callee or null
		/// </summary>
		IStackChain Callee { get; }

		/// <summary>
		/// Gets the caller or null
		/// </summary>
		IStackChain Caller { get; }

		/// <summary>
		/// Gets the next chain or null
		/// </summary>
		IStackChain Next { get; }

		/// <summary>
		/// Gets the previous chain or null
		/// </summary>
		IStackChain Previous { get; }

		/// <summary>
		/// Gets all frames
		/// </summary>
		IEnumerable<IStackFrame> Frames { get; }
	}
}
