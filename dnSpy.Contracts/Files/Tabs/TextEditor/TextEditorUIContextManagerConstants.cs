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

namespace dnSpy.Contracts.Files.Tabs.TextEditor {
	/// <summary>
	/// Constants
	/// </summary>
	public static class TextEditorUIContextManagerConstants {
		/// <summary>Default order</summary>
		public const double ORDER_DEFAULT = double.MaxValue;

		/// <summary>Debugger: create code mappings</summary>
		public static readonly double ORDER_DEBUGGER_CODEMAPPINGSCREATOR = 1000;

		/// <summary>AsmEditor: create code mappings</summary>
		public static readonly double ORDER_ASMEDITOR_CODEMAPPINGSCREATOR = 2000;

		/// <summary>Debugger: call stack</summary>
		public static readonly double ORDER_DEBUGGER_CALLSTACK = 2000;

		/// <summary>Debugger: locals (<c>MethodLocalProvider</c>)</summary>
		public static readonly double ORDER_DEBUGGER_METHODLOCALPROVIDER = 3000;

		/// <summary>Text marker serivce</summary>
		public static readonly double ORDER_TEXTMARKERSERVICE = 1000000;
	}
}
