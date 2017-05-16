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
using dnSpy.Contracts.Debugger.Evaluation.Engine;

namespace dnSpy.Debugger.Evaluation {
	sealed class DbgValueNodeFactoryImpl : DbgValueNodeFactory {
		public override DbgLanguage Language { get; }

		readonly Guid runtimeGuid;
		readonly DbgEngineValueNodeFactory engineValueNodeFactory;

		public DbgValueNodeFactoryImpl(DbgLanguage language, Guid runtimeGuid, DbgEngineValueNodeFactory engineValueNodeFactory) {
			Language = language ?? throw new ArgumentNullException(nameof(language));
			this.runtimeGuid = runtimeGuid;
			this.engineValueNodeFactory = engineValueNodeFactory ?? throw new ArgumentNullException(nameof(engineValueNodeFactory));
		}

		public override DbgValueNode Create(DbgStackFrame frame, string expression, DbgEvaluationOptions options) {
			if (frame == null)
				throw new ArgumentNullException(nameof(frame));
			if (frame.Runtime.Guid != runtimeGuid)
				throw new ArgumentException();
			if (expression == null)
				throw new ArgumentNullException(nameof(expression));
			var engineValueNode = engineValueNodeFactory.Create(frame, expression, options);
			return new DbgValueNodeImpl(Language, frame.Thread, engineValueNode);
		}

		public override void Create(DbgStackFrame frame, string expression, DbgEvaluationOptions options, Action<DbgValueNode> callback) {
			if (frame == null)
				throw new ArgumentNullException(nameof(frame));
			if (frame.Runtime.Guid != runtimeGuid)
				throw new ArgumentException();
			if (expression == null)
				throw new ArgumentNullException(nameof(expression));
			if (callback == null)
				throw new ArgumentNullException(nameof(callback));
			engineValueNodeFactory.Create(frame, expression, options, engineValueNode => callback(new DbgValueNodeImpl(Language, frame.Thread, engineValueNode)));
		}
	}
}
