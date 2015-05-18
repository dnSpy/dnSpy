// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
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
	[Export(typeof(IPaneCreator))]
	public class BreakpointPanelCreator : IPaneCreator
	{
		public IPane Create(string name)
		{
			if (name == BreakpointPanel.Instance.PaneName)
				return BreakpointPanel.Instance;
			return null;
		}
	}

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

		public string PaneName {
			get { return "breakpoint window"; }
		}

		public string PaneTitle {
			get { return "Breakpoints"; }
		}
        
        private BreakpointPanel()
        {
			InitializeComponent();
			dntheme.Themes.ThemeChanged += (s, e) => {
				foreach (BreakpointBookmarkVM bmvm in view.Items)
					bmvm.OnThemeChanged();
			};
        }
        
		public void Show()
		{
			if (!IsVisible)
				MainWindow.Instance.ShowInBottomPane(PaneTitle, this);
		}

		public void Opened()
		{
			SetItemSource();

			foreach (var m in BookmarkManager.Bookmarks) {
				var bpm = m as BreakpointBookmark;
				if (bpm != null)
					bpm.OnModified += BreakpointBookmark_OnModified;
			}
			BookmarkManager.Added += BookmarkManager_Added;
			BookmarkManager.Removed += BookmarkManager_Removed;
			DebuggerSettings.Instance.PropertyChanged += DebuggerSettings_PropertyChanged;
		}

		void DebuggerSettings_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == "ShowAllBookmarks")
				SetItemSource();
		}

		void BookmarkManager_Added(object sender, BookmarkEventArgs e)
		{
			var bpm = e.Bookmark as BreakpointBookmark;
			if (bpm == null)
				return;
			bpm.OnModified += BreakpointBookmark_OnModified;
			SetItemSource();
		}

		void BookmarkManager_Removed(object sender, BookmarkEventArgs e)
		{
			var bpm = e.Bookmark as BreakpointBookmark;
			if (bpm == null)
				return;
			bpm.OnModified += BreakpointBookmark_OnModified;
			SetItemSource();
		}

		void BreakpointBookmark_OnModified(object sender, EventArgs e)
		{
			SetItemSource();
		}

		sealed class BreakpointBookmarkVM : INotifyPropertyChanged
		{
			readonly BreakpointBookmark bpm;

			public BreakpointBookmark BreakpointBookmark {
				get { return bpm; }
			}

			public event PropertyChangedEventHandler PropertyChanged;

			internal void OnThemeChanged()
			{
				OnPropertyChanged("Image");
			}

			public ImageSource Image {
				get { return bpm.GetImage(Images.GetColor(BackgroundType.GridViewItem)); }
			}

			public bool CanToggle {
				get { return bpm.CanToggle; }
			}

			public bool IsEnabled {
				get { return bpm.IsEnabled; }
				set {
					if (bpm.IsEnabled != value) {
						bpm.IsEnabled = value;
						OnPropertyChanged("IsEnabled");
					}
				}
			}

			public string DeclaringTypeFullName {
				get { return bpm.MemberReference.DeclaringType.FullName; }
			}

			public string Name {
				get { return bpm.MemberReference.Name; }
			}

			public int LineNumber {
				get { return bpm.LineNumber; }
			}

			public string ILRangeFromString {
				get { return string.Format("0x{0:X4}", bpm.ILRange.From); }
			}

			public string MDTokenString {
				get { return string.Format("0x{0:X8}", bpm.MemberReference.MDToken.Raw); }
			}

			public string AssemblyFullName {
				get { return bpm.MemberReference.Module.Assembly.FullName; }
			}

			public string ModuleFullName {
				get { return bpm.MemberReference.Module.FullName; }
			}

			public BreakpointBookmarkVM(BreakpointBookmark bpm)
			{
				this.bpm = bpm;
			}

			void OnPropertyChanged(string propName)
			{
				if (PropertyChanged != null)
					PropertyChanged(this, new PropertyChangedEventArgs(propName));
			}
		}
		
		private void SetItemSource()
		{
          	view.ItemsSource = null;
			view.ItemsSource = BookmarkManager.Bookmarks.Where(b => b is BreakpointBookmark).Select(b => new BreakpointBookmarkVM((BreakpointBookmark)b));
		}
        
        public void Closed()
        {
			BookmarkManager.Added -= BookmarkManager_Added;
			BookmarkManager.Removed -= BookmarkManager_Removed;
			DebuggerSettings.Instance.PropertyChanged -= DebuggerSettings_PropertyChanged;
			foreach (var m in BookmarkManager.Bookmarks) {
				var bpm = m as BreakpointBookmark;
				if (bpm != null)
					bpm.OnModified -= BreakpointBookmark_OnModified;
			}
        }

		void view_MouseDoubleClick(object sender, MouseButtonEventArgs e)
		{
			if (!UIUtils.IsLeftDoubleClick<ListViewItem>(view, e))
				return;
			if (view.SelectedItems.Count > 0)
				GoToBookmark(((BreakpointBookmarkVM)view.SelectedItems[0]).BreakpointBookmark);
			e.Handled = true;
		}

		void GoToBookmark(BookmarkBase bm)
		{
            if (null == bm)
                return;
			var textView = MainWindow.Instance.SafeActiveTextView;
			if (DebugUtils.JumpToReference(textView, bm.MemberReference, () => bm.GetLocation(textView)))
				MainWindow.Instance.SetTextEditorFocus(textView);
		}
        
        void view_KeyDown(object sender, KeyEventArgs e)
        {
			if (Keyboard.Modifiers == ModifierKeys.None && e.Key == Key.Delete) {
				foreach (var bmvm in view.SelectedItems.OfType<BreakpointBookmarkVM>().ToArray())
					BookmarkManager.RemoveMark(bmvm.BreakpointBookmark);
				e.Handled = true;
				return;
			}
			if (Keyboard.Modifiers == ModifierKeys.None && e.Key == Key.Enter) {
				if (view.SelectedItems.Count > 0)
					GoToBookmark(((BreakpointBookmarkVM)view.SelectedItems[0]).BreakpointBookmark);
				e.Handled = true;
				return;
			}
		}
    }

	[ExportMainMenuCommand(MenuIcon = "BreakpointsWindow",
						   Menu = "_Debug",
						   MenuHeader = "Show _Breakpoints",
						   MenuCategory = "Breakpoints",
						   MenuOrder = 5320)]
    public class BookmarkManagerPanelCommand : SimpleCommand
    {
        public override void Execute(object parameter)
        {
            BreakpointPanel.Instance.Show();
        }

		public override bool CanExecute(object parameter)
		{
			return MainWindow.Instance.BottomPaneContent != BreakpointPanel.Instance;
		}
    }
}