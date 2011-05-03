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
