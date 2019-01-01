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

using System.Collections.Generic;
using System.Diagnostics;
using dnSpy.Contracts.Debugger.Attach;
using dnSpy.Debugger.DotNet.Mono.Properties;

namespace dnSpy.Debugger.DotNet.Mono.Dialogs.AttachToProcess {
	[ExportAttachProgramOptionsProviderFactory(PredefinedAttachProgramOptionsProviderNames.UnityEditor)]
	sealed class UnityEditorAttachProgramOptionsProviderFactory : AttachProgramOptionsProviderFactory {
		public override AttachProgramOptionsProvider Create(bool allFactories) => allFactories ? null : new UnityEditorAttachProgramOptionsProvider();
	}

	sealed class UnityEditorAttachProgramOptionsProvider : AttachProgramOptionsProvider {
		public override IEnumerable<AttachProgramOptions> Create(AttachProgramOptionsProviderContext context) {
			var processes = Process.GetProcessesByName("Unity");
			try {
				foreach (var p in processes) {
					if (!ProcessUtils.IsValidProcess(context, p.Id, p))
						continue;
					context.CancellationToken.ThrowIfCancellationRequested();
					ushort port = (ushort)(56000 + p.Id % 1000);
					const string ipAddress = "127.0.0.1";
					yield return new UnityAttachProgramOptionsImpl(p.Id, ipAddress, port, $"Unity ({dnSpy_Debugger_DotNet_Mono_Resources.UnityEditor})");
				}
			}
			finally {
				foreach (var p in processes)
					p.Dispose();
			}
		}
	}
}
