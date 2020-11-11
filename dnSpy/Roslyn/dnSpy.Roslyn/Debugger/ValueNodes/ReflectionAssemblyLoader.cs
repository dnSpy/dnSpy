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

using System.Diagnostics;
using System.Reflection;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.DotNet.Evaluation;
using dnSpy.Contracts.Debugger.Evaluation;
using dnSpy.Debugger.DotNet.Metadata;

namespace dnSpy.Roslyn.Debugger.ValueNodes {
	readonly struct ReflectionAssemblyLoader {
		readonly DbgEvaluationInfo evalInfo;
		readonly DmdAppDomain appDomain;

		public ReflectionAssemblyLoader(DbgEvaluationInfo evalInfo, DmdAppDomain appDomain) {
			this.evalInfo = evalInfo;
			this.appDomain = appDomain;
		}

		public bool TryLoadAssembly(string assemblyFullName) => Try_Assembly_Load_String(assemblyFullName);

		// Try System.Reflection.Assembly.Load(string)
		bool Try_Assembly_Load_String(string assemblyFullName) {
			var systemAssemblyType = appDomain.GetWellKnownType(DmdWellKnownType.System_Reflection_Assembly, isOptional: true);
			Debug2.Assert(systemAssemblyType is not null);
			if (systemAssemblyType is null)
				return false;

			var loadMethod = systemAssemblyType.GetMethod(nameof(Assembly.Load), DmdSignatureCallingConvention.Default, 0, systemAssemblyType, new[] { appDomain.System_String }, throwOnError: false);
			if (loadMethod is null)
				return false;

			var runtime = evalInfo.Runtime.GetDotNetRuntime();
			var res = runtime.Call(evalInfo, null, loadMethod, new object[] { assemblyFullName }, DbgDotNetInvokeOptions.None);
			res.Value?.Dispose();
			return res.IsNormalResult;
		}
	}
}
