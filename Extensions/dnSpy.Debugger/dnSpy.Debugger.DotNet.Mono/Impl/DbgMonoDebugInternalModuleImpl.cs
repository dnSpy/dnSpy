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
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.DotNet;
using dnSpy.Debugger.DotNet.Metadata;

namespace dnSpy.Debugger.DotNet.Mono.Impl {
	sealed class DbgMonoDebugInternalModuleImpl : DbgDotNetInternalModule {
		public override DmdModule? ReflectionModule { get; }
		public override DbgModule Module => module ?? throw new ArgumentNullException(nameof(module));
		DbgModule? module;
		readonly ClosedListenerCollection closedListenerCollection;
		public DbgMonoDebugInternalModuleImpl(DmdModule reflectionModule, ClosedListenerCollection closedListenerCollection) {
			ReflectionModule = reflectionModule ?? throw new ArgumentNullException(nameof(reflectionModule));
			this.closedListenerCollection = closedListenerCollection ?? throw new ArgumentNullException(nameof(closedListenerCollection));
		}
		internal void SetModule(DbgModule module) {
			this.module = module ?? throw new ArgumentNullException(nameof(module));
			ReflectionModule!.GetOrCreateData(() => module);
		}
		internal void Remove() {
			var asm = ReflectionModule!.Assembly;
			asm.Remove(ReflectionModule);
			if (asm.GetModules().Length == 0)
				asm.AppDomain.Remove(asm);
		}
		protected override void CloseCore(DbgDispatcher dispatcher) => closedListenerCollection.RaiseClosed();
	}

	sealed class ClosedListenerCollection {
		public event EventHandler? Closed;
		public void RaiseClosed() => Closed?.Invoke(this, EventArgs.Empty);
	}
}
