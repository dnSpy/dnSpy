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
using System.Linq;
using dnSpy.Text.MEF;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace dnSpy.Text.Editor {
	interface IWpfTextViewMarginProviderCollectionProvider {
		IWpfTextViewMarginProviderCollection Create(IWpfTextViewHost wpfTextViewHost, IWpfTextViewMargin marginContainer, string marginContainerName);
	}

	[Export(typeof(IWpfTextViewMarginProviderCollectionProvider))]
	sealed class WpfTextViewMarginProviderCollectionProvider : IWpfTextViewMarginProviderCollectionProvider {
		readonly Lazy<IWpfTextViewMarginProvider, IWpfTextViewMarginMetadata>[] wpfTextViewMarginProviders;

		[ImportingConstructor]
		WpfTextViewMarginProviderCollectionProvider([ImportMany] IEnumerable<Lazy<IWpfTextViewMarginProvider, IWpfTextViewMarginMetadata>> wpfTextViewMarginProviders) => this.wpfTextViewMarginProviders = Orderer.Order(wpfTextViewMarginProviders).ToArray();

		public IWpfTextViewMarginProviderCollection Create(IWpfTextViewHost wpfTextViewHost, IWpfTextViewMargin marginContainer, string marginContainerName) {
			if (wpfTextViewHost is null)
				throw new ArgumentNullException(nameof(wpfTextViewHost));
			if (marginContainer is null)
				throw new ArgumentNullException(nameof(marginContainer));
			if (marginContainerName is null)
				throw new ArgumentNullException(nameof(marginContainerName));
			return new WpfTextViewMarginProviderCollection(wpfTextViewMarginProviders, wpfTextViewHost, marginContainer, marginContainerName);
		}
	}
}
