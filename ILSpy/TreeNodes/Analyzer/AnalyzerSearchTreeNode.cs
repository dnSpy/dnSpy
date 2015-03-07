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

using System;
using System.Collections.Generic;
using System.Threading;

namespace ICSharpCode.ILSpy.TreeNodes.Analyzer
{
	/// <summary>
	/// Base class for analyzer nodes that perform a search.
	/// </summary>
	public abstract class AnalyzerSearchTreeNode : AnalyzerTreeNode
	{
		private readonly ThreadingSupport threading = new ThreadingSupport();
		
		protected AnalyzerSearchTreeNode()
		{
			this.LazyLoading = true;
		}
		
		public override object Icon
		{
			get { return Images.Search; }
		}
		
		protected override void LoadChildren()
		{
			threading.LoadChildren(this, FetchChildren);
		}
		
		protected abstract IEnumerable<AnalyzerTreeNode> FetchChildren(CancellationToken ct);
		
		protected override void OnIsVisibleChanged()
		{
			base.OnIsVisibleChanged();
			if (!this.IsVisible && threading.IsRunning) {
				this.LazyLoading = true;
				threading.Cancel();
				this.Children.Clear();
			}
		}
		
		public override bool HandleAssemblyListChanged(ICollection<LoadedAssembly> removedAssemblies, ICollection<LoadedAssembly> addedAssemblies)
		{
			// only cancel a running analysis if user has manually added/removed assemblies
			bool manualAdd = false;
			foreach (var asm in addedAssemblies) {
				if (!asm.IsAutoLoaded)
					manualAdd = true;
			}
			if (removedAssemblies.Count > 0 || manualAdd) {
				this.LazyLoading = true;
				threading.Cancel();
				this.Children.Clear();
			}
			return true;
		}
	}
}
