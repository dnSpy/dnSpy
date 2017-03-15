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

namespace dnSpy.Contracts.Debugger.Text {
	/// <summary>
	/// Implements <see cref="IDebugOutputWriter"/> and writes all text to a <see cref="StringBuilder"/>
	/// </summary>
	public sealed class StringBuilderDebugOutputWriter : IDebugOutputWriter {
		readonly StringBuilder sb;

		/// <summary>
		/// Constructor
		/// </summary>
		public StringBuilderDebugOutputWriter() => sb = new StringBuilder();

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="sb">String builder to use or null to create a new one</param>
		public StringBuilderDebugOutputWriter(StringBuilder sb) => this.sb = sb ?? new StringBuilder();

		/// <summary>
		/// Writes text
		/// </summary>
		/// <param name="text">Text</param>
		public void Write(string text) => sb.Append(text);

		/// <summary>
		/// Writes text
		/// </summary>
		/// <param name="color">Color (ignored)</param>
		/// <param name="text">Text</param>
		public void Write(object color, string text) => sb.Append(text);

		/// <summary>
		/// Gets the written text
		/// </summary>
		/// <returns></returns>
		public override string ToString() => sb.ToString();
	}
}
