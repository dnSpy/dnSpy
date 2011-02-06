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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using System.Xml;

using ICSharpCode.AvalonEdit.Folding;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using ICSharpCode.Decompiler;
using ICSharpCode.ILSpy.TreeNodes;
using Microsoft.Win32;
using Mono.Cecil;

namespace ICSharpCode.ILSpy.TextView
{
	/// <summary>
	/// Manages the TextEditor showing the decompiled code.
	/// Contains all the threading logic that makes the decompiler work in the background.
	/// </summary>
	sealed partial class DecompilerTextView : UserControl
	{
		readonly ReferenceElementGenerator referenceElementGenerator;
		readonly UIElementGenerator uiElementGenerator;
		FoldingManager foldingManager;
		internal MainWindow mainWindow;
		
		DefinitionLookup definitionLookup;
		CancellationTokenSource currentCancellationTokenSource;
		
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
			this.referenceElementGenerator = new ReferenceElementGenerator(this.JumpToReference);
			textEditor.TextArea.TextView.ElementGenerators.Add(referenceElementGenerator);
			this.uiElementGenerator = new UIElementGenerator();
			textEditor.TextArea.TextView.ElementGenerators.Add(uiElementGenerator);
			textEditor.Text = "Welcome to ILSpy!";
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
		void RunWithCancellation<T>(Func<CancellationToken, Task<T>> taskCreation, Action<Task<T>> taskCompleted)
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
				currentCancellationTokenSource = null;
			}
		}
		#endregion
		
		#region ShowOutput
		/// <summary>
		/// Shows the given output in the text view.
		/// </summary>
		/// <param name="textOutput">The output to show.</param>
		/// <param name="language">The language used for syntax highlighting.</param>
		void ShowOutput(AvalonEditTextOutput textOutput, ILSpy.Language language = null)
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
			textEditor.SyntaxHighlighting = language != null ? language.SyntaxHighlighting : null;
			
			Debug.WriteLine("  Set-up: {0}", w.Elapsed); w.Restart();
			textEditor.Document = textOutput.GetDocument();
			Debug.WriteLine("  Assigning document: {0}", w.Elapsed); w.Restart();
			if (textOutput.Foldings.Count > 0) {
				foldingManager = FoldingManager.Install(textEditor.TextArea);
				foldingManager.UpdateFoldings(textOutput.Foldings.OrderBy(f => f.StartOffset), -1);
				Debug.WriteLine("  Updating folding: {0}", w.Elapsed); w.Restart();
			}
		}
		#endregion
		
		#region Decompile (for display)
		const int defaultOutputLengthLimit  =  5000000; // more than 5M characters is too slow to output (when user browses treeview)
		const int extendedOutputLengthLimit = 75000000; // more than 75M characters can get us into trouble with memory usage
		
		DecompilationContext nextDecompilationRun;
		
		/// <summary>
		/// Starts the decompilation of the given nodes.
		/// The result is displayed in the text view.
		/// </summary>
		public void Decompile(ILSpy.Language language, IEnumerable<ILSpyTreeNodeBase> treeNodes, DecompilationOptions options)
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
						DoDecompile(context, defaultOutputLengthLimit);
					}
				));
			}
		}
		
		sealed class DecompilationContext
		{
			public readonly ILSpy.Language Language;
			public readonly ILSpyTreeNodeBase[] TreeNodes;
			public readonly DecompilationOptions Options;
			
			public DecompilationContext(ILSpy.Language language, ILSpyTreeNodeBase[] treeNodes, DecompilationOptions options)
			{
				this.Language = language;
				this.TreeNodes = treeNodes;
				this.Options = options;
			}
		}
		
		void DoDecompile(DecompilationContext context, int outputLengthLimit)
		{
			RunWithCancellation(
				delegate (CancellationToken ct) { // creation of the background task
					context.Options.CancellationToken = ct;
					return RunDecompiler(context, outputLengthLimit);
				},
				delegate (Task<AvalonEditTextOutput> task) { // handling the result
					try {
						AvalonEditTextOutput textOutput = task.Result;
						ShowOutput(textOutput, context.Language);
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
							WriteOutputLengthExceededMessage(output, context, outputLengthLimit == defaultOutputLengthLimit);
						} else {
							output.WriteLine(ex.ToString());
						}
						ShowOutput(output);
					}
				});
		}
		
		static Task<AvalonEditTextOutput> RunDecompiler(DecompilationContext context, int outputLengthLimit)
		{
			Debug.WriteLine("Start decompilation of {0} tree nodes", context.TreeNodes.Length);
			
			if (context.TreeNodes.Length == 0) {
				// If there's nothing to be decompiled, don't bother starting up a thread.
				// (Improves perf in some cases since we don't have to wait for the thread-pool to accept our task)
				TaskCompletionSource<AvalonEditTextOutput> tcs = new TaskCompletionSource<AvalonEditTextOutput>();
				tcs.SetResult(new AvalonEditTextOutput());
				return tcs.Task;
			}
			
			return Task.Factory.StartNew(
				delegate {
					AvalonEditTextOutput textOutput = new AvalonEditTextOutput();
					textOutput.LengthLimit = outputLengthLimit;
					DecompileNodes(context, textOutput);
					textOutput.PrepareDocument();
					return textOutput;
				});
		}
		
		static void DecompileNodes(DecompilationContext context, ITextOutput textOutput)
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
				output.AddUIElement(MakeButton(
					Images.ViewCode, "Display Code",
					delegate {
						DoDecompile(context, extendedOutputLengthLimit);
					}));
				output.WriteLine();
			}
			
			output.AddUIElement(MakeButton(
				Images.Save, "Save Code",
				delegate {
					SaveToDisk(context.Language, context.TreeNodes, context.Options);
				}));
			output.WriteLine();
		}
		
		/// <summary>
		/// Creates a button for use with <see cref="ISmartTextOutput.AddUIElement"/>.
		/// </summary>
		static Func<Button> MakeButton(ImageSource icon, string text, RoutedEventHandler click)
		{
			return () => {
				Button button = new Button();
				button.Cursor = Cursors.Arrow;
				button.Margin = new Thickness(2);
				if (icon != null) {
					button.Content = new StackPanel {
						Orientation = Orientation.Horizontal,
						Children = {
							new Image { Width = 16, Height = 16, Source = icon, Margin = new Thickness(0, 0, 4, 0) },
							new TextBlock { Text = text }
						}
					};
				} else {
					button.Content = text;
				}
				button.Click += click;
				return button;
			};
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
			var assemblyList = mainWindow.AssemblyList;
			if (reference is TypeReference) {
				mainWindow.SelectNode(assemblyList.FindTypeNode(((TypeReference)reference).Resolve()));
			} else if (reference is MethodReference) {
				mainWindow.SelectNode(assemblyList.FindMethodNode(((MethodReference)reference).Resolve()));
			} else if (reference is FieldReference) {
				mainWindow.SelectNode(assemblyList.FindFieldNode(((FieldReference)reference).Resolve()));
			} else if (reference is PropertyReference) {
				mainWindow.SelectNode(assemblyList.FindPropertyNode(((PropertyReference)reference).Resolve()));
			} else if (reference is EventReference) {
				mainWindow.SelectNode(assemblyList.FindEventNode(((EventReference)reference).Resolve()));
			} else if (reference is AssemblyDefinition) {
				mainWindow.SelectNode(assemblyList.Assemblies.FirstOrDefault(node => node.AssemblyDefinition == reference));
			}
		}
		#endregion
		
		#region SaveToDisk
		/// <summary>
		/// Shows the 'save file dialog', prompting the user to save the decompiled nodes to disk.
		/// </summary>
		public void SaveToDisk(ILSpy.Language language, IEnumerable<ILSpyTreeNodeBase> treeNodes, DecompilationOptions options)
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
		
		/// <summary>
		/// Starts the decompilation of the given nodes.
		/// The result will be saved to the given file name.
		/// </summary>
		void SaveToDisk(DecompilationContext context, string fileName)
		{
			RunWithCancellation(
				delegate (CancellationToken ct) {
					context.Options.CancellationToken = ct;
					return Task.Factory.StartNew(
						delegate {
							using (StreamWriter w = new StreamWriter(fileName)) {
								try {
									DecompileNodes(context, new PlainTextOutput(w));
								} catch (OperationCanceledException) {
									w.WriteLine();
									w.WriteLine("Decompiled was cancelled.");
									throw;
								}
							}
							AvalonEditTextOutput output = new AvalonEditTextOutput();
							output.WriteLine("Decompilation complete.");
							output.WriteLine();
							output.AddUIElement(MakeButton(
								null, "Open Explorer",
								delegate {
									Process.Start("explorer", "/select,\"" + fileName + "\"");
								}
							));
							output.WriteLine();
							return output;
						});
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
				});
		}
		
		/// <summary>
		/// Cleans up a node name for use as a file name.
		/// </summary>
		static string CleanUpName(string text)
		{
			int pos = text.IndexOf(':');
			if (pos > 0)
				text = text.Substring(0, pos);
			text = text.Trim();
			foreach (char c in Path.GetInvalidFileNameChars())
				text = text.Replace(c, '-');
			return text;
		}
		#endregion
	}
}
