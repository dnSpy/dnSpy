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

using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using dnSpy.Contracts.Text;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace dnSpy.Text.Tagging.Xml {
	[Export(typeof(ITaggerProvider))]
	[TagType(typeof(IClassificationTag))]
	[ContentType(ContentTypes.Xaml)]
	sealed class XamlTaggerProvider : ITaggerProvider {
		readonly XamlTaggerClassificationTypes xamlTaggerClassificationTypes;

		[ImportingConstructor]
		XamlTaggerProvider(XamlTaggerClassificationTypes xamlTaggerClassificationTypes) {
			this.xamlTaggerClassificationTypes = xamlTaggerClassificationTypes;
		}

		public ITagger<T> CreateTagger<T>(ITextBuffer buffer) where T : ITag => new XamlTagger(xamlTaggerClassificationTypes) as ITagger<T>;
	}

	sealed class XamlTagger : XmlTaggerBase {
		readonly XamlTaggerClassificationTypes xamlTaggerClassificationTypes;
		readonly XamlAttributeValueClassifier xamlAttributeValueClassifier;

		public XamlTagger(XamlTaggerClassificationTypes xamlTaggerClassificationTypes)
			: base(xamlTaggerClassificationTypes) {
			this.xamlTaggerClassificationTypes = xamlTaggerClassificationTypes;
			this.xamlAttributeValueClassifier = new XamlAttributeValueClassifier();
		}

		protected override IEnumerable<ITagSpan<IClassificationTag>> GetTags(SnapshotSpan span, ClassificationTag tag) {
			if (tag != xamlTaggerClassificationTypes.AttributeValue)
				return base.GetTags(span, tag);
			if (!xamlAttributeValueClassifier.Initialize(span))
				return base.GetTags(span, tag);
			return GetXamlAttributeValueTags();
		}

		IEnumerable<ITagSpan<IClassificationTag>> GetXamlAttributeValueTags() {
			foreach (var info in xamlAttributeValueClassifier.GetTags()) {
				var tag = GetClassificationTag(info.Kind);
				if (tag == null)
					continue;
				yield return new TagSpan<IClassificationTag>(info.Span, tag);
			}
		}

		ClassificationTag GetClassificationTag(XamlKind kind) {
			switch (kind) {
			case XamlKind.Delimiter:
				return xamlTaggerClassificationTypes.Delimiter;

			case XamlKind.Class:
				return xamlTaggerClassificationTypes.MarkupExtensionClass;

			case XamlKind.ParameterName:
				return xamlTaggerClassificationTypes.MarkupExtensionParameterName;

			case XamlKind.ParameterValue:
				return xamlTaggerClassificationTypes.MarkupExtensionParameterValue;

			default:
				Debug.Fail($"Unknown kind: {kind}");
				return null;
			}
		}
	}
}
