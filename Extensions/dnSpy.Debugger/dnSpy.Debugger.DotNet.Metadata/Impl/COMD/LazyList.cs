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

namespace dnSpy.Debugger.DotNet.Metadata.Impl.COMD {
	sealed class LazyList<T> where T : class {
		readonly Dictionary<uint, T> dict;
		readonly Func<uint, T?> readElementByRID;

		public T? this[uint index] {
			get {
				T? elem;
				if (dict.TryGetValue(index, out elem))
					return elem;
				elem = readElementByRID(index + 1);
				if (!(elem is null))
					dict[index] = elem;
				return elem;
			}
		}

		public T? TryGet(uint index) {
			dict.TryGetValue(index, out var elem);
			return elem;
		}

		public LazyList(Func<uint, T?> readElementByRID) {
			this.readElementByRID = readElementByRID;
			dict = new Dictionary<uint, T>();
		}
	}

	sealed class LazyList<TValue, TArg> where TValue : class {
		readonly Dictionary<uint, TValue> dict;
		readonly Func<uint, TArg, TValue?> readElementByRID;

		public TValue? this[uint index, TArg arg] {
			get {
				TValue? elem;
				if (dict.TryGetValue(index, out elem))
					return elem;
				elem = readElementByRID(index + 1, arg);
				if (!(elem is null))
					dict[index] = elem;
				return elem;
			}
		}

		public LazyList(Func<uint, TArg, TValue?> readElementByRID) {
			this.readElementByRID = readElementByRID;
			dict = new Dictionary<uint, TValue>();
		}
	}

	sealed class LazyList2<TValue, TArg1, TArg2> where TValue : class {
		readonly Dictionary<uint, TValue> dict;
		readonly Func<uint, TArg1, TArg2, (TValue? elem, bool containedGenericParams)> readElementByRID;

		public TValue? this[uint index, TArg1 arg1, TArg2 arg2] {
			get {
				TValue? elem;
				if (dict.TryGetValue(index, out elem))
					return elem;
				var info = readElementByRID(index + 1, arg1, arg2);
				if (!((elem = info.elem) is null) && !info.containedGenericParams)
					dict[index] = elem;
				return elem;
			}
		}

		public LazyList2(Func<uint, TArg1, TArg2, (TValue? elem, bool containedGenericParams)> readElementByRID) {
			this.readElementByRID = readElementByRID;
			dict = new Dictionary<uint, TValue>();
		}
	}
}
