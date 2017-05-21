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
using System.Threading;
using dnSpy.Contracts.Debugger.CallStack;
using dnSpy.Contracts.Debugger.Evaluation;
using dnSpy.Contracts.Debugger.Evaluation.Engine;

namespace dnSpy.Debugger.Evaluation {
	sealed class DbgValueNodeProviderImpl : DbgValueNodeProvider {
		public override DbgLanguage Language { get; }

		readonly Guid runtimeGuid;
		readonly DbgEngineValueNodeProvider engineValueNodeProvider;

		public DbgValueNodeProviderImpl(DbgLanguage language, Guid runtimeGuid, DbgEngineValueNodeProvider engineValueNodeProvider) {
			Language = language ?? throw new ArgumentNullException(nameof(language));
			this.runtimeGuid = runtimeGuid;
			this.engineValueNodeProvider = engineValueNodeProvider ?? throw new ArgumentNullException(nameof(engineValueNodeProvider));
		}

		public override DbgValueNode[] GetNodes(DbgStackFrame frame, CancellationToken cancellationToken) {
			if (frame == null)
				throw new ArgumentNullException(nameof(frame));
			if (frame.Runtime.Guid != runtimeGuid)
				throw new ArgumentException();
			return DbgValueNodeUtils.ToValueNodeArray(Language, frame.Runtime, engineValueNodeProvider.GetNodes(frame, cancellationToken));
		}

		public override void GetNodes(DbgStackFrame frame, Action<DbgValueNode[]> callback, CancellationToken cancellationToken) {
			if (frame == null)
				throw new ArgumentNullException(nameof(frame));
			if (frame.Runtime.Guid != runtimeGuid)
				throw new ArgumentException();
			if (callback == null)
				throw new ArgumentNullException(nameof(callback));
			engineValueNodeProvider.GetNodes(frame, engineNodes => callback(DbgValueNodeUtils.ToValueNodeArray(Language, frame.Runtime, engineNodes)), cancellationToken);
		}
	}
}
