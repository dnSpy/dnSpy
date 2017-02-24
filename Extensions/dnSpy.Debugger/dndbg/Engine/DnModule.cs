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

using System.Diagnostics;
using dndbg.COM.CorDebug;
using dndbg.DotNet;

namespace dndbg.Engine {
	/// <summary>
	/// A loaded module
	/// </summary>
	sealed class DnModule {
		/// <summary>
		/// Gets the created module or null if none has been created
		/// </summary>
		public CorModuleDef CorModuleDef { get; internal set; }

		/// <summary>
		/// Returns the created module or creates one if none has been created
		/// </summary>
		/// <returns></returns>
		public CorModuleDef GetOrCreateCorModuleDef() {
			Debugger.DebugVerifyThread();
			if (CorModuleDef != null)
				return CorModuleDef;

			Assembly.InitializeAssemblyAndModules();
			Debug.Assert(CorModuleDef != null);
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

		/// <summary>
		/// Gets the name from the MD, which is the same as <see cref="ModuleDef.Name"/>
		/// </summary>
		public string DnlibName => CorModule.DnlibName;

		/// <summary>
		/// true if the module has been unloaded
		/// </summary>
		public bool HasUnloaded { get; private set; }

		/// <summary>
		/// Gets the base address of the module or 0
		/// </summary>
		public ulong Address => CorModule.Address;

		/// <summary>
		/// Gets the size of the module or 0
		/// </summary>
		public uint Size => CorModule.Size;

		/// <summary>
		/// Gets the token or 0
		/// </summary>
		public uint Token => CorModule.Token;

		/// <summary>
		/// true if it's a dynamic module that can add/remove types
		/// </summary>
		public bool IsDynamic => CorModule.IsDynamic;

		/// <summary>
		/// true if this is an in-memory module
		/// </summary>
		public bool IsInMemory => CorModule.IsInMemory;

		/// <summary>
		/// Gets the owner debugger
		/// </summary>
		public DnDebugger Debugger => Assembly.Debugger;

		/// <summary>
		/// Gets the owner process
		/// </summary>
		public DnProcess Process => Assembly.Process;

		/// <summary>
		/// Gets the owner AppDomain
		/// </summary>
		public DnAppDomain AppDomain => Assembly.AppDomain;

		/// <summary>
		/// Gets the owner assembly
		/// </summary>
		public DnAssembly Assembly { get; }

		public DnModuleId DnModuleId { get; }

		/// <summary>
		/// Gets the JIT compiler flags. This is a cached value and never gets updated
		/// </summary>
		public CorDebugJITCompilerFlags CachedJITCompilerFlags { get; private set; }

		internal DnModule(DnAssembly ownerAssembly, ICorDebugModule module, int uniqueId, int uniqueIdProcess, int uniqueIdAppDomain) {
			Assembly = ownerAssembly;
			CorModule = new CorModule(module);
			UniqueId = uniqueId;
			UniqueIdProcess = uniqueIdProcess;
			UniqueIdAppDomain = uniqueIdAppDomain;
			DnModuleId = CorModule.DnModuleId;
		}

		internal void InitializeCachedValues() =>
			// Cache the value so it's possible to read it even when the process is running
			CachedJITCompilerFlags = CorModule.JITCompilerFlags;

		internal void SetHasUnloaded() => HasUnloaded = true;
		public override string ToString() => string.Format("{0} DYN={1} MEM={2} A={3:X8} S={4:X8} {5}", UniqueId, IsDynamic ? 1 : 0, IsInMemory ? 1 : 0, Address, Size, Name);
	}
}
