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
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using dnSpy.Contracts.Command;
using dnSpy.Contracts.Menus;
using dnSpy.Contracts.Text.Editor;
using dnSpy.Text.Formatting;
using dnSpy.Text.MEF;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;
using Microsoft.VisualStudio.Text.Operations;

namespace dnSpy.Text.Editor {
	[Export(typeof(ITextEditorFactoryService))]
	[Export(typeof(IDnSpyTextEditorFactoryService))]
	sealed class TextEditorFactoryService : IDnSpyTextEditorFactoryService {
		public event EventHandler<TextViewCreatedEventArgs> TextViewCreated;
		readonly ITextBufferFactoryService textBufferFactoryService;
		readonly IEditorOptionsFactoryService editorOptionsFactoryService;
		readonly ICommandManager commandManager;
		readonly ISmartIndentationService smartIndentationService;
		readonly Lazy<IWpfTextViewCreationListener, IDeferrableContentTypeAndTextViewRoleMetadata>[] wpfTextViewCreationListeners;
		readonly IFormattedTextSourceFactoryService formattedTextSourceFactoryService;
		readonly IViewClassifierAggregatorService viewClassifierAggregatorService;
		readonly ITextAndAdornmentSequencerFactoryService textAndAdornmentSequencerFactoryService;
		readonly IClassificationFormatMapService classificationFormatMapService;
		readonly IEditorFormatMapService editorFormatMapService;
		readonly IAdornmentLayerDefinitionService adornmentLayerDefinitionService;
		readonly ILineTransformProviderService lineTransformProviderService;
		readonly IWpfTextViewMarginProviderCollectionProvider wpfTextViewMarginProviderCollectionProvider;
		readonly IMenuManager menuManager;
		readonly IEditorOperationsFactoryService editorOperationsFactoryService;

		public ITextViewRoleSet AllPredefinedRoles => new TextViewRoleSet(allPredefinedRolesList);
		public ITextViewRoleSet DefaultRoles => new TextViewRoleSet(defaultRolesList);
		public ITextViewRoleSet NoRoles => new TextViewRoleSet(Array.Empty<string>());
		static readonly string[] allPredefinedRolesList = new string[] {
			PredefinedTextViewRoles.Analyzable,
			PredefinedTextViewRoles.Debuggable,
			PredefinedTextViewRoles.Document,
			PredefinedTextViewRoles.Editable,
			PredefinedTextViewRoles.Interactive,
			PredefinedTextViewRoles.PrimaryDocument,
			PredefinedTextViewRoles.Structured,
			PredefinedTextViewRoles.Zoomable,
		};
		static readonly string[] defaultRolesList = new string[] {
			PredefinedTextViewRoles.Analyzable,
			PredefinedTextViewRoles.Document,
			PredefinedTextViewRoles.Editable,
			PredefinedTextViewRoles.Interactive,
			PredefinedTextViewRoles.Structured,
			PredefinedTextViewRoles.Zoomable,
		};

		sealed class GuidObjectsProvider : IGuidObjectsProvider {
			readonly Func<GuidObjectsProviderArgs, IEnumerable<GuidObject>> createGuidObjects;
			readonly IGuidObjectsProvider guidObjectsProvider;
			internal IWpfTextView WpfTextView { get; set; }

			public GuidObjectsProvider(Func<GuidObjectsProviderArgs, IEnumerable<GuidObject>> createGuidObjects, IGuidObjectsProvider guidObjectsProvider) {
				this.createGuidObjects = createGuidObjects;
				this.guidObjectsProvider = guidObjectsProvider;
			}

			public IEnumerable<GuidObject> GetGuidObjects(GuidObjectsProviderArgs args) {
				Debug.Assert(WpfTextView != null);
				if (WpfTextView != null) {
					yield return new GuidObject(MenuConstants.GUIDOBJ_WPF_TEXTVIEW_GUID, WpfTextView);
					var loc = WpfTextView.GetTextEditorPosition(args.OpenedFromKeyboard);
					if (loc != null)
						yield return new GuidObject(MenuConstants.GUIDOBJ_TEXTEDITORPOSITION_GUID, loc);
				}

				if (createGuidObjects != null) {
					foreach (var guidObject in createGuidObjects(args))
						yield return guidObject;
				}

				if (guidObjectsProvider != null) {
					foreach (var guidObject in guidObjectsProvider.GetGuidObjects(args))
						yield return guidObject;
				}
			}
		}

