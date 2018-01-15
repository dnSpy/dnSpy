/*
    Copyright (C) 2014-2017 de4dot@gmail.com

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
using System.Diagnostics;
using System.Threading.Tasks;
using dndbg.Engine;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.DotNet.Code;
using dnSpy.Contracts.Debugger.DotNet.Evaluation;
using dnSpy.Contracts.Debugger.DotNet.Steppers.Engine;
using dnSpy.Debugger.DotNet.CorDebug.Impl;
using dnSpy.Debugger.DotNet.Metadata;
using DNE = dnlib.DotNet.Emit;

namespace dnSpy.Debugger.DotNet.CorDebug.Steppers {
	sealed class DbgDotNetEngineStepperImpl : DbgDotNetEngineStepper {
		public override SessionBase Session {
			get => session;
			set {
				if (session != value) {
					session?.Dispose();
					session = (SessionImpl)value;
				}
			}
		}
		SessionImpl session;

		sealed class DbgDotNetEngineStepperFrameInfoImpl : DbgDotNetEngineStepperFrameInfo {
			// Return values are available since .NET Framework 4.5.1 / .NET Core 1.0
			public override bool SupportsReturnValues => CorFrame.Code?.SupportsReturnValues == true;

			internal CorFrame CorFrame { get; }

			readonly DbgEngineImpl engine;

			public DbgDotNetEngineStepperFrameInfoImpl(DbgEngineImpl engine, CorFrame frame) {
				this.engine = engine ?? throw new ArgumentNullException(nameof(engine));
				CorFrame = frame ?? throw new ArgumentNullException(nameof(frame));
			}

			public override bool TryGetLocation(out DbgModule module, out uint token, out uint offset) {
				engine.VerifyCorDebugThread();
				var func = CorFrame.Function;
				module = engine.TryGetModule(func?.Module);
				token = func?.Token ?? 0;
				var offs = GetILOffset(CorFrame);
				offset = offs ?? 0;
				return module != null && token != 0 && offs != null;
			}

			static uint? GetILOffset(CorFrame frame) {
				var ip = frame.ILFrameIP;
				if (ip.IsExact || ip.IsApproximate)
					return ip.Offset;
				if (ip.IsProlog)
					return DbgDotNetCodeRangeService.PROLOG;
				if (ip.IsEpilog)
					return DbgDotNetCodeRangeService.EPILOG;
				return null;
			}
		}

		sealed class CallSiteInfo {
			public DmdMethodBase Method { get; }
			public DnNativeCodeBreakpoint[] Breakpoints { get; }
			public CallSiteInfo(DmdMethodBase method, DnNativeCodeBreakpoint[] breakpoints) {
				Method = method ?? throw new ArgumentNullException(nameof(method));
				Breakpoints = breakpoints ?? throw new ArgumentNullException(nameof(breakpoints));
			}
		}

		sealed class ReturnValuesCollection {
			public List<DbgDotNetReturnValueInfo> ReturnValues { get; }
			public bool TooManyReturnValues { get; private set; }

			readonly DbgEngineImpl engine;
			readonly int maxReturnValues;
			readonly List<ReturnValueState> rvStates;

			public ReturnValuesCollection(DbgEngineImpl engine, int maxReturnValues) {
				this.engine = engine ?? throw new ArgumentNullException(nameof(engine));
				this.maxReturnValues = maxReturnValues;
				ReturnValues = new List<DbgDotNetReturnValueInfo>();
				rvStates = new List<ReturnValueState>();
			}

			public ReturnValueState CreateReturnValueState(CorThread thread, CorFrame frame) {
				var rvState = new ReturnValueState(thread, frame);
				rvStates.Add(rvState);
				return rvState;
			}

			public void Hit(ReturnValueState rvstate, CorThread corThread, uint offset) {
				if (TooManyReturnValues)
					return;
				if (rvstate.CorThread != corThread)
					return;
				var corFrame = corThread.ActiveFrame;
				Debug.Assert(corFrame != null);
				if (corFrame == null)
					return;
				if (rvstate.StackStart != corFrame.StackStart || rvstate.StackEnd != corFrame.StackEnd || rvstate.Token != corFrame.Token)
					return;
				if (!rvstate.CallSiteInfos.TryGetValue(offset, out var callSiteInfo))
					return;

				CorValue corValue = null;
				DbgDotNetValue dnValue = null;
				bool error = true;
				try {
					corValue = corFrame.GetReturnValueForILOffset(offset);
					if (corValue != null) {
						var reflectionModule = engine.TryGetModule(corFrame.Function?.Module)?.GetReflectionModule();
						Debug.Assert(reflectionModule != null);
						if (reflectionModule != null) {
							if (ReturnValues.Count >= maxReturnValues) {
								TooManyReturnValues = true;
								RemoveAllBreakpoints();
								return;
							}

							// Don't add it to the close on continue list since it will get closed when we continue the
							// stepper. This is not what we want...
							dnValue = engine.CreateDotNetValue_CorDebug(corValue, reflectionModule.AppDomain, tryCreateStrongHandle: true, closeOnContinue: false);
							uint id = (uint)ReturnValues.Count + 1;
							ReturnValues.Add(new DbgDotNetReturnValueInfo(id, callSiteInfo.Method, dnValue));
							error = false;
						}
					}
				}
				finally {
					if (error) {
						engine.DisposeHandle_CorDebug(corValue);
						dnValue?.Dispose();
					}
				}
			}

			public DbgDotNetReturnValueInfo[] TakeOwnershipOfReturnValues() {
				if (ReturnValues.Count == 0)
					return Array.Empty<DbgDotNetReturnValueInfo>();
				var res = ReturnValues.ToArray();
				ReturnValues.Clear();
				return res;
			}

			void RemoveAllBreakpoints() {
				foreach (var rvState in rvStates) {
					foreach (var kv in rvState.CallSiteInfos) {
						foreach (var bp in kv.Value.Breakpoints)
							engine.RemoveNativeBreakpointForGetReturnValue(bp);
					}
					rvState.CallSiteInfos.Clear();
				}
			}

			void ClearReturnValues() {
				foreach (var info in ReturnValues)
					info.Value.Dispose();
				ReturnValues.Clear();
			}

			public void Dispose() {
				RemoveAllBreakpoints();
				ClearReturnValues();
			}
		}

		sealed class ReturnValueState {
			public Dictionary<uint, CallSiteInfo> CallSiteInfos { get; }
			public readonly CorThread CorThread;
			public readonly ulong StackStart;
			public readonly ulong StackEnd;
			public readonly uint Token;

			public ReturnValueState(CorThread corThread, CorFrame corFrame) {
				CorThread = corThread;
				StackStart = corFrame.StackStart;
				StackEnd = corFrame.StackEnd;
				Token = corFrame.Token;
				CallSiteInfos = new Dictionary<uint, CallSiteInfo>();
			}
		}

		sealed class SessionImpl : SessionBase {
			public CorStepper CorStepper { get; set; }
			ReturnValuesCollection returnValuesCollection;

			public SessionImpl(object tag) : base(tag) { }
			public ReturnValuesCollection GetOrCreateReturnValuesCollection(DbgEngineImpl engine, int maxReturnValues) =>
				returnValuesCollection ?? (returnValuesCollection = new ReturnValuesCollection(engine, maxReturnValues));
			public DbgDotNetReturnValueInfo[] TakeOwnershipOfReturnValues() => returnValuesCollection?.TakeOwnershipOfReturnValues() ?? Array.Empty<DbgDotNetReturnValueInfo>();
			public void Dispose() => returnValuesCollection?.Dispose();
		}

		readonly DbgEngineImpl engine;
		readonly DbgThread thread;
		readonly DnThread dnThread;

		public DbgDotNetEngineStepperImpl(DbgEngineImpl engine, DbgThread thread, DnThread dnThread) {
			this.engine = engine ?? throw new ArgumentNullException(nameof(engine));
			this.thread = thread ?? throw new ArgumentNullException(nameof(thread));
			this.dnThread = dnThread ?? throw new ArgumentNullException(nameof(dnThread));
		}

		public override SessionBase CreateSession(object tag) => new SessionImpl(tag);

		public override bool IsRuntimePaused => dnThread.Debugger.ProcessState == DebuggerProcessState.Paused;
		public override uint ContinueCounter => dnThread.Debugger.ContinueCounter;

		public override DbgDotNetEngineStepperFrameInfo TryGetFrameInfo() {
			var frame = GetILFrame();
			if (frame == null)
				return null;
			return new DbgDotNetEngineStepperFrameInfoImpl(engine, frame);
		}

		public override void Continue() => engine.Continue_CorDebug();

		public override Task<DbgThread> StepOutAsync(DbgDotNetEngineStepperFrameInfo frame) {
			engine.VerifyCorDebugThread();
			Debug.Assert(Session != null);
			var frameImpl = (DbgDotNetEngineStepperFrameInfoImpl)frame;
			var dbg = dnThread.Debugger;
			Debug.Assert(dbg.ProcessState == DebuggerProcessState.Paused);
			CorStepper newCorStepper = null;
			var tcs = new TaskCompletionSource<DbgThread>();
			newCorStepper = dbg.StepOut(frameImpl.CorFrame, (_, e, canceled) => {
				if (canceled)
					tcs.SetCanceled();
				else {
					e.AddPauseReason(DebuggerPauseReason.Other);
					tcs.SetResult(engine.TryGetThread(e.CorThread) ?? thread);
				}
			});
			session.CorStepper = newCorStepper;
			engine.Continue_CorDebug();
			return tcs.Task;
		}

		public override Task<DbgThread> StepIntoAsync(DbgDotNetEngineStepperFrameInfo frame, DbgCodeRange[] ranges) {
			engine.VerifyCorDebugThread();
			Debug.Assert(Session != null);
			var frameImpl = (DbgDotNetEngineStepperFrameInfoImpl)frame;
			var dbg = dnThread.Debugger;
			Debug.Assert(dbg.ProcessState == DebuggerProcessState.Paused);
			CorStepper newCorStepper = null;
			var tcs = new TaskCompletionSource<DbgThread>();
			var stepRanges = ToStepRanges(ranges);
			newCorStepper = dbg.StepInto(frameImpl.CorFrame, stepRanges, (_, e, canceled) => {
				if (canceled)
					tcs.SetCanceled();
				else {
					e.AddPauseReason(DebuggerPauseReason.Other);
					tcs.SetResult(engine.TryGetThread(e.CorThread) ?? thread);
				}
			});
			session.CorStepper = newCorStepper;
			engine.Continue_CorDebug();
			return tcs.Task;
		}

		public override Task<DbgThread> StepOverAsync(DbgDotNetEngineStepperFrameInfo frame, DbgCodeRange[] ranges) {
			engine.VerifyCorDebugThread();
			Debug.Assert(Session != null);
			var frameImpl = (DbgDotNetEngineStepperFrameInfoImpl)frame;
			var dbg = dnThread.Debugger;
			Debug.Assert(dbg.ProcessState == DebuggerProcessState.Paused);
			CorStepper newCorStepper = null;
			var tcs = new TaskCompletionSource<DbgThread>();
			var stepRanges = ToStepRanges(ranges);
			newCorStepper = dbg.StepOver(frameImpl.CorFrame, stepRanges, (_, e, canceled) => {
				if (canceled)
					tcs.SetCanceled();
				else {
					e.AddPauseReason(DebuggerPauseReason.Other);
					tcs.SetResult(engine.TryGetThread(e.CorThread) ?? thread);
				}
			});
			session.CorStepper = newCorStepper;
			engine.Continue_CorDebug();
			return tcs.Task;
		}

		public override void OnStepComplete() {
			engine.VerifyCorDebugThread();
			Debug.Assert(Session != null);
			var returnValues = session.TakeOwnershipOfReturnValues() ?? Array.Empty<DbgDotNetReturnValueInfo>();
			engine.SetReturnValues(returnValues);
		}

		public override void CollectReturnValues(DbgDotNetEngineStepperFrameInfo frame, in GetCodeRangeResult result) {
			engine.VerifyCorDebugThread();
			Debug.Assert(Session != null);
			var frameImpl = (DbgDotNetEngineStepperFrameInfoImpl)frame;
			var stmtInstrs = result.StatementInstructions;
			if (stmtInstrs.Length == 0)
				return;
			var code = frameImpl.CorFrame.Code;
			if (code == null)
				return;
			var rvColl = session.GetOrCreateReturnValuesCollection(engine, maxReturnValues);
			var rvState = rvColl.CreateReturnValueState(dnThread.CorThread, frameImpl.CorFrame);
			DmdModule reflectionModule = null;
			IList<DmdType> genericTypeArguments = null;
			IList<DmdType> genericMethodArguments = null;
			var bps = new List<DnNativeCodeBreakpoint>();
			foreach (var instrs in stmtInstrs) {
				for (int i = 0; i < instrs.Length; i++) {
					var instr = instrs[i];
					uint instrOffs = instr.Offset;
					if (instr.OpCode == (ushort)DNE.Code.Tailcall) {
						if (i + 1 < instrs.Length)
							instr = instrs[++i];
					}
					// Newobj isn't supported by the CorDebug API
					bool isCall = instr.OpCode == (ushort)DNE.Code.Call ||
								instr.OpCode == (ushort)DNE.Code.Callvirt;
					if (!isCall)
						continue;
					var liveOffsets = code.GetReturnValueLiveOffset(instrOffs);
					if (liveOffsets.Length == 0)
						continue;
					var method = GetMethod(frameImpl.CorFrame, (int)instr.Operand, ref reflectionModule, ref genericTypeArguments, ref genericMethodArguments);
					Debug.Assert((object)method != null);
					if ((object)method == null)
						continue;
					bps.Clear();
					Action<CorThread> bpHitCallback = bpThread => rvColl.Hit(rvState, bpThread, instrOffs);
					foreach (var liveOffset in liveOffsets)
						bps.Add(engine.CreateNativeBreakpointForGetReturnValue(code, liveOffset, bpHitCallback));
					var callSiteInfo = new CallSiteInfo(method, bps.ToArray());
					rvState.CallSiteInfos.Add(instrOffs, callSiteInfo);
				}
			}
		}

		DmdMethodBase GetMethod(CorFrame frame, int methodMetadataToken, ref DmdModule reflectionModule, ref IList<DmdType> genericTypeArguments, ref IList<DmdType> genericMethodArguments) {
			if (reflectionModule == null) {
				reflectionModule = engine.TryGetModule(frame.Function?.Module)?.GetReflectionModule();
				if (reflectionModule == null)
					return null;
			}

			if (genericTypeArguments == null) {
				if (!frame.GetTypeAndMethodGenericParameters(out var typeGenArgs, out var methGenArgs))
					return null;
				var reflectionAppDomain = reflectionModule.AppDomain;
				genericTypeArguments = Convert(reflectionAppDomain, typeGenArgs);
				genericMethodArguments = Convert(reflectionAppDomain, methGenArgs);
			}

			return reflectionModule.ResolveMethod(methodMetadataToken, genericTypeArguments, genericMethodArguments, DmdResolveOptions.None);
		}

		IList<DmdType> Convert(DmdAppDomain reflectionAppDomain, CorType[] typeArgs) {
			if (typeArgs.Length == 0)
				return Array.Empty<DmdType>();
			var types = new DmdType[typeArgs.Length];
			var reflectionTypeCreator = new ReflectionTypeCreator(engine, reflectionAppDomain);
			for (int i = 0; i < types.Length; i++)
				types[i] = reflectionTypeCreator.Create(typeArgs[i]);
			return types;
		}

		static StepRange[] ToStepRanges(DbgCodeRange[] ranges) {
			var result = new StepRange[ranges.Length];
			for (int i = 0; i < result.Length; i++) {
				var r = ranges[i];
				result[i] = new StepRange(r.Start, r.End);
			}
			return result;
		}

		CorFrame GetILFrame() {
			engine.VerifyCorDebugThread();
			foreach (var frame in dnThread.AllFrames) {
				if (frame.IsILFrame)
					return frame;
			}
			return null;
		}

		public override bool IgnoreException(Exception exception) => false;

		public override void OnCanceled(SessionBase session) {
			engine.VerifyCorDebugThread();
			var stepper = ((SessionImpl)session).CorStepper;
			if (stepper != null)
				dnThread.Debugger.CancelStep(stepper);
		}

		public override void Close(DbgDispatcher dispatcher) { }
	}
}
