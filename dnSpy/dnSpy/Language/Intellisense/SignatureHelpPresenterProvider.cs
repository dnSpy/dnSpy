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

using System.ComponentModel.Composition;
using dnSpy.Contracts.Language.Intellisense;
using dnSpy.Contracts.Settings.AppearanceCategory;
using dnSpy.Contracts.Text;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

namespace dnSpy.Language.Intellisense {
	[Export(typeof(IIntellisensePresenterProvider))]
	[Name(PredefinedIntellisensePresenterProviders.DefaultSignatureHelpPresenter)]
	[ContentType(ContentTypes.Any)]
	sealed class SignatureHelpPresenterProvider : IIntellisensePresenterProvider {
		readonly ITextBufferFactoryService textBufferFactoryService;
		readonly IContentTypeRegistryService contentTypeRegistryService;
		readonly IClassifierAggregatorService classifierAggregatorService;
		readonly IClassificationFormatMapService classificationFormatMapService;

		[ImportingConstructor]
		SignatureHelpPresenterProvider(ITextBufferFactoryService textBufferFactoryService, IContentTypeRegistryService contentTypeRegistryService, IClassifierAggregatorService classifierAggregatorService, IClassificationFormatMapService classificationFormatMapService) {
			this.textBufferFactoryService = textBufferFactoryService;
			this.contentTypeRegistryService = contentTypeRegistryService;
			this.classifierAggregatorService = classifierAggregatorService;
			this.classificationFormatMapService = classificationFormatMapService;
		}

		public IIntellisensePresenter? TryCreateIntellisensePresenter(IIntellisenseSession session) {
			var signatureHelpSession = session as ISignatureHelpSession;
			if (signatureHelpSession is null)
				return null;
			return new SignatureHelpPresenter(signatureHelpSession, textBufferFactoryService, contentTypeRegistryService, classifierAggregatorService, classificationFormatMapService.GetClassificationFormatMap(AppearanceCategoryConstants.UIMisc));
		}
	}
}
