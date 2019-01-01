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

using System.Collections.Generic;
using dnSpy.Contracts.Debugger.Text;

namespace dnSpy.Contracts.Debugger.DotNet.Text {
	/// <summary>
	/// Creates <see cref="DbgDotNetText"/>
	/// </summary>
	public sealed class DbgDotNetTextOutput : IDbgTextWriter {
		readonly List<DbgDotNetTextPart> list;

		/// <summary>
		/// Constructor
		/// </summary>
		public DbgDotNetTextOutput() => list = new List<DbgDotNetTextPart>();

		/// <summary>
		/// Writes text
		/// </summary>
		/// <param name="color">Color</param>
		/// <param name="text">Text</param>
		public void Write(DbgTextColor color, string text) => list.Add(new DbgDotNetTextPart(color, text));

		/// <summary>
		/// Creates a <see cref="DbgDotNetText"/>
		/// </summary>
		/// <returns></returns>
		public DbgDotNetText Create() => new DbgDotNetText(list.ToArray());

		/// <summary>
		/// Creates a <see cref="DbgDotNetText"/> and clears this instance so it can be reused
		/// </summary>
		/// <returns></returns>
		public DbgDotNetText CreateAndReset() {
			var res = new DbgDotNetText(list.ToArray());
			list.Clear();
			return res;
		}

		/// <summary>
		/// Clears this instance
		/// </summary>
		public void Clear() => list.Clear();
	}
}
