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
using System.IO;
using System.Linq;
using System.Threading;
using dnSpy.Contracts.App;

namespace dnSpy.Culture {
	interface ICultureManager {
		/// <summary>
		/// Returns true if there's at least one extra added language
		/// </summary>
		bool HasExtraLanguages { get; }

		LanguageInfo Language { get; set; }

		IEnumerable<LanguageInfo> AllLanguages { get; }
	}

	[Export, Export(typeof(ICultureManager)), PartCreationPolicy(CreationPolicy.Shared)]
	sealed class CultureManager : ICultureManager {
		static readonly string DEFAULT_CULTURE = "en";

		CultureInfo[] extraLanguages;

		public bool HasExtraLanguages {
			get {
				InitializeSupportedLanguages();
				return extraLanguages.Length > 0;
			}
		}

		public IEnumerable<LanguageInfo> AllLanguages {
			get {
				InitializeSupportedLanguages();
				var langs = new HashSet<LanguageInfo>();
				langs.Add(LanguageInfo.CreateSystemLanguage());
				foreach (var ci in extraLanguages)
					langs.Add(LanguageInfo.Create(ci));
				var defaultLang = LanguageInfo.Create(new CultureInfo(DEFAULT_CULTURE));
				if (!langs.Contains(defaultLang))
					langs.Add(defaultLang);
				return langs;
			}
		}

		public LanguageInfo Language {
			get {
				var cultureInfo = UICulture;
				if (cultureInfo != null)
					return LanguageInfo.Create(cultureInfo);
				return LanguageInfo.CreateSystemLanguage();
			}
			set {
				if (value == null)
					throw new ArgumentNullException();
				if (value.Type == LanguageType.CultureInfo)
					cultureSettings.UIName = value.CultureInfo.Name;
				else {
					Debug.Assert(value.Type == LanguageType.SystemLanguage);
					cultureSettings.UIName = string.Empty;
				}
			}
		}

		CultureInfo UICulture {
			get { return TryCreateCultureInfo(cultureSettings.UIName); }
		}

		readonly CultureSettingsImpl cultureSettings;

		[ImportingConstructor]
		CultureManager(CultureSettingsImpl cultureSettings) {
			this.cultureSettings = cultureSettings;
			InitializeCulture(UICulture ?? Thread.CurrentThread.CurrentUICulture);
		}

		public void Initialize(IAppCommandLineArgs args) {
			InitializeCulture(TryCreateCultureInfo(args.Culture));
		}

		void InitializeCulture(CultureInfo info) {
			if (info == null)
				return;

			Thread.CurrentThread.CurrentUICulture = info;
			CultureInfo.DefaultThreadCurrentUICulture = info;
			Debug.Assert(Thread.CurrentThread.CurrentUICulture.Equals(info));
		}

		static CultureInfo TryCreateCultureInfo(string name) {
			if (!string.IsNullOrEmpty(name)) {
				try {
					return new CultureInfo(name);
				}
				catch (CultureNotFoundException) {
				}
				catch {
				}
			}
			return null;
		}

		void InitializeSupportedLanguages() {
			if (extraLanguages != null)
				return;
			var langs = new HashSet<CultureInfo>();
			foreach (var di in GetDirectories(AppDirectories.BinDirectory)) {
				var files = GetFiles(di, "*.dll");
				if (files.Length == 0)
					continue;
				var ci = TryCreateCultureInfo(Path.GetFileName(di));
				if (ci != null)
					langs.Add(ci);
			}

			extraLanguages = langs.OrderBy(a => a.NativeName, StringComparer.InvariantCultureIgnoreCase).ToArray();
		}

		static string[] GetDirectories(string dir) {
			try {
				return Directory.GetDirectories(dir);
			}
			catch {
			}
			return new string[0];
		}

		static string[] GetFiles(string dir, string searchPattern) {
			try {
				return Directory.GetFiles(dir, searchPattern);
			}
			catch {
			}
			return new string[0];
		}
	}
}
