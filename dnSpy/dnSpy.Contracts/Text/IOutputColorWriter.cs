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

namespace dnSpy.Contracts.Text {
	/// <summary>
	/// Text writer
	/// </summary>
	public interface IOutputColorWriter {
		/// <summary>
		/// Writes text
		/// </summary>
		/// <param name="color">Color</param>
		/// <param name="text">Text</param>
		void Write(object color, string text);

		/// <summary>
		/// Writes text
		/// </summary>
		/// <param name="color">Color</param>
		/// <param name="text">Text</param>
		void Write(OutputColor color, string text);
	}

	/// <summary>
	/// Extension methods
	/// </summary>
	public static class OutputColorWriterExtensions {
		/// <summary>
		/// Writes text using default text color (<see cref="BoxedOutputColor.Text"/>)
		/// </summary>
		/// <param name="writer">Writer</param>
		/// <param name="text">Text</param>
		public static void Write(this IOutputColorWriter writer, string text) => writer.Write(BoxedOutputColor.Text, text);

		/// <summary>
		/// Writes text and a newline using default text color (<see cref="BoxedOutputColor.Text"/>)
		/// </summary>
		/// <param name="writer">Writer</param>
		/// <param name="text">Text</param>
		public static void WriteLine(this IOutputColorWriter writer, string text = null) {
			writer.Write(BoxedOutputColor.Text, text);
			writer.Write(BoxedOutputColor.Text, Environment.NewLine);
		}
	}
}
