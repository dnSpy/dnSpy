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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.Engine.Evaluation;
using dnSpy.Contracts.Debugger.Evaluation;

namespace dnSpy.Debugger.Evaluation {
	readonly struct LanguageInfo {
		public string LanguageName { get; }
		public string LanguageDisplayName { get; }
		public LanguageInfo(string languageName, string languageDisplayName) {
			LanguageName = languageName ?? throw new ArgumentNullException(nameof(languageName));
			LanguageDisplayName = languageDisplayName ?? throw new ArgumentNullException(nameof(languageDisplayName));
		}
	}

	readonly struct RuntimeLanguageInfo {
		public string RuntimeDisplayName { get; }
		public Guid RuntimeKindGuid { get; }
		public LanguageInfo[] Languages { get; }
		public string CurrentLanguage { get; }
		public RuntimeLanguageInfo(string runtimeDisplayName, Guid runtimeKindGuid, LanguageInfo[] languages, string currentLanguage) {
			RuntimeDisplayName = runtimeDisplayName ?? throw new ArgumentNullException(nameof(runtimeDisplayName));
			RuntimeKindGuid = runtimeKindGuid;
			Languages = languages ?? throw new ArgumentNullException(nameof(languages));
			CurrentLanguage = currentLanguage ?? throw new ArgumentNullException(nameof(currentLanguage));
		}
	}

	interface IDbgLanguageServiceListener {
		void Initialize(DbgLanguageService2 dbgLanguageService);
	}

	abstract class DbgLanguageService2 : DbgLanguageService {
		public abstract RuntimeLanguageInfo[] GetLanguageInfos();
		public abstract void SetDefaultLanguageName(Guid runtimeKindGuid, string languageName);
	}

	[Export(typeof(DbgLanguageService))]
	[Export(typeof(DbgLanguageService2))]
	sealed class DbgLanguageServiceImpl : DbgLanguageService2 {
		readonly object lockObj;
		readonly Lazy<DbgManager> dbgManager;
		readonly Dictionary<Guid, RuntimeInfo> runtimeKindInfos;

		sealed class RuntimeInfo {
			public Lazy<DbgEngineLanguageProvider, IDbgEngineLanguageProviderMetadata>[] Providers { get; }
			public ReadOnlyCollection<DbgLanguage>? Languages { get; set; }
			public DbgLanguage? CurrentLanguage { get; set; }
			public string? DefaultLanguageName { get; set; }

			public RuntimeInfo(Lazy<DbgEngineLanguageProvider, IDbgEngineLanguageProviderMetadata>[] providers) =>
				Providers = providers ?? throw new ArgumentNullException(nameof(providers));
		}

		public override event EventHandler<DbgLanguageChangedEventArgs>? LanguageChanged;

		[ImportingConstructor]
		DbgLanguageServiceImpl(Lazy<DbgManager> dbgManager, [ImportMany] IEnumerable<Lazy<DbgEngineLanguageProvider, IDbgEngineLanguageProviderMetadata>> dbgEngineLanguageProviders, [ImportMany] IEnumerable<Lazy<IDbgLanguageServiceListener>> dbgLanguageServiceListeners) {
			lockObj = new object();
			this.dbgManager = dbgManager;

			var dict = new Dictionary<Guid, List<Lazy<DbgEngineLanguageProvider, IDbgEngineLanguageProviderMetadata>>>();
			foreach (var lz in dbgEngineLanguageProviders.OrderBy(a => a.Metadata.Order)) {
				bool b = Guid.TryParse(lz.Metadata.RuntimeKindGuid, out var runtimeKindGuid);
				Debug.Assert(b);
				if (!b)
					continue;
				if (!dict.TryGetValue(runtimeKindGuid, out var list))
					dict.Add(runtimeKindGuid, list = new List<Lazy<DbgEngineLanguageProvider, IDbgEngineLanguageProviderMetadata>>());
				list.Add(lz);
			}

			runtimeKindInfos = new Dictionary<Guid, RuntimeInfo>(dict.Count);
			foreach (var kv in dict)
				runtimeKindInfos.Add(kv.Key, new RuntimeInfo(kv.Value.ToArray()));

			foreach (var lz in dbgLanguageServiceListeners)
				lz.Value.Initialize(this);
		}

