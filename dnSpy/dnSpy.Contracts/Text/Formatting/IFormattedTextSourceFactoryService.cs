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

using dnSpy.Contracts.Text.Classification;

namespace dnSpy.Contracts.Text.Formatting {
	/// <summary>
	/// <see cref="IFormattedLineSource"/> factory service
	/// </summary>
	public interface IFormattedTextSourceFactoryService {
		/// <summary>
		/// Creates a new <see cref="IFormattedLineSource"/>
		/// </summary>
		/// <param name="sourceTextSnapshot">Source text snapshot</param>
		/// <param name="visualBufferSnapshot">Visual buffer snapshot</param>
		/// <param name="tabSize">Number of spaces between each tab stop</param>
		/// <param name="baseIndent">Base indentatino for all lines</param>
		/// <param name="wordWrapWidth">Word wrap width</param>
		/// <param name="maxAutoIndent">Max auto indent size</param>
		/// <param name="useDisplayMode">true to use display mode</param>
		/// <param name="sequencer">The text and adornment sequencer for the view. If null, there are no space negotiating adornments</param>
		/// <param name="classificationFormatMap">The classification format map to use while formatting text</param>
		/// <returns></returns>
		IFormattedLineSource Create(ITextSnapshot sourceTextSnapshot, ITextSnapshot visualBufferSnapshot, int tabSize, double baseIndent, double wordWrapWidth, double maxAutoIndent, bool useDisplayMode, ITextAndAdornmentSequencer sequencer, IClassificationFormatMap classificationFormatMap);

		/// <summary>
		/// Creates a new <see cref="IFormattedLineSource"/>
		/// </summary>
		/// <param name="sourceTextSnapshot">Source text snapshot</param>
		/// <param name="visualBufferSnapshot">Visual buffer snapshot</param>
		/// <param name="tabSize">Number of spaces between each tab stop</param>
		/// <param name="baseIndent">Base indentatino for all lines</param>
		/// <param name="wordWrapWidth">Word wrap width</param>
		/// <param name="maxAutoIndent">Max auto indent size</param>
		/// <param name="useDisplayMode">true to use display mode</param>
		/// <param name="aggregateClassifier">The aggregate of all classifiers on the view</param>
		/// <param name="sequencer">The text and adornment sequencer for the view. If null, there are no space negotiating adornments</param>
		/// <param name="classificationFormatMap">The classification format map to use while formatting text</param>
		/// <returns></returns>
		IFormattedLineSource Create(ITextSnapshot sourceTextSnapshot, ITextSnapshot visualBufferSnapshot, int tabSize, double baseIndent, double wordWrapWidth, double maxAutoIndent, bool useDisplayMode, IClassifier aggregateClassifier, ITextAndAdornmentSequencer sequencer, IClassificationFormatMap classificationFormatMap);

		/// <summary>
		/// Creates a new <see cref="IFormattedLineSource"/>
		/// </summary>
		/// <param name="sourceTextSnapshot">Source text snapshot</param>
		/// <param name="visualBufferSnapshot">Visual buffer snapshot</param>
		/// <param name="tabSize">Number of spaces between each tab stop</param>
		/// <param name="baseIndent">Base indentatino for all lines</param>
		/// <param name="wordWrapWidth">Word wrap width</param>
		/// <param name="maxAutoIndent">Max auto indent size</param>
		/// <param name="useDisplayMode">true to use display mode</param>
		/// <param name="aggregateClassifier">The aggregate of all classifiers on the view</param>
		/// <param name="sequencer">The text and adornment sequencer for the view. If null, there are no space negotiating adornments</param>
		/// <param name="classificationFormatMap">The classification format map to use while formatting text</param>
		/// <param name="isViewWrapEnabled">true if word wrap glyphs are enabled for wrapped lines</param>
		/// <returns></returns>
		IFormattedLineSource Create(ITextSnapshot sourceTextSnapshot, ITextSnapshot visualBufferSnapshot, int tabSize, double baseIndent, double wordWrapWidth, double maxAutoIndent, bool useDisplayMode, IClassifier aggregateClassifier, ITextAndAdornmentSequencer sequencer, IClassificationFormatMap classificationFormatMap, bool isViewWrapEnabled);
	}
}
