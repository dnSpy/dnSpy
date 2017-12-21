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
using dndbg.Engine;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.DotNet.Code;
using dnSpy.Contracts.Debugger.DotNet.Evaluation;
using dnSpy.Contracts.Debugger.Engine.Steppers;
using dnSpy.Debugger.DotNet.CorDebug.Impl;
using dnSpy.Debugger.DotNet.Metadata;
using DNE = dnlib.DotNet.Emit;

namespace dnSpy.Debugger.DotNet.CorDebug.Steppers {
	sealed class DbgEngineStepperImpl : DbgEngineStepper {
		public override event EventHandler<DbgEngineStepCompleteEventArgs> StepComplete;

		readonly DbgDotNetCodeRangeService dbgDotNetCodeRangeService;
		readonly DbgEngineImpl engine;
		readonly DbgThread thread;
		readonly DnThread dnThread;
		readonly DebuggerSettings debuggerSettings;

		const int maxReturnValues = 100;

		StepDataImpl StepData {
			get => __DONT_USE_stepData;
			set {
				if (__DONT_USE_stepData != value) {
					__DONT_USE_stepData?.Dispose();
					__DONT_USE_stepData = value;
				}
			}
		}
		StepDataImpl __DONT_USE_stepData;

		sealed class CallSiteInfo {
			public DmdMethodBase Method { get; }
			public DnNativeCodeBreakpoint[] Breakpoints { get; }
			public CallSiteInfo(DmdMethodBase method, DnNativeCodeBreakpoint[] breakpoints) {
				Method = method ?? throw new ArgumentNullException(nameof(method));
				Breakpoints = breakpoints ?? throw new ArgumentNullException(nameof(breakpoints));
			}
		}

		sealed class ReturnValueState {
			public List<DbgDotNetReturnValueInfo> ReturnValues { get; }
			public Dictionary<uint, CallSiteInfo> CallSiteInfos { get; }
			public bool TooManyReturnValues { get; private set; }

			readonly DbgEngineImpl engine;
			readonly CorThread corThread;
			readonly ulong stackStart, stackEnd;
			readonly uint token;
			readonly int maxReturnValues;

			public ReturnValueState(DbgEngineImpl engine, CorThread corThread, CorFrame corFrame, int maxReturnValues) {
				this.engine = engine ?? throw new ArgumentNullException(nameof(engine));
				this.corThread = corThread;
				this.maxReturnValues = maxReturnValues;
				stackStart = corFrame.StackStart;
				stackEnd = corFrame.StackEnd;
				token = corFrame.Token;
				ReturnValues = new List<DbgDotNetReturnValueInfo>();
				CallSiteInfos = new Dictionary<uint, CallSiteInfo>();
			}

