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

namespace dnSpy.Contracts.Debugger.Evaluation {
	/// <summary>
	/// Creates <see cref="DbgValueNode"/>s
	/// </summary>
	public abstract class DbgValueNodeFactory {
		/// <summary>
		/// Gets the language
		/// </summary>
		public abstract DbgLanguage Language { get; }

		/// <summary>
		/// Creates <see cref="DbgValueNode"/>s
		/// </summary>
		/// <param name="evalInfo">Evaluation info</param>
		/// <param name="expression">Expression</param>
		/// <param name="nodeOptions">Value node options</param>
		/// <param name="options">Eval options</param>
		/// <param name="expressionEvaluatorState">State created by <see cref="DbgExpressionEvaluator.CreateExpressionEvaluatorState"/> or null to store the state in <paramref name="evalInfo"/>'s context</param>
		/// <returns></returns>
		public DbgCreateValueNodeResult Create(DbgEvaluationInfo evalInfo, string expression, DbgValueNodeEvaluationOptions nodeOptions, DbgEvaluationOptions options, object? expressionEvaluatorState) =>
			Create(evalInfo, new[] { new DbgExpressionEvaluationInfo(expression, nodeOptions, options, expressionEvaluatorState) })[0];

		/// <summary>
		/// Creates a <see cref="DbgValueNode"/>. The returned <see cref="DbgValueNode"/>s are automatically closed when their runtime continues.
		/// </summary>
		/// <param name="evalInfo">Evaluation info</param>
		/// <param name="expressions">Expressions to evaluate</param>
		/// <returns></returns>
		public abstract DbgCreateValueNodeResult[] Create(DbgEvaluationInfo evalInfo, DbgExpressionEvaluationInfo[] expressions);

		/// <summary>
		/// Creates <see cref="DbgValueNode"/>s. The returned <see cref="DbgValueNode"/>s are automatically closed when their runtime continues.
		/// </summary>
		/// <param name="evalInfo">Evaluation info</param>
		/// <param name="objectIds">Object ids</param>
		/// <param name="options">Options</param>
		/// <returns></returns>
		public abstract DbgValueNode[] Create(DbgEvaluationInfo evalInfo, DbgObjectId[] objectIds, DbgValueNodeEvaluationOptions options);
	}

	/// <summary>
	/// Contains the created <see cref="DbgValueNode"/> or an error message
	/// </summary>
	public readonly struct DbgCreateValueNodeResult {
		/// <summary>
		/// Gets the created node
		/// </summary>
		public DbgValueNode ValueNode { get; }

		/// <summary>
		/// true if the expression wasn't evaluated because it causes side effects (<see cref="DbgEvaluationOptions.NoSideEffects"/> was used)
		/// </summary>
		public bool CausesSideEffects { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="node">New value node</param>
		/// <param name="causesSideEffects">true if the expression wasn't evaluated because it causes side effects (<see cref="DbgEvaluationOptions.NoSideEffects"/> was used)</param>
		public DbgCreateValueNodeResult(DbgValueNode node, bool causesSideEffects) {
			ValueNode = node ?? throw new ArgumentNullException(nameof(node));
			CausesSideEffects = causesSideEffects;
		}
	}
}
