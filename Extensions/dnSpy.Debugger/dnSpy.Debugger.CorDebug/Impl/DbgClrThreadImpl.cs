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
using dndbg.Engine;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.DotNet;

namespace dnSpy.Debugger.CorDebug.Impl {
	sealed class DbgClrThreadImpl : DbgClrThread {
		public override DbgRuntime Runtime { get; }
		public override DbgAppDomain AppDomain { get; }
		public override string Kind => kind;
		public override int Id => id;
		public override int? ManagedId => managedId;
		public override string Name => name;

		internal DnThread DnThread { get; }
		string kind;
		int id;
		int? managedId;
		string name;

		public DbgClrThreadImpl(DbgRuntime runtime, DbgAppDomain appDomain, DnThread dnThread) {
			Runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
			DnThread = dnThread ?? throw new ArgumentNullException(nameof(dnThread));
			AppDomain = appDomain;
			kind = PredefinedThreadKinds.Unknown;
			id = dnThread.VolatileThreadId;
			managedId = null;
			name = null;
		}

		protected override void CloseCore() { }
	}
}
