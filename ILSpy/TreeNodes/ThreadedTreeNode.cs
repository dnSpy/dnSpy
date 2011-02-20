// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using ICSharpCode.Decompiler;

namespace ICSharpCode.ILSpy.TreeNodes
{
	/// <summary>
	/// Node that is lazy-loaded and loads its children on a background thread.
	/// </summary>
	abstract class ThreadedTreeNode : ILSpyTreeNode
	{
		Task<List<ILSpyTreeNode>> loadChildrenTask;
		
		public ThreadedTreeNode()
		{
			this.LazyLoading = true;
		}
		
		public void Invalidate()
		{
			this.LazyLoading = true;
			this.Children.Clear();
			loadChildrenTask = null;
		}
		
		/// <summary>
		/// FetchChildren() runs on the main thread; but the enumerator is consumed on a background thread
		/// </summary>
		protected abstract IEnumerable<ILSpyTreeNode> FetchChildren(CancellationToken ct);
		
		protected override sealed void LoadChildren()
		{
			this.Children.Add(new LoadingTreeNode());
			
			CancellationToken ct = CancellationToken.None;
			
			var fetchChildrenEnumerable = FetchChildren(ct);
			Task<List<ILSpyTreeNode>> thisTask = null;
			thisTask = new Task<List<ILSpyTreeNode>>(
				delegate {
					List<ILSpyTreeNode> result = new List<ILSpyTreeNode>();
					foreach (ILSpyTreeNode child in fetchChildrenEnumerable) {
						ct.ThrowIfCancellationRequested();
						result.Add(child);
						App.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action<ILSpyTreeNode>(
							delegate (ILSpyTreeNode newChild) {
								// don't access "child" here the
								// background thread might already be running the next loop iteration
								if (loadChildrenTask == thisTask) {
									this.Children.Insert(this.Children.Count - 1, newChild);
								}
							}), child);
					}
					App.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(
						delegate {
							if (loadChildrenTask == thisTask) {
								this.Children.RemoveAt(this.Children.Count - 1); // remove 'Loading...'
							}
						}));
					return result;
				}, ct);
			loadChildrenTask = thisTask;
			thisTask.Start();
			// Give the task a bit time to complete before we return to WPF - this keeps "Loading..."
			// from showing up for very short waits.
			thisTask.Wait(TimeSpan.FromMilliseconds(200));
		}
		
		public override void Decompile(Language language, ITextOutput output, DecompilationOptions options)
		{
			var loadChildrenTask = this.loadChildrenTask;
			if (loadChildrenTask == null) {
				App.Current.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(EnsureLazyChildren));
				loadChildrenTask = this.loadChildrenTask;
			}
			if (loadChildrenTask != null) {
				foreach (var child in loadChildrenTask.Result) {
					child.Decompile(language, output, options);
				}
			}
		}
		
		sealed class LoadingTreeNode : ILSpyTreeNode
		{
			public override object Text {
				get { return "Loading..."; }
			}
			
			public override FilterResult Filter(FilterSettings settings)
			{
				return FilterResult.Match;
			}
			
			public override void Decompile(Language language, ICSharpCode.Decompiler.ITextOutput output, DecompilationOptions options)
			{
			}
		}
	}
}
