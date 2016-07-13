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
using System.Diagnostics;
using System.Linq;
using dnSpy.Text.MEF;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace dnSpy.Text.Editor {
	interface IWpfTextViewMarginProviderCollection {
		WpfTextViewMarginInfo[] Margins { get; }
		event EventHandler MarginsChanged;
		void Dispose();
	}

	struct WpfTextViewMarginInfo {
		public IWpfTextViewMarginProvider Provider { get; }
		public IWpfTextViewMarginMetadata Metadata { get; }
		public IWpfTextViewMargin Margin { get; }

		public WpfTextViewMarginInfo(IWpfTextViewMarginProvider provider, IWpfTextViewMarginMetadata metadata, IWpfTextViewMargin margin) {
			Provider = provider;
			Metadata = metadata;
			Margin = margin;
		}
	}

	sealed class WpfTextViewMarginProviderCollection : IWpfTextViewMarginProviderCollection {
		readonly Lazy<IWpfTextViewMarginProvider, IWpfTextViewMarginMetadata>[] wpfTextViewMarginProviders;
		readonly IWpfTextViewHost wpfTextViewHost;
		readonly IWpfTextViewMargin marginContainer;
		WpfTextViewMarginInfo[] currentMargins;

		public WpfTextViewMarginInfo[] Margins => currentMargins;
		public event EventHandler MarginsChanged;

		public WpfTextViewMarginProviderCollection(Lazy<IWpfTextViewMarginProvider, IWpfTextViewMarginMetadata>[] wpfTextViewMarginProviders, IWpfTextViewHost wpfTextViewHost, IWpfTextViewMargin marginContainer, string marginContainerName) {
			if (wpfTextViewMarginProviders == null)
				throw new ArgumentNullException(nameof(wpfTextViewMarginProviders));
			if (wpfTextViewHost == null)
				throw new ArgumentNullException(nameof(wpfTextViewHost));
			if (marginContainer == null)
				throw new ArgumentNullException(nameof(marginContainer));
			if (marginContainerName == null)
				throw new ArgumentNullException(nameof(marginContainerName));
			this.wpfTextViewMarginProviders = wpfTextViewMarginProviders.Where(a =>
				StringComparer.OrdinalIgnoreCase.Equals(marginContainerName, a.Metadata.MarginContainer) &&
				wpfTextViewHost.TextView.Roles.ContainsAny(a.Metadata.TextViewRoles)
			).ToArray();
			this.wpfTextViewHost = wpfTextViewHost;
			this.marginContainer = marginContainer;
			this.currentMargins = Array.Empty<WpfTextViewMarginInfo>();
			wpfTextViewHost.Closed += WpfTextViewHost_Closed;
			wpfTextViewHost.TextView.TextDataModel.ContentTypeChanged += TextDataModel_ContentTypeChanged;
			UpdateMargins();
		}

		void UpdateMargins() {
			var newInfos = new List<WpfTextViewMarginInfo>();

			var existingInfos = new Dictionary<IWpfTextViewMarginProvider, WpfTextViewMarginInfo>();
			foreach (var info in currentMargins) {
				Debug.Assert(!existingInfos.ContainsKey(info.Provider));
				existingInfos[info.Provider] = info;
			}

			foreach (var lazy in wpfTextViewMarginProviders) {
				if (!CanUse(lazy.Metadata))
					continue;
				WpfTextViewMarginInfo info;
				if (existingInfos.TryGetValue(lazy.Value, out info)) {
					newInfos.Add(info);
					existingInfos.Remove(lazy.Value);
				}
				else {
					var margin = lazy.Value.CreateMargin(wpfTextViewHost, marginContainer);
					if (margin != null)
						newInfos.Add(new WpfTextViewMarginInfo(lazy.Value, lazy.Metadata, margin));
				}
			}

			if (!MarginsEquals(currentMargins, newInfos)) {
				currentMargins = newInfos.ToArray();
				MarginsChanged?.Invoke(this, EventArgs.Empty);
			}

			// Dispose of them after raising the MarginsChanged event in case they're
			// accessed by the event listeners
			foreach (var info in existingInfos.Values)
				info.Margin.Dispose();
		}

		bool MarginsEquals(WpfTextViewMarginInfo[] a, List<WpfTextViewMarginInfo> b) {
			if (a.Length != b.Count)
				return false;
			for (int i = 0; i < a.Length; i++) {
				if (a[i].Provider != b[i].Provider)
					return false;
				if (a[i].Margin != b[i].Margin)
					return false;
			}
			return true;
		}

		bool CanUse(IWpfTextViewMarginMetadata md) {
			var contentType = wpfTextViewHost.TextView.TextDataModel.ContentType;
			foreach (var ct in md.ContentTypes) {
				if (contentType.IsOfType(ct))
					return true;
			}

			return false;
		}

		void TextDataModel_ContentTypeChanged(object sender, TextDataModelContentTypeChangedEventArgs e) => UpdateMargins();
		void WpfTextViewHost_Closed(object sender, EventArgs e) => Dispose();

		public void Dispose() {
			wpfTextViewHost.Closed -= WpfTextViewHost_Closed;
			wpfTextViewHost.TextView.TextDataModel.ContentTypeChanged -= TextDataModel_ContentTypeChanged;
			foreach (var info in currentMargins)
				info.Margin.Dispose();
			currentMargins = Array.Empty<WpfTextViewMarginInfo>();
		}
	}
}
