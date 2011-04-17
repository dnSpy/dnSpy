// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Globalization;
using System.Linq.Expressions;
using System.Threading;
using ICSharpCode.AvalonEdit.Utils;

namespace ICSharpCode.AvalonEdit.Document
{
	/// <summary>
	/// This class is the main class of the text model. Basically, it is a <see cref="System.Text.StringBuilder"/> with events.
	/// </summary>
	/// <remarks>
	/// <b>Thread safety:</b>
	/// <inheritdoc cref="VerifyAccess"/>
	/// <para>However, there is a single method that is thread-safe: <see cref="CreateSnapshot()"/> (and its overloads).</para>
	/// </remarks>
	public sealed class TextDocument : ITextSource, INotifyPropertyChanged
	{
		#region Thread ownership
		readonly object lockObject = new object();
		Thread owner = Thread.CurrentThread;
		
		/// <summary>
		/// Verifies that the current thread is the documents owner thread.
		/// Throws an <see cref="InvalidOperationException"/> if the wrong thread accesses the TextDocument.
		/// </summary>
		/// <remarks>
		/// <para>The TextDocument class is not thread-safe. A document instance expects to have a single owner thread
		/// and will throw an <see cref="InvalidOperationException"/> when accessed from another thread.
		/// It is possible to change the owner thread using the <see cref="SetOwnerThread"/> method.</para>
		/// </remarks>
		public void VerifyAccess()
		{
			if (Thread.CurrentThread != owner)
				throw new InvalidOperationException("TextDocument can be accessed only from the thread that owns it.");
		}
		
		/// <summary>
		/// Transfers ownership of the document to another thread. This method can be used to load
		/// a file into a TextDocument on a background thread and then transfer ownership to the UI thread
		/// for displaying the document.
		/// </summary>
		/// <remarks>
		/// <inheritdoc cref="VerifyAccess"/>
		/// <para>
		/// The owner can be set to null, which means that no thread can access the document. But, if the document
		/// has no owner thread, any thread may take ownership by calling <see cref="SetOwnerThread"/>.
		/// </para>
		/// </remarks>
		public void SetOwnerThread(Thread newOwner)
		{
			// We need to lock here to ensure that in the null owner case,
			// only one thread succeeds in taking ownership.
			lock (lockObject) {
				if (owner != null) {
					VerifyAccess();
				}
				owner = newOwner;
			}
		}
		#endregion
		
		#region Fields + Constructor
		readonly Rope<char> rope;
		readonly DocumentLineTree lineTree;
		readonly LineManager lineManager;
		readonly TextAnchorTree anchorTree;
		ChangeTrackingCheckpoint currentCheckpoint;
		
		/// <summary>
		/// Create an empty text document.
		/// </summary>
		public TextDocument()
			: this(string.Empty)
		{
		}
		
		/// <summary>
		/// Create a new text document with the specified initial text.
		/// </summary>
		public TextDocument(IEnumerable<char> initialText)
		{
			if (initialText == null)
				throw new ArgumentNullException("initialText");
			rope = new Rope<char>(initialText);
			lineTree = new DocumentLineTree(this);
			lineManager = new LineManager(lineTree, this);
			lineTrackers.CollectionChanged += delegate {
				lineManager.UpdateListOfLineTrackers();
			};
			
			anchorTree = new TextAnchorTree(this);
			undoStack = new UndoStack();
			FireChangeEvents();
		}
		
		/// <summary>
		/// Create a new text document with the specified initial text.
		/// </summary>
		public TextDocument(ITextSource initialText)
			: this(GetTextFromTextSource(initialText))
		{
		}
		
		// gets the text from a text source, directly retrieving the underlying rope where possible
		static IEnumerable<char> GetTextFromTextSource(ITextSource textSource)
		{
			if (textSource == null)
				throw new ArgumentNullException("textSource");
			
			RopeTextSource rts = textSource as RopeTextSource;
			if (rts != null)
				return rts.GetRope();
			
			TextDocument doc = textSource as TextDocument;
			if (doc != null)
				return doc.rope;
			
			return textSource.Text;
		}
		#endregion
		
