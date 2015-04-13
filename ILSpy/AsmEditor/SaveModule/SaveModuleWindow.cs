using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ICSharpCode.ILSpy.AsmEditor.SaveModule
{
	public class SaveModuleWindow : Window
	{
		public SaveModuleWindow()
		{
			SourceInitialized += (s, e) => this.HideMinimizeAndMaximizeButtons();
			Loaded += SaveMultiModule_Loaded;
		}

		void SaveMultiModule_Loaded(object sender, RoutedEventArgs e)
		{
			var data = (SaveMultiModuleVM)DataContext;
			data.OnSavedEvent += SaveMultiModuleVM_OnSavedEvent;
		}

		void SaveMultiModuleVM_OnSavedEvent(object sender, EventArgs e)
		{
			var data = (SaveMultiModuleVM)DataContext;
			if (!data.HasError)
				closeButton_Click(null, null);
		}

		protected void closeButton_Click(object sender, RoutedEventArgs e)
		{
			this.DialogResult = true;
			Close();
		}

		protected override void OnClosing(CancelEventArgs e)
		{
			base.OnClosing(e);

			var data = (SaveMultiModuleVM)DataContext;
			if (data.IsSaving) {
				var res = MessageBox.Show("Are you sure you want to cancel the save?", "dnSpy", MessageBoxButton.YesNo);
				if (res == MessageBoxResult.Yes)
					data.CancelSave();
				e.Cancel = true;
				return;
			}

			if (data.IsCanceling) {
				var res = MessageBox.Show("The save is being canceled.\nAre you sure you want to close the window?", "dnSpy", MessageBoxButton.YesNo);
				if (res == MessageBoxResult.No)
					e.Cancel = true;
				return;
			}
		}

		internal void ShowOptions(SaveModuleOptionsVM data)
		{
			if (data == null)
				return;

			var win = new SaveModuleOptions();
			win.Owner = this;
			var clone = data.Clone();
			win.DataContext = clone;
			var res = win.ShowDialog();
			if (res == true) {
				clone.CopyTo(data);
				((SaveMultiModuleVM)DataContext).OnModuleSettingsSaved();
			}
		}
	}
}
