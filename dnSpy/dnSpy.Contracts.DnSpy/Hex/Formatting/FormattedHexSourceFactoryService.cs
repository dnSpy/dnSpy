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

using dnSpy.Contracts.Hex.Classification;
using Microsoft.VisualStudio.Text.Classification;

namespace dnSpy.Contracts.Hex.Formatting {
	/// <summary>
	/// Formatted hex source factory service
	/// </summary>
	public abstract class FormattedHexSourceFactoryService {
		/// <summary>
		/// Constructor
		/// </summary>
		protected FormattedHexSourceFactoryService() { }

		/// <summary>
		/// Creates a <see cref="HexFormattedLineSource"/> that doesn't classify anything
		/// </summary>
		/// <param name="bufferLines">Buffer lines</param>
		/// <param name="baseIndent">Base indentation</param>
		/// <param name="useDisplayMode">true to use display mode, false to use ideal mode</param>
		/// <param name="sequencer">Sequencer</param>
		/// <param name="classificationFormatMap">Classification format map</param>
		/// <returns></returns>
		public virtual HexFormattedLineSource Create(HexBufferLineProvider bufferLines, double baseIndent, bool useDisplayMode, HexAndAdornmentSequencer sequencer, IClassificationFormatMap classificationFormatMap) =>
			Create(bufferLines, baseIndent, useDisplayMode, NullHexClassifier.Instance, sequencer, classificationFormatMap);

		/// <summary>
		/// Creates a <see cref="HexFormattedLineSource"/>
		/// </summary>
		/// <param name="bufferLines">Buffer lines</param>
		/// <param name="baseIndent">Base indentation</param>
		/// <param name="useDisplayMode">true to use display mode, false to use ideal mode</param>
		/// <param name="aggregateClassifier">Classifier</param>
		/// <param name="sequencer">Sequencer</param>
		/// <param name="classificationFormatMap">Classification format map</param>
		/// <returns></returns>
		public abstract HexFormattedLineSource Create(HexBufferLineProvider bufferLines, double baseIndent, bool useDisplayMode, HexClassifier aggregateClassifier, HexAndAdornmentSequencer sequencer, IClassificationFormatMap classificationFormatMap);
	}
}
