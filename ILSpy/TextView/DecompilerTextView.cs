// Copyright (c) 2011 AlphaSierraPapa for the SharpDevelop Team
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using System.Xml;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;
using ICSharpCode.AvalonEdit.Folding;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using ICSharpCode.AvalonEdit.Rendering;
using ICSharpCode.AvalonEdit.Search;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.ILAst;
using ICSharpCode.ILSpy.AsmEditor;
using ICSharpCode.ILSpy.AvalonEdit;
using ICSharpCode.ILSpy.Debugger;
using ICSharpCode.ILSpy.dntheme;
using ICSharpCode.ILSpy.Options;
using ICSharpCode.ILSpy.TreeNodes;
using ICSharpCode.ILSpy.XmlDoc;
using ICSharpCode.NRefactory;
using ICSharpCode.NRefactory.Documentation;
using Microsoft.Win32;
using dnlib.DotNet;
using dnlib.DotNet.Emit;

namespace ICSharpCode.ILSpy.TextView
{
	/// <summary>
	/// Manages the TextEditor showing the decompiled code.
	/// Contains all the threading logic that makes the decompiler work in the background.
	/// </summary>
	public sealed partial class DecompilerTextView : UserControl, IDisposable
	{
		readonly ReferenceElementGenerator referenceElementGenerator;
		readonly UIElementGenerator uiElementGenerator;
		List<VisualLineElementGenerator> activeCustomElementGenerators = new List<VisualLineElementGenerator>();
		FoldingManager foldingManager;
		
		DefinitionLookup definitionLookup;
		TextSegmentCollection<ReferenceSegment> references;
		CancellationTokenSource currentCancellationTokenSource;
		
		internal readonly IconBarManager manager;
		readonly IconBarMargin iconMargin;
		readonly TextMarkerService textMarkerService;
		readonly List<ITextMarker> markedReferences = new List<ITextMarker>();

		readonly SearchPanel searchPanel;

		public TextEditor TextEditor {
			get { return textEditor; }
		}

		internal TextSegmentCollection<ReferenceSegment> References {
			get { return references; }
		}

		static DecompilerTextView() {
			HighlightingManager.Instance.RegisterHighlighting(
				"ILAsm", new string[] { ".il" },
				delegate {
					using (Stream s = typeof(DecompilerTextView).Assembly.GetManifestResourceStream(typeof(DecompilerTextView), "ILAsm-Mode.xshd")) {
						using (XmlTextReader reader = new XmlTextReader(s)) {
							return HighlightingLoader.Load(reader, HighlightingManager.Instance);
						}
					}
				});
		}
		
		#region Constructor
		public DecompilerTextView()
		{
			this.Loaded += DecompilerTextView_Loaded;
			InitializeComponent();
			
			textEditor.TextArea.SelectionCornerRadius = 0;
			this.referenceElementGenerator = new ReferenceElementGenerator(this.JumpToReference, this.IsLink);
			// Add the ref elem generator first in case one of the refs looks like a http link etc
			textEditor.TextArea.TextView.ElementGenerators.Insert(0, referenceElementGenerator);
			textEditor.TextArea.PreviewKeyDown += TextEditor_PreviewKeyDown;
			this.uiElementGenerator = new UIElementGenerator();
			textEditor.TextArea.TextView.ElementGenerators.Add(uiElementGenerator);
			textEditor.Options.RequireControlModifierForHyperlinkClick = false;
			textEditor.TextArea.TextView.MouseHover += TextViewMouseHover;
			textEditor.TextArea.TextView.MouseHoverStopped += TextViewMouseHoverStopped;
			textEditor.TextArea.TextView.MouseDown += (s, e) => MainWindow.Instance.ClosePopups();
			textEditor.SetBinding(Control.FontFamilyProperty, new Binding { Source = DisplaySettingsPanel.CurrentDisplaySettings, Path = new PropertyPath("SelectedFont") });
			textEditor.SetBinding(Control.FontSizeProperty, new Binding { Source = DisplaySettingsPanel.CurrentDisplaySettings, Path = new PropertyPath("SelectedFontSize") });
			
			// add marker service & margin
			iconMargin = new IconBarMargin(manager = new IconBarManager(), this);
			textMarkerService = new TextMarkerService(this);
			textEditor.TextArea.TextView.BackgroundRenderers.Add(textMarkerService);
			textEditor.ShowLineNumbers = true;
			DisplaySettingsPanel.CurrentDisplaySettings.PropertyChanged += CurrentDisplaySettings_PropertyChanged;

			// SearchPanel
			searchPanel = SearchPanel.Install(textEditor.TextArea);
			searchPanel.RegisterCommands(this.CommandBindings);
			
			textEditor.TextArea.LeftMargins.Insert(0, iconMargin);
			textEditor.TextArea.TextView.VisualLinesChanged += delegate { iconMargin.InvalidateVisual(); };
			
			// Bookmarks context menu
			IconMarginActionsProvider.Add(iconMargin, this);

			// Make sure it's not possible to right-click something under the wait adorner or to
			// move the caret.
			waitAdorner.MouseDown += (s, e) => e.Handled = true;
			waitAdorner.MouseUp += (s, e) => e.Handled = true;
			waitAdornerButton.IsVisibleChanged += waitAdornerButton_IsVisibleChanged;

			textEditor.TextArea.MouseWheel += TextArea_MouseWheel;
			TextEditor.TextArea.Caret.PositionChanged += Caret_PositionChanged;

			InputBindings.Add(new KeyBinding(new RelayCommand(a => MoveReference(true)), Key.Tab, ModifierKeys.None));
			InputBindings.Add(new KeyBinding(new RelayCommand(a => MoveReference(false)), Key.Tab, ModifierKeys.Shift));
			textEditor.TextArea.InputBindings.Add(new KeyBinding(new RelayCommand(a => PageUp()), Key.PageUp, ModifierKeys.Control));
			textEditor.TextArea.InputBindings.Add(new KeyBinding(new RelayCommand(a => PageDown()), Key.PageDown, ModifierKeys.Control));
			textEditor.TextArea.InputBindings.Add(new KeyBinding(new RelayCommand(a => UpDownLine(false)), Key.Down, ModifierKeys.Control));
			textEditor.TextArea.InputBindings.Add(new KeyBinding(new RelayCommand(a => UpDownLine(true)), Key.Up, ModifierKeys.Control));
			InputBindings.Add(new KeyBinding(new RelayCommand(a => MainWindow.Instance.BackCommand(this)), Key.Back, ModifierKeys.None));
			InputBindings.Add(new KeyBinding(new RelayCommand(a => FollowReference()), Key.F12, ModifierKeys.None));
			InputBindings.Add(new KeyBinding(new RelayCommand(a => FollowReference()), Key.Enter, ModifierKeys.None));
			InputBindings.Add(new KeyBinding(new RelayCommand(a => FollowReferenceNewTab()), Key.F12, ModifierKeys.Control));
			InputBindings.Add(new KeyBinding(new RelayCommand(a => FollowReferenceNewTab()), Key.Enter, ModifierKeys.Control));
			InputBindings.Add(new KeyBinding(new RelayCommand(a => ClearMarkedReferencesAndPopups()), Key.Escape, ModifierKeys.None));
		}

