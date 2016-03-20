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
using System.Threading;
using System.Windows;
using dnSpy.Contracts.App;
using dnSpy.Contracts.Controls;
using dnSpy.Contracts.Menus;
using dnSpy.Contracts.Themes;
using dnSpy.Contracts.ToolWindows;
using dnSpy.Contracts.ToolWindows.App;
using dnSpy.Debugger.Properties;
using dnSpy.Shared.Hex;

namespace dnSpy.Debugger.Memory {
	[Export, Export(typeof(IMainToolWindowContentCreator))]
	sealed class MemoryToolWindowContentCreator : IMainToolWindowContentCreator {
		public TWContent[] Contents {
			get { return contents; }
		}
		readonly TWContent[] contents;

		public sealed class TWContent {
			public int Index {
				get { return windowIndex; }
			}
			readonly int windowIndex;

			public Guid Guid {
				get { return guid; }
			}
			readonly Guid guid;

			public AppToolWindowLocation DefaultLocation {
				get { return AppToolWindowLocation.Default; }
			}

			public MemoryToolWindowContent Content {
				get {
					if (content == null)
						content = createContent();
					return content;
				}
			}
			MemoryToolWindowContent content;

			readonly Func<MemoryToolWindowContent> createContent;

			public TWContent(int windowIndex, Func<MemoryToolWindowContent> createContent) {
				this.windowIndex = windowIndex;
				this.createContent = createContent;
				this.guid = new Guid(string.Format("30AD8A10-5C72-47FF-A30D-9E2F{0:X8}", 0xBE2852B4 + (uint)windowIndex));
			}
		}

		readonly IWpfCommandManager wpfCommandManager;
		readonly IThemeManager themeManager;
		readonly IMenuManager menuManager;
		readonly IHexEditorSettings hexEditorSettings;
		readonly Lazy<ITheDebugger> theDebugger;
		readonly IAppSettings appSettings;

		[ImportingConstructor]
		MemoryToolWindowContentCreator(IWpfCommandManager wpfCommandManager, IThemeManager themeManager, IMenuManager menuManager, IHexEditorSettings hexEditorSettings, Lazy<ITheDebugger> theDebugger, IAppSettings appSettings) {
			this.wpfCommandManager = wpfCommandManager;
			this.themeManager = themeManager;
			this.menuManager = menuManager;
			this.hexEditorSettings = hexEditorSettings;
			this.theDebugger = theDebugger;
			this.appSettings = appSettings;
			this.contents = new TWContent[MemoryWindowsHelper.NUMBER_OF_MEMORY_WINDOWS];
			for (int i = 0; i < this.contents.Length; i++) {
				var tmpIndex = i;
				this.contents[i] = new TWContent(i, () => CreateContent(tmpIndex));
			}
		}

		MemoryToolWindowContent CreateContent(int index) {
			var content = contents[index];
			return new MemoryToolWindowContent(new Lazy<IMemoryContent>(() => CreateMemoryContent(content), LazyThreadSafetyMode.None), content.Guid, content.Index);
		}

		public IEnumerable<ToolWindowContentInfo> ContentInfos {
			get {
				foreach (var info in contents)
					yield return new ToolWindowContentInfo(info.Guid, info.DefaultLocation, AppToolWindowConstants.DEFAULT_CONTENT_ORDER_BOTTOM_DEBUGGER_MEMORY, false);
			}
		}

		public IToolWindowContent GetOrCreate(Guid guid) {
			foreach (var info in contents) {
				if (info.Guid == guid)
					return info.Content;
			}
			return null;
		}

		IMemoryContent CreateMemoryContent(TWContent info) {
			var vm = new MemoryVM(theDebugger.Value);
			return new MemoryContent(wpfCommandManager, themeManager, menuManager, hexEditorSettings, vm, appSettings);
		}
	}

	sealed class MemoryToolWindowContent : IToolWindowContent {
		public IInputElement FocusedElement {
			get { return memoryContent.Value.FocusedElement; }
		}

		public FrameworkElement ScaleElement {
			get { return memoryContent.Value.ScaleElement; }
		}

		public Guid Guid {
			get { return guid; }
		}
		readonly Guid guid;

		public string Title {
			get { return string.Format(dnSpy_Debugger_Resources.Window_Memory_N, windowIndex + 1); }
		}

		public object ToolTip {
			get { return null; }
		}

		public object UIObject {
			get { return memoryContent.Value.UIObject; }
		}

		public DnHexBox DnHexBox {
			get { return memoryContent.Value.DnHexBox; }
		}

		readonly Lazy<IMemoryContent> memoryContent;
		readonly int windowIndex;

		public MemoryToolWindowContent(Lazy<IMemoryContent> memoryContent, Guid guid, int windowIndex) {
			this.memoryContent = memoryContent;
			this.guid = guid;
			this.windowIndex = windowIndex;
		}

		public void OnVisibilityChanged(ToolWindowContentVisibilityEvent visEvent) {
			switch (visEvent) {
			case ToolWindowContentVisibilityEvent.Added:
				memoryContent.Value.OnShow();
				break;
			case ToolWindowContentVisibilityEvent.Removed:
				memoryContent.Value.OnClose();
				break;
			case ToolWindowContentVisibilityEvent.Visible:
				memoryContent.Value.OnVisible();
				break;
			case ToolWindowContentVisibilityEvent.Hidden:
				memoryContent.Value.OnHidden();
				break;
			}
		}
	}
}
