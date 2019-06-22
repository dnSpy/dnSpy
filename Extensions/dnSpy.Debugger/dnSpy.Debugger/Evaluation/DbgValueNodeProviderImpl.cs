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
using dnSpy.Contracts.Debugger.Engine.Evaluation;
using dnSpy.Contracts.Debugger.Evaluation;

namespace dnSpy.Debugger.Evaluation {
	sealed class DbgValueNodeProviderImpl : DbgValueNodeProvider {
		public override DbgLanguage Language { get; }

		readonly Guid runtimeKindGuid;
		readonly DbgEngineValueNodeProvider engineValueNodeProvider;

		public DbgValueNodeProviderImpl(DbgLanguage language, Guid runtimeKindGuid, DbgEngineValueNodeProvider engineValueNodeProvider) {
			Language = language ?? throw new ArgumentNullException(nameof(language));
			this.runtimeKindGuid = runtimeKindGuid;
			this.engineValueNodeProvider = engineValueNodeProvider ?? throw new ArgumentNullException(nameof(engineValueNodeProvider));
		}

		public override DbgValueNode[] GetNodes(DbgEvaluationInfo evalInfo, DbgValueNodeEvaluationOptions options) {
			if (evalInfo is null)
				throw new ArgumentNullException(nameof(evalInfo));
			if (!(evalInfo.Context is DbgEvaluationContextImpl))
				throw new ArgumentException();
			if (evalInfo.Context.Language != Language)
				throw new ArgumentException();
			if (evalInfo.Context.Runtime.RuntimeKindGuid != runtimeKindGuid)
				throw new ArgumentException();
			return DbgValueNodeUtils.ToValueNodeArray(Language, evalInfo.Frame.Runtime, engineValueNodeProvider.GetNodes(evalInfo, options));
		}
	}
}
