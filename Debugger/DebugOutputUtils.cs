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
using System.IO;
using System.Linq;
using System.Text;
using dndbg.Engine;
using dnSpy.Contracts.Highlighting;
using dnSpy.Debugger.Properties;
using dnSpy.Decompiler.Shared;
using dnSpy.Shared.UI.Highlighting;

namespace dnSpy.Debugger {
	static class DebugOutputUtils {
		const int MAX_APP_DOMAIN_NAME = 100;

		public static string FilterName(string s, int maxLen) {
			var sb = new StringBuilder(s.Length);

			foreach (var c in s) {
				if (c < ' ')
					sb.Append(string.Format("\\u{0:X4}", (ushort)c));
				else
					sb.Append(c);
			}

			if (sb.Length > maxLen) {
				sb.Length = maxLen;
				sb.Append("...");
			}
			return sb.ToString();
		}

		public static T Write<T>(this T output, CorAppDomain appDomain, DnDebugger dbg) where T : ISyntaxHighlightOutput {
			if (appDomain == null)
				output.Write(dnSpy_Debugger_Resources.AppDomainNotAvailable, TextTokenKind.Error);
			else {
				output.Write("[", TextTokenKind.Operator);
				output.Write(string.Format("{0}", appDomain.Id), TextTokenKind.Number);
				output.Write("]", TextTokenKind.Operator);
				output.WriteSpace();
				var filteredName = FilterName(appDomain.Name, MAX_APP_DOMAIN_NAME);
				if (HasSameNameAsProcess(dbg, appDomain))
					output.WriteFilename(filteredName);
				else
					output.Write(filteredName, TextTokenKind.String);
			}
			return output;
		}

		static bool HasSameNameAsProcess(DnDebugger dbg, CorAppDomain ad) {
			if (ad == null)
				return false;
			if (dbg == null)
				return false;
			var p = dbg.Processes.FirstOrDefault(dp => dp.CorProcess == ad.Process);
			if (p == null)
				return false;
			var fname = GetFilename(p.Filename);
			return !string.IsNullOrEmpty(fname) && StringComparer.OrdinalIgnoreCase.Equals(fname, ad.Name);
		}

		public static string GetFilename(string s) {
			try {
				return Path.GetFileName(s);
			}
			catch {
			}
			return s;
		}

		public static T Write<T>(this T output, DnProcess p, bool useHex) where T : ISyntaxHighlightOutput {
			output.Write("[", TextTokenKind.Operator);
			if (useHex)
				output.Write(string.Format("0x{0:X}", p.ProcessId), TextTokenKind.Number);
			else
				output.Write(string.Format("{0}", p.ProcessId), TextTokenKind.Number);
			output.Write("]", TextTokenKind.Operator);
			output.WriteSpace();
			output.WriteFilename(GetFilename(p.Filename));
			return output;
		}

		public static T WriteYesNo<T>(this T output, bool value) where T : ISyntaxHighlightOutput {
			if (value)
				output.Write(dnSpy_Debugger_Resources.YesNo_Yes, TextTokenKind.Keyword);
			else
				output.Write(dnSpy_Debugger_Resources.YesNo_No, TextTokenKind.InstanceMethod);
			return output;
		}
	}
}
