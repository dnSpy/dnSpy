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
using dnSpy.Contracts.Settings.Dialog;

namespace dnSpy.Text.CodeEditor {
	sealed class LanguageAppSettingsPage : AppSettingsPage {
		public override Guid ParentGuid => new Guid(AppSettingsConstants.GUID_CODE_EDITOR);
		public override Guid Guid => guid;
		public override double Order => order;
		public override string Title => title;
		public override object UIObject => null;
		public override void OnApply() { }
		readonly Guid guid;
		readonly double order;
		readonly string title;
		public LanguageAppSettingsPage(Guid guid, double order, string title) {
			this.guid = guid;
			this.order = order;
			this.title = title;
		}
	}
}
