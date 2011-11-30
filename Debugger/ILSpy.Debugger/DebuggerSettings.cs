// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.ComponentModel;
using System.Xml.Linq;

namespace ICSharpCode.ILSpy.Debugger
{
	public class DebuggerSettings : INotifyPropertyChanged
	{
		#region members
		private static readonly string DEBUGGER_SETTINGS = "DebuggerSettings";
		private static readonly string SHOW_WARNINGS = "showWarnings";
		private static readonly string ASK_ARGUMENTS = "askForArguments";
		private static readonly string SHOW_BOOKMARKS = "showAllBookmarks";
		private static readonly string SHOW_MODULE = "showModuleName";
		private static readonly string SHOW_ARGUMENTS = "showArguments";
		private static readonly string SHOW_ARGUMENTVALUE = "showArgumentValues";
		private static readonly string BREAK_AT_BEGINNING = "breakAtBeginning";
	 	
		private bool showWarnings = true;
		private bool askArguments = true;
		private bool debugWholeTypesOnly = false;
		private bool showAllBookmarks = false;
		private bool showModuleName = true;
		private bool showArguments = false;
		private bool showArgumentValues = false;
		private bool breakAtBeginning = false;
		
		private static DebuggerSettings s_instance;
		#endregion
		
		public static DebuggerSettings Instance 
		{
			get {
				if (null == s_instance) {
					s_instance = new DebuggerSettings();
					ILSpySettings settings = ILSpySettings.Load();
					s_instance.Load(settings);
				}
				return s_instance;
			}
		}
		
		private DebuggerSettings()
		{			
		}
		
		public void Load(ILSpySettings settings)
		{
			XElement e = settings[DEBUGGER_SETTINGS];
			ShowWarnings = (bool?)e.Attribute(SHOW_WARNINGS) ?? ShowWarnings;
			AskForArguments = (bool?)e.Attribute(ASK_ARGUMENTS) ?? AskForArguments;
			ShowAllBookmarks = (bool?)e.Attribute(SHOW_BOOKMARKS) ?? ShowAllBookmarks;
			ShowModuleName = (bool?)e.Attribute(SHOW_MODULE) ?? ShowModuleName;
			ShowArguments = (bool?)e.Attribute(SHOW_ARGUMENTS) ?? ShowArguments;
			ShowArgumentValues = (bool?)e.Attribute(SHOW_ARGUMENTVALUE) ?? ShowArgumentValues;
			BreakAtBeginning = (bool?)e.Attribute(BREAK_AT_BEGINNING) ?? BreakAtBeginning;
		}
		
		public void Save(XElement root)
		{
			XElement section = new XElement(DEBUGGER_SETTINGS);
			section.SetAttributeValue(SHOW_WARNINGS, ShowWarnings);
			section.SetAttributeValue(ASK_ARGUMENTS, AskForArguments);
			section.SetAttributeValue(SHOW_BOOKMARKS, ShowAllBookmarks);
			section.SetAttributeValue(SHOW_MODULE, ShowModuleName);
			section.SetAttributeValue(SHOW_ARGUMENTS, ShowArguments);
			section.SetAttributeValue(SHOW_ARGUMENTVALUE, ShowArgumentValues);
			section.SetAttributeValue(BREAK_AT_BEGINNING, BreakAtBeginning);
	
			XElement existingElement = root.Element(DEBUGGER_SETTINGS);
			if (existingElement != null)
				existingElement.ReplaceWith(section);
			else
				root.Add(section);
		}

		/// <summary>
		/// Show warnings messages.
		/// <remarks>Default value is true.</remarks>
		/// </summary>
		[DefaultValue(true)]
		public bool ShowWarnings {
			get { return showWarnings; }
			set {
				if (showWarnings != value) {
					showWarnings = value;
					OnPropertyChanged("ShowWarnings");
				}
			}
		}
		
		/// <summary>
		/// Ask for arguments and working directory before executing a process.
		/// </summary>
		[DefaultValue(true)]
		public bool AskForArguments {
			get { return askArguments; }
			set {
				if (askArguments != value) {
					askArguments = value;
					OnPropertyChanged("AskForArguments");
				}
			}
		}			
		
		/// <summary>
		/// True, if debug only whole types; otherwise false (debug only methods and properties).
		/// <remarks>Default value is false.</remarks>
		/// </summary>
		[DefaultValue(false)]
		public bool DebugWholeTypesOnly {
			get { return debugWholeTypesOnly; }
			set {
				if (debugWholeTypesOnly != value) {
					debugWholeTypesOnly = value;
					OnPropertyChanged("DebugWholeTypesOnly");
				}
			}
		}
		
		/// <summary>
		/// Show all bookmarks in breakpoints window. 
		/// </summary>
		[DefaultValue(false)]
		public bool ShowAllBookmarks {
			get { return showAllBookmarks; }
			set {
				if (showAllBookmarks != value) {
					showAllBookmarks = value;
					OnPropertyChanged("ShowAllBookmarks");
				}
			}
		}
		
		/// <summary>
		/// Show module name in callstack panel.
		/// </summary>
		[DefaultValue(true)]
		public bool ShowModuleName {
		    get { return showModuleName; }
		    set {
		        if (showModuleName != value) {
		            showModuleName = value;
		            OnPropertyChanged("ShowModuleName");
		        }
		    }
		}
		    
		/// <summary>
		/// Show module name in callstack panel.
		/// </summary>
		[DefaultValue(false)]
		public bool ShowArguments {
		    get { return showArguments; }
		    set {
		        if (showArguments != value) {
		            showArguments = value;
		            OnPropertyChanged("ShowArguments");
		        }
		    }
		}
		/// <summary>
		/// Show module name in callstack panel.
		/// </summary>
		[DefaultValue(false)]
		public bool ShowArgumentValues {
		    get { return showArgumentValues; }
		    set {
		        if (showArgumentValues != value) {
		            showArgumentValues = value;
		            OnPropertyChanged("ShowArgumentValues");
		        }
		    }
		}
		
		/// <summary>
		/// Break debugged process after attach or start.
		/// </summary>
		[DefaultValue(false)]
		public bool BreakAtBeginning {
		    get { return breakAtBeginning; }
		    set {
		        if (breakAtBeginning != value) {
		            breakAtBeginning = value;
								OnPropertyChanged("BreakAtBeginning");
		        }
		    }
		}
		
		public event PropertyChangedEventHandler PropertyChanged;
		
		protected virtual void OnPropertyChanged(string propertyName)
		{
			if (PropertyChanged != null) {
				PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
			}
		}
		
	}
}
