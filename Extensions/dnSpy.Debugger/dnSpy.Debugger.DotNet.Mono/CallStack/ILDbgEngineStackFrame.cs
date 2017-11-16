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
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.CallStack;
using dnSpy.Contracts.Debugger.Code;
using dnSpy.Contracts.Debugger.DotNet.Code;
using dnSpy.Contracts.Debugger.Engine.CallStack;
using dnSpy.Contracts.Text;
using dnSpy.Debugger.DotNet.Metadata;
using dnSpy.Debugger.DotNet.Mono.Impl;
using Mono.Debugger.Soft;
using SD = System.Diagnostics;

namespace dnSpy.Debugger.DotNet.Mono.CallStack {
	sealed class ILDbgEngineStackFrame : DbgEngineStackFrame {
		public override DbgCodeLocation Location { get; }
		public override DbgModule Module { get; }
		public override uint FunctionOffset { get; }
		public override uint FunctionToken { get; }

		internal StackFrame MonoFrame => monoFrame;

		readonly DbgEngineImpl engine;
		readonly StackFrame monoFrame;

		public ILDbgEngineStackFrame(DbgEngineImpl engine, DbgModule module, StackFrame monoFrame, Lazy<DbgDotNetCodeLocationFactory> dbgDotNetCodeLocationFactory) {
			this.engine = engine ?? throw new ArgumentNullException(nameof(engine));
			this.monoFrame = monoFrame ?? throw new ArgumentNullException(nameof(monoFrame));
			Module = module ?? throw new ArgumentNullException(nameof(module));
			FunctionToken = (uint)monoFrame.Method.MetadataToken;
			// Native transitions have no IL offset so -1 is used by mono, but we should use 0 instead
			var ilOffset = (uint)monoFrame.ILOffset;
			FunctionOffset = ilOffset == uint.MaxValue ? 0 : ilOffset;
			var moduleId = DbgEngineImpl.TryGetModuleId(module) ?? default;
			var options = ilOffset == uint.MaxValue ? DbgDotNetCodeLocationOptions.InvalidOffset : 0;
			Location = dbgDotNetCodeLocationFactory.Value.Create(moduleId, FunctionToken, FunctionOffset, options);
		}

		public override void Format(ITextColorWriter writer, DbgStackFrameFormatOptions options) =>
			engine.DebuggerThread.Invoke(() => Format_MonoDebug(writer, options));

		void Format_MonoDebug(ITextColorWriter writer, DbgStackFrameFormatOptions options) {
			if (Module.IsClosed)
				return;
			writer.Write(BoxedTextColor.Error, "NYI");//TODO:
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
			engine.VerifyMonoDebugThread();
			methodMetadataToken = monoFrame.Method.MetadataToken;
			module = Module.GetReflectionModule() ?? throw new InvalidOperationException();

			TypeMirror[] typeGenArgs;
			TypeMirror[] methGenArgs;
			if (monoFrame.VirtualMachine.Version.AtLeast(2, 15)) {
				typeGenArgs = monoFrame.Method.DeclaringType.GetGenericArguments();
				methGenArgs = monoFrame.Method.GetGenericArguments();
			}
			else {
				SD.Debug.Fail("Old version doesn't support generics");
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
			var reflectionTypeCreator = new ReflectionTypeCreator(engine, reflectionAppDomain);
			for (int i = 0; i < types.Length; i++)
				types[i] = reflectionTypeCreator.Create(typeArgs[i]);
			return types;
		}

		internal DmdModule GetReflectionModule() => Module.GetReflectionModule() ?? throw new InvalidOperationException();

		protected override void CloseCore(DbgDispatcher dispatcher) { }
	}
}
