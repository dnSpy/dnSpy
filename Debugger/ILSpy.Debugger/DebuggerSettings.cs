// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.ComponentModel;

namespace ICSharpCode.ILSpy.Debugger
{
	public class DebuggerSettings : INotifyPropertyChanged
	{
		bool showWarnings = true;
		bool askArguments = true;
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
		/// Ask for arguments and working directory before executing a process.
		/// </summary>
		[DefaultValue(true)]
		public bool AskForArguments {
			get { return askArguments; }
			set {
				if (askArguments != value) {
					askArguments = value;
					OnPropertyChanged("AskForArguments");
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
