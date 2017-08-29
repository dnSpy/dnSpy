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
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.DotNet;
using dnSpy.Debugger.DotNet.Metadata;

namespace dnSpy.Debugger.DotNet.CorDebug.Impl {
	sealed class DbgCorDebugInternalModuleImpl : DbgDotNetInternalModule {
		public override DmdModule ReflectionModule => ModuleController.Module;
		public override DbgModule Module => module ?? throw new ArgumentNullException(nameof(module));
		DbgModule module;
		internal DmdAssemblyController AssemblyController { get; }
		internal DmdModuleController ModuleController { get; }
		readonly ClosedListenerCollection closedListenerCollection;
		public DbgCorDebugInternalModuleImpl(DmdAssemblyController assemblyController, DmdModuleController moduleController, ClosedListenerCollection closedListenerCollection) {
			AssemblyController = assemblyController ?? throw new ArgumentNullException(nameof(assemblyController));
			ModuleController = moduleController ?? throw new ArgumentNullException(nameof(moduleController));
			this.closedListenerCollection = closedListenerCollection ?? throw new ArgumentNullException(nameof(closedListenerCollection));
		}
		internal void SetModule(DbgModule module) {
			this.module = module ?? throw new ArgumentNullException(nameof(module));
			ModuleController.Module.GetOrCreateData(() => module);
		}
		internal void Remove() {
			ModuleController.Remove();
			if (AssemblyController.Assembly.GetModules().Length == 0)
				AssemblyController.Remove();
		}
		protected override void CloseCore() => closedListenerCollection.RaiseClosed();
	}

	sealed class ClosedListenerCollection {
		public event EventHandler Closed;
		public void RaiseClosed() => Closed?.Invoke(this, EventArgs.Empty);
	}
}
