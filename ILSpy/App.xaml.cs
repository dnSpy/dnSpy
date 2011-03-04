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
using System.ComponentModel.Composition.Hosting;
using System.Diagnostics;
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
		public App()
		{
			InitializeComponent();
			
			var catalog = new AggregateCatalog();
			catalog.Catalogs.Add(new AssemblyCatalog(typeof(App).Assembly));
			catalog.Catalogs.Add(new DirectoryCatalog(".", "*.Plugin.dll"));
			var container = new CompositionContainer(catalog);
			
			Languages.Initialize(container);
			
			if (!Debugger.IsAttached) {
				AppDomain.CurrentDomain.UnhandledException += ShowErrorBox;
				Dispatcher.CurrentDispatcher.UnhandledException += Dispatcher_UnhandledException;
			}
			
			EventManager.RegisterClassHandler(typeof(Window),
			                                  Hyperlink.RequestNavigateEvent,
			                                  new RequestNavigateEventHandler(Window_RequestNavigate));
		}
		
		
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
		
		void Window_RequestNavigate(object sender, RequestNavigateEventArgs e)
		{
			Process.Start(e.Uri.ToString());
		}
	}
}