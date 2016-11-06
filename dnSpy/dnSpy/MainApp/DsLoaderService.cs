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
using System.Windows.Threading;
using dnSpy.Contracts.App;
using dnSpy.Contracts.Settings;

namespace dnSpy.MainApp {
	interface IDsLoaderService {
		void Initialize(IDsLoaderContentProvider content, Window window, IAppCommandLineArgs args);
		void Save();
		event EventHandler OnAppLoaded;
	}

	interface IDsLoaderContentProvider {
		void SetLoadingContent(object content);
		void RemoveLoadingContent();
	}

	[Export(typeof(IDsLoaderService))]
	sealed class DsLoaderService : IDsLoaderService {
		readonly ISettingsService settingsService;
		readonly Lazy<IDsLoader, IDsLoaderMetadata>[] loaders;

		WindowLoader windowLoader;

		public event EventHandler OnAppLoaded;

		[ImportingConstructor]
		DsLoaderService(ISettingsService settingsService, [ImportMany] IEnumerable<Lazy<IDsLoader, IDsLoaderMetadata>> mefLoaders) {
			this.settingsService = settingsService;
			this.loaders = mefLoaders.OrderBy(a => a.Metadata.Order).ToArray();
			this.windowLoader = new WindowLoader(this, settingsService, loaders);
		}

		public void Initialize(IDsLoaderContentProvider content, Window window, IAppCommandLineArgs args) {
			Debug.Assert(windowLoader != null);
			windowLoader.Initialize(content, window, args);
		}

		internal void LoadAllCodeFinished() {
			Debug.Assert(windowLoader != null);
			windowLoader = null;
			OnAppLoaded?.Invoke(this, EventArgs.Empty);
		}

		public void Save() {
			foreach (var loader in loaders)
				loader.Value.Save(settingsService);
		}
	}

	sealed class WindowLoader {
		const int EXEC_TIME_BEFORE_DELAY_MS = 40;
		readonly DsLoaderService dsLoaderService;
		readonly ISettingsService settingsService;
		readonly Lazy<IDsLoader, IDsLoaderMetadata>[] loaders;

		Window window;
		IDsLoaderContentProvider content;
		DsLoaderControl dsLoaderControl;
		IEnumerator<object> loaderEnumerator;
		IAppCommandLineArgs appArgs;

		public WindowLoader(DsLoaderService dsLoaderService, ISettingsService settingsService, Lazy<IDsLoader, IDsLoaderMetadata>[] loaders) {
			this.dsLoaderService = dsLoaderService;
			this.settingsService = settingsService;
			this.loaders = loaders;
		}

		public void Initialize(IDsLoaderContentProvider content, Window window, IAppCommandLineArgs appArgs) {
			this.window = window;
			this.appArgs = appArgs;
			this.dsLoaderControl = new DsLoaderControl();
			this.content = content;
			this.content.SetLoadingContent(this.dsLoaderControl);

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
				foreach (var a in o.Load(settingsService, appArgs))
					yield return a;
			}
		}

		void StartLoadAllCodeDelay() => window.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(LoadAllCode));

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
			// eg. Ctrl+Shift+K won't work.
			window.Focus();
			foreach (var loader in loaders)
				loader.Value.OnAppLoaded();
			dsLoaderService.LoadAllCodeFinished();
		}
	}
}
