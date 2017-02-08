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
using System.Collections.Generic;
using System.Linq;
using dndbg.COM.CorDebug;
using dnSpy.Contracts.Scripting;
using dnSpy.Contracts.Scripting.Debugger;
using DBG = dndbg.Engine;

namespace dnSpy.Debugger.Scripting {
	sealed class DebuggerThread : IDebuggerThread {
		public IAppDomain AppDomain => debugger.Dispatcher.UI(() => {
			var ad = thread.AppDomainOrNull;
			return ad == null ? null : new DebuggerAppDomain(debugger, ad);
		});

		public IEnumerable<IStackFrame> Frames => debugger.Dispatcher.UIIter(GetFramesUI);

		IEnumerable<IStackFrame> GetFramesUI() {
			int frameNo = 0;
			foreach (var f in thread.AllFrames)
				yield return new StackFrame(debugger, f, frameNo++);
		}

		public IStackFrame ActiveFrame => debugger.Dispatcher.UI(() => {
			var frame = thread.AllFrames.FirstOrDefault();
			return frame == null ? null : new StackFrame(debugger, frame, 0);
		});

		public IStackFrame ActiveILFrame => debugger.Dispatcher.UI(() => {
			int frameNo = 0;
			foreach (var f in thread.AllFrames) {
				if (f.IsILFrame)
					return new StackFrame(debugger, f, frameNo);
				frameNo++;
			}
			return null;
		});

		public bool HasExited => debugger.Dispatcher.UI(() => thread.HasExited);
		public int UniqueId => uniqueId;
		public int ThreadId => debugger.Dispatcher.UI(() => thread.ThreadId);
		public int VolatileThreadId => debugger.Dispatcher.UI(() => thread.VolatileThreadId);
		public IntPtr Handle => debugger.Dispatcher.UI(() => thread.CorThread.Handle);

		public bool IsRunning {
			get { return debugger.Dispatcher.UI(() => thread.CorThread.IsRunning); }
			set { debugger.Dispatcher.UI(() => thread.CorThread.State = value ? CorDebugThreadState.THREAD_RUN : CorDebugThreadState.THREAD_SUSPEND); }
		}

		public bool IsSuspended {
			get { return debugger.Dispatcher.UI(() => thread.CorThread.IsSuspended); }
			set { debugger.Dispatcher.UI(() => thread.CorThread.State = !value ? CorDebugThreadState.THREAD_RUN : CorDebugThreadState.THREAD_SUSPEND); }
		}

		public ThreadState State => debugger.Dispatcher.UI(() => (ThreadState)thread.CorThread.State);
		public bool StopRequested => debugger.Dispatcher.UI(() => thread.CorThread.StopRequested);
		public bool SuspendRequested => debugger.Dispatcher.UI(() => thread.CorThread.SuspendRequested);
		public bool IsBackground => debugger.Dispatcher.UI(() => thread.CorThread.IsBackground);
		public bool IsUnstarted => debugger.Dispatcher.UI(() => thread.CorThread.IsUnstarted);
		public bool IsStopped => debugger.Dispatcher.UI(() => thread.CorThread.IsStopped);
		public bool IsWaitSleepJoin => debugger.Dispatcher.UI(() => thread.CorThread.IsWaitSleepJoin);
		public bool IsUserStateSuspended => debugger.Dispatcher.UI(() => thread.CorThread.IsUserStateSuspended);
		public bool IsUnsafePoint => debugger.Dispatcher.UI(() => thread.CorThread.IsUnsafePoint);
		public bool IsThreadPool => debugger.Dispatcher.UI(() => thread.CorThread.IsThreadPool);
		public ThreadUserState UserState => debugger.Dispatcher.UI(() => (ThreadUserState)thread.CorThread.UserState);

		public IDebuggerValue Object => debugger.Dispatcher.UI(() => {
			var value = thread.CorThread.Object;
			return value == null ? null : new DebuggerValue(debugger, value);
		});

		public IDebuggerValue CurrentException => debugger.Dispatcher.UI(() => {
			var value = thread.CorThread.CurrentException;
			return value == null ? null : new DebuggerValue(debugger, value);
		});

		public IStackChain ActiveChain => debugger.Dispatcher.UI(() => {
			var chain = thread.CorThread.ActiveChain;
			return chain == null ? null : new StackChain(debugger, chain);
		});

		public IEnumerable<IStackChain> Chains => debugger.Dispatcher.UIIter(GetChainsUI);

		IEnumerable<IStackChain> GetChainsUI() {
			foreach (var c in thread.CorThread.Chains)
				yield return new StackChain(debugger, c);
		}

		readonly Debugger debugger;
		readonly int hashCode;
		readonly int uniqueId;

		public DBG.DnThread DnThread => thread;
		readonly DBG.DnThread thread;

		public DebuggerThread(Debugger debugger, DBG.DnThread thread) {
			debugger.Dispatcher.VerifyAccess();
			this.debugger = debugger;
			this.thread = thread;
			uniqueId = thread.UniqueId;
			hashCode = thread.GetHashCode();
		}

		public bool InterceptCurrentException(IStackFrame frame) => debugger.Dispatcher.UI(() => thread.CorThread.InterceptCurrentException(((StackFrame)frame).CorFrame));

		IAppDomain GetAppDomainUIThrow() {
			debugger.Dispatcher.VerifyAccess();
			var appDomain = AppDomain;
			if (appDomain == null)
				throw new InvalidOperationException("The thread doesn't have an AppDomain");
			return appDomain;
		}

