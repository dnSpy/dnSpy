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

namespace dnSpy.Contracts.Text {
	/// <summary>
	/// Tracking fidelity mode
	/// </summary>
	public enum TrackingFidelityMode {
		/// <summary>
		/// Mapping back to a previous version may give a different result from the one that was originally given for that version
		/// </summary>
		Forward,

		/// <summary>
		/// Mapping back to a previous version gives the same result as mapping forward from the origin version
		/// </summary>
		Backward,

		/// <summary>
		/// Mapping to a version that is the result of an undo or redo operation gives the same result as mapping forward to the version that underwent the undo or redo operation
		/// </summary>
		UndoRedo,
	}
}