		#region Text
		void ThrowIfRangeInvalid(int offset, int length)
		{
			if (offset < 0 || offset > rope.Length) {
				throw new ArgumentOutOfRangeException("offset", offset, "0 <= offset <= " + rope.Length.ToString(CultureInfo.InvariantCulture));
			}
			if (length < 0 || offset + length > rope.Length) {
				throw new ArgumentOutOfRangeException("length", length, "0 <= length, offset(" + offset + ")+length <= " + rope.Length.ToString(CultureInfo.InvariantCulture));
			}
		}
		
		/// <inheritdoc/>
		public string GetText(int offset, int length)
		{
			VerifyAccess();
			return rope.ToString(offset, length);
		}
		
		/// <summary>
		/// Retrieves the text for a portion of the document.
		/// </summary>
		public string GetText(ISegment segment)
		{
			if (segment == null)
				throw new ArgumentNullException("segment");
			return GetText(segment.Offset, segment.Length);
		}
		
		int ITextSource.IndexOfAny(char[] anyOf, int startIndex, int count)
		{
			DebugVerifyAccess(); // frequently called (NewLineFinder), so must be fast in release builds
			return rope.IndexOfAny(anyOf, startIndex, count);
		}
		
		/// <inheritdoc/>
		public char GetCharAt(int offset)
		{
			DebugVerifyAccess(); // frequently called, so must be fast in release builds
			return rope[offset];
		}
		
		WeakReference cachedText;
		
		/// <summary>
		/// Gets/Sets the text of the whole document.
		/// </summary>
		public string Text {
			get {
				VerifyAccess();
				string completeText = cachedText != null ? (cachedText.Target as string) : null;
				if (completeText == null) {
					completeText = rope.ToString();
					cachedText = new WeakReference(completeText);
				}
				return completeText;
			}
			set {
				VerifyAccess();
				if (value == null)
					throw new ArgumentNullException("value");
				Replace(0, rope.Length, value);
			}
		}
		
		/// <inheritdoc/>
		/// <remarks><inheritdoc cref="Changing"/></remarks>
		public event EventHandler TextChanged;
		
		/// <inheritdoc/>
		public int TextLength {
			get {
				VerifyAccess();
				return rope.Length;
			}
		}
		
		/// <summary>
		/// Is raised when the TextLength property changes.
		/// </summary>
		/// <remarks><inheritdoc cref="Changing"/></remarks>
		[Obsolete("This event will be removed in a future version; use the PropertyChanged event instead")]
		public event EventHandler TextLengthChanged;
		
		/// <summary>
		/// Is raised when one of the properties <see cref="Text"/>, <see cref="TextLength"/>, <see cref="LineCount"/>,
		/// <see cref="UndoStack"/> changes.
		/// </summary>
		/// <remarks><inheritdoc cref="Changing"/></remarks>
		public event PropertyChangedEventHandler PropertyChanged;
		
		/// <summary>
		/// Is raised before the document changes.
		/// </summary>
		/// <remarks>
		/// <para>Here is the order in which events are raised during a document update:</para>
		/// <list type="bullet">
		/// <item><description><b><see cref="BeginUpdate">BeginUpdate()</see></b></description>
		///   <list type="bullet">
		///   <item><description>Start of change group (on undo stack)</description></item>
		///   <item><description><see cref="UpdateStarted"/> event is raised</description></item>
		///   </list></item>
		/// <item><description><b><see cref="Insert(int,string)">Insert()</see> / <see cref="Remove(int,int)">Remove()</see> / <see cref="Replace(int,int,string)">Replace()</see></b></description>
		///   <list type="bullet">
		///   <item><description><see cref="Changing"/> event is raised</description></item>
		///   <item><description>The document is changed</description></item>
		///   <item><description><see cref="TextAnchor.Deleted">TextAnchor.Deleted</see> event is raised if anchors were
		///     in the deleted text portion</description></item>
		///   <item><description><see cref="Changed"/> event is raised</description></item>
		///   </list></item>
		/// <item><description><b><see cref="EndUpdate">EndUpdate()</see></b></description>
		///   <list type="bullet">
		///   <item><description><see cref="TextChanged"/> event is raised</description></item>
		///   <item><description><see cref="PropertyChanged"/> event is raised (for the Text, TextLength, LineCount properties, in that order)</description></item>
		///   <item><description>End of change group (on undo stack)</description></item>
		///   <item><description><see cref="UpdateFinished"/> event is raised</description></item>
		///   </list></item>
		/// </list>
		/// <para>
		/// If the insert/remove/replace methods are called without a call to <c>BeginUpdate()</c>,
		/// they will call <c>BeginUpdate()</c> and <c>EndUpdate()</c> to ensure no change happens outside of <c>UpdateStarted</c>/<c>UpdateFinished</c>.
		/// </para><para>
		/// There can be multiple document changes between the <c>BeginUpdate()</c> and <c>EndUpdate()</c> calls.
		/// In this case, the events associated with EndUpdate will be raised only once after the whole document update is done.
		/// </para><para>
		/// The <see cref="UndoStack"/> listens to the <c>UpdateStarted</c> and <c>UpdateFinished</c> events to group all changes into a single undo step.
		/// </para>
		/// </remarks>
		public event EventHandler<DocumentChangeEventArgs> Changing;
		
