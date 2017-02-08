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
using System.ComponentModel.Composition;
using dnSpy.Contracts.Hex;
using dnSpy.Contracts.Hex.Editor;
using dnSpy.Contracts.Hex.Tagging;
using CTC = dnSpy.Contracts.Text.Classification;
using VSTE = Microsoft.VisualStudio.Text.Editor;

namespace dnSpy.Hex.Editor.Search {
	[Export(typeof(HexViewTaggerProvider))]
	[VSTE.TextViewRole(PredefinedHexViewRoles.Interactive)]
	[HexTagType(typeof(HexMarkerTag))]
	sealed class HexMarkerTaggerProvider : HexViewTaggerProvider {
		readonly HexViewSearchServiceProvider hexViewSearchServiceProvider;

		[ImportingConstructor]
		HexMarkerTaggerProvider(HexViewSearchServiceProvider hexViewSearchServiceProvider) {
			this.hexViewSearchServiceProvider = hexViewSearchServiceProvider;
		}

		public override IHexTagger<T> CreateTagger<T>(HexView hexView, HexBuffer buffer) {
			var wpfHexView = hexView as WpfHexView;
			if (wpfHexView == null)
				return null;
			return wpfHexView.Properties.GetOrCreateSingletonProperty(typeof(HexMarkerTagger), () => new HexMarkerTagger(hexViewSearchServiceProvider, wpfHexView)) as HexTagger<T>;
		}
	}

	sealed class HexMarkerTagger : HexTagger<HexMarkerTag>, IHexMarkerListener {
		readonly HexViewSearchService hexViewSearchService;

		public HexMarkerTagger(HexViewSearchServiceProvider hexViewSearchServiceProvider, WpfHexView wpfHexView) {
			if (hexViewSearchServiceProvider == null)
				throw new ArgumentNullException(nameof(hexViewSearchServiceProvider));
			if (wpfHexView == null)
				throw new ArgumentNullException(nameof(wpfHexView));
			hexViewSearchService = hexViewSearchServiceProvider.Get(wpfHexView);
			hexViewSearchService.RegisterHexMarkerListener(this);
		}

		public override event EventHandler<HexBufferSpanEventArgs> TagsChanged;
		void IHexMarkerListener.RaiseTagsChanged(HexBufferSpan span) => TagsChanged?.Invoke(this, new HexBufferSpanEventArgs(span));

		public override IEnumerable<IHexTagSpan<HexMarkerTag>> GetTags(NormalizedHexBufferSpanCollection spans) {
			foreach (var span in hexViewSearchService.GetSpans(spans))
				yield return new HexTagSpan<HexMarkerTag>(span, HexSpanSelectionFlags.Selection, searchHexMarkerTag);
		}

		public override IEnumerable<IHexTextTagSpan<HexMarkerTag>> GetTags(HexTaggerContext context) {
			yield break;
		}

		static readonly HexMarkerTag searchHexMarkerTag = new HexMarkerTag(CTC.ThemeClassificationTypeNameKeys.HexFindMatchHighlightMarker);
	}
}
