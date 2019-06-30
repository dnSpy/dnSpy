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

namespace dnSpy.Contracts.Hex {
	/// <summary>
	/// Edits a <see cref="HexBuffer"/>
	/// </summary>
	public abstract class HexEdit : IDisposable {
		/// <summary>
		/// Constructor
		/// </summary>
		protected HexEdit() { }

		/// <summary>
		/// true if this edit has been canceled
		/// </summary>
		public abstract bool Canceled { get; }

		/// <summary>
		/// Gets the buffer
		/// </summary>
		public abstract HexBuffer Buffer { get; }

		/// <summary>
		/// true if the edit has changes in non-read-only regions
		/// </summary>
		public abstract bool HasEffectiveChanges { get; }

		/// <summary>
		/// true if any changes failed to be added to this edit due to read-only regions
		/// </summary>
		public abstract bool HasFailedChanges { get; }

		/// <summary>
		/// Replaces the <see cref="byte"/> at <paramref name="position"/> with <paramref name="value"/>
		/// </summary>
		/// <param name="position">Position</param>
		/// <param name="value">New value</param>
		/// <returns></returns>
		public abstract bool Replace(HexPosition position, byte value);

		/// <summary>
		/// Replaces the <see cref="sbyte"/> at <paramref name="position"/> with <paramref name="value"/>
		/// </summary>
		/// <param name="position">Position</param>
		/// <param name="value">New value</param>
		/// <returns></returns>
		public abstract bool Replace(HexPosition position, sbyte value);

		/// <summary>
		/// Replaces the <see cref="short"/> at <paramref name="position"/> with <paramref name="value"/>
		/// </summary>
		/// <param name="position">Position</param>
		/// <param name="value">New value</param>
		/// <returns></returns>
		public abstract bool Replace(HexPosition position, short value);

		/// <summary>
		/// Replaces the <see cref="ushort"/> at <paramref name="position"/> with <paramref name="value"/>
		/// </summary>
		/// <param name="position">Position</param>
		/// <param name="value">New value</param>
		/// <returns></returns>
		public abstract bool Replace(HexPosition position, ushort value);

		/// <summary>
		/// Replaces the <see cref="int"/> at <paramref name="position"/> with <paramref name="value"/>
		/// </summary>
		/// <param name="position">Position</param>
		/// <param name="value">New value</param>
		/// <returns></returns>
		public abstract bool Replace(HexPosition position, int value);

		/// <summary>
		/// Replaces the <see cref="uint"/> at <paramref name="position"/> with <paramref name="value"/>
		/// </summary>
		/// <param name="position">Position</param>
		/// <param name="value">New value</param>
		/// <returns></returns>
		public abstract bool Replace(HexPosition position, uint value);

		/// <summary>
		/// Replaces the <see cref="long"/> at <paramref name="position"/> with <paramref name="value"/>
		/// </summary>
		/// <param name="position">Position</param>
		/// <param name="value">New value</param>
		/// <returns></returns>
		public abstract bool Replace(HexPosition position, long value);

		/// <summary>
		/// Replaces the <see cref="ulong"/> at <paramref name="position"/> with <paramref name="value"/>
		/// </summary>
		/// <param name="position">Position</param>
		/// <param name="value">New value</param>
		/// <returns></returns>
		public abstract bool Replace(HexPosition position, ulong value);

		/// <summary>
		/// Replaces the <see cref="float"/> at <paramref name="position"/> with <paramref name="value"/>
		/// </summary>
		/// <param name="position">Position</param>
		/// <param name="value">New value</param>
		/// <returns></returns>
		public abstract bool Replace(HexPosition position, float value);

		/// <summary>
		/// Replaces the <see cref="double"/> at <paramref name="position"/> with <paramref name="value"/>
		/// </summary>
		/// <param name="position">Position</param>
		/// <param name="value">New value</param>
		/// <returns></returns>
		public abstract bool Replace(HexPosition position, double value);

		/// <summary>
		/// Replaces the data at <paramref name="position"/> with <paramref name="data"/>
		/// </summary>
		/// <param name="position">Position</param>
		/// <param name="data">New data</param>
		/// <returns></returns>
		public bool Replace(HexPosition position, byte[] data) {
			if (data is null)
				throw new ArgumentNullException(nameof(data));
			return Replace(position, data, 0, data.LongLength);
		}

		/// <summary>
		/// Replaces the data at <paramref name="position"/> with <paramref name="data"/>
		/// </summary>
		/// <param name="position">Position</param>
		/// <param name="data">New data</param>
		/// <param name="index">Index</param>
		/// <param name="length">Length</param>
		/// <returns></returns>
		public abstract bool Replace(HexPosition position, byte[] data, long index, long length);

		/// <summary>
		/// Commits all the modifications
		/// </summary>
		public abstract void Apply();

		/// <summary>
		/// Cancels all modifications
		/// </summary>
		public abstract void Cancel();

		/// <summary>
		/// Disposes this instance. If <see cref="Apply"/> hasn't been called, the hex edit operation is canceled
		/// </summary>
		public abstract void Dispose();
	}
}
