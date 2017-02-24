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
using System.Linq;
using System.Threading;
using dndbg.Engine;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.DotNet;

namespace dnSpy.Debugger.CorDebug.Impl {
	sealed class DbgClrAppDomainImpl : DbgClrAppDomain {
		public override DbgRuntime Runtime { get; }
		public override string Name => name;
		string name;
		public override int Id { get; }

		public override DbgClrModule CorModule {
			get {
				if (corModule == null)
					Interlocked.CompareExchange(ref corModule, Runtime.Modules.OfType<DbgClrModule>().Where(a => a.AppDomain == this).FirstOrDefault(), null);
				return corModule;
			}
		}
		DbgClrModule corModule;

		internal DnAppDomain DnAppDomain { get; }

		public DbgClrAppDomainImpl(DbgClrRuntime runtime, DnAppDomain dnAppDomain) {
			Runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
			DnAppDomain = dnAppDomain ?? throw new ArgumentNullException(nameof(dnAppDomain));
			name = dnAppDomain.Name;
			Id = dnAppDomain.Id;
		}

		internal void SetName_DbgThread(string name) {
			if (this.name == name)
				return;
			this.name = name;
			OnPropertyChanged(nameof(Name));
		}

		protected override void CloseCore() { }
	}
}
