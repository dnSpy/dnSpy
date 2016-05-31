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

namespace dnSpy.Contracts.Text.Editor.Operations {
	/// <summary>
	/// Editor operations
	/// </summary>
	public interface IEditorOperations2 : IEditorOperations {
		/// <summary>
		/// Moves the selected lines below the line bordering the selection on the bottom. Moving down from the bottom of the file will return true, however no changes will be made. Collapsed regions being moved, and being moved over, will remain collapsed. Moves involving readonly regions will result in no changes being made.
		/// </summary>
		/// <returns></returns>
		bool MoveSelectedLinesDown();

		/// <summary>
		/// Moves the selected lines up above the line bordering the selection on top. Moving up from the top of the file returns true, but no changes are made. Collapsed regions being moved, and being moved over, remain collapsed. Moves involving read-only regions result in no changes being made.
		/// </summary>
		/// <returns></returns>
		bool MoveSelectedLinesUp();
	}
}
