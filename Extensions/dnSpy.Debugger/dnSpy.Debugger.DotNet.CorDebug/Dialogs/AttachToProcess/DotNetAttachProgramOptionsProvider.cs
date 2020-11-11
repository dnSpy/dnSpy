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
using dndbg.Engine;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.Attach;
using dnSpy.Debugger.DotNet.CorDebug.Impl;
using dnSpy.Debugger.DotNet.CorDebug.Impl.Attach;
using dnSpy.Debugger.DotNet.CorDebug.Utilities;

namespace dnSpy.Debugger.DotNet.CorDebug.Dialogs.AttachToProcess {
	[ExportAttachProgramOptionsProviderFactory(PredefinedAttachProgramOptionsProviderNames.DotNet)]
	sealed class DotNetAttachProgramOptionsProviderFactory : AttachProgramOptionsProviderFactory {
		public override AttachProgramOptionsProvider? Create(bool allFactories) => new DotNetAttachProgramOptionsProvider();
	}

	sealed class DotNetAttachProgramOptionsProvider : AttachProgramOptionsProvider {
		public override IEnumerable<AttachProgramOptions> Create(AttachProgramOptionsProviderContext context) {
			foreach (var process in DebuggableProcesses.GetProcesses(context.ProcessIds, context.IsValidProcess, context.CancellationToken)) {
				foreach (var info in TryGetCoreCLRInfos(process)) {
					context.CancellationToken.ThrowIfCancellationRequested();
					yield return info;
				}
			}
		}

		IEnumerable<DotNetAttachProgramOptions> TryGetCoreCLRInfos(Process process) {
			// We can only debug processes with the same bitness
			int bitness = IntPtr.Size * 8;
			var dbgShimFilename = DotNetHelpers.GetDebugShimFilename(bitness);
			foreach (var ccInfo in CoreCLRHelper.GetCoreCLRInfos(process.Id, null, dbgShimFilename))
				yield return new DotNetAttachProgramOptions(process.Id, ccInfo.CoreCLRTypeInfo.Version, ccInfo.CoreCLRTypeInfo.CoreCLRFilename);
		}
	}

	sealed class DotNetAttachProgramOptions : AttachProgramOptions {
		public override int ProcessId { get; }
		public override RuntimeId RuntimeId { get; }
		public override string RuntimeName { get; }
		public override Guid RuntimeGuid => PredefinedDbgRuntimeGuids.DotNet_Guid;
		public override Guid RuntimeKindGuid => PredefinedDbgRuntimeKindGuids.DotNet_Guid;

		readonly string? clrModuleVersion;
		readonly string? coreCLRFilename;

		public DotNetAttachProgramOptions(int pid, string? clrModuleVersion, string? coreCLRFilename) {
			ProcessId = pid;
			RuntimeId = new DotNetRuntimeId(clrModuleVersion);
			RuntimeName = "CoreCLR " + clrModuleVersion;
			this.clrModuleVersion = clrModuleVersion;
			this.coreCLRFilename = coreCLRFilename;
		}

		public override AttachToProgramOptions GetOptions() => new DotNetAttachToProgramOptions {
			ProcessId = ProcessId,
			ClrModuleVersion = clrModuleVersion,
			CoreCLRFilename = coreCLRFilename,
		};
	}
}
