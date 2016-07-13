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

using System.ComponentModel.Composition;
using System.Windows;
using dnSpy.Contracts.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace dnSpy.Text.Editor {
	[Export(typeof(IWpfTextViewMarginProvider))]
	[MarginContainer(PredefinedMarginNames.RightControl)]
	[Name(PredefinedMarginNames.VerticalScrollBarContainer)]
	[ContentType(ContentTypes.TEXT)]
	[TextViewRole(PredefinedTextViewRoles.Interactive)]
	[GridCellLength(1.0), GridUnitType(GridUnitType.Star)]
	sealed class VerticalScrollBarContainerMarginProvider : IWpfTextViewMarginProvider {
		readonly IWpfTextViewMarginProviderCollectionCreator wpfTextViewMarginProviderCollectionCreator;

		[ImportingConstructor]
		VerticalScrollBarContainerMarginProvider(IWpfTextViewMarginProviderCollectionCreator wpfTextViewMarginProviderCollectionCreator) {
			this.wpfTextViewMarginProviderCollectionCreator = wpfTextViewMarginProviderCollectionCreator;
		}

		public IWpfTextViewMargin CreateMargin(IWpfTextViewHost wpfTextViewHost, IWpfTextViewMargin marginContainer) =>
			new WpfTextViewContainerMargin(wpfTextViewMarginProviderCollectionCreator, wpfTextViewHost, PredefinedMarginNames.VerticalScrollBarContainer, false);
	}
}
