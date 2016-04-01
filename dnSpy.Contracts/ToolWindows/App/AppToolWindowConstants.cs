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

namespace dnSpy.Contracts.ToolWindows.App {
	/// <summary>
	/// Constants
	/// </summary>
	public static class AppToolWindowConstants {
		/// <summary>Order of files tool window</summary>
		public static readonly double DEFAULT_CONTENT_ORDER_LEFT_FILES = 1000;

		/// <summary>Order of analyzer tool window</summary>
		public static readonly double DEFAULT_CONTENT_ORDER_BOTTOM_ANALYZER = 10000;

		/// <summary>Order of debugger locals window</summary>
		public static readonly double DEFAULT_CONTENT_ORDER_BOTTOM_DEBUGGER_LOCALS = 20000;

		/// <summary>Order of debugger breakpoints tool window</summary>
		public static readonly double DEFAULT_CONTENT_ORDER_BOTTOM_DEBUGGER_BREAKPOINTS = 20001;

		/// <summary>Order of debugger call stack tool window</summary>
		public static readonly double DEFAULT_CONTENT_ORDER_BOTTOM_DEBUGGER_CALLSTACK = 20002;

		/// <summary>Order of debugger threads tool window</summary>
		public static readonly double DEFAULT_CONTENT_ORDER_BOTTOM_DEBUGGER_THREADS = 20003;

		/// <summary>Order of debugger exceptions tool window</summary>
		public static readonly double DEFAULT_CONTENT_ORDER_BOTTOM_DEBUGGER_EXCEPTIONS = 20004;

		/// <summary>Order of debugger modules tool window</summary>
		public static readonly double DEFAULT_CONTENT_ORDER_BOTTOM_DEBUGGER_MODULES = 20005;

		/// <summary>Order of debugger memory tool window</summary>
		public static readonly double DEFAULT_CONTENT_ORDER_BOTTOM_DEBUGGER_MEMORY = 20006;

		/// <summary>Order of C# interactive window</summary>
		public static readonly double DEFAULT_CONTENT_ORDER_BOTTOM_SCRIPTING_CSHARP = 21000;

		/// <summary>Order of Visual Basic interactive window</summary>
		public static readonly double DEFAULT_CONTENT_ORDER_BOTTOM_SCRIPTING_VISUAL_BASIC = 22000;

		/// <summary>Order of search tool window</summary>
		public static readonly double DEFAULT_CONTENT_ORDER_TOP_SEARCH = 10000;
	}
}
