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
	public sealed class DnFrame : IEquatable<DnFrame> {
		/// <summary>
		/// Gets the COM object
		/// </summary>
		public ICorDebugFrame RawObject {
			get { return frame; }
		}
		readonly ICorDebugFrame frame;

		/// <summary>
		/// Gets the frame that this frame called or null
		/// </summary>
		public DnFrame Callee {
			get {
				ICorDebugFrame calleeFrame;
				int hr = frame.GetCallee(out calleeFrame);
				return hr < 0 || calleeFrame == null ? null : new DnFrame(calleeFrame);
			}
		}

		/// <summary>
		/// Gets the frame that called this frame or null
		/// </summary>
		public DnFrame Caller {
			get {
				ICorDebugFrame callerFrame;
				int hr = frame.GetCaller(out callerFrame);
				return hr < 0 || callerFrame == null ? null : new DnFrame(callerFrame);
			}
		}

		/// <summary>
		/// Gets the chain that this frame is part of
		/// </summary>
		public DnChain Chain {
			get {
				ICorDebugChain chain;
				int hr = frame.GetChain(out chain);
				return hr < 0 || chain == null ? null : new DnChain(chain);
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
			get { return frame is ICorDebugILFrame; }
		}

		/// <summary>
		/// true if this is a Native frame (<see cref="ICorDebugNativeFrame"/>). This can be true
		/// even if <see cref="IsILFrame"/> is true (it's a JIT-compiled frame).
		/// </summary>
		public bool IsNativeFrame {
			get { return frame is ICorDebugNativeFrame; }
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
			get { return frame is ICorDebugInternalFrame; }
		}

		/// <summary>
		/// true if this is a runtime unwindable frame (<see cref="ICorDebugRuntimeUnwindableFrame"/>)
		/// </summary>
		public bool IsRuntimeUnwindableFrame {
			get { return frame is ICorDebugRuntimeUnwindableFrame; }
		}

		/// <summary>
		/// Gets the IL frame IP. Only valid if <see cref="IsILFrame"/> is true
		/// </summary>
		public ILFrameIP ILFrameIP {
			get {
				var ilf = frame as ICorDebugILFrame;
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
				var nf = frame as ICorDebugNativeFrame;
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
				var @if = frame as ICorDebugInternalFrame;
				if (@if == null)
					return CorDebugInternalFrameType.STUBFRAME_NONE;
				CorDebugInternalFrameType type;
				int hr = @if.GetFrameType(out type);
				return hr < 0 ? CorDebugInternalFrameType.STUBFRAME_NONE : type;
			}
		}

		internal DnFrame(ICorDebugFrame frame) {
			this.frame = frame;

			int hr = frame.GetFunctionToken(out this.token);
			if (hr < 0)
				this.token = 0;

			hr = frame.GetStackRange(out this.rangeStart, out this.rangeEnd);
			if (hr < 0)
				this.rangeStart = this.rangeEnd = 0;

			//TODO: ICorDebugFrame::GetCode
			//TODO: ICorDebugFrame::GetFunction
			//TODO: ICorDebugILFrame, ICorDebugILFrame2, ICorDebugILFrame3, ICorDebugILFrame4
			//TODO: ICorDebugInternalFrame, ICorDebugInternalFrame2
			//TODO: ICorDebugNativeFrame, ICorDebugNativeFrame2
			//TODO: ICorDebugRuntimeUnwindableFrame
		}

		/// <summary>
		/// Gets the module of the function or null
		/// </summary>
		public SerializedDnModuleWithAssembly? GetSerializedDnModuleWithAssembly() {
			ICorDebugFunction func;
			int hr = frame.GetFunction(out func);
			if (hr < 0)
				return null;

			ICorDebugModule module;
			hr = func.GetModule(out module);
			if (hr < 0)
				return null;

			return DnModule.GetSerializedDnModuleWithAssembly(module);
		}

		/// <summary>
		/// Sets a new IL offset. All frames and chains for the current thread will be invalidated
		/// after this call. This method can only be called if <see cref="IsILFrame"/> is true.
		/// </summary>
		/// <param name="ilOffset">New IL offset</param>
		/// <returns></returns>
		public bool SetILFrameIP(uint ilOffset) {
			var ilf = frame as ICorDebugILFrame;
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
			var ilf = frame as ICorDebugILFrame;
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
			var nf = frame as ICorDebugNativeFrame;
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
			var nf = frame as ICorDebugNativeFrame;
			if (nf == null)
				return false;
			return nf.CanSetIP(offset) == 0;
		}

		public static bool operator ==(DnFrame a, DnFrame b) {
			if (ReferenceEquals(a, b))
				return true;
			if (ReferenceEquals(a, null) || ReferenceEquals(b, null))
				return false;
			return a.Equals(b);
		}

		public static bool operator !=(DnFrame a, DnFrame b) {
			return !(a == b);
		}

		public bool Equals(DnFrame other) {
			return other != null &&
				RawObject == other.RawObject;
		}

		public override bool Equals(object obj) {
			return Equals(obj as DnFrame);
		}

		public override int GetHashCode() {
			return RawObject.GetHashCode();
		}

		public override string ToString() {
			return string.Format("[Frame] Token: {0:X8} {1:X8}-{2:X8}", Token, StackStart, StackEnd);
		}
	}
}
