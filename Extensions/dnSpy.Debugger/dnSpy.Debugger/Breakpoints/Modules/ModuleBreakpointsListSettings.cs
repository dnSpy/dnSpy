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
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.Breakpoints.Modules;
using dnSpy.Contracts.Settings;
using dnSpy.Debugger.Impl;

namespace dnSpy.Debugger.Breakpoints.Modules {
	[Export(typeof(IDbgModuleBreakpointsServiceListener))]
	sealed class ModuleBreakpointsListSettingsListener : IDbgModuleBreakpointsServiceListener {
		readonly DbgDispatcher dbgDispatcher;
		readonly ISettingsService settingsService;

		[ImportingConstructor]
		ModuleBreakpointsListSettingsListener(DbgDispatcher dbgDispatcher, ISettingsService settingsService) {
			this.dbgDispatcher = dbgDispatcher;
			this.settingsService = settingsService;
		}

		void IDbgModuleBreakpointsServiceListener.Initialize(DbgModuleBreakpointsService dbgModuleBreakpointsService) =>
			new ModuleBreakpointsListSettings(dbgDispatcher, settingsService, dbgModuleBreakpointsService);
	}

	sealed class ModuleBreakpointsListSettings {
		static readonly Guid SETTINGS_GUID = new Guid("A7C5956E-593B-4B04-A237-F837CF17E44E");

		readonly DbgDispatcher dbgDispatcher;
		readonly ISettingsService settingsService;
		readonly DbgModuleBreakpointsService dbgModuleBreakpointsService;

		public ModuleBreakpointsListSettings(DbgDispatcher dbgDispatcher, ISettingsService settingsService, DbgModuleBreakpointsService dbgModuleBreakpointsService) {
			this.dbgDispatcher = dbgDispatcher ?? throw new ArgumentNullException(nameof(dbgDispatcher));
			this.settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
			this.dbgModuleBreakpointsService = dbgModuleBreakpointsService ?? throw new ArgumentNullException(nameof(dbgModuleBreakpointsService));
			dbgModuleBreakpointsService.BreakpointsChanged += DbgModuleBreakpointsService_BreakpointsChanged;
			dbgModuleBreakpointsService.BreakpointsModified += DbgModuleBreakpointsService_BreakpointsModified;
			dbgDispatcher.Dbg(() => Load());
		}

		void Load() {
			dbgDispatcher.VerifyAccess();
			ignoreSave = true;

			var section = settingsService.GetOrCreateSection(SETTINGS_GUID);
			var settings = new List<DbgModuleBreakpointSettings>();
			foreach (var bpSect in section.SectionsWithName("Breakpoint")) {
				var isEnabled = bpSect.Attribute<bool?>("IsEnabled");
				if (isEnabled == null)
					continue;
				var bpSettings = new DbgModuleBreakpointSettings {
					IsEnabled = isEnabled.Value,
					ModuleName = bpSect.Attribute<string>("ModuleName"),
					IsDynamic = bpSect.Attribute<bool?>("IsDynamic"),
					IsInMemory = bpSect.Attribute<bool?>("IsInMemory"),
					Order = bpSect.Attribute<int?>("Order"),
					AppDomainName = bpSect.Attribute<string>("AppDomainName"),
					ProcessName = bpSect.Attribute<string>("ProcessName")
				};
				settings.Add(bpSettings);
			}

			dbgModuleBreakpointsService.Add(settings.ToArray());
			dbgDispatcher.Dbg(() => ignoreSave = false);
		}

		void DbgModuleBreakpointsService_BreakpointsChanged(object sender, DbgCollectionChangedEventArgs<DbgModuleBreakpoint> e) => Save();
		void DbgModuleBreakpointsService_BreakpointsModified(object sender, DbgBreakpointsModifiedEventArgs e) => Save();

		void Save() {
			dbgDispatcher.VerifyAccess();
			if (ignoreSave)
				return;

			var section = settingsService.RecreateSection(SETTINGS_GUID);
			foreach (var bp in dbgModuleBreakpointsService.Breakpoints) {
				var bpSect = section.CreateSection("Breakpoint");
				var bpSettings = bp.Settings;
				bpSect.Attribute("IsEnabled", bpSettings.IsEnabled);
				bpSect.Attribute("ModuleName", bpSettings.ModuleName);
				bpSect.Attribute("IsDynamic", bpSettings.IsDynamic);
				bpSect.Attribute("IsInMemory", bpSettings.IsInMemory);
				bpSect.Attribute("Order", bpSettings.Order);
				bpSect.Attribute("AppDomainName", bpSettings.AppDomainName);
				bpSect.Attribute("ProcessName", bpSettings.ProcessName);
			}
		}
		bool ignoreSave;
	}
}
