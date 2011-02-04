// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace ICSharpCode.ILSpy
{
	/// <summary>
	/// Interaction logic for OpenFromGacDialog.xaml
	/// </summary>
	public partial class OpenFromGacDialog : Window
	{
		ObservableCollection<GacEntry> gacEntries = new ObservableCollection<GacEntry>();
		ObservableCollection<GacEntry> filteredEntries = new ObservableCollection<GacEntry>();
		volatile bool cancelFetchThread;
		
		public OpenFromGacDialog()
		{
			InitializeComponent();
			listView.ItemsSource = filteredEntries;
			SortableGridViewColumn.SetCurrentSortColumn(listView, nameColumn);
			SortableGridViewColumn.SetSortDirection(listView, ColumnSortDirection.Ascending);
			
			new Thread(new ThreadStart(FetchGacContents)).Start();
		}
		
		protected override void OnClosing(CancelEventArgs e)
		{
			base.OnClosing(e);
			cancelFetchThread = true;
		}
		
		#region Fetch Gac Contents
		sealed class GacEntry : IEquatable<GacEntry>
		{
			readonly string fullAssemblyName;
			readonly string shortName, culture, publicKeyToken;
			readonly Version version;
			
			public GacEntry(string fullAssemblyName)
			{
				this.fullAssemblyName = fullAssemblyName;
				string[] components = fullAssemblyName.Split(',');
				shortName = components[0];
				for (int i = 1; i < components.Length; i++) {
					string val = components[i].Trim();
					int pos = val.IndexOf('=');
					if (pos > 0) {
						switch (val.Substring(0, pos)) {
							case "Version":
								string versionText = val.Substring(pos + 1);
								Version.TryParse(versionText, out version);
								break;
							case "Culture":
								culture = val.Substring(pos + 1);
								break;
							case "PublicKeyToken":
								publicKeyToken = val.Substring(pos + 1);
								break;
						}
					}
				}
			}
			
			public string FullName {
				get { return fullAssemblyName; }
			}
			
			public string ShortName {
				get { return shortName; }
			}
			
			public Version Version {
				get { return version; }
			}
			
			public string Culture {
				get { return culture; }
			}
			
			public string PublicKeyToken {
				get { return publicKeyToken; }
			}
			
			public override string ToString()
			{
				return fullAssemblyName;
			}
			
			public override int GetHashCode()
			{
				return fullAssemblyName.GetHashCode();
			}
			
			public override bool Equals(object obj)
			{
				return Equals(obj as GacEntry);
			}
			
			public bool Equals(GacEntry o)
			{
				return o != null && fullAssemblyName == o.fullAssemblyName;
			}
		}
		
		IEnumerable<GacEntry> GetGacAssemblyFullNames()
		{
			IApplicationContext applicationContext = null;
			IAssemblyEnum assemblyEnum = null;
			IAssemblyName assemblyName = null;
			
			Fusion.CreateAssemblyEnum(out assemblyEnum, null, null, 2, 0);
			while (!cancelFetchThread && assemblyEnum.GetNextAssembly(out applicationContext, out assemblyName, 0) == 0) {
				uint nChars = 0;
				assemblyName.GetDisplayName(null, ref nChars, 0);
				
				StringBuilder name = new StringBuilder((int)nChars);
				assemblyName.GetDisplayName(name, ref nChars, 0);
				
				yield return new GacEntry(name.ToString());
			}
		}
		
		void FetchGacContents()
		{
			foreach (var entry in GetGacAssemblyFullNames().Distinct()) {
				Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action<GacEntry>(AddNewEntry), entry);
			}
		}
		
		void AddNewEntry(GacEntry entry)
		{
			gacEntries.Add(entry);
			string filter = filterTextBox.Text;
			if (string.IsNullOrEmpty(filter) || entry.ShortName.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0)
				filteredEntries.Add(entry);
		}
		#endregion
		
		void FilterTextBox_TextChanged(object sender, TextChangedEventArgs e)
		{
			string filter = filterTextBox.Text;
			filteredEntries.Clear();
			foreach (GacEntry entry in gacEntries) {
				if (string.IsNullOrEmpty(filter) || entry.ShortName.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0)
					filteredEntries.Add(entry);
			}
		}
		
		void ListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			okButton.IsEnabled = listView.SelectedItems.Count > 0;
		}
		
		void OKButton_Click(object sender, RoutedEventArgs e)
		{
			this.DialogResult = true;
			Close();
		}
		
		public string[] SelectedFullNames {
			get {
				return listView.SelectedItems.OfType<GacEntry>().Select(e => e.FullName).ToArray();
			}
		}
	}
}