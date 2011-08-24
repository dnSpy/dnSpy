// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)
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

using ICSharpCode.ILSpy;
using ICSharpCode.ILSpy.Bookmarks;
using ICSharpCode.ILSpy.Debugger.Bookmarks;
using ICSharpCode.ILSpy.Options;

namespace ICSharpCode.ILSpy.Debugger.UI
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
        }
        
		public void Show()
		{
			if (!IsVisible)
			{
                SetItemSource();
                
			    MainWindow.Instance.ShowInBottomPane("Breakpoints", this);
			    
                BookmarkManager.Added += delegate { SetItemSource(); };
                BookmarkManager.Removed += delegate { SetItemSource(); };
                DebuggerSettings.Instance.PropertyChanged += 
                	delegate(object s, PropertyChangedEventArgs e) { if (e.PropertyName == "ShowAllBookmarks") SetItemSource(); };
			}
		}
		
		private void SetItemSource()
		{
          	view.ItemsSource = null;
          	if (DebuggerSettings.Instance.ShowAllBookmarks)
				view.ItemsSource = BookmarkManager.Bookmarks;
          	else
          		view.ItemsSource = BookmarkManager.Bookmarks.Where(b => b is BreakpointBookmark);
		}
        
        public void Closed()
        {
        	BookmarkManager.Added -= delegate { SetItemSource(); };
        	BookmarkManager.Removed -= delegate { SetItemSource(); };
        	DebuggerSettings.Instance.PropertyChanged -= 
        		delegate(object s, PropertyChangedEventArgs e) { if (e.PropertyName == "ShowAllBookmarks") SetItemSource(); };
        }
        
		void view_MouseDoubleClick(object sender, MouseButtonEventArgs e)
		{
            if (MouseButton.Left != e.ChangedButton)
                return;
            var selectedItem = view.SelectedItem as BookmarkBase;
            if (null == selectedItem)
                return;
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

    [ExportMainMenuCommand(Menu="_Debugger", Header="Show _Breakpoints", MenuCategory="View", MenuOrder=8)]
    public class BookmarkManagerPanelCommand : SimpleCommand
    {
        public override void Execute(object parameter)
        {
            BreakpointPanel.Instance.Show();
        }
    }
}