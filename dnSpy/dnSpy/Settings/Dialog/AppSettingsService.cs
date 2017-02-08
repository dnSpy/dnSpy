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
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Windows;
using dnSpy.Contracts.App;
using dnSpy.Contracts.Settings.AppearanceCategory;
using dnSpy.Contracts.Settings.Dialog;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.Text.Classification;
using dnSpy.Contracts.TreeView;
using dnSpy.Contracts.TreeView.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

namespace dnSpy.Settings.Dialog {
	[Export(typeof(IAppSettingsService))]
	sealed class AppSettingsService : IAppSettingsService {
		readonly IClassificationFormatMap classificationFormatMap;
		readonly ITextElementProvider textElementProvider;
		readonly IAppWindow appWindow;
		readonly ITreeViewService treeViewService;
		readonly ITreeViewNodeTextElementProvider treeViewNodeTextElementProvider;
		readonly Lazy<IAppSettingsPageContainer, IAppSettingsPageContainerMetadata>[] appSettingsPageContainers;
		readonly Lazy<IAppSettingsPageProvider>[] appSettingsPageProviders;
		readonly Lazy<IAppSettingsModifiedListener, IAppSettingsModifiedListenerMetadata>[] appSettingsModifiedListeners;
		Guid? lastSelectedGuid;
		ShowAppSettingsDialog showAppSettings;

#pragma warning disable 0169
		[Export]
		[Name(ContentTypes.OptionsDialogText)]
		[BaseDefinition(ContentTypes.Text)]
		static readonly ContentTypeDefinition OptionsDialogTextContentTypeDefinition;
#pragma warning restore 0169

		[ImportingConstructor]
		AppSettingsService(IClassificationFormatMapService classificationFormatMapService, ITextElementProvider textElementProvider, IAppWindow appWindow, ITreeViewService treeViewService, ITreeViewNodeTextElementProvider treeViewNodeTextElementProvider, [ImportMany] IEnumerable<Lazy<IAppSettingsPageContainer, IAppSettingsPageContainerMetadata>> appSettingsPageContainers, [ImportMany] IEnumerable<Lazy<IAppSettingsPageProvider>> appSettingsPageProviders, [ImportMany] IEnumerable<Lazy<IAppSettingsModifiedListener, IAppSettingsModifiedListenerMetadata>> appSettingsModifiedListeners) {
			classificationFormatMap = classificationFormatMapService.GetClassificationFormatMap(AppearanceCategoryConstants.UIMisc);
			this.textElementProvider = textElementProvider;
			this.appWindow = appWindow;
			this.treeViewService = treeViewService;
			this.treeViewNodeTextElementProvider = treeViewNodeTextElementProvider;
			this.appSettingsPageContainers = appSettingsPageContainers.OrderBy(a => a.Metadata.Order).ToArray();
			this.appSettingsPageProviders = appSettingsPageProviders.ToArray();
			this.appSettingsModifiedListeners = appSettingsModifiedListeners.OrderBy(a => a.Metadata.Order).ToArray();
		}

		public void Show(Window owner) => Show2(null, owner);
		public void Show(Guid guid, Window owner) => Show2(guid, owner);

		void Show2(Guid? guid, Window owner) {
			if (showAppSettings != null) {
				if (guid != null)
					showAppSettings.Select(guid.Value);
				return;
			}
			try {
				showAppSettings = new ShowAppSettingsDialog(classificationFormatMap, textElementProvider, treeViewService, treeViewNodeTextElementProvider, appSettingsPageContainers, appSettingsPageProviders, appSettingsModifiedListeners);
				showAppSettings.Show(guid ?? lastSelectedGuid, owner ?? appWindow.MainWindow);
				lastSelectedGuid = showAppSettings.LastSelectedGuid;
			}
			finally {
				showAppSettings?.Dispose();
				showAppSettings = null;
			}
		}
	}
}
