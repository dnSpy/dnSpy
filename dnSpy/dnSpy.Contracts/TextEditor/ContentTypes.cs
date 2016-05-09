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

namespace dnSpy.Contracts.TextEditor {
	/// <summary>
	/// Content types
	/// </summary>
	public static class ContentTypes {
		/// <summary>
		/// Any content
		/// </summary>
		public const string ANY = "D53EB38F-3D22-42AE-A0A7-6794A005E265";

		/// <summary>
		/// Inert content
		/// </summary>
		public const string INERT = "FAD13E9E-058B-45EA-9E1E-365B7C3C2A20";

		/// <summary>
		/// Text
		/// </summary>
		public const string TEXT = "C73BA084-F0A4-451F-87A1-B95A5662397A";

		/// <summary>
		/// Plain text
		/// </summary>
		public const string PLAIN_TEXT = "A41C5B09-A9D2-4AF9-AF33-199432ABE82D";

		/// <summary>
		/// XML
		/// </summary>
		public const string XML = "70D9332A-CDF9-403C-973C-A32CF0A26230";

		/// <summary>
		/// XAML
		/// </summary>
		public const string XAML = "5092146B-D610-4544-921D-839E7B72BD19";

		/// <summary>
		/// Disassembled BAML
		/// </summary>
		public const string BAML = "3397E321-C6E7-4283-9AD0-F5A352AFA9D2";

		/// <summary>
		/// Disassembled BAML (dnSpy BAML plugin)
		/// </summary>
		public const string BAML_DNSPY = "A95E34C1-006F-4F54-B4C5-04A4EC77774F";

		/// <summary>
		/// Code
		/// </summary>
		public const string CODE = "CF24BA26-CB1C-41EC-ADC5-2F45741CD3B1";

		/// <summary>
		/// C# code
		/// </summary>
		public const string CSHARP = "5DD3CA47-12DE-4A34-A9DD-294E58CD28FF";

		/// <summary>
		/// Visual Basic code
		/// </summary>
		public const string VISUALBASIC = "5C223730-12A2-4053-A409-3E15BF2714C6";

		/// <summary>
		/// IL code
		/// </summary>
		public const string IL = "ECD2654F-E252-44FC-9698-22714C8448D8";

		/// <summary>
		/// Roslyn (C# / Visual Basic) code
		/// </summary>
		public const string ROSLYN_CODE = "A3028D64-E968-461D-BEA6-2DB8FEE37F1F";

		/// <summary>
		/// C# (Roslyn)
		/// </summary>
		public const string CSHARP_ROSLYN = "0111D4FA-C4A3-4424-A92B-04C58D2D61F4";

		/// <summary>
		/// Visual Basic (Roslyn)
		/// </summary>
		public const string VISUALBASIC_ROSLYN = "0DE41AF4-32CC-4898-9514-2DA468F57216";

		/// <summary>
		/// Decompiled code
		/// </summary>
		public const string DECOMPILED_CODE = "30B9980F-CCE4-401B-B164-CE80CAE64165";

		/// <summary>
		/// ILSpy decompiler output
		/// </summary>
		public const string DECOMPILER_ILSPY = "2E61CB2D-D553-4690-9BF7-45AD402101A3";

		/// <summary>
		/// C# (ILSpy decompiler)
		/// </summary>
		public const string CSHARP_ILSPY = "7A15270E-76F5-42E7-A3A6-5116D0E23EC4";

		/// <summary>
		/// Visual Basic (ILSpy decompiler)
		/// </summary>
		public const string VISUALBASIC_ILSPY = "B6ECF0A3-91B9-4E4E-BA9D-E7988B63129F";

		/// <summary>
		/// IL (ILSpy decompiler)
		/// </summary>
		public const string IL_ILSPY = "2438781E-BDF5-45B1-9601-D7C253D45EE1";

		/// <summary>
		/// ILAst (ILSpy decompiler)
		/// </summary>
		public const string ILAST_ILSPY = "E5ADC71D-45F4-4B69-A55A-D67C12293876";

		/// <summary>
		/// REPL
		/// </summary>
		public const string REPL = "884E6207-212C-43BD-A9DF-26B766054224";

		/// <summary>
		/// REPL (Roslyn)
		/// </summary>
		public const string REPL_ROSLYN = "3BBAB541-1D77-47CB-8671-E4BD4DA7DAE0";

		/// <summary>
		/// REPL C# (Roslyn)
		/// </summary>
		public const string REPL_CSHARP_ROSLYN = "BE367973-778C-49F0-95A0-CA1AC038E9F8";

		/// <summary>
		/// REPL Visual Basic (Roslyn)
		/// </summary>
		public const string REPL_VISUALBASIC_ROSLYN = "E601A530-7F6D-4B81-B0DE-FC6D26B16D0C";

		/// <summary>
		/// Output window
		/// </summary>
		public const string OUTPUT = "EAD38A71-11D5-4BB6-B12F-5287A1EABD51";

		/// <summary>
		/// Output window: Debug
		/// </summary>
		public const string OUTPUT_DEBUG = "A240342E-28B0-4117-BD63-65A8F6D6CA1D";

		/// <summary>
		/// About dnSpy
		/// </summary>
		public const string ABOUT_DNSPY = "EB4D03E3-E57E-48E4-9863-DB5703D5B2CE";

		/// <summary>
		/// Returns a content type or null if it's unknown
		/// </summary>
		/// <param name="extension">File extension, with or without the period</param>
		/// <returns></returns>
		public static Guid? TryGetContentTypeGuidByExtension(string extension) {
			var comparer = StringComparer.InvariantCultureIgnoreCase;
			if (comparer.Equals(extension, ".txt") || comparer.Equals(extension, "txt"))
				return new Guid(PLAIN_TEXT);
			if (comparer.Equals(extension, ".xml") || comparer.Equals(extension, "xml"))
				return new Guid(XML);
			if (comparer.Equals(extension, ".xaml") || comparer.Equals(extension, "xaml"))
				return new Guid(XAML);
			if (comparer.Equals(extension, ".cs") || comparer.Equals(extension, "cs"))
				return new Guid(CSHARP);
			if (comparer.Equals(extension, ".csx") || comparer.Equals(extension, "csx"))
				return new Guid(CSHARP);
			if (comparer.Equals(extension, ".vb") || comparer.Equals(extension, "vb"))
				return new Guid(VISUALBASIC);
			if (comparer.Equals(extension, ".vbx") || comparer.Equals(extension, "vbx"))
				return new Guid(VISUALBASIC);
			if (comparer.Equals(extension, ".il") || comparer.Equals(extension, "il"))
				return new Guid(IL);

			return null;
		}
	}
}
