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
using dnSpy.Contracts.Debugger.Breakpoints.Code;
using dnSpy.Contracts.Metadata;
using dnSpy.Contracts.Settings;

namespace dnSpy.Debugger.DotNet.Breakpoints.Code {
	[ExportDbgEngineCodeBreakpointSerializer(PredefinedDbgEngineCodeBreakpointTypes.DotNet)]
	sealed class DbgEngineCodeBreakpointSerializerImpl : DbgEngineCodeBreakpointSerializer {
		readonly Lazy<DbgDotNetEngineCodeBreakpointFactory2> dbgDotNetEngineCodeBreakpointFactory;

		[ImportingConstructor]
		DbgEngineCodeBreakpointSerializerImpl(Lazy<DbgDotNetEngineCodeBreakpointFactory2> dbgDotNetEngineCodeBreakpointFactory) =>
			this.dbgDotNetEngineCodeBreakpointFactory = dbgDotNetEngineCodeBreakpointFactory;

		public override void Serialize(ISettingsSection section, DbgEngineCodeBreakpoint breakpoint) {
			var bp = (DbgDotNetEngineCodeBreakpointImpl)breakpoint;
			section.Attribute("Token", bp.Token);
			section.Attribute("Offset", bp.Offset);
			section.Attribute("AssemblyFullName", bp.Module.AssemblyFullName);
			section.Attribute("ModuleName", bp.Module.ModuleName);
			if (bp.Module.IsDynamic)
				section.Attribute("IsDynamic", bp.Module.IsDynamic);
			if (bp.Module.IsInMemory)
				section.Attribute("IsInMemory", bp.Module.IsInMemory);
			if (bp.Module.ModuleNameOnly)
				section.Attribute("ModuleNameOnly", bp.Module.ModuleNameOnly);
		}

		public override DbgEngineCodeBreakpoint Deserialize(ISettingsSection section) {
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
			return dbgDotNetEngineCodeBreakpointFactory.Value.CreateDotNet(moduleId, token.Value, offset.Value);
		}
	}
}
