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
using dnSpy.Contracts.Language.Intellisense;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.Text.Classification;
using dnSpy.Roslyn.Text.Classification;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace dnSpy.Roslyn.Intellisense.SignatureHelp {
	abstract class SignatureHelpTaggerProvider : ITaggerProvider {
		readonly IThemeClassificationTypeService themeClassificationTypeService;

		protected SignatureHelpTaggerProvider(IThemeClassificationTypeService themeClassificationTypeService) => this.themeClassificationTypeService = themeClassificationTypeService ?? throw new ArgumentNullException(nameof(themeClassificationTypeService));

		public ITagger<T> CreateTagger<T>(ITextBuffer buffer) where T : ITag {
			var session = buffer.TryGetSignatureHelpSession();
			if (session == null)
				return null;
			return new SignatureHelpTagger(session, buffer, themeClassificationTypeService) as ITagger<T>;
		}
	}

	[Export(typeof(ITaggerProvider))]
	[TagType(typeof(IClassificationTag))]
	[ContentType(ContentTypes.CSharpRoslyn + SignatureHelpConstants.SignatureHelpContentTypeSuffix)]
	sealed class CSharpSignatureHelpTaggerProvider : SignatureHelpTaggerProvider {
		[ImportingConstructor]
		CSharpSignatureHelpTaggerProvider(IThemeClassificationTypeService themeClassificationTypeService)
			: base(themeClassificationTypeService) {
		}
	}

	[Export(typeof(ITaggerProvider))]
	[TagType(typeof(IClassificationTag))]
	[ContentType(ContentTypes.VisualBasicRoslyn + SignatureHelpConstants.SignatureHelpContentTypeSuffix)]
	sealed class VisualBasicSignatureHelpTaggerProvider : SignatureHelpTaggerProvider {
		[ImportingConstructor]
		VisualBasicSignatureHelpTaggerProvider(IThemeClassificationTypeService themeClassificationTypeService)
			: base(themeClassificationTypeService) {
		}
	}

	sealed class SignatureHelpTagger : ITagger<IClassificationTag> {
		public event EventHandler<SnapshotSpanEventArgs> TagsChanged { add { } remove { } }

		readonly ITextBuffer buffer;
		readonly ISignatureHelpSession session;
		readonly IThemeClassificationTypeService themeClassificationTypeService;

		public SignatureHelpTagger(ISignatureHelpSession session, ITextBuffer buffer, IThemeClassificationTypeService themeClassificationTypeService) {
			this.session = session ?? throw new ArgumentNullException(nameof(session));
			this.buffer = buffer ?? throw new ArgumentNullException(nameof(buffer));
			this.themeClassificationTypeService = themeClassificationTypeService ?? throw new ArgumentNullException(nameof(themeClassificationTypeService));
		}

		public IEnumerable<ITagSpan<IClassificationTag>> GetTags(NormalizedSnapshotSpanCollection spans) {
			var signature = session.SelectedSignature as Signature;
			if (signature == null)
				yield break;

			var usePrettyPrintedContent = buffer.GetUsePrettyPrintedContent();
			var snapshot = buffer.CurrentSnapshot;
			var snapshotLength = snapshot.Length;
			bool lenOk = usePrettyPrintedContent ? snapshotLength == signature.PrettyPrintedContent.Length : snapshotLength == signature.Content.Length;
			Debug.Assert(lenOk);
			if (!lenOk)
				yield break;

			int pos = 0;
			var taggedTextColl = usePrettyPrintedContent ? signature.PrettyPrintedContentTaggedText : signature.ContentTaggedText;
			foreach (var taggedText in taggedTextColl) {
				var span = new Span(pos, taggedText.Text.Length);
				Debug.Assert(span.End <= snapshotLength);
				if (span.End > snapshotLength)
					yield break;
				var color = TextTagsHelper.ToTextColor(taggedText.Tag);
				var tag = new ClassificationTag(themeClassificationTypeService.GetClassificationType(color));
				yield return new TagSpan<IClassificationTag>(new SnapshotSpan(snapshot, span), tag);
				pos = span.End;
			}
			Debug.Assert(usePrettyPrintedContent ? pos == signature.PrettyPrintedContent.Length : pos == signature.Content.Length);
		}
	}
}
