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
using System.Linq;
using System.Threading;
using dnSpy.Roslyn.Internal.Helpers;
using Microsoft.CodeAnalysis;
using SIGHLP = Microsoft.CodeAnalysis.SignatureHelp;

namespace dnSpy.Roslyn.Internal.SignatureHelp {
	sealed class SignatureHelpParameter {
		public string Name => signatureHelpParameter.Name;
		public bool IsOptional => signatureHelpParameter.IsOptional;

		public Func<CancellationToken, IEnumerable<TaggedText>> DocumentationFactory {
			get {
				if (documentationFactory == null)
					Initialize();
				return documentationFactory;
			}
		}
		Func<CancellationToken, IEnumerable<TaggedText>> documentationFactory;

		public IList<TaggedText> PrefixDisplayParts {
			get {
				if (prefixDisplayParts == null)
					Initialize();
				return prefixDisplayParts;
			}
		}
		IList<TaggedText> prefixDisplayParts;

		public IList<TaggedText> SuffixDisplayParts {
			get {
				if (suffixDisplayParts == null)
					Initialize();
				return suffixDisplayParts;
			}
		}
		IList<TaggedText> suffixDisplayParts;

		public IList<TaggedText> DisplayParts {
			get {
				if (displayParts == null)
					Initialize();
				return displayParts;
			}
		}
		IList<TaggedText> displayParts;

		public IList<TaggedText> SelectedDisplayParts {
			get {
				if (selectedDisplayParts == null)
					Initialize();
				return selectedDisplayParts;
			}
		}
		IList<TaggedText> selectedDisplayParts;

		readonly SIGHLP.SignatureHelpParameter signatureHelpParameter;

		public SignatureHelpParameter(SIGHLP.SignatureHelpParameter signatureHelpParameter) {
			if (signatureHelpParameter == null)
				throw new ArgumentNullException(nameof(signatureHelpParameter));
			this.signatureHelpParameter = signatureHelpParameter;
		}

		void Initialize() {
			documentationFactory = signatureHelpParameter.DocumentationFactory;
			prefixDisplayParts = signatureHelpParameter.PrefixDisplayParts;
			suffixDisplayParts = signatureHelpParameter.SuffixDisplayParts;
			displayParts = signatureHelpParameter.DisplayParts;
			selectedDisplayParts = signatureHelpParameter.SelectedDisplayParts;
		}

		public IEnumerable<TaggedText> GetAllParts() =>
			PrefixDisplayParts.Concat(DisplayParts).Concat(SuffixDisplayParts).Concat(SelectedDisplayParts);
	}
}
