/*
    Copyright (C) 2014-2019 de4dot@gmail.com

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
using System.Diagnostics;
using dnSpy.Contracts.Settings.Groups;
using dnSpy.Contracts.Text;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace dnSpy.Text.Groups {
	sealed class TextViewOptionsGroup : ITextViewOptionsGroup {
		IEnumerable<IWpfTextView> ITextViewOptionsGroup.TextViews => textViews.ToArray();
		public event EventHandler<TextViewOptionChangedEventArgs>? TextViewOptionChanged;

		readonly List<IWpfTextView> textViews;
		readonly Dictionary<IContentType, TextViewGroupOptionCollection> toOptions;
		readonly IContentTypeRegistryService contentTypeRegistryService;
		readonly OptionsStorage optionsStorage;
		readonly string groupName;

		public TextViewOptionsGroup(string groupName, IContentTypeRegistryService contentTypeRegistryService, ContentTypeOptionDefinition[] defaultOptions, OptionsStorage optionsStorage) {
			if (defaultOptions is null)
				throw new ArgumentNullException(nameof(defaultOptions));
			if (optionsStorage is null)
				throw new ArgumentNullException(nameof(optionsStorage));
			this.contentTypeRegistryService = contentTypeRegistryService ?? throw new ArgumentNullException(nameof(contentTypeRegistryService));
			textViews = new List<IWpfTextView>();
			toOptions = new Dictionary<IContentType, TextViewGroupOptionCollection>();
			this.groupName = groupName ?? throw new ArgumentNullException(nameof(groupName));

			foreach (var option in defaultOptions) {
				Debug2.Assert(option.Name is not null);
				if (option.Name is null)
					continue;

				var ct = option.ContentType is null ? null : contentTypeRegistryService.GetContentType(option.ContentType);
				Debug2.Assert(ct is not null);
				if (ct is null)
					continue;

				if (!toOptions.TryGetValue(ct, out var coll))
					toOptions.Add(ct, coll = new TextViewGroupOptionCollection(ct));
				coll.Add(new TextViewGroupOption(this, option));
			}

			foreach (var coll in toOptions.Values)
				optionsStorage.InitializeOptions(groupName, coll);
			this.optionsStorage = optionsStorage;
		}

		TextViewGroupOptionCollection GetCollection(string contentType) => GetCollection(contentTypeRegistryService.GetContentType(contentType));
		TextViewGroupOptionCollection GetCollection(IContentType? contentType) {
			if (contentType is null)
				contentType = contentTypeRegistryService.GetContentType(ContentTypes.Any);
			Debug2.Assert(contentType is not null);
			if (contentType is null)
				return ErrorCollection;

			if (toOptions.TryGetValue(contentType, out var coll))
				return coll;

			// No perfect match, use inherited options
			var contentTypes = new List<IContentType>();
			GetContentTypes(contentType, contentTypes);
			foreach (var ct in contentTypes) {
				if (toOptions.TryGetValue(ct, out coll))
					break;
			}
			if (coll is null)
				coll = ErrorCollection;
			toOptions.Add(contentType, coll);
			return coll;
		}

		static void GetContentTypes(IContentType contentType, List<IContentType> list) {
			if (contentType is null)
				return;
			list.AddRange(contentType.BaseTypes);
			foreach (var bt in contentType.BaseTypes)
				GetContentTypes(bt, list);
		}

		TextViewGroupOptionCollection ErrorCollection => errorCollection ??= new TextViewGroupOptionCollection(contentTypeRegistryService.UnknownContentType);
		TextViewGroupOptionCollection? errorCollection;

		public bool HasOption<T>(string contentType, EditorOptionKey<T> option) => HasOption(contentType, option.Name);
		public bool HasOption(string contentType, string optionId) {
			if (contentType is null)
				throw new ArgumentNullException(nameof(contentType));
			if (optionId is null)
				throw new ArgumentNullException(nameof(optionId));
			return GetCollection(contentType).HasOption(optionId);
		}

		public T GetOptionValue<T>(string contentType, EditorOptionKey<T> option) => (T)GetOptionValue(contentType, option.Name)!;
		public object? GetOptionValue(string contentType, string optionId) {
			if (contentType is null)
				throw new ArgumentNullException(nameof(contentType));
			if (optionId is null)
				throw new ArgumentNullException(nameof(optionId));
			return GetCollection(contentType).GetOptionValue(optionId);
		}

		public void SetOptionValue<T>(string contentType, EditorOptionKey<T> option, T value) => SetOptionValue(contentType, option.Name, value);
		public void SetOptionValue(string contentType, string optionId, object? value) {
			if (contentType is null)
				throw new ArgumentNullException(nameof(contentType));
			if (optionId is null)
				throw new ArgumentNullException(nameof(optionId));
			GetCollection(contentType).SetOptionValue(optionId, value);
		}

		internal void TextViewCreated(IWpfTextView textView) {
			if (textView is null)
				throw new ArgumentNullException(nameof(textView));
			Debug.Assert(!textView.IsClosed);
			if (textView.IsClosed)
				return;
			textViews.Add(textView);
			new TextViewListener(this, textView);
		}

		sealed class TextViewListener {
			readonly TextViewOptionsGroup owner;
			readonly IWpfTextView textView;

			public TextViewListener(TextViewOptionsGroup owner, IWpfTextView textView) {
				this.owner = owner;
				this.textView = textView;
				textView.Closed += TextView_Closed;
				textView.Options.OptionChanged += Options_OptionChanged;
				textView.TextDataModel.ContentTypeChanged += TextDataModel_ContentTypeChanged;
				owner.InitializeOptions(textView, null, textView.TextDataModel.ContentType, force: true);
			}

			void Options_OptionChanged(object? sender, EditorOptionChangedEventArgs e) {
				if (textView.IsClosed)
					return;
				owner.OptionChanged(textView, e);
			}

			void TextDataModel_ContentTypeChanged(object? sender, TextDataModelContentTypeChangedEventArgs e) {
				if (textView.IsClosed)
					return;
				owner.InitializeOptions(textView, e.BeforeContentType, e.AfterContentType, force: false);
			}

			void TextView_Closed(object? sender, EventArgs e) {
				textView.Closed -= TextView_Closed;
				textView.Options.OptionChanged -= Options_OptionChanged;
				textView.TextDataModel.ContentTypeChanged -= TextDataModel_ContentTypeChanged;
				owner.Closed(textView);
			}
		}

		readonly HashSet<TextViewGroupOption> writeOptionHash = new HashSet<TextViewGroupOption>();
		public void OptionChanged(TextViewGroupOption option) {
			if (optionsStorage is null)
				return;
			if (writeOptionHash.Contains(option))
				return;
			try {
				writeOptionHash.Add(option);
				optionsStorage.Write(groupName, option);
				foreach (var textView in textViews.ToArray()) {
					var coll = GetCollection(textView.TextDataModel.ContentType);
					if (!StringComparer.OrdinalIgnoreCase.Equals(option.Definition.ContentType, coll.ContentType.TypeName))
						continue;
					try {
						textView.Options.SetOptionValue(option.OptionId, option.Value);
					}
					catch (ArgumentException) {
						// Invalid option value
					}
				}
				TextViewOptionChanged?.Invoke(this, new TextViewOptionChangedEventArgs(option.Definition.ContentType, option.Definition.Name));
			}
			finally {
				writeOptionHash.Remove(option);
			}
		}

		void OptionChanged(IWpfTextView textView, EditorOptionChangedEventArgs e) {
			var coll = GetCollection(textView.TextDataModel.ContentType);
			if (!coll.HasOption(e.OptionId))
				return;
			coll.SetOptionValue(e.OptionId, textView.Options.GetOptionValue(e.OptionId));
		}

		void InitializeOptions(IWpfTextView textView, IContentType? beforeContentType, IContentType afterContentType, bool force) {
			var oldColl = GetCollection(beforeContentType);
			var newColl = GetCollection(afterContentType);
			if (!force && oldColl == newColl)
				return;

			newColl.InitializeOptions(textView);
		}

		void Closed(IWpfTextView textView) {
			Debug.Assert(textView.IsClosed);
			bool b = textViews.Remove(textView);
			Debug.Assert(b);
		}
	}
}
