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
using System.Collections.Generic;
using System.Threading;
using dnSpy.Contracts.Hex;

namespace dnSpy.Hex {
	sealed class HexBufferImpl : HexBuffer {
		public override bool IsVolatile => stream.IsVolatile;
		public override bool IsReadOnly => stream.IsReadOnly;
		public override HexSpan Span => stream.Span;
		public override string Name => stream.Name;
		public override HexVersion Version => currentHexVersion;
		public override event EventHandler<HexContentChangingEventArgs> Changing;
		public override event EventHandler<HexContentChangedEventArgs> ChangedHighPriority;
		public override event EventHandler<HexContentChangedEventArgs> Changed;
		public override event EventHandler<HexContentChangedEventArgs> ChangedLowPriority;
		public override event EventHandler PostChanged;

		public override event EventHandler<HexBufferSpanInvalidatedEventArgs> BufferSpanInvalidated {
			add {
				if (bufferSpanInvalidated == null)
					stream.BufferStreamSpanInvalidated += HexBufferStream_BufferStreamSpanInvalidated;
				bufferSpanInvalidated += value;
			}
			remove {
				bufferSpanInvalidated -= value;
				if (bufferSpanInvalidated == null)
					stream.BufferStreamSpanInvalidated -= HexBufferStream_BufferStreamSpanInvalidated;
			}
		}
		EventHandler<HexBufferSpanInvalidatedEventArgs> bufferSpanInvalidated;

		HexBufferStream stream;
		HexVersionImpl currentHexVersion;
		readonly bool disposeStream;

		public HexBufferImpl(HexBufferStream stream, HexTags tags, bool disposeStream)
			: base(tags) {
			if (stream == null)
				throw new ArgumentNullException(nameof(stream));
			this.stream = stream;
			this.disposeStream = disposeStream;
			currentHexVersion = new HexVersionImpl(this, 0, 0);
		}

		void HexBufferStream_BufferStreamSpanInvalidated(object sender, HexBufferStreamSpanInvalidatedEventArgs e) =>
			bufferSpanInvalidated?.Invoke(this, new HexBufferSpanInvalidatedEventArgs(e.Span));

		void CreateNewVersion(IList<HexChange> changes, int? reiteratedVersionNumber = null) =>
			currentHexVersion = currentHexVersion.SetChanges(changes, reiteratedVersionNumber);

		Thread ownerThread;
		bool CheckAccess() => ownerThread == null || ownerThread == Thread.CurrentThread;
		void VerifyAccess() {
			if (!CheckAccess())
				throw new InvalidOperationException();
		}

		HexEditImpl hexEditInProgress;
		public override bool EditInProgress => hexEditInProgress != null;
		public override bool CheckEditAccess() => CheckAccess();

		public override void TakeThreadOwnership() {
			if (ownerThread != null && ownerThread != Thread.CurrentThread)
				throw new InvalidOperationException();
			ownerThread = Thread.CurrentThread;
		}

		public override HexSpanInfo GetSpanInfo(HexPosition position) {
			if (position >= HexPosition.MaxEndPosition)
				throw new ArgumentOutOfRangeException(nameof(position));
			return stream.GetSpanInfo(position);
		}

		public override HexEdit CreateEdit() => CreateEdit(null, null);
		public override HexEdit CreateEdit(int? reiteratedVersionNumber, object editTag) {
			VerifyAccess();
			if (EditInProgress)
				throw new InvalidOperationException("An edit operation is in progress");
			return hexEditInProgress = new HexEditImpl(this, reiteratedVersionNumber, editTag);
		}

		internal void Cancel(HexEditImpl hexEdit) {
			VerifyAccess();
			if (hexEdit != hexEditInProgress)
				throw new InvalidOperationException();
			hexEditInProgress = null;
			PostChanged?.Invoke(this, EventArgs.Empty);
		}

		bool RaiseChangingGetIsCanceled(object editTag) {
			var c = Changing;
			if (c == null)
				return false;

			Action<HexContentChangingEventArgs> cancelAction = null;
			var args = new HexContentChangingEventArgs(Version, editTag, cancelAction);
			foreach (EventHandler<HexContentChangingEventArgs> handler in c.GetInvocationList()) {
				handler(this, args);
				if (args.Canceled)
					break;
			}
			return args.Canceled;
		}

