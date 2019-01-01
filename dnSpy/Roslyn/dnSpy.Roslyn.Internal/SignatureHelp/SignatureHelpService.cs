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
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.SignatureHelp;
using SIGHLP = Microsoft.CodeAnalysis.SignatureHelp;

namespace dnSpy.Roslyn.Internal.SignatureHelp {
	[ExportLanguageServiceFactory(typeof(SignatureHelpService), LanguageNames.CSharp), Shared]
	sealed class CSharpSignatureHelpServiceFactory : ILanguageServiceFactory {
		readonly Lazy<ISignatureHelpProvider, OrderableLanguageMetadata>[] signatureHelpProviders;

		[ImportingConstructor]
		public CSharpSignatureHelpServiceFactory([ImportMany] IEnumerable<Lazy<ISignatureHelpProvider, OrderableLanguageMetadata>> signatureHelpProviders) {
			this.signatureHelpProviders = signatureHelpProviders.Where(a => a.Metadata.Language == LanguageNames.CSharp).ToArray();
		}

		public ILanguageService CreateLanguageService(HostLanguageServices languageServices) =>
			new CSharpSignatureHelpService(signatureHelpProviders);
	}

	sealed class CSharpSignatureHelpService : SignatureHelpService {
		public override string Language => LanguageNames.CSharp;
		public CSharpSignatureHelpService(Lazy<ISignatureHelpProvider, OrderableLanguageMetadata>[] signatureHelpProviders)
			: base(signatureHelpProviders) {
		}
	}

	[ExportLanguageServiceFactory(typeof(SignatureHelpService), LanguageNames.VisualBasic), Shared]
	sealed class VisualBasicSignatureHelpServiceFactory : ILanguageServiceFactory {
		readonly Lazy<ISignatureHelpProvider, OrderableLanguageMetadata>[] signatureHelpProviders;

		[ImportingConstructor]
		public VisualBasicSignatureHelpServiceFactory([ImportMany] IEnumerable<Lazy<ISignatureHelpProvider, OrderableLanguageMetadata>> signatureHelpProviders) {
			this.signatureHelpProviders = signatureHelpProviders.Where(a => a.Metadata.Language == LanguageNames.VisualBasic).ToArray();
		}

		public ILanguageService CreateLanguageService(HostLanguageServices languageServices) =>
			new VisualBasicSignatureHelpService(signatureHelpProviders);
	}

	sealed class VisualBasicSignatureHelpService : SignatureHelpService {
		public override string Language => LanguageNames.VisualBasic;
		public VisualBasicSignatureHelpService(Lazy<ISignatureHelpProvider, OrderableLanguageMetadata>[] signatureHelpProviders)
			: base(signatureHelpProviders) {
		}
	}

	sealed class SignatureHelpResult {
		public SignatureHelpItems Items { get; }
		public SignatureHelpItem SelectedItem { get; }
		public int? SelectedParameter { get; }

		public SignatureHelpResult(SignatureHelpItems items, SignatureHelpItem selectedItem, int? selectedParameter) {
			Items = items;
			SelectedItem = selectedItem;
			SelectedParameter = selectedParameter;
		}
	}

	abstract partial class SignatureHelpService : ILanguageService {
		readonly ISignatureHelpProvider[] signatureHelpProviders;

		public abstract string Language { get; }

		protected SignatureHelpService(Lazy<ISignatureHelpProvider, OrderableLanguageMetadata>[] signatureHelpProviders) {
			this.signatureHelpProviders = ExtensionOrderer.Order(signatureHelpProviders).Select(a => a.Value).ToArray();
		}

		public static SignatureHelpService GetService(Document document) {
			if (document == null)
				throw new ArgumentNullException(nameof(document));
			return document.Project.LanguageServices.GetService<SignatureHelpService>();
		}

		public async Task<SignatureHelpResult> GetItemsAsync(Document document, int position, SignatureHelpTriggerInfo triggerInfo, CancellationToken cancellationToken = default(CancellationToken)) {
			var res = await ComputeItemsAsync(signatureHelpProviders, position, triggerInfo.ToSignatureHelpTriggerInfo(), document, cancellationToken).ConfigureAwait(false);
			return GetSignatureHelpResult(res, document);
		}

		public bool IsTriggerCharacter(char ch) {
			foreach (var p in signatureHelpProviders) {
				if (p.IsTriggerCharacter(ch))
					return true;
			}
			return false;
		}

		public bool IsRetriggerCharacter(char ch) {
			foreach (var p in signatureHelpProviders) {
				if (p.IsRetriggerCharacter(ch))
					return true;
			}
			return false;
		}
	}

	enum SignatureHelpTriggerReason {
		InvokeSignatureHelpCommand,
		TypeCharCommand,
		RetriggerCommand,
	}

	readonly struct SignatureHelpTriggerInfo {
		public SignatureHelpTriggerReason TriggerReason { get; }
		public char? TriggerCharacter { get; }
		public SignatureHelpTriggerInfo(SignatureHelpTriggerReason triggerReason, char? triggerCharacter = null) {
			if (triggerReason == SignatureHelpTriggerReason.TypeCharCommand && triggerCharacter == null)
				throw new ArgumentException();
			TriggerReason = triggerReason;
			TriggerCharacter = triggerCharacter;
		}

		public SIGHLP.SignatureHelpTriggerInfo ToSignatureHelpTriggerInfo() => new SIGHLP.SignatureHelpTriggerInfo(ToSignatureHelpTriggerReason(TriggerReason), TriggerCharacter);

		static SIGHLP.SignatureHelpTriggerReason ToSignatureHelpTriggerReason(SignatureHelpTriggerReason triggerReason) {
			switch (triggerReason) {
			case SignatureHelpTriggerReason.InvokeSignatureHelpCommand:	return SIGHLP.SignatureHelpTriggerReason.InvokeSignatureHelpCommand;
			case SignatureHelpTriggerReason.TypeCharCommand:			return SIGHLP.SignatureHelpTriggerReason.TypeCharCommand;
			case SignatureHelpTriggerReason.RetriggerCommand:			return SIGHLP.SignatureHelpTriggerReason.RetriggerCommand;
			default: throw new ArgumentOutOfRangeException(nameof(triggerReason));
			}
		}
	}
}