		public override RuntimeLanguageInfo[] GetLanguageInfos() {
			lock (lockObj) {
				var list = new List<RuntimeLanguageInfo>(runtimeKindInfos.Count);

				foreach (var kv in runtimeKindInfos) {
					var langs = GetLanguages(kv.Key);
					Debug.Assert(langs.Count != 0);
					if (langs.Count == 0 || langs[0].Name == PredefinedDbgLanguageNames.None)
						continue;
					var runtimeName = kv.Value.Providers.FirstOrDefault(a => !(a.Value.RuntimeDisplayName is null))?.Value.RuntimeDisplayName;
					if (runtimeName is null)
						continue;
					var languageName = GetCurrentLanguage(kv.Key).Name;
					var languages = langs.Select(a => new LanguageInfo(a.Name, a.DisplayName)).ToArray();
					list.Add(new RuntimeLanguageInfo(runtimeName, kv.Key, languages, languageName));
				}

				return list.ToArray();
			}
		}

		public override void SetDefaultLanguageName(Guid runtimeKindGuid, string languageName) {
			if (languageName is null)
				throw new ArgumentNullException(nameof(languageName));
			lock (lockObj) {
				if (runtimeKindInfos.TryGetValue(runtimeKindGuid, out var info)) {
					if (info.DefaultLanguageName is null)
						info.DefaultLanguageName = languageName;
				}
			}
		}

		public override ReadOnlyCollection<DbgLanguage> GetLanguages(Guid runtimeKindGuid) {
			lock (lockObj) {
				if (!runtimeKindInfos.TryGetValue(runtimeKindGuid, out var info))
					runtimeKindInfos.Add(runtimeKindGuid, info = new RuntimeInfo(Array.Empty<Lazy<DbgEngineLanguageProvider, IDbgEngineLanguageProviderMetadata>>()));
				var languages = info.Languages;
				if (languages is null) {
					var langs = new List<DbgLanguage>();
					var hash = new HashSet<string>(StringComparer.Ordinal);
					foreach (var lz in info.Providers) {
						foreach (var lang in lz.Value.Create()) {
							if (hash.Add(lang.Name))
								langs.Add(new DbgLanguageImpl(runtimeKindGuid, lang));
						}
					}
					if (langs.Count == 0)
						langs.Add(new DbgLanguageImpl(runtimeKindGuid, NullDbgEngineLanguage.Instance));
					info.Languages = languages = new ReadOnlyCollection<DbgLanguage>(langs.ToArray());
				}
				return languages;
			}
		}

		public override void SetCurrentLanguage(Guid runtimeKindGuid, DbgLanguage language) {
			if (language is null)
				throw new ArgumentNullException(nameof(language));
			dbgManager.Value.Dispatcher.BeginInvoke(() => SetCurrentLanguage_DbgThread(runtimeKindGuid, language));
		}

		void SetCurrentLanguage_DbgThread(Guid runtimeKindGuid, DbgLanguage language) {
			dbgManager.Value.Dispatcher.VerifyAccess();
			lock (lockObj) {
				if (!runtimeKindInfos.TryGetValue(runtimeKindGuid, out var info))
					return;
				if (info.Languages is null)
					return;
				if (!info.Languages.Contains(language))
					return;
				if (info.CurrentLanguage == language)
					return;
				info.CurrentLanguage = language;
			}
			LanguageChanged?.Invoke(this, new DbgLanguageChangedEventArgs(runtimeKindGuid, language));
		}

		public override DbgLanguage GetCurrentLanguage(Guid runtimeKindGuid) {
			lock (lockObj) {
				if (!runtimeKindInfos.TryGetValue(runtimeKindGuid, out var info))
					runtimeKindInfos.Add(runtimeKindGuid, info = new RuntimeInfo(Array.Empty<Lazy<DbgEngineLanguageProvider, IDbgEngineLanguageProviderMetadata>>()));
				if (info.Languages is null)
					GetLanguageInfos();
				if (info.Languages is null)
					throw new InvalidOperationException();
				if (info.CurrentLanguage is null)
					info.CurrentLanguage = info.Languages.FirstOrDefault(a => a.Name == info.DefaultLanguageName) ?? info.Languages.First();
				return info.CurrentLanguage;
			}
		}
	}
}
