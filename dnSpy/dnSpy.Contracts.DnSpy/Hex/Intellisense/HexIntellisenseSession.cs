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
using dnSpy.Contracts.Hex.Editor;
using VSUTIL = Microsoft.VisualStudio.Utilities;

namespace dnSpy.Contracts.Hex.Intellisense {
	/// <summary>
	/// Intellisense session
	/// </summary>
	public abstract class HexIntellisenseSession : VSUTIL.IPropertyOwner {
		/// <summary>
		/// Constructor
		/// </summary>
		protected HexIntellisenseSession() {
			Properties = new VSUTIL.PropertyCollection();
		}

		/// <summary>
		/// Gets all properties
		/// </summary>
		public VSUTIL.PropertyCollection Properties { get; }

		/// <summary>
		/// Gets the trigger point
		/// </summary>
		public abstract HexCellPosition TriggerPoint { get; }

		/// <summary>
		/// Gets the hex view
		/// </summary>
		public abstract HexView HexView { get; }

		/// <summary>
		/// Gets the presenter
		/// </summary>
		public abstract HexIntellisensePresenter Presenter { get; }

		/// <summary>
		/// Raised after <see cref="Presenter"/> is changed
		/// </summary>
		public abstract event EventHandler PresenterChanged;

		/// <summary>
		/// Starts the session
		/// </summary>
		public abstract void Start();

		/// <summary>
		/// Dismisses the session
		/// </summary>
		public abstract void Dismiss();

		/// <summary>
		/// Raised after <see cref="Dismiss"/> is called
		/// </summary>
		public abstract event EventHandler Dismissed;

		/// <summary>
		/// true if the session has been dismissed
		/// </summary>
		public abstract bool IsDismissed { get; }

		/// <summary>
		/// Recalculates the state
		/// </summary>
		public abstract void Recalculate();

		/// <summary>
		/// Raised after <see cref="Recalculate"/> is called
		/// </summary>
		public abstract event EventHandler Recalculated;

		/// <summary>
		/// Tries to get a match
		/// </summary>
		/// <returns></returns>
		public abstract bool Match();

		/// <summary>
		/// Collapses the UI, or if it's not collapsible, closes the UI
		/// </summary>
		public abstract void Collapse();
	}
}
