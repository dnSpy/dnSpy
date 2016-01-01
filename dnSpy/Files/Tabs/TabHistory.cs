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
using System.Diagnostics;
using dnSpy.Contracts.Files.Tabs;

namespace dnSpy.Files.Tabs {
	struct TabContentState {
		public readonly IFileTabContent FileTabContent;
		public readonly object SerializedData;

		public TabContentState(IFileTabContent fileTabContent, object serializedData) {
			this.FileTabContent = fileTabContent;
			this.SerializedData = serializedData;
		}
	}

	sealed class TabHistory {
		readonly List<TabContentState> oldList;
		readonly List<TabContentState> newList;

		public IFileTabContent Current {
			get { return current; }
		}
		IFileTabContent current;

		public TabHistory() {
			this.oldList = new List<TabContentState>();
			this.newList = new List<TabContentState>();
		}

		public void SetCurrent(IFileTabContent content, bool saveCurrent) {
			if (content == null)
				throw new ArgumentNullException();
			if (saveCurrent && current != null)
				oldList.Add(new TabContentState(current, current.FileTab.UIContext.Serialize()));
			this.current = content;
			newList.Clear();
		}

		public bool CanNavigateBackward {
			get { return oldList.Count > 0; }
		}

		public bool CanNavigateForward {
			get { return newList.Count > 0; }
		}

		public object NavigateBackward() {
			Debug.Assert(CanNavigateBackward);
			if (oldList.Count == 0)
				return null;
			var old = oldList[oldList.Count - 1];
			oldList.RemoveAt(oldList.Count - 1);
			newList.Add(new TabContentState(current, current.FileTab.UIContext.Serialize()));
			current = old.FileTabContent;
			return old.SerializedData;
		}

		public object NavigateForward() {
			Debug.Assert(CanNavigateForward);
			if (newList.Count == 0)
				return null;
			var old = newList[newList.Count - 1];
			newList.RemoveAt(newList.Count - 1);
			oldList.Add(new TabContentState(current, current.FileTab.UIContext.Serialize()));
			current = old.FileTabContent;
			return old.SerializedData;
		}
	}
}
