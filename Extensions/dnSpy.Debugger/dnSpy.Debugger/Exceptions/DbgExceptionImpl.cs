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
using dnSpy.Contracts.Debugger.Exceptions;

namespace dnSpy.Debugger.Exceptions {
	sealed class DbgExceptionImpl : DbgException {
		public override DbgRuntime Runtime { get; }
		public override DbgExceptionId Id { get; }
		public override DbgExceptionEventFlags Flags { get; }
		public override string? Message { get; }
		public override DbgThread? Thread { get; }
		public override DbgModule? Module { get; }

		DbgDispatcher Dispatcher => Process.DbgManager.Dispatcher;

		public DbgExceptionImpl(DbgRuntime runtime, DbgExceptionId id, DbgExceptionEventFlags flags, string? message, DbgThread? thread, DbgModule? module) {
			if (id.IsDefaultId)
				throw new ArgumentException();
			Runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
			Id = id;
			Flags = flags;
			Message = message;
			Thread = thread;
			Module = module;
		}

		protected override void CloseCore(DbgDispatcher dispatcher) => Dispatcher.VerifyAccess();
	}
}
