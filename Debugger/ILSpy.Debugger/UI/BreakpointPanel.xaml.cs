// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using ICSharpCode.ILSpy;
using ICSharpCode.ILSpy.Bookmarks;

namespace ILSpyPlugin
{
    /// <summary>
    /// Interaction logic for BreakpointPanel.xaml
    /// </summary>
    public partial class BreakpointPanel : UserControl, IPane
    {
        static BreakpointPanel s_instance;
        
        public static BreakpointPanel Instance
        {
            get {
                if (null == s_instance)
                {
					App.Current.VerifyAccess();
                    s_instance = new BreakpointPanel();
                }
                return s_instance;
            }
        }
        
        private BreakpointPanel()
        {
          InitializeComponent();
          view.ItemsSource = BookmarkManager.Bookmarks;
          BookmarkManager.Added += new BookmarkEventHandler(OnAdded);
          BookmarkManager.Removed += new BookmarkEventHandler(OnRemoved);
        }
        
		public void Show()
		{
			if (!IsVisible)
				MainWindow.Instance.ShowInBottomPane("Breakpoints", this);
		}
        
        private void OnAdded(object sender, BookmarkEventArgs e)
        {
          view.ItemsSource = null;
          view.ItemsSource = BookmarkManager.Bookmarks;
        }
        private void OnRemoved(object sender, BookmarkEventArgs e)
        {
          view.ItemsSource = null;
          view.ItemsSource = BookmarkManager.Bookmarks;
        }
    
		
        public void Closed()
        {
        }
        
		void view_MouseDoubleClick(object sender, MouseButtonEventArgs e)
		{
            if (MouseButton.Left != e.ChangedButton)
                return;
            var selectedItem = view.SelectedItem as BookmarkBase;
            if (null == selectedItem)
                return;
            // TODO: Line should be part of jump target
            MainWindow.Instance.JumpToReference(selectedItem.MemberReference);
            MainWindow.Instance.TextView.UnfoldAndScroll(selectedItem.LineNumber);
            e.Handled = true;
		}
        
        void view_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Delete)
                return;
            var selectedItem = view.SelectedItem as BookmarkBase;
            if (null == selectedItem)
                return;
            BookmarkManager.RemoveMark(selectedItem);
            e.Handled = true;
        }
    }

    [ExportMainMenuCommand(Menu="_Debugger", Header="Show _Breakpoints", MenuCategory="Others", MenuOrder=8)]
    public class BookmarkManagerPanelCommand : SimpleCommand
    {
        public override void Execute(object parameter)
        {
            BreakpointPanel.Instance.Show();
        }
    }
}