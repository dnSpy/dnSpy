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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Text.Editor;

namespace dnSpy.Text.Editor {
	sealed class SpaceReservationManager : ISpaceReservationManager {
		public ReadOnlyCollection<ISpaceReservationAgent> Agents { get; }
		public bool HasAggregateFocus { get; private set; }
		public event EventHandler<SpaceReservationAgentChangedEventArgs> AgentChanged;
		public event EventHandler GotAggregateFocus;
		public event EventHandler LostAggregateFocus;

		public bool IsMouseOver {
			get {
				foreach (var agent in spaceReservationAgents) {
					if (agent.IsMouseOver)
						return true;
				}
				return false;
			}
		}

		readonly IWpfTextView wpfTextView;
		readonly List<ISpaceReservationAgent> spaceReservationAgents;

		public SpaceReservationManager(IWpfTextView wpfTextView) {
			this.wpfTextView = wpfTextView ?? throw new ArgumentNullException(nameof(wpfTextView));
			spaceReservationAgents = new List<ISpaceReservationAgent>();
			Agents = new ReadOnlyCollection<ISpaceReservationAgent>(spaceReservationAgents);
			wpfTextView.Closed += WpfTextView_Closed;
		}

		public void AddAgent(ISpaceReservationAgent agent) {
			if (wpfTextView.IsClosed)
				throw new InvalidOperationException();
			if (agent == null)
				throw new ArgumentNullException(nameof(agent));
			if (spaceReservationAgents.Contains(agent))
				throw new InvalidOperationException();
			spaceReservationAgents.Add(agent);
			agent.GotFocus += SpaceReservationAgent_GotFocus;
			agent.LostFocus += SpaceReservationAgent_LostFocus;
			AgentChanged?.Invoke(this, new SpaceReservationAgentChangedEventArgs(null, agent));
			UpdateAggregateFocus();
			wpfTextView.QueueSpaceReservationStackRefresh();
		}

		public bool RemoveAgent(ISpaceReservationAgent agent) {
			if (agent == null)
				throw new ArgumentNullException(nameof(agent));
			if (!spaceReservationAgents.Remove(agent))
				return false;
			agent.GotFocus -= SpaceReservationAgent_GotFocus;
			agent.LostFocus -= SpaceReservationAgent_LostFocus;
			agent.Hide();
			AgentChanged?.Invoke(this, new SpaceReservationAgentChangedEventArgs(agent, null));
			UpdateAggregateFocus();
			wpfTextView.QueueSpaceReservationStackRefresh();
			return true;
		}

		public ISpaceReservationAgent CreatePopupAgent(ITrackingSpan visualSpan, PopupStyles style, UIElement content) {
			if (wpfTextView.IsClosed)
				throw new InvalidOperationException();
			if (visualSpan == null)
				throw new ArgumentNullException(nameof(visualSpan));
			if (content == null)
				throw new ArgumentNullException(nameof(content));
			if ((style & (PopupStyles.DismissOnMouseLeaveText | PopupStyles.DismissOnMouseLeaveTextOrContent)) == (PopupStyles.DismissOnMouseLeaveText | PopupStyles.DismissOnMouseLeaveTextOrContent))
				throw new ArgumentOutOfRangeException(nameof(style));
			return new PopupSpaceReservationAgent(this, wpfTextView, visualSpan, style, content);
		}

		public void UpdatePopupAgent(ISpaceReservationAgent agent, ITrackingSpan visualSpan, PopupStyles styles) {
			if (wpfTextView.IsClosed)
				throw new InvalidOperationException();
			if (agent == null)
				throw new ArgumentNullException(nameof(agent));
			if (visualSpan == null)
				throw new ArgumentNullException(nameof(visualSpan));
			if ((styles & (PopupStyles.DismissOnMouseLeaveText | PopupStyles.DismissOnMouseLeaveTextOrContent)) == (PopupStyles.DismissOnMouseLeaveText | PopupStyles.DismissOnMouseLeaveTextOrContent))
				throw new ArgumentOutOfRangeException(nameof(styles));
			if (!spaceReservationAgents.Contains(agent))
				throw new ArgumentOutOfRangeException(nameof(agent));
			var popupAgent = agent as PopupSpaceReservationAgent;
			if (popupAgent == null)
				throw new ArgumentException();
			popupAgent.Update(visualSpan, styles);
			UpdateAggregateFocus();
			wpfTextView.QueueSpaceReservationStackRefresh();
		}

		void SpaceReservationAgent_GotFocus(object sender, EventArgs e) => UpdateAggregateFocus();
		void SpaceReservationAgent_LostFocus(object sender, EventArgs e) => UpdateAggregateFocus();

		void UpdateAggregateFocus() {
			bool newValue = CalculateAggregateFocus();
			if (newValue != HasAggregateFocus) {
				HasAggregateFocus = newValue;
				if (newValue)
					GotAggregateFocus?.Invoke(this, EventArgs.Empty);
				else
					LostAggregateFocus?.Invoke(this, EventArgs.Empty);
			}
		}

		bool CalculateAggregateFocus() {
			foreach (var agent in spaceReservationAgents) {
				if (agent.HasFocus)
					return true;
			}
			return false;
		}

		internal void PositionAndDisplay(GeometryGroup reservedSpace) {
			if (spaceReservationAgents.Count == 0)
				return;

			bool isVisible = wpfTextView.VisualElement.IsVisible;
			for (int i = spaceReservationAgents.Count - 1; i >= 0; i--) {
				var agent = spaceReservationAgents[i];
				var geometry = isVisible ? agent.PositionAndDisplay(reservedSpace) : null;
				if (geometry == null)
					RemoveAgent(agent);
				else if (!geometry.IsEmpty())
					reservedSpace.Children.Add(geometry);
			}

			UpdateAggregateFocus();
		}

		void WpfTextView_Closed(object sender, EventArgs e) {
			while (spaceReservationAgents.Count > 0)
				RemoveAgent(spaceReservationAgents[spaceReservationAgents.Count - 1]);
			wpfTextView.Closed -= WpfTextView_Closed;
		}
	}
}
