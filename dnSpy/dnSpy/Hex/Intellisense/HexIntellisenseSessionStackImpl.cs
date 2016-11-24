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
using System.Diagnostics;
using System.Linq;
using System.Windows.Input;
using System.Windows.Threading;
using dnSpy.Contracts.Hex.Editor;
using dnSpy.Contracts.Hex.Intellisense;
using VSLI = Microsoft.VisualStudio.Language.Intellisense;
using VSTA = Microsoft.VisualStudio.Text.Adornments;

namespace dnSpy.Hex.Intellisense {
	sealed partial class HexIntellisenseSessionStackImpl : HexIntellisenseSessionStack {
		public override ReadOnlyObservableCollection<HexIntellisenseSession> Sessions { get; }
		public override HexIntellisenseSession TopSession => sessions.Count == 0 ? null : sessions[0];

		readonly WpfHexView wpfHexView;
		readonly ObservableCollection<HexIntellisenseSession> sessions;
		readonly CommandTargetFilter commandTargetFilter;
		readonly List<SessionState> sessionStates;
		readonly DispatcherTimer clearOpacityTimer;

		const double clearOpacityIntervalMilliSecs = 250;

		sealed class SessionState {
			public HexIntellisenseSession Session { get; }
			public HexSpaceReservationManager SpaceReservationManager { get; private set; }
			public HexSpaceReservationAgent SpaceReservationAgent;
			public IHexPopupIntellisensePresenter PopupIntellisensePresenter { get; set; }
			public SessionState(HexIntellisenseSession session) {
				Session = session;
			}
			public void SetSpaceReservationManager(HexSpaceReservationManager manager) {
				if (manager == null)
					throw new ArgumentNullException(nameof(manager));
				if (SpaceReservationManager != null)
					throw new InvalidOperationException();
				SpaceReservationManager = manager;
			}
		}

		public HexIntellisenseSessionStackImpl(WpfHexView wpfHexView) {
			if (wpfHexView == null)
				throw new ArgumentNullException(nameof(wpfHexView));
			this.wpfHexView = wpfHexView;
			this.sessions = new ObservableCollection<HexIntellisenseSession>();
			this.commandTargetFilter = new CommandTargetFilter(this);
			this.sessionStates = new List<SessionState>();
			this.clearOpacityTimer = new DispatcherTimer(DispatcherPriority.Background, wpfHexView.VisualElement.Dispatcher);
			clearOpacityTimer.Interval = TimeSpan.FromMilliseconds(clearOpacityIntervalMilliSecs);
			clearOpacityTimer.Tick += ClearOpacityTimer_Tick;
			Sessions = new ReadOnlyObservableCollection<HexIntellisenseSession>(sessions);
			wpfHexView.Closed += WpfHexView_Closed;
			wpfHexView.VisualElement.KeyDown += VisualElement_KeyDown;
			wpfHexView.VisualElement.KeyUp += VisualElement_KeyUp;
		}

		void ClearOpacityTimer_Tick(object sender, EventArgs e) {
			clearOpacityTimer.Stop();
			if (wpfHexView.IsClosed)
				return;
			SetOpacity(0.3);
		}

		void VisualElement_KeyUp(object sender, KeyEventArgs e) {
			if (wpfHexView.IsClosed)
				return;
			if (clearOpacityTimer.IsEnabled)
				StopClearOpacityTimer();
			else
				SetOpacity(1);
		}

		void VisualElement_KeyDown(object sender, KeyEventArgs e) {
			if (wpfHexView.IsClosed)
				return;
			var key = e.Key == Key.System ? e.SystemKey : e.Key;
			bool isCtrl = key == Key.LeftCtrl || key == Key.RightCtrl;
			if (isCtrl && e.KeyboardDevice.Modifiers == ModifierKeys.Control) {
				if (!clearOpacityTimer.IsEnabled)
					clearOpacityTimer.Start();
			}
			else
				StopClearOpacityTimer();
		}

		void StopClearOpacityTimer() {
			if (!clearOpacityTimer.IsEnabled)
				return;
			clearOpacityTimer.Stop();
			SetOpacity(1);
		}

