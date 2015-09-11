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
using dnSpy.MVVM;

namespace dnSpy.Debugger.Dialogs {
	sealed class ProcessVM : ViewModelBase {
		public string Filename {
			get { return Path.GetFileName(fullPath); }
		}

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

		public string Type {
			get { return type; }
		}
		readonly string type;

		public string CLRVersion {
			get { return clrVersion; }
		}
		readonly string clrVersion;

		public ProcessVM(int pid, string title, string type, string clrVer, string fullPath) {
			this.fullPath = fullPath;
			this.pid = pid;
			this.title = title;
			this.type = type;
			this.clrVersion = CLRVersion;
		}
	}
}
