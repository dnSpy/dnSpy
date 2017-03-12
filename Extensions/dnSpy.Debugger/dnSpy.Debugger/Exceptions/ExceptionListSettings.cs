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
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.Exceptions;
using dnSpy.Contracts.Settings;
using dnSpy.Debugger.Impl;

namespace dnSpy.Debugger.Exceptions {
	[Export(typeof(IDbgExceptionSettingsServiceListener))]
	sealed class ExceptionListSettingsListener : IDbgExceptionSettingsServiceListener {
		readonly DbgDispatcher dbgDispatcher;
		readonly ISettingsService settingsService;
		readonly DefaultExceptionDefinitionsProvider defaultExceptionDefinitionsProvider;

		[ImportingConstructor]
		ExceptionListSettingsListener(DbgDispatcher dbgDispatcher, ISettingsService settingsService, DefaultExceptionDefinitionsProvider defaultExceptionDefinitionsProvider) {
			this.dbgDispatcher = dbgDispatcher;
			this.settingsService = settingsService;
			this.defaultExceptionDefinitionsProvider = defaultExceptionDefinitionsProvider;
		}

		void IDbgExceptionSettingsServiceListener.Initialize(DbgExceptionSettingsService dbgExceptionSettingsService) =>
			new ExceptionListSettings(dbgDispatcher, dbgExceptionSettingsService, settingsService, defaultExceptionDefinitionsProvider);
	}

	sealed class ExceptionListSettings {
		static readonly Guid SETTINGS_GUID = new Guid("102DCD2E-BE0A-477C-B4D0-600C5CA28A6A");

		enum DiffType {
			Add,
			Remove,
			Update,
		}

		[Flags]
		enum IdFlags : byte {
			None		= 0,
			HasCode		= 1,
			Default		= 2,
		}

		readonly DbgDispatcher dbgDispatcher;
		readonly DbgExceptionSettingsService dbgExceptionSettingsService;
		readonly ISettingsService settingsService;
		readonly DefaultExceptionDefinitionsProvider defaultExceptionDefinitionsProvider;

		public ExceptionListSettings(DbgDispatcher dbgDispatcher, DbgExceptionSettingsService dbgExceptionSettingsService, ISettingsService settingsService, DefaultExceptionDefinitionsProvider defaultExceptionDefinitionsProvider) {
			this.dbgDispatcher = dbgDispatcher ?? throw new ArgumentNullException(nameof(dbgDispatcher));
			this.dbgExceptionSettingsService = dbgExceptionSettingsService ?? throw new ArgumentNullException(nameof(dbgExceptionSettingsService));
			this.settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
			this.defaultExceptionDefinitionsProvider = defaultExceptionDefinitionsProvider ?? throw new ArgumentNullException(nameof(defaultExceptionDefinitionsProvider));
			dbgExceptionSettingsService.ExceptionsChanged += DbgExceptionSettingsService_ExceptionsChanged;
			dbgExceptionSettingsService.ExceptionSettingsModified += DbgExceptionSettingsService_ExceptionSettingsModified;
			dbgDispatcher.Dbg(() => Load());
		}

		void Load() {
			dbgDispatcher.VerifyAccess();
			ignoreSave = true;

			dbgExceptionSettingsService.Reset();
			var section = settingsService.GetOrCreateSection(SETTINGS_GUID);

			var exToAdd = new List<DbgExceptionSettingsInfo>();
			var exToRemove = new List<DbgExceptionId>();
			var exToUpdate = new List<DbgExceptionIdAndSettings>();
			foreach (var groupSect in section.SectionsWithName("Group")) {
				var group = groupSect.Attribute<string>("Name");
				if (string.IsNullOrEmpty(group))
					continue;

				foreach (var exSect in groupSect.SectionsWithName("Exception")) {
					var diffType = exSect.Attribute<DiffType?>("DiffType");
					if (diffType == null)
						continue;

					var flags = exSect.Attribute<IdFlags?>("IdFlags");
					if (flags == null)
						continue;

					DbgExceptionId id;
					switch (flags.Value) {
					case IdFlags.Default:
						id = new DbgExceptionId(group);
						break;

					case IdFlags.HasCode:
						var code = exSect.Attribute<int?>("Code");
						if (code == null)
							continue;
						id = new DbgExceptionId(group, code.Value);
						break;

					case IdFlags.None:
						var name = exSect.Attribute<string>("Name");
						if (name == null)
							continue;
						id = new DbgExceptionId(group, name);
						break;

					default:
						continue;
					}

					DbgExceptionSettings settings;
					switch (diffType.Value) {
					case DiffType.Add:
						if (!ReadSettings(exSect, out settings))
							continue;
						var description = exSect.Attribute<string>("Description");
						exToAdd.Add(new DbgExceptionSettingsInfo(new DbgExceptionDefinition(id, settings.Flags, description), settings));
						break;

					case DiffType.Remove:
						exToRemove.Add(id);
						break;

					case DiffType.Update:
						if (!ReadSettings(exSect, out settings))
							continue;
						exToUpdate.Add(new DbgExceptionIdAndSettings(id, settings));
						break;

					default:
						Debug.Fail($"Unknown diff type: {diffType}");
						break;
					}
				}
			}

			if (exToRemove.Count > 0)
				dbgExceptionSettingsService.Remove(exToRemove.ToArray());
			if (exToAdd.Count > 0)
				dbgExceptionSettingsService.Add(exToAdd.ToArray());
			if (exToUpdate.Count > 0)
				dbgExceptionSettingsService.Modify(exToUpdate.ToArray());

			dbgDispatcher.Dbg(() => ignoreSave = false);
		}

