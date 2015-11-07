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

using System.IO;
using dndbg.Engine;
using dnlib.PE;
using dnSpy.Shared.UI.MVVM;

namespace dnSpy.Debugger.Dialogs {
	sealed class ProcessVM : ViewModelBase {
		public string Filename {
			get { return Path.GetFileName(fullPath); }
		}

		public object FullPathObject { get { return this; } }
		public object FilenameObject { get { return this; } }
		public object PIDObject { get { return this; } }
		public object CLRVersionObject { get { return this; } }
		public object MachineObject { get { return this; } }
		public object TitleObject { get { return this; } }
		public object TypeObject { get { return this; } }

		public string FullPath {
			get { return fullPath; }
		}
		readonly string fullPath;

		public int PID {
			get { return pid; }
		}
		readonly int pid;

		public string Title {
			get { return title; }
		}
		readonly string title;

		public Machine Machine {
			get { return machine; }
		}
		readonly Machine machine;

		public string CLRVersion {
			get { return clrVersion; }
		}
		readonly string clrVersion;

		public CLRTypeAttachInfo CLRTypeInfo {
			get { return type; }
		}
		readonly CLRTypeAttachInfo type;

		public ProcessVM(int pid, string title, Machine machine, CLRTypeAttachInfo type, string fullPath) {
			this.fullPath = fullPath;
			this.pid = pid;
			this.title = title;
			this.machine = machine;
			this.clrVersion = type.Version;
			this.type = type;
		}
	}
}
