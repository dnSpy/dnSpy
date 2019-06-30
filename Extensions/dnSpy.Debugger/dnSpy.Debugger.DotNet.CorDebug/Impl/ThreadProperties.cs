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

using dndbg.COM.CorDebug;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.Engine;

namespace dnSpy.Debugger.DotNet.CorDebug.Impl {
	sealed class ThreadProperties {
		public DbgAppDomain? AppDomain { get; }
		public string Kind { get; }
		public ulong Id { get; }
		public ulong? ManagedId { get; }
		public string? Name { get; }
		public int SuspendedCount { get; }
		public CorDebugUserState UserState { get; }

		public ThreadProperties(DbgAppDomain? appDomain, string kind, ulong id, ulong? managedId, string? name, int suspendedCount, CorDebugUserState userState) {
			AppDomain = appDomain;
			Kind = kind;
			Id = id;
			ManagedId = managedId;
			Name = name;
			SuspendedCount = suspendedCount;
			UserState = userState;
		}

		public DbgEngineThread.UpdateOptions Compare(ThreadProperties? other) {
			var options = DbgEngineThread.UpdateOptions.None;
			if (other?.AppDomain != AppDomain)
				options |= DbgEngineThread.UpdateOptions.AppDomain;
			if (other?.Kind != Kind)
				options |= DbgEngineThread.UpdateOptions.Kind;
			if (other?.Id != Id)
				options |= DbgEngineThread.UpdateOptions.Id;
			if (other?.ManagedId != ManagedId)
				options |= DbgEngineThread.UpdateOptions.ManagedId;
			if (other?.Name != Name)
				options |= DbgEngineThread.UpdateOptions.Name;
			if (other?.SuspendedCount != SuspendedCount)
				options |= DbgEngineThread.UpdateOptions.SuspendedCount;
			if (other?.UserState != UserState)
				options |= DbgEngineThread.UpdateOptions.State;
			return options;
		}
	}
}
