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
using dnSpy.Contracts.Text;
using dnSpy.Contracts.Text.Editor;
using dnSpy.Contracts.Text.Editor.Classification;

namespace dnSpy.Text.Editor.Classification {
	abstract class ClassifierAggregatorBase : IClassifier, IDisposable {
		readonly ITextBuffer textBuffer;

		public event EventHandler<ClassificationChangedEventArgs> ClassificationChanged;

		protected ClassifierAggregatorBase(ITextBuffer textBuffer) {
			if (textBuffer == null)
				throw new ArgumentNullException(nameof(textBuffer));
			this.textBuffer = textBuffer;
		}

		protected ClassifierAggregatorBase(ITextView textView) {
			if (textView == null)
				throw new ArgumentNullException(nameof(textView));
			this.textBuffer = textView.TextBuffer;
		}

		public IList<ClassificationSpan> GetClassificationSpans(SnapshotSpan span) {
			return Array.Empty<ClassificationSpan>();//TODO:
		}

		public void Dispose() {
			//TODO:
		}
	}
}
