// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

using Debugger;
using ICSharpCode.ILSpy;
using ICSharpCode.ILSpy.Debugger.Models.TreeModel;
using ICSharpCode.ILSpy.Debugger.Services;
using ICSharpCode.ILSpy.XmlDoc;
using Mono.Cecil;

namespace ILSpyPlugin
{
    /// <summary>
    /// Interaction logic for CallStackPanel.xaml
    /// </summary>
    public partial class CallStackPanel : UserControl
    {
        static CallStackPanel s_instance;
    	IDebugger m_currentDebugger;
    	Process debuggedProcess;
    	
    	public static CallStackPanel Instance
    	{
    	    get {
    	        if (null == s_instance)
    	        {
					App.Current.VerifyAccess();
    	            s_instance = new CallStackPanel();
    	        }
    	        return s_instance;
    	    }
    	}
    	
        private CallStackPanel()
        {
            InitializeComponent();
            
            DebuggerService.DebugStarted += new EventHandler(OnDebugStarted);
            DebuggerService.DebugStopped += new EventHandler(OnDebugStopped);
            if (DebuggerService.IsDebuggerStarted)
            	OnDebugStarted(null, EventArgs.Empty);
        }
  
		public void Show()
		{
			if (!IsVisible)
				MainWindow.Instance.ShowInBottomPane("Callstack", this);
		}
        
        void OnDebugStarted(object sender, EventArgs args)
        {
        	m_currentDebugger = DebuggerService.CurrentDebugger;
        	m_currentDebugger.IsProcessRunningChanged += new EventHandler(OnProcessRunningChanged);
        	debuggedProcess = ((WindowsDebugger)m_currentDebugger).DebuggedProcess;
        	OnProcessRunningChanged(null, EventArgs.Empty);
        }

        void OnDebugStopped(object sender, EventArgs args)
        {        	
        	m_currentDebugger.IsProcessRunningChanged -= new EventHandler(OnProcessRunningChanged);
        	m_currentDebugger = null;
        	debuggedProcess = null;
        }
        
        void OnProcessRunningChanged(object sender, EventArgs args)
        {
        	if (m_currentDebugger.IsProcessRunning)
        		return;
        	RefreshPad();
        }
        
       	void RefreshPad()
		{
			if (debuggedProcess == null || debuggedProcess.IsRunning || debuggedProcess.SelectedThread == null) {
				view.ItemsSource = null;
				return;
			}
			
			List<CallStackItem> items = null;
			StackFrame activeFrame = null;
			try {
				Utils.DoEvents(debuggedProcess);
				items = CreateItems().ToList();
				activeFrame = debuggedProcess.SelectedThread.SelectedStackFrame;
			} catch(AbortedBecauseDebuggeeResumedException) {
			} catch(System.Exception) {
				if (debuggedProcess == null || debuggedProcess.HasExited) {
					// Process unexpectedly exited
				} else {
					throw;
				}
			}
			view.ItemsSource = items;
			view.SelectedItem = items != null ? items.FirstOrDefault(item => object.Equals(activeFrame, item.Frame)) : null;
		}
		
		IEnumerable<CallStackItem> CreateItems()
		{
			foreach (StackFrame frame in debuggedProcess.SelectedThread.GetCallstack(100)) {
				CallStackItem item;
				
				// show modules names
				string moduleName = frame.MethodInfo.DebugModule.ToString();
				
    			item = new CallStackItem() {
					Name = GetFullName(frame), ModuleName = moduleName
				};
				item.Frame = frame;
				yield return item;
				Utils.DoEvents(debuggedProcess);
			}
		}
		
		internal static string GetFullName(StackFrame frame)
		{
			// disabled by default, my be switched if options / context menu is added
			bool showArgumentNames = false;
			bool showArgumentValues = false;

			StringBuilder name = new StringBuilder();
			name.Append(frame.MethodInfo.DeclaringType.FullName);
			name.Append('.');
			name.Append(frame.MethodInfo.Name);
			if (showArgumentNames || showArgumentValues) {
				name.Append("(");
				for (int i = 0; i < frame.ArgumentCount; i++) {
					string parameterName = null;
					string argValue = null;
					if (showArgumentNames) {
						try {
							parameterName = frame.MethodInfo.GetParameters()[i].Name;
						} catch { }
						if (parameterName == "") parameterName = null;
					}
					if (showArgumentValues) {
						try {
							argValue = frame.GetArgumentValue(i).AsString(100);
						} catch { }
					}
					if (parameterName != null && argValue != null) {
						name.Append(parameterName);
						name.Append("=");
						name.Append(argValue);
					}
					if (parameterName != null && argValue == null) {
						name.Append(parameterName);
					}
					if (parameterName == null && argValue != null) {
						name.Append(argValue);
					}
					if (parameterName == null && argValue == null) {
						name.Append("Global.NA");
					}
					if (i < frame.ArgumentCount - 1) {
						name.Append(", ");
					}
				}
				name.Append(")");
			}
			
			return name.ToString();
		}

        
        void view_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (MouseButton.Left != e.ChangedButton)
                return;
            var selectedItem = view.SelectedItem as CallStackItem;
            if (null == selectedItem)
            	return;
            var foundAssembly = MainWindow.Instance.CurrentAssemblyList.OpenAssembly(selectedItem.Frame.MethodInfo.DebugModule.FullPath);
            if (null == foundAssembly || null == foundAssembly.AssemblyDefinition)
                return;
            
			MemberReference mr = XmlDocKeyProvider.FindMemberByKey(foundAssembly.AssemblyDefinition.MainModule, "M:" + selectedItem.Name);
			if (mr == null)
				return;
			MainWindow.Instance.JumpToReference(mr);
			// TODO: jump to associated line
            // MainWindow.Instance.TextView.UnfoldAndScroll(selectedItem.LineNumber);
            e.Handled = true;
        }
    }
    
	public class CallStackItem
	{
		public string Name { get; set; }
		public string Language { get; set; }
		public StackFrame Frame { get; set; }
		public string Line { get; set; }
		public string ModuleName { get; set; }
		
		public Brush FontColor {
			get { return Brushes.Black; }
		}
	}
	
    [ExportMainMenuCommand(Menu="_Debugger", Header="Show _Callstack", MenuCategory="View", MenuOrder=9)]
    public class CallstackPanelcommand : SimpleCommand
    {
        public override void Execute(object parameter)
        {
            CallStackPanel.Instance.Show();
        }
    }
}