		/// <summary>
		/// Is raised after the document has changed.
		/// </summary>
		/// <remarks><inheritdoc cref="Changing"/></remarks>
		public event EventHandler<DocumentChangeEventArgs> Changed;
		
		/// <summary>
		/// Creates a snapshot of the current text.
		/// </summary>
		/// <remarks>
		/// <para>This method returns an immutable snapshot of the document, and may be safely called even when
		/// the document's owner thread is concurrently modifying the document.
		/// </para><para>
		/// This special thread-safety guarantee is valid only for TextDocument.CreateSnapshot(), not necessarily for other
		/// classes implementing ITextSource.CreateSnapshot().
		/// </para><para>
		/// </para>
		/// </remarks>
		public ITextSource CreateSnapshot()
		{
			lock (lockObject) {
				return new RopeTextSource(rope.Clone());
			}
		}
		
		/// <summary>
		/// Creates a snapshot of the current text.
		/// Additionally, creates a checkpoint that allows tracking document changes.
		/// </summary>
		/// <remarks><inheritdoc cref="CreateSnapshot()"/><inheritdoc cref="ChangeTrackingCheckpoint"/></remarks>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1021:AvoidOutParameters", Justification = "Need to return snapshot and checkpoint together to ensure thread-safety")]
		public ITextSource CreateSnapshot(out ChangeTrackingCheckpoint checkpoint)
		{
			lock (lockObject) {
				if (currentCheckpoint == null)
					currentCheckpoint = new ChangeTrackingCheckpoint(lockObject);
				checkpoint = currentCheckpoint;
				return new RopeTextSource(rope.Clone());
			}
		}
		
		internal ChangeTrackingCheckpoint CreateChangeTrackingCheckpoint()
		{
			lock (lockObject) {
				if (currentCheckpoint == null)
					currentCheckpoint = new ChangeTrackingCheckpoint(lockObject);
				return currentCheckpoint;
			}
		}
		
		/// <summary>
		/// Creates a snapshot of a part of the current text.
		/// </summary>
		/// <remarks><inheritdoc cref="CreateSnapshot()"/></remarks>
		public ITextSource CreateSnapshot(int offset, int length)
		{
			lock (lockObject) {
				return new RopeTextSource(rope.GetRange(offset, length));
			}
		}
		
		/// <inheritdoc/>
		public System.IO.TextReader CreateReader()
		{
			lock (lockObject) {
				return new RopeTextReader(rope);
			}
		}
		#endregion
		
		#region BeginUpdate / EndUpdate
		int beginUpdateCount;
		
		/// <summary>
		/// Gets if an update is running.
		/// </summary>
		/// <remarks><inheritdoc cref="BeginUpdate"/></remarks>
		public bool IsInUpdate {
			get {
				VerifyAccess();
				return beginUpdateCount > 0;
			}
		}
		
