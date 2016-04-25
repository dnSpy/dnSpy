/*
    Copyright (C) 2014-2016 de4dot@gmail.com

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
	public sealed class DnModule {
		/// <summary>
		/// Gets the created module or null if none has been created
		/// </summary>
		public CorModuleDef CorModuleDef {
			get { return corModuleDef; }
			internal set { corModuleDef = value; }
		}
		CorModuleDef corModuleDef;

		/// <summary>
		/// Returns the created module or creates one if none has been created
		/// </summary>
		/// <returns></returns>
		public CorModuleDef GetOrCreateCorModuleDef() {
			Debugger.DebugVerifyThread();
			if (corModuleDef != null)
				return corModuleDef;

			Assembly.InitializeAssemblyAndModules();
			Debug.Assert(corModuleDef != null);
			return corModuleDef;
		}

		public CorModule CorModule {
			get { return module; }
		}
		readonly CorModule module;

		/// <summary>
		/// Unique id per debugger
		/// </summary>
		public int UniqueId {
			get { return uniqueId; }
		}
		readonly int uniqueId;

		/// <summary>
		/// Unique id per process
		/// </summary>
		public int UniqueIdProcess {
			get { return uniqueIdProcess; }
		}
		readonly int uniqueIdProcess;

		/// <summary>
		/// Unique id per AppDomain
		/// </summary>
		public int UniqueIdAppDomain {
			get { return uniqueIdAppDomain; }
		}
		readonly int uniqueIdAppDomain;

		/// <summary>
		/// For on-disk modules this is a full path. For dynamic modules this is just the filename
		/// if one was provided. Otherwise, and for other in-memory modules, this is just the simple
		/// name stored in the module's metadata.
		/// </summary>
		public string Name {
			get { return module.Name; }
		}

		/// <summary>
		/// Gets the name from the MD, which is the same as <see cref="ModuleDef.Name"/>
		/// </summary>
		public string DnlibName {
			get { return CorModule.DnlibName; }
		}

		/// <summary>
		/// true if the module has been unloaded
		/// </summary>
		public bool HasUnloaded {
			get { return hasUnloaded; }
		}
		bool hasUnloaded;

		/// <summary>
		/// Gets the base address of the module or 0
		/// </summary>
		public ulong Address {
			get { return module.Address; }
		}

		/// <summary>
		/// Gets the size of the module or 0
		/// </summary>
		public uint Size {
			get { return module.Size; }
		}

		/// <summary>
		/// Gets the token or 0
		/// </summary>
		public uint Token {
			get { return module.Token; }
		}

		/// <summary>
		/// true if it's a dynamic module that can add/remove types
		/// </summary>
		public bool IsDynamic {
			get { return module.IsDynamic; }
		}

		/// <summary>
		/// true if this is an in-memory module
		/// </summary>
		public bool IsInMemory {
			get { return module.IsInMemory; }
		}

		/// <summary>
		/// Gets the owner debugger
		/// </summary>
		public DnDebugger Debugger {
			get { return Assembly.Debugger; }
		}

		/// <summary>
		/// Gets the owner process
		/// </summary>
		public DnProcess Process {
			get { return Assembly.Process; }
		}

		/// <summary>
		/// Gets the owner AppDomain
		/// </summary>
		public DnAppDomain AppDomain {
			get { return Assembly.AppDomain; }
		}

		/// <summary>
		/// Gets the owner assembly
		/// </summary>
		public DnAssembly Assembly {
			get { return ownerAssembly; }
		}
		readonly DnAssembly ownerAssembly;

		public SerializedDnModule SerializedDnModule {
			get { return serializedDnModule; }
		}
		SerializedDnModule serializedDnModule;

		/// <summary>
		/// Gets the JIT compiler flags. This is a cached value and never gets updated
		/// </summary>
		public CorDebugJITCompilerFlags CachedJITCompilerFlags {
			get { return jitFlags; }
		}
		CorDebugJITCompilerFlags jitFlags;

		internal DnModule(DnAssembly ownerAssembly, ICorDebugModule module, int uniqueId, int uniqueIdProcess, int uniqueIdAppDomain) {
			this.ownerAssembly = ownerAssembly;
			this.module = new CorModule(module);
			this.uniqueId = uniqueId;
			this.uniqueIdProcess = uniqueIdProcess;
			this.uniqueIdAppDomain = uniqueIdAppDomain;
			this.serializedDnModule = this.module.SerializedDnModule;
		}

		internal void InitializeCachedValues() {
			// Cache the value so it's possible to read it even when the process is running
			jitFlags = module.JITCompilerFlags;
		}

		internal void SetHasUnloaded() {
			hasUnloaded = true;
		}

		public override string ToString() {
			return string.Format("{0} DYN={1} MEM={2} A={3:X8} S={4:X8} {5}", UniqueId, IsDynamic ? 1 : 0, IsInMemory ? 1 : 0, Address, Size, Name);
		}
	}
}
