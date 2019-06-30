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
using dndbg.COM.CorDebug;
using dndbg.Engine;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Utilities;

namespace dnSpy.Debugger.DotNet.CorDebug.Impl {
	sealed class DebugOptionsProviderImpl : DebugOptionsProvider {
		readonly bool suppressJITOptimization_SystemModules;
		readonly bool suppressJITOptimization_ProgramModules;

		public DebugOptionsProviderImpl(DebuggerSettings debuggerSettings) {
			suppressJITOptimization_SystemModules = debuggerSettings.SuppressJITOptimization_SystemModules;
			suppressJITOptimization_ProgramModules = debuggerSettings.SuppressJITOptimization_ProgramModules;
		}

		public override CorDebugJITCompilerFlags GetDesiredNGENCompilerFlags(DnProcess process) =>
			suppressJITOptimization_SystemModules ? CorDebugJITCompilerFlags.CORDEBUG_JIT_DISABLE_OPTIMIZATION : CorDebugJITCompilerFlags.CORDEBUG_JIT_DEFAULT;

		bool IsProgramModule(DnModule module) {
			if (module.IsDynamic || module.IsInMemory)
				return true;

			var filename = module.Name;
			if (!File.Exists(filename))
				return true;

			if (GacInfo.IsGacPath(filename))
				return false;

			var dnDebugger = module.Process.Debugger;
			if (IsInDirOrSubDir(Path.GetDirectoryName(dnDebugger.CLRPath), filename))
				return false;

			return true;
		}

		static bool IsInDirOrSubDir(string dir, string filename) {
			dir = dir.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
			if (dir.Length > 0 && dir[dir.Length - 1] != Path.DirectorySeparatorChar)
				dir += Path.DirectorySeparatorChar.ToString();
			filename = filename.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
			return filename.StartsWith(dir, StringComparison.OrdinalIgnoreCase);
		}

		bool IsJustMyCode(DnModule module) => true;

		public override ModuleLoadOptions GetModuleLoadOptions(DnModule module) {
			ModuleLoadOptions options = default;

			bool suppressJITOptimization;
			if (IsProgramModule(module))
				suppressJITOptimization = suppressJITOptimization_ProgramModules;
			else
				suppressJITOptimization = suppressJITOptimization_SystemModules;

			if (suppressJITOptimization) {
				options.JITCompilerFlags = CorDebugJITCompilerFlags.CORDEBUG_JIT_DISABLE_OPTIMIZATION;
				options.ModuleTrackJITInfo = true;
				options.ModuleAllowJitOptimizations = false;
			}
			else {
				options.JITCompilerFlags = CorDebugJITCompilerFlags.CORDEBUG_JIT_DEFAULT;
				options.ModuleTrackJITInfo = false;
				options.ModuleAllowJitOptimizations = true;
			}

			options.JustMyCode = IsJustMyCode(module);

			return options;
		}
	}
}
