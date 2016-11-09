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
using System.Text;

namespace dnSpy.Contracts.Hex {
	sealed class HexChangeImpl : HexChange {
		readonly ulong oldPosition;
		readonly ulong newPosition;
		readonly byte[] oldData;
		readonly byte[] newData;

		public override long Delta => newData.LongLength - oldData.LongLength;

		public override byte[] NewData => newData;
		public override ulong NewPosition => newPosition;
		public override ulong NewEnd => newPosition + (ulong)newData.LongLength;
		public override ulong NewLastPosition => newData.LongLength == 0 ? newPosition : newPosition + (ulong)newData.LongLength - 1UL;
		public override ulong NewLength => (ulong)newData.LongLength;
		public override HexSpan NewSpan => new HexSpan(newPosition, (ulong)newData.LongLength);

		public override byte[] OldData => oldData;
		public override ulong OldPosition => oldPosition;
		public override ulong OldEnd => oldPosition + (ulong)oldData.LongLength;
		public override ulong OldLastPosition => oldData.LongLength == 0 ? oldPosition : oldPosition + (ulong)oldData.LongLength - 1UL;
		public override ulong OldLength => (ulong)oldData.LongLength;
		public override HexSpan OldSpan => new HexSpan(oldPosition, (ulong)oldData.LongLength);

		public HexChangeImpl(ulong position, byte[] oldData, byte[] newData) {
			if (oldData == null)
				throw new ArgumentNullException(nameof(oldData));
			if (newData == null)
				throw new ArgumentNullException(nameof(newData));
			this.oldPosition = position;
			this.newPosition = position;
			this.oldData = oldData;
			this.newData = newData;
		}

		public HexChangeImpl(ulong oldPosition, byte[] oldData, ulong newPosition, byte[] newData) {
			if (oldData == null)
				throw new ArgumentNullException(nameof(oldData));
			if (newData == null)
				throw new ArgumentNullException(nameof(newData));
			this.oldPosition = oldPosition;
			this.newPosition = newPosition;
			this.oldData = oldData;
			this.newData = newData;
		}

		public override string ToString() => $"old={OldSpan}:'{GetString(OldData)}' new={NewSpan}:'{GetString(NewData)}'";

		static string GetString(byte[] d) {
			const int maxBytes = 16;
			const string ellipsis = "...";
			bool tooMuchData = d.LongLength > maxBytes;
			var sb = new StringBuilder((tooMuchData ? maxBytes + ellipsis.Length : d.Length) * 2);
			for (int i = 0; i < d.Length && i < maxBytes; i++)
				sb.Append(d[i].ToString("X8"));
			if (tooMuchData)
				sb.Append(ellipsis);
			return sb.ToString();
		}
	}
}
