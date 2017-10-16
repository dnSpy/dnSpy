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

using System.Threading;
using dnSpy.Contracts.Debugger.CallStack;
using dnSpy.Contracts.Debugger.DotNet.Evaluation;
using dnSpy.Contracts.Debugger.Evaluation;
using dnSpy.Debugger.DotNet.Metadata;

namespace dnSpy.Debugger.DotNet.Evaluation.Engine {
	sealed class DefaultLocalsProviderImpl : VariablesProvider {
		readonly IDbgDotNetRuntime runtime;
		public DefaultLocalsProviderImpl(IDbgDotNetRuntime runtime) => this.runtime = runtime;

		DbgEvaluationContext context;
		DbgStackFrame frame;
		CancellationToken cancellationToken;

		public override void Initialize(DbgEvaluationContext context, DbgStackFrame frame, DmdMethodBase method, DmdMethodBody body, CancellationToken cancellationToken) {
			this.context = context;
			this.frame = frame;
			this.cancellationToken = cancellationToken;
		}

		public override DbgDotNetValueResult GetVariable(int index) =>
			runtime.GetLocalValue(context, frame, (uint)index, cancellationToken);

		public override string SetVariable(int index, DmdType targetType, object value) =>
			runtime.SetLocalValue(context, frame, (uint)index, targetType, value, cancellationToken);

		public override bool CanDispose(DbgDotNetValue value) => true;

		public override void Clear() {
			context = null;
			frame = null;
			cancellationToken = default;
		}
	}
}
