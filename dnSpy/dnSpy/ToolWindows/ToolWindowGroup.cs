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
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using dnSpy.Contracts.Tabs;
using dnSpy.Contracts.ToolWindows;

namespace dnSpy.ToolWindows {
	sealed class ToolWindowGroup : IToolWindowGroup {
		public ITabGroup TabGroup { get; }
		public IToolWindowGroupService ToolWindowGroupService { get; }
		IEnumerable<TabContentImpl> TabContentImpls => TabGroup.TabContents.Cast<TabContentImpl>();
		public IEnumerable<ToolWindowContent> TabContents => TabContentImpls.Select(a => a.Content);

		public ToolWindowContent ActiveTabContent {
			get { return ((TabContentImpl)TabGroup.ActiveTabContent)?.Content; }
			set {
				if (value == null)
					throw new ArgumentNullException(nameof(value));
				var impl = GetTabContentImpl(value);
				if (impl == null)
					throw new ArgumentException();
				TabGroup.ActiveTabContent = impl;
			}
		}

		public ToolWindowGroup(IToolWindowGroupService toolWindowGroupService, ITabGroup tabGroup) {
			ToolWindowGroupService = toolWindowGroupService;
			TabGroup = tabGroup;
			TabGroup.Tag = this;
			TabGroup.TabContentAttached += TabGroup_TabContentAttached;
		}

		void TabGroup_TabContentAttached(object sender, TabContentAttachedEventArgs e) {
			var impl = e.TabContent as TabContentImpl;
			Debug.Assert(impl != null);
			if (impl == null)
				return;
			if (e.Attached)
				impl.Owner = this;
			else
				impl.Owner = null;
		}

		public static ToolWindowGroup GetToolWindowGroup(ITabGroup tabGroup) => (ToolWindowGroup)tabGroup?.Tag;
		TabContentImpl GetTabContentImpl(ToolWindowContent content) => TabContentImpls.FirstOrDefault(a => a.Content == content);
		public void Add(ToolWindowContent content) => TabGroup.Add(new TabContentImpl(this, content));

		public void Close(ToolWindowContent content) {
			if (content == null)
				throw new ArgumentNullException(nameof(content));
			var impl = GetTabContentImpl(content);
			Debug.Assert(impl != null);
			if (impl == null)
				return;
			TabGroup.Close(impl);
		}

		public void Close(TabContentImpl impl) => TabGroup.Close(impl);

		public void MoveTo(IToolWindowGroup destGroup, ToolWindowContent content) {
			if (destGroup == null)
				throw new ArgumentNullException(nameof(destGroup));
			if (content == null)
				throw new ArgumentNullException(nameof(content));
			var impl = GetTabContentImpl(content);
			Debug.Assert(impl != null);
			if (impl == null)
				throw new InvalidOperationException();
			if (destGroup == this)
				return;
			var destGroupImpl = destGroup as ToolWindowGroup;
			if (destGroupImpl == null)
				throw new InvalidOperationException();

			impl.PrepareMove();
			Close(impl);

			impl = new TabContentImpl(destGroupImpl, content);
			impl.PrepareMove();
			destGroupImpl.TabGroup.Add(impl);
		}

		public void SetFocus(TabContentImpl impl) => TabGroup.SetFocus(impl);

		public void SetFocus(ToolWindowContent content) {
			if (content == null)
				throw new ArgumentNullException(nameof(content));
			var impl = GetTabContentImpl(content);
			Debug.Assert(impl != null);
			if (impl == null)
				return;
			TabGroup.SetFocus(impl);
		}

		public bool CloseActiveTabCanExecute => TabGroup.CloseActiveTabCanExecute;
		public void CloseActiveTab() => TabGroup.CloseActiveTab();
	}
}
