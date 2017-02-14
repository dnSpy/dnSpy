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
using System.Diagnostics;
using System.Linq;
using System.Text;
using dndbg.COM.CorDebug;
using dndbg.Engine;
using dnSpy.Contracts.Text;
using dnSpy.Debugger.Old.Properties;

namespace dnSpy.Debugger.Threads {
	sealed class ThreadPrinter {
		const int MAX_THREAD_NAME = 128;

		readonly ITextColorWriter output;
		readonly bool useHex;
		readonly DnDebugger dbg;

		public ThreadPrinter(ITextColorWriter output, bool useHex, DnDebugger dbg) {
			this.output = output;
			this.useHex = useHex;
			this.dbg = dbg;
		}

		void WriteInt32(int value) {
			if (useHex)
				output.Write(BoxedTextColor.Number, string.Format("0x{0:X8}", value));
			else
				output.Write(BoxedTextColor.Number, string.Format("{0}", value));
		}

		public void WriteCurrent(ThreadVM vm) => output.Write(BoxedTextColor.Text, vm.IsCurrent ? ">" : " ");
		public void WriteId(ThreadVM vm) => WriteInt32(vm.Id);
		public void WriteSuspended(ThreadVM vm) => output.WriteYesNo(vm.IsSuspended);
		public void WriteProcess(ThreadVM vm) => output.Write(vm.Thread.Process, useHex);
		public void WriteAppDomain(ThreadVM vm) => output.Write(vm.AppDomain, dbg);

		public void WriteManagedId(ThreadVM vm) {
			var id = vm.ManagedId;
			if (id != null)
				WriteInt32(id.Value);
			else {
				output.Write(BoxedTextColor.Error, "???");
				WriteInt32(vm.Thread.UniqueIdProcess + 1);
			}
		}

		public void WriteCategory(ThreadVM vm) {
			switch (vm.Type) {
			case ThreadType.Unknown:
				output.Write(BoxedTextColor.Text, dnSpy_Debugger_Resources.ThreadType_Unknown);
				break;
			case ThreadType.Main:
				output.Write(BoxedTextColor.Text, dnSpy_Debugger_Resources.ThreadType_Main);
				break;
			case ThreadType.ThreadPool:
				output.Write(BoxedTextColor.Text, dnSpy_Debugger_Resources.ThreadType_ThreadPool);
				break;
			case ThreadType.Worker:
				output.Write(BoxedTextColor.Text, dnSpy_Debugger_Resources.ThreadType_Worker);
				break;
			case ThreadType.BGCOrFinalizer:
				output.Write(BoxedTextColor.Text, dnSpy_Debugger_Resources.ThreadType_BackgroundGC_or_Finalizer);
				output.Write(BoxedTextColor.Error, "???");
				break;
			case ThreadType.Terminated:
				output.Write(BoxedTextColor.Text, dnSpy_Debugger_Resources.ThreadType_Terminated);
				break;
			default:
				Debug.Fail(string.Format("Unknown thread type: {0}", vm.Type));
				goto case ThreadType.Unknown;
			}
		}

		public void WriteName(ThreadVM vm) {
			var name = vm.Name;
			if (vm.UnknownName)
				output.Write(BoxedTextColor.Error, "???");
			else if (name == null)
				output.Write(BoxedTextColor.Text, dnSpy_Debugger_Resources.Thread_NoName);
			else
				output.Write(BoxedTextColor.String, DebugOutputUtils.FilterName(name, MAX_THREAD_NAME));
		}

		public void WriteLocation(ThreadVM vm) {
			var frame = vm.Thread.CorThread.AllFrames.FirstOrDefault();
			if (frame == null)
				output.Write(BoxedTextColor.Text, dnSpy_Debugger_Resources.Thread_LocationNotAvailable);
			else {
				var flags = TypePrinterFlags.Default | TypePrinterFlags.ShowIP;
				if (!useHex)
					flags |= TypePrinterFlags.UseDecimal;
				frame.Write(new OutputConverter(output), flags);
			}
		}

		public void WritePriority(ThreadVM vm) {
			switch (vm.Priority) {
			case ThreadPriority.Lowest:			output.Write(BoxedTextColor.EnumField, dnSpy_Debugger_Resources.Thread_Priority_Lowest); break;
			case ThreadPriority.BelowNormal:	output.Write(BoxedTextColor.EnumField, dnSpy_Debugger_Resources.Thread_Priority_BelowNormal); break;
			case ThreadPriority.Normal:			output.Write(BoxedTextColor.EnumField, dnSpy_Debugger_Resources.Thread_Priority_Normal); break;
			case ThreadPriority.AboveNormal:	output.Write(BoxedTextColor.EnumField, dnSpy_Debugger_Resources.Thread_Priority_AboveNormal); break;
			case ThreadPriority.Highest:		output.Write(BoxedTextColor.EnumField, dnSpy_Debugger_Resources.Thread_Priority_Highest); break;
			default:							output.Write(BoxedTextColor.Error, string.Format("???({0})", (int)vm.Priority)); break;
			}
		}

		public void WriteAffinityMask(ThreadVM vm) {
			ulong affMask = (ulong)vm.AffinityMask.ToInt64();
			if (IntPtr.Size == 4)
				affMask &= uint.MaxValue;
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
			output.Write(BoxedTextColor.Number, sb.ToString());
		}

		static readonly (CorDebugUserState flag, string name)[] UserStates = new (CorDebugUserState, string)[] {
			(CorDebugUserState.USER_STOP_REQUESTED, dnSpy_Debugger_Resources.Thread_UserState_StopRequested),
			(CorDebugUserState.USER_SUSPEND_REQUESTED, dnSpy_Debugger_Resources.Thread_UserState_SuspendRequested),
			(CorDebugUserState.USER_BACKGROUND, dnSpy_Debugger_Resources.Thread_UserState_Background),
			(CorDebugUserState.USER_UNSTARTED, dnSpy_Debugger_Resources.Thread_UserState_Unstarted),
			(CorDebugUserState.USER_STOPPED, dnSpy_Debugger_Resources.Thread_UserState_Stopped),
			(CorDebugUserState.USER_WAIT_SLEEP_JOIN, dnSpy_Debugger_Resources.Thread_UserState_WaitSleepJoin),
			(CorDebugUserState.USER_SUSPENDED, dnSpy_Debugger_Resources.Thread_UserState_Suspended),
			(CorDebugUserState.USER_UNSAFE_POINT, dnSpy_Debugger_Resources.Thread_UserState_UnsafePoint),
			(CorDebugUserState.USER_THREADPOOL, dnSpy_Debugger_Resources.Thread_UserState_ThreadPool),
		};

		public void WriteUserState(ThreadVM vm) {
			var state = vm.UserState;
			bool needComma = false;
			foreach (var t in UserStates) {
				if ((state & t.flag) != 0) {
					state &= ~t.flag;
					if (needComma)
						output.WriteCommaSpace();
					needComma = true;
					output.Write(BoxedTextColor.EnumField, t.name);
				}
			}
			if (state != 0) {
				if (needComma)
					output.WriteCommaSpace();
				output.Write(BoxedTextColor.Number, string.Format("0x{0:X}", (int)state));
			}
		}
	}
}
