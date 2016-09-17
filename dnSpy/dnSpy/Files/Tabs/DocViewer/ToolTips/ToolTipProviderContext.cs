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
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Files.Tabs.DocViewer;
using dnSpy.Contracts.Files.Tabs.DocViewer.ToolTips;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Text.Classification;
using Microsoft.VisualStudio.Text.Classification;

namespace dnSpy.Files.Tabs.DocViewer.ToolTips {
	sealed class ToolTipProviderContext : IDocumentViewerToolTipProviderContext {
		public IDocumentViewer DocumentViewer { get; }
		public IDecompiler Decompiler { get; }

		readonly IImageManager imageManager;
		readonly IDotNetImageManager dotNetImageManager;
		readonly ICodeToolTipSettings codeToolTipSettings;
		readonly IClassificationFormatMap classificationFormatMap;
		readonly IThemeClassificationTypeService themeClassificationTypeService;

		public ToolTipProviderContext(IImageManager imageManager, IDotNetImageManager dotNetImageManager, IDecompiler decompiler, ICodeToolTipSettings codeToolTipSettings, IDocumentViewer documentViewer, IClassificationFormatMap classificationFormatMap, IThemeClassificationTypeService themeClassificationTypeService) {
			if (imageManager == null)
				throw new ArgumentNullException(nameof(imageManager));
			if (dotNetImageManager == null)
				throw new ArgumentNullException(nameof(dotNetImageManager));
			if (decompiler == null)
				throw new ArgumentNullException(nameof(decompiler));
			if (codeToolTipSettings == null)
				throw new ArgumentNullException(nameof(codeToolTipSettings));
			if (documentViewer == null)
				throw new ArgumentNullException(nameof(documentViewer));
			if (classificationFormatMap == null)
				throw new ArgumentNullException(nameof(classificationFormatMap));
			if (themeClassificationTypeService == null)
				throw new ArgumentNullException(nameof(themeClassificationTypeService));
			this.DocumentViewer = documentViewer;
			this.imageManager = imageManager;
			this.dotNetImageManager = dotNetImageManager;
			this.Decompiler = decompiler;
			this.codeToolTipSettings = codeToolTipSettings;
			this.classificationFormatMap = classificationFormatMap;
			this.themeClassificationTypeService = themeClassificationTypeService;
		}

		public ICodeToolTipProvider Create() =>
			new CodeToolTipProvider(imageManager, dotNetImageManager, classificationFormatMap, themeClassificationTypeService, codeToolTipSettings.SyntaxHighlight);
	}
}
