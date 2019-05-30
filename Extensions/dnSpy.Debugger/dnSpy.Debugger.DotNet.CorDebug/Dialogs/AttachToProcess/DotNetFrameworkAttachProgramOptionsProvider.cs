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
using System.Text;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.Attach;
using dnSpy.Debugger.DotNet.CorDebug.Impl;
using dnSpy.Debugger.DotNet.CorDebug.Impl.Attach;
using dnSpy.Debugger.DotNet.CorDebug.Native;
using DEMH = dndbg.COM.MetaHost;

namespace dnSpy.Debugger.DotNet.CorDebug.Dialogs.AttachToProcess {
	[ExportAttachProgramOptionsProviderFactory(PredefinedAttachProgramOptionsProviderNames.DotNetFramework)]
	sealed class DotNetFrameworkAttachProgramOptionsProviderFactory : AttachProgramOptionsProviderFactory {
		public override AttachProgramOptionsProvider? Create(bool allFactories) => new DotNetFrameworkAttachProgramOptionsProvider();
	}

	sealed class DotNetFrameworkAttachProgramOptionsProvider : AttachProgramOptionsProvider {
		public override IEnumerable<AttachProgramOptions> Create(AttachProgramOptionsProviderContext context) {
			var clsid = new Guid("9280188D-0E8E-4867-B30C-7FA83884E8DE");
			var riid = typeof(DEMH.ICLRMetaHost).GUID;
			var mh = (DEMH.ICLRMetaHost)NativeMethods.CLRCreateInstance(ref clsid, ref riid);
			foreach (var process in DebuggableProcesses.GetProcesses(context.ProcessIds, context.IsValidProcess, context.CancellationToken)) {
				int hr = mh.EnumerateLoadedRuntimes(process.Handle, out var iter);
				if (hr >= 0) {
					for (;;) {
						context.CancellationToken.ThrowIfCancellationRequested();
						hr = iter.Next(1, out object obj, out uint fetched);
						if (hr < 0 || fetched == 0)
							break;

						var rtInfo = (DEMH.ICLRRuntimeInfo)obj;
						uint chBuffer = 0;
						var sb = new StringBuilder(300);
						hr = rtInfo.GetVersionString(sb, ref chBuffer);
						sb.EnsureCapacity((int)chBuffer);
						hr = rtInfo.GetVersionString(sb, ref chBuffer);

						yield return new DotNetFrameworkAttachProgramOptions(process.Id, sb.ToString());
					}
				}
			}
		}
	}

	sealed class DotNetFrameworkAttachProgramOptions : AttachProgramOptions {
		public override int ProcessId { get; }
		public override RuntimeId RuntimeId { get; }
		public override string RuntimeName { get; }
		public override Guid RuntimeGuid => PredefinedDbgRuntimeGuids.DotNetFramework_Guid;
		public override Guid RuntimeKindGuid => PredefinedDbgRuntimeKindGuids.DotNet_Guid;

		readonly string debuggeeVersion;

		public DotNetFrameworkAttachProgramOptions(int pid, string clrVersion) {
			ProcessId = pid;
			RuntimeId = new DotNetFrameworkRuntimeId(clrVersion);
			RuntimeName = "CLR " + clrVersion;
			debuggeeVersion = clrVersion;
		}

		public override AttachToProgramOptions GetOptions() => new DotNetFrameworkAttachToProgramOptions {
			ProcessId = ProcessId,
			DebuggeeVersion = debuggeeVersion,
		};
	}
}
