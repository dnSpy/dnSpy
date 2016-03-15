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
using System.IO;
using System.Linq;
using dndbg.Engine;
using dnlib.PE;
using dnSpy.Shared.MVVM.Dialogs;

namespace dnSpy.Debugger.Modules {
	sealed class PEFilesSaver : IProgressTask {
		sealed class ModuleInfo {
			public readonly IntPtr ProcessHandle;
			public readonly ulong Address;
			public readonly uint Size;
			public readonly string Filename;
			public bool MemoryLayout;

			public ModuleInfo(IntPtr handle, ulong addr, uint size, string filename, bool memoryLayout) {
				this.ProcessHandle = handle;
				this.Address = addr;
				this.Size = size;
				this.Filename = filename;
				this.MemoryLayout = memoryLayout;
			}
		}

		readonly ModuleInfo[] infos;

		public PEFilesSaver(Tuple<DnModule, string>[] files) {
			this.infos = new ModuleInfo[files.Length];
			for (int i = 0; i < files.Length; i++) {
				var module = files[i].Item1;
				this.infos[i] = new ModuleInfo(module.Process.CorProcess.Handle, module.Address, module.Size, files[i].Item2, !module.IsInMemory);
				maxProgress += 2;
			}
		}

		bool IProgressTask.IsIndeterminate {
			get { return false; }
		}

		double IProgressTask.ProgressMinimum {
			get { return 0; }
		}

		double IProgressTask.ProgressMaximum {
			get { return maxProgress; }
		}
		readonly uint maxProgress;
		uint currentProgress;

		IProgress progress;

		unsafe void IProgressTask.Execute(IProgress progress) {
			this.progress = progress;
			if (infos.Length == 0)
				return;

			uint maxSize = infos.Max(a => a.Size);
			var buf = new byte[maxSize];
			byte[] buf2 = null;
			foreach (var info in infos) {
				progress.ThrowIfCancellationRequested();
				progress.SetDescription(Path.GetFileName(info.Filename));

				ProcessMemoryUtils.ReadMemory(info.ProcessHandle, info.Address, buf, 0, (int)info.Size);
				progress.ThrowIfCancellationRequested();
				currentProgress++;
				progress.SetTotalProgress(currentProgress);

				byte[] data = buf;
				int dataSize = (int)info.Size;
				if (info.MemoryLayout) {
					if (buf2 == null)
						buf2 = new byte[buf.Length];
					data = buf2;
					progress.ThrowIfCancellationRequested();
					Array.Clear(buf2, 0, dataSize);
					WritePEFile(buf, buf2, dataSize, out dataSize);
				}

				var file = File.Create(info.Filename);
				try {
					file.Write(data, 0, dataSize);
					currentProgress++;
					progress.SetTotalProgress(currentProgress);
				}
				catch {
					file.Dispose();
					try { File.Delete(info.Filename); }
					catch { }
					throw;
				}
				finally {
					file.Dispose();
				}
			}
		}

		// Very simple, will probably fail if various fields have been overwritten with invalid values
		public static void WritePEFile(byte[] raw, byte[] dst, int rawSize, out int finalSize) {
			try {
				var peImage = new PEImage(raw, ImageLayout.Memory, true);
				int offset = 0;
				Array.Copy(raw, 0, dst, 0, (int)peImage.ImageNTHeaders.OptionalHeader.SizeOfHeaders);
				offset += (int)peImage.ImageNTHeaders.OptionalHeader.SizeOfHeaders;

				foreach (var sect in peImage.ImageSectionHeaders)
					Array.Copy(raw, (int)sect.VirtualAddress, dst, (int)sect.PointerToRawData, (int)sect.SizeOfRawData);

				var lastSect = peImage.ImageSectionHeaders[peImage.ImageSectionHeaders.Count - 1];
				var fa = peImage.ImageNTHeaders.OptionalHeader.FileAlignment;
				finalSize = (int)((lastSect.PointerToRawData + lastSect.SizeOfRawData + fa - 1) & ~(fa - 1));
			}
			catch {
				finalSize = rawSize;
				Array.Copy(raw, dst, rawSize);
			}
		}
	}
}
