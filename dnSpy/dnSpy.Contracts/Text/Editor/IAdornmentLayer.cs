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

namespace dnSpy.Contracts.Text.Editor {
	/// <summary>
	/// An <see cref="IWpfTextView"/> adornment layer
	/// </summary>
	public interface IAdornmentLayer {
		/// <summary>
		/// Gets the <see cref="IWpfTextView"/> to which this layer is attached
		/// </summary>
		IWpfTextView TextView { get; }

		/// <summary>
		/// Gets or sets the opacity factor applied to the entire adornment layer when it is rendered in the user interface
		/// </summary>
		double Opacity { get; set; }

		/// <summary>
		/// true if it's empty
		/// </summary>
		bool IsEmpty { get; }

		/// <summary>
		/// Gets a collection of the adornments and their associated data in the layer
		/// </summary>
		ReadOnlyCollection<IAdornmentLayerElement> Elements { get; }

		/// <summary>
		/// Adds a <see cref="UIElement"/> that is <see cref="AdornmentPositioningBehavior.TextRelative"/> to the layer
		/// </summary>
		/// <param name="visualSpan">The span with which <paramref name="adornment"/> is associated</param>
		/// <param name="tag">The tag associated with <paramref name="adornment"/></param>
		/// <param name="adornment">The <see cref="UIElement"/> to add to the view</param>
		/// <returns></returns>
		bool AddAdornment(SnapshotSpan visualSpan, object tag, UIElement adornment);

		/// <summary>
		/// Adds a <see cref="UIElement"/> to the layer
		/// </summary>
		/// <param name="behavior">The positioning behavior of <paramref name="adornment"/></param>
		/// <param name="visualSpan">The span with which <paramref name="adornment"/> is associated</param>
		/// <param name="tag">The tag associated with <paramref name="adornment"/></param>
		/// <param name="adornment">The <see cref="UIElement"/> to add to the view</param>
		/// <param name="removedCallback">The delegate to call when <paramref name="adornment"/> is removed from the view</param>
		/// <returns></returns>
		bool AddAdornment(AdornmentPositioningBehavior behavior, SnapshotSpan? visualSpan, object tag, UIElement adornment, AdornmentRemovedCallback removedCallback);

		/// <summary>
		/// Removes a specific <see cref="UIElement"/>
		/// </summary>
		/// <param name="adornment"><see cref="UIElement"/> to remove</param>
		void RemoveAdornment(UIElement adornment);

		/// <summary>
		/// Removes all <see cref="UIElement"/> objects associated with a particular tag
		/// </summary>
		/// <param name="tag">The tag to use to remove the UI elements</param>
		void RemoveAdornmentsByTag(object tag);

		/// <summary>
		/// Removes all adornments with visual spans that overlap the given visual span
		/// </summary>
		/// <param name="visualSpan">The visual span to check for overlap with adornments</param>
		void RemoveAdornmentsByVisualSpan(SnapshotSpan visualSpan);

		/// <summary>
		/// Removes all <see cref="UIElement"/> objects in the layer
		/// </summary>
		void RemoveAllAdornments();

		/// <summary>
		/// Removes all matching adornments
		/// </summary>
		/// <param name="match">The predicate that will be called for each adornment</param>
		void RemoveMatchingAdornments(Predicate<IAdornmentLayerElement> match);

		/// <summary>
		/// Removes all matching adornments with visual spans
		/// </summary>
		/// <param name="visualSpan">The visual span to check for overlap with adornments</param>
		/// <param name="match">The predicate that will be called for each adornment</param>
		void RemoveMatchingAdornments(SnapshotSpan visualSpan, Predicate<IAdornmentLayerElement> match);
	}
}
