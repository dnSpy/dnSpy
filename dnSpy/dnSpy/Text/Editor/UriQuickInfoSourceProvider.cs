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
using dnSpy.Contracts.Language.Intellisense;
using dnSpy.Contracts.Text;
using dnSpy.Properties;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace dnSpy.Text.Editor {
	[Export(typeof(IQuickInfoSourceProvider))]
	[Name(PredefinedDnSpyQuickInfoSourceProviders.Uri)]
	[ContentType(ContentTypes.Text)]
	sealed class UriQuickInfoSourceProvider : IQuickInfoSourceProvider {
		readonly IViewTagAggregatorFactoryService viewTagAggregatorFactoryService;

		[ImportingConstructor]
		UriQuickInfoSourceProvider(IViewTagAggregatorFactoryService viewTagAggregatorFactoryService) {
			this.viewTagAggregatorFactoryService = viewTagAggregatorFactoryService;
		}

		public IQuickInfoSource TryCreateQuickInfoSource(ITextBuffer textBuffer) {
			var textView = UriWpfTextViewCreationListener.TryGetTextView(textBuffer);
			if (textView == null)
				return null;
			return new UriQuickInfoSource(textView, viewTagAggregatorFactoryService);
		}
	}

	sealed class UriQuickInfoSource : IQuickInfoSource {
		readonly IWpfTextView textView;
		readonly IViewTagAggregatorFactoryService viewTagAggregatorFactoryService;

		public UriQuickInfoSource(IWpfTextView textView, IViewTagAggregatorFactoryService viewTagAggregatorFactoryService) {
			if (textView == null)
				throw new ArgumentNullException(nameof(textView));
			if (viewTagAggregatorFactoryService == null)
				throw new ArgumentNullException(nameof(viewTagAggregatorFactoryService));
			this.textView = textView;
			this.viewTagAggregatorFactoryService = viewTagAggregatorFactoryService;
		}

		public void AugmentQuickInfoSession(IQuickInfoSession session, IList<object> quickInfoContent, out ITrackingSpan applicableToSpan) {
			applicableToSpan = null;

			var triggerPoint = session.GetTriggerPoint(textView.TextSnapshot);
			if (triggerPoint == null)
				return;
			var uriSpan = UriHelper.GetUri(viewTagAggregatorFactoryService, textView, triggerPoint.Value);
			if (uriSpan == null)
				return;

			applicableToSpan = uriSpan.Value.Snapshot.CreateTrackingSpan(uriSpan.Value.Span, SpanTrackingMode.EdgeInclusive);
			var uri = uriSpan.Value.GetText();
			quickInfoContent.Add(uri + "\r\n" + dnSpy_Resources.UriFollowLinkMessage);
		}

		public void Dispose() { }
	}
}
