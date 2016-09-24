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
using dnSpy.Roslyn.Shared.Text;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;

namespace dnSpy.Roslyn.Shared.Intellisense.QuickInfo {
	[Export(typeof(IQuickInfoSourceProvider))]
	[Name(PredefinedDnSpyQuickInfoSourceProviders.Roslyn)]
	[ContentType(ContentTypes.RoslynCode)]
	sealed class QuickInfoSourceProvider : IQuickInfoSourceProvider {
		readonly IQuickInfoContentCreatorProvider quickInfoContentCreatorProvider;
		readonly IQuickInfoTriggerServiceProvider quickInfoTriggerServiceProvider;

		[ImportingConstructor]
		QuickInfoSourceProvider(IQuickInfoContentCreatorProvider quickInfoContentCreatorProvider, IQuickInfoTriggerServiceProvider quickInfoTriggerServiceProvider) {
			this.quickInfoContentCreatorProvider = quickInfoContentCreatorProvider;
			this.quickInfoTriggerServiceProvider = quickInfoTriggerServiceProvider;
		}

		public IQuickInfoSource TryCreateQuickInfoSource(ITextBuffer textBuffer) => new QuickInfoSource(textBuffer, quickInfoContentCreatorProvider, quickInfoTriggerServiceProvider);
	}

	sealed class QuickInfoSource : IQuickInfoSource {
		readonly ITextBuffer textBuffer;
		readonly IQuickInfoContentCreatorProvider quickInfoContentCreatorProvider;
		readonly IQuickInfoTriggerServiceProvider quickInfoTriggerServiceProvider;

		public QuickInfoSource(ITextBuffer textBuffer, IQuickInfoContentCreatorProvider quickInfoContentCreatorProvider, IQuickInfoTriggerServiceProvider quickInfoTriggerServiceProvider) {
			if (textBuffer == null)
				throw new ArgumentNullException(nameof(textBuffer));
			if (quickInfoContentCreatorProvider == null)
				throw new ArgumentNullException(nameof(quickInfoContentCreatorProvider));
			if (quickInfoTriggerServiceProvider == null)
				throw new ArgumentNullException(nameof(quickInfoTriggerServiceProvider));
			this.textBuffer = textBuffer;
			this.quickInfoContentCreatorProvider = quickInfoContentCreatorProvider;
			this.quickInfoTriggerServiceProvider = quickInfoTriggerServiceProvider;
		}
		static readonly object hasTriggeredQuickInfoKey = new object();

		public void AugmentQuickInfoSession(IQuickInfoSession session, IList<object> quickInfoContent, out ITrackingSpan applicableToSpan) {
			applicableToSpan = null;

			var qiSession = QuickInfoSession.TryGetSession(session);
			if (qiSession == null) {
				// Mouse hovered over something and the default quick info controller created
				// a quick info session.

				if (session.Properties.ContainsProperty(hasTriggeredQuickInfoKey))
					return;
				session.Properties.AddProperty(hasTriggeredQuickInfoKey, null);

				var point = session.GetTriggerPoint(session.TextView.TextSnapshot);
				if (point != null)
					quickInfoTriggerServiceProvider.Create(session.TextView).TryTrigger(point.Value, session.TrackMouse);
				return;
			}

			// The item has been fetched async, now show it to the user
			// It's possible for another quick info session to already be active, eg. when
			// hovering over a url in a string, so close it.
			quickInfoTriggerServiceProvider.CloseOtherSessions(session);

			var item = qiSession.Item;
			Debug.Assert(item != null);
			if (item == null)
				return;
			var info = qiSession.State;

			Debug.Assert(item.TextSpan.End <= info.Snapshot.Length);
			if (item.TextSpan.End > info.Snapshot.Length)
				return;

			applicableToSpan = info.Snapshot.CreateTrackingSpan(item.TextSpan.ToSpan(), SpanTrackingMode.EdgeInclusive);
			foreach (var o in quickInfoContentCreatorProvider.Create().Create(item))
				quickInfoContent.Add(o);
		}

		public void Dispose() { }
	}
}