		[ImportingConstructor]
		TextEditorFactoryService(ITextBufferFactoryService textBufferFactoryService, IEditorOptionsFactoryService editorOptionsFactoryService, ICommandManager commandManager, ISmartIndentationService smartIndentationService, [ImportMany] IEnumerable<Lazy<IWpfTextViewCreationListener, IDeferrableContentTypeAndTextViewRoleMetadata>> wpfTextViewCreationListeners, IFormattedTextSourceFactoryService formattedTextSourceFactoryService, IViewClassifierAggregatorService viewClassifierAggregatorService, ITextAndAdornmentSequencerFactoryService textAndAdornmentSequencerFactoryService, IClassificationFormatMapService classificationFormatMapService, IEditorFormatMapService editorFormatMapService, IAdornmentLayerDefinitionService adornmentLayerDefinitionService, ILineTransformProviderService lineTransformProviderService, IWpfTextViewMarginProviderCollectionProvider wpfTextViewMarginProviderCollectionProvider, IMenuManager menuManager, IEditorOperationsFactoryService editorOperationsFactoryService) {
			this.textBufferFactoryService = textBufferFactoryService;
			this.editorOptionsFactoryService = editorOptionsFactoryService;
			this.commandManager = commandManager;
			this.smartIndentationService = smartIndentationService;
			this.wpfTextViewCreationListeners = wpfTextViewCreationListeners.ToArray();
			this.formattedTextSourceFactoryService = formattedTextSourceFactoryService;
			this.viewClassifierAggregatorService = viewClassifierAggregatorService;
			this.textAndAdornmentSequencerFactoryService = textAndAdornmentSequencerFactoryService;
			this.classificationFormatMapService = classificationFormatMapService;
			this.editorFormatMapService = editorFormatMapService;
			this.adornmentLayerDefinitionService = adornmentLayerDefinitionService;
			this.lineTransformProviderService = lineTransformProviderService;
			this.wpfTextViewMarginProviderCollectionProvider = wpfTextViewMarginProviderCollectionProvider;
			this.menuManager = menuManager;
			this.editorOperationsFactoryService = editorOperationsFactoryService;
		}

		public IWpfTextView CreateTextView() => CreateTextView((TextViewCreatorOptions)null);
		public IDnSpyWpfTextView CreateTextView(TextViewCreatorOptions options) => CreateTextView(textBufferFactoryService.CreateTextBuffer(), DefaultRoles, options);

		public IWpfTextView CreateTextView(ITextBuffer textBuffer) =>
			CreateTextView(textBuffer, (TextViewCreatorOptions)null);
		public IDnSpyWpfTextView CreateTextView(ITextBuffer textBuffer, TextViewCreatorOptions options) {
			if (textBuffer == null)
				throw new ArgumentNullException(nameof(textBuffer));
			return CreateTextView(new TextDataModel(textBuffer), DefaultRoles, editorOptionsFactoryService.GlobalOptions, options);
		}

		public IWpfTextView CreateTextView(ITextBuffer textBuffer, ITextViewRoleSet roles) =>
			CreateTextView(textBuffer, roles, (TextViewCreatorOptions)null);
		public IDnSpyWpfTextView CreateTextView(ITextBuffer textBuffer, ITextViewRoleSet roles, TextViewCreatorOptions options) {
			if (textBuffer == null)
				throw new ArgumentNullException(nameof(textBuffer));
			if (roles == null)
				throw new ArgumentNullException(nameof(roles));
			return CreateTextView(new TextDataModel(textBuffer), roles, editorOptionsFactoryService.GlobalOptions, options);
		}

		public IWpfTextView CreateTextView(ITextBuffer textBuffer, ITextViewRoleSet roles, IEditorOptions parentOptions) =>
			CreateTextView(textBuffer, roles, parentOptions, null);
		public IDnSpyWpfTextView CreateTextView(ITextBuffer textBuffer, ITextViewRoleSet roles, IEditorOptions parentOptions, TextViewCreatorOptions options) {
			if (textBuffer == null)
				throw new ArgumentNullException(nameof(textBuffer));
			if (roles == null)
				throw new ArgumentNullException(nameof(roles));
			if (parentOptions == null)
				throw new ArgumentNullException(nameof(parentOptions));
			return CreateTextView(new TextDataModel(textBuffer), roles, parentOptions, options);
		}

