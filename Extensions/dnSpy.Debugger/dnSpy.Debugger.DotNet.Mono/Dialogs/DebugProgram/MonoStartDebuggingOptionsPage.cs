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
using dnSpy.Contracts.Debugger.DotNet.Mono;
using dnSpy.Contracts.Debugger.StartDebugging;
using dnSpy.Contracts.Debugger.StartDebugging.Dialog;
using dnSpy.Contracts.MVVM;

namespace dnSpy.Debugger.DotNet.Mono.Dialogs.DebugProgram {
	sealed class MonoStartDebuggingOptionsPage : MonoStartDebuggingOptionsPageBase {
		public override Guid Guid => new Guid("334A6F3E-2118-4ABE-B9C8-0D16EF723B37");
		public override double DisplayOrder => PredefinedStartDebuggingOptionsPageDisplayOrders.DotNetMono;
		// Shouldn't be localized
		public override string DisplayName => "Mono";

		public string MonoExePath {
			get => monoExePath;
			set {
				if (monoExePath != value) {
					monoExePath = value;
					OnPropertyChanged(nameof(MonoExePath));
					UpdateIsValid();
				}
			}
		}
		string monoExePath;

		public ICommand PickMonoExePathCommand => new RelayCommand(a => PickMonoExePath());

		public MonoStartDebuggingOptionsPage(IPickFilename pickFilename, IPickDirectory pickDirectory)
			: base(pickFilename, pickDirectory) {
		}

		static readonly string MonoExeFilter = $"mono.exe|mono.exe";
		void PickMonoExePath() {
			var newMonoExePath = pickFilename.GetFilename(Filename, "exe", MonoExeFilter);
			if (newMonoExePath == null)
				return;

			MonoExePath = newMonoExePath;
		}

		void Initialize(MonoStartDebuggingOptions options) {
			base.Initialize(options);
			MonoExePath = options.MonoExePath;
		}

		MonoStartDebuggingOptions InitializeDefault(MonoStartDebuggingOptions options, string breakKind) {
			base.InitializeDefault(options, breakKind);
			return options;
		}

		MonoStartDebuggingOptions GetOptions(MonoStartDebuggingOptions options) {
			base.GetOptions(options);
			options.MonoExePath = MonoExePath;
			return options;
		}

		public override void InitializePreviousOptions(StartDebuggingOptions options) {
			var msdOptions = options as MonoStartDebuggingOptions;
			if (msdOptions == null)
				return;
			Initialize(msdOptions);
		}

		public override void InitializeDefaultOptions(string filename, string breakKind, StartDebuggingOptions options) =>
			Initialize(GetDefaultOptions(filename, breakKind, options));

		MonoStartDebuggingOptions GetDefaultOptions(string filename, string breakKind, StartDebuggingOptions options) {
			bool isExe = PortableExecutableFileHelpers.IsExecutable(filename);
			if (isExe) {
				var msdOptions = CreateOptions(breakKind);
				Initialize(filename, msdOptions);
				return msdOptions;
			}
			else {
				// If it's a DLL, use the old EXE options if available
				if (options is MonoStartDebuggingOptions msdOptions)
					return msdOptions;
				return CreateOptions(breakKind);
			}
		}

		MonoStartDebuggingOptions CreateOptions(string breakKind) =>
			InitializeDefault(new MonoStartDebuggingOptions(), breakKind);

		public override StartDebuggingOptionsInfo GetOptions() {
			var options = GetOptions(new MonoStartDebuggingOptions());
			var flags = StartDebuggingOptionsInfoFlags.None;
			if (File.Exists(options.Filename)) {
				var extension = Path.GetExtension(options.Filename);
				if (!StringComparer.OrdinalIgnoreCase.Equals(extension, ".exe"))
					flags |= StartDebuggingOptionsInfoFlags.WrongExtension;
			}
			return new StartDebuggingOptionsInfo(options, options.Filename, flags);
		}

		public override bool SupportsDebugEngine(Guid engineGuid, out double order) {
			if (engineGuid == PredefinedGenericDebugEngineGuids.DotNetFramework) {
				order = PredefinedGenericDebugEngineOrders.DotNetMono;
				return true;
			}

			order = 0;
			return false;
		}

		protected override bool CalculateIsValidCore() => string.IsNullOrEmpty(Verify(nameof(MonoExePath)));

		protected override string VerifyCore(string columnName) {
			if (columnName == nameof(MonoExePath)) {
				if (!string.IsNullOrWhiteSpace(MonoExePath))
					return VerifyFilename(MonoExePath);
			}

			return string.Empty;
		}
	}
}
