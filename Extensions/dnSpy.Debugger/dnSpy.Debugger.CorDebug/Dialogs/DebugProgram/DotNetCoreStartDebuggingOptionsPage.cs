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
using System.Windows.Input;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.CorDebug;
using dnSpy.Contracts.Debugger.UI;
using dnSpy.Contracts.MVVM;

namespace dnSpy.Debugger.CorDebug.Dialogs.DebugProgram {
	sealed class DotNetCoreStartDebuggingOptionsPage : DotNetStartDebuggingOptionsPage {
		public override Guid Guid => new Guid("6DA15E33-27DA-498B-8AF1-552399485002");
		public override double DisplayOrder => PredefinedStartDebuggingOptionsPageDisplayOrders.DotNetCore;
		// Shouldn't be localized
		public override string DisplayName => ".NET Core";

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

		public ICommand PickHostFilenameCommand => new RelayCommand(a => PickNewHostFilename());

		readonly SavedDotNetStartDebuggingOptions savedDotNetStartDebuggingOptions;

		public DotNetCoreStartDebuggingOptionsPage(DotNetCoreStartDebuggingOptions options, SavedDotNetStartDebuggingOptions savedDotNetStartDebuggingOptions, IPickFilename pickFilename, IPickDirectory pickDirectory)
			: base(options, pickFilename, pickDirectory) {
			if (savedDotNetStartDebuggingOptions == null)
				throw new ArgumentNullException(nameof(savedDotNetStartDebuggingOptions));
			this.savedDotNetStartDebuggingOptions = savedDotNetStartDebuggingOptions;
			HostFilename = options.Host;
			HostArguments = options.HostArguments;
		}

		void PickNewHostFilename() {
			var newFilename = pickFilename.GetFilename(HostFilename, "exe", PickFilenameConstants.ExecutableFilter);
			if (newFilename == null)
				return;

			HostFilename = newFilename;
		}

		protected override void PickNewFilename() {
			var newFilename = pickFilename.GetFilename(Filename, "dll", PickFilenameConstants.DotNetAssemblyOrModuleFilter);
			if (newFilename == null)
				return;

			Filename = newFilename;
		}

		public override StartDebuggingOptions GetOptions() {
			var options = new DotNetCoreStartDebuggingOptions {
				Host = HostFilename,
				HostArguments = HostArguments,
				Filename = Filename,
				CommandLine = CommandLine,
				WorkingDirectory = WorkingDirectory,
				BreakProcessKind = BreakProcessKind,
			};
			savedDotNetStartDebuggingOptions.SetOptions(options);
			return options;
		}

		protected override bool CalculateIsValid() =>
			string.IsNullOrEmpty(Verify(nameof(HostFilename))) &&
			string.IsNullOrEmpty(Verify(nameof(Filename)));

		protected override string Verify(string columnName) {
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
