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
using dnSpy.Contracts.Controls;
using dnSpy.Contracts.Hex.Editor;
using dnSpy.Contracts.Hex.Editor.HexGroups;
using dnSpy.Contracts.ToolWindows;
using dnSpy.Contracts.ToolWindows.App;
using dnSpy.Debugger.Old.Properties;

namespace dnSpy.Debugger.Memory {
	//[Export, Export(typeof(IToolWindowContentProvider))]
	sealed class MemoryToolWindowContentProvider : IToolWindowContentProvider {
		public TWContent[] Contents => contents;
		readonly TWContent[] contents;

		public sealed class TWContent {
			public int Index { get; }
			public Guid Guid { get; }
			public AppToolWindowLocation DefaultLocation => AppToolWindowLocation.DefaultHorizontal;

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
				Index = windowIndex;
				this.createContent = createContent;
				Guid = new Guid($"30AD8A10-5C72-47FF-A30D-9E2F{0xBE2852B4 + (uint)windowIndex:X8}");
			}
		}

		readonly IWpfCommandService wpfCommandService;
		readonly Lazy<HexEditorGroupFactoryService> hexEditorGroupFactoryService;
		readonly Lazy<IMemoryVM> memoryVM;

		[ImportingConstructor]
		MemoryToolWindowContentProvider(IWpfCommandService wpfCommandService, Lazy<HexEditorGroupFactoryService> hexEditorGroupFactoryService, Lazy<IMemoryVM> memoryVM) {
			this.wpfCommandService = wpfCommandService;
			this.hexEditorGroupFactoryService = hexEditorGroupFactoryService;
			this.memoryVM = memoryVM;
			contents = new TWContent[MemoryWindowsHelper.NUMBER_OF_MEMORY_WINDOWS];
			for (int i = 0; i < contents.Length; i++) {
				var tmpIndex = i;
				contents[i] = new TWContent(i, () => CreateContent(tmpIndex));
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

		public ToolWindowContent GetOrCreate(Guid guid) {
			foreach (var info in contents) {
				if (info.Guid == guid)
					return info.Content;
			}
			return null;
		}

		IMemoryContent CreateMemoryContent(TWContent info) =>
			new MemoryContent(wpfCommandService, memoryVM.Value, hexEditorGroupFactoryService.Value);
	}

	sealed class MemoryToolWindowContent : ToolWindowContent, IZoomable {
		public override IInputElement FocusedElement => memoryContent.Value.FocusedElement;
		public override FrameworkElement ZoomElement => memoryContent.Value.ZoomElement;
		public override Guid Guid { get; }
		public override string Title => string.Format(dnSpy_Debugger_Resources.Window_Memory_N, windowIndex + 1);
		public override object UIObject => memoryContent.Value.UIObject;
		public WpfHexView HexView => memoryContent.Value.HexView;
		double IZoomable.ZoomValue => memoryContent.Value.ZoomValue;

		readonly Lazy<IMemoryContent> memoryContent;
		readonly int windowIndex;

		public MemoryToolWindowContent(Lazy<IMemoryContent> memoryContent, Guid guid, int windowIndex) {
			this.memoryContent = memoryContent;
			Guid = guid;
			this.windowIndex = windowIndex;
		}

		public override void OnVisibilityChanged(ToolWindowContentVisibilityEvent visEvent) {
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
