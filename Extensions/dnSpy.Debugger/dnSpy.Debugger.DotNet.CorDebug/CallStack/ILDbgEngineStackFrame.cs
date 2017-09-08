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
using dnSpy.Contracts.Debugger.CallStack;
using dnSpy.Contracts.Debugger.Code;
using dnSpy.Contracts.Debugger.DotNet.Code;
using dnSpy.Contracts.Debugger.DotNet.CorDebug.Code;
using dnSpy.Contracts.Debugger.Engine.CallStack;
using dnSpy.Contracts.Text;
using dnSpy.Debugger.DotNet.CorDebug.Code;
using dnSpy.Debugger.DotNet.CorDebug.Impl;
using dnSpy.Debugger.DotNet.Metadata;

namespace dnSpy.Debugger.DotNet.CorDebug.CallStack {
	sealed class ILDbgEngineStackFrame : DbgEngineStackFrame {
		public override DbgCodeLocation Location { get; }
		public override DbgModule Module { get; }
		public override uint FunctionOffset { get; }
		public override uint FunctionToken { get; }

		readonly DbgEngineImpl engine;
		readonly CorFrame corFrame;

		public ILDbgEngineStackFrame(DbgEngineImpl engine, DbgModule module, CorFrame corFrame, CorFunction corFunction, Lazy<DbgDotNetNativeCodeLocationFactory> dbgDotNetNativeCodeLocationFactory, Lazy<DbgDotNetCodeLocationFactory> dbgDotNetCodeLocationFactory) {
			this.engine = engine ?? throw new ArgumentNullException(nameof(engine));
			Module = module ?? throw new ArgumentNullException(nameof(module));
			this.corFrame = corFrame ?? throw new ArgumentNullException(nameof(corFrame));

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
				Location = dbgDotNetCodeLocationFactory.Value.Create(moduleId, FunctionToken, FunctionOffset);
		}

		public override void Format(ITextColorWriter writer, DbgStackFrameFormatOptions options) =>
			engine.DebuggerThread.Invoke(() => Format_CorDebug(writer, options));

		void Format_CorDebug(ITextColorWriter writer, DbgStackFrameFormatOptions options) {
			if (Module.IsClosed)
				return;
			var output = engine.stackFrameData.TypeOutputTextColorWriter.Initialize(writer);
			try {
				var flags = GetFlags(options);
				Func<DnEval> getEval = null;
				Debug.Assert((options & DbgStackFrameFormatOptions.ShowParameterValues) == 0, "NYI");
				new TypeFormatter(output, flags, getEval).Write(corFrame);
			}
			finally {
				output.Clear();
			}
		}

		static TypeFormatterFlags GetFlags(DbgStackFrameFormatOptions options) {
			var flags = TypeFormatterFlags.ShowArrayValueSizes;
			if ((options & DbgStackFrameFormatOptions.ShowReturnTypes) != 0)			flags |= TypeFormatterFlags.ShowReturnTypes;
			if ((options & DbgStackFrameFormatOptions.ShowParameterTypes) != 0)			flags |= TypeFormatterFlags.ShowParameterTypes;
			if ((options & DbgStackFrameFormatOptions.ShowParameterNames) != 0)			flags |= TypeFormatterFlags.ShowParameterNames;
			if ((options & DbgStackFrameFormatOptions.ShowParameterValues) != 0)		flags |= TypeFormatterFlags.ShowParameterValues;
			if ((options & DbgStackFrameFormatOptions.ShowFunctionOffset) != 0)			flags |= TypeFormatterFlags.ShowIP;
			if ((options & DbgStackFrameFormatOptions.ShowModuleNames) != 0)			flags |= TypeFormatterFlags.ShowModuleNames;
			if ((options & DbgStackFrameFormatOptions.ShowDeclaringTypes) != 0)			flags |= TypeFormatterFlags.ShowDeclaringTypes;
			if ((options & DbgStackFrameFormatOptions.ShowNamespaces) != 0)				flags |= TypeFormatterFlags.ShowNamespaces;
			if ((options & DbgStackFrameFormatOptions.ShowIntrinsicTypeKeywords) != 0)	flags |= TypeFormatterFlags.ShowIntrinsicTypeKeywords;
			if ((options & DbgStackFrameFormatOptions.ShowTokens) != 0)					flags |= TypeFormatterFlags.ShowTokens;
			if ((options & DbgStackFrameFormatOptions.UseDecimal) != 0)					flags |= TypeFormatterFlags.UseDecimal;
			return flags;
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

		internal void GetFrameMethodInfo(out DmdModule module, out int methodMetadataToken, out IList<DmdType> genericTypeArguments, out IList<DmdType> genericMethodArguments) {
			engine.VerifyCorDebugThread();
			methodMetadataToken = (int)corFrame.Token;
			module = engine.TryGetModule(corFrame.Function?.Module)?.GetReflectionModule();
			if (!corFrame.GetTypeAndMethodGenericParameters(out var typeGenArgs, out var methGenArgs))
				throw new InvalidOperationException();
			var reflectionAppDomain = module.AppDomain;
			genericTypeArguments = Convert(reflectionAppDomain, typeGenArgs);
			genericMethodArguments = Convert(reflectionAppDomain, methGenArgs);
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
