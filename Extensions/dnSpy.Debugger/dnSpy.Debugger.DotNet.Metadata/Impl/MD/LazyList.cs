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
using System.Threading;

namespace dnSpy.Debugger.DotNet.Metadata.Impl.MD {
	static class LazyListConstants {
		// Max number of elements to make sure it doesn't land in the large object heap.
		public static readonly int MaxArrayObjectElements = (85000 - 100) / IntPtr.Size;
	}

	sealed class LazyList<T> where T : class {
		readonly T[][] allElements;
		readonly Func<uint, T> readElementByRID;
		readonly uint length;

		public T this[uint index] {
			get {
				if (index >= length)
					return null;
				ref var elem = ref allElements[((int)index / LazyListConstants.MaxArrayObjectElements)][((int)index % LazyListConstants.MaxArrayObjectElements)];
				if (elem == null)
					Interlocked.CompareExchange(ref elem, readElementByRID(index + 1), null);
				return elem;
			}
		}

		public LazyList(uint length, Func<uint, T> readElementByRID) {
			this.length = length;
			this.readElementByRID = readElementByRID;
			allElements = new T[((int)length + LazyListConstants.MaxArrayObjectElements - 1) / LazyListConstants.MaxArrayObjectElements][];
			for (int i = 0; i < allElements.Length; i++) {
				var partLen = Math.Min((int)length, LazyListConstants.MaxArrayObjectElements);
				allElements[i] = new T[partLen];
				length -= (uint)partLen;
			}
			if (length != 0)
				throw new InvalidOperationException();
		}
	}

	sealed class LazyList<TValue, TArg> where TValue : class {
		readonly TValue[][] allElements;
		readonly Func<uint, TArg, TValue> readElementByRID;
		readonly uint length;

		public TValue this[uint index, TArg arg] {
			get {
				if (index >= length)
					return null;
				ref var elem = ref allElements[((int)index / LazyListConstants.MaxArrayObjectElements)][((int)index % LazyListConstants.MaxArrayObjectElements)];
				if (elem == null)
					Interlocked.CompareExchange(ref elem, readElementByRID(index + 1, arg), null);
				return elem;
			}
		}

		public LazyList(uint length, Func<uint, TArg, TValue> readElementByRID) {
			this.length = length;
			this.readElementByRID = readElementByRID;
			allElements = new TValue[((int)length + LazyListConstants.MaxArrayObjectElements - 1) / LazyListConstants.MaxArrayObjectElements][];
			for (int i = 0; i < allElements.Length; i++) {
				var partLen = Math.Min((int)length, LazyListConstants.MaxArrayObjectElements);
				allElements[i] = new TValue[partLen];
				length -= (uint)partLen;
			}
			if (length != 0)
				throw new InvalidOperationException();
		}
	}

	sealed class LazyList2<TValue, TArg1, TArg2> where TValue : class {
		readonly TValue[][] allElements;
		readonly Func<uint, TArg1, TArg2, (TValue elem, bool containedGenericParams)> readElementByRID;
		readonly uint length;

		public TValue this[uint index, TArg1 arg1, TArg2 arg2] {
			get {
				if (index >= length)
					return null;
				ref var elem = ref allElements[((int)index / LazyListConstants.MaxArrayObjectElements)][((int)index % LazyListConstants.MaxArrayObjectElements)];
				if (elem == null) {
					var info = readElementByRID(index + 1, arg1, arg2);
					if (info.containedGenericParams)
						return info.elem;
					Interlocked.CompareExchange(ref elem, info.elem, null);
				}
				return elem;
			}
		}

		public LazyList2(uint length, Func<uint, TArg1, TArg2, (TValue elem, bool containedGenericParams)> readElementByRID) {
			this.length = length;
			this.readElementByRID = readElementByRID;
			allElements = new TValue[((int)length + LazyListConstants.MaxArrayObjectElements - 1) / LazyListConstants.MaxArrayObjectElements][];
			for (int i = 0; i < allElements.Length; i++) {
				var partLen = Math.Min((int)length, LazyListConstants.MaxArrayObjectElements);
				allElements[i] = new TValue[partLen];
				length -= (uint)partLen;
			}
			if (length != 0)
				throw new InvalidOperationException();
		}
	}
}
