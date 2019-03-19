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
using System.Linq;
using System.Text;
using System.Threading;
using dnSpy.Roslyn.Internal.SignatureHelp;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;

namespace dnSpy.Roslyn.Intellisense.SignatureHelp {
	sealed class Parameter : IParameter {
		public ISignature Signature { get; }
		public string Name => parameter.Name;
		public Span Locus { get; }
		public Span PrettyPrintedLocus { get; }

		public TaggedText[] DocumentationTaggedText {
			get {
				if (documentationTaggedText == null)
					InitializeDocumentation();
				return documentationTaggedText;
			}
		}
		TaggedText[] documentationTaggedText;

		public string Documentation {
			get {
				if (documentation == null)
					InitializeDocumentation();
				return documentation;
			}
		}
		string documentation;

		readonly SignatureHelpParameter parameter;

		public Parameter(Signature signature, SignatureHelpParameter parameter, Span locus, Span prettyPrintedLocus) {
			Signature = signature ?? throw new ArgumentNullException(nameof(signature));
			Locus = locus;
			PrettyPrintedLocus = prettyPrintedLocus;
			this.parameter = parameter ?? throw new ArgumentNullException(nameof(parameter));
		}

		void InitializeDocumentation() {
			documentationTaggedText = parameter.DocumentationFactory(CancellationToken.None).ToArray();
			documentation = ToString(documentationTaggedText);
		}

		string ToString(TaggedText[] parts) {
			var sb = new StringBuilder();
			foreach (var taggedText in parts)
				sb.Append(taggedText.Text);
			return sb.ToString();
		}
	}
}
