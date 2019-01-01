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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using dnSpy.Contracts.Debugger.DotNet.Evaluation;
using dnSpy.Contracts.Debugger.Engine.Evaluation;
using dnSpy.Contracts.Debugger.Evaluation;
using dnSpy.Debugger.DotNet.Metadata;

namespace dnSpy.Debugger.DotNet.Evaluation.Engine.Interpreter {
	sealed class InterpreterLocalsProvider : VariablesProvider {
		static ReadOnlyCollection<DmdLocalVariableInfo> emptyLocalVariableInfos = new ReadOnlyCollection<DmdLocalVariableInfo>(Array.Empty<DmdLocalVariableInfo>());
		readonly DebuggerRuntimeImpl runtime;
		readonly Dictionary<int, DbgDotNetValue> extraLocals;
		VariablesProvider localsProvider;
		ReadOnlyCollection<DmdLocalVariableInfo> realLocalVariables;
		ReadOnlyCollection<DmdLocalVariableInfo> localVariables;

		public InterpreterLocalsProvider(DebuggerRuntimeImpl runtime) {
			this.runtime = runtime;
			extraLocals = new Dictionary<int, DbgDotNetValue>();
		}

		internal void Initialize(DmdMethodBody realMethodBody, VariablesProvider localsProvider) {
			Debug.Assert(extraLocals.Count == 0);
			realLocalVariables = realMethodBody?.LocalVariables ?? emptyLocalVariableInfos;
			this.localsProvider = localsProvider;
		}

		public override void Initialize(DbgEvaluationInfo evalInfo, DmdMethodBase method, DmdMethodBody body) {
			Debug.Assert(extraLocals.Count == 0);
			localVariables = body.LocalVariables;
			localsProvider.Initialize(evalInfo, method, body);
		}

		public override DbgDotNetValue GetValueAddress(int index, DmdType targetType) {
			if ((uint)index < (uint)realLocalVariables.Count)
				return localsProvider.GetValueAddress(index, targetType);
			return null;
		}

		public override DbgDotNetValueResult GetVariable(int index) {
			if ((uint)index < (uint)realLocalVariables.Count)
				return localsProvider.GetVariable(index);
			if ((uint)index < (uint)localVariables.Count) {
				var type = localVariables[index].LocalType;
				if (!extraLocals.TryGetValue(index, out var localValue)) {
					localValue = runtime.GetDefaultValue(type);
					extraLocals.Add(index, localValue);
				}
				return DbgDotNetValueResult.Create(localValue);
			}
			return DbgDotNetValueResult.CreateError(PredefinedEvaluationErrorMessages.InternalDebuggerError);
		}

		public override string SetVariable(int index, DmdType targetType, object value) {
			if ((uint)index < (uint)realLocalVariables.Count)
				return localsProvider.SetVariable(index, targetType, value);
			if ((uint)index < (uint)localVariables.Count) {
				extraLocals[index] = runtime.CreateValue(value, targetType);
				return null;
			}
			return PredefinedEvaluationErrorMessages.InternalDebuggerError;
		}

		public override bool CanDispose(DbgDotNetValue value) =>
			localsProvider.CanDispose(value);

		public override void Clear() {
			localsProvider.Clear();
			extraLocals.Clear();
			localsProvider = null;
			realLocalVariables = null;
			localVariables = null;
		}
	}
}
