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
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using dnSpy.Contracts.Language.Intellisense;
using dnSpy.Contracts.Language.Intellisense.Classification;
using dnSpy.Text.Formatting;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Formatting;

namespace dnSpy.Language.Intellisense {
	sealed class CompletionTextElementProvider : ICompletionTextElementProvider {
		readonly ICompletionClassifierAggregatorService completionClassifierAggregatorService;
		readonly IClassificationFormatMap classificationFormatMap;
		readonly Dictionary<CompletionCollection, ICompletionClassifier> toClassifier;

		public CompletionTextElementProvider(ICompletionClassifierAggregatorService completionClassifierAggregatorService, IClassificationFormatMap classificationFormatMap) {
			if (completionClassifierAggregatorService == null)
				throw new ArgumentNullException(nameof(completionClassifierAggregatorService));
			if (classificationFormatMap == null)
				throw new ArgumentNullException(nameof(classificationFormatMap));
			this.completionClassifierAggregatorService = completionClassifierAggregatorService;
			this.classificationFormatMap = classificationFormatMap;
			this.toClassifier = new Dictionary<CompletionCollection, ICompletionClassifier>();
		}

		ICompletionClassifier GetCompletionClassifier(CompletionCollection collection) {
			ICompletionClassifier completionClassifier;
			if (!toClassifier.TryGetValue(collection, out completionClassifier))
				toClassifier.Add(collection, completionClassifier = completionClassifierAggregatorService.Create(collection));
			return completionClassifier;
		}

		public FrameworkElement Create(CompletionCollection collection, Completion completion) {
			if (collection == null)
				throw new ArgumentNullException(nameof(collection));
			if (completion == null)
				throw new ArgumentNullException(nameof(completion));
			Debug.Assert(collection.FilteredCollection.Contains(completion));

			var classifier = GetCompletionClassifier(collection);
			var inputText = collection.ApplicableTo.GetText(collection.ApplicableTo.TextBuffer.CurrentSnapshot);
			var context = new CompletionClassifierContext(completion, inputText);
			var text = completion.DisplayText;
			var textBlock = new TextBlock();
			int textOffset = 0;
			foreach (var tag in classifier.GetTags(context)) {
				if (textOffset < tag.Span.Start)
					textBlock.Inlines.Add(CreateRun(text.Substring(textOffset, tag.Span.Start - textOffset), null));
				textBlock.Inlines.Add(CreateRun(text.Substring(tag.Span.Start, tag.Span.Length), tag.ClassificationType));
				textOffset = tag.Span.End;
			}
			if (textOffset < text.Length)
				textBlock.Inlines.Add(CreateRun(text.Substring(textOffset), null));

			var defProps = classificationFormatMap.DefaultTextProperties;
			if (!defProps.BackgroundBrushEmpty)
				textBlock.Background = defProps.BackgroundBrush;
			if (!defProps.ForegroundBrushEmpty)
				textBlock.Foreground = defProps.ForegroundBrush;
			if (!defProps.BoldEmpty)
				textBlock.FontWeight = defProps.Bold ? FontWeights.Bold : FontWeights.Normal;
			if (!defProps.ItalicEmpty)
				textBlock.FontStyle = defProps.Italic ? FontStyles.Italic : FontStyles.Normal;
			if (!defProps.FontRenderingEmSizeEmpty)
				textBlock.FontSize = defProps.FontRenderingEmSize;
			if (!defProps.TextDecorationsEmpty)
				textBlock.TextDecorations = defProps.TextDecorations;
			if (!defProps.TextEffectsEmpty)
				textBlock.TextEffects = defProps.TextEffects;

			return textBlock;
		}

		Run CreateRun(string text, IClassificationType classificationType) {
			var run = new Run(text);

			if (classificationType == null)
				return run;

			var properties = classificationFormatMap.GetTextProperties(classificationType);

			if (!properties.BackgroundBrushEmpty)
				run.Background = properties.BackgroundBrush;
			if (!properties.ForegroundBrushEmpty)
				run.Foreground = properties.ForegroundBrush;
			if (!properties.BoldEmpty)
				run.FontWeight = properties.Bold ? FontWeights.Bold : FontWeights.Normal;
			if (!properties.ItalicEmpty)
				run.FontStyle = properties.Italic ? FontStyles.Italic : FontStyles.Normal;
			if (!properties.FontRenderingEmSizeEmpty)
				run.FontSize = properties.FontRenderingEmSize;
			if (!properties.TextDecorationsEmpty)
				run.TextDecorations = properties.TextDecorations;
			if (!properties.TextEffectsEmpty)
				run.TextEffects = properties.TextEffects;
			if (!properties.TypefaceEmpty && !IsSameTypeFace(classificationFormatMap.DefaultTextProperties, properties))
				run.FontFamily = properties.Typeface.FontFamily;

			return run;
		}

		static bool IsSameTypeFace(TextFormattingRunProperties a, TextFormattingRunProperties b) {
			if (a.TypefaceEmpty != b.TypefaceEmpty)
				return false;
			if (a.Typeface == b.Typeface)
				return true;
			return a.GetFontName() == a.GetFontName();
		}

		public void Dispose() {
			foreach (var classifier in toClassifier.Values)
				(classifier as IDisposable)?.Dispose();
			toClassifier.Clear();
		}
	}
}
