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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;
using dnSpy.Contracts.App;
using dnSpy.Contracts.Controls;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Documents.Tabs;
using dnSpy.Contracts.ETW;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Resources;
using dnSpy.Contracts.Settings;
using dnSpy.Controls;
using dnSpy.Culture;
using dnSpy.Documents.Tabs.Dialogs;
using dnSpy.Documents.TreeView;
using dnSpy.Extension;
using dnSpy.Images;
using dnSpy.Roslyn.Text.Classification;
using dnSpy.Scripting;
using dnSpy.Settings;
using Microsoft.VisualStudio.Composition;

namespace dnSpy.MainApp {
	sealed partial class App : Application {
		static void NotExecuted() {
			// Make sure we have a ref to the assembly. The file is copied to the correct location
			// but unless we have a reference to it in the code (or XAML), it could easily happen
			// that someone accidentally removes the reference. This code makes sure there'll be
			// a compilation error if that ever happens.
			var t = typeof(Images.Dummy);
		}

		static void InstallExceptionHandlers() {
			TaskScheduler.UnobservedTaskException += (s, e) => e.SetObserved();
			if (!System.Diagnostics.Debugger.IsAttached) {
				AppDomain.CurrentDomain.UnhandledException += (s, e) => ShowException(e.ExceptionObject as Exception);
				Dispatcher.CurrentDispatcher.UnhandledException += (s, e) => {
					ShowException(e.Exception);
					e.Handled = true;
				};
			}
		}

		static void ShowException(Exception? ex) {
			string msg = ex?.ToString() ?? "Unknown exception";
			MessageBox.Show(msg, Constants.DnSpy, MessageBoxButton.OK, MessageBoxImage.Error);
		}

		readonly ResourceManagerTokenCacheImpl resourceManagerTokenCacheImpl;
		long resourceManagerTokensOffset;
		volatile Assembly[]? mefAssemblies;
		AppWindow? appWindow;
		ExtensionService? extensionService;
		IDsLoaderService? dsLoaderService;
		readonly List<LoadedExtension> loadedExtensions = new List<LoadedExtension>();
		readonly IAppCommandLineArgs args;
		ExportProvider? exportProvider;
#if NETCOREAPP
		readonly NetCoreAssemblyLoader netCoreAssemblyLoader = new NetCoreAssemblyLoader(System.Runtime.Loader.AssemblyLoadContext.Default);
#endif

		Task<ExportProvider> initializeMEFTask;
		Stopwatch? startupStopwatch;
		public App(bool readSettings, Stopwatch startupStopwatch) {
			resourceManagerTokenCacheImpl = new ResourceManagerTokenCacheImpl();

			// PERF: Init MEF on a BG thread. Results in slightly faster startup, eg. InitializeComponent() becomes a 'free' call on this UI thread
			initializeMEFTask = Task.Run(() => InitializeMEF(readSettings, useCache: readSettings));
			this.startupStopwatch = startupStopwatch;

			resourceManagerTokenCacheImpl.TokensUpdated += ResourceManagerTokenCacheImpl_TokensUpdated;
			ResourceHelper.SetResourceManagerTokenCache(resourceManagerTokenCacheImpl);
			args = new AppCommandLineArgs();
			AppDirectories.SetSettingsFilename(args.SettingsFilename);

			AddAppContextFixes();
			InstallExceptionHandlers();
			InitializeComponent();
			UIFixes();

			Exit += App_Exit;
		}

		void AddAppContextFixes() {
			// This prevents a thin line between the tab item and its content when dpi is eg. 144.
			// It's hard to miss if you check the Options dialog box.
			AppContext.SetSwitch("Switch.MS.Internal.DoNotApplyLayoutRoundingToMarginsAndBorderThickness", true);

#if NETFRAMEWORK
			// Workaround for a bug
			//		Switch.System.Windows.Controls.Grid.StarDefinitionsCanExceedAvailableSpace=true
			//		https://docs.microsoft.com/en-us/dotnet/framework/migration-guide/runtime/4.7-4.7.1#resizing-a-grid-can-hang
			// Repro: DPI=120%, .NET Framework 4.7.1, open the File, View, or Window menus
			//		https://github.com/0xd4d/dnSpy/issues/734
			//		https://github.com/0xd4d/dnSpy/issues/735
			// This has been fixed in .NET Core 3.0 and .NET Framework 4.8
#if NET48
#error Remove this now
#endif
			AppContext.SetSwitch("Switch.System.Windows.Controls.Grid.StarDefinitionsCanExceedAvailableSpace", true);
#endif
		}

