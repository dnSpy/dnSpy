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
		TextSegmentCollection<ReferenceSegment> references;
		CancellationTokenSource currentCancellationTokenSource;
		
		readonly TextMarkerService textMarkerService;
		readonly List<ITextMarker> localReferenceMarks = new List<ITextMarker>();
		
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
			
			InitializeComponent();
			
			this.referenceElementGenerator = new ReferenceElementGenerator(this.JumpToReference, this.IsLink);
			textEditor.TextArea.TextView.ElementGenerators.Add(referenceElementGenerator);
			this.uiElementGenerator = new UIElementGenerator();
			textEditor.TextArea.TextView.ElementGenerators.Add(uiElementGenerator);
			textEditor.Options.RequireControlModifierForHyperlinkClick = false;
			textEditor.TextArea.TextView.MouseHover += TextViewMouseHover;
			textEditor.TextArea.TextView.MouseHoverStopped += TextViewMouseHoverStopped;
			textEditor.SetBinding(Control.FontFamilyProperty, new Binding { Source = DisplaySettingsPanel.CurrentDisplaySettings, Path = new PropertyPath("SelectedFont") });
			textEditor.SetBinding(Control.FontSizeProperty, new Binding { Source = DisplaySettingsPanel.CurrentDisplaySettings, Path = new PropertyPath("SelectedFontSize") });
			
			textMarkerService = new TextMarkerService(textEditor.TextArea.TextView);
			textEditor.TextArea.TextView.BackgroundRenderers.Add(textMarkerService);
			textEditor.TextArea.TextView.LineTransformers.Add(textMarkerService);
			textEditor.ShowLineNumbers = true;
			DisplaySettingsPanel.CurrentDisplaySettings.PropertyChanged += CurrentDisplaySettings_PropertyChanged;
			
			// Bookmarks context menu
			textEditor.TextArea.DefaultInputHandler.NestedInputHandlers.Add(new SearchInputHandler(textEditor.TextArea));
			
			ShowLineMargin();
			
			// add marker service & margin
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
							output.WriteLine("The operation was canceled.");
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
						output.WriteLine(exception.ToString());
					}
					ShowOutput(output);
					decompiledNodes = context.TreeNodes;
				});
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
				output.WriteLine("You have selected too much code for it to be displayed automatically.");
			} else {
				output.WriteLine("You have selected too much code; it cannot be displayed here.");
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
		internal void JumpToReference(ReferenceSegment referenceSegment)
		{
			object reference = referenceSegment.Reference;
			if (referenceSegment.IsLocal) {
				ClearLocalReferenceMarks();
				if (references != null) {
					foreach (var r in references) {
						if (reference.Equals(r.Reference)) {
							var mark = textMarkerService.Create(r.StartOffset, r.Length);
							mark.BackgroundColor = r.IsLocalTarget ? Colors.LightSeaGreen : Colors.GreenYellow;
							localReferenceMarks.Add(mark);
						}
					}
				}
				return;
			}
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
					output.WriteLine(ex.ToString());
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
						output.WriteLine("Decompilation complete in " + stopwatch.Elapsed.TotalSeconds.ToString("F1") + " seconds.");
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
		
		public FoldingManager FoldingManager
		{
			get {
				return foldingManager;
			}
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
