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

using System.Collections.Generic;
using System.Text;
using System.Threading;
using dnSpy.Contracts.Debugger.DotNet.Evaluation;
using dnSpy.Contracts.Debugger.DotNet.Text;
using dnSpy.Debugger.DotNet.Metadata;

namespace dnSpy.Roslyn.Debugger {
	static class ObjectCache {
		const int MAX_STRINGBUILDER_CAPACITY = 1024;
		static volatile StringBuilder? stringBuilder;
		public static StringBuilder AllocStringBuilder() => Interlocked.Exchange(ref stringBuilder, null) ?? new StringBuilder();
		public static void Free(ref StringBuilder sb) {
			if (sb.Capacity <= MAX_STRINGBUILDER_CAPACITY) {
				sb.Clear();
				stringBuilder = sb;
			}
			sb = null!;
		}
		public static string FreeAndToString(ref StringBuilder sb) {
			var res = sb.ToString();
			Free(ref sb);
			return res;
		}

		static volatile List<DbgDotNetValue>? dotNetValueList;
		public static List<DbgDotNetValue> AllocDotNetValueList() => Interlocked.Exchange(ref dotNetValueList, null) ?? new List<DbgDotNetValue>();
		public static void Free(ref List<DbgDotNetValue> list) {
			list.Clear();
			dotNetValueList = list;
		}

		static volatile List<DmdFieldInfo?>? fieldInfoList1;
		public static List<DmdFieldInfo?> AllocFieldInfoList1() => Interlocked.Exchange(ref fieldInfoList1, null) ?? new List<DmdFieldInfo?>();
		public static void FreeFieldInfoList1(ref List<DmdFieldInfo?> list) {
			list.Clear();
			fieldInfoList1 = list;
		}

		static volatile List<DmdFieldInfo>? fieldInfoList2;
		public static List<DmdFieldInfo> AllocFieldInfoList2() => Interlocked.Exchange(ref fieldInfoList2, null) ?? new List<DmdFieldInfo>();
		public static void FreeFieldInfoList2(ref List<DmdFieldInfo> list) {
			list.Clear();
			fieldInfoList2 = list;
		}

		static volatile DbgDotNetTextOutput? dotNetTextOutput;
		public static DbgDotNetTextOutput AllocDotNetTextOutput() => Interlocked.Exchange(ref dotNetTextOutput, null) ?? new DbgDotNetTextOutput();
		public static void Free(ref DbgDotNetTextOutput output) {
			output.Clear();
			dotNetTextOutput = output;
			output = null!;
		}
		public static DbgDotNetText FreeAndToText(ref DbgDotNetTextOutput output) {
			var res = output.CreateAndReset();
			Free(ref output);
			return res;
		}
	}
}
