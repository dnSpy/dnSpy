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

// Code generated from: C:\Program Files (x86)\Windows Kits\NETFXSDK\4.6\Include\um\cordebug.idl
// File date: 2015-06-19
// SHA-1: f8592546655cc6bf15c24f661bf397f962e4de5e

using System;
using System.Runtime.InteropServices;
using System.Text;

#pragma warning disable 0108 // Member hides inherited member; missing new keyword
namespace dndbg.Engine.COM.CorDebug {
	public enum CorDebugJITCompilerFlags : uint {
		CORDEBUG_JIT_DEFAULT = 0x1,
		CORDEBUG_JIT_DISABLE_OPTIMIZATION = 0x3,
		CORDEBUG_JIT_ENABLE_ENC = 0x7
	}
	public struct PROCESS_INFORMATION {
		public IntPtr hProcess;
		public IntPtr hThread;
		public uint dwProcessId;
		public uint dwThreadId;
	}
	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
	public struct STARTUPINFO {
		public uint cb;
		public string lpReserved;
		public string lpDesktop;
		public string lpTitle;
		public uint dwX;
		public uint dwY;
		public uint dwXSize;
		public uint dwYSize;
		public uint dwXCountChars;
		public uint dwYCountChars;
		public uint dwFillAttribute;
		public uint dwFlags;
		public ushort wShowWindow;
		public ushort cbReserved2;
		public IntPtr lpReserved2;
		public IntPtr hStdInput;
		public IntPtr hStdOutput;
		public IntPtr hStdError;
	}
	public struct _COR_HEAPINFO {
		public int areGCStructuresValid;
		public uint pointerSize;
		public uint numHeaps;
		public int concurrent;
		public CorDebugGCType gcType;
	}
	public struct _COR_IL_MAP {
		public uint oldOffset;
		public uint newOffset;
		public int fAccurate;
	}
	public struct _COR_VERSION {
		public uint dwMajor;
		public uint dwMinor;
		public uint dwBuild;
		public uint dwSubBuild;
	}
	public struct _FILETIME {
		public uint dwLowDateTime;
		public uint dwHighDateTime;
	}
	public struct _LARGE_INTEGER {
		public long QuadPart;
	}
	public struct _SECURITY_ATTRIBUTES {
		public uint nLength;
		public IntPtr lpSecurityDescriptor;
		public int bInheritHandle;
	}
	public struct _ULARGE_INTEGER {
		public ulong QuadPart;
	}
	public struct COR_ARRAY_LAYOUT {
		public COR_TYPEID componentID;
		public uint componentType;
		public uint firstElementOffset;
		public uint elementSize;
		public uint countOffset;
		public uint rankSize;
		public uint numRanks;
		public uint rankOffset;
	}
	public struct COR_FIELD {
		public uint token;
		public uint offset;
		public COR_TYPEID id;
		public uint fieldType;
	}
	public struct COR_TYPE_LAYOUT {
		public COR_TYPEID parentID;
		public uint objectSize;
		public uint numFields;
		public uint boxOffset;
		public uint type;
	}
	public struct COR_TYPEID {
		public ulong token1;
		public ulong token2;
	}
	[CoClass(typeof(CorDebugClass)), Guid("3D6F5F61-7538-11D3-8D5B-00104B35E7EF")]
	[ComImport]
	public interface CorDebug : ICorDebug {
	}
	public enum CorDebugChainReason {
		CHAIN_NONE,
		CHAIN_CLASS_INIT,
		CHAIN_EXCEPTION_FILTER,
		CHAIN_SECURITY = 4,
		CHAIN_CONTEXT_POLICY = 8,
		CHAIN_INTERCEPTION = 16,
		CHAIN_PROCESS_START = 32,
		CHAIN_THREAD_START = 64,
		CHAIN_ENTER_MANAGED = 128,
		CHAIN_ENTER_UNMANAGED = 256,
		CHAIN_DEBUGGER_EVAL = 512,
		CHAIN_CONTEXT_SWITCH = 1024,
		CHAIN_FUNC_EVAL = 2048
	}
	[ClassInterface(ClassInterfaceType.None), Guid("6FEF44D0-39E7-4C77-BE8E-C9F8CF988630"), TypeLibType(TypeLibTypeFlags.FCanCreate)]
	[ComImport]
	public class CorDebugClass : ICorDebug, CorDebug {
		public virtual extern void Initialize();
		public virtual extern int Terminate();
		public virtual extern void SetManagedHandler([MarshalAs(UnmanagedType.Interface)] [In] ICorDebugManagedCallback pCallback);
		public virtual extern void SetUnmanagedHandler([MarshalAs(UnmanagedType.Interface)] [In] ICorDebugUnmanagedCallback pCallback);
		public virtual extern void CreateProcess([MarshalAs(UnmanagedType.LPWStr)] [In] string lpApplicationName, [MarshalAs(UnmanagedType.LPWStr)] [In] string lpCommandLine, IntPtr lpProcessAttributes, IntPtr lpThreadAttributes, [In] int bInheritHandles, [In] ProcessCreationFlags dwCreationFlags, [In] IntPtr lpEnvironment, [MarshalAs(UnmanagedType.LPWStr)] [In] string lpCurrentDirectory, [In] ref STARTUPINFO lpStartupInfo, [In] ref PROCESS_INFORMATION lpProcessInformation, [In] CorDebugCreateProcessFlags debuggingFlags, [MarshalAs(UnmanagedType.Interface)] out ICorDebugProcess ppProcess);
		public virtual extern void DebugActiveProcess([In] uint id, [In] int win32Attach, [MarshalAs(UnmanagedType.Interface)] out ICorDebugProcess ppProcess);
		public virtual extern void EnumerateProcesses([MarshalAs(UnmanagedType.Interface)] out ICorDebugProcessEnum ppProcess);
		public virtual extern void GetProcess([In] uint dwProcessId, [MarshalAs(UnmanagedType.Interface)] out ICorDebugProcess ppProcess);
		public virtual extern void CanLaunchOrAttach([In] uint dwProcessId, [In] int win32DebuggingEnabled);
	}
	public enum CorDebugCodeInvokeKind {
		CODE_INVOKE_KIND_NONE,
		CODE_INVOKE_KIND_RETURN,
		CODE_INVOKE_KIND_TAILCALL
	}
	public enum CorDebugCodeInvokePurpose {
		CODE_INVOKE_PURPOSE_NONE,
		CODE_INVOKE_PURPOSE_NATIVE_TO_MANAGED_TRANSITION,
		CODE_INVOKE_PURPOSE_CLASS_INIT,
		CODE_INVOKE_PURPOSE_INTERFACE_DISPATCH
	}
	public enum CorDebugCreateProcessFlags {
		DEBUG_NO_SPECIAL_OPTIONS
	}
	public enum CorDebugDebugEventKind {
		DEBUG_EVENT_KIND_MODULE_LOADED = 1,
		DEBUG_EVENT_KIND_MODULE_UNLOADED,
		DEBUG_EVENT_KIND_MANAGED_EXCEPTION_FIRST_CHANCE,
		DEBUG_EVENT_KIND_MANAGED_EXCEPTION_USER_FIRST_CHANCE,
		DEBUG_EVENT_KIND_MANAGED_EXCEPTION_CATCH_HANDLER_FOUND,
		DEBUG_EVENT_KIND_MANAGED_EXCEPTION_UNHANDLED
	}
	public enum CorDebugExceptionCallbackType {
		/// <summary>
		/// An exception was thrown.
		/// </summary>
		DEBUG_EXCEPTION_FIRST_CHANCE = 1,
		/// <summary>
		/// The exception windup process entered user code.
		/// </summary>
		DEBUG_EXCEPTION_USER_FIRST_CHANCE,
		/// <summary>
		/// The exception windup process found a catch block in user code.
		/// </summary>
		DEBUG_EXCEPTION_CATCH_HANDLER_FOUND,
		/// <summary>
		/// The exception was not handled.
		/// </summary>
		DEBUG_EXCEPTION_UNHANDLED
	}
	public enum CorDebugExceptionFlags : uint {
		/// <summary>
		/// There is no exception.
		/// </summary>
		DEBUG_EXCEPTION_NONE,
		/// <summary>
		/// The exception is interceptable.
		/// 
		/// The timing of the exception may still be such that the debugger cannot intercept it.
		/// For example, if there is no managed code below the current callback or the exception
		/// event resulted from a just-in-time (JIT) attachment, the exception cannot be intercepted.
		/// </summary>
		DEBUG_EXCEPTION_CAN_BE_INTERCEPTED
	}
	public enum CorDebugExceptionUnwindCallbackType {
		/// <summary>
		/// The beginning of the unwind process.
		/// </summary>
		DEBUG_EXCEPTION_UNWIND_BEGIN = 1,
		/// <summary>
		/// The exception was intercepted.
		/// </summary>
		DEBUG_EXCEPTION_INTERCEPTED
	}
	public enum CorDebugGCType {
		CorDebugWorkstationGC,
		CorDebugServerGC
	}
	public enum CorDebugHandleType {
		HANDLE_STRONG = 1,
		HANDLE_WEAK_TRACK_RESURRECTION
	}
	public enum CorDebugIntercept {
		INTERCEPT_NONE,
		INTERCEPT_CLASS_INIT,
		INTERCEPT_EXCEPTION_FILTER,
		INTERCEPT_SECURITY = 4,
		INTERCEPT_CONTEXT_POLICY = 8,
		INTERCEPT_INTERCEPTION = 16,
		INTERCEPT_ALL = 65535
	}
	public enum CorDebugInternalFrameType {
		STUBFRAME_NONE,
		STUBFRAME_M2U,
		STUBFRAME_U2M,
		STUBFRAME_APPDOMAIN_TRANSITION,
		STUBFRAME_LIGHTWEIGHT_FUNCTION,
		STUBFRAME_FUNC_EVAL,
		STUBFRAME_INTERNALCALL,
		STUBFRAME_CLASS_INIT,
		STUBFRAME_EXCEPTION,
		STUBFRAME_SECURITY,
		STUBFRAME_JIT_COMPILATION
	}
	public enum CorDebugMappingResult {
		MAPPING_PROLOG = 1,
		MAPPING_EPILOG,
		MAPPING_NO_INFO = 4,
		MAPPING_UNMAPPED_ADDRESS = 8,
		MAPPING_EXACT = 16,
		MAPPING_APPROXIMATE = 32
	}
	public enum CorDebugMDAFlags {
		MDA_FLAG_SLIP = 2
	}
	public enum CorDebugNGENPolicy {
		DISABLE_LOCAL_NIC = 1
	}
	public enum CorDebugPlatform {
		CORDB_PLATFORM_WINDOWS_X86,
		CORDB_PLATFORM_WINDOWS_AMD64,
		CORDB_PLATFORM_WINDOWS_IA64,
		CORDB_PLATFORM_MAC_PPC,
		CORDB_PLATFORM_MAC_X86,
		CORDB_PLATFORM_WINDOWS_ARM,
		CORDB_PLATFORM_MAC_AMD64,
		CORDB_PLATFORM_WINDOWS_ARM64
	}
	public enum CorDebugRecordFormat {
		FORMAT_WINDOWS_EXCEPTIONRECORD32 = 1,
		FORMAT_WINDOWS_EXCEPTIONRECORD64
	}
	public enum CorDebugRegister {
		REGISTER_INSTRUCTION_POINTER,
		REGISTER_STACK_POINTER,
		REGISTER_FRAME_POINTER,
		REGISTER_X86_EIP = 0,
		REGISTER_X86_ESP,
		REGISTER_X86_EBP,
		REGISTER_X86_EAX,
		REGISTER_X86_ECX,
		REGISTER_X86_EDX,
		REGISTER_X86_EBX,
		REGISTER_X86_ESI,
		REGISTER_X86_EDI,
		REGISTER_X86_FPSTACK_0,
		REGISTER_X86_FPSTACK_1,
		REGISTER_X86_FPSTACK_2,
		REGISTER_X86_FPSTACK_3,
		REGISTER_X86_FPSTACK_4,
		REGISTER_X86_FPSTACK_5,
		REGISTER_X86_FPSTACK_6,
		REGISTER_X86_FPSTACK_7,
		REGISTER_AMD64_RIP = 0,
		REGISTER_AMD64_RSP,
		REGISTER_AMD64_RBP,
		REGISTER_AMD64_RAX,
		REGISTER_AMD64_RCX,
		REGISTER_AMD64_RDX,
		REGISTER_AMD64_RBX,
		REGISTER_AMD64_RSI,
		REGISTER_AMD64_RDI,
		REGISTER_AMD64_R8,
		REGISTER_AMD64_R9,
		REGISTER_AMD64_R10,
		REGISTER_AMD64_R11,
		REGISTER_AMD64_R12,
		REGISTER_AMD64_R13,
		REGISTER_AMD64_R14,
		REGISTER_AMD64_R15,
		REGISTER_AMD64_XMM0,
		REGISTER_AMD64_XMM1,
		REGISTER_AMD64_XMM2,
		REGISTER_AMD64_XMM3,
		REGISTER_AMD64_XMM4,
		REGISTER_AMD64_XMM5,
		REGISTER_AMD64_XMM6,
		REGISTER_AMD64_XMM7,
		REGISTER_AMD64_XMM8,
		REGISTER_AMD64_XMM9,
		REGISTER_AMD64_XMM10,
		REGISTER_AMD64_XMM11,
		REGISTER_AMD64_XMM12,
		REGISTER_AMD64_XMM13,
		REGISTER_AMD64_XMM14,
		REGISTER_AMD64_XMM15,
		REGISTER_IA64_BSP = 2,
		REGISTER_IA64_R0,
		REGISTER_IA64_F0 = 131,
		REGISTER_ARM_PC = 0,
		REGISTER_ARM_SP,
		REGISTER_ARM_R0,
		REGISTER_ARM_R1,
		REGISTER_ARM_R2,
		REGISTER_ARM_R3,
		REGISTER_ARM_R4,
		REGISTER_ARM_R5,
		REGISTER_ARM_R6,
		REGISTER_ARM_R7,
		REGISTER_ARM_R8,
		REGISTER_ARM_R9,
		REGISTER_ARM_R10,
		REGISTER_ARM_R11,
		REGISTER_ARM_R12,
		REGISTER_ARM_LR,
		REGISTER_ARM64_PC = 0,
		REGISTER_ARM64_SP,
		REGISTER_ARM64_FP,
		REGISTER_ARM64_X0,
		REGISTER_ARM64_X1,
		REGISTER_ARM64_X2,
		REGISTER_ARM64_X3,
		REGISTER_ARM64_X4,
		REGISTER_ARM64_X5,
		REGISTER_ARM64_X6,
		REGISTER_ARM64_X7,
		REGISTER_ARM64_X8,
		REGISTER_ARM64_X9,
		REGISTER_ARM64_X10,
		REGISTER_ARM64_X11,
		REGISTER_ARM64_X12,
		REGISTER_ARM64_X13,
		REGISTER_ARM64_X14,
		REGISTER_ARM64_X15,
		REGISTER_ARM64_X16,
		REGISTER_ARM64_X17,
		REGISTER_ARM64_X18,
		REGISTER_ARM64_X19,
		REGISTER_ARM64_X20,
		REGISTER_ARM64_X21,
		REGISTER_ARM64_X22,
		REGISTER_ARM64_X23,
		REGISTER_ARM64_X24,
		REGISTER_ARM64_X25,
		REGISTER_ARM64_X26,
		REGISTER_ARM64_X27,
		REGISTER_ARM64_X28,
		REGISTER_ARM64_LR
	}
	public enum CorDebugSetContextFlag {
		SET_CONTEXT_FLAG_ACTIVE_FRAME = 1,
		SET_CONTEXT_FLAG_UNWIND_FRAME
	}
	public enum CorDebugStateChange {
		PROCESS_RUNNING = 1,
		FLUSH_ALL
	}
	public enum CorDebugStepReason {
		STEP_NORMAL,
		STEP_RETURN,
		STEP_CALL,
		STEP_EXCEPTION_FILTER,
		STEP_EXCEPTION_HANDLER,
		STEP_INTERCEPT,
		STEP_EXIT
	}
	public enum CorDebugThreadState {
		THREAD_RUN,
		THREAD_SUSPEND
	}
	public enum CorDebugUnmappedStop {
		STOP_NONE,
		STOP_PROLOG,
		STOP_EPILOG,
		STOP_NO_MAPPING_INFO = 4,
		STOP_OTHER_UNMAPPED = 8,
		STOP_UNMANAGED = 16,
		STOP_ALL = 65535
	}
	public enum CorDebugUserState {
		USER_STOP_REQUESTED = 1,
		USER_SUSPEND_REQUESTED,
		USER_BACKGROUND = 4,
		USER_UNSTARTED = 8,
		USER_STOPPED = 16,
		USER_WAIT_SLEEP_JOIN = 32,
		USER_SUSPENDED = 64,
		USER_UNSAFE_POINT = 128,
		USER_THREADPOOL = 256
	}
	public enum CorGCReferenceType {
		CorHandleStrong = 1,
		CorHandleStrongPinning,
		CorHandleWeakShort = 4,
		CorHandleWeakLong = 8,
		CorHandleWeakRefCount = 16,
		CorHandleStrongRefCount = 32,
		CorHandleStrongDependent = 64,
		CorHandleStrongAsyncPinned = 128,
		CorHandleStrongSizedByref = 256,
		CorHandleWeakWinRT = 512,
		CorReferenceStack = -2147483647,
		CorReferenceFinalizer = 80000002,
		CorHandleStrongOnly = 483,
		CorHandleWeakOnly = 540,
		CorHandleAll = 2147483647
	}
	[CoClass(typeof(EmbeddedCLRCorDebugClass)), Guid("3D6F5F61-7538-11D3-8D5B-00104B35E7EF")]
	[ComImport]
	public interface EmbeddedCLRCorDebug : ICorDebug {
	}
	[ClassInterface(ClassInterfaceType.None), Guid("211F1254-BC7E-4AF5-B9AA-067308D83DD1"), TypeLibType(TypeLibTypeFlags.FCanCreate)]
	[ComImport]
	public class EmbeddedCLRCorDebugClass : ICorDebug, EmbeddedCLRCorDebug {
		public virtual extern void Initialize();
		public virtual extern int Terminate();
		public virtual extern void SetManagedHandler([MarshalAs(UnmanagedType.Interface)] [In] ICorDebugManagedCallback pCallback);
		public virtual extern void SetUnmanagedHandler([MarshalAs(UnmanagedType.Interface)] [In] ICorDebugUnmanagedCallback pCallback);
		public virtual extern void CreateProcess([MarshalAs(UnmanagedType.LPWStr)] [In] string lpApplicationName, [MarshalAs(UnmanagedType.LPWStr)] [In] string lpCommandLine, IntPtr lpProcessAttributes, IntPtr lpThreadAttributes, [In] int bInheritHandles, [In] ProcessCreationFlags dwCreationFlags, [In] IntPtr lpEnvironment, [MarshalAs(UnmanagedType.LPWStr)] [In] string lpCurrentDirectory, [In] ref STARTUPINFO lpStartupInfo, [In] ref PROCESS_INFORMATION lpProcessInformation, [In] CorDebugCreateProcessFlags debuggingFlags, [MarshalAs(UnmanagedType.Interface)] out ICorDebugProcess ppProcess);
		public virtual extern void DebugActiveProcess([In] uint id, [In] int win32Attach, [MarshalAs(UnmanagedType.Interface)] out ICorDebugProcess ppProcess);
		public virtual extern void EnumerateProcesses([MarshalAs(UnmanagedType.Interface)] out ICorDebugProcessEnum ppProcess);
		public virtual extern void GetProcess([In] uint dwProcessId, [MarshalAs(UnmanagedType.Interface)] out ICorDebugProcess ppProcess);
		public virtual extern void CanLaunchOrAttach([In] uint dwProcessId, [In] int win32DebuggingEnabled);
	}
	[Guid("3D6F5F61-7538-11D3-8D5B-00104B35E7EF"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[ComImport]
	public interface ICorDebug {
		void Initialize();
		[PreserveSig]
		int Terminate();
		void SetManagedHandler([MarshalAs(UnmanagedType.Interface)] [In] ICorDebugManagedCallback pCallback);
		void SetUnmanagedHandler([MarshalAs(UnmanagedType.Interface)] [In] ICorDebugUnmanagedCallback pCallback);
		void CreateProcess([MarshalAs(UnmanagedType.LPWStr)] [In] string lpApplicationName, [MarshalAs(UnmanagedType.LPWStr)] [In] string lpCommandLine, IntPtr lpProcessAttributes, IntPtr lpThreadAttributes, [In] int bInheritHandles, [In] ProcessCreationFlags dwCreationFlags, [In] IntPtr lpEnvironment, [MarshalAs(UnmanagedType.LPWStr)] [In] string lpCurrentDirectory, [In] ref STARTUPINFO lpStartupInfo, [In] ref PROCESS_INFORMATION lpProcessInformation, [In] CorDebugCreateProcessFlags debuggingFlags, [MarshalAs(UnmanagedType.Interface)] out ICorDebugProcess ppProcess);
		void DebugActiveProcess([In] uint id, [In] int win32Attach, [MarshalAs(UnmanagedType.Interface)] out ICorDebugProcess ppProcess);
		void EnumerateProcesses([MarshalAs(UnmanagedType.Interface)] out ICorDebugProcessEnum ppProcess);
		void GetProcess([In] uint dwProcessId, [MarshalAs(UnmanagedType.Interface)] out ICorDebugProcess ppProcess);
		void CanLaunchOrAttach([In] uint dwProcessId, [In] int win32DebuggingEnabled);
	}
	[Guid("ECCCCF2E-B286-4B3E-A983-860A8793D105"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[ComImport]
	public interface ICorDebug2 {
	}
	[Guid("3D6F5F63-7538-11D3-8D5B-00104B35E7EF"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[ComImport]
	public interface ICorDebugAppDomain : ICorDebugController {
		void Stop([In] uint dwTimeoutIgnored);
		[PreserveSig]
		int Continue([In] int fIsOutOfBand);
		[PreserveSig]
		int IsRunning(out int pbRunning);
		void HasQueuedCallbacks([MarshalAs(UnmanagedType.Interface)] [In] ICorDebugThread pThread, out int pbQueued);
		void EnumerateThreads([MarshalAs(UnmanagedType.Interface)] out ICorDebugThreadEnum ppThreads);
		void SetAllThreadsDebugState([In] CorDebugThreadState state, [MarshalAs(UnmanagedType.Interface)] [In] ICorDebugThread pExceptThisThread);
		void Detach();
		[PreserveSig]
		int Terminate([In] uint exitCode);
		void CanCommitChanges([In] uint cSnapshots, [MarshalAs(UnmanagedType.Interface)] [In] ref ICorDebugEditAndContinueSnapshot pSnapshots, [MarshalAs(UnmanagedType.Interface)] out ICorDebugErrorInfoEnum pError);
		void CommitChanges([In] uint cSnapshots, [MarshalAs(UnmanagedType.Interface)] [In] ref ICorDebugEditAndContinueSnapshot pSnapshots, [MarshalAs(UnmanagedType.Interface)] out ICorDebugErrorInfoEnum pError);
		[PreserveSig]
		int GetProcess([MarshalAs(UnmanagedType.Interface)] out ICorDebugProcess ppProcess);
		void EnumerateAssemblies([MarshalAs(UnmanagedType.Interface)] out ICorDebugAssemblyEnum ppAssemblies);
		void GetModuleFromMetaDataInterface([MarshalAs(UnmanagedType.IUnknown)] [In] object pIMetaData, [MarshalAs(UnmanagedType.Interface)] out ICorDebugModule ppModule);
		void EnumerateBreakpoints([MarshalAs(UnmanagedType.Interface)] out ICorDebugBreakpointEnum ppBreakpoints);
		void EnumerateSteppers([MarshalAs(UnmanagedType.Interface)] out ICorDebugStepperEnum ppSteppers);
		void IsAttached(out int pbAttached);
		[PreserveSig]
		int GetName([In] uint cchName, out uint pcchName, [Out] [MarshalAs(UnmanagedType.LPWStr)] StringBuilder szName);
		void GetObject([MarshalAs(UnmanagedType.Interface)] out ICorDebugValue ppObject);
		[PreserveSig]
		int Attach();
		[PreserveSig]
		int GetID(out int pId);
	}
	[Guid("096E81D5-ECDA-4202-83F5-C65980A9EF75"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[ComImport]
	public interface ICorDebugAppDomain2 {
		void GetArrayOrPointerType([In] uint elementType, [In] uint nRank, [MarshalAs(UnmanagedType.Interface)] [In] ICorDebugType pTypeArg, [MarshalAs(UnmanagedType.Interface)] out ICorDebugType ppType);
		void GetFunctionPointerType([In] uint nTypeArgs, [MarshalAs(UnmanagedType.Interface)] [In] ref ICorDebugType ppTypeArgs, [MarshalAs(UnmanagedType.Interface)] out ICorDebugType ppType);
	}
	[Guid("8CB96A16-B588-42E2-B71C-DD849FC2ECCC"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[ComImport]
	public interface ICorDebugAppDomain3 {
		void GetCachedWinRTTypesForIIDs([In] uint cReqTypes, [In] ref Guid iidsToResolve, [MarshalAs(UnmanagedType.Interface)] out ICorDebugTypeEnum ppTypesEnum);
		void GetCachedWinRTTypes([MarshalAs(UnmanagedType.Interface)] out ICorDebugGuidToTypeEnum ppGuidToTypeEnum);
	}
	[Guid("FB99CC40-83BE-4724-AB3B-768E796EBAC2"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[ComImport]
	public interface ICorDebugAppDomain4 {
		void GetObjectForCCW([In] ulong ccwPointer, [MarshalAs(UnmanagedType.Interface)] out ICorDebugValue ppManagedObject);
	}
	[Guid("63CA1B24-4359-4883-BD57-13F815F58744"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[ComImport]
	public interface ICorDebugAppDomainEnum : ICorDebugEnum {
		void Skip([In] uint celt);
		void Reset();
		void Clone([MarshalAs(UnmanagedType.Interface)] out ICorDebugEnum ppEnum);
		void GetCount(out uint pcelt);
		void Next([In] uint celt, [MarshalAs(UnmanagedType.Interface)] [Out] ICorDebugAppDomainEnum values, out uint pceltFetched);
	}
	[Guid("0405B0DF-A660-11D2-BD02-0000F80849BD"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[ComImport]
	public interface ICorDebugArrayValue : ICorDebugHeapValue {
		void GetType(out uint pType);
		void GetSize(out uint pSize);
		void GetAddress(out ulong pAddress);
		void CreateBreakpoint([MarshalAs(UnmanagedType.Interface)] out ICorDebugValueBreakpoint ppBreakpoint);
		void IsValid(out int pbValid);
		void CreateRelocBreakpoint([MarshalAs(UnmanagedType.Interface)] out ICorDebugValueBreakpoint ppBreakpoint);
		void GetElementType(out uint pType);
		void GetRank(out uint pnRank);
		void GetCount(out uint pnCount);
		void GetDimensions([In] uint cdim, [MarshalAs(UnmanagedType.Interface)] [Out] ICorDebugArrayValue dims);
		void HasBaseIndicies(out int pbHasBaseIndicies);
		void GetBaseIndicies([In] uint cdim, [MarshalAs(UnmanagedType.Interface)] [Out] ICorDebugArrayValue indicies);
		void GetElement([In] uint cdim, [MarshalAs(UnmanagedType.Interface)] [In] ICorDebugArrayValue indices, [MarshalAs(UnmanagedType.Interface)] out ICorDebugValue ppValue);
		void GetElementAtPosition([In] uint nPosition, [MarshalAs(UnmanagedType.Interface)] out ICorDebugValue ppValue);
	}
	[Guid("DF59507C-D47A-459E-BCE2-6427EAC8FD06"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[ComImport]
	public interface ICorDebugAssembly {
		void GetProcess([MarshalAs(UnmanagedType.Interface)] out ICorDebugProcess ppProcess);
		void GetAppDomain([MarshalAs(UnmanagedType.Interface)] out ICorDebugAppDomain ppAppDomain);
		void EnumerateModules([MarshalAs(UnmanagedType.Interface)] out ICorDebugModuleEnum ppModules);
		void GetCodeBase([In] uint cchName, out uint pcchName, [MarshalAs(UnmanagedType.Interface)] [Out] ICorDebugAssembly szName);
		[PreserveSig]
		int GetName([In] uint cchName, out uint pcchName, [Out] [MarshalAs(UnmanagedType.LPWStr)] StringBuilder szName);
	}
	[Guid("426D1F9E-6DD4-44C8-AEC7-26CDBAF4E398"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[ComImport]
	public interface ICorDebugAssembly2 {
		void IsFullyTrusted(out int pbFullyTrusted);
	}
	[Guid("76361AB2-8C86-4FE9-96F2-F73D8843570A"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[ComImport]
	public interface ICorDebugAssembly3 {
		void GetContainerAssembly([MarshalAs(UnmanagedType.Interface)] ref ICorDebugAssembly ppAssembly);
		void EnumerateContainedAssemblies([MarshalAs(UnmanagedType.Interface)] ref ICorDebugAssemblyEnum ppAssemblies);
	}
	[Guid("4A2A1EC9-85EC-4BFB-9F15-A89FDFE0FE83"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[ComImport]
	public interface ICorDebugAssemblyEnum : ICorDebugEnum {
		void Skip([In] uint celt);
		void Reset();
		void Clone([MarshalAs(UnmanagedType.Interface)] out ICorDebugEnum ppEnum);
		void GetCount(out uint pcelt);
		void Next([In] uint celt, [MarshalAs(UnmanagedType.Interface)] [Out] ICorDebugAssemblyEnum values, out uint pceltFetched);
	}
	[Guid("976A6278-134A-4A81-81A3-8F277943F4C3"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[ComImport]
	public interface ICorDebugBlockingObjectEnum : ICorDebugEnum {
		void Skip([In] uint celt);
		void Reset();
		void Clone([MarshalAs(UnmanagedType.Interface)] out ICorDebugEnum ppEnum);
		void GetCount(out uint pcelt);
		void Next([In] uint celt, [MarshalAs(UnmanagedType.Interface)] [Out] ICorDebugBlockingObjectEnum values, out uint pceltFetched);
	}
	[Guid("CC7BCAFC-8A68-11D2-983C-0000F808342D"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[ComImport]
	public interface ICorDebugBoxValue : ICorDebugHeapValue {
		void GetType(out uint pType);
		void GetSize(out uint pSize);
		void GetAddress(out ulong pAddress);
		void CreateBreakpoint([MarshalAs(UnmanagedType.Interface)] out ICorDebugValueBreakpoint ppBreakpoint);
		void IsValid(out int pbValid);
		void CreateRelocBreakpoint([MarshalAs(UnmanagedType.Interface)] out ICorDebugValueBreakpoint ppBreakpoint);
		void GetObject([MarshalAs(UnmanagedType.Interface)] out ICorDebugObjectValue ppObject);
	}
	[Guid("CC7BCAE8-8A68-11D2-983C-0000F808342D"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[ComImport]
	public interface ICorDebugBreakpoint {
		void Activate([In] int bActive);
		void IsActive(out int pbActive);
	}
	[Guid("CC7BCB03-8A68-11D2-983C-0000F808342D"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[ComImport]
	public interface ICorDebugBreakpointEnum : ICorDebugEnum {
		void Skip([In] uint celt);
		void Reset();
		void Clone([MarshalAs(UnmanagedType.Interface)] out ICorDebugEnum ppEnum);
		void GetCount(out uint pcelt);
		void Next([In] uint celt, [MarshalAs(UnmanagedType.Interface)] [Out] ICorDebugBreakpointEnum breakpoints, out uint pceltFetched);
	}
	[Guid("CC7BCAEE-8A68-11D2-983C-0000F808342D"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[ComImport]
	public interface ICorDebugChain {
		void GetThread([MarshalAs(UnmanagedType.Interface)] out ICorDebugThread ppThread);
		void GetStackRange(out ulong pStart, out ulong pEnd);
		void GetContext([MarshalAs(UnmanagedType.Interface)] out ICorDebugContext ppContext);
		void GetCaller([MarshalAs(UnmanagedType.Interface)] out ICorDebugChain ppChain);
		void GetCallee([MarshalAs(UnmanagedType.Interface)] out ICorDebugChain ppChain);
		void GetPrevious([MarshalAs(UnmanagedType.Interface)] out ICorDebugChain ppChain);
		void GetNext([MarshalAs(UnmanagedType.Interface)] out ICorDebugChain ppChain);
		void IsManaged(out int pManaged);
		void EnumerateFrames([MarshalAs(UnmanagedType.Interface)] out ICorDebugFrameEnum ppFrames);
		void GetActiveFrame([MarshalAs(UnmanagedType.Interface)] out ICorDebugFrame ppFrame);
		void GetRegisterSet([MarshalAs(UnmanagedType.Interface)] out ICorDebugRegisterSet ppRegisters);
		void GetReason(out CorDebugChainReason pReason);
	}
	[Guid("CC7BCB08-8A68-11D2-983C-0000F808342D"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[ComImport]
	public interface ICorDebugChainEnum : ICorDebugEnum {
		void Skip([In] uint celt);
		void Reset();
		void Clone([MarshalAs(UnmanagedType.Interface)] out ICorDebugEnum ppEnum);
		void GetCount(out uint pcelt);
		void Next([In] uint celt, [MarshalAs(UnmanagedType.Interface)] [Out] ICorDebugChainEnum chains, out uint pceltFetched);
	}
	[Guid("CC7BCAF5-8A68-11D2-983C-0000F808342D"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[ComImport]
	public interface ICorDebugClass {
		void GetModule([MarshalAs(UnmanagedType.Interface)] out ICorDebugModule pModule);
		void GetToken(out uint pTypeDef);
		void GetStaticFieldValue([In] uint fieldDef, [MarshalAs(UnmanagedType.Interface)] [In] ICorDebugFrame pFrame, [MarshalAs(UnmanagedType.Interface)] out ICorDebugValue ppValue);
	}
	[Guid("B008EA8D-7AB1-43F7-BB20-FBB5A04038AE"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[ComImport]
	public interface ICorDebugClass2 {
		void GetParameterizedType([In] uint elementType, [In] uint nTypeArgs, [MarshalAs(UnmanagedType.Interface)] [In] ref ICorDebugType ppTypeArgs, [MarshalAs(UnmanagedType.Interface)] out ICorDebugType ppType);
		void SetJMCStatus([In] int bIsJustMyCode);
	}
	[Guid("CC7BCAF4-8A68-11D2-983C-0000F808342D"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[ComImport]
	public interface ICorDebugCode {
		void IsIL(out int pbIL);
		void GetFunction([MarshalAs(UnmanagedType.Interface)] out ICorDebugFunction ppFunction);
		void GetAddress(out ulong pStart);
		void GetSize(out uint pcBytes);
		[PreserveSig]
		int CreateBreakpoint([In] uint offset, [MarshalAs(UnmanagedType.Interface)] out ICorDebugFunctionBreakpoint ppBreakpoint);
		void GetCode([In] uint startOffset, [In] uint endOffset, [In] uint cBufferAlloc, [MarshalAs(UnmanagedType.Interface)] [Out] ICorDebugCode buffer, out uint pcBufferSize);
		void GetVersionNumber(out uint nVersion);
		void GetILToNativeMapping([In] uint cMap, out uint pcMap, [MarshalAs(UnmanagedType.Interface)] [Out] ICorDebugCode map);
		void GetEnCRemapSequencePoints([In] uint cMap, out uint pcMap, [MarshalAs(UnmanagedType.Interface)] [Out] ICorDebugCode offsets);
	}
	[Guid("5F696509-452F-4436-A3FE-4D11FE7E2347"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[ComImport]
	public interface ICorDebugCode2 {
		void GetCodeChunks([In] uint cbufSize, out uint pcnumChunks, [MarshalAs(UnmanagedType.Interface)] [Out] ICorDebugCode2 chunks);
		void GetCompilerFlags(out uint pdwFlags);
	}
	[Guid("D13D3E88-E1F2-4020-AA1D-3D162DCBE966"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[ComImport]
	public interface ICorDebugCode3 {
		void GetReturnValueLiveOffset([In] uint ILoffset, [In] uint bufferSize, out uint pFetched, [MarshalAs(UnmanagedType.Interface)] [Out] ICorDebugCode3 pOffsets);
	}
	[Guid("55E96461-9645-45E4-A2FF-0367877ABCDE"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[ComImport]
	public interface ICorDebugCodeEnum : ICorDebugEnum {
		void Skip([In] uint celt);
		void Reset();
		void Clone([MarshalAs(UnmanagedType.Interface)] out ICorDebugEnum ppEnum);
		void GetCount(out uint pcelt);
		void Next([In] uint celt, [MarshalAs(UnmanagedType.Interface)] [Out] ICorDebugCodeEnum values, out uint pceltFetched);
	}
	[Guid("5F69C5E5-3E12-42DF-B371-F9D761D6EE24"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[ComImport]
	public interface ICorDebugComObjectValue {
		void GetCachedInterfaceTypes([In] int bIInspectableOnly, [MarshalAs(UnmanagedType.Interface)] out ICorDebugTypeEnum ppInterfacesEnum);
		void GetCachedInterfacePointers([In] int bIInspectableOnly, [In] uint celt, out uint pceltFetched, out ulong ptrs);
	}
	[Guid("CC7BCB00-8A68-11D2-983C-0000F808342D"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[ComImport]
	public interface ICorDebugContext : ICorDebugObjectValue {
		void GetType(out uint pType);
		void GetSize(out uint pSize);
		void GetAddress(out ulong pAddress);
		void CreateBreakpoint([MarshalAs(UnmanagedType.Interface)] out ICorDebugValueBreakpoint ppBreakpoint);
		void GetClass([MarshalAs(UnmanagedType.Interface)] out ICorDebugClass ppClass);
		void GetFieldValue([MarshalAs(UnmanagedType.Interface)] [In] ICorDebugClass pClass, [In] uint fieldDef, [MarshalAs(UnmanagedType.Interface)] out ICorDebugValue ppValue);
		void GetVirtualMethod([In] uint memberRef, [MarshalAs(UnmanagedType.Interface)] out ICorDebugFunction ppFunction);
		void GetContext([MarshalAs(UnmanagedType.Interface)] out ICorDebugContext ppContext);
		void IsValueClass(out int pbIsValueClass);
		void GetManagedCopy([MarshalAs(UnmanagedType.IUnknown)] out object ppObject);
		void SetFromManagedCopy([MarshalAs(UnmanagedType.IUnknown)] [In] object pObject);
	}
	[Guid("3D6F5F62-7538-11D3-8D5B-00104B35E7EF"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[ComImport]
	public interface ICorDebugController {
		void Stop([In] uint dwTimeoutIgnored);
		[PreserveSig]
		int Continue([In] int fIsOutOfBand);
		[PreserveSig]
		int IsRunning(out int pbRunning);
		void HasQueuedCallbacks([MarshalAs(UnmanagedType.Interface)] [In] ICorDebugThread pThread, out int pbQueued);
		void EnumerateThreads([MarshalAs(UnmanagedType.Interface)] out ICorDebugThreadEnum ppThreads);
		void SetAllThreadsDebugState([In] CorDebugThreadState state, [MarshalAs(UnmanagedType.Interface)] [In] ICorDebugThread pExceptThisThread);
		void Detach();
		[PreserveSig]
		int Terminate([In] uint exitCode);
		void CanCommitChanges([In] uint cSnapshots, [MarshalAs(UnmanagedType.Interface)] [In] ref ICorDebugEditAndContinueSnapshot pSnapshots, [MarshalAs(UnmanagedType.Interface)] out ICorDebugErrorInfoEnum pError);
		void CommitChanges([In] uint cSnapshots, [MarshalAs(UnmanagedType.Interface)] [In] ref ICorDebugEditAndContinueSnapshot pSnapshots, [MarshalAs(UnmanagedType.Interface)] out ICorDebugErrorInfoEnum pError);
	}
	[Guid("FE06DC28-49FB-4636-A4A3-E80DB4AE116C"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[ComImport]
	public interface ICorDebugDataTarget {
		void GetPlatform(out CorDebugPlatform pTargetPlatform);
		void ReadVirtual([In] ulong address, out byte pBuffer, [In] uint bytesRequested, out uint pBytesRead);
		void GetThreadContext([In] uint dwThreadId, [In] uint contextFlags, [In] uint contextSize, out byte pContext);
	}
	[Guid("2EB364DA-605B-4E8D-B333-3394C4828D41"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[ComImport]
	public interface ICorDebugDataTarget2 {
		void GetImageFromPointer([In] ulong addr, out ulong pImageBase, out uint pSize);
		void GetImageLocation([In] ulong baseAddress, [In] uint cchName, out uint pcchName, [MarshalAs(UnmanagedType.Interface)] [Out] ICorDebugDataTarget2 szName);
		void GetSymbolProviderForImage([In] ulong imageBaseAddress, [MarshalAs(UnmanagedType.Interface)] out ICorDebugSymbolProvider ppSymProvider);
		void EnumerateThreadIDs([In] uint cThreadIds, out uint pcThreadIds, [MarshalAs(UnmanagedType.Interface)] [Out] ICorDebugDataTarget2 pThreadIds);
		void CreateVirtualUnwinder([In] uint nativeThreadID, [In] uint contextFlags, [In] uint cbContext, [In] ref byte initialContext, [MarshalAs(UnmanagedType.Interface)] out ICorDebugVirtualUnwinder ppUnwinder);
	}
	[Guid("D05E60C3-848C-4E7D-894E-623320FF6AFA"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[ComImport]
	public interface ICorDebugDataTarget3 {
		void GetLoadedModules([In] uint cRequestedModules, out uint pcFetchedModules, [MarshalAs(UnmanagedType.Interface)] [Out] ICorDebugDataTarget3 pLoadedModules);
	}
	[Guid("41BD395D-DE99-48F1-BF7A-CC0F44A6D281"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[ComImport]
	public interface ICorDebugDebugEvent {
		void GetEventKind(out CorDebugDebugEventKind pDebugEventKind);
		void GetThread([MarshalAs(UnmanagedType.Interface)] out ICorDebugThread ppThread);
	}
	[Guid("8D600D41-F4F6-4CB3-B7EC-7BD164944036"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[ComImport]
	public interface ICorDebugEditAndContinueErrorInfo {
		void GetModule([MarshalAs(UnmanagedType.Interface)] out ICorDebugModule ppModule);
		void GetToken(out uint pToken);
		void GetErrorCode([MarshalAs(UnmanagedType.Error)] out int pHr);
		void GetString([In] uint cchString, out uint pcchString, [MarshalAs(UnmanagedType.Interface)] [Out] ICorDebugEditAndContinueErrorInfo szString);
	}
	[Guid("6DC3FA01-D7CB-11D2-8A95-0080C792E5D8"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[ComImport]
	public interface ICorDebugEditAndContinueSnapshot {
		void CopyMetaData([MarshalAs(UnmanagedType.Interface)] [In] IStream pIStream, out Guid pMvid);
		void GetMvid(out Guid pMvid);
		void GetRoDataRVA(out uint pRoDataRVA);
		void GetRwDataRVA(out uint pRwDataRVA);
		void SetPEBytes([MarshalAs(UnmanagedType.Interface)] [In] IStream pIStream);
		void SetILMap([In] uint mdFunction, [In] uint cMapSize, [In] ref _COR_IL_MAP map);
		void SetPESymbolBytes([MarshalAs(UnmanagedType.Interface)] [In] IStream pIStream);
	}
	[Guid("CC7BCB01-8A68-11D2-983C-0000F808342D"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[ComImport]
	public interface ICorDebugEnum {
		void Skip([In] uint celt);
		void Reset();
		void Clone([MarshalAs(UnmanagedType.Interface)] out ICorDebugEnum ppEnum);
		void GetCount(out uint pcelt);
	}
	[Guid("F0E18809-72B5-11D2-976F-00A0C9B4D50C"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[ComImport]
	public interface ICorDebugErrorInfoEnum : ICorDebugEnum {
		void Skip([In] uint celt);
		void Reset();
		void Clone([MarshalAs(UnmanagedType.Interface)] out ICorDebugEnum ppEnum);
		void GetCount(out uint pcelt);
		void Next([In] uint celt, [MarshalAs(UnmanagedType.Interface)] [Out] ICorDebugErrorInfoEnum errors, out uint pceltFetched);
	}
	[Guid("CC7BCAF6-8A68-11D2-983C-0000F808342D"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[ComImport]
	public interface ICorDebugEval {
		void CallFunction([MarshalAs(UnmanagedType.Interface)] [In] ICorDebugFunction pFunction, [In] uint nArgs, [MarshalAs(UnmanagedType.Interface)] [In] ref ICorDebugValue ppArgs);
		void NewObject([MarshalAs(UnmanagedType.Interface)] [In] ICorDebugFunction pConstructor, [In] uint nArgs, [MarshalAs(UnmanagedType.Interface)] [In] ref ICorDebugValue ppArgs);
		void NewObjectNoConstructor([MarshalAs(UnmanagedType.Interface)] [In] ICorDebugClass pClass);
		void NewString([MarshalAs(UnmanagedType.LPWStr)] [In] string @string);
		void NewArray([In] uint elementType, [MarshalAs(UnmanagedType.Interface)] [In] ICorDebugClass pElementClass, [In] uint rank, [In] ref uint dims, [In] ref uint lowBounds);
		void IsActive(out int pbActive);
		void Abort();
		void GetResult([MarshalAs(UnmanagedType.Interface)] out ICorDebugValue ppResult);
		void GetThread([MarshalAs(UnmanagedType.Interface)] out ICorDebugThread ppThread);
		void CreateValue([In] uint elementType, [MarshalAs(UnmanagedType.Interface)] [In] ICorDebugClass pElementClass, [MarshalAs(UnmanagedType.Interface)] out ICorDebugValue ppValue);
	}
	[Guid("FB0D9CE7-BE66-4683-9D32-A42A04E2FD91"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[ComImport]
	public interface ICorDebugEval2 {
		void CallParameterizedFunction([MarshalAs(UnmanagedType.Interface)] [In] ICorDebugFunction pFunction, [In] uint nTypeArgs, [MarshalAs(UnmanagedType.Interface)] [In] ref ICorDebugType ppTypeArgs, [In] uint nArgs, [MarshalAs(UnmanagedType.Interface)] [In] ref ICorDebugValue ppArgs);
		void CreateValueForType([MarshalAs(UnmanagedType.Interface)] [In] ICorDebugType pType, [MarshalAs(UnmanagedType.Interface)] out ICorDebugValue ppValue);
		void NewParameterizedObject([MarshalAs(UnmanagedType.Interface)] [In] ICorDebugFunction pConstructor, [In] uint nTypeArgs, [MarshalAs(UnmanagedType.Interface)] [In] ref ICorDebugType ppTypeArgs, [In] uint nArgs, [MarshalAs(UnmanagedType.Interface)] [In] ref ICorDebugValue ppArgs);
		void NewParameterizedObjectNoConstructor([MarshalAs(UnmanagedType.Interface)] [In] ICorDebugClass pClass, [In] uint nTypeArgs, [MarshalAs(UnmanagedType.Interface)] [In] ref ICorDebugType ppTypeArgs);
		void NewParameterizedArray([MarshalAs(UnmanagedType.Interface)] [In] ICorDebugType pElementType, [In] uint rank, [In] ref uint dims, [In] ref uint lowBounds);
		void NewStringWithLength([MarshalAs(UnmanagedType.LPWStr)] [In] string @string, [In] uint uiLength);
		void RudeAbort();
	}
	[Guid("AF79EC94-4752-419C-A626-5FB1CC1A5AB7"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[ComImport]
	public interface ICorDebugExceptionDebugEvent : ICorDebugDebugEvent {
		void GetEventKind(out CorDebugDebugEventKind pDebugEventKind);
		void GetThread([MarshalAs(UnmanagedType.Interface)] out ICorDebugThread ppThread);
		void GetStackPointer(out ulong pStackPointer);
		void GetNativeIP(out ulong pIP);
		void GetFlags(out CorDebugExceptionFlags pdwFlags);
	}
	[Guid("ED775530-4DC4-41F7-86D0-9E2DEF7DFC66"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[ComImport]
	public interface ICorDebugExceptionObjectCallStackEnum : ICorDebugEnum {
		void Skip([In] uint celt);
		void Reset();
		void Clone([MarshalAs(UnmanagedType.Interface)] out ICorDebugEnum ppEnum);
		void GetCount(out uint pcelt);
		void Next([In] uint celt, [MarshalAs(UnmanagedType.Interface)] [Out] ICorDebugExceptionObjectCallStackEnum values, out uint pceltFetched);
	}
	[Guid("AE4CA65D-59DD-42A2-83A5-57E8A08D8719"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[ComImport]
	public interface ICorDebugExceptionObjectValue {
		void EnumerateExceptionCallStack([MarshalAs(UnmanagedType.Interface)] out ICorDebugExceptionObjectCallStackEnum ppCallStackEnum);
	}
	[Guid("CC7BCAEF-8A68-11D2-983C-0000F808342D"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[ComImport]
	public interface ICorDebugFrame {
		void GetChain([MarshalAs(UnmanagedType.Interface)] out ICorDebugChain ppChain);
		void GetCode([MarshalAs(UnmanagedType.Interface)] out ICorDebugCode ppCode);
		void GetFunction([MarshalAs(UnmanagedType.Interface)] out ICorDebugFunction ppFunction);
		void GetFunctionToken(out uint pToken);
		void GetStackRange(out ulong pStart, out ulong pEnd);
		void GetCaller([MarshalAs(UnmanagedType.Interface)] out ICorDebugFrame ppFrame);
		void GetCallee([MarshalAs(UnmanagedType.Interface)] out ICorDebugFrame ppFrame);
		void CreateStepper([MarshalAs(UnmanagedType.Interface)] out ICorDebugStepper ppStepper);
	}
	[Guid("CC7BCB07-8A68-11D2-983C-0000F808342D"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[ComImport]
	public interface ICorDebugFrameEnum : ICorDebugEnum {
		void Skip([In] uint celt);
		void Reset();
		void Clone([MarshalAs(UnmanagedType.Interface)] out ICorDebugEnum ppEnum);
		void GetCount(out uint pcelt);
		void Next([In] uint celt, [MarshalAs(UnmanagedType.Interface)] [Out] ICorDebugFrameEnum frames, out uint pceltFetched);
	}
	[Guid("CC7BCAF3-8A68-11D2-983C-0000F808342D"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[ComImport]
	public interface ICorDebugFunction {
		void GetModule([MarshalAs(UnmanagedType.Interface)] out ICorDebugModule ppModule);
		void GetClass([MarshalAs(UnmanagedType.Interface)] out ICorDebugClass ppClass);
		void GetToken(out uint pMethodDef);
		[PreserveSig]
		int GetILCode([MarshalAs(UnmanagedType.Interface)] out ICorDebugCode ppCode);
		void GetNativeCode([MarshalAs(UnmanagedType.Interface)] out ICorDebugCode ppCode);
		void CreateBreakpoint([MarshalAs(UnmanagedType.Interface)] out ICorDebugFunctionBreakpoint ppBreakpoint);
		void GetLocalVarSigToken(out uint pmdSig);
		void GetCurrentVersionNumber(out uint pnCurrentVersion);
	}
	[Guid("EF0C490B-94C3-4E4D-B629-DDC134C532D8"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[ComImport]
	public interface ICorDebugFunction2 {
		void SetJMCStatus([In] int bIsJustMyCode);
		void GetJMCStatus(out int pbIsJustMyCode);
		void EnumerateNativeCode([MarshalAs(UnmanagedType.Interface)] out ICorDebugCodeEnum ppCodeEnum);
		void GetVersionNumber(out uint pnVersion);
	}
	[Guid("09B70F28-E465-482D-99E0-81A165EB0532"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[ComImport]
	public interface ICorDebugFunction3 {
		void GetActiveReJitRequestILCode([MarshalAs(UnmanagedType.Interface)] ref ICorDebugILCode ppReJitedILCode);
	}
	[Guid("CC7BCAE9-8A68-11D2-983C-0000F808342D"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[ComImport]
	public interface ICorDebugFunctionBreakpoint : ICorDebugBreakpoint {
		[PreserveSig]
		int Activate([In] int bActive);
		void IsActive(out int pbActive);
		void GetFunction([MarshalAs(UnmanagedType.Interface)] out ICorDebugFunction ppFunction);
		void GetOffset(out uint pnOffset);
	}
	[Guid("7F3C24D3-7E1D-4245-AC3A-F72F8859C80C"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[ComImport]
	public interface ICorDebugGCReferenceEnum : ICorDebugEnum {
		void Skip([In] uint celt);
		void Reset();
		void Clone([MarshalAs(UnmanagedType.Interface)] out ICorDebugEnum ppEnum);
		void GetCount(out uint pcelt);
		void Next([In] uint celt, [MarshalAs(UnmanagedType.Interface)] [Out] ICorDebugGCReferenceEnum roots, out uint pceltFetched);
	}
	[Guid("CC7BCAF8-8A68-11D2-983C-0000F808342D"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[ComImport]
	public interface ICorDebugGenericValue : ICorDebugValue {
		void GetType(out uint pType);
		void GetSize(out uint pSize);
		void GetAddress(out ulong pAddress);
		void CreateBreakpoint([MarshalAs(UnmanagedType.Interface)] out ICorDebugValueBreakpoint ppBreakpoint);
		void GetValue([Out] IntPtr pTo);
		void SetValue([In] IntPtr pFrom);
	}
	[Guid("6164D242-1015-4BD6-8CBE-D0DBD4B8275A"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[ComImport]
	public interface ICorDebugGuidToTypeEnum : ICorDebugEnum {
		void Skip([In] uint celt);
		void Reset();
		void Clone([MarshalAs(UnmanagedType.Interface)] out ICorDebugEnum ppEnum);
		void GetCount(out uint pcelt);
		void Next([In] uint celt, [MarshalAs(UnmanagedType.Interface)] [Out] ICorDebugGuidToTypeEnum values, out uint pceltFetched);
	}
	[Guid("029596E8-276B-46A1-9821-732E96BBB00B"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[ComImport]
	public interface ICorDebugHandleValue : ICorDebugReferenceValue {
		void GetType(out uint pType);
		void GetSize(out uint pSize);
		void GetAddress(out ulong pAddress);
		void CreateBreakpoint([MarshalAs(UnmanagedType.Interface)] out ICorDebugValueBreakpoint ppBreakpoint);
		void IsNull(out int pbNull);
		void GetValue(out ulong pValue);
		void SetValue([In] ulong value);
		void Dereference([MarshalAs(UnmanagedType.Interface)] out ICorDebugValue ppValue);
		void DereferenceStrong([MarshalAs(UnmanagedType.Interface)] out ICorDebugValue ppValue);
		void GetHandleType(out CorDebugHandleType pType);
		void Dispose();
	}
	[Guid("76D7DAB8-D044-11DF-9A15-7E29DFD72085"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[ComImport]
	public interface ICorDebugHeapEnum : ICorDebugEnum {
		void Skip([In] uint celt);
		void Reset();
		void Clone([MarshalAs(UnmanagedType.Interface)] out ICorDebugEnum ppEnum);
		void GetCount(out uint pcelt);
		void Next([In] uint celt, [MarshalAs(UnmanagedType.Interface)] [Out] ICorDebugHeapEnum objects, out uint pceltFetched);
	}
	[Guid("A2FA0F8E-D045-11DF-AC8E-CE2ADFD72085"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[ComImport]
	public interface ICorDebugHeapSegmentEnum : ICorDebugEnum {
		void Skip([In] uint celt);
		void Reset();
		void Clone([MarshalAs(UnmanagedType.Interface)] out ICorDebugEnum ppEnum);
		void GetCount(out uint pcelt);
		void Next([In] uint celt, [MarshalAs(UnmanagedType.Interface)] [Out] ICorDebugHeapSegmentEnum segments, out uint pceltFetched);
	}
	[Guid("CC7BCAFA-8A68-11D2-983C-0000F808342D"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[ComImport]
	public interface ICorDebugHeapValue : ICorDebugValue {
		void GetType(out uint pType);
		void GetSize(out uint pSize);
		void GetAddress(out ulong pAddress);
		void CreateBreakpoint([MarshalAs(UnmanagedType.Interface)] out ICorDebugValueBreakpoint ppBreakpoint);
		void IsValid(out int pbValid);
		void CreateRelocBreakpoint([MarshalAs(UnmanagedType.Interface)] out ICorDebugValueBreakpoint ppBreakpoint);
	}
	[Guid("E3AC4D6C-9CB7-43E6-96CC-B21540E5083C"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[ComImport]
	public interface ICorDebugHeapValue2 {
		void CreateHandle([In] CorDebugHandleType type, [MarshalAs(UnmanagedType.Interface)] out ICorDebugHandleValue ppHandle);
	}
	[Guid("A69ACAD8-2374-46E9-9FF8-B1F14120D296"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[ComImport]
	public interface ICorDebugHeapValue3 {
		void GetThreadOwningMonitorLock([MarshalAs(UnmanagedType.Interface)] out ICorDebugThread ppThread, out uint pAcquisitionCount);
		void GetMonitorEventWaitList([MarshalAs(UnmanagedType.Interface)] out ICorDebugThreadEnum ppThreadEnum);
	}
	[Guid("598D46C2-C877-42A7-89D2-3D0C7F1C1264"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[ComImport]
	public interface ICorDebugILCode {
		void GetEHClauses([In] uint cClauses, out uint pcClauses, [MarshalAs(UnmanagedType.Interface)] [Out] ICorDebugILCode clauses);
	}
	[Guid("46586093-D3F5-4DB6-ACDB-955BCE228C15"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[ComImport]
	public interface ICorDebugILCode2 {
		void GetLocalVarSigToken(out uint pmdSig);
		void GetInstrumentedILMap([In] uint cMap, out uint pcMap, [MarshalAs(UnmanagedType.Interface)] [Out] ICorDebugILCode2 map);
	}
	[Guid("03E26311-4F76-11D3-88C6-006097945418"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[ComImport]
	public interface ICorDebugILFrame : ICorDebugFrame {
		void GetChain([MarshalAs(UnmanagedType.Interface)] out ICorDebugChain ppChain);
		void GetCode([MarshalAs(UnmanagedType.Interface)] out ICorDebugCode ppCode);
		void GetFunction([MarshalAs(UnmanagedType.Interface)] out ICorDebugFunction ppFunction);
		void GetFunctionToken(out uint pToken);
		void GetStackRange(out ulong pStart, out ulong pEnd);
		void GetCaller([MarshalAs(UnmanagedType.Interface)] out ICorDebugFrame ppFrame);
		void GetCallee([MarshalAs(UnmanagedType.Interface)] out ICorDebugFrame ppFrame);
		void CreateStepper([MarshalAs(UnmanagedType.Interface)] out ICorDebugStepper ppStepper);
		void GetIP(out uint pnOffset, out CorDebugMappingResult pMappingResult);
		void SetIP([In] uint nOffset);
		void EnumerateLocalVariables([MarshalAs(UnmanagedType.Interface)] out ICorDebugValueEnum ppValueEnum);
		void GetLocalVariable([In] uint dwIndex, [MarshalAs(UnmanagedType.Interface)] out ICorDebugValue ppValue);
		void EnumerateArguments([MarshalAs(UnmanagedType.Interface)] out ICorDebugValueEnum ppValueEnum);
		void GetArgument([In] uint dwIndex, [MarshalAs(UnmanagedType.Interface)] out ICorDebugValue ppValue);
		void GetStackDepth(out uint pDepth);
		void GetStackValue([In] uint dwIndex, [MarshalAs(UnmanagedType.Interface)] out ICorDebugValue ppValue);
		void CanSetIP([In] uint nOffset);
	}
	[Guid("5D88A994-6C30-479B-890F-BCEF88B129A5"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[ComImport]
	public interface ICorDebugILFrame2 {
		void RemapFunction([In] uint newILOffset);
		void EnumerateTypeParameters([MarshalAs(UnmanagedType.Interface)] out ICorDebugTypeEnum ppTyParEnum);
	}
	[Guid("9A9E2ED6-04DF-4FE0-BB50-CAB64126AD24"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[ComImport]
	public interface ICorDebugILFrame3 {
		void GetReturnValueForILOffset(uint ILoffset, [MarshalAs(UnmanagedType.Interface)] out ICorDebugValue ppReturnValue);
	}
	[Guid("AD914A30-C6D1-4AC5-9C5E-577F3BAA8A45"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[ComImport]
	public interface ICorDebugILFrame4 {
		void EnumerateLocalVariablesEx([In] ILCodeKind flags, [MarshalAs(UnmanagedType.Interface)] out ICorDebugValueEnum ppValueEnum);
		void GetLocalVariableEx([In] ILCodeKind flags, [In] uint dwIndex, [MarshalAs(UnmanagedType.Interface)] out ICorDebugValue ppValue);
		void GetCodeEx([In] ILCodeKind flags, [MarshalAs(UnmanagedType.Interface)] out ICorDebugCode ppCode);
	}
	[Guid("A074096B-3ADC-4485-81DA-68C7A4EA52DB"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[ComImport]
	public interface ICorDebugInstanceFieldSymbol {
		void GetName([In] uint cchName, out uint pcchName, [Out] [MarshalAs(UnmanagedType.LPWStr)] StringBuilder szName);
		void GetSize(out uint pcbSize);
		void GetOffset(out uint pcbOffset);
	}
	[Guid("B92CC7F7-9D2D-45C4-BC2B-621FCC9DFBF4"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[ComImport]
	public interface ICorDebugInternalFrame : ICorDebugFrame {
		void GetChain([MarshalAs(UnmanagedType.Interface)] out ICorDebugChain ppChain);
		void GetCode([MarshalAs(UnmanagedType.Interface)] out ICorDebugCode ppCode);
		void GetFunction([MarshalAs(UnmanagedType.Interface)] out ICorDebugFunction ppFunction);
		void GetFunctionToken(out uint pToken);
		void GetStackRange(out ulong pStart, out ulong pEnd);
		void GetCaller([MarshalAs(UnmanagedType.Interface)] out ICorDebugFrame ppFrame);
		void GetCallee([MarshalAs(UnmanagedType.Interface)] out ICorDebugFrame ppFrame);
		void CreateStepper([MarshalAs(UnmanagedType.Interface)] out ICorDebugStepper ppStepper);
		void GetFrameType(out CorDebugInternalFrameType pType);
	}
	[Guid("C0815BDC-CFAB-447E-A779-C116B454EB5B"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[ComImport]
	public interface ICorDebugInternalFrame2 {
		void GetAddress(out ulong pAddress);
		void IsCloserToLeaf([MarshalAs(UnmanagedType.Interface)] [In] ICorDebugFrame pFrameToCompare, out int pIsCloser);
	}
	[Guid("817F343A-6630-4578-96C5-D11BC0EC5EE2"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[ComImport]
	public interface ICorDebugLoadedModule {
		void GetBaseAddress(out ulong pAddress);
		void GetName([In] uint cchName, out uint pcchName, [Out] [MarshalAs(UnmanagedType.LPWStr)] StringBuilder szName);
		void GetSize(out uint pcBytes);
	}
	[Guid("3D6F5F60-7538-11D3-8D5B-00104B35E7EF"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[ComImport]
	public interface ICorDebugManagedCallback {
		void Breakpoint([MarshalAs(UnmanagedType.Interface)] [In] ICorDebugAppDomain pAppDomain, [MarshalAs(UnmanagedType.Interface)] [In] ICorDebugThread pThread, [MarshalAs(UnmanagedType.Interface)] [In] ICorDebugBreakpoint pBreakpoint);
		void StepComplete([MarshalAs(UnmanagedType.Interface)] [In] ICorDebugAppDomain pAppDomain, [MarshalAs(UnmanagedType.Interface)] [In] ICorDebugThread pThread, [MarshalAs(UnmanagedType.Interface)] [In] ICorDebugStepper pStepper, [In] CorDebugStepReason reason);
		void Break([MarshalAs(UnmanagedType.Interface)] [In] ICorDebugAppDomain pAppDomain, [MarshalAs(UnmanagedType.Interface)] [In] ICorDebugThread thread);
		void Exception([MarshalAs(UnmanagedType.Interface)] [In] ICorDebugAppDomain pAppDomain, [MarshalAs(UnmanagedType.Interface)] [In] ICorDebugThread pThread, [In] int unhandled);
		void EvalComplete([MarshalAs(UnmanagedType.Interface)] [In] ICorDebugAppDomain pAppDomain, [MarshalAs(UnmanagedType.Interface)] [In] ICorDebugThread pThread, [MarshalAs(UnmanagedType.Interface)] [In] ICorDebugEval pEval);
		void EvalException([MarshalAs(UnmanagedType.Interface)] [In] ICorDebugAppDomain pAppDomain, [MarshalAs(UnmanagedType.Interface)] [In] ICorDebugThread pThread, [MarshalAs(UnmanagedType.Interface)] [In] ICorDebugEval pEval);
		void CreateProcess([MarshalAs(UnmanagedType.Interface)] [In] ICorDebugProcess pProcess);
		void ExitProcess([MarshalAs(UnmanagedType.Interface)] [In] ICorDebugProcess pProcess);
		void CreateThread([MarshalAs(UnmanagedType.Interface)] [In] ICorDebugAppDomain pAppDomain, [MarshalAs(UnmanagedType.Interface)] [In] ICorDebugThread thread);
		void ExitThread([MarshalAs(UnmanagedType.Interface)] [In] ICorDebugAppDomain pAppDomain, [MarshalAs(UnmanagedType.Interface)] [In] ICorDebugThread thread);
		void LoadModule([MarshalAs(UnmanagedType.Interface)] [In] ICorDebugAppDomain pAppDomain, [MarshalAs(UnmanagedType.Interface)] [In] ICorDebugModule pModule);
		void UnloadModule([MarshalAs(UnmanagedType.Interface)] [In] ICorDebugAppDomain pAppDomain, [MarshalAs(UnmanagedType.Interface)] [In] ICorDebugModule pModule);
		void LoadClass([MarshalAs(UnmanagedType.Interface)] [In] ICorDebugAppDomain pAppDomain, [MarshalAs(UnmanagedType.Interface)] [In] ICorDebugClass c);
		void UnloadClass([MarshalAs(UnmanagedType.Interface)] [In] ICorDebugAppDomain pAppDomain, [MarshalAs(UnmanagedType.Interface)] [In] ICorDebugClass c);
		void DebuggerError([MarshalAs(UnmanagedType.Interface)] [In] ICorDebugProcess pProcess, [MarshalAs(UnmanagedType.Error)] [In] int errorHR, [In] uint errorCode);
		void LogMessage([MarshalAs(UnmanagedType.Interface)] [In] ICorDebugAppDomain pAppDomain, [MarshalAs(UnmanagedType.Interface)] [In] ICorDebugThread pThread, [In] int lLevel, [In] [MarshalAs(UnmanagedType.LPWStr)] string pLogSwitchName, [In] [MarshalAs(UnmanagedType.LPWStr)] string pMessage);
		void LogSwitch([MarshalAs(UnmanagedType.Interface)] [In] ICorDebugAppDomain pAppDomain, [MarshalAs(UnmanagedType.Interface)] [In] ICorDebugThread pThread, [In] int lLevel, [In] uint ulReason, [In] [MarshalAs(UnmanagedType.LPWStr)] string pLogSwitchName, [In] [MarshalAs(UnmanagedType.LPWStr)] string pParentName);
		void CreateAppDomain([MarshalAs(UnmanagedType.Interface)] [In] ICorDebugProcess pProcess, [MarshalAs(UnmanagedType.Interface)] [In] ICorDebugAppDomain pAppDomain);
		void ExitAppDomain([MarshalAs(UnmanagedType.Interface)] [In] ICorDebugProcess pProcess, [MarshalAs(UnmanagedType.Interface)] [In] ICorDebugAppDomain pAppDomain);
		void LoadAssembly([MarshalAs(UnmanagedType.Interface)] [In] ICorDebugAppDomain pAppDomain, [MarshalAs(UnmanagedType.Interface)] [In] ICorDebugAssembly pAssembly);
		void UnloadAssembly([MarshalAs(UnmanagedType.Interface)] [In] ICorDebugAppDomain pAppDomain, [MarshalAs(UnmanagedType.Interface)] [In] ICorDebugAssembly pAssembly);
		void ControlCTrap([MarshalAs(UnmanagedType.Interface)] [In] ICorDebugProcess pProcess);
		void NameChange([MarshalAs(UnmanagedType.Interface)] [In] ICorDebugAppDomain pAppDomain, [MarshalAs(UnmanagedType.Interface)] [In] ICorDebugThread pThread);
		void UpdateModuleSymbols([MarshalAs(UnmanagedType.Interface)] [In] ICorDebugAppDomain pAppDomain, [MarshalAs(UnmanagedType.Interface)] [In] ICorDebugModule pModule, [MarshalAs(UnmanagedType.Interface)] [In] IStream pSymbolStream);
		void EditAndContinueRemap([MarshalAs(UnmanagedType.Interface)] [In] ICorDebugAppDomain pAppDomain, [MarshalAs(UnmanagedType.Interface)] [In] ICorDebugThread pThread, [MarshalAs(UnmanagedType.Interface)] [In] ICorDebugFunction pFunction, [In] int fAccurate);
		void BreakpointSetError([MarshalAs(UnmanagedType.Interface)] [In] ICorDebugAppDomain pAppDomain, [MarshalAs(UnmanagedType.Interface)] [In] ICorDebugThread pThread, [MarshalAs(UnmanagedType.Interface)] [In] ICorDebugBreakpoint pBreakpoint, [In] uint dwError);
	}
	[Guid("250E5EEA-DB5C-4C76-B6F3-8C46F12E3203"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[ComImport]
	public interface ICorDebugManagedCallback2 {
		void FunctionRemapOpportunity([MarshalAs(UnmanagedType.Interface)] [In] ICorDebugAppDomain pAppDomain, [MarshalAs(UnmanagedType.Interface)] [In] ICorDebugThread pThread, [MarshalAs(UnmanagedType.Interface)] [In] ICorDebugFunction pOldFunction, [MarshalAs(UnmanagedType.Interface)] [In] ICorDebugFunction pNewFunction, [In] uint oldILOffset);
		void CreateConnection([MarshalAs(UnmanagedType.Interface)] [In] ICorDebugProcess pProcess, [In] uint dwConnectionId, [In] [MarshalAs(UnmanagedType.LPWStr)] string pConnName);
		void ChangeConnection([MarshalAs(UnmanagedType.Interface)] [In] ICorDebugProcess pProcess, [In] uint dwConnectionId);
		void DestroyConnection([MarshalAs(UnmanagedType.Interface)] [In] ICorDebugProcess pProcess, [In] uint dwConnectionId);
		void Exception([MarshalAs(UnmanagedType.Interface)] [In] ICorDebugAppDomain pAppDomain, [MarshalAs(UnmanagedType.Interface)] [In] ICorDebugThread pThread, [MarshalAs(UnmanagedType.Interface)] [In] ICorDebugFrame pFrame, [In] uint nOffset, [In] CorDebugExceptionCallbackType dwEventType, [In] CorDebugExceptionFlags dwFlags);
		void ExceptionUnwind([MarshalAs(UnmanagedType.Interface)] [In] ICorDebugAppDomain pAppDomain, [MarshalAs(UnmanagedType.Interface)] [In] ICorDebugThread pThread, [In] CorDebugExceptionUnwindCallbackType dwEventType, [In] CorDebugExceptionFlags dwFlags);
		void FunctionRemapComplete([MarshalAs(UnmanagedType.Interface)] [In] ICorDebugAppDomain pAppDomain, [MarshalAs(UnmanagedType.Interface)] [In] ICorDebugThread pThread, [MarshalAs(UnmanagedType.Interface)] [In] ICorDebugFunction pFunction);
		void MDANotification([MarshalAs(UnmanagedType.Interface)] [In] ICorDebugController pController, [MarshalAs(UnmanagedType.Interface)] [In] ICorDebugThread pThread, [MarshalAs(UnmanagedType.Interface)] [In] ICorDebugMDA pMDA);
	}
	[Guid("264EA0FC-2591-49AA-868E-835E6515323F"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[ComImport]
	public interface ICorDebugManagedCallback3 {
		void CustomNotification([MarshalAs(UnmanagedType.Interface)] [In] ICorDebugThread pThread, [MarshalAs(UnmanagedType.Interface)] [In] ICorDebugAppDomain pAppDomain);
	}
	[Guid("CC726F2F-1DB7-459B-B0EC-05F01D841B42"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[ComImport]
	public interface ICorDebugMDA {
		void GetName([In] uint cchName, out uint pcchName, [Out] [MarshalAs(UnmanagedType.LPWStr)] StringBuilder szName);
		void GetDescription([In] uint cchName, out uint pcchName, [MarshalAs(UnmanagedType.Interface)] [Out] ICorDebugMDA szName);
		void GetXML([In] uint cchName, out uint pcchName, [MarshalAs(UnmanagedType.Interface)] [Out] ICorDebugMDA szName);
		void GetFlags([In] ref CorDebugMDAFlags pFlags);
		void GetOSThreadId(out uint pOsTid);
	}
	[Guid("677888B3-D160-4B8C-A73B-D79E6AAA1D13"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[ComImport]
	public interface ICorDebugMemoryBuffer {
		void GetStartAddress(out IntPtr address);
		void GetSize(out uint pcbBufferLength);
	}
	[Guid("FAA8637B-3BBE-4671-8E26-3B59875B922A"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[ComImport]
	public interface ICorDebugMergedAssemblyRecord {
		void GetSimpleName([In] uint cchName, out uint pcchName, [MarshalAs(UnmanagedType.Interface)] [Out] ICorDebugMergedAssemblyRecord szName);
		void GetVersion(out ushort pMajor, out ushort pMinor, out ushort pBuild, out ushort pRevision);
		void GetCulture([In] uint cchCulture, out uint pcchCulture, [MarshalAs(UnmanagedType.Interface)] [Out] ICorDebugMergedAssemblyRecord szCulture);
		void GetPublicKey([In] uint cbPublicKey, out uint pcbPublicKey, [MarshalAs(UnmanagedType.Interface)] [Out] ICorDebugMergedAssemblyRecord pbPublicKey);
		void GetPublicKeyToken([In] uint cbPublicKeyToken, out uint pcbPublicKeyToken, [MarshalAs(UnmanagedType.Interface)] [Out] ICorDebugMergedAssemblyRecord pbPublicKeyToken);
		void GetIndex(out uint pIndex);
	}
	[Guid("7CEF8BA9-2EF7-42BF-973F-4171474F87D9"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[ComImport]
	public interface ICorDebugMetaDataLocator {
		void GetMetaData([MarshalAs(UnmanagedType.LPWStr)] [In] string wszImagePath, [In] uint dwImageTimeStamp, [In] uint dwImageSize, [In] uint cchPathBuffer, out uint pcchPathBuffer, [MarshalAs(UnmanagedType.Interface)] [Out] ICorDebugMetaDataLocator wszPathBuffer);
	}
	[Guid("DBA2D8C1-E5C5-4069-8C13-10A7C6ABF43D"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[ComImport]
	public interface ICorDebugModule {
		void GetProcess([MarshalAs(UnmanagedType.Interface)] out ICorDebugProcess ppProcess);
		[PreserveSig]
		int GetBaseAddress(out ulong pAddress);
		[PreserveSig]
		int GetAssembly([MarshalAs(UnmanagedType.Interface)] out ICorDebugAssembly ppAssembly);
		[PreserveSig]
		int GetName([In] uint cchName, out uint pcchName, [Out] [MarshalAs(UnmanagedType.LPWStr)] StringBuilder szName);
		[PreserveSig]
		int EnableJITDebugging([In] int bTrackJITInfo, [In] int bAllowJitOpts);
		[PreserveSig]
		int EnableClassLoadCallbacks([In] int bClassLoadCallbacks);
		[PreserveSig]
		int GetFunctionFromToken([In] uint methodDef, [MarshalAs(UnmanagedType.Interface)] out ICorDebugFunction ppFunction);
		void GetFunctionFromRVA([In] ulong rva, [MarshalAs(UnmanagedType.Interface)] out ICorDebugFunction ppFunction);
		void GetClassFromToken([In] uint typeDef, [MarshalAs(UnmanagedType.Interface)] out ICorDebugClass ppClass);
		void CreateBreakpoint([MarshalAs(UnmanagedType.Interface)] out ICorDebugModuleBreakpoint ppBreakpoint);
		void GetEditAndContinueSnapshot([MarshalAs(UnmanagedType.Interface)] out ICorDebugEditAndContinueSnapshot ppEditAndContinueSnapshot);
		void GetMetaDataInterface([In] ref Guid riid, [MarshalAs(UnmanagedType.IUnknown)] out object ppObj);
		[PreserveSig]
		int GetToken(out uint pToken);
		[PreserveSig]
		int IsDynamic(out int pDynamic);
		void GetGlobalVariableValue([In] uint fieldDef, [MarshalAs(UnmanagedType.Interface)] out ICorDebugValue ppValue);
		[PreserveSig]
		int GetSize(out uint pcBytes);
		[PreserveSig]
		int IsInMemory(out int pInMemory);
	}
	[Guid("7FCC5FB5-49C0-41DE-9938-3B88B5B9ADD7"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[ComImport]
	public interface ICorDebugModule2 {
		[PreserveSig]
		int SetJMCStatus([In] int bIsJustMyCode, [In] uint cTokens, [In] IntPtr pTokens);
		void ApplyChanges([In] uint cbMetadata, [In] IntPtr pbMetadata, [In] uint cbIL, [In] IntPtr pbIL);
		[PreserveSig]
		int SetJITCompilerFlags([In] CorDebugJITCompilerFlags dwFlags);
		void GetJITCompilerFlags(out CorDebugJITCompilerFlags pdwFlags);
		void ResolveAssembly([In] uint tkAssemblyRef, [MarshalAs(UnmanagedType.Interface)] out ICorDebugAssembly ppAssembly);
	}
	[Guid("86F012BF-FF15-4372-BD30-B6F11CAAE1DD"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[ComImport]
	public interface ICorDebugModule3 {
		void CreateReaderForInMemorySymbols([In] ref Guid riid, out IntPtr ppObj);
	}
	[Guid("CC7BCAEA-8A68-11D2-983C-0000F808342D"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[ComImport]
	public interface ICorDebugModuleBreakpoint : ICorDebugBreakpoint {
		void Activate([In] int bActive);
		void IsActive(out int pbActive);
		void GetModule([MarshalAs(UnmanagedType.Interface)] out ICorDebugModule ppModule);
	}
	[Guid("51A15E8D-9FFF-4864-9B87-F4FBDEA747A2"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[ComImport]
	public interface ICorDebugModuleDebugEvent : ICorDebugDebugEvent {
		void GetEventKind(out CorDebugDebugEventKind pDebugEventKind);
		void GetThread([MarshalAs(UnmanagedType.Interface)] out ICorDebugThread ppThread);
		void GetModule([MarshalAs(UnmanagedType.Interface)] out ICorDebugModule ppModule);
	}
	[Guid("CC7BCB09-8A68-11D2-983C-0000F808342D"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[ComImport]
	public interface ICorDebugModuleEnum : ICorDebugEnum {
		void Skip([In] uint celt);
		void Reset();
		void Clone([MarshalAs(UnmanagedType.Interface)] out ICorDebugEnum ppEnum);
		void GetCount(out uint pcelt);
		void Next([In] uint celt, [MarshalAs(UnmanagedType.Interface)] [Out] ICorDebugModuleEnum modules, out uint pceltFetched);
	}
	[Guid("A1B8A756-3CB6-4CCB-979F-3DF999673A59"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[ComImport]
	public interface ICorDebugMutableDataTarget : ICorDebugDataTarget {
		void GetPlatform(out CorDebugPlatform pTargetPlatform);
		void ReadVirtual([In] ulong address, out byte pBuffer, [In] uint bytesRequested, out uint pBytesRead);
		void GetThreadContext([In] uint dwThreadId, [In] uint contextFlags, [In] uint contextSize, out byte pContext);
		void WriteVirtual([In] ulong address, [In] ref byte pBuffer, [In] uint bytesRequested);
		void SetThreadContext([In] uint dwThreadId, [In] uint contextSize, [In] ref byte pContext);
		void ContinueStatusChanged([In] uint dwThreadId, [In] uint continueStatus);
	}
	[Guid("03E26314-4F76-11D3-88C6-006097945418"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[ComImport]
	public interface ICorDebugNativeFrame : ICorDebugFrame {
		void GetChain([MarshalAs(UnmanagedType.Interface)] out ICorDebugChain ppChain);
		void GetCode([MarshalAs(UnmanagedType.Interface)] out ICorDebugCode ppCode);
		void GetFunction([MarshalAs(UnmanagedType.Interface)] out ICorDebugFunction ppFunction);
		void GetFunctionToken(out uint pToken);
		void GetStackRange(out ulong pStart, out ulong pEnd);
		void GetCaller([MarshalAs(UnmanagedType.Interface)] out ICorDebugFrame ppFrame);
		void GetCallee([MarshalAs(UnmanagedType.Interface)] out ICorDebugFrame ppFrame);
		void CreateStepper([MarshalAs(UnmanagedType.Interface)] out ICorDebugStepper ppStepper);
		void GetIP(out uint pnOffset);
		void SetIP([In] uint nOffset);
		void GetRegisterSet([MarshalAs(UnmanagedType.Interface)] out ICorDebugRegisterSet ppRegisters);
		void GetLocalRegisterValue([In] CorDebugRegister reg, [In] uint cbSigBlob, [In] UIntPtr pvSigBlob, [MarshalAs(UnmanagedType.Interface)] out ICorDebugValue ppValue);
		void GetLocalDoubleRegisterValue([In] CorDebugRegister highWordReg, [In] CorDebugRegister lowWordReg, [In] uint cbSigBlob, [In] UIntPtr pvSigBlob, [MarshalAs(UnmanagedType.Interface)] out ICorDebugValue ppValue);
		void GetLocalMemoryValue([In] ulong address, [In] uint cbSigBlob, [In] UIntPtr pvSigBlob, [MarshalAs(UnmanagedType.Interface)] out ICorDebugValue ppValue);
		void GetLocalRegisterMemoryValue([In] CorDebugRegister highWordReg, [In] ulong lowWordAddress, [In] uint cbSigBlob, [In] UIntPtr pvSigBlob, [MarshalAs(UnmanagedType.Interface)] out ICorDebugValue ppValue);
		void GetLocalMemoryRegisterValue([In] ulong highWordAddress, [In] CorDebugRegister lowWordRegister, [In] uint cbSigBlob, [In] UIntPtr pvSigBlob, [MarshalAs(UnmanagedType.Interface)] out ICorDebugValue ppValue);
		void CanSetIP([In] uint nOffset);
	}
	[Guid("35389FF1-3684-4C55-A2EE-210F26C60E5E"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[ComImport]
	public interface ICorDebugNativeFrame2 {
		void IsChild(out int pIsChild);
		void IsMatchingParentFrame([MarshalAs(UnmanagedType.Interface)] [In] ICorDebugNativeFrame2 pPotentialParentFrame, out int pIsParent);
		void GetStackParameterSize(out uint pSize);
	}
	[Guid("CC7BCB02-8A68-11D2-983C-0000F808342D"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[ComImport]
	public interface ICorDebugObjectEnum : ICorDebugEnum {
		void Skip([In] uint celt);
		void Reset();
		void Clone([MarshalAs(UnmanagedType.Interface)] out ICorDebugEnum ppEnum);
		void GetCount(out uint pcelt);
		void Next([In] uint celt, [MarshalAs(UnmanagedType.Interface)] [Out] ICorDebugObjectEnum objects, out uint pceltFetched);
	}
	[Guid("18AD3D6E-B7D2-11D2-BD04-0000F80849BD"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[ComImport]
	public interface ICorDebugObjectValue : ICorDebugValue {
		void GetType(out uint pType);
		void GetSize(out uint pSize);
		void GetAddress(out ulong pAddress);
		void CreateBreakpoint([MarshalAs(UnmanagedType.Interface)] out ICorDebugValueBreakpoint ppBreakpoint);
		void GetClass([MarshalAs(UnmanagedType.Interface)] out ICorDebugClass ppClass);
		void GetFieldValue([MarshalAs(UnmanagedType.Interface)] [In] ICorDebugClass pClass, [In] uint fieldDef, [MarshalAs(UnmanagedType.Interface)] out ICorDebugValue ppValue);
		void GetVirtualMethod([In] uint memberRef, [MarshalAs(UnmanagedType.Interface)] out ICorDebugFunction ppFunction);
		void GetContext([MarshalAs(UnmanagedType.Interface)] out ICorDebugContext ppContext);
		void IsValueClass(out int pbIsValueClass);
		void GetManagedCopy([MarshalAs(UnmanagedType.IUnknown)] out object ppObject);
		void SetFromManagedCopy([MarshalAs(UnmanagedType.IUnknown)] [In] object pObject);
	}
	[Guid("49E4A320-4A9B-4ECA-B105-229FB7D5009F"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[ComImport]
	public interface ICorDebugObjectValue2 {
		void GetVirtualMethodAndType([In] uint memberRef, [MarshalAs(UnmanagedType.Interface)] out ICorDebugFunction ppFunction, [MarshalAs(UnmanagedType.Interface)] out ICorDebugType ppType);
	}
	[Guid("3D6F5F64-7538-11D3-8D5B-00104B35E7EF"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[ComImport]
	public interface ICorDebugProcess : ICorDebugController {
		void Stop([In] uint dwTimeoutIgnored);
		[PreserveSig]
		int Continue([In] int fIsOutOfBand);
		[PreserveSig]
		int IsRunning(out int pbRunning);
		void HasQueuedCallbacks([MarshalAs(UnmanagedType.Interface)] [In] ICorDebugThread pThread, out int pbQueued);
		void EnumerateThreads([MarshalAs(UnmanagedType.Interface)] out ICorDebugThreadEnum ppThreads);
		void SetAllThreadsDebugState([In] CorDebugThreadState state, [MarshalAs(UnmanagedType.Interface)] [In] ICorDebugThread pExceptThisThread);
		void Detach();
		[PreserveSig]
		int Terminate([In] uint exitCode);
		void CanCommitChanges([In] uint cSnapshots, [MarshalAs(UnmanagedType.Interface)] [In] ref ICorDebugEditAndContinueSnapshot pSnapshots, [MarshalAs(UnmanagedType.Interface)] out ICorDebugErrorInfoEnum pError);
		void CommitChanges([In] uint cSnapshots, [MarshalAs(UnmanagedType.Interface)] [In] ref ICorDebugEditAndContinueSnapshot pSnapshots, [MarshalAs(UnmanagedType.Interface)] out ICorDebugErrorInfoEnum pError);
		[PreserveSig]
		int GetID(out int pdwProcessId);
		void GetHandle(out UIntPtr phProcessHandle);
		void GetThread([In] uint dwThreadId, [MarshalAs(UnmanagedType.Interface)] out ICorDebugThread ppThread);
		void EnumerateObjects([MarshalAs(UnmanagedType.Interface)] out ICorDebugObjectEnum ppObjects);
		void IsTransitionStub([In] ulong address, out int pbTransitionStub);
		void IsOSSuspended([In] uint threadID, out int pbSuspended);
		void GetThreadContext([In] uint threadID, [In] uint contextSize, [MarshalAs(UnmanagedType.Interface)] [In] [Out] ICorDebugProcess context);
		void SetThreadContext([In] uint threadID, [In] uint contextSize, [MarshalAs(UnmanagedType.Interface)] [In] ICorDebugProcess context);
		void ReadMemory([In] ulong address, [In] uint size, [MarshalAs(UnmanagedType.Interface)] [Out] ICorDebugProcess buffer, out UIntPtr read);
		void WriteMemory([In] ulong address, [In] uint size, [In] ref byte buffer, out UIntPtr written);
		void ClearCurrentException([In] uint threadID);
		[PreserveSig]
		int EnableLogMessages([In] int fOnOff);
		void ModifyLogSwitch([In] [MarshalAs(UnmanagedType.LPWStr)] string pLogSwitchName, [In] int lLevel);
		void EnumerateAppDomains([MarshalAs(UnmanagedType.Interface)] out ICorDebugAppDomainEnum ppAppDomains);
		void GetObject([MarshalAs(UnmanagedType.Interface)] out ICorDebugValue ppObject);
		void ThreadForFiberCookie([In] uint fiberCookie, [MarshalAs(UnmanagedType.Interface)] out ICorDebugThread ppThread);
		[PreserveSig]
		int GetHelperThreadID(out uint pThreadID);
	}
	[Guid("AD1B3588-0EF0-4744-A496-AA09A9F80371"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[ComImport]
	public interface ICorDebugProcess2 {
		void GetThreadForTaskID([In] ulong taskid, [MarshalAs(UnmanagedType.Interface)] out ICorDebugThread2 ppThread);
		void GetVersion(out _COR_VERSION version);
		void SetUnmanagedBreakpoint([In] ulong address, [In] uint bufsize, [MarshalAs(UnmanagedType.Interface)] [Out] ICorDebugProcess2 buffer, out uint bufLen);
		void ClearUnmanagedBreakpoint([In] ulong address);
		[PreserveSig]
		int SetDesiredNGENCompilerFlags([In] CorDebugJITCompilerFlags pdwFlags);
		void GetDesiredNGENCompilerFlags(out CorDebugJITCompilerFlags pdwFlags);
		void GetReferenceValueFromGCHandle([In] UIntPtr handle, [MarshalAs(UnmanagedType.Interface)] out ICorDebugReferenceValue pOutValue);
	}
	[Guid("2EE06488-C0D4-42B1-B26D-F3795EF606FB"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[ComImport]
	public interface ICorDebugProcess3 {
		void SetEnableCustomNotification([MarshalAs(UnmanagedType.Interface)] ICorDebugClass pClass, int fEnable);
	}
	[Guid("21E9D9C0-FCB8-11DF-8CFF-0800200C9A66"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[ComImport]
	public interface ICorDebugProcess5 {
		void GetGCHeapInformation(out _COR_HEAPINFO pHeapInfo);
		void EnumerateHeap([MarshalAs(UnmanagedType.Interface)] out ICorDebugHeapEnum ppObjects);
		void EnumerateHeapRegions([MarshalAs(UnmanagedType.Interface)] out ICorDebugHeapSegmentEnum ppRegions);
		void GetObject([In] ulong addr, [MarshalAs(UnmanagedType.Interface)] out ICorDebugObjectValue pObject);
		void EnumerateGCReferences([In] int enumerateWeakReferences, [MarshalAs(UnmanagedType.Interface)] out ICorDebugGCReferenceEnum ppEnum);
		void EnumerateHandles([In] CorGCReferenceType types, [MarshalAs(UnmanagedType.Interface)] out ICorDebugGCReferenceEnum ppEnum);
		void GetTypeID([In] ulong obj, out COR_TYPEID pId);
		void GetTypeForTypeID([In] COR_TYPEID id, [MarshalAs(UnmanagedType.Interface)] out ICorDebugType ppType);
		void GetArrayLayout([In] COR_TYPEID id, out COR_ARRAY_LAYOUT pLayout);
		void GetTypeLayout([In] COR_TYPEID id, out COR_TYPE_LAYOUT pLayout);
		void GetTypeFields([In] COR_TYPEID id, uint celt, ref COR_FIELD fields, ref uint pceltNeeded);
		void EnableNGENPolicy([In] CorDebugNGENPolicy ePolicy);
	}
	[Guid("11588775-7205-4CEB-A41A-93753C3153E9"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[ComImport]
	public interface ICorDebugProcess6 {
		void DecodeEvent([MarshalAs(UnmanagedType.Interface)] [In] ICorDebugProcess6 pRecord, [In] uint countBytes, [In] CorDebugRecordFormat format, [In] uint dwFlags, [In] uint dwThreadId, [MarshalAs(UnmanagedType.Interface)] out ICorDebugDebugEvent ppEvent);
		void ProcessStateChanged([In] CorDebugStateChange change);
		void GetCode([In] ulong codeAddress, [MarshalAs(UnmanagedType.Interface)] out ICorDebugCode ppCode);
		void EnableVirtualModuleSplitting(int enableSplitting);
		void MarkDebuggerAttached(int fIsAttached);
		void GetExportStepInfo([MarshalAs(UnmanagedType.LPWStr)] [In] string pszExportName, out CorDebugCodeInvokeKind pInvokeKind, out CorDebugCodeInvokePurpose pInvokePurpose);
	}
	[Guid("9B2C54E4-119F-4D6F-B402-527603266D69"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[ComImport]
	public interface ICorDebugProcess7 {
		[PreserveSig]
		int SetWriteableMetadataUpdateMode(WriteableMetadataUpdateMode flags);
	}
	[Guid("2E6F28C1-85EB-4141-80AD-0A90944B9639"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[ComImport]
	public interface ICorDebugProcess8 {
		[PreserveSig]
		int EnableExceptionCallbacksOutsideOfMyCode([In] int enableExceptionsOutsideOfJMC);
	}
	[Guid("CC7BCB05-8A68-11D2-983C-0000F808342D"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[ComImport]
	public interface ICorDebugProcessEnum : ICorDebugEnum {
		void Skip([In] uint celt);
		void Reset();
		void Clone([MarshalAs(UnmanagedType.Interface)] out ICorDebugEnum ppEnum);
		void GetCount(out uint pcelt);
		void Next([In] uint celt, [MarshalAs(UnmanagedType.Interface)] [Out] ICorDebugProcessEnum processes, out uint pceltFetched);
	}
	[Guid("CC7BCAF9-8A68-11D2-983C-0000F808342D"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[ComImport]
	public interface ICorDebugReferenceValue : ICorDebugValue {
		void GetType(out uint pType);
		void GetSize(out uint pSize);
		void GetAddress(out ulong pAddress);
		void CreateBreakpoint([MarshalAs(UnmanagedType.Interface)] out ICorDebugValueBreakpoint ppBreakpoint);
		void IsNull(out int pbNull);
		void GetValue(out ulong pValue);
		void SetValue([In] ulong value);
		void Dereference([MarshalAs(UnmanagedType.Interface)] out ICorDebugValue ppValue);
		void DereferenceStrong([MarshalAs(UnmanagedType.Interface)] out ICorDebugValue ppValue);
	}
	[Guid("CC7BCB0B-8A68-11D2-983C-0000F808342D"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[ComImport]
	public interface ICorDebugRegisterSet {
		void GetRegistersAvailable(out ulong pAvailable);
		void GetRegisters([In] ulong mask, [In] uint regCount, [MarshalAs(UnmanagedType.Interface)] [Out] ICorDebugRegisterSet regBuffer);
		void SetRegisters([In] ulong mask, [In] uint regCount, [In] ref ulong regBuffer);
		void GetThreadContext([In] uint contextSize, [MarshalAs(UnmanagedType.Interface)] [In] [Out] ICorDebugRegisterSet context);
		void SetThreadContext([In] uint contextSize, [MarshalAs(UnmanagedType.Interface)] [In] ICorDebugRegisterSet context);
	}
	[Guid("6DC7BA3F-89BA-4459-9EC1-9D60937B468D"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[ComImport]
	public interface ICorDebugRegisterSet2 {
		void GetRegistersAvailable([In] uint numChunks, out byte availableRegChunks);
		void GetRegisters([In] uint maskCount, [In] ref byte mask, [In] uint regCount, out ulong regBuffer);
		void SetRegisters([In] uint maskCount, [In] ref byte mask, [In] uint regCount, [In] ref ulong regBuffer);
	}
	[Guid("D5EBB8E2-7BBE-4C1D-98A6-A3C04CBDEF64"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[ComImport]
	public interface ICorDebugRemote {
		void CreateProcessEx([MarshalAs(UnmanagedType.Interface)] [In] ICorDebugRemoteTarget pRemoteTarget, [MarshalAs(UnmanagedType.LPWStr)] [In] string lpApplicationName, [MarshalAs(UnmanagedType.LPWStr)] [In] string lpCommandLine, IntPtr lpProcessAttributes, IntPtr lpThreadAttributes, [In] int bInheritHandles, [In] ProcessCreationFlags dwCreationFlags, [In] IntPtr lpEnvironment, [MarshalAs(UnmanagedType.LPWStr)] [In] string lpCurrentDirectory, [In] ref STARTUPINFO lpStartupInfo, [In] ref PROCESS_INFORMATION lpProcessInformation, [In] CorDebugCreateProcessFlags debuggingFlags, [MarshalAs(UnmanagedType.Interface)] out ICorDebugProcess ppProcess);
		void DebugActiveProcessEx([MarshalAs(UnmanagedType.Interface)] [In] ICorDebugRemoteTarget pRemoteTarget, [In] uint dwProcessId, [In] int fWin32Attach, [MarshalAs(UnmanagedType.Interface)] out ICorDebugProcess ppProcess);
	}
	[Guid("C3ED8383-5A49-4CF5-B4B7-01864D9E582D"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[ComImport]
	public interface ICorDebugRemoteTarget {
		void GetHostName([In] uint cchHostName, out uint pcchHostName, [MarshalAs(UnmanagedType.Interface)] [Out] ICorDebugRemoteTarget szHostName);
	}
	[Guid("879CAC0A-4A53-4668-B8E3-CB8473CB187F"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[ComImport]
	public interface ICorDebugRuntimeUnwindableFrame : ICorDebugFrame {
		void GetChain([MarshalAs(UnmanagedType.Interface)] out ICorDebugChain ppChain);
		void GetCode([MarshalAs(UnmanagedType.Interface)] out ICorDebugCode ppCode);
		void GetFunction([MarshalAs(UnmanagedType.Interface)] out ICorDebugFunction ppFunction);
		void GetFunctionToken(out uint pToken);
		void GetStackRange(out ulong pStart, out ulong pEnd);
		void GetCaller([MarshalAs(UnmanagedType.Interface)] out ICorDebugFrame ppFrame);
		void GetCallee([MarshalAs(UnmanagedType.Interface)] out ICorDebugFrame ppFrame);
		void CreateStepper([MarshalAs(UnmanagedType.Interface)] out ICorDebugStepper ppStepper);
	}
	[Guid("A0647DE9-55DE-4816-929C-385271C64CF7"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[ComImport]
	public interface ICorDebugStackWalk {
		void GetContext([In] uint contextFlags, [In] uint contextBufSize, out uint contextSize, out byte contextBuf);
		void SetContext([In] CorDebugSetContextFlag flag, [In] uint contextSize, [In] ref byte context);
		void Next();
		void GetFrame([MarshalAs(UnmanagedType.Interface)] out ICorDebugFrame pFrame);
	}
	[Guid("CBF9DA63-F68D-4BBB-A21C-15A45EAADF5B"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[ComImport]
	public interface ICorDebugStaticFieldSymbol {
		void GetName([In] uint cchName, out uint pcchName, [Out] [MarshalAs(UnmanagedType.LPWStr)] StringBuilder szName);
		void GetSize(out uint pcbSize);
		void GetAddress(out ulong pRVA);
	}
	[Guid("CC7BCAEC-8A68-11D2-983C-0000F808342D"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[ComImport]
	public interface ICorDebugStepper {
		void IsActive(out int pbActive);
		void Deactivate();
		[PreserveSig]
		int SetInterceptMask([In] CorDebugIntercept mask);
		[PreserveSig]
		int SetUnmappedStopMask([In] CorDebugUnmappedStop mask);
		[PreserveSig]
		int Step([In] int bStepIn);
		[PreserveSig]
		int StepRange([In] int bStepIn, [In] IntPtr ranges, [In] uint cRangeCount);
		[PreserveSig]
		int StepOut();
		void SetRangeIL([In] int bIL);
	}
	[Guid("C5B6E9C3-E7D1-4A8E-873B-7F047F0706F7"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[ComImport]
	public interface ICorDebugStepper2 {
		[PreserveSig]
		int SetJMC([In] int fIsJMCStepper);
	}
	[Guid("CC7BCB04-8A68-11D2-983C-0000F808342D"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[ComImport]
	public interface ICorDebugStepperEnum : ICorDebugEnum {
		void Skip([In] uint celt);
		void Reset();
		void Clone([MarshalAs(UnmanagedType.Interface)] out ICorDebugEnum ppEnum);
		void GetCount(out uint pcelt);
		void Next([In] uint celt, [MarshalAs(UnmanagedType.Interface)] [Out] ICorDebugStepperEnum steppers, out uint pceltFetched);
	}
	[Guid("CC7BCAFD-8A68-11D2-983C-0000F808342D"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[ComImport]
	public interface ICorDebugStringValue : ICorDebugHeapValue {
		void GetType(out uint pType);
		void GetSize(out uint pSize);
		void GetAddress(out ulong pAddress);
		void CreateBreakpoint([MarshalAs(UnmanagedType.Interface)] out ICorDebugValueBreakpoint ppBreakpoint);
		void IsValid(out int pbValid);
		void CreateRelocBreakpoint([MarshalAs(UnmanagedType.Interface)] out ICorDebugValueBreakpoint ppBreakpoint);
		void GetLength(out uint pcchString);
		void GetString([In] uint cchString, out uint pcchString, [MarshalAs(UnmanagedType.Interface)] [Out] ICorDebugStringValue szString);
	}
	[Guid("3948A999-FD8A-4C38-A708-8A71E9B04DBB"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[ComImport]
	public interface ICorDebugSymbolProvider {
		void GetStaticFieldSymbols([In] uint cbSignature, [In] ref byte typeSig, [In] uint cRequestedSymbols, out uint pcFetchedSymbols, [MarshalAs(UnmanagedType.Interface)] [Out] ICorDebugSymbolProvider pSymbols);
		void GetInstanceFieldSymbols([In] uint cbSignature, [In] ref byte typeSig, [In] uint cRequestedSymbols, out uint pcFetchedSymbols, [MarshalAs(UnmanagedType.Interface)] [Out] ICorDebugSymbolProvider pSymbols);
		void GetMethodLocalSymbols([In] uint nativeRVA, [In] uint cRequestedSymbols, out uint pcFetchedSymbols, [MarshalAs(UnmanagedType.Interface)] [Out] ICorDebugSymbolProvider pSymbols);
		void GetMethodParameterSymbols([In] uint nativeRVA, [In] uint cRequestedSymbols, out uint pcFetchedSymbols, [MarshalAs(UnmanagedType.Interface)] [Out] ICorDebugSymbolProvider pSymbols);
		void GetMergedAssemblyRecords([In] uint cRequestedRecords, out uint pcFetchedRecords, [MarshalAs(UnmanagedType.Interface)] [Out] ICorDebugSymbolProvider pRecords);
		void GetMethodProps([In] uint codeRva, out uint pMethodToken, out uint pcGenericParams, [In] uint cbSignature, out uint pcbSignature, [MarshalAs(UnmanagedType.Interface)] [Out] ICorDebugSymbolProvider signature);
		void GetTypeProps([In] uint vtableRva, [In] uint cbSignature, out uint pcbSignature, [MarshalAs(UnmanagedType.Interface)] [Out] ICorDebugSymbolProvider signature);
		void GetCodeRange([In] uint codeRva, out uint pCodeStartAddress, ref uint pCodeSize);
		void GetAssemblyImageBytes([In] ulong rva, [In] uint length, [MarshalAs(UnmanagedType.Interface)] out ICorDebugMemoryBuffer ppMemoryBuffer);
		void GetObjectSize([In] uint cbSignature, [In] ref byte typeSig, out uint pObjectSize);
		void GetAssemblyImageMetadata([MarshalAs(UnmanagedType.Interface)] out ICorDebugMemoryBuffer ppMemoryBuffer);
	}
	[Guid("F9801807-4764-4330-9E67-4F685094165E"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[ComImport]
	public interface ICorDebugSymbolProvider2 {
		void GetGenericDictionaryInfo([MarshalAs(UnmanagedType.Interface)] out ICorDebugMemoryBuffer ppMemoryBuffer);
		void GetFrameProps([In] uint codeRva, out uint pCodeStartRva, out uint pParentFrameStartRva);
	}
	[Guid("938C6D66-7FB6-4F69-B389-425B8987329B"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[ComImport]
	public interface ICorDebugThread {
		[PreserveSig]
		int GetProcess([MarshalAs(UnmanagedType.Interface)] out ICorDebugProcess ppProcess);
		[PreserveSig]
		int GetID(out int pdwThreadId);
		void GetHandle(out UIntPtr phThreadHandle);
		[PreserveSig]
		int GetAppDomain([MarshalAs(UnmanagedType.Interface)] out ICorDebugAppDomain ppAppDomain);
		void SetDebugState([In] CorDebugThreadState state);
		void GetDebugState(out CorDebugThreadState pState);
		void GetUserState(out CorDebugUserState pState);
		void GetCurrentException([MarshalAs(UnmanagedType.Interface)] out ICorDebugValue ppExceptionObject);
		void ClearCurrentException();
		[PreserveSig]
		int CreateStepper([MarshalAs(UnmanagedType.Interface)] out ICorDebugStepper ppStepper);
		void EnumerateChains([MarshalAs(UnmanagedType.Interface)] out ICorDebugChainEnum ppChains);
		void GetActiveChain([MarshalAs(UnmanagedType.Interface)] out ICorDebugChain ppChain);
		void GetActiveFrame([MarshalAs(UnmanagedType.Interface)] out ICorDebugFrame ppFrame);
		void GetRegisterSet([MarshalAs(UnmanagedType.Interface)] out ICorDebugRegisterSet ppRegisters);
		void CreateEval([MarshalAs(UnmanagedType.Interface)] out ICorDebugEval ppEval);
		void GetObject([MarshalAs(UnmanagedType.Interface)] out ICorDebugValue ppObject);
	}
	[Guid("2BD956D9-7B07-4BEF-8A98-12AA862417C5"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[ComImport]
	public interface ICorDebugThread2 {
		void GetActiveFunctions([In] uint cFunctions, out uint pcFunctions, [MarshalAs(UnmanagedType.Interface)] [In] [Out] ICorDebugThread2 pFunctions);
		void GetConnectionID(out uint pdwConnectionId);
		void GetTaskID(out ulong pTaskId);
		[PreserveSig]
		int GetVolatileOSThreadID(out int pdwTid);
		void InterceptCurrentException([MarshalAs(UnmanagedType.Interface)] [In] ICorDebugFrame pFrame);
	}
	[Guid("F8544EC3-5E4E-46C7-8D3E-A52B8405B1F5"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[ComImport]
	public interface ICorDebugThread3 {
		void CreateStackWalk([MarshalAs(UnmanagedType.Interface)] out ICorDebugStackWalk ppStackWalk);
		void GetActiveInternalFrames([In] uint cInternalFrames, out uint pcInternalFrames, [MarshalAs(UnmanagedType.Interface)] [In] [Out] ICorDebugThread3 ppInternalFrames);
	}
	[Guid("1A1F204B-1C66-4637-823F-3EE6C744A69C"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[ComImport]
	public interface ICorDebugThread4 {
		void HasUnhandledException();
		void GetBlockingObjects([MarshalAs(UnmanagedType.Interface)] out ICorDebugBlockingObjectEnum ppBlockingObjectEnum);
		void GetCurrentCustomDebuggerNotification([MarshalAs(UnmanagedType.Interface)] out ICorDebugValue ppNotificationObject);
	}
	[Guid("CC7BCB06-8A68-11D2-983C-0000F808342D"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[ComImport]
	public interface ICorDebugThreadEnum : ICorDebugEnum {
		void Skip([In] uint celt);
		void Reset();
		void Clone([MarshalAs(UnmanagedType.Interface)] out ICorDebugEnum ppEnum);
		void GetCount(out uint pcelt);
		void Next([In] uint celt, [MarshalAs(UnmanagedType.Interface)] [Out] ICorDebugThreadEnum threads, out uint pceltFetched);
	}
	[Guid("D613F0BB-ACE1-4C19-BD72-E4C08D5DA7F5"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[ComImport]
	public interface ICorDebugType {
		void GetType(out uint ty);
		void GetClass([MarshalAs(UnmanagedType.Interface)] out ICorDebugClass ppClass);
		void EnumerateTypeParameters([MarshalAs(UnmanagedType.Interface)] out ICorDebugTypeEnum ppTyParEnum);
		void GetFirstTypeParameter([MarshalAs(UnmanagedType.Interface)] out ICorDebugType value);
		void GetBase([MarshalAs(UnmanagedType.Interface)] out ICorDebugType pBase);
		void GetStaticFieldValue([In] uint fieldDef, [MarshalAs(UnmanagedType.Interface)] [In] ICorDebugFrame pFrame, [MarshalAs(UnmanagedType.Interface)] out ICorDebugValue ppValue);
		void GetRank(out uint pnRank);
	}
	[Guid("10F27499-9DF2-43CE-8333-A321D7C99CB4"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[ComImport]
	public interface ICorDebugTypeEnum : ICorDebugEnum {
		void Skip([In] uint celt);
		void Reset();
		void Clone([MarshalAs(UnmanagedType.Interface)] out ICorDebugEnum ppEnum);
		void GetCount(out uint pcelt);
		void Next([In] uint celt, [MarshalAs(UnmanagedType.Interface)] [Out] ICorDebugTypeEnum values, out uint pceltFetched);
	}
	[Guid("5263E909-8CB5-11D3-BD2F-0000F80849BD"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[ComImport]
	public interface ICorDebugUnmanagedCallback {
		void DebugEvent([In] UIntPtr pDebugEvent, [In] int fOutOfBand);
	}
	[Guid("CC7BCAF7-8A68-11D2-983C-0000F808342D"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[ComImport]
	public interface ICorDebugValue {
		void GetType(out uint pType);
		void GetSize(out uint pSize);
		void GetAddress(out ulong pAddress);
		void CreateBreakpoint([MarshalAs(UnmanagedType.Interface)] out ICorDebugValueBreakpoint ppBreakpoint);
	}
	[Guid("5E0B54E7-D88A-4626-9420-A691E0A78B49"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[ComImport]
	public interface ICorDebugValue2 {
		void GetExactType([MarshalAs(UnmanagedType.Interface)] out ICorDebugType ppType);
	}
	[Guid("565005FC-0F8A-4F3E-9EDB-83102B156595"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[ComImport]
	public interface ICorDebugValue3 {
		void GetSize64(out ulong pSize);
	}
	[Guid("CC7BCAEB-8A68-11D2-983C-0000F808342D"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[ComImport]
	public interface ICorDebugValueBreakpoint : ICorDebugBreakpoint {
		void Activate([In] int bActive);
		void IsActive(out int pbActive);
		void GetValue([MarshalAs(UnmanagedType.Interface)] out ICorDebugValue ppValue);
	}
	[Guid("CC7BCB0A-8A68-11D2-983C-0000F808342D"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[ComImport]
	public interface ICorDebugValueEnum : ICorDebugEnum {
		void Skip([In] uint celt);
		void Reset();
		void Clone([MarshalAs(UnmanagedType.Interface)] out ICorDebugEnum ppEnum);
		void GetCount(out uint pcelt);
		void Next([In] uint celt, [MarshalAs(UnmanagedType.Interface)] [Out] ICorDebugValueEnum values, out uint pceltFetched);
	}
	[Guid("707E8932-1163-48D9-8A93-F5B1F480FBB7"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[ComImport]
	public interface ICorDebugVariableSymbol {
		void GetName([In] uint cchName, out uint pcchName, [Out] [MarshalAs(UnmanagedType.LPWStr)] StringBuilder szName);
		void GetSize(out uint pcbValue);
		void GetValue([In] uint offset, [In] uint cbContext, [In] ref byte context, [In] uint cbValue, out uint pcbValue, [MarshalAs(UnmanagedType.Interface)] [Out] ICorDebugVariableSymbol pValue);
		void SetValue([In] uint offset, [In] uint threadID, [In] uint cbContext, [In] ref byte context, [In] uint cbValue, [In] ref byte pValue);
		void GetSlotIndex(out uint pSlotIndex);
	}
	[Guid("F69126B7-C787-4F6B-AE96-A569786FC670"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[ComImport]
	public interface ICorDebugVirtualUnwinder {
		void GetContext([In] uint contextFlags, [In] uint cbContextBuf, out uint contextSize, out byte contextBuf);
		void Next();
	}
	public enum ILCodeKind {
		ILCODE_ORIGINAL_IL = 1,
		ILCODE_REJIT_IL
	}
	[Guid("0C733A30-2A1C-11CE-ADE5-00AA0044773D"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[ComImport]
	public interface ISequentialStream {
		void RemoteRead(out byte pv, [In] uint cb, out uint pcbRead);
		void RemoteWrite([In] ref byte pv, [In] uint cb, out uint pcbWritten);
	}
	[Guid("0000000C-0000-0000-C000-000000000046"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[ComImport]
	public interface IStream : ISequentialStream {
		void RemoteRead(out byte pv, [In] uint cb, out uint pcbRead);
		void RemoteWrite([In] ref byte pv, [In] uint cb, out uint pcbWritten);
		void RemoteSeek([In] _LARGE_INTEGER dlibMove, [In] uint dwOrigin, out _ULARGE_INTEGER plibNewPosition);
		void SetSize([In] _ULARGE_INTEGER libNewSize);
		void RemoteCopyTo([MarshalAs(UnmanagedType.Interface)] [In] IStream pstm, [In] _ULARGE_INTEGER cb, out _ULARGE_INTEGER pcbRead, out _ULARGE_INTEGER pcbWritten);
		void Commit([In] uint grfCommitFlags);
		void Revert();
		void LockRegion([In] _ULARGE_INTEGER libOffset, [In] _ULARGE_INTEGER cb, [In] uint dwLockType);
		void UnlockRegion([In] _ULARGE_INTEGER libOffset, [In] _ULARGE_INTEGER cb, [In] uint dwLockType);
		void Stat(out tagSTATSTG pstatstg, [In] uint grfStatFlag);
		void Clone([MarshalAs(UnmanagedType.Interface)] out IStream ppstm);
	}
	public struct tagSTATSTG {
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
	public enum WriteableMetadataUpdateMode {
		LegacyCompatPolicy,
		AlwaysShowUpdates
	}
}
#pragma warning restore 0108 // Member hides inherited member; missing new keyword
