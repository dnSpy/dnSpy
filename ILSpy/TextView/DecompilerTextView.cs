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
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
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
using ICSharpCode.ILSpy.AvalonEdit;
using ICSharpCode.ILSpy.Bookmarks;
using ICSharpCode.ILSpy.Debugger;
using ICSharpCode.ILSpy.Debugger.Bookmarks;
using ICSharpCode.ILSpy.Debugger.Services;
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
	[Export, PartCreationPolicy(CreationPolicy.Shared)]
	public sealed partial class DecompilerTextView : UserControl, IDisposable
	{
		readonly ReferenceElementGenerator referenceElementGenerator;
		readonly UIElementGenerator uiElementGenerator;
		List<VisualLineElementGenerator> activeCustomElementGenerators = new List<VisualLineElementGenerator>();
		FoldingManager foldingManager;
		ILSpyTreeNode[] decompiledNodes;
		
		DefinitionLookup definitionLookup;
		TextSegmentCollection<ReferenceSegment> references;
		CancellationTokenSource currentCancellationTokenSource;
		
		internal readonly IconBarManager manager;
		readonly IconBarMargin iconMargin;
		readonly TextMarkerService textMarkerService;
		readonly List<ITextMarker> localReferenceMarks = new List<ITextMarker>();

		readonly SearchPanel searchPanel;
		
		[ImportMany(typeof(ITextEditorListener))]
		IEnumerable<ITextEditorListener> textEditorListeners = null;

		public TextEditor TextEditor {
			get { return textEditor; }
		}
		
		#region Constructor
		public DecompilerTextView()
		{
			HighlightingManager.Instance.RegisterHighlighting(
				"ILAsm", new string[] { ".il" },
				delegate {
					using (Stream s = typeof(DecompilerTextView).Assembly.GetManifestResourceStream(typeof(DecompilerTextView), "ILAsm-Mode.xshd")) {
						using (XmlTextReader reader = new XmlTextReader(s)) {
							return HighlightingLoader.Load(reader, HighlightingManager.Instance);
						}
					}
				});
			
			this.Loaded+= new RoutedEventHandler(DecompilerTextView_Loaded);
			InitializeComponent();
			
			this.referenceElementGenerator = new ReferenceElementGenerator(this.JumpToReference, this.IsLink);
			textEditor.TextArea.TextView.ElementGenerators.Add(referenceElementGenerator);
			textEditor.PreviewKeyDown += TextEditor_PreviewKeyDown;
			this.uiElementGenerator = new UIElementGenerator();
			textEditor.TextArea.TextView.ElementGenerators.Add(uiElementGenerator);
			textEditor.Options.RequireControlModifierForHyperlinkClick = false;
			textEditor.TextArea.TextView.MouseHover += TextViewMouseHover;
			textEditor.TextArea.TextView.MouseHoverStopped += TextViewMouseHoverStopped;
			textEditor.SetBinding(Control.FontFamilyProperty, new Binding { Source = DisplaySettingsPanel.CurrentDisplaySettings, Path = new PropertyPath("SelectedFont") });
			textEditor.SetBinding(Control.FontSizeProperty, new Binding { Source = DisplaySettingsPanel.CurrentDisplaySettings, Path = new PropertyPath("SelectedFontSize") });
			
			// add marker service & margin
			iconMargin = new IconBarMargin((manager = new IconBarManager()));
			textMarkerService = new TextMarkerService(textEditor.TextArea.TextView);
			textEditor.TextArea.TextView.BackgroundRenderers.Add(textMarkerService);
			textEditor.TextArea.TextView.LineTransformers.Add(textMarkerService);
			textEditor.ShowLineNumbers = true;
			DisplaySettingsPanel.CurrentDisplaySettings.PropertyChanged += CurrentDisplaySettings_PropertyChanged;

			// SearchPanel
			searchPanel = SearchPanel.Install(textEditor.TextArea);
			searchPanel.RegisterCommands(Application.Current.MainWindow.CommandBindings);
			
			textEditor.TextArea.LeftMargins.Insert(0, iconMargin);
			textEditor.TextArea.TextView.VisualLinesChanged += delegate { iconMargin.InvalidateVisual(); };
			
			// Bookmarks context menu
			IconMarginActionsProvider.Add(iconMargin);
			
			this.Loaded += new RoutedEventHandler(DecompilerTextView_Loaded);
		}

		void DecompilerTextView_Loaded(object sender, RoutedEventArgs e)
		{
			ShowLineMargin();
			
			// wire the events
			if (textEditorListeners != null) {
				foreach (var listener in textEditorListeners) {
					ICSharpCode.ILSpy.AvalonEdit.TextEditorWeakEventManager.MouseHover.AddListener(textEditor, listener);
					ICSharpCode.ILSpy.AvalonEdit.TextEditorWeakEventManager.MouseHoverStopped.AddListener(textEditor, listener);
					ICSharpCode.ILSpy.AvalonEdit.TextEditorWeakEventManager.MouseDown.AddListener(textEditor, listener);
				}
			}
			textEditor.TextArea.TextView.VisualLinesChanged += (s, _) => iconMargin.InvalidateVisual();
			
			// add marker service & margin
			textEditor.TextArea.TextView.BackgroundRenderers.Add(textMarkerService);
			textEditor.TextArea.TextView.LineTransformers.Add(textMarkerService);
		}
		
		#endregion

		internal void OnThemeUpdated()
		{
			textEditor.OnThemeUpdated();
			var theme = MainWindow.Instance.Theme;
			var marker = theme.GetColor(ColorType.SearchResultMarker).InheritedColor;
			searchPanel.MarkerBrush = marker.Background == null ? Brushes.LightGreen : marker.Background.GetBrush(null);
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
			if (tooltip != null)
				tooltip.IsOpen = false;
		}

		void TextViewMouseHover(object sender, MouseEventArgs e)
		{
			TextViewPosition? position = textEditor.TextArea.TextView.GetPosition(e.GetPosition(textEditor.TextArea.TextView) + textEditor.TextArea.TextView.ScrollOffset);
			if (position == null)
				return;
			int offset = textEditor.Document.GetOffset(position.Value.Location);
			ReferenceSegment seg = referenceElementGenerator.References.FindSegmentsContaining(offset).FirstOrDefault();
			if (seg == null)
				return;
			object content = GenerateTooltip(seg);
			if (tooltip != null)
				tooltip.IsOpen = false;
			if (content != null)
				tooltip = new ToolTip() { Content = content, IsOpen = true };
		}
		
		object GenerateTooltip(ReferenceSegment segment)
		{
			if (segment.Reference is OpCode) {
				OpCode code = (OpCode)segment.Reference;
				var s = ILLanguage.GetOpCodeDocumentation(code);
				string opCodeHex = code.Size > 1 ? string.Format("0x{0:x4}", code.Value) : string.Format("0x{0:x2}", code.Value);
				if (s != null)
					return new TextBlock { Text = string.Format("{0} ({1}) - {2}", code.Name, opCodeHex, s) };
				return string.Format("{0} ({1})", code.Name, opCodeHex);
			} else if (segment.Reference is IMemberRef) {
				IMemberRef mr = (IMemberRef)segment.Reference;
				// if possible, resolve the reference
				if (mr is ITypeDefOrRef) {
					mr = ((ITypeDefOrRef)mr).ResolveTypeDef() ?? mr;
				} else if (mr is IMethod && ((IMethod)mr).IsMethod) {
					mr = ((IMethod)mr).ResolveMethodDef() ?? mr;
				} else if (mr is IField && ((IField)mr).IsField) {
					mr = ((IField)mr).ResolveFieldDef() ?? mr;
				}
				XmlDocRenderer renderer = new XmlDocRenderer();
				renderer.AppendText(MainWindow.Instance.CurrentLanguage.GetTooltip(mr));
				try {
					XmlDocumentationProvider docProvider = XmlDocLoader.LoadDocumentation(mr.Module);
					if (docProvider != null) {
						string documentation = GetDocumentation(docProvider, mr);
						if (documentation != null) {
							renderer.AppendText(Environment.NewLine);
							renderer.AddXmlDocumentation(documentation);
						}
					}
				} catch (XmlException) {
					// ignore
				}
				return renderer.CreateTextBlock();
			}
			return null;
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
		/// <summary>
		/// Switches the GUI into "waiting" mode, then calls <paramref name="taskCreation"/> to create
		/// the task.
		/// When the task completes without being cancelled, the <paramref name="taskCompleted"/>
		/// callback is called on the GUI thread.
		/// When the task is cancelled before completing, the callback is not called; and any result
		/// of the task (including exceptions) are ignored.
		/// </summary>
		[Obsolete("RunWithCancellation(taskCreation).ContinueWith(taskCompleted) instead")]
		public void RunWithCancellation<T>(Func<CancellationToken, Task<T>> taskCreation, Action<Task<T>> taskCompleted)
		{
			RunWithCancellation(taskCreation).ContinueWith(taskCompleted, CancellationToken.None, TaskContinuationOptions.NotOnCanceled, TaskScheduler.FromCurrentSynchronizationContext());
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
			// Cancel the decompilation task:
			if (currentCancellationTokenSource != null) {
				currentCancellationTokenSource.Cancel();
				currentCancellationTokenSource = null; // prevent canceled task from producing output
			}
			if (this.nextDecompilationRun != null) {
				// remove scheduled decompilation run
				this.nextDecompilationRun.TaskCompletionSource.TrySetCanceled();
				this.nextDecompilationRun = null;
			}
			ShowOutput(textOutput, highlighting);
			decompiledNodes = nodes;
		}
		
		/// <summary>
		/// Shows the given output in the text view.
		/// </summary>
		void ShowOutput(AvalonEditTextOutput textOutput, IHighlightingDefinition highlighting = null, DecompilerTextViewState state = null)
		{
			Debug.WriteLine("Showing {0} characters of output", textOutput.TextLength);
			Stopwatch w = Stopwatch.StartNew();
			textEditor.LanguageTokens = textOutput.tokens;
			textEditor.LanguageTokens.Finish();

			ClearLocalReferenceMarks();
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
			
			Debug.WriteLine("  Set-up: {0}", w.Elapsed); w.Restart();
			textEditor.Document = textOutput.GetDocument();
			Debug.WriteLine("  Assigning document: {0}", w.Elapsed); w.Restart();
			if (textOutput.Foldings.Count > 0) {
				if (state != null) {
					state.RestoreFoldings(textOutput.Foldings);
				}
				foldingManager = FoldingManager.Install(textEditor.TextArea);
				foldingManager.UpdateFoldings(textOutput.Foldings.OrderBy(f => f.StartOffset), -1);
				Debug.WriteLine("  Updating folding: {0}", w.Elapsed); w.Restart();
			}
			if (state != null) {
				textEditor.ScrollToVerticalOffset(state.VerticalOffset);
				textEditor.ScrollToHorizontalOffset(state.HorizontalOffset);
				textEditor.TextArea.Caret.Position = state.TextViewPosition;
				textEditor.TextArea.Caret.DesiredXPos = state.DesiredXPos;
			}
			
			if (DisplaySettingsPanel.CurrentDisplaySettings.AutoFocusTextView)
				textEditor.Focus();

			// update debugger info
			DebugInformation.CodeMappings = textOutput.DebuggerMemberMappings.ToDictionary(m => new MethodKey(m.MethodDefinition));
			UpdateDebugUI(state);
			
			// update class bookmarks
			var document = textEditor.Document;
			manager.Bookmarks.Clear();
			foreach (var pair in textOutput.DefinitionLookup.definitions) {
				IMemberRef member = pair.Key as IMemberRef;
				int offset = pair.Value;
				if (member != null) {
					var location = document.GetLocation(offset);
					manager.Bookmarks.Add(new MemberBookmark(member, location));
				}
			}
		}
		#endregion
		
		#region Decompile (for display)
		// more than 5M characters is too slow to output (when user browses treeview)
		public const int DefaultOutputLengthLimit  =  5000000;
		
		// more than 75M characters can get us into trouble with memory usage
		public const int ExtendedOutputLengthLimit = 75000000;
		
		DecompilationContext nextDecompilationRun;
		
		[Obsolete("Use DecompileAsync() instead")]
		public void Decompile(ILSpy.Language language, IEnumerable<ILSpyTreeNode> treeNodes, DecompilationOptions options)
		{
			DecompileAsync(language, treeNodes, options).HandleExceptions();
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
			
			bool isDecompilationScheduled = this.nextDecompilationRun != null;
			if (this.nextDecompilationRun != null)
				this.nextDecompilationRun.TaskCompletionSource.TrySetCanceled();
			this.nextDecompilationRun = new DecompilationContext(language, treeNodes.ToArray(), options);
			var task = this.nextDecompilationRun.TaskCompletionSource.Task;
			if (!isDecompilationScheduled) {
				Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(
					delegate {
						var context = this.nextDecompilationRun;
						this.nextDecompilationRun = null;
						if (context != null)
							DoDecompile(context, DefaultOutputLengthLimit)
								.ContinueWith(t => context.TaskCompletionSource.SetFromTask(t)).HandleExceptions();
					}
				));
			}
			return task;
		}
		
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
			// close popup
			if (textEditorListeners != null) {
				foreach (var listener in textEditorListeners) {
					listener.ClosePopup();
				}
			}

			return RunWithCancellation(
				delegate (CancellationToken ct) { // creation of the background task
					context.Options.CancellationToken = ct;
					return DecompileAsync(context, outputLengthLimit);
				})
			.Then(
				delegate (AvalonEditTextOutput textOutput) { // handling the result
					ShowOutput(textOutput, context.Language.SyntaxHighlighting, context.Options.TextViewState);
					decompiledNodes = context.TreeNodes;
				})
			.Catch<Exception>(exception => {
					textEditor.SyntaxHighlighting = null;
					Debug.WriteLine("Decompiler crashed: " + exception.ToString());
					AvalonEditTextOutput output = new AvalonEditTextOutput();
					if (exception is OutputLengthExceededException) {
						WriteOutputLengthExceededMessage(output, context, outputLengthLimit == DefaultOutputLengthLimit);
					} else {
						output.WriteLine(exception.ToString(), TextTokenType.Text);
					}
					ShowOutput(output);
					decompiledNodes = context.TreeNodes;
				});
		}
		
		void UpdateDebugUI(DecompilerTextViewState state)
		{
			// sync bookmarks
			iconMargin.SyncBookmarks();

			bool updateDebugInfo =
					DebuggerService.CurrentDebugger != null &&
					DebuggerService.CurrentDebugger.IsDebugging &&
					DebugInformation.DebugStepInformation != null &&
					DebugInformation.DebugStepInformation.Item2 != int.MaxValue;
			if (updateDebugInfo) {
				// repaint bookmarks
				iconMargin.InvalidateVisual();

				// show the currentline marker
				var key = DebugInformation.DebugStepInformation.Item1;
				int ilOffset = DebugInformation.DebugStepInformation.Item2;
				ICSharpCode.NRefactory.TextLocation location, endLocation;
				var cm = DebugInformation.CodeMappings;
				if (cm != null && cm.ContainsKey(key) &&
					cm[key].GetInstructionByTokenAndOffset((uint)ilOffset, out location, out endLocation)) {
					if (state == null)
						ScrollAndMoveCaretTo(location.Line, location.Column);

					UnfoldAndScroll(location.Line, false);
				}
			}

			StackFrameStatementBookmark.UpdateReturnStatementBookmarks(false);

			if (state == null && DebugInformation.JumpToThisLine != null) {
				if (DebugInformation.JumpToThisLine is ICSharpCode.NRefactory.TextLocation) {
					var loc = (ICSharpCode.NRefactory.TextLocation)DebugInformation.JumpToThisLine;
					ScrollAndMoveCaretTo(loc.Line, loc.Column);
				}
				else if (DebugInformation.JumpToThisLine is Tuple<MethodKey, int>) {
					var tuple = (Tuple<MethodKey, int>)DebugInformation.JumpToThisLine;
					var key = tuple.Item1;
					int offset = tuple.Item2;
					var cm = DebugInformation.CodeMappings;
					ICSharpCode.NRefactory.TextLocation location, endLocation;
					if (cm != null && cm.ContainsKey(key) &&
						cm[key].GetInstructionByTokenAndOffset((uint)offset, out location, out endLocation)) {
						ScrollAndMoveCaretTo(location.Line, location.Column);
					}
				}
				else {
					Debug.Fail(string.Format("Unknown type: {0} = {1}", DebugInformation.JumpToThisLine.GetType(), DebugInformation.JumpToThisLine));
				}
			}
			DebugInformation.JumpToThisLine = null;
		}
		
		Task<AvalonEditTextOutput> DecompileAsync(DecompilationContext context, int outputLengthLimit)
		{
			Debug.WriteLine("Start decompilation of {0} tree nodes", context.TreeNodes.Length);
			
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
					Images.ViewCode, "Display Code",
					delegate {
						DoDecompile(context, ExtendedOutputLengthLimit).HandleExceptions();
					});
				output.WriteLine();
			}
			
			output.AddButton(
				Images.Save, "Save Code",
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
		internal bool JumpToReference(ReferenceSegment referenceSegment)
		{
			var localTarget = FindLocalTarget(referenceSegment);
			if (localTarget != null)
				referenceSegment = localTarget;

			int pos = -1;
			if (referenceSegment.IsLocalTarget)
				pos = referenceSegment.EndOffset;
			if (pos < 0 && definitionLookup != null)
				pos = definitionLookup.GetDefinitionPosition(referenceSegment.Reference);
			if (pos >= 0) {
				MarkLocals(referenceSegment);
				textEditor.TextArea.Focus();
				textEditor.Select(pos, 0);
				textEditor.ScrollTo(textEditor.TextArea.Caret.Line, textEditor.TextArea.Caret.Column);
				Dispatcher.Invoke(DispatcherPriority.Background, new Action(
					delegate {
						CaretHighlightAdorner.DisplayCaretHighlightAnimation(textEditor.TextArea);
					}));
				return true;
			}

			if (MarkLocals(referenceSegment))
				return true;

			MainWindow.Instance.JumpToReference(referenceSegment.Reference);
			return true;
		}

		bool MarkLocals(ReferenceSegment referenceSegment)
		{
			object reference = referenceSegment.Reference;
			if (referenceSegment.IsLocal) {
				ClearLocalReferenceMarks();
				if (references != null && reference != null) {
					foreach (var r in references) {
						if (RefSegEquals(referenceSegment, r)) {
							var mark = textMarkerService.Create(r.StartOffset, r.Length);
							mark.HighlightingColor = () => {
								return (r.IsLocalTarget ?
									MainWindow.Instance.Theme.GetColor(dntheme.ColorType.LocalDefinition) :
									MainWindow.Instance.Theme.GetColor(dntheme.ColorType.LocalReference)).TextInheritedColor;
							};
							localReferenceMarks.Add(mark);
						}
					}
				}
				return true;
			}

			return false;
		}

		void ClearLocalReferenceMarks()
		{
			foreach (var mark in localReferenceMarks) {
				textMarkerService.Remove(mark);
			}
			localReferenceMarks.Clear();
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
				.Then(output => ShowOutput(output))
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

		Task<AvalonEditTextOutput> SaveToDiskAsync(DecompilationContext context, string fileName)
		{
			TaskCompletionSource<AvalonEditTextOutput> tcs = new TaskCompletionSource<AvalonEditTextOutput>();
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
								w.WriteLine("Decompiled was cancelled.");
								throw;
							}
						}
						stopwatch.Stop();
						AvalonEditTextOutput output = new AvalonEditTextOutput();
						output.WriteLine("Decompilation complete in " + stopwatch.Elapsed.TotalSeconds.ToString("F1") + " seconds.", TextTokenType.Text);
						output.WriteLine();
						output.AddButton(null, "Open Explorer", delegate { Process.Start("explorer", "/select,\"" + fileName + "\""); });
						output.WriteLine();
						tcs.SetResult(output);
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

		internal ReferenceSegment GetReferenceSegmentAtMousePosition()
		{
			TextViewPosition? position = GetPositionFromMousePosition();
			if (position == null)
				return null;
			int offset = textEditor.Document.GetOffset(position.Value.Location);
			return referenceElementGenerator.References.FindSegmentsContaining(offset).FirstOrDefault();
		}
		
		internal TextViewPosition? GetPositionFromMousePosition()
		{
			return textEditor.TextArea.TextView.GetPosition(Mouse.GetPosition(textEditor.TextArea.TextView) + textEditor.TextArea.TextView.ScrollOffset);
		}
		
		public DecompilerTextViewState GetState()
		{
			if (decompiledNodes == null)
				return null;

			var state = new DecompilerTextViewState();
			if (foldingManager != null)
				state.SaveFoldingsState(foldingManager.AllFoldings);
			state.VerticalOffset = textEditor.VerticalOffset;
			state.HorizontalOffset = textEditor.HorizontalOffset;
			state.TextViewPosition = textEditor.TextArea.Caret.Position;
			state.DesiredXPos = textEditor.TextArea.Caret.DesiredXPos;
			state.DecompiledNodes = decompiledNodes;
			return state;
		}
		
		public void Dispose()
		{
			DisplaySettingsPanel.CurrentDisplaySettings.PropertyChanged -= CurrentDisplaySettings_PropertyChanged;
		}
		
		#region Unfold
		public void UnfoldAndScroll(int lineNumber, bool scroll = true)
		{
			if (lineNumber <= 0 || lineNumber > textEditor.Document.LineCount)
				return;
			
			var line = textEditor.Document.GetLineByNumber(lineNumber);
			
			// unfold
			var foldings = foldingManager == null ? null : foldingManager.GetFoldingsContaining(line.Offset);
			if (foldings != null) {
				foreach (var folding in foldings) {
					if (folding.IsFolded) {
						folding.IsFolded = false;
					}
				}
			}
			// scroll to
			if (scroll)
				textEditor.ScrollTo(lineNumber, 0);
		}
		
		public FoldingManager FoldingManager
		{
			get {
				return foldingManager;
			}
		}
		#endregion

		public void ScrollAndMoveCaretTo(int line, int column)
		{
			// Make sure the lines have been re-initialized or the ScrollTo() method could fail
			TextEditor.TextArea.TextView.EnsureVisualLines();
			TextEditor.ScrollTo(line, column);
			SetCaretPosition(line, column);
		}

		void SetCaretPosition(int line, int column, double desiredXPos = double.NaN)
		{
			TextEditor.TextArea.Caret.Location = new ICSharpCode.AvalonEdit.Document.TextLocation(line, column);
			TextEditor.TextArea.Caret.DesiredXPos = desiredXPos;
		}

		void TextEditor_PreviewKeyDown(object sender, KeyEventArgs e)
		{
			if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.PageUp) {
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
				e.Handled = true;
				return;
			}

			if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.PageDown) {
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
				e.Handled = true;
				return;
			}

			if (Keyboard.Modifiers == ModifierKeys.None && e.Key == Key.Escape) {
				ClearLocalReferenceMarks();
				e.Handled = true;
				return;
			}

			if (Keyboard.Modifiers == ModifierKeys.None && e.Key == Key.F12) {
				int offset = textEditor.TextArea.Caret.Offset;
				var refSeg = referenceElementGenerator.References.FindSegmentsContaining(offset).FirstOrDefault();
				if (refSeg == null)
					return;
				var localTarget = FindLocalTarget(refSeg);
				if (localTarget != null)
					refSeg = localTarget;

				if (refSeg.IsLocalTarget) {
					var line = textEditor.Document.GetLineByOffset(refSeg.StartOffset);
					int column = refSeg.StartOffset - line.Offset + 1;
					ScrollAndMoveCaretTo(line.LineNumber, column);
					e.Handled = true;
					return;
				}

				if (refSeg.IsLocal)
					return;
				MainWindow.Instance.JumpToReference(refSeg.Reference);
				e.Handled = true;
			}
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
			if (ma != null && mb != null)
				return new SigComparer(SigComparerOptions.CompareDeclaringTypes | SigComparerOptions.PrivateScopeIsComparable).Equals(ma, mb);

			// Labels are strings, but the strings might not be the same reference, so make sure
			// to do the comparison as strings.
			var sa = a.Reference as string;
			var sb = b.Reference as string;
			if (sa != null && sb != null)
				return sa == sb;

			return false;
		}
	}

	public class DecompilerTextViewState
	{
		private List<Tuple<int, int>> ExpandedFoldings;
		private int FoldingsChecksum;
		public double VerticalOffset;
		public double HorizontalOffset;
		public TextViewPosition TextViewPosition;
		public double DesiredXPos;
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
	}
}
