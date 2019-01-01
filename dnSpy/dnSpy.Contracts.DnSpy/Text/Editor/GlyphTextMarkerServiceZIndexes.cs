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

namespace dnSpy.Contracts.Text.Editor {
	/// <summary>
	/// <see cref="IGlyphTextMarkerService"/> Z-indexes
	/// </summary>
	public static class GlyphTextMarkerServiceZIndexes {
		/// <summary>
		/// Z-index of disabled bookmarks
		/// </summary>
		public const int DisabledBookmark = 990;

		/// <summary>
		/// Z-index of enabled bookmarks
		/// </summary>
		public const int Bookmark = 1000;

		/// <summary>
		/// (Debugger) Z-index of disabled breakpoints
		/// </summary>
		public const int DisabledBreakpoint = 2000;

		/// <summary>
		/// (Debugger) Z-index of advanced disabled breakpoints
		/// </summary>
		public const int DisabledAdvancedBreakpoint = 2010;

		/// <summary>
		/// (Debugger) Z-index of enabled breakpoints
		/// </summary>
		public const int EnabledBreakpoint = 2500;

		/// <summary>
		/// (Debugger) Z-index of advanced enabled breakpoints
		/// </summary>
		public const int EnabledAdvancedBreakpoint = 2530;

		/// <summary>
		/// (Debugger) Z-index of breakpoints with warnings
		/// </summary>
		public const int BreakpointWarning = 2540;

		/// <summary>
		/// (Debugger) Z-index of breakpoints with errors
		/// </summary>
		public const int BreakpointError = 2550;

		/// <summary>
		/// (Debugger) Z-index of advanced breakpoints with warnings
		/// </summary>
		public const int AdvancedBreakpointWarning = 2560;

		/// <summary>
		/// (Debugger) Z-index of advanced breakpoints with errors
		/// </summary>
		public const int AdvancedBreakpointError = 2570;

		/// <summary>
		/// (Debugger) Z-index of disabled tracepionts
		/// </summary>
		public const int DisabledTracepoint = 2600;

		/// <summary>
		/// (Debugger) Z-index of advanced disabled tracepoints
		/// </summary>
		public const int DisabledAdvancedTracepoint = 2610;

		/// <summary>
		/// (Debugger) Z-index of enabled tracepoints
		/// </summary>
		public const int EnabledTracepoint = 2620;

		/// <summary>
		/// (Debugger) Z-index of advanced enabled tracepoints
		/// </summary>
		public const int EnabledAdvancedTracepoint = 2630;

		/// <summary>
		/// (Debugger) Z-index of tracepoints with warnings
		/// </summary>
		public const int TracepointWarning = 2640;

		/// <summary>
		/// (Debugger) Z-index of tracepoints with errors
		/// </summary>
		public const int TracepointError = 2650;

		/// <summary>
		/// (Debugger) Z-index of advanced tracepoints with warnings
		/// </summary>
		public const int AdvancedTracepointWarning = 2660;

		/// <summary>
		/// (Debugger) Z-index of advanced tracepoints with errors
		/// </summary>
		public const int AdvancedTracepointError = 2670;

		/// <summary>
		/// (Debugger) Z-index of current statement
		/// </summary>
		public const int CurrentStatement = 3000;

		/// <summary>
		/// (Debugger) Z-index of return statement
		/// </summary>
		public const int ReturnStatement = 4000;
	}
}
