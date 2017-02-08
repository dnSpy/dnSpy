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

namespace dnSpy.Contracts.Scripting.Debugger {
	/// <summary>
	/// Chain reason
	/// </summary>
	[Flags]
	public enum ChainReason {
		// IMPORTANT: This enum should match dndbg.Engine.CorDebugChainReason (enum field names may be different)

		/// <summary>
		/// No call chain has been initiated.
		/// </summary>
		None,
		/// <summary>
		/// The chain was initiated by a constructor.
		/// </summary>
		ClassInit,
		/// <summary>
		/// The chain was initiated by an exception filter.
		/// </summary>
		ExceptionFilter,
		/// <summary>
		/// The chain was initiated by code that enforces security.
		/// </summary>
		Security = 4,
		/// <summary>
		/// The chain was initiated by a context policy.
		/// </summary>
		ContextPolicy = 8,
		/// <summary>
		/// Not used.
		/// </summary>
		Interception = 16,
		/// <summary>
		/// Not used.
		/// </summary>
		ProcessStart = 32,
		/// <summary>
		/// The chain was initiated by the start of a thread execution.
		/// </summary>
		ThreadStart = 64,
		/// <summary>
		/// The chain was initiated by entry into managed code.
		/// </summary>
		EnterManaged = 128,
		/// <summary>
		/// The chain was initiated by entry into unmanaged code.
		/// </summary>
		EnterUnmanaged = 256,
		/// <summary>
		/// Not used.
		/// </summary>
		DebuggerEval = 512,
		/// <summary>
		/// Not used.
		/// </summary>
		ContextSwitch = 1024,
		/// <summary>
		/// The chain was initiated by a function evaluation.
		/// </summary>
		FuncEval = 2048
	}
}
