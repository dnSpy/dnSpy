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
using System.ComponentModel.Composition;
using dnlib.DotNet;
using dnSpy.Contracts.Debugger.Breakpoints.Code;
using dnSpy.Contracts.Debugger.DotNet.Metadata;
using dnSpy.Contracts.Metadata;
using dnSpy.Contracts.Settings;

namespace dnSpy.Debugger.DotNet.Breakpoints.Code {
	[ExportDbgBreakpointLocationSerializer(PredefinedDbgBreakpointLocationTypes.DotNet)]
	sealed class DbgBreakpointLocationSerializerImpl : DbgBreakpointLocationSerializer {
		readonly Lazy<DbgDotNetBreakpointLocationFactory2> dbgDotNetBreakpointLocationFactory;
		readonly Lazy<DbgMetadataService> dbgMetadataService;

		[ImportingConstructor]
		DbgBreakpointLocationSerializerImpl(Lazy<DbgDotNetBreakpointLocationFactory2> dbgDotNetBreakpointLocationFactory, Lazy<DbgMetadataService> dbgMetadataService) {
			this.dbgDotNetBreakpointLocationFactory = dbgDotNetBreakpointLocationFactory;
			this.dbgMetadataService = dbgMetadataService;
		}

		public override void Serialize(ISettingsSection section, DbgBreakpointLocation location) {
			var loc = (DbgDotNetBreakpointLocationImpl)location;
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
				var s = GetMethodAsString(loc.Module, loc.Token);
				if (s != null)
					section.Attribute("Method", s);
			}
		}

		public override DbgBreakpointLocation Deserialize(ISettingsSection section) {
			var token = section.Attribute<uint?>("Token");
			var offset = section.Attribute<uint?>("Offset");
			var assemblyFullName = section.Attribute<string>("AssemblyFullName");
			var moduleName = section.Attribute<string>("ModuleName");
			var isDynamic = section.Attribute<bool?>("IsDynamic") ?? false;
			var isInMemory = section.Attribute<bool?>("IsInMemory") ?? false;
			var moduleNameOnly = section.Attribute<bool?>("ModuleNameOnly") ?? false;
			if (token == null || offset == null || assemblyFullName == null || moduleName == null)
				return null;
			var moduleId = new ModuleId(assemblyFullName, moduleName, isDynamic, isInMemory, moduleNameOnly);

			if (!isInMemory && !isDynamic) {
				var s = section.Attribute<string>("Method");
				if (!string.IsNullOrEmpty(s) && s != GetMethodAsString(moduleId, token.Value))
					return null;
			}

			return dbgDotNetBreakpointLocationFactory.Value.CreateDotNet(moduleId, token.Value, offset.Value);
		}

		string GetMethodAsString(ModuleId moduleId, uint token) {
			var module = dbgMetadataService.Value.TryGetModule(moduleId, DbgLoadModuleOptions.AutoLoaded);
			return (module?.ResolveToken(token) as MethodDef)?.ToString();
		}
	}
}
