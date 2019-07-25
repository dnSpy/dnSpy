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
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.CallStack;
using dnSpy.Contracts.Debugger.Code;
using dnSpy.Contracts.Debugger.DotNet.Code;
using dnSpy.Contracts.Debugger.Engine.CallStack;
using dnSpy.Debugger.DotNet.Metadata;
using dnSpy.Debugger.DotNet.Mono.Impl;
using Mono.Debugger.Soft;
using MDS = Mono.Debugger.Soft;

namespace dnSpy.Debugger.DotNet.Mono.CallStack {
	sealed class ILDbgEngineStackFrame : DbgEngineStackFrame {
		public override DbgCodeLocation? Location { get; }
		public override DbgModule? Module { get; }
		public override DbgStackFrameFlags Flags => DbgStackFrameFlags.None;
		public override uint FunctionOffset { get; }
		public override uint FunctionToken { get; }

		internal MDS.StackFrame MonoFrame {
			get {
				if (engine.MethodInvokeCounter != methodInvokeCounter)
					UpdateFrame();
				return __monoFrame_DONT_USE;
			}
		}
		readonly ThreadMirror frameThread;
		MDS.StackFrame __monoFrame_DONT_USE;
		int frameIndex;
		int methodInvokeCounter;

		void UpdateFrame() {
			var frames = frameThread.GetFrames();
			// PERF: It's not likely that frames were removed, so always start searching from the frame index
			for (int i = frameIndex; i < frames.Length; i++) {
				var currentFrame = frames[i];
				if (IsSameAsOurFrame(currentFrame)) {
					__monoFrame_DONT_USE = currentFrame;
					frameIndex = i;
					methodInvokeCounter = engine.MethodInvokeCounter;
					return;
				}
			}
			Debug.Fail("Failed to find the frame");
		}

		bool IsSameAsOurFrame(MDS.StackFrame otherFrame) {
			if (otherFrame is null)
				return false;

			// None of the properties below can throw, all values are cached in the StackFrame instance
			if (__monoFrame_DONT_USE.Method != otherFrame.Method)
				return false;
			if (__monoFrame_DONT_USE.ILOffset != otherFrame.ILOffset)
				return false;
			if (__monoFrame_DONT_USE.IsDebuggerInvoke != otherFrame.IsDebuggerInvoke)
				return false;
			if (__monoFrame_DONT_USE.IsNativeTransition != otherFrame.IsNativeTransition)
				return false;

			// Looks like it could be the correct frame. There's no address so we don't know for sure,
			// but since the properties above matched AND the frame index is probably the same too,
			// it's very likely the same frame.

			return true;
		}

		readonly DbgEngineImpl engine;

		public ILDbgEngineStackFrame(DbgEngineImpl engine, DbgModule module, ThreadMirror frameThread, MDS.StackFrame monoFrame, int frameIndex, Lazy<DbgDotNetCodeLocationFactory> dbgDotNetCodeLocationFactory) {
			this.engine = engine ?? throw new ArgumentNullException(nameof(engine));
			this.frameThread = frameThread;
			__monoFrame_DONT_USE = monoFrame ?? throw new ArgumentNullException(nameof(monoFrame));
			this.frameIndex = frameIndex;
			methodInvokeCounter = engine.MethodInvokeCounter;
			Module = module ?? throw new ArgumentNullException(nameof(module));
			FunctionToken = (uint)monoFrame.Method.MetadataToken;
			// Native transitions have no IL offset so -1 is used by mono, but we should use 0 instead
			uint ilOffset = (uint)monoFrame.ILOffset;
			FunctionOffset = ilOffset == uint.MaxValue ? 0 : ilOffset;
			var moduleId = DbgEngineImpl.TryGetModuleId(module) ?? default;
			var ilOffsetMapping = ilOffset == uint.MaxValue || monoFrame.IsNativeTransition ? DbgILOffsetMapping.Unknown :
				frameIndex == 0 ? DbgILOffsetMapping.Exact : DbgILOffsetMapping.Approximate;
			Location = dbgDotNetCodeLocationFactory.Value.Create(moduleId, FunctionToken, FunctionOffset, ilOffsetMapping);
		}

		sealed class ILFrameState {
			public readonly ILDbgEngineStackFrame ILFrame;
			public ILFrameState(ILDbgEngineStackFrame ilFrame) => ILFrame = ilFrame;
		}
		public override void OnFrameCreated(DbgStackFrame frame) => frame.GetOrCreateData(() => new ILFrameState(this));
		internal static bool TryGetEngineStackFrame(DbgStackFrame frame, [NotNullWhen(true)] out ILDbgEngineStackFrame? ilFrame) {
			if (frame.TryGetData<ILFrameState>(out var data)) {
				ilFrame = data.ILFrame;
				return true;
			}
			ilFrame = null;
			return false;
		}

		internal void GetFrameMethodInfo(out DmdModule module, out int methodMetadataToken, out IList<DmdType> genericTypeArguments, out IList<DmdType> genericMethodArguments) {
			engine.VerifyMonoDebugThread();
			methodMetadataToken = MonoFrame.Method.MetadataToken;
			module = Module!.GetReflectionModule() ?? throw new InvalidOperationException();

			TypeMirror[] typeGenArgs;
			TypeMirror[] methGenArgs;
			if (MonoFrame.VirtualMachine.Version.AtLeast(2, 15)) {
				typeGenArgs = MonoFrame.Method.DeclaringType.GetGenericArguments();
				methGenArgs = MonoFrame.Method.GetGenericArguments();
			}
			else {
				typeGenArgs = Array.Empty<TypeMirror>();
				methGenArgs = Array.Empty<TypeMirror>();
			}

			var reflectionAppDomain = module.AppDomain;
			genericTypeArguments = Convert(reflectionAppDomain, typeGenArgs);
			genericMethodArguments = Convert(reflectionAppDomain, methGenArgs);
		}

		IList<DmdType> Convert(DmdAppDomain reflectionAppDomain, TypeMirror[] typeArgs) {
			if (typeArgs.Length == 0)
				return Array.Empty<DmdType>();
			var types = new DmdType[typeArgs.Length];
			for (int i = 0; i < types.Length; i++)
				types[i] = engine.GetReflectionType(reflectionAppDomain, typeArgs[i], null);
			return types;
		}

		internal DmdModule GetReflectionModule() => Module!.GetReflectionModule() ?? throw new InvalidOperationException();

		protected override void CloseCore(DbgDispatcher dispatcher) { }
	}
}
