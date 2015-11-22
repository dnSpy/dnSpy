/*
    Copyright (C) 2014-2015 de4dot@gmail.com

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
using System.Windows.Media;

namespace dnSpy.Files.Tabs.TextEditor {
	/// <summary>
	/// Z-order of eg. icons in the icon bar
	/// </summary>
	enum TextLineObjectZOrder {
		Breakpoint,
		ReturnStatement,
		SelectedReturnStatement,
		CurrentStatement,
		SearchResult,
	}

	class TextLineObjectEventArgs : EventArgs {
		/// <summary>
		/// The object needs to be redrawn
		/// </summary>
		public static readonly string RedrawProperty = "Redraw";

		public string Property { get; private set; }

		public TextLineObjectEventArgs(string property) {
			this.Property = property;
		}
	}

	interface ITextLineObject {
		double ZOrder { get; }
		bool IsVisible(TextEditorControl textView);
		event EventHandler<TextLineObjectEventArgs> ObjPropertyChanged;
	}

	/// <summary>
	/// Add an icon in the icon bar
	/// </summary>
	interface IIconBarObject : ITextLineObject {
		int GetLineNumber(TextEditorControl textView);
		bool HasImage { get; }
		ImageSource GetImage(Color bgColor);
	}

	/// <summary>
	/// Mark text in the text editor
	/// </summary>
	interface ITextMarkerObject : ITextLineObject {
		ITextMarker CreateMarker(TextEditorControl textView, ITextMarkerService markerService);
	}

	/// <summary>
	/// Implemented by code breakpoints, call stack lines, current line and any other classes that
	/// must have an icon in the icon bar and always-marked text in the text editor
	/// </summary>
	interface IMarkedTextLine : IIconBarObject, ITextMarkerObject {
	}
}
