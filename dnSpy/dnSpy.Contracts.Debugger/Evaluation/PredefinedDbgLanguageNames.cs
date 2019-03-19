/*
    Copyright (C) 2014-2019 de4dot@gmail.com

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

namespace dnSpy.Contracts.Debugger.Evaluation {
	/// <summary>
	/// Predefined language names. These aren't language display names, they're just unique language names.
	/// </summary>
	public static class PredefinedDbgLanguageNames {
		/// <summary>
		/// No language is available
		/// </summary>
		public const string None = "<no language>";

		/// <summary>
		/// C#
		/// </summary>
		public const string CSharp = "c-#-";// Not a display name so don't use "C#"

		/// <summary>
		/// Visual Basic
		/// </summary>
		public const string VisualBasic = "v-b-";// Not a display name so don't use "Visual Basic"
	}
}
