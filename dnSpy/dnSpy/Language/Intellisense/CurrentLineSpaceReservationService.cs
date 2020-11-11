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

// Prevents the current edited line from being hidden by any space reservation agents (popups)

using System;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using dnSpy.Contracts.Text.Editor;
using dnSpy.Text.Editor;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text.Editor;

namespace dnSpy.Language.Intellisense {
	interface ICurrentLineSpaceReservationService {
		void SessionCreated(IIntellisenseSession session);
	}

	[Export(typeof(ICurrentLineSpaceReservationService))]
	sealed class CurrentLineSpaceReservationService : ICurrentLineSpaceReservationService {
		public void SessionCreated(IIntellisenseSession session) {
			if (session is null)
				throw new ArgumentNullException(nameof(session));
			if (!CurrentLineSpaceReservationAgent.IsSupportedSession(session))
				return;
			var wpfTextView = session.TextView as IWpfTextView;
			Debug2.Assert(wpfTextView is not null);
			if (wpfTextView is null)
				return;
			var currentLineAgent = session.TextView.Properties.GetOrCreateSingletonProperty(typeof(CurrentLineSpaceReservationAgent), () => new CurrentLineSpaceReservationAgent(wpfTextView));
			currentLineAgent.SessionCreated(session);
		}
	}

	sealed class CurrentLineSpaceReservationAgent : ISpaceReservationAgent {
		bool ISpaceReservationAgent.HasFocus => false;
		bool ISpaceReservationAgent.IsMouseOver => false;
		event EventHandler? ISpaceReservationAgent.GotFocus { add { } remove { } }
		event EventHandler? ISpaceReservationAgent.LostFocus { add { } remove { } }
		readonly IWpfTextView wpfTextView;
		ISpaceReservationManager? spaceReservationManager;

		int ActiveSessions {
			get => activeSessions;
			set {
				if (wpfTextView.IsClosed)
					return;
				if (value < 0)
					throw new InvalidOperationException();
				if (activeSessions == value)
					return;
				var oldValue = activeSessions;
				activeSessions = value;
				if ((oldValue != 0) == (activeSessions != 0))
					return;
				if (activeSessions == 0) {
					Debug2.Assert(spaceReservationManager is not null);
					wpfTextView.Caret.PositionChanged -= Caret_PositionChanged;
					spaceReservationManager.RemoveAgent(this);
				}
				else {
					if (spaceReservationManager is null)
						spaceReservationManager = wpfTextView.GetSpaceReservationManager(PredefinedSpaceReservationManagerNames.CurrentLine);
					wpfTextView.Caret.PositionChanged += Caret_PositionChanged;
					spaceReservationManager.AddAgent(this);
				}
			}
		}
		int activeSessions;

		public CurrentLineSpaceReservationAgent(IWpfTextView wpfTextView) {
			this.wpfTextView = wpfTextView ?? throw new ArgumentNullException(nameof(wpfTextView));
			wpfTextView.Closed += WpfTextView_Closed;
		}

		void ISpaceReservationAgent.Hide() => wpfTextView.Caret.PositionChanged -= Caret_PositionChanged;
		void Caret_PositionChanged(object? sender, CaretPositionChangedEventArgs e) => wpfTextView.QueueSpaceReservationStackRefresh();

		Rect WpfTextViewRectToScreenRect(Rect wpfTextViewRect) {
			wpfTextViewRect.X -= wpfTextView.ViewportLeft;
			wpfTextViewRect.Y -= wpfTextView.ViewportTop;
			return ToScreenRect(wpfTextViewRect);
		}

		Rect ToScreenRect(Rect wpfRect) => new Rect(ToScreenPoint(wpfRect.TopLeft), ToScreenPoint(wpfRect.BottomRight));
		Point ToScreenPoint(Point point) => wpfTextView.VisualElement.PointToScreen(point);

		Geometry? ISpaceReservationAgent.PositionAndDisplay(Geometry reservedSpace) {
			if (wpfTextView.IsClosed)
				return null;

			var line = wpfTextView.Caret.ContainingTextViewLine;
			if (!line.IsVisible())
				return Geometry.Empty;

			var wpfLineRect = new Rect(wpfTextView.ViewportLeft, line.TextTop, wpfTextView.ViewportWidth, line.TextHeight);
			var screenLineRect = WpfTextViewRectToScreenRect(wpfLineRect);
			return new RectangleGeometry(screenLineRect);
		}

		public static bool IsSupportedSession(IIntellisenseSession session) => session is ICompletionSession || session is ISignatureHelpSession;

		public void SessionCreated(IIntellisenseSession session) {
			if (session is null)
				throw new ArgumentNullException(nameof(session));
			if (wpfTextView.IsClosed)
				return;
			Debug.Assert(!session.IsDismissed);
			if (session.IsDismissed)
				return;
			if (!IsSupportedSession(session))
				return;
			session.Dismissed += Session_Dismissed;
			ActiveSessions++;
		}

		void Session_Dismissed(object? sender, EventArgs e) {
			var session = (IIntellisenseSession)sender!;
			session.Dismissed -= Session_Dismissed;
			ActiveSessions--;
		}

		void WpfTextView_Closed(object? sender, EventArgs e) {
			wpfTextView.Closed -= WpfTextView_Closed;
			wpfTextView.Caret.PositionChanged -= Caret_PositionChanged;
		}
	}
}
