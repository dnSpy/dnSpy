/*
    Copyright (C) 2014-2017 de4dot@gmail.com

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
using System.ComponentModel;
using System.IO;
using System.Windows.Input;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.CorDebug;
using dnSpy.Contracts.Debugger.UI;
using dnSpy.Contracts.MVVM;
using dnSpy.Debugger.CorDebug.Properties;

namespace dnSpy.Debugger.CorDebug.Dialogs.DebugProgram {
	sealed class DotNetCoreStartDebuggingOptionsPage : StartDebuggingOptionsPage, IDataErrorInfo {
		public override Guid Guid => new Guid("6DA15E33-27DA-498B-8AF1-552399485002");
		public override double DisplayOrder => PredefinedStartDebuggingOptionsPageDisplayOrders.DotNetCore;
		// Shouldn't be localized
		public override string DisplayName => ".NET Core";
		public override object UIObject => this;

		public string HostFilename {
			get { return hostFilename; }
			set {
				if (hostFilename != value) {
					hostFilename = value;
					OnPropertyChanged(nameof(HostFilename));
					UpdateIsValid();
				}
			}
		}
		string hostFilename = string.Empty;

		public string HostArguments {
			get { return hostArguments; }
			set {
				if (hostArguments != value) {
					hostArguments = value;
					OnPropertyChanged(nameof(HostArguments));
					UpdateIsValid();
				}
			}
		}
		string hostArguments = string.Empty;

		public string Filename {
			get { return filename; }
			set {
				if (filename != value) {
					filename = value;
					OnPropertyChanged(nameof(Filename));
					UpdateIsValid();
					var path = GetPath(filename);
					if (path != null)
						WorkingDirectory = path;
				}
			}
		}
		string filename = string.Empty;

		public string CommandLine {
			get { return commandLine; }
			set {
				if (commandLine != value) {
					commandLine = value;
					OnPropertyChanged(nameof(CommandLine));
					UpdateIsValid();
				}
			}
		}
		string commandLine = string.Empty;

		public string WorkingDirectory {
			get { return workingDirectory; }
			set {
				if (workingDirectory != value) {
					workingDirectory = value;
					OnPropertyChanged(nameof(WorkingDirectory));
					UpdateIsValid();
				}
			}
		}
		string workingDirectory = string.Empty;

		public ICommand PickHostFilenameCommand => new RelayCommand(a => PickNewHostFilename());
		public ICommand PickFilenameCommand => new RelayCommand(a => PickNewFilename());
		public ICommand PickWorkingDirectoryCommand => new RelayCommand(a => PickNewWorkingDirectory());

		public EnumListVM BreakProcessKindVM => breakProcessKindVM;
		readonly EnumListVM breakProcessKindVM = new EnumListVM(DotNetFrameworkStartDebuggingOptionsPage.breakProcessKindList);

		public BreakProcessKind BreakProcessKind {
			get { return (BreakProcessKind)BreakProcessKindVM.SelectedItem; }
			set { BreakProcessKindVM.SelectedItem = value; }
		}

		public override bool IsValid => isValid;
		bool isValid;

		void UpdateIsValid() {
			var newIsValid = CalculateIsValid();
			if (newIsValid == isValid)
				return;
			isValid = newIsValid;
			OnPropertyChanged(nameof(IsValid));
		}

		bool CalculateIsValid() => string.IsNullOrEmpty(Verify(nameof(HostFilename))) && string.IsNullOrEmpty(Verify(nameof(Filename)));

		readonly IPickFilename pickFilename;
		readonly IPickDirectory pickDirectory;

		public DotNetCoreStartDebuggingOptionsPage(string currentFilename, IPickFilename pickFilename, IPickDirectory pickDirectory) {
			if (currentFilename == null)
				throw new ArgumentNullException(nameof(currentFilename));
			if (pickFilename == null)
				throw new ArgumentNullException(nameof(pickFilename));
			if (pickDirectory == null)
				throw new ArgumentNullException(nameof(pickDirectory));
			Filename = currentFilename;
			this.pickFilename = pickFilename;
			this.pickDirectory = pickDirectory;
		}

		static string GetPath(string file) {
			try {
				return Path.GetDirectoryName(file);
			}
			catch {
			}
			return null;
		}

		void PickNewHostFilename() {
			var newFilename = pickFilename.GetFilename(HostFilename, "exe", PickFilenameConstants.ExecutableFilter);
			if (newFilename == null)
				return;

			HostFilename = newFilename;
		}

		void PickNewFilename() {
			var newFilename = pickFilename.GetFilename(Filename, "dll", PickFilenameConstants.DotNetAssemblyOrModuleFilter);
			if (newFilename == null)
				return;

			Filename = newFilename;
		}

		void PickNewWorkingDirectory() {
			var newDir = pickDirectory.GetDirectory(WorkingDirectory);
			if (newDir == null)
				return;

			WorkingDirectory = newDir;
		}

		public override StartDebuggingOptions GetOptions() {
			return new DotNetCoreStartDebuggingOptions {
				Host = HostFilename,
				HostArguments = HostArguments,
				Filename = Filename,
				CommandLine = CommandLine,
				WorkingDirectory = WorkingDirectory,
				BreakProcessKind = BreakProcessKind,
			};
		}

		string IDataErrorInfo.Error { get { throw new NotImplementedException(); } }
		string IDataErrorInfo.this[string columnName] => Verify(columnName);

		static string VerifyFilename(string filename) {
			if (!File.Exists(filename)) {
				if (string.IsNullOrWhiteSpace(filename))
					return dnSpy_Debugger_CorDebug_Resources.Error_MissingFilename;
				return dnSpy_Debugger_CorDebug_Resources.Error_FileDoesNotExist;
			}
			return string.Empty;
		}

		string Verify(string columnName) {

			// Also update CalculateIsValid() if this method gets updated

			if (columnName == nameof(HostFilename)) {
				if (!string.IsNullOrWhiteSpace(HostFilename))
					return VerifyFilename(HostFilename);
			}
			else if (columnName == nameof(Filename))
				return VerifyFilename(Filename);

			return string.Empty;
		}
	}
}
