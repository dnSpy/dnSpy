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
using dnSpy.Contracts.Settings.Dialog;
using dnSpy.Text.Settings;

namespace dnSpy.Output.Settings {
	sealed class ScrollBarsAppSettingsPage : ScrollBarsAppSettingsPageBase {
		public override Guid ParentGuid => new Guid(AppSettingsConstants.GUID_OUTPUT);
		public override Guid Guid => new Guid("3B1B04B9-E284-4A3B-8946-F8CADA302C08");
		public override double Order => AppSettingsConstants.ORDER_OUTPUT_DEFAULT_SCROLLBARS;

		public ScrollBarsAppSettingsPage(IOutputWindowOptions options)
			: base(options) {
		}
	}
}
