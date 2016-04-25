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

namespace dnSpy.Contracts.Scripting.Debugger {
	/// <summary>
	/// Step reason
	/// </summary>
	public enum DebugStepReason {
		// IMPORTANT: This enum should match dndbg.COM.CorDebug.CorDebugStepReason (enum field names may be different)

		/// <summary>
		/// Stepping completed normally, within the same function.
		/// </summary>
		Normal,
		/// <summary>
		/// Stepping continued normally, after the function returned.
		/// </summary>
		Return,
		/// <summary>
		/// Stepping continued normally, at the beginning of a newly called function.
		/// </summary>
		Call,
		/// <summary>
		/// An exception was generated and control was passed to an exception filter.
		/// </summary>
		ExceptionFilter,
		/// <summary>
		/// An exception was generated and control was passed to an exception handler.
		/// </summary>
		ExceptionHandler,
		/// <summary>
		/// Control was passed to an interceptor.
		/// </summary>
		Intercept,
		/// <summary>
		/// The thread exited before the step was completed.
		/// </summary>
		Exit,
	}
}
