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
using dnSpy.Contracts.Debugger.DotNet.Evaluation;
using dnSpy.Contracts.Debugger.Evaluation;
using dnSpy.Debugger.DotNet.Metadata;

namespace dnSpy.Debugger.DotNet.Evaluation.Engine {
	abstract class DbgDotNetILInterpreter {
		public abstract DbgDotNetILInterpreterState CreateState(DbgEvaluationContext context, byte[] assembly);
		public abstract DbgDotNetValue Execute(DbgEvaluationContext context, DbgStackFrame frame, DbgDotNetILInterpreterState state, string typeName, string methodName, DbgValueNodeEvaluationOptions options, out DmdType expectedType, CancellationToken cancellationToken);
	}

	abstract class DbgDotNetILInterpreterState {
	}

	sealed class DbgDotNetILInterpreterImpl : DbgDotNetILInterpreter {
		sealed class DbgDotNetILInterpreterStateImpl : DbgDotNetILInterpreterState {
			readonly byte[] assemblyBytes;

			public DbgDotNetILInterpreterStateImpl(byte[] assemblyBytes) =>
				this.assemblyBytes = assemblyBytes ?? throw new ArgumentNullException(nameof(assemblyBytes));
		}

		public override DbgDotNetILInterpreterState CreateState(DbgEvaluationContext context, byte[] assembly) {
			if (context == null)
				throw new ArgumentNullException(nameof(context));
			if (assembly == null)
				throw new ArgumentNullException(nameof(assembly));
			return new DbgDotNetILInterpreterStateImpl(assembly);
		}

		public override DbgDotNetValue Execute(DbgEvaluationContext context, DbgStackFrame frame, DbgDotNetILInterpreterState state, string typeName, string methodName, DbgValueNodeEvaluationOptions options, out DmdType expectedType, CancellationToken cancellationToken) {
			if (context == null)
				throw new ArgumentNullException(nameof(context));
			if (frame == null)
				throw new ArgumentNullException(nameof(frame));
			if (state == null)
				throw new ArgumentNullException(nameof(state));
			if (typeName == null)
				throw new ArgumentNullException(nameof(typeName));
			if (methodName == null)
				throw new ArgumentNullException(nameof(methodName));
			var stateImpl = state as DbgDotNetILInterpreterStateImpl;
			if (stateImpl == null)
				throw new ArgumentException();

			throw new NotImplementedException();//TODO:
		}
	}
}
