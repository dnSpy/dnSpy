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
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using dndbg.COM.CorDebug;
using dndbg.Engine;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.DotNet.Code;
using dnSpy.Contracts.Debugger.Engine.CallStack;
using dnSpy.Debugger.DotNet.CorDebug.Code;
using dnSpy.Debugger.DotNet.CorDebug.Impl;
using dnSpy.Debugger.DotNet.CorDebug.Properties;

namespace dnSpy.Debugger.DotNet.CorDebug.CallStack {
	sealed class DbgEngineStackWalkerImpl : DbgEngineStackWalker {
		readonly Lazy<DbgDotNetNativeCodeLocationFactory> dbgDotNetNativeCodeLocationFactory;
		readonly Lazy<DbgDotNetCodeLocationFactory> dbgDotNetCodeLocationFactory;
		readonly DbgEngineImpl engine;
		readonly DnThread dnThread;
		readonly DbgThread thread;
		ICorDebugFrame[] framesBuffer;
		readonly uint continueCounter;
		IEnumerator<CorFrame> enumerator;

		sealed class EmptyEnumerator<T> : IEnumerator<T> {
			public static readonly IEnumerator<T> Empty = new EmptyEnumerator<T>();
			EmptyEnumerator() { }
			T IEnumerator<T>.Current => default;
			object IEnumerator.Current => default(T);
			bool IEnumerator.MoveNext() => false;
			void IEnumerator.Reset() => throw new NotSupportedException();
			void IDisposable.Dispose() { }
		}

		public DbgEngineStackWalkerImpl(Lazy<DbgDotNetNativeCodeLocationFactory> dbgDotNetNativeCodeLocationFactory, Lazy<DbgDotNetCodeLocationFactory> dbgDotNetCodeLocationFactory, DbgEngineImpl engine, DnThread dnThread, DbgThread thread, ICorDebugFrame[] framesBuffer) {
			this.dbgDotNetNativeCodeLocationFactory = dbgDotNetNativeCodeLocationFactory ?? throw new ArgumentNullException(nameof(dbgDotNetNativeCodeLocationFactory));
			this.dbgDotNetCodeLocationFactory = dbgDotNetCodeLocationFactory ?? throw new ArgumentNullException(nameof(dbgDotNetCodeLocationFactory));
			this.engine = engine ?? throw new ArgumentNullException(nameof(engine));
			this.dnThread = dnThread ?? throw new ArgumentNullException(nameof(dnThread));
			this.thread = thread ?? throw new ArgumentNullException(nameof(thread));
			this.framesBuffer = framesBuffer ?? throw new ArgumentNullException(nameof(framesBuffer));
			continueCounter = dnThread.Debugger.ContinueCounter;
		}

		public override DbgEngineStackFrame[] GetNextStackFrames(int maxFrames) {
			if (engine.DebuggerThread.CheckAccess())
				return GetNextStackFrames_CorDebug(maxFrames);
			return GetNextStackFrames2(maxFrames);

			DbgEngineStackFrame[] GetNextStackFrames2(int maxFrames2) =>
				engine.DebuggerThread.Invoke(() => GetNextStackFrames_CorDebug(maxFrames2));
		}

		DbgEngineStackFrame[] GetNextStackFrames_CorDebug(int maxFrames) {
			engine.DebuggerThread.VerifyAccess();
			if (dnThread.Debugger.ProcessState != DebuggerProcessState.Paused || dnThread.HasExited || thread.IsClosed || continueCounter != dnThread.Debugger.ContinueCounter) {
				enumerator?.Dispose();
				enumerator = EmptyEnumerator<CorFrame>.Empty;
				return Array.Empty<DbgEngineStackFrame>();
			}
			if (enumerator == null)
				enumerator = dnThread.GetAllFrames(framesBuffer).GetEnumerator();
			var list = engine.stackFrameData.DbgEngineStackFrameList;
			try {
				Debug.Assert(list.Count == 0);
				int count = 0;
				while (count++ < maxFrames && enumerator.MoveNext())
					list.Add(CreateEngineStackFrame(enumerator.Current));
				return list.Count == 0 ? Array.Empty<DbgEngineStackFrame>() : list.ToArray();
			}
			finally {
				list.Clear();
			}
		}

