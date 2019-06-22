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
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace dnSpy.Controls {
	readonly struct Screen {
		/// <summary>
		/// true if we managed to get monitor info
		/// </summary>
		public bool IsValid { get; }

		/// <summary>
		/// true if it's the primary monitor
		/// </summary>
		public bool IsPrimary => (info.dwFlags & MONITORINFOF_PRIMARY) != 0;

		/// <summary>
		/// Display monitor rectangle, expressed in virtual-screen coordinates.
		/// Note that if the monitor is not the primary display monitor, some of the rectangle's coordinates may be negative values.
		/// </summary>
		public Rect DisplayRect => new Rect(info.rcMonitor.left, info.rcMonitor.top, info.rcMonitor.right - info.rcMonitor.left, info.rcMonitor.bottom - info.rcMonitor.top);

		/// <summary>
		/// Work area rectangle of the display monitor, expressed in virtual-screen coordinates.
		/// Note that if the monitor is not the primary display monitor, some of the rectangle's coordinates may be negative values.
		/// </summary>
		public Rect WorkRect => new Rect(info.rcWork.left, info.rcWork.top, info.rcWork.right - info.rcWork.left, info.rcWork.bottom - info.rcWork.top);

		/// <summary>
		/// Gets the name of the device
		/// </summary>
		public string DeviceName => info.szDevice ?? string.Empty;

		[DllImport("user32")]
		static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);
		const int MONITOR_DEFAULTTONULL = 0x00000000;
		const int MONITOR_DEFAULTTOPRIMARY = 0x00000001;
		const int MONITOR_DEFAULTTONEAREST = 0x00000002;

		[DllImport("user32", CharSet = CharSet.Auto)]
		static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFOEX lpmi);
		const uint MONITORINFOF_PRIMARY = 0x00000001;

		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
		struct MONITORINFOEX {
			public int cbSize;
			public RECT rcMonitor;
			public RECT rcWork;
			public uint dwFlags;	// MONITORINFOF_XXX
			const int CCHDEVICENAME = 32;
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = CCHDEVICENAME)]
			public string szDevice;
			public const int SIZE = 4 + 4 * 4 + 4 * 4 + 4 + 2 * CCHDEVICENAME;
		}

		struct RECT {
			public int left;
			public int top;
			public int right;
			public int bottom;
		}

		readonly MONITORINFOEX info;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="o">Dependency object attached somewhere in a <see cref="Window"/> or the <see cref="Window"/> object itself</param>
		public Screen(DependencyObject? o) {
			var helper = GetHelper(o);
			IsValid = false;
			info = default;
			if (!(helper is null)) {
				var hMonitor = MonitorFromWindow(helper.Handle, MONITOR_DEFAULTTONEAREST);
				info.cbSize = MONITORINFOEX.SIZE;
				if (!GetMonitorInfo(hMonitor, ref info))
					info = default;
				else
					IsValid = true;
			}
		}

		static WindowInteropHelper? GetHelper(DependencyObject? o) {
			if (o is null)
				return null;
			var win = Window.GetWindow(o);
			if (win is null)
				return null;
			var helper = new WindowInteropHelper(win);
			if (helper.Handle == IntPtr.Zero)
				helper.EnsureHandle();
			if (helper.Handle == IntPtr.Zero)
				return null;
			return helper;
		}
	}
}
