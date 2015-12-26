/*
    Copyright (C) 2014-2015 de4dot@gmail.com

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
using dnSpy.Contracts.Plugin;
using dnSpy.Contracts.Settings;

namespace dnSpy.Debugger.Exceptions {
	enum ExceptionDiffType {
		Remove,
		AddOrUpdate,
	}

	[ExportAutoLoaded]
	sealed class ExceptionListSettingsLoader : IAutoLoaded {
		[ImportingConstructor]
		ExceptionListSettingsLoader(ExceptionListSettings exceptionListSettings) {
			// Nothing to do, we needed it created
		}
	}

	interface IExceptionListSettings {
		IDisposable TemporarilyDisableSave();
	}

	[Export, Export(typeof(IExceptionListSettings)), PartCreationPolicy(CreationPolicy.Shared)]
	sealed class ExceptionListSettings : IExceptionListSettings {
		static readonly Guid SETTINGS_GUID = new Guid("102DCD2E-BE0A-477C-B4D0-600C5CA28A6A");

		readonly IExceptionManager exceptionManager;
		readonly ISettingsManager settingsManager;
		readonly Lazy<IDefaultExceptionSettings> defaultExceptionSettings;

		[ImportingConstructor]
		ExceptionListSettings(IExceptionManager exceptionManager, ISettingsManager settingsManager, Lazy<IDefaultExceptionSettings> defaultExceptionSettings) {
			this.exceptionManager = exceptionManager;
			this.settingsManager = settingsManager;
			this.defaultExceptionSettings = defaultExceptionSettings;
			exceptionManager.Changed += ExceptionManager_Changed;

			disableSaveCounter++;
			Load();
			disableSaveCounter--;
		}
		int disableSaveCounter;

		void ExceptionManager_Changed(object sender, ExceptionManagerEventArgs e) {
			Save();
		}

		void Load() {
			exceptionManager.RestoreDefaults();
			var section = settingsManager.GetOrCreateSection(SETTINGS_GUID);
			foreach (var exx in section.SectionsWithName("Exception")) {
				var exceptionType = exx.Attribute<ExceptionType?>("ExceptionType");
				var fullName = exx.Attribute<string>("FullName");
				bool? breakOnFirstChance = exx.Attribute<bool?>("BreakOnFirstChance");
				bool isOtherExceptions = exx.Attribute<bool?>("IsOtherExceptions") ?? false;
				var diffType = exx.Attribute<ExceptionDiffType?>("DiffType");

				if (diffType == null)
					continue;
				if (exceptionType == null || exceptionType.Value < 0 || exceptionType.Value >= ExceptionType.Last)
					continue;
				if (fullName == null)
					continue;

				var key = new ExceptionInfoKey(exceptionType.Value, fullName);
				switch (diffType.Value) {
				case ExceptionDiffType.Remove:
					exceptionManager.Remove(key);
					break;

				case ExceptionDiffType.AddOrUpdate:
					if (breakOnFirstChance == null)
						continue;
					exceptionManager.AddOrUpdate(key, breakOnFirstChance.Value, isOtherExceptions);
					break;

				default:
					Debug.Fail("Unknown ExceptionDiffType");
					break;
				}
			}
		}

		void Save() {
			if (disableSaveCounter != 0)
				return;
			var section = settingsManager.RecreateSection(SETTINGS_GUID);
			foreach (var tuple in GetDiff()) {
				var exx = section.CreateSection("Exception");
				exx.Attribute("ExceptionType", tuple.Item2.ExceptionType);
				exx.Attribute("FullName", tuple.Item2.Name);
				exx.Attribute("BreakOnFirstChance", tuple.Item2.BreakOnFirstChance);
				if (tuple.Item2.IsOtherExceptions)
					exx.Attribute("IsOtherExceptions", tuple.Item2.IsOtherExceptions);
				exx.Attribute("DiffType", tuple.Item1);
			}
		}

		IEnumerable<Tuple<ExceptionDiffType, ExceptionInfo>> GetDiff() {
			var defaultInfos = new Dictionary<ExceptionInfoKey, ExceptionInfo>();
			foreach (var info in defaultExceptionSettings.Value.ExceptionInfos)
				defaultInfos[info.Key] = info;

			foreach (var info in exceptionManager.ExceptionInfos) {
				if (info.IsOtherExceptions) {
					if (!info.BreakOnFirstChance)
						continue;
					yield return Tuple.Create(ExceptionDiffType.AddOrUpdate, info);
					continue;
				}

				ExceptionInfo info2;
				if (defaultInfos.TryGetValue(info.Key, out info2)) {
					defaultInfos.Remove(info.Key);
					if (info.Equals(info2))
						continue;
				}
				yield return Tuple.Create(ExceptionDiffType.AddOrUpdate, info);
			}
			foreach (var info in defaultInfos.Values)
				yield return Tuple.Create(ExceptionDiffType.Remove, info);
		}

		sealed class TemporarilyDisableSaveHelper : IDisposable {
			readonly ExceptionListSettings settings;

			public TemporarilyDisableSaveHelper(ExceptionListSettings settings) {
				this.settings = settings;
				settings.disableSaveCounter++;
			}

			public void Dispose() {
				settings.disableSaveCounter--;
				if (settings.disableSaveCounter == 0)
					settings.Save();
			}
		}

		public IDisposable TemporarilyDisableSave() {
			return new TemporarilyDisableSaveHelper(this);
		}
	}
}
