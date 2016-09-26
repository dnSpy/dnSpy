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

using System.ComponentModel.Composition;
using dnSpy.Contracts.Documents;
using dnSpy.Contracts.Settings.Dialog;

namespace dnSpy.Documents {
	[ExportAppSettingsModifiedListener(Order = AppSettingsConstants.ORDER_SETTINGS_LISTENER_DOCUMENTMANAGER)]
	sealed class DsDocumentServiceAppSettingsModifiedListener : IAppSettingsModifiedListener {
		readonly IDsDocumentService documentService;

		[ImportingConstructor]
		DsDocumentServiceAppSettingsModifiedListener(IDsDocumentService documentService) {
			this.documentService = documentService;
		}

		public void OnSettingsModified(IAppRefreshSettings appRefreshSettings) {
			if (appRefreshSettings.Has(AppSettingsConstants.DISABLE_MMAP))
				DisableMemoryMappedIO();
		}

		void DisableMemoryMappedIO() {
			foreach (var m in documentService.GetDocuments()) {
				foreach (var f in m.GetAllChildrenAndSelf())
					MemoryMappedIOHelper.DisableMemoryMappedIO(f);
			}
		}
	}
}
