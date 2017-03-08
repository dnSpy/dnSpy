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
using System.ComponentModel.Composition;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Text;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace dnSpy.Debugger.ToolWindows.Threads {
	[Export(typeof(ITaggerProvider))]
	[TagType(typeof(IClassificationTag))]
	[ContentType(ContentTypes.ThreadsWindowName)]
	sealed class ThreadNameTaggerProvider : ITaggerProvider {
		readonly IClassificationTag nameClassificationTag;
		readonly DebuggerSettings debuggerSettings;

		[ImportingConstructor]
		ThreadNameTaggerProvider(IClassificationTypeRegistryService classificationTypeRegistryService, DebuggerSettings debuggerSettings) {
			nameClassificationTag = new ClassificationTag(classificationTypeRegistryService.GetClassificationType(ThreadFormatter.NameColorClassificationTypeName));
			this.debuggerSettings = debuggerSettings;
		}

		public ITagger<T> CreateTagger<T>(ITextBuffer buffer) where T : ITag =>
			new ThreadNameTagger(nameClassificationTag, debuggerSettings) as ITagger<T>;
	}

	sealed class ThreadNameTagger : ITagger<IClassificationTag> {
		public event EventHandler<SnapshotSpanEventArgs> TagsChanged { add { } remove { } }
		readonly IClassificationTag nameClassificationTag;
		readonly DebuggerSettings debuggerSettings;

		public ThreadNameTagger(IClassificationTag nameClassificationTag, DebuggerSettings debuggerSettings) {
			this.nameClassificationTag = nameClassificationTag;
			this.debuggerSettings = debuggerSettings;
		}

		public IEnumerable<ITagSpan<IClassificationTag>> GetTags(NormalizedSnapshotSpanCollection spans) {
			if (!debuggerSettings.SyntaxHighlight)
				yield break;
			foreach (var span in spans)
				yield return new TagSpan<IClassificationTag>(span, nameClassificationTag);
		}
	}
}