		IDebuggerMethod FindMethodUIThrow(string modName, string className, string methodName) {
			var method = GetAppDomainUIThrow().GetMethod(modName, className, methodName);
			if (method == null)
				throw new ArgumentException(string.Format("Couldn't find method [{0}] {1}::{2}", modName, className, methodName));
			return method;
		}

		IDebuggerMethod FindMethodUIThrow(string modName, uint token) {
			var method = GetAppDomainUIThrow().GetMethod(modName, token);
			if (method == null)
				throw new ArgumentException(string.Format("Couldn't find method [{0}] 0x{1:X8}", modName, token));
			return method;
		}

		public IDebuggerValue CreateBox(object value) => debugger.Dispatcher.UI(() => {
			using (var eval = debugger.CreateEvalUI(this))
				return eval.CreateBox(value);
		});

		public IDebuggerValue CreateBox(IDebuggerType type) => debugger.Dispatcher.UI(() => {
			using (var eval = debugger.CreateEvalUI(this))
				return eval.CreateBox(type);
		});

		public IDebuggerValue CreateBox(Type type) => debugger.Dispatcher.UI(() => {
			using (var eval = debugger.CreateEvalUI(this))
				return eval.CreateBox(type);
		});

		public IDebuggerValue Create(object value) => debugger.Dispatcher.UI(() => {
			using (var eval = debugger.CreateEvalUI(this))
				return eval.Create(value);
		});

		public IDebuggerValue Create(IDebuggerType type) => debugger.Dispatcher.UI(() => {
			using (var eval = debugger.CreateEvalUI(this))
				return eval.Create(type);
		});

		public IDebuggerValue Create(Type type) => debugger.Dispatcher.UI(() => {
			using (var eval = debugger.CreateEvalUI(this))
				return eval.Create(type);
		});

		public IDebuggerValue CreateArray(IDebuggerType elementType, int length) => debugger.Dispatcher.UI(() => {
			using (var eval = debugger.CreateEvalUI(this))
				return eval.CreateArray(elementType, length);
		});

		public IDebuggerValue CreateArray(Type elementType, int length) => debugger.Dispatcher.UI(() => {
			using (var eval = debugger.CreateEvalUI(this))
				return eval.CreateArray(elementType, length);
		});

		public IDebuggerValue Create(IDebuggerMethod ctor, params object[] args) => debugger.Dispatcher.UI(() => {
			using (var eval = debugger.CreateEvalUI(this))
				return eval.Create(ctor, args);
		});

		public IDebuggerValue Create(object[] genericArgs, IDebuggerMethod ctor, params object[] args) => debugger.Dispatcher.UI(() => {
			using (var eval = debugger.CreateEvalUI(this))
				return eval.Create(genericArgs, ctor, args);
		});

		public IDebuggerValue Call(IDebuggerMethod method, params object[] args) => debugger.Dispatcher.UI(() => {
			using (var eval = debugger.CreateEvalUI(this))
				return eval.Call(method, args);
		});

		public IDebuggerValue Call(string modName, string className, string methodName, params object[] args) => debugger.Dispatcher.UI(() => {
			using (var eval = debugger.CreateEvalUI(this))
				return eval.Call(FindMethodUIThrow(modName, className, methodName), args);
		});

		public IDebuggerValue Call(string modName, uint token, params object[] args) => debugger.Dispatcher.UI(() => {
			using (var eval = debugger.CreateEvalUI(this))
				return eval.Call(FindMethodUIThrow(modName, token), args);
		});

		public IDebuggerValue Call(object[] genericArgs, IDebuggerMethod method, params object[] args) => debugger.Dispatcher.UI(() => {
			using (var eval = debugger.CreateEvalUI(this))
				return eval.Call(genericArgs, method, args);
		});

		public IDebuggerValue Call(object[] genericArgs, string modName, string className, string methodName, params object[] args) => debugger.Dispatcher.UI(() => {
			using (var eval = debugger.CreateEvalUI(this))
				return eval.Call(genericArgs, FindMethodUIThrow(modName, className, methodName), args);
		});

		public IDebuggerValue Call(object[] genericArgs, string modName, uint token, params object[] args) => debugger.Dispatcher.UI(() => {
			using (var eval = debugger.CreateEvalUI(this))
				return eval.Call(genericArgs, FindMethodUIThrow(modName, token), args);
		});

		public IDebuggerValue AssemblyLoad(byte[] rawAssembly) => debugger.Dispatcher.UI(() => {
			using (var eval = debugger.CreateEvalUI(this))
				return eval.AssemblyLoad(rawAssembly);
		});

		public IDebuggerValue AssemblyLoad(string assemblyString) => debugger.Dispatcher.UI(() => {
			using (var eval = debugger.CreateEvalUI(this))
				return eval.AssemblyLoad(assemblyString);
		});

		public IDebuggerValue AssemblyLoadFrom(string assemblyFile) => debugger.Dispatcher.UI(() => {
			using (var eval = debugger.CreateEvalUI(this))
				return eval.AssemblyLoadFrom(assemblyFile);
		});

		public IDebuggerValue AssemblyLoadFile(string filename) => debugger.Dispatcher.UI(() => {
			using (var eval = debugger.CreateEvalUI(this))
				return eval.AssemblyLoadFile(filename);
		});

		public string ToString(IDebuggerValue value) => debugger.Dispatcher.UI(() => {
			using (var eval = debugger.CreateEvalUI(this))
				return eval.ToString(value);
		});

		public IEval CreateEval() => debugger.Dispatcher.UI(() => debugger.CreateEvalUI(this));
		public override bool Equals(object obj) => (obj as DebuggerThread)?.thread == thread;
		public override int GetHashCode() => hashCode;
		public override string ToString() => debugger.Dispatcher.UI(() => thread.ToString());
	}
}
