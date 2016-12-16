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
using dnSpy.AsmEditor.Hex.PE;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Documents.Tabs.DocViewer;
using dnSpy.Contracts.Documents.TreeView;
using dnSpy.Contracts.Hex;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Text;

namespace dnSpy.AsmEditor.Hex.Nodes {
	abstract class HexNode : DocumentTreeNodeData, IDecompileSelf {
		protected abstract IEnumerable<HexVM> HexVMs { get; }
		public abstract object VMObject { get; }
		public virtual bool IsVirtualizingCollectionVM => false;
		public HexSpan Span { get; }
		protected sealed override ImageReference GetIcon(IDotNetImageService dnImgMgr) => IconReference;
		protected abstract ImageReference IconReference { get; }

		protected HexNode(HexSpan span) {
			Span = span;
		}

		public override FilterType GetFilterType(IDocumentTreeNodeFilter filter) => filter.GetResultOther(this).FilterType;

		public bool Decompile(IDecompileNodeContext context) {
			context.ContentTypeString = context.Decompiler.ContentTypeString;
			context.Decompiler.WriteCommentLine(context.Output, string.Format("{0:X8} - {1:X8} {2}", Span.Start.ToUInt64(), Span.End.ToUInt64() - 1, ToString()));
			DecompileFields(context.Decompiler, context.Output);
			(context.Output as IDocumentViewerOutput)?.DisableCaching();
			return true;
		}

		protected virtual void DecompileFields(IDecompiler decompiler, IDecompilerOutput output) {
			foreach (var vm in HexVMs) {
				decompiler.WriteCommentLine(output, string.Empty);
				decompiler.WriteCommentLine(output, string.Format("{0}:", vm.Name));
				foreach (var field in vm.HexFields)
					decompiler.WriteCommentLine(output, string.Format("{0:X8} - {1:X8} {2} = {3}", field.Span.Start.ToUInt64(), field.Span.End.ToUInt64() - 1, field.FormattedValue, field.Name));
			}
		}

		protected override void WriteCore(ITextColorWriter output, IDecompiler decompiler, DocumentNodeWriteOptions options) => WriteCore(output, options);
		protected abstract void WriteCore(ITextColorWriter output, DocumentNodeWriteOptions options);

		public virtual void OnBufferChanged(NormalizedHexChangeCollection changes) {
			if (!changes.OverlapsWith(Span))
				return;

			foreach (var vm in HexVMs)
				vm.OnBufferChanged(changes);
		}
	}
}
