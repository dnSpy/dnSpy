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
	enum BreakpointImageType {
		BreakpointDisabled,
		BreakpointEnabled,
		AdvancedBreakpointDisabled,
		AdvancedBreakpointEnabled,
		TracepointDisabled,
		TracepointEnabled,
		AdvancedTracepointDisabled,
		AdvancedTracepointEnabled,
	}

	static class BreakpointImageUtilities {
		static bool IsAdvanced(ref DbgCodeBreakpointSettings settings) =>
			settings.Condition != null || settings.HitCount != null || settings.Filter != null;

		public static BreakpointImageType GetImageType(ref DbgCodeBreakpointSettings settings) {
			bool isAdvanced = IsAdvanced(ref settings);
			if (settings.Trace == null) {
				if (isAdvanced)
					return settings.IsEnabled ? BreakpointImageType.AdvancedBreakpointEnabled : BreakpointImageType.AdvancedBreakpointDisabled;
				return settings.IsEnabled ? BreakpointImageType.BreakpointEnabled : BreakpointImageType.BreakpointDisabled;
			}
			else {
				if (isAdvanced)
					return settings.IsEnabled ? BreakpointImageType.AdvancedTracepointEnabled : BreakpointImageType.AdvancedTracepointDisabled;
				return settings.IsEnabled ? BreakpointImageType.TracepointEnabled : BreakpointImageType.TracepointDisabled;
			}
		}

		public static ImageReference GetImage(ref DbgCodeBreakpointSettings settings) => GetImage(GetImageType(ref settings));

		public static ImageReference GetImage(BreakpointImageType type) {
			switch (type) {
			case BreakpointImageType.BreakpointDisabled:			return DsImages.BreakpointDisabled;
			case BreakpointImageType.BreakpointEnabled:				return DsImages.BreakpointEnabled;
			case BreakpointImageType.AdvancedBreakpointDisabled:	return DsImages.AdvancedBreakpointDisabled;
			case BreakpointImageType.AdvancedBreakpointEnabled:		return DsImages.AdvancedBreakpointEnabled;
			case BreakpointImageType.TracepointDisabled:			return DsImages.TracepointDisabled;
			case BreakpointImageType.TracepointEnabled:				return DsImages.TracepointEnabled;
			case BreakpointImageType.AdvancedTracepointDisabled:	return DsImages.AdvancedTracepointDisabled;
			case BreakpointImageType.AdvancedTracepointEnabled:		return DsImages.AdvancedTracepointEnabled;
			default: throw new InvalidOperationException();
			}
		}
	}
}
