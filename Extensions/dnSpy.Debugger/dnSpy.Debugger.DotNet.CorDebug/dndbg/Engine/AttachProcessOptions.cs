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

namespace dndbg.Engine {
	enum CLRType {
		Desktop,
		CoreCLR,
	}

	abstract class CLRTypeAttachInfo {
		public abstract CLRType CLRType { get; }
		public abstract string? Version { get; }
	}

	sealed class DesktopCLRTypeAttachInfo : CLRTypeAttachInfo {
		public override CLRType CLRType => CLRType.Desktop;
		public override string? Version => DebuggeeVersion;

		/// <summary>
		/// null if we should auto detect the version, else it should be a version of an already
		/// installed CLR, eg. "v2.0.50727" etc.
		/// </summary>
		public string? DebuggeeVersion { get; }

		public DesktopCLRTypeAttachInfo(string? debuggeeVersion) => DebuggeeVersion = debuggeeVersion;
	}

	sealed class CoreCLRTypeAttachInfo : CLRTypeAttachInfo {
		public override CLRType CLRType => CLRType.CoreCLR;

		public override string? Version { get; }
		public string? DbgShimFilename { get; }
		public string? CoreCLRFilename { get; }

		public CoreCLRTypeAttachInfo(string? version, string? dbgShimFilename, string? coreclrFilename) {
			Version = version;
			DbgShimFilename = dbgShimFilename;
			CoreCLRFilename = coreclrFilename;
		}
	}

	sealed class AttachProcessOptions {
		/// <summary>
		/// Info needed to attach to the CLR
		/// </summary>
		public CLRTypeAttachInfo CLRTypeAttachInfo { get; }

		/// <summary>
		/// Process ID of the process that will be debugged
		/// </summary>
		public int ProcessId { get; set; }

		/// <summary>
		/// An <see cref="IDebugMessageDispatcher"/> instance. Can't be null.
		/// </summary>
		public IDebugMessageDispatcher? DebugMessageDispatcher { get; set; }

		/// <summary>
		/// Debug options
		/// </summary>
		public DebugOptions DebugOptions { get; set; }

		public AttachProcessOptions(CLRTypeAttachInfo info) {
			DebugOptions = new DebugOptions();
			CLRTypeAttachInfo = info;
		}
	}
}
