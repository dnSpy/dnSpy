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

using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace dnSpy.Contracts.Text.Editor {
	/// <summary>
	/// Can cancel replaces (without having to create read-only regions)
	/// </summary>
	public interface IReplaceListener {
		/// <summary>
		/// Returns true if <paramref name="span"/> can be modified and replaced with new content
		/// </summary>
		/// <param name="span">Span to be replaced if all <see cref="IReplaceListener"/>s return true.
		/// This is the latest textview snapshot (<see cref="ITextView.TextSnapshot"/>)</param>
		/// <param name="newText">New text</param>
		/// <returns></returns>
		bool CanReplace(SnapshotSpan span, string newText);
	}
}
