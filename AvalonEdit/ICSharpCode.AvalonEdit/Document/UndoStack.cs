// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using ICSharpCode.AvalonEdit.Utils;

namespace ICSharpCode.AvalonEdit.Document
{
	/// <summary>
	/// Undo stack implementation.
	/// </summary>
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1711:IdentifiersShouldNotHaveIncorrectSuffix")]
	public sealed class UndoStack : INotifyPropertyChanged
	{
		/// undo stack is listening for changes
		internal const int StateListen = 0;
		/// undo stack is reverting/repeating a set of changes
		internal const int StatePlayback = 1;
		// undo stack is reverting/repeating a set of changes and modifies the document to do this
		internal const int StatePlaybackModifyDocument = 2;
		/// state is used for checking that noone but the UndoStack performs changes
		/// during Undo events
		internal int state = StateListen;
		
		Deque<IUndoableOperation> undostack = new Deque<IUndoableOperation>();
		Deque<IUndoableOperation> redostack = new Deque<IUndoableOperation>();
		int sizeLimit = int.MaxValue;
		
		int undoGroupDepth;
		int actionCountInUndoGroup;
		int optionalActionCount;
		object lastGroupDescriptor;
		bool allowContinue;
		
		#region IsOriginalFile implementation
		// implements feature request SD2-784 - File still considered dirty after undoing all changes
		
		/// <summary>
		/// Number of times undo must be executed until the original state is reached.
		/// Negative: number of times redo must be executed until the original state is reached.
		/// Special case: int.MinValue == original state is unreachable
		/// </summary>
		int elementsOnUndoUntilOriginalFile;
		
		bool isOriginalFile = true;
		
		/// <summary>
		/// Gets whether the document is currently in its original state (no modifications).
		/// </summary>
		public bool IsOriginalFile {
			get { return isOriginalFile; }
		}
		
		void RecalcIsOriginalFile()
		{
			bool newIsOriginalFile = (elementsOnUndoUntilOriginalFile == 0);
			if (newIsOriginalFile != isOriginalFile) {
				isOriginalFile = newIsOriginalFile;
				NotifyPropertyChanged("IsOriginalFile");
			}
		}
		
		/// <summary>
		/// Marks the current state as original. Discards any previous "original" markers.
		/// </summary>
		public void MarkAsOriginalFile()
		{
			elementsOnUndoUntilOriginalFile = 0;
			RecalcIsOriginalFile();
		}
		
		/// <summary>
		/// Discards the current "original" marker.
		/// </summary>
		public void DiscardOriginalFileMarker()
		{
			elementsOnUndoUntilOriginalFile = int.MinValue;
			RecalcIsOriginalFile();
		}
		
		void FileModified(int newElementsOnUndoStack)
		{
			if (elementsOnUndoUntilOriginalFile == int.MinValue)
				return;
			
			elementsOnUndoUntilOriginalFile += newElementsOnUndoStack;
			if (elementsOnUndoUntilOriginalFile > undostack.Count)
				elementsOnUndoUntilOriginalFile = int.MinValue;
			
			// don't call RecalcIsOriginalFile(): wait until end of undo group
		}
		#endregion
		
		/// <summary>
		/// Gets if the undo stack currently accepts changes.
		/// Is false while an undo action is running.
		/// </summary>
		public bool AcceptChanges {
			get { return state == StateListen; }
		}
		
		/// <summary>
		/// Gets if there are actions on the undo stack.
		/// Use the PropertyChanged event to listen to changes of this property.
		/// </summary>
		public bool CanUndo {
			get { return undostack.Count > 0; }
		}
		
		/// <summary>
		/// Gets if there are actions on the redo stack.
		/// Use the PropertyChanged event to listen to changes of this property.
		/// </summary>
		public bool CanRedo {
			get { return redostack.Count > 0; }
		}
		
		/// <summary>
		/// Gets/Sets the limit on the number of items on the undo stack.
		/// </summary>
		/// <remarks>The size limit is enforced only on the number of stored top-level undo groups.
		/// Elements within undo groups do not count towards the size limit.</remarks>
		public int SizeLimit {
			get { return sizeLimit; }
			set {
				if (value < 0)
					ThrowUtil.CheckNotNegative(value, "value");
				if (sizeLimit != value) {
					sizeLimit = value;
					NotifyPropertyChanged("SizeLimit");
					if (undoGroupDepth == 0)
						EnforceSizeLimit();
				}
			}
		}
		
		void EnforceSizeLimit()
		{
			Debug.Assert(undoGroupDepth == 0);
			while (undostack.Count > sizeLimit)
				undostack.PopFront();
			while (redostack.Count > sizeLimit)
				redostack.PopFront();
		}
		
