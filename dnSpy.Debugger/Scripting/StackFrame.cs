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
using System.Threading;
using System.Threading.Tasks;
using dndbg.Engine;
using dnSpy.Contracts.Highlighting;
using dnSpy.Contracts.Scripting.Debugger;
using dnSpy.Shared.Scripting;

namespace dnSpy.Debugger.Scripting {
	sealed class StackFrame : IStackFrame {
		[Flags]
		enum SFFlags : byte {
			ILFrame					= 0x01,
			NativeFrame				= 0x02,
			InternalFrame			= 0x04,
			RuntimeUnwindableFrame	= 0x08,
		}

		public IStackChain Chain {
			get {
				return debugger.Dispatcher.UI(() => {
					var chain = frame.Chain;
					return chain == null ? null : new StackChain(debugger, chain);
				});
			}
		}

		public bool IsNeutered {
			get { return debugger.Dispatcher.UI(() => frame.IsNeutered); }
		}

		public Contracts.Scripting.Debugger.ILFrameIP ILFrameIP {
			get { return ilFrameIP; }
		}

		public InternalFrameType InternalFrameType {
			get { return debugger.Dispatcher.UI(() => (InternalFrameType)frame.InternalFrameType); }
		}

		public bool IsILFrame {
			get { return (sfFlags & SFFlags.ILFrame) != 0; }
		}

		public bool IsInternalFrame {
			get { return (sfFlags & SFFlags.InternalFrame) != 0; }
		}

		public bool IsJITCompiledFrame {
			get { return (sfFlags & (SFFlags.ILFrame | SFFlags.NativeFrame)) == (SFFlags.ILFrame | SFFlags.NativeFrame); }
		}

		public bool IsNativeFrame {
			get { return (sfFlags & SFFlags.NativeFrame) != 0; }
		}

		public bool IsRuntimeUnwindableFrame {
			get { return (sfFlags & SFFlags.RuntimeUnwindableFrame) != 0; }
		}

		public uint NativeOffset {
			get { return nativeFrameIP; }
			set { SetNativeOffset(value); }
		}

		public ulong StackEnd {
			get { return stackEnd; }
		}

		public ulong StackStart {
			get { return stackStart; }
		}

		public uint Token {
			get { return token; }
		}

		public int Index {
			get { return frameNo; }
		}
		readonly int frameNo;

		public IDebuggerMethod Method {
			get {
				return debugger.Dispatcher.UI(() => {
					var func = frame.Function;
					return func == null ? null : new DebuggerMethod(debugger, func);
				});
			}
		}

		public IDebuggerCode ILCode {
			get {
				return debugger.Dispatcher.UI(() => {
					var func = frame.Function;
					var code = func == null ? null : func.ILCode;
					return code == null ? null : new DebuggerCode(debugger, code);
				});
			}
		}

		public IDebuggerCode Code {
			get {
				return debugger.Dispatcher.UI(() => {
					var code = frame.Code;
					return code == null ? null : new DebuggerCode(debugger, code);
				});
			}
		}

		public IDebuggerValue[] Arguments {
			get {
				return debugger.Dispatcher.UI(() => {
					var list = new List<IDebuggerValue>();
					foreach (var v in frame.ILArguments)
						list.Add(new DebuggerValue(debugger, v));
					return list.ToArray();
				});
			}
		}

		public IDebuggerValue[] Locals {
			get {
				return debugger.Dispatcher.UI(() => {
					var list = new List<IDebuggerValue>();
					foreach (var v in frame.ILLocals)
						list.Add(new DebuggerValue(debugger, v));
					return list.ToArray();
				});
			}
		}

		public IDebuggerType[] GenericArguments {
			get {
				return debugger.Dispatcher.UI(() => {
					var list = new List<IDebuggerType>();
					foreach (var t in frame.TypeParameters)
						list.Add(new DebuggerType(debugger, t));
					return list.ToArray();
				});
			}
		}

		public IDebuggerType[] GenericTypeArguments {
			get {
				List<IDebuggerType> targs, margs;
				GetGenericArguments(out targs, out margs);
				return targs.ToArray();
			}
		}

		public IDebuggerType[] GenericMethodArguments {
			get {
				List<IDebuggerType> targs, margs;
				GetGenericArguments(out targs, out margs);
				return margs.ToArray();
			}
		}

		internal CorFrame CorFrame {
			get { return frame; }
		}

