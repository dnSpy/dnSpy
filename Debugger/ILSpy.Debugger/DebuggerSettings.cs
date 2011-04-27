// Copyright (c) 2011 AlphaSierraPapa for the SharpDevelop Team
// This code is distributed under MIT license (for details please see \doc\license.txt)

using System;
using System.ComponentModel;

namespace ICSharpCode.ILSpy.Debugger
{
	public class DebuggerSettings : INotifyPropertyChanged
	{
		bool showWarnings = true;
		bool debugWholeTypesOnly = false;
		
		/// <summary>
		/// Show warnings messages.
		/// <remarks>Default value is true.</remarks>
		/// </summary>
		[DefaultValue(true)]
		public bool ShowWarnings {
			get { return showWarnings; }
			set {
				if (showWarnings != value) {
					showWarnings = value;
					OnPropertyChanged("ShowWarnings");
				}
			}
		}
		
		/// <summary>
		/// True, if debug only whole types; otherwise false (debug only methods and properties).
		/// <remarks>Default value is false.</remarks>
		/// </summary>
		[DefaultValue(false)]
		public bool DebugWholeTypesOnly {
			get { return debugWholeTypesOnly; }
			set {
				if (debugWholeTypesOnly != value) {
					debugWholeTypesOnly = value;
					OnPropertyChanged("DebugWholeTypesOnly");
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
