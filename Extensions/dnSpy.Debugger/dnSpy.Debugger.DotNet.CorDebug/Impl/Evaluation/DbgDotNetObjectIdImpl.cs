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
using System.Threading;
using dnSpy.Contracts.Debugger.DotNet.Evaluation;

namespace dnSpy.Debugger.DotNet.CorDebug.Impl.Evaluation {
	sealed class DbgDotNetObjectIdImpl : DbgDotNetObjectId {
		public override uint Id { get; }

		internal DbgCorValueHolder Value { get; }

		int disposed;

		public DbgDotNetObjectIdImpl(DbgCorValueHolder value, uint id) {
			if (!value.CorValue.IsHandle)
				throw new ArgumentException();
			Value = value;
			Id = id;
		}

		public override void Dispose() {
			if (Interlocked.Exchange(ref disposed, 1) == 0)
				Value.Release();
		}
	}
}
