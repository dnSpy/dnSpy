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

namespace dnSpy.Contracts.Hex.Intellisense {
	/// <summary>
	/// Quick info source
	/// </summary>
	public abstract class HexQuickInfoSource : IDisposable {
		/// <summary>
		/// Constructor
		/// </summary>
		protected HexQuickInfoSource() { }

		/// <summary>
		/// Augments the quick info session
		/// </summary>
		/// <param name="session">Session</param>
		/// <param name="quickInfoContent">Updated with new content</param>
		/// <param name="applicableToSpan">Updated with applicable-to span or the default value if nothing was added to <paramref name="quickInfoContent"/></param>
		public abstract void AugmentQuickInfoSession(HexQuickInfoSession session, IList<object> quickInfoContent, out HexBufferSpanSelection applicableToSpan);

		/// <summary>
		/// Disposes this instance
		/// </summary>
		public void Dispose() => DisposeCore();

		/// <summary>
		/// Disposes this instance
		/// </summary>
		protected virtual void DisposeCore() { }
	}
}
