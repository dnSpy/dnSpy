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
using System.Diagnostics;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.DotNet.Code;
using dnSpy.Contracts.Debugger.Engine.CallStack;
using dnSpy.Debugger.DotNet.Mono.Impl;
using dnSpy.Debugger.DotNet.Mono.Properties;
using Mono.Debugger.Soft;
using MDS = Mono.Debugger.Soft;

namespace dnSpy.Debugger.DotNet.Mono.CallStack {
	sealed class DbgEngineStackWalkerImpl : DbgEngineStackWalker {
		readonly Lazy<DbgDotNetCodeLocationFactory> dbgDotNetCodeLocationFactory;
		readonly DbgEngineImpl engine;
		readonly ThreadMirror monoThread;
		readonly DbgThread thread;
		readonly uint continueCounter;
		MDS.StackFrame[]? frames;
		int frameIndex;

		public DbgEngineStackWalkerImpl(Lazy<DbgDotNetCodeLocationFactory> dbgDotNetCodeLocationFactory, DbgEngineImpl engine, ThreadMirror monoThread, DbgThread thread) {
			this.dbgDotNetCodeLocationFactory = dbgDotNetCodeLocationFactory ?? throw new ArgumentNullException(nameof(dbgDotNetCodeLocationFactory));
			this.engine = engine ?? throw new ArgumentNullException(nameof(engine));
			this.monoThread = monoThread ?? throw new ArgumentNullException(nameof(monoThread));
			this.thread = thread ?? throw new ArgumentNullException(nameof(thread));
			continueCounter = engine.ContinueCounter;
		}

		public override DbgEngineStackFrame[] GetNextStackFrames(int maxFrames) {
			if (engine.DebuggerThread.CheckAccess())
				return GetNextStackFrames_MonoDebug(maxFrames);
			return GetNextStackFrames2(maxFrames);

			DbgEngineStackFrame[] GetNextStackFrames2(int maxFrames2) =>
				engine.DebuggerThread.Invoke(() => GetNextStackFrames_MonoDebug(maxFrames2));
		}

		DbgEngineStackFrame[] GetNextStackFrames_MonoDebug(int maxFrames) {
			engine.DebuggerThread.VerifyAccess();
			if (!engine.IsPaused || thread.IsClosed || continueCounter != engine.ContinueCounter)
				return Array.Empty<DbgEngineStackFrame>();
			try {
				if (frames is null)
					frames = monoThread.GetFrames();
				var list = engine.stackFrameData.DbgEngineStackFrameList;
				try {
					while (list.Count < maxFrames && frameIndex < frames.Length) {
						list.Add(CreateEngineStackFrame(frames[frameIndex], frameIndex));
						frameIndex++;
					}
					return list.Count == 0 ? Array.Empty<DbgEngineStackFrame>() : list.ToArray();
				}
				catch {
					engine.DbgRuntime.Process.DbgManager.Close(list.ToArray());
					throw;
				}
				finally {
					list.Clear();
				}
			}
			catch (VMDisconnectedException) {
				return Array.Empty<DbgEngineStackFrame>();
			}
			catch {
				return Array.Empty<DbgEngineStackFrame>();
			}
		}

		DbgEngineStackFrame CreateEngineStackFrame(MDS.StackFrame monoFrame, int frameIndex) {
			engine.DebuggerThread.VerifyAccess();
			var method = monoFrame.Method;
			if (method is null) {
				if (monoFrame.IsDebuggerInvoke)
					return engine.ObjectFactory.CreateSpecialStackFrame(dnSpy_Debugger_DotNet_Mono_Resources.StackFrame_FunctionEvaluation);
				if (monoFrame.IsNativeTransition)
					return engine.ObjectFactory.CreateSpecialStackFrame(dnSpy_Debugger_DotNet_Mono_Resources.StackFrame_NativeTransition);

				Debug.Fail("Unknown frame without a method");
				return CreateErrorStackFrame();
			}
			else {
				var module = engine.TryGetModule(method.DeclaringType.Module);
				if (module is not null)
					return new ILDbgEngineStackFrame(engine, module, monoThread, monoFrame, frameIndex, dbgDotNetCodeLocationFactory);

				Debug.Fail("Creating an error stack frame");
				return CreateErrorStackFrame();
			}
		}

		DbgEngineStackFrame CreateErrorStackFrame() => engine.ObjectFactory.CreateSpecialStackFrame("???");

		protected override void CloseCore(DbgDispatcher dispatcher) { }
	}
}
