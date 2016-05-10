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

using dnSpy.Contracts.TextEditor;
using dnSpy.Contracts.Themes;

namespace dnSpy.Shared.TextEditor {
	public struct ColorInfo {
		public Span Span { get; }
		public ITextColor Foreground { get; }
		public ITextColor Background { get; }
		public double Priority { get; }

		public ITextColor TextColor {
			get {
				if (Foreground == Background)
					return Foreground ?? Contracts.Themes.TextColor.Null;
				return new TextColor(Foreground?.Foreground, Background?.Background, Foreground?.FontWeight, Foreground?.FontStyle);
			}
		}

		public ColorInfo(Span span, ITextColor color, double priority) {
			Span = span;
			Foreground = color;
			Background = color;
			Priority = priority;
		}

		public ColorInfo(Span span, ITextColor fg, ITextColor bg, double priority) {
			Span = span;
			Foreground = fg;
			Background = bg;
			Priority = priority;
		}
	}
}
