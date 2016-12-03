/*
    Copyright (C) 2014-2016 de4dot@gmail.com

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
	public abstract class CLRTypeDebugInfo {
		public abstract CLRType CLRType { get; }
		public abstract CLRTypeDebugInfo Clone();
	}

	public sealed class DesktopCLRTypeDebugInfo : CLRTypeDebugInfo {
		public override CLRType CLRType => CLRType.Desktop;

		/// <summary>
		/// null if we should auto detect the version, else it should be a version of an already
		/// installed CLR, eg. "v2.0.50727" etc.
		/// </summary>
		public string DebuggeeVersion { get; set; }

		public DesktopCLRTypeDebugInfo()
			: this(null) {
		}

		public DesktopCLRTypeDebugInfo(string debuggeeVersion) {
			DebuggeeVersion = debuggeeVersion;
		}

		public override CLRTypeDebugInfo Clone() => new DesktopCLRTypeDebugInfo(DebuggeeVersion);
	}

	public sealed class CoreCLRTypeDebugInfo : CLRTypeDebugInfo {
		/// <summary>
		/// dbgshim.dll filename or null
		/// </summary>
		public string DbgShimFilename { get; set; }

		/// <summary>
		/// Host filename
		/// </summary>
		public string HostFilename { get; set; }

		/// <summary>
		/// Host command line
		/// </summary>
		public string HostCommandLine { get; set; }

		public override CLRType CLRType => CLRType.CoreCLR;

		public CoreCLRTypeDebugInfo(string dbgShimFilename, string hostFilename, string hostCommandLine) {
			DbgShimFilename = dbgShimFilename;
			HostFilename = hostFilename;
			HostCommandLine = hostCommandLine;
		}

		public override CLRTypeDebugInfo Clone() => new CoreCLRTypeDebugInfo(DbgShimFilename, HostFilename, HostCommandLine);
	}

	public sealed class DebugProcessOptions {
		/// <summary>
		/// Use desktop or CoreCLR?
		/// </summary>
		public CLRTypeDebugInfo CLRTypeDebugInfo { get; private set; }

		/// <summary>
		/// File to debug
		/// </summary>
		public string Filename { get; set; }

		/// <summary>
		/// Command line to pass to debugged program
		/// </summary>
		public string CommandLine { get; set; }

		/// <summary>
		/// Current directory of debugged program or null to use the debugger's cwd
		/// </summary>
		public string CurrentDirectory { get; set; }

		/// <summary>
		/// true if handles should be inherited by the started process
		/// </summary>
		public bool InheritHandles { get; set; }

		/// <summary>
		/// Process creation flags passed to CreateProcess()
		/// </summary>
		public ProcessCreationFlags? ProcessCreationFlags { get; set; }

		/// <summary>
		/// An <see cref="IDebugMessageDispatcher"/> instance. Can't be null.
		/// </summary>
		public IDebugMessageDispatcher DebugMessageDispatcher { get; set; }

		/// <summary>
		/// Debug options
		/// </summary>
		public DebugOptions DebugOptions { get; set; }

		/// <summary>
		/// Decides when to break the created process
		/// </summary>
		public BreakProcessKind BreakProcessKind { get; set; }

		public static readonly ProcessCreationFlags DefaultProcessCreationFlags = Engine.ProcessCreationFlags.CREATE_NEW_CONSOLE;

		public DebugProcessOptions(CLRTypeDebugInfo info) {
			CLRTypeDebugInfo = info;
			DebugOptions = new DebugOptions();
			BreakProcessKind = BreakProcessKind.None;
		}

		public DebugProcessOptions CopyTo(DebugProcessOptions other) {
			other.CLRTypeDebugInfo = CLRTypeDebugInfo.Clone();
			other.Filename = Filename;
			other.CommandLine = CommandLine;
			other.CurrentDirectory = CurrentDirectory;
			other.InheritHandles = InheritHandles;
			other.ProcessCreationFlags = ProcessCreationFlags;
			other.DebugMessageDispatcher = DebugMessageDispatcher;
			other.DebugOptions = DebugOptions == null ? null : DebugOptions.Clone();
			other.BreakProcessKind = BreakProcessKind;
			return other;
		}

		public DebugProcessOptions Clone() => CopyTo(new DebugProcessOptions(CLRTypeDebugInfo));
	}
}
