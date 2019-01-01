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
using System.Diagnostics;
using dnSpy.Contracts.Text;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;

namespace dnSpy.Text.Tagging.Xml {
	abstract class XmlTaggerBase : ITagger<IClassificationTag> {
		public event EventHandler<SnapshotSpanEventArgs> TagsChanged { add { } remove { } }

		readonly TaggerClassificationTypes taggerClassificationTypes;
		SpanDataCollection<ClassificationTag> classifications;
		int classificationsVersion;

		protected XmlTaggerBase(TaggerClassificationTypes taggerClassificationTypes) {
			if (taggerClassificationTypes == null)
				throw new ArgumentNullException(nameof(taggerClassificationTypes));
			Debug.Assert(taggerClassificationTypes.Attribute?.ClassificationType != null);
			Debug.Assert(taggerClassificationTypes.AttributeQuotes?.ClassificationType != null);
			Debug.Assert(taggerClassificationTypes.AttributeValue?.ClassificationType != null);
			Debug.Assert(taggerClassificationTypes.AttributeValueXaml?.ClassificationType != null);
			Debug.Assert(taggerClassificationTypes.CDataSection?.ClassificationType != null);
			Debug.Assert(taggerClassificationTypes.Comment?.ClassificationType != null);
			Debug.Assert(taggerClassificationTypes.Delimiter?.ClassificationType != null);
			Debug.Assert(taggerClassificationTypes.Keyword?.ClassificationType != null);
			Debug.Assert(taggerClassificationTypes.Name?.ClassificationType != null);
			Debug.Assert(taggerClassificationTypes.ProcessingInstruction?.ClassificationType != null);
			Debug.Assert(taggerClassificationTypes.Text?.ClassificationType != null);
			this.taggerClassificationTypes = taggerClassificationTypes;
		}

		public IEnumerable<ITagSpan<IClassificationTag>> GetTags(NormalizedSnapshotSpanCollection spans) {
			if (spans.Count == 0)
				yield break;

			var snapshot = spans[0].Snapshot;
			if (classifications == null || classificationsVersion != snapshot.Version.VersionNumber) {
				//TODO: Do this asynchronously
				classificationsVersion = snapshot.Version.VersionNumber;
				classifications = CreateClassifications(snapshot);
			}

			foreach (var span in spans) {
				int index = classifications.GetStartIndex(span.Span.Start);
				if (index < 0)
					continue;
				for (int i = index; i < classifications.Count; i++) {
					var spanData = classifications[i];
					if (spanData.Span.Start > span.Span.End)
						break;
					Debug.Assert(spanData.Span.End <= snapshot.Length);
					if (spanData.Span.End > snapshot.Length)
						break;
					foreach (var t in GetTags(new SnapshotSpan(snapshot, spanData.Span), spanData.Data))
						yield return t;
				}
			}
		}

		protected virtual IEnumerable<ITagSpan<IClassificationTag>> GetTags(SnapshotSpan span, ClassificationTag tag) {
			yield return new TagSpan<IClassificationTag>(span, tag);
		}

		SpanDataCollection<ClassificationTag> CreateClassifications(ITextSnapshot snapshot) {
			var builder = SpanDataCollectionBuilder<ClassificationTag>.CreateBuilder();

			var classifier = new XmlClassifier(snapshot);
			for (;;) {
				var info = classifier.GetNext();
				if (info == null)
					break;
				var classificationTag = GetClassificationTag(info.Value.Kind);
				if (classificationTag == null)
					continue;
				builder.Add(info.Value.Span, classificationTag);
			}

			return builder.Create();
		}

		ClassificationTag GetClassificationTag(XmlKind kind) {
			switch (kind) {
			case XmlKind.EntityReference:
				return taggerClassificationTypes.Attribute;

			case XmlKind.Text:
				return taggerClassificationTypes.Text;

			case XmlKind.TextWhitespace:
				return null;

			case XmlKind.Delimiter:
				return taggerClassificationTypes.Delimiter;

			case XmlKind.Comment:
				return taggerClassificationTypes.Comment;

			case XmlKind.ElementWhitespace:
				return null;

			case XmlKind.ElementName:
				return taggerClassificationTypes.Name;

			case XmlKind.AttributeName:
				return taggerClassificationTypes.Attribute;

			case XmlKind.AttributeQuote:
				return taggerClassificationTypes.AttributeQuotes;

			case XmlKind.AttributeValue:
				return taggerClassificationTypes.AttributeValue;

			case XmlKind.AttributeValueXaml:
				return taggerClassificationTypes.AttributeValueXaml;

			default:
				Debug.Fail($"Unknown kind: {kind}");
				return null;
			}
		}
	}
}
