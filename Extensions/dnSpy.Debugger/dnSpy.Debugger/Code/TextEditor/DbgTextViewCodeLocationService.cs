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
using System.ComponentModel.Composition;
using System.Linq;
using dnSpy.Contracts.Debugger.Code.TextEditor;
using dnSpy.Contracts.Documents.Tabs;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace dnSpy.Debugger.Code.TextEditor {
	abstract class DbgTextViewCodeLocationService {
		public abstract IEnumerable<DbgTextViewBreakpointLocationResult> CreateLocation(IDocumentTab tab, ITextView textView, VirtualSnapshotPoint position);
	}

	[Export(typeof(DbgTextViewCodeLocationService))]
	sealed class DbgTextViewCodeLocationServiceImpl : DbgTextViewCodeLocationService {
		readonly Lazy<DbgTextViewCodeLocationProvider>[] dbgTextViewCodeLocationProviders;

		[ImportingConstructor]
		DbgTextViewCodeLocationServiceImpl([ImportMany] IEnumerable<Lazy<DbgTextViewCodeLocationProvider>> dbgTextViewCodeLocationProviders) =>
			this.dbgTextViewCodeLocationProviders = dbgTextViewCodeLocationProviders.ToArray();

		public override IEnumerable<DbgTextViewBreakpointLocationResult> CreateLocation(IDocumentTab tab, ITextView textView, VirtualSnapshotPoint position) {
			if (tab == null)
				throw new ArgumentNullException(nameof(tab));
			if (textView == null)
				throw new ArgumentNullException(nameof(textView));
			if (position.Position.Snapshot == null)
				throw new ArgumentException();
			foreach (var lz in dbgTextViewCodeLocationProviders) {
				var res = lz.Value.CreateLocation(tab, textView, position);
				if (res != null)
					yield return res.Value;
			}
		}
	}
}
