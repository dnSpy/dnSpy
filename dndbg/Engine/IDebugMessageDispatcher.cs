/*
    Copyright (C) 2014-2015 de4dot@gmail.com

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
	public interface IDebugMessageDispatcher {
		/// <summary>
		/// Executes <see cref="action"/> in the debugger thread. This is typically the GUI thread.
		/// The debugger thread can be any thread as long as only one thread is executing debugger
		/// code.
		/// </summary>
		/// <param name="action">Code to execute in the debugger thread</param>
		void ExecuteAsync(Action action);
	}
}
