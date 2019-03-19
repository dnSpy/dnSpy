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
using dnSpy.Roslyn.Internal.SignatureHelp;
using dnSpy.Roslyn.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;

namespace dnSpy.Roslyn.Intellisense.SignatureHelp {
	readonly struct SignatureHelpInfo {
		public SignatureHelpService SignatureHelpService { get; }
		public Document Document { get; }
		public SourceText SourceText { get; }
		public ITextSnapshot Snapshot { get; }

		SignatureHelpInfo(SignatureHelpService signatureHelpService, Document document, SourceText sourceText, ITextSnapshot snapshot) {
			SignatureHelpService = signatureHelpService;
			Document = document;
			SourceText = sourceText;
			Snapshot = snapshot;
		}

		public static SignatureHelpInfo? Create(ITextSnapshot snapshot) {
			if (snapshot == null)
				throw new ArgumentNullException(nameof(snapshot));
			var sourceText = snapshot.AsText();
			var document = sourceText.GetOpenDocumentInCurrentContextWithChanges();
			if (document == null)
				return null;
			var signatureHelpService = SignatureHelpService.GetService(document);
			if (signatureHelpService == null)
				return null;
			return new SignatureHelpInfo(signatureHelpService, document, sourceText, snapshot);
		}
	}
}
