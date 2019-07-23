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
using System.ComponentModel.Composition;
using dnSpy.Contracts.Debugger.Evaluation;
using dnSpy.Contracts.Settings;

namespace dnSpy.Debugger.Evaluation {
	[Export(typeof(IDbgLanguageServiceListener))]
	sealed class DbgLanguageServiceSettings : IDbgLanguageServiceListener {
		static readonly Guid SETTINGS_GUID = new Guid("2E163D85-E4B5-419A-A525-010DF2BEA323");

		readonly ISettingsService settingsService;

		[ImportingConstructor]
		DbgLanguageServiceSettings(ISettingsService settingsService) => this.settingsService = settingsService;

		void IDbgLanguageServiceListener.Initialize(DbgLanguageService2 dbgLanguageService) {
			Load(dbgLanguageService);
			dbgLanguageService.LanguageChanged += DbgLanguageService_LanguageChanged;
		}

		void DbgLanguageService_LanguageChanged(object? sender, DbgLanguageChangedEventArgs e) => Save((DbgLanguageService2)sender!);

		void Load(DbgLanguageService2 dbgLanguageService) {
			var rootSect = settingsService.GetOrCreateSection(SETTINGS_GUID);
			foreach (var sect in rootSect.SectionsWithName("Language")) {
				var guid = sect.Attribute<Guid?>("Guid");
				var languageName = sect.Attribute<string>("Language");
				if (guid is null || languageName is null)
					continue;
				dbgLanguageService.SetDefaultLanguageName(guid.Value, languageName);
			}
		}

		void Save(DbgLanguageService2 dbgLanguageService) {
			var rootSect = settingsService.RecreateSection(SETTINGS_GUID);
			foreach (var info in dbgLanguageService.GetLanguageInfos()) {
				var sect = rootSect.CreateSection("Language");
				sect.Attribute("Guid", info.RuntimeKindGuid);
				sect.Attribute("Language", info.CurrentLanguage);
			}
		}
	}
}
