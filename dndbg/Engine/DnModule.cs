/*
    Copyright (C) 2014-2015 de4dot@gmail.com

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

using dndbg.Engine.COM.CorDebug;

namespace dndbg.Engine {
	/// <summary>
	/// A loaded module
	/// </summary>
	public sealed class DnModule {
		public CorModule CorModule {
			get { return module; }
		}
		readonly CorModule module;

		/// <summary>
		/// Unique id per Assembly. Each new created module gets an incremented value.
		/// </summary>
		public int IncrementedId {
			get { return incrementedId; }
		}
		readonly int incrementedId;

		/// <summary>
		/// For on-disk modules this is a full path. For dynamic modules this is just the filename
		/// if one was provided. Otherwise, and for other in-memory modules, this is just the simple
		/// name stored in the module's metadata.
		/// </summary>
		public string Name {
			get { return module.Name; }
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
			get { return module.SerializedDnModule; }
		}

		public SerializedDnModuleWithAssembly SerializedDnModuleWithAssembly {
			get { return new SerializedDnModuleWithAssembly(Assembly.Name, SerializedDnModule); }
		}

		internal DnModule(DnAssembly ownerAssembly, ICorDebugModule module, int incrementedId) {
			this.ownerAssembly = ownerAssembly;
			this.module = new CorModule(module);
			this.incrementedId = incrementedId;
		}

		internal void SetHasUnloaded() {
			hasUnloaded = true;
		}

		public override string ToString() {
			return string.Format("{0} DYN={1} MEM={2} A={3:X8} S={4:X8} {5}", IncrementedId, IsDynamic ? 1 : 0, IsInMemory ? 1 : 0, Address, Size, Name);
		}
	}
}
