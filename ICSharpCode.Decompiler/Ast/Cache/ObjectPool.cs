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

namespace ICSharpCode.Decompiler.Ast.Cache {
	/// <summary>
	/// Object pool. It's not thread safe.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	sealed class ObjectPool<T> where T : class {
		readonly Func<T> create;
		readonly Action<T> initialize;

		/// <summary>
		/// All allocated objects. Used to restore <see cref="freeObjs"/> when <see cref="ReuseAllObjects"/>
		/// gets called.
		/// </summary>
		readonly List<T> allObjs;

		/// <summary>
		/// All free objects. A subset of <see cref="allObjs"/>
		/// </summary>
		readonly List<T> freeObjs;

		public ObjectPool(Func<T> create, Action<T> initialize) {
			this.create = create;
			this.initialize = initialize;
			this.allObjs = new List<T>();
			this.freeObjs = new List<T>();
		}

		public T Allocate() {
			if (freeObjs.Count > 0) {
				int i = freeObjs.Count - 1;
				var o = freeObjs[i];
				freeObjs.RemoveAt(i);
				if (initialize != null)
					initialize(o);
				return o;
			}

			var newObj = create();
			allObjs.Add(newObj);
			return newObj;
		}

		public void Free(T obj) {
			freeObjs.Add(obj);
		}

		public void ReuseAllObjects() {
			freeObjs.Clear();
			freeObjs.AddRange(allObjs);
		}
	}
}
