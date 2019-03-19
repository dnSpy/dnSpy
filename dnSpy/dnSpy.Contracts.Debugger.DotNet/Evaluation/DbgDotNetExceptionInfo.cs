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

namespace dnSpy.Contracts.Debugger.DotNet.Evaluation {
	/// <summary>
	/// Exception info
	/// </summary>
	public readonly struct DbgDotNetExceptionInfo {
		/// <summary>
		/// Gets the flags
		/// </summary>
		public DbgDotNetExceptionInfoFlags Flags { get; }

		/// <summary>
		/// true if it's a stowed exception
		/// </summary>
		public bool IsStowedException => (Flags & DbgDotNetExceptionInfoFlags.StowedException) != 0;

		/// <summary>
		/// Gets the exception id
		/// </summary>
		public uint Id { get; }

		/// <summary>
		/// Gets the exception instance. There's no guarantee that it derives from <see cref="Exception"/>.
		/// </summary>
		public DbgDotNetValue Value { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="value">Exception value</param>
		/// <param name="id">Exception id</param>
		/// <param name="flags">Flags</param>
		public DbgDotNetExceptionInfo(DbgDotNetValue value, uint id, DbgDotNetExceptionInfoFlags flags) {
			Value = value ?? throw new ArgumentNullException(nameof(value));
			Id = id;
			Flags = flags;
		}
	}

	/// <summary>
	/// Exception info flags
	/// </summary>
	[Flags]
	public enum DbgDotNetExceptionInfoFlags {
		/// <summary>
		/// No bit is set
		/// </summary>
		None					= 0,

		/// <summary>
		/// If set, it's a stowed exception, else it's an exception
		/// </summary>
		StowedException			= 0x00000001,
	}
}
