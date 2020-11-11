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
using System.ComponentModel.Composition;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.DotNet.Metadata.Internal;

namespace dnSpy.Debugger.DotNet.Metadata.Internal {
	[Export(typeof(DbgRawMetadataService))]
	sealed class DbgRawMetadataServiceImpl : DbgRawMetadataService {
		sealed class RuntimeState : IDisposable {
			public readonly object LockObj = new object();
			public readonly Dictionary<ulong, DbgRawMetadataImpl> Dict = new Dictionary<ulong, DbgRawMetadataImpl>();
			public readonly List<DbgRawMetadataImpl> OtherMetadata = new List<DbgRawMetadataImpl>();
			public void Dispose() {
				foreach (var kv in Dict)
					kv.Value.ForceDispose();
				Dict.Clear();
				foreach (var m in OtherMetadata)
					m.ForceDispose();
				OtherMetadata.Clear();
			}
		}

		public override DbgRawMetadata Create(DbgRuntime runtime, bool isFileLayout, ulong moduleAddress, int moduleSize) {
			if (runtime is null)
				throw new ArgumentNullException(nameof(runtime));
			if (moduleAddress == 0)
				throw new ArgumentOutOfRangeException(nameof(moduleAddress));
			if (moduleSize <= 0)
				throw new ArgumentOutOfRangeException(nameof(moduleSize));

			var state = runtime.GetOrCreateData<RuntimeState>();
			lock (state.LockObj) {
				if (state.Dict.TryGetValue(moduleAddress, out var rawMd)) {
					if (rawMd.TryAddRef() is not null) {
						if (rawMd.IsFileLayout != isFileLayout || rawMd.Size != moduleSize) {
							rawMd.Release();
							throw new InvalidOperationException();
						}
						return rawMd;
					}
					state.Dict.Remove(moduleAddress);
				}

				rawMd = new DbgRawMetadataImpl(runtime.Process, isFileLayout, moduleAddress, moduleSize);
				try {
					state.Dict.Add(moduleAddress, rawMd);
				}
				catch {
					rawMd.Release();
					throw;
				}
				return rawMd;
			}
		}

		public override DbgRawMetadata Create(DbgRuntime runtime, bool isFileLayout, byte[] moduleBytes) {
			if (runtime is null)
				throw new ArgumentNullException(nameof(runtime));
			if (moduleBytes is null)
				throw new ArgumentNullException(nameof(moduleBytes));

			var state = runtime.GetOrCreateData<RuntimeState>();
			lock (state.LockObj) {
				var rawMd = new DbgRawMetadataImpl(moduleBytes, isFileLayout);
				try {
					state.OtherMetadata.Add(rawMd);
				}
				catch {
					rawMd.Release();
					throw;
				}
				return rawMd;
			}
		}
	}
}