		internal void ApplyChanges(HexEditImpl hexEdit, List<HexChange> changes, int? reiteratedVersionNumber, object editTag) {
			VerifyAccess();
			if (hexEdit != hexEditInProgress)
				throw new InvalidOperationException();
			hexEditInProgress = null;

			if (RaiseChangingGetIsCanceled(editTag)) {
				PostChanged?.Invoke(this, EventArgs.Empty);
				return;
			}

			if (changes.Count != 0) {
				// We don't support overlapping changes. All offsets are relative to the original buffer
				changes.Sort(ReverseOldPositionSorter.Instance);
				for (int i = 1; i < changes.Count; i++) {
					if (changes[i - 1].OldSpan.OverlapsWith(changes[i].OldSpan))
						throw new InvalidOperationException("Two edit operations overlap");
				}

				var beforeVersion = Version;
				// changes is sorted in reverse order by OldPosition
				foreach (var change in changes)
					stream.Write(change.OldPosition, change.NewData);
				CreateNewVersion(changes, reiteratedVersionNumber);
				var afterVersion = Version;

				HexContentChangedEventArgs args = null;
				//TODO: The event handlers are allowed to modify the buffer, but the new events must only be
				//		raised after all of these three events have been raised.
				ChangedHighPriority?.Invoke(this, args ?? (args = new HexContentChangedEventArgs(beforeVersion, afterVersion, editTag)));
				Changed?.Invoke(this, args ?? (args = new HexContentChangedEventArgs(beforeVersion, afterVersion, editTag)));
				ChangedLowPriority?.Invoke(this, args ?? (args = new HexContentChangedEventArgs(beforeVersion, afterVersion, editTag)));
			}
			PostChanged?.Invoke(this, EventArgs.Empty);
		}

		sealed class ReverseOldPositionSorter : IComparer<HexChange> {
			public static readonly ReverseOldPositionSorter Instance = new ReverseOldPositionSorter();
			public int Compare(HexChange x, HexChange y) => y.OldPosition.CompareTo(x.OldPosition);
		}

		public override int TryReadByte(HexPosition position) => stream.TryReadByte(position);
		public override byte ReadByte(HexPosition position) => stream.ReadByte(position);
		public override sbyte ReadSByte(HexPosition position) => stream.ReadSByte(position);
		public override short ReadInt16(HexPosition position) => stream.ReadInt16(position);
		public override ushort ReadUInt16(HexPosition position) => stream.ReadUInt16(position);
		public override int ReadInt32(HexPosition position) => stream.ReadInt32(position);
		public override uint ReadUInt32(HexPosition position) => stream.ReadUInt32(position);
		public override long ReadInt64(HexPosition position) => stream.ReadInt64(position);
		public override ulong ReadUInt64(HexPosition position) => stream.ReadUInt64(position);
		public override float ReadSingle(HexPosition position) => stream.ReadSingle(position);
		public override double ReadDouble(HexPosition position) => stream.ReadDouble(position);
		public override byte[] ReadBytes(HexPosition position, long length) => stream.ReadBytes(position, length);
		public override byte[] ReadBytes(HexPosition position, ulong length) => stream.ReadBytes(position, checked((long)length));
		public override void ReadBytes(HexPosition position, byte[] destination, long destinationIndex, long length) =>
			stream.ReadBytes(position, destination, destinationIndex, length);
		public override HexBytes ReadHexBytes(HexPosition position, long length) => stream.ReadHexBytes(position, length);

		public override byte[] ReadBytes(HexSpan span) {
			if (span.Length >= HexPosition.MaxEndPosition)
				throw new ArgumentOutOfRangeException(nameof(span));
			return ReadBytes(span.Start, span.Length.ToUInt64());
		}

		protected override void DisposeCore() {
			if (disposeStream)
				stream?.Dispose();
			stream = null;
		}
	}
}
