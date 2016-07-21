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

using Microsoft.VisualStudio.Text;

namespace dnSpy.Contracts.Text.Editor {
	/// <summary>
	/// <see cref="VirtualSnapshotPoint"/> extension methods
	/// </summary>
	public static class VirtualSnapshotPointExtensions {
		/// <summary>
		/// Converts <paramref name="point"/> to a <see cref="TextEditorLocation"/>
		/// </summary>
		/// <param name="point">Point</param>
		/// <returns></returns>
		public static TextEditorLocation ToTextEditorLocation(this VirtualSnapshotPoint point) {
			var line = point.Position.GetContainingLine();
			return new TextEditorLocation(line.LineNumber, point.Position - line.Start + point.VirtualSpaces);
		}
	}
}
