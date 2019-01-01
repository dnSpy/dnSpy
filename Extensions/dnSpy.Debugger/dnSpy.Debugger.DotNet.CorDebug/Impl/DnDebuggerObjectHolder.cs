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

namespace dnSpy.Debugger.DotNet.CorDebug.Impl {
	abstract class DnDebuggerObjectHolder {
		public abstract void Dispose();
		public abstract void Close();
		public abstract DbgEngineImpl Engine { get; }
	}

	abstract class DnDebuggerObjectHolder<T> : DnDebuggerObjectHolder where T : class {
		public bool IsClosed => Object == null;
		public abstract T Object { get; }
		public abstract int HashCode { get; }
		public abstract DnDebuggerObjectHolder<T> AddRef();
	}

	sealed class DnDebuggerObjectHolderImpl<T> : DnDebuggerObjectHolder<T> where T : class {
		public override T Object => obj;
		public override int HashCode { get; }
		public override DbgEngineImpl Engine => engine;

		T obj;
		DbgEngineImpl engine;
		int refCount;

		DnDebuggerObjectHolderImpl(DbgEngineImpl engine, T obj) {
			this.obj = obj ?? throw new ArgumentNullException(nameof(obj));
			HashCode = obj.GetHashCode();
			this.engine = engine ?? throw new ArgumentNullException(nameof(engine));
			refCount = 1;
		}

		/// <summary>
		/// Should only be called by <see cref="DbgEngineImpl"/>
		/// </summary>
		public static DnDebuggerObjectHolderImpl<T> Create_DONT_CALL(DbgEngineImpl engine, T obj) => new DnDebuggerObjectHolderImpl<T>(engine, obj);

		public override DnDebuggerObjectHolder<T> AddRef() {
			Interlocked.Increment(ref refCount);
			return this;
		}

		public override void Close() {
			if (Interlocked.Decrement(ref refCount) == 0) {
				// engine can be null if its breakpoint is still alive or DbgEngineImpl gets closed before
				// some code has a chance to call Close()
				engine?.Remove(this);
			}
		}

		public override void Dispose() {
			obj = null;
			engine = null;
		}
	}
}
