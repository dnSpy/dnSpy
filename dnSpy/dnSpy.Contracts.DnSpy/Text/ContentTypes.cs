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
		public const string Any = "any";

		/// <summary>
		/// Inert content
		/// </summary>
		public const string Inert = "inert";

		/// <summary>
		/// Text
		/// </summary>
		public const string Text = "text";

		/// <summary>
		/// Plain text
		/// </summary>
		public const string PlainText = "plaintext";

		/// <summary>
		/// XML
		/// </summary>
		public const string Xml = "xml";

		/// <summary>
		/// XAML
		/// </summary>
		public const string Xaml = "XAML";

		/// <summary>
		/// Disassembled BAML
		/// </summary>
		public const string Baml = "BAML";

		/// <summary>
		/// Disassembled BAML (dnSpy BAML extension)
		/// </summary>
		public const string BamlDnSpy = "BAML-dnSpy";

		/// <summary>
		/// Code
		/// </summary>
		public const string Code = "code";

		/// <summary>
		/// C# code
		/// </summary>
		public const string CSharp = "C#-code";

		/// <summary>
		/// Visual Basic code
		/// </summary>
		public const string VisualBasic = "VB-code";

		/// <summary>
		/// IL code
		/// </summary>
		public const string IL = "MSIL";

		/// <summary>
		/// Roslyn (C# / Visual Basic) code
		/// </summary>
		public const string RoslynCode = "Roslyn Languages";

		/// <summary>
		/// C# (Roslyn)
		/// </summary>
		public const string CSharpRoslyn = "CSharp";

		/// <summary>
		/// Visual Basic (Roslyn)
		/// </summary>
		public const string VisualBasicRoslyn = "Basic";

		/// <summary>
		/// Decompiled code
		/// </summary>
		public const string DecompiledCode = "Decompiled Code";

		/// <summary>
		/// REPL
		/// </summary>
		public const string Repl = "REPL";

		/// <summary>
		/// REPL (Roslyn)
		/// </summary>
		public const string ReplRoslyn = "REPL Roslyn";

		/// <summary>
		/// REPL C# (Roslyn)
		/// </summary>
		public const string ReplCSharpRoslyn = "REPL C# Roslyn";

		/// <summary>
		/// REPL Visual Basic (Roslyn)
		/// </summary>
		public const string ReplVisualBasicRoslyn = "REPL VB Roslyn";

		/// <summary>
		/// Output window
		/// </summary>
		public const string Output = "Output";

		/// <summary>
		/// Output window: Debug
		/// </summary>
		public const string OutputDebug = "DebugOutput";

		/// <summary>
		/// About dnSpy
		/// </summary>
		public const string AboutDnSpy = "About dnSpy";

		/// <summary>
		/// Returns a content type or null if it's unknown
		/// </summary>
		/// <param name="extension">File extension, with or without the period</param>
		/// <returns></returns>
		public static string TryGetContentTypeStringByExtension(string extension) {
			var comparer = StringComparer.InvariantCultureIgnoreCase;
			if (comparer.Equals(extension, ".txt") || comparer.Equals(extension, "txt"))
				return PlainText;
			if (comparer.Equals(extension, ".xml") || comparer.Equals(extension, "xml"))
				return Xml;
			if (comparer.Equals(extension, ".xaml") || comparer.Equals(extension, "xaml"))
				return Xaml;
			if (comparer.Equals(extension, ".cs") || comparer.Equals(extension, "cs"))
				return CSharp;
			if (comparer.Equals(extension, ".csx") || comparer.Equals(extension, "csx"))
				return CSharp;
			if (comparer.Equals(extension, ".vb") || comparer.Equals(extension, "vb"))
				return VisualBasic;
			if (comparer.Equals(extension, ".vbx") || comparer.Equals(extension, "vbx"))
				return VisualBasic;
			if (comparer.Equals(extension, ".il") || comparer.Equals(extension, "il"))
				return IL;

			return null;
		}
	}
}
