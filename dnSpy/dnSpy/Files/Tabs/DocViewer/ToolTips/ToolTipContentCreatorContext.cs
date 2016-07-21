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

using dnSpy.Contracts.Files.Tabs.DocViewer.ToolTips;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Languages;

namespace dnSpy.Files.Tabs.DocViewer.ToolTips {
	sealed class ToolTipContentCreatorContext : IToolTipContentCreatorContext {
		public ILanguage Language { get; }

		readonly IImageManager imageManager;
		readonly IDotNetImageManager dotNetImageManager;
		readonly ICodeToolTipSettings codeToolTipSettings;

		public ToolTipContentCreatorContext(IImageManager imageManager, IDotNetImageManager dotNetImageManager, ILanguage language, ICodeToolTipSettings codeToolTipSettings) {
			this.imageManager = imageManager;
			this.dotNetImageManager = dotNetImageManager;
			this.Language = language;
			this.codeToolTipSettings = codeToolTipSettings;
		}

		public ICodeToolTipCreator Create() =>
			new CodeToolTipCreator(imageManager, dotNetImageManager, codeToolTipSettings.SyntaxHighlight);
	}
}
