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
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using dnSpy.Contracts.Settings.Dialog;

namespace dnSpy.Text.Repl {
	[Export(typeof(IAppSettingsPageProvider))]
	sealed class AppSettingsPageProvider : IAppSettingsPageProvider {
		readonly IReplOptionsService replOptionsService;

		const int GENERAL_GUID_INC = 1;
		const int SCROLLBARS_GUID_INC = 2;
		const int TABS_GUID_INC = 3;
		const int ADVANCED_GUID_INC = 4;

		[ImportingConstructor]
		AppSettingsPageProvider(IReplOptionsService replOptionsService) {
			this.replOptionsService = replOptionsService;
		}

		public IEnumerable<AppSettingsPage> Create() {
			var options = replOptionsService.Options.OrderBy(a => a.LanguageName, StringComparer.CurrentCultureIgnoreCase).ToArray();
			if (options.Length == 0)
				yield break;

			double order = AppSettingsConstants.ORDER_REPL_LANGUAGES;
			double orderIncrement = 1.0 / options.Length;
			foreach (var option in options) {
				yield return new LanguageAppSettingsPage(option.Guid, order, option.LanguageName);
				order += orderIncrement;

				yield return new GeneralAppSettingsPage(option, IncrementGuid(option.Guid, GENERAL_GUID_INC));
				yield return new ScrollBarsAppSettingsPage(option, IncrementGuid(option.Guid, SCROLLBARS_GUID_INC));
				yield return new TabsAppSettingsPage(option, IncrementGuid(option.Guid, TABS_GUID_INC));
				yield return new AdvancedAppSettingsPage(option, IncrementGuid(option.Guid, ADVANCED_GUID_INC));
			}
		}

		static Guid IncrementGuid(Guid guid, int increment) {
			var s = guid.ToString();
			Debug.Assert(s.Length == 36);
			uint val = uint.Parse(s.Substring(36 - 8), NumberStyles.HexNumber);
			val += (uint)increment;
			return new Guid(s.Substring(0, 36 - 8) + val.ToString("X8"));
		}
	}
}
