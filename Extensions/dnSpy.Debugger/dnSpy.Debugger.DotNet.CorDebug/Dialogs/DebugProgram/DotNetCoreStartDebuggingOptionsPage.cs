/*
    Copyright (C) 2014-2019 de4dot@gmail.com

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
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.DotNet.CorDebug;
using dnSpy.Contracts.Debugger.StartDebugging;
using dnSpy.Contracts.Debugger.StartDebugging.Dialog;
using dnSpy.Contracts.MVVM;
using dnSpy.Debugger.DotNet.CorDebug.Utilities;

namespace dnSpy.Debugger.DotNet.CorDebug.Dialogs.DebugProgram {
	sealed class DotNetCoreStartDebuggingOptionsPage : DotNetStartDebuggingOptionsPage {
		public override Guid Guid => new Guid("6DA15E33-27DA-498B-8AF1-552399485002");
		public override double DisplayOrder => PredefinedStartDebuggingOptionsPageDisplayOrders.DotNetCore;
		// Shouldn't be localized
		public override string DisplayName => ".NET Core";

		public bool UseHost {
			get => useHost;
			set {
				if (useHost != value) {
					useHost = value;
					OnPropertyChanged(nameof(UseHost));
					OnPropertyChanged(nameof(HostFilename));
					UpdateIsValid();
				}
			}
		}
		bool useHost;

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

		public ICommand PickHostFilenameCommand => new RelayCommand(a => PickNewHostFilename(), a => CanPickNewHostFilename);

		public DotNetCoreStartDebuggingOptionsPage(IPickFilename pickFilename, IPickDirectory pickDirectory)
			: base(pickFilename, pickDirectory) {
		}

		bool CanPickNewHostFilename => UseHost;

		void PickNewHostFilename() {
			var newFilename = pickFilename.GetFilename(HostFilename, "exe", PickFilenameConstants.ExecutableFilter);
			if (newFilename is null)
				return;

			HostFilename = newFilename;
		}

		protected override void PickNewFilename() {
			var newFilename = pickFilename.GetFilename(Filename, "dll", PickFilenameConstants.DotNetAssemblyOrModuleFilter);
			if (newFilename is null)
				return;

			Filename = newFilename;
		}

		public override void InitializePreviousOptions(StartDebuggingOptions options) {
			var dncOptions = options as DotNetCoreStartDebuggingOptions;
			if (dncOptions is null)
				return;
			Initialize(dncOptions);
		}

		public override void InitializeDefaultOptions(string filename, string breakKind, StartDebuggingOptions? options) =>
			Initialize(GetDefaultOptions(filename, breakKind, options));

		DotNetCoreStartDebuggingOptions GetDefaultOptions(string filename, string breakKind, StartDebuggingOptions? options) {
			bool isExe = PortableExecutableFileHelpers.IsExecutable(filename);
			if (isExe) {
				var dncOptions = CreateOptions(breakKind);
				Initialize(filename, dncOptions);
				dncOptions.UseHost = !DotNetCoreGenericDebugEngineGuidProvider.IsDotNetCoreAppHostFilename(filename);
				return dncOptions;
			}
			else {
				// If it's a DLL, use the old EXE options if available
				if (options is DotNetCoreStartDebuggingOptions dncOptions)
					return dncOptions;
				return CreateOptions(breakKind);
			}
		}

		DotNetCoreStartDebuggingOptions CreateOptions(string breakKind) =>
			InitializeDefault(new DotNetCoreStartDebuggingOptions { HostArguments = "exec" }, breakKind);

		void Initialize(DotNetCoreStartDebuggingOptions options) {
			base.Initialize(options);
			UseHost = options.UseHost;
			HostFilename = options.Host ?? string.Empty;
			HostArguments = options.HostArguments ?? string.Empty;
		}

		public override StartDebuggingOptionsInfo GetOptions() {
			var options = GetOptions(new DotNetCoreStartDebuggingOptions {
				UseHost = UseHost,
				Host = HostFilename,
				HostArguments = HostArguments,
			});
			var flags = StartDebuggingOptionsInfoFlags.None;
			if (File.Exists(options.Filename)) {
				var extension = Path.GetExtension(options.Filename);
				if (!StringComparer.OrdinalIgnoreCase.Equals(extension, ".exe") && !StringComparer.OrdinalIgnoreCase.Equals(extension, ".dll"))
					flags |= StartDebuggingOptionsInfoFlags.WrongExtension;
			}
			return new StartDebuggingOptionsInfo(options, options.Filename, flags);
		}

		public override bool SupportsDebugEngine(Guid engineGuid, out double order) {
			if (engineGuid == PredefinedGenericDebugEngineGuids.DotNetCore) {
				order = PredefinedGenericDebugEngineOrders.DotNetCore;
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
				if (UseHost && !string.IsNullOrWhiteSpace(HostFilename))
					return VerifyFilename(HostFilename);
			}
			else if (columnName == nameof(Filename))
				return VerifyFilename(Filename);

			return string.Empty;
		}
	}
}
