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
	/// Text change interface
	/// </summary>
	public interface ITextChange {
		/// <summary>
		/// Gets the difference of the new text length and the old text length
		/// </summary>
		int Delta { get; }

		/// <summary>
		/// Offset of the new string after the change
		/// </summary>
		int NewEnd { get; }

		/// <summary>
		/// Length of the new string
		/// </summary>
		int NewLength { get; }

		/// <summary>
		/// Offset of the new string after the change
		/// </summary>
		int NewPosition { get; }

		/// <summary>
		/// Span of the new string after the change
		/// </summary>
		Span NewSpan { get; }

		/// <summary>
		/// New text that replaced the old text
		/// </summary>
		string NewText { get; }

		/// <summary>
		/// Offset of the old string before the change
		/// </summary>
		int OldEnd { get; }

		/// <summary>
		/// Length of the old string
		/// </summary>
		int OldLength { get; }

		/// <summary>
		/// Offset of the old string before the change
		/// </summary>
		int OldPosition { get; }

		/// <summary>
		/// Span of the old string before the change
		/// </summary>
		Span OldSpan { get; }

		/// <summary>
		/// Old text that was replaced
		/// </summary>
		string OldText { get; }
	}
}
