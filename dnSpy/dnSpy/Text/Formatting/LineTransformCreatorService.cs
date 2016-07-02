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
using dnSpy.Text.MEF;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;
using Microsoft.VisualStudio.Utilities;

namespace dnSpy.Text.Formatting {
	interface ILineTransformCreator : ILineTransformSource {
	}

	interface ILineTransformCreatorService {
		ILineTransformCreator Create(IWpfTextView textView);
	}

	[Export(typeof(ILineTransformCreatorService))]
	sealed class LineTransformCreatorService : ILineTransformCreatorService {
		readonly IContentTypeRegistryService contentTypeRegistryService;
		readonly Lazy<ILineTransformSourceProvider, IContentTypeAndTextViewRoleMetadata>[] lineTransformSourceProviders;
		ProviderSelector<ILineTransformSourceProvider, IContentTypeAndTextViewRoleMetadata> providerSelector;

		[ImportingConstructor]
		LineTransformCreatorService(IContentTypeRegistryService contentTypeRegistryService, [ImportMany] IEnumerable<Lazy<ILineTransformSourceProvider, IContentTypeAndTextViewRoleMetadata>> lineTransformSourceProviders) {
			this.contentTypeRegistryService = contentTypeRegistryService;
			this.lineTransformSourceProviders = lineTransformSourceProviders.ToArray();
		}

		public ILineTransformCreator Create(IWpfTextView textView) {
			if (providerSelector == null)
				providerSelector = new ProviderSelector<ILineTransformSourceProvider, IContentTypeAndTextViewRoleMetadata>(contentTypeRegistryService, lineTransformSourceProviders, a => a.Metadata.ContentTypes);
			var contentType = textView.TextDataModel.ContentType;
			var list = new List<ILineTransformSource>();
			foreach (var p in providerSelector.GetProviders(contentType)) {
				if (!textView.Roles.ContainsAny(p.Metadata.TextViewRoles))
					continue;
				var source = p.Value.Create(textView);
				if (source != null)
					list.Add(source);
			}
			return new LineTransformCreator(list.ToArray());
		}

		sealed class LineTransformCreator : ILineTransformCreator {
			readonly ILineTransformSource[] lineTransformSources;

			public LineTransformCreator(ILineTransformSource[] lineTransformSources) {
				if (lineTransformSources == null)
					throw new ArgumentNullException(nameof(lineTransformSources));
				this.lineTransformSources = lineTransformSources;
			}

			public LineTransform GetLineTransform(ITextViewLine line, double yPosition, ViewRelativePosition placement) {
				var transform = line.DefaultLineTransform;
				foreach (var source in lineTransformSources)
					transform = LineTransform.Combine(transform, source.GetLineTransform(line, yPosition, placement));
				return transform;
			}
		}
	}
}
