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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using dnSpy.Contracts.BackgroundImage;

namespace dnSpy.BackgroundImage {
	interface IImageSourceServiceListener {
		void OnEnabled();
		void OnDisabled();
		void OnImageChanged();
		void OnSettingsChanged();
	}

	interface IImageSourceService {
		void Register(IImageSourceServiceListener listener);
		void Unregister(IImageSourceServiceListener listener);
		ImageSource ImageSource { get; }
		Stretch Stretch { get; }
		StretchDirection StretchDirection { get; }
		double Opacity { get; }
		double HorizontalOffset { get; }
		double VerticalOffset { get; }
		double LeftMarginWidthPercent { get; }
		double RightMarginWidthPercent { get; }
		double TopMarginHeightPercent { get; }
		double BottomMarginHeightPercent { get; }
		double MaxHeight { get; }
		double MaxWidth { get; }
		double Zoom { get; }
		ImagePlacement ImagePlacement { get; }
	}

	sealed class ImageSourceService : IImageSourceService {
		readonly IBackgroundImageSettings backgroundImageSettings;
		readonly List<IImageSourceServiceListener> listeners;
		ImageIterator imageIterator;
		bool enabled;

		public ImageSource ImageSource => imageIterator.ImageSource;
		public Stretch Stretch => backgroundImageSettings.Stretch;
		public StretchDirection StretchDirection => backgroundImageSettings.StretchDirection;
		public double Opacity => backgroundImageSettings.Opacity;
		public double HorizontalOffset => backgroundImageSettings.HorizontalOffset;
		public double VerticalOffset => backgroundImageSettings.VerticalOffset;
		public double LeftMarginWidthPercent => backgroundImageSettings.LeftMarginWidthPercent;
		public double RightMarginWidthPercent => backgroundImageSettings.RightMarginWidthPercent;
		public double TopMarginHeightPercent => backgroundImageSettings.TopMarginHeightPercent;
		public double BottomMarginHeightPercent => backgroundImageSettings.BottomMarginHeightPercent;
		public double MaxHeight => backgroundImageSettings.MaxHeight;
		public double MaxWidth => backgroundImageSettings.MaxWidth;
		public double Zoom => backgroundImageSettings.Zoom;
		public ImagePlacement ImagePlacement => backgroundImageSettings.ImagePlacement;

		sealed class ImageIterator : IDisposable {
			ImageInfo currentImageInfo;
			FilenameIterator[] filenameIterators;
			int currentFilenameIteratorIndex;
			IEnumerator<string> currentEnumerator;
			bool isRandom;

			sealed class ImageInfo {
				public ImageSource ImageSource { get; }
				public string Filename { get; }
				public ImageInfo(ImageSource imageSource, string filename) {
					if (imageSource == null)
						throw new ArgumentNullException(nameof(imageSource));
					if (filename == null)
						throw new ArgumentNullException(nameof(filename));
					ImageSource = imageSource;
					Filename = filename;
				}
			}

			public ImageSource ImageSource {
				get {
					if (currentImageInfo == null)
						throw new InvalidOperationException();
					return currentImageInfo.ImageSource;
				}
			}

			public bool HasImageSource => currentImageInfo != null;

			abstract class FilenameIterator {
				public abstract IEnumerable<string> Filenames { get; }
			}

			sealed class FileIterator : FilenameIterator {
				readonly string filename;

				public FileIterator(string filename) {
					this.filename = filename;
				}

				public override IEnumerable<string> Filenames {
					get { yield return filename; }
				}
			}

			sealed class DirectoryIterator : FilenameIterator {
				static readonly string[] imageFileExtensions = new string[] {
					"*.png",
					"*.bmp",
					"*.jpg",
					"*.jpeg",
					"*.gif",
				};
				readonly string dirPath;

				public DirectoryIterator(string dirPath) {
					this.dirPath = dirPath;
				}

				public override IEnumerable<string> Filenames => GetFiles();

				string[] GetFiles() {
					var list = new List<string>();
					foreach (var searchPattern in imageFileExtensions)
						list.AddRange(GetFiles(searchPattern));
					list.Sort(StringComparer.InvariantCultureIgnoreCase);
					return list.ToArray();
				}

				string[] GetFiles(string searchPattern) {
					if (!Directory.Exists(dirPath))
						return Array.Empty<string>();
					try {
						return Directory.GetFiles(dirPath, searchPattern);
					}
					catch {
					}
					return Array.Empty<string>();
				}
			}

