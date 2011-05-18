// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.ComponentModel;
using System.Windows.Media;

namespace ICSharpCode.ILSpy
{
	/// <summary>
	/// Description of DisplaySettings.
	/// </summary>
	public class DisplaySettings : INotifyPropertyChanged
	{
		public DisplaySettings()
		{
		}
		
		#region INotifyPropertyChanged implementation
		public event PropertyChangedEventHandler PropertyChanged;
		
		protected virtual void OnPropertyChanged(PropertyChangedEventArgs e)
		{
			if (PropertyChanged != null) {
				PropertyChanged(this, e);
			}
		}
		
		protected void OnPropertyChanged(string propertyName)
		{
			OnPropertyChanged(new PropertyChangedEventArgs(propertyName));
		}
		#endregion
		
		FontFamily selectedFont;
		
		public FontFamily SelectedFont {
			get { return selectedFont; }
			set {
				if (selectedFont != value) {
					selectedFont = value;
					OnPropertyChanged("SelectedFont");
				}
			}
		}
		
		double selectedFontSize;
		
		public double SelectedFontSize {
			get { return selectedFontSize; }
			set {
				if (selectedFontSize != value) {
					selectedFontSize = value;
					OnPropertyChanged("SelectedFontSize");
				}
			}
		}
		
		public void CopyValues(DisplaySettings s)
		{
			this.SelectedFont = s.selectedFont;
			this.SelectedFontSize = s.selectedFontSize;
		}
	}
}
