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
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using dnSpy.Contracts.Debugger.CorDebug;
using dnSpy.Contracts.Debugger.UI;
using dnSpy.Contracts.MVVM;

namespace dnSpy.Debugger.CorDebug.Dialogs.DebugProgram {
	[Export(typeof(StartDebuggingOptionsPageProvider))]
	sealed class StartDebuggingOptionsPageProviderImpl : StartDebuggingOptionsPageProvider {
		readonly SavedDotNetStartDebuggingOptions savedDotNetStartDebuggingOptions;
		readonly IPickFilename pickFilename;
		readonly IPickDirectory pickDirectory;

		[ImportingConstructor]
		StartDebuggingOptionsPageProviderImpl(SavedDotNetStartDebuggingOptions savedDotNetStartDebuggingOptions, IPickFilename pickFilename, IPickDirectory pickDirectory) {
			this.savedDotNetStartDebuggingOptions = savedDotNetStartDebuggingOptions;
			this.pickFilename = pickFilename;
			this.pickDirectory = pickDirectory;
		}

		public override IEnumerable<StartDebuggingOptionsPage> Create(StartDebuggingOptionsPageContext context) {
			bool isExe = PortableExecutableFileHelpers.IsExecutable(context.CurrentFilename);

			{
				var options = savedDotNetStartDebuggingOptions.DotNetFrameworkOptions;
				if (isExe && !IsSameFile(context.CurrentFilename, options)) {
					options = savedDotNetStartDebuggingOptions.CreateDotNetFramework();
					Initialize(context, options);
				}
				yield return new DotNetFrameworkStartDebuggingOptionsPage(options, savedDotNetStartDebuggingOptions, pickFilename, pickDirectory);
			}

			{
				var options = savedDotNetStartDebuggingOptions.DotNetCoreOptions;
				if (isExe && !IsSameFile(context.CurrentFilename, options)) {
					options = savedDotNetStartDebuggingOptions.CreateDotNetCore();
					Initialize(context, options);
				}
				yield return new DotNetCoreStartDebuggingOptionsPage(options, savedDotNetStartDebuggingOptions, pickFilename, pickDirectory);
			}
		}

		static bool IsSameFile(string filename, CorDebugStartDebuggingOptions options) =>
			StringComparer.OrdinalIgnoreCase.Equals(filename, options.Filename);

		static void Initialize(StartDebuggingOptionsPageContext context, CorDebugStartDebuggingOptions options) {
			options.Filename = context.CurrentFilename;
			options.WorkingDirectory = GetPath(options.Filename);
		}

		static string GetPath(string file) {
			try {
				return Path.GetDirectoryName(file);
			}
			catch {
			}
			return null;
		}
	}
}
