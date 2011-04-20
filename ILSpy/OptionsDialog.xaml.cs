// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Linq;

namespace ICSharpCode.ILSpy
{
	/// <summary>
	/// Interaction logic for OptionsDialog.xaml
	/// </summary>
	public partial class OptionsDialog : Window
	{
		[ImportMany("OptionPages", typeof(UIElement), RequiredCreationPolicy = CreationPolicy.NonShared)]
		Lazy<UIElement, IOptionsMetadata>[] optionPages = null;
		
		public OptionsDialog()
		{
			InitializeComponent();
			App.CompositionContainer.ComposeParts(this);
			ILSpySettings settings = ILSpySettings.Load();
			foreach (var optionPage in optionPages) {
				TabItem tabItem = new TabItem();
				tabItem.Header = optionPage.Metadata.Title;
				tabItem.Content = optionPage.Value;
				tabControl.Items.Add(tabItem);
				
				IOptionPage page = optionPage.Value as IOptionPage;
				if (page != null)
					page.Load(settings);
			}
		}
		
		void OKButton_Click(object sender, RoutedEventArgs e)
		{
			ILSpySettings.Update(
				delegate (XElement root) {
					foreach (var optionPage in optionPages) {
						IOptionPage page = optionPage.Value as IOptionPage;
						if (page != null)
							page.Save(root);
					}
				});
			this.DialogResult = true;
			Close();
		}
	}
	
	public interface IOptionsMetadata
	{
		string Title { get; }
	}
	
	public interface IOptionPage
	{
		void Load(ILSpySettings settings);
		void Save(XElement root);
	}
	
	[MetadataAttribute]
	[AttributeUsage(AttributeTargets.Class, AllowMultiple=false)]
	public class ExportOptionPageAttribute : ExportAttribute
	{
		public ExportOptionPageAttribute(string title)
			: base("OptionPages", typeof(UIElement))
		{
			this.Title = title;
		}
		
		public string Title { get; private set; }
	}
	
	[ExportMainMenuCommand(Menu = "_View", Header = "_Options", MenuCategory = "Options", MenuOrder = 999)]
	sealed class ShowOptionsCommand : SimpleCommand
	{
		public override void Execute(object parameter)
		{
			OptionsDialog dlg = new OptionsDialog();
			dlg.Owner = MainWindow.Instance;
			if (dlg.ShowDialog() == true) {
				new RefreshCommand().Execute(parameter);
			}
		}
	}
}