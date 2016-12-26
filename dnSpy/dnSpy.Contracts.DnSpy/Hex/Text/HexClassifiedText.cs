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

namespace dnSpy.Contracts.Hex.Text {
	/// <summary>
	/// Classified text
	/// </summary>
	public struct HexClassifiedText {
		/// <summary>
		/// Text
		/// </summary>
		public string Text { get; }

		/// <summary>
		/// Tag, see <see cref="PredefinedClassifiedTextTags"/>
		/// </summary>
		public string Tag { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="text">Text</param>
		/// <param name="tag">Tag, see <see cref="PredefinedClassifiedTextTags"/></param>
		public HexClassifiedText(string text, string tag) {
			if (text == null)
				throw new ArgumentNullException(nameof(text));
			if (tag == null)
				throw new ArgumentNullException(nameof(tag));
			Text = text;
			Tag = tag;
		}
	}
}
