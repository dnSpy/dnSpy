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
using dndbg.COM.CorDebug;

namespace dndbg.Engine {
	sealed class CorChain : COMObject<ICorDebugChain>, IEquatable<CorChain?> {
		public CorThread? Thread {
			get {
				int hr = obj.GetThread(out var thread);
				return hr < 0 || thread is null ? null : new CorThread(thread);
			}
		}

		public bool IsManaged { get; }

		public CorDebugChainReason Reason => reason;
		readonly CorDebugChainReason reason;

		public ulong StackStart => rangeStart;
		readonly ulong rangeStart;

		public ulong StackEnd => rangeEnd;
		readonly ulong rangeEnd;

		public CorFrame? ActiveFrame {
			get {
				int hr = obj.GetActiveFrame(out var frame);
				return hr < 0 || frame is null ? null : new CorFrame(frame);
			}
		}

		public CorChain? Callee {
			get {
				int hr = obj.GetCallee(out var callee);
				return hr < 0 || callee is null ? null : new CorChain(callee);
			}
		}

		public CorChain? Caller {
			get {
				int hr = obj.GetCaller(out var caller);
				return hr < 0 || caller is null ? null : new CorChain(caller);
			}
		}

		public CorChain? Next {
			get {
				int hr = obj.GetNext(out var next);
				return hr < 0 || next is null ? null : new CorChain(next);
			}
		}

		public CorChain? Previous {
			get {
				int hr = obj.GetPrevious(out var prev);
				return hr < 0 || prev is null ? null : new CorChain(prev);
			}
		}

		public IEnumerable<CorFrame> Frames => GetFrames(new ICorDebugFrame[1]);

		public IEnumerable<CorFrame> GetFrames(ICorDebugFrame[] frames) {
			int hr = obj.EnumerateFrames(out var frameEnum);
			if (hr < 0)
				yield break;
			for (;;) {
				hr = frameEnum.Next((uint)frames.Length, frames, out uint count);
				if (hr < 0 || count == 0)
					break;
				int count2 = (int)count;
				for (int i = 0; i < count2; i++)
					yield return new CorFrame(frames[i]);
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
		}

		public bool Equals(CorChain? other) => other is not null && RawObject == other.RawObject;
		public override bool Equals(object? obj) => Equals(obj as CorChain);
		public override int GetHashCode() => RawObject.GetHashCode();
		public override string ToString() => $"[Chain] Managed={(IsManaged ? 1 : 0)} {StackStart:X8}-{StackEnd:X8} {Reason}";
	}
}
