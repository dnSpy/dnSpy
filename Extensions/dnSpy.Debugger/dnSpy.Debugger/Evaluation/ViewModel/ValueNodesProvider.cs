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

using System;
using dnSpy.Contracts.Debugger.CallStack;
using dnSpy.Contracts.Debugger.Evaluation;

namespace dnSpy.Debugger.Evaluation.ViewModel {
	abstract class ValueNodesProvider {
		/// <summary>
		/// Gets all nodes. Caller owns the nodes and must close them.
		/// </summary>
		/// <returns></returns>
		public abstract DbgValueNodeInfo[] GetNodes();

		/// <summary>
		/// Raised when <see cref="GetNodes"/> must be called again, eg. the debugged program is paused
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
		public abstract string[] AddExpressions(string[] expressions);

		public abstract (DbgLanguage language, DbgStackFrame frame) GetEvaluateInfo();
	}

	struct DbgValueNodeInfo {
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
			Node = null;
			Id = id ?? throw new ArgumentNullException(nameof(id));
			Expression = expression ?? throw new ArgumentNullException(nameof(expression));
			ErrorMessage = errorMessage ?? throw new ArgumentNullException(nameof(errorMessage));
			CausesSideEffects = causesSideEffects;
		}

		public DbgValueNodeInfo(DbgValueNode node) {
			Node = node ?? throw new ArgumentNullException(nameof(node));
			Id = null;
			Expression = null;
			ErrorMessage = null;
			CausesSideEffects = false;
		}

		public DbgValueNodeInfo(DbgValueNode node, string id) {
			Node = node ?? throw new ArgumentNullException(nameof(node));
			Id = id ?? throw new ArgumentNullException(nameof(id));
			Expression = null;
			ErrorMessage = null;
			CausesSideEffects = false;
		}
	}
}
