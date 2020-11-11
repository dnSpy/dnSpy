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

using System.Diagnostics;
using dndbg.COM.CorDebug;
using dndbg.DotNet;

namespace dndbg.Engine {
	sealed class DnModule {
		/// <summary>
		/// Gets the created module or null if none has been created
		/// </summary>
		public CorModuleDef? CorModuleDef { get; internal set; }

		/// <summary>
		/// Returns the created module or creates one if none has been created
		/// </summary>
		/// <returns></returns>
		public CorModuleDef GetOrCreateCorModuleDef() {
			Debugger.DebugVerifyThread();
			if (CorModuleDef is not null)
				return CorModuleDef;

			Assembly.InitializeAssemblyAndModules();
			Debug2.Assert(CorModuleDef is not null);
			return CorModuleDef;
		}

		public CorModule CorModule { get; }

		/// <summary>
		/// Unique id per debugger
		/// </summary>
		public int UniqueId { get; }

		/// <summary>
		/// Unique id per process
		/// </summary>
		public int UniqueIdProcess { get; }

		/// <summary>
		/// Unique id per AppDomain
		/// </summary>
		public int UniqueIdAppDomain { get; }

		/// <summary>
		/// For on-disk modules this is a full path. For dynamic modules this is just the filename
		/// if one was provided. Otherwise, and for other in-memory modules, this is just the simple
		/// name stored in the module's metadata.
		/// </summary>
		public string Name => CorModule.Name;

		public bool HasUnloaded { get; private set; }
		public ulong Address => CorModule.Address;
		public uint Size => CorModule.Size;
		public bool IsDynamic => CorModule.IsDynamic;
		public bool IsInMemory => CorModule.IsInMemory;
		public DnDebugger Debugger => Assembly.Debugger;
		public DnProcess Process => Assembly.Process;
		public DnAppDomain AppDomain => Assembly.AppDomain;
		public DnAssembly Assembly { get; }
		public DnModuleId DnModuleId { get; }
		public CorDebugJITCompilerFlags CachedJITCompilerFlags { get; private set; }

		internal DnModule(DnAssembly ownerAssembly, ICorDebugModule module, int uniqueId, int uniqueIdProcess, int uniqueIdAppDomain) {
			Assembly = ownerAssembly;
			CorModule = new CorModule(module);
			UniqueId = uniqueId;
			UniqueIdProcess = uniqueIdProcess;
			UniqueIdAppDomain = uniqueIdAppDomain;
			DnModuleId = CorModule.GetModuleId((uint)UniqueId);
		}

		internal void InitializeCachedValues() =>
			// Cache the value so it's possible to read it even when the process is running
			CachedJITCompilerFlags = CorModule.JITCompilerFlags;

		internal void SetHasUnloaded() => HasUnloaded = true;
		public override string ToString() => $"{UniqueId} DYN={(IsDynamic ? 1 : 0)} MEM={(IsInMemory ? 1 : 0)} A={Address:X8} S={Size:X8} {Name}";
	}
}