		bool ReadSettings(ISettingsSection section, out DbgExceptionSettings settings) {
			settings = default(DbgExceptionSettings);
			var flags = section.Attribute<DbgExceptionDefinitionFlags?>("Flags");
			if (flags == null)
				return false;
			DbgExceptionConditionSettings[] condSettings;
			var condSects = section.SectionsWithName("Conditions");
			if (condSects.Length == 0)
				condSettings = Array.Empty<DbgExceptionConditionSettings>();
			else {
				condSettings = new DbgExceptionConditionSettings[condSects.Length];
				for (int i = 0; i < condSects.Length; i++) {
					var condSect = condSects[i];
					var condType = condSect.Attribute<DbgExceptionConditionType?>("Type");
					var cond = condSect.Attribute<string>("Condition");
					if (condType == null || cond == null || !IsValid(condType.Value))
						return false;
					condSettings[i] = new DbgExceptionConditionSettings(condType.Value, cond);
				}
			}
			settings = new DbgExceptionSettings(flags.Value, condSettings);
			return true;
		}

		static bool IsValid(DbgExceptionConditionType type) => (uint)type <= (uint)DbgExceptionConditionType.ModuleNotEquals;

		void DbgExceptionSettingsService_ExceptionsChanged(object sender, DbgCollectionChangedEventArgs<DbgExceptionSettingsInfo> e) => Save();
		void DbgExceptionSettingsService_ExceptionSettingsModified(object sender, DbgExceptionSettingsModifiedEventArgs e) => Save();

		void Save() {
			dbgDispatcher.VerifyAccess();
			if (ignoreSave)
				return;
			var section = settingsService.RecreateSection(SETTINGS_GUID);

			var dict = new Dictionary<string, List<(DiffType diffType, DbgExceptionDefinition def, DbgExceptionSettings settings)>>(StringComparer.Ordinal);
			foreach (var t in GetDiff()) {
				if (!dict.TryGetValue(t.def.Id.Group, out var list))
					dict.Add(t.def.Id.Group, list = new List<(DiffType, DbgExceptionDefinition, DbgExceptionSettings)>());
				list.Add(t);
			}

			foreach (var group in dict.Keys.OrderBy(a => a, StringComparer.OrdinalIgnoreCase)) {
				var groupSect = section.CreateSection("Group");
				groupSect.Attribute("Name", group);
				var list = dict[group];
				list.Sort((a, b) => {
					if (a.def.Id.IsDefaultId != b.def.Id.IsDefaultId) {
						if (a.def.Id.IsDefaultId)
							return -1;
						if (b.def.Id.IsDefaultId)
							return 1;
					}
					else if (a.def.Id.IsDefaultId)
						return 0;

					if (a.def.Id.HasCode != b.def.Id.HasCode) {
						if (a.def.Id.HasCode)
							return -1;
						if (b.def.Id.HasCode)
							return 1;
					}
					else if (a.def.Id.HasCode)
						return a.def.Id.Code - b.def.Id.Code;

					return StringComparer.OrdinalIgnoreCase.Compare(a.def.Id.Name, b.def.Id.Name);
				});
				foreach (var t in list) {
					var exSect = groupSect.CreateSection("Exception");
					exSect.Attribute("IdFlags", GetFlags(t.def.Id));
					if (t.def.Id.HasName)
						exSect.Attribute("Name", t.def.Id.Name);
					if (t.def.Id.HasCode)
						exSect.Attribute("Code", t.def.Id.Code);
					if (!string.IsNullOrEmpty(t.def.Description))
						exSect.Attribute("Description", t.def.Description);
					exSect.Attribute("DiffType", t.diffType);

					switch (t.diffType) {
					case DiffType.Add:
						AddSettings(exSect, t.settings);
						break;

					case DiffType.Remove:
						break;

					case DiffType.Update:
						AddSettings(exSect, t.settings);
						break;

					default:
						Debug.Fail($"Unknown diff type: {t.diffType}");
						break;
					}
				}
			}
		}
		bool ignoreSave;

		static IdFlags GetFlags(DbgExceptionId id) {
			var flags = IdFlags.None;
			if (id.IsDefaultId)
				flags |= IdFlags.Default;
			if (id.HasCode)
				flags |= IdFlags.HasCode;
			return flags;
		}

		static void AddSettings(ISettingsSection section, DbgExceptionSettings settings) {
			section.Attribute("Flags", settings.Flags);
			foreach (var cond in settings.Conditions) {
				var condSect = section.CreateSection("Conditions");
				condSect.Attribute("Type", cond.ConditionType);
				condSect.Attribute("Condition", cond.Condition);
			}
		}

		IEnumerable<(DiffType diffType, DbgExceptionDefinition def, DbgExceptionSettings settings)> GetDiff() {
			var defaultDefs = new Dictionary<DbgExceptionId, DbgExceptionDefinition>(defaultExceptionDefinitionsProvider.Definitions.Length);
			foreach (var def in defaultExceptionDefinitionsProvider.Definitions)
				defaultDefs[def.Id] = def;

			foreach (var info in dbgExceptionSettingsService.Exceptions) {
				if (defaultDefs.TryGetValue(info.Definition.Id, out var def)) {
					defaultDefs.Remove(info.Definition.Id);
					if (Compare(info.Settings, def))
						continue;
					yield return (DiffType.Update, info.Definition, info.Settings);
				}
				else
					yield return (DiffType.Add, info.Definition, info.Settings);
			}
			foreach (var def in defaultDefs.Values)
				yield return (DiffType.Remove, def, default(DbgExceptionSettings));
		}

		static bool Compare(DbgExceptionSettings settings, DbgExceptionDefinition def) {
			if (settings.Conditions.Length != 0)
				return false;
			return settings.Flags == def.Flags;
		}
	}
}
