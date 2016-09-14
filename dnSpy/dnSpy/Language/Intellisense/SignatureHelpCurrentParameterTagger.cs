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
using System.ComponentModel.Composition;
using System.Diagnostics;
using dnSpy.Contracts.Language.Intellisense;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.Text.Classification;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace dnSpy.Language.Intellisense {
	[Export(typeof(ITaggerProvider))]
	[TagType(typeof(IClassificationTag))]
	[ContentType(ContentTypes.SignatureHelp)]
	sealed class SignatureHelpCurrentParameterTaggerProvider : ITaggerProvider {
		readonly ClassificationTag currentParameterClassificationTag;

		[ImportingConstructor]
		SignatureHelpCurrentParameterTaggerProvider(IClassificationTypeRegistryService classificationTypeRegistryService) {
			currentParameterClassificationTag = new ClassificationTag(classificationTypeRegistryService.GetClassificationType(ThemeClassificationTypeNames.SignatureHelpCurrentParameter));
		}

		public ITagger<T> CreateTagger<T>(ITextBuffer buffer) where T : ITag {
			var session = buffer.TryGetSignatureHelpSession();
			if (session == null)
				return null;
			return new SignatureHelpCurrentParameterTagger(session, buffer, currentParameterClassificationTag) as ITagger<T>;
		}
	}

	sealed class SignatureHelpCurrentParameterTagger : ITagger<IClassificationTag> {
		public event EventHandler<SnapshotSpanEventArgs> TagsChanged { add { } remove { } }

		readonly ITextBuffer buffer;
		readonly ISignatureHelpSession session;
		readonly ClassificationTag currentParameterClassificationTag;

		public SignatureHelpCurrentParameterTagger(ISignatureHelpSession session, ITextBuffer buffer, ClassificationTag currentParameterClassificationTag) {
			if (session == null)
				throw new ArgumentNullException(nameof(session));
			if (buffer == null)
				throw new ArgumentNullException(nameof(buffer));
			if (currentParameterClassificationTag == null)
				throw new ArgumentNullException(nameof(currentParameterClassificationTag));
			this.session = session;
			this.buffer = buffer;
			this.currentParameterClassificationTag = currentParameterClassificationTag;
		}

		public IEnumerable<ITagSpan<IClassificationTag>> GetTags(NormalizedSnapshotSpanCollection spans) {
			if (session.IsDismissed)
				yield break;
			var parameter = session.SelectedSignature?.CurrentParameter;
			if (parameter == null)
				yield break;
			bool usePrettyPrintedContent = buffer.GetUsePrettyPrintedContent();
			var locus = usePrettyPrintedContent ? parameter.PrettyPrintedLocus : parameter.Locus;
			var snapshot = buffer.CurrentSnapshot;
			Debug.Assert(locus.End <= snapshot.Length);
			if (locus.End > snapshot.Length)
				yield break;

			yield return new TagSpan<IClassificationTag>(new SnapshotSpan(snapshot, locus), currentParameterClassificationTag);
		}
	}
}
