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
using System.Diagnostics;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.Text.Classification;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace dnSpy.Text {
	[Export(typeof(ITaggerProvider))]
	[TagType(typeof(IClassificationTag))]
	[ContentType(ContentTypes.Any)]
	sealed class CachedColorsListTaggerProvider : ITaggerProvider {
		readonly IThemeClassificationTypeService themeClassificationTypeService;
		readonly IClassificationTypeRegistryService classificationTypeRegistryService;

		[ImportingConstructor]
		CachedColorsListTaggerProvider(IThemeClassificationTypeService themeClassificationTypeService, IClassificationTypeRegistryService classificationTypeRegistryService) {
			this.themeClassificationTypeService = themeClassificationTypeService;
			this.classificationTypeRegistryService = classificationTypeRegistryService;
		}

		public ITagger<T> CreateTagger<T>(ITextBuffer buffer) where T : ITag {
			if (buffer.Properties.TryGetProperty(typeof(CachedColorsListTagger), out CachedColorsListTagger colorizer)) {
				colorizer.ThemeClassificationTypeService = themeClassificationTypeService;
				colorizer.ClassificationTypeRegistryService = classificationTypeRegistryService;
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
		IClassificationType textClassificationType;

		public IThemeClassificationTypeService ThemeClassificationTypeService { get; internal set; }
		public IClassificationTypeRegistryService ClassificationTypeRegistryService { get; internal set; }

		CachedColorsListTagger(CachedColorsList cachedColorsList) => this.cachedColorsList = cachedColorsList;

		public event EventHandler<SnapshotSpanEventArgs> TagsChanged {
			add { }
			remove { }
		}

		internal static CachedColorsListTagger Create(CachedColorsList cachedColorsList) =>
			new CachedColorsListTagger(cachedColorsList);

		public IEnumerable<ITagSpan<IClassificationTag>> GetTags(NormalizedSnapshotSpanCollection spans) {
			Debug.Assert(ThemeClassificationTypeService != null);
			Debug.Assert(ClassificationTypeRegistryService != null);
			if (ThemeClassificationTypeService == null || ClassificationTypeRegistryService == null)
				yield break;
			if (textClassificationType == null)
				textClassificationType = ThemeClassificationTypeService.GetClassificationType(TextColor.Text);

			var snapshot = spans[0].Snapshot;
			foreach (var span in spans) {
				var infoPart = cachedColorsList.Find(span.Span.Start);
				int offs = infoPart.DocOffsetToRelativeOffset(span.Span.Start);
				int end = infoPart.DocOffsetToRelativeOffset(span.Span.End);

				int index = infoPart.CachedColors.GetStartIndex(offs);
				if (index < 0)
					continue;
				int count = infoPart.CachedColors.Count;
				while (index < count) {
					var info = infoPart.CachedColors[index];
					if (info.Span.Start > end)
						break;

					var realSpan = new Span(span.Span.Start - offs + info.Span.Start, info.Span.Length);
					if (realSpan.End > snapshot.Length)
						break;

					var ct = ColorUtils.GetClassificationType(ClassificationTypeRegistryService, ThemeClassificationTypeService, info.Data);
					if (ct != textClassificationType)
						yield return new TagSpan<IClassificationTag>(new SnapshotSpan(snapshot, realSpan), new ClassificationTag(ct));
					index++;
				}
			}
		}
	}
}
