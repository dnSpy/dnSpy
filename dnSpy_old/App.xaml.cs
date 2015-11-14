// Copyright (c) 2011 AlphaSierraPapa for the SharpDevelop Team
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Navigation;
using System.Windows.Threading;
using dnSpy;
using dnSpy.Contracts;
using dnSpy.NRefactory;
using dnSpy.Shared.UI.MVVM;
using ICSharpCode.ILSpy.TextView;

namespace dnSpy {
	public static class StartUpClass {
		[STAThread]
		public static void Main() {
			if (!dnlib.Settings.IsThreadSafe) {
				MessageBox.Show("dnlib wasn't compiled with THREAD_SAFE defined.");
				Environment.Exit(1);
			}

			ICSharpCode.ILSpy.App.Main();
		}
	}
}

namespace ICSharpCode.ILSpy {
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application {
		internal static CommandLineArguments CommandLineArguments;

		public App() {
			// Add Ctrl+Shift+Z as a redo command. Don't know why it isn't enabled by default.
			ApplicationCommands.Redo.InputGestures.Add(new KeyGesture(Key.Z, ModifierKeys.Control | ModifierKeys.Shift));

			var cmdArgs = Environment.GetCommandLineArgs().Skip(1);
			App.CommandLineArguments = new CommandLineArguments(cmdArgs);
			if (App.CommandLineArguments.SingleInstance ?? true) {
				cmdArgs = cmdArgs.Select(FullyQualifyPath);
				string message = string.Join(Environment.NewLine, cmdArgs);
				if (SendToPreviousInstance("dnSpy:\r\n" + message, !App.CommandLineArguments.NoActivate)) {
					Environment.Exit(0);
				}
			}
			InitializeComponent();
			AddMergedResourceDictionary(typeof(AppCreator).Assembly, "Themes/wpf.styles.templates.xaml");
			AddMergedResourceDictionary(typeof(RelayCommand).Assembly, "Themes/wpf.styles.templates.xaml");
			AddMergedResourceDictionary(GetType().Assembly, "DnTheme/wpf.styles.templates.xaml");
			AddMergedResourceDictionary(GetType().Assembly, "TreeNodes/Hex/wpf.styles.templates.xaml");

			var asms = new List<Assembly>();
			asms.Add(GetType().Assembly);
			asms.Add(typeof(RelayCommand).Assembly);	// dnSpy.Shared.UI
			AppCreator.Create(asms, "*.Plugin.dll");
			((AppImpl)DnSpy.App).InitializeSettings();

			Languages.Initialize();

			if (!System.Diagnostics.Debugger.IsAttached) {
				AppDomain.CurrentDomain.UnhandledException += ShowErrorBox;
				Dispatcher.CurrentDispatcher.UnhandledException += Dispatcher_UnhandledException;
			}
			TaskScheduler.UnobservedTaskException += DotNet40_UnobservedTaskException;

			EventManager.RegisterClassHandler(typeof(Window),
											  Hyperlink.RequestNavigateEvent,
											  new RequestNavigateEventHandler(Window_RequestNavigate));

			FixEditorContextMenuStyle();
		}

		public static void AddMergedResourceDictionary(Assembly asm, string path) {
			var name = asm.GetName();
			var s = "pack://application:,,,/" + name.Name + ";v" + name.Version + ";component/" + path;
			var uri = new Uri(s, UriKind.Absolute);
			var rsrcDict = new ResourceDictionary();
			rsrcDict.Source = uri;
			Current.Resources.MergedDictionaries.Add(rsrcDict);
		}

		// The text editor creates an EditorContextMenu which derives from ContextMenu. This
		// class is private in the assembly and can't be referenced from XAML. In order to style
		// this class we must get the type at runtime and add its style to the Resources.
		void FixEditorContextMenuStyle() {
			var module = typeof(System.Windows.Controls.ContextMenu).Module;
			var type = module.GetType("System.Windows.Documents.TextEditorContextMenu+EditorContextMenu", false, false);
			Debug.Assert(type != null);
			if (type == null)
				return;
			const string styleKey = "EditorContextMenuStyle";
			var style = this.Resources[styleKey];
			Debug.Assert(style != null);
			if (style == null)
				return;
			this.Resources.Remove(styleKey);
			this.Resources.Add(type, style);
		}

		string FullyQualifyPath(string argument) {
			// Fully qualify the paths before passing them to another process,
			// because that process might use a different current directory.
			if (string.IsNullOrEmpty(argument) || argument[0] == '/')
				return argument;
			try {
				return Path.Combine(Environment.CurrentDirectory, argument);
			}
			catch (ArgumentException) {
				return argument;
			}
		}

		void DotNet40_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e) {
			// On .NET 4.0, an unobserved exception in a task terminates the process unless we mark it as observed
			e.SetObserved();
		}

