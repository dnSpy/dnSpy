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
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace dnSpy.Language.Intellisense {
	[Export(typeof(ITaggerProvider))]
	[TagType(typeof(IClassificationTag))]
	[ContentType(ContentTypes.SignatureHelp)]
	sealed class SignatureHelpCurrentParameterTaggerProvider : ITaggerProvider {
		readonly ClassificationTag signatureHelpDocumentationClassificationTag;
		readonly ClassificationTag signatureHelpCurrentParameterClassificationTag;
		readonly ClassificationTag signatureHelpParameterClassificationTag;
		readonly ClassificationTag signatureHelpParameterDocumentationClassificationTag;

		[ImportingConstructor]
		SignatureHelpCurrentParameterTaggerProvider(IClassificationTypeRegistryService classificationTypeRegistryService) {
			signatureHelpDocumentationClassificationTag = new ClassificationTag(classificationTypeRegistryService.GetClassificationType(ThemeClassificationTypeNames.SignatureHelpDocumentation));
			signatureHelpCurrentParameterClassificationTag = new ClassificationTag(classificationTypeRegistryService.GetClassificationType(ThemeClassificationTypeNames.SignatureHelpCurrentParameter));
			signatureHelpParameterClassificationTag = new ClassificationTag(classificationTypeRegistryService.GetClassificationType(ThemeClassificationTypeNames.SignatureHelpParameter));
			signatureHelpParameterDocumentationClassificationTag = new ClassificationTag(classificationTypeRegistryService.GetClassificationType(ThemeClassificationTypeNames.SignatureHelpParameterDocumentation));
		}

		public ITagger<T>? CreateTagger<T>(ITextBuffer buffer) where T : ITag {
			var session = buffer.TryGetSignatureHelpSession();
			if (session is null)
				return null;
			if (buffer.ContentType.TypeName.EndsWith(SignatureHelpConstants.ExtendedSignatureHelpContentTypeSuffix))
				return new SignatureHelpCurrentParameterTaggerEx(buffer, signatureHelpDocumentationClassificationTag, signatureHelpParameterClassificationTag, signatureHelpParameterDocumentationClassificationTag) as ITagger<T>;
			return new SignatureHelpCurrentParameterTagger(session, buffer, signatureHelpCurrentParameterClassificationTag) as ITagger<T>;
		}
	}

	sealed class SignatureHelpCurrentParameterTagger : ITagger<IClassificationTag> {
		public event EventHandler<SnapshotSpanEventArgs>? TagsChanged { add { } remove { } }

		readonly ITextBuffer buffer;
		readonly ISignatureHelpSession session;
		readonly ClassificationTag signatureHelpCurrentParameterClassificationTag;

		public SignatureHelpCurrentParameterTagger(ISignatureHelpSession session, ITextBuffer buffer, ClassificationTag signatureHelpCurrentParameterClassificationTag) {
			this.session = session ?? throw new ArgumentNullException(nameof(session));
			this.buffer = buffer ?? throw new ArgumentNullException(nameof(buffer));
			this.signatureHelpCurrentParameterClassificationTag = signatureHelpCurrentParameterClassificationTag ?? throw new ArgumentNullException(nameof(signatureHelpCurrentParameterClassificationTag));
		}

		public IEnumerable<ITagSpan<IClassificationTag>> GetTags(NormalizedSnapshotSpanCollection spans) {
			if (session.IsDismissed)
				yield break;
			var parameter = session.SelectedSignature?.CurrentParameter;
			if (parameter is null)
				yield break;
			bool usePrettyPrintedContent = buffer.GetUsePrettyPrintedContent();
			var locus = usePrettyPrintedContent ? parameter.PrettyPrintedLocus : parameter.Locus;
			var snapshot = buffer.CurrentSnapshot;
			Debug.Assert(locus.End <= snapshot.Length);
			if (locus.End > snapshot.Length)
				yield break;

			yield return new TagSpan<IClassificationTag>(new SnapshotSpan(snapshot, locus), signatureHelpCurrentParameterClassificationTag);
		}
	}

	sealed class SignatureHelpCurrentParameterTaggerEx : ITagger<IClassificationTag> {
		public event EventHandler<SnapshotSpanEventArgs>? TagsChanged { add { } remove { } }

		readonly ITextBuffer buffer;
		readonly ClassificationTag signatureHelpDocumentationClassificationTag;
		readonly ClassificationTag signatureHelpParameterClassificationTag;
		readonly ClassificationTag signatureHelpParameterDocumentationClassificationTag;

		public SignatureHelpCurrentParameterTaggerEx(ITextBuffer buffer, ClassificationTag signatureHelpDocumentationClassificationTag, ClassificationTag signatureHelpParameterClassificationTag, ClassificationTag signatureHelpParameterDocumentationClassificationTag) {
			this.buffer = buffer ?? throw new ArgumentNullException(nameof(buffer));
			this.signatureHelpDocumentationClassificationTag = signatureHelpDocumentationClassificationTag ?? throw new ArgumentNullException(nameof(signatureHelpDocumentationClassificationTag));
			this.signatureHelpParameterClassificationTag = signatureHelpParameterClassificationTag ?? throw new ArgumentNullException(nameof(signatureHelpParameterClassificationTag));
			this.signatureHelpParameterDocumentationClassificationTag = signatureHelpParameterDocumentationClassificationTag ?? throw new ArgumentNullException(nameof(signatureHelpParameterDocumentationClassificationTag));
		}

		public IEnumerable<ITagSpan<IClassificationTag>> GetTags(NormalizedSnapshotSpanCollection spans) {
			var context = buffer.TryGetSignatureHelpClassifierContext();
			Debug2.Assert(context is not null);
			if (context is null || context.Session.IsDismissed)
				yield break;
			ClassificationTag tag;
			if (context.Type == SignatureHelpClassifierContextTypes.SignatureDocumentation)
				tag = signatureHelpDocumentationClassificationTag;
			else if (context.Type == SignatureHelpClassifierContextTypes.ParameterName)
				tag = signatureHelpParameterClassificationTag;
			else if (context.Type == SignatureHelpClassifierContextTypes.ParameterDocumentation)
				tag = signatureHelpParameterDocumentationClassificationTag;
			else {
				Debug.Fail($"Unknown sig help ctx type: {context.Type}");
				yield break;
			}
			yield return new TagSpan<IClassificationTag>(new SnapshotSpan(buffer.CurrentSnapshot, 0, buffer.CurrentSnapshot.Length), tag);
		}
	}
}
