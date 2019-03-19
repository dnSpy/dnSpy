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

using dnlib.DotNet;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Metadata;
using Mono.Debugger.Soft;

namespace dnSpy.Debugger.DotNet.Mono.Impl {
	static class ModuleIdUtils {
		// This must match the code in CorDebug
		public static ModuleId Create(DbgModule module, ModuleMirror monoModule) {
			// CorDebug also uses AssemblyNameInfo to get the asm full name. It's possible that
			// AssemblyName (monoModule.Assembly.GetName()) returns a slightly different string
			// for some input.
			var asmFullName = new AssemblyNameInfo(monoModule.Assembly.GetName()).FullName;

			string moduleName;
			uint id = (uint)module.Order;
			if (module.IsInMemory || module.IsDynamic)
				moduleName = monoModule.ScopeName + " (id=" + id.ToString() + ")";
			else
				moduleName = module.Filename;

			return new ModuleId(asmFullName, moduleName, module.IsDynamic, module.IsInMemory, false);
		}
	}
}
