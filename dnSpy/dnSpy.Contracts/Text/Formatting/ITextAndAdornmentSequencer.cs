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

namespace dnSpy.Contracts.Text.Formatting {
	/// <summary>
	/// Creates a sequence of text and adornment elements to compose an <see cref="ITextSnapshotLine"/>
	/// </summary>
	public interface ITextAndAdornmentSequencer {
		/// <summary>
		/// Occurs when there has been a change in the data used by the sequencer
		/// </summary>
		event EventHandler<TextAndAdornmentSequenceChangedEventArgs> SequenceChanged;

		/// <summary>
		/// Gets the source buffer of the buffer graph
		/// </summary>
		ITextBuffer SourceBuffer { get; }

		/// <summary>
		/// Gets the top <see cref="ITextBuffer"/> in the buffer graph
		/// </summary>
		ITextBuffer TopBuffer { get; }

		/// <summary>
		/// Creates a sequence of text and adornment elements that compose the specified <see cref="ITextSnapshotLine"/>
		/// </summary>
		/// <param name="topLine">The <see cref="ITextSnapshotLine"/> to sequence</param>
		/// <param name="sourceTextSnapshot">The <see cref="ITextSnapshot"/> of the <see cref="TopBuffer"/> that corresponds to <paramref name="topLine"/></param>
		/// <returns></returns>
		ITextAndAdornmentCollection CreateTextAndAdornmentCollection(ITextSnapshotLine topLine, ITextSnapshot sourceTextSnapshot);

		/// <summary>
		/// Creates a sequence of text and adornment elements that compose the specified <see cref="SnapshotSpan"/>
		/// </summary>
		/// <param name="topSpan">The <see cref="SnapshotSpan"/> in the <see cref="TopBuffer"/> to sequence</param>
		/// <param name="sourceTextSnapshot">The <see cref="ITextSnapshot"/> of the <see cref="SourceBuffer"/> that corresponds to <paramref name="topSpan"/></param>
		/// <returns></returns>
		ITextAndAdornmentCollection CreateTextAndAdornmentCollection(SnapshotSpan topSpan, ITextSnapshot sourceTextSnapshot);
	}
}
