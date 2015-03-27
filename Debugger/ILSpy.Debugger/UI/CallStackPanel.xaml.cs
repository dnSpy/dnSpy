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
using System.Windows.Media.Imaging;
using System.Xml.Linq;

using Debugger;
using ICSharpCode.Decompiler;
using ICSharpCode.ILSpy;
using ICSharpCode.ILSpy.Debugger.Bookmarks;
using ICSharpCode.ILSpy.Debugger.Commands;
using ICSharpCode.ILSpy.Debugger.Models.TreeModel;
using ICSharpCode.ILSpy.Debugger.Services;
using ICSharpCode.ILSpy.XmlDoc;
using dnlib.DotNet;
using Mono.CSharp;

using NR = ICSharpCode.NRefactory;

namespace ICSharpCode.ILSpy.Debugger.UI
{
	[Export(typeof(IPaneCreator))]
	public class CallStackPanelCreator : IPaneCreator
	{
		public IPane Create(string name)
		{
			if (name == CallStackPanel.Instance.PaneName)
				return CallStackPanel.Instance;
			return null;
		}
	}

    /// <summary>
    /// Interaction logic for CallStackPanel.xaml
    /// </summary>
    public partial class CallStackPanel : UserControl, IPane
    {
        static CallStackPanel s_instance;
    	IDebugger m_currentDebugger;
    	
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

		public string PaneName {
			get { return "call stack window"; }
		}

		public string PaneTitle {
			get { return "Call Stack"; }
		}
    	
        private CallStackPanel()
        {
            InitializeComponent();
        }
  
		public void Show()
		{
			if (!IsVisible)
				MainWindow.Instance.ShowInBottomPane(PaneTitle, this);
		}

		public void Opened()
		{
			DebuggerSettings.Instance.PropertyChanged += new PropertyChangedEventHandler(OnDebuggerSettingChanged);

			SwitchModuleColumn();

			DebuggerService.DebugStarted += new EventHandler(OnDebugStarted);
			DebuggerService.DebugStopped += new EventHandler(OnDebugStopped);
			StackFrameStatementManager.SelectedFrameChanged += OnSelectedFrameChanged;
			if (DebuggerService.IsDebuggerStarted)
				OnDebugStarted(null, EventArgs.Empty);
		}
		
		public void Closed()
		{
            DebuggerService.DebugStarted -= new EventHandler(OnDebugStarted);
            DebuggerService.DebugStopped -= new EventHandler(OnDebugStopped);
			StackFrameStatementManager.SelectedFrameChanged -= OnSelectedFrameChanged;
            if (null != m_currentDebugger)
                OnDebugStopped(null, EventArgs.Empty);
            
            DebuggerSettings.Instance.PropertyChanged -= new PropertyChangedEventHandler(OnDebuggerSettingChanged);
		}
		
		void OnDebuggerSettingChanged(object sender, PropertyChangedEventArgs args)
		{
            ILSpySettings.Update(
                delegate (XElement root) {
                    DebuggerSettings.Instance.Save(root);
                });

		    if (args.PropertyName == "ShowModuleName") {
		        SwitchModuleColumn();
		    }
		    else if (args.PropertyName == "ShowArguments"
		             || args.PropertyName == "ShowArgumentValues") {
		        RefreshPad();
		    }
		        
		}
        
        void OnDebugStarted(object sender, EventArgs args)
        {
        	m_currentDebugger = DebuggerService.CurrentDebugger;
			DebuggerService.ProcessRunningChanged += new EventHandler(OnProcessRunningChanged);
        	
        	OnProcessRunningChanged(null, EventArgs.Empty);
        }

        void OnDebugStopped(object sender, EventArgs args)
        {
        	DebuggerService.ProcessRunningChanged -= new EventHandler(OnProcessRunningChanged);
        	m_currentDebugger = null;
			view.ItemsSource = null;
        }
        
        void OnProcessRunningChanged(object sender, EventArgs args)
        {
        	RefreshPad();
        }

		void OnSelectedFrameChanged(object sender, EventArgs e)
		{
			RefreshPad();
		}
        
        void SwitchModuleColumn()
        {
	        foreach (GridViewColumn c in ((GridView)view.View).Columns) {
	            if ((string)c.Header == "Module") {
	                c.Width = DebuggerSettings.Instance.ShowModuleName ? double.NaN : 0d;
	            }
	        }
        }
        
