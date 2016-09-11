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

using System.Windows;
using System.Windows.Controls;
using Microsoft.VisualStudio.Utilities;

namespace dnSpy.Contracts.Language.Intellisense {
	/// <summary>
	/// Creates <see cref="UIElement"/>s. Export it with a <see cref="NameAttribute"/>
	/// and a <see cref="ContentTypeAttribute"/>. Optional: <see cref="OrderAttribute"/>.
	/// </summary>
	/// <typeparam name="TItem">Source item, eg. <see cref="Completion"/></typeparam>
	/// <typeparam name="TContext">Context, eg. <see cref="ICompletionSession"/></typeparam>
	interface IUIElementProvider<TItem, TContext> {
		/// <summary>
		/// Creates a <see cref="UIElement"/> or returns null
		/// </summary>
		/// <param name="itemToRender">Item to render</param>
		/// <param name="context">Context</param>
		/// <param name="elementType">Requested <see cref="UIElement"/> type</param>
		/// <returns></returns>
		UIElement GetUIElement(TItem itemToRender, TContext context, UIElementType elementType);
	}

	/// <summary>
	/// <see cref="UIElement"/> type
	/// </summary>
	enum UIElementType {
		/// <summary>
		/// Small element, eg. a <see cref="ListBoxItem"/>
		/// </summary>
		Small,

		/// <summary>
		/// Large element, eg. a <see cref="ListBox"/>
		/// </summary>
		Large,

		/// <summary>
		/// Tooltip element
		/// </summary>
		Tooltip
	}
}
