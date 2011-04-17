// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Media;

using Debugger;
using Debugger.MetaData;
using ICSharpCode.Decompiler.Ast;
using ICSharpCode.Decompiler.ILAst;
using ICSharpCode.ILSpy.Debugger.Services;
using ICSharpCode.ILSpy.Debugger.Services.Debugger;
using ICSharpCode.NRefactory.Ast;
using ICSharpCode.NRefactory.CSharp;

namespace ICSharpCode.ILSpy.Debugger.Models.TreeModel
{
	internal class ExpressionNode: TreeNode, ISetText, INotifyPropertyChanged
	{
		bool evaluated;
		
		Expression expression;
		bool canSetText;
		GetValueException error;
		string fullText;
		
		public bool Evaluated {
			get { return evaluated; }
			set { evaluated = value; }
		}
		
		public Expression Expression {
			get { return expression; }
		}
		
		public override bool CanSetText {
			get {
				if (!evaluated) EvaluateExpression();
				return canSetText;
			}
		}
		
		public GetValueException Error {
			get {
				if (!evaluated) EvaluateExpression();
				return error;
			}
		}
		
		public string FullText {
			get { return fullText; }
		}
		
		public override string Text {
			get {
				if (!evaluated) EvaluateExpression();
				return base.Text;
			}
			set {
				if (value != base.Text) {
					base.Text = value;
					NotifyPropertyChanged("Text");
				}
			}
		}
		
		public override string FullName {
			get {
				if (!evaluated) EvaluateExpression();
				
				return this.expression.PrettyPrint() ?? Name.Trim();
			}
		}
		
		public override string Type {
			get {
				if (!evaluated) EvaluateExpression();
				return base.Type;
			}
		}
		
		public override IEnumerable<TreeNode> ChildNodes {
			get {
				if (!evaluated) EvaluateExpression();
				return base.ChildNodes;
			}
		}
		
		public override bool HasChildNodes {
			get {
				if (!evaluated) EvaluateExpression();
				return base.HasChildNodes;
			}
		}
		
		/// <summary> Used to determine available VisualizerCommands </summary>
		private DebugType expressionType;
		/// <summary> Used to determine available VisualizerCommands </summary>
		private bool valueIsNull = true;
		
		private IEnumerable<IVisualizerCommand> visualizerCommands;
		public override IEnumerable<IVisualizerCommand> VisualizerCommands {
			get {
				if (visualizerCommands == null) {
					visualizerCommands = getAvailableVisualizerCommands();
				}
				return visualizerCommands;
			}
		}
		
		private IEnumerable<IVisualizerCommand> getAvailableVisualizerCommands()
		{
			if (!evaluated) EvaluateExpression();
			
			if (this.expressionType == null) {
				// no visualizers if EvaluateExpression failed
				yield break;
			}
			if (this.valueIsNull) {
				// no visualizers if evaluated value is null
				yield break;
			}
			if (this.expressionType.IsPrimitive || this.expressionType.IsSystemDotObject() || this.expressionType.IsEnum()) {
				// no visualizers for primitive types
				yield break;
			}
			
			yield break;
//			foreach (var descriptor in VisualizerDescriptors.GetAllDescriptors()) {
//				if (descriptor.IsVisualizerAvailable(this.expressionType)) {
//					yield return descriptor.CreateVisualizerCommand(this.Expression);
//				}
//			}
		}

		public ExpressionNode(ImageSource image, string name, Expression expression)
		{
			this.ImageSource = image;
			this.Name = name;
			this.expression = expression;
		}
		
