/*
    Copyright (C) 2014-2018 de4dot@gmail.com

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

using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.Breakpoints.Code;
using dnSpy.Contracts.Debugger.CallStack;
using dnSpy.Contracts.Debugger.Code;
using dnSpy.Contracts.Debugger.Disassembly;
using dnSpy.Contracts.Debugger.DotNet.Disassembly;
using dnSpy.Contracts.Debugger.DotNet.Evaluation;
using dnSpy.Contracts.Debugger.DotNet.Metadata;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Disassembly;

namespace dnSpy.Debugger.DotNet.Disassembly {
	[ExportDbgRuntimeNativeCodeProvider(null, PredefinedDbgRuntimeKindGuids.DotNet)]
	sealed class DbgRuntimeNativeCodeProviderImpl : DbgRuntimeNativeCodeProvider {
		readonly DbgMetadataService dbgMetadataService;
		readonly IDecompilerService decompilerService;
		readonly IDecompiler ilDecompiler;

		[ImportingConstructor]
		DbgRuntimeNativeCodeProviderImpl(DbgMetadataService dbgMetadataService, IDecompilerService decompilerService) {
			this.dbgMetadataService = dbgMetadataService;
			this.decompilerService = decompilerService;
			ilDecompiler = decompilerService.AllDecompilers.FirstOrDefault(a => a.GenericGuid == DecompilerConstants.LANGUAGE_IL);
		}

		sealed class DotNetSymbolResolver : ISymbolResolver {
			readonly IDbgDotNetRuntime runtime;

			public DotNetSymbolResolver(IDbgDotNetRuntime runtime) => this.runtime = runtime;

			public void Resolve(ulong[] addresses, SymbolResolverResult[] result) =>
				runtime.Dispatcher.Invoke(() => ResolveCore(addresses, result));

			void ResolveCore(ulong[] addresses, SymbolResolverResult[] result) {
				Debug.Assert(addresses.Length == result.Length);
				runtime.Dispatcher.VerifyAccess();
				for (int i = 0; i < addresses.Length; i++) {
					ulong address = addresses[i];
					if (runtime.TryGetSymbol(address, out var symResult))
						result[i] = symResult;
				}
			}
		}

		static bool HasSequencePoints(in DbgDotNetNativeCode nativeCode) {
			foreach (var block in nativeCode.Blocks) {
				if (block.ILOffset >= 0)
					return true;
			}
			return false;
		}

		bool CreateResult(IDbgDotNetRuntime runtime, DbgModule methodModule, int methodToken, string header, DbgNativeCodeOptions options, DbgDotNetNativeCode nativeCode, out GetNativeCodeResult result) {
			var newBlocks = new NativeCodeBlock[nativeCode.Blocks.Length];
			for (int i = 0; i < newBlocks.Length; i++) {
				ref var blocks = ref nativeCode.Blocks[i];
				newBlocks[i] = new NativeCodeBlock(blocks.Kind, blocks.Address, blocks.Code, null);
			}

			var decompiler = decompilerService.Decompiler;
			bool canShowILCode = (options & DbgNativeCodeOptions.ShowILCode) != 0 && ilDecompiler != null;
			bool canShowCode = (options & DbgNativeCodeOptions.ShowCode) != 0 && decompiler != null;
			if (methodModule != null && methodToken != 0 && (canShowILCode || canShowCode) && HasSequencePoints(nativeCode)) {
				var module = dbgMetadataService.TryGetMetadata(methodModule, DbgLoadModuleOptions.AutoLoaded);
				var method = module?.ResolveToken(methodToken);
				if (method != null) {
					if (canShowILCode) {
						//TODO:
					}

					if (canShowCode) {
						//TODO:
					}
				}
			}

			var newCode = new NativeCode(nativeCode.Kind, nativeCode.Optimization, newBlocks);
			var symbolResolver = new DotNetSymbolResolver(runtime);
			result = new GetNativeCodeResult(newCode, symbolResolver, header);
			return true;
		}

		static bool TryGetDotNetRuntime(DbgRuntime dbgRuntime, out IDbgDotNetRuntime runtime) {
			runtime = null;
			if (dbgRuntime.Process.State != DbgProcessState.Paused || dbgRuntime.IsClosed)
				return false;
			runtime = dbgRuntime.GetDotNetRuntime();
			if ((runtime.Features & DbgDotNetRuntimeFeatures.NativeMethodBodies) == 0) {
				runtime = null;
				return false;
			}
			return true;
		}

		public override bool CanGetNativeCode(DbgStackFrame frame) {
			if (!TryGetDotNetRuntime(frame.Runtime, out var runtime))
				return false;

			// If it's an IL frame (very likely), the body should be available, else we'll fail later in the next method
			return true;
		}

		public override bool TryGetNativeCode(DbgStackFrame frame, DbgNativeCodeOptions options, out GetNativeCodeResult result) {
			result = default;
			if (!TryGetDotNetRuntime(frame.Runtime, out var runtime))
				return false;

			if (!runtime.TryGetNativeCode(frame, out var nativeCode))
				return false;

			const int methodToken = 0;//TODO:
			const string header = null;//TODO:
			return CreateResult(runtime, frame.Module, methodToken, header, options, nativeCode, out result);
		}

		public override bool CanGetNativeCode(DbgBoundCodeBreakpoint boundBreakpoint) {
			if (!TryGetDotNetRuntime(boundBreakpoint.Runtime, out var runtime))
				return false;

			return false;//TODO:
		}

		public override bool TryGetNativeCode(DbgBoundCodeBreakpoint boundBreakpoint, DbgNativeCodeOptions options, out GetNativeCodeResult result) {
			result = default;
			if (!TryGetDotNetRuntime(boundBreakpoint.Runtime, out var runtime))
				return false;

			return false;//TODO:
		}

		public override bool CanGetNativeCode(DbgRuntime dbgRuntime, DbgCodeLocation location) {
			if (!TryGetDotNetRuntime(dbgRuntime, out var runtime))
				return false;

			return false;//TODO:
		}

		public override bool TryGetNativeCode(DbgRuntime dbgRuntime, DbgCodeLocation location, DbgNativeCodeOptions options, out GetNativeCodeResult result) {
			result = default;
			if (!TryGetDotNetRuntime(dbgRuntime, out var runtime))
				return false;

			return false;//TODO:
		}
	}
}
