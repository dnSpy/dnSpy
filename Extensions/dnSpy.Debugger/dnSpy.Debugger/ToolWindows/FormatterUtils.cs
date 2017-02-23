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
using System.Text;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Text;
using dnSpy.Debugger.Properties;

namespace dnSpy.Debugger.ToolWindows {
	static class FormatterUtils {
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

		public static T Write<T>(this T output, DbgAppDomain appDomain) where T : ITextColorWriter {
			if (appDomain == null)
				output.Write(BoxedTextColor.Error, dnSpy_Debugger_Resources.AppDomainNotAvailable);
			else {
				output.Write(BoxedTextColor.Punctuation, "[");
				// Id is always in decimal (same as VS)
				output.Write(BoxedTextColor.Number, appDomain.Id.ToString());
				output.Write(BoxedTextColor.Punctuation, "]");
				output.WriteSpace();
				var filteredName = FilterName(appDomain.Name, MAX_APP_DOMAIN_NAME);
				if (HasSameNameAsProcess(appDomain))
					output.WriteFilename(filteredName);
				else
					output.Write(BoxedTextColor.String, filteredName);
			}
			return output;
		}

		static bool HasSameNameAsProcess(DbgAppDomain ad) {
			if (ad == null)
				return false;
			var fname = PathUtils.GetFilename(ad.Runtime.Process.Filename);
			return !string.IsNullOrEmpty(fname) && StringComparer.OrdinalIgnoreCase.Equals(fname, ad.Name);
		}

		public static T Write<T>(this T output, DbgProcess process, bool useHex) where T : ITextColorWriter {
			output.Write(BoxedTextColor.Punctuation, "[");
			if (useHex)
				output.Write(BoxedTextColor.Number, "0x" + process.Id.ToString("X"));
			else
				output.Write(BoxedTextColor.Number, process.Id.ToString());
			output.Write(BoxedTextColor.Punctuation, "]");
			output.WriteSpace();
			output.WriteFilename(PathUtils.GetFilename(process.Filename));
			return output;
		}

		public static T WriteYesNoError<T>(this T output, bool? value) where T : ITextColorWriter {
			if (value != null)
				output.WriteYesNo(value.Value);
			else
				output.Write(BoxedTextColor.Error, "???");
			return output;
		}

		public static T WriteYesNo<T>(this T output, bool value) where T : ITextColorWriter {
			if (value)
				output.Write(BoxedTextColor.Keyword, dnSpy_Debugger_Resources.YesNo_Yes);
			else
				output.Write(BoxedTextColor.InstanceMethod, dnSpy_Debugger_Resources.YesNo_No);
			return output;
		}
	}
}
