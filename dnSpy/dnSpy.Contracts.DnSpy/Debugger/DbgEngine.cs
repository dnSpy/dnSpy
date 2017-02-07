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
	/// A debug engine contains all the logic to control the debugged process
	/// </summary>
	public abstract class DbgEngine {
		/// <summary>
		/// How did we start the debugged process?
		/// </summary>
		public abstract DbgStartKind StartKind { get; }
	}

	/// <summary>
	/// Start kind
	/// </summary>
	public enum DbgStartKind {
		/// <summary>
		/// The program was started by the debugger
		/// </summary>
		Start,

		/// <summary>
		/// The debugger attached to the program after it was started by someone else
		/// </summary>
		Attach,
	}
}
