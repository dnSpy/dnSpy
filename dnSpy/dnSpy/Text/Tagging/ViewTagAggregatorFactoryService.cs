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
using dnSpy.Contracts.Text.Editor;
using dnSpy.Contracts.Text.Tagging;

namespace dnSpy.Text.Tagging {
	[Export(typeof(IViewTagAggregatorFactoryService))]
	sealed class ViewTagAggregatorFactoryService : IViewTagAggregatorFactoryService {
		readonly ITaggerFactory taggerFactory;

		[ImportingConstructor]
		ViewTagAggregatorFactoryService(ITaggerFactory taggerFactory) {
			this.taggerFactory = taggerFactory;
		}

		public ITagAggregator<T> CreateTagAggregator<T>(ITextView textView) where T : ITag => CreateTagAggregator<T>(textView, TagAggregatorOptions.None);
		public ITagAggregator<T> CreateTagAggregator<T>(ITextView textView, TagAggregatorOptions options) where T : ITag {
			if (textView == null)
				throw new ArgumentNullException(nameof(textView));
			return new TextViewTagAggregator<T>(taggerFactory, textView, options);
		}
	}
}