			public ImageIterator(bool isRandom) {
				this.filenameIterators = Array.Empty<FilenameIterator>();
				this.currentFilenameIteratorIndex = 0;
				this.currentEnumerator = null;
				this.isRandom = isRandom;
			}

			public void SetImagePaths(string[] imagePaths, bool isRandom) {
				if (imagePaths == null)
					throw new ArgumentNullException(nameof(imagePaths));
				var list = new List<FilenameIterator>(imagePaths.Length);
				foreach (var path in imagePaths) {
					if (path == null)
						continue;
					if (HasAllowedUriScheme(path))
						list.Add(new FileIterator(path));
					else if (File.Exists(path))
						list.Add(new FileIterator(path));
					else if (Directory.Exists(path))
						list.Add(new DirectoryIterator(path));
				}
				this.isRandom = isRandom;
				cachedAllFilenamesListWeakRef = null;
				currentEnumerator?.Dispose();
				currentEnumerator = null;
				filenameIterators = list.ToArray();
				currentFilenameIteratorIndex = 0;
				NextImageSource();
			}

			public bool NextImageSource() {
				var oldImgInfo = currentImageInfo;
				if (isRandom) {
					var list = Shuffle(random, GetAllFilenames());
					currentImageInfo = TryCreateNextImageSource(list);
				}
				else
					currentImageInfo = TryCreateNextImageSource();
				return oldImgInfo != currentImageInfo;
			}
			static readonly Random random = new Random();

			static List<T> Shuffle<T>(Random random, List<T> list) {
				int n = list.Count;
				while (n > 1) {
					n--;
					int k = random.Next(n + 1);
					var value = list[k];
					list[k] = list[n];
					list[n] = value;
				}
				return list;
			}

			List<string> GetAllFilenames() {
				var list = cachedAllFilenamesListWeakRef?.Target as List<string>;
				if (list != null && (DateTimeOffset.Now - cachedTime).TotalMilliseconds <= cachedFilenamesMaxMilliseconds)
					return list;

				var hash = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);
				foreach (var iter in filenameIterators) {
					foreach (var filename in iter.Filenames)
						hash.Add(filename);
				}
				cachedTime = DateTimeOffset.Now;
				cachedAllFilenamesListWeakRef = new WeakReference(list = hash.ToList());
				return list;
			}
			WeakReference cachedAllFilenamesListWeakRef;
			DateTimeOffset cachedTime;
			const double cachedFilenamesMaxMilliseconds = 5 * 1000;

			ImageInfo TryCreateNextImageSource(List<string> filenames) {
				foreach (var filename in filenames) {
					var imgInfo = TryCreateImageSource(filename);
					if (imgInfo != null)
						return imgInfo;
				}

				return null;
			}

			ImageInfo TryCreateNextImageSource() {
				if (filenameIterators.Length == 0)
					return null;
				if (currentEnumerator == null)
					currentEnumerator = filenameIterators[currentFilenameIteratorIndex].Filenames.GetEnumerator();
				var imgInfo = TryCreateNextImageSource(currentEnumerator);
				if (imgInfo != null)
					return imgInfo;

				int baseIndex = currentFilenameIteratorIndex;
				// This loop will retry the current iterator again in case it has already returned
				// some filenames.
				for (int i = 0; i < filenameIterators.Length; i++) {
					currentEnumerator?.Dispose();
					currentEnumerator = null;
					currentFilenameIteratorIndex = (i + 1 + baseIndex) % filenameIterators.Length;
					currentEnumerator = filenameIterators[currentFilenameIteratorIndex].Filenames.GetEnumerator();

					imgInfo = TryCreateNextImageSource(currentEnumerator);
					if (imgInfo != null)
						return imgInfo;
				}

				return null;
			}

			ImageInfo TryCreateNextImageSource(IEnumerator<string> enumerator) {
				if (enumerator == null)
					return null;
				while (enumerator.MoveNext()) {
					var imgInfo = TryCreateImageSource(enumerator.Current);
					if (imgInfo != null)
						return imgInfo;
				}
				return null;
			}

			static bool HasAllowedUriScheme(string filename) {
				foreach (var scheme in allowedUriSchemes) {
					if (filename.StartsWith(scheme, StringComparison.OrdinalIgnoreCase))
						return true;
				}
				return false;
			}
			static readonly string[] allowedUriSchemes = new string[] {
				"pack://",
				"file://",
			};

