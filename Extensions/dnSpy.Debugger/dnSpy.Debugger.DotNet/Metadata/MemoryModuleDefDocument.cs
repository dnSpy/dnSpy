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
using System.Diagnostics;
using dnlib.DotNet;
using dnlib.PE;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Documents;
using dnSpy.Contracts.Metadata;
using dnSpy.Contracts.Utilities;

namespace dnSpy.Debugger.DotNet.Metadata {
	sealed class MemoryModuleDefDocument : DsDotNetDocumentBase, IModuleIdHolder {
		sealed class DocKey : IDsDocumentNameKey {
			readonly DbgProcess process;
			readonly ulong address;

			public DocKey(DbgProcess process, ulong address) {
				this.process = process ?? throw new ArgumentNullException(nameof(process));
				this.address = address;
			}

			public override bool Equals(object obj) => obj is DocKey o && process == o.process && address == o.address;
			public override int GetHashCode() => process.GetHashCode() ^ address.GetHashCode();
		}

		public ModuleId ModuleId => isInMemory ? ModuleId.CreateInMemory(ModuleDef) : ModuleId.CreateFromFile(ModuleDef);
		public override IDsDocumentNameKey Key => CreateKey(Process, Address);
		public override DsDocumentInfo? SerializedDocument => null;
		public bool AutoUpdateMemory { get; }
		public DbgProcess Process { get; }
		public ulong Address { get; }
		public override bool IsActive => Process.State != DbgProcessState.Terminated;

		readonly DbgInMemoryModuleServiceImpl owner;
		readonly byte[] data;
		readonly bool isInMemory;

		MemoryModuleDefDocument(DbgInMemoryModuleServiceImpl owner, DbgProcess process, ulong address, byte[] data, bool isInMemory, ModuleDef module, bool loadSyms, bool autoUpdateMemory)
			: base(module, loadSyms) {
			this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
			Process = process ?? throw new ArgumentNullException(nameof(process));
			Address = address;
			this.data = data ?? throw new ArgumentNullException(nameof(data));
			this.isInMemory = isInMemory;
			AutoUpdateMemory = autoUpdateMemory;
		}

		public static IDsDocumentNameKey CreateKey(DbgProcess process, ulong address) => new DocKey(process, address);

		protected override TList<IDsDocument> CreateChildren() {
			var list = new TList<IDsDocument>();
			if (files != null) {
				list.AddRange(files);
				files = null;
			}
			return list;
		}
		List<MemoryModuleDefDocument> files;

		public void UpdateMemory() => owner.UpdateModuleMemory(this);

		internal bool TryUpdateMemory() {
			if (Process.State == DbgProcessState.Terminated)
				return false;
			var newData = new byte[data.Length];
			Process.ReadMemory(Address, newData, 0, data.Length);
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

		public static MemoryModuleDefDocument CreateAssembly(List<MemoryModuleDefDocument> files) {
			var manifest = files[0];
			var file = new MemoryModuleDefDocument(manifest.owner, manifest.Process, manifest.Address, manifest.data, manifest.isInMemory, manifest.ModuleDef, false, manifest.AutoUpdateMemory);
			file.files = new List<MemoryModuleDefDocument>(files);
			return file;
		}

		public static MemoryModuleDefDocument Create(DbgInMemoryModuleServiceImpl owner, DbgModule module, bool loadSyms) {
			Debug.Assert(!module.IsDynamic);
			Debug.Assert(module.HasAddress);
			var data = new byte[module.Size];
			module.Process.ReadMemory(module.Address, data, 0, data.Length);

			var peImage = new PEImage(data, GetImageLayout(module), true);
			var mod = ModuleDefMD.Load(peImage);
			mod.Location = module.IsInMemory ? string.Empty : module.Filename;
			bool autoUpdateMemory = false;
			if (GacInfo.IsGacPath(mod.Location))
				autoUpdateMemory = false;   // GAC files are not likely to decrypt methods in memory
			return new MemoryModuleDefDocument(owner, module.Process, module.Address, data, module.IsInMemory, mod, loadSyms, autoUpdateMemory);
		}

		static ImageLayout GetImageLayout(DbgModule module) {
			Debug.Assert(!module.IsDynamic);
			switch (module.ImageLayout) {
			case DbgImageLayout.File:		return ImageLayout.File;
			case DbgImageLayout.Memory:		return ImageLayout.Memory;
			case DbgImageLayout.Unknown:
			default:
				Debug.Fail($"Unsupported image layout: {module.ImageLayout}");
				return ImageLayout.File;
			}
		}
	}
}
