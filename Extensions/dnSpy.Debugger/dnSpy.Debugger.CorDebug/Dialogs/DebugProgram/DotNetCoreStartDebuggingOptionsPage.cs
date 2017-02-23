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
using dnSpy.Contracts.Debugger.DotNet.CorDebug;
using dnSpy.Contracts.Debugger.UI;
using dnSpy.Contracts.MVVM;

namespace dnSpy.Debugger.CorDebug.Dialogs.DebugProgram {
	sealed class DotNetCoreStartDebuggingOptionsPage : DotNetStartDebuggingOptionsPage {
		public override Guid Guid => new Guid("6DA15E33-27DA-498B-8AF1-552399485002");
		public override double DisplayOrder => PredefinedStartDebuggingOptionsPageDisplayOrders.DotNetCore;
		// Shouldn't be localized
		public override string DisplayName => ".NET Core";

		public string HostFilename {
			get => hostFilename;
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
			get => hostArguments;
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

		public DotNetCoreStartDebuggingOptionsPage(DebuggerSettings debuggerSettings, IPickFilename pickFilename, IPickDirectory pickDirectory)
			: base(debuggerSettings, pickFilename, pickDirectory) {
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

		public override void InitializePreviousOptions(StartDebuggingOptions options) {
			var dncOptions = options as DotNetCoreStartDebuggingOptions;
			if (dncOptions == null)
				return;
			Initialize(dncOptions);
		}

		public override void InitializeDefaultOptions(string filename, StartDebuggingOptions options) => Initialize(GetDefaultOptions(filename, options));

		DotNetCoreStartDebuggingOptions GetDefaultOptions(string filename, StartDebuggingOptions options) {
			bool isExe = PortableExecutableFileHelpers.IsExecutable(filename);
			if (isExe) {
				var dncOptions = CreateOptions();
				Initialize(filename, dncOptions);
				return dncOptions;
			}
			else {
				// If it's a DLL, use the old EXE options if available
				if (options is DotNetCoreStartDebuggingOptions dncOptions)
					return dncOptions;
				return CreateOptions();
			}
		}

		DotNetCoreStartDebuggingOptions CreateOptions() => InitializeDefault(new DotNetCoreStartDebuggingOptions { HostArguments = "exec" });

		void Initialize(DotNetCoreStartDebuggingOptions options) {
			base.Initialize(options);
			HostFilename = options.Host;
			HostArguments = options.HostArguments;
		}

		public override StartDebuggingOptionsInfo GetOptions() {
			var options = GetOptions(new DotNetCoreStartDebuggingOptions {
				Host = HostFilename,
				HostArguments = HostArguments,
			});
			return new StartDebuggingOptionsInfo(options, options.Filename);
		}

		public override bool SupportsDebugEngine(Guid engineGuid, out double order) {
			if (engineGuid == PredefinedGenericDebugEngineGuids.DotNetCore) {
				order = PredefinedGenericDebugEngineOrders.DotNetCore_CorDebug;
				return true;
			}

			order = 0;
			return false;
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
