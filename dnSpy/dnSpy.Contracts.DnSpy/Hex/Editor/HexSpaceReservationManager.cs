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
using System.Collections.ObjectModel;
using System.Windows;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Adornments;

namespace dnSpy.Contracts.Hex.Editor {
	/// <summary>
	/// Space reservation manager
	/// </summary>
	public abstract class HexSpaceReservationManager {
		/// <summary>
		/// Constructor
		/// </summary>
		protected HexSpaceReservationManager() { }

		/// <summary>
		/// Gets all agents
		/// </summary>
		public abstract ReadOnlyCollection<HexSpaceReservationAgent> Agents { get; }

		/// <summary>
		/// true if any of the agents' adornments have keyboard focus
		/// </summary>
		public abstract bool HasAggregateFocus { get; }

		/// <summary>
		/// true if the mouse is over any of the agents' adornments
		/// </summary>
		public abstract bool IsMouseOver { get; }

		/// <summary>
		/// Raised after an agent has been added or removed from <see cref="Agents"/>
		/// </summary>
		public abstract event EventHandler<HexSpaceReservationAgentChangedEventArgs> AgentChanged;

		/// <summary>
		/// Raised after it got aggregate focus
		/// </summary>
		public abstract event EventHandler GotAggregateFocus;

		/// <summary>
		/// Raised after it lost aggregate focus
		/// </summary>
		public abstract event EventHandler LostAggregateFocus;

		/// <summary>
		/// Adds an agent
		/// </summary>
		/// <param name="agent">Agent to add</param>
		public abstract void AddAgent(HexSpaceReservationAgent agent);

		/// <summary>
		/// Creates a popup agent
		/// </summary>
		/// <param name="bufferSpan">Buffer span</param>
		/// <param name="flags">Selection flags</param>
		/// <param name="style">Popup style</param>
		/// <param name="content">Popup content</param>
		/// <returns></returns>
		public HexSpaceReservationAgent CreatePopupAgent(HexBufferSpan bufferSpan, HexSpanSelectionFlags flags, PopupStyles style, UIElement content) =>
			CreatePopupAgent(new HexLineSpan(bufferSpan, flags), style, content);

		/// <summary>
		/// Creates a popup agent
		/// </summary>
		/// <param name="line">Line</param>
		/// <param name="span">Line span</param>
		/// <param name="style">Popup style</param>
		/// <param name="content">Popup content</param>
		/// <returns></returns>
		public HexSpaceReservationAgent CreatePopupAgent(HexBufferLine line, Span span, PopupStyles style, UIElement content) =>
			CreatePopupAgent(new HexLineSpan(line, span), style, content);

		/// <summary>
		/// Creates a popup agent
		/// </summary>
		/// <param name="lineSpan">Line span</param>
		/// <param name="style">Popup style</param>
		/// <param name="content">Popup content</param>
		/// <returns></returns>
		public abstract HexSpaceReservationAgent CreatePopupAgent(HexLineSpan lineSpan, PopupStyles style, UIElement content);

		/// <summary>
		/// Removes an agent
		/// </summary>
		/// <param name="agent">Agent to remove</param>
		/// <returns></returns>
		public abstract bool RemoveAgent(HexSpaceReservationAgent agent);

		/// <summary>
		/// Updates a popup agent
		/// </summary>
		/// <param name="agent">Popup agent created by <see cref="CreatePopupAgent(HexLineSpan, PopupStyles, UIElement)"/></param>
		/// <param name="bufferSpan">New buffer span</param>
		/// <param name="flags">New selection flags</param>
		/// <param name="styles">New popup style</param>
		public void UpdatePopupAgent(HexSpaceReservationAgent agent, HexBufferSpan bufferSpan, HexSpanSelectionFlags flags, PopupStyles styles) =>
			UpdatePopupAgent(agent, new HexLineSpan(bufferSpan, flags), styles);

		/// <summary>
		/// Updates a popup agent
		/// </summary>
		/// <param name="agent">Popup agent created by <see cref="CreatePopupAgent(HexLineSpan, PopupStyles, UIElement)"/></param>
		/// <param name="line">Line</param>
		/// <param name="span">Line span</param>
		/// <param name="styles">New popup style</param>
		public void UpdatePopupAgent(HexSpaceReservationAgent agent, HexBufferLine line, Span span, PopupStyles styles) =>
			UpdatePopupAgent(agent, new HexLineSpan(line, span), styles);

		/// <summary>
		/// Updates a popup agent
		/// </summary>
		/// <param name="agent">Popup agent created by <see cref="CreatePopupAgent(HexLineSpan, PopupStyles, UIElement)"/></param>
		/// <param name="lineSpan">New line span</param>
		/// <param name="styles">New popup style</param>
		public abstract void UpdatePopupAgent(HexSpaceReservationAgent agent, HexLineSpan lineSpan, PopupStyles styles);
	}

	/// <summary>
	/// Space reservation agent changed event args
	/// </summary>
	public sealed class HexSpaceReservationAgentChangedEventArgs : EventArgs {
		/// <summary>
		/// Gets the new agent or null
		/// </summary>
		public HexSpaceReservationAgent NewAgent { get; }

		/// <summary>
		/// Gets the old agent or null
		/// </summary>
		public HexSpaceReservationAgent OldAgent { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="oldAgent">Old agent or null</param>
		/// <param name="newAgent">New agent or null</param>
		public HexSpaceReservationAgentChangedEventArgs(HexSpaceReservationAgent oldAgent, HexSpaceReservationAgent newAgent) {
			NewAgent = newAgent;
			OldAgent = oldAgent;
		}
	}
}
