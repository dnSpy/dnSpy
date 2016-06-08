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
using dnSpy.Contracts.Text.Editor.Operations;

namespace dnSpy.Text.Editor.Operations {
	[Export(typeof(IEditorOperationsFactoryService))]
	sealed class EditorOperationsFactoryService : IEditorOperationsFactoryService {
		readonly ITextStructureNavigatorSelectorService textStructureNavigatorSelectorService;
		readonly ISmartIndentationService smartIndentationService;

		[ImportingConstructor]
		EditorOperationsFactoryService(ITextStructureNavigatorSelectorService textStructureNavigatorSelectorService, ISmartIndentationService smartIndentationService) {
			this.textStructureNavigatorSelectorService = textStructureNavigatorSelectorService;
			this.smartIndentationService = smartIndentationService;
		}

		public IEditorOperations2 GetEditorOperations(ITextView textView) {
			if (textView == null)
				throw new ArgumentNullException(nameof(textView));
			return textView.Properties.GetOrCreateSingletonProperty(typeof(IEditorOperations),
				() => new EditorOperations(textView, textStructureNavigatorSelectorService, smartIndentationService));
		}
	}
}