		readonly Debugger debugger;
		readonly CorFrame frame;
		readonly int hashCode;
		readonly uint nativeFrameIP;
		readonly uint token;
		readonly ulong stackStart, stackEnd;
		/*readonly*/ Contracts.Scripting.Debugger.ILFrameIP ilFrameIP;
		readonly SFFlags sfFlags;

		public StackFrame(Debugger debugger, CorFrame frame, int frameNo) {
			debugger.Dispatcher.VerifyAccess();
			this.debugger = debugger;
			this.frame = frame;
			this.frameNo = frameNo;
			this.hashCode = frame.GetHashCode();
			this.ilFrameIP = new Contracts.Scripting.Debugger.ILFrameIP(frame.ILFrameIP.Offset, (MappingResult)frame.ILFrameIP.Mapping);
			this.nativeFrameIP = frame.NativeFrameIP;
			this.token = frame.Token;
			this.stackStart = frame.StackStart;
			this.stackEnd = frame.StackEnd;
			this.sfFlags = 0;
			if (frame.IsILFrame)
				this.sfFlags |= SFFlags.ILFrame;
			if (frame.IsInternalFrame)
				this.sfFlags |= SFFlags.InternalFrame;
			if (frame.IsNativeFrame)
				this.sfFlags |= SFFlags.NativeFrame;
			if (frame.IsRuntimeUnwindableFrame)
				this.sfFlags |= SFFlags.RuntimeUnwindableFrame;
		}

		public IDebuggerValue GetLocal(uint index) {
			return debugger.Dispatcher.UI(() => {
				var value = frame.GetILLocal(index);
				return value == null ? null : new DebuggerValue(debugger, value);
			});
		}

		public IDebuggerValue GetLocal(int index) {
			return debugger.Dispatcher.UI(() => {
				var value = frame.GetILLocal(index);
				return value == null ? null : new DebuggerValue(debugger, value);
			});
		}

		public IDebuggerValue GetArgument(uint index) {
			return debugger.Dispatcher.UI(() => {
				var value = frame.GetILArgument(index);
				return value == null ? null : new DebuggerValue(debugger, value);
			});
		}

		public IDebuggerValue GetArgument(int index) {
			return debugger.Dispatcher.UI(() => {
				var value = frame.GetILArgument(index);
				return value == null ? null : new DebuggerValue(debugger, value);
			});
		}

		public IDebuggerValue[] GetLocals(ILCodeKind kind) {
			return debugger.Dispatcher.UI(() => {
				var list = new List<IDebuggerValue>();
				foreach (var v in frame.GetILLocals((dndbg.COM.CorDebug.ILCodeKind)kind))
					list.Add(new DebuggerValue(debugger, v));
				return list.ToArray();
			});
		}

		public IDebuggerValue GetLocal(ILCodeKind kind, uint index) {
			return debugger.Dispatcher.UI(() => {
				var value = frame.GetILLocal((dndbg.COM.CorDebug.ILCodeKind)kind, index);
				return value == null ? null : new DebuggerValue(debugger, value);
			});
		}

		public IDebuggerValue GetLocal(ILCodeKind kind, int index) {
			return debugger.Dispatcher.UI(() => {
				var value = frame.GetILLocal((dndbg.COM.CorDebug.ILCodeKind)kind, index);
				return value == null ? null : new DebuggerValue(debugger, value);
			});
		}

		public IDebuggerCode GetCode(ILCodeKind kind) {
			return debugger.Dispatcher.UI(() => {
				var code = frame.GetCode((dndbg.COM.CorDebug.ILCodeKind)kind);
				return code == null ? null : new DebuggerCode(debugger, code);
			});
		}

		public bool GetGenericArguments(out List<IDebuggerType> typeGenArgs, out List<IDebuggerType> methGenArgs) {
			List<IDebuggerType> typeGenArgsTmp = null, methGenArgsTmp = null;
			bool res = debugger.Dispatcher.UI(() => {
				List<CorType> corTypeGenArgs, corMethGenArgs;
				var res2 = frame.GetTypeAndMethodGenericParameters(out corTypeGenArgs, out corMethGenArgs);
				typeGenArgsTmp = new List<IDebuggerType>(corTypeGenArgs.Count);
				methGenArgsTmp = new List<IDebuggerType>(corMethGenArgs.Count);
				foreach (var t in corTypeGenArgs)
					typeGenArgsTmp.Add(new DebuggerType(debugger, t));
				foreach (var t in corMethGenArgs)
					methGenArgsTmp.Add(new DebuggerType(debugger, t));
				return res2;
			});
			typeGenArgs = typeGenArgsTmp;
			methGenArgs = methGenArgsTmp;
			return res;
		}

