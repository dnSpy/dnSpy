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
using dnSpy.Contracts.Language.Intellisense.Classification;
using dnSpy.Contracts.Text.Classification;

namespace dnSpy.Language.Intellisense.Classification {
	sealed class CompletionClassifierAggregator : ICompletionClassifier, IDisposable {
		readonly ITextClassifier textClassifierAggregator;
		readonly ICompletionClassifier[] completionClassifiers;

		// Don't expose ITextClassifier on the main class
		sealed class TextClassifier : ITextClassifier {
			readonly CompletionClassifierAggregator owner;
			public TextClassifier(CompletionClassifierAggregator owner) { this.owner = owner; }
			public IEnumerable<TextClassificationTag> GetTags(TextClassifierContext context) => owner.GetTags(context);
		}

		public CompletionClassifierAggregator(ITextClassifierAggregatorService textClassifierAggregatorService, ICompletionClassifier[] completionClassifiers) {
			if (textClassifierAggregatorService == null)
				throw new ArgumentNullException(nameof(textClassifierAggregatorService));
			if (completionClassifiers == null)
				throw new ArgumentNullException(nameof(completionClassifiers));
			this.textClassifierAggregator = textClassifierAggregatorService.Create(new ITextClassifier[] { new TextClassifier(this) });
			this.completionClassifiers = completionClassifiers;
		}

		sealed class MyTextClassifierContext : TextClassifierContext {
			public CompletionClassifierContext CompletionClassifierContext { get; }

			public MyTextClassifierContext(string text, CompletionClassifierContext completionClassifierContext)
				: base(text) {
				CompletionClassifierContext = completionClassifierContext;
			}
		}

		public IEnumerable<CompletionClassificationTag> GetTags(CompletionClassifierContext context) {
			var textClassifierContext = new MyTextClassifierContext(context.DisplayText, context);
			foreach (var tag in textClassifierAggregator.GetTags(textClassifierContext))
				yield return new CompletionClassificationTag(tag.Span, tag.ClassificationType);
		}

		// Called indirectly by GetTags() above
		IEnumerable<TextClassificationTag> GetTags(TextClassifierContext context) {
			var myContext = context as MyTextClassifierContext;
			Debug.Assert(myContext != null);
			if (myContext == null)
				yield break;

			var realContext = myContext.CompletionClassifierContext;
			foreach (var classifier in completionClassifiers) {
				foreach (var tag in classifier.GetTags(realContext))
					yield return new TextClassificationTag(tag.Span, tag.ClassificationType);
			}
		}

		public void Dispose() {
			(textClassifierAggregator as IDisposable)?.Dispose();
			foreach (var classifier in completionClassifiers)
				(classifier as IDisposable)?.Dispose();
		}
	}
}
