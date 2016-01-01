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
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using dnSpy.Contracts.App;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Settings;
using dnSpy.Contracts.Themes;

namespace dnSpy.MainApp {
	interface IDnSpyLoaderManager {
		void Initialize(IDnSpyLoaderContentProvider content, Window window);
		void Save();
		event EventHandler OnAppLoaded;
	}

	interface IDnSpyLoaderContentProvider {
		void SetLoadingContent(object content);
		void RemoveLoadingContent();
	}

	[Export, Export(typeof(IDnSpyLoaderManager)), PartCreationPolicy(CreationPolicy.Shared)]
	sealed class DnSpyLoaderManager : IDnSpyLoaderManager {
		readonly IImageManager imageManager;
		readonly IThemeManager themeManager;
		readonly ISettingsManager settingsManager;
		readonly Lazy<IDnSpyLoader, IDnSpyLoaderMetadata>[] loaders;

		WindowLoader windowLoader;

		public event EventHandler OnAppLoaded;

		[ImportingConstructor]
		DnSpyLoaderManager(IImageManager imageManager, IThemeManager themeManager, ISettingsManager settingsManager, [ImportMany] IEnumerable<Lazy<IDnSpyLoader, IDnSpyLoaderMetadata>> mefLoaders) {
			this.imageManager = imageManager;
			this.themeManager = themeManager;
			this.settingsManager = settingsManager;
			this.loaders = mefLoaders.OrderBy(a => a.Metadata.Order).ToArray();
			this.windowLoader = new WindowLoader(this, imageManager, themeManager, settingsManager, loaders);
		}

		public void Initialize(IDnSpyLoaderContentProvider content, Window window) {
			Debug.Assert(windowLoader != null);
			windowLoader.Initialize(content, window);
		}

		internal void LoadAllCodeFinished() {
			Debug.Assert(windowLoader != null);
			windowLoader = null;
			if (OnAppLoaded != null)
				OnAppLoaded(this, EventArgs.Empty);
		}

		public void Save() {
			foreach (var loader in loaders)
				loader.Value.Save(settingsManager);
		}
	}

	sealed class WindowLoader {
		const int EXEC_TIME_BEFORE_DELAY_MS = 40;
		readonly DnSpyLoaderManager dnSpyLoaderManager;
		readonly IImageManager imageManager;
		readonly IThemeManager themeManager;
		readonly ISettingsManager settingsManager;
		readonly Lazy<IDnSpyLoader, IDnSpyLoaderMetadata>[] loaders;

		Window window;
		IDnSpyLoaderContentProvider content;
		DnSpyLoaderControl dnSpyLoaderControl;
		IEnumerator<object> loaderEnumerator;

		public WindowLoader(DnSpyLoaderManager dnSpyLoaderManager, IImageManager imageManager, IThemeManager themeManager, ISettingsManager settingsManager, Lazy<IDnSpyLoader, IDnSpyLoaderMetadata>[] loaders) {
			this.dnSpyLoaderManager = dnSpyLoaderManager;
			this.imageManager = imageManager;
			this.themeManager = themeManager;
			this.settingsManager = settingsManager;
			this.loaders = loaders;
		}

		public void Initialize(IDnSpyLoaderContentProvider content, Window window) {
			this.window = window;
			this.dnSpyLoaderControl = new DnSpyLoaderControl();
			this.dnSpyLoaderControl.Image.Source = imageManager.GetImage(GetType().Assembly, "dnSpy-Big", ((SolidColorBrush)themeManager.Theme.GetColor(ColorType.EnvironmentBackground).Background).Color);
			this.content = content;
			this.content.SetLoadingContent(this.dnSpyLoaderControl);

			this.window.ContentRendered += Window_ContentRendered;
			this.window.IsEnabled = false;
		}

		void Window_ContentRendered(object sender, EventArgs e) {
			window.ContentRendered -= Window_ContentRendered;
			loaderEnumerator = LoadCode().GetEnumerator();
			StartLoadAllCodeDelay();
		}

		IEnumerable<object> LoadCode() {
			yield return null;
			foreach (var l in loaders) {
				var o = l.Value;
				yield return null;
				foreach (var a in o.Load(settingsManager))
					yield return a;
			}
		}

		void StartLoadAllCodeDelay() {
			window.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(LoadAllCode));
		}

		void LoadAllCode() {
			var sw = Stopwatch.StartNew();
			do {
				if (!loaderEnumerator.MoveNext()) {
					LoadAllCodeFinished();
					return;
				}
				var item = loaderEnumerator.Current;
				if (item == LoaderConstants.Delay)
					break;
			} while (sw.ElapsedMilliseconds < EXEC_TIME_BEFORE_DELAY_MS);
			StartLoadAllCodeDelay();
		}

		void LoadAllCodeFinished() {
			content.RemoveLoadingContent();
			window.IsEnabled = true;
			// This is needed if there's nothing shown at startup (no tabs, no TV, etc), otherwise
			// eg. Ctrl+K won't work.
			window.Focus();
			foreach (var loader in loaders)
				loader.Value.OnAppLoaded();
			dnSpyLoaderManager.LoadAllCodeFinished();
		}
	}
}
