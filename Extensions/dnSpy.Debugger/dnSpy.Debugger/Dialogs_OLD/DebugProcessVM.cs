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
using dnSpy.Contracts.MVVM;
using dnSpy.Debugger.Properties;

namespace dnSpy.Debugger.Dialogs_OLD {
	sealed class DebugProcessVM : ViewModelBase {
		public IPickFilename PickFilename {
			set { pickFilename = value; }
		}
		IPickFilename pickFilename;

		public IPickDirectory PickDirectory {
			set { pickDirectory = value; }
		}
		IPickDirectory pickDirectory;

		public ICommand PickFilenameCommand => new RelayCommand(a => PickNewFilename());
		public ICommand PickCurrentDirectoryCommand => new RelayCommand(a => PickNewCurrentDirectory());

		public static readonly EnumVM[] breakProcessKindList = new EnumVM[(int)BreakProcessKind.Last] {
			new EnumVM(BreakProcessKind.None, dnSpy_Debugger_Resources.DbgBreak_Dont),
			new EnumVM(BreakProcessKind.CreateProcess, dnSpy_Debugger_Resources.DbgBreak_CreateProcessEvent),
			new EnumVM(BreakProcessKind.CreateAppDomain, dnSpy_Debugger_Resources.DbgBreak_FirstCreateAppDomainEvent),
			new EnumVM(BreakProcessKind.LoadModule, dnSpy_Debugger_Resources.DbgBreak_FirstLoadModuleEvent),
			new EnumVM(BreakProcessKind.LoadClass, dnSpy_Debugger_Resources.DbgBreak_FirstLoadClassEvent),
			new EnumVM(BreakProcessKind.CreateThread, dnSpy_Debugger_Resources.DbgBreak_FirstCreateThreadEvent),
			new EnumVM(BreakProcessKind.ExeLoadModule, dnSpy_Debugger_Resources.DbgBreak_ExeLoadModuleEvent),
			new EnumVM(BreakProcessKind.ExeLoadClass, dnSpy_Debugger_Resources.DbgBreak_ExeFirstLoadClassEvent),
			new EnumVM(BreakProcessKind.ModuleCctorOrEntryPoint, dnSpy_Debugger_Resources.DbgBreak_ModuleClassConstructorOrEntryPoint),
			new EnumVM(BreakProcessKind.EntryPoint, dnSpy_Debugger_Resources.DbgBreak_EntryPoint),
		};
		public EnumListVM BreakProcessKindVM => breakProcessKindVM;
		readonly EnumListVM breakProcessKindVM = new EnumListVM(breakProcessKindList);

		public BreakProcessKind BreakProcessKind {
			get { return (BreakProcessKind)BreakProcessKindVM.SelectedItem; }
			set { BreakProcessKindVM.SelectedItem = value; }
		}

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

		public DebugProcessVM() {
		}

		static string GetPath(string file) {
			try {
				return Path.GetDirectoryName(file);
			}
			catch {
			}
			return null;
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

		public DebugProcessVM Clone() => CopyTo(new DebugProcessVM());

		public DebugProcessVM CopyTo(DebugProcessVM other) {
			other.Filename = Filename;
			other.CommandLine = CommandLine;
			other.CurrentDirectory = CurrentDirectory;
			other.BreakProcessKind = BreakProcessKind;
			return other;
		}

		protected override string Verify(string columnName) {
			if (columnName == nameof(Filename)) {
				if (!File.Exists(filename)) {
					if (string.IsNullOrWhiteSpace(filename))
						return dnSpy_Debugger_Resources.Error_MissingFilename;
					return dnSpy_Debugger_Resources.Error_FileDoesNotExist;
				}
				return string.Empty;
			}

			return string.Empty;
		}

		public override bool HasError {
			get {
				if (!string.IsNullOrEmpty(Verify(nameof(Filename))))
					return true;

				return false;
			}
		}
	}
}
