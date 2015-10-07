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
using System.Xml.Linq;
using dnSpy.Search;
using dnSpy.TreeNodes;

namespace ICSharpCode.ILSpy
{
	/// <summary>
	/// Represents the filters applied to the tree view.
	/// </summary>
	/// <remarks>
	/// This class is mutable; but the ILSpyTreeNode filtering assumes that filter settings are immutable.
	/// Thus, the main window will use one mutable instance (for data-binding), and will assign a new
	/// clone to the ILSpyTreeNodes whenever the main mutable instance changes.
	/// </remarks>
	public class FilterSettings : INotifyPropertyChanged
	{
		internal FilterSettings(ITreeViewNodeFilter filter, Language language, bool showInternalApi)
		{
			this.filter = filter;
			this.origFilter = this.filter;
			InitializeFilter();
			this.ShowInternalApi = showInternalApi;
			this.Language = language ?? Languages.GetLanguage("C#");
		}

		public FilterSettings(XElement element)
		{
			this.filter = FilterNothingTreeViewNodeFilter.Instance;
			this.origFilter = this.filter;
			InitializeFilter();
			this.ShowInternalApi = (bool?)element.Element("ShowInternalApi") ?? true;
			this.Language = Languages.GetLanguage("C#");
		}
		
		public XElement SaveAsXml()
		{
			return new XElement(
				"FilterSettings",
				new XElement("ShowInternalApi", this.ShowInternalApi)
			);
		}

		internal ITreeViewNodeFilter Filter {
			get { return filter; }
		}
		ITreeViewNodeFilter filter;
		readonly ITreeViewNodeFilter origFilter;
		
		string searchTerm;
		
		/// <summary>
		/// Gets/Sets the search term.
		/// Only tree nodes containing the search term will be shown.
		/// </summary>
		public string SearchTerm {
			get { return searchTerm; }
			set {
				if (searchTerm != value) {
					searchTerm = value;
					OnPropertyChanged("SearchTerm");
				}
			}
		}
		
		/// <summary>
		/// Gets whether a node with the specified text is matched by the current search term.
		/// </summary>
		public bool SearchTermMatches(string text)
		{
			if (string.IsNullOrEmpty(searchTerm))
				return true;
			return text.IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase) >= 0;
		}
		
		bool showInternalApi;
		
		/// <summary>
		/// Gets/Sets whether internal API members should be shown.
		/// </summary>
		public bool ShowInternalApi {
			get { return showInternalApi; }
			set {
				if (showInternalApi != value) {
					showInternalApi = value;
					InitializeFilter();
					OnPropertyChanged("ShowInternalApi");
				}
			}
		}

		void InitializeFilter()
		{
			if (ShowInternalApi)
				filter = origFilter;
			else
				filter = new PublicApiTreeViewNodeFilter(origFilter, () => !ShowInternalApi);
		}
		
		Language language;
		
		/// <summary>
		/// Gets/Sets the current language.
		/// </summary>
		/// <remarks>
		/// While this isn't related to filtering, having it as part of the FilterSettings
		/// makes it easy to pass it down into all tree nodes.
		/// </remarks>
		public Language Language {
			get { return language; }
			set {
				if (language != value) {
					language = value;
					OnPropertyChanged("Language");
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
		
		public FilterSettings Clone()
		{
			FilterSettings f = (FilterSettings)MemberwiseClone();
			f.PropertyChanged = null;
			return f;
		}
	}
}