		ExportProvider InitializeMEF(bool readSettings, bool useCache) {
			mefAssemblies = GetAssemblies();
			var resolver = Resolver.DefaultInstance;

			var factory = TryCreateExportProviderFactoryCached(resolver, useCache, out resourceManagerTokensOffset) ?? CreateExportProviderFactorySlow(resolver);
			var exportProvider = factory.CreateExportProvider();

			exportProvider.GetExportedValue<ServiceLocator>().SetExportProvider(Dispatcher, exportProvider);
			if (readSettings) {
				var settingsService = exportProvider.GetExportedValue<ISettingsService>();
				try {
					new XmlSettingsReader(settingsService).Read();
				}
				catch {
				}
			}

			return exportProvider;
		}

		static string GetCachedCompositionConfigurationFilename() {
			var profileDir = BGJitUtils.GetFolder();
			return Path.Combine(profileDir, Constants.DnSpyFile + "-mef-info.bin");
		}

		IExportProviderFactory? TryCreateExportProviderFactoryCached(Resolver resolver, bool useCache, out long resourceManagerTokensOffset) {
			resourceManagerTokensOffset = -1;
			if (!useCache)
				return null;
			try {
				return TryCreateExportProviderFactoryCachedCore(resolver, out resourceManagerTokensOffset);
			}
			catch (Exception ex) {
				Debug.Fail(ex.ToString());
				return null;
			}
		}

		IExportProviderFactory? TryCreateExportProviderFactoryCachedCore(Resolver resolver, out long resourceManagerTokensOffset) {
			Debug2.Assert(!(mefAssemblies is null));
			resourceManagerTokensOffset = -1;
			var filename = GetCachedCompositionConfigurationFilename();
			if (!File.Exists(filename))
				return null;

			Stream? cachedStream = null;
			try {
				try {
					cachedStream = File.OpenRead(filename);
					if (!new CachedMefInfo(mefAssemblies, cachedStream).CheckFile(resourceManagerTokenCacheImpl, out resourceManagerTokensOffset))
						return null;
				}
				catch (Exception ex) when (IsFileIOException(ex)) {
					return null;
				}
				try {
					return new CachedComposition().LoadExportProviderFactoryAsync(cachedStream, resolver).Result;
				}
				catch (AggregateException ex) when (ex.InnerExceptions.Count == 1 && IsFileIOException(ex.InnerExceptions[0])) {
					return null;
				}
			}
			finally {
				cachedStream?.Dispose();
			}

			static bool IsFileIOException(Exception ex) => ex is IOException || ex is UnauthorizedAccessException || ex is SecurityException;
		}

		IExportProviderFactory CreateExportProviderFactorySlow(Resolver resolver) {
			var discovery = new AttributedPartDiscoveryV1(resolver);
			var parts = discovery.CreatePartsAsync(mefAssemblies).Result;
			Debug.Assert(parts.ThrowOnErrors() == parts);

			var catalog = ComposableCatalog.Create(resolver).AddParts(parts);
			var config = CompositionConfiguration.Create(catalog);
			// If this fails/throws, one of the following is probably true:
			//	- you didn't build all projects or all files aren't in the same output dir
			//	- netcoreapp: dnSpy isn't the startup project (eg. dnSpy-x86 is)
			Debug.Assert(config.ThrowOnErrors() == config);

			writingCachedMefFile = true;
			Task.Run(() => SaveMefStateAsync(config)).ContinueWith(t => {
				var ex = t.Exception;
				Debug2.Assert(ex is null);
				writingCachedMefFile = false;
			}, CancellationToken.None);

			return config.CreateExportProviderFactory();
		}

