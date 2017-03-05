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

using System.ComponentModel.Composition;
using System.Text;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Text;
using dnSpy.Debugger.Properties;

namespace dnSpy.Debugger.ToolWindows.Threads {
	[Export(typeof(ThreadFormatterProvider))]
	sealed class ThreadFormatterProvider {
		readonly DebuggerSettings debuggerSettings;

		[ImportingConstructor]
		ThreadFormatterProvider(DebuggerSettings debuggerSettings) => this.debuggerSettings = debuggerSettings;

		public ThreadFormatter Create() =>
			ThreadFormatter.Create_DONT_USE(debuggerSettings.UseHexadecimal);
	}

	sealed class ThreadFormatter {
		const int maxThreadName = 128;
		readonly bool useHex;

		ThreadFormatter(bool useHex) => this.useHex = useHex;

		internal static ThreadFormatter Create_DONT_USE(bool useHex) => new ThreadFormatter(useHex);

		void WriteInt32(ITextColorWriter output, int value) {
			if (useHex)
				output.Write(BoxedTextColor.Number, "0x" + value.ToString("X8"));
			else
				output.Write(BoxedTextColor.Number, value.ToString());
		}

		public void WriteImage(ITextColorWriter output, ThreadVM vm) {
			if (vm.IsSelectedThread)
				output.Write(BoxedTextColor.Text, ">");
		}

		public void WriteId(ITextColorWriter output, DbgThread thread) => WriteInt32(output, thread.Id);
		public void WriteSuspendedCount(ITextColorWriter output, DbgThread thread) => WriteInt32(output, thread.SuspendedCount);
		public void WriteCategoryText(ITextColorWriter output, ThreadVM vm) => output.Write(BoxedTextColor.Text, vm.CategoryText);

		public void WriteProcessName(ITextColorWriter output, DbgThread thread) {
			var process = thread.Process;
			output.WriteFilename(PathUtils.GetFilename(process.Filename));
			output.WriteSpace();
			output.Write(BoxedTextColor.Punctuation, "(");
			output.Write(BoxedTextColor.Text, "id");
			output.WriteSpace();
			output.Write(BoxedTextColor.Operator, "=");
			output.WriteSpace();
			// Always decimal, same as VS
			output.Write(BoxedTextColor.Number, process.Id.ToString());
			output.Write(BoxedTextColor.Punctuation, ")");
		}

		public void WriteName(ITextColorWriter output, ThreadVM vm) {
			var name = vm.Name;
			if (name == null)
				output.Write(BoxedTextColor.Text, dnSpy_Debugger_Resources.Thread_NoName);
			else
				output.Write(BoxedTextColor.String, FormatterUtils.FilterName(name, maxThreadName));
		}

		public void WriteLocation(ITextColorWriter output, DbgThread thread) {
			//TODO:
		}

		public void WritePriority(ITextColorWriter output, ThreadVM vm) {
			switch (vm.Priority) {
			case ThreadPriority.Lowest:			output.Write(BoxedTextColor.EnumField, dnSpy_Debugger_Resources.Thread_Priority_Lowest); break;
			case ThreadPriority.BelowNormal:	output.Write(BoxedTextColor.EnumField, dnSpy_Debugger_Resources.Thread_Priority_BelowNormal); break;
			case ThreadPriority.Normal:			output.Write(BoxedTextColor.EnumField, dnSpy_Debugger_Resources.Thread_Priority_Normal); break;
			case ThreadPriority.AboveNormal:	output.Write(BoxedTextColor.EnumField, dnSpy_Debugger_Resources.Thread_Priority_AboveNormal); break;
			case ThreadPriority.Highest:		output.Write(BoxedTextColor.EnumField, dnSpy_Debugger_Resources.Thread_Priority_Highest); break;
			default:							output.Write(BoxedTextColor.Error, ((int)vm.Priority).ToString()); break;
			}
		}

		public void WriteAffinityMask(ITextColorWriter output, ThreadVM vm) {
			var affMask = vm.AffinityMask;
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

		public void WriteAppDomain(ITextColorWriter output, DbgThread thread) {
			var appDomain = thread.AppDomain;
			if (appDomain != null)
				output.Write(appDomain);
		}

		public void WriteState(ITextColorWriter output, DbgThread thread) {
			bool needComma = false;
			foreach (var info in thread.State) {
				if (needComma)
					output.WriteCommaSpace();
				needComma = true;
				output.Write(BoxedTextColor.EnumField, info.LocalizedState);
			}
		}
	}
}
