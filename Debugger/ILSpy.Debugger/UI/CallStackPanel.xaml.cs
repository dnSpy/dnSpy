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
    	
        private CallStackPanel()
        {
            InitializeComponent();
        }

		public void CloseIfActive()
		{
			if (MainWindow.Instance.BottomPaneContent == Instance)
				MainWindow.Instance.CloseBottomPane();
		}
  
		public void Show()
		{
			if (!IsVisible)
			{
                DebuggerSettings.Instance.PropertyChanged += new PropertyChangedEventHandler(OnDebuggerSettingChanged);
                
                SwitchModuleColumn();
			    MainWindow.Instance.ShowInBottomPane("Call Stack", this);
                
                DebuggerService.DebugStarted += new EventHandler(OnDebugStarted);
                DebuggerService.DebugStopped += new EventHandler(OnDebugStopped);
				StackFrameStatementBookmark.SelectedFrameChanged += new EventHandler(OnSelectedFrameChanged);
                if (DebuggerService.IsDebuggerStarted)
                	OnDebugStarted(null, EventArgs.Empty);
			}
		}
		
		public void Closed()
		{
            DebuggerService.DebugStarted -= new EventHandler(OnDebugStarted);
            DebuggerService.DebugStopped -= new EventHandler(OnDebugStopped);
			StackFrameStatementBookmark.SelectedFrameChanged -= new EventHandler(OnSelectedFrameChanged);
            if (null != m_currentDebugger)
                OnDebugStopped(null, EventArgs.Empty);
            
            // save settings
            DebuggerSettings.Instance.PropertyChanged -= new PropertyChangedEventHandler(OnDebuggerSettingChanged);
            ILSpySettings.Update(
                delegate (XElement root) {
                    DebuggerSettings.Instance.Save(root);
                });
		}
		
		void OnDebuggerSettingChanged(object sender, PropertyChangedEventArgs args)
		{
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
        	if (m_currentDebugger.IsProcessRunning)
        		return;
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
				Utils.DoEvents(debuggedProcess);
				items = CreateItems(debuggedProcess);
			} catch(AbortedBecauseDebuggeeResumedException) {
			} catch(System.Exception) {
				if (debuggedProcess == null || debuggedProcess.HasExited) {
					// Process unexpectedly exited
				} else {
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
				if (frameNumber == StackFrameStatementBookmark.SelectedFrame)
					item.Image = ImageService.CurrentLine;
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
				Utils.DoEvents(debuggedProcess);
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
			StackFrameStatementBookmark.SelectedFrame = item.FrameNumber;
            
            var foundAssembly = MainWindow.Instance.CurrentAssemblyList.OpenAssembly(item.Frame.MethodInfo.DebugModule.FullPath, true);
            if (null == foundAssembly)
                return;
			var module = foundAssembly.ModuleDefinition as ModuleDefMD;
			if (module == null)
				return;
            
			IMemberRef mr = module.ResolveToken(item.Token) as IMemberRef;
			if (mr == null)
				return;
			bool alreadySelected;
			if (DebugUtils.JumpToReference(mr, out alreadySelected)) {
				var cm = DebugInformation.CodeMappings;
				var key = item.MethodKey;
				NR.TextLocation location, endLocation;
				if (cm != null && cm.ContainsKey(key)) {
					if (!cm[key].GetInstructionByTokenAndOffset(unchecked((uint)item.ILOffset), out location, out endLocation)) {
						//TODO: Missing IL ranges
					}
					else if (alreadySelected)
						MainWindow.Instance.TextView.ScrollAndMoveCaretTo(location.Line, location.Column);
					else // should never happen
						DebugInformation.JumpToThisLine = location;
				}
				else {
					DebugInformation.JumpToThisLine = Tuple.Create(item.MethodKey, item.ILOffset);
				}
				MainWindow.Instance.TextView.TextEditor.TextArea.Focus();
			}
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
			return DebuggerService.CurrentDebugger != null &&
				DebuggerService.CurrentDebugger.IsDebugging &&
				!DebuggerService.CurrentDebugger.IsProcessRunning &&
				MainWindow.Instance.BottomPaneContent != CallStackPanel.Instance;
		}
    }
}