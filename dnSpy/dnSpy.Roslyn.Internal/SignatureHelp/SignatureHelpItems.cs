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
using Microsoft.CodeAnalysis.Text;
using SIGHLP = Microsoft.CodeAnalysis.SignatureHelp;

namespace dnSpy.Roslyn.Internal.SignatureHelp {
	sealed class SignatureHelpItems {
		public TextSpan ApplicableSpan { get; }
		public int ArgumentCount { get; }
		public int ArgumentIndex { get; }
		public string ArgumentName { get; }
		public int? SelectedItemIndex { get; }
		public IList<SignatureHelpItem> Items { get; }

		public SignatureHelpItems(SIGHLP.SignatureHelpItems signatureHelpItems) {
			if (signatureHelpItems == null)
				throw new ArgumentNullException(nameof(signatureHelpItems));
			ApplicableSpan = signatureHelpItems.ApplicableSpan;
			ArgumentCount = signatureHelpItems.ArgumentCount;
			ArgumentIndex = signatureHelpItems.ArgumentIndex;
			ArgumentName = signatureHelpItems.ArgumentName;
			SelectedItemIndex = signatureHelpItems.SelectedItemIndex;
			Items = ToSignatureHelpItem(signatureHelpItems.Items);
		}

		static IList<SignatureHelpItem> ToSignatureHelpItem(IList<SIGHLP.SignatureHelpItem> items) {
			if (items.Count == 0)
				return Array.Empty<SignatureHelpItem>();
			var list = new List<SignatureHelpItem>(items.Count);
			for (int i = 0; i < items.Count; i++)
				list.Add(new SignatureHelpItem(items[i]));
			return list;
		}
	}
}
