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

using System.Text;
using dnSpy.Contracts.Decompiler;

namespace dnSpy.Contracts.Text {
	/// <summary>
	/// Utility methods
	/// </summary>
	public static class NameUtilities {
		/// <summary>
		/// Cleans a name
		/// </summary>
		/// <param name="n">name</param>
		/// <returns></returns>
		public static string CleanName(string n) {
			if (n == null)
				return n;
			const int MAX_LEN = 0x100;
			if (n.Length > MAX_LEN)
				n = n.Substring(0, MAX_LEN);
			var sb = new StringBuilder(n.Length);
			for (int i = 0; i < n.Length; i++) {
				var c = n[i];
				if (c < 0x20)
					c = '_';
				sb.Append(c);
			}
			return sb.ToString();
		}

		/// <summary>
		/// Cleans an identifier
		/// </summary>
		/// <param name="id">Identifier</param>
		/// <returns></returns>
		public static string CleanIdentifier(string id) {
			if (id == null)
				return id;
			id = IdentifierEscaper.Escape(id);
			return CleanName(id);
		}
	}
}
