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

using dnSpy.Contracts.BackgroundImage;
using dnSpy.Contracts.Text.Editor;
using dnSpy.Properties;
using Microsoft.VisualStudio.Text.Editor;

namespace dnSpy.BackgroundImage {
	static class BackgroundImageOptionDefinitions {
		[ExportBackgroundImageOptionDefinition(double.PositiveInfinity)]
		sealed class Default : IBackgroundImageOptionDefinition {
			public string Id => "Default";
			public string DisplayName => dnSpy_Resources.BgImgDisplayName_Default;
			public double UIOrder => double.PositiveInfinity;
			public bool UserVisible => true;
			public DefaultImageSettings GetDefaultImageSettings() => null;
			public bool IsSupported(ITextView textView) => true;
		}

		[ExportBackgroundImageOptionDefinition(BackgroundImageOptionDefinitionConstants.AttrOrder_DocumentViewer)]
		sealed class DocumentViewer : IBackgroundImageOptionDefinition {
			public string Id => "DocumentViewer";
			public string DisplayName => dnSpy_Resources.BgImgDisplayName_DocumentViewer;
			public double UIOrder => BackgroundImageOptionDefinitionConstants.UIOrder_DocumentViewer;
			public bool UserVisible => true;
			public DefaultImageSettings GetDefaultImageSettings() => null;
			public bool IsSupported(ITextView textView) => textView.Roles.Contains(PredefinedDsTextViewRoles.DocumentViewer);
		}

		[ExportBackgroundImageOptionDefinition(BackgroundImageOptionDefinitionConstants.AttrOrder_Repl)]
		sealed class Repl : IBackgroundImageOptionDefinition {
			public string Id => "REPL";
			public string DisplayName => dnSpy_Resources.BgImgDisplayName_REPL;
			public double UIOrder => BackgroundImageOptionDefinitionConstants.UIOrder_Repl;
			public bool UserVisible => true;
			public DefaultImageSettings GetDefaultImageSettings() => null;
			public bool IsSupported(ITextView textView) => textView.Roles.Contains(PredefinedDsTextViewRoles.ReplEditor);
		}

		[ExportBackgroundImageOptionDefinition(BackgroundImageOptionDefinitionConstants.AttrOrder_CodeEditor)]
		sealed class CodeEditor : IBackgroundImageOptionDefinition {
			public string Id => "CodeEditor";
			public string DisplayName => dnSpy_Resources.BgImgDisplayName_CodeEditor;
			public double UIOrder => BackgroundImageOptionDefinitionConstants.UIOrder_CodeEditor;
			public bool UserVisible => true;
			public DefaultImageSettings GetDefaultImageSettings() => null;
			public bool IsSupported(ITextView textView) => textView.Roles.Contains(PredefinedDsTextViewRoles.CodeEditor);
		}

		[ExportBackgroundImageOptionDefinition(BackgroundImageOptionDefinitionConstants.AttrOrder_Logger)]
		sealed class Logger : IBackgroundImageOptionDefinition {
			public string Id => "Logger";
			public string DisplayName => dnSpy_Resources.BgImgDisplayName_Logger;
			public double UIOrder => BackgroundImageOptionDefinitionConstants.UIOrder_Logger;
			public bool UserVisible => true;
			public DefaultImageSettings GetDefaultImageSettings() => null;
			public bool IsSupported(ITextView textView) => textView.Roles.Contains(PredefinedDsTextViewRoles.LogEditor);
		}
	}
}
