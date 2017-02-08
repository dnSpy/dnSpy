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
using System.Collections.Generic;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;

namespace dnSpy.Text.Tagging {
	sealed class TextViewTagAggregator<T> : TagAggregatorBase<T> where T : ITag {
		readonly ITaggerFactory taggerFactory;
		readonly ITextView textView;

		public TextViewTagAggregator(ITaggerFactory taggerFactory, ITextView textView, TagAggregatorOptions options)
			: base(textView.BufferGraph, textView.TextBuffer, options) {
			this.taggerFactory = taggerFactory;
			this.textView = textView;
			textView.Closed += TextView_Closed;
			Initialize();
		}

		void TextView_Closed(object sender, EventArgs e) => Dispose();

		public override void Dispose() {
			base.Dispose();
			textView.Closed -= TextView_Closed;
		}

		protected override bool CanRaiseBatchedTagsChanged => !textView.InLayout;
		protected override IEnumerable<ITagger<T>> CreateTaggers() => taggerFactory.Create<T>(textView, TextBuffer, TextBuffer.ContentType);
	}
}