		/// <summary>
		/// Immediately calls <see cref="BeginUpdate()"/>,
		/// and returns an IDisposable that calls <see cref="EndUpdate()"/>.
		/// </summary>
		/// <remarks><inheritdoc cref="BeginUpdate"/></remarks>
		public IDisposable RunUpdate()
		{
			BeginUpdate();
			return new CallbackOnDispose(EndUpdate);
		}
		
		/// <summary>
		/// <para>Begins a group of document changes.</para>
		/// <para>Some events are suspended until EndUpdate is called, and the <see cref="UndoStack"/> will
		/// group all changes into a single action.</para>
		/// <para>Calling BeginUpdate several times increments a counter, only after the appropriate number
		/// of EndUpdate calls the events resume their work.</para>
		/// </summary>
		/// <remarks><inheritdoc cref="Changing"/></remarks>
		public void BeginUpdate()
		{
			VerifyAccess();
			if (inDocumentChanging)
				throw new InvalidOperationException("Cannot change document within another document change.");
			beginUpdateCount++;
			if (beginUpdateCount == 1) {
				undoStack.StartUndoGroup();
				if (UpdateStarted != null)
					UpdateStarted(this, EventArgs.Empty);
			}
		}
		
		/// <summary>
		/// Ends a group of document changes.
		/// </summary>
		/// <remarks><inheritdoc cref="Changing"/></remarks>
		public void EndUpdate()
		{
			VerifyAccess();
			if (inDocumentChanging)
				throw new InvalidOperationException("Cannot end update within document change.");
			if (beginUpdateCount == 0)
				throw new InvalidOperationException("No update is active.");
			if (beginUpdateCount == 1) {
				// fire change events inside the change group - event handlers might add additional
				// document changes to the change group
				FireChangeEvents();
				undoStack.EndUndoGroup();
				beginUpdateCount = 0;
				if (UpdateFinished != null)
					UpdateFinished(this, EventArgs.Empty);
			} else {
				beginUpdateCount -= 1;
			}
		}
		
		/// <summary>
		/// Occurs when a document change starts.
		/// </summary>
		/// <remarks><inheritdoc cref="Changing"/></remarks>
		public event EventHandler UpdateStarted;
		
		/// <summary>
		/// Occurs when a document change is finished.
		/// </summary>
		/// <remarks><inheritdoc cref="Changing"/></remarks>
		public event EventHandler UpdateFinished;
		#endregion
		
		#region Fire events after update
		int oldTextLength;
		int oldLineCount;
		bool fireTextChanged;
		
		/// <summary>
		/// Fires TextChanged, TextLengthChanged, LineCountChanged if required.
		/// </summary>
		internal void FireChangeEvents()
		{
			// it may be necessary to fire the event multiple times if the document is changed
			// from inside the event handlers
			while (fireTextChanged) {
				fireTextChanged = false;
				if (TextChanged != null)
					TextChanged(this, EventArgs.Empty);
				OnPropertyChanged("Text");
				
				int textLength = rope.Length;
				if (textLength != oldTextLength) {
					oldTextLength = textLength;
					if (TextLengthChanged != null)
						TextLengthChanged(this, EventArgs.Empty);
					OnPropertyChanged("TextLength");
				}
				int lineCount = lineTree.LineCount;
				if (lineCount != oldLineCount) {
					oldLineCount = lineCount;
					if (LineCountChanged != null)
						LineCountChanged(this, EventArgs.Empty);
					OnPropertyChanged("LineCount");
				}
			}
		}
		
		void OnPropertyChanged(string propertyName)
		{
			if (PropertyChanged != null)
				PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
		}
		#endregion
		
		#region Insert / Remove  / Replace
		/// <summary>
		/// Inserts text.
		/// </summary>
		public void Insert(int offset, string text)
		{
			Replace(offset, 0, text);
		}
		
		/// <summary>
		/// Removes text.
		/// </summary>
		public void Remove(ISegment segment)
		{
			Replace(segment, string.Empty);
		}
		
		/// <summary>
		/// Removes text.
		/// </summary>
		public void Remove(int offset, int length)
		{
			Replace(offset, length, string.Empty);
		}
		
