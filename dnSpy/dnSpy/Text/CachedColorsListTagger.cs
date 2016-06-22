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
using System.Diagnostics;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.Text.Classification;
using dnSpy.Contracts.Text.Tagging;

namespace dnSpy.Text {
	[ExportTaggerProvider(typeof(IClassificationTag), ContentTypes.ANY)]
	sealed class CachedColorsListTaggerProvider : ITaggerProvider {
		readonly IThemeClassificationTypes themeClassificationTypes;

		[ImportingConstructor]
		CachedColorsListTaggerProvider(IThemeClassificationTypes themeClassificationTypes) {
			this.themeClassificationTypes = themeClassificationTypes;
		}

		public ITagger<T> CreateTagger<T>(ITextBuffer buffer) where T : ITag {
			CachedColorsListTagger colorizer;
			if (buffer.Properties.TryGetProperty(typeof(CachedColorsListTagger), out colorizer)) {
				colorizer.ThemeClassificationTypes = themeClassificationTypes;
				return colorizer as ITagger<T>;
			}
			return null;
		}

		public static void AddColorizer(ITextBuffer textBuffer, CachedColorsList cachedColorsList) {
			if (textBuffer == null)
				throw new ArgumentNullException(nameof(textBuffer));
			if (cachedColorsList == null)
				throw new ArgumentNullException(nameof(cachedColorsList));
			textBuffer.Properties.GetOrCreateSingletonProperty(typeof(CachedColorsListTagger), () => CachedColorsListTagger.Create(cachedColorsList));
		}
	}

	sealed class CachedColorsListTagger : ITagger<IClassificationTag> {
		readonly CachedColorsList cachedColorsList;

		public IThemeClassificationTypes ThemeClassificationTypes { get; internal set; }

		CachedColorsListTagger(CachedColorsList cachedColorsList) {
			this.cachedColorsList = cachedColorsList;
		}

		public event EventHandler<SnapshotSpanEventArgs> TagsChanged {
			add { }
			remove { }
		}

		internal static CachedColorsListTagger Create(CachedColorsList cachedColorsList) =>
			new CachedColorsListTagger(cachedColorsList);

		public IEnumerable<ITagSpan<IClassificationTag>> GetTags(NormalizedSnapshotSpanCollection spans) {
			Debug.Assert(ThemeClassificationTypes != null);
			if (ThemeClassificationTypes == null)
				yield break;

			var snapshot = spans[0].Snapshot;
			foreach (var span in spans) {
				int offs = span.Span.Start;
				int end = span.Span.End;

				//TODO: You should verify that the snapshot is correct before calling Find()
				var infoPart = cachedColorsList.Find(offs);
				while (offs < end) {
					int defaultTextLength, tokenLength;
					object color;
					if (!infoPart.FindByDocOffset(offs, out defaultTextLength, out color, out tokenLength))
						yield break;

					if (tokenLength != 0)
						yield return new TagSpan<IClassificationTag>(new SnapshotSpan(snapshot, new Span(offs + defaultTextLength, tokenLength)), new ClassificationTag(ThemeClassificationTypes.GetClassificationTypeByColorObject(color)));

					offs += defaultTextLength + tokenLength;
				}
			}
		}
	}
}
