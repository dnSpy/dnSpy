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

using System;

namespace dnSpy.Contracts.Text.Editor {
	/// <summary>
	/// Smart indent
	/// </summary>
	public interface ISmartIndent : IDisposable {
		/// <summary>
		/// Gets the desired indentation of an <see cref="ITextSnapshotLine"/>.
		/// </summary>
		/// <param name="line">The line for which to compute the indentation</param>
		/// <returns>The number of spaces to place at the start of the line, or null if there is no desired indentation</returns>
		int? GetDesiredIndentation(ITextSnapshotLine line);
	}
}
