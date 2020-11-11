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

using dnSpy.Contracts.Debugger.DotNet.Evaluation.ExpressionCompiler;
using dnSpy.Contracts.Debugger.Engine.Evaluation;
using dnSpy.Contracts.Debugger.Evaluation;
using dnSpy.Debugger.DotNet.Evaluation.Engine.Interpreter;

namespace dnSpy.Debugger.DotNet.Evaluation.Engine {
	readonly struct DbgDotNetValueCreator {
		readonly DbgDotNetEngineValueNodeFactory valueNodeFactory;
		readonly DbgDotNetILInterpreter dnILInterpreter;
		readonly DbgEvaluationInfo evalInfo;
		readonly DbgValueNodeEvaluationOptions nodeOptions;
		readonly DbgEvaluationOptions options;
		readonly byte[] assemblyBytes;

		public DbgDotNetValueCreator(DbgDotNetEngineValueNodeFactory valueNodeFactory, DbgDotNetILInterpreter dnILInterpreter, DbgEvaluationInfo evalInfo, DbgValueNodeEvaluationOptions nodeOptions, DbgEvaluationOptions options, byte[] assemblyBytes) {
			this.valueNodeFactory = valueNodeFactory;
			this.dnILInterpreter = dnILInterpreter;
			this.evalInfo = evalInfo;
			this.nodeOptions = nodeOptions;
			this.options = options;
			this.assemblyBytes = assemblyBytes;
		}

		public DbgEngineValueNode CreateValueNode(ref DbgDotNetILInterpreterState? ilInterpreterState, ref DbgDotNetCompiledExpressionResult compExprInfo) {
			if (compExprInfo.ErrorMessage is not null)
				return valueNodeFactory.CreateError(evalInfo, compExprInfo.Name, compExprInfo.ErrorMessage, compExprInfo.Expression, (compExprInfo.Flags & DbgEvaluationResultFlags.SideEffects) != 0);
			else {
				if (ilInterpreterState is null)
					ilInterpreterState = dnILInterpreter.CreateState(assemblyBytes);
				var res = dnILInterpreter.Execute(evalInfo, ilInterpreterState, compExprInfo.TypeName, compExprInfo.MethodName, options, out var expectedType);
				try {
					if (res.ErrorMessage is not null)
						return valueNodeFactory.CreateError(evalInfo, compExprInfo.Name, res.ErrorMessage, compExprInfo.Expression, (compExprInfo.Flags & DbgEvaluationResultFlags.SideEffects) != 0);
					//TODO: Pass in compExprInfo.CustomTypeInfo, or attach it to the DbgDotNetValueNode
					return valueNodeFactory.Create(evalInfo, compExprInfo.Name, res.Value!, compExprInfo.FormatSpecifiers, nodeOptions, compExprInfo.Expression, compExprInfo.ImageName, (compExprInfo.Flags & DbgEvaluationResultFlags.ReadOnly) != 0, (compExprInfo.Flags & DbgEvaluationResultFlags.SideEffects) != 0, expectedType);
				}
				catch {
					res.Value?.Dispose();
					throw;
				}
			}
		}
	}
}
