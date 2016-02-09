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

using System.Collections.Generic;
using dnSpy.Contracts.Files.Tabs.TextEditor;
using dnSpy.Contracts.Files.TreeView;
using dnSpy.Contracts.Highlighting;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Languages;
using dnSpy.Decompiler.Shared;
using dnSpy.Shared.Decompiler;
using dnSpy.Shared.Files.TreeView;

namespace dnSpy.AsmEditor.Hex.Nodes {
	abstract class HexNode : FileTreeNodeData, IDecompileSelf {
		protected abstract IEnumerable<HexVM> HexVMs { get; }
		public abstract object VMObject { get; }

		public virtual bool IsVirtualizingCollectionVM {
			get { return false; }
		}

		public ulong StartOffset {
			get { return startOffset; }
		}
		readonly ulong startOffset;

		public ulong EndOffset {
			get { return endOffset; }
		}
		readonly ulong endOffset;

		protected sealed override ImageReference GetIcon(IDotNetImageManager dnImgMgr) {
			return new ImageReference(GetType().Assembly, IconName);
		}

		protected abstract string IconName { get; }

		protected HexNode(ulong start, ulong end) {
			this.startOffset = start;
			this.endOffset = end;
		}

		public override FilterType GetFilterType(IFileTreeNodeFilter filter) {
			return filter.GetResult(this).FilterType;
		}

		public bool Decompile(IDecompileNodeContext context) {
			context.Language.WriteCommentLine(context.Output, string.Format("{0:X8} - {1:X8} {2}", StartOffset, EndOffset, this.ToString()));
			DecompileFields(context.Language, context.Output);
			var smartOutput = context.Output as ISmartTextOutput;
			if (smartOutput != null)
				smartOutput.DontCacheOutput();
			return true;
		}

		protected virtual void DecompileFields(ILanguage language, ITextOutput output) {
			foreach (var vm in HexVMs) {
				language.WriteCommentLine(output, string.Empty);
				language.WriteCommentLine(output, string.Format("{0}:", vm.Name));
				foreach (var field in vm.HexFields)
					language.WriteCommentLine(output, string.Format("{0:X8} - {1:X8} {2} = {3}", field.StartOffset, field.EndOffset, field.FormattedValue, field.Name));
			}
		}

		protected override void Write(ISyntaxHighlightOutput output, ILanguage language) {
			Write(output);
		}

		protected abstract void Write(ISyntaxHighlightOutput output);

		public virtual void OnDocumentModified(ulong modifiedStart, ulong modifiedEnd) {
			if (!HexUtils.IsModified(StartOffset, EndOffset, modifiedStart, modifiedEnd))
				return;

			foreach (var vm in HexVMs)
				vm.OnDocumentModified(modifiedStart, modifiedEnd);
		}
	}
}
