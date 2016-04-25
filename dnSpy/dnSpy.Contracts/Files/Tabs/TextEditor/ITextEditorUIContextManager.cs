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

namespace dnSpy.Contracts.Files.Tabs.TextEditor {
	/// <summary>
	/// Called at various times
	/// </summary>
	/// <param name="event">Event</param>
	/// <param name="uiContext">Instance</param>
	/// <param name="data">Data, see <see cref="TextEditorUIContextListenerEvent"/></param>
	public delegate void TextEditorUIContextListener(TextEditorUIContextListenerEvent @event, ITextEditorUIContext uiContext, object data);

	/// <summary>
	/// Notifies listeners of <see cref="ITextEditorUIContext"/> events
	/// </summary>
	public interface ITextEditorUIContextManager {
		/// <summary>
		/// Adds a listener
		/// </summary>
		/// <param name="listener">Listener</param>
		/// <param name="order">Order, see constants in <see cref="TextEditorUIContextManagerConstants"/></param>
		void Add(TextEditorUIContextListener listener, double order = TextEditorUIContextManagerConstants.ORDER_DEFAULT);

		/// <summary>
		/// Removes a listener
		/// </summary>
		/// <param name="listener">Listener</param>
		void Remove(TextEditorUIContextListener listener);
	}
}
