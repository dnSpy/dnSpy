/*
    Copyright (C) 2014-2017 de4dot@gmail.com

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
using System.Collections.Generic;
using System.Threading;
using Microsoft.VisualStudio.Utilities;

namespace dnSpy.Contracts.Text.Classification {
	/// <summary>
	/// <see cref="ITextClassifier"/> context
	/// </summary>
	public class TextClassifierContext : IPropertyOwner {
		/// <summary>
		/// Gets the properties
		/// </summary>
		public PropertyCollection Properties {
			get {
				if (properties == null)
					Interlocked.CompareExchange(ref properties, new PropertyCollection(), null);
				return properties;
			}
		}
		PropertyCollection properties;

		/// <summary>
		/// Gets the text to classify
		/// </summary>
		public string Text { get; }

		/// <summary>
		/// Tag, see <see cref="PredefinedTextClassifierTags"/>
		/// </summary>
		public string Tag { get; }

		/// <summary>
		/// true if it should be colorized. Only special classifiers can ignore this, eg. highlighters
		/// </summary>
		public bool Colorize { get; }

		/// <summary>
		/// Default colors, can be empty and there could be non-classified parts
		/// </summary>
		public IReadOnlyCollection<SpanData<object>> Colors { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="text">Text to classify</param>
		/// <param name="tag">Tag (<see cref="PredefinedTextClassifierTags"/>), can be null</param>
		/// <param name="colorize">true if it should be colorized. Only special classifiers can ignore this, eg. highlighters</param>
		/// <param name="colors">Default colors or null (see <see cref="TextClassifierTextColorWriter"/>)</param>
		public TextClassifierContext(string text, string tag, bool colorize, IReadOnlyCollection<SpanData<object>> colors = null) {
			Text = text ?? throw new ArgumentNullException(nameof(text));
			Tag = tag ?? string.Empty;
			Colorize = colorize;
			Colors = colors ?? Array.Empty<SpanData<object>>();
		}
	}
}
