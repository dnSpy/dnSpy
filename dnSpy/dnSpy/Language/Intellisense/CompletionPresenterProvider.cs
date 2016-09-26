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
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Language.Intellisense;
using dnSpy.Contracts.Text;
using dnSpy.Text.MEF;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Utilities;

namespace dnSpy.Language.Intellisense {
	[Export(typeof(IIntellisensePresenterProvider))]
	[Name(PredefinedIntellisensePresenterProviders.DefaultCompletionPresenter)]
	[ContentType(ContentTypes.Any)]
	sealed class CompletionPresenterProvider : IIntellisensePresenterProvider {
		readonly IImageMonikerService imageMonikerService;
		readonly IImageService imageService;
		readonly Lazy<ICompletionTextElementProviderService> completionTextElementProviderService;
		readonly Lazy<IUIElementProvider<Completion, ICompletionSession>, IOrderableContentTypeMetadata>[] completionUIElementProviders;

		[ImportingConstructor]
		CompletionPresenterProvider(IImageMonikerService imageMonikerService, IImageService imageService, Lazy<ICompletionTextElementProviderService> completionTextElementProviderService, [ImportMany] IEnumerable<Lazy<IUIElementProvider<Completion, ICompletionSession>, IOrderableContentTypeMetadata>> completionUIElementProviders) {
			this.imageMonikerService = imageMonikerService;
			this.imageService = imageService;
			this.completionTextElementProviderService = completionTextElementProviderService;
			this.completionUIElementProviders = Orderer.Order(completionUIElementProviders).ToArray();
		}

		public IIntellisensePresenter TryCreateIntellisensePresenter(IIntellisenseSession session) {
			var completionSession = session as ICompletionSession;
			if (completionSession == null)
				return null;
			return new CompletionPresenter(imageMonikerService, imageService, completionSession, completionTextElementProviderService.Value.Create(), completionUIElementProviders);
		}
	}
}
