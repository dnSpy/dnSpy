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
using VSTE = Microsoft.VisualStudio.Text.Editor;

namespace dnSpy.Contracts.Hex.Editor {
	/// <summary>
	/// Adornment layer
	/// </summary>
	public abstract class HexAdornmentLayer {
		/// <summary>
		/// Constructor
		/// </summary>
		protected HexAdornmentLayer() { }

		/// <summary>
		/// Gets the UI element
		/// </summary>
		public abstract FrameworkElement VisualElement { get; }

		/// <summary>
		/// true if the layer is empty
		/// </summary>
		public abstract bool IsEmpty { get; }

		/// <summary>
		/// Gets/sets the layer opacity
		/// </summary>
		public virtual double Opacity {
			get { return VisualElement.Opacity; }
			set { VisualElement.Opacity = value; }
		}

		/// <summary>
		/// Gets the hex view
		/// </summary>
		public abstract WpfHexView HexView { get; }

		/// <summary>
		/// Gets all elements
		/// </summary>
		public abstract ReadOnlyCollection<HexAdornmentLayerElement> Elements { get; }

		/// <summary>
		/// Adds an adornment. Returns true if the adornment was added.
		/// </summary>
		/// <param name="line">Line</param>
		/// <param name="tag">Tag</param>
		/// <param name="adornment">Adornment</param>
		/// <returns></returns>
		public bool AddAdornment(HexBufferLine line, object tag, UIElement adornment) =>
			AddAdornment(VSTE.AdornmentPositioningBehavior.TextRelative, line, tag, adornment, null);

		/// <summary>
		/// Adds an adornment. Returns true if the adornment was added.
		/// </summary>
		/// <param name="behavior">Positioning behavior</param>
		/// <param name="line">Line</param>
		/// <param name="tag">Tag</param>
		/// <param name="adornment">Adornment</param>
		/// <param name="removedCallback">Called when the adornment is removed</param>
		/// <returns></returns>
		public bool AddAdornment(VSTE.AdornmentPositioningBehavior behavior, HexBufferLine line, object tag, UIElement adornment, VSTE.AdornmentRemovedCallback removedCallback) {
			if (line == null)
				throw new ArgumentNullException(nameof(line));
			return AddAdornment(behavior, line.BufferSpan, tag, adornment, removedCallback);
		}

		/// <summary>
		/// Adds an adornment. Returns true if the adornment was added.
		/// </summary>
		/// <param name="visualSpan">Span</param>
		/// <param name="tag">Tag</param>
		/// <param name="adornment">Adornment</param>
		/// <returns></returns>
		public bool AddAdornment(HexBufferSpan visualSpan, object tag, UIElement adornment) =>
			AddAdornment(VSTE.AdornmentPositioningBehavior.TextRelative, visualSpan, tag, adornment, null);

		/// <summary>
		/// Adds an adornment. Returns true if the adornment was added.
		/// </summary>
		/// <param name="behavior">Positioning behavior</param>
		/// <param name="visualSpan">Span</param>
		/// <param name="tag">Tag</param>
		/// <param name="adornment">Adornment</param>
		/// <param name="removedCallback">Called when the adornment is removed</param>
		/// <returns></returns>
		public abstract bool AddAdornment(VSTE.AdornmentPositioningBehavior behavior, HexBufferSpan? visualSpan, object tag, UIElement adornment, VSTE.AdornmentRemovedCallback removedCallback);

		/// <summary>
		/// Removes an adornment
		/// </summary>
		/// <param name="adornment">Adornment to remove</param>
		public abstract void RemoveAdornment(UIElement adornment);

		/// <summary>
		/// Removes all adornments with the specified tag
		/// </summary>
		/// <param name="tag">Tag</param>
		public abstract void RemoveAdornmentsByTag(object tag);

		/// <summary>
		/// Removes all matching adornments
		/// </summary>
		/// <param name="match">Returns true if the adornment should be removed</param>
		public abstract void RemoveMatchingAdornments(Predicate<HexAdornmentLayerElement> match);

		/// <summary>
		/// Removes an adornment
		/// </summary>
		/// <param name="line">Line</param>
		public void RemoveAdornmentsByVisualSpan(HexBufferLine line) =>
			RemoveMatchingAdornments(line, returnTruePredicate);

		/// <summary>
		/// Removes an adornment
		/// </summary>
		/// <param name="visualSpan">Span</param>
		public void RemoveAdornmentsByVisualSpan(HexBufferSpan visualSpan) =>
			RemoveMatchingAdornments(visualSpan, returnTruePredicate);

		static readonly Predicate<HexAdornmentLayerElement> returnTruePredicate = a => true;

		/// <summary>
		/// Removes all matching adornments
		/// </summary>
		/// <param name="line">Line</param>
		/// <param name="match">Returns true if the adornment should be removed</param>
		public void RemoveMatchingAdornments(HexBufferLine line, Predicate<HexAdornmentLayerElement> match) {
			if (line == null)
				throw new ArgumentNullException(nameof(line));
			if (match == null)
				throw new ArgumentNullException(nameof(match));
			RemoveMatchingAdornments(line.BufferSpan, match);
		}

		/// <summary>
		/// Removes all matching adornments
		/// </summary>
		/// <param name="visualSpan">Span</param>
		/// <param name="match">Returns true if the adornment should be removed</param>
		public abstract void RemoveMatchingAdornments(HexBufferSpan visualSpan, Predicate<HexAdornmentLayerElement> match);

		/// <summary>
		/// Removes all adornments
		/// </summary>
		public abstract void RemoveAllAdornments();
	}
}
