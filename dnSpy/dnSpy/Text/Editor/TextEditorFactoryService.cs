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
using dnSpy.Contracts.Command;
using dnSpy.Contracts.Menus;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.Text.Editor;
using dnSpy.Contracts.Text.Editor.Operations;

namespace dnSpy.Text.Editor {
	//TODO: This iface should be removed. The users shouldn't depend on the text editor impl
	interface ITextEditorFactoryService2 : ITextEditorFactoryService {
		WpfTextView CreateTextView(ITextBuffer textBuffer, TextViewCreatorOptions options, Func<IGuidObjectsCreator> createGuidObjectsCreator);
	}

	[Export(typeof(ITextEditorFactoryService))]
	[Export(typeof(ITextEditorFactoryService2))]
	sealed class TextEditorFactoryService : ITextEditorFactoryService2 {
		public event EventHandler<TextViewCreatedEventArgs> TextViewCreated;
		readonly ITextBufferFactoryService textBufferFactoryService;
		readonly IDnSpyTextEditorCreator dnSpyTextEditorCreator;
		readonly IEditorOptionsFactoryService editorOptionsFactoryService;
		readonly ICommandManager commandManager;
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

		sealed class GuidObjectsCreator : IGuidObjectsCreator {
			readonly Func<GuidObjectsCreatorArgs, IEnumerable<GuidObject>> createGuidObjects;
			readonly IGuidObjectsCreator guidObjectsCreator;
			internal WpfTextView WpfTextView { get; set; }

			public GuidObjectsCreator(Func<GuidObjectsCreatorArgs, IEnumerable<GuidObject>> createGuidObjects, IGuidObjectsCreator guidObjectsCreator) {
				this.createGuidObjects = createGuidObjects;
				this.guidObjectsCreator = guidObjectsCreator;
			}

			public IEnumerable<GuidObject> GetGuidObjects(GuidObjectsCreatorArgs args) {
				Debug.Assert(WpfTextView != null);
				if (WpfTextView != null) {
					yield return new GuidObject(MenuConstants.GUIDOBJ_WPF_TEXTVIEW_GUID, WpfTextView);
					foreach (var go in WpfTextView.DnSpyTextEditor.GetGuidObjects(args.OpenedFromKeyboard))
						yield return go;
				}

				if (createGuidObjects != null) {
					foreach (var guidObject in createGuidObjects(args))
						yield return guidObject;
				}

				if (guidObjectsCreator != null) {
					foreach (var guidObject in guidObjectsCreator.GetGuidObjects(args))
						yield return guidObject;
				}
			}
		}

		[ImportingConstructor]
		TextEditorFactoryService(ITextBufferFactoryService textBufferFactoryService, IDnSpyTextEditorCreator dnSpyTextEditorCreator, IEditorOptionsFactoryService editorOptionsFactoryService, ICommandManager commandManager, IEditorOperationsFactoryService editorOperationsFactoryService) {
			this.textBufferFactoryService = textBufferFactoryService;
			this.dnSpyTextEditorCreator = dnSpyTextEditorCreator;
			this.editorOptionsFactoryService = editorOptionsFactoryService;
			this.commandManager = commandManager;
			this.editorOperationsFactoryService = editorOperationsFactoryService;
		}

		public IWpfTextView CreateTextView(TextViewCreatorOptions options) => CreateTextView(textBufferFactoryService.CreateTextBuffer(), DefaultRoles, options);

		public IWpfTextView CreateTextView(ITextBuffer textBuffer, TextViewCreatorOptions options) {
			if (textBuffer == null)
				throw new ArgumentNullException(nameof(textBuffer));
			return CreateTextView(new TextDataModel(textBuffer), DefaultRoles, editorOptionsFactoryService.GlobalOptions, options);
		}

		public IWpfTextView CreateTextView(ITextBuffer textBuffer, ITextViewRoleSet roles, TextViewCreatorOptions options) {
			if (textBuffer == null)
				throw new ArgumentNullException(nameof(textBuffer));
			if (roles == null)
				throw new ArgumentNullException(nameof(roles));
			return CreateTextView(new TextDataModel(textBuffer), roles, editorOptionsFactoryService.GlobalOptions, options);
		}

