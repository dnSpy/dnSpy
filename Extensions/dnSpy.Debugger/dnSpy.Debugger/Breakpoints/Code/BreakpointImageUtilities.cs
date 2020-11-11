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

using System;
using System.Diagnostics;
using dnSpy.Contracts.Debugger.Breakpoints.Code;
using dnSpy.Contracts.Images;

namespace dnSpy.Debugger.Breakpoints.Code {
	enum BreakpointKind {
		BreakpointDisabled,
		BreakpointEnabled,
		AdvancedBreakpointDisabled,
		AdvancedBreakpointEnabled,
		BreakpointWarning,
		BreakpointError,
		AdvancedBreakpointWarning,
		AdvancedBreakpointError,
		TracepointDisabled,
		TracepointEnabled,
		AdvancedTracepointDisabled,
		AdvancedTracepointEnabled,
		TracepointWarning,
		TracepointError,
		AdvancedTracepointWarning,
		AdvancedTracepointError,

		Last,
	}

	static class BreakpointImageUtilities {
		public static BreakpointKind GetBreakpointKind(DbgCodeBreakpoint breakpoint) {
			var settings = breakpoint.Settings;
			bool isAdvanced = settings.Condition is not null || settings.HitCount is not null || settings.Filter is not null;
			var msg = breakpoint.BoundBreakpointsMessage;
			if (settings.Trace is null || !settings.Trace.Value.Continue) {
				switch (msg.Severity) {
				case DbgBoundCodeBreakpointSeverity.None:		break;
				case DbgBoundCodeBreakpointSeverity.Warning:	return isAdvanced ? BreakpointKind.AdvancedBreakpointWarning : BreakpointKind.BreakpointWarning;
				case DbgBoundCodeBreakpointSeverity.Error:		return isAdvanced ? BreakpointKind.AdvancedBreakpointError : BreakpointKind.BreakpointError;
				default:
					Debug.Fail($"Unknown severity: {msg.Severity}");
					break;
				}
				if (isAdvanced)
					return settings.IsEnabled ? BreakpointKind.AdvancedBreakpointEnabled : BreakpointKind.AdvancedBreakpointDisabled;
				return settings.IsEnabled ? BreakpointKind.BreakpointEnabled : BreakpointKind.BreakpointDisabled;
			}
			else {
				switch (msg.Severity) {
				case DbgBoundCodeBreakpointSeverity.None:		break;
				case DbgBoundCodeBreakpointSeverity.Warning:	return isAdvanced ? BreakpointKind.AdvancedTracepointWarning : BreakpointKind.TracepointWarning;
				case DbgBoundCodeBreakpointSeverity.Error:		return isAdvanced ? BreakpointKind.AdvancedTracepointError : BreakpointKind.TracepointError;
				default:
					Debug.Fail($"Unknown severity: {msg.Severity}");
					break;
				}
				if (isAdvanced)
					return settings.IsEnabled ? BreakpointKind.AdvancedTracepointEnabled : BreakpointKind.AdvancedTracepointDisabled;
				return settings.IsEnabled ? BreakpointKind.TracepointEnabled : BreakpointKind.TracepointDisabled;
			}
		}

		public static ImageReference GetImage(BreakpointKind type) {
			switch (type) {
			case BreakpointKind.BreakpointDisabled:			return DsImages.BreakpointDisabled;
			case BreakpointKind.BreakpointEnabled:			return DsImages.BreakpointEnabled;
			case BreakpointKind.AdvancedBreakpointDisabled:	return DsImages.AdvancedBreakpointDisabled;
			case BreakpointKind.AdvancedBreakpointEnabled:	return DsImages.AdvancedBreakpointEnabled;
			case BreakpointKind.BreakpointWarning:			return DsImages.BreakpointWarning;
			case BreakpointKind.BreakpointError:			return DsImages.BreakpointError;
			case BreakpointKind.AdvancedBreakpointWarning:	return DsImages.BreakpointWarning;
			case BreakpointKind.AdvancedBreakpointError:	return DsImages.BreakpointError;
			case BreakpointKind.TracepointDisabled:			return DsImages.TracepointDisabled;
			case BreakpointKind.TracepointEnabled:			return DsImages.TracepointEnabled;
			case BreakpointKind.AdvancedTracepointDisabled:	return DsImages.AdvancedTracepointDisabled;
			case BreakpointKind.AdvancedTracepointEnabled:	return DsImages.AdvancedTracepointEnabled;
			case BreakpointKind.TracepointWarning:			return DsImages.TracepointWarning;
			case BreakpointKind.TracepointError:			return DsImages.TracepointError;
			case BreakpointKind.AdvancedTracepointWarning:	return DsImages.TracepointWarning;
			case BreakpointKind.AdvancedTracepointError:	return DsImages.TracepointError;
			default: throw new InvalidOperationException();
			}
		}
	}
}
