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

using System.Windows;
using System.Windows.Controls;
using Mono.Cecil;
using System.Windows.Input;

namespace ICSharpCode.ILSpy
{
	/// <summary>
	/// Interaction logic for OpenListDialog.xaml
	/// </summary>
	public partial class OpenListDialog : Window
	{

		public const string DotNet4List = ".NET 4 (WPF)";
		public const string DotNet35List = ".NET 3.5";
		public const string ASPDotNetMVC3List = "ASP.NET (MVC3)";

		readonly AssemblyListManager manager;

		public OpenListDialog()
		{
			InitializeComponent();
			manager = MainWindow.Instance.assemblyListManager;
		}

		private void listView_Loaded(object sender, RoutedEventArgs e)
		{
			listView.ItemsSource = manager.AssemblyLists;
			CreateDefaultAssemblyLists();
		}

		void ListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			okButton.IsEnabled = listView.SelectedItem != null;
			removeButton.IsEnabled = listView.SelectedItem != null;
		}

		void OKButton_Click(object sender, RoutedEventArgs e)
		{
			this.DialogResult = true;
		}

		public string SelectedListName
		{
			get
			{
				return listView.SelectedItem.ToString();
			}
		}

		private void CreateDefaultAssemblyLists()
		{
			if (!manager.AssemblyLists.Contains(DotNet4List))
			{
				AssemblyList dotnet4 = new AssemblyList(DotNet4List);
				AddToList(dotnet4, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089");
				AddToList(dotnet4, "System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089");
				AddToList(dotnet4, "System.Core, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089");
				AddToList(dotnet4, "System.Data, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089");
				AddToList(dotnet4, "System.Data.DataSetExtensions, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089");
				AddToList(dotnet4, "System.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089");
				AddToList(dotnet4, "System.Xml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089");
				AddToList(dotnet4, "System.Xml.Linq, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089");
				AddToList(dotnet4, "Microsoft.CSharp, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a");
				AddToList(dotnet4, "PresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35");
				AddToList(dotnet4, "PresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35");
				AddToList(dotnet4, "WindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35");

				if (dotnet4.assemblies.Count > 0)
				{
					manager.CreateList(dotnet4);
				}
			}

			if (!manager.AssemblyLists.Contains(DotNet35List))
			{
				AssemblyList dotnet35 = new AssemblyList(DotNet35List);
				AddToList(dotnet35, "mscorlib, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089");
				AddToList(dotnet35, "System, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089");
				AddToList(dotnet35, "System.Core, Version=3.5.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089");
				AddToList(dotnet35, "System.Data, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089");
				AddToList(dotnet35, "System.Data.DataSetExtensions, Version=3.5.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089");
				AddToList(dotnet35, "System.Xml, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089");
				AddToList(dotnet35, "System.Xml.Linq, Version=3.5.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089");
				AddToList(dotnet35, "PresentationCore, Version=3.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35");
				AddToList(dotnet35, "PresentationFramework, Version=3.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35");
				AddToList(dotnet35, "WindowsBase, Version=3.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35");

				if (dotnet35.assemblies.Count > 0)
				{
					manager.CreateList(dotnet35);
				}
			}

			if (!manager.AssemblyLists.Contains(ASPDotNetMVC3List))
			{
				AssemblyList mvc = new AssemblyList(ASPDotNetMVC3List);
				AddToList(mvc, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089");
				AddToList(mvc, "System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089");
				AddToList(mvc, "System.ComponentModel.DataAnnotations, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35");
				AddToList(mvc, "System.Configuration, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a");
				AddToList(mvc, "System.Core, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089");
				AddToList(mvc, "System.Data, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089");
				AddToList(mvc, "System.Data.DataSetExtensions, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089");
				AddToList(mvc, "System.Data.Entity, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089");
				AddToList(mvc, "System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a");
				AddToList(mvc, "System.EnterpriseServices, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a");
				AddToList(mvc, "System.Web, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a");
				AddToList(mvc, "System.Web.Abstractions, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35");
				AddToList(mvc, "System.Web.ApplicationServices, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35");
				AddToList(mvc, "System.Web.DynamicData, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35");
				AddToList(mvc, "System.Web.Entity, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089");
				AddToList(mvc, "System.Web.Extensions, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35");
				AddToList(mvc, "System.Web.Mvc, Version=3.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35");
				AddToList(mvc, "System.Web.Routing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35");
				AddToList(mvc, "System.Web.Services, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a");
				AddToList(mvc, "System.Web.WebPages, Version=1.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35");
				AddToList(mvc, "System.Web.Helpers, Version=1.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35");
				AddToList(mvc, "System.Xml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089");
				AddToList(mvc, "System.Xml.Linq, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089");
				AddToList(mvc, "Microsoft.CSharp, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a");

				if (mvc.assemblies.Count > 0)
				{
					manager.CreateList(mvc);
				}
			}
		}

		private void AddToList(AssemblyList list, string FullName)
		{
			AssemblyNameReference reference = AssemblyNameReference.Parse(FullName);
			string file = GacInterop.FindAssemblyInNetGac(reference);
			if (file != null)
				list.OpenAssembly(file);
		}

		private void CreateButton_Click(object sender, RoutedEventArgs e)
		{
			CreateListDialog dlg = new CreateListDialog();
			dlg.Owner = this;
			dlg.Closing += (s, args) =>
			{
				if (dlg.DialogResult == true)
				{
					if (manager.AssemblyLists.Contains(dlg.NewListName))
					{
						args.Cancel = true;
						MessageBox.Show("A list with the same name was found.", null, MessageBoxButton.OK);
					}
				}
			};
			if (dlg.ShowDialog() == true)
			{
				manager.CreateList(new AssemblyList(dlg.NewListName));
			}

		}

		private void RemoveButton_Click(object sender, RoutedEventArgs e)
		{
			if (listView.SelectedItem != null)
				manager.DeleteList(listView.SelectedItem.ToString());
		}

    private void listView_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
      if (e.ChangedButton == MouseButton.Left && listView.SelectedItem != null)
        this.DialogResult = true;
    }

	}
}