		internal bool inDocumentChanging;
		
		/// <summary>
		/// Replaces text.
		/// </summary>
		public void Replace(ISegment segment, string text)
		{
			if (segment == null)
				throw new ArgumentNullException("segment");
			Replace(segment.Offset, segment.Length, text, null);
		}
		
		/// <summary>
		/// Replaces text.
		/// </summary>
		public void Replace(int offset, int length, string text)
		{
			Replace(offset, length, text, null);
		}
		
		/// <summary>
		/// Replaces text.
		/// </summary>
		/// <param name="offset">The starting offset of the text to be replaced.</param>
		/// <param name="length">The length of the text to be replaced.</param>
		/// <param name="text">The new text.</param>
		/// <param name="offsetChangeMappingType">The offsetChangeMappingType determines how offsets inside the old text are mapped to the new text.
		/// This affects how the anchors and segments inside the replaced region behave.</param>
		public void Replace(int offset, int length, string text, OffsetChangeMappingType offsetChangeMappingType)
		{
			if (text == null)
				throw new ArgumentNullException("text");
			// Please see OffsetChangeMappingType XML comments for details on how these modes work.
			switch (offsetChangeMappingType) {
				case OffsetChangeMappingType.Normal:
					Replace(offset, length, text, null);
					break;
				case OffsetChangeMappingType.KeepAnchorBeforeInsertion:
					Replace(offset, length, text, OffsetChangeMap.FromSingleElement(
						new OffsetChangeMapEntry(offset, length, text.Length, false, true)));
					break;
				case OffsetChangeMappingType.RemoveAndInsert:
					if (length == 0 || text.Length == 0) {
						// only insertion or only removal?
						// OffsetChangeMappingType doesn't matter, just use Normal.
						Replace(offset, length, text, null);
					} else {
						OffsetChangeMap map = new OffsetChangeMap(2);
						map.Add(new OffsetChangeMapEntry(offset, length, 0));
						map.Add(new OffsetChangeMapEntry(offset, 0, text.Length));
						map.Freeze();
						Replace(offset, length, text, map);
					}
					break;
				case OffsetChangeMappingType.CharacterReplace:
					if (length == 0 || text.Length == 0) {
						// only insertion or only removal?
						// OffsetChangeMappingType doesn't matter, just use Normal.
						Replace(offset, length, text, null);
					} else if (text.Length > length) {
						// look at OffsetChangeMappingType.CharacterReplace XML comments on why we need to replace
						// the last character
						OffsetChangeMapEntry entry = new OffsetChangeMapEntry(offset + length - 1, 1, 1 + text.Length - length);
						Replace(offset, length, text, OffsetChangeMap.FromSingleElement(entry));
					} else if (text.Length < length) {
						OffsetChangeMapEntry entry = new OffsetChangeMapEntry(offset + text.Length, length - text.Length, 0, true, false);
						Replace(offset, length, text, OffsetChangeMap.FromSingleElement(entry));
					} else {
						Replace(offset, length, text, OffsetChangeMap.Empty);
					}
					break;
				default:
					throw new ArgumentOutOfRangeException("offsetChangeMappingType", offsetChangeMappingType, "Invalid enum value");
			}
		}
		
		/// <summary>
		/// Replaces text.
		/// </summary>
		/// <param name="offset">The starting offset of the text to be replaced.</param>
		/// <param name="length">The length of the text to be replaced.</param>
		/// <param name="text">The new text.</param>
		/// <param name="offsetChangeMap">The offsetChangeMap determines how offsets inside the old text are mapped to the new text.
		/// This affects how the anchors and segments inside the replaced region behave.
		/// If you pass null (the default when using one of the other overloads), the offsets are changed as
		/// in OffsetChangeMappingType.Normal mode.
		/// If you pass OffsetChangeMap.Empty, then everything will stay in its old place (OffsetChangeMappingType.CharacterReplace mode).
		/// The offsetChangeMap must be a valid 'explanation' for the document change. See <see cref="OffsetChangeMap.IsValidForDocumentChange"/>.
		/// Passing an OffsetChangeMap to the Replace method will automatically freeze it to ensure the thread safety of the resulting
		/// DocumentChangeEventArgs instance.
		/// </param>
		public void Replace(int offset, int length, string text, OffsetChangeMap offsetChangeMap)
		{
			if (text == null)
				throw new ArgumentNullException("text");
			
			if (offsetChangeMap != null)
				offsetChangeMap.Freeze();
			
			// Ensure that all changes take place inside an update group.
			// Will also take care of throwing an exception if inDocumentChanging is set.
			BeginUpdate();
			try {
				// protect document change against corruption by other changes inside the event handlers
				inDocumentChanging = true;
				try {
					// The range verification must wait until after the BeginUpdate() call because the document
					// might be modified inside the UpdateStarted event.
					ThrowIfRangeInvalid(offset, length);
					
					DoReplace(offset, length, text, offsetChangeMap);
				} finally {
					inDocumentChanging = false;
				}
			} finally {
				EndUpdate();
			}
		}
		
