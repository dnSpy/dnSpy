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
using dnSpy.Contracts.Debugger.Evaluation;
using dnSpy.Debugger.Evaluation.ViewModel;

namespace dnSpy.Debugger.Evaluation.UI {
	readonly struct ValueNodesProviderResult {
		public DbgValueNodeInfo[] Nodes { get; }
		public bool RecreateAllNodes { get; }

		public ValueNodesProviderResult(DbgValueNodeInfo[] nodes, bool recreateAllNodes) {
			Nodes = nodes ?? throw new ArgumentNullException(nameof(nodes));
			RecreateAllNodes = recreateAllNodes;
		}
	}

	abstract class VariablesWindowValueNodesProvider {
		public virtual event EventHandler NodesChanged { add { } remove { } }
		public abstract ValueNodesProviderResult GetNodes(DbgEvaluationInfo evalInfo, DbgLanguage language, DbgEvaluationOptions options, DbgValueNodeEvaluationOptions nodeEvalOptions, DbgValueFormatterOptions nameFormatterOptions);
		public virtual DbgValueNodeInfo[] GetDefaultNodes() => Array.Empty<DbgValueNodeInfo>();

		/// <summary>
		/// Called when the window gets shown or closed
		/// </summary>
		/// <param name="enable">true if window is shown, false if it's closed</param>
		public virtual void Initialize(bool enable) { }

		public virtual void OnIsDebuggingChanged(bool isDebugging) { }

		/// <summary>
		/// true if root nodes can be added/deleted (supported by watch window)
		/// </summary>
		public virtual bool CanAddRemoveExpressions => false;
		public virtual void DeleteExpressions(string[] ids) => throw new NotSupportedException();
		public virtual void ClearAllExpressions() => throw new NotSupportedException();
		public virtual void EditExpression(string id, string expression) => throw new NotSupportedException();
		public virtual void AddExpressions(string[] expressions) => throw new NotSupportedException();
	}

	sealed class VariablesWindowVMOptions {
		public VariablesWindowValueNodesProvider VariablesWindowValueNodesProvider { get; set; }
		public string WindowContentType { get; set; }
		public string NameColumnName { get; set; }
		public string ValueColumnName { get; set; }
		public string TypeColumnName { get; set; }
		public VariablesWindowKind VariablesWindowKind { get; set; }
		public Guid VariablesWindowGuid { get; set; }
	}
}
