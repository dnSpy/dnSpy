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

namespace dnSpy.Contracts.Hex.Editor {
	/// <summary>
	/// Keyboard processor
	/// </summary>
	public abstract class HexKeyProcessor {
		/// <summary>
		/// Constructor
		/// </summary>
		protected HexKeyProcessor() { }

		/// <summary>
		/// true if the instance is interested in handled events. Default value is false.
		/// </summary>
		public virtual bool IsInterestedInHandledEvents => false;

		/// <summary>
		/// Key down handler
		/// </summary>
		/// <param name="args">Key event args</param>
		public virtual void KeyDown(KeyEventArgs args) { }

		/// <summary>
		/// Key up handler
		/// </summary>
		/// <param name="args">Key event args</param>
		public virtual void KeyUp(KeyEventArgs args) { }

		/// <summary>
		/// Text input handler
		/// </summary>
		/// <param name="args">Key event args</param>
		public virtual void TextInput(TextCompositionEventArgs args) { }

		/// <summary>
		/// Text input start handler
		/// </summary>
		/// <param name="args">Key event args</param>
		public virtual void TextInputStart(TextCompositionEventArgs args) { }

		/// <summary>
		/// Text input update handler
		/// </summary>
		/// <param name="args">Key event args</param>
		public virtual void TextInputUpdate(TextCompositionEventArgs args) { }

		/// <summary>
		/// Preview key down handler
		/// </summary>
		/// <param name="args">Key event args</param>
		public virtual void PreviewKeyDown(KeyEventArgs args) { }

		/// <summary>
		/// Preview key up handler
		/// </summary>
		/// <param name="args">Key event args</param>
		public virtual void PreviewKeyUp(KeyEventArgs args) { }

		/// <summary>
		/// Preview text input handler
		/// </summary>
		/// <param name="args">Key event args</param>
		public virtual void PreviewTextInput(TextCompositionEventArgs args) { }

		/// <summary>
		/// Preview text input start handler
		/// </summary>
		/// <param name="args">Key event args</param>
		public virtual void PreviewTextInputStart(TextCompositionEventArgs args) { }

		/// <summary>
		/// Preview text input update handler
		/// </summary>
		/// <param name="args">Key event args</param>
		public virtual void PreviewTextInputUpdate(TextCompositionEventArgs args) { }
	}
}
