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
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using dnSpy.Contracts.Files.Tabs;
using dnSpy.Contracts.Languages;
using dnSpy.Contracts.Settings.Dialog;

namespace dnSpy.Decompiler {
	[ExportAppSettingsModifiedListener(Order = AppSettingsConstants.ORDER_SETTINGS_LISTENER_DECOMPILER)]
	sealed class DecompilerAppSettingsModifiedListener : IAppSettingsModifiedListener {
		readonly IFileTabManager fileTabManager;

		[ImportingConstructor]
		DecompilerAppSettingsModifiedListener(IFileTabManager fileTabManager) {
			this.fileTabManager = fileTabManager;
		}

		public void OnSettingsModified(IAppRefreshSettings appRefreshSettings) {
			bool refreshIL = appRefreshSettings.Has(AppSettingsConstants.REDISASSEMBLE_IL_CODE);
			bool refreshILAst = appRefreshSettings.Has(AppSettingsConstants.REDECOMPILE_ILAST_CODE);
			bool refreshCSharp = appRefreshSettings.Has(AppSettingsConstants.REDECOMPILE_CSHARP_CODE);
			bool refreshVB = appRefreshSettings.Has(AppSettingsConstants.REDECOMPILE_VB_CODE);
			if (refreshILAst)
				refreshCSharp = refreshVB = true;
			if (refreshCSharp)
				refreshVB = true;

			if (refreshIL)
				RefreshCode(LanguageConstants.LANGUAGE_IL);
			if (refreshILAst)
				RefreshCode(LanguageConstants.LANGUAGE_ILAST_ILSPY);
			if (refreshCSharp)
				RefreshCode(LanguageConstants.LANGUAGE_CSHARP);
			if (refreshVB)
				RefreshCode(LanguageConstants.LANGUAGE_VB);
		}

		IEnumerable<Tuple<IFileTab, ILanguage>> LanguageTabs {
			get {
				foreach (var tab in fileTabManager.VisibleFirstTabs) {
					var langContent = tab.Content as ILanguageTabContent;
					var lang = langContent == null ? null : langContent.Language;
					if (lang != null)
						yield return Tuple.Create(tab, lang);
				}
			}
		}

		void RefreshCode(Guid guid) {
			fileTabManager.Refresh(LanguageTabs.Where(t => t.Item2.GenericGuid == guid || t.Item2.UniqueGuid == guid).Select(a => a.Item1).ToArray());
		}
	}
}
