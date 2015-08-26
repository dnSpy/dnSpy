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

using System;
using System.Runtime.InteropServices;
using System.Text;

namespace dndbg.Engine.COM.MetaHost {
	[Guid("00000100-0000-0000-C000-000000000046"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[ComImport]
	public interface IEnumUnknown {
		void RemoteNext([In] uint celt, [MarshalAs(UnmanagedType.IUnknown)] out object rgelt, out uint pceltFetched);
		void Skip([In] uint celt);
		void Reset();
		void Clone([MarshalAs(UnmanagedType.Interface)] out IEnumUnknown ppenum);
	}
	[Guid("D332DB9E-B9B3-4125-8207-A14884F53216"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[ComImport]
	public interface ICLRMetaHost {
		[return: MarshalAs(UnmanagedType.Interface)]
		object GetRuntime([MarshalAs(UnmanagedType.LPWStr)] [In] string pwzVersion, [In] ref Guid riid);
		void GetVersionFromFile([MarshalAs(UnmanagedType.LPWStr)] [In] string pwzFilePath, [MarshalAs(UnmanagedType.LPWStr)] [Out] StringBuilder pwzBuffer, [In] [Out] ref uint pcchBuffer);
		[return: MarshalAs(UnmanagedType.Interface)]
		IEnumUnknown EnumerateInstalledRuntimes();
		[return: MarshalAs(UnmanagedType.Interface)]
		IEnumUnknown EnumerateLoadedRuntimes([In] IntPtr hndProcess);
		void RequestRuntimeLoadedNotification([MarshalAs(UnmanagedType.Interface)] [In] ICLRMetaHost pCallbackFunction);
		IntPtr QueryLegacyV2RuntimeBinding([In] ref Guid riid);
		void ExitProcess([In] int iExitCode);
	}
	[Guid("BD39D1D2-BA2F-486A-89B0-B4B0CB466891"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[ComImport]
	public interface ICLRRuntimeInfo {
		void GetVersionString([MarshalAs(UnmanagedType.LPWStr)] [Out] StringBuilder pwzBuffer, [In] [Out] ref uint pcchBuffer);
		void GetRuntimeDirectory([MarshalAs(UnmanagedType.LPWStr)] [Out] StringBuilder pwzBuffer, [In] [Out] ref uint pcchBuffer);
		int IsLoaded([In] IntPtr hndProcess);
		[LCIDConversion(3)]
		void LoadErrorString([In] uint iResourceID, [MarshalAs(UnmanagedType.LPWStr)] [Out] StringBuilder pwzBuffer, [In] [Out] ref uint pcchBuffer);
		IntPtr LoadLibrary([MarshalAs(UnmanagedType.LPWStr)] [In] string pwzDllName);
		IntPtr GetProcAddress([MarshalAs(UnmanagedType.LPStr)] [In] string pszProcName);
		[return: MarshalAs(UnmanagedType.IUnknown)]
		object GetInterface([In] ref Guid rclsid, [In] ref Guid riid);
		int IsLoadable();
		void SetDefaultStartupFlags([In] uint dwStartupFlags, [MarshalAs(UnmanagedType.LPWStr)] [In] string pwzHostConfigFile);
		void GetDefaultStartupFlags(out uint pdwStartupFlags, [MarshalAs(UnmanagedType.LPWStr)] [Out] StringBuilder pwzHostConfigFile, [In] [Out] ref uint pcchHostConfigFile);
		void BindAsLegacyV2Runtime();
		void IsStarted(out int pbStarted, out uint pdwStartupFlags);
	}
}
