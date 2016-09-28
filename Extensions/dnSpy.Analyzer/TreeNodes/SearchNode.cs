// Copyright (c) 2011 AlphaSierraPapa for the SharpDevelop Team
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using dnSpy.Contracts.Documents;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.TreeView;

namespace dnSpy.Analyzer.TreeNodes {
	/// <summary>
	/// Base class for analyzer nodes that perform a search.
	/// </summary>
	abstract class SearchNode : AnalyzerTreeNodeData, IAsyncCancellable {
		protected SearchNode() {
		}

		public override void Initialize() => TreeNode.LazyLoading = true;
		protected override ImageReference GetIcon(IDotNetImageService dnImgMgr) => DsImages.Search;

		public override IEnumerable<ITreeNodeData> CreateChildren() {
			Debug.Assert(asyncFetchChildrenHelper == null);
			asyncFetchChildrenHelper = new AsyncFetchChildrenHelper(this, () => asyncFetchChildrenHelper = null);
			yield break;
		}
		AsyncFetchChildrenHelper asyncFetchChildrenHelper;

		protected abstract IEnumerable<IAnalyzerTreeNodeData> FetchChildren(CancellationToken ct);
		internal IEnumerable<IAnalyzerTreeNodeData> FetchChildrenInternal(CancellationToken token) => FetchChildren(token);

		public override void OnIsVisibleChanged() {
			if (!TreeNode.IsVisible && asyncFetchChildrenHelper != null && !asyncFetchChildrenHelper.CompletedSuccessfully) {
				CancelAndClearChildren();
				TreeNode.LazyLoading = true;
			}
 		}

		public override void OnIsExpandedChanged(bool isExpanded) {
			if (!isExpanded && asyncFetchChildrenHelper != null && !asyncFetchChildrenHelper.CompletedSuccessfully) {
				CancelAndClearChildren();
				TreeNode.LazyLoading = true;
			}
		}

		public override bool HandleAssemblyListChanged(IDsDocument[] removedAssemblies, IDsDocument[] addedAssemblies) {
			// only cancel a running analysis if user has manually added/removed assemblies
			bool manualAdd = false;
			foreach (var asm in addedAssemblies) {
				if (!asm.IsAutoLoaded)
					manualAdd = true;
			}
			if (removedAssemblies.Length > 0 || manualAdd) {
				CancelAndClearChildren();
			}
			return true;
		}

		public override bool HandleModelUpdated(IDsDocument[] documents) {
			CancelAndClearChildren();
			return true;
		}

		void CancelAndClearChildren() {
			AnalyzerTreeNodeData.CancelSelfAndChildren(this);
			this.TreeNode.Children.Clear();
			this.TreeNode.LazyLoading = true;
		}

		public void Cancel() {
			asyncFetchChildrenHelper?.Cancel();
			asyncFetchChildrenHelper = null;
		}
	}
}
