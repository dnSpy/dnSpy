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
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Utilities;

namespace dnSpy.Roslyn.Internal.QuickInfo {
	[ExportLanguageServiceFactory(typeof(QuickInfoService), LanguageNames.CSharp), Shared]
	sealed class CSharpQuickInfoServiceFactory : ILanguageServiceFactory {
		readonly Lazy<IQuickInfoProvider, OrderableLanguageMetadata>[] quickInfoProviders;

		[ImportingConstructor]
		public CSharpQuickInfoServiceFactory([ImportMany] IEnumerable<Lazy<IQuickInfoProvider, OrderableLanguageMetadata>> quickInfoProviders) {
			this.quickInfoProviders = quickInfoProviders.Where(a => a.Metadata.Language == LanguageNames.CSharp).ToArray();
		}

		public ILanguageService CreateLanguageService(HostLanguageServices languageServices) =>
			new CSharpQuickInfoService(quickInfoProviders);
	}

	sealed class CSharpQuickInfoService : QuickInfoService {
		public override string Language => LanguageNames.CSharp;
		public CSharpQuickInfoService(Lazy<IQuickInfoProvider, OrderableLanguageMetadata>[] quickInfoProviders)
			: base(quickInfoProviders) {
		}
	}

	[ExportLanguageServiceFactory(typeof(QuickInfoService), LanguageNames.VisualBasic), Shared]
	sealed class VisualBasicQuickInfoServiceFactory : ILanguageServiceFactory {
		readonly Lazy<IQuickInfoProvider, OrderableLanguageMetadata>[] quickInfoProviders;

		[ImportingConstructor]
		public VisualBasicQuickInfoServiceFactory([ImportMany] IEnumerable<Lazy<IQuickInfoProvider, OrderableLanguageMetadata>> quickInfoProviders) {
			this.quickInfoProviders = quickInfoProviders.Where(a => a.Metadata.Language == LanguageNames.VisualBasic).ToArray();
		}

		public ILanguageService CreateLanguageService(HostLanguageServices languageServices) =>
			new VisualBasicQuickInfoService(quickInfoProviders);
	}

	sealed class VisualBasicQuickInfoService : QuickInfoService {
		public override string Language => LanguageNames.VisualBasic;
		public VisualBasicQuickInfoService(Lazy<IQuickInfoProvider, OrderableLanguageMetadata>[] quickInfoProviders)
			: base(quickInfoProviders) {
		}
	}

	abstract partial class QuickInfoService : ILanguageService {
		readonly IQuickInfoProvider[] quickInfoProviders;

		public abstract string Language { get; }

		protected QuickInfoService(Lazy<IQuickInfoProvider, OrderableLanguageMetadata>[] quickInfoProviders) {
			this.quickInfoProviders = ExtensionOrderer.Order(quickInfoProviders).Select(a => a.Value).ToArray();
		}

		public static QuickInfoService GetService(Document document) {
			if (document == null)
				throw new ArgumentNullException(nameof(document));
			return document.Project.LanguageServices.GetService<QuickInfoService>();
		}

		public async Task<QuickInfoItem> GetItemAsync(Document document, int position, CancellationToken cancellationToken = default(CancellationToken)) {
			foreach (var p in quickInfoProviders) {
				var item = await p.GetItemAsync(document, position, cancellationToken);
				if (item != null)
					return item;
			}
			return null;
		}
	}
}
