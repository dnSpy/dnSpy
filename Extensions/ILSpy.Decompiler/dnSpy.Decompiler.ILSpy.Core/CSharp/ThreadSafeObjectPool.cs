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
using System.Collections.Generic;

namespace dnSpy.Decompiler.ILSpy.Core.CSharp {
	sealed class ThreadSafeObjectPool<T> where T : class {
		readonly List<T> freeObjs;
		readonly Func<T> createObject;
		readonly Action<T> resetObject;
		readonly object lockObj = new object();

		public ThreadSafeObjectPool(int size, Func<T> createObject, Action<T> resetObject) {
			if (size <= 0)
				throw new ArgumentException();
			freeObjs = new List<T>(size);
			this.createObject = createObject;
			this.resetObject = resetObject;
		}

		public T Allocate() {
			lock (lockObj) {
				if (freeObjs.Count > 0) {
					int i = freeObjs.Count - 1;
					var o = freeObjs[i];
					freeObjs.RemoveAt(i);
					return o;
				}

				return createObject();
			}
		}

		public void Free(T obj) {
			resetObject(obj);
			lock (lockObj)
				freeObjs.Add(obj);
		}
	}
}
