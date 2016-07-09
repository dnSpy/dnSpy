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
	/// Content types
	/// </summary>
	public static class ContentTypes {
		/// <summary>
		/// Any content
		/// </summary>
		public const string ANY = "any";

		/// <summary>
		/// Inert content
		/// </summary>
		public const string INERT = "inert";

		/// <summary>
		/// Text
		/// </summary>
		public const string TEXT = "text";

		/// <summary>
		/// Plain text
		/// </summary>
		public const string PLAIN_TEXT = "plaintext";

		/// <summary>
		/// XML
		/// </summary>
		public const string XML = "xml";

		/// <summary>
		/// XAML
		/// </summary>
		public const string XAML = "XAML";

		/// <summary>
		/// Disassembled BAML
		/// </summary>
		public const string BAML = "BAML";

		/// <summary>
		/// Disassembled BAML (dnSpy BAML plugin)
		/// </summary>
		public const string BAML_DNSPY = "BAML-dnSpy";

		/// <summary>
		/// Code
		/// </summary>
		public const string CODE = "code";

		/// <summary>
		/// C# code
		/// </summary>
		public const string CSHARP = "C#-code";

		/// <summary>
		/// Visual Basic code
		/// </summary>
		public const string VISUALBASIC = "VB-code";

		/// <summary>
		/// IL code
		/// </summary>
		public const string IL = "MSIL";

		/// <summary>
		/// Roslyn (C# / Visual Basic) code
		/// </summary>
		public const string ROSLYN_CODE = "Roslyn Languages";

		/// <summary>
		/// C# (Roslyn)
		/// </summary>
		public const string CSHARP_ROSLYN = "CSharp";

		/// <summary>
		/// Visual Basic (Roslyn)
		/// </summary>
		public const string VISUALBASIC_ROSLYN = "Basic";

		/// <summary>
		/// Decompiled code
		/// </summary>
		public const string DECOMPILED_CODE = "Decompiled Code";

		/// <summary>
		/// REPL
		/// </summary>
		public const string REPL = "REPL";

		/// <summary>
		/// REPL (Roslyn)
		/// </summary>
		public const string REPL_ROSLYN = "REPL Roslyn";

		/// <summary>
		/// REPL C# (Roslyn)
		/// </summary>
		public const string REPL_CSHARP_ROSLYN = "REPL C# Roslyn";

		/// <summary>
		/// REPL Visual Basic (Roslyn)
		/// </summary>
		public const string REPL_VISUALBASIC_ROSLYN = "REPL VB Roslyn";

		/// <summary>
		/// Output window
		/// </summary>
		public const string OUTPUT = "Output";

		/// <summary>
		/// Output window: Debug
		/// </summary>
		public const string OUTPUT_DEBUG = "DebugOutput";

		/// <summary>
		/// About dnSpy
		/// </summary>
		public const string ABOUT_DNSPY = "About dnSpy";

		/// <summary>
		/// Returns a content type or null if it's unknown
		/// </summary>
		/// <param name="extension">File extension, with or without the period</param>
		/// <returns></returns>
		public static string TryGetContentTypeStringByExtension(string extension) {
			var comparer = StringComparer.InvariantCultureIgnoreCase;
			if (comparer.Equals(extension, ".txt") || comparer.Equals(extension, "txt"))
				return PLAIN_TEXT;
			if (comparer.Equals(extension, ".xml") || comparer.Equals(extension, "xml"))
				return XML;
			if (comparer.Equals(extension, ".xaml") || comparer.Equals(extension, "xaml"))
				return XAML;
			if (comparer.Equals(extension, ".cs") || comparer.Equals(extension, "cs"))
				return CSHARP;
			if (comparer.Equals(extension, ".csx") || comparer.Equals(extension, "csx"))
				return CSHARP;
			if (comparer.Equals(extension, ".vb") || comparer.Equals(extension, "vb"))
				return VISUALBASIC;
			if (comparer.Equals(extension, ".vbx") || comparer.Equals(extension, "vbx"))
				return VISUALBASIC;
			if (comparer.Equals(extension, ".il") || comparer.Equals(extension, "il"))
				return IL;

			return null;
		}
	}
}
