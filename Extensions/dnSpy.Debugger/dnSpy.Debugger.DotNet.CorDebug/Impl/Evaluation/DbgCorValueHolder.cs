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
using dndbg.Engine;
using dnSpy.Debugger.DotNet.Metadata;

namespace dnSpy.Debugger.DotNet.CorDebug.Impl.Evaluation {
	/// <summary>
	/// A reference counted <see cref="CorValue"/> class used by <see cref="DbgDotNetValueImpl"/> and <see cref="DbgDotNetObjectIdImpl"/>
	/// </summary>
	sealed class DbgCorValueHolder {
		public CorValue CorValue {
			get {
				if (disposed)
					throw new ObjectDisposedException(nameof(DbgCorValueHolder));
				return value;
			}
		}

		public DmdType Type {
			get {
				if (disposed)
					throw new ObjectDisposedException(nameof(DbgCorValueHolder));
				return type;
			}
		}

		readonly DbgEngineImpl engine;
		readonly CorValue value;
		readonly DmdType type;
		volatile int referenceCounter;
		volatile bool disposed;

		public DbgCorValueHolder(DbgEngineImpl engine, CorValue value, DmdType type) {
			this.engine = engine;
			this.value = value;
			this.type = type ?? throw new ArgumentNullException(nameof(type));
			referenceCounter = 1;
		}

		public DbgCorValueHolder AddRef() {
			if (disposed)
				throw new ObjectDisposedException(nameof(DbgCorValueHolder));
			Interlocked.Increment(ref referenceCounter);
			return this;
		}

		public void Release() {
			if (disposed)
				throw new ObjectDisposedException(nameof(DbgCorValueHolder));
			if (Interlocked.Decrement(ref referenceCounter) == 0) {
				disposed = true;
				engine.Close(this);
			}
		}

		internal void Dispose_CorDebug() => engine.DisposeHandle_CorDebug(value);
	}
}
