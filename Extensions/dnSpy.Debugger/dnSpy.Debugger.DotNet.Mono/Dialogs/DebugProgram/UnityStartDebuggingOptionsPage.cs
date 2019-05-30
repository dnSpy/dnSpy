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
	sealed class UnityStartDebuggingOptionsPage : MonoStartDebuggingOptionsPageBase {
		public override Guid Guid => new Guid("CA5230F9-5FC9-477A-B2D5-6EE8E2EAE876");
		public override double DisplayOrder => PredefinedStartDebuggingOptionsPageDisplayOrders.DotNetUnity;
		// Shouldn't be localized
		public override string DisplayName => "Unity";

		public ICommand DebuggingUnityGamesCommand => new RelayCommand(a => DebuggingUnityGamesHelper.OpenDebuggingUnityGames());
		public string DebuggingUnityGamesText => DebuggingUnityGamesHelper.DebuggingUnityGamesText;

		public UnityStartDebuggingOptionsPage(IPickFilename pickFilename, IPickDirectory pickDirectory)
			: base(pickFilename, pickDirectory) {
		}

		void Initialize(UnityStartDebuggingOptions options) {
			base.Initialize(options);
		}

		UnityStartDebuggingOptions InitializeDefault(UnityStartDebuggingOptions options, string breakKind) {
			base.InitializeDefault(options, breakKind);
			return options;
		}

		UnityStartDebuggingOptions GetOptions(UnityStartDebuggingOptions options) {
			base.GetOptions(options);
			return options;
		}

		public override void InitializePreviousOptions(StartDebuggingOptions options) {
			var usdOptions = options as UnityStartDebuggingOptions;
			if (usdOptions is null)
				return;
			Initialize(usdOptions);
		}

		public override void InitializeDefaultOptions(string filename, string breakKind, StartDebuggingOptions? options) =>
			Initialize(GetDefaultOptions(filename, breakKind, options));

		UnityStartDebuggingOptions GetDefaultOptions(string filename, string breakKind, StartDebuggingOptions? options) {
			bool isExe = PortableExecutableFileHelpers.IsExecutable(filename);
			if (isExe) {
				var usdOptions = CreateOptions(breakKind);
				Initialize(filename, usdOptions);
				return usdOptions;
			}
			else {
				// If it's a DLL, use the old EXE options if available
				if (options is UnityStartDebuggingOptions usdOptions)
					return usdOptions;
				return CreateOptions(breakKind);
			}
		}

		UnityStartDebuggingOptions CreateOptions(string breakKind) =>
			InitializeDefault(new UnityStartDebuggingOptions(), breakKind);

		public override StartDebuggingOptionsInfo GetOptions() {
			var options = GetOptions(new UnityStartDebuggingOptions());
			var flags = StartDebuggingOptionsInfoFlags.None;
			if (File.Exists(options.Filename)) {
				var extension = Path.GetExtension(options.Filename);
				if (!StringComparer.OrdinalIgnoreCase.Equals(extension, ".exe"))
					flags |= StartDebuggingOptionsInfoFlags.WrongExtension;
			}
			return new StartDebuggingOptionsInfo(options, options.Filename, flags);
		}

		public override bool SupportsDebugEngine(Guid engineGuid, out double order) {
			if (engineGuid == PredefinedGenericDebugEngineGuids.DotNetUnity) {
				order = PredefinedGenericDebugEngineOrders.DotNetUnity;
				return true;
			}

			order = 0;
			return false;
		}

		protected override bool CalculateIsValidCore() => true;
		protected override string VerifyCore(string columnName) => string.Empty;
	}
}
