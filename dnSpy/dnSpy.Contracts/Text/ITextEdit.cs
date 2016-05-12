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
	/// Text edit
	/// </summary>
	public interface ITextEdit : ITextBufferEdit {
		/// <summary>
		/// Deletes characters
		/// </summary>
		/// <param name="deleteSpan">Span to delete</param>
		/// <returns></returns>
		bool Delete(Span deleteSpan);

		/// <summary>
		/// Deletes characters
		/// </summary>
		/// <param name="startPosition">Start position</param>
		/// <param name="charsToDelete">Number of characters to delete</param>
		/// <returns></returns>
		bool Delete(int startPosition, int charsToDelete);

		/// <summary>
		/// Inserts characters
		/// </summary>
		/// <param name="position">Position</param>
		/// <param name="text">Text</param>
		/// <returns></returns>
		bool Insert(int position, string text);

		/// <summary>
		/// Inserts characters
		/// </summary>
		/// <param name="position">Position</param>
		/// <param name="characterBuffer">Characters</param>
		/// <param name="startIndex">Index in <paramref name="characterBuffer"/></param>
		/// <param name="length">Number of characters to insert</param>
		/// <returns></returns>
		bool Insert(int position, char[] characterBuffer, int startIndex, int length);

		/// <summary>
		/// Replaces characters
		/// </summary>
		/// <param name="replaceSpan">Span to replace</param>
		/// <param name="replaceWith">New text</param>
		/// <returns></returns>
		bool Replace(Span replaceSpan, string replaceWith);

		/// <summary>
		/// Replaces characters
		/// </summary>
		/// <param name="startPosition">Position</param>
		/// <param name="charsToReplace">Number of characters to remove</param>
		/// <param name="replaceWith">New text</param>
		/// <returns></returns>
		bool Replace(int startPosition, int charsToReplace, string replaceWith);
	}
}
