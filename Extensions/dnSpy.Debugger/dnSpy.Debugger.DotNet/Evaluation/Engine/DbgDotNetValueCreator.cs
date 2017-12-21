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
using dnSpy.Debugger.DotNet.Evaluation.Engine.Interpreter;

namespace dnSpy.Debugger.DotNet.Evaluation.Engine {
	readonly struct DbgDotNetValueCreator {
		readonly DbgDotNetEngineValueNodeFactory valueNodeFactory;
		readonly DbgDotNetILInterpreter dnILInterpreter;
		readonly DbgEvaluationContext context;
		readonly DbgStackFrame frame;
		readonly DbgValueNodeEvaluationOptions nodeOptions;
		readonly DbgEvaluationOptions options;
		readonly byte[] assemblyBytes;
		readonly CancellationToken cancellationToken;

		public DbgDotNetValueCreator(DbgDotNetEngineValueNodeFactory valueNodeFactory, DbgDotNetILInterpreter dnILInterpreter, DbgEvaluationContext context, DbgStackFrame frame, DbgValueNodeEvaluationOptions nodeOptions, DbgEvaluationOptions options, byte[] assemblyBytes, CancellationToken cancellationToken) {
			this.valueNodeFactory = valueNodeFactory;
			this.dnILInterpreter = dnILInterpreter;
			this.context = context;
			this.frame = frame;
			this.nodeOptions = nodeOptions;
			this.options = options;
			this.assemblyBytes = assemblyBytes;
			this.cancellationToken = cancellationToken;
		}

		public DbgEngineValueNode CreateValueNode(ref DbgDotNetILInterpreterState ilInterpreterState, ref DbgDotNetCompiledExpressionResult compExprInfo) {
			if (compExprInfo.ErrorMessage != null)
				return valueNodeFactory.CreateError(context, frame, compExprInfo.Name, compExprInfo.ErrorMessage, compExprInfo.Expression, (compExprInfo.Flags & DbgEvaluationResultFlags.SideEffects) != 0, cancellationToken);
			else {
				if (ilInterpreterState == null)
					ilInterpreterState = dnILInterpreter.CreateState(assemblyBytes);
				var res = dnILInterpreter.Execute(context, frame, ilInterpreterState, compExprInfo.TypeName, compExprInfo.MethodName, options, out var expectedType, cancellationToken);
				try {
					if (res.ErrorMessage != null)
						return valueNodeFactory.CreateError(context, frame, compExprInfo.Name, res.ErrorMessage, compExprInfo.Expression, (compExprInfo.Flags & DbgEvaluationResultFlags.SideEffects) != 0, cancellationToken);
					//TODO: Pass in compExprInfo.CustomTypeInfo, or attach it to the DbgDotNetValueNode
					return valueNodeFactory.Create(context, frame, compExprInfo.Name, res.Value, compExprInfo.FormatSpecifiers, nodeOptions, compExprInfo.Expression, compExprInfo.ImageName, (compExprInfo.Flags & DbgEvaluationResultFlags.ReadOnly) != 0, (compExprInfo.Flags & DbgEvaluationResultFlags.SideEffects) != 0, expectedType, cancellationToken);
				}
				catch {
					res.Value?.Dispose();
					throw;
				}
			}
		}
	}
}
