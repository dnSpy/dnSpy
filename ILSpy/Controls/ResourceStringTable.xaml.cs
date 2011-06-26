/*
 * Created by SharpDevelop.
 * User: Ronny Klier
 * Date: 31.05.2011
 * Time: 00:13
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace ICSharpCode.ILSpy.Controls
{
	/// <summary>
	/// Interaction logic for ResourceStringTable.xaml
	/// </summary>
	public partial class ResourceStringTable : UserControl
	{
		public ResourceStringTable(IEnumerable strings)
		{
			InitializeComponent();
			// set size to fit decompiler window
			// TODO: there should be a more transparent way to do this
			MaxWidth = MainWindow.Instance.mainPane.ActualWidth-20;
			MaxHeight = MainWindow.Instance.mainPane.ActualHeight-100;
			resourceListView.ItemsSource = strings;
		}
		
		void ExecuteCopy(object sender, ExecutedRoutedEventArgs args)
		{
		  StringBuilder sb = new StringBuilder();
		  foreach (var item in resourceListView.SelectedItems)
		  {
		    sb.AppendLine(item.ToString());
		  }
		  Clipboard.SetText(sb.ToString());
		}
		
		void CanExecuteCopy(object sender, CanExecuteRoutedEventArgs args)
		{
			args.CanExecute = true;
		}
	}
}