/*
    Copyright (C) 2014-2015 de4dot@gmail.com

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
using dndbg.Engine.COM.CorDebug;

namespace dndbg.Engine {
	public sealed class CorFrame : COMObject<ICorDebugFrame>, IEquatable<CorFrame> {
		/// <summary>
		/// Gets the frame that this frame called or null
		/// </summary>
		public CorFrame Callee {
			get {
				ICorDebugFrame calleeFrame;
				int hr = obj.GetCallee(out calleeFrame);
				return hr < 0 || calleeFrame == null ? null : new CorFrame(calleeFrame);
			}
		}

		/// <summary>
		/// Gets the frame that called this frame or null
		/// </summary>
		public CorFrame Caller {
			get {
				ICorDebugFrame callerFrame;
				int hr = obj.GetCaller(out callerFrame);
				return hr < 0 || callerFrame == null ? null : new CorFrame(callerFrame);
			}
		}

		/// <summary>
		/// Gets the chain that this frame is part of
		/// </summary>
		public CorChain Chain {
			get {
				ICorDebugChain chain;
				int hr = obj.GetChain(out chain);
				return hr < 0 || chain == null ? null : new CorChain(chain);
			}
		}

		/// <summary>
		/// Gets the token of the method or 0
		/// </summary>
		public uint Token {
			get { return token; }
		}
		readonly uint token;

		/// <summary>
		/// Start address of the stack segment
		/// </summary>
		public ulong StackStart {
			get { return rangeStart; }
		}
		readonly ulong rangeStart;

		/// <summary>
		/// End address of the stack segment
		/// </summary>
		public ulong StackEnd {
			get { return rangeEnd; }
		}
		readonly ulong rangeEnd;

		/// <summary>
		/// true if this is an IL frame (<see cref="ICorDebugILFrame"/>)
		/// </summary>
		public bool IsILFrame {
			get { return obj is ICorDebugILFrame; }
		}

		/// <summary>
		/// true if this is a Native frame (<see cref="ICorDebugNativeFrame"/>). This can be true
		/// even if <see cref="IsILFrame"/> is true (it's a JIT-compiled frame).
		/// </summary>
		public bool IsNativeFrame {
			get { return obj is ICorDebugNativeFrame; }
		}

		/// <summary>
		/// true if it's a JIT-compiled frame (<see cref="IsILFrame"/> and <see cref="IsNativeFrame"/>
		/// are both true).
		/// </summary>
		public bool IsJITCompiledFrame {
			get { return IsILFrame && IsNativeFrame; }
		}

		/// <summary>
		/// true if this is an internal frame (<see cref="ICorDebugInternalFrame"/>)
		/// </summary>
		public bool IsInternalFrame {
			get { return obj is ICorDebugInternalFrame; }
		}

		/// <summary>
		/// true if this is a runtime unwindable frame (<see cref="ICorDebugRuntimeUnwindableFrame"/>)
		/// </summary>
		public bool IsRuntimeUnwindableFrame {
			get { return obj is ICorDebugRuntimeUnwindableFrame; }
		}

		/// <summary>
		/// Gets the IL frame IP. Only valid if <see cref="IsILFrame"/> is true
		/// </summary>
		public ILFrameIP ILFrameIP {
			get {
				var ilf = obj as ICorDebugILFrame;
				if (ilf == null)
					return new ILFrameIP();
				uint offset;
				CorDebugMappingResult mappingResult;
				int hr = ilf.GetIP(out offset, out mappingResult);
				return hr < 0 ? new ILFrameIP() : new ILFrameIP(offset, mappingResult);
			}
		}

		/// <summary>
		/// Gets the native frame IP. Only valid if <see cref="IsNativeFrame"/> is true
		/// </summary>
		public uint NativeFrameIP {
			get {
				var nf = obj as ICorDebugNativeFrame;
				if (nf == null)
					return 0;
				uint offset;
				int hr = nf.GetIP(out offset);
				return hr < 0 ? 0 : offset;
			}
		}

		/// <summary>
		/// Gets the internal frame type or <see cref="CorDebugInternalFrameType.STUBFRAME_NONE"/>
		/// if it's not an internal frame (<see cref="ICorDebugInternalFrame"/>)
		/// </summary>
		public CorDebugInternalFrameType InternalFrameType {
			get {
				var @if = obj as ICorDebugInternalFrame;
				if (@if == null)
					return CorDebugInternalFrameType.STUBFRAME_NONE;
				CorDebugInternalFrameType type;
				int hr = @if.GetFrameType(out type);
				return hr < 0 ? CorDebugInternalFrameType.STUBFRAME_NONE : type;
			}
		}

		/// <summary>
		/// Gets the function or null
		/// </summary>
		public CorFunction Function {
			get {
				ICorDebugFunction func;
				int hr = obj.GetFunction(out func);
				return hr < 0 || func == null ? null : new CorFunction(func);
			}
		}

		/// <summary>
		/// Gets the code or null
		/// </summary>
		public CorCode Code {
			get {
				ICorDebugCode code;
				int hr = obj.GetCode(out code);
				return hr < 0 || code == null ? null : new CorCode(code);
			}
		}

		internal CorFrame(ICorDebugFrame frame)
			: base(frame) {
			int hr = frame.GetFunctionToken(out this.token);
			if (hr < 0)
				this.token = 0;

			hr = frame.GetStackRange(out this.rangeStart, out this.rangeEnd);
			if (hr < 0)
				this.rangeStart = this.rangeEnd = 0;

			//TODO: ICorDebugILFrame, ICorDebugILFrame2, ICorDebugILFrame3, ICorDebugILFrame4
			//TODO: ICorDebugInternalFrame, ICorDebugInternalFrame2
			//TODO: ICorDebugNativeFrame, ICorDebugNativeFrame2
			//TODO: ICorDebugRuntimeUnwindableFrame
		}

		public CorStepper CreateStepper() {
			ICorDebugStepper stepper;
			int hr = obj.CreateStepper(out stepper);
			return hr < 0 || stepper == null ? null : new CorStepper(stepper);
		}

		/// <summary>
		/// Gets the module of the function or null
		/// </summary>
		public SerializedDnModuleWithAssembly? GetSerializedDnModuleWithAssembly() {
			var func = Function;
			if (func == null)
				return null;

			var module = func.Module;
			if (module == null)
				return null;

			return module.SerializedDnModuleWithAssembly;
		}

		/// <summary>
		/// Sets a new IL offset. All frames and chains for the current thread will be invalidated
		/// after this call. This method can only be called if <see cref="IsILFrame"/> is true.
		/// </summary>
		/// <param name="ilOffset">New IL offset</param>
		/// <returns></returns>
		public bool SetILFrameIP(uint ilOffset) {
			var ilf = obj as ICorDebugILFrame;
			if (ilf == null)
				return false;
			int hr = ilf.SetIP(ilOffset);
			return hr >= 0;
		}

		/// <summary>
		/// Returns true if it's safe to call <see cref="SetILFrameIP(uint)"/> but it can still be
		/// called if this method fails. This method can only be called if <see cref="IsILFrame"/>
		/// is true.
		/// </summary>
		/// <param name="ilOffset">IL offset</param>
		/// <returns></returns>
		public bool CanSetILFrameIP(uint ilOffset) {
			var ilf = obj as ICorDebugILFrame;
			if (ilf == null)
				return false;
			return ilf.CanSetIP(ilOffset) == 0;
		}

		/// <summary>
		/// Sets a new native offset. All frames and chains for the current thread will be invalidated
		/// after this call. This method can only be called if <see cref="IsNativeFrame"/> is true.
		/// </summary>
		/// <param name="offset">New offset</param>
		/// <returns></returns>
		public bool SetNativeFrameIP(uint offset) {
			var nf = obj as ICorDebugNativeFrame;
			if (nf == null)
				return false;
			int hr = nf.SetIP(offset);
			return hr >= 0;
		}

		/// <summary>
		/// Returns true if it's safe to call <see cref="SetNativeFrameIP(uint)"/> but it can still be
		/// called if this method fails. This method can only be called if <see cref="IsNativeFrame"/>
		/// is true.
		/// </summary>
		/// <param name="offset">Offset</param>
		/// <returns></returns>
		public bool CanSetNativeFrameIP(uint offset) {
			var nf = obj as ICorDebugNativeFrame;
			if (nf == null)
				return false;
			return nf.CanSetIP(offset) == 0;
		}

		public static bool operator ==(CorFrame a, CorFrame b) {
			if (ReferenceEquals(a, b))
				return true;
			if (ReferenceEquals(a, null) || ReferenceEquals(b, null))
				return false;
			return a.Equals(b);
		}

		public static bool operator !=(CorFrame a, CorFrame b) {
			return !(a == b);
		}

		public bool Equals(CorFrame other) {
			return !ReferenceEquals(other, null) &&
				RawObject == other.RawObject;
		}

		public override bool Equals(object obj) {
			return Equals(obj as CorFrame);
		}

		public override int GetHashCode() {
			return RawObject.GetHashCode();
		}

		public override string ToString() {
			return string.Format("[Frame] Token: {0:X8} {1:X8}-{2:X8}", Token, StackStart, StackEnd);
		}
	}
}
