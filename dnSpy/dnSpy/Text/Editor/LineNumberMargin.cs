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
using dnSpy.Contracts.Text;
using dnSpy.Contracts.Text.Classification;
using dnSpy.Contracts.Text.Editor;
using dnSpy.Text.Formatting;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;
using Microsoft.VisualStudio.Utilities;

namespace dnSpy.Text.Editor {
	[Export(typeof(IWpfTextViewMarginProvider))]
	[MarginContainer(PredefinedMarginNames.LeftSelection)]
	[Name(PredefinedMarginNames.LineNumber)]
	[ContentType(ContentTypes.Text)]
	[TextViewRole(PredefinedTextViewRoles.Document)]
	[TextViewRole(PredefinedDsTextViewRoles.CanHaveLineNumberMargin)]
	[Order(Before = PredefinedMarginNames.Spacer)]
	sealed class LineNumberMarginProvider : IWpfTextViewMarginProvider {
		readonly IClassificationFormatMapService classificationFormatMapService;
		readonly IThemeClassificationTypeService themeClassificationTypeService;
		readonly ITextFormatterProvider textFormatterProvider;

		[ImportingConstructor]
		LineNumberMarginProvider(IClassificationFormatMapService classificationFormatMapService, IThemeClassificationTypeService themeClassificationTypeService, ITextFormatterProvider textFormatterProvider) {
			this.classificationFormatMapService = classificationFormatMapService;
			this.themeClassificationTypeService = themeClassificationTypeService;
			this.textFormatterProvider = textFormatterProvider;
		}

		public IWpfTextViewMargin CreateMargin(IWpfTextViewHost wpfTextViewHost, IWpfTextViewMargin marginContainer) {
			if (wpfTextViewHost.TextView.Roles.Contains(PredefinedDsTextViewRoles.CustomLineNumberMargin))
				return null;
			return new LineNumberMargin(wpfTextViewHost, classificationFormatMapService, themeClassificationTypeService, textFormatterProvider);
		}
	}

	sealed class LineNumberMargin : LineNumberMarginBase {
		readonly IClassificationType lineNumberClassificationType;
		TextFormattingRunProperties lineNumberTextFormattingRunProperties;

		public LineNumberMargin(IWpfTextViewHost wpfTextViewHost, IClassificationFormatMapService classificationFormatMapService, IThemeClassificationTypeService themeClassificationTypeService, ITextFormatterProvider textFormatterProvider)
			: base(PredefinedMarginNames.LineNumber, wpfTextViewHost, classificationFormatMapService, textFormatterProvider) => lineNumberClassificationType = themeClassificationTypeService.GetClassificationType(TextColor.LineNumber);

		protected override int? GetLineNumber(ITextViewLine viewLine, ref LineNumberState state) {
			if (!viewLine.IsFirstTextViewLineForSnapshotLine)
				return null;
			if (state == null)
				state = new LineNumberState();
			if (state.SnapshotLine == null || state.SnapshotLine.EndIncludingLineBreak != viewLine.Start)
				state.SnapshotLine = viewLine.Start.GetContainingLine();
			else
				state.SnapshotLine = state.SnapshotLine.Snapshot.GetLineFromLineNumber(state.SnapshotLine.LineNumber + 1);
			return state.SnapshotLine.LineNumber + 1;
		}

		protected override TextFormattingRunProperties GetLineNumberTextFormattingRunProperties(ITextViewLine viewLine, LineNumberState state, int lineNumber) =>
			lineNumberTextFormattingRunProperties;
		protected override TextFormattingRunProperties GetDefaultTextFormattingRunProperties() => lineNumberTextFormattingRunProperties;
		protected override void OnTextPropertiesChangedCore() =>
			lineNumberTextFormattingRunProperties = classificationFormatMap.GetTextProperties(lineNumberClassificationType);
		protected override void UnregisterEventsCore() => lineNumberTextFormattingRunProperties = null;
	}
}