		#region Exception Handling
		static void Dispatcher_UnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e) {
			Debug.WriteLine(e.Exception.ToString());
			MessageBox.Show(e.Exception.ToString(), "Sorry, we crashed");
			e.Handled = true;
		}

		static void ShowErrorBox(object sender, UnhandledExceptionEventArgs e) {
			Exception ex = e.ExceptionObject as Exception;
			if (ex != null) {
				Debug.WriteLine(ex.ToString());
				MessageBox.Show(ex.ToString(), "Sorry, we crashed");
			}
		}
		#endregion

		#region Pass Command Line Arguments to previous instance
		bool SendToPreviousInstance(string message, bool activate) {
			bool success = false;
			NativeMethods.EnumWindows(
				(hWnd, lParam) => {
					string windowTitle = NativeMethods.GetWindowText(hWnd, 100);
					if (windowTitle.StartsWith("dnSpy (", StringComparison.Ordinal)) {
						Debug.WriteLine("Found {0:x4}: {1}", hWnd, windowTitle);
						IntPtr result = Send(hWnd, message);
						Debug.WriteLine("WM_COPYDATA result: {0:x8}", result);
						if (result == (IntPtr)0x2E9A5913) {
							if (activate)
								NativeMethods.SetForegroundWindow(hWnd);
							success = true;
							return false; // stop enumeration
						}
					}
					return true; // continue enumeration
				}, IntPtr.Zero);
			return success;
		}

		unsafe static IntPtr Send(IntPtr hWnd, string message) {
			const uint SMTO_NORMAL = 0;

			CopyDataStruct lParam;
			lParam.Padding = IntPtr.Zero;
			lParam.Size = message.Length * 2;
			fixed (char* buffer = message)
			{
				lParam.Buffer = (IntPtr)buffer;
				IntPtr result;
				// SendMessage with 3s timeout (e.g. when the target process is stopped in the debugger)
				if (NativeMethods.SendMessageTimeout(
					hWnd, NativeMethods.WM_COPYDATA, IntPtr.Zero, ref lParam,
					SMTO_NORMAL, 3000, out result) != IntPtr.Zero) {
					return result;
				}
				else {
					return IntPtr.Zero;
				}
			}
		}
		#endregion

		void Window_RequestNavigate(object sender, RequestNavigateEventArgs e) {
			if (e.Uri.Scheme == "resource") {
				AvalonEditTextOutput output = new AvalonEditTextOutput();
				using (Stream s = typeof(App).Assembly.GetManifestResourceStream(typeof(dnSpy.StartUpClass), e.Uri.AbsolutePath)) {
					using (StreamReader r = new StreamReader(s)) {
						string line;
						while ((line = r.ReadLine()) != null) {
							output.Write(line, TextTokenType.Text);
							output.WriteLine();
						}
					}
				}
				var textView = ILSpy.MainWindow.Instance.SafeActiveTextView;
				textView.ShowText(output);
				ILSpy.MainWindow.Instance.SetTitle(textView, e.Uri.AbsolutePath);
				e.Handled = true;
			}
		}
	}
}