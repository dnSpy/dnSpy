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
using dnSpy.Contracts.Settings;
using dnSpy.Contracts.Settings.Dialog;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.OptionsExtensionMethods;
using Microsoft.VisualStudio.Utilities;

namespace dnSpy.Text.CodeEditor {
	interface ICodeEditorOptionsService {
		IEnumerable<ICodeEditorOptions> Options { get; }
		void TextViewCreated(IWpfTextView textView);
	}

	[Export(typeof(ICodeEditorOptionsService))]
	sealed class CodeEditorOptionsService : ICodeEditorOptionsService {
		public IEnumerable<ICodeEditorOptions> Options => codeEditorOptionsCollection.Options;

		readonly CodeEditorOptionsCollection codeEditorOptionsCollection;
		readonly CodeEditorOptionsStorage codeEditorOptionsStorage;
		readonly List<IWpfTextView> codeTextViews;

		[ImportingConstructor]
		CodeEditorOptionsService(ISettingsService settingsService, IContentTypeRegistryService contentTypeRegistryService, [ImportMany] IEnumerable<Lazy<CodeEditorOptionsDefinition, ICodeEditorOptionsDefinitionMetadata>> codeEditorOptionsDefinitions) {
			this.codeTextViews = new List<IWpfTextView>();
			var codeEditorOptions = codeEditorOptionsDefinitions.Select(a => CodeEditorOptions.TryCreate(this, contentTypeRegistryService, a.Metadata)).Where(a => a != null).ToArray();
			this.codeEditorOptionsCollection = new CodeEditorOptionsCollection(codeEditorOptions);
			this.codeEditorOptionsStorage = new CodeEditorOptionsStorage(settingsService, codeEditorOptionsCollection);
		}

		public void TextViewCreated(IWpfTextView textView) {
			if (textView == null)
				throw new ArgumentNullException(nameof(textView));
			codeTextViews.Add(textView);
			new TextViewListener(this, textView);
		}

		sealed class TextViewListener {
			readonly CodeEditorOptionsService owner;
			readonly IWpfTextView textView;

			public TextViewListener(CodeEditorOptionsService owner, IWpfTextView textView) {
				this.owner = owner;
				this.textView = textView;
				textView.Closed += TextView_Closed;
				textView.Options.OptionChanged += Options_OptionChanged;
				textView.TextDataModel.ContentTypeChanged += TextDataModel_ContentTypeChanged;
				owner.InitializeOptions(textView, null, textView.TextDataModel.ContentType);
			}

			void Options_OptionChanged(object sender, EditorOptionChangedEventArgs e) {
				if (textView.IsClosed)
					return;
				owner.OptionChanged(textView, e);
			}

			void TextDataModel_ContentTypeChanged(object sender, TextDataModelContentTypeChangedEventArgs e) {
				if (textView.IsClosed)
					return;
				owner.InitializeOptions(textView, e.BeforeContentType, e.AfterContentType);
			}

			void TextView_Closed(object sender, EventArgs e) {
				textView.Closed -= TextView_Closed;
				textView.Options.OptionChanged -= Options_OptionChanged;
				textView.TextDataModel.ContentTypeChanged -= TextDataModel_ContentTypeChanged;
				owner.Closed(textView);
			}
		}

		struct ChangeOptionKey : IEquatable<ChangeOptionKey> {
			readonly CodeEditorOptions options;
			readonly string optionId;
			public ChangeOptionKey(CodeEditorOptions options, string optionId) {
				this.options = options;
				this.optionId = optionId;
			}
			public bool Equals(ChangeOptionKey other) => options == other.options && optionId == other.optionId;
			public override bool Equals(object obj) => obj is ChangeOptionKey && Equals((ChangeOptionKey)obj);
			public override int GetHashCode() => options.GetHashCode() ^ optionId.GetHashCode();
			public override string ToString() => options.GetHashCode().ToString("X8") + ": " + optionId;
		}

		readonly HashSet<ChangeOptionKey> writeOptionHash = new HashSet<ChangeOptionKey>();
		void WriteOption(CodeEditorOptions options, object value, string optionId) {
			// Check if called from ctor
			if (codeEditorOptionsStorage == null)
				return;
			var key = new ChangeOptionKey(options, optionId);
			if (writeOptionHash.Contains(key))
				return;
			try {
				writeOptionHash.Add(key);
				codeEditorOptionsStorage.Write(options);
				foreach (var textView in codeTextViews.ToArray())
					textView.Options.SetOptionValue(optionId, value);
			}
			finally {
				writeOptionHash.Remove(key);
			}
		}