		void SetOpacity(double opacity) {
			bool newIsInClearOpacityMode = opacity != 1;
			if (isInClearOpacityMode == newIsInClearOpacityMode)
				return;
			isInClearOpacityMode = newIsInClearOpacityMode;
			foreach (var session in sessions.ToArray()) {
				var popupPresenter = session.Presenter as IHexPopupIntellisensePresenter;
				if (popupPresenter != null)
					popupPresenter.Opacity = opacity;
			}
		}
		bool isInClearOpacityMode;

		bool ExecuteKeyboardCommand(VSLI.IntellisenseKeyboardCommand command) {
			foreach (var session in sessions) {
				if ((session.Presenter as VSLI.IIntellisenseCommandTarget)?.ExecuteKeyboardCommand(command) == true)
					return true;
			}
			return false;
		}

		public override void PushSession(HexIntellisenseSession session) {
			if (wpfHexView.IsClosed)
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

		public override HexIntellisenseSession PopSession() {
			if (wpfHexView.IsClosed)
				throw new InvalidOperationException();
			if (sessions.Count == 0)
				return null;
			var session = sessions[0];
			RemoveSessionAt(0);
			return session;
		}

		public override void MoveSessionToTop(HexIntellisenseSession session) {
			if (wpfHexView.IsClosed)
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

		public override void CollapseAllSessions() {
			if (wpfHexView.IsClosed)
				throw new InvalidOperationException();
			CollapseAllSessionsCore();
		}

		void CollapseAllSessionsCore() {
			var allSessions = sessions.ToArray();
			for (int i = allSessions.Length - 1; i >= 0; i--)
				allSessions[i].Collapse();
		}

		void Session_PresenterChanged(object sender, EventArgs e) {
			if (wpfHexView.IsClosed)
				return;
			PresenterUpdated((HexIntellisenseSession)sender);
		}

		int GetSessionStateIndex(HexIntellisenseSession session) {
			for (int i = 0; i < sessionStates.Count; i++) {
				if (sessionStates[i].Session == session)
					return i;
			}
			return -1;
		}

		SessionState GetSessionState(HexIntellisenseSession session) {
			int index = GetSessionStateIndex(session);
			if (index >= 0)
				return sessionStates[index];

			var sessionState = new SessionState(session);
			sessionStates.Add(sessionState);
			return sessionState;
		}

		SessionState TryGetSessionState(HexSpaceReservationAgent agent) {
			foreach (var sessionState in sessionStates) {
				if (sessionState.SpaceReservationAgent == agent)
					return sessionState;
			}
			return null;
		}

		SessionState TryGetSessionState(IHexPopupIntellisensePresenter popupPresenter) {
			foreach (var sessionState in sessionStates) {
				if (sessionState.PopupIntellisensePresenter == popupPresenter)
					return sessionState;
			}
			return null;
		}

		void PresenterUpdated(HexIntellisenseSession session) {
			var sessionState = GetSessionState(session);
			if (sessionState.SpaceReservationAgent != null)
				sessionState.SpaceReservationManager.RemoveAgent(sessionState.SpaceReservationAgent);
			Debug.Assert(sessionState.SpaceReservationAgent == null);

			var presenter = session.Presenter;
			var popupPresenter = presenter as IHexPopupIntellisensePresenter;
			if (popupPresenter != null) {
				if (sessionState.SpaceReservationManager == null) {
					sessionState.SetSpaceReservationManager(wpfHexView.GetSpaceReservationManager(popupPresenter.SpaceReservationManagerName));
					sessionState.SpaceReservationManager.AgentChanged += SpaceReservationManager_AgentChanged;
				}
				UnregisterPopupIntellisensePresenterEvents(sessionState.PopupIntellisensePresenter);
				sessionState.PopupIntellisensePresenter = popupPresenter;
				RegisterPopupIntellisensePresenterEvents(sessionState.PopupIntellisensePresenter);

				var presentationSpan = popupPresenter.PresentationSpan;
				var surfaceElement = popupPresenter.SurfaceElement;
				if (!presentationSpan.IsDefault && surfaceElement != null) {
					sessionState.SpaceReservationAgent = sessionState.SpaceReservationManager.CreatePopupAgent(presentationSpan, popupPresenter.PopupStyles, surfaceElement);
					sessionState.SpaceReservationManager.AddAgent(sessionState.SpaceReservationAgent);
				}
			}
			else {
				var customPresenter = presenter as IHexCustomIntellisensePresenter;
				if (customPresenter != null)
					customPresenter.Render();
				else
					Debug.Assert(presenter == null, $"Unsupported presenter: {presenter?.GetType()}");
			}
		}

		void RegisterPopupIntellisensePresenterEvents(IHexPopupIntellisensePresenter popupPresenter) {
			if (popupPresenter != null) {
				popupPresenter.PopupStylesChanged += PopupIntellisensePresenter_PopupStylesChanged;
				popupPresenter.PresentationSpanChanged += PopupIntellisensePresenter_PresentationSpanChanged;
				popupPresenter.SurfaceElementChanged += PopupIntellisensePresenter_SurfaceElementChanged;
			}
		}

		void UnregisterPopupIntellisensePresenterEvents(IHexPopupIntellisensePresenter popupPresenter) {
			if (popupPresenter != null) {
				popupPresenter.PopupStylesChanged -= PopupIntellisensePresenter_PopupStylesChanged;
				popupPresenter.PresentationSpanChanged -= PopupIntellisensePresenter_PresentationSpanChanged;
				popupPresenter.SurfaceElementChanged -= PopupIntellisensePresenter_SurfaceElementChanged;
			}
		}

		void PopupIntellisensePresenter_SurfaceElementChanged(object sender, EventArgs e) =>
			PopupIntellisensePresenter_PropertyChanged((IHexPopupIntellisensePresenter)sender, nameof(IHexPopupIntellisensePresenter.SurfaceElement));

		void PopupIntellisensePresenter_PresentationSpanChanged(object sender, EventArgs e) =>
			PopupIntellisensePresenter_PropertyChanged((IHexPopupIntellisensePresenter)sender, nameof(IHexPopupIntellisensePresenter.PresentationSpan));

		void PopupIntellisensePresenter_PopupStylesChanged(object sender, VSLI.ValueChangedEventArgs<VSTA.PopupStyles> e) =>
			PopupIntellisensePresenter_PropertyChanged((IHexPopupIntellisensePresenter)sender, nameof(IHexPopupIntellisensePresenter.PopupStyles));

		void PopupIntellisensePresenter_PropertyChanged(IHexPopupIntellisensePresenter popupPresenter, string propertyName) {
			if (wpfHexView.IsClosed) {
				UnregisterPopupIntellisensePresenterEvents(popupPresenter);
				return;
			}
			var sessionState = TryGetSessionState(popupPresenter);
			Debug.Assert(sessionState != null);
			if (sessionState == null)
				return;
			if (propertyName == nameof(popupPresenter.PresentationSpan) || propertyName == nameof(popupPresenter.PopupStyles)) {
				var presentationSpan = popupPresenter.PresentationSpan;
				if (presentationSpan.IsDefault || sessionState.SpaceReservationAgent == null)
					PresenterUpdated(popupPresenter.Session);
				else
					sessionState.SpaceReservationManager.UpdatePopupAgent(sessionState.SpaceReservationAgent, presentationSpan, popupPresenter.PopupStyles);
			}
			else if (propertyName == nameof(popupPresenter.SurfaceElement))
				PresenterUpdated(popupPresenter.Session);
		}

		void SpaceReservationManager_AgentChanged(object sender, HexSpaceReservationAgentChangedEventArgs e) {
			if (wpfHexView.IsClosed)
				return;
			var sessionState = TryGetSessionState(e.OldAgent);
			if (sessionState != null) {
				sessionState.SpaceReservationAgent = null;
				// Its popup was hidden, so dismiss the session
				sessionState.Session.Dismiss();
			}
		}

		void Session_Dismissed(object sender, EventArgs e) {
			var session = sender as HexIntellisenseSession;
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
			UnregisterPopupIntellisensePresenterEvents(sessionState.PopupIntellisensePresenter);
			sessionStates.Remove(sessionState);
			if (sessions.Count == 0) {
				Debug.Assert(sessionStates.Count == 0);
				commandTargetFilter.UnhookKeyboard();
			}
		}

		void WpfHexView_Closed(object sender, EventArgs e) {
			clearOpacityTimer.Stop();
			CollapseAllSessionsCore();
			while (sessions.Count > 0)
				RemoveSessionAt(sessions.Count - 1);
			commandTargetFilter.Destroy();
			wpfHexView.Closed -= WpfHexView_Closed;
			wpfHexView.VisualElement.KeyDown -= VisualElement_KeyDown;
			wpfHexView.VisualElement.KeyUp -= VisualElement_KeyUp;
		}
	}
}