		void EvaluateExpression()
		{
			evaluated = true;
			
			Value val;
			try {
				var frame = WindowsDebugger.DebuggedProcess.SelectedThread.MostRecentStackFrame;
				int token = frame.MethodInfo.MetadataToken;
				// get the target name
				int index = Name.IndexOf('.');
				string targetName = Name;
				if (index != -1) {
					targetName = Name.Substring(0, index);
				}
				
				// get local variable index
				IEnumerable<ILVariable> list;
				if (DebugData.LocalVariables.TryGetValue(token, out list)) {
					var variable = list.FirstOrDefault(v => v.Name == targetName);
					if (variable != null && variable.OriginalVariable != null) {
						if (expression is MemberReferenceExpression) {
							var memberExpression = (MemberReferenceExpression)expression;
							memberExpression.Target.AddAnnotation(new [] { variable.OriginalVariable.Index });
						} else {
							expression.AddAnnotation(new [] { variable.OriginalVariable.Index });
						}
					}
				}
				
				// evaluate expression
				val = expression.Evaluate(WindowsDebugger.DebuggedProcess);
			} catch (GetValueException e) {
				error = e;
				this.Text = e.Message;
				return;
			}
			
			this.canSetText = val.Type.IsPrimitive;
			
			this.expressionType = val.Type;
			this.Type = val.Type.Name;
			this.valueIsNull = val.IsNull;
			
			// Note that these return enumerators so they are lazy-evaluated
			if (val.IsNull) {
			} else if (val.Type.IsPrimitive || val.Type.FullName == typeof(string).FullName) { // Must be before IsClass
			} else if (val.Type.IsArray) { // Must be before IsClass
				if (val.ArrayLength > 0)
					this.ChildNodes = Utils.LazyGetChildNodesOfArray(this.Expression, val.ArrayDimensions);
			} else if (val.Type.IsClass || val.Type.IsValueType) {
				if (val.Type.FullNameWithoutGenericArguments == typeof(List<>).FullName) {
					if ((int)val.GetMemberValue("_size").PrimitiveValue > 0)
						this.ChildNodes = Utils.LazyGetItemsOfIList(this.expression);
				} else {
					this.ChildNodes = Utils.LazyGetChildNodesOfObject(this.Expression, val.Type);
				}
			} else if (val.Type.IsPointer) {
				Value deRef = val.Dereference();
				if (deRef != null) {
					this.ChildNodes = new ExpressionNode [] { new ExpressionNode(this.ImageSource, "*" + this.Name, this.Expression.AppendDereference()) };
				}
			}
			
//			if (DebuggingOptions.Instance.ICorDebugVisualizerEnabled) {
//				TreeNode info = ICorDebug.GetDebugInfoRoot(val.AppDomain, val.CorValue);
//				this.ChildNodes = Utils.PrependNode(info, this.ChildNodes);
//			}
			
			// Do last since it may expire the object
			if (val.Type.IsInteger) {
				fullText = FormatInteger(val.PrimitiveValue);
			} else if (val.Type.IsPointer) {
				fullText = String.Format("0x{0:X}", val.PointerAddress);
			} else if ((val.Type.FullName == typeof(string).FullName ||
			            val.Type.FullName == typeof(char).FullName) && !val.IsNull) {
				try {
					fullText = '"' + Escape(val.InvokeToString()) + '"';
				} catch (GetValueException e) {
					error = e;
					fullText = e.Message;
					return;
				}
			} else if ((val.Type.IsClass || val.Type.IsValueType) && !val.IsNull) {
				try {
					fullText = val.InvokeToString();
				} catch (GetValueException e) {
					error = e;
					fullText = e.Message;
					return;
				}
			} else {
				fullText = val.AsString();
			}
			
			this.Text = (fullText.Length > 256) ? fullText.Substring(0, 256) + "..." : fullText;
		}
		
		string Escape(string source)
		{
			return source.Replace("\n", "\\n")
				.Replace("\t", "\\t")
				.Replace("\r", "\\r")
				.Replace("\0", "\\0")
				.Replace("\b", "\\b")
				.Replace("\a", "\\a")
				.Replace("\f", "\\f")
				.Replace("\v", "\\v")
				.Replace("\"", "\\\"");
		}
		
		string FormatInteger(object i)
		{
			// if (DebuggingOptions.Instance.ShowIntegersAs == ShowIntegersAs.Decimal)
			if (true)
				return i.ToString();
			
//			string hex = null;
//			for(int len = 1;; len *= 2) {
//				hex = string.Format("{0:X" + len + "}", i);
//				if (hex.Length == len)
//					break;
//			}
//			
//			if (true) {
//				return "0x" + hex;
//			} else {
//				if (ShowAsHex(i)) {
//					return String.Format("{0} (0x{1})", i, hex);
//				} else {
//					return i.ToString();
//				}
//			}
		}
		
		bool ShowAsHex(object i)
		{
			ulong val;
			if (i is sbyte || i is short || i is int || i is long) {
				unchecked { val = (ulong)Convert.ToInt64(i); }
				if (val > (ulong)long.MaxValue)
					val = ~val + 1;
			} else {
				val = Convert.ToUInt64(i);
			}
			if (val >= 0x10000)
				return true;
			
			int ones = 0; // How many 1s there is
			int runs = 0; // How many runs of 1s there is
			int size = 0; // Size of the integer in bits
			while(val != 0) { // There is at least one 1
				while((val & 1) == 0) { // Skip 0s
					val = val >> 1;
					size++;
				}
				while((val & 1) == 1) { // Skip 1s
					val = val >> 1;
					size++;
					ones++;
				}
				runs++;
			}
			
			return size >= 7 && runs <= (size + 7) / 8;
		}
		
