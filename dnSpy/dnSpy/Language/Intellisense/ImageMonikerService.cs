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
using System.ComponentModel.Composition;
using System.Diagnostics;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Language.Intellisense;
using Microsoft.VisualStudio.Imaging.Interop;

namespace dnSpy.Language.Intellisense {
	[Export(typeof(IImageMonikerService))]
	sealed class ImageMonikerService : IImageMonikerService {
		readonly object lockObj;
		readonly Dictionary<ImageMoniker, ImageReference> toImageReferenceDict;
		readonly Dictionary<ImageReference, ImageMoniker> toImageMonikerDict;
		readonly Guid imageMonikerGuid;
		int imageMonikerId;

		sealed class ImageMonikerEqualityComparer : IEqualityComparer<ImageMoniker> {
			public static readonly IEqualityComparer<ImageMoniker> Instance = new ImageMonikerEqualityComparer();
			public bool Equals(ImageMoniker x, ImageMoniker y) => x.Id == y.Id && x.Guid == y.Guid;
			public int GetHashCode(ImageMoniker obj) => obj.Id ^ obj.Guid.GetHashCode();
		}

		sealed class ImageReferenceEqualityComparer : IEqualityComparer<ImageReference> {
			public static readonly IEqualityComparer<ImageReference> Instance = new ImageReferenceEqualityComparer();
			public bool Equals(ImageReference x, ImageReference y) => x.Assembly == y.Assembly && x.Name == y.Name;
			public int GetHashCode(ImageReference obj) => (obj.Assembly?.GetHashCode() ?? 0) ^ (obj.Name?.GetHashCode() ?? 0);
		}

		ImageMonikerService() {
			lockObj = new object();
			toImageReferenceDict = new Dictionary<ImageMoniker, ImageReference>(ImageMonikerEqualityComparer.Instance);
			toImageMonikerDict = new Dictionary<ImageReference, ImageMoniker>(ImageReferenceEqualityComparer.Instance);
			imageMonikerGuid = Guid.NewGuid();
			imageMonikerId = 1;
		}

		public ImageMoniker ToImageMoniker(ImageReference imageReference) {
			if (imageReference.IsDefault)
				return default(ImageMoniker);
			lock (lockObj) {
				ImageMoniker imageMoniker;
				if (toImageMonikerDict.TryGetValue(imageReference, out imageMoniker))
					return imageMoniker;
				imageMoniker.Guid = imageMonikerGuid;
				imageMoniker.Id = imageMonikerId++;
				toImageMonikerDict.Add(imageReference, imageMoniker);
				Debug.Assert(!toImageReferenceDict.ContainsKey(imageMoniker));
				toImageReferenceDict.Add(imageMoniker, imageReference);
				return imageMoniker;
			}
		}

		public ImageReference ToImageReference(ImageMoniker imageMoniker) {
			if (imageMoniker.Id == 0 && imageMoniker.Guid == Guid.Empty)
				return default(ImageReference);
			lock (lockObj) {
				ImageReference imageReference;
				bool b = toImageReferenceDict.TryGetValue(imageMoniker, out imageReference);
				Debug.Assert(b, $"{nameof(ToImageMoniker)}() hasn't been called yet");
				return imageReference;
			}
		}
	}
}
