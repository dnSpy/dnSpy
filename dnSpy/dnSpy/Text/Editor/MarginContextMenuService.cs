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
using System.Linq;
using System.Windows.Input;
using dnSpy.Contracts.Menus;
using dnSpy.Contracts.Text.Editor;
using dnSpy.Text.MEF;
using Microsoft.VisualStudio.Text.Editor;

namespace dnSpy.Text.Editor {
	[Export(typeof(IMarginContextMenuService))]
	sealed class MarginContextMenuService : IMarginContextMenuService {
		readonly Lazy<IMarginContextMenuHandlerProvider, IMarginContextMenuHandlerProviderMetadata>[] marginContextMenuHandlerProviders;

		[ImportingConstructor]
		MarginContextMenuService([ImportMany] IEnumerable<Lazy<IMarginContextMenuHandlerProvider, IMarginContextMenuHandlerProviderMetadata>> marginContextMenuHandlerProviders) {
			this.marginContextMenuHandlerProviders = marginContextMenuHandlerProviders.ToArray();
		}

		sealed class GuidObjectsProvider : IGuidObjectsProvider {
			readonly IWpfTextViewHost wpfTextViewHost;
			readonly IWpfTextViewMargin margin;
			readonly string marginName;
			readonly Lazy<IMarginContextMenuHandlerProvider, IMarginContextMenuHandlerProviderMetadata>[] marginContextMenuHandlerProviders;
			IMarginContextMenuHandler[] handlers;

			public GuidObjectsProvider(IWpfTextViewHost wpfTextViewHost, IWpfTextViewMargin margin, string marginName, Lazy<IMarginContextMenuHandlerProvider, IMarginContextMenuHandlerProviderMetadata>[] marginContextMenuHandlerProviders) {
				this.wpfTextViewHost = wpfTextViewHost;
				this.margin = margin;
				this.marginName = marginName;
				this.marginContextMenuHandlerProviders = marginContextMenuHandlerProviders;
			}

			void InitializeHandlers() {
				if (handlers != null)
					return;
				var list = new List<IMarginContextMenuHandler>(marginContextMenuHandlerProviders.Length);
				foreach (var lazy in marginContextMenuHandlerProviders) {
					if (!StringComparer.OrdinalIgnoreCase.Equals(lazy.Metadata.MarginName, marginName))
						continue;
					if (lazy.Metadata.TextViewRoles != null && !wpfTextViewHost.TextView.Roles.ContainsAny(lazy.Metadata.TextViewRoles))
						continue;
					var handler = lazy.Value.Create(wpfTextViewHost, margin);
					if (handler != null)
						list.Add(handler);
				}
				handlers = list.ToArray();
			}

			public IEnumerable<GuidObject> GetGuidObjects(GuidObjectsProviderArgs args) {
				if (handlers == null)
					InitializeHandlers();

				var point = Mouse.PrimaryDevice.GetPosition(margin.VisualElement);

				yield return new GuidObject(MenuConstants.GUIDOBJ_WPF_TEXTVIEW_HOST_GUID, wpfTextViewHost);
				yield return new GuidObject(MenuConstants.GUIDOBJ_WPF_TEXTVIEW_GUID, wpfTextViewHost.TextView);
				yield return new GuidObject(MenuConstants.GUIDOBJ_WPF_TEXTVIEW_MARGIN_GUID, margin);
				yield return new GuidObject(MenuConstants.GUIDOBJ_MARGIN_POINT_GUID, point);

				foreach (var handler in handlers) {
					foreach (var o in handler.GetContextMenuObjects(point))
						yield return o;
				}
			}
		}

		public IGuidObjectsProvider Create(IWpfTextViewHost wpfTextViewHost, IWpfTextViewMargin margin, string marginName) {
			if (wpfTextViewHost == null)
				throw new ArgumentNullException(nameof(wpfTextViewHost));
			if (margin == null)
				throw new ArgumentNullException(nameof(margin));
			if (marginName == null)
				throw new ArgumentNullException(nameof(marginName));
			if (margin.GetTextViewMargin(marginName) != margin)
				throw new ArgumentException();
			return new GuidObjectsProvider(wpfTextViewHost, margin, marginName, marginContextMenuHandlerProviders);
		}
	}
}
