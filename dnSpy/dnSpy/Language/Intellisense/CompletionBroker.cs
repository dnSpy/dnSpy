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
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Linq;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Language.Intellisense;
using dnSpy.Text.MEF;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace dnSpy.Language.Intellisense {
	[Export(typeof(ICompletionBroker))]
	sealed class CompletionBroker : ICompletionBroker, ICompletionPresenterService {
		readonly IImageManager imageManager;
		readonly Lazy<IIntellisenseSessionStackMapService> intellisenseSessionStackMapService;
		readonly Lazy<ICompletionTextElementProviderService> completionTextElementProviderService;
		readonly Lazy<ICompletionSourceProvider, IOrderableContentTypeMetadata>[] completionSourceProviders;
		readonly Lazy<IUIElementProvider<Completion, ICompletionSession>, IOrderableContentTypeMetadata>[] completionUIElementProviders;

		[ImportingConstructor]
		CompletionBroker(IImageManager imageManager, Lazy<IIntellisenseSessionStackMapService> intellisenseSessionStackMapService, Lazy<ICompletionTextElementProviderService> completionTextElementProviderService, [ImportMany] IEnumerable<Lazy<ICompletionSourceProvider, IOrderableContentTypeMetadata>> completionSourceProviders, [ImportMany] IEnumerable<Lazy<IUIElementProvider<Completion, ICompletionSession>, IOrderableContentTypeMetadata>> completionUIElementProviders) {
			this.imageManager = imageManager;
			this.intellisenseSessionStackMapService = intellisenseSessionStackMapService;
			this.completionTextElementProviderService = completionTextElementProviderService;
			this.completionSourceProviders = Orderer.Order(completionSourceProviders).ToArray();
			this.completionUIElementProviders = Orderer.Order(completionUIElementProviders).ToArray();
		}

		public ICompletionSession TriggerCompletion(ITextView textView) {
			if (textView == null)
				throw new ArgumentNullException(nameof(textView));
			var triggerPoint = textView.TextSnapshot.CreateTrackingPoint(textView.Caret.Position.BufferPosition.Position, PointTrackingMode.Negative, TrackingFidelityMode.Forward);
			return TriggerCompletion(textView, triggerPoint, trackCaret: true);
		}

		public ICompletionSession TriggerCompletion(ITextView textView, ITrackingPoint triggerPoint, bool trackCaret) {
			if (textView == null)
				throw new ArgumentNullException(nameof(textView));
			if (triggerPoint == null)
				throw new ArgumentNullException(nameof(triggerPoint));
			var session = CreateCompletionSession(textView, triggerPoint, trackCaret);
			session.Start();
			return session;
		}

		public ICompletionSession CreateCompletionSession(ITextView textView, ITrackingPoint triggerPoint, bool trackCaret) {
			if (textView == null)
				throw new ArgumentNullException(nameof(textView));
			if (triggerPoint == null)
				throw new ArgumentNullException(nameof(triggerPoint));
			var stack = intellisenseSessionStackMapService.Value.GetStackForTextView(textView);
			var session = new CompletionSession(textView, triggerPoint, trackCaret, this, completionSourceProviders);
			stack.PushSession(session);
			return session;
		}

		public void DismissAllSessions(ITextView textView) {
			if (textView == null)
				throw new ArgumentNullException(nameof(textView));
			foreach (var session in GetSessions(textView))
				session.Dismiss();
		}

		public bool IsCompletionActive(ITextView textView) {
			if (textView == null)
				throw new ArgumentNullException(nameof(textView));
			return GetSessions(textView).Count != 0;
		}

		public ReadOnlyCollection<ICompletionSession> GetSessions(ITextView textView) {
			if (textView == null)
				throw new ArgumentNullException(nameof(textView));
			var stack = intellisenseSessionStackMapService.Value.GetStackForTextView(textView);
			return new ReadOnlyCollection<ICompletionSession>(stack.Sessions.OfType<ICompletionSession>().ToArray());
		}

		IIntellisensePresenter ICompletionPresenterService.Create(ICompletionSession completionSession) {
			if (completionSession == null)
				throw new ArgumentNullException(nameof(completionSession));
			return new CompletionPresenter(imageManager, completionSession, completionTextElementProviderService.Value.Create(), completionUIElementProviders);
		}
	}
}
