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
using System.Windows.Media.TextFormatting;

namespace dnSpy.Shared.HexEditor {
	sealed class HexTextParagraphProperties : TextParagraphProperties {
		public TextRunProperties _DefaultTextRunProperties;
		public bool _FirstLineInParagraph = false;
		public FlowDirection _FlowDirection = 0;
		public double _Indent = 0;
		public double _LineHeight = 0;
		public TextAlignment _TextAlignment = 0;
		public TextMarkerProperties _TextMarkerProperties = null;
		public TextWrapping _TextWrapping;

		public override TextRunProperties DefaultTextRunProperties => _DefaultTextRunProperties;
		public override bool FirstLineInParagraph => _FirstLineInParagraph;
		public override FlowDirection FlowDirection => _FlowDirection;
		public override double Indent => _Indent;
		public override double LineHeight => _LineHeight;
		public override TextAlignment TextAlignment => _TextAlignment;
		public override TextMarkerProperties TextMarkerProperties => _TextMarkerProperties;
		public override TextWrapping TextWrapping => _TextWrapping;
	}
}
