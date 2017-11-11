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
using dnSpy.Contracts.Debugger.DotNet.Mono;
using dnSpy.Contracts.Debugger.StartDebugging;
using dnSpy.Contracts.Debugger.StartDebugging.Dialog;
using dnSpy.Contracts.MVVM;
using dnSpy.Debugger.DotNet.Mono.Properties;

namespace dnSpy.Debugger.DotNet.Mono.Dialogs.DebugProgram {
	sealed class MonoStartDebuggingOptionsPage : StartDebuggingOptionsPage, IDataErrorInfo {
		public override Guid Guid => new Guid("334A6F3E-2118-4ABE-B9C8-0D16EF723B37");
		public override double DisplayOrder => PredefinedStartDebuggingOptionsPageDisplayOrders.DotNetMono;
		// Shouldn't be localized
		public override string DisplayName => "Mono";
		public override object UIObject => this;

		public string Filename {
			get => filename;
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
			get => commandLine;
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
			get => workingDirectory;
			set {
				if (workingDirectory != value) {
					workingDirectory = value;
					OnPropertyChanged(nameof(WorkingDirectory));
					UpdateIsValid();
				}
			}
		}
		string workingDirectory = string.Empty;

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

		public UInt16VM ConnectionPort { get; }

		public ICommand PickFilenameCommand => new RelayCommand(a => PickNewFilename());
		public ICommand PickWorkingDirectoryCommand => new RelayCommand(a => PickNewWorkingDirectory());
		public ICommand PickMonoExePathCommand => new RelayCommand(a => PickMonoExePath());

		public EnumListVM BreakProcessKindVM => breakProcessKindVM;
		readonly EnumListVM breakProcessKindVM = new EnumListVM(BreakProcessKindsUtils.BreakProcessKindList);

		public string BreakKind {
			get => (string)BreakProcessKindVM.SelectedItem;
			set => BreakProcessKindVM.SelectedItem = value;
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

		readonly IPickFilename pickFilename;
		readonly IPickDirectory pickDirectory;

		public MonoStartDebuggingOptionsPage(IPickFilename pickFilename, IPickDirectory pickDirectory) {
			this.pickFilename = pickFilename ?? throw new ArgumentNullException(nameof(pickFilename));
			this.pickDirectory = pickDirectory ?? throw new ArgumentNullException(nameof(pickDirectory));
			ConnectionPort = new UInt16VM(a => UpdateIsValid(), useDecimal: true);
		}

		static string GetPath(string file) {
			try {
				return Path.GetDirectoryName(file);
			}
			catch {
			}
			return null;
		}

		static void Initialize(string filename, MonoStartDebuggingOptions options) {
			options.Filename = filename;
			options.WorkingDirectory = GetPath(options.Filename);
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

		static readonly string MonoExeFilter = $"mono.exe|mono.exe";
		void PickMonoExePath() {
			var newMonoExePath = pickFilename.GetFilename(Filename, "exe", MonoExeFilter);
			if (newMonoExePath == null)
				return;

			MonoExePath = newMonoExePath;
		}

		static string FilterBreakKind(string breakKind) {
			foreach (var info in BreakProcessKindsUtils.BreakProcessKindList) {
				if (StringComparer.Ordinal.Equals(breakKind, (string)info.Value))
					return breakKind;
			}
			return PredefinedBreakKinds.DontBreak;
		}

		void Initialize(MonoStartDebuggingOptions options) {
			Filename = options.Filename;
			CommandLine = options.CommandLine;
			// Must be init'd after Filename since it also overwrites this property
			WorkingDirectory = options.WorkingDirectory;
			MonoExePath = options.MonoExePath;
			ConnectionPort.Value = options.ConnectionPort;
			BreakKind = FilterBreakKind(options.BreakKind);
		}

		MonoStartDebuggingOptions InitializeDefault(MonoStartDebuggingOptions options, string breakKind) {
			options.BreakKind = FilterBreakKind(breakKind);
			return options;
		}

		MonoStartDebuggingOptions GetOptions(MonoStartDebuggingOptions options) {
			options.Filename = Filename;
			options.CommandLine = CommandLine;
			options.WorkingDirectory = WorkingDirectory;
			options.MonoExePath = MonoExePath;
			options.ConnectionPort = ConnectionPort.Value;
			options.BreakKind = FilterBreakKind(BreakKind);
			return options;
		}

		string IDataErrorInfo.Error => throw new NotImplementedException();
		string IDataErrorInfo.this[string columnName] => Verify(columnName);

		static string VerifyFilename(string filename) {
			if (!File.Exists(filename)) {
				if (string.IsNullOrWhiteSpace(filename))
					return dnSpy_Debugger_DotNet_Mono_Resources.Error_MissingFilename;
				return dnSpy_Debugger_DotNet_Mono_Resources.Error_FileDoesNotExist;
			}
			return string.Empty;
		}

		public override void InitializePreviousOptions(StartDebuggingOptions options) {
			var dncOptions = options as MonoStartDebuggingOptions;
			if (dncOptions == null)
				return;
			Initialize(dncOptions);
		}

		public override void InitializeDefaultOptions(string filename, string breakKind, StartDebuggingOptions options) =>
			Initialize(GetDefaultOptions(filename, breakKind, options));

		MonoStartDebuggingOptions GetDefaultOptions(string filename, string breakKind, StartDebuggingOptions options) {
			bool isExe = PortableExecutableFileHelpers.IsExecutable(filename);
			if (isExe) {
				var dncOptions = CreateOptions(breakKind);
				Initialize(filename, dncOptions);
				return dncOptions;
			}
			else {
				// If it's a DLL, use the old EXE options if available
				if (options is MonoStartDebuggingOptions dncOptions)
					return dncOptions;
				return CreateOptions(breakKind);
			}
		}

		MonoStartDebuggingOptions CreateOptions(string breakKind) =>
			InitializeDefault(new MonoStartDebuggingOptions(), breakKind);

		public override StartDebuggingOptionsInfo GetOptions() {
			var options = GetOptions(new MonoStartDebuggingOptions());
			return new StartDebuggingOptionsInfo(options, options.Filename);
		}

		public override bool SupportsDebugEngine(Guid engineGuid, out double order) {
			if (engineGuid == PredefinedGenericDebugEngineGuids.DotNetFramework) {
				order = PredefinedGenericDebugEngineOrders.DotNetMono;
				return true;
			}

			order = 0;
			return false;
		}

		bool CalculateIsValid() =>
			!ConnectionPort.HasError &&
			string.IsNullOrEmpty(Verify(nameof(MonoExePath))) &&
			string.IsNullOrEmpty(Verify(nameof(Filename)));

		string Verify(string columnName) {
			if (columnName == nameof(MonoExePath)) {
				if (!string.IsNullOrWhiteSpace(MonoExePath))
					return VerifyFilename(MonoExePath);
			}
			else if (columnName == nameof(Filename))
				return VerifyFilename(Filename);

			return string.Empty;
		}
	}
}
