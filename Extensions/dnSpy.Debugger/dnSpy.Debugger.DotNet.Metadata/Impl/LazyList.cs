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
using System.Diagnostics;
using System.Threading;

namespace dnSpy.Debugger.DotNet.Metadata.Impl {
	[DebuggerDisplay("Count = {Length}")]
	sealed class LazyList<T> where T : class {
		[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
		readonly T[] elements;
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		readonly Func<uint, T> readElementByRID;
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		readonly uint length;

		uint Length => length;

		public T this[uint index] {
			get {
				if (index >= length)
					return null;
				ref var elem = ref elements[index];
				if (elem == null)
					Interlocked.CompareExchange(ref elem, readElementByRID(index + 1), null);
				return elem;
			}
		}

		public LazyList(uint length, Func<uint, T> readElementByRID) {
			this.length = length;
			this.readElementByRID = readElementByRID;
			elements = new T[length];
		}
	}

	[DebuggerDisplay("Count = {Length}")]
	sealed class LazyList<TValue, TArg> where TValue : class {
		[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
		readonly TValue[] elements;
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		readonly Func<uint, TArg, TValue> readElementByRID;
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		readonly uint length;

		uint Length => length;

		public TValue this[uint index, TArg arg] {
			get {
				if (index >= length)
					return null;
				ref var elem = ref elements[index];
				if (elem == null)
					Interlocked.CompareExchange(ref elem, readElementByRID(index + 1, arg), null);
				return elem;
			}
		}

		public LazyList(uint length, Func<uint, TArg, TValue> readElementByRID) {
			this.length = length;
			this.readElementByRID = readElementByRID;
			elements = new TValue[length];
		}
	}

	[DebuggerDisplay("Count = {Length}")]
	sealed class LazyList2<TValue, TArg> where TValue : class {
		[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
		readonly TValue[] elements;
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		readonly Func<uint, TArg, (TValue elem, bool containedGenericParams)> readElementByRID;
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		readonly uint length;

		uint Length => length;

		public TValue this[uint index, TArg arg] {
			get {
				if (index >= length)
					return null;
				ref var elem = ref elements[index];
				if (elem == null) {
					var info = readElementByRID(index + 1, arg);
					if (info.containedGenericParams)
						return info.elem;
					Interlocked.CompareExchange(ref elem, info.elem, null);
				}
				return elem;
			}
		}

		public LazyList2(uint length, Func<uint, TArg, (TValue elem, bool containedGenericParams)> readElementByRID) {
			this.length = length;
			this.readElementByRID = readElementByRID;
			elements = new TValue[length];
		}
	}

	[DebuggerDisplay("Count = {Length}")]
	sealed class LazyList2<TValue, TArg1, TArg2> where TValue : class {
		[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
		readonly TValue[] elements;
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		readonly Func<uint, TArg1, TArg2, (TValue elem, bool containedGenericParams)> readElementByRID;
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		readonly uint length;

		uint Length => length;

		public TValue this[uint index, TArg1 arg1, TArg2 arg2] {
			get {
				if (index >= length)
					return null;
				ref var elem = ref elements[index];
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
			elements = new TValue[length];
		}
	}
}
