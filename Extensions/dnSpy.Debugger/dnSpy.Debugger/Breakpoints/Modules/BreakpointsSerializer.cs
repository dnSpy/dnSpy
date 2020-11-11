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
using System.Linq;
using dnSpy.Contracts.Debugger.Breakpoints.Modules;
using dnSpy.Contracts.Settings;

namespace dnSpy.Debugger.Breakpoints.Modules {
	readonly struct BreakpointsSerializer {
		static readonly Guid SETTINGS_GUID = new Guid("A7C5956E-593B-4B04-A237-F837CF17E44E");

		readonly ISettingsService settingsService;

		public BreakpointsSerializer(ISettingsService settingsService) => this.settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));

		public DbgModuleBreakpointSettings[] Load() {
			var section = settingsService.GetOrCreateSection(SETTINGS_GUID);
			var settings = new List<DbgModuleBreakpointSettings>();
			foreach (var bpSect in section.SectionsWithName("Breakpoint")) {
				var isEnabled = bpSect.Attribute<bool?>("IsEnabled");
				if (isEnabled is null)
					continue;
				var bpSettings = new DbgModuleBreakpointSettings {
					IsEnabled = isEnabled.Value,
					ModuleName = bpSect.Attribute<string>("ModuleName"),
					IsDynamic = bpSect.Attribute<bool?>("IsDynamic"),
					IsInMemory = bpSect.Attribute<bool?>("IsInMemory"),
					IsLoaded = bpSect.Attribute<bool?>("IsLoaded"),
					Order = bpSect.Attribute<int?>("Order"),
					AppDomainName = bpSect.Attribute<string>("AppDomainName"),
					ProcessName = bpSect.Attribute<string>("ProcessName"),
				};
				settings.Add(bpSettings);
			}
			return settings.ToArray();
		}

		public void Save(DbgModuleBreakpoint[] breakpoints) {
			var section = settingsService.RecreateSection(SETTINGS_GUID);
			foreach (var bp in breakpoints.OrderBy(a => a.Id)) {
				var bpSect = section.CreateSection("Breakpoint");
				var bpSettings = bp.Settings;
				bpSect.Attribute("IsEnabled", bpSettings.IsEnabled);
				if (!string.IsNullOrEmpty(bpSettings.ModuleName))
					bpSect.Attribute("ModuleName", bpSettings.ModuleName);
				if (bpSettings.IsDynamic is not null)
					bpSect.Attribute("IsDynamic", bpSettings.IsDynamic);
				if (bpSettings.IsInMemory is not null)
					bpSect.Attribute("IsInMemory", bpSettings.IsInMemory);
				if (bpSettings.IsLoaded is not null)
					bpSect.Attribute("IsLoaded", bpSettings.IsLoaded);
				if (bpSettings.Order is not null)
					bpSect.Attribute("Order", bpSettings.Order);
				if (!string.IsNullOrEmpty(bpSettings.AppDomainName))
					bpSect.Attribute("AppDomainName", bpSettings.AppDomainName);
				if (!string.IsNullOrEmpty(bpSettings.ProcessName))
					bpSect.Attribute("ProcessName", bpSettings.ProcessName);
			}
		}
	}
}
