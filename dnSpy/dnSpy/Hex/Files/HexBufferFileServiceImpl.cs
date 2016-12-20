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
using System.Linq;
using dnSpy.Contracts.Hex;
using dnSpy.Contracts.Hex.Files;
using VSUTIL = Microsoft.VisualStudio.Utilities;

namespace dnSpy.Hex.Files {
	sealed class HexBufferFileServiceImpl : HexBufferFileService {
		public override event EventHandler<BufferFilesAddedEventArgs> BufferFilesAdded;
		public override event EventHandler<BufferFilesRemovedEventArgs> BufferFilesRemoved;
		public override HexBuffer Buffer => buffer;

		public override IEnumerable<HexBufferFile> Files {
			get {
				// This property isn't defined to return an ordered enumerable of files so make
				// sure no-one depends on sorted files.
				int index = GetHashCode() % files.Count;
				for (int i = files.Count - 1; i >= 0; i--)
					yield return files[(index + i) % files.Count].Data;
			}
		}

		readonly HexBuffer buffer;
		readonly Lazy<StructureProviderFactory, VSUTIL.IOrderable>[] structureProviderFactories;
		readonly SpanDataCollection<HexBufferFileImpl> files;

		public HexBufferFileServiceImpl(HexBuffer buffer, Lazy<StructureProviderFactory, VSUTIL.IOrderable>[] structureProviderFactories) {
			if (buffer == null)
				throw new ArgumentNullException(nameof(buffer));
			if (structureProviderFactories == null)
				throw new ArgumentNullException(nameof(structureProviderFactories));
			this.buffer = buffer;
			this.structureProviderFactories = structureProviderFactories;
			files = new SpanDataCollection<HexBufferFileImpl>();
		}

		public override HexBufferFile[] CreateFiles(BufferFileOptions[] options) {
			if (options == null)
				throw new ArgumentNullException(nameof(options));
			var newFiles = new HexBufferFileImpl[options.Length];
			for (int i = 0; i < newFiles.Length; i++) {
				var opts = options[i];
				if (opts.IsDefault)
					throw new ArgumentException();
				newFiles[i] = new HexBufferFileImpl(structureProviderFactories, buffer, opts.Span, opts.Name, opts.Filename, opts.Tags);
			}
			files.Add(newFiles.Select(a => new SpanData<HexBufferFileImpl>(a.Span, a)));
			BufferFilesAdded?.Invoke(this, new BufferFilesAddedEventArgs(newFiles));
			return newFiles;
		}

		public override void RemoveFiles(HexSpan span) => RaiseRemovedFiles(files.Remove(new[] { span }));

		public override void RemoveFile(HexBufferFile file) {
			if (file == null)
				throw new ArgumentNullException(nameof(file));
			RaiseRemovedFiles(files.Remove(new[] { file.Span }).ToArray());
		}

		public override void RemoveFiles(IEnumerable<HexBufferFile> files) {
			if (files == null)
				throw new ArgumentNullException(nameof(files));
			RaiseRemovedFiles(this.files.Remove(files.Select(a => a.Span).ToArray()));
		}

		void RaiseRemovedFiles(SpanData<HexBufferFileImpl>[] files) {
			if (files.Length == 0)
				return;
			foreach (var file in files)
				file.Data.RaiseRemoved();
			BufferFilesRemoved?.Invoke(this, new BufferFilesRemovedEventArgs(files.Select(a => a.Data).ToArray()));
		}

		public override HexBufferFile GetFile(HexPosition position) => files.FindData(position);
	}
}