		bool writingCachedMefFile;
		async Task SaveMefStateAsync(CompositionConfiguration config) {
			Debug2.Assert(!(mefAssemblies is null));
			string filename = GetCachedCompositionConfigurationFilename();
			bool fileCreated = false;
			bool deleteFile = true;
			try {
				using (var cachedStream = File.Create(filename)) {
					fileCreated = true;
					long resourceManagerTokensOffsetTmp;
					new CachedMefInfo(mefAssemblies, cachedStream).WriteFile(resourceManagerTokenCacheImpl.GetTokens(mefAssemblies), out resourceManagerTokensOffsetTmp);
					await new CachedComposition().SaveAsync(config, cachedStream);
					resourceManagerTokensOffset = resourceManagerTokensOffsetTmp;
					deleteFile = false;
				}
			}
			catch (IOException) {
			}
			catch (UnauthorizedAccessException) {
			}
			catch (SecurityException) {
			}
			finally {
				if (fileCreated && deleteFile) {
					try {
						File.Delete(filename);
					}
					catch { }
				}
			}
		}

		void ResourceManagerTokenCacheImpl_TokensUpdated(object? sender, EventArgs e) => OnTokensUpdated();

		void OnTokensUpdated() {
			if (writingCachedMefFile)
				Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(OnTokensUpdated));
			else
				UpdateResourceManagerTokens();
		}

		void UpdateResourceManagerTokens() {
			Debug2.Assert(!(mefAssemblies is null));
			var tokensOffset = resourceManagerTokensOffset;
			if (tokensOffset < 0)
				return;
			string filename = GetCachedCompositionConfigurationFilename();
			if (!File.Exists(filename))
				return;
			bool deleteFile = true;
			try {
				using (var cachedStream = File.OpenWrite(filename)) {
					new CachedMefInfo(mefAssemblies, cachedStream).UpdateResourceManagerTokens(tokensOffset, resourceManagerTokenCacheImpl);
					deleteFile = false;
				}
			}
			catch (IOException) {
			}
			catch (UnauthorizedAccessException) {
			}
			catch (SecurityException) {
			}
			if (deleteFile) {
				try {
					File.Delete(filename);
				}
				catch { }
			}
		}

		Assembly[] GetAssemblies() {
#if NETCOREAPP
			netCoreAssemblyLoader.AddSearchPath(AppDirectories.BinDirectory);
#endif
			var list = new List<Assembly>();
			list.Add(GetType().Assembly);
			// dnSpy.Contracts.DnSpy
			list.Add(typeof(MetroWindow).Assembly);
			// dnSpy.Roslyn
			list.Add(typeof(RoslynClassifier).Assembly);
			// Microsoft.VisualStudio.Text.Logic (needed for the editor option definitions)
			list.Add(typeof(Microsoft.VisualStudio.Text.Editor.ConvertTabsToSpaces).Assembly);
			// Microsoft.VisualStudio.Text.UI (needed for the editor option definitions)
			list.Add(typeof(Microsoft.VisualStudio.Text.Editor.AutoScrollEnabled).Assembly);
			// Microsoft.VisualStudio.Text.UI.Wpf (needed for the editor option definitions)
			list.Add(typeof(Microsoft.VisualStudio.Text.Editor.HighlightCurrentLineOption).Assembly);
			// dnSpy.Roslyn.EditorFeatures
			list.Add(typeof(Roslyn.EditorFeatures.Dummy).Assembly);
			// dnSpy.Roslyn.CSharp.EditorFeatures
			list.Add(typeof(Roslyn.CSharp.EditorFeatures.Dummy).Assembly);
			// dnSpy.Roslyn.VisualBasic.EditorFeatures
			list.Add(typeof(Roslyn.VisualBasic.EditorFeatures.Dummy).Assembly);
			foreach (var asm in LoadExtensionAssemblies())
				list.Add(asm);
			return list.ToArray();
		}