		public IWpfTextView CreateTextView(ITextDataModel dataModel, ITextViewRoleSet roles, IEditorOptions parentOptions) =>
			CreateTextView(dataModel, roles, parentOptions, null);
		public IDnSpyWpfTextView CreateTextView(ITextDataModel dataModel, ITextViewRoleSet roles, IEditorOptions parentOptions, TextViewCreatorOptions options) {
			if (dataModel == null)
				throw new ArgumentNullException(nameof(dataModel));
			if (roles == null)
				throw new ArgumentNullException(nameof(roles));
			if (parentOptions == null)
				throw new ArgumentNullException(nameof(parentOptions));
			return CreateTextView(new TextViewModel(dataModel), roles, parentOptions, options);
		}

		public IWpfTextView CreateTextView(ITextViewModel viewModel, ITextViewRoleSet roles, IEditorOptions parentOptions) =>
			CreateTextView(viewModel, roles, parentOptions, null);
		public IDnSpyWpfTextView CreateTextView(ITextViewModel viewModel, ITextViewRoleSet roles, IEditorOptions parentOptions, TextViewCreatorOptions options) {
			if (viewModel == null)
				throw new ArgumentNullException(nameof(viewModel));
			if (roles == null)
				throw new ArgumentNullException(nameof(roles));
			if (parentOptions == null)
				throw new ArgumentNullException(nameof(parentOptions));
			return CreateTextViewImpl(viewModel, roles, parentOptions, options);
		}

		IDnSpyWpfTextView CreateTextViewImpl(ITextViewModel textViewModel, ITextViewRoleSet roles, IEditorOptions parentOptions, TextViewCreatorOptions options, Func<IGuidObjectsProvider> createGuidObjectsProvider = null) {
			var guidObjectsProvider = new GuidObjectsProvider(options?.CreateGuidObjects, createGuidObjectsProvider?.Invoke());
			var wpfTextView = new WpfTextView(textViewModel, roles, parentOptions, editorOptionsFactoryService, commandManager, smartIndentationService, formattedTextSourceFactoryService, viewClassifierAggregatorService, textAndAdornmentSequencerFactoryService, classificationFormatMapService, editorFormatMapService, adornmentLayerDefinitionService, lineTransformProviderService, wpfTextViewCreationListeners);
			guidObjectsProvider.WpfTextView = wpfTextView;

			if (options?.MenuGuid != null)
				menuManager.InitializeContextMenu(wpfTextView.VisualElement, options.MenuGuid.Value, guidObjectsProvider, new ContextMenuInitializer(wpfTextView));

			TextViewCreated?.Invoke(this, new TextViewCreatedEventArgs(wpfTextView));

			return wpfTextView;
		}

		public IWpfTextViewHost CreateTextViewHost(IWpfTextView wpfTextView, bool setFocus) {
			if (wpfTextView == null)
				throw new ArgumentNullException(nameof(wpfTextView));
			var dnSpyWpfTextView = wpfTextView as IDnSpyWpfTextView;
			if (dnSpyWpfTextView == null)
				throw new ArgumentException($"Only {nameof(IDnSpyWpfTextView)}s are allowed. Create your own proxy object if needed.");
			return CreateTextViewHost(dnSpyWpfTextView, setFocus);
		}

		public IDnSpyWpfTextViewHost CreateTextViewHost(IDnSpyWpfTextView wpfTextView, bool setFocus) {
			if (wpfTextView == null)
				throw new ArgumentNullException(nameof(wpfTextView));
			return new WpfTextViewHost(wpfTextViewMarginProviderCollectionProvider, wpfTextView, editorOperationsFactoryService, setFocus);
		}

		public ITextViewRoleSet CreateTextViewRoleSet(IEnumerable<string> roles) => new TextViewRoleSet(roles);
		public ITextViewRoleSet CreateTextViewRoleSet(params string[] roles) => new TextViewRoleSet(roles);
	}
}