       	void RefreshPad()
		{
       	    Process debuggedProcess = ((WindowsDebugger)m_currentDebugger).DebuggedProcess;
			if (debuggedProcess == null || debuggedProcess.IsRunning || debuggedProcess.SelectedThread == null) {
				view.ItemsSource = null;
				return;
			}
			
			int selectedIndex = view.SelectedIndex;
			IList<CallStackItem> items = null;
			try {
				items = CreateItems(debuggedProcess);
			} catch {
				if (debuggedProcess == null || debuggedProcess.HasExited) {
					// Process unexpectedly exited
					return;
				}
				else {
					throw;
				}
			}
			view.ItemsSource = items;
			view.SelectedIndex = items == null ? -1 : Math.Min(selectedIndex, items.Count - 1);
		}
		
		IList<CallStackItem> CreateItems(Process debuggedProcess)
		{
		    List<CallStackItem> items = new List<CallStackItem>();
			int frameNumber = 0;
			foreach (StackFrame frame in debuggedProcess.SelectedThread.GetCallstack(100)) {
				CallStackItem item;
				
    			item = new CallStackItem() {
					FrameNumber = frameNumber,
					Name = GetFullName(frame),
					ModuleName = frame.MethodInfo.DebugModule.ToString(),
					Token = (uint)frame.MethodInfo.MetadataToken,
					ILOffset = frame.IP.IsValid ? frame.IP.Offset : -1,
					ILOffsetString = frame.IP.IsValid ? string.Format("0x{0:X4}", frame.IP.Offset) : "????",
					MethodKey = frame.MethodInfo.ToMethodKey(),
				};
				if (frameNumber == 0)
					item.Image = ImageService.CurrentLine;
				else if (frameNumber == StackFrameStatementManager.SelectedFrame)
					item.Image = ImageService.SelectedReturnLine;
				frameNumber++;
				var module = frame.MethodInfo.DebugModule;
				if (module.IsDynamic || module.IsInMemory) {
					//TODO: support this
				}
				else {
					var loadedMod = MainWindow.Instance.LoadAssembly(module.AssemblyFullPath, module.FullPath).ModuleDefinition as ModuleDef;
					if (loadedMod != null) {
						item.ModuleName = loadedMod.FullName;
						var asm = loadedMod.Assembly;
						if (asm != null) // Should never fail
							item.AssemblyName = asm.FullName;
					}
				}
				item.Frame = frame;
				items.Add(item);
			}
		    return items;
		}
		
		internal static string GetFullName(StackFrame frame)
		{
			// disabled by default, my be switched if options / context menu is added
			bool showArgumentNames = DebuggerSettings.Instance.ShowArguments;
			bool showArgumentValues = DebuggerSettings.Instance.ShowArgumentValues;

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
			GoToStatement(view.SelectedItem as CallStackItem);
		}

		void GoToStatement(CallStackItem item)
		{
            if (null == item)
            	return;
			StackFrameStatementManager.SelectedFrame = item.FrameNumber;

			var debugModule = item.Frame.MethodInfo.DebugModule;
			var module = MainWindow.Instance.LoadAssembly(debugModule.AssemblyFullPath, debugModule.FullPath).ModuleDefinition as ModuleDefMD;
			if (module == null)
				return;
            
			IMemberRef mr = module.ResolveToken(item.Token) as IMemberRef;
			if (mr == null)
				return;
			var textView = MainWindow.Instance.SafeActiveTextView;
			if (DebugUtils.JumpTo(textView, mr, item.MethodKey, item.ILOffset))
				textView.TextEditor.TextArea.Focus();
        }

		void view_KeyDown(object sender, KeyEventArgs e)
        {
			if (Keyboard.Modifiers == ModifierKeys.None && e.Key == Key.Enter) {
				GoToStatement(view.SelectedItem as CallStackItem);
				e.Handled = true;
				return;
			}
		}
        
        void SwitchIsChecked(object sender, EventArgs args)
        {
            if (sender is MenuItem) {
                var mi = (MenuItem)sender;
                mi.IsChecked = !mi.IsChecked;
            }
        }
    }
    
	public class CallStackItem
	{
		public ImageSource Image { get; set; }
		public int FrameNumber { get; set; }
		public string Name { get; set; }
		public StackFrame Frame { get; set; }
		public string ModuleName { get; set; }
		public string AssemblyName { get; set; }
		public uint Token { get; set; }
		public int ILOffset { get; set; }
		public string ILOffsetString { get; set; }
		public MethodKey MethodKey { get; set; }
		
		public Brush FontColor {
			get { return Brushes.Black; }
		}
	}
	
    [ExportMainMenuCommand(Menu="_Debug", Header="_Show Call Stack", MenuCategory="View", MenuOrder=9)]
    public class CallstackPanelcommand : SimpleCommand
    {
        public override void Execute(object parameter)
        {
            CallStackPanel.Instance.Show();
        }

		public override bool CanExecute(object parameter)
		{
			return MainWindow.Instance.BottomPaneContent != CallStackPanel.Instance;
		}
    }
}