		/// <summary>
		/// If an undo group is open, gets the group descriptor of the current top-level
		/// undo group.
		/// If no undo group is open, gets the group descriptor from the previous undo group.
		/// </summary>
		/// <remarks>The group descriptor can be used to join adjacent undo groups:
		/// use a group descriptor to mark your changes, and on the second action,
		/// compare LastGroupDescriptor and use <see cref="StartContinuedUndoGroup"/> if you
		/// want to join the undo groups.</remarks>
		public object LastGroupDescriptor {
			get { return lastGroupDescriptor; }
		}
		
		/// <summary>
		/// Starts grouping changes.
		/// Maintains a counter so that nested calls are possible.
		/// </summary>
		public void StartUndoGroup()
		{
			StartUndoGroup(null);
		}
		
		/// <summary>
		/// Starts grouping changes.
		/// Maintains a counter so that nested calls are possible.
		/// </summary>
		/// <param name="groupDescriptor">An object that is stored with the undo group.
		/// If this is not a top-level undo group, the parameter is ignored.</param>
		public void StartUndoGroup(object groupDescriptor)
		{
			if (undoGroupDepth == 0) {
				actionCountInUndoGroup = 0;
				optionalActionCount = 0;
				lastGroupDescriptor = groupDescriptor;
			}
			undoGroupDepth++;
			//Util.LoggingService.Debug("Open undo group (new depth=" + undoGroupDepth + ")");
		}
		
		/// <summary>
		/// Starts grouping changes, continuing with the previously closed undo group if possible.
		/// Maintains a counter so that nested calls are possible.
		/// If the call to StartContinuedUndoGroup is a nested call, it behaves exactly
		/// as <see cref="StartUndoGroup()"/>, only top-level calls can continue existing undo groups.
		/// </summary>
		/// <param name="groupDescriptor">An object that is stored with the undo group.
		/// If this is not a top-level undo group, the parameter is ignored.</param>
		public void StartContinuedUndoGroup(object groupDescriptor)
		{
			if (undoGroupDepth == 0) {
				actionCountInUndoGroup = (allowContinue && undostack.Count > 0) ? 1 : 0;
				optionalActionCount = 0;
				lastGroupDescriptor = groupDescriptor;
			}
			undoGroupDepth++;
			//Util.LoggingService.Debug("Continue undo group (new depth=" + undoGroupDepth + ")");
		}
		
		/// <summary>
		/// Stops grouping changes.
		/// </summary>
		public void EndUndoGroup()
		{
			if (undoGroupDepth == 0) throw new InvalidOperationException("There are no open undo groups");
			undoGroupDepth--;
			//Util.LoggingService.Debug("Close undo group (new depth=" + undoGroupDepth + ")");
			if (undoGroupDepth == 0) {
				Debug.Assert(state == StateListen || actionCountInUndoGroup == 0);
				if (actionCountInUndoGroup == optionalActionCount) {
					// only optional actions: don't store them
					for (int i = 0; i < optionalActionCount; i++) {
						undostack.PopBack();
					}
				} else if (actionCountInUndoGroup > 1) {
					// combine all actions within the group into a single grouped action
					undostack.PushBack(new UndoOperationGroup(undostack, actionCountInUndoGroup));
					FileModified(-actionCountInUndoGroup + 1 + optionalActionCount);
				}
				//if (state == StateListen) {
				EnforceSizeLimit();
				allowContinue = true;
				RecalcIsOriginalFile(); // can raise event
				//}
			}
		}
		
		/// <summary>
		/// Throws an InvalidOperationException if an undo group is current open.
		/// </summary>
		void ThrowIfUndoGroupOpen()
		{
			if (undoGroupDepth != 0) {
				undoGroupDepth = 0;
				throw new InvalidOperationException("No undo group should be open at this point");
			}
			if (state != StateListen) {
				throw new InvalidOperationException("This method cannot be called while an undo operation is being performed");
			}
		}
		
		List<TextDocument> affectedDocuments;
		
		internal void RegisterAffectedDocument(TextDocument document)
		{
			if (affectedDocuments == null)
				affectedDocuments = new List<TextDocument>();
			if (!affectedDocuments.Contains(document)) {
				affectedDocuments.Add(document);
				document.BeginUpdate();
			}
		}
		
		void CallEndUpdateOnAffectedDocuments()
		{
			if (affectedDocuments != null) {
				foreach (TextDocument doc in affectedDocuments) {
					doc.EndUpdate();
				}
				affectedDocuments = null;
			}
		}
		
