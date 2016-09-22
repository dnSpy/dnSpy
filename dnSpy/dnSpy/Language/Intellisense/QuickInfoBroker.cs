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
using dnSpy.Contracts.Language.Intellisense;
using dnSpy.Text.MEF;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace dnSpy.Language.Intellisense {
	[Export(typeof(IQuickInfoBroker))]
	sealed class QuickInfoBroker : IQuickInfoBroker {
		readonly Lazy<IIntellisenseSessionStackMapService> intellisenseSessionStackMapService;
		readonly Lazy<IIntellisensePresenterFactoryService> intellisensePresenterFactoryService;
		readonly Lazy<IQuickInfoSourceProvider, IOrderableContentTypeMetadata>[] quickInfoSourceProviders;

		[ImportingConstructor]
		QuickInfoBroker(Lazy<IIntellisenseSessionStackMapService> intellisenseSessionStackMapService, Lazy<IIntellisensePresenterFactoryService> intellisensePresenterFactoryService, [ImportMany] IEnumerable<Lazy<IQuickInfoSourceProvider, IOrderableContentTypeMetadata>> quickInfoSourceProviders) {
			this.intellisenseSessionStackMapService = intellisenseSessionStackMapService;
			this.intellisensePresenterFactoryService = intellisensePresenterFactoryService;
			this.quickInfoSourceProviders = Orderer.Order(quickInfoSourceProviders).ToArray();
		}

		public IQuickInfoSession TriggerQuickInfo(ITextView textView) {
			if (textView == null)
				throw new ArgumentNullException(nameof(textView));
			var pos = textView.Caret.Position.BufferPosition;
			var triggerPoint = pos.Snapshot.CreateTrackingPoint(pos.Position, PointTrackingMode.Negative, TrackingFidelityMode.Forward);
			return TriggerQuickInfo(textView, triggerPoint, trackMouse: false);
		}

		public IQuickInfoSession TriggerQuickInfo(ITextView textView, ITrackingPoint triggerPoint, bool trackMouse) {
			if (textView == null)
				throw new ArgumentNullException(nameof(textView));
			if (triggerPoint == null)
				throw new ArgumentNullException(nameof(triggerPoint));
			var session = CreateQuickInfoSession(textView, triggerPoint, trackMouse);
			session.Start();
			return session.IsDismissed ? null : session;
		}

		public IQuickInfoSession CreateQuickInfoSession(ITextView textView, ITrackingPoint triggerPoint, bool trackMouse) {
			if (textView == null)
				throw new ArgumentNullException(nameof(textView));
			if (triggerPoint == null)
				throw new ArgumentNullException(nameof(triggerPoint));
			var stack = intellisenseSessionStackMapService.Value.GetStackForTextView(textView);
			var session = new QuickInfoSession(textView, triggerPoint, trackMouse, intellisensePresenterFactoryService.Value, quickInfoSourceProviders);
			stack.PushSession(session);
			return session;
		}

		public bool IsQuickInfoActive(ITextView textView) {
			if (textView == null)
				throw new ArgumentNullException(nameof(textView));
			return GetSessions(textView).Count != 0;
		}

		public ReadOnlyCollection<IQuickInfoSession> GetSessions(ITextView textView) {
			if (textView == null)
				throw new ArgumentNullException(nameof(textView));
			var stack = intellisenseSessionStackMapService.Value.GetStackForTextView(textView);
			return new ReadOnlyCollection<IQuickInfoSession>(stack.Sessions.OfType<IQuickInfoSession>().ToArray());
		}
	}
}
