// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.ComponentModel;
using System.Threading;
using System.Xml.Linq;
using ICSharpCode.ILSpy.Debugger.Services;

namespace ICSharpCode.ILSpy.Debugger
{
	public class DebuggerSettings : INotifyPropertyChanged
	{
		#region members
		static readonly string DEBUGGER_SETTINGS = "DebuggerSettings";
		static readonly string ASK_ARGUMENTS = "askForArguments";
		static readonly string SHOW_MODULE = "showModuleName";
		static readonly string SHOW_ARGUMENTS = "showArguments";
		static readonly string SHOW_ARGUMENTVALUE = "showArgumentValues";
		static readonly string BREAK_AT_BEGINNING = "breakAtBeginning";
		static readonly string ENABLE_JUST_MY_CODE = "enableJustMyCode";
		static readonly string STEP_OVER_DEBUGGER_ATTRIBUTES = "stepOverDebuggerAttributes";
		static readonly string STEP_OVER_ALL_PROPERTIES = "stepOverAllProperties";
		static readonly string STEP_OVER_SINGLE_LINE_PROPERTIES = "stepOverSingleLineProperties";
		static readonly string STEP_OVER_FIELD_ACCESS_PROPERTIES = "stepOverFieldAccessProperties";
	 	
		bool askArguments = true;
		bool showModuleName = true;
		bool showArguments = false;
		bool showArgumentValues = false;
		bool breakAtBeginning = true;
		bool enableJustMyCode = false;
		bool stepOverDebuggerAttributes = false;
		bool stepOverAllProperties = false;
		bool stepOverSingleLineProperties = false;
		bool stepOverFieldAccessProperties = false;
		
		static DebuggerSettings s_instance;
		#endregion
		
		public static DebuggerSettings Instance 
		{
			get {
				if (null == s_instance) {
					var newInstance = new DebuggerSettings();
					ILSpySettings settings = ILSpySettings.Load();
					newInstance.Load(settings);
					Interlocked.CompareExchange(ref s_instance, newInstance, null);
				}
				return s_instance;
			}
		}
		
		DebuggerSettings()
		{			
		}
		
		public void Load(ILSpySettings settings)
		{
			XElement e = settings[DEBUGGER_SETTINGS];
			AskForArguments = (bool?)e.Attribute(ASK_ARGUMENTS) ?? AskForArguments;
			ShowModuleName = (bool?)e.Attribute(SHOW_MODULE) ?? ShowModuleName;
			ShowArguments = (bool?)e.Attribute(SHOW_ARGUMENTS) ?? ShowArguments;
			ShowArgumentValues = (bool?)e.Attribute(SHOW_ARGUMENTVALUE) ?? ShowArgumentValues;
			BreakAtBeginning = (bool?)e.Attribute(BREAK_AT_BEGINNING) ?? BreakAtBeginning;
			EnableJustMyCode = (bool?)e.Attribute(ENABLE_JUST_MY_CODE) ?? EnableJustMyCode;
			StepOverDebuggerAttributes = (bool?)e.Attribute(STEP_OVER_DEBUGGER_ATTRIBUTES) ?? StepOverDebuggerAttributes;
			StepOverAllProperties = (bool?)e.Attribute(STEP_OVER_ALL_PROPERTIES) ?? StepOverAllProperties;
			StepOverSingleLineProperties = (bool?)e.Attribute(STEP_OVER_SINGLE_LINE_PROPERTIES) ?? StepOverSingleLineProperties;
			StepOverFieldAccessProperties = (bool?)e.Attribute(STEP_OVER_FIELD_ACCESS_PROPERTIES) ?? StepOverFieldAccessProperties;

			UpdateDebugger();
		}
		
		public void Save(XElement root)
		{
			XElement section = new XElement(DEBUGGER_SETTINGS);
			section.SetAttributeValue(ASK_ARGUMENTS, AskForArguments);
			section.SetAttributeValue(SHOW_MODULE, ShowModuleName);
			section.SetAttributeValue(SHOW_ARGUMENTS, ShowArguments);
			section.SetAttributeValue(SHOW_ARGUMENTVALUE, ShowArgumentValues);
			section.SetAttributeValue(BREAK_AT_BEGINNING, BreakAtBeginning);
			section.SetAttributeValue(ENABLE_JUST_MY_CODE, EnableJustMyCode);
			section.SetAttributeValue(STEP_OVER_DEBUGGER_ATTRIBUTES, StepOverDebuggerAttributes);
			section.SetAttributeValue(STEP_OVER_ALL_PROPERTIES, StepOverAllProperties);
			section.SetAttributeValue(STEP_OVER_SINGLE_LINE_PROPERTIES, StepOverSingleLineProperties);
			section.SetAttributeValue(STEP_OVER_FIELD_ACCESS_PROPERTIES, StepOverFieldAccessProperties);
	
			XElement existingElement = root.Element(DEBUGGER_SETTINGS);
			if (existingElement != null)
				existingElement.ReplaceWith(section);
			else
				root.Add(section);

			UpdateDebugger();
		}

		void UpdateDebugger()
		{
			var debugger = DebuggerService.CurrentDebugger;
			debugger.EnableJustMyCode = EnableJustMyCode;
			debugger.StepOverDebuggerAttributes = StepOverDebuggerAttributes;
			debugger.StepOverAllProperties = StepOverAllProperties;
			debugger.StepOverSingleLineProperties = StepOverSingleLineProperties;
			debugger.StepOverFieldAccessProperties = StepOverFieldAccessProperties;
		}
		
		/// <summary>
		/// Ask for arguments and working directory before executing a process.
		/// </summary>
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
		/// Show module name in callstack panel.
		/// </summary>
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
		public bool BreakAtBeginning {
		    get { return breakAtBeginning; }
		    set {
		        if (breakAtBeginning != value) {
		            breakAtBeginning = value;
					OnPropertyChanged("BreakAtBeginning");
		        }
		    }
		}

		public bool EnableJustMyCode {
			get { return enableJustMyCode; }
			set {
				if (enableJustMyCode != value) {
					enableJustMyCode = value;
					OnPropertyChanged("EnableJustMyCode");
				}
			}
		}

		public bool StepOverDebuggerAttributes {
			get { return stepOverDebuggerAttributes; }
			set {
				if (stepOverDebuggerAttributes != value) {
					stepOverDebuggerAttributes = value;
					OnPropertyChanged("StepOverDebuggerAttributes");
				}
			}
		}

		public bool StepOverAllProperties {
			get { return stepOverAllProperties; }
			set {
				if (stepOverAllProperties != value) {
					stepOverAllProperties = value;
					OnPropertyChanged("StepOverAllProperties");
				}
			}
		}

		public bool StepOverSingleLineProperties {
			get { return stepOverSingleLineProperties; }
			set {
				if (stepOverSingleLineProperties != value) {
					stepOverSingleLineProperties = value;
					OnPropertyChanged("StepOverSingleLineProperties");
				}
			}
		}

		public bool StepOverFieldAccessProperties {
			get { return stepOverFieldAccessProperties; }
			set {
				if (stepOverFieldAccessProperties != value) {
					stepOverFieldAccessProperties = value;
					OnPropertyChanged("StepOverFieldAccessProperties");
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
