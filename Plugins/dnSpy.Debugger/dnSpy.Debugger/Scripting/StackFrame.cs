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
using dnSpy.Contracts.Scripting;
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

		public IStackChain Chain => debugger.Dispatcher.UI(() => {
			var chain = CorFrame.Chain;
			return chain == null ? null : new StackChain(debugger, chain);
		});

		public bool IsNeutered => debugger.Dispatcher.UI(() => CorFrame.IsNeutered);
		public Contracts.Scripting.Debugger.ILFrameIP ILFrameIP => ilFrameIP;
		public InternalFrameType InternalFrameType => debugger.Dispatcher.UI(() => (InternalFrameType)CorFrame.InternalFrameType);
		public bool IsILFrame => (sfFlags & SFFlags.ILFrame) != 0;
		public bool IsInternalFrame => (sfFlags & SFFlags.InternalFrame) != 0;
		public bool IsJITCompiledFrame => (sfFlags & (SFFlags.ILFrame | SFFlags.NativeFrame)) == (SFFlags.ILFrame | SFFlags.NativeFrame);
		public bool IsNativeFrame => (sfFlags & SFFlags.NativeFrame) != 0;
		public bool IsRuntimeUnwindableFrame => (sfFlags & SFFlags.RuntimeUnwindableFrame) != 0;

		public uint NativeOffset {
			get { return nativeFrameIP; }
			set { SetNativeOffset(value); }
		}

		public ulong StackEnd => stackEnd;
		public ulong StackStart => stackStart;
		public uint Token => token;

		public int Index => frameNo;
		int frameNo;

		public IDebuggerMethod Method => debugger.Dispatcher.UI(() => {
			var func = CorFrame.Function;
			return func == null ? null : new DebuggerMethod(debugger, func);
		});

		public IDebuggerCode ILCode => debugger.Dispatcher.UI(() => {
			var func = CorFrame.Function;
			var code = func == null ? null : func.ILCode;
			return code == null ? null : new DebuggerCode(debugger, code);
		});

		public IDebuggerCode Code => debugger.Dispatcher.UI(() => {
			var code = CorFrame.Code;
			return code == null ? null : new DebuggerCode(debugger, code);
		});

		public IDebuggerValue[] Arguments => debugger.Dispatcher.UI(() => {
			var list = new List<IDebuggerValue>();
			foreach (var v in CorFrame.ILArguments)
				list.Add(new DebuggerValue(debugger, v));
			return list.ToArray();
		});

		public IDebuggerValue[] Locals => debugger.Dispatcher.UI(() => {
			var list = new List<IDebuggerValue>();
			foreach (var v in CorFrame.ILLocals)
				list.Add(new DebuggerValue(debugger, v));
			return list.ToArray();
		});

		public IDebuggerType[] GenericArguments => debugger.Dispatcher.UI(() => {
			var list = new List<IDebuggerType>();
			foreach (var t in CorFrame.TypeParameters)
				list.Add(new DebuggerType(debugger, t));
			return list.ToArray();
		});

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
			get {
				if (frame.IsNeutered) {
					var t = debugger.TryGetNewCorFrameUI(token, stackStart, stackEnd);
					if (t != null) {
						frame = t.Item1;
						frameNo = t.Item2;
					}
				}
				return frame;
			}
		}
		CorFrame frame;

		readonly Debugger debugger;
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

		public IDebuggerValue GetLocal(uint index) => debugger.Dispatcher.UI(() => {
			var value = CorFrame.GetILLocal(index);
			return value == null ? null : new DebuggerValue(debugger, value);
		});

		public IDebuggerValue GetLocal(int index) => debugger.Dispatcher.UI(() => {
			var value = CorFrame.GetILLocal(index);
			return value == null ? null : new DebuggerValue(debugger, value);
		});

		public IDebuggerValue GetArgument(uint index) => debugger.Dispatcher.UI(() => {
			var value = CorFrame.GetILArgument(index);
			return value == null ? null : new DebuggerValue(debugger, value);
		});

		public IDebuggerValue GetArgument(int index) => debugger.Dispatcher.UI(() => {
			var value = CorFrame.GetILArgument(index);
			return value == null ? null : new DebuggerValue(debugger, value);
		});

		public IDebuggerValue[] GetLocals(ILCodeKind kind) => debugger.Dispatcher.UI(() => {
			var list = new List<IDebuggerValue>();
			foreach (var v in CorFrame.GetILLocals((dndbg.COM.CorDebug.ILCodeKind)kind))
				list.Add(new DebuggerValue(debugger, v));
			return list.ToArray();
		});

		public IDebuggerValue GetLocal(ILCodeKind kind, uint index) => debugger.Dispatcher.UI(() => {
			var value = CorFrame.GetILLocal((dndbg.COM.CorDebug.ILCodeKind)kind, index);
			return value == null ? null : new DebuggerValue(debugger, value);
		});

		public IDebuggerValue GetLocal(ILCodeKind kind, int index) => debugger.Dispatcher.UI(() => {
			var value = CorFrame.GetILLocal((dndbg.COM.CorDebug.ILCodeKind)kind, index);
			return value == null ? null : new DebuggerValue(debugger, value);
		});

		public IDebuggerCode GetCode(ILCodeKind kind) => debugger.Dispatcher.UI(() => {
			var code = CorFrame.GetCode((dndbg.COM.CorDebug.ILCodeKind)kind);
			return code == null ? null : new DebuggerCode(debugger, code);
		});

		public bool GetGenericArguments(out List<IDebuggerType> typeGenArgs, out List<IDebuggerType> methGenArgs) {
			List<IDebuggerType> typeGenArgsTmp = null, methGenArgsTmp = null;
			bool res = debugger.Dispatcher.UI(() => {
				List<CorType> corTypeGenArgs, corMethGenArgs;
				var res2 = CorFrame.GetTypeAndMethodGenericParameters(out corTypeGenArgs, out corMethGenArgs);
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

		public void StepInto() {
			debugger.StepInto(this);
		}

		public Task<bool> StepIntoAsync(int millisecondsTimeout) => debugger.StepIntoAsync(this, millisecondsTimeout);
		public bool StepIntoWait(int millisecondsTimeout) => debugger.StepIntoWait(this, millisecondsTimeout);
		public bool StepIntoWait(CancellationToken token, int millisecondsTimeout) => debugger.StepIntoWait(this, token, millisecondsTimeout);
		public void StepOver() => debugger.StepOver(this);
		public Task<bool> StepOverAsync(int millisecondsTimeout) => debugger.StepOverAsync(this, millisecondsTimeout);
		public bool StepOverWait(int millisecondsTimeout) => debugger.StepOverWait(this, millisecondsTimeout);
		public bool StepOverWait(CancellationToken token, int millisecondsTimeout) => debugger.StepOverWait(this, token, millisecondsTimeout);
		public void StepOut() => debugger.StepOut(this);
		public Task<bool> StepOutAsync(int millisecondsTimeout) => debugger.StepOutAsync(this, millisecondsTimeout);
		public bool StepOutWait(int millisecondsTimeout) => debugger.StepOutWait(this, millisecondsTimeout);
		public bool StepOutWait(CancellationToken token, int millisecondsTimeout) => debugger.StepOutWait(this, token, millisecondsTimeout);
		public bool RunTo() => debugger.RunTo(this);
		public Task<bool> RunToAsync(int millisecondsTimeout) => debugger.RunToAsync(this, millisecondsTimeout);
		public bool RunToWait(int millisecondsTimeout) => debugger.RunToWait(this, millisecondsTimeout);
		public bool RunToWait(CancellationToken token, int millisecondsTimeout) => debugger.RunToWait(this, token, millisecondsTimeout);
		public bool SetOffset(int offset) => debugger.SetOffset(this, offset);
		public bool SetOffset(uint offset) => debugger.SetOffset(this, offset);
		public bool SetNativeOffset(int offset) => debugger.SetNativeOffset(this, offset);
		public bool SetNativeOffset(uint offset) => debugger.SetNativeOffset(this, offset);
		public IDebuggerValue ReadStaticField(IDebuggerField field) => field.Class.ReadStaticField(this, field);
		public IDebuggerValue ReadStaticField(IDebuggerClass cls, uint token) => cls.ReadStaticField(this, token);
		public IDebuggerValue ReadStaticField(IDebuggerType type, uint token) => type.ReadStaticField(this, token);
		public IDebuggerValue ReadStaticField(IDebuggerClass cls, string name, bool checkBaseClasses) => cls.ReadStaticField(this, name, checkBaseClasses);
		public IDebuggerValue ReadStaticField(IDebuggerType type, string name, bool checkBaseClasses) => type.ReadStaticField(this, name, checkBaseClasses);
		public IDebuggerValue ReadStaticField(IDebuggerType type, IDebuggerField field) => type.ReadStaticField(this, field);
		public override bool Equals(object obj) => (obj as StackFrame)?.CorFrame == CorFrame;
		public override int GetHashCode() => hashCode;
		const TypePrinterFlags DEFAULT_FLAGS = TypePrinterFlags.Default;
		public void WriteTo(IOutputWriter output) => Write(output, (TypeFormatFlags)DEFAULT_FLAGS);
		public void Write(IOutputWriter output, TypeFormatFlags flags) =>
			debugger.Dispatcher.UI(() => CorFrame.Write(new OutputWriterConverter(output), (TypePrinterFlags)flags));
		public string ToString(TypeFormatFlags flags) => debugger.Dispatcher.UI(() => CorFrame.ToString((TypePrinterFlags)flags));
		public override string ToString() => debugger.Dispatcher.UI(() => CorFrame.ToString(DEFAULT_FLAGS));
	}
}