		public void OptionChanged(CodeEditorOptions options, string name) {
			switch (name) {
			case nameof(options.UseVirtualSpace):
				WriteOption(options, options.UseVirtualSpace, DefaultTextViewOptions.UseVirtualSpaceName);
				break;
			case nameof(options.WordWrapStyle):
				WriteOption(options, options.WordWrapStyle, DefaultTextViewOptions.WordWrapStyleName);
				break;
			case nameof(options.ShowLineNumbers):
				WriteOption(options, options.ShowLineNumbers, DefaultTextViewHostOptions.LineNumberMarginName);
				break;
			case nameof(options.TabSize):
				WriteOption(options, options.TabSize, DefaultOptions.TabSizeOptionName);
				break;
			case nameof(options.IndentSize):
				WriteOption(options, options.IndentSize, DefaultOptions.IndentSizeOptionName);
				break;
			case nameof(options.ConvertTabsToSpaces):
				WriteOption(options, options.ConvertTabsToSpaces, DefaultOptions.ConvertTabsToSpacesOptionName);
				break;
			default:
				Debug.Fail($"Unknown option: {name}");
				break;
			}
		}

		void OptionChanged(IWpfTextView textView, EditorOptionChangedEventArgs e) {
			if (e.OptionId == DefaultTextViewOptions.UseVirtualSpaceName) {
				var options = codeEditorOptionsCollection.Find(textView.TextDataModel.ContentType);
				if (options != null)
					options.UseVirtualSpace = textView.Options.IsVirtualSpaceEnabled();
			}
			else if (e.OptionId == DefaultTextViewOptions.WordWrapStyleName) {
				var options = codeEditorOptionsCollection.Find(textView.TextDataModel.ContentType);
				if (options != null)
					options.WordWrapStyle = textView.Options.WordWrapStyle();
			}
			else if (e.OptionId == DefaultTextViewHostOptions.LineNumberMarginName) {
				var options = codeEditorOptionsCollection.Find(textView.TextDataModel.ContentType);
				if (options != null)
					options.ShowLineNumbers = textView.Options.IsLineNumberMarginEnabled();
			}
			else if (e.OptionId == DefaultOptions.TabSizeOptionName) {
				var options = codeEditorOptionsCollection.Find(textView.TextDataModel.ContentType);
				if (options != null)
					options.TabSize = textView.Options.GetTabSize();
			}
			else if (e.OptionId == DefaultOptions.IndentSizeOptionName) {
				var options = codeEditorOptionsCollection.Find(textView.TextDataModel.ContentType);
				if (options != null)
					options.IndentSize = textView.Options.GetIndentSize();
			}
			else if (e.OptionId == DefaultOptions.ConvertTabsToSpacesOptionName) {
				var options = codeEditorOptionsCollection.Find(textView.TextDataModel.ContentType);
				if (options != null)
					options.ConvertTabsToSpaces = textView.Options.IsConvertTabsToSpacesEnabled();
			}
		}

		void InitializeOptions(IWpfTextView textView, IContentType beforeContentType, IContentType afterContentType) {
			ICodeEditorOptions oldOptions = codeEditorOptionsCollection.Find(beforeContentType);
			ICodeEditorOptions options = codeEditorOptionsCollection.Find(afterContentType);
			if (oldOptions == options)
				return;
			if (options == null)
				options = DefaultCodeEditorOptionsImpl.Instance;

			textView.Options.SetOptionValue(DefaultTextViewOptions.UseVirtualSpaceId, options.UseVirtualSpace);
			textView.Options.SetOptionValue(DefaultTextViewOptions.WordWrapStyleId, options.WordWrapStyle);
			textView.Options.SetOptionValue(DefaultTextViewHostOptions.LineNumberMarginId, options.ShowLineNumbers);
			textView.Options.SetOptionValue(DefaultOptions.TabSizeOptionId, options.TabSize);
			textView.Options.SetOptionValue(DefaultOptions.IndentSizeOptionId, options.IndentSize);
			textView.Options.SetOptionValue(DefaultOptions.ConvertTabsToSpacesOptionId, options.ConvertTabsToSpaces);
		}

		void Closed(IWpfTextView textView) {
			Debug.Assert(textView.IsClosed);
			bool b = codeTextViews.Remove(textView);
			Debug.Assert(b);
		}
	}
}
