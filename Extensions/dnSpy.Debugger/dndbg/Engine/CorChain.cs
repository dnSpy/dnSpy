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
using dndbg.COM.CorDebug;

namespace dndbg.Engine {
	public sealed class CorChain : COMObject<ICorDebugChain>, IEquatable<CorChain> {
		/// <summary>
		/// Gets the thread or null
		/// </summary>
		public CorThread Thread {
			get {
				int hr = obj.GetThread(out var thread);
				return hr < 0 || thread == null ? null : new CorThread(thread);
			}
		}

		/// <summary>
		/// true if this is a managed chain
		/// </summary>
		public bool IsManaged { get; }

		/// <summary>
		/// Gets the reason
		/// </summary>
		public CorDebugChainReason Reason => reason;
		readonly CorDebugChainReason reason;

		/// <summary>
		/// Start address of the stack segment
		/// </summary>
		public ulong StackStart => rangeStart;
		readonly ulong rangeStart;

		/// <summary>
		/// End address of the stack segment
		/// </summary>
		public ulong StackEnd => rangeEnd;
		readonly ulong rangeEnd;

		/// <summary>
		/// Gets the active frame or null
		/// </summary>
		public CorFrame ActiveFrame {
			get {
				int hr = obj.GetActiveFrame(out var frame);
				return hr < 0 || frame == null ? null : new CorFrame(frame);
			}
		}

		/// <summary>
		/// Gets the callee or null
		/// </summary>
		public CorChain Callee {
			get {
				int hr = obj.GetCallee(out var callee);
				return hr < 0 || callee == null ? null : new CorChain(callee);
			}
		}

		/// <summary>
		/// Gets the caller or null
		/// </summary>
		public CorChain Caller {
			get {
				int hr = obj.GetCaller(out var caller);
				return hr < 0 || caller == null ? null : new CorChain(caller);
			}
		}

		/// <summary>
		/// Gets the next chain or null
		/// </summary>
		public CorChain Next {
			get {
				int hr = obj.GetNext(out var next);
				return hr < 0 || next == null ? null : new CorChain(next);
			}
		}

		/// <summary>
		/// Gets the previous chain or null
		/// </summary>
		public CorChain Previous {
			get {
				int hr = obj.GetPrevious(out var prev);
				return hr < 0 || prev == null ? null : new CorChain(prev);
			}
		}

		/// <summary>
		/// Gets all frames
		/// </summary>
		public IEnumerable<CorFrame> Frames {
			get {
				int hr = obj.EnumerateFrames(out var frameEnum);
				if (hr < 0)
					yield break;
				for (;;) {
					hr = frameEnum.Next(1, out var frame, out uint count);
					if (hr != 0 || frame == null)
						break;
					yield return new CorFrame(frame);
				}
			}
		}

		public CorChain(ICorDebugChain chain)
			: base(chain) {
			int hr = chain.IsManaged(out int isManaged);
			IsManaged = hr >= 0 && isManaged != 0;

			hr = chain.GetReason(out reason);
			if (hr < 0)
				reason = 0;

			hr = chain.GetStackRange(out rangeStart, out rangeEnd);
			if (hr < 0)
				rangeStart = rangeEnd = 0;

			//TODO: ICorDebugChain::GetContext
			//TODO: ICorDebugChain::GetRegisterSet
		}

		public static bool operator ==(CorChain a, CorChain b) {
			if (ReferenceEquals(a, b))
				return true;
			if (ReferenceEquals(a, null) || ReferenceEquals(b, null))
				return false;
			return a.Equals(b);
		}

		public static bool operator !=(CorChain a, CorChain b) => !(a == b);
		public bool Equals(CorChain other) => !ReferenceEquals(other, null) && RawObject == other.RawObject;
		public override bool Equals(object obj) => Equals(obj as CorChain);
		public override int GetHashCode() => RawObject.GetHashCode();
		public override string ToString() => string.Format("[Chain] Managed={0} {1:X8}-{2:X8} {3}", IsManaged ? 1 : 0, StackStart, StackEnd, Reason);
	}
}
