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
using System.Linq;
using dnSpy.Contracts.Files.Tabs.DocViewer.ToolTips;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Languages;

namespace dnSpy.Files.Tabs.DocViewer.ToolTips {
	interface ICodeToolTipManager {
		object CreateToolTip(ILanguage language, object @ref);
	}

	[Export(typeof(ICodeToolTipManager))]
	sealed class CodeToolTipManager : ICodeToolTipManager {
		readonly IImageManager imageManager;
		readonly IDotNetImageManager dotNetImageManager;
		readonly ICodeToolTipSettings codeToolTipSettings;
		readonly Lazy<IToolTipContentCreator, IToolTipContentCreatorMetadata>[] creators;

		[ImportingConstructor]
		CodeToolTipManager(IImageManager imageManager, IDotNetImageManager dotNetImageManager, ICodeToolTipSettings codeToolTipSettings, [ImportMany] IEnumerable<Lazy<IToolTipContentCreator, IToolTipContentCreatorMetadata>> mefCreators) {
			this.imageManager = imageManager;
			this.dotNetImageManager = dotNetImageManager;
			this.codeToolTipSettings = codeToolTipSettings;
			this.creators = mefCreators.OrderBy(a => a.Metadata.Order).ToArray();
		}

		public object CreateToolTip(ILanguage language, object @ref) {
			if (language == null)
				return null;
			if (@ref == null)
				return null;

			var ctx = new ToolTipContentCreatorContext(imageManager, dotNetImageManager, language, codeToolTipSettings);
			foreach (var creator in creators) {
				var ttObj = creator.Value.Create(ctx, @ref);
				if (ttObj != null)
					return ttObj;
			}

			return null;
		}
	}
}
