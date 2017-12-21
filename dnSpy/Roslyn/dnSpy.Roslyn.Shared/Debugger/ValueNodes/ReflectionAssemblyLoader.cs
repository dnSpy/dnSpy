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

using System.Diagnostics;
using System.Reflection;
using System.Threading;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.CallStack;
using dnSpy.Contracts.Debugger.DotNet.Evaluation;
using dnSpy.Contracts.Debugger.Evaluation;
using dnSpy.Debugger.DotNet.Metadata;

namespace dnSpy.Roslyn.Shared.Debugger.ValueNodes {
	readonly struct ReflectionAssemblyLoader {
		readonly DbgEvaluationContext context;
		readonly DbgStackFrame frame;
		readonly DmdAppDomain appDomain;
		readonly CancellationToken cancellationToken;

		public ReflectionAssemblyLoader(DbgEvaluationContext context, DbgStackFrame frame, DmdAppDomain appDomain, CancellationToken cancellationToken) {
			this.context = context;
			this.frame = frame;
			this.appDomain = appDomain;
			this.cancellationToken = cancellationToken;
		}

		public bool TryLoadAssembly(string assemblyFullName) => Try_Assembly_Load_String(assemblyFullName);

		// Try System.Reflection.Assembly.Load(string)
		bool Try_Assembly_Load_String(string assemblyFullName) {
			var systemAssemblyType = appDomain.GetWellKnownType(DmdWellKnownType.System_Reflection_Assembly, isOptional: true);
			Debug.Assert((object)systemAssemblyType != null);
			if ((object)systemAssemblyType == null)
				return false;

			var loadMethod = systemAssemblyType.GetMethod(nameof(Assembly.Load), DmdSignatureCallingConvention.Default, 0, systemAssemblyType, new[] { appDomain.System_String }, throwOnError: false);
			if ((object)loadMethod == null)
				return false;

			var runtime = context.Runtime.GetDotNetRuntime();
			var res = runtime.Call(context, frame, null, loadMethod, new object[] { assemblyFullName }, DbgDotNetInvokeOptions.None, cancellationToken);
			res.Value?.Dispose();
			return res.IsNormalResult;
		}
	}
}