			ImageInfo TryCreateImageSource(string filename) {
				if (!HasAllowedUriScheme(filename) && !File.Exists(filename))
					return null;
				if (currentImageInfo != null && StringComparer.InvariantCultureIgnoreCase.Equals(filename, currentImageInfo.Filename))
					return currentImageInfo;
				try {
					// Make sure \\?\C:\some\path\image.png won't throw an exception
					var img = new BitmapImage(new Uri(filename, filename.StartsWith(@"\\") ? UriKind.Relative : UriKind.RelativeOrAbsolute));
					img.Freeze();
					return new ImageInfo(img, filename);
				}
				catch {
				}
				return null;
			}

			public void Dispose() => currentEnumerator?.Dispose();
		}

		public ImageSourceService(IBackgroundImageSettings backgroundImageSettings) {
			if (backgroundImageSettings == null)
				throw new ArgumentNullException(nameof(backgroundImageSettings));
			this.backgroundImageSettings = backgroundImageSettings;
			this.listeners = new List<IImageSourceServiceListener>();
		}

		public void Register(IImageSourceServiceListener listener) {
			if (listener == null)
				throw new ArgumentNullException(nameof(listener));
			if (listeners.Contains(listener))
				throw new InvalidOperationException();
			if (listeners.Count == 0) {
				Debug.Assert(imageIterator == null);
				imageIterator = new ImageIterator(backgroundImageSettings.IsRandom);
				backgroundImageSettings.SettingsChanged += BackgroundImageSettings_SettingsChanged;
				OnSettingsChanged();
			}
			listeners.Add(listener);
			if (enabled) {
				listener.OnEnabled();
				listener.OnSettingsChanged();
			}
		}

		public void Unregister(IImageSourceServiceListener listener) {
			if (listener == null)
				throw new ArgumentNullException(nameof(listener));
			int index = listeners.IndexOf(listener);
			if (index < 0)
				throw new ArgumentException();
			listeners.RemoveAt(index);
			if (listeners.Count == 0) {
				DisposeTimer();
				backgroundImageSettings.SettingsChanged -= BackgroundImageSettings_SettingsChanged;
				Debug.Assert(imageIterator != null);
				imageIterator.Dispose();
				imageIterator = null;
			}
		}

		void BackgroundImageSettings_SettingsChanged(object sender, EventArgs e) => OnSettingsChanged();

		void UpdateEnabled() {
			bool newEnabled = backgroundImageSettings.IsEnabled && imageIterator.HasImageSource;
			if (newEnabled != enabled) {
				enabled = newEnabled;
				UpdateTimer();
				if (enabled) {
					foreach (var listener in listeners)
						listener.OnEnabled();
				}
				else {
					foreach (var listener in listeners)
						listener.OnDisabled();
				}
			}
		}

		void NotifyImageChanged() {
			if (enabled) {
				Debug.Assert(imageIterator.HasImageSource);
				foreach (var listener in listeners)
					listener.OnImageChanged();
			}
		}

		void OnSettingsChanged() {
			if (backgroundImageSettings.IsEnabled)
				imageIterator.SetImagePaths(backgroundImageSettings.Images, backgroundImageSettings.IsRandom);
			UpdateEnabled();
			UpdateTimer();
			NotifySettingsChanged();
		}
		DispatcherTimer dispatcherTimer;

		void TimerHandlerShowNextImage(object sender, EventArgs e) {
			if (dispatcherTimer != sender)
				return;
			if (imageIterator == null)
				return;
			if (imageIterator.NextImageSource()) {
				UpdateEnabled();
				NotifyImageChanged();
			}
		}

		void UpdateTimer() {
			const double minimumMilliseconds = 50;
			if (!enabled || backgroundImageSettings.Interval < TimeSpan.FromMilliseconds(minimumMilliseconds))
				DisposeTimer();
			else if (dispatcherTimer != null) {
				// Settings got changed, always write the value even if it's identical. If we
				// don't, the next image could be shown in eg. 1 second.
				dispatcherTimer.Interval = backgroundImageSettings.Interval;
			}
			else
				dispatcherTimer = new DispatcherTimer(backgroundImageSettings.Interval, DispatcherPriority.Background, TimerHandlerShowNextImage, Dispatcher.CurrentDispatcher);
		}

		void DisposeTimer() {
			if (dispatcherTimer == null)
				return;
			dispatcherTimer.Stop();
			dispatcherTimer = null;
		}

		void NotifySettingsChanged() {
			if (enabled) {
				Debug.Assert(imageIterator.HasImageSource);
				foreach (var listener in listeners)
					listener.OnSettingsChanged();
			}
		}
	}
}
