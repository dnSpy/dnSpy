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
using System.Diagnostics;
using System.Threading;
using dnlib.DotNet;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Disassembly;
using dnSpy.Decompiler.Utils;

namespace dnSpy.Debugger.DotNet.Disassembly {
	struct DecompiledCodeProvider {
		readonly IDecompiler decompiler;
		readonly MethodDef method;
		readonly CancellationToken cancellationToken;
		DecompilerOutputImpl output;
		MethodDebugInfo debugInfo;

		public DecompiledCodeProvider(IDecompiler decompiler, MethodDef method, CancellationToken cancellationToken) {
			this.decompiler = decompiler ?? throw new ArgumentNullException(nameof(decompiler));
			this.method = method ?? throw new ArgumentNullException(nameof(method));
			this.cancellationToken = cancellationToken;
			output = null;
			debugInfo = null;
		}

		public bool TryDecompile() {
			Debug.Assert(output == null);
			output = new DecompilerOutputImpl();

			if (!StateMachineHelpers.TryGetKickoffMethod(method, out var containingMethod))
				containingMethod = method;

			var ctx = new DecompilationContext() {
				CancellationToken = cancellationToken,
				CalculateILSpans = true,
				AsyncMethodBodyDecompilation = false,
			};
			var info = TryDecompileCode(containingMethod, method.MDToken.Raw, ctx, output);
			if (info.debugInfo == null && containingMethod != method) {
				output.Clear();
				// The decompiler can't decompile the iterator / async method, try again,
				// but only decompile the MoveNext method
				info = TryDecompileCode(method, method.MDToken.Raw, ctx, output);
			}
			debugInfo = info.debugInfo;
			return debugInfo != null;
		}

		public NativeVariableInfo[] CreateNativeVariableInfo() {
			if (debugInfo == null)
				return Array.Empty<NativeVariableInfo>();
			var list = new List<NativeVariableInfo>();
			foreach (var arg in debugInfo.Parameters) {
				var p = arg.Parameter;
				if (p == null || p.Index < 0)
					continue;
				var name = arg.Name;
				if (string.IsNullOrEmpty(name))
					continue;
				list.Add(new NativeVariableInfo(isLocal: false, p.Index, name));
			}
			foreach (var scope in GetScopes(debugInfo.Scope)) {
				foreach (var local in scope.Locals) {
					var l = local.Local;
					if (l == null)
						continue;
					var name = local.Name;
					if (string.IsNullOrEmpty(name))
						continue;
					list.Add(new NativeVariableInfo(isLocal: true, l.Index, name));
				}
			}
			return list.Count == 0 ? Array.Empty<NativeVariableInfo>() : list.ToArray();
		}

		static IEnumerable<MethodDebugScope> GetScopes(MethodDebugScope scope) {
			yield return scope;
			foreach (var childScope in scope.Scopes) {
				foreach (var s in GetScopes(childScope))
					yield return s;
			}
		}

		(MethodDebugInfo debugInfo, MethodDebugInfo stateMachineDebugInfoOrNull) TryDecompileCode(MethodDef method, uint methodToken, DecompilationContext ctx, DecompilerOutputImpl output) {
			output.Initialize(methodToken);
			decompiler.Decompile(method, output, ctx);
			var info = output.TryGetMethodDebugInfo();
			cancellationToken.ThrowIfCancellationRequested();
			return info;
		}

		public SourceStatementProvider CreateCodeProvider() {
			if (output == null || debugInfo == null)
				return default;
			return new SourceStatementProvider(output.ToString(), debugInfo);
		}

		public ILSourceStatementProvider CreateILCodeProvider() {
			Debug.Assert(decompiler.GenericGuid == DecompilerConstants.LANGUAGE_IL);
			if (output == null || debugInfo == null)
				return default;
			return new ILSourceStatementProvider(output.ToString(), debugInfo);
		}
	}
}
