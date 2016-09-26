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
using System.ComponentModel.Composition;
using System.Diagnostics;
using dnSpy.Contracts.ToolWindows.App;

namespace dnSpy.Debugger.Memory {
	interface IMemoryWindowService {
		void Show(ulong addr, ulong size);
		void Show(ulong addr, ulong size, int windowIndex);
	}

	[Export(typeof(IMemoryWindowService))]
	sealed class MemoryWindowService : IMemoryWindowService {
		readonly Lazy<MemoryToolWindowContentProvider> memoryToolWindowContentProvider;
		readonly IDsToolWindowService toolWindowService;

		[ImportingConstructor]
		MemoryWindowService(Lazy<MemoryToolWindowContentProvider> memoryToolWindowContentProvider, IDsToolWindowService toolWindowService) {
			this.memoryToolWindowContentProvider = memoryToolWindowContentProvider;
			this.toolWindowService = toolWindowService;
		}

		public void Show(ulong addr, ulong size) {
			var mc = GetMemoryToolWindowContent(addr, size);
			if (mc == null)
				mc = memoryToolWindowContentProvider.Value.Contents[0].Content;
			ShowInMemoryWindow(mc, addr, size);
		}

		public void Show(ulong addr, ulong size, int windowIndex) {
			var mc = GetMemoryToolWindowContent(windowIndex);
			Debug.Assert(mc != null);
			if (mc == null)
				return;
			ShowInMemoryWindow(mc, addr, size);
		}

		void ShowInMemoryWindow(MemoryToolWindowContent mc, ulong addr, ulong size) {
			MakeSureAddressCanBeShown(mc, addr, size);
			toolWindowService.Show(mc);
			mc.DnHexBox.SelectAndMoveCaret(addr, size);
		}

		void MakeSureAddressCanBeShown(MemoryToolWindowContent mc, ulong addr, ulong size) {
			if (CanShowAll(mc, addr, size))
				return;
			mc.DnHexBox.InitializeStartEndOffsetToDocument();
		}

		bool CanShowAll(MemoryToolWindowContent mc, ulong addr, ulong size) {
			if (size == 0)
				size = 1;
			var endAddr = addr + size - 1;
			if (endAddr < addr)
				endAddr = ulong.MaxValue;
			var hb = mc.DnHexBox;
			return addr >= hb.StartOffset && endAddr <= hb.EndOffset && hb.StartOffset <= hb.EndOffset;
		}

		MemoryToolWindowContent GetMemoryToolWindowContent(ulong addr, ulong size) {
			foreach (var info in memoryToolWindowContentProvider.Value.Contents) {
				var mc = info.Content;
				if (CanShowAll(mc, addr, size))
					return mc;
			}
			return null;
		}

		MemoryToolWindowContent GetMemoryToolWindowContent(int windowIndex) {
			if ((uint)windowIndex >= memoryToolWindowContentProvider.Value.Contents.Length)
				return null;
			return memoryToolWindowContentProvider.Value.Contents[windowIndex].Content;
		}
	}
}
