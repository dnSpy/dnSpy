/*
    Copyright (C) 2014-2016 de4dot@gmail.com

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

namespace dndbg.Engine {
	public interface ICorValueHolder : IDisposable {
		/// <summary>
		/// true if the cached <see cref="dndbg.Engine.CorValue"/> is null or has been neutered
		/// </summary>
		bool IsNeutered { get; }

		/// <summary>
		/// Gets the <see cref="dndbg.Engine.CorValue"/>, creating a new one if it's been neutered.
		/// null is returned if it was neutered but couldn't be recreated.
		/// </summary>
		CorValue CorValue { get; }

		/// <summary>
		/// Invalidates the current <see cref="CorValue"/>. It will return a new value next time
		/// it's called.
		/// </summary>
		/// <returns></returns>
		void InvalidateCorValue();
	}

	/// <summary>
	/// Holds a <see cref="CorValue"/> that gets neutered when Continue() is called. A new
	/// <see cref="CorValue"/> is automatically fetched whenever that happens.
	/// </summary>
	public sealed class CorValueHolder : ICorValueHolder {
		public bool IsNeutered => value == null || value.IsNeutered;

		public CorValue CorValue {
			get {
				if (IsNeutered) {
					InvalidateCorValue();
					value = getCorValue();
				}
				return value;
			}
		}
		CorValue value;

		readonly Func<CorValue> getCorValue;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="value">Value (will be owned by this <see cref="CorValueHolder"/> instance) or null</param>
		/// <param name="getCorValue">Delegate to fetch a new value once it's been neutered. The
		/// returned value will be owned by this <see cref="CorValueHolder"/> instance</param>
		public CorValueHolder(CorValue value, Func<CorValue> getCorValue) {
			if (getCorValue == null)
				throw new ArgumentNullException(nameof(getCorValue));
			this.value = value;
			this.getCorValue = getCorValue;
		}

		public void InvalidateCorValue() {
			value?.DisposeHandle();
			value = null;
		}

		public void Dispose() => InvalidateCorValue();
	}

	/// <summary>
	/// Holds a <see cref="dndbg.Engine.CorValue"/> that doesn't get neutered when Continue() is
	/// called. The value is not owned by this class and won't get disposed by this class.
	/// </summary>
	public sealed class DummyCorValueHolder : ICorValueHolder {
		public CorValue CorValue { get; }
		public bool IsNeutered => false;

		public DummyCorValueHolder(CorValue value) {
			CorValue = value;
		}

		public void InvalidateCorValue() { }
		public void Dispose() { }
	}
}