		void DoReplace(int offset, int length, string newText, OffsetChangeMap offsetChangeMap)
		{
			if (length == 0 && newText.Length == 0)
				return;
			
			// trying to replace a single character in 'Normal' mode?
			// for single characters, 'CharacterReplace' mode is equivalent, but more performant
			// (we don't have to touch the anchorTree at all in 'CharacterReplace' mode)
			if (length == 1 && newText.Length == 1 && offsetChangeMap == null)
				offsetChangeMap = OffsetChangeMap.Empty;
			
			string removedText = rope.ToString(offset, length);
			DocumentChangeEventArgs args = new DocumentChangeEventArgs(offset, removedText, newText, offsetChangeMap);
			
			// fire DocumentChanging event
			if (Changing != null)
				Changing(this, args);
			
			undoStack.Push(this, args);
			
			cachedText = null; // reset cache of complete document text
			fireTextChanged = true;
			DelayedEvents delayedEvents = new DelayedEvents();
			
			lock (lockObject) {
				// create linked list of checkpoints, if required
				if (currentCheckpoint != null) {
					currentCheckpoint = currentCheckpoint.Append(args);
				}
				
				// now update the textBuffer and lineTree
				if (offset == 0 && length == rope.Length) {
					// optimize replacing the whole document
					rope.Clear();
					rope.InsertText(0, newText);
					lineManager.Rebuild();
				} else {
					rope.RemoveRange(offset, length);
					lineManager.Remove(offset, length);
					#if DEBUG
					lineTree.CheckProperties();
					#endif
					rope.InsertText(offset, newText);
					lineManager.Insert(offset, newText);
					#if DEBUG
					lineTree.CheckProperties();
					#endif
				}
			}
			
			// update text anchors
			if (offsetChangeMap == null) {
				anchorTree.HandleTextChange(args.CreateSingleChangeMapEntry(), delayedEvents);
			} else {
				foreach (OffsetChangeMapEntry entry in offsetChangeMap) {
					anchorTree.HandleTextChange(entry, delayedEvents);
				}
			}
			
			// raise delayed events after our data structures are consistent again
			delayedEvents.RaiseEvents();
			
			// fire DocumentChanged event
			if (Changed != null)
				Changed(this, args);
		}
		#endregion
		
		#region GetLineBy...
		/// <summary>
		/// Gets a read-only list of lines.
		/// </summary>
		/// <remarks><inheritdoc cref="DocumentLine"/></remarks>
		public IList<DocumentLine> Lines {
			get { return lineTree; }
		}
		
		/// <summary>
		/// Gets a line by the line number: O(log n)
		/// </summary>
		public DocumentLine GetLineByNumber(int number)
		{
			VerifyAccess();
			if (number < 1 || number > lineTree.LineCount)
				throw new ArgumentOutOfRangeException("number", number, "Value must be between 1 and " + lineTree.LineCount);
			return lineTree.GetByNumber(number);
		}
		
		/// <summary>
		/// Gets a document lines by offset.
		/// Runtime: O(log n)
		/// </summary>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.Int32.ToString")]
		public DocumentLine GetLineByOffset(int offset)
		{
			VerifyAccess();
			if (offset < 0 || offset > rope.Length) {
				throw new ArgumentOutOfRangeException("offset", offset, "0 <= offset <= " + rope.Length.ToString());
			}
			return lineTree.GetByOffset(offset);
		}
		#endregion
		