		public IWpfTextView CreateTextView(ITextBuffer textBuffer, ITextViewRoleSet roles, IEditorOptions parentOptions, TextViewCreatorOptions options) {
			if (textBuffer == null)
				throw new ArgumentNullException(nameof(textBuffer));
			if (roles == null)
				throw new ArgumentNullException(nameof(roles));
			if (parentOptions == null)
				throw new ArgumentNullException(nameof(parentOptions));
			return CreateTextView(new TextDataModel(textBuffer), roles, parentOptions, options);
		}

		public IWpfTextView CreateTextView(ITextDataModel dataModel, ITextViewRoleSet roles, IEditorOptions parentOptions, TextViewCreatorOptions options) {
			if (dataModel == null)
				throw new ArgumentNullException(nameof(dataModel));
			if (roles == null)
				throw new ArgumentNullException(nameof(roles));
			if (parentOptions == null)
				throw new ArgumentNullException(nameof(parentOptions));
			return CreateTextView(new TextViewModel(dataModel), roles, parentOptions, options);
		}

		public IWpfTextView CreateTextView(ITextViewModel viewModel, ITextViewRoleSet roles, IEditorOptions parentOptions, TextViewCreatorOptions options) {
			if (viewModel == null)
				throw new ArgumentNullException(nameof(viewModel));
			if (roles == null)
				throw new ArgumentNullException(nameof(roles));
			if (parentOptions == null)
				throw new ArgumentNullException(nameof(parentOptions));
			return CreateTextViewImpl(viewModel, roles, parentOptions, options);
		}

		WpfTextView CreateTextViewImpl(ITextViewModel textViewModel, ITextViewRoleSet roles, IEditorOptions parentOptions, TextViewCreatorOptions options, Func<IGuidObjectsCreator> createGuidObjectsCreator = null) {
			var commonTextEditorOptions = new CommonTextEditorOptions {
				TextEditorCommandGuid = options?.TextEditorCommandGuid,
				TextAreaCommandGuid = options?.TextAreaCommandGuid,
				MenuGuid = options?.MenuGuid,
				ContentType = textViewModel.DataModel.ContentType,
				CreateGuidObjects = options?.CreateGuidObjects,
			};
			var guidObjectsCreator = new GuidObjectsCreator(options?.CreateGuidObjects, createGuidObjectsCreator?.Invoke());
			var dnSpyTextEditorOptions = new DnSpyTextEditorOptions(commonTextEditorOptions, textViewModel.EditBuffer, () => guidObjectsCreator);
			var dnSpyTextEditor = dnSpyTextEditorCreator.Create(dnSpyTextEditorOptions);
			var wpfTextView = new WpfTextView(dnSpyTextEditor, textViewModel, roles, parentOptions, editorOptionsFactoryService, commandManager, editorOperationsFactoryService);
			guidObjectsCreator.WpfTextView = wpfTextView;
			TextViewCreated?.Invoke(this, new TextViewCreatedEventArgs(wpfTextView));
			return wpfTextView;
		}

		WpfTextView ITextEditorFactoryService2.CreateTextView(ITextBuffer textBuffer, TextViewCreatorOptions options, Func<IGuidObjectsCreator> createGuidObjectsCreator) {
			if (textBuffer == null)
				throw new ArgumentNullException(nameof(textBuffer));
			return CreateTextViewImpl(new TextViewModel(new TextDataModel(textBuffer)), DefaultRoles, editorOptionsFactoryService.GlobalOptions, options, createGuidObjectsCreator);
		}

		public IWpfTextViewHost CreateTextViewHost(IWpfTextView wpfTextView, bool setFocus) {
			if (wpfTextView == null)
				throw new ArgumentNullException(nameof(wpfTextView));
			return new WpfTextViewHost(wpfTextView, setFocus);
		}

		public ITextViewRoleSet CreateTextViewRoleSet(IEnumerable<string> roles) => new TextViewRoleSet(roles);
		public ITextViewRoleSet CreateTextViewRoleSet(params string[] roles) => new TextViewRoleSet(roles);
	}
}
