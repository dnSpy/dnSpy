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

using dnSpy.Contracts.Text;

namespace dnSpy.Contracts.Decompiler {
	/// <summary>
	/// Interface used by decompilers to write text
	/// </summary>
	public interface IDecompilerOutput {
		/// <summary>
		/// Gets the total length of the written text
		/// </summary>
		int Length { get; }

		/// <summary>
		/// This equals <see cref="Length"/> plus any indentation that must be written
		/// before the next text.
		/// </summary>
		int NextPosition { get; }

		/// <summary>
		/// Increments the indentation level. Nothing is added to the output stream.
		/// </summary>
		void Indent();

		/// <summary>
		/// Decrements the indentation level. Nothing is added to the output stream.
		/// </summary>
		void Unindent();

		/// <summary>
		/// Writes a new line without writing any indentation
		/// </summary>
		void WriteLine();

		/// <summary>
		/// Writes text and color. The text will be indented if needed.
		/// </summary>
		/// <param name="text">Text</param>
		/// <param name="color">Color, eg. <see cref="BoxedOutputColor.Keyword"/></param>
		void Write(string text, object color);

		/// <summary>
		/// Writes text and color. The text will be indented if needed.
		/// </summary>
		/// <param name="text">Text</param>
		/// <param name="index">Index in <paramref name="text"/></param>
		/// <param name="count">Number of characters to write</param>
		/// <param name="color">Color, eg. <see cref="BoxedOutputColor.Keyword"/></param>
		void Write(string text, int index, int count, object color);

		/// <summary>
		/// Writes text, color and a reference. The text will be indented if needed.
		/// </summary>
		/// <param name="text">Text</param>
		/// <param name="reference">Reference</param>
		/// <param name="flags">Flags</param>
		/// <param name="color">Color, eg. <see cref="BoxedOutputColor.Keyword"/></param>
		void Write(string text, object reference, DecompilerReferenceFlags flags, object color);

		/// <summary>
		/// Adds debug info, see also <see cref="UsesDebugInfo"/>
		/// </summary>
		/// <param name="methodDebugInfo">Debug info</param>
		void AddDebugInfo(MethodDebugInfo methodDebugInfo);

		/// <summary>
		/// true if the debug info added by <see cref="AddDebugInfo(MethodDebugInfo)"/> is used
		/// and isn't ignored. If this is false, <see cref="AddDebugInfo(MethodDebugInfo)"/> doesn't
		/// have to be called.
		/// </summary>
		bool UsesDebugInfo { get; }
	}

	/// <summary>
	/// <see cref="IDecompilerOutput"/> extension methods
	/// </summary>
	public static class DecompilerOutputExtensions {
		/// <summary>
		/// Writes text and a new line
		/// </summary>
		/// <param name="output">Output</param>
		/// <param name="text">Text</param>
		/// <param name="color">Color</param>
		public static void WriteLine(this IDecompilerOutput output, string text, object color) {
			output.Write(text, color);
			output.WriteLine();
		}

		/// <summary>
		/// Writes XML documentation
		/// </summary>
		/// <param name="output">Output</param>
		/// <param name="xmlDocText">XML documentation</param>
		public static void WriteXmlDoc(this IDecompilerOutput output, string xmlDocText) {
			foreach (var info in SimpleXmlParser.Parse(xmlDocText))
				output.Write(info.Key, info.Value);
		}
	}
}
