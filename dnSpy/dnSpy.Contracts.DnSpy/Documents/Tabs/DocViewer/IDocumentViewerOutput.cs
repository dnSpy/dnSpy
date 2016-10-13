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
using System.Windows;
using dnSpy.Contracts.Decompiler;

namespace dnSpy.Contracts.Documents.Tabs.DocViewer {
	/// <summary>
	/// dnSpy text output
	/// </summary>
	public interface IDocumentViewerOutput : IDecompilerOutput {
		/// <summary>
		/// true if the output can be cached
		/// </summary>
		bool CanBeCached { get; }

		/// <summary>
		/// Called to disable caching of the result
		/// </summary>
		void DisableCaching();

		/// <summary>
		/// Adds a UI element
		/// </summary>
		/// <param name="createElement">Creates the UI element. Only called on the UI thread</param>
		void AddUIElement(Func<UIElement> createElement);

		/// <summary>
		/// Adds a button
		/// </summary>
		/// <param name="buttonText">Button text</param>
		/// <param name="clickHandler">Button click handler</param>
		void AddButton(string buttonText, RoutedEventHandler clickHandler);
	}
}
