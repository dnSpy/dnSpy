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
	/// Handle type
	/// </summary>
	public enum DebugHandleType {
		// IMPORTANT: Must be identical to dndbg.COM.CorDebug.CorDebugHandleType (enum field names may be different)

		/// <summary>
		/// The handle is strong, which prevents an object from being reclaimed by garbage collection.
		/// </summary>
		Strong = 1,
		/// <summary>
		/// The handle is weak, which does not prevent an object from being reclaimed by garbage collection.
		/// 
		/// The handle becomes invalid when the object is collected.
		/// </summary>
		WeakTrackResurrection
	}
}
