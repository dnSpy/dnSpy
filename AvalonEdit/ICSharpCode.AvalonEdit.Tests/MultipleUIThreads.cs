// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Threading;
using System.Windows;
using NUnit.Framework;

namespace ICSharpCode.AvalonEdit
{
	[TestFixture]
	public class MultipleUIThreads
	{
		Exception error;
		
		[Test]
		public void CreateEditorInstancesOnMultipleUIThreads()
		{
			Thread t1 = new Thread(new ThreadStart(Run));
			Thread t2 = new Thread(new ThreadStart(Run));
			t1.SetApartmentState(ApartmentState.STA);
			t2.SetApartmentState(ApartmentState.STA);
			t1.Start();
			t2.Start();
			t1.Join();
			t2.Join();
			if (error != null)
				throw new InvalidOperationException(error.Message, error);
		}
		
		[STAThread]
		void Run()
		{
			try {
				var window = new Window();
				window.Content = new TextEditor();
				window.ShowActivated = false;
				window.Show();
			} catch (Exception ex) {
				error = ex;
			}
		}
	}
}
