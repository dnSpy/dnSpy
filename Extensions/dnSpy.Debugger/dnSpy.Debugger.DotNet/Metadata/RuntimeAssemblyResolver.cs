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
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using dnlib.DotNet;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.DnSpy.Metadata;

namespace dnSpy.Debugger.DotNet.Metadata {
	[ExportRuntimeAssemblyResolver]
	[Export(typeof(IDbgManagerStartListener))]
	sealed class RuntimeAssemblyResolver : IRuntimeAssemblyResolver, IDbgManagerStartListener {
		DbgManager? dbgManager;

		void IDbgManagerStartListener.OnStart(DbgManager dbgManager) => this.dbgManager = dbgManager;

		RuntimeAssemblyResolverResult IRuntimeAssemblyResolver.Resolve(IAssembly assembly, ModuleDef? sourceModule) {
			if (dbgManager is null || !dbgManager.IsDebugging)
				return default;

			var checkedRuntimes = new HashSet<DbgRuntime>();
			foreach (var runtime in GetRuntimes(dbgManager, assembly, sourceModule?.Location)) {
				if (!checkedRuntimes.Add(runtime))
					continue;

				var reflectionRuntime = runtime.GetReflectionRuntime();
				if (reflectionRuntime is null)
					continue;
				var asmName = new DmdReadOnlyAssemblyName(assembly.Name, assembly.Version, assembly.Culture, (DmdAssemblyNameFlags)assembly.Attributes, assembly.PublicKeyOrToken?.Data, GetAssemblyHashAlgorithm(assembly));
				foreach (var reflectionAppDomain in reflectionRuntime.GetAppDomains()) {
					var reflectionAssembly = reflectionAppDomain.GetAssembly(asmName);
					var module = reflectionAssembly?.ManifestModule.GetDebuggerModule();
					if (module is null)
						continue;

					if (module.IsDynamic)
						return default;
					if (module.IsInMemory) {
						if (module.ImageLayout == DbgImageLayout.Unknown)
							return default;
						string? filename = CreateInMemoryFilename(sourceModule?.Location, asmName.Name, module.IsExe);
						return RuntimeAssemblyResolverResult.Create(() => ReadModuleBytes(module), filename);
					}
					return RuntimeAssemblyResolverResult.Create(module.Filename);
				}
			}

			return default;
		}

		static DmdAssemblyHashAlgorithm GetAssemblyHashAlgorithm(IAssembly assembly) {
			if (assembly is AssemblyDef asmDef)
				return (DmdAssemblyHashAlgorithm)asmDef.HashAlgorithm;
			return DmdAssemblyHashAlgorithm.SHA1;
		}

		static string? CreateInMemoryFilename(string? moduleFilename, string? asmName, bool isExe) {
			if (moduleFilename is null || asmName is null)
				return null;
			try {
				return Path.Combine(Path.GetDirectoryName(moduleFilename)!, asmName + (isExe ? ".exe" : ".dll"));
			}
			catch (ArgumentException) {
			}
			catch (PathTooLongException) {
			}
			return null;
		}

		static (byte[]? filedata, bool isFileLayout) ReadModuleBytes(DbgModule module) {
			if (module.IsClosed)
				return default;
			if (module.Runtime.IsClosed)
				return default;
			if (module.Process.IsClosed)
				return default;
			if (!module.HasAddress)
				return default;
			Debug.Assert(module.Size <= int.MaxValue);
			if (module.Size > int.MaxValue)
				return default;
			Debug.Assert(module.ImageLayout != DbgImageLayout.Unknown);
			if (module.ImageLayout == DbgImageLayout.Unknown)
				return default;
			bool isFileLayout = module.ImageLayout == DbgImageLayout.File;
			return (module.Process.ReadMemory(module.Address, (int)module.Size), isFileLayout);
		}

		static IEnumerable<DbgRuntime> GetRuntimes(DbgManager dbgManager, IAssembly assembly, string? sourceModuleFilename) {
			// Prefer runtimes that have a matching assembly or source module
			foreach (var runtime in GetRuntimesCore(dbgManager)) {
				if (HasAssemblyOrModule(runtime, assembly, sourceModuleFilename))
					yield return runtime;
			}

			foreach (var runtime in GetRuntimesCore(dbgManager))
				yield return runtime;
		}

		static bool HasAssemblyOrModule(DbgRuntime runtime, IAssembly assembly, string? sourceModuleFilename) {
			foreach (var module in runtime.Modules) {
				if (module.IsDynamic)
					continue;
				if (module.IsInMemory) {
					// We could get the reflection module and then call reflectionModule.Assembly.GetName(),
					// but GetName() will load the metadata which we're trying to avoid.
					continue;
				}
				else {
					if (StringComparer.OrdinalIgnoreCase.Equals(module.Filename, sourceModuleFilename))
						return true;
					// reflectionModule.Assembly.GetName() isn't called since it loads the metadata.
					// Instead we assume that simple name == filename_without_extension. It's true most
					// of the time, and if it's false, it's likely that the normal asm resolver will
					// find the assembly.
					string asmSimpleName;
					try {
						asmSimpleName = Path.GetFileNameWithoutExtension(module.Filename);
					}
					catch (ArgumentException) {
						continue;
					}
					if (asmSimpleName.EndsWith(".ni", StringComparison.OrdinalIgnoreCase))
						asmSimpleName = asmSimpleName.Substring(0, asmSimpleName.Length - 3);
					if (StringComparer.OrdinalIgnoreCase.Equals(asmSimpleName, assembly.Name))
						return true;
				}
			}

			return false;
		}

		static IEnumerable<DbgRuntime> GetRuntimesCore(DbgManager dbgManager) {
			var currentRuntime = dbgManager.CurrentRuntime;
			DbgRuntime? runtime;
			if ((runtime = currentRuntime.Current) is not null)
				yield return runtime;
			if ((runtime = currentRuntime.Break) is not null)
				yield return runtime;

			if (currentRuntime.Current?.Process is DbgProcess currentProcess) {
				foreach (var r in currentProcess.Runtimes)
					yield return r;
			}
			if (currentRuntime.Break?.Process is DbgProcess breakProcess) {
				foreach (var r in breakProcess.Runtimes)
					yield return r;
			}

			foreach (var process in dbgManager.Processes) {
				foreach (var rt in process.Runtimes)
					yield return rt;
			}
		}
	}
}
