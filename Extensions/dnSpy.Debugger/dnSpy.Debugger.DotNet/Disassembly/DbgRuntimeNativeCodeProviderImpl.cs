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
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading;
using dnlib.DotNet;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.Breakpoints.Code;
using dnSpy.Contracts.Debugger.CallStack;
using dnSpy.Contracts.Debugger.Code;
using dnSpy.Contracts.Debugger.Disassembly;
using dnSpy.Contracts.Debugger.DotNet.Code;
using dnSpy.Contracts.Debugger.DotNet.Disassembly;
using dnSpy.Contracts.Debugger.DotNet.Evaluation;
using dnSpy.Contracts.Debugger.DotNet.Metadata;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Disassembly;
using dnSpy.Debugger.DotNet.Metadata;

namespace dnSpy.Debugger.DotNet.Disassembly {
	[ExportDbgRuntimeNativeCodeProvider(null, PredefinedDbgRuntimeKindGuids.DotNet)]
	sealed class DbgRuntimeNativeCodeProviderImpl : DbgRuntimeNativeCodeProvider {
		readonly Lazy<DbgMetadataService> dbgMetadataService;
		readonly Lazy<DbgModuleIdProviderService> dbgModuleIdProviderService;
		readonly IDecompilerService decompilerService;
		readonly IDecompiler? ilDecompiler;

		[ImportingConstructor]
		DbgRuntimeNativeCodeProviderImpl(Lazy<DbgMetadataService> dbgMetadataService, Lazy<DbgModuleIdProviderService> dbgModuleIdProviderService, IDecompilerService decompilerService) {
			this.dbgMetadataService = dbgMetadataService;
			this.dbgModuleIdProviderService = dbgModuleIdProviderService;
			this.decompilerService = decompilerService;
			ilDecompiler = decompilerService.AllDecompilers.FirstOrDefault(a => a.GenericGuid == DecompilerConstants.LANGUAGE_IL);
		}

		sealed class DotNetSymbolResolver : ISymbolResolver {
			readonly IDbgDotNetRuntime runtime;

			public DotNetSymbolResolver(IDbgDotNetRuntime runtime) => this.runtime = runtime;

			public void Resolve(ulong[] addresses, SymbolResolverResult[] result) {
				if (!runtime.Dispatcher.TryInvoke(() => ResolveCore(addresses, result))) {
					// process has exited
				}
			}

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

		bool CreateResult(IDbgDotNetRuntime runtime, DbgModule? methodModule, uint methodToken, string? header, DbgNativeCodeOptions options, DbgDotNetNativeCode nativeCode, out GetNativeCodeResult result) {
			if (methodToken == uint.MaxValue)
				methodToken = 0;

			var newBlocks = new NativeCodeBlock[nativeCode.Blocks.Length];
			for (int i = 0; i < newBlocks.Length; i++) {
				ref var blocks = ref nativeCode.Blocks[i];
				newBlocks[i] = new NativeCodeBlock(blocks.Kind, blocks.Address, blocks.Code, null);
			}

			IDecompiler? decompiler = decompilerService.Decompiler;
			if (!(decompiler is null) && decompiler.GenericGuid == DecompilerConstants.LANGUAGE_IL)
				decompiler = null;
			bool canShowILCode = (options & DbgNativeCodeOptions.ShowILCode) != 0 && !(ilDecompiler is null);
			bool canShowCode = (options & DbgNativeCodeOptions.ShowCode) != 0 && !(decompiler is null);
			NativeVariableInfo[]? nativeVariableInfo = null;
			if (!(methodModule is null) && methodToken != 0 && (canShowILCode || canShowCode) && HasSequencePoints(nativeCode)) {
				var module = dbgMetadataService.Value.TryGetMetadata(methodModule, DbgLoadModuleOptions.AutoLoaded);
				var method = module?.ResolveToken(methodToken) as MethodDef;
				if (!(method is null)) {
					var cancellationToken = CancellationToken.None;

					ILSourceStatementProvider ilCodeProvider = default;
					SourceStatementProvider codeProvider = default;
					List<int>? ilOffsets = null;

					if (canShowILCode) {
						Debug2.Assert(!(ilDecompiler is null));
						var provider = new DecompiledCodeProvider(ilDecompiler, method, cancellationToken);
						if (provider.TryDecompile())
							ilCodeProvider = provider.CreateILCodeProvider();
					}

					if (canShowCode) {
						var provider = new DecompiledCodeProvider(decompiler, method, cancellationToken);
						if (provider.TryDecompile()) {
							codeProvider = provider.CreateCodeProvider();
							nativeVariableInfo = provider.CreateNativeVariableInfo();
						}
					}

					var commentBuilder = new StringBuilder();
					var nativeBlocks = nativeCode.Blocks;
					for (int i = 0; i < newBlocks.Length; i++) {
						int ilOffset = nativeBlocks[i].ILOffset;
						if (ilOffset < 0)
							continue;

						var block = newBlocks[i];

						var info = codeProvider.GetStatement(ilOffset);
						AddStatement(commentBuilder, info.line, info.span, showStmt: true);
						if (!ilCodeProvider.IsDefault) {
							if (ilOffsets is null)
								ilOffsets = GetILOffsets(nativeBlocks);
							int endILOffset = GetNextILOffset(ilOffsets, ilOffset);
							if (endILOffset < 0)
								endILOffset = ilOffset + 1;
							info = ilCodeProvider.GetStatement(ilOffset, endILOffset);
							AddStatement(commentBuilder, info.line, info.span, showStmt: false);
						}

						if (commentBuilder.Length != 0)
							newBlocks[i] = new NativeCodeBlock(block.Kind, block.Address, block.Code, commentBuilder.ToString());
						commentBuilder.Length = 0;
					}
				}
			}

			var newCode = new NativeCode(nativeCode.Kind, nativeCode.Optimization, newBlocks, nativeCode.CodeInfo, nativeVariableInfo, nativeCode.MethodName, nativeCode.ShortMethodName, nativeCode.ModuleName);
			var symbolResolver = new DotNetSymbolResolver(runtime);
			result = new GetNativeCodeResult(newCode, symbolResolver, header);
			return true;
		}

