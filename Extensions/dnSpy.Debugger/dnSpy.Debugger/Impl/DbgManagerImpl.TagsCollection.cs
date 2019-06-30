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
using System.Diagnostics;
using System.Linq;

namespace dnSpy.Debugger.Impl {
	sealed partial class DbgManagerImpl {
		sealed class TagsCollection {
			public string[] Tags => list.Select(a => a.tag).ToArray();

			readonly List<(string tag, int refCount)> list;

			public TagsCollection() => list = new List<(string, int)>();

			int GetIndex(string tag) {
				for (int i = 0; i < list.Count; i++) {
					if (list[i].tag == tag)
						return i;
				}
				return -1;
			}

			public string[] Add(string[] tags) {
				var addedTags = new List<string>();
				foreach (var tag in tags) {
					int index = GetIndex(tag);
					if (index >= 0)
						list[index] = (tag, list[index].refCount + 1);
					else {
						list.Add((tag, 1));
						addedTags.Add(tag);
					}
				}
				return addedTags.ToArray();
			}

			public string[] Remove(string[] tags) {
				var removedTags = new List<string>();
				foreach (var tag in tags) {
					int index = GetIndex(tag);
					Debug.Assert(index >= 0);
					if (index >= 0) {
						int refCount = list[index].refCount;
						Debug.Assert(refCount >= 1);
						if (refCount > 1)
							list[index] = (tag, refCount - 1);
						else {
							list.RemoveAt(index);
							removedTags.Add(tag);
						}
					}
				}
				return removedTags.ToArray();
			}
		}
	}
}
