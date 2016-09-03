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
using System.Linq;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Language.Intellisense;
using dnSpy.Text.MEF;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace dnSpy.Language.Intellisense {
	[Export(typeof(ICompletionService))]
	sealed class CompletionService : ICompletionService, ICompletionPresenterService {
		readonly IImageManager imageManager;
		readonly Lazy<ICompletionSourceProvider, IOrderableContentTypeMetadata>[] completionSourceProviders;

		[ImportingConstructor]
		CompletionService(IImageManager imageManager, [ImportMany] IEnumerable<Lazy<ICompletionSourceProvider, IOrderableContentTypeMetadata>> completionSourceProviders) {
			this.imageManager = imageManager;
			this.completionSourceProviders = Orderer.Order(completionSourceProviders).ToArray();
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
			return new CompletionSession(textView, triggerPoint, trackCaret, this, completionSourceProviders);
		}

		ICompletionPresenter ICompletionPresenterService.Create(ICompletionSession completionSession) {
			if (completionSession == null)
				throw new ArgumentNullException(nameof(completionSession));
			return new CompletionPresenter(imageManager, completionSession);
		}
	}
}
