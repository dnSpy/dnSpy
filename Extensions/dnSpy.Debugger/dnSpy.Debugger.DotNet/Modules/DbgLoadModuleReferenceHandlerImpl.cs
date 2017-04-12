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
using System.Linq;
using dnlib.DotNet;
using dnSpy.Contracts.App;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.DotNet.Metadata;
using dnSpy.Contracts.Debugger.References;
using dnSpy.Contracts.Documents;
using dnSpy.Contracts.Documents.Tabs;
using dnSpy.Debugger.DotNet.Properties;
using dnSpy.Debugger.DotNet.UI;

namespace dnSpy.Debugger.DotNet.Modules {
	[ExportDbgLoadModuleReferenceHandler]
	sealed class DbgLoadModuleReferenceHandlerImpl : DbgLoadModuleReferenceHandler {
		readonly UIDispatcher uiDispatcher;
		readonly IDocumentTabService documentTabService;
		readonly Lazy<DbgMetadataService> dbgMetadataService;
		readonly Lazy<IMessageBoxService> messageBoxService;

		[ImportingConstructor]
		DbgLoadModuleReferenceHandlerImpl(UIDispatcher uiDispatcher, IDocumentTabService documentTabService, Lazy<IMessageBoxService> messageBoxService, Lazy<DbgMetadataService> dbgMetadataService) {
			this.uiDispatcher = uiDispatcher;
			this.documentTabService = documentTabService;
			this.dbgMetadataService = dbgMetadataService;
			this.messageBoxService = messageBoxService;
		}

		public override bool GoTo(DbgLoadModuleReference moduleRef, ReadOnlyCollection<object> options) {
			if (moduleRef.Module.IsDotNetModule()) {
				GoToCore(moduleRef, options);
				return true;
			}

			return false;
		}

		bool GoToCore(DbgLoadModuleReference moduleRef, ReadOnlyCollection<object> options) {
			bool canShowMessageBox = true;
			var md = LoadModule(moduleRef.Module, moduleRef.UseMemory, ref canShowMessageBox);
			if (md == null)
				return false;

			// The file could've been added lazily to the list so add a short delay before we select it
			bool newTab = options.Any(a => StringComparer.Ordinal.Equals(PredefinedReferenceNavigatorOptions.NewTab, a));
			uiDispatcher.UIBackground(() => documentTabService.FollowReference(md, newTab));
			return true;
		}

		ModuleDef LoadModule(DbgModule module, bool useMemory, ref bool canShowMessageBox) {
			if (!module.IsDotNetModule())
				return null;

			if (module.IsDynamic && !module.Runtime.IsClosed && module.Process.IsRunning) {
				if (canShowMessageBox) {
					canShowMessageBox = false;
					messageBoxService.Value.Show(dnSpy_Debugger_DotNet_Resources.Module_BreakProcessBeforeLoadingDynamicModules);
				}
				return null;
			}

			var loadOptions = DbgLoadModuleOptions.AutoLoaded;
			if (useMemory)
				loadOptions |= DbgLoadModuleOptions.ForceMemory;
			return dbgMetadataService.Value.TryGetMetadata(module, loadOptions);
		}

		public override DbgModule[] Load(DbgModule[] modules, bool useMemory) {
			var loaded = new List<DbgModule>();
			bool canShowMessageBox = true;
			foreach (var module in modules) {
				if (!module.IsDotNetModule())
					continue;
				LoadModule(module, useMemory, ref canShowMessageBox);
				loaded.Add(module);
			}
			return loaded.ToArray();
		}
	}
}
