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
using System.Threading;

namespace dnSpy.Languages.ILSpy.Settings {
	[Export, PartCreationPolicy(CreationPolicy.Shared)]
	sealed class LanguageSettingsManager {
		/// <summary>
		/// Should only be used indirectly by dnSpy.Console.exe
		/// </summary>
		public static LanguageSettingsManager __Instance_DONT_USE {
			get {
				if (__instance_DONT_USE == null)
					Interlocked.CompareExchange(ref __instance_DONT_USE, new LanguageSettingsManager(), null);
				return __instance_DONT_USE;
			}
		}
		static LanguageSettingsManager __instance_DONT_USE;

		LanguageSettingsManager() {
			this.languageDecompilerSettings = new LanguageDecompilerSettings();
			this.ilLanguageDecompilerSettings = new ILLanguageDecompilerSettings();
		}

		[ImportingConstructor]
		LanguageSettingsManager(DecompilerSettingsImpl decompilerSettings, ILSettingsImpl ilSettings) {
			this.languageDecompilerSettings = new LanguageDecompilerSettings(decompilerSettings);
			this.ilLanguageDecompilerSettings = new ILLanguageDecompilerSettings(ilSettings);
		}

		public LanguageDecompilerSettings LanguageDecompilerSettings {
			get { return languageDecompilerSettings; }
		}
		readonly LanguageDecompilerSettings languageDecompilerSettings;

		public ILLanguageDecompilerSettings ILLanguageDecompilerSettings {
			get { return ilLanguageDecompilerSettings; }
		}
		readonly ILLanguageDecompilerSettings ilLanguageDecompilerSettings;

#if DEBUG
		public ILAstLanguageDecompilerSettings ILAstLanguageDecompilerSettings {
			get { return ilAstLanguageDecompilerSettings; }
		}
		readonly ILAstLanguageDecompilerSettings ilAstLanguageDecompilerSettings = new ILAstLanguageDecompilerSettings();
#endif
	}
}
