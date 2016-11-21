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
using dnSpy.Contracts.Hex.Editor;
using dnSpy.Hex.MEF;
using VSUTIL = Microsoft.VisualStudio.Utilities;

namespace dnSpy.Hex.Editor {
	abstract class WpfHexViewMarginProviderCollectionProvider {
		public abstract WpfHexViewMarginProviderCollection Create(WpfHexViewHost wpfHexViewHost, WpfHexViewMargin marginContainer, string marginContainerName);
	}

	[Export(typeof(WpfHexViewMarginProviderCollectionProvider))]
	sealed class WpfHexViewMarginProviderCollectionProviderImpl : WpfHexViewMarginProviderCollectionProvider {
		readonly Lazy<WpfHexViewMarginProvider, IWpfHexViewMarginMetadata>[] wpfHexViewMarginProviders;

		[ImportingConstructor]
		WpfHexViewMarginProviderCollectionProviderImpl([ImportMany] IEnumerable<Lazy<WpfHexViewMarginProvider, IWpfHexViewMarginMetadata>> wpfHexViewMarginProviders) {
			this.wpfHexViewMarginProviders = VSUTIL.Orderer.Order(wpfHexViewMarginProviders).ToArray();
		}

		public override WpfHexViewMarginProviderCollection Create(WpfHexViewHost wpfHexViewHost, WpfHexViewMargin marginContainer, string marginContainerName) {
			if (wpfHexViewHost == null)
				throw new ArgumentNullException(nameof(wpfHexViewHost));
			if (marginContainer == null)
				throw new ArgumentNullException(nameof(marginContainer));
			if (marginContainerName == null)
				throw new ArgumentNullException(nameof(marginContainerName));
			return new WpfHexViewMarginProviderCollectionImpl(wpfHexViewMarginProviders, wpfHexViewHost, marginContainer, marginContainerName);
		}
	}
}
