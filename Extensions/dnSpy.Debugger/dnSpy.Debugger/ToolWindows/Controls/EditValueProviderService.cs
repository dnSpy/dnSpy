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
using System.Windows;
using System.Windows.Controls;
using dnSpy.Contracts.Command;
using dnSpy.Contracts.Settings.AppearanceCategory;
using dnSpy.Contracts.Text.Editor;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace dnSpy.Debugger.ToolWindows.Controls {
	abstract class EditValueProviderService {
		public abstract IEditValueProvider Create(string contentType, string[] extraTextViewRoles);
	}

	[Export(typeof(EditValueProviderService))]
	sealed class EditValueProviderServiceImpl : EditValueProviderService {
		readonly IContentTypeRegistryService contentTypeRegistryService;
		readonly ITextBufferFactoryService textBufferFactoryService;
		readonly ITextEditorFactoryService textEditorFactoryService;

		[ImportingConstructor]
		EditValueProviderServiceImpl(IContentTypeRegistryService contentTypeRegistryService, ITextBufferFactoryService textBufferFactoryService, ITextEditorFactoryService textEditorFactoryService) {
			this.contentTypeRegistryService = contentTypeRegistryService;
			this.textBufferFactoryService = textBufferFactoryService;
			this.textEditorFactoryService = textEditorFactoryService;
		}

		public override IEditValueProvider Create(string contentType, string[] extraTextViewRoles) {
			if (contentType == null)
				throw new ArgumentNullException(nameof(contentType));
			if (extraTextViewRoles == null)
				throw new ArgumentNullException(nameof(extraTextViewRoles));
			var ct = contentTypeRegistryService.GetContentType(contentType);
			if (ct == null)
				throw new ArgumentOutOfRangeException(nameof(contentType));
			return new EditValueProviderImpl(ct, textBufferFactoryService, textEditorFactoryService, extraTextViewRoles);
		}
	}

	static class EditValueConstants {
		public const string EditValueTextViewRole = nameof(EditValueTextViewRole);
	}

	sealed class EditValueProviderImpl : IEditValueProvider {
		readonly IContentType contentType;
		readonly ITextBufferFactoryService textBufferFactoryService;
		readonly ITextEditorFactoryService textEditorFactoryService;
		readonly string[] extraTextViewRoles;

		public EditValueProviderImpl(IContentType contentType, ITextBufferFactoryService textBufferFactoryService, ITextEditorFactoryService textEditorFactoryService, string[] extraTextViewRoles) {
			this.contentType = contentType ?? throw new ArgumentNullException(nameof(contentType));
			this.textBufferFactoryService = textBufferFactoryService ?? throw new ArgumentNullException(nameof(textBufferFactoryService));
			this.textEditorFactoryService = textEditorFactoryService ?? throw new ArgumentNullException(nameof(textEditorFactoryService));
			this.extraTextViewRoles = extraTextViewRoles ?? throw new ArgumentNullException(nameof(extraTextViewRoles));
		}

		public IEditValue Create(string text) {
			var buffer = textBufferFactoryService.CreateTextBuffer(text, contentType);
			var rolesHash = new HashSet<string>(textEditorFactoryService.DefaultRoles, StringComparer.OrdinalIgnoreCase) {
				EditValueConstants.EditValueTextViewRole,
			};
			// This also disables: line compressor, current line highlighter
			rolesHash.Remove(PredefinedTextViewRoles.Document);
			foreach (var s in extraTextViewRoles)
				rolesHash.Add(s);
			var roles = textEditorFactoryService.CreateTextViewRoleSet(rolesHash);
			var textView = textEditorFactoryService.CreateTextView(buffer, roles);
			try {
				return new EditValueImpl(textView);
			}
			catch {
				textView.Close();
				throw;
			}
		}
	}

	sealed class EditValueImpl : IEditValue {
		public event EventHandler<EditCompletedEventArgs> EditCompleted;
		public object UIObject => uiControl;
		public bool IsKeyboardFocused => wpfTextView.HasAggregateFocus;

		sealed class UIControl : ContentControl {
			readonly IWpfTextView wpfTextView;
			double lastHeight;

			public UIControl(IWpfTextView wpfTextView) {
				this.wpfTextView = wpfTextView ?? throw new ArgumentNullException(nameof(wpfTextView));
				wpfTextView.LayoutChanged += WpfTextView_LayoutChanged;
				Content = wpfTextView.VisualElement;
			}

			void WpfTextView_LayoutChanged(object sender, TextViewLayoutChangedEventArgs e) {
				var height = wpfTextView.TextViewLines[0].Height;
				if (height != lastHeight) {
					lastHeight = height;
					InvalidateMeasure();
				}
			}

			protected override Size MeasureOverride(Size constraint) {
				var res = base.MeasureOverride(constraint);
				return new Size(res.Width, Math.Max(res.Height, lastHeight));
			}

			internal void Dispose() => wpfTextView.LayoutChanged -= WpfTextView_LayoutChanged;
		}

		readonly IWpfTextView wpfTextView;
		readonly UIControl uiControl;

		public EditValueImpl(IWpfTextView wpfTextView) {
			this.wpfTextView = wpfTextView;
			uiControl = new UIControl(wpfTextView);
			wpfTextView.VisualElement.Loaded += VisualElement_Loaded;
			wpfTextView.TextBuffer.Properties.AddProperty(typeof(EditValueImpl), this);
			wpfTextView.Options.SetOptionValue(DefaultOptions.ConvertTabsToSpacesOptionId, true);
			wpfTextView.Options.SetOptionValue(DefaultTextViewOptions.WordWrapStyleId, WordWrapStyles.None);
			wpfTextView.Options.SetOptionValue(DefaultWpfViewOptions.EnableHighlightCurrentLineId, false);
			wpfTextView.Options.SetOptionValue(DefaultWpfViewOptions.EnableMouseWheelZoomId, false);
			wpfTextView.Options.SetOptionValue(DefaultWpfViewOptions.AppearanceCategory, AppearanceCategoryConstants.UIMisc);
			wpfTextView.Options.SetOptionValue(DefaultDsTextViewOptions.CanChangeWordWrapStyleId, false);
			wpfTextView.Options.SetOptionValue(DefaultDsTextViewOptions.CompressEmptyOrWhitespaceLinesId, false);
			wpfTextView.Options.SetOptionValue(DefaultDsTextViewOptions.CompressNonLetterLinesId, false);
		}

		public static EditValueImpl TryGetInstance(ITextView textView) {
			textView.TextBuffer.Properties.TryGetProperty<EditValueImpl>(typeof(EditValueImpl), out var instance);
			return instance;
		}

		void VisualElement_Loaded(object sender, RoutedEventArgs e) {
			wpfTextView.VisualElement.Loaded -= VisualElement_Loaded;
			wpfTextView.VisualElement.Focus();
			var snapshot = wpfTextView.TextSnapshot;
			wpfTextView.Selection.Select(new SnapshotSpan(snapshot, new Span(0, snapshot.Length)), isReversed: false);
			wpfTextView.Caret.MoveTo(new SnapshotPoint(snapshot, snapshot.Length));
			wpfTextView.LostAggregateFocus += WpfTextView_LostAggregateFocus;
		}

		void WpfTextView_LostAggregateFocus(object sender, EventArgs e) => Cancel();
		public void Cancel() => OnEditCompleted(null);
		public void Commit() => OnEditCompleted(wpfTextView.TextBuffer.CurrentSnapshot.GetText());

		void OnEditCompleted(string text) {
			EditCompleted?.Invoke(this, new EditCompletedEventArgs(text));
			Dispose();
		}

		public void Dispose() {
			if (wpfTextView.IsClosed)
				return;
			uiControl.Dispose();
			wpfTextView.Properties.RemoveProperty(typeof(EditValueImpl));
			wpfTextView.VisualElement.Loaded -= VisualElement_Loaded;
			wpfTextView.LostAggregateFocus -= WpfTextView_LostAggregateFocus;
			wpfTextView.Close();
		}
	}

	[ExportCommandTargetFilterProvider(CommandTargetFilterOrder.TextEditor - 100)]
	sealed class EditValueCommandTargetFilterProvider : ICommandTargetFilterProvider {
		public ICommandTargetFilter Create(object target) {
			var textView = target as ITextView;
			if (textView?.Roles.Contains(EditValueConstants.EditValueTextViewRole) != true)
				return null;

			return new EditValueCommandTargetFilter(textView);
		}
	}

	sealed class EditValueCommandTargetFilter : ICommandTargetFilter {
		readonly ITextView textView;

		EditValueImpl TryGetInstance() =>
			__editValueImpl ?? (__editValueImpl = EditValueImpl.TryGetInstance(textView));
		EditValueImpl __editValueImpl;

		public EditValueCommandTargetFilter(ITextView textView) => this.textView = textView;

		public CommandTargetStatus CanExecute(Guid group, int cmdId) {
			if (TryGetInstance() == null)
				return CommandTargetStatus.NotHandled;

			if (group == CommandConstants.TextEditorGroup) {
				switch ((TextEditorIds)cmdId) {
				case TextEditorIds.CANCEL:
				case TextEditorIds.RETURN:
					return CommandTargetStatus.Handled;
				default:
					return CommandTargetStatus.NotHandled;
				}
			}
			return CommandTargetStatus.NotHandled;
		}

		public CommandTargetStatus Execute(Guid group, int cmdId, object args = null) {
			object result = null;
			return Execute(group, cmdId, args, ref result);
		}

		public CommandTargetStatus Execute(Guid group, int cmdId, object args, ref object result) {
			var editValueImpl = TryGetInstance();
			if (editValueImpl == null)
				return CommandTargetStatus.NotHandled;

			if (group == CommandConstants.TextEditorGroup) {
				switch ((TextEditorIds)cmdId) {
				case TextEditorIds.CANCEL:
					editValueImpl.Cancel();
					return CommandTargetStatus.Handled;

				case TextEditorIds.RETURN:
					editValueImpl.Commit();
					return CommandTargetStatus.Handled;

				default:
					return CommandTargetStatus.NotHandled;
				}
			}
			return CommandTargetStatus.NotHandled;
		}

		public void SetNextCommandTarget(ICommandTarget commandTarget) { }
		public void Dispose() { }
	}
}