		static List<int> GetILOffsets(DbgDotNetNativeCodeBlock[] blocks) {
			var list = new List<int>(blocks.Length);
			foreach (var block in blocks) {
				if (block.ILOffset >= 0)
					list.Add(block.ILOffset);
			}
			list.Sort();
			return list;
		}

		static int GetNextILOffset(List<int> sortedOffsets, int ilOffset) {
			int index = sortedOffsets.BinarySearch(ilOffset);
			if (index < 0)
				return -1;
			while (index + 1 < sortedOffsets.Count) {
				if (sortedOffsets[index + 1] != ilOffset)
					break;
				index++;
			}
			if (index + 1 == sortedOffsets.Count)
				return int.MaxValue;
			return sortedOffsets[index + 1];
		}

		void AddStatement(StringBuilder sb, string lines, TextSpan span, bool showStmt) {
			if (lines is null)
				return;

			Debug.Assert(span.End <= lines.Length);
			int pos = 0;
			while (pos < lines.Length) {
				int nextLineOffset;
				int eol = lines.IndexOf('\n', pos);
				if (eol < 0) {
					eol = lines.Length;
					nextLineOffset = eol;
				}
				else {
					nextLineOffset = eol + 1;
					if (eol > 0 && lines[eol - 1] == '\r')
						eol--;
				}
				sb.Append(lines, pos, eol - pos);
				sb.AppendLine();

				// Show statement, but only if it's the first line, and if there are multiple statements on the same line
				if (showStmt && pos == 0) {
					int nonSpace = FindNonSpace(lines, pos, eol);
					int stmtEnd = Math.Min(eol, span.End);
					if (!(nonSpace >= span.Start && stmtEnd == eol)) {
						sb.Append(' ', span.Start - pos);
						sb.Append('^', stmtEnd - span.Start);
						sb.AppendLine();
					}
				}

				pos = nextLineOffset;
			}
		}

		static int FindNonSpace(string lines, int pos, int end) {
			while (pos < end) {
				if (!char.IsWhiteSpace(lines[pos]))
					return pos;
				pos++;
			}
			return -1;
		}

		static bool TryGetDotNetRuntime(DbgRuntime dbgRuntime, [NotNullWhen(true)] out IDbgDotNetRuntime? runtime) {
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

			uint methodToken = frame.FunctionToken;
			const string? header = null;
			return CreateResult(runtime, frame.Module, methodToken, header, options, nativeCode, out result);
		}

		public override bool CanGetNativeCode(DbgBoundCodeBreakpoint boundBreakpoint) =>
			CanGetNativeCode(boundBreakpoint.Runtime, boundBreakpoint.Breakpoint.Location);

		public override bool TryGetNativeCode(DbgBoundCodeBreakpoint boundBreakpoint, DbgNativeCodeOptions options, out GetNativeCodeResult result) =>
			TryGetNativeCode(boundBreakpoint.Runtime, boundBreakpoint.Breakpoint.Location, options, out result);

		public override bool CanGetNativeCode(DbgRuntime dbgRuntime, DbgCodeLocation location) {
			if (!TryGetDotNetRuntime(dbgRuntime, out var runtime))
				return false;

			return location is IDbgDotNetCodeLocation loc;
		}

		public override bool TryGetNativeCode(DbgRuntime dbgRuntime, DbgCodeLocation location, DbgNativeCodeOptions options, out GetNativeCodeResult result) {
			result = default;
			if (!TryGetDotNetRuntime(dbgRuntime, out var runtime))
				return false;

			if (!(location is IDbgDotNetCodeLocation loc))
				return false;

			var module = loc.DbgModule ?? dbgModuleIdProviderService.Value.GetModule(loc.Module);
			if (module is null)
				return false;
			var reflectionModule = module.GetReflectionModule();
			if (reflectionModule is null)
				return false;

			var reflectionMethod = reflectionModule.ResolveMethod((int)loc.Token);
			if (reflectionMethod is null)
				return false;

			if (!runtime.TryGetNativeCode(reflectionMethod, out var nativeCode))
				return false;

			const string? header = null;
			return CreateResult(runtime, module, loc.Token, header, options, nativeCode, out result);
		}
	}
}
