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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using dnlib.DotNet.MD;
using dnSpy.NRefactory;
using dnSpy.Shared.UI.HexEditor;
using ICSharpCode.Decompiler;
using ICSharpCode.ILSpy;

namespace dnSpy.TreeNodes.Hex {
	public sealed class MetaDataTableTreeNode : HexTreeNode {
		public override NodePathName NodePathName {
			get { return new NodePathName("mdtblstrm", ((byte)tablesStreamVM.Table).ToString()); }
		}

		protected override object ViewObject {
			get { return tablesStreamVM; }
		}

		protected override IEnumerable<HexVM> HexVMs {
			get { yield return tablesStreamVM; }
		}

		protected override string IconName {
			get { return "MetaData"; }
		}

		protected override bool IsVirtualizingCollectionVM {
			get { return true; }
		}

		public MetaDataTableVM MetaDataTableVM {
			get { return tablesStreamVM; }
		}
		readonly MetaDataTableVM tablesStreamVM;

		// It could have tens of thousands of children so prevent loading all of them when
		// single-clicking the treenode
		public override bool SingleClickExpandsChildren {
			get { return false; }
		}

		public TableInfo TableInfo {
			get { return tableInfo; }
		}
		readonly TableInfo tableInfo;

		internal HexDocument Document {
			get { return doc; }
		}
		readonly HexDocument doc;

		public MetaDataTableTreeNode(HexDocument doc, MDTable mdTable, IMetaData md)
			: base((ulong)mdTable.StartOffset, (ulong)mdTable.EndOffset - 1) {
			LazyLoading = true;
			this.doc = doc;
			this.tableInfo = mdTable.TableInfo;
			this.tablesStreamVM = MetaDataTableVM.Create(this, doc, StartOffset, mdTable);
			this.tablesStreamVM.FindMetaDataTable = FindMetaDataTable;
			this.tablesStreamVM.InitializeHeapOffsets((ulong)md.StringsStream.StartOffset, (ulong)md.StringsStream.EndOffset - 1);
		}

		protected override void Write(ITextOutput output) {
			output.Write(string.Format("{0:X2}", (byte)tablesStreamVM.Table), TextTokenType.Number);
			output.WriteSpace();
			output.Write(string.Format("{0}", tablesStreamVM.Table), TextTokenType.Type);
			output.WriteSpace();
			output.Write("(", TextTokenType.Operator);
			output.Write(string.Format("{0}", tablesStreamVM.Rows), TextTokenType.Number);
			output.Write(")", TextTokenType.Operator);
		}

		protected override void DecompileFields(Language language, ITextOutput output) {
			language.WriteCommentLine(output, string.Empty);
			language.WriteComment(output, string.Empty);
			WriteHeader(output);

			for (int i = 0; i < (int)tablesStreamVM.Rows; i++) {
				var obj = tablesStreamVM.Get(i);
				language.WriteComment(output, string.Empty);
				Write(output, obj);
			}
		}

		public void WriteHeader(ITextOutput output) {
			var cols = tablesStreamVM.TableInfo.Columns;

			output.Write("RID\tToken\tOffset", TextTokenType.Comment);
			for (int i = 0; i < cols.Count; i++) {
				output.Write("\t", TextTokenType.Comment);
				output.Write(tablesStreamVM.GetColumnName(i), TextTokenType.Comment);
			}
			if (tablesStreamVM.HasInfo) {
				output.Write("\t", TextTokenType.Comment);
				output.Write(tablesStreamVM.InfoName, TextTokenType.Comment);
			}
			output.WriteLine();
		}

		public void Write(ITextOutput output, MetaDataTableRecordVM mdVM) {
			var cols = tablesStreamVM.TableInfo.Columns;

			output.Write(mdVM.RidString, TextTokenType.Comment);
			output.Write("\t", TextTokenType.Comment);
			output.Write(mdVM.TokenString, TextTokenType.Comment);
			output.Write("\t", TextTokenType.Comment);
			output.Write(mdVM.OffsetString, TextTokenType.Comment);
			for (int j = 0; j < cols.Count; j++) {
				output.Write("\t", TextTokenType.Comment);
				output.Write(mdVM.GetField(j).DataFieldVM.StringValue, TextTokenType.Comment);
			}
			if (tablesStreamVM.HasInfo) {
				output.Write("\t", TextTokenType.Comment);
				output.Write(mdVM.Info, TextTokenType.Comment);
			}
			output.WriteLine();
		}

		protected override void LoadChildren() {
			Debug.Assert(Children.Count == 0);

			ulong offs = StartOffset;
			ulong rowSize = (ulong)MetaDataTableVM.TableInfo.RowSize;
			for (uint i = 0; i < MetaDataTableVM.Rows; i++) {
				Children.Add(new MetaDataTableRecordTreeNode(tableInfo, (int)i, offs, offs + rowSize - 1));
				offs += rowSize;
			}
		}

		public override void OnDocumentModified(ulong modifiedStart, ulong modifiedEnd) {
			base.OnDocumentModified(modifiedStart, modifiedEnd);

			if (Children.Count == 0)
				return;

			if (!HexUtils.IsModified(StartOffset, EndOffset, modifiedStart, modifiedEnd))
				return;

			ulong start = Math.Max(StartOffset, modifiedStart);
			ulong end = Math.Min(EndOffset, modifiedEnd);
			int i = (int)((start - StartOffset) / (ulong)tableInfo.RowSize);
			int endi = (int)((end - StartOffset) / (ulong)tableInfo.RowSize);
			Debug.Assert(0 <= i && i <= endi && endi < Children.Count);
			while (i <= endi) {
				var obj = (MetaDataTableRecordTreeNode)Children[i];
				obj.OnDocumentModified(modifiedStart, modifiedEnd);
				i++;
			}
		}

		public MetaDataTableRecordTreeNode FindTokenNode(uint token) {
			uint rid = token & 0x00FFFFFF;
			if (rid - 1 >= tablesStreamVM.Rows)
				return null;
			EnsureChildrenFiltered();
			return (MetaDataTableRecordTreeNode)Children[(int)rid - 1];
		}

		MetaDataTableVM FindMetaDataTable(Table table) {
			return ((TablesStreamTreeNode)Parent).FindMetaDataTable(table);
		}
	}
}
