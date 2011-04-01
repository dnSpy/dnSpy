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
using System.ComponentModel.Composition.Hosting;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Navigation;
using System.Windows.Threading;

namespace ICSharpCode.ILSpy
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
	{
		static CompositionContainer compositionContainer;
		
		public static CompositionContainer CompositionContainer {
			get { return compositionContainer; }
		}
		
		internal static CommandLineArguments CommandLineArguments;
		
		public App()
		{
			App.CommandLineArguments = new CommandLineArguments(Environment.GetCommandLineArgs().Skip(1));
			if (App.CommandLineArguments.SharedInstance) {
				string message = string.Join(Environment.NewLine, Environment.GetCommandLineArgs().Skip(1));
				if (SendToPreviousInstance("ILSpy:\r\n" + message)) {
					Environment.Exit(0);
				}
			}
			InitializeComponent();
			
			var catalog = new AggregateCatalog();
			catalog.Catalogs.Add(new AssemblyCatalog(typeof(App).Assembly));
			catalog.Catalogs.Add(new DirectoryCatalog(".", "*.Plugin.dll"));
			
			compositionContainer = new CompositionContainer(catalog);
			
			Languages.Initialize(compositionContainer);
			
			if (!Debugger.IsAttached) {
				AppDomain.CurrentDomain.UnhandledException += ShowErrorBox;
				Dispatcher.CurrentDispatcher.UnhandledException += Dispatcher_UnhandledException;
			}
			
			EventManager.RegisterClassHandler(typeof(Window),
			                                  Hyperlink.RequestNavigateEvent,
			                                  new RequestNavigateEventHandler(Window_RequestNavigate));
		}
		
		#region Exception Handling
		static void Dispatcher_UnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
		{
			Debug.WriteLine(e.Exception.ToString());
			MessageBox.Show(e.Exception.ToString(), "Sorry, we crashed");
			e.Handled = true;
		}
		
		static void ShowErrorBox(object sender, UnhandledExceptionEventArgs e)
		{
			Exception ex = e.ExceptionObject as Exception;
			if (ex != null) {
				Debug.WriteLine(ex.ToString());
				MessageBox.Show(ex.ToString(), "Sorry, we crashed");
			}
		}
		#endregion
		
		#region Pass Command Line Arguments to previous instance
		bool SendToPreviousInstance(string message)
		{
			bool success = false;
			NativeMethods.EnumWindows(
				(hWnd, lParam) => {
					string windowTitle = NativeMethods.GetWindowText(hWnd, 100);
					if (windowTitle.StartsWith("ILSpy", StringComparison.Ordinal)) {
						Debug.WriteLine("Found {0:x4}: {1}", hWnd, windowTitle);
						IntPtr result = Send(hWnd, message);
						Debug.WriteLine("WM_COPYDATA result: {0:x8}", result);
						if (result == (IntPtr)1) {
							NativeMethods.SetForegroundWindow(hWnd);
							success = true;
							return false; // stop enumeration
						}
					}
					return true; // continue enumeration
				}, IntPtr.Zero);
			return success;
		}
		
		unsafe static IntPtr Send(IntPtr hWnd, string message)
		{
			const uint SMTO_NORMAL = 0;
			
			CopyDataStruct lParam;
			lParam.Padding = IntPtr.Zero;
			lParam.Size = message.Length * 2;
			fixed (char *buffer = message) {
				lParam.Buffer = (IntPtr)buffer;
				IntPtr result;
				// SendMessage with 3s timeout (e.g. when the target process is stopped in the debugger)
				if (NativeMethods.SendMessageTimeout(
					hWnd, NativeMethods.WM_COPYDATA, IntPtr.Zero, ref lParam,
					SMTO_NORMAL, 3000, out result) != IntPtr.Zero)
				{
					return result;
				} else {
					return IntPtr.Zero;
				}
			}
		}
		#endregion
		
		void Window_RequestNavigate(object sender, RequestNavigateEventArgs e)
		{
			Process.Start(e.Uri.ToString());
		}
	}
}