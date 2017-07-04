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

namespace dndbg.Engine {
	interface IDebugMessageDispatcher {
		/// <summary>
		/// Executes <see cref="action"/> in the dndbg thread. This is typically the GUI thread,
		/// and must always be the same thread since the debugger isn't thread safe.
		/// </summary>
		/// <param name="action">Code to execute in the dndbg thread</param>
		void ExecuteAsync(Action action);

		/// <summary>
		/// Empty the queue and wait for <see cref="CancelDispatchQueue(object)"/> to get called.
		/// The return value is the input to <see cref="CancelDispatchQueue(object)"/> unless it
		/// timed out.
		/// </summary>
		/// <param name="waitTime">Time to wait or -1 to wait forever</param>
		/// <param name="timedOut">Set to true if it timed out</param>
		/// <returns></returns>
		object DispatchQueue(TimeSpan waitTime, out bool timedOut);

		/// <summary>
		/// Cancels <see cref="DispatchQueue(TimeSpan,out bool)"/>
		/// </summary>
		/// <param name="result">Result</param>
		void CancelDispatchQueue(object result);
	}
}
