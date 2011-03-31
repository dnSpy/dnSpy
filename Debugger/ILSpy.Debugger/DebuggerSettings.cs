// Copyright (c) 2011 AlphaSierraPapa for the SharpDevelop Team
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.ComponentModel;

namespace ILSpy.Debugger
{
	public class DebuggerSettings : INotifyPropertyChanged
	{
		bool showWarnings = true;
		
		/// <summary>
		/// Show warnings messages.
		/// </summary>
		public bool ShowWarnings {
			get { return showWarnings; }
			set {
				if (showWarnings != value) {
					showWarnings = value;
					OnPropertyChanged("ShowWarnings");
				}
			}
		}
		
		public event PropertyChangedEventHandler PropertyChanged;
		
		protected virtual void OnPropertyChanged(string propertyName)
		{
			if (PropertyChanged != null) {
				PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
			}
		}
	}
}
