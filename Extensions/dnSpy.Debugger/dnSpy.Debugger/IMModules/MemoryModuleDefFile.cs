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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using dndbg.Engine;
using dnlib.DotNet;
using dnlib.PE;
using dnSpy.Contracts.Files;
using dnSpy.Contracts.Metadata;
using dnSpy.Contracts.Utilities;

namespace dnSpy.Debugger.IMModules {
	/// <summary>
	/// A class that reads the module from the debugged process' address space.
	/// </summary>
	sealed class MemoryModuleDefFile : DnSpyDotNetFileBase, IModuleIdHolder {
		sealed class MyKey : IDnSpyFilenameKey {
			readonly DnProcess process;
			readonly ulong address;

			public MyKey(DnProcess process, ulong address) {
				this.process = process;
				this.address = address;
			}

			public override bool Equals(object obj) {
				var o = obj as MyKey;
				return o != null && process == o.process && address == o.address;
			}

			public override int GetHashCode() => process.GetHashCode() ^ (int)address ^ (int)(address >> 32);
		}

		public ModuleId ModuleId {
			get {
				if (!isInMemory)
					return Contracts.Metadata.ModuleId.CreateFromFile(ModuleDef);
				return Contracts.Metadata.ModuleId.CreateInMemory(ModuleDef);
			}
		}

		public override IDnSpyFilenameKey Key => CreateKey(Process, Address);
		public override DnSpyFileInfo? SerializedFile => null;
		public bool AutoUpdateMemory { get; }
		public DnProcess Process { get; }
		public ulong Address { get; }

		readonly byte[] data;
		readonly bool isInMemory;

		MemoryModuleDefFile(DnProcess process, ulong address, byte[] data, bool isInMemory, ModuleDef module, bool loadSyms, bool autoUpdateMemory)
			: base(module, loadSyms) {
			this.Process = process;
			this.Address = address;
			this.data = data;
			this.isInMemory = isInMemory;
			this.AutoUpdateMemory = autoUpdateMemory;
		}

		public static IDnSpyFilenameKey CreateKey(DnProcess process, ulong address) => new MyKey(process, address);

		protected override List<IDnSpyFile> CreateChildren() {
			var list = new List<IDnSpyFile>();
			if (files != null) {
				list.AddRange(files);
				files = null;
			}
			return list;
		}
		List<MemoryModuleDefFile> files;

		public bool UpdateMemory() {
			if (Process.HasExited)
				return false;
			//TODO: Only compare the smallest possible region, eg. all MD and IL bodies. Don't include writable sects.
			var newData = new byte[data.Length];
			ProcessMemoryUtils.ReadMemory(Process, Address, newData, 0, data.Length);
			if (Equals(data, newData))
				return false;
			Array.Copy(newData, data, data.Length);
			return true;
		}

		static bool Equals(byte[] a, byte[] b) {
			if (a == b)
				return true;
			if (a == null || b == null)
				return false;
			if (a.Length != b.Length)
				return false;
			for (int i = 0; i < a.Length; i++) {
				if (a[i] != b[i])
					return false;
			}
			return true;
		}

		public static MemoryModuleDefFile CreateAssembly(List<MemoryModuleDefFile> files) {
			var manifest = files[0];
			var file = new MemoryModuleDefFile(manifest.Process, manifest.Address, manifest.data, manifest.isInMemory, manifest.ModuleDef, false, manifest.AutoUpdateMemory);
			file.files = new List<MemoryModuleDefFile>(files);
			return file;
		}

		public static MemoryModuleDefFile Create(DnModule dnModule, bool loadSyms) {
			Debug.Assert(!dnModule.IsDynamic);
			Debug.Assert(dnModule.Address != 0);
			ulong address = dnModule.Address;
			var process = dnModule.Process;
			var data = new byte[dnModule.Size];
			string location = dnModule.IsInMemory ? string.Empty : dnModule.Name;

			ProcessMemoryUtils.ReadMemory(process, address, data, 0, data.Length);

			var peImage = new PEImage(data, GetImageLayout(dnModule), true);
			var module = ModuleDefMD.Load(peImage);
			module.Location = location;
			bool autoUpdateMemory = false;//TODO: Init to default value
			if (GacInfo.IsGacPath(dnModule.Name))
				autoUpdateMemory = false;	// GAC files are not likely to decrypt methods in memory
			return new MemoryModuleDefFile(process, address, data, dnModule.IsInMemory, module, loadSyms, autoUpdateMemory);
		}

		static ImageLayout GetImageLayout(DnModule module) {
			Debug.Assert(!module.IsDynamic);
			return module.IsInMemory ? ImageLayout.File : ImageLayout.Memory;
		}
	}
}
