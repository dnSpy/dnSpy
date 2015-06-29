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
using System.Windows.Media;

namespace ICSharpCode.ILSpy.Options
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
					const int min = 1;
					const int max = 200;
					if (selectedFontSize > max)
						selectedFontSize = max;
					if (selectedFontSize < min)
						selectedFontSize = min;
					OnPropertyChanged("SelectedFontSize");
				}
			}
		}
		
		bool showLineNumbers;
		
		public bool ShowLineNumbers {
			get { return showLineNumbers; }
			set {
				if (showLineNumbers != value) {
					showLineNumbers = value;
					OnPropertyChanged("ShowLineNumbers");
				}
			}
		}

		bool showMetadataTokens;

		public bool ShowMetadataTokens
		{
			get { return showMetadataTokens; }
			set {
				if (showMetadataTokens != value) {
					showMetadataTokens = value;
					OnPropertyChanged("ShowMetadataTokens");
				}
			}
		}

		bool showAssemblyVersion;

		public bool ShowAssemblyVersion
		{
			get { return showAssemblyVersion; }
			set {
				if (showAssemblyVersion != value) {
					showAssemblyVersion = value;
					OnPropertyChanged("ShowAssemblyVersion");
				}
			}
		}

		bool showAssemblyPublicKeyToken;

		public bool ShowAssemblyPublicKeyToken
		{
			get { return showAssemblyPublicKeyToken; }
			set {
				if (showAssemblyPublicKeyToken != value) {
					showAssemblyPublicKeyToken = value;
					OnPropertyChanged("ShowAssemblyPublicKeyToken");
				}
			}
		}

		bool decompileFullType;

		public bool DecompileFullType
		{
			get { return decompileFullType; }
			set {
				if (decompileFullType != value) {
					decompileFullType = value;
					OnPropertyChanged("DecompileFullType");
				}
			}
		}

		bool newEmptyTabs;

		public bool NewEmptyTabs
		{
			get { return newEmptyTabs; }
			set {
				if (newEmptyTabs != value) {
					newEmptyTabs = value;
					OnPropertyChanged("NewEmptyTabs");
				}
			}
		}

		bool restoreTabsAtStartup;

		public bool RestoreTabsAtStartup
		{
			get { return restoreTabsAtStartup; }
			set {
				if (restoreTabsAtStartup != value) {
					restoreTabsAtStartup = value;
					OnPropertyChanged("RestoreTabsAtStartup");
				}
			}
		}

		bool autoHighlightRefs;

		public bool AutoHighlightRefs
		{
			get { return autoHighlightRefs; }
			set {
				if (autoHighlightRefs != value) {
					autoHighlightRefs = value;
					OnPropertyChanged("AutoHighlightRefs");
				}
			}
		}

		bool syntaxHighlightTreeViewUI;

		public bool SyntaxHighlightTreeViewUI {
			get { return syntaxHighlightTreeViewUI; }
			set {
				if (syntaxHighlightTreeViewUI != value) {
					syntaxHighlightTreeViewUI = value;
					OnPropertyChanged("SyntaxHighlightTreeViewUI");
				}
			}
		}

		bool syntaxHighlightAnalyzerTreeViewUI;

		public bool SyntaxHighlightAnalyzerTreeViewUI {
			get { return syntaxHighlightAnalyzerTreeViewUI; }
			set {
				if (syntaxHighlightAnalyzerTreeViewUI != value) {
					syntaxHighlightAnalyzerTreeViewUI = value;
					OnPropertyChanged("SyntaxHighlightAnalyzerTreeViewUI");
				}
			}
		}

		bool syntaxHighlightSearchListUI;

		public bool SyntaxHighlightSearchListUI {
			get { return syntaxHighlightSearchListUI; }
			set {
				if (syntaxHighlightSearchListUI != value) {
					syntaxHighlightSearchListUI = value;
					OnPropertyChanged("SyntaxHighlightSearchListUI");
				}
			}
		}

		public void CopyValues(DisplaySettings s)
		{
			this.SelectedFont = s.selectedFont;
			this.SelectedFontSize = s.selectedFontSize;
			this.ShowLineNumbers = s.showLineNumbers;
			this.ShowMetadataTokens = s.showMetadataTokens;
			this.ShowAssemblyVersion = s.showAssemblyVersion;
			this.ShowAssemblyPublicKeyToken = s.showAssemblyPublicKeyToken;
			this.DecompileFullType = s.decompileFullType;
			this.NewEmptyTabs = s.newEmptyTabs;
			this.RestoreTabsAtStartup = s.restoreTabsAtStartup;
			this.AutoHighlightRefs = s.autoHighlightRefs;
			this.SyntaxHighlightTreeViewUI = s.syntaxHighlightTreeViewUI;
			this.SyntaxHighlightAnalyzerTreeViewUI = s.syntaxHighlightAnalyzerTreeViewUI;
			this.SyntaxHighlightSearchListUI = s.syntaxHighlightSearchListUI;
		}
	}
}
