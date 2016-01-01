/*
    Copyright (C) 2014-2015 de4dot@gmail.com

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
		public static readonly double CSHARP_ORDERUI = 0;

		/// <summary>Order of VB language</summary>
		public static readonly double VB_ORDERUI = 100;

		/// <summary>Order of IL language</summary>
		public static readonly double IL_ORDERUI = 200;

		/// <summary>Order of C# debug languages</summary>
		public static readonly double CSHARP_DEBUG_ORDERUI = 10000;

		/// <summary>Order of ILAst debug languages</summary>
		public static readonly double ILAST_DEBUG_ORDERUI = 20000;

		/// <summary>IL language</summary>
		public static readonly Guid LANGUAGE_IL = new Guid("9EF276FD-3293-42A4-B48A-1D6A69086B3D");

		/// <summary>ILAst language</summary>
		public static readonly Guid LANGUAGE_ILAST = new Guid("CA52A515-12AE-4182-BC88-81ED037C3D32");

		/// <summary>C# language</summary>
		public static readonly Guid LANGUAGE_CSHARP = new Guid("F5A318D4-4B2A-48D2-AE33-F4D2B1EFF4B0");

		/// <summary>VB language</summary>
		public static readonly Guid LANGUAGE_VB = new Guid("B6849618-8239-4FBB-8DFF-D45EB023C193");
	}
}
