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
using System.Windows;
using dnSpy.Contracts.Controls;
using dnSpy.Contracts.Hex.Editor;
using dnSpy.Contracts.Hex.Editor.HexGroups;
using dnSpy.Contracts.Menus;

namespace dnSpy.Debugger.Memory {
	interface IMemoryContent : IUIObjectProvider, IZoomable {
		void OnShow();
		void OnClose();
		void OnVisible();
		void OnHidden();
		WpfHexView HexView { get; }
	}

	sealed class MemoryContent : IMemoryContent {
		public IInputElement FocusedElement => hexViewHost.HexView.VisualElement;
		public FrameworkElement ZoomElement => null;
		public object UIObject => memoryControl;
		public WpfHexView HexView => hexViewHost.HexView;
		double IZoomable.ZoomValue => hexViewHost.HexView.ZoomLevel / 100;

		readonly MemoryControl memoryControl;
		readonly IMemoryVM vmMemory;
		readonly WpfHexViewHost hexViewHost;

		public MemoryContent(IWpfCommandService wpfCommandService, IMemoryVM memoryVM, HexEditorGroupFactoryService hexEditorGroupFactoryService) {
			vmMemory = memoryVM;
			memoryVM.UnderlyingStreamChanged += MemoryVM_UnderlyingStreamChanged;

			hexViewHost = hexEditorGroupFactoryService.Create(memoryVM.Buffer, PredefinedHexViewRoles.HexEditorGroup, PredefinedHexViewRoles.HexEditorGroupDebuggerMemory, new Guid(MenuConstants.GUIDOBJ_DEBUGGER_MEMORY_HEXVIEW_GUID));
			memoryControl = new MemoryControl(hexViewHost.HostControl);
			memoryControl.DataContext = vmMemory;

			wpfCommandService.Add(ControlConstants.GUID_DEBUGGER_MEMORY_CONTROL, memoryControl);
			wpfCommandService.Add(ControlConstants.GUID_DEBUGGER_MEMORY_WPFHEXVIEWHOST, hexViewHost.HostControl);
		}

		void MemoryVM_UnderlyingStreamChanged(object sender, EventArgs e) {
			if (HexView.IsClosed)
				return;
			HexView.Options.SetOptionValue(DefaultHexViewOptions.StartPositionId, vmMemory.Buffer.Span.Start);
			HexView.Options.SetOptionValue(DefaultHexViewOptions.EndPositionId, vmMemory.Buffer.Span.End);
		}

		public void OnClose() { }
		public void OnShow() { }
		public void OnHidden() { }
		public void OnVisible() { }
	}
}
