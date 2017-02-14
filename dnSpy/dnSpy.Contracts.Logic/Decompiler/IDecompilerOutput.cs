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

using System;
using dnSpy.Contracts.Text;

namespace dnSpy.Contracts.Decompiler {
	/// <summary>
	/// Interface used by decompilers to write text
	/// </summary>
	public interface IDecompilerOutput {
		/// <summary>
		/// Gets the total number of written characters
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
		void IncreaseIndent();

		/// <summary>
		/// Decrements the indentation level. Nothing is added to the output stream.
		/// </summary>
		void DecreaseIndent();

		/// <summary>
		/// Writes a new line without writing any indentation
		/// </summary>
		void WriteLine();

		/// <summary>
		/// Writes text and color. The text will be indented if needed.
		/// </summary>
		/// <param name="text">Text</param>
		/// <param name="color">Color, eg. <see cref="BoxedTextColor.Keyword"/></param>
		void Write(string text, object color);

		/// <summary>
		/// Writes text and color. The text will be indented if needed.
		/// </summary>
		/// <param name="text">Text</param>
		/// <param name="index">Index in <paramref name="text"/></param>
		/// <param name="length">Number of characters to write</param>
		/// <param name="color">Color, eg. <see cref="BoxedTextColor.Keyword"/></param>
		void Write(string text, int index, int length, object color);

		/// <summary>
		/// Writes text, color and a reference. The text will be indented if needed.
		/// </summary>
		/// <param name="text">Text</param>
		/// <param name="reference">Reference</param>
		/// <param name="flags">Flags</param>
		/// <param name="color">Color, eg. <see cref="BoxedTextColor.Keyword"/></param>
		void Write(string text, object reference, DecompilerReferenceFlags flags, object color);

		/// <summary>
		/// Writes text, color and a reference. The text will be indented if needed.
		/// </summary>
		/// <param name="text">Text</param>
		/// <param name="index">Index in <paramref name="text"/></param>
		/// <param name="length">Number of characters to write</param>
		/// <param name="reference">Reference</param>
		/// <param name="flags">Flags</param>
		/// <param name="color">Color, eg. <see cref="BoxedTextColor.Keyword"/></param>
		void Write(string text, int index, int length, object reference, DecompilerReferenceFlags flags, object color);

		/// <summary>
		/// Adds custom data to a list
		/// </summary>
		/// <typeparam name="TData">Type of data to store</typeparam>
		/// <param name="id">Key, eg., <see cref="PredefinedCustomDataIds.DebugInfo"/></param>
		/// <param name="data">Data to add. If a span is needed, see <see cref="TextSpanData{TData}"/></param>
		void AddCustomData<TData>(string id, TData data);

		/// <summary>
		/// true if custom data added by <see cref="AddCustomData{TData}(string, TData)"/> is used
		/// and isn't ignored. If this is false, <see cref="AddCustomData{TData}(string, TData)"/> doesn't
		/// have to be called.
		/// </summary>
		bool UsesCustomData { get; }
	}

	/// <summary>
	/// <see cref="IDecompilerOutput"/> extension methods
	/// </summary>
	public static class DecompilerOutputExtensions {
		/// <summary>
		/// Adds debug info to the custom data collection, see also <see cref="IDecompilerOutput.UsesCustomData"/>
		/// </summary>
		/// <param name="output">Output</param>
		/// <param name="methodDebugInfo">Debug info</param>
		public static void AddDebugInfo(this IDecompilerOutput output, MethodDebugInfo methodDebugInfo) {
			if (methodDebugInfo == null)
				throw new ArgumentNullException(nameof(methodDebugInfo));
			output.AddCustomData(PredefinedCustomDataIds.DebugInfo, methodDebugInfo);
		}

		/// <summary>
		/// Adds a <see cref="SpanReference"/>
		/// </summary>
		/// <param name="output">Output</param>
		/// <param name="spanReference">Data</param>
		public static void AddSpanReference(this IDecompilerOutput output, SpanReference spanReference) =>
			output.AddCustomData(PredefinedCustomDataIds.SpanReference, spanReference);

		/// <summary>
		/// Adds a <see cref="SpanReference"/>
		/// </summary>
		/// <param name="output">Output</param>
		/// <param name="reference">Reference</param>
		/// <param name="start">Start position</param>
		/// <param name="end">End position</param>
		/// <param name="id">Reference id or null, eg. <see cref="PredefinedSpanReferenceIds.HighlightRelatedKeywords"/></param>
		public static void AddSpanReference(this IDecompilerOutput output, object reference, int start, int end, string id) =>
			output.AddCustomData(PredefinedCustomDataIds.SpanReference, new SpanReference(reference, TextSpan.FromBounds(start, end), id));

		/// <summary>
		/// Adds a <see cref="CodeBracesRange"/>
		/// </summary>
		/// <param name="output">Output</param>
		/// <param name="range">Range</param>
		public static void AddCodeBracesRange(this IDecompilerOutput output, CodeBracesRange range) =>
			output.AddCustomData(PredefinedCustomDataIds.CodeBracesRange, range);

		/// <summary>
		/// Adds a <see cref="CodeBracesRange"/>
		/// </summary>
		/// <param name="output">Output</param>
		/// <param name="start">Start span</param>
		/// <param name="end">End span</param>
		/// <param name="flags">Flags</param>
		public static void AddBracePair(this IDecompilerOutput output, TextSpan start, TextSpan end, CodeBracesRangeFlags flags) =>
			output.AddCustomData(PredefinedCustomDataIds.CodeBracesRange, new CodeBracesRange(start, end, flags));

		/// <summary>
		/// Adds a <see cref="LineSeparator"/>
		/// </summary>
		/// <param name="output">Output</param>
		/// <param name="position">Position of the line that gets a line separator</param>
		public static void AddLineSeparator(this IDecompilerOutput output, int position) =>
			output.AddCustomData(PredefinedCustomDataIds.LineSeparator, new LineSeparator(position));

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
				output.Write(info.text, info.color);
		}
	}
}