		void Caret_PositionChanged(object sender, EventArgs e)
		{
			MainWindow.Instance.ClosePopups();
			CloseToolTip();

			if (ICSharpCode.ILSpy.Options.DisplaySettingsPanel.CurrentDisplaySettings.AutoHighlightRefs) {
				int offset = textEditor.TextArea.Caret.Offset;
				var refSeg = GetReferenceSegmentAt(offset);
				if (refSeg != null)
					MarkReferences(refSeg);
				else
					ClearMarkedReferences();
			}
		}

		void TextArea_MouseWheel(object sender, MouseWheelEventArgs e)
		{
			if (Keyboard.Modifiers != ModifierKeys.Control)
				return;

			MainWindow.Instance.ZoomMouseWheel(this, e.Delta);
			e.Handled = true;
		}

		void DecompilerTextView_Loaded(object sender, RoutedEventArgs e)
		{
			Loaded -= DecompilerTextView_Loaded;
			ShowLineMargin();
			
			textEditor.TextArea.TextView.VisualLinesChanged += (s, _) => iconMargin.InvalidateVisual();

			// We need to add this here in Loaded and not in the ctor. Adding it in the ctor causes
			// the highlighted line not to be shown when opening a new tab. It's shown again when
			// the caret is moved to another line.
			textEditor.TextArea.TextView.LineTransformers.Add(textMarkerService);
		}
		
		#endregion

		internal void OnThemeUpdated()
		{
			textEditor.OnThemeUpdated();
			var theme = Themes.Theme;
			var marker = theme.GetColor(ColorType.SearchResultMarker).InheritedColor;
			searchPanel.MarkerBrush = marker.Background == null ? Brushes.LightGreen : marker.Background.GetBrush(null);
			iconMargin.InvalidateVisual();
		}
		
		#region Line margin

