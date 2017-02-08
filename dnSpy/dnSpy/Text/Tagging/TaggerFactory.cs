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
using System.Linq;
using dnSpy.Text.MEF;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace dnSpy.Text.Tagging {
	[Export(typeof(ITaggerFactory))]
	sealed class TaggerFactory : ITaggerFactory {
		readonly Lazy<ITaggerProvider, INamedTaggerMetadata>[] textBufferTaggerProviders;
		readonly Lazy<IViewTaggerProvider, IViewTaggerMetadata>[] textViewTaggerProviders;

		[ImportingConstructor]
		TaggerFactory([ImportMany] IEnumerable<Lazy<ITaggerProvider, INamedTaggerMetadata>> textBufferTaggerProviders, [ImportMany] IEnumerable<Lazy<IViewTaggerProvider, IViewTaggerMetadata>> textViewTaggerProviders) {
			this.textBufferTaggerProviders = textBufferTaggerProviders.ToArray();
			this.textViewTaggerProviders = textViewTaggerProviders.ToArray();
		}

		public IEnumerable<ITagger<T>> Create<T>(ITextView textView, ITextBuffer textBuffer, IContentType contentType) where T : ITag {
			foreach (var t in Create<T>(textBuffer, contentType))
				yield return t;

			var type = typeof(T);
			foreach (var info in textViewTaggerProviders) {
				if (info.Metadata.TextViewRoles != null && !textView.Roles.ContainsAny(info.Metadata.TextViewRoles))
					continue;
				if (CanCreateTagger(contentType, type, info.Metadata.ContentTypes, info.Metadata.TagTypes)) {
					var tagger = info.Value.CreateTagger<T>(textView, textBuffer);
					if (tagger != null)
						yield return tagger;
				}
			}
		}

		public IEnumerable<ITagger<T>> Create<T>(ITextBuffer textBuffer, IContentType contentType) where T : ITag {
			var type = typeof(T);
			foreach (var info in textBufferTaggerProviders) {
				if (CanCreateTagger(contentType, type, info.Metadata.ContentTypes, info.Metadata.TagTypes)) {
					var tagger = info.Value.CreateTagger<T>(textBuffer);
					if (tagger != null)
						yield return tagger;
				}
			}
		}

		static bool CanCreateTagger(IContentType contentType, Type type, IEnumerable<string> contentTypes, IEnumerable<Type> types) {
			foreach (var ct in contentTypes) {
				if (contentType.IsOfType(ct)) {
					foreach (var t in types) {
						if (type.IsAssignableFrom(t))
							return true;
					}
					return false;
				}
			}
			return false;
		}
	}
}
