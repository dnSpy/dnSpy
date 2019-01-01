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

using System.Collections.ObjectModel;
using dnSpy.Contracts.Hex.Editor;

namespace dnSpy.Contracts.Hex.Intellisense {
	/// <summary>
	/// Quick info broker
	/// </summary>
	public abstract class HexQuickInfoBroker {
		/// <summary>
		/// Constructor
		/// </summary>
		protected HexQuickInfoBroker() { }

		/// <summary>
		/// Returns true if quick info is active in <paramref name="hexView"/>
		/// </summary>
		/// <param name="hexView">Hex view to check</param>
		/// <returns></returns>
		public abstract bool IsQuickInfoActive(HexView hexView);

		/// <summary>
		/// Triggers a quick info session
		/// </summary>
		/// <param name="hexView">Hex view</param>
		/// <returns></returns>
		public abstract HexQuickInfoSession TriggerQuickInfo(HexView hexView);

		/// <summary>
		/// Triggers a quick info session
		/// </summary>
		/// <param name="hexView">Hex view</param>
		/// <param name="triggerPoint">Trigger point</param>
		/// <param name="trackMouse">true to track the mouse</param>
		/// <returns></returns>
		public abstract HexQuickInfoSession TriggerQuickInfo(HexView hexView, HexCellPosition triggerPoint, bool trackMouse);

		/// <summary>
		/// Creates a quick info session
		/// </summary>
		/// <param name="hexView">Hex view</param>
		/// <param name="triggerPoint">Trigger point</param>
		/// <param name="trackMouse">true to track the mouse</param>
		/// <returns></returns>
		public abstract HexQuickInfoSession CreateQuickInfoSession(HexView hexView, HexCellPosition triggerPoint, bool trackMouse);

		/// <summary>
		/// Gets all active quick info sessions in <paramref name="hexView"/>
		/// </summary>
		/// <param name="hexView">Hex view</param>
		/// <returns></returns>
		public abstract ReadOnlyCollection<HexQuickInfoSession> GetSessions(HexView hexView);
	}
}
