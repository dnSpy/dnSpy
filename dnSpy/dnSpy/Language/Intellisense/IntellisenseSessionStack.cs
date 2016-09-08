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
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using dnSpy.Contracts.Language.Intellisense;
using Microsoft.VisualStudio.Text.Editor;

namespace dnSpy.Language.Intellisense {
	sealed partial class IntellisenseSessionStack : IIntellisenseSessionStack {
		public ReadOnlyObservableCollection<IIntellisenseSession> Sessions { get; }
		public IIntellisenseSession TopSession => sessions.Count == 0 ? null : sessions[0];

		readonly IWpfTextView wpfTextView;
		readonly ObservableCollection<IIntellisenseSession> sessions;
		readonly CommandTargetFilter commandTargetFilter;
		readonly List<SessionState> sessionStates;

		sealed class SessionState {
			public IIntellisenseSession Session { get; }
			public ISpaceReservationManager SpaceReservationManager { get; private set; }
			public ISpaceReservationAgent SpaceReservationAgent;
			public IPopupIntellisensePresenter PopupIntellisensePresenter { get; set; }
			public SessionState(IIntellisenseSession session) {
				Session = session;
			}
			public void SetSpaceReservationManager(ISpaceReservationManager manager) {
				if (manager == null)
					throw new ArgumentNullException(nameof(manager));
				if (SpaceReservationManager != null)
					throw new InvalidOperationException();
				SpaceReservationManager = manager;
			}
		}

		public IntellisenseSessionStack(IWpfTextView wpfTextView) {
			if (wpfTextView == null)
				throw new ArgumentNullException(nameof(wpfTextView));
			this.wpfTextView = wpfTextView;
			this.sessions = new ObservableCollection<IIntellisenseSession>();
			this.commandTargetFilter = new CommandTargetFilter(this);
			this.sessionStates = new List<SessionState>();
			Sessions = new ReadOnlyObservableCollection<IIntellisenseSession>(sessions);
			wpfTextView.Closed += WpfTextView_Closed;
		}

		bool ExecuteKeyboardCommand(IntellisenseKeyboardCommand command) {
			foreach (var session in sessions) {
				if (session.Presenter?.ExecuteKeyboardCommand(command) == true)
					return true;
			}
			return false;
		}

		public void PushSession(IIntellisenseSession session) {
			if (wpfTextView.IsClosed)
				throw new InvalidOperationException();
			if (session == null)
				throw new ArgumentNullException(nameof(session));
			if (sessions.Contains(session))
				throw new InvalidOperationException();
			if (sessions.Count == 0)
				commandTargetFilter.HookKeyboard();
			sessions.Insert(0, session);
			session.Dismissed += Session_Dismissed;
			session.PresenterChanged += Session_PresenterChanged;
			PresenterUpdated(session);
		}

		public IIntellisenseSession PopSession() {
			if (wpfTextView.IsClosed)
				throw new InvalidOperationException();
			if (sessions.Count == 0)
				return null;
			var session = sessions[0];
			RemoveSessionAt(0);
			return session;
		}

		public void MoveSessionToTop(IIntellisenseSession session) {
			if (wpfTextView.IsClosed)
				throw new InvalidOperationException();
			if (session == null)
				throw new ArgumentNullException(nameof(session));
			int index = sessions.IndexOf(session);
			if (index < 0)
				throw new InvalidOperationException();
			if (index == 0)
				return;
			sessions.Move(index, 0);
		}

		public void CollapseAllSessions() {
			if (wpfTextView.IsClosed)
				throw new InvalidOperationException();
			CollapseAllSessionsCore();
		}

		void CollapseAllSessionsCore() {
			var allSessions = sessions.ToArray();
			for (int i = allSessions.Length - 1; i >= 0; i--)
				allSessions[i].Collapse();
		}

		void Session_PresenterChanged(object sender, EventArgs e) {
			if (wpfTextView.IsClosed)
				return;
			PresenterUpdated((IIntellisenseSession)sender);
		}

		int GetSessionStateIndex(IIntellisenseSession session) {
			for (int i = 0; i < sessionStates.Count; i++) {
				if (sessionStates[i].Session == session)
					return i;
			}
			return -1;
		}

		SessionState GetSessionState(IIntellisenseSession session) {
			int index = GetSessionStateIndex(session);
			if (index >= 0)
				return sessionStates[index];

			var sessionState = new SessionState(session);
			sessionStates.Add(sessionState);
			return sessionState;
		}

		SessionState TryGetSessionState(ISpaceReservationAgent agent) {
			foreach (var sessionState in sessionStates) {
				if (sessionState.SpaceReservationAgent == agent)
					return sessionState;
			}
			return null;
		}

		SessionState TryGetSessionState(IPopupIntellisensePresenter popupPresenter) {
			foreach (var sessionState in sessionStates) {
				if (sessionState.PopupIntellisensePresenter == popupPresenter)
					return sessionState;
			}
			return null;
		}

