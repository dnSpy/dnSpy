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
using Microsoft.VisualStudio.Text.Formatting;
using Microsoft.VisualStudio.Utilities;

namespace dnSpy.Text.Formatting {
	interface ILineTransformProvider : ILineTransformSource {
	}

	interface ILineTransformProviderService {
		ILineTransformProvider Create(IWpfTextView textView, bool removeExtraTextLineVerticalPixels);
	}

	[Export(typeof(ILineTransformProviderService))]
	sealed class LineTransformProviderService : ILineTransformProviderService {
		readonly IContentTypeRegistryService contentTypeRegistryService;
		readonly Lazy<ILineTransformSourceProvider, IContentTypeAndTextViewRoleMetadata>[] lineTransformSourceProviders;
		ProviderSelector<ILineTransformSourceProvider, IContentTypeAndTextViewRoleMetadata>? providerSelector;

		[ImportingConstructor]
		LineTransformProviderService(IContentTypeRegistryService contentTypeRegistryService, [ImportMany] IEnumerable<Lazy<ILineTransformSourceProvider, IContentTypeAndTextViewRoleMetadata>> lineTransformSourceProviders) {
			this.contentTypeRegistryService = contentTypeRegistryService;
			this.lineTransformSourceProviders = lineTransformSourceProviders.ToArray();
		}

		public ILineTransformProvider Create(IWpfTextView textView, bool removeExtraTextLineVerticalPixels) {
			if (providerSelector is null)
				providerSelector = new ProviderSelector<ILineTransformSourceProvider, IContentTypeAndTextViewRoleMetadata>(contentTypeRegistryService, lineTransformSourceProviders);
			var contentType = textView.TextDataModel.ContentType;
			var list = new List<ILineTransformSource>();
			foreach (var p in providerSelector.GetProviders(contentType)) {
				if (!textView.Roles.ContainsAny(p.Metadata.TextViewRoles))
					continue;
				var source = p.Value.Create(textView);
				if (source is not null)
					list.Add(source);
			}
			return new LineTransformProvider(list.ToArray(), removeExtraTextLineVerticalPixels);
		}

		sealed class LineTransformProvider : ILineTransformProvider {
			readonly ILineTransformSource[] lineTransformSources;
			readonly bool removeExtraTextLineVerticalPixels;

			public LineTransformProvider(ILineTransformSource[] lineTransformSources, bool removeExtraTextLineVerticalPixels) {
				this.lineTransformSources = lineTransformSources ?? throw new ArgumentNullException(nameof(lineTransformSources));
				this.removeExtraTextLineVerticalPixels = removeExtraTextLineVerticalPixels;
			}

			public LineTransform GetLineTransform(ITextViewLine line, double yPosition, ViewRelativePosition placement) {
				var transform = removeExtraTextLineVerticalPixels ?
					new LineTransform(0, 0, line.DefaultLineTransform.VerticalScale, line.DefaultLineTransform.Right) :
					line.DefaultLineTransform;
				foreach (var source in lineTransformSources)
					transform = LineTransform.Combine(transform, source.GetLineTransform(line, yPosition, placement));
				return transform;
			}
		}
	}
}
