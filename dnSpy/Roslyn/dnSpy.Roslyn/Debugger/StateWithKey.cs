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
using System.Diagnostics;
using dnSpy.Debugger.DotNet.Metadata;

namespace dnSpy.Roslyn.Debugger {
	sealed class StateWithKey<T> where T : class {
		readonly object lockObj = new object();
		readonly List<(object key, T data)> list = new List<(object, T)>();

		public static T? TryGet(DmdObject obj, object key) {
			Debug2.Assert(obj is not null);
			Debug2.Assert(key is not null);
			var state = obj.GetOrCreateData<StateWithKey<T>>();
			lock (state.lockObj) {
				var list = state.list;
				for (int i = 0; i < list.Count; i++) {
					var info = list[i];
					if (info.key == key)
						return info.data;
				}
				return null;
			}
		}

		public static T GetOrCreate(DmdObject obj, object key, Func<T> create) {
			Debug2.Assert(obj is not null);
			Debug2.Assert(key is not null);
			var state = obj.GetOrCreateData<StateWithKey<T>>();
			lock (state.lockObj) {
				var list = state.list;
				for (int i = 0; i < list.Count; i++) {
					var info = list[i];
					if (info.key == key)
						return info.data;
				}
				var data = create();
				list.Add((key, data));
				return data;
			}
		}
	}
}
