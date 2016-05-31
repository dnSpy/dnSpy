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

namespace dnSpy.Contracts.Text.Editor {
	/// <summary>
	/// View state
	/// </summary>
	public sealed class ViewState {
		/// <summary>
		/// Edit snapshot (<see cref="ITextView.TextSnapshot"/>)
		/// </summary>
		public ITextSnapshot EditSnapshot { get; }

		/// <summary>
		/// Visual snapshot (<see cref="ITextView.VisualSnapshot"/>)
		/// </summary>
		public ITextSnapshot VisualSnapshot { get; }

		/// <summary>
		/// Viewport top (<see cref="ITextView.ViewportTop"/>)
		/// </summary>
		public double ViewportTop { get; }

		/// <summary>
		/// Viewport bottom (<see cref="ITextView.ViewportBottom"/>)
		/// </summary>
		public double ViewportBottom { get; }

		/// <summary>
		/// Viewport left (<see cref="ITextView.ViewportLeft"/>)
		/// </summary>
		public double ViewportLeft { get; }

		/// <summary>
		/// Viewport right (<see cref="ITextView.ViewportRight"/>)
		/// </summary>
		public double ViewportRight { get; }

		/// <summary>
		/// Viewport width (<see cref="ITextView.ViewportWidth"/>)
		/// </summary>
		public double ViewportWidth { get; }

		/// <summary>
		/// Viewport height (<see cref="ITextView.ViewportHeight"/>)
		/// </summary>
		public double ViewportHeight { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="view">View</param>
		public ViewState(ITextView view)
			: this(view, view.ViewportWidth, view.ViewportHeight) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="view">View</param>
		/// <param name="effectiveViewportWidth">Effective viewport width</param>
		/// <param name="effectiveViewportHeight">Effective viewport height</param>
		public ViewState(ITextView view, double effectiveViewportWidth, double effectiveViewportHeight) {
			if (view == null)
				throw new ArgumentNullException(nameof(view));
			EditSnapshot = view.TextSnapshot;
			VisualSnapshot = view.VisualSnapshot;
			ViewportTop = view.ViewportTop;
			ViewportBottom = view.ViewportBottom;
			ViewportLeft = view.ViewportLeft;
			ViewportRight = view.ViewportRight;
			ViewportWidth = view.ViewportWidth;
			ViewportHeight = view.ViewportHeight;
		}
	}
}
