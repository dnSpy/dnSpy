/*
    Copyright (C) 2014-2015 de4dot@gmail.com

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
using System.Linq;
using dnSpy.Contracts.Menus;

namespace dnSpy.Menus {
	sealed class MenuItemContext : IMenuItemContext {
		public bool OpenedFromKeyboard { get; private set; }

		public Guid MenuGuid { get; private set; }

		public GuidObject CreatorObject {
			get { return guidObjects[0]; }
		}

		public IEnumerable<GuidObject> GuidObjects {
			get { return guidObjects.AsEnumerable(); }
		}
		readonly List<GuidObject> guidObjects;

		public MenuItemContext(Guid menuGuid, bool openedFromKeyboard, GuidObject creatorObject, IEnumerable<GuidObject> guidObjects) {
			this.MenuGuid = menuGuid;
			this.OpenedFromKeyboard = openedFromKeyboard;
			this.guidObjects = new List<GuidObject>();
			this.guidObjects.Add(creatorObject);
			if (guidObjects != null)
				this.guidObjects.AddRange(guidObjects);
			this.state = new Dictionary<object, object>();
		}

		public T GetOrCreateState<T>(object key, Func<T> createState) where T : class {
			Debug.Assert(key != null);
			object o;
			T value;
			if (state.TryGetValue(key, out o)) {
				value = o as T;
				Debug.Assert(o == null || value != null);
				return value;
			}
			value = createState();
			state[key] = value;
			return value;
		}
		readonly Dictionary<object, object> state;
	}
}
