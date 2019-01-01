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
using System.Text;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.Text;
using dnSpy.Contracts.Debugger.Text.DnSpy;
using dnSpy.Contracts.Text;
using dnSpy.Debugger.Properties;

namespace dnSpy.Debugger.ToolWindows {
	static class FormatterUtils {
		const int MAX_APP_DOMAIN_NAME = 100;

		public static string FilterName(string s, int maxLen) {
			var sb = new StringBuilder(s.Length);

			foreach (var c in s) {
				if (c < ' ')
					sb.Append($"\\u{(ushort)c:X4}");
				else
					sb.Append(c);
			}

			if (sb.Length > maxLen) {
				sb.Length = maxLen;
				sb.Append("...");
			}
			return sb.ToString();
		}

		public static T Write<T>(this T output, DbgAppDomain appDomain) where T : IDbgTextWriter {
			if (appDomain == null)
				output.Write(DbgTextColor.Error, dnSpy_Debugger_Resources.AppDomainNotAvailable);
			else {
				output.Write(DbgTextColor.Punctuation, "[");
				// Id is always in decimal (same as VS)
				output.Write(DbgTextColor.Number, appDomain.Id.ToString());
				output.Write(DbgTextColor.Punctuation, "]");
				output.Write(DbgTextColor.Text, " ");
				var filteredName = FilterName(appDomain.Name, MAX_APP_DOMAIN_NAME);
				if (HasSameNameAsProcess(appDomain))
					new DbgTextColorWriter(output).WriteFilename(filteredName);
				else
					output.Write(DbgTextColor.String, filteredName);
			}
			return output;
		}

		static bool HasSameNameAsProcess(DbgAppDomain ad) {
			if (ad == null)
				return false;
			var fname = ad.Process.Name;
			return !string.IsNullOrEmpty(fname) && StringComparer.OrdinalIgnoreCase.Equals(fname, ad.Name);
		}

		public static T Write<T>(this T output, DbgProcess process, bool useHex) where T : IDbgTextWriter {
			output.Write(DbgTextColor.Punctuation, "[");
			if (useHex)
				output.Write(DbgTextColor.Number, "0x" + process.Id.ToString("X"));
			else
				output.Write(DbgTextColor.Number, process.Id.ToString());
			output.Write(DbgTextColor.Punctuation, "]");
			output.Write(DbgTextColor.Text, " ");
			new DbgTextColorWriter(output).WriteFilename(process.Name);
			return output;
		}

		public static T WriteYesNoOrNA<T>(this T output, bool? value) where T : IDbgTextWriter {
			if (value != null)
				output.WriteYesNo(value.Value);
			else
				output.Write(DbgTextColor.Text, "N/A");
			return output;
		}

		public static T WriteYesNo<T>(this T output, bool value) where T : IDbgTextWriter {
			if (value)
				output.Write(DbgTextColor.Text, dnSpy_Debugger_Resources.YesNo_Yes);
			else
				output.Write(DbgTextColor.Text, dnSpy_Debugger_Resources.YesNo_No);
			return output;
		}
	}
}
