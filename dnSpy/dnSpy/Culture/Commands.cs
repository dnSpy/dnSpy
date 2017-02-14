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
using System.Diagnostics;
using System.Linq;
using dnSpy.Contracts.App;
using dnSpy.Contracts.Menus;
using dnSpy.Contracts.Utilities;
using dnSpy.Properties;

namespace dnSpy.Culture {
	static class Constants {
		public const string LANGUAGE_GUID = "0BA3C4A0-F861-4FAE-89FF-FE2C6808C1CB";
		public const string GROUP_LANGUAGE = "0,053B9218-1CA8-4F97-8D53-30334DE4250E";
	}

	[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_VIEW_GUID, Guid = Constants.LANGUAGE_GUID, Header = "res:LanguageCommand", Group = MenuConstants.GROUP_APP_MENU_VIEW_OPTS, Order = 10000)]
	sealed class LanguagesCommand : MenuItemBase {
		readonly ICultureService cultureService;

		[ImportingConstructor]
		LanguagesCommand(ICultureService cultureService) => this.cultureService = cultureService;

		public override bool IsVisible(IMenuItemContext context) => cultureService.HasExtraLanguages;
		public override void Execute(IMenuItemContext context) => Debug.Fail("Shouldn't execute");
	}

	[ExportMenuItem(OwnerGuid = Constants.LANGUAGE_GUID, Group = Constants.GROUP_LANGUAGE, Order = 0)]
	sealed class ShowSupportedLanguagesCommand : MenuItemBase, IMenuItemProvider {
		readonly ICultureService cultureService;

		[ImportingConstructor]
		ShowSupportedLanguagesCommand(ICultureService cultureService) => this.cultureService = cultureService;

		public IEnumerable<CreatedMenuItem> Create(IMenuItemContext context) {
			var langs = cultureService.AllLanguages.OrderBy(a => a, LanguageInfoComparer.Instance);
			foreach (var lang in langs) {
				var attr = new ExportMenuItemAttribute { Header = UIUtilities.EscapeMenuItemHeader(lang.UIName) };
				yield return new CreatedMenuItem(attr, new SwitchLanguageCommand(cultureService, lang));
			}
		}

		public override void Execute(IMenuItemContext context) => Debug.Fail("Shouldn't execute");
	}

	sealed class SwitchLanguageCommand : MenuItemBase {
		readonly ICultureService cultureService;
		readonly LanguageInfo langInfo;

		public SwitchLanguageCommand(ICultureService cultureService, LanguageInfo langInfo) {
			this.cultureService = cultureService;
			this.langInfo = langInfo;
		}

		public override bool IsChecked(IMenuItemContext context) => cultureService.Language.Equals(langInfo);

		public override void Execute(IMenuItemContext context) {
			if (!cultureService.Language.Equals(langInfo)) {
				cultureService.Language = langInfo;
				MsgBox.Instance.ShowIgnorableMessage(new Guid("778A97E0-E7F8-4965-B2A0-BB6E0281B9F9"), dnSpy_Resources.LanguageSwitchMessage);
			}
		}
	}

	sealed class LanguageInfoComparer : IComparer<LanguageInfo> {
		public static readonly LanguageInfoComparer Instance = new LanguageInfoComparer();

		public int Compare(LanguageInfo x, LanguageInfo y) {
			if (x == y)
				return 0;
			if (x == null)
				return -1;
			if (y == null)
				return 1;
			int o = ToNumber(x.Type).CompareTo(ToNumber(y.Type));
			if (o != 0)
				return o;
			if (x.Type == LanguageType.CultureInfo) {
				o = StringComparer.CurrentCultureIgnoreCase.Compare(x.CultureInfo.NativeName, y.CultureInfo.NativeName);
				if (o != 0)
					return o;
			}
			return 0;
		}

		static int ToNumber(LanguageType type) {
			switch (type) {
			case LanguageType.SystemLanguage: return 0;
			case LanguageType.CultureInfo: return 1;
			default: throw new InvalidOperationException();
			}
		}
	}
}
