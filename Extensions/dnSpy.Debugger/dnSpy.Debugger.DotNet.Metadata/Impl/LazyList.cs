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
				if (elements[index] == null)
					Interlocked.CompareExchange(ref elements[index], readElementByRID(index + 1), null);
				return elements[index];
			}
		}

		public LazyList(uint length, Func<uint, T> readElementByRID) {
			this.length = length;
			this.readElementByRID = readElementByRID;
			this.elements = new T[length];
		}
	}
}
