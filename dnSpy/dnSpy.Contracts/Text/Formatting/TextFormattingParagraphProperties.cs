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
using System.Windows.Media.TextFormatting;

namespace dnSpy.Contracts.Text.Formatting {
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
	public class TextFormattingParagraphProperties : TextParagraphProperties {
		readonly double defaultIncrementalTab;
		readonly TextRunProperties defaultTextRunProperties;

		public override double DefaultIncrementalTab => defaultIncrementalTab;
		public override TextRunProperties DefaultTextRunProperties => defaultTextRunProperties;
		public override bool FirstLineInParagraph => false;
		public override FlowDirection FlowDirection => FlowDirection.LeftToRight;
		public override double Indent => 0;
		public sealed override double LineHeight => 0;
		public override TextAlignment TextAlignment => TextAlignment.Left;
		public override TextMarkerProperties TextMarkerProperties => null;
		public sealed override TextWrapping TextWrapping => TextWrapping.Wrap;

		public TextFormattingParagraphProperties(TextFormattingRunProperties defaultTextRunProperties) {
			if (defaultTextRunProperties == null)
				throw new ArgumentNullException(nameof(defaultTextRunProperties));
			this.defaultTextRunProperties = defaultTextRunProperties;
			this.defaultIncrementalTab = defaultTextRunProperties.FontRenderingEmSize * 4;
		}

		public TextFormattingParagraphProperties(TextFormattingRunProperties defaultTextRunProperties, double defaultTabSize) {
			if (defaultTextRunProperties == null)
				throw new ArgumentNullException(nameof(defaultTextRunProperties));
			this.defaultTextRunProperties = defaultTextRunProperties;
			this.defaultIncrementalTab = defaultTabSize;
		}
	}
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}
