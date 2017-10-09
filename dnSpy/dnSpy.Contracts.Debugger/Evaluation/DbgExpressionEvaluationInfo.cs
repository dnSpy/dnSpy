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

namespace dnSpy.Contracts.Debugger.Evaluation {
	/// <summary>
	/// Contains the expression to evaluate and options
	/// </summary>
	public struct DbgExpressionEvaluationInfo {
		/// <summary>
		/// Gets the expression to evaluate
		/// </summary>
		public string Expression { get; }

		/// <summary>
		/// Gets the value node options
		/// </summary>
		public DbgValueNodeEvaluationOptions NodeOptions { get; }

		/// <summary>
		/// Gets the evaluation options
		/// </summary>
		public DbgEvaluationOptions Options { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="expression">Expression to evaluate</param>
		/// <param name="nodeOptions">Value node options</param>
		/// <param name="options">Evaluation options</param>
		public DbgExpressionEvaluationInfo(string expression, DbgValueNodeEvaluationOptions nodeOptions, DbgEvaluationOptions options) {
			Expression = expression ?? throw new ArgumentNullException(nameof(expression));
			NodeOptions = nodeOptions;
			Options = options;
		}
	}
}
