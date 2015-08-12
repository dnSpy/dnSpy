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

using System.Collections.Generic;
using dnSpy.Images;
using ICSharpCode.Decompiler;
using ICSharpCode.ILSpy;
using ICSharpCode.ILSpy.TextView;
using ICSharpCode.ILSpy.TreeNodes;
using ICSharpCode.NRefactory;

namespace dnSpy.TreeNodes.Hex {
	abstract class HexTreeNode : ILSpyTreeNode {
		protected abstract string Name { get; }
		protected abstract IEnumerable<HexVM> HexVMs { get; }
		protected abstract object ViewObject { get; }

		public ulong StartOffset {
			get { return startOffset; }
		}
		readonly ulong startOffset;

		public ulong EndOffset {
			get { return endOffset; }
		}
		readonly ulong endOffset;

		public override object Icon {
			get { return ImageCache.Instance.GetImage(IconName, BackgroundType.TreeNode); }
		}

		protected abstract string IconName { get; }

		protected HexTreeNode(ulong start, ulong end) {
			this.startOffset = start;
			this.endOffset = end;
		}

		public sealed override FilterResult Filter(FilterSettings settings) {
			var res = settings.Filter.GetFilterResult(this);
			if (res.FilterResult != null)
				return res.FilterResult.Value;
			return base.Filter(settings);
		}

		public sealed override object GetViewObject(DecompilerTextView textView) {
			return ViewObject;
		}

		public sealed override void Decompile(Language language, ITextOutput output, DecompilationOptions options) {
			language.WriteCommentLine(output, string.Format("{0:X8} - {1:X8} {2}", StartOffset, EndOffset, Name));
			foreach (var vm in HexVMs) {
				language.WriteCommentLine(output, string.Empty);
				language.WriteCommentLine(output, string.Format("{0}:", vm.Name));
				foreach (var field in vm.HexFields)
					language.WriteCommentLine(output, string.Format("{0:X8} - {1:X8} {2} = {3}", field.StartOffset, field.EndOffset, field.FormattedValue, field.Name));
			}
			var smartOutput = output as ISmartTextOutput;
			if (smartOutput != null)
				smartOutput.MarkAsNonCached();
		}

		protected sealed override void Write(ITextOutput output, Language language) {
			output.Write(Name, TextTokenType.Text);
		}

		public virtual void OnDocumentModified(ulong modifiedStart, ulong modifiedEnd) {
			if (!HexUtils.IsModified(StartOffset, EndOffset, modifiedStart, modifiedEnd))
				return;

			foreach (var vm in HexVMs)
				vm.OnDocumentModified(modifiedStart, modifiedEnd);
		}
	}
}
