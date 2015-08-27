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

using System.Text;
using dndbg.Engine.COM.CorDebug;

namespace dndbg.Engine {
	/// <summary>
	/// A loaded module
	/// </summary>
	public sealed class DnModule {
		/// <summary>
		/// Gets the COM object
		/// </summary>
		public ICorDebugModule RawObject {
			get { return module; }
		}
		readonly ICorDebugModule module;

		/// <summary>
		/// Unique id per Assembly. Each new created module gets an incremented value.
		/// </summary>
		public int IncrementedId {
			get { return incrementedId; }
		}
		readonly int incrementedId;

		/// <summary>
		/// Module name, and is usually the full path to the manifest (first) module on disk
		/// (the EXE or DLL file).
		/// If it's a dynamic module, the name is eg. "&lt;unknown&gt;" but don't rely on it.
		/// </summary>
		public string Name {
			get { return name; }
		}
		readonly string name;

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
			get { return address; }
		}
		readonly ulong address;

		/// <summary>
		/// Gets the size of the module or 0
		/// </summary>
		public uint Size {
			get { return size; }
		}
		readonly uint size;

		/// <summary>
		/// Gets the token or 0
		/// </summary>
		public uint Token {
			get { return token; }
		}
		readonly uint token;

		/// <summary>
		/// true if it's a dynamic module that can add/remove types
		/// </summary>
		public bool IsDynamic {
			get { return isDynamic; }
		}
		readonly bool isDynamic;

		/// <summary>
		/// true if this is an in-memory module
		/// </summary>
		public bool IsInMemory {
			get { return isInMemory; }
		}
		readonly bool isInMemory;

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
			get { return new SerializedDnModule(Name, IsDynamic, IsInMemory); }
		}

		internal DnModule(DnAssembly ownerAssembly, ICorDebugModule module, int incrementedId) {
			this.ownerAssembly = ownerAssembly;
			this.module = module;
			this.incrementedId = incrementedId;
			this.name = GetName(module) ?? string.Empty;

			int hr = module.GetBaseAddress(out this.address);
			if (hr < 0)
				this.address = 0;
			hr = module.GetSize(out this.size);
			if (hr < 0)
				this.size = 0;
			hr = module.GetToken(out this.token);
			if (hr < 0)
				this.token = 0;

			int b;
			hr = module.IsDynamic(out b);
			this.isDynamic = hr >= 0 && b != 0;
			hr = module.IsInMemory(out b);
			this.isInMemory = hr >= 0 && b != 0;
		}

		static string GetName(ICorDebugModule module) {
			uint cchName = 0;
			int hr = module.GetName(0, out cchName, null);
			if (hr < 0)
				return null;
			var sb = new StringBuilder((int)cchName);
			hr = module.GetName(cchName, out cchName, sb);
			if (hr < 0)
				return null;
			return sb.ToString();
		}

		internal void SetHasUnloaded() {
			hasUnloaded = true;
		}

		public override string ToString() {
			return string.Format("{0} DYN={1} MEM={2} A={3:X8} S={4:X8} {5}", IncrementedId, IsDynamic ? 1 : 0, IsInMemory ? 1 : 0, Address, Size, Name);
		}
	}
}
