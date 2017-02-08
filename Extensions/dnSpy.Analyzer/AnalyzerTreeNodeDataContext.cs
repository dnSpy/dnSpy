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

using dnSpy.Analyzer.TreeNodes;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Documents;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.TreeView;
using dnSpy.Contracts.TreeView.Text;

namespace dnSpy.Analyzer {
	sealed class AnalyzerTreeNodeDataContext : IAnalyzerTreeNodeDataContext {
		public IDotNetImageService DotNetImageService { get; set; }
		public ITreeView TreeView { get; set; }
		public IDecompiler Decompiler { get; set; }
		public ITreeViewNodeTextElementProvider TreeViewNodeTextElementProvider { get; set; }
		public IDsDocumentService DocumentService { get; set; }
		public IAnalyzerService AnalyzerService { get; set; }
		public bool ShowToken { get; set; }
		public bool SingleClickExpandsChildren { get; set; }
		public bool SyntaxHighlight { get; set; }
		public bool UseNewRenderer { get; set; }
	}
}
