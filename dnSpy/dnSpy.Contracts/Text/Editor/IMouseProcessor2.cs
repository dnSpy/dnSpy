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

using System.Windows.Input;

namespace dnSpy.Contracts.Text.Editor {
	/// <summary>
	/// Mouse processor
	/// </summary>
	public interface IMouseProcessor2 : IMouseProcessor {
		/// <summary>
		/// Handles the event before the default handler
		/// </summary>
		/// <param name="e">Event args</param>
		void PreprocessManipulationCompleted(ManipulationCompletedEventArgs e);

		/// <summary>
		/// Handles the event before the default handler
		/// </summary>
		/// <param name="e">Event args</param>
		void PreprocessManipulationDelta(ManipulationDeltaEventArgs e);

		/// <summary>
		/// Handles the event before the default handler
		/// </summary>
		/// <param name="e">Event args</param>
		void PreprocessManipulationInertiaStarting(ManipulationInertiaStartingEventArgs e);

		/// <summary>
		/// Handles the event before the default handler
		/// </summary>
		/// <param name="e">Event args</param>
		void PreprocessManipulationStarting(ManipulationStartingEventArgs e);

		/// <summary>
		/// Handles the event before the default handler
		/// </summary>
		/// <param name="e">Event args</param>
		void PreprocessStylusSystemGesture(StylusSystemGestureEventArgs e);

		/// <summary>
		/// Handles the event before the default handler
		/// </summary>
		/// <param name="e">Event args</param>
		void PreprocessTouchDown(TouchEventArgs e);

		/// <summary>
		/// Handles the event before the default handler
		/// </summary>
		/// <param name="e">Event args</param>
		void PreprocessTouchUp(TouchEventArgs e);

		/// <summary>
		/// Handles the event after the default handler
		/// </summary>
		/// <param name="e">Event args</param>
		void PostprocessManipulationCompleted(ManipulationCompletedEventArgs e);

		/// <summary>
		/// Handles the event after the default handler
		/// </summary>
		/// <param name="e">Event args</param>
		void PostprocessManipulationDelta(ManipulationDeltaEventArgs e);

		/// <summary>
		/// Handles the event after the default handler
		/// </summary>
		/// <param name="e">Event args</param>
		void PostprocessManipulationInertiaStarting(ManipulationInertiaStartingEventArgs e);

		/// <summary>
		/// Handles the event after the default handler
		/// </summary>
		/// <param name="e">Event args</param>
		void PostprocessManipulationStarting(ManipulationStartingEventArgs e);

		/// <summary>
		/// Handles the event after the default handler
		/// </summary>
		/// <param name="e">Event args</param>
		void PostprocessStylusSystemGesture(StylusSystemGestureEventArgs e);

		/// <summary>
		/// Handles the event after the default handler
		/// </summary>
		/// <param name="e">Event args</param>
		void PostprocessTouchDown(TouchEventArgs e);

		/// <summary>
		/// Handles the event after the default handler
		/// </summary>
		/// <param name="e">Event args</param>
		void PostprocessTouchUp(TouchEventArgs e);
	}
}
