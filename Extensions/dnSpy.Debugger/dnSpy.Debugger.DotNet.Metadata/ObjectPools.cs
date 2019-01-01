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

namespace dnSpy.Debugger.DotNet.Metadata {
	static class ObjectPools {
		static StringBuilder stringBuilder;
		public static StringBuilder AllocStringBuilder() =>
			Interlocked.Exchange(ref stringBuilder, null) ?? new StringBuilder();
		public static string FreeAndToString(ref StringBuilder sb) {
			var result = sb.ToString();
			FreeNoToString(ref sb);
			return result;
		}
		public static void FreeNoToString(ref StringBuilder sb) {
			var tmp = sb;
			sb = null;
			if (tmp.Capacity <= 1024 && stringBuilder == null) {
				tmp.Clear();
				stringBuilder = tmp;
			}
		}

		static HashSet<DmdType> typeHashSet;
		public static HashSet<DmdType> AllocHashSetOfType() =>
			Interlocked.Exchange(ref typeHashSet, null) ?? new HashSet<DmdType>(DmdMemberInfoEqualityComparer.DefaultType);
		public static void Free(ref HashSet<DmdType> hash) {
			var tmp = hash;
			hash = null;
			if (tmp.Count <= 1024 && typeHashSet == null) {
				tmp.Clear();
				typeHashSet = tmp;
			}
		}

		static Stack<DmdType> typeStack;
		public static Stack<DmdType> AllocStackOfType() =>
			Interlocked.Exchange(ref typeStack, null) ?? new Stack<DmdType>();
		public static void Free(ref Stack<DmdType> stack) {
			var tmp = stack;
			stack = null;
			if (typeStack == null) {
				tmp.Clear();
				typeStack = tmp;
			}
		}

		static Stack<IEnumerator<DmdType>> enumeratorTypeStack;
		public static Stack<IEnumerator<DmdType>> AllocStackOfIEnumeratorOfType() =>
			Interlocked.Exchange(ref enumeratorTypeStack, null) ?? new Stack<IEnumerator<DmdType>>();
		public static void Free(ref Stack<IEnumerator<DmdType>> stack) {
			var tmp = stack;
			stack = null;
			if (enumeratorTypeStack == null) {
				tmp.Clear();
				enumeratorTypeStack = tmp;
			}
		}

		static List<DmdType> typeList;
		public static List<DmdType> AllocListOfType() =>
			Interlocked.Exchange(ref typeList, null) ?? new List<DmdType>();
		public static DmdType[] FreeAndToArray(ref List<DmdType> list) {
			var res = list.ToArray();
			Free(ref list);
			return res;
		}
		public static void Free(ref List<DmdType> list) {
			var tmp = list;
			list = null;
			if (tmp.Capacity <= 1024 && typeList == null) {
				tmp.Clear();
				typeList = tmp;
			}
		}
	}
}
