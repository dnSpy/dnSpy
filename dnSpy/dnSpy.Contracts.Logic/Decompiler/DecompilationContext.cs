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
using System.Collections.Generic;
using System.Threading;
using dnlib.DotNet;

namespace dnSpy.Contracts.Decompiler {
	/// <summary>
	/// Decompilation options
	/// </summary>
	public class DecompilationContext {
		const int STRINGBUILDER_POOL_SIZE = 256;

		/// <summary />
		public CancellationToken CancellationToken { get; set; }

		/// <summary>
		/// Returns true if the method body has been modified
		/// </summary>
		public Func<MethodDef, bool> IsBodyModified { get; set; }

		/// <summary>
		/// Disables assembly loading until Dispose() gets called
		/// </summary>
		public Func<IDisposable>? GetDisableAssemblyLoad { get; set; }

		/// <summary>
		/// true to calculate ILSpans. Used when debugging
		/// </summary>
		public bool CalculateILSpans { get; set; }

		/// <summary>
		/// true to decompile method bodies asynchronously. Should not be enabled when decompiling
		/// to a project since that code already decompiles one type per CPU core.
		/// Should also not be enabled when only one method body is decompiled since the code won't be faster.
		/// </summary>
		public bool AsyncMethodBodyDecompilation { get; set; }

		/// <summary>
		/// Constructor
		/// </summary>
		public DecompilationContext() {
			CancellationToken = CancellationToken.None;
			IsBodyModified = m => false;
			AsyncMethodBodyDecompilation = true;
		}

		/// <summary />
		public IDisposable? DisableAssemblyLoad() => GetDisableAssemblyLoad?.Invoke();

		/// <summary>
		/// Gets or creates a cached object
		/// </summary>
		/// <typeparam name="T">Type</typeparam>
		/// <returns></returns>
		public T GetOrCreate<T>() where T : class, new() {
			lock (lockObj) {
				if (cachedObjs.TryGetValue(typeof(T), out var obj))
					return (T)obj;
				T res = new T();
				cachedObjs.Add(typeof(T), res);
				return res;
			}
		}
		readonly object lockObj = new object();
		readonly Dictionary<Type, object> cachedObjs = new Dictionary<Type, object>();

		/// <summary>
		/// Gets or creates a cached object
		/// </summary>
		/// <typeparam name="T">Type</typeparam>
		/// <param name="creator">Creates the object if necessary</param>
		/// <returns></returns>
		public T GetOrCreate<T>(Func<T> creator) where T : class {
			lock (lockObj) {
				if (cachedObjs.TryGetValue(typeof(T), out var obj))
					return (T)obj;
				T res = creator();
				cachedObjs.Add(typeof(T), res);
				return res;
			}
		}
	}
}
