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
using dnSpy.Contracts.Settings.Dialog;
using dnSpy.Text.Settings;

namespace dnSpy.Text.Repl {
	sealed class ScrollBarsAppSettingsPage : ScrollBarsAppSettingsPageBase {
		public override Guid ParentGuid => options.Guid;
		public override Guid Guid => guid;
		public override double Order => AppSettingsConstants.ORDER_REPL_LANGUAGES_SCROLLBARS;
		readonly Guid guid;
		readonly IReplOptions options;

		public ScrollBarsAppSettingsPage(IReplOptions options, Guid guid)
			: base(options) {
			this.options = options;
			this.guid = guid;
		}
	}
}
