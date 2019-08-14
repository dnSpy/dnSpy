/*
    Copyright (C) 2014-2019 de4dot@gmail.com

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
using System.Diagnostics;
using System.Linq;
using System.Windows.Input;
using dnSpy.Contracts.Hex.Editor;
using dnSpy.Contracts.Menus;
using dnSpy.Hex.MEF;

namespace dnSpy.Hex.Editor {
	[Export(typeof(HexMarginContextMenuService))]
	sealed class HexMarginContextMenuServiceImpl : HexMarginContextMenuService {
		readonly Lazy<HexMarginContextMenuHandlerProvider, IMarginContextMenuHandlerProviderMetadata>[] marginContextMenuHandlerProviders;

		[ImportingConstructor]
		HexMarginContextMenuServiceImpl([ImportMany] IEnumerable<Lazy<HexMarginContextMenuHandlerProvider, IMarginContextMenuHandlerProviderMetadata>> marginContextMenuHandlerProviders) => this.marginContextMenuHandlerProviders = marginContextMenuHandlerProviders.ToArray();

		sealed class GuidObjectsProvider : IGuidObjectsProvider {
			readonly WpfHexViewHost wpfHexViewHost;
			readonly WpfHexViewMargin margin;
			readonly string marginName;
			readonly Lazy<HexMarginContextMenuHandlerProvider, IMarginContextMenuHandlerProviderMetadata>[] marginContextMenuHandlerProviders;
			IHexMarginContextMenuHandler[]? handlers;

			public GuidObjectsProvider(WpfHexViewHost wpfHexViewHost, WpfHexViewMargin margin, string marginName, Lazy<HexMarginContextMenuHandlerProvider, IMarginContextMenuHandlerProviderMetadata>[] marginContextMenuHandlerProviders) {
				this.wpfHexViewHost = wpfHexViewHost;
				this.margin = margin;
				this.marginName = marginName;
				this.marginContextMenuHandlerProviders = marginContextMenuHandlerProviders;
			}

			void InitializeHandlers() {
				if (!(handlers is null))
					return;
				var list = new List<IHexMarginContextMenuHandler>(marginContextMenuHandlerProviders.Length);
				foreach (var lazy in marginContextMenuHandlerProviders) {
					if (!StringComparer.OrdinalIgnoreCase.Equals(lazy.Metadata.MarginName, marginName))
						continue;
					if (!(lazy.Metadata.TextViewRoles is null) && !wpfHexViewHost.HexView.Roles.ContainsAny(lazy.Metadata.TextViewRoles))
						continue;
					var handler = lazy.Value.Create(wpfHexViewHost, margin);
					if (!(handler is null))
						list.Add(handler);
				}
				handlers = list.ToArray();
			}

			public IEnumerable<GuidObject> GetGuidObjects(GuidObjectsProviderArgs args) {
				if (handlers is null)
					InitializeHandlers();
				Debug2.Assert(!(handlers is null));

				var point = Mouse.PrimaryDevice.GetPosition(margin.VisualElement);

				yield return new GuidObject(MenuConstants.GUIDOBJ_WPF_HEXVIEW_HOST_GUID, wpfHexViewHost);
				yield return new GuidObject(MenuConstants.GUIDOBJ_WPF_HEXVIEW_GUID, wpfHexViewHost.HexView);
				yield return new GuidObject(MenuConstants.GUIDOBJ_WPF_HEXVIEW_MARGIN_GUID, margin);
				yield return new GuidObject(MenuConstants.GUIDOBJ_MARGIN_POINT_GUID, point);

				foreach (var handler in handlers) {
					foreach (var o in handler.GetContextMenuObjects(point))
						yield return o;
				}
			}
		}

		public override IGuidObjectsProvider Create(WpfHexViewHost wpfHexViewHost, WpfHexViewMargin margin, string marginName) {
			if (wpfHexViewHost is null)
				throw new ArgumentNullException(nameof(wpfHexViewHost));
			if (margin is null)
				throw new ArgumentNullException(nameof(margin));
			if (marginName is null)
				throw new ArgumentNullException(nameof(marginName));
			if (margin.GetHexViewMargin(marginName) != margin)
				throw new ArgumentException();
			return new GuidObjectsProvider(wpfHexViewHost, margin, marginName, marginContextMenuHandlerProviders);
		}
	}
}
