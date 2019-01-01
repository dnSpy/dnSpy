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
using dnSpy.Contracts.Debugger.CallStack;
using dnSpy.Contracts.Debugger.Evaluation;

namespace dnSpy.Debugger.Evaluation.ViewModel {
	readonly struct GetNodesResult {
		/// <summary>
		/// Gets all nodes
		/// </summary>
		public DbgValueNodeInfo[] Nodes { get; }

		/// <summary>
		/// true if the frame got closed while we were getting the nodes. The caller should
		/// ignore <see cref="Nodes"/> and wait until we get a new frame.
		/// </summary>
		public bool FrameClosed { get; }

		/// <summary>
		/// true if all nodes should be recreated, eg. it's a new frame
		/// </summary>
		public bool RecreateAllNodes { get; }

		public GetNodesResult(DbgValueNodeInfo[] nodes, bool frameClosed, bool recreateAllNodes) {
			Nodes = nodes ?? throw new ArgumentNullException(nameof(nodes));
			FrameClosed = frameClosed;
			RecreateAllNodes = recreateAllNodes;
		}
	}

	abstract class ValueNodesProvider {
		/// <summary>
		/// Gets all nodes
		/// </summary>
		/// <param name="evalOptions">Evaluation options</param>
		/// <param name="nodeEvalOptions">Value node evaluation options</param>
		/// <param name="nameFormatterOptions">Name formatter options</param>
		/// <returns></returns>
		public abstract GetNodesResult GetNodes(DbgEvaluationOptions evalOptions, DbgValueNodeEvaluationOptions nodeEvalOptions, DbgValueFormatterOptions nameFormatterOptions);

		/// <summary>
		/// Raised when <see cref="GetNodes(DbgEvaluationOptions, DbgValueNodeEvaluationOptions, DbgValueFormatterOptions)"/> must be called again, eg. the debugged program is paused
		/// </summary>
		public abstract event EventHandler NodesChanged;

		/// <summary>
		/// true if the window should be made read-only, eg. the program is running or nothing is being debugged.
		/// </summary>
		public abstract bool IsReadOnly { get; }
		public abstract event EventHandler IsReadOnlyChanged;

		/// <summary>
		/// Gets the language or null if none
		/// </summary>
		public abstract DbgLanguage Language { get; }
		public abstract event EventHandler LanguageChanged;

		/// <summary>
		/// true if root nodes can be added/deleted (supported by watch window)
		/// </summary>
		public abstract bool CanAddRemoveExpressions { get; }
		public abstract void DeleteExpressions(string[] ids);
		public abstract void ClearAllExpressions();
		public abstract void EditExpression(string id, string expression);
		public abstract void AddExpressions(string[] expressions);

		public abstract DbgEvaluationInfo TryGetEvaluationInfo();
		public abstract DbgStackFrame TryGetFrame();

		public abstract void RefreshAllNodes();
	}

	readonly struct DbgValueNodeInfo {
		/// <summary>
		/// null or the id of the value. Should be used if <see cref="DbgValueNode.Expression"/> isn't unique
		/// or when <see cref="ValueNodesProvider.CanAddRemoveExpressions"/> is true
		/// </summary>
		public string Id { get; }

		/// <summary>
		/// null if it's been invalidated (it causes side effects and it wasn't re-evaluated or there was another error)
		/// </summary>
		public DbgValueNode Node { get; }

 		/// <summary>
		/// Shown in Name column if <see cref="Node"/> is null, else it's ignored
		/// </summary>
		public string Expression { get; }

		/// <summary>
		/// Shown in Value column if <see cref="Node"/> is null, else it's ignored
		/// </summary>
		public string ErrorMessage { get; }

		/// <summary>
		/// The expression wasn't evaluated because it causes side effects
		/// </summary>
		public bool CausesSideEffects { get; }

		public DbgValueNodeInfo(string id, string expression, string errorMessage, bool causesSideEffects) {
			Id = id ?? throw new ArgumentNullException(nameof(id));
			Node = null;
			Expression = expression ?? throw new ArgumentNullException(nameof(expression));
			ErrorMessage = errorMessage ?? throw new ArgumentNullException(nameof(errorMessage));
			CausesSideEffects = causesSideEffects;
		}

		public DbgValueNodeInfo(DbgValueNode node, string id, bool causesSideEffects) {
			Id = id ?? throw new ArgumentNullException(nameof(id));
			Node = node ?? throw new ArgumentNullException(nameof(node));
			Expression = null;
			ErrorMessage = null;
			CausesSideEffects = causesSideEffects;
		}

		public DbgValueNodeInfo(DbgValueNode node, bool causesSideEffects) {
			Id = null;
			Node = node ?? throw new ArgumentNullException(nameof(node));
			Expression = null;
			ErrorMessage = null;
			CausesSideEffects = causesSideEffects;
		}
	}
}