		/// <summary>
		/// Call this method to undo the last operation on the stack
		/// </summary>
		public void Undo()
		{
			ThrowIfUndoGroupOpen();
			if (undostack.Count > 0) {
				// disallow continuing undo groups after undo operation
				lastGroupDescriptor = null; allowContinue = false;
				// fetch operation to undo and move it to redo stack
				IUndoableOperation uedit = undostack.PopBack();
				redostack.PushBack(uedit);
				state = StatePlayback;
				try {
					RunUndo(uedit);
				} finally {
					state = StateListen;
					FileModified(-1);
					CallEndUpdateOnAffectedDocuments();
				}
				RecalcIsOriginalFile();
				if (undostack.Count == 0)
					NotifyPropertyChanged("CanUndo");
				if (redostack.Count == 1)
					NotifyPropertyChanged("CanRedo");
			}
		}
		
		internal void RunUndo(IUndoableOperation op)
		{
			IUndoableOperationWithContext opWithCtx = op as IUndoableOperationWithContext;
			if (opWithCtx != null)
				opWithCtx.Undo(this);
			else
				op.Undo();
		}
		
		/// <summary>
		/// Call this method to redo the last undone operation
		/// </summary>
		public void Redo()
		{
			ThrowIfUndoGroupOpen();
			if (redostack.Count > 0) {
				lastGroupDescriptor = null; allowContinue = false;
				IUndoableOperation uedit = redostack.PopBack();
				undostack.PushBack(uedit);
				state = StatePlayback;
				try {
					RunRedo(uedit);
				} finally {
					state = StateListen;
					FileModified(1);
					CallEndUpdateOnAffectedDocuments();
				}
				RecalcIsOriginalFile();
				if (redostack.Count == 0)
					NotifyPropertyChanged("CanRedo");
				if (undostack.Count == 1)
					NotifyPropertyChanged("CanUndo");
			}
		}
		
		internal void RunRedo(IUndoableOperation op)
		{
			IUndoableOperationWithContext opWithCtx = op as IUndoableOperationWithContext;
			if (opWithCtx != null)
				opWithCtx.Redo(this);
			else
				op.Redo();
		}
		
		/// <summary>
		/// Call this method to push an UndoableOperation on the undostack.
		/// The redostack will be cleared if you use this method.
		/// </summary>
		public void Push(IUndoableOperation operation)
		{
			Push(operation, false);
		}
		
		/// <summary>
		/// Call this method to push an UndoableOperation on the undostack.
		/// However, the operation will be only stored if the undo group contains a
		/// non-optional operation.
		/// Use this method to store the caret position/selection on the undo stack to
		/// prevent having only actions that affect only the caret and not the document.
		/// </summary>
		public void PushOptional(IUndoableOperation operation)
		{
			if (undoGroupDepth == 0)
				throw new InvalidOperationException("Cannot use PushOptional outside of undo group");
			Push(operation, true);
		}
		
		void Push(IUndoableOperation operation, bool isOptional)
		{
			if (operation == null) {
				throw new ArgumentNullException("operation");
			}
			
			if (state == StateListen && sizeLimit > 0) {
				bool wasEmpty = undostack.Count == 0;
				
				bool needsUndoGroup = undoGroupDepth == 0;
				if (needsUndoGroup) StartUndoGroup();
				undostack.PushBack(operation);
				actionCountInUndoGroup++;
				if (isOptional)
					optionalActionCount++;
				else
					FileModified(1);
				if (needsUndoGroup) EndUndoGroup();
				if (wasEmpty)
					NotifyPropertyChanged("CanUndo");
				ClearRedoStack();
			}
		}
		
		/// <summary>
		/// Call this method, if you want to clear the redo stack
		/// </summary>
		public void ClearRedoStack()
		{
			if (redostack.Count != 0) {
				redostack.Clear();
				NotifyPropertyChanged("CanRedo");
				// if the "original file" marker is on the redo stack: remove it
				if (elementsOnUndoUntilOriginalFile < 0)
					elementsOnUndoUntilOriginalFile = int.MinValue;
			}
		}
		
		/// <summary>
		/// Clears both the undo and redo stack.
		/// </summary>
		public void ClearAll()
		{
			ThrowIfUndoGroupOpen();
			actionCountInUndoGroup = 0;
			optionalActionCount = 0;
			if (undostack.Count != 0) {
				lastGroupDescriptor = null;
				allowContinue = false;
				undostack.Clear();
				NotifyPropertyChanged("CanUndo");
			}
			ClearRedoStack();
		}
		
		internal void Push(TextDocument document, DocumentChangeEventArgs e)
		{
			if (state == StatePlayback)
				throw new InvalidOperationException("Document changes during undo/redo operations are not allowed.");
			if (state == StatePlaybackModifyDocument)
				state = StatePlayback; // allow only 1 change per expected modification
			else
				Push(new DocumentChangeOperation(document, e));
		}
		
		/// <summary>
		/// Is raised when a property (CanUndo, CanRedo) changed.
		/// </summary>
		public event PropertyChangedEventHandler PropertyChanged;
		
		void NotifyPropertyChanged(string propertyName)
		{
			if (PropertyChanged != null)
				PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
		}
	}
}
