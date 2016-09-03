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
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace dnSpy.Contracts.Language.Intellisense {
	/// <summary>
	/// Intellisense session
	/// </summary>
	public interface IIntellisenseSession : IPropertyOwner {
		/// <summary>
		/// Gets the trigger point
		/// </summary>
		/// <param name="textSnapshot">Snapshot</param>
		/// <returns></returns>
		SnapshotPoint? GetTriggerPoint(ITextSnapshot textSnapshot);

		/// <summary>
		/// Gets the text view
		/// </summary>
		ITextView TextView { get; }

		/// <summary>
		/// Starts the session
		/// </summary>
		void Start();

		/// <summary>
		/// Dismisses the session
		/// </summary>
		void Dismiss();

		/// <summary>
		/// Raised when it's been dismissed
		/// </summary>
		event EventHandler Dismissed;

		/// <summary>
		/// true if it's been dismissed
		/// </summary>
		bool IsDismissed { get; }

		/// <summary>
		/// Finds the best match and selects it. Returns false if no match was found.
		/// </summary>
		/// <returns></returns>
		bool Match();
	}
}
