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
using System.IO;
using System.Linq;
using System.Text;
using dndbg.Engine;
using dnSpy.NRefactory;
using ICSharpCode.Decompiler;

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

		public static T Write<T>(this T output, CorAppDomain appDomain) where T : ITextOutput {
			if (appDomain == null)
				output.Write("<not available>", TextTokenType.Error);
			else {
				output.Write("[", TextTokenType.Operator);
				output.Write(string.Format("{0}", appDomain.Id), TextTokenType.Number);
				output.Write("]", TextTokenType.Operator);
				output.WriteSpace();
				var filteredName = FilterName(appDomain.Name, MAX_APP_DOMAIN_NAME);
				if (HasSameNameAsProcess(appDomain))
					output.WriteFilename_OLD(filteredName);
				else
					output.Write(filteredName, TextTokenType.String);
			}
			return output;
		}

		static bool HasSameNameAsProcess(CorAppDomain ad) {
			if (ad == null)
				return false;
			var dbg = DebugManager.Instance.Debugger;
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

		public static T Write<T>(this T output, DnProcess p, bool useHex) where T : ITextOutput {
			output.Write("[", TextTokenType.Operator);
			if (useHex)
				output.Write(string.Format("0x{0:X}", p.ProcessId), TextTokenType.Number);
			else
				output.Write(string.Format("{0}", p.ProcessId), TextTokenType.Number);
			output.Write("]", TextTokenType.Operator);
			output.WriteSpace();
			output.WriteFilename_OLD(GetFilename(p.Filename));
			return output;
		}

		public static T WriteYesNo<T>(this T output, bool value) where T : ITextOutput {
			if (value)
				output.Write("Yes", TextTokenType.Keyword);
			else
				output.Write("No", TextTokenType.InstanceMethod);
			return output;
		}
	}
}
