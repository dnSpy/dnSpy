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
using dnSpy.Contracts.Text.Tagging;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Projection;
using Microsoft.VisualStudio.Text.Tagging;

namespace dnSpy.Text.Tagging {
	[Export(typeof(ISynchronousBufferTagAggregatorFactoryService))]
	[Export(typeof(IBufferTagAggregatorFactoryService))]
	sealed class BufferTagAggregatorFactoryService : ISynchronousBufferTagAggregatorFactoryService {
		readonly ITaggerFactory taggerFactory;
		readonly IBufferGraphFactoryService bufferGraphFactoryService;

		[ImportingConstructor]
		BufferTagAggregatorFactoryService(ITaggerFactory taggerFactory, IBufferGraphFactoryService bufferGraphFactoryService) {
			this.taggerFactory = taggerFactory;
			this.bufferGraphFactoryService = bufferGraphFactoryService;
		}

		public ITagAggregator<T> CreateTagAggregator<T>(ITextBuffer textBuffer) where T : ITag => CreateTagAggregator<T>(textBuffer, TagAggregatorOptions.None);
		public ITagAggregator<T> CreateTagAggregator<T>(ITextBuffer textBuffer, TagAggregatorOptions options) where T : ITag {
			if (textBuffer == null)
				throw new ArgumentNullException(nameof(textBuffer));
			return new TextBufferTagAggregator<T>(taggerFactory, bufferGraphFactoryService.CreateBufferGraph(textBuffer), textBuffer, options);
		}

		public ISynchronousTagAggregator<T> CreateSynchronousTagAggregator<T>(ITextBuffer textBuffer) where T : ITag => CreateSynchronousTagAggregator<T>(textBuffer, TagAggregatorOptions.None);
		public ISynchronousTagAggregator<T> CreateSynchronousTagAggregator<T>(ITextBuffer textBuffer, TagAggregatorOptions options) where T : ITag {
			if (textBuffer == null)
				throw new ArgumentNullException(nameof(textBuffer));
			return new TextBufferTagAggregator<T>(taggerFactory, bufferGraphFactoryService.CreateBufferGraph(textBuffer), textBuffer, options);
		}
	}
}
