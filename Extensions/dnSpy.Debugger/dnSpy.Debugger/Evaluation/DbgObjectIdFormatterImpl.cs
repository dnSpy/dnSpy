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
	sealed class DbgObjectIdFormatterImpl : DbgObjectIdFormatter {
		public override DbgLanguage Language { get; }

		readonly Guid runtimeGuid;
		readonly DbgEngineObjectIdFormatter engineObjectIdFormatter;

		public DbgObjectIdFormatterImpl(DbgLanguage language, Guid runtimeGuid, DbgEngineObjectIdFormatter engineObjectIdFormatter) {
			Language = language ?? throw new ArgumentNullException(nameof(language));
			this.runtimeGuid = runtimeGuid;
			this.engineObjectIdFormatter = engineObjectIdFormatter ?? throw new ArgumentNullException(nameof(engineObjectIdFormatter));
		}

		public override void FormatName(ITextColorWriter output, DbgObjectId objectId) {
			if (output == null)
				throw new ArgumentNullException(nameof(output));
			if (objectId == null)
				throw new ArgumentNullException(nameof(objectId));
			if (!(objectId is DbgObjectIdImpl objectIdImpl))
				throw new ArgumentException();
			if (objectId.Runtime.Guid != runtimeGuid)
				throw new ArgumentException();
			engineObjectIdFormatter.FormatName(output, objectIdImpl.EngineObjectId);
		}
	}
}
