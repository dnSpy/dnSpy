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
using System.ComponentModel.Composition;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.TreeView;
using dnSpy.Debugger.UI;
using dnSpy.Debugger.UI.Wpf;
using Microsoft.VisualStudio.Text.Classification;

namespace dnSpy.Debugger.Evaluation.ViewModel.Impl {
	[Export(typeof(ValueNodesVMFactory))]
	sealed class ValueNodesVMFactoryImpl : ValueNodesVMFactory {
		readonly UIDispatcher uiDispatcher;
		readonly ITreeViewService treeViewService;
		readonly LanguageEditValueProviderFactory languageEditValueProviderFactory;
		readonly DbgValueNodeImageReferenceService dbgValueNodeImageReferenceService;
		readonly DebuggerSettings debuggerSettings;
		readonly DbgEvalFormatterSettings dbgEvalFormatterSettings;
		readonly IClassificationFormatMapService classificationFormatMapService;
		readonly ITextBlockContentInfoFactory textBlockContentInfoFactory;

		[ImportingConstructor]
		ValueNodesVMFactoryImpl(UIDispatcher uiDispatcher, ITreeViewService treeViewService, LanguageEditValueProviderFactory languageEditValueProviderFactory, DbgValueNodeImageReferenceService dbgValueNodeImageReferenceService, DebuggerSettings debuggerSettings, DbgEvalFormatterSettings dbgEvalFormatterSettings, IClassificationFormatMapService classificationFormatMapService, ITextBlockContentInfoFactory textBlockContentInfoFactory) {
			uiDispatcher.VerifyAccess();
			this.uiDispatcher = uiDispatcher;
			this.treeViewService = treeViewService;
			this.languageEditValueProviderFactory = languageEditValueProviderFactory;
			this.dbgValueNodeImageReferenceService = dbgValueNodeImageReferenceService;
			this.debuggerSettings = debuggerSettings;
			this.dbgEvalFormatterSettings = dbgEvalFormatterSettings;
			this.classificationFormatMapService = classificationFormatMapService;
			this.textBlockContentInfoFactory = textBlockContentInfoFactory;
		}

		public override IValueNodesVM Create(ValueNodesVMOptions options) {
			if (options == null)
				throw new ArgumentNullException(nameof(options));
			if (options.NodesProvider == null)
				throw new ArgumentException();
			if (options.WindowContentType == null)
				throw new ArgumentException();
			if (options.NameColumnName == null)
				throw new ArgumentException();
			if (options.ValueColumnName == null)
				throw new ArgumentException();
			if (options.TypeColumnName == null)
				throw new ArgumentException();
			if (options.ShowMessageBox == null)
				throw new ArgumentException();
			return new ValueNodesVM(uiDispatcher, options, treeViewService, languageEditValueProviderFactory, dbgValueNodeImageReferenceService, debuggerSettings, dbgEvalFormatterSettings, classificationFormatMapService, textBlockContentInfoFactory);
		}
	}
}
