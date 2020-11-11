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
using System.Linq;
using dnSpy.Contracts.Debugger.Breakpoints.Code;
using dnSpy.Contracts.Settings;

namespace dnSpy.Debugger.Breakpoints.Code {
	readonly struct BreakpointsSerializer {
		static readonly Guid SETTINGS_GUID = new Guid("FBC6039C-8A7A-49DC-9C32-52C1B73DE0A3");

		readonly ISettingsService settingsService;
		readonly DbgCodeLocationSerializerService dbgCodeLocationSerializerService;

		public BreakpointsSerializer(ISettingsService settingsService, DbgCodeLocationSerializerService dbgCodeLocationSerializerService) {
			this.settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
			this.dbgCodeLocationSerializerService = dbgCodeLocationSerializerService ?? throw new ArgumentNullException(nameof(dbgCodeLocationSerializerService));
		}

		public DbgCodeBreakpointInfo[] Load() {
			var section = settingsService.GetOrCreateSection(SETTINGS_GUID);
			var settings = new List<DbgCodeBreakpointInfo>();
			foreach (var bpSect in section.SectionsWithName("Breakpoint")) {
				var isEnabled = bpSect.Attribute<bool?>("IsEnabled");
				if (isEnabled is null)
					continue;
				var location = dbgCodeLocationSerializerService.Deserialize(bpSect.TryGetSection("BPL"));
				if (location is null)
					continue;
				var bpSettings = new DbgCodeBreakpointSettings {
					IsEnabled = isEnabled.Value,
					Condition = LoadCondition(bpSect.TryGetSection("Condition")),
					HitCount = LoadHitCount(bpSect.TryGetSection("HitCount")),
					Filter = LoadFilter(bpSect.TryGetSection("Filter")),
					Trace = LoadTrace(bpSect.TryGetSection("Trace")),
					Labels = new ReadOnlyCollection<string>(LoadLabels(bpSect)),
				};
				settings.Add(new DbgCodeBreakpointInfo(location, bpSettings));
			}
			return settings.ToArray();
		}

		DbgCodeBreakpointCondition? LoadCondition(ISettingsSection? section) {
			if (section is null)
				return null;
			var kind = section.Attribute<DbgCodeBreakpointConditionKind?>("Kind");
			var condition = section.Attribute<string>("Condition");
			if (kind is null || condition is null)
				return null;
			return new DbgCodeBreakpointCondition(kind.Value, condition);
		}

		DbgCodeBreakpointHitCount? LoadHitCount(ISettingsSection? section) {
			if (section is null)
				return null;
			var kind = section.Attribute<DbgCodeBreakpointHitCountKind?>("Kind");
			var count = section.Attribute<int?>("Count");
			if (kind is null || count is null)
				return null;
			return new DbgCodeBreakpointHitCount(kind.Value, count.Value);
		}

		DbgCodeBreakpointFilter? LoadFilter(ISettingsSection? section) {
			if (section is null)
				return null;
			var filter = section.Attribute<string>("Filter");
			if (filter is null)
				return null;
			return new DbgCodeBreakpointFilter(filter);
		}

		DbgCodeBreakpointTrace? LoadTrace(ISettingsSection? section) {
			if (section is null)
				return null;
			var message = section.Attribute<string>("Message");
			var @continue = section.Attribute<bool?>("Continue") ?? true;
			if (message is null)
				return null;
			return new DbgCodeBreakpointTrace(message, @continue);
		}

		string[] LoadLabels(ISettingsSection section) {
			var labels = section.Attribute<string>("Labels") ?? string.Empty;
			return labels.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(a => a.Trim()).ToArray();
		}

		public void Save(DbgCodeBreakpoint[] breakpoints) {
			var section = settingsService.RecreateSection(SETTINGS_GUID);
			foreach (var bp in breakpoints.OrderBy(a => a.Id)) {
				var location = bp.Location;
				if (!dbgCodeLocationSerializerService.CanSerialize(location))
					continue;
				var bpSect = section.CreateSection("Breakpoint");
				var bpSettings = bp.Settings;
				bpSect.Attribute("IsEnabled", bpSettings.IsEnabled);
				dbgCodeLocationSerializerService.Serialize(bpSect.CreateSection("BPL"), location);
				if (bpSettings.Condition is not null)
					Save(bpSect.CreateSection("Condition"), bpSettings.Condition.Value);
				if (bpSettings.HitCount is not null)
					Save(bpSect.CreateSection("HitCount"), bpSettings.HitCount.Value);
				if (bpSettings.Filter is not null)
					Save(bpSect.CreateSection("Filter"), bpSettings.Filter.Value);
				if (bpSettings.Trace is not null)
					Save(bpSect.CreateSection("Trace"), bpSettings.Trace.Value);
				if (bpSettings.Labels is not null && bpSettings.Labels.Count != 0)
					SaveLabels(bpSect, bpSettings.Labels.ToArray());
			}
		}

		void Save(ISettingsSection section, DbgCodeBreakpointCondition settings) {
			section.Attribute("Kind", settings.Kind);
			section.Attribute("Condition", settings.Condition);
		}

		void Save(ISettingsSection section, DbgCodeBreakpointHitCount settings) {
			section.Attribute("Kind", settings.Kind);
			section.Attribute("Count", settings.Count);
		}

		void Save(ISettingsSection section, DbgCodeBreakpointFilter settings) => section.Attribute("Filter", settings.Filter);

		void Save(ISettingsSection section, DbgCodeBreakpointTrace settings) {
			section.Attribute("Message", settings.Message);
			if (!settings.Continue)
				section.Attribute("Continue", settings.Continue);
		}

		void SaveLabels(ISettingsSection section, string[] labels) {
			if (labels is null || labels.Length == 0)
				return;
			section.Attribute("Labels", string.Join(", ", labels));
		}
	}
}
