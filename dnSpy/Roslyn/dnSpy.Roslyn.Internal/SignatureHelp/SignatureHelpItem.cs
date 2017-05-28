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
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using dnSpy.Roslyn.Internal.Helpers;
using Microsoft.CodeAnalysis;
using SIGHLP = Microsoft.CodeAnalysis.SignatureHelp;

namespace dnSpy.Roslyn.Internal.SignatureHelp {
	sealed class SignatureHelpItem {
		public bool IsVariadic => signatureHelpItem.IsVariadic;
		public ImmutableArray<TaggedText> PrefixDisplayParts {
			get {
				if (prefixDisplayParts.IsDefault)
					Initialize();
				return prefixDisplayParts;
			}
		}
		ImmutableArray<TaggedText> prefixDisplayParts;

		public ImmutableArray<TaggedText> SuffixDisplayParts {
			get {
				if (suffixDisplayParts.IsDefault)
					Initialize();
				return suffixDisplayParts;
			}
		}
		ImmutableArray<TaggedText> suffixDisplayParts;

		public ImmutableArray<TaggedText> SeparatorDisplayParts {
			get {
				if (separatorDisplayParts.IsDefault)
					Initialize();
				return separatorDisplayParts;
			}
		}
		ImmutableArray<TaggedText> separatorDisplayParts;

		public ImmutableArray<SignatureHelpParameter> Parameters {
			get {
				if (parameters.IsDefault)
					Initialize();
				return parameters;
			}
		}
		ImmutableArray<SignatureHelpParameter> parameters;

		public ImmutableArray<TaggedText> DescriptionParts {
			get {
				if (descriptionParts.IsDefault)
					Initialize();
				return descriptionParts;
			}
		}
		ImmutableArray<TaggedText> descriptionParts;

		public Func<CancellationToken, IEnumerable<TaggedText>> DocumentationFactory {
			get {
				if (documentationFactory == null)
					Initialize();
				return documentationFactory;
			}
		}
		Func<CancellationToken, IEnumerable<TaggedText>> documentationFactory;

		readonly SIGHLP.SignatureHelpItem signatureHelpItem;

		public SignatureHelpItem(SIGHLP.SignatureHelpItem signatureHelpItem) {
			if (signatureHelpItem == null)
				throw new ArgumentNullException(nameof(signatureHelpItem));
			this.signatureHelpItem = signatureHelpItem;
		}

		void Initialize() {
			descriptionParts = signatureHelpItem.DescriptionParts;
			documentationFactory = signatureHelpItem.DocumentationFactory;
			parameters = ToSignatureHelpParameter(signatureHelpItem.Parameters);
			prefixDisplayParts = signatureHelpItem.PrefixDisplayParts;
			separatorDisplayParts = signatureHelpItem.SeparatorDisplayParts;
			suffixDisplayParts = signatureHelpItem.SuffixDisplayParts;
		}

		static ImmutableArray<SignatureHelpParameter> ToSignatureHelpParameter(ImmutableArray<SIGHLP.SignatureHelpParameter> parameters) {
			var builder = ImmutableArray.CreateBuilder<SignatureHelpParameter>(parameters.Length);
			for (int i = 0; i < parameters.Length; i++)
				builder.Add(new SignatureHelpParameter(parameters[i]));
			return builder.MoveToImmutable();
		}

		public IEnumerable<TaggedText> GetAllParts() =>
			PrefixDisplayParts.Concat(
			SeparatorDisplayParts.Concat(
			SuffixDisplayParts.Concat(
			Parameters.SelectMany(p => p.GetAllParts())).Concat(
			DescriptionParts)));
	}
}