		Assembly[] LoadExtensionAssemblies() {
			var dir = AppDirectories.BinDirectory;
			// Load the modules in a predictable order or multicore-JIT could stop recording. See
			// "Understanding Background JIT compilation -> What can go wrong with background JIT compilation"
			// in the PerfView docs for more info.
			var files = GetExtensionFiles(dir).OrderBy(a => a, StringComparer.OrdinalIgnoreCase).ToArray();
#if NETCOREAPP
			foreach (var file in files)
				netCoreAssemblyLoader.AddSearchPath(Path.GetDirectoryName(file)!);
#endif
			var asms = new List<Assembly>();
			foreach (var file in files) {
				try {
					if (!File.Exists(file))
						continue;
					if (!CanLoadExtension(file))
						continue;
					var asm = Assembly.LoadFrom(file);
					if (!CanLoadExtension(asm)) {
						Debug.WriteLine($"Old extension detected ({file})");
						continue;
					}
					asms.Add(asm);
					loadedExtensions.Add(new LoadedExtension(asm));
				}
				catch (Exception ex) {
					Debug.WriteLine($"Failed to load file '{file}', msg: {ex.Message}");
				}
			}
			return asms.ToArray();
		}

		IEnumerable<string> GetExtensionFiles(string baseDir) {
			const string EXTENSION_SEARCH_PATTERN = "*.x.dll";
			const string EXTENSIONS_SUBDIR = "Extensions";
			foreach (var f in GetFiles(baseDir, EXTENSION_SEARCH_PATTERN))
				yield return f;
			var extensionsSubDir = Path.Combine(baseDir, EXTENSIONS_SUBDIR);
			if (Directory.Exists(extensionsSubDir)) {
				foreach (var f in GetFiles(extensionsSubDir, EXTENSION_SEARCH_PATTERN))
					yield return f;
				foreach (var d in GetDirectories(extensionsSubDir)) {
					foreach (var f in GetFiles(d, EXTENSION_SEARCH_PATTERN))
						yield return f;
				}
			}
		}

		static string[] GetFiles(string dir, string searchPattern) {
			try {
				return Directory.GetFiles(dir, searchPattern);
			}
			catch {
				return Array.Empty<string>();
			}
		}

		static string[] GetDirectories(string dir) {
			try {
				return Directory.GetDirectories(dir);
			}
			catch {
				return Array.Empty<string>();
			}
		}

		bool CanLoadExtension(string file) {
			var xmlFile = file + ".xml";
			var config = ExtensionConfigReader.Read(xmlFile);
			return config.IsSupportedOSversion(Environment.OSVersion.Version) &&
				config.IsSupportedFrameworkVersion(Environment.Version) &&
				config.IsSupportedAppVersion(GetType().Assembly.GetName().Version!);
		}

		bool CanLoadExtension(Assembly asm) {
			var ourPublicKeyToken = GetType().Assembly.GetName().GetPublicKeyToken();
			var minimumVersion = new Version(5, 0, 0, 0);
			foreach (var a in asm.GetReferencedAssemblies()) {
				if (!Equals(ourPublicKeyToken, a.GetPublicKeyToken()))
					continue;
				if (a.Version < minimumVersion)
					return false;
			}
			return true;
		}

		static bool Equals(byte[]? a, byte[]? b) {
			if (a is null || b is null || a.Length != b.Length)
				return false;
			for (int i = 0; i < a.Length; i++) {
				if (a[i] != b[i])
					return false;
			}
			return true;
		}

