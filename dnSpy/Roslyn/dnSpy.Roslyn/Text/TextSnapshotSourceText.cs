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
using System.Text;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;

namespace dnSpy.Roslyn.Text {
	sealed class TextSnapshotSourceText : SourceText {
		public override char this[int position] => TextSnapshot[position];
		public override Encoding Encoding { get; }
		public override int Length => TextSnapshot.Length;
		public override SourceTextContainer Container { get; }
		public ITextSnapshot TextSnapshot { get; }

		public TextSnapshotSourceText(ITextSnapshot snapshot, Encoding encoding) {
			TextSnapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot));
			Encoding = encoding;
			Container = snapshot.TextBuffer.AsTextContainer();
		}

		public override void CopyTo(int sourceIndex, char[] destination, int destinationIndex, int count) =>
			TextSnapshot.CopyTo(sourceIndex, destination, destinationIndex, count);
		protected override TextLineCollection GetLinesCore() => new TextSnapshotTextLineCollection(this);
		public override string ToString(TextSpan span) => TextSnapshot.GetText(span.ToSpan());
	}
}
