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
using System.Diagnostics;
using System.Linq;
using System.Text;
using dndbg.Engine;
using dndbg.Engine.COM.CorDebug;
using dnSpy.NRefactory;
using ICSharpCode.Decompiler;

namespace dnSpy.Debugger.Threads {
	sealed class ThreadPrinter {
		readonly ITextOutput output;
		readonly bool useHex;

		public ThreadPrinter(ITextOutput output, bool useHex) {
			this.output = output;
			this.useHex = useHex;
		}

		void WriteInt32(int value) {
			if (useHex)
				output.Write(string.Format("0x{0:X8}", value), TextTokenType.Number);
			else
				output.Write(string.Format("{0}", value), TextTokenType.Number);
		}

		public void WriteCurrent(ThreadVM vm) {
			output.Write(vm.IsCurrent ? '>' : ' ', TextTokenType.Text);
		}

		public void WriteId(ThreadVM vm) {
			WriteInt32(vm.Id);
		}

		public void WriteManagedId(ThreadVM vm) {
			WriteInt32(vm.ManagedId);
		}

		public void WriteCategory(ThreadVM vm) {
			switch (vm.Type) {
			case ThreadType.Unknown:
				output.Write("Unknown Thread", TextTokenType.Text);
				break;
			case ThreadType.Main:
				output.Write("Main Thread", TextTokenType.Text);
				break;
			case ThreadType.ThreadPool:
				output.Write("Thread Pool", TextTokenType.Text);
				break;
			case ThreadType.Worker:
				output.Write("Worker Thread", TextTokenType.Text);
				break;
			case ThreadType.BGCOrFinalizer:
				output.Write("BGC / Finalizer", TextTokenType.Text);
				output.Write("???", TextTokenType.Error);
				break;
			case ThreadType.Terminated:
				output.Write("Terminated Thread", TextTokenType.Text);
				break;
			default:
				Debug.Fail(string.Format("Unknown thread type: {0}", vm.Type));
				goto case ThreadType.Unknown;
			}
		}

		public void WriteName(ThreadVM vm) {
			if (vm.Name == null)
				output.Write("???", TextTokenType.Error);
			else
				output.Write(vm.Name, TextTokenType.Text);
		}

		public void WriteLocation(ThreadVM vm) {
			var frame = vm.Thread.CorThread.AllFrames.FirstOrDefault();
			if (frame == null)
				output.Write("<not available>", TextTokenType.Text);
			else {
				var flags = TypePrinterFlags.Default | TypePrinterFlags.ShowIP;
				if (!useHex)
					flags |= TypePrinterFlags.UseDecimal;
				frame.Write(new OutputConverter(output), flags);
			}
		}

		public void WritePriority(ThreadVM vm) {
			switch (vm.Priority) {
			case ThreadPriority.Lowest:			output.Write("Lowest", TextTokenType.EnumField); break;
			case ThreadPriority.BelowNormal:	output.Write("Below Normal", TextTokenType.EnumField); break;
			case ThreadPriority.Normal:			output.Write("Normal", TextTokenType.EnumField); break;
			case ThreadPriority.AboveNormal:	output.Write("Above Normal", TextTokenType.EnumField); break;
			case ThreadPriority.Highest:		output.Write("Highest", TextTokenType.EnumField); break;
			default:							output.Write(string.Format("???({0})", (int)vm.Priority), TextTokenType.Error); break;
			}
		}

		public void WriteAffinityMask(ThreadVM vm) {
			ulong affMask = (ulong)vm.AffinityMask.ToInt64();
			bool started = false;
			var sb = new StringBuilder();
			for (ulong bitMask = 1UL << 63; bitMask != 0; bitMask >>= 1) {
				if (!started && bitMask == 0x8000)
					started = true;
				if ((affMask & bitMask) != 0) {
					started = true;
					sb.Append('1');
				}
				else {
					if (started)
						sb.Append('0');
				}
			}
			output.Write(sb.ToString(), TextTokenType.Number);
		}

		public void WriteSuspended(ThreadVM vm) {
			output.WriteYesNo(vm.IsSuspended);
		}

		public void WriteProcess(ThreadVM vm) {
			output.Write(vm.Thread.Process, useHex);
		}

		public void WriteAppDomain(ThreadVM vm) {
			output.Write(vm.AppDomain);
		}

		static readonly Tuple<CorDebugUserState, string>[] UserStates = new Tuple<CorDebugUserState, string>[] {
			Tuple.Create(CorDebugUserState.USER_STOP_REQUESTED, "StopRequested"),
			Tuple.Create(CorDebugUserState.USER_SUSPEND_REQUESTED, "SuspendRequested"),
			Tuple.Create(CorDebugUserState.USER_BACKGROUND, "Background"),
			Tuple.Create(CorDebugUserState.USER_UNSTARTED, "Unstarted"),
			Tuple.Create(CorDebugUserState.USER_STOPPED, "Stopped"),
			Tuple.Create(CorDebugUserState.USER_WAIT_SLEEP_JOIN, "WaitSleepJoin"),
			Tuple.Create(CorDebugUserState.USER_SUSPENDED, "Suspended"),
			Tuple.Create(CorDebugUserState.USER_UNSAFE_POINT, "UnsafePoint"),
			Tuple.Create(CorDebugUserState.USER_THREADPOOL, "ThreadPool"),
		};

		public void WriteUserState(ThreadVM vm) {
			var state = vm.UserState;
			bool needComma = false;
			foreach (var t in UserStates) {
				if ((state & t.Item1) != 0) {
					state &= ~t.Item1;
					if (needComma)
						output.WriteCommaSpace();
					needComma = true;
					output.Write(t.Item2, TextTokenType.EnumField);
				}
			}
			if (state != 0) {
				if (needComma)
					output.WriteCommaSpace();
				output.Write(string.Format("0x{0:X}", (int)state), TextTokenType.Number);
			}
		}
	}
}
