/*
    Copyright (C) 2014-2019 de4dot@gmail.com

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
using System.Collections.ObjectModel;
using Microsoft.VisualStudio.Text.Formatting;

namespace dnSpy.Text.Formatting {
#pragma warning disable CS8644 // Type does not implement interface member. Nullability of reference types in interface implemented by the base type doesn't match.
	sealed class TextAndAdornmentCollection : ReadOnlyCollection<ISequenceElement>, ITextAndAdornmentCollection {
#pragma warning restore CS8644 // Type does not implement interface member. Nullability of reference types in interface implemented by the base type doesn't match.
		public ITextAndAdornmentSequencer Sequencer { get; }

		public TextAndAdornmentCollection(ITextAndAdornmentSequencer textAndAdornmentSequencer, IList<ISequenceElement> list)
			: base(list) => Sequencer = textAndAdornmentSequencer ?? throw new ArgumentNullException(nameof(textAndAdornmentSequencer));
	}
}
