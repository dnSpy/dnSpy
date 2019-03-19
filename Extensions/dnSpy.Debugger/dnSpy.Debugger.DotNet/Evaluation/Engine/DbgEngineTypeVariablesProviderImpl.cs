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
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.DotNet.Evaluation;
using dnSpy.Contracts.Debugger.DotNet.Evaluation.ValueNodes;
using dnSpy.Contracts.Debugger.Engine.Evaluation;
using dnSpy.Contracts.Debugger.Evaluation;
using dnSpy.Debugger.DotNet.Metadata;

namespace dnSpy.Debugger.DotNet.Evaluation.Engine {
	sealed class DbgEngineTypeVariablesProviderImpl : DbgEngineValueNodeProvider {
		readonly DbgDotNetEngineValueNodeFactory valueNodeFactory;

		public DbgEngineTypeVariablesProviderImpl(DbgDotNetEngineValueNodeFactory valueNodeFactory) =>
			this.valueNodeFactory = valueNodeFactory ?? throw new ArgumentNullException(nameof(valueNodeFactory));

		public override DbgEngineValueNode[] GetNodes(DbgEvaluationInfo evalInfo, DbgValueNodeEvaluationOptions options) {
			var dispatcher = evalInfo.Runtime.GetDotNetRuntime().Dispatcher;
			if (dispatcher.CheckAccess())
				return GetNodesCore(evalInfo, options);
			return GetNodes(dispatcher, evalInfo, options);

			DbgEngineValueNode[] GetNodes(DbgDotNetDispatcher dispatcher2, DbgEvaluationInfo evalInfo2, DbgValueNodeEvaluationOptions options2) {
				if (!dispatcher2.TryInvokeRethrow(() => GetNodesCore(evalInfo2, options2), out var result))
					result = Array.Empty<DbgEngineValueNode>();
				return result;
			}
		}

		DbgEngineValueNode[] GetNodesCore(DbgEvaluationInfo evalInfo, DbgValueNodeEvaluationOptions options) {
			try {
				var runtime = evalInfo.Runtime.GetDotNetRuntime();
				var method = runtime.GetFrameMethod(evalInfo);
				if ((object)method == null)
					return Array.Empty<DbgEngineValueNode>();

				IList<DmdType> genericTypeParameters, genericTypeArguments;
				IList<DmdType> genericMethodParameters, genericMethodArguments;

				genericTypeArguments = method.ReflectedType.GetGenericArguments();
				genericMethodArguments = method.GetGenericArguments();

				genericTypeParameters = genericTypeArguments.Count == 0 ? genericTypeArguments : method.ReflectedType.GetGenericTypeDefinition().GetGenericArguments();
				genericMethodParameters = genericMethodArguments.Count == 0 ? genericMethodArguments : method.Module.ResolveMethod(method.MetadataToken).GetGenericArguments();
				if (genericTypeParameters.Count != genericTypeArguments.Count)
					throw new InvalidOperationException();
				if (genericMethodParameters.Count != genericMethodArguments.Count)
					throw new InvalidOperationException();

				int count = genericTypeParameters.Count + genericMethodParameters.Count;
				if (count == 0)
					return Array.Empty<DbgEngineValueNode>();

				var infos = new DbgDotNetTypeVariableInfo[count];
				int w = 0;
				for (int i = 0; i < genericTypeParameters.Count; i++)
					infos[w++] = new DbgDotNetTypeVariableInfo(genericTypeParameters[i], genericTypeArguments[i]);
				for (int i = 0; i < genericMethodParameters.Count; i++)
					infos[w++] = new DbgDotNetTypeVariableInfo(genericMethodParameters[i], genericMethodArguments[i]);
				if (infos.Length != w)
					throw new InvalidOperationException();

				var res = new DbgEngineValueNode[1];
				res[0] = valueNodeFactory.CreateTypeVariables(evalInfo, infos);
				return res;
			}
			catch (Exception ex) when (ExceptionUtils.IsInternalDebuggerError(ex)) {
				return valueNodeFactory.CreateInternalErrorResult(evalInfo);
			}
		}
	}
}
