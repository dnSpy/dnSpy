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
using System.ComponentModel;
using System.Windows;
using dnSpy.Contracts.App;
using dnSpy.Contracts.Controls;
using dnSpy.Contracts.Hex;
using dnSpy.Contracts.HexEditor;
using dnSpy.Contracts.Menus;

namespace dnSpy.Debugger.Memory {
	interface IMemoryContent : IUIObjectProvider {
		void OnShow();
		void OnClose();
		void OnVisible();
		void OnHidden();
		DnHexBox DnHexBox { get; }
	}

	sealed class MemoryContent : IMemoryContent {
		public object UIObject => memoryControl;
		public IInputElement FocusedElement => memoryControl.DnHexBox;
		public FrameworkElement ZoomElement => memoryControl;
		public DnHexBox DnHexBox => memoryControl.DnHexBox;

		readonly MemoryControl memoryControl;
		readonly IMemoryVM vmMemory;

		public MemoryContent(IWpfCommandService wpfCommandService, IMenuService menuService, IHexEditorSettings hexEditorSettings, IMemoryVM memoryVM, IAppSettings appSettings) {
			this.memoryControl = new MemoryControl();
			this.vmMemory = memoryVM;
			this.vmMemory.SetRefreshLines(() => this.memoryControl.DnHexBox.RedrawModifiedLines());
			this.memoryControl.DataContext = this.vmMemory;
			var dnHexBox = new DnHexBox(menuService, hexEditorSettings) {
				CacheLineBytes = true,
				IsMemory = true,
			};
			dnHexBox.SetBinding(HexBox.DocumentProperty, nameof(vmMemory.HexDocument));
			this.memoryControl.DnHexBox = dnHexBox;
			dnHexBox.StartOffset = 0;
			dnHexBox.EndOffset = IntPtr.Size == 4 ? uint.MaxValue : ulong.MaxValue;

			appSettings.PropertyChanged += AppSettings_PropertyChanged;
			UpdateHexBoxRenderer(appSettings.UseNewRenderer_HexEditor);

			wpfCommandService.Add(ControlConstants.GUID_DEBUGGER_MEMORY_CONTROL, memoryControl);
			wpfCommandService.Add(ControlConstants.GUID_DEBUGGER_MEMORY_HEXBOX, memoryControl.DnHexBox);
		}

		void AppSettings_PropertyChanged(object sender, PropertyChangedEventArgs e) {
			var appSettings = (IAppSettings)sender;
			if (e.PropertyName == nameof(appSettings.UseNewRenderer_HexEditor))
				UpdateHexBoxRenderer(appSettings.UseNewRenderer_HexEditor);
		}

		void UpdateHexBoxRenderer(bool useNewRenderer) => this.memoryControl.DnHexBox.UseNewFormatter = useNewRenderer;
		public void OnClose() => vmMemory.IsEnabled = false;
		public void OnShow() => vmMemory.IsEnabled = true;
		public void OnHidden() => vmMemory.IsVisible = false;
		public void OnVisible() => vmMemory.IsVisible = true;
	}
}
