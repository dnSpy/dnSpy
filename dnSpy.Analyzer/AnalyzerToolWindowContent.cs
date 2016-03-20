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
using System.ComponentModel.Composition;
using System.Windows;
using dnSpy.Analyzer.Properties;
using dnSpy.Contracts.Controls;
using dnSpy.Contracts.ToolWindows;
using dnSpy.Contracts.ToolWindows.App;

namespace dnSpy.Analyzer {
	[Export(typeof(IMainToolWindowContentCreator))]
	sealed class AnalyzerToolWindowContentCreator : IMainToolWindowContentCreator {
		readonly Lazy<IAnalyzerManager> analyzerManager;

		public AnalyzerToolWindowContent FileTreeViewWindowContent {
			get { return analyzerToolWindowContent ?? (analyzerToolWindowContent = new AnalyzerToolWindowContent(analyzerManager)); }
		}
		AnalyzerToolWindowContent analyzerToolWindowContent;

		[ImportingConstructor]
		AnalyzerToolWindowContentCreator(Lazy<IAnalyzerManager> analyzerManager) {
			this.analyzerManager = analyzerManager;
		}

		public IEnumerable<ToolWindowContentInfo> ContentInfos {
			get { yield return new ToolWindowContentInfo(AnalyzerToolWindowContent.THE_GUID, AnalyzerToolWindowContent.DEFAULT_LOCATION, AppToolWindowConstants.DEFAULT_CONTENT_ORDER_BOTTOM_ANALYZER, false); }
		}

		public IToolWindowContent GetOrCreate(Guid guid) {
			if (guid == AnalyzerToolWindowContent.THE_GUID)
				return FileTreeViewWindowContent;
			return null;
		}
	}

	sealed class AnalyzerToolWindowContent : IToolWindowContent, IFocusable {
		public static readonly Guid THE_GUID = new Guid("5827D693-A5DF-4D65-B1F8-ACF249508A96");
		public const AppToolWindowLocation DEFAULT_LOCATION = AppToolWindowLocation.Default;

		public IInputElement FocusedElement {
			get { return null; }
		}

		public FrameworkElement ScaleElement {
			get { return analyzerManager.Value.TreeView.UIObject as FrameworkElement; }
		}

		public Guid Guid {
			get { return THE_GUID; }
		}

		public string Title {
			get { return dnSpy_Analyzer_Resources.AnalyzerWindowTitle; }
		}

		public object ToolTip {
			get { return null; }
		}

		public object UIObject {
			get { return analyzerManager.Value.TreeView.UIObject; }
		}

		public bool CanFocus {
			get { return true; }
		}

		readonly Lazy<IAnalyzerManager> analyzerManager;

		public AnalyzerToolWindowContent(Lazy<IAnalyzerManager> analyzerManager) {
			this.analyzerManager = analyzerManager;
		}

		public void OnVisibilityChanged(ToolWindowContentVisibilityEvent visEvent) {
			if (visEvent == ToolWindowContentVisibilityEvent.Removed)
				analyzerManager.Value.OnClose();
		}

		public void Focus() {
			analyzerManager.Value.TreeView.Focus();
		}
	}
}
