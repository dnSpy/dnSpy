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
using dnlib.DotNet;
using dnSpy.Debugger.DotNet.Metadata;

namespace dnSpy.Debugger.DotNet.CorDebug.Impl {
	sealed class DnlibAssemblyResolverImpl : IAssemblyResolver {
		readonly DbgEngineImpl engine;
		readonly DmdAppDomain appDomain;
		readonly Dictionary<IAssembly, AssemblyDef> dict;

		public DnlibAssemblyResolverImpl(DbgEngineImpl engine, DmdAppDomain appDomain) {
			this.engine = engine ?? throw new ArgumentNullException(nameof(engine));
			this.appDomain = appDomain ?? throw new ArgumentNullException(nameof(appDomain));
			dict = new Dictionary<IAssembly, AssemblyDef>(AssemblyNameComparer.CompareAll);
		}

		public AssemblyDef Resolve(IAssembly assembly, ModuleDef sourceModule) {
			engine.VerifyCorDebugThread();
			if (dict.TryGetValue(assembly, out var res))
				return res;
			res = Lookup_CorDebug(assembly);
			if (res != null) {
				dict[assembly] = res;
				dict[res] = res;
			}
			return res;
		}

		AssemblyDef Lookup_CorDebug(IAssembly assembly) {
			engine.VerifyCorDebugThread();
			if (assembly == null)
				return null;
			var asm = appDomain.GetAssembly(new DmdReadOnlyAssemblyName(assembly.FullName));
			if (asm == null)
				return null;
			var dbgModule = asm.ManifestModule.GetDebuggerModule();
			if (dbgModule == null)
				return null;
			if (!engine.TryGetDnModule(dbgModule, out var dnModule))
				return null;
			return dnModule.GetOrCreateCorModuleDef().Assembly;
		}
	}
}
