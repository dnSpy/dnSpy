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

using System;
using System.IO;
using System.Windows.Input;
using dndbg.Engine;
using dnSpy.Debugger.Properties;
using dnSpy.Shared.MVVM;

namespace dnSpy.Debugger.Dialogs {
	sealed class DebugCoreCLRVM : ViewModelBase {
		public IPickFilename PickFilename {
			set { pickFilename = value; }
		}
		IPickFilename pickFilename;

		public IPickDirectory PickDirectory {
			set { pickDirectory = value; }
		}
		IPickDirectory pickDirectory;

		public ICommand PickDbgShimFilenameCommand => new RelayCommand(a => PickNewDbgShimFilename());
		public ICommand PickHostFilenameCommand => new RelayCommand(a => PickNewHostFilename());
		public ICommand PickFilenameCommand => new RelayCommand(a => PickNewFilename());
		public ICommand PickCurrentDirectoryCommand => new RelayCommand(a => PickNewCurrentDirectory());

		public EnumListVM BreakProcessKindVM => breakProcessKindVM;
		readonly EnumListVM breakProcessKindVM = new EnumListVM(DebugProcessVM.breakProcessKindList);

		public BreakProcessKind BreakProcessKind {
			get { return (BreakProcessKind)BreakProcessKindVM.SelectedItem; }
			set { BreakProcessKindVM.SelectedItem = value; }
		}

		public string DbgShimFilename {
			get { return dbgShimFilename; }
			set {
				if (dbgShimFilename != value) {
					dbgShimFilename = value;
					OnPropertyChanged(nameof(DbgShimFilename));
					HasErrorUpdated();
				}
			}
		}
		string dbgShimFilename;

		public string HostFilename {
			get { return hostFilename; }
			set {
				if (hostFilename != value) {
					hostFilename = value;
					OnPropertyChanged(nameof(HostFilename));
					HasErrorUpdated();
				}
			}
		}
		string hostFilename;

		public string HostCommandLine {
			get { return hostCommandLine; }
			set {
				if (hostCommandLine != value) {
					hostCommandLine = value;
					OnPropertyChanged(nameof(HostCommandLine));
				}
			}
		}
		string hostCommandLine;

		public string Filename {
			get { return filename; }
			set {
				if (filename != value) {
					filename = value;
					OnPropertyChanged(nameof(Filename));
					HasErrorUpdated();
					var path = GetPath(filename);
					if (path != null)
						CurrentDirectory = path;
				}
			}
		}
		string filename;

		public string CommandLine {
			get { return commandLine; }
			set {
				if (commandLine != value) {
					commandLine = value;
					OnPropertyChanged(nameof(CommandLine));
				}
			}
		}
		string commandLine;

		public string CurrentDirectory {
			get { return currentDirectory; }
			set {
				if (currentDirectory != value) {
					currentDirectory = value;
					OnPropertyChanged(nameof(CurrentDirectory));
				}
			}
		}
		string currentDirectory;

		public DebugCoreCLRVM() {
		}

		static string GetPath(string file) {
			try {
				return Path.GetDirectoryName(file);
			}
			catch {
			}
			return null;
		}

		void PickNewDbgShimFilename() {
			if (pickFilename == null)
				throw new InvalidOperationException();

			var filter = string.Format("dbgshim.dll|dbgshim.dll|{0} (*.*)|*.*", dnSpy_Debugger_Resources.AllFiles);
			var newFilename = pickFilename.GetFilename(DbgShimFilename, "exe", filter);
			if (newFilename == null)
				return;

			this.DbgShimFilename = newFilename;
		}

		void PickNewHostFilename() {
			if (pickFilename == null)
				throw new InvalidOperationException();

			var newFilename = pickFilename.GetFilename(HostFilename, "exe", PickFilenameConstants.ExecutableFilter);
			if (newFilename == null)
				return;

			this.HostFilename = newFilename;
		}

		void PickNewFilename() {
			if (pickFilename == null)
				throw new InvalidOperationException();

			var newFilename = pickFilename.GetFilename(Filename, "exe", PickFilenameConstants.DotNetExecutableFilter);
			if (newFilename == null)
				return;

			Filename = newFilename;
		}

		void PickNewCurrentDirectory() {
			if (pickDirectory == null)
				throw new InvalidOperationException();

			var newDir = pickDirectory.GetDirectory(currentDirectory);
			if (newDir == null)
				return;

			CurrentDirectory = newDir;
		}

		public DebugCoreCLRVM Clone() => CopyTo(new DebugCoreCLRVM());

		public DebugCoreCLRVM CopyTo(DebugCoreCLRVM other) {
			other.DbgShimFilename = this.DbgShimFilename;
			other.HostFilename = this.HostFilename;
			other.HostCommandLine = this.HostCommandLine;
			other.Filename = this.Filename;
			other.CommandLine = this.CommandLine;
			other.CurrentDirectory = this.CurrentDirectory;
			other.BreakProcessKind = this.BreakProcessKind;
			return other;
		}

		static string VerifyFilename(string filename) {
			if (!File.Exists(filename)) {
				if (string.IsNullOrWhiteSpace(filename))
					return dnSpy_Debugger_Resources.Error_MissingFilename;
				return dnSpy_Debugger_Resources.Error_FileDoesNotExist;
			}
			return string.Empty;
		}

		protected override string Verify(string columnName) {
			if (columnName == nameof(HostFilename)) {
				if (string.IsNullOrWhiteSpace(HostFilename))
					return dnSpy_Debugger_Resources.Error_HostEgCoreRunExe;
				return VerifyFilename(HostFilename);
			}
			if (columnName == nameof(Filename))
				return VerifyFilename(Filename);
			if (columnName == nameof(DbgShimFilename))
				return VerifyFilename(DbgShimFilename);

			return string.Empty;
		}

		public override bool HasError {
			get {
				if (!string.IsNullOrEmpty(Verify(nameof(HostFilename))))
					return true;
				if (!string.IsNullOrEmpty(Verify(nameof(Filename))))
					return true;
				if (!string.IsNullOrEmpty(Verify(nameof(DbgShimFilename))))
					return true;

				return false;
			}
		}
	}
}
