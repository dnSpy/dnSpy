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
using dnSpy.Contracts.Debugger.DotNet.Evaluation;

namespace dnSpy.Debugger.DotNet.Mono.Impl.Evaluation {
	sealed class DbgDotNetDispatcherImpl : DbgDotNetDispatcher {
		readonly DbgEngineImpl engine;

		public DbgDotNetDispatcherImpl(DbgEngineImpl engine) =>
			this.engine = engine ?? throw new ArgumentNullException(nameof(engine));

		public override void BeginInvoke(Action callback) => engine.MonoDebugThread(callback);
		public override bool CheckAccess() => engine.CheckMonoDebugThread();

		public override T Invoke<T>(Func<T> callback) {
			System.Diagnostics.Debugger.NotifyOfCrossThreadDependency();
			return engine.InvokeMonoDebugThread(callback);
		}
	}
}
