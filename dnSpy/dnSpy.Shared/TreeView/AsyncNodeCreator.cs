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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Windows.Threading;
using dnSpy.Contracts.TreeView;

namespace dnSpy.Shared.TreeView {
	public abstract class AsyncNodeCreator {
		readonly Thread thread;
		readonly CancellationTokenSource cancellationTokenSource;
		protected readonly CancellationToken cancellationToken;
		readonly object lockObj;
		readonly List<Action> uiThreadActions;
		readonly Dispatcher dispatcher;
		readonly ITreeNodeData targetNode;
		ITreeNode msgNode;

		protected AsyncNodeCreator(ITreeNodeData targetNode) {
			this.lockObj = new object();
			this.targetNode = targetNode;
			this.dispatcher = Dispatcher.CurrentDispatcher;
			this.uiThreadActions = new List<Action>();
			this.cancellationTokenSource = new CancellationTokenSource();
			this.cancellationToken = cancellationTokenSource.Token;
			this.thread = new Thread(ThreadMethodImpl);
			thread.IsBackground = true;
		}

		void ExecInUIThread(Action action) {
			bool start;
			lock (lockObj) {
				uiThreadActions.Add(action);
				start = uiThreadActions.Count == 1;
			}
			if (start)
				dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(ExecActions));
		}

		protected void AddNode(ITreeNodeData node) {
			lock (lockObj) {
				nodesToAdd.Add(node);
				if (nodesToAdd.Count == 1)
					ExecInUIThread(AddNodes_UI);
			}
		}
		readonly List<ITreeNodeData> nodesToAdd = new List<ITreeNodeData>();

		void AddNodes_UI() {
			List<ITreeNodeData> nodes;
			lock (lockObj) {
				nodes = new List<ITreeNodeData>(nodesToAdd);
				nodesToAdd.Clear();
			}
			// If it's been canceled, don't add any new nodes since the search might've restarted.
			// We must not add 'old' nodes to the current Children list.
			if (cancellationTokenSource.IsCancellationRequested)
				return;
			foreach (var n in nodes)
				targetNode.TreeNode.AddChild(targetNode.TreeNode.TreeView.Create(n));
		}

		protected void AddMessageNode(Func<ITreeNodeData> create) {
			ExecInUIThread(() => {
				Debug.Assert(msgNode == null);
				msgNode = targetNode.TreeNode.TreeView.Create(create());
				targetNode.TreeNode.AddChild(msgNode);
			});
		}

		void RemoveMessageNode_UI() {
			if (msgNode != null)
				targetNode.TreeNode.Children.Remove(msgNode);
			OnCompleted();
		}

		protected virtual void OnCompleted() {
		}

		void ExecActions() {
			List<Action> actions;
			lock (lockObj) {
				actions = new List<Action>(uiThreadActions);
				uiThreadActions.Clear();
			}

			foreach (var a in actions)
				a();
		}

		protected void Start() {
			Debug.Assert(!isRunning);
			isRunning = true;
			completedSuccessfully = false;
			thread.Start();
		}

		public bool CompletedSuccessfully {
			get { return completedSuccessfully; }
		}
		bool completedSuccessfully;

		public bool IsRunning {
			get { return isRunning; }
		}
		bool isRunning;

		void ThreadMethodImpl() {
			Debug.Assert(isRunning);
			Debug.Assert(!completedSuccessfully);
			try {
				ThreadMethod();
				completedSuccessfully = true;
			}
			catch (OperationCanceledException) {
			}
			ExecInUIThread(RemoveMessageNode_UI);
			isRunning = false;
		}

		protected abstract void ThreadMethod();

		public void Cancel() {
			cancellationTokenSource.Cancel();
		}
	}
}
