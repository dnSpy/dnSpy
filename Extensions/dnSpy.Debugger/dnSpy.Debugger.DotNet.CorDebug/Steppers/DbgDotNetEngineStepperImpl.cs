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
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
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
		public override SessionBase? Session {
			get => session;
			set {
				if (session != value) {
					session?.Dispose();
					session = (SessionImpl?)value;
				}
			}
		}
		SessionImpl? session;

		sealed class DbgDotNetEngineStepperFrameInfoImpl : DbgDotNetEngineStepperFrameInfo {
			// Return values are available since .NET Framework 4.5.1 / .NET Core 1.0
			public override bool SupportsReturnValues => CorFrame.Code?.SupportsReturnValues == true;
			public override DbgThread Thread { get; }

			internal CorFrame CorFrame { get; }

			readonly DbgEngineImpl engine;

			public DbgDotNetEngineStepperFrameInfoImpl(DbgEngineImpl engine, DbgThread thread, CorFrame frame) {
				this.engine = engine ?? throw new ArgumentNullException(nameof(engine));
				Thread = thread;
				CorFrame = frame ?? throw new ArgumentNullException(nameof(frame));
			}

			public override bool TryGetLocation([NotNullWhen(true)] out DbgModule? module, out uint token, out uint offset) {
				engine.VerifyCorDebugThread();
				var func = CorFrame.Function;
				module = engine.TryGetModule(func?.Module);
				token = func?.Token ?? 0;
				var offs = GetILOffset(CorFrame);
				offset = offs ?? 0;
				return !(module is null) && token != 0 && !(offs is null);
			}

			public override bool Equals(DbgDotNetEngineStepperFrameInfo other) {
				var otherImpl = (DbgDotNetEngineStepperFrameInfoImpl)other;
				return otherImpl.CorFrame.StackStart == CorFrame.StackStart &&
					otherImpl.CorFrame.StackEnd == CorFrame.StackEnd;
			}

			static uint? GetILOffset(CorFrame frame) {
				var ip = frame.ILFrameIP;
				if (ip.IsExact || ip.IsApproximate)
					return ip.Offset;
				if (ip.IsProlog)
					return DbgDotNetInstructionOffsetConstants.PROLOG;
				if (ip.IsEpilog)
					return DbgDotNetInstructionOffsetConstants.EPILOG;
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

			public void Hit(ReturnValueState rvState, CorThread? corThread, uint offset) {
				if (TooManyReturnValues)
					return;
				if (!rvState.CorThread.Equals(corThread))
					return;
				Debug2.Assert(!(corThread is null));
				var corFrame = corThread.ActiveFrame;
				Debug2.Assert(!(corFrame is null));
				if (corFrame is null)
					return;
				if (rvState.StackStart != corFrame.StackStart || rvState.StackEnd != corFrame.StackEnd || rvState.Token != corFrame.Token)
					return;
				if (!rvState.CallSiteInfos.TryGetValue(offset, out var callSiteInfo))
					return;

				CorValue? corValue = null;
				DbgDotNetValue? dnValue = null;
				bool error = true;
				try {
					corValue = corFrame.GetReturnValueForILOffset(offset);
					if (!(corValue is null)) {
						var reflectionModule = engine.TryGetModule(corFrame.Function?.Module)?.GetReflectionModule();
						Debug2.Assert(!(reflectionModule is null));
						if (!(reflectionModule is null)) {
							if (ReturnValues.Count >= maxReturnValues) {
								TooManyReturnValues = true;
								RemoveAllBreakpointsCore();
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

			public void ClearReturnValues() {
				RemoveAllBreakpointsCore();
				foreach (var info in TakeOwnershipOfReturnValues())
					info.Value.Dispose();
			}

			void RemoveAllBreakpointsCore() {
				foreach (var rvState in rvStates) {
					foreach (var kv in rvState.CallSiteInfos) {
						foreach (var bp in kv.Value.Breakpoints)
							engine.RemoveNativeBreakpointForGetReturnValue(bp);
					}
					rvState.CallSiteInfos.Clear();
				}
			}

			void ClearReturnValuesCore() {
				foreach (var info in ReturnValues)
					info.Value.Dispose();
				ReturnValues.Clear();
			}

			public void Dispose() {
				RemoveAllBreakpointsCore();
				ClearReturnValuesCore();
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
			public CorStepper? CorStepper { get; set; }
			ReturnValuesCollection? returnValuesCollection;

			public SessionImpl(object? tag) : base(tag) { }
			public ReturnValuesCollection GetOrCreateReturnValuesCollection(DbgEngineImpl engine, int maxReturnValues) =>
				returnValuesCollection ??= new ReturnValuesCollection(engine, maxReturnValues);
			public void ClearReturnValues() => returnValuesCollection?.ClearReturnValues();
			public DbgDotNetReturnValueInfo[] TakeOwnershipOfReturnValues() => returnValuesCollection?.TakeOwnershipOfReturnValues() ?? Array.Empty<DbgDotNetReturnValueInfo>();
			public void Dispose() => returnValuesCollection?.Dispose();
		}

		readonly DbgEngineImpl engine;
		readonly DnDebugger dnDebugger;

		public DbgDotNetEngineStepperImpl(DbgEngineImpl engine, DnDebugger dnDebugger) {
			this.engine = engine ?? throw new ArgumentNullException(nameof(engine));
			this.dnDebugger = dnDebugger ?? throw new ArgumentNullException(nameof(dnDebugger));
		}

		public override SessionBase CreateSession(object? tag) => new SessionImpl(tag);

		public override bool IsRuntimePaused => dnDebugger.ProcessState == DebuggerProcessState.Paused;
		public override uint ContinueCounter => dnDebugger.ContinueCounter;

		public override DbgDotNetEngineStepperFrameInfo? TryGetFrameInfo(DbgThread thread) {
			var frame = GetILFrame(thread);
			if (frame is null)
				return null;
			return new DbgDotNetEngineStepperFrameInfoImpl(engine, thread, frame);
		}

		public override void Continue() => engine.Continue_CorDebug();

		public override Task<DbgThread> StepOutAsync(DbgDotNetEngineStepperFrameInfo frame) {
			engine.VerifyCorDebugThread();
			Debug2.Assert(!(session is null));
			var frameImpl = (DbgDotNetEngineStepperFrameInfoImpl)frame;
			Debug.Assert(dnDebugger.ProcessState == DebuggerProcessState.Paused);
			CorStepper? newCorStepper = null;
			var tcs = new TaskCompletionSource<DbgThread>();
			newCorStepper = dnDebugger.StepOut(frameImpl.CorFrame, (_, e, canceled) => {
				if (canceled)
					tcs.SetCanceled();
				else {
					Debug2.Assert(!(e is null));
					e.AddPauseReason(DebuggerPauseReason.Other);
					var thread = engine.TryGetThread(e.CorThread);
					if (!(thread is null))
						tcs.SetResult(thread);
					else
						tcs.SetException(new InvalidOperationException());
				}
			});
			session.CorStepper = newCorStepper;
			engine.Continue_CorDebug();
			return tcs.Task;
		}

		public override Task<DbgThread> StepIntoAsync(DbgDotNetEngineStepperFrameInfo frame, DbgCodeRange[] ranges) {
			engine.VerifyCorDebugThread();
			Debug2.Assert(!(session is null));
			var frameImpl = (DbgDotNetEngineStepperFrameInfoImpl)frame;
			Debug.Assert(dnDebugger.ProcessState == DebuggerProcessState.Paused);
			CorStepper? newCorStepper = null;
			var tcs = new TaskCompletionSource<DbgThread>();
			var stepRanges = ToStepRanges(ranges);
			newCorStepper = dnDebugger.StepInto(frameImpl.CorFrame, stepRanges, (_, e, canceled) => {
				if (canceled)
					tcs.SetCanceled();
				else {
					Debug2.Assert(!(e is null));
					e.AddPauseReason(DebuggerPauseReason.Other);
					var thread = engine.TryGetThread(e.CorThread);
					if (!(thread is null))
						tcs.SetResult(thread);
					else
						tcs.SetException(new InvalidOperationException());
				}
			});
			session.CorStepper = newCorStepper;
			engine.Continue_CorDebug();
			return tcs.Task;
		}

		public override Task<DbgThread> StepOverAsync(DbgDotNetEngineStepperFrameInfo frame, DbgCodeRange[] ranges) {
			engine.VerifyCorDebugThread();
			Debug2.Assert(!(session is null));
			var frameImpl = (DbgDotNetEngineStepperFrameInfoImpl)frame;
			Debug.Assert(dnDebugger.ProcessState == DebuggerProcessState.Paused);
			CorStepper? newCorStepper = null;
			var tcs = new TaskCompletionSource<DbgThread>();
			var stepRanges = ToStepRanges(ranges);
			newCorStepper = dnDebugger.StepOver(frameImpl.CorFrame, stepRanges, (_, e, canceled) => {
				if (canceled)
					tcs.SetCanceled();
				else {
					Debug2.Assert(!(e is null));
					e.AddPauseReason(DebuggerPauseReason.Other);
					var thread = engine.TryGetThread(e.CorThread);
					if (!(thread is null))
						tcs.SetResult(thread);
					else
						tcs.SetException(new InvalidOperationException());
				}
			});
			session.CorStepper = newCorStepper;
			engine.Continue_CorDebug();
			return tcs.Task;
		}

		public override void OnStepComplete() {
			engine.VerifyCorDebugThread();
			Debug2.Assert(!(session is null));
			var returnValues = session.TakeOwnershipOfReturnValues() ?? Array.Empty<DbgDotNetReturnValueInfo>();
			engine.SetReturnValues(returnValues);
		}

		public override void CollectReturnValues(DbgDotNetEngineStepperFrameInfo frame, DbgILInstruction[][] statementInstructions) {
			engine.VerifyCorDebugThread();
			Debug2.Assert(!(session is null));
			var frameImpl = (DbgDotNetEngineStepperFrameInfoImpl)frame;
			if (statementInstructions.Length == 0)
				return;
			var code = frameImpl.CorFrame.Code;
			if (code is null)
				return;
			var rvColl = session.GetOrCreateReturnValuesCollection(engine, maxReturnValues);
			var rvState = rvColl.CreateReturnValueState(engine.GetThread(frameImpl.Thread).CorThread, frameImpl.CorFrame);
			DmdModule? reflectionModule = null;
			IList<DmdType>? genericTypeArguments = null;
			IList<DmdType>? genericMethodArguments = null;
			var bps = new List<DnNativeCodeBreakpoint>();
			foreach (var instrs in statementInstructions) {
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
					Debug2.Assert(!(method is null));
					if (method is null)
						continue;
					bps.Clear();
					Action<CorThread?> bpHitCallback = bpThread => rvColl.Hit(rvState, bpThread, instrOffs);
					foreach (var liveOffset in liveOffsets)
						bps.Add(engine.CreateNativeBreakpointForGetReturnValue(code, liveOffset, bpHitCallback));
					var callSiteInfo = new CallSiteInfo(method, bps.ToArray());
					rvState.CallSiteInfos.Add(instrOffs, callSiteInfo);
				}
			}
		}

		public override void ClearReturnValues() {
			engine.VerifyCorDebugThread();
			session?.ClearReturnValues();
		}

		DmdMethodBase? GetMethod(CorFrame frame, int methodMetadataToken, ref DmdModule? reflectionModule, ref IList<DmdType>? genericTypeArguments, ref IList<DmdType>? genericMethodArguments) {
			if (reflectionModule is null) {
				reflectionModule = engine.TryGetModule(frame.Function?.Module)?.GetReflectionModule();
				if (reflectionModule is null)
					return null;
			}

			if (genericTypeArguments is null) {
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

		CorFrame? GetILFrame(DbgThread thread) {
			engine.VerifyCorDebugThread();
			var dnThread = engine.GetThread(thread);
			foreach (var frame in dnThread.AllFrames) {
				if (frame.IsILFrame)
					return frame;
			}
			return null;
		}

		public override DbgDotNetStepperBreakpoint CreateBreakpoint(DbgThread? thread, DbgModule module, uint token, uint offset) {
			engine.VerifyCorDebugThread();
			return new DbgDotNetStepperBreakpointImpl(engine, thread, module, token, offset);
		}

		public override void RemoveBreakpoints(DbgDotNetStepperBreakpoint[] breakpoints) {
			engine.VerifyCorDebugThread();
			foreach (DbgDotNetStepperBreakpointImpl bp in breakpoints)
				bp.Dispose();
		}

		public override bool IgnoreException(Exception exception) => false;

		public override void OnCanceled(SessionBase session) {
			engine.VerifyCorDebugThread();
			CancelStepper(session);
		}

		public override void CancelLastStep() {
			engine.VerifyCorDebugThread();
			CancelStepper(session);
		}

		void CancelStepper(SessionBase? session) {
			engine.VerifyCorDebugThread();
			if (session is null)
				return;
			var sessionImpl = (SessionImpl)session;
			var stepper = sessionImpl.CorStepper;
			sessionImpl.CorStepper = null;
			if (!(stepper is null))
				dnDebugger.CancelStep(stepper);
		}

		public override void Close(DbgDispatcher dispatcher) { }
	}
}
