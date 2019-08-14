/*
    Copyright (C) 2014-2019 de4dot@gmail.com

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
using System.Threading;
using System.Windows.Threading;

namespace dnSpy.Contracts.TreeView {
	/// <summary>
	/// Creates nodes asynchronously
	/// </summary>
	public abstract class AsyncNodeProvider {
		readonly Thread thread;
		readonly CancellationTokenSource cancellationTokenSource;
		/// <summary>Cancellation token</summary>
		protected readonly CancellationToken cancellationToken;
		readonly object lockObj;
		readonly List<Action> uiThreadActions;
		readonly Dispatcher dispatcher;
		readonly TreeNodeData targetNode;
		ITreeNode? msgNode;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="targetNode">Target node that will be the parent of the new nodes</param>
		protected AsyncNodeProvider(TreeNodeData targetNode) {
			lockObj = new object();
			this.targetNode = targetNode;
			dispatcher = Dispatcher.CurrentDispatcher;
			uiThreadActions = new List<Action>();
			cancellationTokenSource = new CancellationTokenSource();
			cancellationToken = cancellationTokenSource.Token;
			thread = new Thread(ThreadMethodImpl);
			thread.IsBackground = true;
		}

		void UI(Action action) {
			bool start;
			lock (lockObj) {
				uiThreadActions.Add(action);
				start = uiThreadActions.Count == 1;
			}
			if (start)
				dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(ExecActions));
		}

		/// <summary>
		/// Adds a new node
		/// </summary>
		/// <param name="node">New node</param>
		protected void AddNode(TreeNodeData node) {
			if (node is null)
				throw new ArgumentNullException(nameof(node));
			lock (lockObj) {
				nodesToAdd.Add(node);
				if (nodesToAdd.Count == 1)
					UI(AddNodes_UI);
			}
		}
		readonly List<TreeNodeData> nodesToAdd = new List<TreeNodeData>();

		void AddNodes_UI() {
			List<TreeNodeData> nodes;
			lock (lockObj) {
				nodes = new List<TreeNodeData>(nodesToAdd);
				nodesToAdd.Clear();
			}
			AddNodes_UI(nodes, 0);
		}

		// If too many nodes are added at the same time, the whole UI stops working, so
		// pause a little bit before adding the rest. Repro: analyze a common type such
		// as 'int' with a lot of assemblies in Assembly Explorer.
		const int MaxAddNodeInMilliSecs = 100;
		void AddNodes_UI(List<TreeNodeData> nodes, int index) {
			// If it's been canceled, don't add any new nodes since the search might've restarted.
			// We must not add 'old' nodes to the current Children list.
			if (!canceled) {
				var sw = Stopwatch.StartNew();
				for (int i = index; i < nodes.Count; i++) {
					var n = nodes[i];
					targetNode.TreeNode.AddChild(targetNode.TreeNode.TreeView.Create(n));
					if (sw.ElapsedMilliseconds > MaxAddNodeInMilliSecs && i + 1 < nodes.Count) {
						UI(() => AddNodes_UI(nodes, i + 1));
						return;
					}
				}
			}
		}

		/// <summary>
		/// Adds a node with a message
		/// </summary>
		/// <param name="create">Creates the message node</param>
		protected void AddMessageNode(Func<TreeNodeData> create) => UI(() => {
			Debug2.Assert(msgNode is null);
			msgNode = targetNode.TreeNode.TreeView.Create(create());
			targetNode.TreeNode.AddChild(msgNode);
		});

		void RemoveMessageNode_UI() {
			if (!(msgNode is null))
				targetNode.TreeNode.Children.Remove(msgNode);
			OnCompleted();
		}

		/// <summary>
		/// Called when the async code has stopped
		/// </summary>
		protected virtual void OnCompleted() { }

		void ExecActions() {
			List<Action> actions;
			lock (lockObj) {
				actions = new List<Action>(uiThreadActions);
				uiThreadActions.Clear();
			}

			foreach (var a in actions)
				a();
		}

		/// <summary>
		/// Starts the thread
		/// </summary>
		protected void Start() {
			Debug.Assert(!IsRunning);
			IsRunning = true;
			CompletedSuccessfully = false;
			thread.Start();
		}

		/// <summary>
		/// true if it completed successfully
		/// </summary>
		public bool CompletedSuccessfully { get; private set; }

		/// <summary>
		/// true if it's still running
		/// </summary>
		public bool IsRunning { get; private set; }

		void ThreadMethodImpl() {
			Debug.Assert(IsRunning);
			Debug.Assert(!CompletedSuccessfully);
			try {
				ThreadMethod();
				CompletedSuccessfully = true;
			}
			catch (OperationCanceledException) {
			}
			UI(RemoveMessageNode_UI);
			IsRunning = false;
			disposed = true;
			cancellationTokenSource.Dispose();
		}
		bool disposed;

		/// <summary>
		/// Method that gets called in the worker thread
		/// </summary>
		protected abstract void ThreadMethod();

		/// <summary>
		/// Cancels the async worker
		/// </summary>
		public void Cancel() {
			canceled = true;
			if (!disposed)
				cancellationTokenSource.Cancel();
		}
		bool canceled;
	}
}
