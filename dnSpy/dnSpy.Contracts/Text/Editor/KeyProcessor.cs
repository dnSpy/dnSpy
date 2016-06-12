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
	/// Key processor
	/// </summary>
	public abstract class KeyProcessor {
		/// <summary>
		/// Constructor
		/// </summary>
		protected KeyProcessor() { }

		/// <summary>
		/// Determines whether this processor should be called for events that have been handled by earlier <see cref="KeyProcessor"/> objects.
		/// </summary>
		public virtual bool IsInterestedInHandledEvents => false;

		/// <summary>
		/// Handles the KeyDown event
		/// </summary>
		/// <param name="args">Event arguments</param>
		public virtual void KeyDown(KeyEventArgs args) { }

		/// <summary>
		/// Handles the KeyUp event
		/// </summary>
		/// <param name="args">Event arguments</param>
		public virtual void KeyUp(KeyEventArgs args) { }

		/// <summary>
		/// Handles the PreviewKeyDown event
		/// </summary>
		/// <param name="args">Event arguments</param>
		public virtual void PreviewKeyDown(KeyEventArgs args) { }

		/// <summary>
		/// Handles the PreviewKeyUp event
		/// </summary>
		/// <param name="args">Event arguments</param>
		public virtual void PreviewKeyUp(KeyEventArgs args) { }

		/// <summary>
		/// Handles the TextInput event
		/// </summary>
		/// <param name="args">Event arguments</param>
		public virtual void TextInput(TextCompositionEventArgs args) { }

		/// <summary>
		/// Handles the TextInputStart event
		/// </summary>
		/// <param name="args">Event arguments</param>
		public virtual void TextInputStart(TextCompositionEventArgs args) { }

		/// <summary>
		/// Handles the TextInputUpdate event
		/// </summary>
		/// <param name="args">Event arguments</param>
		public virtual void TextInputUpdate(TextCompositionEventArgs args) { }

		/// <summary>
		/// Handles the PreviewTextInput event
		/// </summary>
		/// <param name="args">Event arguments</param>
		public virtual void PreviewTextInput(TextCompositionEventArgs args) { }

		/// <summary>
		/// Handles the PreviewTextInputStart event
		/// </summary>
		/// <param name="args">Event arguments</param>
		public virtual void PreviewTextInputStart(TextCompositionEventArgs args) { }

		/// <summary>
		/// Handles the PreviewTextInputUpdate event
		/// </summary>
		/// <param name="args">Event arguments</param>
		public virtual void PreviewTextInputUpdate(TextCompositionEventArgs args) { }
	}
}
