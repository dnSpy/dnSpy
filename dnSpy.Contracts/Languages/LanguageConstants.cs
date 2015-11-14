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

namespace dnSpy.Contracts.Languages {
	/// <summary>
	/// Language constants
	/// </summary>
	public static class LanguageConstants {
		/// <summary>Order of C# language</summary>
		public const double CSHARP_ORDERUI = 0;

		/// <summary>Order of VB language</summary>
		public const double VB_ORDERUI = 100;

		/// <summary>Order of IL language</summary>
		public const double IL_ORDERUI = 200;

		/// <summary>Order of C# debug languages</summary>
		public const double CSHARP_DEBUG_ORDERUI = 10000;

		/// <summary>Order of ILAst debug languages</summary>
		public const double ILAST_DEBUG_ORDERUI = 20000;
	}
}
