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
using dnSpy.Contracts.Debugger.Exceptions;

namespace dnSpy.Debugger.DotNet.Exceptions {
	struct ExceptionInfo {
		public string Name { get; }
		public DbgExceptionDefinitionFlags Flags { get; }
		public ExceptionInfo(string name, DbgExceptionDefinitionFlags flags) {
			Name = name ?? throw new ArgumentNullException(nameof(name));
			Flags = flags;
		}
	}
	/*
	// You must load VS2017's privateregistry.bin file (see AppData\Local\Microsoft\VisualStudio\15.0_XXXXXXXXXXXX) in
	// regedit (select HKEY_USERS then File -> Load Hive, name it "vs2017")
	// Update CODE below with your hash (the X's above)
	class Program {
		const string CODE = "XXXXXXXXXX";
		static void Main(string[] args) {
			var s = Dump();
		}
		[Flags]
		enum ExceptionState : uint {// https://msdn.microsoft.com/en-us/library/vstudio/bb146192%28v=vs.140%29.aspx
			EXCEPTION_NONE						= 0x0000,
			EXCEPTION_STOP_FIRST_CHANCE			= 0x0001,
			EXCEPTION_STOP_SECOND_CHANCE		= 0x0002,
			EXCEPTION_STOP_USER_FIRST_CHANCE	= 0x0010,
			EXCEPTION_STOP_USER_UNCAUGHT		= 0x0020,
			EXCEPTION_STOP_ALL					= 0x00FF,
			EXCEPTION_CANNOT_BE_CONTINUED		= 0x0100,

			EXCEPTION_CODE_SUPPORTED			= 0x1000,
			EXCEPTION_CODE_DISPLAY_IN_HEX		= 0x2000,
			EXCEPTION_JUST_MY_CODE_SUPPORTED	= 0x4000,
			EXCEPTION_MANAGED_DEBUG_ASSISTANT	= 0x8000,

			EXCEPTION_STOP_FIRST_CHANCE_USE_PARENT = 0x0004,
			EXCEPTION_STOP_SECOND_CHANCE_USE_PARENT = 0x0008,
			EXCEPTION_STOP_USER_FIRST_CHANCE_USE_PARENT = 0x0040,
			EXCEPTION_STOP_USER_UNCAUGHT_USE_PARENT = 0x0080,
		}
		static string Dump() {
			var sb = new StringBuilder();
			Dump(sb, @"{449EC4CC-30D2-4032-9256-EE18EB41B62B}");
			Dump(sb, @"{6ECE07A9-0EDE-45C4-8296-818D8FC401D4}");
			return sb.ToString();
		}
		static void Dump(StringBuilder sb, string regExName) {
			using (var key = Registry.Users.OpenSubKey(@"vs2017\Software\Microsoft\VisualStudio\15.0_" + CODE + @"_Config\AD7Metrics\Exception\" + regExName)) {
				foreach (var subKeyName in key.GetSubKeyNames()) {
					using (var subKey = key.OpenSubKey(subKeyName)) {
						var exState = (ExceptionState)(int)subKey.GetValue("State");
						bool codeSupported = (exState & ExceptionState.EXCEPTION_CODE_SUPPORTED) != 0;
						sb.AppendLine();
						sb.AppendLine($"			// {subKeyName}");
						Dump(sb, subKey, codeSupported);
					}
				}
			}
		}
		static int Dump(StringBuilder sb, RegistryKey key, bool codeSupported) {
			var subKeyNames = key.GetSubKeyNames();
			foreach (var subKeyName in subKeyNames) {
				using (var subKey = key.OpenSubKey(subKeyName)) {
					int count = Dump(sb, subKey, codeSupported);
					if (count == 0)
						Write(sb, codeSupported, subKey, subKeyName);
				}
			}
			return subKeyNames.Length;
		}
		static void Write(StringBuilder sb, bool codeSupported, RegistryKey key, string name) {
			int code = (int)key.GetValue("Code");
			var state = (ExceptionState)(int)key.GetValue("State");
			if (codeSupported)
				sb.AppendLine($"			new ExceptionInfo(\"{name}\", 0x{code:X8}, {ToString(state)}),");
			else
				sb.AppendLine($"			new ExceptionInfo(\"{name}\", {ToString(state)}),");
		}
		static string ToString(ExceptionState f) {
			var sb = new StringBuilder();
			if ((f & ExceptionState.EXCEPTION_STOP_FIRST_CHANCE) != 0) Append(sb, "DbgExceptionDefinitionFlags.StopFirstChance");
			if ((f & ExceptionState.EXCEPTION_STOP_SECOND_CHANCE) != 0) Append(sb, "DbgExceptionDefinitionFlags.StopSecondChance");
			//if ((f & ExceptionState.EXCEPTION_STOP_USER_FIRST_CHANCE) != 0) Append(sb, "DbgExceptionDefinitionFlags.StopUserFirstChance");
			//if ((f & ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT) != 0) Append(sb, "DbgExceptionDefinitionFlags.StopUserUncaught");
			//if ((f & ExceptionState.EXCEPTION_CANNOT_BE_CONTINUED) != 0) Append(sb, "DbgExceptionDefinitionFlags.CanNotBeContinued");
			//if ((f & ExceptionState.EXCEPTION_CODE_SUPPORTED) != 0) Append(sb, "DbgExceptionDefinitionFlags.CodeSupported");
			//if ((f & ExceptionState.EXCEPTION_CODE_DISPLAY_IN_HEX) != 0) Append(sb, "DbgExceptionDefinitionFlags.CodeDisplayInHex");
			//if ((f & ExceptionState.EXCEPTION_JUST_MY_CODE_SUPPORTED) != 0) Append(sb, "DbgExceptionDefinitionFlags.JustMyCodeSupported");
			//if ((f & ExceptionState.EXCEPTION_MANAGED_DEBUG_ASSISTANT) != 0) Append(sb, "DbgExceptionDefinitionFlags.ManagedDebugAssistant");
			//if ((f & ExceptionState.EXCEPTION_STOP_FIRST_CHANCE_USE_PARENT) != 0) Append(sb, "DbgExceptionDefinitionFlags.StopFirstChanceUseParent");
			//if ((f & ExceptionState.EXCEPTION_STOP_SECOND_CHANCE_USE_PARENT) != 0) Append(sb, "DbgExceptionDefinitionFlags.StopSecondChanceUseParent");
			//if ((f & ExceptionState.EXCEPTION_STOP_USER_FIRST_CHANCE_USE_PARENT) != 0) Append(sb, "DbgExceptionDefinitionFlags.StopUserFirstChanceUseParent");
			//if ((f & ExceptionState.EXCEPTION_STOP_USER_UNCAUGHT_USE_PARENT) != 0) Append(sb, "DbgExceptionDefinitionFlags.StopUserUncaughtUseParent");
			if (sb.Length == 0) return "DbgExceptionDefinitionFlags.None";
			return sb.ToString();
		}
		static void Append(StringBuilder sb, string s) {
			if (sb.Length > 0)
				sb.Append(" | ");
			sb.Append(s);
		}
	}
	*/
}
