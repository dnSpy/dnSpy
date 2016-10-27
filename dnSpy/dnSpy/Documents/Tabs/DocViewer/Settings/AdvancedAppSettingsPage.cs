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
using dnSpy.Text.Settings;

namespace dnSpy.Documents.Tabs.DocViewer.Settings {
	sealed class AdvancedAppSettingsPage : AdvancedAppSettingsPageBase {
		public override Guid ParentGuid => new Guid(AppSettingsConstants.GUID_DOCUMENT_VIEWER);
		public override Guid Guid => new Guid("6F00676C-2AB2-434E-9A44-D1D329D5CA4B");
		public override double Order => AppSettingsConstants.ORDER_DOCUMENT_VIEWER_DEFAULT_ADVANCED;

		public AdvancedAppSettingsPage(IDocumentViewerOptions options)
			: base(options) {
		}
	}
}
