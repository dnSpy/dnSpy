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
using dnSpy.Contracts.Debugger.DotNet.Evaluation.ExpressionCompiler;
using dnSpy.Contracts.Debugger.Engine.Evaluation;
using dnSpy.Contracts.Debugger.Evaluation;

namespace dnSpy.Debugger.DotNet.Evaluation.Engine {
	struct DbgDotNetValueCreator {
		readonly DbgDotNetEngineValueNodeFactory valueNodeFactory;
		readonly DbgDotNetILInterpreter dnILInterpreter;
		readonly DbgEvaluationContext context;
		readonly DbgStackFrame frame;
		readonly DbgValueNodeEvaluationOptions options;
		readonly byte[] assemblyBytes;
		/*readonly*/ CancellationToken cancellationToken;

		public DbgDotNetValueCreator(DbgDotNetEngineValueNodeFactory valueNodeFactory, DbgDotNetILInterpreter dnILInterpreter, DbgEvaluationContext context, DbgStackFrame frame, DbgValueNodeEvaluationOptions options, byte[] assemblyBytes, CancellationToken cancellationToken) {
			this.valueNodeFactory = valueNodeFactory;
			this.dnILInterpreter = dnILInterpreter;
			this.context = context;
			this.frame = frame;
			this.options = options;
			this.assemblyBytes = assemblyBytes;
			this.cancellationToken = cancellationToken;
		}

		public DbgEngineValueNode CreateValueNode(ref DbgDotNetILInterpreterState ilInterpreterState, ref DbgDotNetCompiledExpressionResult compExprInfo) {
			if (compExprInfo.ErrorMessage != null)
				return valueNodeFactory.CreateError(context, compExprInfo.Name, compExprInfo.ErrorMessage, compExprInfo.Expression);
			else {
				if (ilInterpreterState == null)
					ilInterpreterState = dnILInterpreter.CreateState(context, assemblyBytes);
				var value = dnILInterpreter.Execute(context, frame, ilInterpreterState, compExprInfo.TypeName, compExprInfo.MethodName, options, out var expectedType, cancellationToken);
				//TODO: Pass in compExprInfo.CustomTypeInfo, or attach it to the DbgDotNetValueNode
				return valueNodeFactory.Create(context, compExprInfo.Name, value, compExprInfo.Expression, compExprInfo.ImageName, (compExprInfo.Flags & DbgEvaluationResultFlags.ReadOnly) != 0, (compExprInfo.Flags & DbgEvaluationResultFlags.SideEffects) != 0, expectedType);
			}
		}
	}
}
