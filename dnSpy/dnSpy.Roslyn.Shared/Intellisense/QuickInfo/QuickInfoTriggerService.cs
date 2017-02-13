/*
    Copyright (C) 2014-2017 de4dot@gmail.com

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
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace dnSpy.Roslyn.Shared.Intellisense.QuickInfo {
	interface IQuickInfoTriggerServiceProvider {
		IQuickInfoTriggerService Create(ITextView textView);
		void CloseOtherSessions(IQuickInfoSession session);
	}

	interface IQuickInfoTriggerService {
		bool TryTrigger(SnapshotPoint triggerPoint, bool trackMouse);
	}

	[Export(typeof(IQuickInfoTriggerServiceProvider))]
	sealed class QuickInfoTriggerServiceProvider : IQuickInfoTriggerServiceProvider {
		readonly IQuickInfoBroker quickInfoBroker;

		[ImportingConstructor]
		QuickInfoTriggerServiceProvider(IQuickInfoBroker quickInfoBroker) {
			this.quickInfoBroker = quickInfoBroker;
		}

		public IQuickInfoTriggerService Create(ITextView textView) {
			if (textView == null)
				throw new ArgumentNullException(nameof(textView));
			return textView.Properties.GetOrCreateSingletonProperty(typeof(QuickInfoTriggerService), () => new QuickInfoTriggerService(quickInfoBroker, textView));
		}

		public void CloseOtherSessions(IQuickInfoSession session) {
			foreach (var s in quickInfoBroker.GetSessions(session.TextView)) {
				if (s != session)
					s.Dismiss();
			}
		}
	}

	sealed class QuickInfoTriggerService : IQuickInfoTriggerService {
		readonly IQuickInfoBroker quickInfoBroker;
		readonly ITextView textView;
		QuickInfoSession currentQuickInfoSession;

		public QuickInfoTriggerService(IQuickInfoBroker quickInfoBroker, ITextView textView) {
			this.quickInfoBroker = quickInfoBroker ?? throw new ArgumentNullException(nameof(quickInfoBroker));
			this.textView = textView ?? throw new ArgumentNullException(nameof(textView));
		}

		public bool TryTrigger(SnapshotPoint point, bool trackMouse) {
			if (point.Snapshot == null)
				throw new ArgumentException();
			var info = QuickInfoState.Create(point.Snapshot);
			if (info == null)
				return false;

			currentQuickInfoSession?.Dispose();
			currentQuickInfoSession = new QuickInfoSession(info.Value, point, trackMouse, quickInfoBroker, textView);
			currentQuickInfoSession.Disposed += QuickInfoSession_Disposed;
			currentQuickInfoSession.Start();

			return true;
		}

		void QuickInfoSession_Disposed(object sender, EventArgs e) {
			var session = (QuickInfoSession)sender;
			session.Disposed -= QuickInfoSession_Disposed;
			if (session == currentQuickInfoSession)
				currentQuickInfoSession = null;
		}
	}
}
