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
	public sealed class AttachProcessOptions {
		/// <summary>
		/// null if we should auto detect the version, else it should be a version of an already
		/// installed CLR, eg. "v2.0.50727" etc.
		/// </summary>
		public string DebuggeeVersion { get; set; }

		/// <summary>
		/// Process ID of the process that will be debugged
		/// </summary>
		public int ProcessId { get; set; }

		/// <summary>
		/// An <see cref="IDebugMessageDispatcher"/> instance. Can't be null.
		/// </summary>
		public IDebugMessageDispatcher DebugMessageDispatcher { get; set; }

		/// <summary>
		/// Debug options
		/// </summary>
		public DebugOptions DebugOptions { get; set; }

		public AttachProcessOptions() {
			this.DebugOptions = new DebugOptions();
		}

		public DebugProcessOptions CopyTo(DebugProcessOptions other) {
			other.DebuggeeVersion = this.DebuggeeVersion;
			other.DebugMessageDispatcher = this.DebugMessageDispatcher;
			other.DebugOptions = this.DebugOptions == null ? null : this.DebugOptions.Clone();
			return other;
		}

		public DebugProcessOptions Clone() {
			return CopyTo(new DebugProcessOptions());
		}
	}
}
