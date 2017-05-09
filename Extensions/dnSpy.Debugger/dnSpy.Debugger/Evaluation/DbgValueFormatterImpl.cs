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
using dnSpy.Contracts.Debugger.Evaluation;
using dnSpy.Contracts.Debugger.Evaluation.Engine;
using dnSpy.Contracts.Text;

namespace dnSpy.Debugger.Evaluation {
	sealed class DbgValueFormatterImpl : DbgValueFormatter {
		public override DbgLanguage Language { get; }

		readonly Guid runtimeGuid;
		readonly DbgEngineValueFormatter engineValueFormatter;

		public DbgValueFormatterImpl(DbgLanguage language, Guid runtimeGuid, DbgEngineValueFormatter engineValueFormatter) {
			Language = language ?? throw new ArgumentNullException(nameof(language));
			this.runtimeGuid = runtimeGuid;
			this.engineValueFormatter = engineValueFormatter ?? throw new ArgumentNullException(nameof(engineValueFormatter));
		}

		public override void Format(DbgEvaluationContext context, ITextColorWriter output, DbgValue value, DbgValueFormatterOptions options) {
			if (context == null)
				throw new ArgumentNullException(nameof(context));
			if (!(context is DbgEvaluationContextImpl))
				throw new ArgumentException();
			if (context.Language != Language)
				throw new ArgumentException();
			if (output == null)
				throw new ArgumentNullException(nameof(output));
			if (value == null)
				throw new ArgumentNullException(nameof(value));
			if (!(value is DbgValueImpl valueImpl))
				throw new ArgumentException();
			if (value.Runtime.Guid != runtimeGuid)
				throw new ArgumentException();
			engineValueFormatter.Format(context, output, valueImpl.EngineValue, options);
		}

		public override void Format(DbgEvaluationContext context, ITextColorWriter output, DbgValue value, DbgValueFormatterOptions options, Action callback) {
			if (context == null)
				throw new ArgumentNullException(nameof(context));
			if (!(context is DbgEvaluationContextImpl))
				throw new ArgumentException();
			if (context.Language != Language)
				throw new ArgumentException();
			if (output == null)
				throw new ArgumentNullException(nameof(output));
			if (value == null)
				throw new ArgumentNullException(nameof(value));
			if (!(value is DbgValueImpl valueImpl))
				throw new ArgumentException();
			if (value.Runtime.Guid != runtimeGuid)
				throw new ArgumentException();
			if (callback == null)
				throw new ArgumentNullException(nameof(callback));
			engineValueFormatter.Format(context, output, valueImpl.EngineValue, options, callback);
		}

		public override void FormatType(DbgEvaluationContext context, ITextColorWriter output, DbgValue value, DbgValueFormatterTypeOptions options) {
			if (context == null)
				throw new ArgumentNullException(nameof(context));
			if (!(context is DbgEvaluationContextImpl))
				throw new ArgumentException();
			if (context.Language != Language)
				throw new ArgumentException();
			if (output == null)
				throw new ArgumentNullException(nameof(output));
			if (value == null)
				throw new ArgumentNullException(nameof(value));
			if (!(value is DbgValueImpl valueImpl))
				throw new ArgumentException();
			if (value.Runtime.Guid != runtimeGuid)
				throw new ArgumentException();
			engineValueFormatter.FormatType(context, output, valueImpl.EngineValue, options);
		}

		public override void FormatType(DbgEvaluationContext context, ITextColorWriter output, DbgValue value, DbgValueFormatterTypeOptions options, Action callback) {
			if (context == null)
				throw new ArgumentNullException(nameof(context));
			if (!(context is DbgEvaluationContextImpl))
				throw new ArgumentException();
			if (context.Language != Language)
				throw new ArgumentException();
			if (output == null)
				throw new ArgumentNullException(nameof(output));
			if (value == null)
				throw new ArgumentNullException(nameof(value));
			if (!(value is DbgValueImpl valueImpl))
				throw new ArgumentException();
			if (value.Runtime.Guid != runtimeGuid)
				throw new ArgumentException();
			if (callback == null)
				throw new ArgumentNullException(nameof(callback));
			engineValueFormatter.FormatType(context, output, valueImpl.EngineValue, options, callback);
		}
	}
}
