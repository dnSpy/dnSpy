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
	/// Adds threading support to nodes
	/// </summary>
	class ThreadingSupport
	{
		Task<List<ILSpyTreeNode>> loadChildrenTask;
		
		/// <summary>
		/// 
		/// </summary>
		public void LoadChildren(ILSpyTreeNode node, Func<CancellationToken, IEnumerable<ILSpyTreeNode>> fetchChildren)
		{
			node.Children.Add(new LoadingTreeNode());
			
			CancellationToken ct = CancellationToken.None;
			
			var fetchChildrenEnumerable = fetchChildren(ct);
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
									node.Children.Insert(node.Children.Count - 1, newChild);
								}
							}), child);
					}
					App.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(
						delegate {
							if (loadChildrenTask == thisTask) {
								node.Children.RemoveAt(node.Children.Count - 1); // remove 'Loading...'
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
		
		public void Decompile(Language language, ITextOutput output, DecompilationOptions options, Action ensureLazyChildren)
		{
			var loadChildrenTask = this.loadChildrenTask;
			if (loadChildrenTask == null) {
				App.Current.Dispatcher.Invoke(DispatcherPriority.Normal, ensureLazyChildren);
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
