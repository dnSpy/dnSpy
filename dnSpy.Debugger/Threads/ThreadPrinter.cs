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
using System.Diagnostics;
using System.Linq;
using System.Text;
using dndbg.COM.CorDebug;
using dndbg.Engine;
using dnSpy.Contracts.Highlighting;
using dnSpy.Debugger.Properties;
using dnSpy.Decompiler.Shared;
using dnSpy.Shared.Highlighting;

namespace dnSpy.Debugger.Threads {
	sealed class ThreadPrinter {
		const int MAX_THREAD_NAME = 128;

		readonly ISyntaxHighlightOutput output;
		readonly bool useHex;
		readonly DnDebugger dbg;

		public ThreadPrinter(ISyntaxHighlightOutput output, bool useHex, DnDebugger dbg) {
			this.output = output;
			this.useHex = useHex;
			this.dbg = dbg;
		}

		void WriteInt32(int value) {
			if (useHex)
				output.Write(string.Format("0x{0:X8}", value), TextTokenKind.Number);
			else
				output.Write(string.Format("{0}", value), TextTokenKind.Number);
		}

		public void WriteCurrent(ThreadVM vm) {
			output.Write(vm.IsCurrent ? ">" : " ", TextTokenKind.Text);
		}

		public void WriteId(ThreadVM vm) {
			WriteInt32(vm.Id);
		}

		public void WriteManagedId(ThreadVM vm) {
			var id = vm.ManagedId;
			if (id != null)
				WriteInt32(id.Value);
			else {
				output.Write("???", TextTokenKind.Error);
				WriteInt32(vm.Thread.UniqueIdProcess + 1);
			}
		}

		public void WriteCategory(ThreadVM vm) {
			switch (vm.Type) {
			case ThreadType.Unknown:
				output.Write(dnSpy_Debugger_Resources.ThreadType_Unknown, TextTokenKind.Text);
				break;
			case ThreadType.Main:
				output.Write(dnSpy_Debugger_Resources.ThreadType_Main, TextTokenKind.Text);
				break;
			case ThreadType.ThreadPool:
				output.Write(dnSpy_Debugger_Resources.ThreadType_ThreadPool, TextTokenKind.Text);
				break;
			case ThreadType.Worker:
				output.Write(dnSpy_Debugger_Resources.ThreadType_Worker, TextTokenKind.Text);
				break;
			case ThreadType.BGCOrFinalizer:
				output.Write(dnSpy_Debugger_Resources.ThreadType_BackgroundGC_or_Finalizer, TextTokenKind.Text);
				output.Write("???", TextTokenKind.Error);
				break;
			case ThreadType.Terminated:
				output.Write(dnSpy_Debugger_Resources.ThreadType_Terminated, TextTokenKind.Text);
				break;
			default:
				Debug.Fail(string.Format("Unknown thread type: {0}", vm.Type));
				goto case ThreadType.Unknown;
			}
		}

		public void WriteName(ThreadVM vm) {
			var name = vm.Name;
			if (vm.UnknownName)
				output.Write("???", TextTokenKind.Error);
			else if (name == null)
				output.Write(dnSpy_Debugger_Resources.Thread_NoName, TextTokenKind.Text);
			else
				output.Write(DebugOutputUtils.FilterName(name, MAX_THREAD_NAME), TextTokenKind.String);
		}

		public void WriteLocation(ThreadVM vm) {
			var frame = vm.Thread.CorThread.AllFrames.FirstOrDefault();
			if (frame == null)
				output.Write(dnSpy_Debugger_Resources.Thread_LocationNotAvailable, TextTokenKind.Text);
			else {
				var flags = TypePrinterFlags.Default | TypePrinterFlags.ShowIP;
				if (!useHex)
					flags |= TypePrinterFlags.UseDecimal;
				frame.Write(new OutputConverter(output), flags);
			}
		}

		public void WritePriority(ThreadVM vm) {
			switch (vm.Priority) {
			case ThreadPriority.Lowest:			output.Write(dnSpy_Debugger_Resources.Thread_Priority_Lowest, TextTokenKind.EnumField); break;
			case ThreadPriority.BelowNormal:	output.Write(dnSpy_Debugger_Resources.Thread_Priority_BelowNormal, TextTokenKind.EnumField); break;
			case ThreadPriority.Normal:			output.Write(dnSpy_Debugger_Resources.Thread_Priority_Normal, TextTokenKind.EnumField); break;
			case ThreadPriority.AboveNormal:	output.Write(dnSpy_Debugger_Resources.Thread_Priority_AboveNormal, TextTokenKind.EnumField); break;
			case ThreadPriority.Highest:		output.Write(dnSpy_Debugger_Resources.Thread_Priority_Highest, TextTokenKind.EnumField); break;
			default:							output.Write(string.Format("???({0})", (int)vm.Priority), TextTokenKind.Error); break;
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
			output.Write(sb.ToString(), TextTokenKind.Number);
		}

		public void WriteSuspended(ThreadVM vm) {
			output.WriteYesNo(vm.IsSuspended);
		}

		public void WriteProcess(ThreadVM vm) {
			output.Write(vm.Thread.Process, useHex);
		}

		public void WriteAppDomain(ThreadVM vm) {
			output.Write(vm.AppDomain, dbg);
		}

		static readonly Tuple<CorDebugUserState, string>[] UserStates = new Tuple<CorDebugUserState, string>[] {
			Tuple.Create(CorDebugUserState.USER_STOP_REQUESTED, dnSpy_Debugger_Resources.Thread_UserState_StopRequested),
			Tuple.Create(CorDebugUserState.USER_SUSPEND_REQUESTED, dnSpy_Debugger_Resources.Thread_UserState_SuspendRequested),
			Tuple.Create(CorDebugUserState.USER_BACKGROUND, dnSpy_Debugger_Resources.Thread_UserState_Background),
			Tuple.Create(CorDebugUserState.USER_UNSTARTED, dnSpy_Debugger_Resources.Thread_UserState_Unstarted),
			Tuple.Create(CorDebugUserState.USER_STOPPED, dnSpy_Debugger_Resources.Thread_UserState_Stopped),
			Tuple.Create(CorDebugUserState.USER_WAIT_SLEEP_JOIN, dnSpy_Debugger_Resources.Thread_UserState_WaitSleepJoin),
			Tuple.Create(CorDebugUserState.USER_SUSPENDED, dnSpy_Debugger_Resources.Thread_UserState_Suspended),
			Tuple.Create(CorDebugUserState.USER_UNSAFE_POINT, dnSpy_Debugger_Resources.Thread_UserState_UnsafePoint),
			Tuple.Create(CorDebugUserState.USER_THREADPOOL, dnSpy_Debugger_Resources.Thread_UserState_ThreadPool),
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
					output.Write(t.Item2, TextTokenKind.EnumField);
				}
			}
			if (state != 0) {
				if (needComma)
					output.WriteCommaSpace();
				output.Write(string.Format("0x{0:X}", (int)state), TextTokenKind.Number);
			}
		}
	}
}
