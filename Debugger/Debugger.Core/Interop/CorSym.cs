// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

#pragma warning disable 108, 1591 

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Debugger.Interop.CorSym
{
    public enum CorSymAddrKind
    {
        // Fields
        ADDR_BITFIELD = 9,
        ADDR_IL_OFFSET = 1,
        ADDR_NATIVE_ISECTOFFSET = 10,
        ADDR_NATIVE_OFFSET = 5,
        ADDR_NATIVE_REGISTER = 3,
        ADDR_NATIVE_REGREG = 6,
        ADDR_NATIVE_REGREL = 4,
        ADDR_NATIVE_REGSTK = 7,
        ADDR_NATIVE_RVA = 2,
        ADDR_NATIVE_STKREG = 8
    }

    [ComImport, CoClass(typeof(CorSymBinder_SxSClass)), Guid("AA544D42-28CB-11D3-BD22-0000F80849BD")]
    public interface CorSymBinder_SxS : ISymUnmanagedBinder
    {
    }

    [ComImport, Guid("0A29FF9E-7F9C-4437-8B11-F424491E3931"), ClassInterface((short) 0), TypeLibType((short) 2)]
    public class CorSymBinder_SxSClass : ISymUnmanagedBinder, CorSymBinder_SxS
    {
        // Methods
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime), PreserveSig]
        public virtual extern int __GetReaderForFile([In, MarshalAs(UnmanagedType.IUnknown)] object importer, [In] IntPtr filename, [In] IntPtr searchPath, [Out, In, MarshalAs(UnmanagedType.IUnknown)] ref object retVal);

        [return: MarshalAs(UnmanagedType.Interface)]
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        public virtual extern ISymUnmanagedReader __GetReaderFromStream([In, MarshalAs(UnmanagedType.IUnknown)] object importer, [In, MarshalAs(UnmanagedType.Interface)] IStream pstream);

    }

    [ComImport, CoClass(typeof(CorSymReader_SxSClass)), Guid("B4CE6286-2A6B-3712-A3B7-1EE1DAD467B5")]
    public interface CorSymReader_SxS : ISymUnmanagedReader
    {
    }

    [ComImport, Guid("0A3976C5-4529-4EF8-B0B0-42EED37082CD"), TypeLibType((short) 2), ClassInterface((short) 0), ComConversionLoss]
    public class CorSymReader_SxSClass : ISymUnmanagedReader, CorSymReader_SxS
    {
        // Methods
        [return: MarshalAs(UnmanagedType.Interface)]
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        public virtual extern ISymUnmanagedDocument __GetDocument([In] IntPtr url, [In] Guid language, [In] Guid languageVendor, [In] Guid documentType);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        public virtual extern void __GetDocuments([In] uint cDocs, out uint pcDocs, [Out, MarshalAs(UnmanagedType.LPArray)] ISymUnmanagedDocument[] pDocs);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        public virtual extern void __GetDocumentVersion([In, MarshalAs(UnmanagedType.Interface)] ISymUnmanagedDocument pDoc, out int version, out int pbCurrent);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        public virtual extern void __GetGlobalVariables([In] uint cVars, out uint pcVars, [Out] IntPtr pVars);

        [return: MarshalAs(UnmanagedType.Interface)]
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        public virtual extern ISymUnmanagedMethod __GetMethod([In] uint token);

        [return: MarshalAs(UnmanagedType.Interface)]
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        public virtual extern ISymUnmanagedMethod __GetMethodByVersion([In] uint token, [In] int version);

        [return: MarshalAs(UnmanagedType.Interface)]
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        public virtual extern ISymUnmanagedMethod __GetMethodFromDocumentPosition([In, MarshalAs(UnmanagedType.Interface)] ISymUnmanagedDocument document, [In] uint line, [In] uint column);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        public virtual extern void __GetMethodsFromDocumentPosition([In, MarshalAs(UnmanagedType.Interface)] ISymUnmanagedDocument document, [In] uint line, [In] uint column, [In] uint cMethod, out uint pcMethod, [Out] IntPtr pRetVal);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        public virtual extern void __GetMethodVersion([In, MarshalAs(UnmanagedType.Interface)] ISymUnmanagedMethod pMethod, out int version);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        public virtual extern void __GetNamespaces([In] uint cNameSpaces, out uint pcNameSpaces, [Out, MarshalAs(UnmanagedType.LPArray)] ISymUnmanagedNamespace[] namespaces);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        public virtual extern void __GetSymAttribute([In] uint parent, [In] IntPtr name, [In] uint cBuffer, out uint pcBuffer, [Out] IntPtr buffer);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        public virtual extern void __GetSymbolStoreFileName([In] uint cchName, out uint pcchName, [Out] IntPtr szName);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        public virtual extern uint __GetUserEntryPoint();

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        public virtual extern void __GetVariables([In] uint parent, [In] uint cVars, out uint pcVars, [Out] IntPtr pVars);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        public virtual extern void __Initialize([In, MarshalAs(UnmanagedType.IUnknown)] object importer, [In] IntPtr filename, [In] IntPtr searchPath, [In, MarshalAs(UnmanagedType.Interface)] IStream pIStream);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        public virtual extern void __ReplaceSymbolStore([In] IntPtr filename, [In, MarshalAs(UnmanagedType.Interface)] IStream pIStream);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        public virtual extern void __UpdateSymbolStore([In] IntPtr filename, [In, MarshalAs(UnmanagedType.Interface)] IStream pIStream);

    }

    public enum CorSymSearchPolicyAttributes
    {
        // Fields
        AllowOriginalPathAccess = 4,
        AllowReferencePathAccess = 8,
        AllowRegistryAccess = 1,
        AllowSymbolServerAccess = 2
    }

    public enum CorSymVarFlag
    {
        // Fields
        VAR_IS_COMP_GEN = 1
    }

    [ComImport, CoClass(typeof(CorSymWriter_SxSClass)), Guid("ED14AA72-78E2-4884-84E2-334293AE5214")]
    public interface CorSymWriter_SxS : ISymUnmanagedWriter
    {
    }

    [ComImport, TypeLibType((short) 2), Guid("0AE2DEB0-F901-478B-BB9F-881EE8066788"), ClassInterface((short) 0), ComConversionLoss]
    public class CorSymWriter_SxSClass : ISymUnmanagedWriter, CorSymWriter_SxS
    {
        // Methods
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        public virtual extern void __Abort();

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        public virtual extern void __Close();

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        public virtual extern void __CloseMethod();

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        public virtual extern void __CloseNamespace();

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        public virtual extern void __CloseScope([In] uint endOffset);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        public virtual extern void __DefineConstant([In] IntPtr name, [In, MarshalAs(UnmanagedType.Struct)] object value, [In] uint cSig, [In] ref byte signature);

        [return: MarshalAs(UnmanagedType.Interface)]
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        public virtual extern ISymUnmanagedDocumentWriter __DefineDocument([In] IntPtr url, [In] ref Guid language, [In] ref Guid languageVendor, [In] ref Guid documentType);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        public virtual extern void __DefineField([In] uint parent, [In] IntPtr name, [In] uint attributes, [In] uint cSig, [In] ref byte signature, [In] uint addrKind, [In] uint addr1, [In] uint addr2, [In] uint addr3);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        public virtual extern void __DefineGlobalVariable([In] IntPtr name, [In] uint attributes, [In] uint cSig, [In] ref byte signature, [In] uint addrKind, [In] uint addr1, [In] uint addr2, [In] uint addr3);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        public virtual extern void __DefineLocalVariable([In] IntPtr name, [In] uint attributes, [In] uint cSig, [In] ref byte signature, [In] uint addrKind, [In] uint addr1, [In] uint addr2, [In] uint addr3, [In] uint startOffset, [In] uint endOffset);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        public virtual extern void __DefineParameter([In] IntPtr name, [In] uint attributes, [In] uint sequence, [In] uint addrKind, [In] uint addr1, [In] uint addr2, [In] uint addr3);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        public virtual extern void __DefineSequencePoints([In, MarshalAs(UnmanagedType.Interface)] ISymUnmanagedDocumentWriter document, [In] uint spCount, [In] ref uint offsets, [In] ref uint lines, [In] ref uint columns, [In] ref uint endLines, [In] ref uint endColumns);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        public virtual extern void __GetDebugInfo([In] ref uint pIDD, [In] uint cData, out uint pcData, [Out] IntPtr data);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        public virtual extern void __Initialize([In, MarshalAs(UnmanagedType.IUnknown)] object emitter, [In] IntPtr filename, [In, MarshalAs(UnmanagedType.Interface)] IStream pIStream, [In] int fFullBuild);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        public virtual extern void __Initialize2([In, MarshalAs(UnmanagedType.IUnknown)] object emitter, [In] IntPtr tempfilename, [In, MarshalAs(UnmanagedType.Interface)] IStream pIStream, [In] int fFullBuild, [In] IntPtr finalfilename);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        public virtual extern void __OpenMethod([In] uint method);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        public virtual extern void __OpenNamespace([In] IntPtr name);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        public virtual extern uint __OpenScope([In] uint startOffset);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        public virtual extern void __RemapToken([In] uint oldToken, [In] uint newToken);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        public virtual extern void __SetMethodSourceRange([In, MarshalAs(UnmanagedType.Interface)] ISymUnmanagedDocumentWriter startDoc, [In] uint startLine, [In] uint startColumn, [In, MarshalAs(UnmanagedType.Interface)] ISymUnmanagedDocumentWriter endDoc, [In] uint endLine, [In] uint endColumn);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        public virtual extern void __SetScopeRange([In] uint scopeID, [In] uint startOffset, [In] uint endOffset);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        public virtual extern void __SetSymAttribute([In] uint parent, [In] IntPtr name, [In] uint cData, [In] ref byte data);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        public virtual extern void __SetUserEntryPoint([In] uint entryMethod);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        public virtual extern void __UsingNamespace([In] IntPtr fullName);

    }

    [ComImport, InterfaceType((short) 1), Guid("AA544D42-28CB-11D3-BD22-0000F80849BD")]
    public interface ISymUnmanagedBinder
    {
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime), PreserveSig]
        int __GetReaderForFile([In, MarshalAs(UnmanagedType.IUnknown)] object importer, [In] IntPtr filename, [In] IntPtr searchPath, [Out, In, MarshalAs(UnmanagedType.IUnknown)] ref object retVal);
        [return: MarshalAs(UnmanagedType.Interface)]
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        ISymUnmanagedReader __GetReaderFromStream([In, MarshalAs(UnmanagedType.IUnknown)] object importer, [In, MarshalAs(UnmanagedType.Interface)] IStream pstream);
    }

    [ComImport, InterfaceType((short) 1), Guid("969708D2-05E5-4861-A3B0-96E473CDF63F")]
    public interface ISymUnmanagedDispose
    {
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void __Destroy();
    }

    [ComImport, Guid("40DE4037-7C81-3E1E-B022-AE1ABFF2CA08"), ComConversionLoss, InterfaceType((short) 1)]
    public interface ISymUnmanagedDocument
    {
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void __GetURL([In] uint cchUrl, out uint pcchUrl, [Out] IntPtr szUrl);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        Guid __GetDocumentType();
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        Guid __GetLanguage();
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        Guid __GetLanguageVendor();
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        Guid __GetCheckSumAlgorithmId();
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void __GetCheckSum([In] uint cData, out uint pcData, [Out] IntPtr data);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        uint __FindClosestLine([In] uint line);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        int __HasEmbeddedSource();
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        uint __GetSourceLength();
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void __GetSourceRange([In] uint startLine, [In] uint startColumn, [In] uint endLine, [In] uint endColumn, [In] uint cSourceBytes, out uint pcSourceBytes, [Out] IntPtr source);
    }

    [ComImport, Guid("B01FAFEB-C450-3A4D-BEEC-B4CEEC01E006"), InterfaceType((short) 1)]
    public interface ISymUnmanagedDocumentWriter
    {
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void __SetSource([In] uint sourceSize, [In] ref byte source);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void __SetCheckSum([In] Guid algorithmId, [In] uint checkSumSize, [In] ref byte checkSum);
    }

    [ComImport, InterfaceType((short) 1), ComConversionLoss, Guid("B62B923C-B500-3158-A543-24F307A8B7E1")]
    public interface ISymUnmanagedMethod
    {
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        uint __GetToken();
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        uint __GetSequencePointCount();
        [return: MarshalAs(UnmanagedType.Interface)]
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        ISymUnmanagedScope __GetRootScope();
        [return: MarshalAs(UnmanagedType.Interface)]
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        ISymUnmanagedScope __GetScopeFromOffset([In] uint offset);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        uint __GetOffset([In, MarshalAs(UnmanagedType.Interface)] ISymUnmanagedDocument document, [In] uint line, [In] uint column);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void __GetRanges([In, MarshalAs(UnmanagedType.Interface)] ISymUnmanagedDocument document, [In] uint line, [In] uint column, [In] uint cRanges, out uint pcRanges, [Out] IntPtr ranges);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void __GetParameters([In] uint cParams, out uint pcParams, [Out] IntPtr @params);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void __GetNamespace([MarshalAs(UnmanagedType.Interface)] out ISymUnmanagedNamespace pRetVal);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void __GetSourceStartEnd([In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex=0, SizeConst=2)] ISymUnmanagedDocument[] docs, [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex=0, SizeConst=2)] uint[] lines, [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex=0, SizeConst=2)] uint[] columns, out int pRetVal);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void __GetSequencePoints([In] uint cPoints, out uint pcPoints, [Out, MarshalAs(UnmanagedType.LPArray)] uint[] offsets, [Out, MarshalAs(UnmanagedType.LPArray)] ISymUnmanagedDocument[] documents, [Out, MarshalAs(UnmanagedType.LPArray)] uint[] lines, [Out, MarshalAs(UnmanagedType.LPArray)] uint[] columns, [Out, MarshalAs(UnmanagedType.LPArray)] uint[] endLines, [Out, MarshalAs(UnmanagedType.LPArray)] uint[] endColumns);
    }

    [ComImport, InterfaceType((short) 1), ComConversionLoss, Guid("0DFF7289-54F8-11D3-BD28-0000F80849BD")]
    public interface ISymUnmanagedNamespace
    {
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void __GetName([In] uint cchName, out uint pcchName, [Out] IntPtr szName);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void __GetNamespaces([In] uint cNameSpaces, out uint pcNameSpaces, [Out] IntPtr namespaces);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void __GetVariables([In] uint cVars, out uint pcVars, [Out] IntPtr pVars);
    }

    [ComImport, InterfaceType((short) 1), ComConversionLoss, Guid("B4CE6286-2A6B-3712-A3B7-1EE1DAD467B5")]
    public interface ISymUnmanagedReader
    {
        [return: MarshalAs(UnmanagedType.Interface)]
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        ISymUnmanagedDocument __GetDocument([In] IntPtr url, [In] Guid language, [In] Guid languageVendor, [In] Guid documentType);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void __GetDocuments([In] uint cDocs, out uint pcDocs, [Out, MarshalAs(UnmanagedType.LPArray)] ISymUnmanagedDocument[] pDocs);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        uint __GetUserEntryPoint();
        [return: MarshalAs(UnmanagedType.Interface)]
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        ISymUnmanagedMethod __GetMethod([In] uint token);
        [return: MarshalAs(UnmanagedType.Interface)]
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        ISymUnmanagedMethod __GetMethodByVersion([In] uint token, [In] int version);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void __GetVariables([In] uint parent, [In] uint cVars, out uint pcVars, [Out] IntPtr pVars);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void __GetGlobalVariables([In] uint cVars, out uint pcVars, [Out] IntPtr pVars);
        [return: MarshalAs(UnmanagedType.Interface)]
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        ISymUnmanagedMethod __GetMethodFromDocumentPosition([In, MarshalAs(UnmanagedType.Interface)] ISymUnmanagedDocument document, [In] uint line, [In] uint column);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void __GetSymAttribute([In] uint parent, [In] IntPtr name, [In] uint cBuffer, out uint pcBuffer, [Out] IntPtr buffer);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void __GetNamespaces([In] uint cNameSpaces, out uint pcNameSpaces, [Out, MarshalAs(UnmanagedType.LPArray)] ISymUnmanagedNamespace[] namespaces);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void __Initialize([In, MarshalAs(UnmanagedType.IUnknown)] object importer, [In] IntPtr filename, [In] IntPtr searchPath, [In, MarshalAs(UnmanagedType.Interface)] IStream pIStream);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void __UpdateSymbolStore([In] IntPtr filename, [In, MarshalAs(UnmanagedType.Interface)] IStream pIStream);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void __ReplaceSymbolStore([In] IntPtr filename, [In, MarshalAs(UnmanagedType.Interface)] IStream pIStream);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void __GetSymbolStoreFileName([In] uint cchName, out uint pcchName, [Out] IntPtr szName);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void __GetMethodsFromDocumentPosition([In, MarshalAs(UnmanagedType.Interface)] ISymUnmanagedDocument document, [In] uint line, [In] uint column, [In] uint cMethod, out uint pcMethod, [Out] IntPtr pRetVal);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void __GetDocumentVersion([In, MarshalAs(UnmanagedType.Interface)] ISymUnmanagedDocument pDoc, out int version, out int pbCurrent);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void __GetMethodVersion([In, MarshalAs(UnmanagedType.Interface)] ISymUnmanagedMethod pMethod, out int version);
    }

    [ComImport, Guid("20D9645D-03CD-4E34-9C11-9848A5B084F1"), InterfaceType((short) 1)]
    public interface ISymUnmanagedReaderSymbolSearchInfo
    {
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void __GetSymbolSearchInfoCount(out uint pcSearchInfo);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void __GetSymbolSearchInfo([In] uint cSearchInfo, out uint pcSearchInfo, [MarshalAs(UnmanagedType.Interface)] out ISymUnmanagedSymbolSearchInfo rgpSearchInfo);
    }

    [ComImport, Guid("68005D0F-B8E0-3B01-84D5-A11A94154942"), ComConversionLoss, InterfaceType((short) 1)]
    public interface ISymUnmanagedScope
    {
        [return: MarshalAs(UnmanagedType.Interface)]
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        ISymUnmanagedMethod __GetMethod();
        [return: MarshalAs(UnmanagedType.Interface)]
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        ISymUnmanagedScope __GetParent();
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void __GetChildren([In] uint cChildren, out uint pcChildren, [Out, MarshalAs(UnmanagedType.LPArray)] ISymUnmanagedScope[] children);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        uint __GetStartOffset();
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        uint __GetEndOffset();
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        uint __GetLocalCount();
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void __GetLocals([In] uint cLocals, out uint pcLocals, [Out, MarshalAs(UnmanagedType.LPArray)] ISymUnmanagedVariable[] locals);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void __GetNamespaces([In] uint cNameSpaces, out uint pcNameSpaces, [Out, MarshalAs(UnmanagedType.LPArray)] ISymUnmanagedNamespace[] namespaces);
    }

    [ComImport, ComConversionLoss, InterfaceType((short) 1), Guid("F8B3534A-A46B-4980-B520-BEC4ACEABA8F")]
    public interface ISymUnmanagedSymbolSearchInfo
    {
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void __GetSearchPathLength(out uint pcchPath);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void __GetSearchPath([In] uint cchPath, out uint pcchPath, [Out] IntPtr szPath);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void __GetHRESULT([MarshalAs(UnmanagedType.Error)] out int phr);
    }

    [ComImport, Guid("9F60EEBE-2D9A-3F7C-BF58-80BC991C60BB"), InterfaceType((short) 1), ComConversionLoss]
    public interface ISymUnmanagedVariable
    {
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void __GetName([In] uint cchName, out uint pcchName, [In] IntPtr szName);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        uint __GetAttributes();
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void __GetSignature([In] uint cSig, out uint pcSig, [In] IntPtr sig);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        uint __GetAddressKind();
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        uint __GetAddressField1();
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        uint __GetAddressField2();
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        uint __GetAddressField3();
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        uint __GetStartOffset();
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        uint __GetEndOffset();
    }

    [ComImport, ComConversionLoss, InterfaceType((short) 1), Guid("ED14AA72-78E2-4884-84E2-334293AE5214")]
    public interface ISymUnmanagedWriter
    {
        [return: MarshalAs(UnmanagedType.Interface)]
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        ISymUnmanagedDocumentWriter __DefineDocument([In] IntPtr url, [In] ref Guid language, [In] ref Guid languageVendor, [In] ref Guid documentType);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void __SetUserEntryPoint([In] uint entryMethod);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void __OpenMethod([In] uint method);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void __CloseMethod();
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        uint __OpenScope([In] uint startOffset);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void __CloseScope([In] uint endOffset);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void __SetScopeRange([In] uint scopeID, [In] uint startOffset, [In] uint endOffset);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void __DefineLocalVariable([In] IntPtr name, [In] uint attributes, [In] uint cSig, [In] ref byte signature, [In] uint addrKind, [In] uint addr1, [In] uint addr2, [In] uint addr3, [In] uint startOffset, [In] uint endOffset);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void __DefineParameter([In] IntPtr name, [In] uint attributes, [In] uint sequence, [In] uint addrKind, [In] uint addr1, [In] uint addr2, [In] uint addr3);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void __DefineField([In] uint parent, [In] IntPtr name, [In] uint attributes, [In] uint cSig, [In] ref byte signature, [In] uint addrKind, [In] uint addr1, [In] uint addr2, [In] uint addr3);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void __DefineGlobalVariable([In] IntPtr name, [In] uint attributes, [In] uint cSig, [In] ref byte signature, [In] uint addrKind, [In] uint addr1, [In] uint addr2, [In] uint addr3);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void __Close();
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void __SetSymAttribute([In] uint parent, [In] IntPtr name, [In] uint cData, [In] ref byte data);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void __OpenNamespace([In] IntPtr name);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void __CloseNamespace();
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void __UsingNamespace([In] IntPtr fullName);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void __SetMethodSourceRange([In, MarshalAs(UnmanagedType.Interface)] ISymUnmanagedDocumentWriter startDoc, [In] uint startLine, [In] uint startColumn, [In, MarshalAs(UnmanagedType.Interface)] ISymUnmanagedDocumentWriter endDoc, [In] uint endLine, [In] uint endColumn);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void __Initialize([In, MarshalAs(UnmanagedType.IUnknown)] object emitter, [In] IntPtr filename, [In, MarshalAs(UnmanagedType.Interface)] IStream pIStream, [In] int fFullBuild);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void __GetDebugInfo([In] ref uint pIDD, [In] uint cData, out uint pcData, [Out] IntPtr data);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void __DefineSequencePoints([In, MarshalAs(UnmanagedType.Interface)] ISymUnmanagedDocumentWriter document, [In] uint spCount, [In] ref uint offsets, [In] ref uint lines, [In] ref uint columns, [In] ref uint endLines, [In] ref uint endColumns);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void __RemapToken([In] uint oldToken, [In] uint newToken);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void __Initialize2([In, MarshalAs(UnmanagedType.IUnknown)] object emitter, [In] IntPtr tempfilename, [In, MarshalAs(UnmanagedType.Interface)] IStream pIStream, [In] int fFullBuild, [In] IntPtr finalfilename);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void __DefineConstant([In] IntPtr name, [In, MarshalAs(UnmanagedType.Struct)] object value, [In] uint cSig, [In] ref byte signature);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void __Abort();
    }

    [ComImport, Guid("0B97726E-9E6D-4F05-9A26-424022093CAA"), InterfaceType((short) 1)]
    public interface ISymUnmanagedWriter2 : ISymUnmanagedWriter
    {
        [return: MarshalAs(UnmanagedType.Interface)]
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        ISymUnmanagedDocumentWriter __DefineDocument([In] IntPtr url, [In] ref Guid language, [In] ref Guid languageVendor, [In] ref Guid documentType);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void __SetUserEntryPoint([In] uint entryMethod);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void __OpenMethod([In] uint method);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void __CloseMethod();
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        uint __OpenScope([In] uint startOffset);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void __CloseScope([In] uint endOffset);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void __SetScopeRange([In] uint scopeID, [In] uint startOffset, [In] uint endOffset);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void __DefineLocalVariable([In] IntPtr name, [In] uint attributes, [In] uint cSig, [In] ref byte signature, [In] uint addrKind, [In] uint addr1, [In] uint addr2, [In] uint addr3, [In] uint startOffset, [In] uint endOffset);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void __DefineParameter([In] IntPtr name, [In] uint attributes, [In] uint sequence, [In] uint addrKind, [In] uint addr1, [In] uint addr2, [In] uint addr3);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void __DefineField([In] uint parent, [In] IntPtr name, [In] uint attributes, [In] uint cSig, [In] ref byte signature, [In] uint addrKind, [In] uint addr1, [In] uint addr2, [In] uint addr3);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void __DefineGlobalVariable([In] IntPtr name, [In] uint attributes, [In] uint cSig, [In] ref byte signature, [In] uint addrKind, [In] uint addr1, [In] uint addr2, [In] uint addr3);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void __Close();
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void __SetSymAttribute([In] uint parent, [In] IntPtr name, [In] uint cData, [In] ref byte data);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void __OpenNamespace([In] IntPtr name);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void __CloseNamespace();
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void __UsingNamespace([In] IntPtr fullName);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void __SetMethodSourceRange([In, MarshalAs(UnmanagedType.Interface)] ISymUnmanagedDocumentWriter startDoc, [In] uint startLine, [In] uint startColumn, [In, MarshalAs(UnmanagedType.Interface)] ISymUnmanagedDocumentWriter endDoc, [In] uint endLine, [In] uint endColumn);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void __Initialize([In, MarshalAs(UnmanagedType.IUnknown)] object emitter, [In] IntPtr filename, [In, MarshalAs(UnmanagedType.Interface)] IStream pIStream, [In] int fFullBuild);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void __GetDebugInfo([In] ref uint pIDD, [In] uint cData, out uint pcData, [Out] IntPtr data);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void __DefineSequencePoints([In, MarshalAs(UnmanagedType.Interface)] ISymUnmanagedDocumentWriter document, [In] uint spCount, [In] ref uint offsets, [In] ref uint lines, [In] ref uint columns, [In] ref uint endLines, [In] ref uint endColumns);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void __RemapToken([In] uint oldToken, [In] uint newToken);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void __Initialize2([In, MarshalAs(UnmanagedType.IUnknown)] object emitter, [In] IntPtr tempfilename, [In, MarshalAs(UnmanagedType.Interface)] IStream pIStream, [In] int fFullBuild, [In] IntPtr finalfilename);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void __DefineConstant([In] IntPtr name, [In, MarshalAs(UnmanagedType.Struct)] object value, [In] uint cSig, [In] ref byte signature);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void __Abort();
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void __DefineLocalVariable2([In] IntPtr name, [In] uint attributes, [In] uint sigToken, [In] uint addrKind, [In] uint addr1, [In] uint addr2, [In] uint addr3, [In] uint startOffset, [In] uint endOffset);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void __DefineGlobalVariable2([In] IntPtr name, [In] uint attributes, [In] uint sigToken, [In] uint addrKind, [In] uint addr1, [In] uint addr2, [In] uint addr3);
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType=MethodCodeType.Runtime)]
        void __DefineConstant2([In] IntPtr name, [In, MarshalAs(UnmanagedType.Struct)] object value, [In] uint sigToken);
    }
}

#pragma warning restore 108, 1591
