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

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media;
using dnSpy.Contracts.Hex;
using dnSpy.Contracts.Hex.Editor;
using VSTA = Microsoft.VisualStudio.Text.Adornments;

namespace dnSpy.Hex.Editor {
	sealed class HexSpaceReservationManagerImpl : HexSpaceReservationManager {
		public override ReadOnlyCollection<HexSpaceReservationAgent> Agents { get; }
		public override bool HasAggregateFocus => hasAggregateFocus;
		bool hasAggregateFocus;
		public override event EventHandler<HexSpaceReservationAgentChangedEventArgs> AgentChanged;
		public override event EventHandler GotAggregateFocus;
		public override event EventHandler LostAggregateFocus;

		public override bool IsMouseOver {
			get {
				foreach (var agent in spaceReservationAgents) {
					if (agent.IsMouseOver)
						return true;
				}
				return false;
			}
		}

		readonly WpfHexView wpfHexView;
		readonly List<HexSpaceReservationAgent> spaceReservationAgents;

		public HexSpaceReservationManagerImpl(WpfHexView wpfHexView) {
			this.wpfHexView = wpfHexView ?? throw new ArgumentNullException(nameof(wpfHexView));
			spaceReservationAgents = new List<HexSpaceReservationAgent>();
			Agents = new ReadOnlyCollection<HexSpaceReservationAgent>(spaceReservationAgents);
			wpfHexView.Closed += WpfHexView_Closed;
		}

		public override void AddAgent(HexSpaceReservationAgent agent) {
			if (wpfHexView.IsClosed)
				throw new InvalidOperationException();
			if (agent == null)
				throw new ArgumentNullException(nameof(agent));
			if (spaceReservationAgents.Contains(agent))
				throw new InvalidOperationException();
			spaceReservationAgents.Add(agent);
			agent.GotFocus += HexSpaceReservationAgent_GotFocus;
			agent.LostFocus += HexSpaceReservationAgent_LostFocus;
			AgentChanged?.Invoke(this, new HexSpaceReservationAgentChangedEventArgs(null, agent));
			UpdateAggregateFocus();
			wpfHexView.QueueSpaceReservationStackRefresh();
		}

		public override bool RemoveAgent(HexSpaceReservationAgent agent) {
			if (agent == null)
				throw new ArgumentNullException(nameof(agent));
			if (!spaceReservationAgents.Remove(agent))
				return false;
			agent.GotFocus -= HexSpaceReservationAgent_GotFocus;
			agent.LostFocus -= HexSpaceReservationAgent_LostFocus;
			agent.Hide();
			AgentChanged?.Invoke(this, new HexSpaceReservationAgentChangedEventArgs(agent, null));
			UpdateAggregateFocus();
			wpfHexView.QueueSpaceReservationStackRefresh();
			return true;
		}

		public override HexSpaceReservationAgent CreatePopupAgent(HexLineSpan lineSpan, VSTA.PopupStyles style, UIElement content) {
			if (wpfHexView.IsClosed)
				throw new InvalidOperationException();
			if (lineSpan.IsDefault)
				throw new ArgumentException();
			if (content == null)
				throw new ArgumentNullException(nameof(content));
			if ((style & (VSTA.PopupStyles.DismissOnMouseLeaveText | VSTA.PopupStyles.DismissOnMouseLeaveTextOrContent)) == (VSTA.PopupStyles.DismissOnMouseLeaveText | VSTA.PopupStyles.DismissOnMouseLeaveTextOrContent))
				throw new ArgumentOutOfRangeException(nameof(style));
			return new HexPopupSpaceReservationAgent(this, wpfHexView, lineSpan, style, content);
		}

		public override void UpdatePopupAgent(HexSpaceReservationAgent agent, HexLineSpan lineSpan, VSTA.PopupStyles styles) {
			if (wpfHexView.IsClosed)
				throw new InvalidOperationException();
			if (agent == null)
				throw new ArgumentNullException(nameof(agent));
			if (lineSpan.IsDefault)
				throw new ArgumentException();
			if ((styles & (VSTA.PopupStyles.DismissOnMouseLeaveText | VSTA.PopupStyles.DismissOnMouseLeaveTextOrContent)) == (VSTA.PopupStyles.DismissOnMouseLeaveText | VSTA.PopupStyles.DismissOnMouseLeaveTextOrContent))
				throw new ArgumentOutOfRangeException(nameof(styles));
			if (!spaceReservationAgents.Contains(agent))
				throw new ArgumentOutOfRangeException(nameof(agent));
			var popupAgent = agent as HexPopupSpaceReservationAgent;
			if (popupAgent == null)
				throw new ArgumentException();
			popupAgent.Update(lineSpan, styles);
			UpdateAggregateFocus();
			wpfHexView.QueueSpaceReservationStackRefresh();
		}

		void HexSpaceReservationAgent_GotFocus(object sender, EventArgs e) => UpdateAggregateFocus();
		void HexSpaceReservationAgent_LostFocus(object sender, EventArgs e) => UpdateAggregateFocus();

		void UpdateAggregateFocus() {
			bool newValue = CalculateAggregateFocus();
			if (newValue != HasAggregateFocus) {
				hasAggregateFocus = newValue;
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

			bool isVisible = wpfHexView.VisualElement.IsVisible;
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

		void WpfHexView_Closed(object sender, EventArgs e) {
			while (spaceReservationAgents.Count > 0)
				RemoveAgent(spaceReservationAgents[spaceReservationAgents.Count - 1]);
			wpfHexView.Closed -= WpfHexView_Closed;
		}
	}
}
