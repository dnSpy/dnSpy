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

using System.Collections.Generic;
using dndbg.Engine;
using dnSpy.Contracts.Scripting;
using dnSpy.Contracts.Scripting.Debugger;

namespace dnSpy.Debugger.Scripting {
	sealed class StackChain : IStackChain {
		int GetStartFrameNumber() {
			if (startFrameNumber >= 0)
				return startFrameNumber;
			int frameNo = 0;
			var c = Previous;
			while (c != null) {
				foreach (var f in c.Frames)
					frameNo++;
				c = c.Previous;
			}
			return startFrameNumber = frameNo;
		}
		int startFrameNumber = -1;

		public IStackFrame ActiveFrame => debugger.Dispatcher.UI(() => {
			int frameNo = GetStartFrameNumber();
			var frame = chain.ActiveFrame;
			return frame == null ? null : new StackFrame(debugger, frame, frameNo);
		});

		public IStackChain Callee => debugger.Dispatcher.UI(() => {
			var c = chain.Callee;
			return c == null ? null : new StackChain(debugger, c);
		});

		public IStackChain Caller => debugger.Dispatcher.UI(() => {
			var c = chain.Caller;
			return c == null ? null : new StackChain(debugger, c);
		});

		public IEnumerable<IStackFrame> Frames => debugger.Dispatcher.UIIter(GetFramesUI);

		IEnumerable<IStackFrame> GetFramesUI() {
			int frameNo = GetStartFrameNumber();
			foreach (var f in chain.Frames)
				yield return new StackFrame(debugger, f, frameNo++);
		}

		public bool IsManaged => isManaged;

		public IStackChain Next => debugger.Dispatcher.UI(() => {
			var c = chain.Next;
			return c == null ? null : new StackChain(debugger, c);
		});

		public IStackChain Previous => debugger.Dispatcher.UI(() => {
			var c = chain.Previous;
			return c == null ? null : new StackChain(debugger, c);
		});

		public ChainReason Reason => reason;
		public ulong StackEnd => stackEnd;
		public ulong StackStart => stackStart;
		public IDebuggerThread Thread => debugger.Dispatcher.UI(() => debugger.FindThreadUI(chain.Thread));

		readonly Debugger debugger;
		readonly CorChain chain;
		readonly int hashCode;
		readonly bool isManaged;
		readonly ChainReason reason;
		readonly ulong stackStart;
		readonly ulong stackEnd;

		public StackChain(Debugger debugger, CorChain chain) {
			debugger.Dispatcher.VerifyAccess();
			this.debugger = debugger;
			this.chain = chain;
			this.hashCode = chain.GetHashCode();
			this.isManaged = chain.IsManaged;
			this.reason = (ChainReason)chain.Reason;
			this.stackStart = chain.StackStart;
			this.stackEnd = chain.StackEnd;
		}

		public override bool Equals(object obj) => (obj as StackChain)?.chain == chain;
		public override int GetHashCode() => hashCode;
		public override string ToString() => debugger.Dispatcher.UI(() => chain.ToString());
	}
}