			public void Hit(CorThread corThread, uint offset) {
				if (TooManyReturnValues)
					return;
				if (this.corThread != corThread)
					return;
				var corFrame = corThread.ActiveFrame;
				Debug.Assert(corFrame != null);
				if (corFrame == null)
					return;
				if (stackStart != corFrame.StackStart || stackEnd != corFrame.StackEnd || token != corFrame.Token)
					return;
				if (!CallSiteInfos.TryGetValue(offset, out var callSiteInfo))
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
				foreach (var kv in CallSiteInfos) {
					foreach (var bp in kv.Value.Breakpoints)
						engine.RemoveNativeBreakpointForGetReturnValue(bp);
				}
				CallSiteInfos.Clear();
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

		sealed class StepDataImpl {
			public object Tag { get; }
			public CorStepper CorStepper { get; }
			public ReturnValueState ReturnValueState { get; }
			public StepDataImpl(object tag, CorStepper corStepper, ReturnValueState returnValueState) {
				Tag = tag;
				CorStepper = corStepper;
				ReturnValueState = returnValueState;
			}

			public void Dispose() => ReturnValueState?.Dispose();
		}

		public DbgEngineStepperImpl(DbgDotNetCodeRangeService dbgDotNetCodeRangeService, DbgEngineImpl engine, DbgThread thread, DnThread dnThread, DebuggerSettings debuggerSettings) {
			this.dbgDotNetCodeRangeService = dbgDotNetCodeRangeService ?? throw new ArgumentNullException(nameof(dbgDotNetCodeRangeService));
			this.engine = engine ?? throw new ArgumentNullException(nameof(engine));
			this.thread = thread ?? throw new ArgumentNullException(nameof(thread));
			this.dnThread = dnThread ?? throw new ArgumentNullException(nameof(dnThread));
			this.debuggerSettings = debuggerSettings ?? throw new ArgumentNullException(nameof(debuggerSettings));
		}

		void RaiseStepComplete(DbgThread thread, object tag, string error) {
			if (IsClosed)
				return;
			Debug.Assert(StepComplete != null);
			StepComplete?.Invoke(this, new DbgEngineStepCompleteEventArgs(thread, tag, error, false));
		}

		public override void Step(object tag, DbgEngineStepKind step) => engine.CorDebugThread(() => Step_CorDebug(tag, step));
		void Step_CorDebug(object tag, DbgEngineStepKind step) {
			engine.VerifyCorDebugThread();

			if (StepData != null) {
				Debug.Fail("The previous step hasn't been canceled");
				// No need to localize it, if we're here it's a bug
				RaiseStepComplete(thread, tag, "The previous step hasn't been canceled");
				return;
			}

			var dbg = dnThread.Debugger;
			if (dbg.ProcessState != DebuggerProcessState.Paused) {
				Debug.Fail("Process is not paused");
				// No need to localize it, if we're here it's a bug
				RaiseStepComplete(thread, tag, "Process is not paused");
				return;
			}

			var frame = GetILFrame();
			if (frame == null) {
				// No frame? Just let the process run.
				engine.Continue_CorDebug();
				return;
			}

			CorStepper newCorStepper = null;
			switch (step) {
			case DbgEngineStepKind.StepInto:
				GetStepRanges(frame, tag, isStepInto: true);
				return;

			case DbgEngineStepKind.StepOver:
				GetStepRanges(frame, tag, isStepInto: false);
				return;

			case DbgEngineStepKind.StepOut:
				newCorStepper = dbg.StepOut(frame, (_, e) => StepCompleted(e, newCorStepper, tag));
				SaveStepper(newCorStepper, null, tag);
				return;

			default:
				RaiseStepComplete(thread, tag, $"Unsupported step kind: {step}");
				return;
			}
		}

		void SaveStepper(CorStepper newCorStepper, ReturnValueState returnValueState, object tag) {
			engine.VerifyCorDebugThread();
			if (newCorStepper != null) {
				StepData = new StepDataImpl(tag, newCorStepper, returnValueState);
				engine.Continue_CorDebug();
			}
			else {
				// This should rarely if ever happen so the string doesn't need to be localized
				RaiseStepComplete(thread, tag, "Could not step");
			}
		}

		void GetStepRanges(CorFrame frame, object tag, bool isStepInto) {
			engine.VerifyCorDebugThread();
			var module = engine.TryGetModule(frame.Function?.Module);
			var offset = GetILOffset(frame);
			if (module == null || offset == null)
				SaveStepper(null, null, tag);
			else {
				uint continueCounter = dnThread.Debugger.ContinueCounter;
				// Return values are available since .NET Framework 4.5.1 / .NET Core 1.0
				var options = isStepInto || !debuggerSettings.ShowReturnValues || frame.Code?.SupportsReturnValues != true ?
					GetCodeRangesOptions.None : GetCodeRangesOptions.Instructions;
				dbgDotNetCodeRangeService.GetCodeRanges(module, frame.Token, offset.Value, options,
					result => engine.CorDebugThread(() => GotStepRanges(frame, offset.Value, tag, isStepInto, result, continueCounter)));
			}
		}

		void GotStepRanges(CorFrame frame, uint offset, object tag, bool isStepInto, in GetCodeRangeResult result, uint continueCounter) {
			engine.VerifyCorDebugThread();
			if (IsClosed)
				return;
			if (StepData != null)
				return;
			if (continueCounter != dnThread.Debugger.ContinueCounter || frame.IsNeutered) {
				RaiseStepComplete(thread, tag, "Internal error");
				return;
			}
			// If we failed to find the statement ranges (result.Success == false), step anyway.
			// We'll just step until the next sequence point instead of not doing anything.
			var ranges = result.Success ? ToStepRanges(result.StatementRanges) : new StepRange[] { new StepRange(offset, offset + 1) };
			CorStepper newCorStepper = null;
			ReturnValueState returnValueState;
			var dbg = dnThread.Debugger;
			if (isStepInto) {
				returnValueState = null;
				newCorStepper = dbg.StepInto(frame, ranges, (_, e) => StepCompleted(e, newCorStepper, tag));
			}
			else {
				returnValueState = CreateReturnValueState(frame, offset, result);
				newCorStepper = dbg.StepOver(frame, ranges, (_, e) => StepCompleted(e, newCorStepper, tag));
			}
			SaveStepper(newCorStepper, returnValueState, tag);
		}

		ReturnValueState CreateReturnValueState(CorFrame frame, uint offset, in GetCodeRangeResult result) {
			var stmtInstrs = result.StatementInstructions;
			if (stmtInstrs.Length == 0)
				return null;
			var code = frame.Code;
			if (code == null)
				return null;
			var rvState = new ReturnValueState(engine, dnThread.CorThread, frame, maxReturnValues);
			DmdModule reflectionModule = null;
			IList<DmdType> genericTypeArguments = null;
			IList<DmdType> genericMethodArguments = null;
			var bps = new List<DnNativeCodeBreakpoint>();
			foreach (var instrs in stmtInstrs) {
				for (int i = 0; i < instrs.Length; i++) {
					var instr = instrs[i];
					uint instrOffs = instr.Offset;
					if (instr.OpCode == (ushort)DNE.Code.Tailcall)
						if (i + 1 < instrs.Length) {
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
					var method = GetMethod(frame, (int)instr.Operand, ref reflectionModule, ref genericTypeArguments, ref genericMethodArguments);
					Debug.Assert((object)method != null);
					if ((object)method == null)
						continue;
					bps.Clear();
					Action<CorThread> bpHitCallback = bpThread => rvState.Hit(bpThread, instrOffs);
					foreach (var liveOffset in liveOffsets)
						bps.Add(engine.CreateNativeBreakpointForGetReturnValue(code, liveOffset, bpHitCallback));
					var callSiteInfo = new CallSiteInfo(method, bps.ToArray());
					rvState.CallSiteInfos.Add(instrOffs, callSiteInfo);
				}
			}
			return rvState;
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

		CorFrame GetILFrame() {
			engine.VerifyCorDebugThread();
			foreach (var frame in dnThread.AllFrames) {
				if (frame.IsILFrame)
					return frame;
			}
			return null;
		}

		void StepCompleted(StepCompleteDebugCallbackEventArgs e, CorStepper corStepper, object tag) {
			engine.VerifyCorDebugThread();
			if (StepData == null || StepData.CorStepper != corStepper || StepData.Tag != tag)
				return;
			var returnValues = StepData?.ReturnValueState?.TakeOwnershipOfReturnValues() ?? Array.Empty<DbgDotNetReturnValueInfo>();
			engine.SetReturnValues(returnValues);
			StepData = null;
			e.AddPauseReason(DebuggerPauseReason.Other);
			var pausedThread = e.CorThread == dnThread.CorThread ? thread : engine.TryGetThread(e.CorThread);
			Debug.Assert(engine.TryGetThread(e.CorThread) == pausedThread);
			RaiseStepComplete(pausedThread, tag, null);
		}

		public override void Cancel(object tag) => engine.CorDebugThread(() => Cancel_CorDebug(tag));
		void Cancel_CorDebug(object tag) {
			engine.VerifyCorDebugThread();
			var oldStepperData = StepData;
			if (oldStepperData == null)
				return;
			if (oldStepperData.Tag != tag)
				return;
			ForceCancel_CorDebug();
		}

		void ForceCancel_CorDebug() {
			engine.VerifyCorDebugThread();
			var oldDnStepperData = StepData;
			StepData = null;
			if (oldDnStepperData != null)
				dnThread.Debugger.CancelStep(oldDnStepperData.CorStepper);
		}

		protected override void CloseCore(DbgDispatcher dispatcher) {
			if (StepData != null)
				engine.CorDebugThread(() => ForceCancel_CorDebug());
		}
	}
}
