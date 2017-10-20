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
using dnSpy.Contracts.Debugger.Engine.Evaluation;
using dnSpy.Contracts.Debugger.Evaluation;
using dnSpy.Contracts.Text;

namespace dnSpy.Debugger.Evaluation {
	sealed class DbgFormatterImpl : DbgFormatter {
		public override DbgLanguage Language { get; }

		readonly Guid runtimeKindGuid;
		readonly DbgEngineFormatter engineFormatter;

		public DbgFormatterImpl(DbgLanguage language, Guid runtimeKindGuid, DbgEngineFormatter engineFormatter) {
			Language = language ?? throw new ArgumentNullException(nameof(language));
			this.runtimeKindGuid = runtimeKindGuid;
			this.engineFormatter = engineFormatter ?? throw new ArgumentNullException(nameof(engineFormatter));
		}

		public override void FormatExceptionName(DbgEvaluationContext context, ITextColorWriter output, uint id) {
			if (context == null)
				throw new ArgumentNullException(nameof(context));
			if (!(context is DbgEvaluationContextImpl))
				throw new ArgumentException();
			if (context.Language != Language)
				throw new ArgumentException();
			if (context.Runtime.RuntimeKindGuid != runtimeKindGuid)
				throw new ArgumentException();
			if (output == null)
				throw new ArgumentNullException(nameof(output));
			engineFormatter.FormatExceptionName(context, output, id);
		}

		public override void FormatStowedExceptionName(DbgEvaluationContext context, ITextColorWriter output, uint id) {
			if (context == null)
				throw new ArgumentNullException(nameof(context));
			if (!(context is DbgEvaluationContextImpl))
				throw new ArgumentException();
			if (context.Language != Language)
				throw new ArgumentException();
			if (context.Runtime.RuntimeKindGuid != runtimeKindGuid)
				throw new ArgumentException();
			if (output == null)
				throw new ArgumentNullException(nameof(output));
			engineFormatter.FormatStowedExceptionName(context, output, id);
		}

		public override void FormatReturnValueName(DbgEvaluationContext context, ITextColorWriter output, uint id) {
			if (context == null)
				throw new ArgumentNullException(nameof(context));
			if (!(context is DbgEvaluationContextImpl))
				throw new ArgumentException();
			if (context.Language != Language)
				throw new ArgumentException();
			if (context.Runtime.RuntimeKindGuid != runtimeKindGuid)
				throw new ArgumentException();
			if (output == null)
				throw new ArgumentNullException(nameof(output));
			engineFormatter.FormatReturnValueName(context, output, id);
		}

		public override void FormatObjectIdName(DbgEvaluationContext context, ITextColorWriter output, uint id) {
			if (context == null)
				throw new ArgumentNullException(nameof(context));
			if (!(context is DbgEvaluationContextImpl))
				throw new ArgumentException();
			if (context.Language != Language)
				throw new ArgumentException();
			if (context.Runtime.RuntimeKindGuid != runtimeKindGuid)
				throw new ArgumentException();
			if (output == null)
				throw new ArgumentNullException(nameof(output));
			engineFormatter.FormatObjectIdName(context, output, id);
		}
	}
}
