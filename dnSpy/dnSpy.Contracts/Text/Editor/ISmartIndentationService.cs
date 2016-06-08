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

namespace dnSpy.Contracts.Text.Editor {
	/// <summary>
	/// Smart indentation service
	/// </summary>
	public interface ISmartIndentationService {
		/// <summary>
		/// Gets the desired indentation of an <see cref="ITextSnapshotLine"/> as displayed in <see cref="ITextView"/>.
		/// </summary>
		/// <param name="textView">The text view in which the line is displayed</param>
		/// <param name="line">The line for which to compute the indentation</param>
		/// <returns></returns>
		int? GetDesiredIndentation(ITextView textView, ITextSnapshotLine line);
	}
}
