// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

#pragma warning disable 108, 1591 

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Debugger.Interop
{
    // These are used in both CorDebug and CorSym
     
    [StructLayout(LayoutKind.Sequential, Pack=4)]
    public struct _FILETIME
    {
        public uint dwLowDateTime;
        public uint dwHighDateTime;
    }
    
    [StructLayout(LayoutKind.Sequential, Pack=8)]
    public struct _LARGE_INTEGER
    {
        public long QuadPart;
    }
    
    [StructLayout(LayoutKind.Sequential, Pack=8)]
    public struct _ULARGE_INTEGER
    {
        public ulong QuadPart;
    }
    
    [ComImport, Guid("0C733A30-2A1C-11CE-ADE5-00AA0044773D"), InterfaceType((short) 1)]
    public interface ISequentialStream
    {
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void __RemoteRead(out byte pv, [In] uint cb, out uint pcbRead);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void __RemoteWrite([In] ref byte pv, [In] uint cb, out uint pcbWritten);
    }

    [ComImport, InterfaceType((short) 1), Guid("0000000C-0000-0000-C000-000000000046")]
    public interface IStream : ISequentialStream
    {
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void __RemoteRead(out byte pv, [In] uint cb, out uint pcbRead);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void __RemoteWrite([In] ref byte pv, [In] uint cb, out uint pcbWritten);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void __RemoteSeek([In] _LARGE_INTEGER dlibMove, [In] uint dwOrigin, out _ULARGE_INTEGER plibNewPosition);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void __SetSize([In] _ULARGE_INTEGER libNewSize);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void __RemoteCopyTo([In, MarshalAs(UnmanagedType.Interface)] IStream pstm, [In] _ULARGE_INTEGER cb, out _ULARGE_INTEGER pcbRead, out _ULARGE_INTEGER pcbWritten);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void __Commit([In] uint grfCommitFlags);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void __Revert();
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void __LockRegion([In] _ULARGE_INTEGER libOffset, [In] _ULARGE_INTEGER cb, [In] uint dwLockType);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void __UnlockRegion([In] _ULARGE_INTEGER libOffset, [In] _ULARGE_INTEGER cb, [In] uint dwLockType);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void __Stat(out tagSTATSTG pstatstg, [In] uint grfStatFlag);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void __Clone([MarshalAs(UnmanagedType.Interface)] out IStream ppstm);
    }
    
    [StructLayout(LayoutKind.Sequential, Pack=8)]
    public struct tagSTATSTG
    {
        [MarshalAs(UnmanagedType.LPWStr)]
        public string pwcsName;
        public uint type;
        public _ULARGE_INTEGER cbSize;
        public _FILETIME mtime;
        public _FILETIME ctime;
        public _FILETIME atime;
        public uint grfMode;
        public uint grfLocksSupported;
        public Guid clsid;
        public uint grfStateBits;
        public uint reserved;
    }
}
