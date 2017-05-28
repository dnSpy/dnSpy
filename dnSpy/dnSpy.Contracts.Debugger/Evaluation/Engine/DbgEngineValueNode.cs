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

namespace dnSpy.Contracts.Debugger.Evaluation.Engine {
	/// <summary>
	/// A value shown in a treeview (eg. in locals window)
	/// </summary>
	public abstract class DbgEngineValueNode : DbgObject {
		/// <summary>
		/// Gets the value
		/// </summary>
		public abstract DbgEngineValue Value { get; }

		/// <summary>
		/// Gets the expression that is used when adding an expression to the watch window or
		/// when assigning a new value to the source.
		/// </summary>
		public abstract string Expression { get; }

		/// <summary>
		/// Image name, see <see cref="PredefinedDbgValueNodeImageNames"/>
		/// </summary>
		public abstract string ImageName { get; }

		/// <summary>
		/// true if this is a read-only value
		/// </summary>
		public abstract bool IsReadOnly { get; }

		/// <summary>
		/// true if the expression causes side effects
		/// </summary>
		public abstract bool CausesSideEffects { get; }

		/// <summary>
		/// Returns true if it has children, false if it has no children and null if it's unknown (eg. it's too expensive to calculate it now).
		/// UI code can use this property to decide if it shows the treeview node expander ("|>").
		/// </summary>
		public abstract bool? HasChildren { get; }

		/// <summary>
		/// Number of children. This property is called as late as possible and can be lazily initialized.
		/// It's assumed to be 0 if <see cref="HasChildren"/> is false.
		/// </summary>
		public abstract ulong ChildrenCount { get; }

		/// <summary>
		/// Creates new children. This method blocks the current thread until the children have been created.
		/// </summary>
		/// <param name="context">Evaluation context</param>
		/// <param name="index">Index of first child</param>
		/// <param name="count">Max number of children to return</param>
		/// <returns></returns>
		public abstract DbgEngineValueNode[] GetChildren(DbgEvaluationContext context, ulong index, int count);

		/// <summary>
		/// Creates new children
		/// </summary>
		/// <param name="context">Evaluation context</param>
		/// <param name="index">Index of first child</param>
		/// <param name="count">Max number of children to return</param>
		/// <param name="callback">Called when this method is complete</param>
		public abstract void GetChildren(DbgEvaluationContext context, ulong index, int count, Action<DbgEngineValueNode[]> callback);

		/// <summary>
		/// Formats the name, value, and type. This method blocks the current thread until all requested values have been formatted
		/// </summary>
		/// <param name="context">Evaluation context</param>
		/// <param name="options">Options</param>
		public abstract void Format(DbgEvaluationContext context, IDbgValueNodeFormatParameters options);

		/// <summary>
		/// Formats the name, value, and type
		/// </summary>
		/// <param name="context">Evaluation context</param>
		/// <param name="options">Options</param>
		/// <param name="callback">Called when this method is complete</param>
		public abstract void Format(DbgEvaluationContext context, IDbgValueNodeFormatParameters options, Action callback);

		/// <summary>
		/// Writes a new value. It blocks the current thread until the assignment is complete.
		/// </summary>
		/// <param name="context">Evaluation context</param>
		/// <param name="expression">Source expression (rhs)</param>
		/// <param name="options">Options</param>
		/// <returns></returns>
		public abstract DbgEngineValueNodeAssignmentResult Assign(DbgEvaluationContext context, string expression, DbgEvaluationOptions options);

		/// <summary>
		/// Writes a new value
		/// </summary>
		/// <param name="context">Evaluation context</param>
		/// <param name="expression">Source expression (rhs)</param>
		/// <param name="options">Options</param>
		/// <param name="callback">Called when this method is complete</param>
		/// <returns></returns>
		public abstract void Assign(DbgEvaluationContext context, string expression, DbgEvaluationOptions options, Action<DbgEngineValueNodeAssignmentResult> callback);
	}

	/// <summary>
	/// Assignment result
	/// </summary>
	public struct DbgEngineValueNodeAssignmentResult {
		/// <summary>
		/// Gets the value or null. This value is owned by the <see cref="DbgEngineValueNode"/> instance.
		/// </summary>
		public DbgEngineValue Value { get; }

		/// <summary>
		/// Gets the error or null if none
		/// </summary>
		public string Error { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="value">New value. It's owned by the <see cref="DbgEngineValueNode"/> instance</param>
		public DbgEngineValueNodeAssignmentResult(DbgEngineValue value) {
			Value = value ?? throw new ArgumentNullException(nameof(value));
			Error = null;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="error">Error message</param>
		public DbgEngineValueNodeAssignmentResult(string error) {
			Value = null;
			Error = error ?? throw new ArgumentNullException(nameof(error));
		}
	}
}
