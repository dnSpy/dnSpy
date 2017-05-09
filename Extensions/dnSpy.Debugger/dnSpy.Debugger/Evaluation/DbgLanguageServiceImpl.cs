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
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.Evaluation;
using dnSpy.Contracts.Debugger.Evaluation.Engine;

namespace dnSpy.Debugger.Evaluation {
	struct LanguageInfo {
		public string LanguageName { get; }
		public string LanguageDisplayName { get; }
		public LanguageInfo(string languageName, string languageDisplayName) {
			LanguageName = languageName ?? throw new ArgumentNullException(nameof(languageName));
			LanguageDisplayName = languageDisplayName ?? throw new ArgumentNullException(nameof(languageDisplayName));
		}
	}

	struct RuntimeLanguageInfo {
		public string RuntimeDisplayName { get; }
		public Guid RuntimeGuid { get; }
		public LanguageInfo[] Languages { get; }
		public string CurrentLanguage { get; }
		public RuntimeLanguageInfo(string runtimeDisplayName, Guid runtimeGuid, LanguageInfo[] languages, string currentLanguage) {
			RuntimeDisplayName = runtimeDisplayName ?? throw new ArgumentNullException(nameof(runtimeDisplayName));
			RuntimeGuid = runtimeGuid;
			Languages = languages ?? throw new ArgumentNullException(nameof(languages));
			CurrentLanguage = currentLanguage ?? throw new ArgumentNullException(nameof(currentLanguage));
		}
	}

	interface IDbgLanguageServiceListener {
		void Initialize(DbgLanguageService2 dbgLanguageService);
	}

	abstract class DbgLanguageService2 : DbgLanguageService {
		public abstract RuntimeLanguageInfo[] GetLanguageInfos();
		public abstract void SetDefaultLanguageName(Guid runtimeGuid, string languageName);
	}

	[Export(typeof(DbgLanguageService))]
	[Export(typeof(DbgLanguageService2))]
	sealed class DbgLanguageServiceImpl : DbgLanguageService2 {
		readonly object lockObj;
		readonly Lazy<DbgManager> dbgManager;
		readonly Dictionary<Guid, RuntimeInfo> runtimeInfos;

		sealed class RuntimeInfo {
			public Lazy<DbgEngineLanguageProvider, IDbgEngineLanguageProviderMetadata>[] Providers { get; }
			public ReadOnlyCollection<DbgLanguage> Languages { get; set; }
			public DbgLanguage CurrentLanguage { get; set; }
			public string DefaultLanguageName { get; set; }

			public RuntimeInfo(Lazy<DbgEngineLanguageProvider, IDbgEngineLanguageProviderMetadata>[] providers) =>
				Providers = providers ?? throw new ArgumentNullException(nameof(providers));
		}

		public override event EventHandler<DbgLanguageChangedEventArgs> LanguageChanged;

		[ImportingConstructor]
		DbgLanguageServiceImpl(Lazy<DbgManager> dbgManager, [ImportMany] IEnumerable<Lazy<DbgEngineLanguageProvider, IDbgEngineLanguageProviderMetadata>> dbgEngineLanguageProviders, [ImportMany] IEnumerable<Lazy<IDbgLanguageServiceListener>> dbgLanguageServiceListeners) {
			lockObj = new object();
			this.dbgManager = dbgManager;

			var dict = new Dictionary<Guid, List<Lazy<DbgEngineLanguageProvider, IDbgEngineLanguageProviderMetadata>>>();
			foreach (var lz in dbgEngineLanguageProviders.OrderBy(a => a.Metadata.Order)) {
				bool b = Guid.TryParse(lz.Metadata.RuntimeGuid, out var runtimeGuid);
				Debug.Assert(b);
				if (!b)
					continue;
				if (!dict.TryGetValue(runtimeGuid, out var list))
					dict.Add(runtimeGuid, list = new List<Lazy<DbgEngineLanguageProvider, IDbgEngineLanguageProviderMetadata>>());
				list.Add(lz);
			}

			runtimeInfos = new Dictionary<Guid, RuntimeInfo>(dict.Count);
			foreach (var kv in dict)
				runtimeInfos.Add(kv.Key, new RuntimeInfo(kv.Value.ToArray()));

			foreach (var lz in dbgLanguageServiceListeners)
				lz.Value.Initialize(this);
		}

