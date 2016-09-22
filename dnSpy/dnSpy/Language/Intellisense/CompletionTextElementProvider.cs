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
using System.Linq;
using System.Windows;
using dnSpy.Contracts.Language.Intellisense;
using dnSpy.Contracts.Language.Intellisense.Classification;
using dnSpy.Contracts.Text.Classification;
using Microsoft.VisualStudio.Text.Classification;

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
			Debug.Assert(collection.Completions.Contains(completion));

			var classifier = GetCompletionClassifier(collection);
			var inputText = collection.ApplicableTo.GetText(collection.ApplicableTo.TextBuffer.CurrentSnapshot);
			var context = new CompletionClassifierContext(collection, completion, completion.DisplayText, inputText);
			return TextBlockFactory.Create(context.DisplayText, classificationFormatMap.DefaultTextProperties,
				classifier.GetTags(context).Select(a => new TextRunPropertiesAndSpan(a.Span, classificationFormatMap.GetTextProperties(a.ClassificationType))), TextBlockFactory.Flags.DisableSetTextBlockFontFamily | TextBlockFactory.Flags.DisableFontSize);
		}

		public void Dispose() {
			foreach (var classifier in toClassifier.Values)
				(classifier as IDisposable)?.Dispose();
			toClassifier.Clear();
		}
	}
}
