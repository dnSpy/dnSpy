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
using System.ComponentModel.Composition;
using dnSpy.Contracts.Hex.Classification;
using dnSpy.Contracts.Hex.Formatting;
using dnSpy.Text.Formatting;
using Microsoft.VisualStudio.Text.Classification;

namespace dnSpy.Hex.Formatting {
	[Export(typeof(FormattedHexSourceFactoryService))]
	sealed class FormattedHexSourceFactoryServiceImpl : FormattedHexSourceFactoryService {
		readonly ITextFormatterProvider textFormatterProvider;

		[ImportingConstructor]
		FormattedHexSourceFactoryServiceImpl(ITextFormatterProvider textFormatterProvider) {
			this.textFormatterProvider = textFormatterProvider;
		}

		public override HexFormattedLineSource Create(double baseIndent, bool useDisplayMode, HexClassifier aggregateClassifier, HexAndAdornmentSequencer sequencer, IClassificationFormatMap classificationFormatMap) {
			if (aggregateClassifier == null)
				throw new ArgumentNullException(nameof(aggregateClassifier));
			if (sequencer == null)
				throw new ArgumentNullException(nameof(sequencer));
			if (classificationFormatMap == null)
				throw new ArgumentNullException(nameof(classificationFormatMap));
			return new HexFormattedLineSourceImpl(textFormatterProvider, baseIndent, useDisplayMode, aggregateClassifier, sequencer, classificationFormatMap);
		}
	}
}
