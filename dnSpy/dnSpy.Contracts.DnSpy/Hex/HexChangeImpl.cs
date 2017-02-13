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
using System.Text;

namespace dnSpy.Contracts.Hex {
	sealed class HexChangeImpl : HexChange {
		readonly HexPosition oldPosition;
		readonly HexPosition newPosition;
		readonly byte[] oldData;
		readonly byte[] newData;

		public override long Delta => newData.LongLength - oldData.LongLength;

		public override byte[] NewData => newData;
		public override HexPosition NewPosition => newPosition;
		public override HexPosition NewEnd => newPosition + newData.LongLength;
		public override HexPosition NewLength => newData.LongLength;
		public override HexSpan NewSpan => new HexSpan(newPosition, (ulong)newData.LongLength);

		public override byte[] OldData => oldData;
		public override HexPosition OldPosition => oldPosition;
		public override HexPosition OldEnd => oldPosition + oldData.LongLength;
		public override HexPosition OldLength => oldData.LongLength;
		public override HexSpan OldSpan => new HexSpan(oldPosition, (ulong)oldData.LongLength);

		public HexChangeImpl(HexPosition position, byte[] oldData, byte[] newData) {
			if (position > HexPosition.MaxEndPosition || position + oldData.LongLength > HexPosition.MaxEndPosition)
				throw new ArgumentOutOfRangeException(nameof(position));
			if (position + newData.LongLength > HexPosition.MaxEndPosition)
				throw new ArgumentOutOfRangeException(nameof(position));
			oldPosition = position;
			newPosition = position;
			this.oldData = oldData ?? throw new ArgumentNullException(nameof(oldData));
			this.newData = newData ?? throw new ArgumentNullException(nameof(newData));
		}

		public HexChangeImpl(HexPosition oldPosition, byte[] oldData, HexPosition newPosition, byte[] newData) {
			if (oldPosition > HexPosition.MaxEndPosition || oldPosition + oldData.LongLength > HexPosition.MaxEndPosition)
				throw new ArgumentOutOfRangeException(nameof(oldPosition));
			if (newPosition > HexPosition.MaxEndPosition || newPosition + newData.LongLength > HexPosition.MaxEndPosition)
				throw new ArgumentOutOfRangeException(nameof(newPosition));
			this.oldPosition = oldPosition;
			this.newPosition = newPosition;
			this.oldData = oldData ?? throw new ArgumentNullException(nameof(oldData));
			this.newData = newData ?? throw new ArgumentNullException(nameof(newData));
		}

		public override string ToString() => $"old={OldSpan}:'{GetString(OldData)}' new={NewSpan}:'{GetString(NewData)}'";

		static string GetString(byte[] d) {
			const int maxBytes = 16;
			const string ellipsis = "...";
			bool tooMuchData = d.LongLength > maxBytes;
			var sb = new StringBuilder((tooMuchData ? maxBytes + ellipsis.Length : d.Length) * 2);
			for (int i = 0; i < d.Length && i < maxBytes; i++)
				sb.Append(d[i].ToString("X2"));
			if (tooMuchData)
				sb.Append(ellipsis);
			return sb.ToString();
		}
	}
}
