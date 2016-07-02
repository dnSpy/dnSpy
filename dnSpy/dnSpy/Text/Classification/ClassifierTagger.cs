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
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Tagging;

namespace dnSpy.Text.Classification {
	sealed class ClassifierTagger : ITagger<ClassificationTag>, IDisposable {
		IClassifier[] classifiers;

		public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

		public ClassifierTagger(IClassifier[] classifiers) {
			if (classifiers == null)
				throw new ArgumentNullException(nameof(classifiers));
			this.classifiers = classifiers;
			foreach (var c in classifiers)
				c.ClassificationChanged += Classifier_ClassificationChanged;
		}

		public IEnumerable<ITagSpan<ClassificationTag>> GetTags(NormalizedSnapshotSpanCollection spans) {
			foreach (var classifier in classifiers) {
				foreach (var span in spans) {
					var cspans = classifier.GetClassificationSpans(span);
					foreach (var cspan in cspans)
						yield return new TagSpan<ClassificationTag>(cspan.Span, new ClassificationTag(cspan.ClassificationType));
				}
			}
		}

		void Classifier_ClassificationChanged(object sender, ClassificationChangedEventArgs e) =>
			TagsChanged?.Invoke(this, new SnapshotSpanEventArgs(e.ChangeSpan));

		public void Dispose() {
			foreach (var c in classifiers)
				c.ClassificationChanged -= Classifier_ClassificationChanged;
			classifiers = Array.Empty<IClassifier>();
		}
	}
}