		public override bool SetText(string newText)
		{
			string fullName = FullName;
			
			Value val = null;
			try {
				val = this.Expression.Evaluate(WindowsDebugger.DebuggedProcess);
				if (val.Type.IsInteger && newText.StartsWith("0x")) {
					try {
						val.PrimitiveValue = long.Parse(newText.Substring(2), NumberStyles.HexNumber);
					} catch (FormatException) {
						throw new NotSupportedException();
					} catch (OverflowException) {
						throw new NotSupportedException();
					}
				} else {
					val.PrimitiveValue = newText;
				}
				this.Text = newText;
				return true;
			} catch (NotSupportedException) {
				string format = "Can not convert {0} to {1}";
				string msg = string.Format(format, newText, val.Type.PrimitiveType);
				System.Windows.MessageBox.Show(msg);
			} catch (COMException) {
				// COMException (0x80131330): Cannot perfrom SetValue on non-leaf frames.
				// Happens if trying to set value after exception is breaked
				System.Windows.MessageBox.Show("UnknownError");
			}
			return false;
		}
		
		public static ImageSource GetImageForThis(out string imageName)
		{
			imageName = "Icons.16x16.Parameter";
			return ImageService.GetImage(imageName);
		}
		
		public static ImageSource GetImageForParameter(out string imageName)
		{
			imageName = "Icons.16x16.Parameter";
			return ImageService.GetImage(imageName);
		}
		
		public static ImageSource GetImageForLocalVariable(out string imageName)
		{
			imageName = "Icons.16x16.Local";
			return ImageService.GetImage(imageName);
		}
		
		public static ImageSource GetImageForArrayIndexer(out string imageName)
		{
			imageName = "Icons.16x16.Field";
			return ImageService.GetImage(imageName);
		}
		
		public static ImageSource GetImageForMember(IDebugMemberInfo memberInfo, out string imageName)
		{
			string name = string.Empty;
			if (memberInfo.IsPublic) {
			} else if (memberInfo.IsAssembly) {
				name += "Internal";
			} else if (memberInfo.IsFamily) {
				name += "Protected";
			} else if (memberInfo.IsPrivate) {
				name += "Private";
			}
			if (memberInfo is FieldInfo) {
				name += "Field";
			} else if (memberInfo is PropertyInfo) {
				name += "Property";
			} else if (memberInfo is MethodInfo) {
				name += "Method";
			} else {
				throw new DebuggerException("Unknown member type " + memberInfo.GetType().FullName);
			}
			
			imageName = "Icons.16x16." + name;
			return ImageService.GetImage(imageName);
		}
		
//		public ContextMenuStrip GetContextMenu()
//		{
//			if (this.Error != null) return GetErrorContextMenu();
//
//			ContextMenuStrip menu = new ContextMenuStrip();
//
//			ToolStripMenuItem copyItem;
//			copyItem = new ToolStripMenuItem();
//			copyItem.Text = ResourceService.GetString("MainWindow.Windows.Debug.LocalVariables.CopyToClipboard");
//			copyItem.Checked = false;
//			copyItem.Click += delegate {
//				ClipboardWrapper.SetText(fullText);
//			};
		
//			ToolStripMenuItem hexView;
//			hexView = new ToolStripMenuItem();
//			hexView.Text = ResourceService.GetString("MainWindow.Windows.Debug.LocalVariables.ShowInHexadecimal");
//			hexView.Checked = DebuggingOptions.Instance.ShowValuesInHexadecimal;
//			hexView.Click += delegate {
//				// refresh all pads that use ValueNode for display
//				DebuggingOptions.Instance.ShowValuesInHexadecimal = !DebuggingOptions.Instance.ShowValuesInHexadecimal;
//				// always check if instance is null, might be null if pad is not opened
//				if (LocalVarPad.Instance != null)
//					LocalVarPad.Instance.RefreshPad();
//				if (WatchPad.Instance != null)
//					WatchPad.Instance.RefreshPad();
//			};
		
//			menu.Items.AddRange(new ToolStripItem[] {
//			                    	copyItem,
//			                    	//hexView
//			                    });
//
//			return menu;
//		}
		
		public static WindowsDebugger WindowsDebugger {
			get {
				return (WindowsDebugger)DebuggerService.CurrentDebugger;
			}
		}
		
		public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
		
		private void NotifyPropertyChanged(string info)
		{
			if (PropertyChanged != null)
			{
				PropertyChanged(this, new System.ComponentModel.PropertyChangedEventArgs(info));
			}
		}
	}
}
