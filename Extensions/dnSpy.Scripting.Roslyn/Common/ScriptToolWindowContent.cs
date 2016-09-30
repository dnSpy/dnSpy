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
using System.Windows;
using dnSpy.Contracts.Controls;
using dnSpy.Contracts.ToolWindows;
using dnSpy.Contracts.ToolWindows.App;

namespace dnSpy.Scripting.Roslyn.Common {
	abstract class ScriptToolWindowContentProvider : IMainToolWindowContentProvider {
		public ScriptToolWindowContent ScriptToolWindowContent => scriptToolWindowContent ?? (scriptToolWindowContent = CreateContent());
		ScriptToolWindowContent scriptToolWindowContent;

		readonly Guid contentGuid;

		protected ScriptToolWindowContentProvider(Guid contentGuid) {
			this.contentGuid = contentGuid;
		}

		public abstract IEnumerable<ToolWindowContentInfo> ContentInfos { get; }
		protected abstract ScriptToolWindowContent CreateContent();
		public IToolWindowContent GetOrCreate(Guid guid) => guid == contentGuid ? ScriptToolWindowContent : null;
	}

	abstract class ScriptToolWindowContent : IToolWindowContent, IZoomable {
		protected abstract IScriptContent ScriptContent { get; }
		public IInputElement FocusedElement => ScriptContent.FocusedElement;
		public FrameworkElement ZoomElement => ScriptContent.ZoomElement;
		public Guid Guid { get; }
		public abstract string Title { get; }
		public object ToolTip => null;
		public object UIObject => ScriptContent.UIObject;
		double IZoomable.ZoomValue => ScriptContent.ZoomLevel / 100.0;

		public ScriptToolWindowContent(Guid contentGuid) {
			this.Guid = contentGuid;
		}

		public void OnVisibilityChanged(ToolWindowContentVisibilityEvent visEvent) {
			switch (visEvent) {
			case ToolWindowContentVisibilityEvent.Added:
				ScriptContent.OnShow();
				break;
			case ToolWindowContentVisibilityEvent.Removed:
				ScriptContent.OnClose();
				break;
			case ToolWindowContentVisibilityEvent.Visible:
				ScriptContent.OnVisible();
				break;
			case ToolWindowContentVisibilityEvent.Hidden:
				ScriptContent.OnHidden();
				break;
			}
		}

		public void OnZoomChanged(double value) => ScriptContent.OnZoomChanged(value);
	}
}
