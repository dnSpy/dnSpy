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
using ICSharpCode.Decompiler;
using ICSharpCode.ILSpy.AvalonEdit;
using ICSharpCode.ILSpy.Bookmarks;
using ICSharpCode.ILSpy.Debugger;
using ICSharpCode.ILSpy.Debugger.Bookmarks;
using ICSharpCode.ILSpy.Options;
using ICSharpCode.ILSpy.TreeNodes;
using ICSharpCode.ILSpy.XmlDoc;
using ICSharpCode.NRefactory.Documentation;
using Microsoft.Win32;
using Mono.Cecil;

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
		CancellationTokenSource currentCancellationTokenSource;
		
		internal readonly IconBarManager manager;
		readonly IconBarMargin iconMargin;
		readonly TextMarkerService textMarkerService;
		
		[ImportMany(typeof(ITextEditorListener))]
		IEnumerable<ITextEditorListener> textEditorListeners;
		
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
			this.uiElementGenerator = new UIElementGenerator();
			textEditor.TextArea.TextView.ElementGenerators.Add(uiElementGenerator);
			textEditor.Options.RequireControlModifierForHyperlinkClick = false;
			textEditor.TextArea.TextView.MouseHover += TextViewMouseHover;
			textEditor.TextArea.TextView.MouseHoverStopped += TextViewMouseHoverStopped;
			textEditor.SetBinding(TextEditor.FontFamilyProperty, new Binding { Source = DisplaySettingsPanel.CurrentDisplaySettings, Path = new PropertyPath("SelectedFont") });
			textEditor.SetBinding(TextEditor.FontSizeProperty, new Binding { Source = DisplaySettingsPanel.CurrentDisplaySettings, Path = new PropertyPath("SelectedFontSize") });
			
			// add marker service & margin
			iconMargin = new IconBarMargin((manager = new IconBarManager()));
			textMarkerService = new TextMarkerService();
			textMarkerService.CodeEditor = textEditor;
			textEditor.TextArea.TextView.BackgroundRenderers.Add(textMarkerService);
			textEditor.TextArea.TextView.LineTransformers.Add(textMarkerService);
			textEditor.ShowLineNumbers = true;
			DisplaySettingsPanel.CurrentDisplaySettings.PropertyChanged += CurrentDisplaySettings_PropertyChanged;
			
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
			textMarkerService.CodeEditor = textEditor;
			textEditor.TextArea.TextView.BackgroundRenderers.Add(textMarkerService);
			textEditor.TextArea.TextView.LineTransformers.Add(textMarkerService);
		}
		
		#endregion
		
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
			int offset = textEditor.Document.GetOffset(position.Value);
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
			if (segment.Reference is Mono.Cecil.Cil.OpCode) {
				Mono.Cecil.Cil.OpCode code = (Mono.Cecil.Cil.OpCode)segment.Reference;
				string encodedName = code.Code.ToString();
				string opCodeHex = code.Size > 1 ? string.Format("0x{0:x2}{1:x2}", code.Op1, code.Op2) : string.Format("0x{0:x2}", code.Op2);
				XmlDocumentationProvider docProvider = XmlDocLoader.MscorlibDocumentation;
				if (docProvider != null){
					string documentation = docProvider.GetDocumentation("F:System.Reflection.Emit.OpCodes." + encodedName);
					if (documentation != null) {
						XmlDocRenderer renderer = new XmlDocRenderer();
						renderer.AppendText(string.Format("{0} ({1}) - ", code.Name, opCodeHex));
						renderer.AddXmlDocumentation(documentation);
						return renderer.CreateTextBlock();
					}
				}
				return string.Format("{0} ({1})", code.Name, opCodeHex);
			} else if (segment.Reference is MemberReference) {
				MemberReference mr = (MemberReference)segment.Reference;
				// if possible, resolve the reference
				if (mr is TypeReference) {
					mr = ((TypeReference)mr).Resolve() ?? mr;
				} else if (mr is MethodReference) {
					mr = ((MethodReference)mr).Resolve() ?? mr;
				}
				XmlDocRenderer renderer = new XmlDocRenderer();
				renderer.AppendText(MainWindow.Instance.CurrentLanguage.GetTooltip(mr));
				XmlDocumentationProvider docProvider = XmlDocLoader.LoadDocumentation(mr.Module);
				if (docProvider != null) {
					string documentation = docProvider.GetDocumentation(XmlDocKeyProvider.GetKey(mr));
					if (documentation != null) {
						renderer.AppendText(Environment.NewLine);
						renderer.AddXmlDocumentation(documentation);
					}
				}
				return renderer.CreateTextBlock();
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
		public void RunWithCancellation<T>(Func<CancellationToken, Task<T>> taskCreation, Action<Task<T>> taskCompleted)
		{
			if (waitAdorner.Visibility != Visibility.Visible) {
				waitAdorner.Visibility = Visibility.Visible;
				waitAdorner.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, new Duration(TimeSpan.FromSeconds(0.5)), FillBehavior.Stop));
			}
			CancellationTokenSource previousCancellationTokenSource = currentCancellationTokenSource;
			var myCancellationTokenSource = new CancellationTokenSource();
			currentCancellationTokenSource = myCancellationTokenSource;
			// cancel the previous only after current was set to the new one (avoid that the old one still finishes successfully)
			if (previousCancellationTokenSource != null)
				previousCancellationTokenSource.Cancel();
			
			var task = taskCreation(myCancellationTokenSource.Token);
			Action continuation = delegate {
				try {
					if (currentCancellationTokenSource == myCancellationTokenSource) {
						currentCancellationTokenSource = null;
						waitAdorner.Visibility = Visibility.Collapsed;
						taskCompleted(task);
					} else {
						try {
							task.Wait();
						} catch (AggregateException) {
							// observe the exception (otherwise the task's finalizer will shut down the AppDomain)
						}
					}
				} finally {
					myCancellationTokenSource.Dispose();
				}
			};
			task.ContinueWith(delegate { Dispatcher.BeginInvoke(DispatcherPriority.Normal, continuation); });
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
			this.nextDecompilationRun = null; // remove scheduled decompilation run
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
			
			textEditor.ScrollToHome();
			if (foldingManager != null) {
				FoldingManager.Uninstall(foldingManager);
				foldingManager = null;
			}
			textEditor.Document = null; // clear old document while we're changing the highlighting
			uiElementGenerator.UIElements = textOutput.UIElements;
			referenceElementGenerator.References = textOutput.References;
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
					textEditor.ScrollToVerticalOffset(state.VerticalOffset);
					textEditor.ScrollToHorizontalOffset(state.HorizontalOffset);
				}
				foldingManager = FoldingManager.Install(textEditor.TextArea);
				foldingManager.UpdateFoldings(textOutput.Foldings.OrderBy(f => f.StartOffset), -1);
				Debug.WriteLine("  Updating folding: {0}", w.Elapsed); w.Restart();
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
		/// Starts the decompilation of the given nodes.
		/// The result is displayed in the text view.
		/// </summary>
		public void Decompile(ILSpy.Language language, IEnumerable<ILSpyTreeNode> treeNodes, DecompilationOptions options)
		{
			// Some actions like loading an assembly list cause several selection changes in the tree view,
			// and each of those will start a decompilation action.
			bool isDecompilationScheduled = this.nextDecompilationRun != null;
			this.nextDecompilationRun = new DecompilationContext(language, treeNodes.ToArray(), options);
			if (!isDecompilationScheduled) {
				Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(
					delegate {
						var context = this.nextDecompilationRun;
						this.nextDecompilationRun = null;
						if (context != null)
							DoDecompile(context, DefaultOutputLengthLimit);
					}
				));
			}
		}
		
		sealed class DecompilationContext
		{
			public readonly ILSpy.Language Language;
			public readonly ILSpyTreeNode[] TreeNodes;
			public readonly DecompilationOptions Options;
			
			public DecompilationContext(ILSpy.Language language, ILSpyTreeNode[] treeNodes, DecompilationOptions options)
			{
				this.Language = language;
				this.TreeNodes = treeNodes;
				this.Options = options;
			}
		}
		
		void DoDecompile(DecompilationContext context, int outputLengthLimit)
		{
			// close popup
			if (textEditorListeners != null) {
				foreach (var listener in textEditorListeners) {
					listener.ClosePopup();
				}
			}
			bool isDecompilationOk = true;
			
			RunWithCancellation(
				delegate (CancellationToken ct) { // creation of the background task
					context.Options.CancellationToken = ct;
					return DecompileAsync(context, outputLengthLimit);
				},
				delegate (Task<AvalonEditTextOutput> task) { // handling the result
					try {
						AvalonEditTextOutput textOutput = task.Result;
						ShowOutput(textOutput, context.Language.SyntaxHighlighting, context.Options.TextViewState);
					} catch (AggregateException aggregateException) {
						textEditor.SyntaxHighlighting = null;
						Debug.WriteLine("Decompiler crashed: " + aggregateException.ToString());
						// Unpack aggregate exceptions as long as there's only a single exception:
						// (assembly load errors might produce nested aggregate exceptions)
						Exception ex = aggregateException;
						while (ex is AggregateException && (ex as AggregateException).InnerExceptions.Count == 1)
							ex = ex.InnerException;
						AvalonEditTextOutput output = new AvalonEditTextOutput();
						if (ex is OutputLengthExceededException) {
							WriteOutputLengthExceededMessage(output, context, outputLengthLimit == DefaultOutputLengthLimit);
						} else {
							output.WriteLine(ex.ToString());
						}
						ShowOutput(output);
						isDecompilationOk = false;
					} finally {
						this.UpdateDebugUI(isDecompilationOk);
					}
					decompiledNodes = context.TreeNodes;
				});
		}
		
		void UpdateDebugUI(bool isDecompilationOk)
		{
			// sync bookmarks
			iconMargin.SyncBookmarks();
			
			if (isDecompilationOk) {
				if (DebugData.DebugStepInformation != null) {
					// repaint bookmarks
					iconMargin.InvalidateVisual();
					
					// show the currentline marker
					int token = DebugData.DebugStepInformation.Item1;
					int ilOffset = DebugData.DebugStepInformation.Item2;
					int line;
					MemberReference member;
					if (!DebugData.CodeMappings.ContainsKey(token))
						return;
					
					DebugData.CodeMappings[token].GetInstructionByTokenAndOffset(token, ilOffset, out member, out line);
					
					// update marker
					CurrentLineBookmark.Remove();
					CurrentLineBookmark.SetPosition(member, line, 0, line, 0);

					var bm = CurrentLineBookmark.Instance;
					DocumentLine docline = textEditor.Document.GetLineByNumber(line);
					bm.Marker = bm.CreateMarker(textMarkerService, docline.Offset + 1, docline.Length);
					
					UnfoldAndScroll(line);
				}
			} else {
				// remove currentline marker
				CurrentLineBookmark.Remove();
			}
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
						} catch (AggregateException ex) {
							tcs.SetException(ex);
						} catch (OperationCanceledException ex) {
							tcs.SetException(ex);
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
			// reset data
			DebugData.OldCodeMappings = DebugData.CodeMappings;
			DebugData.CodeMappings = null;
			DebugData.LocalVariables = null;
			DebugData.DecompiledMemberReferences = null;
			// set the language
			DebugData.Language = MainWindow.Instance.sessionSettings.FilterSettings.Language.Name.StartsWith("IL") ? DecompiledLanguages.IL : DecompiledLanguages.CSharp;
			
			var nodes = context.TreeNodes;
			context.Language.DecompileFinished += Language_DecompileFinished;
			for (int i = 0; i < nodes.Length; i++) {
				if (i > 0)
					textOutput.WriteLine();
				
				context.Options.CancellationToken.ThrowIfCancellationRequested();
				nodes[i].Decompile(context.Language, textOutput, context.Options);
			}
			context.Language.DecompileFinished -= Language_DecompileFinished;
		}
		
		void Language_DecompileFinished(object sender, DecompileEventArgs e)
		{
			if (e != null) {
				manager.UpdateClassMemberBookmarks(e.AstNodes);
				if (iconMargin.DecompiledMembers == null) {
					iconMargin.DecompiledMembers = new List<MemberReference>();
				}
				iconMargin.DecompiledMembers.AddRange(e.DecompiledMemberReferences.Values.AsEnumerable());
				
				// debugger info
				if (DebugData.CodeMappings == null) {
					DebugData.CodeMappings = e.CodeMappings;
					DebugData.LocalVariables = e.LocalVariables;
					DebugData.DecompiledMemberReferences = e.DecompiledMemberReferences;
				} else {
					DebugData.CodeMappings.AddRange(e.CodeMappings);
					DebugData.DecompiledMemberReferences.AddRange(e.DecompiledMemberReferences);
					if (e.LocalVariables != null)
						DebugData.LocalVariables.AddRange(e.LocalVariables);
				}
			} else {
				manager.UpdateClassMemberBookmarks(null);
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
				output.WriteLine("You have selected too much code for it to be displayed automatically.");
			} else {
				output.WriteLine("You have selected too much code; it cannot be displayed here.");
			}
			output.WriteLine();
			if (wasNormalLimit) {
				output.AddButton(
					Images.ViewCode, "Display Code",
					delegate {
						DoDecompile(context, ExtendedOutputLengthLimit);
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
		internal void JumpToReference(ReferenceSegment referenceSegment)
		{
			object reference = referenceSegment.Reference;
			if (definitionLookup != null) {
				int pos = definitionLookup.GetDefinitionPosition(reference);
				if (pos >= 0) {
					textEditor.TextArea.Focus();
					textEditor.Select(pos, 0);
					textEditor.ScrollTo(textEditor.TextArea.Caret.Line, textEditor.TextArea.Caret.Column);
					Dispatcher.Invoke(DispatcherPriority.Background, new Action(
						delegate {
							CaretHighlightAdorner.DisplayCaretHighlightAnimation(textEditor.TextArea);
						}));
					return;
				}
			}
			MainWindow.Instance.JumpToReference(reference);
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
				},
				delegate (Task<AvalonEditTextOutput> task) {
					try {
						ShowOutput(task.Result);
					} catch (AggregateException aggregateException) {
						textEditor.SyntaxHighlighting = null;
						Debug.WriteLine("Decompiler crashed: " + aggregateException.ToString());
						// Unpack aggregate exceptions as long as there's only a single exception:
						// (assembly load errors might produce nested aggregate exceptions)
						Exception ex = aggregateException;
						while (ex is AggregateException && (ex as AggregateException).InnerExceptions.Count == 1)
							ex = ex.InnerException;
						AvalonEditTextOutput output = new AvalonEditTextOutput();
						output.WriteLine(ex.ToString());
						ShowOutput(output);
					}
					decompiledNodes = context.TreeNodes;
				});
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
						output.WriteLine("Decompilation complete in " + stopwatch.Elapsed.TotalSeconds.ToString("F1") + " seconds.");
						output.WriteLine();
						output.AddButton(null, "Open Explorer", delegate { Process.Start("explorer", "/select,\"" + fileName + "\""); });
						output.WriteLine();
						tcs.SetResult(output);
						#if DEBUG
					} catch (OperationCanceledException ex) {
						tcs.SetException(ex);
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
			return text;
		}
		#endregion

		public DecompilerTextViewState GetState()
		{
			if (decompiledNodes == null)
				return null;

			var state = new DecompilerTextViewState();
			if (foldingManager != null)
				state.SaveFoldingsState(foldingManager.AllFoldings);
			state.VerticalOffset = textEditor.VerticalOffset;
			state.HorizontalOffset = textEditor.HorizontalOffset;
			state.DecompiledNodes = decompiledNodes;
			return state;
		}
		
		public void Dispose()
		{
			DisplaySettingsPanel.CurrentDisplaySettings.PropertyChanged -= CurrentDisplaySettings_PropertyChanged;
		}
		
		#region Unfold
		public void UnfoldAndScroll(int lineNumber)
		{
			if (lineNumber <= 0 || lineNumber > textEditor.Document.LineCount)
				return;
			
			var line = textEditor.Document.GetLineByNumber(lineNumber);
			
			// unfold
			var foldings = foldingManager.GetFoldingsContaining(line.Offset);
			if (foldings != null) {
				foreach (var folding in foldings) {
					if (folding.IsFolded) {
						folding.IsFolded = false;
					}
				}
			}
			// scroll to
			textEditor.ScrollTo(lineNumber, 0);
		}
		#endregion
	}

	public class DecompilerTextViewState
	{
		private List<Tuple<int, int>> ExpandedFoldings;
		private int FoldingsChecksum;
		public double VerticalOffset;
		public double HorizontalOffset;
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
