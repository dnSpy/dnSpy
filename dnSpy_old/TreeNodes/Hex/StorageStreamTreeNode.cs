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
using System.Diagnostics;
using dnlib.DotNet.MD;
using dnSpy.NRefactory;
using dnSpy.Shared.UI.HexEditor;
using ICSharpCode.Decompiler;

namespace dnSpy.TreeNodes.Hex {
	public enum StorageStreamType {
		None,
		Strings,
		US,
		Blob,
		Guid,
		Tables,
		HotHeap,
	}

	public sealed class StorageStreamTreeNode : HexTreeNode {
		public StorageStreamType StorageStreamType {
			get { return storageStreamType; }
		}
		readonly StorageStreamType storageStreamType;

		public override NodePathName NodePathName {
			get { return new NodePathName("strgstrm", streamNumber.ToString()); }
		}

		protected override object ViewObject {
			get { return storageStreamVM; }
		}

		protected override IEnumerable<HexVM> HexVMs {
			get { yield return storageStreamVM; }
		}

		protected override string IconName {
			get { return "BinaryFile"; }
		}

		public int StreamNumber {
			get { return streamNumber; }
		}
		readonly int streamNumber;

		readonly StorageStreamVM storageStreamVM;

		public StorageStreamTreeNode(HexDocument doc, StreamHeader sh, int streamNumber, DotNetStream knownStream, IMetaData md)
			: base((ulong)sh.StartOffset, (ulong)sh.EndOffset - 1) {
			this.streamNumber = streamNumber;
			this.storageStreamType = GetStorageStreamType(knownStream);
			this.storageStreamVM = new StorageStreamVM(this, doc, StartOffset, (int)(EndOffset - StartOffset + 1 - 8));

			var tblStream = knownStream as TablesStream;
			if (tblStream != null)
				this.Children.Add(new TablesStreamTreeNode(doc, tblStream, md));
		}

		static StorageStreamType GetStorageStreamType(DotNetStream stream) {
			if (stream == null)
				return StorageStreamType.None;
			if (stream is StringsStream)
				return StorageStreamType.Strings;
			if (stream is USStream)
				return StorageStreamType.US;
			if (stream is BlobStream)
				return StorageStreamType.Blob;
			if (stream is GuidStream)
				return StorageStreamType.Guid;
			if (stream is TablesStream)
				return StorageStreamType.Tables;
			if (stream.Name == "#!")
				return StorageStreamType.HotHeap;
			Debug.Fail(string.Format("Shouldn't be here when stream is a known stream type: {0}", stream.GetType()));
			return StorageStreamType.None;
		}

		public override void OnDocumentModified(ulong modifiedStart, ulong modifiedEnd) {
			base.OnDocumentModified(modifiedStart, modifiedEnd);
			if (HexUtils.IsModified(storageStreamVM.RCNameVM.StartOffset, storageStreamVM.RCNameVM.EndOffset, modifiedStart, modifiedEnd))
				RaisePropertyChanged("Text");

			foreach (HexTreeNode node in Children)
				node.OnDocumentModified(modifiedStart, modifiedEnd);
		}

		protected override void Write(ITextOutput output) {
			output.Write("Storage", TextTokenType.InstanceField);
			output.WriteSpace();
			output.Write("Stream", TextTokenType.InstanceField);
			output.WriteSpace();
			output.Write("#", TextTokenType.Operator);
			output.Write(streamNumber.ToString(), TextTokenType.Number);
			output.Write(":", TextTokenType.Operator);
			output.WriteSpace();
			output.Write(string.Format("{0}", storageStreamVM.RCNameVM.StringZ), storageStreamType == StorageStreamType.None ? TextTokenType.Error : TextTokenType.Type);
		}

		public MetaDataTableRecordTreeNode FindTokenNode(uint token) {
			if (StorageStreamType != StorageStreamType.Tables)
				return null;
			return ((TablesStreamTreeNode)Children[0]).FindTokenNode(token);
		}
	}
}
