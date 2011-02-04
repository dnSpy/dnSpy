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
using Mono.Cecil;

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
		sealed class GacEntry
		{
			readonly AssemblyNameReference r;
			readonly string fileName;
			
			public GacEntry(AssemblyNameReference r, string fileName)
			{
				this.r = r;
				this.fileName = fileName;
			}
			
			public string FullName {
				get { return r.FullName; }
			}
			
			public string ShortName {
				get { return r.Name; }
			}
			
			public string FileName {
				get { return fileName; }
			}
			
			public Version Version {
				get { return r.Version; }
			}
			
			public string Culture {
				get { return r.Culture; }
			}
			
			public string PublicKeyToken {
				get {
					StringBuilder s = new StringBuilder();
					foreach (byte b in r.PublicKeyToken)
						s.Append(b.ToString("x2"));
					return s.ToString();
				}
			}
			
			public override string ToString()
			{
				return r.FullName;
			}
		}
		
		void FetchGacContents()
		{
			HashSet<string> fullNames = new HashSet<string>();
			foreach (var r in GacInterop.GetGacAssemblyFullNames()) {
				if (cancelFetchThread)
					return;
				if (fullNames.Add(r.FullName)) { // filter duplicates
					var file = GacInterop.FindAssemblyInNetGac(r);
					if (file != null) {
						var entry = new GacEntry(r, file);
						Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action<GacEntry>(AddNewEntry), entry);
					}
				}
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
		
		public string[] SelectedFileNames {
			get {
				return listView.SelectedItems.OfType<GacEntry>().Select(e => e.FileName).ToArray();
			}
		}
	}
}