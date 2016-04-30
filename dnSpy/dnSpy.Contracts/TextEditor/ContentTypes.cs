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
		/// Output window: Debug
		/// </summary>
		public static readonly Guid OUTPUT_DEBUG = new Guid("A240342E-28B0-4117-BD63-65A8F6D6CA1D");

		/// <summary>
		/// C# (Roslyn)
		/// </summary>
		public static readonly Guid CSHARP_ROSLYN = new Guid("0111D4FA-C4A3-4424-A92B-04C58D2D61F4");

		/// <summary>
		/// Visual Basic (Roslyn)
		/// </summary>
		public static readonly Guid VISUALBASIC_ROSLYN = new Guid("0DE41AF4-32CC-4898-9514-2DA468F57216");
	}
}
