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
using System.Windows;
using System.Windows.Controls;
using dnSpy.Contracts.Documents.Tabs;
using dnSpy.Contracts.Documents.TreeView;
using dnSpy.Contracts.Settings;

namespace dnSpy.AsmEditor.Hex.Nodes {
	[ExportDocumentTabContentFactory(Order = TabConstants.ORDER_HEXDOCUMENTTABCONTENTFACTORY)]
	sealed class HexDocumentTabContentFactory : IDocumentTabContentFactory {
		public DocumentTabContent Create(IDocumentTabContentFactoryContext context) {
			if (context.Nodes.Length == 1) {
				if (context.Nodes[0] is HexNode hexNode)
					return new HexDocumentTabContent(hexNode);
			}

			return null;
		}

		static readonly Guid GUID_SerializedContent = new Guid("02B2234B-761B-47EC-95A1-F30783CF5990");

		public Guid? Serialize(DocumentTabContent content, ISettingsSection section) {
			var dc = content as HexDocumentTabContent;
			if (dc == null)
				return null;

			return GUID_SerializedContent;
		}

		public DocumentTabContent Deserialize(Guid guid, ISettingsSection section, IDocumentTabContentFactoryContext context) {
			if (guid != GUID_SerializedContent)
				return null;
			var hexNode = context.Nodes.Length != 1 ? null : context.Nodes[0] as HexNode;
			if (hexNode == null)
				return null;

			return new HexDocumentTabContent(hexNode);
		}
	}

	sealed class HexDocumentTabContent : DocumentTabContent {
		public override IEnumerable<DocumentTreeNodeData> Nodes {
			get { yield return hexNode; }
		}

		public override string Title => hexNode.ToString();
		public override object ToolTip => hexNode.ToString();

		readonly HexNode hexNode;

		public HexDocumentTabContent(HexNode hexNode) => this.hexNode = hexNode;

		public override DocumentTabContent Clone() => new HexDocumentTabContent(hexNode);
		public override DocumentTabUIContext CreateUIContext(IDocumentTabUIContextLocator locator) =>
			locator.Get(hexNode, () => new HexDocumentTabUIContext(hexNode.VMObject, hexNode.IsVirtualizingCollectionVM));
	}

	sealed class HexDocumentTabUIContext : DocumentTabUIContext {
		public override IInputElement FocusedElement => uiObj is ScrollViewer ? (IInputElement)((ScrollViewer)uiObj).Content : uiObj;
		public override FrameworkElement ZoomElement => uiObj is ScrollViewer ? (FrameworkElement)((ScrollViewer)uiObj).Content : uiObj;
		public override object UIObject => uiObj;

		readonly FrameworkElement uiObj;

		public HexDocumentTabUIContext(object vmObj, bool isVirtualizingCollection) {
			if (isVirtualizingCollection) {
				uiObj = new ContentPresenter {
					Content = vmObj,
					Focusable = true
				};
			}
			else {
				uiObj = new ScrollViewer {
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
	}
}
