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

		public ModuleId ModuleId { get; }
		public override IDsDocumentNameKey Key => CreateKey(Process, Address);
		public override DsDocumentInfo? SerializedDocument => null;
		public bool AutoUpdateMemory { get; }
		public DbgProcess Process { get; }
		public ulong Address { get; }
		public override bool IsActive => Process.State != DbgProcessState.Terminated;

		readonly DbgInMemoryModuleServiceImpl owner;
		readonly byte[] data;

		MemoryModuleDefDocument(DbgInMemoryModuleServiceImpl owner, ModuleId moduleId, DbgProcess process, ulong address, byte[] data, ModuleDef module, bool loadSyms, bool autoUpdateMemory)
			: base(module, loadSyms) {
			this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
			ModuleId = moduleId;
			Process = process ?? throw new ArgumentNullException(nameof(process));
			Address = address;
			this.data = data ?? throw new ArgumentNullException(nameof(data));
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
			byte[] buffer = null;
			try {
				buffer = GetBuffer();
				int pos = 0;
				var addr = Address;
				while (pos < data.Length) {
					int len = Math.Min(data.Length - pos, buffer.Length);
					Process.ReadMemory(addr, buffer, 0, len);
					if (!Equals(data, pos, buffer, len)) {
						Process.ReadMemory(Address, data, 0, data.Length);
						return true;
					}
					addr += (uint)len;
					pos += len;
				}
				return false;
			}
			finally {
				if (buffer != null)
					ReleaseBuffer(buffer);
			}
		}
		WeakReference weakBuffer;
		byte[] GetBuffer() => weakBuffer?.Target as byte[] ?? new byte[0x2000];
		void ReleaseBuffer(byte[] buffer) => weakBuffer = new WeakReference(buffer);

		static bool Equals(byte[] a, int ai, byte[] b, int len) {
			for (int i = 0; i < len; i++) {
				if (a[ai + i] != b[i])
					return false;
			}
			return true;
		}

		public static MemoryModuleDefDocument CreateAssembly(List<MemoryModuleDefDocument> files) {
			var manifest = files[0];
			var file = new MemoryModuleDefDocument(manifest.owner, manifest.ModuleId, manifest.Process, manifest.Address, manifest.data, manifest.ModuleDef, false, manifest.AutoUpdateMemory);
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
			var moduleId = module.Runtime.GetDotNetRuntime().GetModuleId(module);
			return new MemoryModuleDefDocument(owner, moduleId, module.Process, module.Address, data, mod, loadSyms, autoUpdateMemory);
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
