/*
    Copyright (C) 2014-2015 de4dot@gmail.com

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
	public sealed class DebugProcessOptions {
		/// <summary>
		/// null if we should auto detect the version, else it should be a version of an already
		/// installed CLR, eg. "v2.0.50727" etc.
		/// </summary>
		public string DebuggeeVersion { get; set; }

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

		public static readonly ProcessCreationFlags DefaultProcessCreationFlags = Engine.ProcessCreationFlags.CREATE_NEW_CONSOLE;

		public DebugProcessOptions CopyTo(DebugProcessOptions other) {
			other.DebuggeeVersion = this.DebuggeeVersion;
			other.Filename = this.Filename;
			other.CommandLine = this.CommandLine;
			other.CurrentDirectory = this.CurrentDirectory;
			other.InheritHandles = this.InheritHandles;
			other.ProcessCreationFlags = this.ProcessCreationFlags;
			other.DebugMessageDispatcher = this.DebugMessageDispatcher;
			other.DebugOptions = this.DebugOptions == null ? null : this.DebugOptions.Clone();
			return other;
		}

		public DebugProcessOptions Clone() {
			return CopyTo(new DebugProcessOptions());
		}
	}
}
