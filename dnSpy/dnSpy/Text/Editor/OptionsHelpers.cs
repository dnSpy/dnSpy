/*
    Copyright (C) 2014-2019 de4dot@gmail.com

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

namespace dnSpy.Text.Editor {
	/// <summary>
	/// Options methods
	/// </summary>
	public static class OptionsHelpers {
		/// <summary>
		/// Minimum tab size
		/// </summary>
		public static readonly int MinimumTabSize = 1;

		/// <summary>
		/// Maximum tab size
		/// </summary>
		public static readonly int MaximumTabSize = 60;

		/// <summary>
		/// Minimum indent size
		/// </summary>
		public static readonly int MinimumIndentSize = 1;

		/// <summary>
		/// Maximum indent size
		/// </summary>
		public static readonly int MaximumIndentSize = 60;

		/// <summary>
		/// Filters <paramref name="tabSize"/> so it's between <see cref="MinimumTabSize"/> and <see cref="MaximumTabSize"/> (inclusive)
		/// </summary>
		/// <param name="tabSize">Tab size</param>
		/// <returns></returns>
		public static int FilterTabSize(int tabSize) => FilterSize(tabSize, MinimumTabSize, MaximumTabSize);

		/// <summary>
		/// Filters <paramref name="indentSize"/> so it's between <see cref="MinimumIndentSize"/> and <see cref="MaximumIndentSize"/> (inclusive)
		/// </summary>
		/// <param name="indentSize">Indent size</param>
		/// <returns></returns>
		public static int FilterIndentSize(int indentSize) => FilterSize(indentSize, MinimumIndentSize, MaximumIndentSize);

		static int FilterSize(int size, int minSize, int maxSize) {
			if (size < minSize)
				return minSize;
			if (size > maxSize)
				return maxSize;
			return size;
		}
	}
}
