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
using System.Windows.Controls;
using dnSpy.Contracts.Files.Tabs;
using dnSpy.Contracts.Files.TreeView;
using dnSpy.Contracts.Settings;

namespace dnSpy.AsmEditor.Hex.Nodes {
	[ExportFileTabContentFactory(Order = TabConstants.ORDER_HEXFILETABCONTENTFACTORY)]
	sealed class HexFileTabContentFactory : IFileTabContentFactory {
		public IFileTabContent Create(IFileTabContentFactoryContext context) {
			if (context.Nodes.Length == 1) {
				var hexNode = context.Nodes[0] as HexNode;
				if (hexNode != null)
					return new HexFileTabContent(hexNode);
			}

			return null;
		}

		static readonly Guid GUID_SerializedContent = new Guid("02B2234B-761B-47EC-95A1-F30783CF5990");

		public Guid? Serialize(IFileTabContent content, ISettingsSection section) {
			var dc = content as HexFileTabContent;
			if (dc == null)
				return null;

			return GUID_SerializedContent;
		}

		public IFileTabContent Deserialize(Guid guid, ISettingsSection section, IFileTabContentFactoryContext context) {
			if (guid != GUID_SerializedContent)
				return null;
			var hexNode = context.Nodes.Length != 1 ? null : context.Nodes[0] as HexNode;
			if (hexNode == null)
				return null;

			return new HexFileTabContent(hexNode);
		}
	}

	sealed class HexFileTabContent : IFileTabContent {
		public IFileTab FileTab { get; set; }

		public IEnumerable<IFileTreeNodeData> Nodes {
			get { yield return hexNode; }
		}

		public string Title => hexNode.ToString();
		public object ToolTip => hexNode.ToString();

		readonly HexNode hexNode;

		public HexFileTabContent(HexNode hexNode) {
			this.hexNode = hexNode;
		}

		public IFileTabContent Clone() => new HexFileTabContent(hexNode);
		public IFileTabUIContext CreateUIContext(IFileTabUIContextLocator locator) =>
			locator.Get(hexNode, () => new HexFileTabUIContext(hexNode.VMObject, hexNode.IsVirtualizingCollectionVM));
		public void OnHide() { }
		public void OnSelected() { }
		public void OnShow(IShowContext ctx) { }
		public void OnUnselected() { }
	}

	sealed class HexFileTabUIContext : IFileTabUIContext {
		public IFileTab FileTab { get; set; }
		public IInputElement FocusedElement => uiObj is ScrollViewer ? (IInputElement)((ScrollViewer)uiObj).Content : uiObj;
		public FrameworkElement ScaleElement => uiObj is ScrollViewer ? (FrameworkElement)((ScrollViewer)uiObj).Content : uiObj;
		public object UIObject => uiObj;

		readonly FrameworkElement uiObj;

		public HexFileTabUIContext(object vmObj, bool isVirtualizingCollection) {
			if (isVirtualizingCollection) {
				this.uiObj = new ContentPresenter {
					Content = vmObj,
					Focusable = true
				};
			}
			else {
				this.uiObj = new ScrollViewer {
					CanContentScroll = true,
					HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
					VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
					Content = new ContentPresenter {
						Content = vmObj,
						Focusable = true
					},
					Focusable = false,
				};
			}
		}

		public object CreateSerialized(ISettingsSection section) => null;
		public void Deserialize(object obj) { }
		public void OnHide() { }
		public void OnShow() { }
		public void SaveSerialized(ISettingsSection section, object obj) { }
		public object Serialize() => null;
	}
}
