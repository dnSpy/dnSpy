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

namespace dnSpy.Contracts.Languages {
	/// <summary>
	/// Language constants
	/// </summary>
	public static class LanguageConstants {
		/// <summary>Order of C# language</summary>
		public static readonly double CSHARP_ILSPY_ORDERUI = 0;

		/// <summary>Order of VB language</summary>
		public static readonly double VB_ILSPY_ORDERUI = 100;

		/// <summary>Order of IL language</summary>
		public static readonly double IL_ILSPY_ORDERUI = 200;

		/// <summary>Order of C# debug languages</summary>
		public static readonly double CSHARP_ILSPY_DEBUG_ORDERUI = 10000;

		/// <summary>Order of ILAst debug languages</summary>
		public static readonly double ILAST_ILSPY_DEBUG_ORDERUI = 20000;

		/// <summary>IL language</summary>
		public static readonly Guid LANGUAGE_IL = new Guid("9EF276FD-3293-42A4-B48A-1D6A69086B3D");

		/// <summary>IL language (ILSpy)</summary>
		public static readonly Guid LANGUAGE_IL_ILSPY = new Guid("A4F35508-691F-4BD0-B74D-D5D5D1D0E8E6");

		/// <summary>ILAst language (ILSpy)</summary>
		public static readonly Guid LANGUAGE_ILAST_ILSPY = new Guid("CA52A515-12AE-4182-BC88-81ED037C3D32");

		/// <summary>C# language</summary>
		public static readonly Guid LANGUAGE_CSHARP = new Guid("F5A318D4-4B2A-48D2-AE33-F4D2B1EFF4B0");

		/// <summary>C# language (ILSpy)</summary>
		public static readonly Guid LANGUAGE_CSHARP_ILSPY = new Guid("4162DADA-67C3-4DE4-A5F3-6552C8353ECE");

		/// <summary>VB language</summary>
		public static readonly Guid LANGUAGE_VB = new Guid("B6849618-8239-4FBB-8DFF-D45EB023C193");

		/// <summary>VB language (ILSpy)</summary>
		public static readonly Guid LANGUAGE_VB_ILSPY = new Guid("BBA40092-76B2-4184-8E81-0F1E3ED14E72");

		/// <summary>Name of IL language returned by <see cref="ILanguage.GenericNameUI"/></summary>
		public static readonly string GENERIC_NAMEUI_IL = "IL";

		/// <summary>Name of C# language returned by <see cref="ILanguage.GenericNameUI"/></summary>
		public static readonly string GENERIC_NAMEUI_CSHARP = "C#";

		/// <summary>Name of VB language returned by <see cref="ILanguage.GenericNameUI"/></summary>
		public static readonly string GENERIC_NAMEUI_VB= "VB";

		/// <summary>
		/// Order of ILSpy C#/VB decompiler settings
		/// </summary>
		public const double ORDER_DECOMPILER_SETTINGS_ILSPY_CSHARP = 10000;

		/// <summary>
		/// Order of ILSpy IL disassembler settings
		/// </summary>
		public const double ORDER_DECOMPILER_SETTINGS_ILSPY_IL = 20000;
	}
}