		public override RuntimeLanguageInfo[] GetLanguageInfos() {
			lock (lockObj) {
				var list = new List<RuntimeLanguageInfo>(runtimeInfos.Count);

				foreach (var kv in runtimeInfos) {
					var langs = GetLanguages(kv.Key);
					Debug.Assert(langs.Count != 0);
					if (langs.Count == 0 || langs[0].Name == PredefinedDbgLanguageNames.None)
						continue;
					var runtimeName = kv.Value.Providers.FirstOrDefault(a => a.Value.RuntimeDisplayName != null)?.Value.RuntimeDisplayName;
					if (runtimeName == null)
						continue;
					var languageName = GetCurrentLanguage(kv.Key).Name;
					var languages = langs.Select(a => new LanguageInfo(a.Name, a.DisplayName)).ToArray();
					list.Add(new RuntimeLanguageInfo(runtimeName, kv.Key, languages, languageName));
				}

				return list.ToArray();
			}
		}

		public override void SetDefaultLanguageName(Guid runtimeGuid, string languageName) {
			if (languageName == null)
				throw new ArgumentNullException(nameof(languageName));
			lock (lockObj) {
				if (runtimeInfos.TryGetValue(runtimeGuid, out var info)) {
					if (info.DefaultLanguageName == null)
						info.DefaultLanguageName = languageName;
				}
			}
		}

		public override ReadOnlyCollection<DbgLanguage> GetLanguages(Guid runtimeGuid) {
			lock (lockObj) {
				if (!runtimeInfos.TryGetValue(runtimeGuid, out var info))
					runtimeInfos.Add(runtimeGuid, info = new RuntimeInfo(Array.Empty<Lazy<DbgEngineLanguageProvider, IDbgEngineLanguageProviderMetadata>>()));
				var languages = info.Languages;
				if (languages == null) {
					var langs = new List<DbgLanguage>();
					var hash = new HashSet<string>(StringComparer.Ordinal);
					foreach (var lz in info.Providers) {
						foreach (var lang in lz.Value.Create()) {
							if (hash.Add(lang.Name))
								langs.Add(new DbgLanguageImpl(runtimeGuid, lang));
						}
					}
					if (langs.Count == 0)
						langs.Add(new DbgLanguageImpl(runtimeGuid, NullDbgEngineLanguage.Instance));
					info.Languages = languages = new ReadOnlyCollection<DbgLanguage>(langs.ToArray());
				}
				return languages;
			}
		}

		public override void SetCurrentLanguage(Guid runtimeGuid, DbgLanguage language) {
			if (language == null)
				throw new ArgumentNullException(nameof(language));
			dbgManager.Value.Dispatcher.BeginInvoke(() => SetCurrentLanguage_DbgThread(runtimeGuid, language));
		}

		void SetCurrentLanguage_DbgThread(Guid runtimeGuid, DbgLanguage language) {
			dbgManager.Value.Dispatcher.VerifyAccess();
			lock (lockObj) {
				if (!runtimeInfos.TryGetValue(runtimeGuid, out var info))
					return;
				if (info.Languages == null)
					return;
				if (!info.Languages.Contains(language))
					return;
				if (info.CurrentLanguage == language)
					return;
				info.CurrentLanguage = language;
			}
			LanguageChanged?.Invoke(this, new DbgLanguageChangedEventArgs(runtimeGuid, language));
		}

		public override DbgLanguage GetCurrentLanguage(Guid runtimeGuid) {
			lock (lockObj) {
				if (!runtimeInfos.TryGetValue(runtimeGuid, out var info))
					runtimeInfos.Add(runtimeGuid, info = new RuntimeInfo(Array.Empty<Lazy<DbgEngineLanguageProvider, IDbgEngineLanguageProviderMetadata>>()));
				if (info.Languages == null)
					GetLanguageInfos();
				if (info.Languages == null)
					throw new InvalidOperationException();
				if (info.CurrentLanguage == null)
					info.CurrentLanguage = info.Languages.FirstOrDefault(a => a.Name == info.DefaultLanguageName) ?? info.Languages.First();
				return info.CurrentLanguage;
			}
		}
	}
}