		[return: MarshalAs(UnmanagedType.Bool)]
		[DllImport("user32")]
		static extern bool SetForegroundWindow(IntPtr hWnd);
		[DllImport("user32")]
		[return: MarshalAs(UnmanagedType.Bool)]
		static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);
		[return: MarshalAs(UnmanagedType.Bool)]
		delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
		[DllImport("user32", CharSet = CharSet.Auto, SetLastError = true)]
		static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);
		[DllImport("user32", CharSet = CharSet.Auto)]
		static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, ref COPYDATASTRUCT lParam);
		[StructLayout(LayoutKind.Sequential)]
		struct COPYDATASTRUCT {
			public IntPtr dwData;
			public int cbData;
			public IntPtr lpData;
		}
		static readonly IntPtr COPYDATASTRUCT_dwData = new IntPtr(0x11C9B152);
		static readonly IntPtr COPYDATASTRUCT_result = new IntPtr(0x615F9D6E);
		const string COPYDATASTRUCT_HEADER = Constants.DnSpy;	// One line only

		void SwitchToOtherInstance() => EnumWindows(EnumWindowsHandler, IntPtr.Zero);

		unsafe bool EnumWindowsHandler(IntPtr hWnd, IntPtr lParam) {
			var sb = new StringBuilder(256);
			GetWindowText(hWnd, sb, sb.Capacity);
			if (sb.ToString().StartsWith(Constants.DnSpy + " ", StringComparison.Ordinal)) {
				var args = Environment.GetCommandLineArgs();
				args[0] = COPYDATASTRUCT_HEADER;
				var msg = string.Join(Environment.NewLine, args);
				COPYDATASTRUCT data;
				data.dwData = COPYDATASTRUCT_dwData;
				data.cbData = msg.Length * 2;
				fixed (void* pmsg = msg) {
					data.lpData = new IntPtr(pmsg);
					var res = SendMessage(hWnd, 0x4A, IntPtr.Zero, ref data);
					if (res == COPYDATASTRUCT_result) {
						if (this.args.Activate)
							SetForegroundWindow(hWnd);
						Environment.Exit(0);
					}
				}
			}

			return true;
		}

		unsafe IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled) {
			if (msg != 0x4A)
				return IntPtr.Zero;

			var data = *(COPYDATASTRUCT*)lParam;
			if (data.dwData != COPYDATASTRUCT_dwData)
				return IntPtr.Zero;

			var argsString = new string((char*)data.lpData, 0, data.cbData / 2);
			var args = argsString.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
			if (args[0] != COPYDATASTRUCT_HEADER)
				return IntPtr.Zero;

			HandleAppArgs(new AppCommandLineArgs(args.Skip(1).ToArray()));
			handled = true;
			return COPYDATASTRUCT_result;
		}

		void MainWindow_SourceInitialized(object? sender, EventArgs e) {
			Debug2.Assert(!(appWindow is null));
			appWindow.MainWindow.SourceInitialized -= MainWindow_SourceInitialized;

			var hwndSource = PresentationSource.FromVisual(appWindow.MainWindow) as HwndSource;
			Debug2.Assert(!(hwndSource is null));
			if (!(hwndSource is null))
				hwndSource.AddHook(WndProc);
		}

		void App_Exit(object? sender, ExitEventArgs e) {
			extensionService?.OnAppExit();
			dsLoaderService?.Save();
			try {
				var settingsService = exportProvider?.GetExportedValue<SettingsService>();
				if (!(settingsService is null))
					new XmlSettingsWriter(settingsService).Write();
			}
			catch {
			}
		}

		void UIFixes() {
			// Add Ctrl+Shift+Z as a redo command. Don't know why it isn't enabled by default.
			ApplicationCommands.Redo.InputGestures.Add(new KeyGesture(Key.Z, ModifierKeys.Control | ModifierKeys.Shift));
			FixEditorContextMenuStyle();
		}

		// The text editor creates an EditorContextMenu which derives from ContextMenu. This
		// class is private in the assembly and can't be referenced from XAML. In order to style
		// this class we must get the type at runtime and add its style to the Resources.
		void FixEditorContextMenuStyle() {
			var module = typeof(ContextMenu).Module;
			var type = module.GetType("System.Windows.Documents.TextEditorContextMenu+EditorContextMenu", false, false);
			Debug2.Assert(!(type is null));
			if (type is null)
				return;
			const string styleKey = "EditorContextMenuStyle";
			var style = Resources[styleKey];
			Debug2.Assert(!(style is null));
			if (style is null)
				return;
			Resources.Remove(styleKey);
			Resources.Add(type, style);
		}

		protected override void OnStartup(StartupEventArgs e) {
			base.OnStartup(e);

			exportProvider = initializeMEFTask.GetAwaiter().GetResult();

			if (args.SingleInstance && !exportProvider.GetExportedValue<AppSettingsImpl>().AllowMoreThanOneInstance)
				SwitchToOtherInstance();

			var cultureService = exportProvider.GetExportedValue<CultureService>();
			cultureService.Initialize(args);

			// Make sure IDpiService gets created before any MetroWindows
			exportProvider.GetExportedValue<IDpiService>();

			// It's needed very early, and an IAutoLoaded can't be used (it gets called too late for the first 64x64 image request)
			DsImageConverter.imageService = exportProvider.GetExportedValue<IImageService>();

			appWindow = exportProvider.GetExportedValue<AppWindow>();
			extensionService = exportProvider.GetExportedValue<ExtensionService>();
			dsLoaderService = exportProvider.GetExportedValue<IDsLoaderService>();

			extensionService.LoadedExtensions = loadedExtensions;
			appWindow.CommandLineArgs = args;

			var win = appWindow.InitializeMainWindow();
			appWindow.MainWindow.SourceInitialized += MainWindow_SourceInitialized;
			dsLoaderService.OnAppLoaded += DsLoaderService_OnAppLoaded;
			dsLoaderService.Initialize(appWindow, win, args);
			extensionService.LoadExtensions(Resources.MergedDictionaries);
			win.Show();
		}

		void DsLoaderService_OnAppLoaded(object? sender, EventArgs e) {
			startupStopwatch!.Stop();
			DnSpyEventSource.Log.StartupStop();
			var sw = startupStopwatch;
			startupStopwatch = null;

			if (args.ShowStartupTime)
				ShowElapsedTime(sw);

			dsLoaderService!.OnAppLoaded -= DsLoaderService_OnAppLoaded;
			appWindow!.AppLoaded = true;
			extensionService!.OnAppLoaded();
			HandleAppArgs(args);
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		static void ShowElapsedTime(Stopwatch sw) => MsgBox.Instance.Show($"{sw.ElapsedMilliseconds} ms, {sw.ElapsedTicks} ticks");

		void HandleAppArgs(IAppCommandLineArgs appArgs) {
			Debug2.Assert(!(exportProvider is null));
			Debug2.Assert(!(appWindow is null));
			if (appArgs.Activate && appWindow.MainWindow.WindowState == WindowState.Minimized)
				WindowUtils.SetState(appWindow.MainWindow, WindowState.Normal);

			var decompiler = GetDecompiler(appArgs.Language);
			if (!(decompiler is null))
				exportProvider.GetExportedValue<IDecompilerService>().Decompiler = decompiler;

			if (!(appArgs.FullScreen is null))
				appWindow.MainWindow.IsFullScreen = appArgs.FullScreen.Value;

			if (appArgs.NewTab)
				exportProvider.GetExportedValue<IDocumentTabService>().OpenEmptyTab();

			var files = appArgs.Filenames.ToArray();
			if (files.Length > 0) {
				var mruList = exportProvider.GetExportedValue<AssemblyExplorerMostRecentlyUsedList>();
				OpenDocumentsHelper.OpenDocuments(exportProvider.GetExportedValue<IDocumentTabService>().DocumentTreeView, appWindow.MainWindow, mruList, files, false);
			}

			// The files were lazily added to the treeview. Make sure they've been added to the TV
			// before we process the remaining command line args.
			if (files.Length > 0)
				Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => HandleAppArgs2(appArgs)));
			else
				HandleAppArgs2(appArgs);
		}

		void HandleAppArgs2(IAppCommandLineArgs appArgs) {
			Debug2.Assert(!(exportProvider is null));
			foreach (var handler in exportProvider.GetExports<IAppCommandLineArgsHandler>().OrderBy(a => a.Value.Order))
				handler.Value.OnNewArgs(appArgs);
		}

		IDecompiler? GetDecompiler(string language) {
			Debug2.Assert(!(exportProvider is null));
			if (string.IsNullOrEmpty(language))
				return null;

			var decompilerService = exportProvider.GetExportedValue<IDecompilerService>();
			if (Guid.TryParse(language, out var guid)) {
				var lang = decompilerService.Find(guid);
				if (!(lang is null))
					return lang;
			}

			return decompilerService.AllDecompilers.FirstOrDefault(a => StringComparer.OrdinalIgnoreCase.Equals(a.UniqueNameUI, language)) ??
				decompilerService.AllDecompilers.FirstOrDefault(a => StringComparer.OrdinalIgnoreCase.Equals(a.GenericNameUI, language));
		}
	}
}
