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
using System.Collections.Generic;
using dnSpy.Contracts.Hex;

namespace dnSpy.Hex {
	sealed class HexEditImpl : HexEdit {
		public override bool Canceled => canceled;
		bool canceled;

		public override HexBuffer Buffer => hexBufferImpl;

		public override bool HasEffectiveChanges {
			get {
				throw new NotImplementedException();//TODO:
			}
		}

		public override bool HasFailedChanges {
			get {
				throw new NotImplementedException();//TODO:
			}
		}

		readonly HexBufferImpl hexBufferImpl;
		readonly List<HexChange> changes;
		readonly int? reiteratedVersionNumber;
		readonly object editTag;

		public HexEditImpl(HexBufferImpl hexBufferImpl, int? reiteratedVersionNumber, object editTag) {
			this.hexBufferImpl = hexBufferImpl ?? throw new ArgumentNullException(nameof(hexBufferImpl));
			changes = new List<HexChange>();
			this.reiteratedVersionNumber = reiteratedVersionNumber;
			this.editTag = editTag;
		}

		bool hasApplied;
		public override void Apply() {
			if (Canceled || hasApplied)
				throw new InvalidOperationException();
			hasApplied = true;
			hexBufferImpl.ApplyChanges(this, changes, reiteratedVersionNumber, editTag);
		}

		public override void Cancel() {
			if (canceled)
				return;
			canceled = true;
			hexBufferImpl.Cancel(this);
		}

		public override bool Replace(HexPosition position, byte value) => ReplaceSafe(position, new byte[1] { value });
		public override bool Replace(HexPosition position, sbyte value) => ReplaceSafe(position, new byte[1] { (byte)value });
		public override bool Replace(HexPosition position, short value) => ReplaceSafe(position, BitConverter.GetBytes(value));
		public override bool Replace(HexPosition position, ushort value) => ReplaceSafe(position, BitConverter.GetBytes(value));
		public override bool Replace(HexPosition position, int value) => ReplaceSafe(position, BitConverter.GetBytes(value));
		public override bool Replace(HexPosition position, uint value) => ReplaceSafe(position, BitConverter.GetBytes(value));
		public override bool Replace(HexPosition position, long value) => ReplaceSafe(position, BitConverter.GetBytes(value));
		public override bool Replace(HexPosition position, ulong value) => ReplaceSafe(position, BitConverter.GetBytes(value));
		public override bool Replace(HexPosition position, float value) => ReplaceSafe(position, BitConverter.GetBytes(value));
		public override bool Replace(HexPosition position, double value) => ReplaceSafe(position, BitConverter.GetBytes(value));

		public override bool Replace(HexPosition position, byte[] data, long index, long length) {
			// Make a copy of it so the caller can't modify it
			var newData = new byte[length];
			Array.Copy(data, index, newData, 0, length);
			return ReplaceSafe(position, newData);
		}

		bool ReplaceSafe(HexPosition position, byte[] replaceWith) {
			var replaceSpan = new HexSpan(position, (ulong)replaceWith.LongLength);
			return ReplaceSafe(replaceSpan, replaceWith);
		}

		bool ReplaceSafe(HexSpan replaceSpan, byte[] replaceWith) {
			if (Canceled || hasApplied)
				throw new InvalidOperationException();
			if (replaceSpan.Length != (ulong)replaceWith.LongLength)
				throw new NotSupportedException("Must overwrite data");
			if (replaceSpan.IsEmpty && replaceWith.LongLength == 0)
				return true;
			var origData = hexBufferImpl.ReadBytes(replaceSpan);
			changes.Add(new HexChangeImpl(replaceSpan.Start, origData, replaceWith));
			return true;
		}

		public override void Dispose() {
			if (!Canceled && !hasApplied)
				Cancel();
		}
	}
}