		void PresenterUpdated(IIntellisenseSession session) {
			var sessionState = GetSessionState(session);
			if (sessionState.SpaceReservationAgent != null)
				sessionState.SpaceReservationManager.RemoveAgent(sessionState.SpaceReservationAgent);
			Debug.Assert(sessionState.SpaceReservationAgent == null);

			var presenter = session.Presenter;
			var popupPresenter = presenter as IPopupIntellisensePresenter;
			if (popupPresenter != null) {
				if (sessionState.SpaceReservationManager == null) {
					sessionState.SetSpaceReservationManager(wpfTextView.GetSpaceReservationManager(popupPresenter.SpaceReservationManagerName));
					sessionState.SpaceReservationManager.AgentChanged += SpaceReservationManager_AgentChanged;
				}
				if (sessionState.PopupIntellisensePresenter != null)
					sessionState.PopupIntellisensePresenter.PropertyChanged -= PopupIntellisensePresenter_PropertyChanged;
				sessionState.PopupIntellisensePresenter = popupPresenter;
				sessionState.PopupIntellisensePresenter.PropertyChanged += PopupIntellisensePresenter_PropertyChanged;

				var presentationSpan = popupPresenter.PresentationSpan;
				var surfaceElement = popupPresenter.SurfaceElement;
				if (presentationSpan != null && surfaceElement != null) {
					sessionState.SpaceReservationAgent = sessionState.SpaceReservationManager.CreatePopupAgent(presentationSpan, popupPresenter.PopupStyles, surfaceElement);
					sessionState.SpaceReservationManager.AddAgent(sessionState.SpaceReservationAgent);
				}
			}
			else {
				var customPresenter = presenter as ICustomIntellisensePresenter;
				if (customPresenter != null)
					customPresenter.Render();
				else
					Debug.Assert(presenter == null, $"Unsupported presenter: {presenter?.GetType()}");
			}
		}

		void PopupIntellisensePresenter_PropertyChanged(object sender, PropertyChangedEventArgs e) {
			if (wpfTextView.IsClosed)
				return;
			var popupPresenter = (IPopupIntellisensePresenter)sender;
			var sessionState = TryGetSessionState(popupPresenter);
			Debug.Assert(sessionState != null);
			if (sessionState == null)
				return;
			if (e.PropertyName == nameof(popupPresenter.PresentationSpan) || e.PropertyName == nameof(popupPresenter.PopupStyles)) {
				var presentationSpan = popupPresenter.PresentationSpan;
				if (presentationSpan == null || sessionState.SpaceReservationAgent == null)
					PresenterUpdated(popupPresenter.Session);
				else
					sessionState.SpaceReservationManager.UpdatePopupAgent(sessionState.SpaceReservationAgent, presentationSpan, popupPresenter.PopupStyles);
			}
			else if (e.PropertyName == nameof(popupPresenter.SurfaceElement))
				PresenterUpdated(popupPresenter.Session);
		}

		void SpaceReservationManager_AgentChanged(object sender, SpaceReservationAgentChangedEventArgs e) {
			if (wpfTextView.IsClosed)
				return;
			var sessionState = TryGetSessionState(e.OldAgent);
			if (sessionState != null) {
				// Its popup was hidden, so dismiss the session
				sessionState.Session.Dismiss();
			}
		}

		void Session_Dismissed(object sender, EventArgs e) {
			var session = sender as IIntellisenseSession;
			Debug.Assert(session != null);
			if (session == null)
				return;
			int index = sessions.IndexOf(session);
			Debug.Assert(index >= 0);
			if (index < 0)
				return;
			RemoveSessionAt(index);
		}

		void RemoveSessionAt(int index) {
			Debug.Assert(sessionStates.Count <= sessions.Count);
			var session = sessions[index];
			sessions.RemoveAt(index);
			session.Dismissed -= Session_Dismissed;
			session.PresenterChanged -= Session_PresenterChanged;
			var sessionState = GetSessionState(session);
			if (sessionState.SpaceReservationAgent != null)
				sessionState.SpaceReservationManager.RemoveAgent(sessionState.SpaceReservationAgent);
			if (sessionState.SpaceReservationManager != null)
				sessionState.SpaceReservationManager.AgentChanged -= SpaceReservationManager_AgentChanged;
			if (sessionState.PopupIntellisensePresenter != null)
				sessionState.PopupIntellisensePresenter.PropertyChanged -= PopupIntellisensePresenter_PropertyChanged;
			sessionStates.Remove(sessionState);
			if (sessions.Count == 0) {
				Debug.Assert(sessionStates.Count == 0);
				commandTargetFilter.UnhookKeyboard();
			}
		}

		void WpfTextView_Closed(object sender, EventArgs e) {
			CollapseAllSessionsCore();
			while (sessions.Count > 0)
				RemoveSessionAt(sessions.Count - 1);
			commandTargetFilter.Destroy();
			wpfTextView.Closed -= WpfTextView_Closed;
		}
	}
}
