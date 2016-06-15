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

using System.Collections.Generic;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.Text.Tagging;

namespace dnSpy.Text.Tagging {
	sealed class TextBufferTagAggregator<T> : TagAggregatorBase<T> where T : ITag {
		readonly ITaggerFactory taggerFactory;

		public TextBufferTagAggregator(ITaggerFactory taggerFactory, ITextBuffer textBuffer, TagAggregatorOptions options)
			: base(textBuffer, options) {
			this.taggerFactory = taggerFactory;
		}

		protected override IEnumerable<ITagger<T>> CreateTaggers() => taggerFactory.Create<T>(TextBuffer, TextBuffer.ContentType);
	}
}