		DbgEngineStackFrame CreateEngineStackFrame(CorFrame corFrame) {
			engine.DebuggerThread.VerifyAccess();
			if (corFrame.IsILFrame) {
				var func = corFrame.Function;
				if (func == null)
					return CreateErrorStackFrame();
				var module = engine.TryGetModule(func.Module);
				if (module == null)
					return CreateErrorStackFrame();
				return new ILDbgEngineStackFrame(engine, module, corFrame, dnThread, func, dbgDotNetNativeCodeLocationFactory, dbgDotNetCodeLocationFactory);
			}

			if (corFrame.IsInternalFrame)
				return engine.ObjectFactory.CreateSpecialStackFrame(GetInternalFrameTypeName(corFrame));

			if (corFrame.IsNativeFrame) {
				var name = string.Format(dnSpy_Debugger_DotNet_CorDebug_Resources.StackFrame_NativeFrame, "0x" + corFrame.NativeFrameIP.ToString("X8"));
				return engine.ObjectFactory.CreateSpecialStackFrame(name);
			}

			return CreateErrorStackFrame();
		}

		static string GetInternalFrameTypeName(CorFrame corFrame) {
			Debug.Assert(corFrame.IsInternalFrame);
			switch (corFrame.InternalFrameType) {
			case CorDebugInternalFrameType.STUBFRAME_M2U:
				return dnSpy_Debugger_DotNet_CorDebug_Resources.StackFrame_ManagedToNativeTransition;

			case CorDebugInternalFrameType.STUBFRAME_U2M:
				return dnSpy_Debugger_DotNet_CorDebug_Resources.StackFrame_NativeToManagedTransition;

			case CorDebugInternalFrameType.STUBFRAME_APPDOMAIN_TRANSITION:
				return dnSpy_Debugger_DotNet_CorDebug_Resources.StackFrame_AppdomainTransition;

			case CorDebugInternalFrameType.STUBFRAME_LIGHTWEIGHT_FUNCTION:
				return dnSpy_Debugger_DotNet_CorDebug_Resources.StackFrame_LightweightFunction;

			case CorDebugInternalFrameType.STUBFRAME_FUNC_EVAL:
				return dnSpy_Debugger_DotNet_CorDebug_Resources.StackFrame_FunctionEvaluation;

			case CorDebugInternalFrameType.STUBFRAME_INTERNALCALL:
				return dnSpy_Debugger_DotNet_CorDebug_Resources.StackFrame_InternalCall;

			case CorDebugInternalFrameType.STUBFRAME_CLASS_INIT:
				return dnSpy_Debugger_DotNet_CorDebug_Resources.StackFrame_ClassInit;

			case CorDebugInternalFrameType.STUBFRAME_EXCEPTION:
				return dnSpy_Debugger_DotNet_CorDebug_Resources.StackFrame_Exception;

			case CorDebugInternalFrameType.STUBFRAME_SECURITY:
				return dnSpy_Debugger_DotNet_CorDebug_Resources.StackFrame_Security;

			case CorDebugInternalFrameType.STUBFRAME_JIT_COMPILATION:
				return dnSpy_Debugger_DotNet_CorDebug_Resources.StackFrame_JITCompilation;

			case CorDebugInternalFrameType.STUBFRAME_NONE:
				Debug.Fail("Shouldn't be here");
				goto default;

			default:
				return string.Format(dnSpy_Debugger_DotNet_CorDebug_Resources.StackFrame_InternalFrame, "0x" + ((int)corFrame.InternalFrameType).ToString("X8"));
			}
		}

		DbgEngineStackFrame CreateErrorStackFrame() {
			Debug.Fail("Creating an error stack frame");
			return engine.ObjectFactory.CreateSpecialStackFrame("???");
		}

		protected override void CloseCore(DbgDispatcher dispatcher) {
			if (framesBuffer != null)
				engine.ReturnFramesBuffer(ref framesBuffer);
			framesBuffer = null;
			enumerator?.Dispose();
			enumerator = null;
		}
	}
}
