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

using System.Collections.Generic;
using System.ComponentModel.Composition;
using dnSpy.Contracts.Language.Intellisense;
using dnSpy.Contracts.Text;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;

namespace dnSpy.Roslyn.Intellisense.SignatureHelp {
	[Export(typeof(ISignatureHelpSourceProvider))]
	[Name(PredefinedDsSignatureHelpSourceProviders.Roslyn)]
	[ContentType(ContentTypes.RoslynCode)]
	sealed class SignatureHelpSourceProvider : ISignatureHelpSourceProvider {
		public ISignatureHelpSource TryCreateSignatureHelpSource(ITextBuffer textBuffer) => new SignatureHelpSource();
	}

	sealed class SignatureHelpSource : ISignatureHelpSource {
		public SignatureHelpSource() { }

		public void AugmentSignatureHelpSession(ISignatureHelpSession session, IList<ISignature> signatures) =>
			SignatureHelpSession.TryGetSession(session)?.AugmentSignatureHelpSession(signatures);

		public ISignature? GetBestMatch(ISignatureHelpSession session) =>
			SignatureHelpSession.TryGetSession(session)?.GetBestMatch();

		public void Dispose() { }
	}
}
