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
using System.Windows;
using dnSpy.Contracts.Hex.Classification.DnSpy;
using CT = dnSpy.Contracts.Text;
using CTC = dnSpy.Contracts.Text.Classification;
using VSTC = Microsoft.VisualStudio.Text.Classification;

namespace dnSpy.Hex.Classification.DnSpy {
	sealed class HexTextElementCreatorImpl : HexTextElementCreator {
		public override CT.ITextColorWriter Writer => writer;
		public override bool IsEmpty => writer.Text.Length == 0;

		readonly CTC.ITextElementProvider textElementProvider;
		readonly VSTC.IClassificationFormatMap classificationFormatMap;
		readonly string contentType;
		readonly CTC.TextClassifierTextColorWriter writer;

		public HexTextElementCreatorImpl(CTC.ITextElementProvider textElementProvider, VSTC.IClassificationFormatMap classificationFormatMap, string contentType) {
			this.textElementProvider = textElementProvider ?? throw new ArgumentNullException(nameof(textElementProvider));
			this.classificationFormatMap = classificationFormatMap ?? throw new ArgumentNullException(nameof(classificationFormatMap));
			this.contentType = contentType ?? throw new ArgumentNullException(nameof(contentType));
			writer = new CTC.TextClassifierTextColorWriter();
		}

		public override FrameworkElement CreateTextElement(bool colorize, string tag) {
			var context = new CTC.TextClassifierContext(writer.Text, tag, colorize, writer.Colors);
			return textElementProvider.CreateTextElement(classificationFormatMap, context, contentType, CTC.TextElementFlags.Wrap);
		}
	}
}
