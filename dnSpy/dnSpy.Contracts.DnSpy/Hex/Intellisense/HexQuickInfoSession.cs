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
using VSLI = Microsoft.VisualStudio.Language.Intellisense;

namespace dnSpy.Contracts.Hex.Intellisense {
	/// <summary>
	/// Quick info session
	/// </summary>
	public abstract class HexQuickInfoSession : HexIntellisenseSession {
		/// <summary>
		/// Constructor
		/// </summary>
		protected HexQuickInfoSession() { }

		/// <summary>
		/// Gets the quick info content
		/// </summary>
		public abstract VSLI.BulkObservableCollection<object> QuickInfoContent { get; }

		/// <summary>
		/// Gets the applicable-to span
		/// </summary>
		public abstract HexBufferSpanSelection ApplicableToSpan { get; }

		/// <summary>
		/// Raised after <see cref="ApplicableToSpan"/> is changed
		/// </summary>
		public abstract event EventHandler ApplicableToSpanChanged;

		/// <summary>
		/// true if the mouse is tracked
		/// </summary>
		public abstract bool TrackMouse { get; }

		/// <summary>
		/// true if it has interactive content
		/// </summary>
		public virtual bool HasInteractiveContent => false;
	}
}
