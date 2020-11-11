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

using System.ComponentModel.Composition;
using System.Text;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.Text;
using dnSpy.Contracts.Debugger.Text.DnSpy;
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

		void WriteInt32(IDbgTextWriter output, int value) {
			if (useHex)
				output.Write(DbgTextColor.Number, "0x" + value.ToString("X8"));
			else
				output.Write(DbgTextColor.Number, value.ToString());
		}

		void WriteUInt64(IDbgTextWriter output, ulong value) {
			if (useHex)
				output.Write(DbgTextColor.Number, "0x" + value.ToString("X8"));
			else
				output.Write(DbgTextColor.Number, value.ToString());
		}

		public void WriteImage(IDbgTextWriter output, ThreadVM vm) {
			if (vm.IsCurrentThread)
				output.Write(DbgTextColor.Text, ">");
		}

		public void WriteManagedId(IDbgTextWriter output, DbgThread thread) {
			var managedId = thread.ManagedId;
			if (managedId is not null)
				WriteUInt64(output, managedId.Value);
		}

		public void WriteId(IDbgTextWriter output, DbgThread thread) => WriteUInt64(output, thread.Id);
		public void WriteSuspendedCount(IDbgTextWriter output, DbgThread thread) => WriteInt32(output, thread.SuspendedCount);
		public void WriteCategoryText(IDbgTextWriter output, ThreadVM vm) => output.Write(DbgTextColor.Text, vm.CategoryText);

		public void WriteProcessName(IDbgTextWriter output, DbgThread thread) {
			var process = thread.Process;
			new DbgTextColorWriter(output).WriteFilename(process.Name);
			output.Write(DbgTextColor.Text, " ");
			output.Write(DbgTextColor.Punctuation, "(");
			output.Write(DbgTextColor.Text, "id");
			output.Write(DbgTextColor.Text, " ");
			output.Write(DbgTextColor.Operator, "=");
			output.Write(DbgTextColor.Text, " ");
			// Always decimal, same as VS
			output.Write(DbgTextColor.Number, process.Id.ToString());
			output.Write(DbgTextColor.Punctuation, ")");
		}

		public void WriteName(IDbgTextWriter output, DbgThread thread) {
			var color = thread.HasName() ? NameColor : DbgTextColor.Text;
			output.Write(color, FormatterUtils.FilterName(thread.UIName, maxThreadName));
		}
		// Make sure these fields are in sync
		const DbgTextColor NameColor = DbgTextColor.String;
		internal const string NameColorClassificationTypeName = Contracts.Text.Classification.ThemeClassificationTypeNames.String;

		public void WriteLocation(IDbgTextWriter output, ThreadVM vm) => vm.LocationCachedOutput.WriteTo(output);

		public void WritePriority(IDbgTextWriter output, ThreadVM vm) {
			switch (vm.Priority) {
			case ThreadPriority.Lowest:			output.Write(DbgTextColor.EnumField, dnSpy_Debugger_Resources.Thread_Priority_Lowest); break;
			case ThreadPriority.BelowNormal:	output.Write(DbgTextColor.EnumField, dnSpy_Debugger_Resources.Thread_Priority_BelowNormal); break;
			case ThreadPriority.Normal:			output.Write(DbgTextColor.EnumField, dnSpy_Debugger_Resources.Thread_Priority_Normal); break;
			case ThreadPriority.AboveNormal:	output.Write(DbgTextColor.EnumField, dnSpy_Debugger_Resources.Thread_Priority_AboveNormal); break;
			case ThreadPriority.Highest:		output.Write(DbgTextColor.EnumField, dnSpy_Debugger_Resources.Thread_Priority_Highest); break;
			default:							output.Write(DbgTextColor.Error, ((int)vm.Priority).ToString()); break;
			}
		}

		public void WriteAffinityMask(IDbgTextWriter output, ThreadVM vm) {
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
			output.Write(DbgTextColor.Number, sb.ToString());
		}

		public void WriteAppDomain(IDbgTextWriter output, DbgThread thread) {
			var appDomain = thread.AppDomain;
			if (appDomain is not null)
				output.Write(appDomain);
		}

		public void WriteState(IDbgTextWriter output, DbgThread thread) {
			bool needComma = false;
			foreach (var info in thread.State) {
				if (needComma) {
					output.Write(DbgTextColor.Punctuation, ",");
					output.Write(DbgTextColor.Text, " ");
				}
				needComma = true;
				output.Write(DbgTextColor.EnumField, info.LocalizedState);
			}
		}
	}
}
