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
using System.Diagnostics;
using System.Linq;
using dnSpy.Contracts.Hex.Editor;
using dnSpy.Hex.MEF;

namespace dnSpy.Hex.Editor {
	abstract class WpfHexViewMarginProviderCollection {
		public abstract WpfHexViewMarginInfo[] Margins { get; }
		public abstract event EventHandler? MarginsChanged;
		public abstract void Dispose();
	}

	readonly struct WpfHexViewMarginInfo {
		public WpfHexViewMarginProvider Provider { get; }
		public IWpfHexViewMarginMetadata Metadata { get; }
		public WpfHexViewMargin Margin { get; }

		public WpfHexViewMarginInfo(WpfHexViewMarginProvider provider, IWpfHexViewMarginMetadata metadata, WpfHexViewMargin margin) {
			Provider = provider;
			Metadata = metadata;
			Margin = margin;
		}
	}

	sealed class WpfHexViewMarginProviderCollectionImpl : WpfHexViewMarginProviderCollection {
		readonly Lazy<WpfHexViewMarginProvider, IWpfHexViewMarginMetadata>[] wpfHexViewMarginProviders;
		readonly WpfHexViewHost wpfHexViewHost;
		readonly WpfHexViewMargin marginContainer;
		WpfHexViewMarginInfo[] currentMargins;

		public override WpfHexViewMarginInfo[] Margins => currentMargins;
		public override event EventHandler? MarginsChanged;

		public WpfHexViewMarginProviderCollectionImpl(Lazy<WpfHexViewMarginProvider, IWpfHexViewMarginMetadata>[] wpfHexViewMarginProviders, WpfHexViewHost wpfHexViewHost, WpfHexViewMargin marginContainer, string marginContainerName) {
			if (wpfHexViewMarginProviders is null)
				throw new ArgumentNullException(nameof(wpfHexViewMarginProviders));
			if (wpfHexViewHost is null)
				throw new ArgumentNullException(nameof(wpfHexViewHost));
			if (marginContainerName is null)
				throw new ArgumentNullException(nameof(marginContainerName));
			this.wpfHexViewMarginProviders = wpfHexViewMarginProviders.Where(a =>
				StringComparer.OrdinalIgnoreCase.Equals(marginContainerName, a.Metadata.MarginContainer) &&
				wpfHexViewHost.HexView.Roles.ContainsAny(a.Metadata.TextViewRoles)
			).ToArray();
			this.wpfHexViewHost = wpfHexViewHost;
			this.marginContainer = marginContainer ?? throw new ArgumentNullException(nameof(marginContainer));
			currentMargins = Array.Empty<WpfHexViewMarginInfo>();
			wpfHexViewHost.Closed += WpfHexViewHost_Closed;
			UpdateMargins();
		}

		void UpdateMargins() {
			var newInfos = new List<WpfHexViewMarginInfo>();

			var existingInfos = new Dictionary<WpfHexViewMarginProvider, WpfHexViewMarginInfo>();
			foreach (var info in currentMargins) {
				Debug.Assert(!existingInfos.ContainsKey(info.Provider));
				existingInfos[info.Provider] = info;
			}

			foreach (var lazy in wpfHexViewMarginProviders) {
				if (existingInfos.TryGetValue(lazy.Value, out var info)) {
					newInfos.Add(info);
					existingInfos.Remove(lazy.Value);
				}
				else {
					var margin = lazy.Value.CreateMargin(wpfHexViewHost, marginContainer);
					if (margin is not null)
						newInfos.Add(new WpfHexViewMarginInfo(lazy.Value, lazy.Metadata, margin));
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

		bool MarginsEquals(WpfHexViewMarginInfo[] a, List<WpfHexViewMarginInfo> b) {
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

		void WpfHexViewHost_Closed(object? sender, EventArgs e) => Dispose();

		public override void Dispose() {
			wpfHexViewHost.Closed -= WpfHexViewHost_Closed;
			foreach (var info in currentMargins)
				info.Margin.Dispose();
			currentMargins = Array.Empty<WpfHexViewMarginInfo>();
		}
	}
}
