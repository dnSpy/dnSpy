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
using dnSpy.Contracts.Files.Tabs;

namespace dnSpy.Files.Tabs {
	sealed class TabHistory {
		readonly List<IFileTabContent> oldList;
		readonly List<IFileTabContent> newList;

		public IFileTabContent Current {
			get { return current; }
		}
		IFileTabContent current;

		public TabHistory() {
			this.oldList = new List<IFileTabContent>();
			this.newList = new List<IFileTabContent>();
		}

		public void SetCurrent(IFileTabContent content, bool saveCurrent) {
			if (content == null)
				throw new ArgumentNullException();
			if (saveCurrent && current != null)
				oldList.Add(current);
			this.current = content;
			newList.Clear();
		}

		public bool CanNavigateBackward {
			get { return oldList.Count > 0; }
		}

		public bool CanNavigateForward {
			get { return newList.Count > 0; }
		}

		public void NavigateBackward() {
			Debug.Assert(CanNavigateBackward);
			if (oldList.Count == 0)
				return;
			var old = oldList[oldList.Count - 1];
			oldList.RemoveAt(oldList.Count - 1);
			newList.Add(current);
			current = old;
		}

		public void NavigateForward() {
			Debug.Assert(CanNavigateForward);
			if (newList.Count == 0)
				return;
			var old = newList[newList.Count - 1];
			newList.RemoveAt(newList.Count - 1);
			oldList.Add(current);
			current = old;
		}
	}
}