		void CurrentDisplaySettings_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == "ShowLineNumbers") {
				ShowLineMargin();
			}
		}
		
		void ShowLineMargin()
		{
			foreach (var margin in this.textEditor.TextArea.LeftMargins) {
				if (margin is LineNumberMargin || margin is System.Windows.Shapes.Line) {
					margin.Visibility = DisplaySettingsPanel.CurrentDisplaySettings.ShowLineNumbers ? Visibility.Visible : Visibility.Collapsed;
				}
			}
		}
		
		#endregion
		
		#region Tooltip support
		ToolTip tooltip;
		
		void TextViewMouseHoverStopped(object sender, MouseEventArgs e)
		{
			CloseToolTip();
		}

		void CloseToolTip()
		{
			if (tooltip != null)
				tooltip.IsOpen = false;
		}

		void TextViewMouseHover(object sender, MouseEventArgs e)
		{
			TextViewPosition? position = textEditor.TextArea.TextView.GetPosition(e.GetPosition(textEditor.TextArea.TextView) + textEditor.TextArea.TextView.ScrollOffset);
			if (position == null)
				return;
			int offset = textEditor.Document.GetOffset(position.Value.Location);
			ReferenceSegment seg = GetReferenceSegmentAt(offset);
			if (seg == null)
				return;
			object content = GenerateToolTip(seg);
			if (tooltip != null)
				tooltip.IsOpen = false;
			if (content != null)
				tooltip = new ToolTip() { Content = content, IsOpen = true, Style = (Style)this.FindResource("CodeToolTip") };
		}
		
		object GenerateToolTip(ReferenceSegment segment)
		{
			if (segment.Reference is OpCode) {
				OpCode code = (OpCode)segment.Reference;

				var gen = new SimpleHighlighter();

				var s = ILLanguage.GetOpCodeDocumentation(code);
				string opCodeHex = code.Size > 1 ? string.Format("0x{0:X4}", code.Value) : string.Format("0x{0:X2}", code.Value);
				gen.TextOutput.Write(code.Name, TextTokenType.OpCode);
				gen.TextOutput.WriteSpace();
				gen.TextOutput.Write('(', TextTokenType.Operator);
				gen.TextOutput.Write(opCodeHex, TextTokenType.Number);
				gen.TextOutput.Write(')', TextTokenType.Operator);
				if (s != null) {
					gen.TextOutput.Write(" - ", TextTokenType.Text);
					gen.TextOutput.Write(s, TextTokenType.Text);
				}

				return gen.Create();
			} else if (segment.Reference is GenericParam) {
				return GenerateToolTip((GenericParam)segment.Reference);
			} else if (segment.Reference is IMemberRef) {
				var mr = (IMemberRef)segment.Reference;
				var resolvedRef = Resolve(mr) ?? mr;
				var genFirstLine = new SimpleHighlighter();
				MainWindow.Instance.GetLanguage(this).WriteToolTip(genFirstLine.TextOutput, mr, null);
				var gen = new SimpleHighlighter();
				try {
					if (resolvedRef is IMemberDef) {
						XmlDocumentationProvider docProvider = XmlDocLoader.LoadDocumentation(resolvedRef.Module);
						if (docProvider != null)
							gen.WriteXmlDoc(GetDocumentation(docProvider, resolvedRef));
					}
				} catch (XmlException) {
					// ignore
				}
				return GenerateToolTip(resolvedRef, genFirstLine, gen);
			} else if (segment.Reference is Parameter) {
				return GenerateToolTip((Parameter)segment.Reference, null);
			} else if (segment.Reference is ILVariable) {
				var ilVar = (ILVariable)segment.Reference;
				return GenerateToolTip(ilVar.OriginalVariable, ilVar.Name);
			}

			return null;
		}

		static IMemberRef Resolve(IMemberRef mr)
		{
			if (mr is ITypeDefOrRef)
				return ((ITypeDefOrRef)mr).ResolveTypeDef();
			if (mr is IMethod && ((IMethod)mr).IsMethod)
				return ((IMethod)mr).ResolveMethodDef();
			if (mr is IField)
				return ((IField)mr).ResolveFieldDef();
			Debug.Assert(mr is PropertyDef || mr is EventDef || mr is GenericParam, "Unknown IMemberRef");
			return null;
		}

		static UIElement GenerateToolTip(object iconType, SimpleHighlighter genFirstLine, SimpleHighlighter gen)
		{
			var res = new StackPanel {
				Orientation = Orientation.Vertical,
			};
			var sp = new StackPanel {
				Orientation = Orientation.Horizontal,
			};
			res.Children.Add(sp);
			if (!gen.IsEmpty)
				res.Children.Add(gen.Create());
			var icon = GetImage(iconType, BackgroundType.CodeToolTip);
			if (icon != null) {
				sp.Children.Add(new Image {
					Width = 16,
					Height = 16,
					Source = icon,
					Margin = new Thickness(0, 0, 4, 0),
					VerticalAlignment = VerticalAlignment.Top,
					HorizontalAlignment = HorizontalAlignment.Left,
				});
			}
			sp.Children.Add(genFirstLine.Create());
			return res;
		}

		static ImageSource GetImage(object obj, BackgroundType bgType)
		{
			var td = obj as TypeDef;
			if (td != null)
				return TypeTreeNode.GetIcon(td, bgType);

			var md = obj as MethodDef;
			if (md != null)
				return MethodTreeNode.GetIcon(md, bgType);

			var pd = obj as PropertyDef;
			if (pd != null)
				return PropertyTreeNode.GetIcon(pd, bgType);

			var ed = obj as EventDef;
			if (ed != null)
				return EventTreeNode.GetIcon(ed, bgType);

			var fd = obj as FieldDef;
			if (fd != null)
				return FieldTreeNode.GetIcon(fd, bgType);

			var gd = obj as GenericParam;
			if (gd != null)
				return ImageCache.Instance.GetImage("GenericParameter", bgType);

			if (obj is Local)
				return ImageCache.Instance.GetImage("Local", bgType);

			if (obj is Parameter)
				return ImageCache.Instance.GetImage("Parameter", bgType);

			if (obj is IType)
				return ImageCache.Instance.GetImage("Class", bgType);
			if (obj is IMethod && ((IMethod)obj).IsMethod)
				return ImageCache.Instance.GetImage("Method", bgType);
			if (obj is IField && ((IField)obj).IsField)
				return ImageCache.Instance.GetImage("Field", bgType);

			return null;
		}

		object GenerateToolTip(IVariable variable, string name)
		{
			if (variable == null)
				return name == null ? null : string.Format("(local variable) {0}", name);

			var genFirstLine = new SimpleHighlighter();
			MainWindow.Instance.GetLanguage(this).WriteToolTip(genFirstLine.TextOutput, variable, name);

			var gen = new SimpleHighlighter();
			if (variable is Parameter) {
				var method = ((Parameter)variable).Method;
				try {
					XmlDocumentationProvider docProvider = XmlDocLoader.LoadDocumentation(method.Module);
					if (docProvider != null) {
						if (!gen.WriteXmlDocParameter(GetDocumentation(docProvider, method), variable.Name)) {
							TypeDef owner = method.DeclaringType;
							while (owner != null) {
								if (gen.WriteXmlDocParameter(GetDocumentation(docProvider, owner), variable.Name))
									break;
								owner = owner.DeclaringType;
							}
						}
					}
				}
				catch (XmlException) {
				}
			}

			return GenerateToolTip(variable, genFirstLine, gen);
		}

		object GenerateToolTip(GenericParam gp)
		{
			if (gp == null)
				return null;

			var genFirstLine = new SimpleHighlighter();
			MainWindow.Instance.GetLanguage(this).WriteToolTip(genFirstLine.TextOutput, gp, null);

			var gen = new SimpleHighlighter();
			try {
				XmlDocumentationProvider docProvider = XmlDocLoader.LoadDocumentation(gp.Module);
				if (docProvider != null) {
					if (!gen.WriteXmlDocGeneric(GetDocumentation(docProvider, gp.Owner), gp.Name) && gp.Owner is TypeDef) {
						// If there's no doc available, use the parent class' documentation if this
						// is a generic type parameter (and not a generic method parameter).
						TypeDef owner = ((TypeDef)gp.Owner).DeclaringType;
						while (owner != null) {
							if (gen.WriteXmlDocGeneric(GetDocumentation(docProvider, owner), gp.Name))
								break;
							owner = owner.DeclaringType;
						}
					}
				}
			}
			catch (XmlException) {
			}

			return GenerateToolTip(gp, genFirstLine, gen);
		}

		string GetDocumentation(XmlDocumentationProvider docProvider, IMemberRef mr)
		{
			var doc = docProvider.GetDocumentation(XmlDocKeyProvider.GetKey(mr));
			if (doc != null)
				return doc;
			var method = mr as IMethod;
			if (method == null)
				return null;
			string name = method.Name;
			if (name.StartsWith("set_") || name.StartsWith("get_")) {
				var md = ICSharpCode.Decompiler.DnlibExtensions.Resolve(method);
				if (md == null)
					return null;
				mr = md.DeclaringType.Properties.FirstOrDefault(p => p.GetMethod == md || p.SetMethod == md);
				return docProvider.GetDocumentation(XmlDocKeyProvider.GetKey(mr));
			}
			else if (name.StartsWith("add_")) {
				var md = ICSharpCode.Decompiler.DnlibExtensions.Resolve(method);
				if (md == null)
					return null;
				mr = md.DeclaringType.Events.FirstOrDefault(p => p.AddMethod == md);
				return docProvider.GetDocumentation(XmlDocKeyProvider.GetKey(mr));
			}
			else if (name.StartsWith("remove_")) {
				var md = ICSharpCode.Decompiler.DnlibExtensions.Resolve(method);
				if (md == null)
					return null;
				mr = md.DeclaringType.Events.FirstOrDefault(p => p.RemoveMethod == md);
				return docProvider.GetDocumentation(XmlDocKeyProvider.GetKey(mr));
			}
			return null;
		}
		#endregion
		
		#region RunWithCancellation
		void waitAdornerButton_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
		{
			if (waitAdornerButton.IsVisible && IsKeyboardFocused)
				MainWindow.SetFocusIfNoMenuIsOpened(waitAdornerButton);
		}

		/// <summary>
		/// Switches the GUI into "waiting" mode, then calls <paramref name="taskCreation"/> to create
		/// the task.
		/// If another task is started before the previous task finishes running, the previous task is cancelled.
		/// </summary>
		public Task<T> RunWithCancellation<T>(Func<CancellationToken, Task<T>> taskCreation)
		{
			if (waitAdorner.Visibility != Visibility.Visible) {
				waitAdorner.Visibility = Visibility.Visible;
				waitAdorner.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, new Duration(TimeSpan.FromSeconds(0.5)), FillBehavior.Stop));
				if (IsKeyboardFocused)
					MainWindow.SetFocusIfNoMenuIsOpened(waitAdornerButton);
				var taskBar = MainWindow.Instance.TaskbarItemInfo;
				if (taskBar != null) {
					taskBar.ProgressState = System.Windows.Shell.TaskbarItemProgressState.Indeterminate;
				}
			}
			CancellationTokenSource previousCancellationTokenSource = currentCancellationTokenSource;
			var myCancellationTokenSource = new CancellationTokenSource();
			currentCancellationTokenSource = myCancellationTokenSource;
			// cancel the previous only after current was set to the new one (avoid that the old one still finishes successfully)
			if (previousCancellationTokenSource != null)
				previousCancellationTokenSource.Cancel();
			
			var tcs = new TaskCompletionSource<T>();
			Task<T> task;
			try {
				task = taskCreation(myCancellationTokenSource.Token);
			} catch (OperationCanceledException) {
				task = TaskHelper.FromCancellation<T>();
			} catch (Exception ex) {
				task = TaskHelper.FromException<T>(ex);
			}
			Action continuation = delegate {
				try {
					if (currentCancellationTokenSource == myCancellationTokenSource) {
						currentCancellationTokenSource = null;
						waitAdorner.Visibility = Visibility.Collapsed;
						if (waitAdornerButton.IsKeyboardFocused)
							MainWindow.Instance.SetTextEditorFocus(this);
						var taskBar = MainWindow.Instance.TaskbarItemInfo;
						if (taskBar != null) {
							taskBar.ProgressState = System.Windows.Shell.TaskbarItemProgressState.None;
						}
						if (task.IsCanceled) {
							AvalonEditTextOutput output = new AvalonEditTextOutput();
							output.WriteLine("The operation was canceled.", TextTokenType.Text);
							ShowOutput(output);
						}
						tcs.SetFromTask(task);
					} else {
						tcs.SetCanceled();
					}
				} finally {
					myCancellationTokenSource.Dispose();
				}
			};
			task.ContinueWith(delegate { Dispatcher.BeginInvoke(DispatcherPriority.Normal, continuation); });
			return tcs.Task;
		}
		
		void cancelButton_Click(object sender, RoutedEventArgs e)
		{
			if (currentCancellationTokenSource != null) {
				currentCancellationTokenSource.Cancel();
				// Don't set to null: the task still needs to produce output and hide the wait adorner
			}
		}
		#endregion
		
		#region ShowOutput
		public void ShowText(AvalonEditTextOutput textOutput)
		{
			ShowNodes(textOutput, null);
		}

		public void ShowNode(AvalonEditTextOutput textOutput, ILSpyTreeNode node, IHighlightingDefinition highlighting = null)
		{
			ShowNodes(textOutput, new[] { node }, highlighting);
		}

		/// <summary>
		/// Shows the given output in the text view.
		/// Cancels any currently running decompilation tasks.
		/// </summary>
		public void ShowNodes(AvalonEditTextOutput textOutput, ILSpyTreeNode[] nodes, IHighlightingDefinition highlighting = null)
		{
			CancelDecompilation();
			ShowOutput(textOutput, highlighting);
		}

		void CancelDecompilation()
		{
			// Cancel the decompilation task:
			if (currentCancellationTokenSource != null) {
				currentCancellationTokenSource.Cancel();
				currentCancellationTokenSource = null; // prevent canceled task from producing output
			}
			CancelDecompileAsync();
			waitAdorner.Visibility = Visibility.Collapsed;
		}
		
		/// <summary>
		/// Shows the given output in the text view.
		/// </summary>
		void ShowOutput(AvalonEditTextOutput textOutput, IHighlightingDefinition highlighting = null, DecompilerTextViewState state = null, ILSpyTreeNode[] nodes = null)
		{
			var evt = OnBeforeShowOutput;
			if (evt != null)
				evt(this, new ShowOutputEventArgs(nodes, highlighting, state));

			//Debug.WriteLine("Showing {0} characters of output", textOutput.TextLength);
			Stopwatch w = Stopwatch.StartNew();
			textEditor.LanguageTokens = textOutput.LanguageTokens;
			textEditor.LanguageTokens.Finish();

			ClearMarkedReferences();
			textEditor.ScrollToHome();
			if (foldingManager != null) {
				FoldingManager.Uninstall(foldingManager);
				foldingManager = null;
			}
			textEditor.Document = null; // clear old document while we're changing the highlighting
			uiElementGenerator.UIElements = textOutput.UIElements;
			referenceElementGenerator.References = textOutput.References;
			references = textOutput.References;
			definitionLookup = textOutput.DefinitionLookup;
			textEditor.SyntaxHighlighting = highlighting;
			
			// Change the set of active element generators:
			foreach (var elementGenerator in activeCustomElementGenerators) {
				textEditor.TextArea.TextView.ElementGenerators.Remove(elementGenerator);
			}
			activeCustomElementGenerators.Clear();
			
			foreach (var elementGenerator in textOutput.elementGenerators) {
				textEditor.TextArea.TextView.ElementGenerators.Add(elementGenerator);
				activeCustomElementGenerators.Add(elementGenerator);
			}
			
			//Debug.WriteLine("  Set-up: {0}", w.Elapsed); w.Restart();
			textEditor.Document = textOutput.GetDocument();
			//Debug.WriteLine("  Assigning document: {0}", w.Elapsed); w.Restart();
			if (textOutput.Foldings.Count > 0) {
				if (state != null) {
					state.RestoreFoldings(textOutput.Foldings);
				}
				foldingManager = FoldingManager.Install(textEditor.TextArea);
				foldingManager.UpdateFoldings(textOutput.Foldings.OrderBy(f => f.StartOffset), -1);
				Debug.WriteLine("  Updating folding: {0}", w.Elapsed); w.Restart();
			}
			if (state != null)
				EditorPositionState = state.EditorPositionState;
			
			var cm = new Dictionary<MethodKey, MemberMapping>();
			foreach (var m in textOutput.DebuggerMemberMappings) {
				var key = MethodKey.Create(m.MethodDefinition);
				if (key == null)
					continue;
				MemberMapping oldMm;
				if (cm.TryGetValue(key.Value, out oldMm))
					Debug.Assert(oldMm == m);
				else
					cm[key.Value] = m;
			}
			CodeMappings = cm;

			evt = OnShowOutput;
			if (evt != null)
				evt(this, new ShowOutputEventArgs(nodes, highlighting, state));
		}
		public Dictionary<MethodKey, MemberMapping> CodeMappings { get; private set; }
		public event EventHandler<ShowOutputEventArgs> OnBeforeShowOutput;
		public event EventHandler<ShowOutputEventArgs> OnShowOutput;
		public class ShowOutputEventArgs : EventArgs
		{
			public readonly ILSpyTreeNode[] Nodes;
			public readonly IHighlightingDefinition Highlighting;
			public readonly DecompilerTextViewState State;

			/// <summary>
			/// Code that moves the caret should set this to true if the caret was moved. Other code
			/// can then decide not to move the caret if it's already been moved.
			/// </summary>
			public bool HasMovedCaret;

			public ShowOutputEventArgs(ILSpyTreeNode[] nodes, IHighlightingDefinition highlighting, DecompilerTextViewState state)
			{
				this.Nodes = nodes;
				this.Highlighting = highlighting;
				this.State = state;
			}
		}
		#endregion
		
		#region Decompile (for display)
		// more than 5M characters is too slow to output (when user browses treeview)
		public const int DefaultOutputLengthLimit  =  5000000;
		
		// more than 75M characters can get us into trouble with memory usage
		public const int ExtendedOutputLengthLimit = 75000000;

		DecompilationContext nextDecompilationRun;

		/// <summary>
		/// Returns old context
		/// </summary>
		/// <returns></returns>
		internal void CancelDecompileAsync()
		{
			SetNextDecompilationRun(null);
		}

		bool CancelDecompileAsyncIf(DecompilationContext context)
		{
			var oldContext = Interlocked.CompareExchange(ref nextDecompilationRun, null, context);
			return oldContext != context;
		}

		/// <summary>
		/// Sets new context, and returns old context that has been canceled
		/// </summary>
		/// <param name="newContext">New context</param>
		/// <returns></returns>
		DecompilationContext SetNextDecompilationRun(DecompilationContext newContext)
		{
			var oldContext = Interlocked.CompareExchange(ref nextDecompilationRun, newContext, nextDecompilationRun);
			if (oldContext != null)
				oldContext.TaskCompletionSource.TrySetCanceled();
			return oldContext;
		}
		
		/// <summary>
		/// Starts the decompilation of the given nodes.
		/// The result is displayed in the text view.
		/// If any errors occur, the error message is displayed in the text view, and the task returned by this method completes successfully.
		/// If the operation is cancelled (by starting another decompilation action); the returned task is marked as cancelled.
		/// </summary>
		public Task DecompileAsync(ILSpy.Language language, IEnumerable<ILSpyTreeNode> treeNodes, DecompilationOptions options)
		{
			// Some actions like loading an assembly list cause several selection changes in the tree view,
			// and each of those will start a decompilation action.

			Interlocked.Increment(ref decompilationId);

			var newContext = new DecompilationContext(language, treeNodes.ToArray(), options);
			var textOutput = DecompileCache.Instance.Lookup(newContext.Language, newContext.TreeNodes, newContext.Options);
			if (textOutput != null) {
				CancelDecompilation();
				ShowOutput(textOutput, newContext.Language.SyntaxHighlighting, newContext.Options.TextViewState, newContext.TreeNodes);
				return TaskHelper.CompletedTask;
			}
			
			SetNextDecompilationRun(newContext);
			var task = newContext.TaskCompletionSource.Task;
			Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(
				delegate {
					bool canceled = CancelDecompileAsyncIf(newContext);
					if (!canceled)
						DoDecompile(newContext, DefaultOutputLengthLimit)
							.ContinueWith(t => newContext.TaskCompletionSource.SetFromTask(t)).HandleExceptions();
				}
			));
			return task;
		}

		int decompilationId;
		
		sealed class DecompilationContext
		{
			public readonly ILSpy.Language Language;
			public readonly ILSpyTreeNode[] TreeNodes;
			public readonly DecompilationOptions Options;
			public readonly TaskCompletionSource<object> TaskCompletionSource = new TaskCompletionSource<object>();
			
			public DecompilationContext(ILSpy.Language language, ILSpyTreeNode[] treeNodes, DecompilationOptions options)
			{
				this.Language = language;
				this.TreeNodes = treeNodes;
				this.Options = options;
			}
		}

		Task DoDecompile(DecompilationContext context, int outputLengthLimit)
		{
			if (this.IsVisible)
				MainWindow.Instance.ClosePopups();

			int id = Interlocked.Increment(ref decompilationId);

			return RunWithCancellation(
				delegate (CancellationToken ct) { // creation of the background task
					context.Options.CancellationToken = ct;
					return DecompileAsync(context, outputLengthLimit);
				})
			.Then(
				delegate (AvalonEditTextOutput textOutput) { // handling the result
					DecompileCache.Instance.Cache(context.Language, context.TreeNodes, context.Options, textOutput);
					if (id == decompilationId) {
						ShowOutput(textOutput, context.Language.SyntaxHighlighting, context.Options.TextViewState, context.TreeNodes);
					}
				})
			.Catch<Exception>(exception => {
					if (id == decompilationId) {
						textEditor.SyntaxHighlighting = null;
						Debug.WriteLine("Decompiler crashed: " + exception.ToString());
						AvalonEditTextOutput output = new AvalonEditTextOutput();
						if (exception is OutputLengthExceededException) {
							WriteOutputLengthExceededMessage(output, context, outputLengthLimit == DefaultOutputLengthLimit);
						}
						else {
							output.WriteLine(exception.ToString(), TextTokenType.Text);
						}
						ShowOutput(output);
					}
				});
		}

		internal bool GoToLocation(object destLoc)
		{
			if (destLoc == null)
				return false;

			if (destLoc is ICSharpCode.NRefactory.TextLocation) {
				var loc = (ICSharpCode.NRefactory.TextLocation)destLoc;
				ScrollAndMoveCaretTo(loc.Line, loc.Column);
				return true;
			}
			else if (destLoc is IMemberDef) {
				var member = destLoc as IMemberDef;
				ReferenceSegment refSeg = null;
				if (references != null) {
					foreach (var r in references) {
						if (r.IsLocalTarget && r.Reference == member) {
							refSeg = r;
							break;
						}
					}
				}
				return GoToTarget(refSeg, false, false);
			}
			else {
				Debug.Fail(string.Format("Unknown type: {0} = {1}", destLoc.GetType(), destLoc));
				return false;
			}
		}
		
		Task<AvalonEditTextOutput> DecompileAsync(DecompilationContext context, int outputLengthLimit)
		{
			//Debug.WriteLine("Start decompilation of {0} tree nodes", context.TreeNodes.Length);
			
			TaskCompletionSource<AvalonEditTextOutput> tcs = new TaskCompletionSource<AvalonEditTextOutput>();
			if (context.TreeNodes.Length == 0) {
				// If there's nothing to be decompiled, don't bother starting up a thread.
				// (Improves perf in some cases since we don't have to wait for the thread-pool to accept our task)
				tcs.SetResult(new AvalonEditTextOutput());
				return tcs.Task;
			}
			
			Thread thread = new Thread(new ThreadStart(
				delegate {
					#if DEBUG
					if (System.Diagnostics.Debugger.IsAttached) {
						try {
							AvalonEditTextOutput textOutput = new AvalonEditTextOutput();
							textOutput.LengthLimit = outputLengthLimit;
							DecompileNodes(context, textOutput);
							textOutput.PrepareDocument();
							tcs.SetResult(textOutput);
						} catch (OutputLengthExceededException ex) {
							tcs.SetException(ex);
						} catch (AggregateException ex) {
							tcs.SetException(ex.InnerExceptions);
						} catch (OperationCanceledException) {
							tcs.SetCanceled();
						}
					} else
						#endif
					{
						try {
							AvalonEditTextOutput textOutput = new AvalonEditTextOutput();
							textOutput.LengthLimit = outputLengthLimit;
							DecompileNodes(context, textOutput);
							textOutput.PrepareDocument();
							tcs.SetResult(textOutput);
						} catch (OperationCanceledException) {
							tcs.SetCanceled();
						} catch (Exception ex) {
							tcs.SetException(ex);
						}
					}
				}));
			thread.Start();
			return tcs.Task;
		}
		
		void DecompileNodes(DecompilationContext context, ITextOutput textOutput)
		{
			var nodes = context.TreeNodes;
			for (int i = 0; i < nodes.Length; i++) {
				if (i > 0)
					textOutput.WriteLine();
				
				context.Options.CancellationToken.ThrowIfCancellationRequested();
				nodes[i].Decompile(context.Language, textOutput, context.Options);
			}
		}
		#endregion
		
		#region WriteOutputLengthExceededMessage
		/// <summary>
		/// Creates a message that the decompiler output was too long.
		/// The message contains buttons that allow re-trying (with larger limit) or saving to a file.
		/// </summary>
		void WriteOutputLengthExceededMessage(ISmartTextOutput output, DecompilationContext context, bool wasNormalLimit)
		{
			if (wasNormalLimit) {
				output.WriteLine("You have selected too much code for it to be displayed automatically.", TextTokenType.Text);
			} else {
				output.WriteLine("You have selected too much code; it cannot be displayed here.", TextTokenType.Text);
			}
			output.WriteLine();
			if (wasNormalLimit) {
				output.AddButton(
					ImageCache.Instance.GetImage("ViewCode", BackgroundType.Button), "Display Code",
					delegate {
						DoDecompile(context, ExtendedOutputLengthLimit).HandleExceptions();
					});
				output.WriteLine();
			}
			
			output.AddButton(
				ImageCache.Instance.GetImage("Save", BackgroundType.Button), "Save Code",
				delegate {
					SaveToDisk(context.Language, context.TreeNodes, context.Options);
				});
			output.WriteLine();
		}
		#endregion

		#region JumpToReference
		/// <summary>
		/// Jumps to the definition referred to by the <see cref="ReferenceSegment"/>.
		/// </summary>
		internal void JumpToReference(ReferenceSegment referenceSegment, MouseEventArgs e)
		{
			if (Keyboard.Modifiers == ModifierKeys.Control) {
				MainWindow.Instance.SetActiveView(this);
				GoToMousePosition();
				MainWindow.Instance.OpenReferenceInNewTab(this, referenceSegment);
				e.Handled = true;
				return;
			}

			var localTarget = FindLocalTarget(referenceSegment);
			if (localTarget != null)
				referenceSegment = localTarget;

			int pos = -1;
			if (!referenceSegment.IsLocal) {
				if (referenceSegment.IsLocalTarget)
					pos = referenceSegment.EndOffset;
				if (pos < 0 && definitionLookup != null)
					pos = definitionLookup.GetDefinitionPosition(referenceSegment.Reference);
			}
			if (pos >= 0) {
				MainWindow.Instance.SetActiveView(this);
				GoToMousePosition();
				MainWindow.Instance.RecordHistory(this);
				MarkReferences(referenceSegment);
				MainWindow.Instance.SetTextEditorFocus(this);
				textEditor.Select(pos, 0);
				textEditor.ScrollTo(textEditor.TextArea.Caret.Line, textEditor.TextArea.Caret.Column);
				Dispatcher.Invoke(DispatcherPriority.Background, new Action(
					delegate {
						CaretHighlightAdorner.DisplayCaretHighlightAnimation(textEditor.TextArea);
					}));
				e.Handled = true;
				return;
			}

			if (referenceSegment.IsLocal && MarkReferences(referenceSegment)) {
				e.Handled = false;	// Allow another handler to set a new caret position
				return;
			}

			MainWindow.Instance.SetActiveView(this);
			GoToMousePosition();
			MainWindow.Instance.JumpToReference(this, referenceSegment.Reference);
			e.Handled = true;
			return;
		}

		bool MarkReferences(ReferenceSegment referenceSegment)
		{
			if (TextEditor.TextArea.TextView.Document == null)
				return false;
			if (previousReferenceSegment == referenceSegment)
				return true;
			object reference = referenceSegment.Reference;
			if (references == null || reference == null)
				return false;
			ClearMarkedReferences();
			previousReferenceSegment = referenceSegment;
			foreach (var tmp in references) {
				var r = tmp;
				if (RefSegEquals(referenceSegment, r)) {
					var mark = textMarkerService.Create(r.StartOffset, r.Length);
					mark.ZOrder = (int)Bookmarks.TextMarkerZOrder.SearchResult;
					mark.HighlightingColor = () => {
						return (r.IsLocalTarget ?
							Themes.Theme.GetColor(dntheme.ColorType.LocalDefinition) :
							Themes.Theme.GetColor(dntheme.ColorType.LocalReference)).TextInheritedColor;
					};
					markedReferences.Add(mark);
				}
			}
			return true;
		}
		ReferenceSegment previousReferenceSegment = null;

		void ClearMarkedReferences()
		{
			foreach (var mark in markedReferences) {
				textMarkerService.Remove(mark);
			}
			markedReferences.Clear();
			previousReferenceSegment = null;
		}
		
		/// <summary>
		/// Filters all ReferenceSegments that are no real links.
		/// </summary>
		bool IsLink(ReferenceSegment referenceSegment)
		{
			return true;
		}
		#endregion
		
		#region SaveToDisk
		/// <summary>
		/// Shows the 'save file dialog', prompting the user to save the decompiled nodes to disk.
		/// </summary>
		public void SaveToDisk(ILSpy.Language language, IEnumerable<ILSpyTreeNode> treeNodes, DecompilationOptions options)
		{
			if (!treeNodes.Any())
				return;
			
			SaveFileDialog dlg = new SaveFileDialog();
			dlg.DefaultExt = language.FileExtension;
			dlg.Filter = language.Name + "|*" + language.FileExtension + "|All Files|*.*";
			dlg.FileName = CleanUpName(treeNodes.First().ToString()) + language.FileExtension;
			if (dlg.ShowDialog() == true) {
				SaveToDisk(new DecompilationContext(language, treeNodes.ToArray(), options), dlg.FileName);
			}
		}
		
		public void SaveToDisk(ILSpy.Language language, IEnumerable<ILSpyTreeNode> treeNodes, DecompilationOptions options, string fileName)
		{
			SaveToDisk(new DecompilationContext(language, treeNodes.ToArray(), options), fileName);
		}
		
		/// <summary>
		/// Starts the decompilation of the given nodes.
		/// The result will be saved to the given file name.
		/// </summary>
		void SaveToDisk(DecompilationContext context, string fileName)
		{
			RunWithCancellation(
				delegate (CancellationToken ct) {
					context.Options.CancellationToken = ct;
					return SaveToDiskAsync(context, fileName);
				})
				.Catch((Exception ex) => {
					textEditor.SyntaxHighlighting = null;
					Debug.WriteLine("Decompiler crashed: " + ex.ToString());
					// Unpack aggregate exceptions as long as there's only a single exception:
					// (assembly load errors might produce nested aggregate exceptions)
					AvalonEditTextOutput output = new AvalonEditTextOutput();
					output.WriteLine(ex.ToString(), TextTokenType.Text);
					ShowOutput(output);
				}).HandleExceptions();
		}

		Task<int> SaveToDiskAsync(DecompilationContext context, string fileName)
		{
			TaskCompletionSource<int> tcs = new TaskCompletionSource<int>();
			Thread thread = new Thread(new ThreadStart(
				delegate {
					try {
						Stopwatch stopwatch = new Stopwatch();
						stopwatch.Start();
						using (StreamWriter w = new StreamWriter(fileName)) {
							try {
								DecompileNodes(context, new PlainTextOutput(w));
							} catch (OperationCanceledException) {
								w.WriteLine();
								w.WriteLine("Decompilation was canceled.");
								throw;
							}
						}
						stopwatch.Stop();
						tcs.SetResult(0);
					} catch (OperationCanceledException) {
						tcs.SetCanceled();
						#if DEBUG
					} catch (AggregateException ex) {
						tcs.SetException(ex);
						#else
					} catch (Exception ex) {
						tcs.SetException(ex);
						#endif
					}
				}));
			thread.Start();
			return tcs.Task;
		}
		
		static HashSet<string> ReservedFileNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase) {
			"CON", "PRN", "AUX", "NUL",
			"COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
			"LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9",
		};

		/// <summary>
		/// Cleans up a node name for use as a file name.
		/// </summary>
		internal static string CleanUpName(string text)
		{
			int pos = text.IndexOf(':');
			if (pos > 0)
				text = text.Substring(0, pos);
			pos = text.IndexOf('`');
			if (pos > 0)
				text = text.Substring(0, pos);
			text = text.Trim();
			foreach (char c in Path.GetInvalidFileNameChars())
				text = text.Replace(c, '-');
			if (ReservedFileNames.Contains(text))
				text = "__" + text + "__";
			return text;
		}
		#endregion

		internal ReferenceSegment GetReferenceSegmentAt(TextViewPosition? position)
		{
			if (position == null)
				return null;
			int offset = textEditor.Document.GetOffset(position.Value.Location);
			return GetReferenceSegmentAt(offset);
		}

		ReferenceSegment GetReferenceSegmentAt(int offset)
		{
			if (referenceElementGenerator == null || referenceElementGenerator.References == null)
				return null;
			var segs = referenceElementGenerator.References.FindSegmentsContaining(offset).ToArray();
			foreach (var seg in segs) {
				if (seg.StartOffset <= offset && offset < seg.EndOffset)
					return seg;
			}
			return segs.Length == 0 ? null : segs[0];
		}
		
		internal TextViewPosition? GetPositionFromMousePosition()
		{
			return textEditor.TextArea.TextView.GetPosition(Mouse.GetPosition(textEditor.TextArea.TextView) + textEditor.TextArea.TextView.ScrollOffset);
		}

		public DecompilerTextViewState GetState(ILSpyTreeNode[] nodes)
		{
			if (nodes == null || nodes.Length == 0)
				return null;

			var state = new DecompilerTextViewState();
			if (foldingManager != null)
				state.SaveFoldingsState(foldingManager.AllFoldings);
			state.EditorPositionState = new EditorPositionState(textEditor);
			state.DecompiledNodes = nodes;
			return state;
		}
		
		public void Dispose()
		{
			DisplaySettingsPanel.CurrentDisplaySettings.PropertyChanged -= CurrentDisplaySettings_PropertyChanged;
		}

		public void ScrollAndMoveCaretTo(int line, int column, bool focus = true)
		{
			// Make sure the lines have been re-initialized or the ScrollTo() method could fail
			TextEditor.TextArea.TextView.EnsureVisualLines();
			TextEditor.ScrollTo(line, column);
			SetCaretPosition(line, column);
			if (focus)
				MainWindow.Instance.SetTextEditorFocus(this);
		}

		void SetCaretPosition(int line, int column, double desiredXPos = double.NaN)
		{
			TextEditor.TextArea.Caret.Location = new ICSharpCode.AvalonEdit.Document.TextLocation(line, column);
			TextEditor.TextArea.Caret.DesiredXPos = desiredXPos;
		}

		static Point FilterCaretPos(ICSharpCode.AvalonEdit.Rendering.TextView textView, Point pt)
		{
			Point firstPos;
			if (textView.VisualLines.Count == 0)
				firstPos = new Point(0, 0);
			else {
				var line = textView.VisualLines[0];
				if (line.VisualTop < textView.VerticalOffset && textView.VisualLines.Count > 1)
					line = textView.VisualLines[1];
				firstPos = line.GetVisualPosition(0, VisualYPosition.LineMiddle);
			}

			Point lastPos;
			if (textView.VisualLines.Count == 0)
				lastPos = new Point(0, 0);
			else {
				var line = textView.VisualLines[textView.VisualLines.Count - 1];
				if (line.VisualTop - textView.VerticalOffset + line.Height > textView.ActualHeight && textView.VisualLines.Count > 1)
					line = textView.VisualLines[textView.VisualLines.Count - 2];
				lastPos = line.GetVisualPosition(0, VisualYPosition.LineMiddle);
			}

			if (pt.Y < firstPos.Y)
				return new Point(pt.X, firstPos.Y);
			else if (pt.Y > lastPos.Y)
				return new Point(pt.X, lastPos.Y);
			return pt;
		}

		void TextEditor_PreviewKeyDown(object sender, KeyEventArgs e)
		{
			if (Keyboard.Modifiers == ModifierKeys.None && (e.Key == Key.PageDown || e.Key == Key.PageUp)) {
				var textView = TextEditor.TextArea.TextView;
				var si = (System.Windows.Controls.Primitives.IScrollInfo)textView;

				// Re-use the existing code in AvalonEdit
				var cmd = e.Key == Key.PageDown ? EditingCommands.MoveDownByPage : EditingCommands.MoveUpByPage;
				var target = textView;
				bool canExec = cmd.CanExecute(null, target);
				Debug.Assert(canExec);
				if (canExec) {
					if (e.Key == Key.PageDown)
						si.PageDown();
					else
						si.PageUp();

					cmd.Execute(null, target);
					e.Handled = true;
				}
				return;
			}
		}

		void PageUp()
		{
			var textView = TextEditor.TextArea.TextView;
			textView.EnsureVisualLines();
			if (textView.VisualLines.Count > 0) {
				var line = textView.VisualLines[0];
				// If the full height isn't visible, pick the next one
				if (line.VisualTop < textView.VerticalOffset && textView.VisualLines.Count > 1)
					line = textView.VisualLines[1];
				var docLine = line.FirstDocumentLine;
				var caret = TextEditor.TextArea.Caret;
				SetCaretPosition(docLine.LineNumber, caret.Location.Column);
			}
		}

		void PageDown()
		{
			var textView = TextEditor.TextArea.TextView;
			textView.EnsureVisualLines();
			if (textView.VisualLines.Count > 0) {
				var line = textView.VisualLines[textView.VisualLines.Count - 1];
				// If the full height isn't visible, pick the previous one
				if (line.VisualTop - textView.VerticalOffset + line.Height > textView.ActualHeight && textView.VisualLines.Count > 1)
					line = textView.VisualLines[textView.VisualLines.Count - 2];
				var docLine = line.LastDocumentLine;
				var caret = TextEditor.TextArea.Caret;
				SetCaretPosition(docLine.LineNumber, caret.Location.Column);
			}
		}

		void UpDownLine(bool up)
		{
			var textView = TextEditor.TextArea.TextView;
			var scrollViewer = ((System.Windows.Controls.Primitives.IScrollInfo)textView).ScrollOwner;
			textView.EnsureVisualLines();

			var currPos = FilterCaretPos(textView, textView.GetVisualPosition(TextEditor.TextArea.Caret.Position, VisualYPosition.LineMiddle));

			if (!up)
				scrollViewer.LineDown();
			else
				scrollViewer.LineUp();
			textView.UpdateLayout();
			textView.EnsureVisualLines();

			var newPos = FilterCaretPos(textView, currPos);
			var newVisPos = textView.GetPosition(newPos);
			Debug.Assert(newVisPos != null);
			if (newVisPos != null)
				TextEditor.TextArea.Caret.Position = newVisPos.Value;
		}

		void FollowReference()
		{
			int offset = textEditor.TextArea.Caret.Offset;
			var refSeg = GetReferenceSegmentAt(offset);
			GoToTarget(refSeg, true, true);
		}

		void FollowReferenceNewTab()
		{
			int offset = textEditor.TextArea.Caret.Offset;
			var refSeg = GetReferenceSegmentAt(offset);
			MainWindow.Instance.OpenReferenceInNewTab(this, refSeg);
		}

		void ClearMarkedReferencesAndPopups()
		{
			ClearMarkedReferences();
			MainWindow.Instance.ClosePopups();
		}

		void MoveReference(bool forward)
		{
			if (references == null)
				return;
			int offset = textEditor.TextArea.Caret.Offset;
			var refSeg = GetReferenceSegmentAt(offset);
			if (refSeg == null)
				return;

			foreach (var newSeg in GetReferenceSegmentsFrom(refSeg, forward)) {
				if (RefSegEquals(newSeg, refSeg)) {
					var line = textEditor.Document.GetLineByOffset(newSeg.StartOffset);
					int column = newSeg.StartOffset - line.Offset + 1;
					ScrollAndMoveCaretTo(line.LineNumber, column);
					break;
				}
			}
		}

		IEnumerable<ReferenceSegment> GetReferenceSegmentsFrom(ReferenceSegment refSeg, bool forward)
		{
			if (references == null || refSeg == null)
				yield break;

			var currSeg = refSeg;
			while (true) {
				currSeg = forward ? references.GetNextSegment(currSeg) : references.GetPreviousSegment(currSeg);
				if (currSeg == null)
					currSeg = forward ? references.FirstSegment : references.LastSegment;
				if (currSeg == refSeg)
					break;

				yield return currSeg;
			}
		}

		internal bool GoToTarget(ReferenceSegment refSeg, bool canJumpToReference, bool canRecordHistory)
		{
			if (refSeg == null)
				return false;
			var localTarget = FindLocalTarget(refSeg);
			if (localTarget != null)
				refSeg = localTarget;

			if (refSeg.IsLocalTarget) {
				if (canRecordHistory)
					MainWindow.Instance.RecordHistory(this);
				var line = textEditor.Document.GetLineByOffset(refSeg.StartOffset);
				int column = refSeg.StartOffset - line.Offset + 1;
				ScrollAndMoveCaretTo(line.LineNumber, column);
				return true;
			}

			if (refSeg.IsLocal)
				return false;
			if (canJumpToReference) {
				MainWindow.Instance.JumpToReference(this, refSeg.Reference, canRecordHistory);
				return true;
			}

			return false;
		}

		ReferenceSegment FindLocalTarget(ReferenceSegment refSeg)
		{
			if (references == null)
				return null;
			if (refSeg.IsLocalTarget)
				return refSeg;
			foreach (var r in references) {
				if (r.IsLocalTarget && RefSegEquals(r, refSeg))
					return r;
			}
			return null;
		}

		static bool RefSegEquals(ReferenceSegment a, ReferenceSegment b)
		{
			if (a == b)
				return true;
			if (a == null || b == null)
				return false;
			if (a.Reference == b.Reference)
				return true;
			if (a.Reference == null || b.Reference == null)
				return false;

			var ma = a.Reference as IMemberRef;
			var mb = b.Reference as IMemberRef;
			if (ma != null && mb != null) {
				ma = Resolve(ma) ?? ma;
				mb = Resolve(mb) ?? mb;
				return new SigComparer(SigComparerOptions.CompareDeclaringTypes | SigComparerOptions.PrivateScopeIsComparable).Equals(ma, mb);
			}

			// Labels are strings, but the strings might not be the same reference, so make sure
			// to do the comparison as strings.
			var sa = a.Reference as string;
			var sb = b.Reference as string;
			if (sa != null && sb != null)
				return sa == sb;

			return false;
		}

		public EditorPositionState EditorPositionState {
			get {
				return new EditorPositionState(textEditor);
			}
			set {
				textEditor.ScrollToVerticalOffset(value.VerticalOffset);
				textEditor.ScrollToHorizontalOffset(value.HorizontalOffset);
				textEditor.TextArea.Caret.Position = value.TextViewPosition;
				textEditor.TextArea.Caret.DesiredXPos = value.DesiredXPos;
			}
		}

		public void GoToMousePosition()
		{
			var pos = GetPositionFromMousePosition();
			if (pos != null) {
				var textArea = textEditor.TextArea;
				textArea.Caret.Position = pos.Value;
				textArea.Caret.DesiredXPos = double.NaN;
			}
		}
	}

	public class DecompilerTextViewState : IEquatable<DecompilerTextViewState>
	{
		private List<Tuple<int, int>> ExpandedFoldings;
		private int FoldingsChecksum;
		public EditorPositionState EditorPositionState;
		public ILSpyTreeNode[] DecompiledNodes;

		public void SaveFoldingsState(IEnumerable<FoldingSection> foldings)
		{
			ExpandedFoldings = foldings.Where(f => !f.IsFolded).Select(f => Tuple.Create(f.StartOffset, f.EndOffset)).ToList();
			FoldingsChecksum = unchecked(foldings.Select(f => f.StartOffset * 3 - f.EndOffset).Aggregate((a, b) => a + b));
		}

		internal void RestoreFoldings(List<NewFolding> list)
		{
			var checksum = unchecked(list.Select(f => f.StartOffset * 3 - f.EndOffset).Aggregate((a, b) => a + b));
			if (FoldingsChecksum == checksum)
				foreach (var folding in list)
					folding.DefaultClosed = !ExpandedFoldings.Any(f => f.Item1 == folding.StartOffset && f.Item2 == folding.EndOffset);
		}

		public bool Equals(DecompilerTextViewState other)
		{
			if (other == null)
				return false;
			return EditorPositionState.Equals(other.EditorPositionState) &&
				Equals(DecompiledNodes, other.DecompiledNodes);
		}

		static bool Equals(ILSpyTreeNode[] a, ILSpyTreeNode[] b)
		{
			if (a == b)
				return true;
			if (a == null || b == null)
				return false;
			if (a.Length != b.Length)
				return false;
			for (int i = 0; i < a.Length; i++) {
				if (a[i] != b[i])
					return false;
			}
			return true;
		}
	}
}
