// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Windows;

namespace ICSharpCode.ILSpy.AvalonEdit
{
	public interface ITextEditorListener : IWeakEventListener
	{
		new bool ReceiveWeakEvent(Type managerType, object sender, EventArgs e);
		void ClosePopup();
	}
}
