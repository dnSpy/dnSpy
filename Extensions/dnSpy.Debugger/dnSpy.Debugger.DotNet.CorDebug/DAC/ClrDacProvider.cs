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
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using Microsoft.Diagnostics.Runtime;

namespace dnSpy.Debugger.DotNet.CorDebug.DAC {
	abstract class ClrDacProvider {
		public abstract ClrDac Create(int pid, string clrPath, IClrDacDebugger clrDacDebugger);
	}

	[Export(typeof(ClrDacProvider))]
	sealed class ClrDacProviderImpl : ClrDacProvider {
		public override ClrDac Create(int pid, string clrPath, IClrDacDebugger clrDacDebugger) {
			if (clrPath == null)
				throw new ArgumentNullException(nameof(clrPath));
			var clrDac = CreateCore(pid, clrPath, clrDacDebugger);
			Debug.Assert(clrDac != null);
			return clrDac ?? NullClrDac.Instance;
		}

		ClrDac CreateCore(int pid, string clrPath, IClrDacDebugger clrDacDebugger) {
			DataTarget dataTarget = null;
			bool failed = true;
			try {
				// The timeout isn't used if Passive is used
				dataTarget = DataTarget.AttachToProcess(pid, 0, AttachFlag.Passive);
				var clrInfo = GetClrInfo(dataTarget, clrPath);
				if (clrInfo == null)
					return null;

				// Use this overload to make sure it doesn't try to download the dac file which
				// will block this thread for several seconds or much longer.
				var clrRuntime = clrInfo.CreateRuntime(clrInfo.LocalMatchingDac);

				var clrDac = new ClrDacImpl(dataTarget, clrRuntime, clrDacDebugger);
				failed = false;
				return clrDac;
			}
			catch (ClrDiagnosticsException) {
				return null;
			}
			catch (IOException) {
				return null;
			}
			catch (InvalidOperationException) {
				return null;
			}
			finally {
				if (failed)
					dataTarget?.Dispose();
			}
		}

		static ClrInfo GetClrInfo(DataTarget dt, string clrPath) {
			foreach (var clrInfo in dt.ClrVersions) {
				if (!StringComparer.OrdinalIgnoreCase.Equals(clrInfo.ModuleInfo.FileName, clrPath))
					continue;
				Debug.Assert(File.Exists(clrInfo.LocalMatchingDac));
				if (!File.Exists(clrInfo.LocalMatchingDac))
					continue;
				return clrInfo;
			}
			return null;
		}
	}
}
