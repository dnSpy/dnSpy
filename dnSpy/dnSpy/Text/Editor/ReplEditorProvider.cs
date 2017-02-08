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

using System.ComponentModel.Composition;
using dnSpy.Contracts.MVVM;
using dnSpy.Contracts.Text.Classification;
using dnSpy.Contracts.Text.Editor;
using dnSpy.Contracts.Text.Editor.Operations;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Utilities;

namespace dnSpy.Text.Editor {
	[Export(typeof(IReplEditorProvider))]
	sealed class ReplEditorProvider : IReplEditorProvider {
		readonly IDsTextEditorFactoryService dsTextEditorFactoryService;
		readonly IContentTypeRegistryService contentTypeRegistryService;
		readonly ITextBufferFactoryService textBufferFactoryService;
		readonly IEditorOperationsFactoryService editorOperationsFactoryService;
		readonly IEditorOptionsFactoryService editorOptionsFactoryService;
		readonly IClassificationTypeRegistryService classificationTypeRegistryService;
		readonly IThemeClassificationTypeService themeClassificationTypeService;
		readonly IPickSaveFilename pickSaveFilename;
		readonly ITextViewUndoManagerProvider textViewUndoManagerProvider;

		[ImportingConstructor]
		ReplEditorProvider(IDsTextEditorFactoryService dsTextEditorFactoryService, IContentTypeRegistryService contentTypeRegistryService, ITextBufferFactoryService textBufferFactoryService, IEditorOperationsFactoryService editorOperationsFactoryService, IEditorOptionsFactoryService editorOptionsFactoryService, IClassificationTypeRegistryService classificationTypeRegistryService, IThemeClassificationTypeService themeClassificationTypeService, IPickSaveFilename pickSaveFilename, ITextViewUndoManagerProvider textViewUndoManagerProvider) {
			this.dsTextEditorFactoryService = dsTextEditorFactoryService;
			this.contentTypeRegistryService = contentTypeRegistryService;
			this.textBufferFactoryService = textBufferFactoryService;
			this.editorOperationsFactoryService = editorOperationsFactoryService;
			this.editorOptionsFactoryService = editorOptionsFactoryService;
			this.classificationTypeRegistryService = classificationTypeRegistryService;
			this.themeClassificationTypeService = themeClassificationTypeService;
			this.pickSaveFilename = pickSaveFilename;
			this.textViewUndoManagerProvider = textViewUndoManagerProvider;
		}

		public IReplEditor Create(ReplEditorOptions options) => new ReplEditor(options, dsTextEditorFactoryService, contentTypeRegistryService, textBufferFactoryService, editorOperationsFactoryService, editorOptionsFactoryService, classificationTypeRegistryService, themeClassificationTypeService, pickSaveFilename, textViewUndoManagerProvider);
	}
}
