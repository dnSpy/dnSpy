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

using System.Diagnostics;

namespace dnSpy.Debugger.Memory {
	static class MemoryUtils {
		public static void ShowInMemoryWindow(ulong addr, ulong size) {
			var mc = GetMemoryControl(addr, size);
			if (mc == null)
				mc = MemoryControlCreator.GetMemoryControlInstance(0);
			ShowInMemoryWindow(mc, addr, size);
		}

		public static void ShowInMemoryWindow(int windowNumber, ulong addr, ulong size) {
			var mc = GetMemoryControl(windowNumber);
			Debug.Assert(mc != null);
			if (mc == null)
				return;
			ShowInMemoryWindow(mc, addr, size);
		}

		static void ShowInMemoryWindow(MemoryControl mc, ulong addr, ulong size) {
			MakeSureAddressCanBeShown(mc, addr, size);
			if (mc.CanShow)
				mc.Show();
			mc.hexBox.SelectAndMoveCaret(addr, size);
		}

		static void MakeSureAddressCanBeShown(MemoryControl mc, ulong addr, ulong size) {
			if (CanShowAll(mc, addr, size))
				return;
			mc.hexBox.InitializeStartEndOffsetToDocument();
		}

		static bool CanShowAll(MemoryControl mc, ulong addr, ulong size) {
			if (size == 0)
				size = 1;
			var endAddr = addr + size - 1;
			if (endAddr < addr)
				endAddr = ulong.MaxValue;
			var hb = mc.hexBox;
			return addr >= hb.StartOffset && endAddr <= hb.EndOffset && hb.StartOffset <= hb.EndOffset;
		}

		static MemoryControl GetMemoryControl(ulong addr, ulong size) {
			for (int i = 0; i < MemoryControlCreator.NUMBER_OF_MEMORY_WINDOWS; i++) {
				var mc = MemoryControlCreator.GetMemoryControlInstance(i);
				if (CanShowAll(mc, addr, size))
					return mc;
			}
			return null;
		}

		static MemoryControl GetMemoryControl(int windowNumber) {
			int i = windowNumber - 1;
			if ((uint)i >= MemoryControlCreator.NUMBER_OF_MEMORY_WINDOWS)
				return null;
			return MemoryControlCreator.GetMemoryControlInstance(i);
		}
	}
}