		/// <summary>
		/// Gets the offset from a text location.
		/// </summary>
		/// <seealso cref="GetLocation"/>
		public int GetOffset(TextLocation location)
		{
			return GetOffset(location.Line, location.Column);
		}
		
		/// <summary>
		/// Gets the offset from a text location.
		/// </summary>
		/// <seealso cref="GetLocation"/>
		public int GetOffset(int line, int column)
		{
			DocumentLine docLine = GetLineByNumber(line);
			if (column <= 0)
				return docLine.Offset;
			if (column > docLine.Length)
				return docLine.EndOffset;
			return docLine.Offset + column - 1;
		}
		
		/// <summary>
		/// Gets the location from an offset.
		/// </summary>
		/// <seealso cref="GetOffset(TextLocation)"/>
		public TextLocation GetLocation(int offset)
		{
			DocumentLine line = GetLineByOffset(offset);
			return new TextLocation(line.LineNumber, offset - line.Offset + 1);
		}
		
		readonly ObservableCollection<ILineTracker> lineTrackers = new ObservableCollection<ILineTracker>();
		
		/// <summary>
		/// Gets the list of <see cref="ILineTracker"/>s attached to this document.
		/// You can add custom line trackers to this list.
		/// </summary>
		public IList<ILineTracker> LineTrackers {
			get {
				VerifyAccess();
				return lineTrackers;
			}
		}
		
		UndoStack undoStack;
		
		/// <summary>
		/// Gets the <see cref="UndoStack"/> of the document.
		/// </summary>
		/// <remarks>This property can also be used to set the undo stack, e.g. for sharing a common undo stack between multiple documents.</remarks>
		public UndoStack UndoStack {
			get { return undoStack; }
			set {
				if (value == null)
					throw new ArgumentNullException();
				if (value != undoStack) {
					undoStack.ClearAll(); // first clear old undo stack, so that it can't be used to perform unexpected changes on this document
					// ClearAll() will also throw an exception when it's not safe to replace the undo stack (e.g. update is currently in progress)
					undoStack = value;
					OnPropertyChanged("UndoStack");
				}
			}
		}
		
		/// <summary>
		/// Creates a new <see cref="TextAnchor"/> at the specified offset.
		/// </summary>
		/// <inheritdoc cref="TextAnchor" select="remarks|example"/>
		public TextAnchor CreateAnchor(int offset)
		{
			VerifyAccess();
			if (offset < 0 || offset > rope.Length) {
				throw new ArgumentOutOfRangeException("offset", offset, "0 <= offset <= " + rope.Length.ToString(CultureInfo.InvariantCulture));
			}
			return anchorTree.CreateAnchor(offset);
		}
		
		#region LineCount
		/// <summary>
		/// Gets the total number of lines in the document.
		/// Runtime: O(1).
		/// </summary>
		public int LineCount {
			get {
				VerifyAccess();
				return lineTree.LineCount;
			}
		}
		
		/// <summary>
		/// Is raised when the LineCount property changes.
		/// </summary>
		[Obsolete("This event will be removed in a future version; use the PropertyChanged event instead")]
		public event EventHandler LineCountChanged;
		#endregion
		
		#region Debugging
		[Conditional("DEBUG")]
		internal void DebugVerifyAccess()
		{
			VerifyAccess();
		}
		
		/// <summary>
		/// Gets the document lines tree in string form.
		/// </summary>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate")]
		internal string GetLineTreeAsString()
		{
			#if DEBUG
			return lineTree.GetTreeAsString();
			#else
			return "Not available in release build.";
			#endif
		}
		
		/// <summary>
		/// Gets the text anchor tree in string form.
		/// </summary>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate")]
		internal string GetTextAnchorTreeAsString()
		{
			#if DEBUG
			return anchorTree.GetTreeAsString();
			#else
			return "Not available in release build.";
			#endif
		}
		#endregion
	}
}
