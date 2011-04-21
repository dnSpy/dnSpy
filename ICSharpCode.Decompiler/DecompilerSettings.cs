// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.ComponentModel;

namespace ICSharpCode.Decompiler
{
	/// <summary>
	/// Settings for the decompiler.
	/// </summary>
	public class DecompilerSettings : INotifyPropertyChanged
	{
		bool anonymousMethods = true;
		
		/// <summary>
		/// Decompile anonymous methods/lambdas.
		/// </summary>
		public bool AnonymousMethods {
			get { return anonymousMethods; }
			set {
				if (anonymousMethods != value) {
					anonymousMethods = value;
					OnPropertyChanged("AnonymousMethods");
				}
			}
		}
		
		bool yieldReturn = true;
		
		/// <summary>
		/// Decompile enumerators.
		/// </summary>
		public bool YieldReturn {
			get { return yieldReturn; }
			set {
				if (yieldReturn != value) {
					yieldReturn = value;
					OnPropertyChanged("YieldReturn");
				}
			}
		}
		
		bool automaticProperties = true;
		
		/// <summary>
		/// Decompile automatic properties
		/// </summary>
		public bool AutomaticProperties {
			get { return automaticProperties; }
			set {
				if (automaticProperties != value) {
					automaticProperties = value;
					OnPropertyChanged("AutomaticProperties");
				}
			}
		}
		
		bool automaticEvents = true;
		
		/// <summary>
		/// Decompile automatic events
		/// </summary>
		public bool AutomaticEvents {
			get { return automaticEvents; }
			set {
				if (automaticEvents != value) {
					automaticEvents = value;
					OnPropertyChanged("AutomaticEvents");
				}
			}
		}
		
		bool usingStatement = true;
		
		/// <summary>
		/// Decompile using statements.
		/// </summary>
		public bool UsingStatement {
			get { return usingStatement; }
			set {
				if (usingStatement != value) {
					usingStatement = value;
					OnPropertyChanged("UsingStatement");
				}
			}
		}
		
		bool forEachStatement = true;
		
		/// <summary>
		/// Decompile foreach statements.
		/// </summary>
		public bool ForEachStatement {
			get { return forEachStatement; }
			set {
				if (forEachStatement != value) {
					forEachStatement = value;
					OnPropertyChanged("ForEachStatement");
				}
			}
		}
		
		bool lockStatement = true;
		
		/// <summary>
		/// Decompile lock statements.
		/// </summary>
		public bool LockStatement {
			get { return lockStatement; }
			set {
				if (lockStatement != value) {
					lockStatement = value;
					OnPropertyChanged("LockStatement");
				}
			}
		}
		
		bool switchStatementOnString = true;
		
		public bool SwitchStatementOnString {
			get { return switchStatementOnString; }
			set {
				if (switchStatementOnString != value) {
					switchStatementOnString = value;
					OnPropertyChanged("SwitchStatementOnString");
				}
			}
		}
		
		bool usingDeclarations = true;
		
		public bool UsingDeclarations {
			get { return usingDeclarations; }
			set {
				if (usingDeclarations != value) {
					usingDeclarations = value;
					OnPropertyChanged("UsingDeclarations");
				}
			}
		}
		
		bool queryExpressions = true;
		
		public bool QueryExpressions {
			get { return queryExpressions; }
			set {
				if (queryExpressions != value) {
					queryExpressions = value;
					OnPropertyChanged("QueryExpressions");
				}
			}
		}
		
		bool fullyQualifyAmbiguousTypeNames = true;
		
		public bool FullyQualifyAmbiguousTypeNames {
			get { return fullyQualifyAmbiguousTypeNames; }
			set {
				if (fullyQualifyAmbiguousTypeNames != value) {
					fullyQualifyAmbiguousTypeNames = value;
					OnPropertyChanged("FullyQualifyAmbiguousTypeNames");
				}
			}
		}
		
		bool useDebugSymbols = true;
		
		/// <summary>
		/// Gets/Sets whether to use variable names from debug symbols, if available.
		/// </summary>
		public bool UseDebugSymbols {
			get { return useDebugSymbols; }
			set {
				if (useDebugSymbols != value) {
					useDebugSymbols = value;
					OnPropertyChanged("UseDebugSymbols");
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
		
		public DecompilerSettings Clone()
		{
			DecompilerSettings settings = (DecompilerSettings)MemberwiseClone();
			settings.PropertyChanged = null;
			return settings;
		}
	}
}
