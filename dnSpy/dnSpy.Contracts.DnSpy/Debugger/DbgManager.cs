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

namespace dnSpy.Contracts.Debugger {
	/// <summary>
	/// Manages all debug engines
	/// </summary>
	public abstract class DbgManager {
		/// <summary>
		/// Starts debugging
		/// </summary>
		/// <param name="options">Options needed to start the program or attach to it</param>
		public abstract void Start(StartDebuggingOptions options);

		/// <summary>
		/// Gets all debugged processes
		/// </summary>
		public abstract DbgProcess[] Processes { get; }
	}
}
