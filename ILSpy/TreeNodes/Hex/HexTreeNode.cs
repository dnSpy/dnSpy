/*
    Copyright (C) 2014-2015 de4dot@gmail.com

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

using ICSharpCode.Decompiler;
using ICSharpCode.ILSpy;
using ICSharpCode.ILSpy.TextView;
using ICSharpCode.ILSpy.TreeNodes;
using ICSharpCode.NRefactory;

namespace dnSpy.TreeNodes.Hex {
	abstract class HexTreeNode : ILSpyTreeNode {
		protected abstract string Name { get; }
		protected abstract object ViewObject { get; }

		public ulong StartOffset {
			get { return startOffset; }
		}
		readonly ulong startOffset;

		public ulong EndOffset {
			get { return endOffset; }
		}
		readonly ulong endOffset;

		protected HexTreeNode(ulong start, ulong end) {
			this.startOffset = start;
			this.endOffset = end;
		}

		public sealed override FilterResult Filter(FilterSettings settings) {
			//TODO:
			return base.Filter(settings);
		}

		public sealed override object GetViewObject(DecompilerTextView textView) {
			return ViewObject;
		}

		public sealed override void Decompile(Language language, ITextOutput output, DecompilationOptions options) {
			//TODO: Write comments and make sure output isn't cached
		}

		protected sealed override void Write(ITextOutput output, Language language) {
			output.Write(Name, TextTokenType.Text);
		}

		public void OnDocumentModified(ulong modifiedStart, ulong modifiedEnd) {
			if (!HexUtils.IsModified(StartOffset, EndOffset, modifiedStart, modifiedEnd))
				return;

			OnDocumentModifiedOverride(modifiedStart, modifiedEnd);
		}

		protected abstract void OnDocumentModifiedOverride(ulong modifiedStart, ulong modifiedEnd);
	}
}