		public IStackFrame TryGetNewFrame() {
			return debugger.Dispatcher.UI(() => debugger.TryGetNewFrameUI(token, stackStart, stackEnd));
		}

		public void StepInto() {
			debugger.StepInto(this);
		}

		public Task<bool> StepIntoAsync(int millisecondsTimeout) {
			return debugger.StepIntoAsync(this, millisecondsTimeout);
		}

		public bool StepIntoWait(int millisecondsTimeout) {
			return debugger.StepIntoWait(this, millisecondsTimeout);
		}

		public bool StepIntoWait(CancellationToken token, int millisecondsTimeout) {
			return debugger.StepIntoWait(this, token, millisecondsTimeout);
		}

		public void StepOver() {
			debugger.StepOver(this);
		}

		public Task<bool> StepOverAsync(int millisecondsTimeout) {
			return debugger.StepOverAsync(this, millisecondsTimeout);
		}

		public bool StepOverWait(int millisecondsTimeout) {
			return debugger.StepOverWait(this, millisecondsTimeout);
		}

		public bool StepOverWait(CancellationToken token, int millisecondsTimeout) {
			return debugger.StepOverWait(this, token, millisecondsTimeout);
		}

		public void StepOut() {
			debugger.StepOut(this);
		}

		public Task<bool> StepOutAsync(int millisecondsTimeout) {
			return debugger.StepOutAsync(this, millisecondsTimeout);
		}

		public bool StepOutWait(int millisecondsTimeout) {
			return debugger.StepOutWait(this, millisecondsTimeout);
		}

		public bool StepOutWait(CancellationToken token, int millisecondsTimeout) {
			return debugger.StepOutWait(this, token, millisecondsTimeout);
		}

		public bool RunTo() {
			return debugger.RunTo(this);
		}

		public Task<bool> RunToAsync(int millisecondsTimeout) {
			return debugger.RunToAsync(this, millisecondsTimeout);
		}

		public bool RunToWait(int millisecondsTimeout) {
			return debugger.RunToWait(this, millisecondsTimeout);
		}

		public bool RunToWait(CancellationToken token, int millisecondsTimeout) {
			return debugger.RunToWait(this, token, millisecondsTimeout);
		}

		public bool SetOffset(int offset) {
			return debugger.SetOffset(this, offset);
		}

		public bool SetOffset(uint offset) {
			return debugger.SetOffset(this, offset);
		}

		public bool SetNativeOffset(int offset) {
			return debugger.SetNativeOffset(this, offset);
		}

		public bool SetNativeOffset(uint offset) {
			return debugger.SetNativeOffset(this, offset);
		}

		public IDebuggerValue ReadStaticField(IDebuggerField field) {
			return field.Class.ReadStaticField(this, field);
		}

		public IDebuggerValue ReadStaticField(IDebuggerClass cls, uint token) {
			return cls.ReadStaticField(this, token);
		}

		public IDebuggerValue ReadStaticField(IDebuggerType type, uint token) {
			return type.ReadStaticField(this, token);
		}

		public IDebuggerValue ReadStaticField(IDebuggerClass cls, string name, bool checkBaseClasses) {
			return cls.ReadStaticField(this, name, checkBaseClasses);
		}

		public IDebuggerValue ReadStaticField(IDebuggerType type, string name, bool checkBaseClasses) {
			return type.ReadStaticField(this, name, checkBaseClasses);
		}

		public IDebuggerValue ReadStaticField(IDebuggerType type, IDebuggerField field) {
			return type.ReadStaticField(this, field);
		}

		public override bool Equals(object obj) {
			var other = obj as StackFrame;
			return other != null && other.frame == frame;
		}

		public override int GetHashCode() {
			return hashCode;
		}

		public void Write(ISyntaxHighlightOutput output, TypeFormatFlags flags) {
			debugger.Dispatcher.UI(() => frame.Write(new OutputConverter(output), (TypePrinterFlags)flags));
		}

		public string ToString(TypeFormatFlags flags) {
			return debugger.Dispatcher.UI(() => frame.ToString((TypePrinterFlags)flags));
		}

		public override string ToString() {
			return debugger.Dispatcher.UI(() => frame.ToString());
		}
	}
}
