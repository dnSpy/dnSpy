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

using System.IO;
using dndbg.Engine;
using dnlib.PE;
using dnSpy.Contracts.MVVM;

namespace dnSpy.Debugger.Dialogs {
	sealed class ProcessVM : ViewModelBase {
		public string Filename => Path.GetFileName(FullPath);
		public object FullPathObject => this;
		public object FilenameObject => this;
		public object PIDObject => this;
		public object CLRVersionObject => this;
		public object MachineObject => this;
		public object TitleObject => this;
		public object TypeObject => this;
		public string FullPath { get; }
		public int PID { get; }
		public string Title { get; }
		public Machine Machine { get; }
		public string CLRVersion { get; }
		public CLRTypeAttachInfo CLRTypeInfo { get; }
		public IProcessContext Context { get; }

		public ProcessVM(int pid, string title, Machine machine, CLRTypeAttachInfo type, string fullPath, IProcessContext context) {
			this.FullPath = fullPath;
			this.PID = pid;
			this.Title = title;
			this.Machine = machine;
			this.CLRVersion = type.Version;
			this.CLRTypeInfo = type;
			this.Context = context;
		}
	}
}
