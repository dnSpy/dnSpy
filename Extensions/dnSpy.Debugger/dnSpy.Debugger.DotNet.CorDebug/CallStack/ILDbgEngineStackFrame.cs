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
using dndbg.Engine;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.CallStack;
using dnSpy.Contracts.Debugger.Code;
using dnSpy.Contracts.Debugger.DotNet.Code;
using dnSpy.Contracts.Debugger.Engine.CallStack;
using dnSpy.Debugger.DotNet.CorDebug.Code;
using dnSpy.Debugger.DotNet.CorDebug.Impl;
using dnSpy.Debugger.DotNet.Metadata;

namespace dnSpy.Debugger.DotNet.CorDebug.CallStack {
	sealed class ILDbgEngineStackFrame : DbgEngineStackFrame {
		public override DbgCodeLocation Location { get; }
		public override DbgModule Module { get; }
		public override DbgStackFrameFlags Flags => DbgStackFrameFlags.LocationIsNextStatement;
		public override uint FunctionOffset { get; }
		public override uint FunctionToken { get; }

		internal CorFrame CorFrame {
			get {
				if (__corFrame_DONT_USE.IsNeutered)
					__corFrame_DONT_USE = FindFrame(__corFrame_DONT_USE) ?? __corFrame_DONT_USE;
				return __corFrame_DONT_USE;
			}
		}
		CorFrame __corFrame_DONT_USE;

		CorFrame FindFrame(CorFrame frame) {
			foreach (var f in dnThread.AllFrames) {
				if (f.StackStart == frame.StackStart && f.StackEnd == frame.StackEnd)
					return f;
			}
			return null;
		}

		readonly DbgEngineImpl engine;
		readonly DnThread dnThread;

		public ILDbgEngineStackFrame(DbgEngineImpl engine, DbgModule module, CorFrame corFrame, DnThread dnThread, CorFunction corFunction, Lazy<DbgDotNetNativeCodeLocationFactory> dbgDotNetNativeCodeLocationFactory, Lazy<DbgDotNetCodeLocationFactory> dbgDotNetCodeLocationFactory) {
			Debug.Assert(!corFrame.IsNeutered);
			this.engine = engine ?? throw new ArgumentNullException(nameof(engine));
			Module = module ?? throw new ArgumentNullException(nameof(module));
			__corFrame_DONT_USE = corFrame ?? throw new ArgumentNullException(nameof(corFrame));
			this.dnThread = dnThread ?? throw new ArgumentNullException(nameof(dnThread));

			FunctionToken = corFunction?.Token ?? throw new ArgumentNullException(nameof(corFunction));

			uint functionOffset;
			DbgILOffsetMapping ilOffsetMapping;
			Debug.Assert(corFrame.IsILFrame);
			var ip = corFrame.ILFrameIP;
			if (ip.IsExact) {
				functionOffset = ip.Offset;
				ilOffsetMapping = DbgILOffsetMapping.Exact;
			}
			else if (ip.IsApproximate) {
				functionOffset = ip.Offset;
				ilOffsetMapping = DbgILOffsetMapping.Approximate;
			}
			else if (ip.IsProlog) {
				Debug.Assert(ip.Offset == 0);
				functionOffset = ip.Offset;// 0
				ilOffsetMapping = DbgILOffsetMapping.Prolog;
			}
			else if (ip.IsEpilog) {
				functionOffset = ip.Offset;// end of method == DebuggerJitInfo.m_lastIL
				ilOffsetMapping = DbgILOffsetMapping.Epilog;
			}
			else if (ip.HasNoInfo) {
				functionOffset = uint.MaxValue;
				ilOffsetMapping = DbgILOffsetMapping.NoInfo;
			}
			else if (ip.IsUnmappedAddress) {
				functionOffset = uint.MaxValue;
				ilOffsetMapping = DbgILOffsetMapping.UnmappedAddress;
			}
			else {
				Debug.Fail($"Unknown mapping: {ip.Mapping}");
				functionOffset = uint.MaxValue;
				ilOffsetMapping = DbgILOffsetMapping.Unknown;
			}
			FunctionOffset = functionOffset;

			var moduleId = engine.TryGetModuleId(corFrame).GetValueOrDefault().ToModuleId();
			var nativeCode = corFrame.Code;
			Debug.Assert(nativeCode?.IsIL == false);
			if (nativeCode?.IsIL == false) {
				var corCode = engine.CreateDnDebuggerObjectHolder(nativeCode);
				Location = dbgDotNetNativeCodeLocationFactory.Value.Create(module, moduleId, FunctionToken, FunctionOffset, ilOffsetMapping, corCode.Object.Address, corFrame.NativeFrameIP, corCode);
			}
			else
				Location = dbgDotNetCodeLocationFactory.Value.Create(moduleId, FunctionToken, FunctionOffset, ilOffsetMapping);
		}

		sealed class ILFrameState {
			public readonly ILDbgEngineStackFrame ILFrame;
			public ILFrameState(ILDbgEngineStackFrame ilFrame) => ILFrame = ilFrame;
		}
		public override void OnFrameCreated(DbgStackFrame frame) => frame.GetOrCreateData(() => new ILFrameState(this));
		internal static bool TryGetEngineStackFrame(DbgStackFrame frame, out ILDbgEngineStackFrame ilFrame) {
			if (frame.TryGetData<ILFrameState>(out var data)) {
				ilFrame = data.ILFrame;
				return true;
			}
			ilFrame = null;
			return false;
		}

		internal DmdModule GetReflectionModule() => Module.GetReflectionModule() ?? throw new InvalidOperationException();
		internal CorAppDomain GetCorAppDomain() => dnThread.AppDomainOrNull?.CorAppDomain ?? throw new InvalidOperationException();

		internal void GetFrameMethodInfo(out DmdModule module, out int methodMetadataToken, out IList<DmdType> genericTypeArguments, out IList<DmdType> genericMethodArguments) {
			engine.VerifyCorDebugThread();
			var corFrame = CorFrame;
			methodMetadataToken = (int)corFrame.Token;
			var corModule = corFrame.Function?.Module;
			if (corModule != null) {
				module = engine.TryGetModule(corModule)?.GetReflectionModule() ?? throw new InvalidOperationException();
				if (!corFrame.GetTypeAndMethodGenericParameters(out var typeGenArgs, out var methGenArgs))
					throw new InvalidOperationException();
				var reflectionAppDomain = module.AppDomain;
				genericTypeArguments = Convert(reflectionAppDomain, typeGenArgs);
				genericMethodArguments = Convert(reflectionAppDomain, methGenArgs);
				return;
			}

			module = null;
			methodMetadataToken = 0;
			genericTypeArguments = Array.Empty<DmdType>();
			genericMethodArguments = Array.Empty<DmdType>();
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

		protected override void CloseCore(DbgDispatcher dispatcher) { }
	}
}
