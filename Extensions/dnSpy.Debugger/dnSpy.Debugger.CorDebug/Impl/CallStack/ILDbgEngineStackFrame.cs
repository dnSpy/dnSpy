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
using System.Diagnostics;
using dndbg.Engine;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.CallStack;
using dnSpy.Contracts.Debugger.DotNet.CallStack;
using dnSpy.Contracts.Debugger.DotNet.CorDebug.CallStack;
using dnSpy.Contracts.Debugger.Engine.CallStack;
using dnSpy.Contracts.Text;

namespace dnSpy.Debugger.CorDebug.Impl.CallStack {
	sealed class ILDbgEngineStackFrame : DbgEngineStackFrame {
		public override DbgStackFrameLocation Location { get; }
		public override DbgModule Module { get; }
		public override uint FunctionOffset { get; }
		public override uint FunctionToken { get; }

		readonly DbgEngineImpl engine;
		readonly CorFrame corFrame;

		public ILDbgEngineStackFrame(DbgEngineImpl engine, DbgModule module, CorFrame corFrame, CorFunction corFunction) {
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

			var moduleId = corFrame.DnModuleId.GetValueOrDefault().ToModuleId();
			var nativeCode = corFrame.Code;
			Debug.Assert(nativeCode?.IsIL == false);
			if (nativeCode?.IsIL == false)
				Location = new DbgDotNetStackFrameLocationImpl(moduleId, FunctionToken, FunctionOffset, ilOffsetMapping, nativeCode, corFrame.NativeFrameIP);
			else
				Location = new DbgDotNetMethodBodyStackFrameLocation(moduleId, FunctionToken, FunctionOffset);
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
			if ((options & DbgStackFrameFormatOptions.ShowReturnTypes) != 0) flags |= TypeFormatterFlags.ShowReturnTypes;
			if ((options & DbgStackFrameFormatOptions.ShowParameterTypes) != 0) flags |= TypeFormatterFlags.ShowParameterTypes;
			if ((options & DbgStackFrameFormatOptions.ShowParameterNames) != 0) flags |= TypeFormatterFlags.ShowParameterNames;
			if ((options & DbgStackFrameFormatOptions.ShowParameterValues) != 0) flags |= TypeFormatterFlags.ShowParameterValues;
			if ((options & DbgStackFrameFormatOptions.ShowFunctionOffset) != 0) flags |= TypeFormatterFlags.ShowIP;
			if ((options & DbgStackFrameFormatOptions.ShowModuleNames) != 0) flags |= TypeFormatterFlags.ShowModuleNames;
			if ((options & DbgStackFrameFormatOptions.ShowDeclaringTypes) != 0) flags |= TypeFormatterFlags.ShowDeclaringTypes;
			if ((options & DbgStackFrameFormatOptions.ShowNamespaces) != 0) flags |= TypeFormatterFlags.ShowNamespaces;
			if ((options & DbgStackFrameFormatOptions.ShowIntrinsicTypeKeywords) != 0) flags |= TypeFormatterFlags.ShowIntrinsicTypeKeywords;
			if ((options & DbgStackFrameFormatOptions.ShowTokens) != 0) flags |= TypeFormatterFlags.ShowTokens;
			if ((options & DbgStackFrameFormatOptions.UseDecimal) != 0) flags |= TypeFormatterFlags.UseDecimal;
			return flags;
		}

		protected override void CloseCore() { }
	}
}
