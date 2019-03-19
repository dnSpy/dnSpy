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

using System.Diagnostics;
using dndbg.Engine;
using dnlib.DotNet;
using dnSpy.Contracts.Debugger;
using dnSpy.Debugger.DotNet.Metadata;

namespace dnSpy.Debugger.DotNet.CorDebug.Impl {
	abstract partial class DbgEngineImpl {
		sealed class AppDomainModuleState {
			public ModuleContext ModuleContext;
		}

		void DnDebugger_OnCorModuleDefCreated(object sender, CorModuleDefCreatedEventArgs e) {
			debuggerThread.VerifyAccess();
			var appDomain = TryGetEngineAppDomain(e.Module.AppDomain)?.AppDomain;
			Debug.Assert(appDomain != null);
			if (appDomain != null) {
				var state = appDomain.GetOrCreateData<AppDomainModuleState>();
				if (state.ModuleContext == null) {
					var reflectionAppDomain = appDomain.GetReflectionAppDomain();
					Debug.Assert(reflectionAppDomain != null);
					state.ModuleContext = CreateModuleContext(reflectionAppDomain);
				}
				e.CorModuleDef.Context = state.ModuleContext;
			}
		}

		ModuleContext CreateModuleContext(DmdAppDomain appDomain) {
			var context = new ModuleContext();
			context.AssemblyResolver = new DnlibAssemblyResolverImpl(this, appDomain);
			context.Resolver = new Resolver(context.AssemblyResolver);
			return context;
		}
	}
}
