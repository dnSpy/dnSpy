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
using dnSpy.Contracts.Debugger.Breakpoints.Code;
using dnSpy.Contracts.Settings;

namespace dnSpy.Debugger.Breakpoints.Code {
	struct BreakpointsSerializer {
		static readonly Guid SETTINGS_GUID = new Guid("FBC6039C-8A7A-49DC-9C32-52C1B73DE0A3");

		readonly ISettingsService settingsService;
		readonly DbgEngineCodeBreakpointSerializerService dbgEngineCodeBreakpointSerializerService;

		public BreakpointsSerializer(ISettingsService settingsService, DbgEngineCodeBreakpointSerializerService dbgEngineCodeBreakpointSerializerService) {
			this.settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
			this.dbgEngineCodeBreakpointSerializerService = dbgEngineCodeBreakpointSerializerService ?? throw new ArgumentNullException(nameof(dbgEngineCodeBreakpointSerializerService));
		}

		public DbgCodeBreakpointInfo[] Load() {
			var section = settingsService.GetOrCreateSection(SETTINGS_GUID);
			var settings = new List<DbgCodeBreakpointInfo>();
			foreach (var bpSect in section.SectionsWithName("Breakpoint")) {
				var isEnabled = bpSect.Attribute<bool?>("IsEnabled");
				if (isEnabled == null)
					continue;
				var engineBreakpoint = dbgEngineCodeBreakpointSerializerService.Deserialize(bpSect.TryGetSection("EBP"));
				if (engineBreakpoint == null)
					continue;
				var bpSettings = new DbgCodeBreakpointSettings {
					IsEnabled = isEnabled.Value,
					Condition = LoadCondition(bpSect.TryGetSection("Condition")),
					HitCount = LoadHitCount(bpSect.TryGetSection("HitCount")),
					Filter = LoadFilter(bpSect.TryGetSection("Filter")),
					Trace = LoadTrace(bpSect.TryGetSection("Trace")),
				};
				settings.Add(new DbgCodeBreakpointInfo(engineBreakpoint, bpSettings));
			}
			return settings.ToArray();
		}

		DbgCodeBreakpointCondition? LoadCondition(ISettingsSection section) {
			if (section == null)
				return null;
			var kind = section.Attribute<DbgCodeBreakpointConditionKind?>("Kind");
			var condition = section.Attribute<string>("Condition");
			if (kind == null || condition == null)
				return null;
			return new DbgCodeBreakpointCondition(kind.Value, condition);
		}

		DbgCodeBreakpointHitCount? LoadHitCount(ISettingsSection section) {
			if (section == null)
				return null;
			var kind = section.Attribute<DbgCodeBreakpointHitCountKind?>("Kind");
			var count = section.Attribute<int?>("Count");
			if (kind == null || count == null)
				return null;
			return new DbgCodeBreakpointHitCount(kind.Value, count.Value);
		}

		DbgCodeBreakpointFilter? LoadFilter(ISettingsSection section) {
			if (section == null)
				return null;
			var filter = section.Attribute<string>("Filter");
			if (filter == null)
				return null;
			return new DbgCodeBreakpointFilter(filter);
		}

		DbgCodeBreakpointTrace? LoadTrace(ISettingsSection section) {
			if (section == null)
				return null;
			var message = section.Attribute<string>("Message");
			var @continue = section.Attribute<bool?>("Continue") ?? true;
			if (message == null)
				return null;
			return new DbgCodeBreakpointTrace(message, @continue);
		}

		public void Save(IEnumerable<DbgCodeBreakpoint> breakpoints) {
			var section = settingsService.RecreateSection(SETTINGS_GUID);
			foreach (var bp in breakpoints) {
				var bpSect = section.CreateSection("Breakpoint");
				var bpSettings = bp.Settings;
				bpSect.Attribute("IsEnabled", bpSettings.IsEnabled);
				dbgEngineCodeBreakpointSerializerService.Serialize(bpSect.CreateSection("EBP"), bp.EngineBreakpoint);
				if (bpSettings.Condition != null)
					Save(bpSect.CreateSection("Condition"), bpSettings.Condition.Value);
				if (bpSettings.HitCount != null)
					Save(bpSect.CreateSection("HitCount"), bpSettings.HitCount.Value);
				if (bpSettings.Filter != null)
					Save(bpSect.CreateSection("Filter"), bpSettings.Filter.Value);
				if (bpSettings.Trace != null)
					Save(bpSect.CreateSection("Trace"), bpSettings.Trace.Value);
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
	}
}
