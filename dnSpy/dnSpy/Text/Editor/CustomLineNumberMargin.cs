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
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Windows;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.Text.Editor;
using dnSpy.Text.Formatting;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;
using Microsoft.VisualStudio.Utilities;

namespace dnSpy.Text.Editor {
	[Export(typeof(IWpfTextViewMarginProvider))]
	[MarginContainer(PredefinedMarginNames.LeftSelection)]
	[Name(PredefinedDsMarginNames.CustomLineNumber)]
	[ContentType(ContentTypes.Text)]
	[TextViewRole(PredefinedDsTextViewRoles.CustomLineNumberMargin)]
	[Order(Before = PredefinedMarginNames.Spacer)]
	sealed class CustomLineNumberMarginProvider : IWpfTextViewMarginProvider {
		readonly IClassificationFormatMapService classificationFormatMapService;
		readonly ITextFormatterProvider textFormatterProvider;

		[ImportingConstructor]
		CustomLineNumberMarginProvider(IClassificationFormatMapService classificationFormatMapService, ITextFormatterProvider textFormatterProvider) {
			this.classificationFormatMapService = classificationFormatMapService;
			this.textFormatterProvider = textFormatterProvider;
		}

		public IWpfTextViewMargin CreateMargin(IWpfTextViewHost wpfTextViewHost, IWpfTextViewMargin marginContainer) =>
			new CustomLineNumberMarginImpl(wpfTextViewHost, classificationFormatMapService, textFormatterProvider);
	}

	sealed class CustomLineNumberMarginImpl : LineNumberMarginBase, ICustomLineNumberMargin {
		ICustomLineNumberMarginOwner owner;

		public CustomLineNumberMarginImpl(IWpfTextViewHost wpfTextViewHost, IClassificationFormatMapService classificationFormatMapService, ITextFormatterProvider textFormatterProvider)
			: base(PredefinedDsMarginNames.CustomLineNumber, wpfTextViewHost, classificationFormatMapService, textFormatterProvider) => CustomLineNumberMargin.SetMargin(wpfTextViewHost.TextView, this);

		void ICustomLineNumberMargin.SetOwner(ICustomLineNumberMarginOwner owner) {
			if (this.owner != null)
				throw new InvalidOperationException();
			this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
			if (Visibility == Visibility.Visible)
				owner.OnVisible();
			RefreshMargin();
		}

		sealed class CustomLineNumberState : LineNumberState {
			public object State;
		}

		protected override int? GetLineNumber(ITextViewLine viewLine, ref LineNumberState state) {
			if (owner == null)
				return null;
			CustomLineNumberState customState;
			if (state == null)
				state = customState = new CustomLineNumberState();
			else
				customState = (CustomLineNumberState)state;
			if (state.SnapshotLine == null || state.SnapshotLine.EndIncludingLineBreak != viewLine.Start)
				state.SnapshotLine = viewLine.Start.GetContainingLine();
			else
				state.SnapshotLine = state.SnapshotLine.Snapshot.GetLineFromLineNumber(state.SnapshotLine.LineNumber + 1);
			return owner.GetLineNumber(viewLine, state.SnapshotLine, ref customState.State);
		}

		protected override int? GetMaxLineDigitsCore() {
			Debug.Assert(owner != null);
			return owner?.GetMaxLineNumberDigits();
		}

		protected override TextFormattingRunProperties GetLineNumberTextFormattingRunProperties(ITextViewLine viewLine, LineNumberState state, int lineNumber) {
			Debug.Assert(owner != null);
			Debug.Assert(state != null);
			if (owner == null)
				throw new InvalidOperationException();
			var customState = (CustomLineNumberState)state;
			return owner.GetLineNumberTextFormattingRunProperties(viewLine, customState.SnapshotLine, lineNumber, customState.State);
		}

		protected override TextFormattingRunProperties GetDefaultTextFormattingRunProperties() => owner?.GetDefaultTextFormattingRunProperties();
		protected override void OnTextPropertiesChangedCore() => owner?.OnTextPropertiesChanged(classificationFormatMap);
		protected override void RegisterEventsCore() => owner?.OnVisible();
		protected override void UnregisterEventsCore() => owner?.OnInvisible();
	}
}
