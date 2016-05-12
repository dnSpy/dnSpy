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

using System.Collections.Generic;

namespace dnSpy.Contracts.Text {
	/// <summary>
	/// A text control that allows appending text. Writing text is thread safe.
	/// </summary>
	public interface ILogEditor {
		/// <summary>
		/// true to show line numbers
		/// </summary>
		bool ShowLineNumbers { get; set; }

		/// <summary>
		/// Enables/disables word wrapping
		/// </summary>
		bool WordWrap { get; set; }

		/// <summary>
		/// Writes text
		/// </summary>
		/// <param name="text">Text</param>
		/// <param name="color">Color</param>
		void Write(string text, object color);

		/// <summary>
		/// Writes text
		/// </summary>
		/// <param name="text">Text</param>
		/// <param name="color">Color</param>
		void Write(string text, OutputColor color);

		/// <summary>
		/// Writes text
		/// </summary>
		/// <param name="text">Text</param>
		/// <param name="color">Color</param>
		void WriteLine(string text, object color);

		/// <summary>
		/// Writes text
		/// </summary>
		/// <param name="text">Text</param>
		/// <param name="color">Color</param>
		void WriteLine(string text, OutputColor color);

		/// <summary>
		/// Writes text
		/// </summary>
		/// <param name="text">Text</param>
		void Write(IEnumerable<ColorAndText> text);

		/// <summary>
		/// Clears all text
		/// </summary>
		void Clear();

		/// <summary>
		/// Gets all text
		/// </summary>
		/// <returns></returns>
		string GetText();
	}
}
