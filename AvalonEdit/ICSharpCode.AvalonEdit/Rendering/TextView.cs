// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.TextFormatting;
using System.Windows.Threading;

using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Utils;

namespace ICSharpCode.AvalonEdit.Rendering
{
	/// <summary>
	/// A virtualizing panel producing+showing <see cref="VisualLine"/>s for a <see cref="TextDocument"/>.
	/// 
	/// This is the heart of the text editor, this class controls the text rendering process.
	/// 
	/// Taken as a standalone control, it's a text viewer without any editing capability.
	/// </summary>
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1001:TypesThatOwnDisposableFieldsShouldBeDisposable",
	                                                 Justification = "The user usually doesn't work with TextView but with TextEditor; and nulling the Document property is sufficient to dispose everything.")]
	public class TextView : FrameworkElement, IScrollInfo, IWeakEventListener, ITextEditorComponent, IServiceProvider
	{
		#region Constructor
		static TextView()
		{
			ClipToBoundsProperty.OverrideMetadata(typeof(TextView), new FrameworkPropertyMetadata(Boxes.True));
			FocusableProperty.OverrideMetadata(typeof(TextView), new FrameworkPropertyMetadata(Boxes.False));
		}
		
		/// <summary>
		/// Creates a new TextView instance.
		/// </summary>
		public TextView()
		{
			services.AddService(typeof(TextView), this);
			textLayer = new TextLayer(this);
			elementGenerators = new ObserveAddRemoveCollection<VisualLineElementGenerator>(ElementGenerator_Added, ElementGenerator_Removed);
			lineTransformers = new ObserveAddRemoveCollection<IVisualLineTransformer>(LineTransformer_Added, LineTransformer_Removed);
			backgroundRenderers = new ObserveAddRemoveCollection<IBackgroundRenderer>(BackgroundRenderer_Added, BackgroundRenderer_Removed);
			this.Options = new TextEditorOptions();
			Debug.Assert(singleCharacterElementGenerator != null); // assert that the option change created the builtin element generators
			
			layers = new LayerCollection(this);
			InsertLayer(textLayer, KnownLayer.Text, LayerInsertionPosition.Replace);
			
			this.hoverLogic = new MouseHoverLogic(this);
			this.hoverLogic.MouseHover += (sender, e) => RaiseHoverEventPair(e, PreviewMouseHoverEvent, MouseHoverEvent);
			this.hoverLogic.MouseHoverStopped += (sender, e) => RaiseHoverEventPair(e, PreviewMouseHoverStoppedEvent, MouseHoverStoppedEvent);
		}

		#endregion
		
		#region Document Property
		/// <summary>
		/// Document property.
		/// </summary>
		public static readonly DependencyProperty DocumentProperty =
			DependencyProperty.Register("Document", typeof(TextDocument), typeof(TextView),
			                            new FrameworkPropertyMetadata(OnDocumentChanged));
		
		TextDocument document;
		HeightTree heightTree;
		
		/// <summary>
		/// Gets/Sets the document displayed by the text editor.
		/// </summary>
		public TextDocument Document {
			get { return (TextDocument)GetValue(DocumentProperty); }
			set { SetValue(DocumentProperty, value); }
		}
		
		static void OnDocumentChanged(DependencyObject dp, DependencyPropertyChangedEventArgs e)
		{
			((TextView)dp).OnDocumentChanged((TextDocument)e.OldValue, (TextDocument)e.NewValue);
		}
		
		internal double FontSize {
			get {
				return (double)GetValue(TextBlock.FontSizeProperty);
			}
		}
		
		/// <summary>
		/// Occurs when the document property has changed.
		/// </summary>
		public event EventHandler DocumentChanged;
		
		void OnDocumentChanged(TextDocument oldValue, TextDocument newValue)
		{
			if (oldValue != null) {
				heightTree.Dispose();
				heightTree = null;
				formatter.Dispose();
				formatter = null;
				cachedElements.Dispose();
				cachedElements = null;
				TextDocumentWeakEventManager.Changing.RemoveListener(oldValue, this);
			}
			this.document = newValue;
			ClearScrollData();
			ClearVisualLines();
			if (newValue != null) {
				TextDocumentWeakEventManager.Changing.AddListener(newValue, this);
				formatter = TextFormatterFactory.Create(this);
				heightTree = new HeightTree(newValue, DefaultLineHeight); // measuring DefaultLineHeight depends on formatter
				cachedElements = new TextViewCachedElements();
			}
			InvalidateMeasure(DispatcherPriority.Normal);
			if (DocumentChanged != null)
				DocumentChanged(this, EventArgs.Empty);
		}
		
		/// <summary>
		/// Recreates the text formatter that is used internally
		/// by calling <see cref="TextFormatterFactory.Create"/>.
		/// </summary>
		void RecreateTextFormatter()
		{
			if (formatter != null) {
				formatter.Dispose();
				formatter = TextFormatterFactory.Create(this);
				Redraw();
			}
		}
		
		void RecreateCachedElements()
		{
			if (cachedElements != null) {
				cachedElements.Dispose();
				cachedElements = new TextViewCachedElements();
			}
		}
		
		/// <inheritdoc cref="IWeakEventListener.ReceiveWeakEvent"/>
		protected virtual bool ReceiveWeakEvent(Type managerType, object sender, EventArgs e)
		{
			if (managerType == typeof(TextDocumentWeakEventManager.Changing)) {
				// TODO: put redraw into background so that other input events can be handled before the redraw.
				// Unfortunately the "easy" approach (just use DispatcherPriority.Background) here makes the editor twice as slow because
				// the caret position change forces an immediate redraw, and the text input then forces a background redraw.
				// When fixing this, make sure performance on the SharpDevelop "type text in C# comment" stress test doesn't get significantly worse.
				DocumentChangeEventArgs change = (DocumentChangeEventArgs)e;
				Redraw(change.Offset, change.RemovalLength, DispatcherPriority.Normal);
				return true;
			} else if (managerType == typeof(PropertyChangedWeakEventManager)) {
				OnOptionChanged((PropertyChangedEventArgs)e);
				return true;
			}
			return false;
		}
		
		bool IWeakEventListener.ReceiveWeakEvent(Type managerType, object sender, EventArgs e)
		{
			return ReceiveWeakEvent(managerType, sender, e);
		}
		#endregion
		
		#region Options property
		/// <summary>
		/// Options property.
		/// </summary>
		public static readonly DependencyProperty OptionsProperty =
			DependencyProperty.Register("Options", typeof(TextEditorOptions), typeof(TextView),
			                            new FrameworkPropertyMetadata(OnOptionsChanged));
		
		/// <summary>
		/// Gets/Sets the document displayed by the text editor.
		/// </summary>
		public TextEditorOptions Options {
			get { return (TextEditorOptions)GetValue(OptionsProperty); }
			set { SetValue(OptionsProperty, value); }
		}
		
		/// <summary>
		/// Occurs when a text editor option has changed.
		/// </summary>
		public event PropertyChangedEventHandler OptionChanged;
		
		/// <summary>
		/// Raises the <see cref="OptionChanged"/> event.
		/// </summary>
		protected virtual void OnOptionChanged(PropertyChangedEventArgs e)
		{
			if (OptionChanged != null) {
				OptionChanged(this, e);
			}
			UpdateBuiltinElementGeneratorsFromOptions();
			Redraw();
		}
		
		static void OnOptionsChanged(DependencyObject dp, DependencyPropertyChangedEventArgs e)
		{
			((TextView)dp).OnOptionsChanged((TextEditorOptions)e.OldValue, (TextEditorOptions)e.NewValue);
		}
		
		void OnOptionsChanged(TextEditorOptions oldValue, TextEditorOptions newValue)
		{
			if (oldValue != null) {
				PropertyChangedWeakEventManager.RemoveListener(oldValue, this);
			}
			if (newValue != null) {
				PropertyChangedWeakEventManager.AddListener(newValue, this);
			}
			OnOptionChanged(new PropertyChangedEventArgs(null));
		}
		#endregion
		
		#region ElementGenerators+LineTransformers Properties
		readonly ObserveAddRemoveCollection<VisualLineElementGenerator> elementGenerators;
		
		/// <summary>
		/// Gets a collection where element generators can be registered.
		/// </summary>
		public IList<VisualLineElementGenerator> ElementGenerators {
			get { return elementGenerators; }
		}
		
		void ElementGenerator_Added(VisualLineElementGenerator generator)
		{
			ConnectToTextView(generator);
			Redraw();
		}
		
		void ElementGenerator_Removed(VisualLineElementGenerator generator)
		{
			DisconnectFromTextView(generator);
			Redraw();
		}
		
		readonly ObserveAddRemoveCollection<IVisualLineTransformer> lineTransformers;
		
		/// <summary>
		/// Gets a collection where line transformers can be registered.
		/// </summary>
		public IList<IVisualLineTransformer> LineTransformers {
			get { return lineTransformers; }
		}
		
		void LineTransformer_Added(IVisualLineTransformer lineTransformer)
		{
			ConnectToTextView(lineTransformer);
			Redraw();
		}
		
		void LineTransformer_Removed(IVisualLineTransformer lineTransformer)
		{
			DisconnectFromTextView(lineTransformer);
			Redraw();
		}
		#endregion
		
		#region Builtin ElementGenerators
		NewLineElementGenerator newLineElementGenerator;
		SingleCharacterElementGenerator singleCharacterElementGenerator;
		LinkElementGenerator linkElementGenerator;
		MailLinkElementGenerator mailLinkElementGenerator;
		
		void UpdateBuiltinElementGeneratorsFromOptions()
		{
			TextEditorOptions options = this.Options;
			
			AddRemoveDefaultElementGeneratorOnDemand(ref newLineElementGenerator, options.ShowEndOfLine);
			AddRemoveDefaultElementGeneratorOnDemand(ref singleCharacterElementGenerator, options.ShowBoxForControlCharacters || options.ShowSpaces || options.ShowTabs);
			AddRemoveDefaultElementGeneratorOnDemand(ref linkElementGenerator, options.EnableHyperlinks);
			AddRemoveDefaultElementGeneratorOnDemand(ref mailLinkElementGenerator, options.EnableEmailHyperlinks);
		}
		
		void AddRemoveDefaultElementGeneratorOnDemand<T>(ref T generator, bool demand)
			where T : VisualLineElementGenerator, IBuiltinElementGenerator, new()
		{
			bool hasGenerator = generator != null;
			if (hasGenerator != demand) {
				if (demand) {
					generator = new T();
					this.ElementGenerators.Add(generator);
				} else {
					this.ElementGenerators.Remove(generator);
					generator = null;
				}
			}
			if (generator != null)
				generator.FetchOptions(this.Options);
		}
		#endregion
		
		#region Layers
		internal readonly TextLayer textLayer;
		readonly LayerCollection layers;
		
		/// <summary>
		/// Gets the list of layers displayed in the text view.
		/// </summary>
		public UIElementCollection Layers {
			get { return layers; }
		}
		
		sealed class LayerCollection : UIElementCollection
		{
			readonly TextView textView;
			
			public LayerCollection(TextView textView)
				: base(textView, textView)
			{
				this.textView = textView;
			}
			
			public override void Clear()
			{
				base.Clear();
				textView.LayersChanged();
			}
			
			public override int Add(UIElement element)
			{
				int r = base.Add(element);
				textView.LayersChanged();
				return r;
			}
			
			public override void RemoveAt(int index)
			{
				base.RemoveAt(index);
				textView.LayersChanged();
			}
			
			public override void RemoveRange(int index, int count)
			{
				base.RemoveRange(index, count);
				textView.LayersChanged();
			}
		}
		
		void LayersChanged()
		{
			textLayer.index = layers.IndexOf(textLayer);
		}
		
		/// <summary>
		/// Inserts a new layer at a position specified relative to an existing layer.
		/// </summary>
		/// <param name="layer">The new layer to insert.</param>
		/// <param name="referencedLayer">The existing layer</param>
		/// <param name="position">Specifies whether the layer is inserted above,below, or replaces the referenced layer</param>
		public void InsertLayer(UIElement layer, KnownLayer referencedLayer, LayerInsertionPosition position)
		{
			if (layer == null)
				throw new ArgumentNullException("layer");
			if (!Enum.IsDefined(typeof(KnownLayer), referencedLayer))
				throw new InvalidEnumArgumentException("referencedLayer", (int)referencedLayer, typeof(KnownLayer));
			if (!Enum.IsDefined(typeof(LayerInsertionPosition), position))
				throw new InvalidEnumArgumentException("position", (int)position, typeof(LayerInsertionPosition));
			if (referencedLayer == KnownLayer.Background && position != LayerInsertionPosition.Above)
				throw new InvalidOperationException("Cannot replace or insert below the background layer.");
			
			LayerPosition newPosition = new LayerPosition(referencedLayer, position);
			LayerPosition.SetLayerPosition(layer, newPosition);
			for (int i = 0; i < layers.Count; i++) {
				LayerPosition p = LayerPosition.GetLayerPosition(layers[i]);
				if (p != null) {
					if (p.KnownLayer == referencedLayer && p.Position == LayerInsertionPosition.Replace) {
						// found the referenced layer
						switch (position) {
							case LayerInsertionPosition.Below:
								layers.Insert(i, layer);
								return;
							case LayerInsertionPosition.Above:
								layers.Insert(i + 1, layer);
								return;
							case LayerInsertionPosition.Replace:
								layers[i] = layer;
								return;
						}
					} else if (p.KnownLayer == referencedLayer && p.Position == LayerInsertionPosition.Above
					           || p.KnownLayer > referencedLayer) {
						// we skipped the insertion position (referenced layer does not exist?)
						layers.Insert(i, layer);
						return;
					}
				}
			}
			// inserting after all existing layers:
			layers.Add(layer);
		}
		
		/// <inheritdoc/>
		protected override int VisualChildrenCount {
			get { return layers.Count + inlineObjects.Count; }
		}
		
		/// <inheritdoc/>
		protected override Visual GetVisualChild(int index)
		{
			int cut = textLayer.index + 1;
			if (index < cut)
				return layers[index];
			else if (index < cut + inlineObjects.Count)
				return inlineObjects[index - cut].Element;
			else
				return layers[index - inlineObjects.Count];
		}
		
		/// <inheritdoc/>
		protected override System.Collections.IEnumerator LogicalChildren {
			get {
				return inlineObjects.Select(io => io.Element).Concat(layers.Cast<UIElement>()).GetEnumerator();
			}
		}
		#endregion
		
		#region Inline object handling
		List<InlineObjectRun> inlineObjects = new List<InlineObjectRun>();
		
		/// <summary>
		/// Adds a new inline object.
		/// </summary>
		internal void AddInlineObject(InlineObjectRun inlineObject)
		{
			Debug.Assert(inlineObject.VisualLine != null);
			
			// Remove inline object if its already added, can happen e.g. when recreating textrun for word-wrapping
			bool alreadyAdded = false;
			for (int i = 0; i < inlineObjects.Count; i++) {
				if (inlineObjects[i].Element == inlineObject.Element) {
					RemoveInlineObjectRun(inlineObjects[i], true);
					inlineObjects.RemoveAt(i);
					alreadyAdded = true;
					break;
				}
			}
			
			inlineObjects.Add(inlineObject);
			if (!alreadyAdded) {
				AddVisualChild(inlineObject.Element);
			}
			inlineObject.Element.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
			inlineObject.desiredSize = inlineObject.Element.DesiredSize;
		}
		
		void MeasureInlineObjects()
		{
			// As part of MeasureOverride(), re-measure the inline objects
			foreach (InlineObjectRun inlineObject in inlineObjects) {
				if (inlineObject.VisualLine.IsDisposed) {
					// Don't re-measure inline objects that are going to be removed anyways.
					// If the inline object will be reused in a different VisualLine, we'll measure it in the AddInlineObject() call.
					continue;
				}
				inlineObject.Element.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
				if (!inlineObject.Element.DesiredSize.IsClose(inlineObject.desiredSize)) {
					// the element changed size -> recreate its parent visual line
					inlineObject.desiredSize = inlineObject.Element.DesiredSize;
					if (allVisualLines.Remove(inlineObject.VisualLine)) {
						DisposeVisualLine(inlineObject.VisualLine);
					}
				}
			}
		}
		
		List<VisualLine> visualLinesWithOutstandingInlineObjects = new List<VisualLine>();
		
		void RemoveInlineObjects(VisualLine visualLine)
		{
			// Delay removing inline objects:
			// A document change immediately invalidates affected visual lines, but it does not
			// cause an immediate redraw.
			// To prevent inline objects from flickering when they are recreated, we delay removing
			// inline objects until the next redraw.
			if (visualLine.hasInlineObjects) {
				visualLinesWithOutstandingInlineObjects.Add(visualLine);
			}
		}
		
		/// <summary>
		/// Remove the inline objects that were marked for removal.
		/// </summary>
		void RemoveInlineObjectsNow()
		{
			if (visualLinesWithOutstandingInlineObjects.Count == 0)
				return;
			inlineObjects.RemoveAll(
				ior => {
					if (visualLinesWithOutstandingInlineObjects.Contains(ior.VisualLine)) {
						RemoveInlineObjectRun(ior, false);
						return true;
					}
					return false;
				});
			visualLinesWithOutstandingInlineObjects.Clear();
		}

		// Remove InlineObjectRun.Element from TextLayer.
		// Caller of RemoveInlineObjectRun will remove it from inlineObjects collection.
		void RemoveInlineObjectRun(InlineObjectRun ior, bool keepElement)
		{
			if (!keepElement && ior.Element.IsKeyboardFocusWithin) {
				// When the inline element that has the focus is removed, WPF will reset the
				// focus to the main window without raising appropriate LostKeyboardFocus events.
				// To work around this, we manually set focus to the next focusable parent.
				UIElement element = this;
				while (element != null && !element.Focusable) {
					element = VisualTreeHelper.GetParent(element) as UIElement;
				}
				if (element != null)
					Keyboard.Focus(element);
			}
			ior.VisualLine = null;
			if (!keepElement)
				RemoveVisualChild(ior.Element);
		}
		#endregion
		
		#region Brushes
		/// <summary>
		/// NonPrintableCharacterBrush dependency property.
		/// </summary>
		public static readonly DependencyProperty NonPrintableCharacterBrushProperty =
			DependencyProperty.Register("NonPrintableCharacterBrush", typeof(Brush), typeof(TextView),
			                            new FrameworkPropertyMetadata(Brushes.LightGray));
		
		/// <summary>
		/// Gets/sets the Brush used for displaying non-printable characters.
		/// </summary>
		public Brush NonPrintableCharacterBrush {
			get { return (Brush)GetValue(NonPrintableCharacterBrushProperty); }
			set { SetValue(NonPrintableCharacterBrushProperty, value); }
		}
		#endregion
		
		#region Redraw methods / VisualLine invalidation
		/// <summary>
		/// Causes the text editor to regenerate all visual lines.
		/// </summary>
		public void Redraw()
		{
			Redraw(DispatcherPriority.Normal);
		}
		
		/// <summary>
		/// Causes the text editor to regenerate all visual lines.
		/// </summary>
		public void Redraw(DispatcherPriority redrawPriority)
		{
			VerifyAccess();
			ClearVisualLines();
			InvalidateMeasure(redrawPriority);
		}
		
		/// <summary>
		/// Causes the text editor to regenerate the specified visual line.
		/// </summary>
		public void Redraw(VisualLine visualLine, DispatcherPriority redrawPriority = DispatcherPriority.Normal)
		{
			VerifyAccess();
			if (allVisualLines.Remove(visualLine)) {
				DisposeVisualLine(visualLine);
				InvalidateMeasure(redrawPriority);
			}
		}
		
		/// <summary>
		/// Causes the text editor to redraw all lines overlapping with the specified segment.
		/// </summary>
		public void Redraw(int offset, int length, DispatcherPriority redrawPriority = DispatcherPriority.Normal)
		{
			VerifyAccess();
			bool changedSomethingBeforeOrInLine = false;
			for (int i = 0; i < allVisualLines.Count; i++) {
				VisualLine visualLine = allVisualLines[i];
				int lineStart = visualLine.FirstDocumentLine.Offset;
				int lineEnd = visualLine.LastDocumentLine.Offset + visualLine.LastDocumentLine.TotalLength;
				if (offset <= lineEnd) {
					changedSomethingBeforeOrInLine = true;
					if (offset + length >= lineStart) {
						allVisualLines.RemoveAt(i--);
						DisposeVisualLine(visualLine);
					}
				}
			}
			if (changedSomethingBeforeOrInLine) {
				// Repaint not only when something in visible area was changed, but also when anything in front of it
				// was changed. We might have to redraw the line number margin. Or the highlighting changed.
				// However, we'll try to reuse the existing VisualLines.
				InvalidateMeasure(redrawPriority);
			}
		}
		
		/// <summary>
		/// Causes a known layer to redraw.
		/// This method does not invalidate visual lines;
		/// use the <see cref="Redraw()"/> method to do that.
		/// </summary>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "knownLayer",
		                                                 Justification="This method is meant to invalidate only a specific layer - I just haven't figured out how to do that, yet.")]
		public void InvalidateLayer(KnownLayer knownLayer)
		{
			InvalidateMeasure(DispatcherPriority.Normal);
		}
		
		/// <summary>
		/// Causes the text editor to redraw all lines overlapping with the specified segment.
		/// Does nothing if segment is null.
		/// </summary>
		public void Redraw(ISegment segment, DispatcherPriority redrawPriority = DispatcherPriority.Normal)
		{
			if (segment != null) {
				Redraw(segment.Offset, segment.Length, redrawPriority);
			}
		}
		
		/// <summary>
		/// Invalidates all visual lines.
		/// The caller of ClearVisualLines() must also call InvalidateMeasure() to ensure
		/// that the visual lines will be recreated.
		/// </summary>
		void ClearVisualLines()
		{
			visibleVisualLines = null;
			if (allVisualLines.Count != 0) {
				foreach (VisualLine visualLine in allVisualLines) {
					DisposeVisualLine(visualLine);
				}
				allVisualLines.Clear();
			}
		}
		
		void DisposeVisualLine(VisualLine visualLine)
		{
			if (newVisualLines != null && newVisualLines.Contains(visualLine)) {
				throw new ArgumentException("Cannot dispose visual line because it is in construction!");
			}
			visibleVisualLines = null;
			visualLine.IsDisposed = true;
			foreach (TextLine textLine in visualLine.TextLines) {
				textLine.Dispose();
			}
			RemoveInlineObjects(visualLine);
		}
		#endregion
		
		#region InvalidateMeasure(DispatcherPriority)
		DispatcherOperation invalidateMeasureOperation;
		
		void InvalidateMeasure(DispatcherPriority priority)
		{
			if (priority >= DispatcherPriority.Render) {
				if (invalidateMeasureOperation != null) {
					invalidateMeasureOperation.Abort();
					invalidateMeasureOperation = null;
				}
				base.InvalidateMeasure();
			} else {
				if (invalidateMeasureOperation != null) {
					invalidateMeasureOperation.Priority = priority;
				} else {
					invalidateMeasureOperation = Dispatcher.BeginInvoke(
						priority,
						new Action(
							delegate {
								invalidateMeasureOperation = null;
								base.InvalidateMeasure();
							}
						)
					);
				}
			}
		}
		#endregion
		
		#region Get(OrConstruct)VisualLine
		/// <summary>
		/// Gets the visual line that contains the document line with the specified number.
		/// Returns null if the document line is outside the visible range.
		/// </summary>
		public VisualLine GetVisualLine(int documentLineNumber)
		{
			// TODO: EnsureVisualLines() ?
			foreach (VisualLine visualLine in allVisualLines) {
				Debug.Assert(visualLine.IsDisposed == false);
				int start = visualLine.FirstDocumentLine.LineNumber;
				int end = visualLine.LastDocumentLine.LineNumber;
				if (documentLineNumber >= start && documentLineNumber <= end)
					return visualLine;
			}
			return null;
		}
		
		/// <summary>
		/// Gets the visual line that contains the document line with the specified number.
		/// If that line is outside the visible range, a new VisualLine for that document line is constructed.
		/// </summary>
		public VisualLine GetOrConstructVisualLine(DocumentLine documentLine)
		{
			if (documentLine == null)
				throw new ArgumentNullException("documentLine");
			if (!this.Document.Lines.Contains(documentLine))
				throw new InvalidOperationException("Line belongs to wrong document");
			VerifyAccess();
			
			VisualLine l = GetVisualLine(documentLine.LineNumber);
			if (l == null) {
				TextRunProperties globalTextRunProperties = CreateGlobalTextRunProperties();
				VisualLineTextParagraphProperties paragraphProperties = CreateParagraphProperties(globalTextRunProperties);
				
				while (heightTree.GetIsCollapsed(documentLine.LineNumber)) {
					documentLine = documentLine.PreviousLine;
				}
				
				l = BuildVisualLine(documentLine,
				                    globalTextRunProperties, paragraphProperties,
				                    elementGenerators.ToArray(), lineTransformers.ToArray(),
				                    lastAvailableSize);
				allVisualLines.Add(l);
				// update all visual top values (building the line might have changed visual top of other lines due to word wrapping)
				foreach (var line in allVisualLines) {
					line.VisualTop = heightTree.GetVisualPosition(line.FirstDocumentLine);
				}
			}
			return l;
		}
		#endregion
		
		#region Visual Lines (fields and properties)
		List<VisualLine> allVisualLines = new List<VisualLine>();
		ReadOnlyCollection<VisualLine> visibleVisualLines;
		double clippedPixelsOnTop;
		List<VisualLine> newVisualLines;
		
		/// <summary>
		/// Gets the currently visible visual lines.
		/// </summary>
		/// <exception cref="VisualLinesInvalidException">
		/// Gets thrown if there are invalid visual lines when this property is accessed.
		/// You can use the <see cref="VisualLinesValid"/> property to check for this case,
		/// or use the <see cref="EnsureVisualLines()"/> method to force creating the visual lines
		/// when they are invalid.
		/// </exception>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1065:DoNotRaiseExceptionsInUnexpectedLocations")]
		public ReadOnlyCollection<VisualLine> VisualLines {
			get {
				if (visibleVisualLines == null)
					throw new VisualLinesInvalidException();
				return visibleVisualLines;
			}
		}
		
		/// <summary>
		/// Gets whether the visual lines are valid.
		/// Will return false after a call to Redraw().
		/// Accessing the visual lines property will cause a <see cref="VisualLinesInvalidException"/>
		/// if this property is <c>false</c>.
		/// </summary>
		public bool VisualLinesValid {
			get { return visibleVisualLines != null; }
		}
		
		/// <summary>
		/// Occurs when the TextView is about to be measured and will regenerate its visual lines.
		/// This event may be used to mark visual lines as invalid that would otherwise be reused.
		/// </summary>
		public event EventHandler<VisualLineConstructionStartEventArgs> VisualLineConstructionStarting;
		
		/// <summary>
		/// Occurs when the TextView was measured and changed its visual lines.
		/// </summary>
		public event EventHandler VisualLinesChanged;
		
		/// <summary>
		/// If the visual lines are invalid, creates new visual lines for the visible part
		/// of the document.
		/// If all visual lines are valid, this method does nothing.
		/// </summary>
		/// <exception cref="InvalidOperationException">The visual line build process is already running.
		/// It is not allowed to call this method during the construction of a visual line.</exception>
		public void EnsureVisualLines()
		{
			Dispatcher.VerifyAccess();
			if (inMeasure)
				throw new InvalidOperationException("The visual line build process is already running! Cannot EnsureVisualLines() during Measure!");
			if (!VisualLinesValid) {
				// increase priority for re-measure
				InvalidateMeasure(DispatcherPriority.Normal);
				// force immediate re-measure
				UpdateLayout();
			}
			// Sometimes we still have invalid lines after UpdateLayout - work around the problem
			// by calling MeasureOverride directly.
			if (!VisualLinesValid) {
				Debug.WriteLine("UpdateLayout() failed in EnsureVisualLines");
				MeasureOverride(lastAvailableSize);
			}
			if (!VisualLinesValid)
				throw new VisualLinesInvalidException("Internal error: visual lines invalid after EnsureVisualLines call");
		}
		#endregion
		
		#region Measure
		/// <summary>
		/// Additonal amount that allows horizontal scrolling past the end of the longest line.
		/// This is necessary to ensure the caret always is visible, even when it is at the end of the longest line.
		/// </summary>
		const double AdditionalHorizontalScrollAmount = 30;
		
		Size lastAvailableSize;
		bool inMeasure;
		
		/// <inheritdoc/>
		protected override Size MeasureOverride(Size availableSize)
		{
			// We don't support infinite available width, so we'll limit it to 32000 pixels.
			if (availableSize.Width > 32000)
				availableSize.Width = 32000;
			
			if (!canHorizontallyScroll && !availableSize.Width.IsClose(lastAvailableSize.Width))
				ClearVisualLines();
			lastAvailableSize = availableSize;
			
			foreach (UIElement layer in layers) {
				layer.Measure(availableSize);
			}
			MeasureInlineObjects();
			
			InvalidateVisual(); // = InvalidateArrange+InvalidateRender
			
			double maxWidth;
			if (document == null) {
				// no document -> create empty list of lines
				allVisualLines = new List<VisualLine>();
				visibleVisualLines = allVisualLines.AsReadOnly();
				maxWidth = 0;
			} else {
				inMeasure = true;
				try {
					maxWidth = CreateAndMeasureVisualLines(availableSize);
				} finally {
					inMeasure = false;
				}
			}
			
			// remove inline objects only at the end, so that inline objects that were re-used are not removed from the editor
			RemoveInlineObjectsNow();
			
			maxWidth += AdditionalHorizontalScrollAmount;
			double heightTreeHeight = this.DocumentHeight;
			TextEditorOptions options = this.Options;
			if (options.AllowScrollBelowDocument) {
				if (!double.IsInfinity(scrollViewport.Height)) {
					heightTreeHeight = Math.Max(heightTreeHeight, Math.Min(heightTreeHeight - 50, scrollOffset.Y) + scrollViewport.Height);
				}
			}
			
			textLayer.SetVisualLines(visibleVisualLines);
			
			SetScrollData(availableSize,
			              new Size(maxWidth, heightTreeHeight),
			              scrollOffset);
			if (VisualLinesChanged != null)
				VisualLinesChanged(this, EventArgs.Empty);
			
			return new Size(
				canHorizontallyScroll ? Math.Min(availableSize.Width, maxWidth) : maxWidth,
				canVerticallyScroll ? Math.Min(availableSize.Height, heightTreeHeight) : heightTreeHeight
			);
		}
		
		/// <summary>
		/// Build all VisualLines in the visible range.
		/// </summary>
		/// <returns>Width the longest line</returns>
		double CreateAndMeasureVisualLines(Size availableSize)
		{
			TextRunProperties globalTextRunProperties = CreateGlobalTextRunProperties();
			VisualLineTextParagraphProperties paragraphProperties = CreateParagraphProperties(globalTextRunProperties);
			
			Debug.WriteLine("Measure availableSize=" + availableSize + ", scrollOffset=" + scrollOffset);
			var firstLineInView = heightTree.GetLineByVisualPosition(scrollOffset.Y);
			
			// number of pixels clipped from the first visual line(s)
			clippedPixelsOnTop = scrollOffset.Y - heightTree.GetVisualPosition(firstLineInView);
			Debug.Assert(clippedPixelsOnTop >= 0);
			
			newVisualLines = new List<VisualLine>();
			
			if (VisualLineConstructionStarting != null)
				VisualLineConstructionStarting(this, new VisualLineConstructionStartEventArgs(firstLineInView));
			
			var elementGeneratorsArray = elementGenerators.ToArray();
			var lineTransformersArray = lineTransformers.ToArray();
			var nextLine = firstLineInView;
			double maxWidth = 0;
			double yPos = -clippedPixelsOnTop;
			while (yPos < availableSize.Height && nextLine != null) {
				VisualLine visualLine = GetVisualLine(nextLine.LineNumber);
				if (visualLine == null) {
					visualLine = BuildVisualLine(nextLine,
					                             globalTextRunProperties, paragraphProperties,
					                             elementGeneratorsArray, lineTransformersArray,
					                             availableSize);
				}
				
				visualLine.VisualTop = scrollOffset.Y + yPos;
				
				nextLine = visualLine.LastDocumentLine.NextLine;
				
				yPos += visualLine.Height;
				
				foreach (TextLine textLine in visualLine.TextLines) {
					if (textLine.WidthIncludingTrailingWhitespace > maxWidth)
						maxWidth = textLine.WidthIncludingTrailingWhitespace;
				}
				
				newVisualLines.Add(visualLine);
			}
			
			foreach (VisualLine line in allVisualLines) {
				Debug.Assert(line.IsDisposed == false);
				if (!newVisualLines.Contains(line))
					DisposeVisualLine(line);
			}
			
			allVisualLines = newVisualLines;
			// visibleVisualLines = readonly copy of visual lines
			visibleVisualLines = new ReadOnlyCollection<VisualLine>(newVisualLines.ToArray());
			newVisualLines = null;
			
			if (allVisualLines.Any(line => line.IsDisposed)) {
				throw new InvalidOperationException("A visual line was disposed even though it is still in use.\n" +
				                                    "This can happen when Redraw() is called during measure for lines " +
				                                    "that are already constructed.");
			}
			return maxWidth;
		}
		#endregion
		
		#region BuildVisualLine
		TextFormatter formatter;
		internal TextViewCachedElements cachedElements;
		
		TextRunProperties CreateGlobalTextRunProperties()
		{
			var p = new GlobalTextRunProperties();
			p.typeface = this.CreateTypeface();
			p.fontRenderingEmSize = FontSize;
			p.foregroundBrush = (Brush)GetValue(Control.ForegroundProperty);
			ExtensionMethods.CheckIsFrozen(p.foregroundBrush);
			p.cultureInfo = CultureInfo.CurrentCulture;
			return p;
		}
		
		VisualLineTextParagraphProperties CreateParagraphProperties(TextRunProperties defaultTextRunProperties)
		{
			return new VisualLineTextParagraphProperties {
				defaultTextRunProperties = defaultTextRunProperties,
				textWrapping = canHorizontallyScroll ? TextWrapping.NoWrap : TextWrapping.Wrap,
				tabSize = Options.IndentationSize * WideSpaceWidth
			};
		}
		
		VisualLine BuildVisualLine(DocumentLine documentLine,
		                           TextRunProperties globalTextRunProperties,
		                           VisualLineTextParagraphProperties paragraphProperties,
		                           VisualLineElementGenerator[] elementGeneratorsArray,
		                           IVisualLineTransformer[] lineTransformersArray,
		                           Size availableSize)
		{
			if (heightTree.GetIsCollapsed(documentLine.LineNumber))
				throw new InvalidOperationException("Trying to build visual line from collapsed line");
			
			Debug.WriteLine("Building line " + documentLine.LineNumber);
			
			VisualLine visualLine = new VisualLine(this, documentLine);
			VisualLineTextSource textSource = new VisualLineTextSource(visualLine) {
				Document = document,
				GlobalTextRunProperties = globalTextRunProperties,
				TextView = this
			};
			
			visualLine.ConstructVisualElements(textSource, elementGeneratorsArray);
			
			#if DEBUG
			for (int i = visualLine.FirstDocumentLine.LineNumber + 1; i <= visualLine.LastDocumentLine.LineNumber; i++) {
				if (!heightTree.GetIsCollapsed(i))
					throw new InvalidOperationException("Line " + i + " was skipped by a VisualLineElementGenerator, but it is not collapsed.");
			}
			#endif
			
			visualLine.RunTransformers(textSource, lineTransformersArray);
			
			// now construct textLines:
			int textOffset = 0;
			TextLineBreak lastLineBreak = null;
			var textLines = new List<TextLine>();
			paragraphProperties.indent = 0;
			paragraphProperties.firstLineInParagraph = true;
			while (textOffset <= visualLine.VisualLength) {
				TextLine textLine = formatter.FormatLine(
					textSource,
					textOffset,
					availableSize.Width,
					paragraphProperties,
					lastLineBreak
				);
				textLines.Add(textLine);
				textOffset += textLine.Length;
				
				// exit loop so that we don't do the indentation calculation if there's only a single line
				if (textOffset >= visualLine.VisualLength)
					break;
				
				if (paragraphProperties.firstLineInParagraph) {
					paragraphProperties.firstLineInParagraph = false;
					
					TextEditorOptions options = this.Options;
					double indentation = 0;
					if (options.InheritWordWrapIndentation) {
						// determine indentation for next line:
						int indentVisualColumn = GetIndentationVisualColumn(visualLine);
						if (indentVisualColumn > 0 && indentVisualColumn < textOffset) {
							indentation = textLine.GetDistanceFromCharacterHit(new CharacterHit(indentVisualColumn, 0));
						}
					}
					indentation += options.WordWrapIndentation;
					// apply the calculated indentation unless it's more than half of the text editor size:
					if (indentation > 0 && indentation * 2 < availableSize.Width)
						paragraphProperties.indent = indentation;
				}
				lastLineBreak = textLine.GetTextLineBreak();
			}
			visualLine.SetTextLines(textLines);
			heightTree.SetHeight(visualLine.FirstDocumentLine, visualLine.Height);
			return visualLine;
		}
		
		static int GetIndentationVisualColumn(VisualLine visualLine)
		{
			if (visualLine.Elements.Count == 0)
				return 0;
			int column = 0;
			int elementIndex = 0;
			VisualLineElement element = visualLine.Elements[elementIndex];
			while (element.IsWhitespace(column)) {
				column++;
				if (column == element.VisualColumn + element.VisualLength) {
					elementIndex++;
					if (elementIndex == visualLine.Elements.Count)
						break;
					element = visualLine.Elements[elementIndex];
				}
			}
			return column;
		}
		#endregion
		
		#region Arrange
		/// <summary>
		/// Arrange implementation.
		/// </summary>
		protected override Size ArrangeOverride(Size finalSize)
		{
			EnsureVisualLines();
			
			foreach (UIElement layer in layers) {
				layer.Arrange(new Rect(new Point(0, 0), finalSize));
			}
			
			if (document == null || allVisualLines.Count == 0)
				return finalSize;
			
			// validate scroll position
			Vector newScrollOffset = scrollOffset;
			if (scrollOffset.X + finalSize.Width > scrollExtent.Width) {
				newScrollOffset.X = Math.Max(0, scrollExtent.Width - finalSize.Width);
			}
			if (scrollOffset.Y + finalSize.Height > scrollExtent.Height) {
				newScrollOffset.Y = Math.Max(0, scrollExtent.Height - finalSize.Height);
			}
			if (SetScrollData(scrollViewport, scrollExtent, newScrollOffset))
				InvalidateMeasure(DispatcherPriority.Normal);
			
			//Debug.WriteLine("Arrange finalSize=" + finalSize + ", scrollOffset=" + scrollOffset);
			
//			double maxWidth = 0;
			
			if (visibleVisualLines != null) {
				Point pos = new Point(-scrollOffset.X, -clippedPixelsOnTop);
				foreach (VisualLine visualLine in visibleVisualLines) {
					int offset = 0;
					foreach (TextLine textLine in visualLine.TextLines) {
						foreach (var span in textLine.GetTextRunSpans()) {
							InlineObjectRun inline = span.Value as InlineObjectRun;
							if (inline != null && inline.VisualLine != null) {
								Debug.Assert(inlineObjects.Contains(inline));
								double distance = textLine.GetDistanceFromCharacterHit(new CharacterHit(offset, 0));
								inline.Element.Arrange(new Rect(new Point(pos.X + distance, pos.Y), inline.Element.DesiredSize));
							}
							offset += span.Length;
						}
						pos.Y += textLine.Height;
					}
				}
			}
			InvalidateCursor();
			
			return finalSize;
		}
		#endregion
		
		#region Render
		readonly ObserveAddRemoveCollection<IBackgroundRenderer> backgroundRenderers;
		
		/// <summary>
		/// Gets the list of background renderers.
		/// </summary>
		public IList<IBackgroundRenderer> BackgroundRenderers {
			get { return backgroundRenderers; }
		}
		
		void BackgroundRenderer_Added(IBackgroundRenderer renderer)
		{
			ConnectToTextView(renderer);
			InvalidateLayer(renderer.Layer);
		}
		
		void BackgroundRenderer_Removed(IBackgroundRenderer renderer)
		{
			DisconnectFromTextView(renderer);
			InvalidateLayer(renderer.Layer);
		}
		
		/// <inheritdoc/>
		protected override void OnRender(DrawingContext drawingContext)
		{
			RenderBackground(drawingContext, KnownLayer.Background);
		}
		
		internal void RenderBackground(DrawingContext drawingContext, KnownLayer layer)
		{
			foreach (IBackgroundRenderer bg in backgroundRenderers) {
				if (bg.Layer == layer) {
					bg.Draw(this, drawingContext);
				}
			}
		}
		
		internal void ArrangeTextLayer(IList<VisualLineDrawingVisual> visuals)
		{
			Point pos = new Point(-scrollOffset.X, -clippedPixelsOnTop);
			foreach (VisualLineDrawingVisual visual in visuals) {
				TranslateTransform t = visual.Transform as TranslateTransform;
				if (t == null || t.X != pos.X || t.Y != pos.Y) {
					visual.Transform = new TranslateTransform(pos.X, pos.Y);
					visual.Transform.Freeze();
				}
				pos.Y += visual.Height;
			}
		}
		#endregion
		
		#region IScrollInfo implementation
		/// <summary>
		/// Size of the document, in pixels.
		/// </summary>
		Size scrollExtent;
		
		/// <summary>
		/// Offset of the scroll position.
		/// </summary>
		Vector scrollOffset;
		
		/// <summary>
		/// Size of the viewport.
		/// </summary>
		Size scrollViewport;
		
		void ClearScrollData()
		{
			SetScrollData(new Size(), new Size(), new Vector());
		}
		
		bool SetScrollData(Size viewport, Size extent, Vector offset)
		{
			if (!(viewport.IsClose(this.scrollViewport)
			      && extent.IsClose(this.scrollExtent)
			      && offset.IsClose(this.scrollOffset)))
			{
				this.scrollViewport = viewport;
				this.scrollExtent = extent;
				SetScrollOffset(offset);
				this.OnScrollChange();
				return true;
			}
			return false;
		}
		
		void OnScrollChange()
		{
			ScrollViewer scrollOwner = ((IScrollInfo)this).ScrollOwner;
			if (scrollOwner != null) {
				scrollOwner.InvalidateScrollInfo();
			}
		}
		
		bool canVerticallyScroll;
		bool IScrollInfo.CanVerticallyScroll {
			get { return canVerticallyScroll; }
			set {
				if (canVerticallyScroll != value) {
					canVerticallyScroll = value;
					InvalidateMeasure(DispatcherPriority.Normal);
				}
			}
		}
		bool canHorizontallyScroll;
		bool IScrollInfo.CanHorizontallyScroll {
			get { return canHorizontallyScroll; }
			set {
				if (canHorizontallyScroll != value) {
					canHorizontallyScroll = value;
					ClearVisualLines();
					InvalidateMeasure(DispatcherPriority.Normal);
				}
			}
		}
		
		double IScrollInfo.ExtentWidth {
			get { return scrollExtent.Width; }
		}
		
		double IScrollInfo.ExtentHeight {
			get { return scrollExtent.Height; }
		}
		
		double IScrollInfo.ViewportWidth {
			get { return scrollViewport.Width; }
		}
		
		double IScrollInfo.ViewportHeight {
			get { return scrollViewport.Height; }
		}
		
		/// <summary>
		/// Gets the horizontal scroll offset.
		/// </summary>
		public double HorizontalOffset {
			get { return scrollOffset.X; }
		}
		
		/// <summary>
		/// Gets the vertical scroll offset.
		/// </summary>
		public double VerticalOffset {
			get { return scrollOffset.Y; }
		}
		
		/// <summary>
		/// Gets the scroll offset;
		/// </summary>
		public Vector ScrollOffset {
			get { return scrollOffset; }
		}
		
		/// <summary>
		/// Occurs when the scroll offset has changed.
		/// </summary>
		public event EventHandler ScrollOffsetChanged;
		
		void SetScrollOffset(Vector vector)
		{
			if (!scrollOffset.IsClose(vector)) {
				scrollOffset = vector;
				if (ScrollOffsetChanged != null)
					ScrollOffsetChanged(this, EventArgs.Empty);
			}
		}
		
		ScrollViewer IScrollInfo.ScrollOwner { get; set; }
		
		void IScrollInfo.LineUp()
		{
			((IScrollInfo)this).SetVerticalOffset(scrollOffset.Y - DefaultLineHeight);
		}
		
		void IScrollInfo.LineDown()
		{
			((IScrollInfo)this).SetVerticalOffset(scrollOffset.Y + DefaultLineHeight);
		}
		
		void IScrollInfo.LineLeft()
		{
			((IScrollInfo)this).SetHorizontalOffset(scrollOffset.X - WideSpaceWidth);
		}
		
		void IScrollInfo.LineRight()
		{
			((IScrollInfo)this).SetHorizontalOffset(scrollOffset.X + WideSpaceWidth);
		}
		
		void IScrollInfo.PageUp()
		{
			((IScrollInfo)this).SetVerticalOffset(scrollOffset.Y - scrollViewport.Height);
		}
		
		void IScrollInfo.PageDown()
		{
			((IScrollInfo)this).SetVerticalOffset(scrollOffset.Y + scrollViewport.Height);
		}
		
		void IScrollInfo.PageLeft()
		{
			((IScrollInfo)this).SetHorizontalOffset(scrollOffset.X - scrollViewport.Width);
		}
		
		void IScrollInfo.PageRight()
		{
			((IScrollInfo)this).SetHorizontalOffset(scrollOffset.X + scrollViewport.Width);
		}
		
		void IScrollInfo.MouseWheelUp()
		{
			((IScrollInfo)this).SetVerticalOffset(
				scrollOffset.Y - (SystemParameters.WheelScrollLines * DefaultLineHeight));
			OnScrollChange();
		}
		
		void IScrollInfo.MouseWheelDown()
		{
			((IScrollInfo)this).SetVerticalOffset(
				scrollOffset.Y + (SystemParameters.WheelScrollLines * DefaultLineHeight));
			OnScrollChange();
		}
		
		void IScrollInfo.MouseWheelLeft()
		{
			((IScrollInfo)this).SetHorizontalOffset(
				scrollOffset.X - (SystemParameters.WheelScrollLines * WideSpaceWidth));
			OnScrollChange();
		}
		
		void IScrollInfo.MouseWheelRight()
		{
			((IScrollInfo)this).SetHorizontalOffset(
				scrollOffset.X + (SystemParameters.WheelScrollLines * WideSpaceWidth));
			OnScrollChange();
		}
		
		double wideSpaceWidth; // Width of an 'x'. Used as basis for the tab width, and for scrolling.
		double defaultLineHeight; // Height of a line containing 'x'. Used for scrolling.
		
		double WideSpaceWidth {
			get {
				if (wideSpaceWidth == 0) {
					MeasureWideSpaceWidthAndDefaultLineHeight();
				}
				return wideSpaceWidth;
			}
		}
		
		double DefaultLineHeight {
			get {
				if (defaultLineHeight == 0) {
					MeasureWideSpaceWidthAndDefaultLineHeight();
				}
				return defaultLineHeight;
			}
		}
		
		void MeasureWideSpaceWidthAndDefaultLineHeight()
		{
			if (formatter != null) {
				var textRunProperties = CreateGlobalTextRunProperties();
				using (var line = formatter.FormatLine(
					new SimpleTextSource("x", textRunProperties),
					0, 32000,
					new VisualLineTextParagraphProperties { defaultTextRunProperties = textRunProperties },
					null))
				{
					wideSpaceWidth = Math.Max(1, line.WidthIncludingTrailingWhitespace);
					defaultLineHeight = Math.Max(1, line.Height);
				}
			} else {
				wideSpaceWidth = FontSize / 2;
				defaultLineHeight = FontSize + 3;
			}
			// Update heightTree.DefaultLineHeight, if a document is loaded.
			if (heightTree != null)
				heightTree.DefaultLineHeight = defaultLineHeight;
		}
		
		static double ValidateVisualOffset(double offset)
		{
			if (double.IsNaN(offset))
				throw new ArgumentException("offset must not be NaN");
			if (offset < 0)
				return 0;
			else
				return offset;
		}
		
		void IScrollInfo.SetHorizontalOffset(double offset)
		{
			offset = ValidateVisualOffset(offset);
			if (!scrollOffset.X.IsClose(offset)) {
				SetScrollOffset(new Vector(offset, scrollOffset.Y));
				InvalidateVisual();
				textLayer.InvalidateVisual();
			}
		}
		
		void IScrollInfo.SetVerticalOffset(double offset)
		{
			offset = ValidateVisualOffset(offset);
			if (!scrollOffset.Y.IsClose(offset)) {
				SetScrollOffset(new Vector(scrollOffset.X, offset));
				InvalidateMeasure(DispatcherPriority.Normal);
			}
		}
		
		Rect IScrollInfo.MakeVisible(Visual visual, Rect rectangle)
		{
			if (rectangle.IsEmpty || visual == null || visual == this || !this.IsAncestorOf(visual)) {
				return Rect.Empty;
			}
			// Convert rectangle into our coordinate space.
			GeneralTransform childTransform = visual.TransformToAncestor(this);
			rectangle = childTransform.TransformBounds(rectangle);
			
			MakeVisible(Rect.Offset(rectangle, scrollOffset));
			
			return rectangle;
		}
		
		/// <summary>
		/// Scrolls the text view so that the specified rectangle gets visible.
		/// </summary>
		public void MakeVisible(Rect rectangle)
		{
			Rect visibleRectangle = new Rect(scrollOffset.X, scrollOffset.Y,
			                                 scrollViewport.Width, scrollViewport.Height);
			Vector newScrollOffset = scrollOffset;
			if (rectangle.Left < visibleRectangle.Left) {
				if (rectangle.Right > visibleRectangle.Right) {
					newScrollOffset.X = rectangle.Left + rectangle.Width / 2;
				} else {
					newScrollOffset.X = rectangle.Left;
				}
			} else if (rectangle.Right > visibleRectangle.Right) {
				newScrollOffset.X = rectangle.Right - scrollViewport.Width;
			}
			if (rectangle.Top < visibleRectangle.Top) {
				if (rectangle.Bottom > visibleRectangle.Bottom) {
					newScrollOffset.Y = rectangle.Top + rectangle.Height / 2;
				} else {
					newScrollOffset.Y = rectangle.Top;
				}
			} else if (rectangle.Bottom > visibleRectangle.Bottom) {
				newScrollOffset.Y = rectangle.Bottom - scrollViewport.Height;
			}
			newScrollOffset.X = ValidateVisualOffset(newScrollOffset.X);
			newScrollOffset.Y = ValidateVisualOffset(newScrollOffset.Y);
			if (!scrollOffset.IsClose(newScrollOffset)) {
				SetScrollOffset(newScrollOffset);
				this.OnScrollChange();
				InvalidateMeasure(DispatcherPriority.Normal);
			}
		}
		#endregion
		
		#region Visual element mouse handling
		/// <inheritdoc/>
		protected override HitTestResult HitTestCore(PointHitTestParameters hitTestParameters)
		{
			// accept clicks even where the text area draws no background
			return new PointHitTestResult(this, hitTestParameters.HitPoint);
		}
		
		[ThreadStatic] static bool invalidCursor;
		
		/// <summary>
		/// Updates the mouse cursor by calling <see cref="Mouse.UpdateCursor"/>, but with input priority.
		/// </summary>
		public static void InvalidateCursor()
		{
			if (!invalidCursor) {
				invalidCursor = true;
				Dispatcher.CurrentDispatcher.BeginInvoke(
					DispatcherPriority.Input,
					new Action(
						delegate {
							invalidCursor = false;
							Mouse.UpdateCursor();
						}));
			}
		}
		
		/// <inheritdoc/>
		protected override void OnQueryCursor(QueryCursorEventArgs e)
		{
			VisualLineElement element = GetVisualLineElementFromPosition(e.GetPosition(this) + scrollOffset);
			if (element != null) {
				element.OnQueryCursor(e);
			}
		}
		
		/// <inheritdoc/>
		protected override void OnMouseDown(MouseButtonEventArgs e)
		{
			base.OnMouseDown(e);
			if (!e.Handled) {
				EnsureVisualLines();
				VisualLineElement element = GetVisualLineElementFromPosition(e.GetPosition(this) + scrollOffset);
				if (element != null) {
					element.OnMouseDown(e);
				}
			}
		}
		
		/// <inheritdoc/>
		protected override void OnMouseUp(MouseButtonEventArgs e)
		{
			base.OnMouseUp(e);
			if (!e.Handled) {
				EnsureVisualLines();
				VisualLineElement element = GetVisualLineElementFromPosition(e.GetPosition(this) + scrollOffset);
				if (element != null) {
					element.OnMouseUp(e);
				}
			}
		}
		#endregion
		
		#region Getting elements from Visual Position
		/// <summary>
		/// Gets the visual line at the specified document position (relative to start of document).
		/// Returns null if there is no visual line for the position (e.g. the position is outside the visible
		/// text area).
		/// </summary>
		public VisualLine GetVisualLineFromVisualTop(double visualTop)
		{
			// TODO: change this method to also work outside the visible range -
			// required to make GetPosition work as expected!
			EnsureVisualLines();
			foreach (VisualLine vl in this.VisualLines) {
				if (visualTop < vl.VisualTop)
					continue;
				if (visualTop < vl.VisualTop + vl.Height)
					return vl;
			}
			return null;
		}
		
		/// <summary>
		/// Gets the visual top position (relative to start of document) from a document line number.
		/// </summary>
		public double GetVisualTopByDocumentLine(int line)
		{
			VerifyAccess();
			if (heightTree == null)
				throw ThrowUtil.NoDocumentAssigned();
			return heightTree.GetVisualPosition(heightTree.GetLineByNumber(line));
		}
		
		VisualLineElement GetVisualLineElementFromPosition(Point visualPosition)
		{
			VisualLine vl = GetVisualLineFromVisualTop(visualPosition.Y);
			if (vl != null) {
				int column = vl.GetVisualColumnFloor(visualPosition);
//				Debug.WriteLine(vl.FirstDocumentLine.LineNumber + " vc " + column);
				foreach (VisualLineElement element in vl.Elements) {
					if (element.VisualColumn + element.VisualLength <= column)
						continue;
					return element;
				}
			}
			return null;
		}
		#endregion
		
		#region Visual Position <-> TextViewPosition
		/// <summary>
		/// Gets the visual position from a text view position.
		/// </summary>
		/// <param name="position">The text view position.</param>
		/// <param name="yPositionMode">The mode how to retrieve the Y position.</param>
		/// <returns>The position in WPF device-independent pixels relative
		/// to the top left corner of the document.</returns>
		public Point GetVisualPosition(TextViewPosition position, VisualYPosition yPositionMode)
		{
			VerifyAccess();
			if (this.Document == null)
				throw ThrowUtil.NoDocumentAssigned();
			DocumentLine documentLine = this.Document.GetLineByNumber(position.Line);
			VisualLine visualLine = GetOrConstructVisualLine(documentLine);
			int visualColumn = position.VisualColumn;
			if (visualColumn < 0) {
				int offset = documentLine.Offset + position.Column - 1;
				visualColumn = visualLine.GetVisualColumn(offset - visualLine.FirstDocumentLine.Offset);
			}
			return visualLine.GetVisualPosition(visualColumn, yPositionMode);
		}
		
		/// <summary>
		/// Gets the text view position from the specified visual position.
		/// </summary>
		/// <param name="visualPosition">The position in WPF device-independent pixels relative
		/// to the top left corner of the document.</param>
		/// <returns>The logical position, or null if the position is outside the document.</returns>
		public TextViewPosition? GetPosition(Point visualPosition)
		{
			VerifyAccess();
			if (this.Document == null)
				throw ThrowUtil.NoDocumentAssigned();
			VisualLine line = GetVisualLineFromVisualTop(visualPosition.Y);
			if (line == null)
				return null;
			int visualColumn = line.GetVisualColumn(visualPosition);
			int documentOffset = line.GetRelativeOffset(visualColumn) + line.FirstDocumentLine.Offset;
			return new TextViewPosition(document.GetLocation(documentOffset), visualColumn);
		}
		#endregion
		
		#region Service Provider
		readonly ServiceContainer services = new ServiceContainer();
		
		/// <summary>
		/// Gets a service container used to associate services with the text view.
		/// </summary>
		public ServiceContainer Services {
			get { return services; }
		}
		
		object IServiceProvider.GetService(Type serviceType)
		{
			return services.GetService(serviceType);
		}
		
		void ConnectToTextView(object obj)
		{
			ITextViewConnect c = obj as ITextViewConnect;
			if (c != null)
				c.AddToTextView(this);
		}
		
		void DisconnectFromTextView(object obj)
		{
			ITextViewConnect c = obj as ITextViewConnect;
			if (c != null)
				c.RemoveFromTextView(this);
		}
		#endregion
		
		#region MouseHover
		/// <summary>
		/// The PreviewMouseHover event.
		/// </summary>
		public static readonly RoutedEvent PreviewMouseHoverEvent =
			EventManager.RegisterRoutedEvent("PreviewMouseHover", RoutingStrategy.Tunnel,
			                                 typeof(MouseEventHandler), typeof(TextView));
		/// <summary>
		/// The MouseHover event.
		/// </summary>
		public static readonly RoutedEvent MouseHoverEvent =
			EventManager.RegisterRoutedEvent("MouseHover", RoutingStrategy.Bubble,
			                                 typeof(MouseEventHandler), typeof(TextView));
		
		/// <summary>
		/// The PreviewMouseHoverStopped event.
		/// </summary>
		public static readonly RoutedEvent PreviewMouseHoverStoppedEvent =
			EventManager.RegisterRoutedEvent("PreviewMouseHoverStopped", RoutingStrategy.Tunnel,
			                                 typeof(MouseEventHandler), typeof(TextView));
		/// <summary>
		/// The MouseHoverStopped event.
		/// </summary>
		public static readonly RoutedEvent MouseHoverStoppedEvent =
			EventManager.RegisterRoutedEvent("MouseHoverStopped", RoutingStrategy.Bubble,
			                                 typeof(MouseEventHandler), typeof(TextView));
		
		
		/// <summary>
		/// Occurs when the mouse has hovered over a fixed location for some time.
		/// </summary>
		public event MouseEventHandler PreviewMouseHover {
			add { AddHandler(PreviewMouseHoverEvent, value); }
			remove { RemoveHandler(PreviewMouseHoverEvent, value); }
		}
		
		/// <summary>
		/// Occurs when the mouse has hovered over a fixed location for some time.
		/// </summary>
		public event MouseEventHandler MouseHover {
			add { AddHandler(MouseHoverEvent, value); }
			remove { RemoveHandler(MouseHoverEvent, value); }
		}
		
		/// <summary>
		/// Occurs when the mouse had previously hovered but now started moving again.
		/// </summary>
		public event MouseEventHandler PreviewMouseHoverStopped {
			add { AddHandler(PreviewMouseHoverStoppedEvent, value); }
			remove { RemoveHandler(PreviewMouseHoverStoppedEvent, value); }
		}
		
		/// <summary>
		/// Occurs when the mouse had previously hovered but now started moving again.
		/// </summary>
		public event MouseEventHandler MouseHoverStopped {
			add { AddHandler(MouseHoverStoppedEvent, value); }
			remove { RemoveHandler(MouseHoverStoppedEvent, value); }
		}
		
		MouseHoverLogic hoverLogic;
		
		void RaiseHoverEventPair(MouseEventArgs e, RoutedEvent tunnelingEvent, RoutedEvent bubblingEvent)
		{
			var mouseDevice = e.MouseDevice;
			var stylusDevice = e.StylusDevice;
			int inputTime = Environment.TickCount;
			var args1 = new MouseEventArgs(mouseDevice, inputTime, stylusDevice) {
				RoutedEvent = tunnelingEvent,
				Source = this
			};
			RaiseEvent(args1);
			var args2 = new MouseEventArgs(mouseDevice, inputTime, stylusDevice) {
				RoutedEvent = bubblingEvent,
				Source = this,
				Handled = args1.Handled
			};
			RaiseEvent(args2);
		}
		#endregion
		
		/// <summary>
		/// Collapses lines for the purpose of scrolling. <see cref="DocumentLine"/>s marked as collapsed will be hidden
		/// and not used to start the generation of a <see cref="VisualLine"/>.
		/// </summary>
		/// <remarks>
		/// This method is meant for <see cref="VisualLineElementGenerator"/>s that cause <see cref="VisualLine"/>s to span
		/// multiple <see cref="DocumentLine"/>s. Do not call it without providing a corresponding
		/// <see cref="VisualLineElementGenerator"/>.
		/// If you want to create collapsible text sections, see <see cref="Folding.FoldingManager"/>.
		/// 
		/// Note that if you want a VisualLineElement to span from line N to line M, then you need to collapse only the lines
		/// N+1 to M. Do not collapse line N itself.
		/// 
		/// When you no longer need the section to be collapsed, call <see cref="CollapsedLineSection.Uncollapse()"/> on the
		/// <see cref="CollapsedLineSection"/> returned from this method.
		/// </remarks>
		public CollapsedLineSection CollapseLines(DocumentLine start, DocumentLine end)
		{
			VerifyAccess();
			if (heightTree == null)
				throw ThrowUtil.NoDocumentAssigned();
			return heightTree.CollapseText(start, end);
		}
		
		/// <summary>
		/// Gets the height of the document.
		/// </summary>
		public double DocumentHeight {
			get {
				// return 0 if there is no document = no heightTree
				return heightTree != null ? heightTree.TotalHeight : 0;
			}
		}
		
		/// <summary>
		/// Gets the document line at the specified visual position.
		/// </summary>
		public DocumentLine GetDocumentLineByVisualTop(double visualTop)
		{
			VerifyAccess();
			if (heightTree == null)
				throw ThrowUtil.NoDocumentAssigned();
			return heightTree.GetLineByVisualPosition(visualTop);
		}
		
		/// <inheritdoc/>
		protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
		{
			base.OnPropertyChanged(e);
			if (TextFormatterFactory.PropertyChangeAffectsTextFormatter(e.Property)) {
				// first, create the new text formatter:
				RecreateTextFormatter();
				// changing text formatter requires recreating the cached elements
				RecreateCachedElements();
				// and we need to re-measure the font metrics:
				MeasureWideSpaceWidthAndDefaultLineHeight();
			} else if (e.Property == Control.ForegroundProperty
			           || e.Property == TextView.NonPrintableCharacterBrushProperty)
			{
				// changing brushes requires recreating the cached elements
				RecreateCachedElements();
				Redraw();
			}
			if (e.Property == Control.FontFamilyProperty
			    || e.Property == Control.FontSizeProperty
			    || e.Property == Control.FontStretchProperty
			    || e.Property == Control.FontStyleProperty
			    || e.Property == Control.FontWeightProperty)
			{
				// changing font properties requires recreating cached elements
				RecreateCachedElements();
				// and we need to re-measure the font metrics:
				MeasureWideSpaceWidthAndDefaultLineHeight();
				Redraw();
			}
		}
	}
}
