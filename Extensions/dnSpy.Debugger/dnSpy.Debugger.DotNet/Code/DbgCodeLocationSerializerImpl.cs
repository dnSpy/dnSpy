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
using System.ComponentModel.Composition;
using dnlib.DotNet;
using dnSpy.Contracts.Debugger.Code;
using dnSpy.Contracts.Debugger.DotNet.Code;
using dnSpy.Contracts.Debugger.DotNet.Metadata;
using dnSpy.Contracts.Metadata;
using dnSpy.Contracts.Settings;

namespace dnSpy.Debugger.DotNet.Code {
	[ExportDbgCodeLocationSerializer(PredefinedDbgCodeLocationTypes.DotNet)]
	sealed class DbgCodeLocationSerializerImpl : DbgCodeLocationSerializer {
		readonly Lazy<DbgDotNetCodeLocationFactory> dbgDotNetCodeLocationFactory;
		readonly Lazy<DbgMetadataService> dbgMetadataService;

		[ImportingConstructor]
		DbgCodeLocationSerializerImpl(Lazy<DbgDotNetCodeLocationFactory> dbgDotNetCodeLocationFactory, Lazy<DbgMetadataService> dbgMetadataService) {
			this.dbgDotNetCodeLocationFactory = dbgDotNetCodeLocationFactory;
			this.dbgMetadataService = dbgMetadataService;
		}

		// PERF: Getting the serialized name of a method is slow if there are lots of assemblies
		sealed class SerializedState {
			public bool Initialized;
			public string? MethodAsString;
		}

		public override void Serialize(ISettingsSection section, DbgCodeLocation location) {
			var loc = (DbgDotNetCodeLocation)location;
			section.Attribute("Token", loc.Token);
			section.Attribute("Offset", loc.Offset);
			section.Attribute("AssemblyFullName", loc.Module.AssemblyFullName);
			section.Attribute("ModuleName", loc.Module.ModuleName);
			if (loc.Module.IsDynamic)
				section.Attribute("IsDynamic", loc.Module.IsDynamic);
			if (loc.Module.IsInMemory)
				section.Attribute("IsInMemory", loc.Module.IsInMemory);
			if (loc.Module.ModuleNameOnly)
				section.Attribute("ModuleNameOnly", loc.Module.ModuleNameOnly);

			if (!loc.Module.IsInMemory && !loc.Module.IsDynamic) {
				var state = location.GetOrCreateData<SerializedState>();
				if (!state.Initialized) {
					state.MethodAsString = GetMethodAsString(loc.Module, loc.Token);
					state.Initialized = true;
				}
				if (state.MethodAsString is not null)
					section.Attribute("Method", state.MethodAsString);
			}
		}

		public override DbgCodeLocation? Deserialize(ISettingsSection section) {
			var token = section.Attribute<uint?>("Token");
			var offset = section.Attribute<uint?>("Offset");
			var assemblyFullName = section.Attribute<string>("AssemblyFullName");
			var moduleName = section.Attribute<string>("ModuleName");
			var isDynamic = section.Attribute<bool?>("IsDynamic") ?? false;
			var isInMemory = section.Attribute<bool?>("IsInMemory") ?? false;
			var moduleNameOnly = section.Attribute<bool?>("ModuleNameOnly") ?? false;
			if (token is null || offset is null || assemblyFullName is null || moduleName is null)
				return null;
			var moduleId = new ModuleId(assemblyFullName, moduleName, isDynamic, isInMemory, moduleNameOnly);

			var location = dbgDotNetCodeLocationFactory.Value.Create(moduleId, token.Value, offset.Value);

			if (!isInMemory && !isDynamic) {
				var s = section.Attribute<string>("Method");
				if (!string.IsNullOrEmpty(s) && s != GetMethodAsString(moduleId, token.Value)) {
					location.Close();
					return null;
				}
				var state = location.GetOrCreateData<SerializedState>();
				state.MethodAsString = s;
				state.Initialized = true;
			}

			return location;
		}

		string? GetMethodAsString(ModuleId moduleId, uint token) {
			var module = dbgMetadataService.Value.TryGetMetadata(moduleId, DbgLoadModuleOptions.AutoLoaded);
			return (module?.ResolveToken(token) as MethodDef)?.ToString();
		}
	}
}
