/*
    Copyright (C) 2014-2017 de4dot@gmail.com

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
using dnSpy.Contracts.Debugger.Breakpoints.Code;
using dnSpy.Contracts.Images;

namespace dnSpy.Debugger.Breakpoints.Code {
	enum BreakpointKind {
		BreakpointDisabled,
		BreakpointEnabled,
		AdvancedBreakpointDisabled,
		AdvancedBreakpointEnabled,
		TracepointDisabled,
		TracepointEnabled,
		AdvancedTracepointDisabled,
		AdvancedTracepointEnabled,

		Last,
	}

	static class BreakpointImageUtilities {
		static bool IsAdvanced(ref DbgCodeBreakpointSettings settings) =>
			settings.Condition != null || settings.HitCount != null || settings.Filter != null;

		public static BreakpointKind GetBreakpointKind(DbgCodeBreakpoint breakpoint) {
			var settings = breakpoint.Settings;
			return GetBreakpointKind(ref settings);
		}

		public static BreakpointKind GetBreakpointKind(ref DbgCodeBreakpointSettings settings) {
			bool isAdvanced = IsAdvanced(ref settings);
			if (settings.Trace == null || !settings.Trace.Value.Continue) {
				if (isAdvanced)
					return settings.IsEnabled ? BreakpointKind.AdvancedBreakpointEnabled : BreakpointKind.AdvancedBreakpointDisabled;
				return settings.IsEnabled ? BreakpointKind.BreakpointEnabled : BreakpointKind.BreakpointDisabled;
			}
			else {
				if (isAdvanced)
					return settings.IsEnabled ? BreakpointKind.AdvancedTracepointEnabled : BreakpointKind.AdvancedTracepointDisabled;
				return settings.IsEnabled ? BreakpointKind.TracepointEnabled : BreakpointKind.TracepointDisabled;
			}
		}

		public static ImageReference GetImage(ref DbgCodeBreakpointSettings settings) => GetImage(GetBreakpointKind(ref settings));

		public static ImageReference GetImage(BreakpointKind type) {
			switch (type) {
			case BreakpointKind.BreakpointDisabled:			return DsImages.BreakpointDisabled;
			case BreakpointKind.BreakpointEnabled:			return DsImages.BreakpointEnabled;
			case BreakpointKind.AdvancedBreakpointDisabled:	return DsImages.AdvancedBreakpointDisabled;
			case BreakpointKind.AdvancedBreakpointEnabled:	return DsImages.AdvancedBreakpointEnabled;
			case BreakpointKind.TracepointDisabled:			return DsImages.TracepointDisabled;
			case BreakpointKind.TracepointEnabled:			return DsImages.TracepointEnabled;
			case BreakpointKind.AdvancedTracepointDisabled:	return DsImages.AdvancedTracepointDisabled;
			case BreakpointKind.AdvancedTracepointEnabled:	return DsImages.AdvancedTracepointEnabled;
			default: throw new InvalidOperationException();
			}
		}
	}
}
