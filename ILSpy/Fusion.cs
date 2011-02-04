// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;

namespace ICSharpCode.ILSpy
{
	// .NET Fusion COM interfaces
	[ComImport(), Guid("E707DCDE-D1CD-11D2-BAB9-00C04F8ECEAE"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	internal interface IAssemblyCache
	{
		[PreserveSig()]
		int UninstallAssembly(uint dwFlags,
		                      [MarshalAs(UnmanagedType.LPWStr)] string pszAssemblyName,
		                      IntPtr pvReserved,
		                      out uint pulDisposition);
		
		[PreserveSig()]
		int QueryAssemblyInfo(uint dwFlags,
		                      [MarshalAs(UnmanagedType.LPWStr)] string pszAssemblyName,
		                      IntPtr pAsmInfo);
		
		[PreserveSig()]
		int CreateAssemblyCacheItem(uint dwFlags,
		                            IntPtr pvReserved,
		                            out IAssemblyCacheItem ppAsmItem,
		                            [MarshalAs(UnmanagedType.LPWStr)] string pszAssemblyName);
		
		[PreserveSig()]
		int CreateAssemblyScavenger(out object ppAsmScavenger);
		
		[PreserveSig()]
		int InstallAssembly(uint dwFlags,
		                    [MarshalAs(UnmanagedType.LPWStr)] string pszManifestFilePath,
		                    IntPtr pvReserved);
	}
	
	[ComImport(), Guid("9E3AAEB4-D1CD-11D2-BAB9-00C04F8ECEAE"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	internal interface IAssemblyCacheItem
	{
		void CreateStream([MarshalAs(UnmanagedType.LPWStr)] string pszName,
		                  uint dwFormat,
		                  uint dwFlags,
		                  uint dwMaxSize,
		                  out IStream ppStream);
		
		void IsNameEqual(IAssemblyName pName);
		
		void Commit(uint dwFlags);
		
		void MarkAssemblyVisible(uint dwFlags);
	}
	
	[ComImport(), Guid("CD193BC0-B4BC-11D2-9833-00C04FC31D2E"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	internal interface IAssemblyName
	{
		[PreserveSig()]
		int SetProperty(uint PropertyId, IntPtr pvProperty, uint cbProperty);
		
		[PreserveSig()]
		int GetProperty(uint PropertyId, IntPtr pvProperty, ref uint pcbProperty);
		
		[PreserveSig()]
		int Finalize();
		
		[PreserveSig()]
		int GetDisplayName([Out(), MarshalAs(UnmanagedType.LPWStr)] StringBuilder szDisplayName,
		                   ref uint pccDisplayName,
		                   uint dwDisplayFlags);
		
		[PreserveSig()]
		int BindToObject(object refIID,
		                 object pAsmBindSink,
		                 IApplicationContext pApplicationContext,
		                 [MarshalAs(UnmanagedType.LPWStr)] string szCodeBase,
		                 long llFlags,
		                 int pvReserved,
		                 uint cbReserved,
		                 out int ppv);
		
		[PreserveSig()]
		int GetName(ref uint lpcwBuffer,
		            [Out(), MarshalAs(UnmanagedType.LPWStr)] StringBuilder pwzName);
		
		[PreserveSig()]
		int GetVersion(out uint pdwVersionHi, out uint pdwVersionLow);
		
		[PreserveSig()]
		int IsEqual(IAssemblyName pName,
		            uint dwCmpFlags);
		
		[PreserveSig()]
		int Clone(out IAssemblyName pName);
	}
	
	[ComImport(), Guid("7C23FF90-33AF-11D3-95DA-00A024A85B51"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	internal interface IApplicationContext
	{
		void SetContextNameObject(IAssemblyName pName);
		
		void GetContextNameObject(out IAssemblyName ppName);
		
		void Set([MarshalAs(UnmanagedType.LPWStr)] string szName,
		         int pvValue,
		         uint cbValue,
		         uint dwFlags);
		
		void Get([MarshalAs(UnmanagedType.LPWStr)] string szName,
		         out int pvValue,
		         ref uint pcbValue,
		         uint dwFlags);
		
		void GetDynamicDirectory(out int wzDynamicDir,
		                         ref uint pdwSize);
	}
	
	[ComImport(), Guid("21B8916C-F28E-11D2-A473-00C04F8EF448"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	internal interface IAssemblyEnum
	{
		[PreserveSig()]
		int GetNextAssembly(out IApplicationContext ppAppCtx,
		                    out IAssemblyName ppName,
		                    uint dwFlags);
		
		[PreserveSig()]
		int Reset();
		
		[PreserveSig()]
		int Clone(out IAssemblyEnum ppEnum);
	}
	
	
	[ComImport(), Guid("1D23DF4D-A1E2-4B8B-93D6-6EA3DC285A54"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	internal interface IHistoryReader
	{
		[PreserveSig()]
		int GetFilePath([Out(), MarshalAs(UnmanagedType.LPWStr)] StringBuilder wzFilePath,
		                ref uint pdwSize);
		
		[PreserveSig()]
		int GetApplicationName([Out(), MarshalAs(UnmanagedType.LPWStr)] StringBuilder wzAppName,
		                       ref uint pdwSize);
		
		[PreserveSig()]
		int GetEXEModulePath([Out(), MarshalAs(UnmanagedType.LPWStr)] StringBuilder wzExePath,
		                     ref uint pdwSize);
		
		void GetNumActivations(out uint pdwNumActivations);
		
		void GetActivationDate(uint dwIdx,             // One-based!
		                       out long /* FILETIME */ pftDate);
		
		[PreserveSig()]
		int GetRunTimeVersion(ref long /* FILETIME */ pftActivationDate,
		                      [Out(), MarshalAs(UnmanagedType.LPWStr)] StringBuilder wzRunTimeVersion,
		                      ref uint pdwSize);
		
		void GetNumAssemblies(ref long /* FILETIME */ pftActivationDate,
		                      out uint pdwNumAsms);
		
		void GetHistoryAssembly(ref long /* FILETIME */ pftActivationDate,
		                        uint dwIdx,             // One-based!
		                        [MarshalAs(UnmanagedType.IUnknown)] out object ppHistAsm);
		
	}
	
	internal static class Fusion
	{
		[DllImport("fusion.dll", CharSet=CharSet.Auto)]
		internal static extern int CreateAssemblyCache(out IAssemblyCache ppAsmCache,
		                                               uint dwReserved);
		
		// dwFlags: 1 = Enumerate native image (NGEN) assemblies
		//          2 = Enumerate GAC assemblies
		//          4 = Enumerate Downloaded assemblies
		//
		[DllImport("fusion.dll", CharSet=CharSet.Auto)]
		internal static extern int CreateAssemblyEnum(out IAssemblyEnum ppEnum,
		                                              IApplicationContext pAppCtx,
		                                              IAssemblyName pName,
		                                              uint dwFlags,
		                                              int pvReserved);
		
		[DllImport("fusion.dll", CharSet=CharSet.Auto)]
		internal static extern int CreateAssemblyNameObject(out IAssemblyName ppName,
		                                                    string szAssemblyName,
		                                                    uint dwFlags,
		                                                    int pvReserved);
		
		// ?????
		[DllImport("fusion.dll")]
		internal static extern int CreateApplicationContext(out IApplicationContext ppAppContext,
		                                                    uint dw);
		
		[DllImport("fusion.dll")]
		internal static extern int GetCachePath(uint flags,
		                                        [MarshalAs(UnmanagedType.LPWStr)] StringBuilder wzDir,
		                                        ref uint pdwSize);
		
		public static string GetGacPath(bool isCLRv4 = false)
		{
			const uint ASM_CACHE_ROOT    = 0x08; // CLR V2.0
			const uint ASM_CACHE_ROOT_EX = 0x80; // CLR V4.0
			uint flags = isCLRv4 ? ASM_CACHE_ROOT_EX : ASM_CACHE_ROOT;
			
			const int size = 260; // MAX_PATH
			StringBuilder b = new StringBuilder(size);
			uint tmp = size;
			GetCachePath(flags, b, ref tmp);
			return b.ToString();
		}
	}
}
