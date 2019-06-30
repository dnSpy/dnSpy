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
using System.IO;
using System.Text;
using System.Xml.Linq;
using Microsoft.Win32;

namespace dnSpy.Debugger.Exceptions {
	// You must load VS2017's privateregistry.bin file (see AppData\Local\Microsoft\VisualStudio\15.0_XXXXXXXXXXXX) in
	// regedit (select HKEY_USERS then File -> Load Hive, name it "vs2017")
	// Update CODE below with your hash (the X's above)
	class Program {
		const string CODE = "XXXXXXXXXXXX";
		static void Main(string[] args) {
			var s = new Program().Dump();
			File.WriteAllText(@"c:\XXXXXXXXXXXX\out.ex.xml", s);
		}
		[Flags]
		enum ExceptionState : uint {// https://docs.microsoft.com/en-us/visualstudio/extensibility/debugger/reference/exception-state
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
		sealed class ExceptionInfo {
			public string Name { get; }
			public int Code { get; }
			public ExceptionState State { get; }
			public ExceptionInfo(string name, int code, ExceptionState state) {
				Name = name;
				Code = code;
				State = state;
			}
			public override string ToString() => $"0x{Code:X8} {Name} {State}";
		}
		sealed class Category {
			public string Name { get; }
			public string DisplayName { get; }
			public string ShortDisplayName { get; }
			public ExceptionState State { get; }
			public List<ExceptionInfo> Exceptions { get; } = new List<ExceptionInfo>();
			public bool HasCode => (State & ExceptionState.EXCEPTION_CODE_SUPPORTED) != 0;
			public Category(string name, string displayName, string shortDisplayName, ExceptionState state) {
				Name = name;
				DisplayName = displayName;
				ShortDisplayName = shortDisplayName;
				State = state;
			}
		}
		readonly List<Category> categories = new List<Category>();
		string Dump() {
			Dump(@"{449EC4CC-30D2-4032-9256-EE18EB41B62B}", ".NET", PredefinedExceptionCategories.DotNet);
			Dump(@"{6ECE07A9-0EDE-45C4-8296-818D8FC401D4}", "MDA", PredefinedExceptionCategories.MDA);
			return CreateXml(categories);
		}
		static string CreateXml(List<Category> categories) {
			var doc = new XDocument(new XElement("Exceptions"));
			categories.Sort((a, b) => StringComparer.OrdinalIgnoreCase.Compare(a.Name, b.Name));
			foreach (var category in categories) {
				if (category.HasCode)
					category.Exceptions.Sort((a, b) => ((uint)a.Code).CompareTo((uint)b.Code));
				else
					category.Exceptions.Sort((a, b) => StringComparer.OrdinalIgnoreCase.Compare(a.Name, b.Name));
			}
			var root = doc.Root;
			var sb = new StringBuilder();
			foreach (var category in categories) {
				var defElem = new XElement("CategoryDef");
				defElem.SetAttributeValue("Name", category.Name);
				defElem.SetAttributeValue("DisplayName", category.DisplayName);
				defElem.SetAttributeValue("ShortDisplayName", category.ShortDisplayName);
				sb.Clear();
				AppendFlags(sb, category.State, ExceptionState.EXCEPTION_CODE_SUPPORTED, "code");
				if ((category.State & ExceptionState.EXCEPTION_CODE_SUPPORTED) != 0)
					AppendFlags(sb, category.State, ExceptionState.EXCEPTION_CODE_DISPLAY_IN_HEX, "decimal", false);
				if (sb.Length > 0)
					defElem.SetAttributeValue("Flags", sb.ToString());
				root.Add(defElem);
			}
			foreach (var category in categories) {
				var defElem = new XElement("ExceptionDefs");
				defElem.SetAttributeValue("Category", category.Name);
				root.Add(defElem);
				foreach (var ex in category.Exceptions) {
					var exElem = new XElement("Exception");
					defElem.Add(exElem);
					if (category.HasCode) {
						exElem.SetAttributeValue("Code", "0x" + ex.Code.ToString("X8"));
						if (!string.IsNullOrWhiteSpace(ex.Name))
							exElem.SetAttributeValue("Description", ex.Name);
					}
					else
						exElem.SetAttributeValue("Name", ex.Name);
					sb.Clear();
					AppendFlags(sb, ex.State, ExceptionState.EXCEPTION_STOP_FIRST_CHANCE, "stop1");
					AppendFlags(sb, ex.State, ExceptionState.EXCEPTION_STOP_SECOND_CHANCE, "stop2");
					if (sb.Length > 0)
						exElem.SetAttributeValue("Flags", sb.ToString());
				}
			}
			return doc.ToString();
		}
		static void AppendFlags(StringBuilder sb, ExceptionState state, ExceptionState flag, string flagName, bool expValue = true) {
			if (((state & flag) != 0) != expValue)
				return;
			if (sb.Length > 0)
				sb.Append(", ");
			sb.Append(flagName);
		}
		void Dump(string regExName, string shortDisplayName, string gropuName) {
			using (var key = Registry.Users.OpenSubKey(@"vs2017\Software\Microsoft\VisualStudio\15.0_" + CODE + @"_Config\AD7Metrics\Exception\" + regExName)) {
				foreach (var subKeyName in key.GetSubKeyNames()) {
					using (var subKey = key.OpenSubKey(subKeyName)) {
						var exState = (ExceptionState)(int)subKey.GetValue("State");
						var category = new Category(gropuName, subKeyName, shortDisplayName, exState);
						categories.Add(category);
						Dump(category, subKey);
					}
				}
			}
		}
		int Dump(Category category, RegistryKey key) {
			var subKeyNames = key.GetSubKeyNames();
			foreach (var subKeyName in subKeyNames) {
				using (var subKey = key.OpenSubKey(subKeyName)) {
					int count = Dump(category, subKey);
					if (count == 0)
						category.Exceptions.Add(new ExceptionInfo(subKeyName, (int)subKey.GetValue("Code"), (ExceptionState)(int)subKey.GetValue("State")));
				}
			}
			return subKeyNames.Length;
		}
	}
}
