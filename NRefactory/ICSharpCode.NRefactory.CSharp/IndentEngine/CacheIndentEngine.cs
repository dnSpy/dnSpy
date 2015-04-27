//
// CacheIndentEngine.cs
//
// Author:
//       Matej Miklečić <matej.miklecic@gmail.com>
//
// Copyright (c) 2013 Matej Miklečić (matej.miklecic@gmail.com)
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using ICSharpCode.NRefactory.Editor;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ICSharpCode.NRefactory.CSharp
{
	/// <summary>
	///     Represents a decorator of an IStateMachineIndentEngine instance that provides
	///     logic for reseting and updating the engine on text changed events.
	/// </summary>
	/// <remarks>
	///     The decorator is based on periodical caching of the engine's state and
	///     delegating all logic behind indentation to the currently active engine.
	/// </remarks>
	public class CacheIndentEngine : IStateMachineIndentEngine
	{

		#region Properties

		IStateMachineIndentEngine currentEngine;
		Stack<IStateMachineIndentEngine> cachedEngines = new Stack<IStateMachineIndentEngine>();

		#endregion

		#region Constructors

		/// <summary>
		///     Creates a new CacheIndentEngine instance.
		/// </summary>
		/// <param name="decoratedEngine">
		///     An instance of <see cref="IStateMachineIndentEngine"/> to which the
		///     logic for indentation will be delegated.
		/// </param>
		/// <param name="cacheRate">
		///     The number of chars between caching.
		/// </param>
		public CacheIndentEngine(IStateMachineIndentEngine decoratedEngine, int cacheRate = 2000)
		{
			this.currentEngine = decoratedEngine;
		}

		/// <summary>
		///     Creates a new CacheIndentEngine instance from the given prototype.
		/// </summary>
		/// <param name="prototype">
		///     A CacheIndentEngine instance.
		/// </param>
		public CacheIndentEngine(CacheIndentEngine prototype)
		{
			this.currentEngine = prototype.currentEngine.Clone();
		}

		#endregion

		#region IDocumentIndentEngine

		/// <inheritdoc />
		public IDocument Document {
			get { return currentEngine.Document; }
		}

		/// <inheritdoc />
		public string ThisLineIndent {
			get { return currentEngine.ThisLineIndent; }
		}

		/// <inheritdoc />
		public string NextLineIndent {
			get { return currentEngine.NextLineIndent; }
		}

		/// <inheritdoc />
		public string CurrentIndent {
			get { return currentEngine.CurrentIndent; }
		}

		/// <inheritdoc />
		public bool NeedsReindent {
			get { return currentEngine.NeedsReindent; }
		}

		/// <inheritdoc />
		public int Offset {
			get { return currentEngine.Offset; }
		}

		/// <inheritdoc />
		public TextLocation Location {
			get { return currentEngine.Location; }
		}

		/// <inheritdoc />
		public bool EnableCustomIndentLevels
		{
			get { return currentEngine.EnableCustomIndentLevels; }
			set { currentEngine.EnableCustomIndentLevels = value; }
		}

		/// <inheritdoc />
		public void Push(char ch)
		{
			currentEngine.Push(ch);
		}

		/// <inheritdoc />
		public void Reset()
		{
			currentEngine.Reset();
			cachedEngines.Clear();
		}

		/// <summary>
		/// Resets the engine to offset. Clears all cached engines after the given offset.
		/// </summary>
		public void ResetEngineToPosition(int offset)
		{
			// We are already there
			if (currentEngine.Offset <= offset)
				return;
			
			bool gotCachedEngine = false;
			while (cachedEngines.Count > 0) {
				var topEngine = cachedEngines.Peek();
				if (topEngine.Offset <= offset) {
					currentEngine = topEngine.Clone();
					gotCachedEngine = true;
					break;
				} else {
					cachedEngines.Pop();
				}
			}
			if (!gotCachedEngine)
				currentEngine.Reset();
		}

		/// <inheritdoc />
		/// <remarks>
		///     If the <paramref name="position"/> is negative, the engine will
		///     update to: document.TextLength + (offset % document.TextLength+1)
		///     Otherwise it will update to: offset % document.TextLength+1
		/// </remarks>
		public void Update(int position)
		{
			const int BUFFER_SIZE = 2000;
			
			if (currentEngine.Offset == position) {
				//positions match, nothing to be done
				return;
			} else if (currentEngine.Offset > position) {
				//moving backwards, so reset from previous saved location
				ResetEngineToPosition(position);
			}

			// get the engine caught up
			int nextSave = (cachedEngines.Count == 0) ? BUFFER_SIZE : cachedEngines.Peek().Offset + BUFFER_SIZE;
			if (currentEngine.Offset + 1 == position) {
				char ch = currentEngine.Document.GetCharAt(currentEngine.Offset);
				currentEngine.Push(ch);
				if (currentEngine.Offset == nextSave)
					cachedEngines.Push(currentEngine.Clone());
			} else {
				//bulk copy characters in case buffer is unmanaged 
				//(faster if we reduce managed/unmanaged transitions)
				while (currentEngine.Offset < position) {
					int endCut = currentEngine.Offset + BUFFER_SIZE;
					if (endCut > position)
						endCut = position;
					string buffer = currentEngine.Document.GetText(currentEngine.Offset, endCut - currentEngine.Offset);
					foreach (char ch in buffer) {
						currentEngine.Push(ch);
						//ConsoleWrite ("pushing character '{0}'", ch);
						if (currentEngine.Offset == nextSave) {
							cachedEngines.Push(currentEngine.Clone());
							nextSave += BUFFER_SIZE;
						}
					}
				}
			}
		}

		public IStateMachineIndentEngine GetEngine(int offset)
		{
			ResetEngineToPosition(offset);
			return currentEngine;
		}

		#endregion

		#region IClonable

		/// <inheritdoc />
		public IStateMachineIndentEngine Clone()
		{
			return new CacheIndentEngine(this);
		}

		/// <inheritdoc />
		IDocumentIndentEngine IDocumentIndentEngine.Clone()
		{
			return Clone();
		}

		object ICloneable.Clone()
		{
			return Clone();
		}

		#endregion

		#region IStateMachineIndentEngine

		public bool IsInsidePreprocessorDirective {
			get { return currentEngine.IsInsidePreprocessorDirective; }
		}

		public bool IsInsidePreprocessorComment {
			get { return currentEngine.IsInsidePreprocessorComment; }
		}

		public bool IsInsideStringLiteral {
			get { return currentEngine.IsInsideStringLiteral; }
		}

		public bool IsInsideVerbatimString {
			get { return currentEngine.IsInsideVerbatimString; }
		}

		public bool IsInsideCharacter {
			get { return currentEngine.IsInsideCharacter; }
		}

		public bool IsInsideString {
			get { return currentEngine.IsInsideString; }
		}

		public bool IsInsideLineComment {
			get { return currentEngine.IsInsideLineComment; }
		}

		public bool IsInsideMultiLineComment {
			get { return currentEngine.IsInsideMultiLineComment; }
		}

		public bool IsInsideDocLineComment {
			get { return currentEngine.IsInsideDocLineComment; }
		}

		public bool IsInsideComment {
			get { return currentEngine.IsInsideComment; }
		}

		public bool IsInsideOrdinaryComment {
			get { return currentEngine.IsInsideOrdinaryComment; }
		}

		public bool IsInsideOrdinaryCommentOrString {
			get { return currentEngine.IsInsideOrdinaryCommentOrString; }
		}

		public bool LineBeganInsideVerbatimString {
			get { return currentEngine.LineBeganInsideVerbatimString; }
		}

		public bool LineBeganInsideMultiLineComment {
			get { return currentEngine.LineBeganInsideMultiLineComment; }
		}

		#endregion

	}
	/*
/		// <summary>
	///     Represents a decorator of an IStateMachineIndentEngine instance that provides
	///     logic for reseting and updating the engine on text changed events.
	/// </summary>
	/// <remarks>
	///     The decorator is based on periodical caching of the engine's state and
	///     delegating all logic behind indentation to the currently active engine.
	/// </remarks>
	public class CacheIndentEngine : IStateMachineIndentEngine
	{
		#region Properties
		
		/// <summary>
		///     Represents the cache interval in number of chars pushed to the engine.
		/// </summary>
		/// <remarks>
		///     When this many new chars are pushed to the engine, the currently active
		///     engine gets cloned and added to the end of <see cref="cachedEngines"/>.
		/// </remarks>
		readonly int cacheRate;
		
		/// <summary>
		///     Determines how much memory to reserve on initialization for the
		///     cached engines.
		/// </summary>
		const int cacheCapacity = 25;
		
		/// <summary>
		///     Currently active engine.
		/// </summary>
		/// <remarks>
		///     Should be equal to the last engine in <see cref="cachedEngines"/>.
		/// </remarks>
		IStateMachineIndentEngine currentEngine;
		
		/// <summary>
		///     List of cached engines sorted ascending by 
		///     <see cref="IDocumentIndentEngine.Offset"/>.
		/// </summary>
		IStateMachineIndentEngine[] cachedEngines;
		
		/// <summary>
		///     The index of the last cached engine in cachedEngines.
		/// </summary>
		/// <remarks>
		///     Should be equal to: currentEngine.Offset / CacheRate
		/// </remarks>
		int lastCachedEngine;
		
		#endregion
		
		#region Constructors
		
		/// <summary>
		///     Creates a new CacheIndentEngine instance.
		/// </summary>
		/// <param name="decoratedEngine">
		///     An instance of <see cref="IStateMachineIndentEngine"/> to which the
		///     logic for indentation will be delegated.
		/// </param>
		/// <param name="cacheRate">
		///     The number of chars between caching.
		/// </param>
		public CacheIndentEngine(IStateMachineIndentEngine decoratedEngine, int cacheRate = 2000)
		{
			this.cachedEngines = new IStateMachineIndentEngine[cacheCapacity];
			
			this.cachedEngines[0] = decoratedEngine.Clone();
			this.currentEngine = this.cachedEngines[0].Clone();
			this.cacheRate = cacheRate;
		}
		
		/// <summary>
		///     Creates a new CacheIndentEngine instance from the given prototype.
		/// </summary>
		/// <param name="prototype">
		///     A CacheIndentEngine instance.
		/// </param>
		public CacheIndentEngine(CacheIndentEngine prototype)
		{
			this.cachedEngines = new IStateMachineIndentEngine[prototype.cachedEngines.Length];
			Array.Copy(prototype.cachedEngines, this.cachedEngines, prototype.cachedEngines.Length);
			
			this.lastCachedEngine = prototype.lastCachedEngine;
			this.currentEngine = prototype.currentEngine.Clone();
			this.cacheRate = prototype.cacheRate;
		}
		
		#endregion
		
		#region Methods
		
		/// <summary>
		///     Performs caching of the <see cref="CacheIndentEngine.currentEngine"/>.
		/// </summary>
		void cache()
		{
			if (currentEngine.Offset % cacheRate != 0)
			{
				throw new Exception("The current engine's offset is not divisable with the cacheRate.");
			}
			
			// determine the new current engine from cachedEngines
			lastCachedEngine = currentEngine.Offset / cacheRate;
			
			if (cachedEngines.Length < lastCachedEngine + 1)
			{
				Array.Resize(ref cachedEngines, lastCachedEngine * 2);
			}
			
			cachedEngines[lastCachedEngine] = currentEngine.Clone();
		}
		
		#endregion
		
		#region IDocumentIndentEngine
		
		/// <inheritdoc />
		public IDocument Document
		{
			get { return currentEngine.Document; }
		}
		
		/// <inheritdoc />
		public string ThisLineIndent
		{
			get { return currentEngine.ThisLineIndent; }
		}
		
		/// <inheritdoc />
		public string NextLineIndent
		{
			get { return currentEngine.NextLineIndent; }
		}
		
		/// <inheritdoc />
		public string CurrentIndent
		{
			get { return currentEngine.CurrentIndent; }
		}
		
		/// <inheritdoc />
		public bool NeedsReindent
		{
			get { return currentEngine.NeedsReindent; }
		}
		
		/// <inheritdoc />
		public int Offset
		{
			get { return currentEngine.Offset; }
		}
		
		/// <inheritdoc />
		public TextLocation Location
		{
			get { return currentEngine.Location; }
		}
		
		/// <inheritdoc />
		public void Push(char ch)
		{
			currentEngine.Push(ch);
			
			if (currentEngine.Offset % cacheRate == 0)
			{
				cache();
			}
		}
		
		/// <inheritdoc />
		public void Reset()
		{
			currentEngine = cachedEngines[lastCachedEngine = 0];
		}
		
		/// <inheritdoc />
		/// <remarks>
		///     If the <paramref name="offset"/> is negative, the engine will
		///     update to: document.TextLength + (offset % document.TextLength+1)
		///     Otherwise it will update to: offset % document.TextLength+1
		/// </remarks>
		public void Update(int offset)
		{
			// map the given offset to the [0, document.TextLength] interval
			// using modulo arithmetics
			offset %= Document.TextLength + 1;
			if (offset < 0)
			{
				offset += Document.TextLength + 1;
			}
			
			// check if the engine has to be updated to some previous offset
			if (currentEngine.Offset > offset)
			{
				// replace the currentEngine with the first one whose offset
				// is less then the given <paramref name="offset"/>
				lastCachedEngine =  offset / cacheRate;
				currentEngine = cachedEngines[lastCachedEngine].Clone();
			}
			
			// update the engine to the given offset
			while (Offset < offset)
			{
				Push(Document.GetCharAt(Offset));
			}
		}
		
		public IStateMachineIndentEngine GetEngine(int offset)
		{
			// map the given offset to the [0, document.TextLength] interval
			// using modulo arithmetics
			offset %= Document.TextLength + 1;
			if (offset < 0)
			{
				offset += Document.TextLength + 1;
			}
			
			// check if the engine has to be updated to some previous offset
			if (currentEngine.Offset > offset)
			{
				// replace the currentEngine with the first one whose offset
				// is less then the given <paramref name="offset"/>
				lastCachedEngine =  offset / cacheRate;
				return cachedEngines[lastCachedEngine].Clone();
			}
			
			return currentEngine;
		}
		
		#endregion
		
		#region IClonable
		
		/// <inheritdoc />
		public IStateMachineIndentEngine Clone()
		{
			return new CacheIndentEngine(this);
		}
		
		/// <inheritdoc />
		IDocumentIndentEngine IDocumentIndentEngine.Clone()
		{
			return Clone();
		}
		
		object ICloneable.Clone()
		{
			return Clone();
		}
		
		#endregion
		
		#region IStateMachineIndentEngine
		
		public bool IsInsidePreprocessorDirective
		{
			get { return currentEngine.IsInsidePreprocessorDirective; }
		}
		
		public bool IsInsidePreprocessorComment
		{
			get { return currentEngine.IsInsidePreprocessorComment; }
		}
		
		public bool IsInsideStringLiteral
		{
			get { return currentEngine.IsInsideStringLiteral; }
		}
		
		public bool IsInsideVerbatimString
		{
			get { return currentEngine.IsInsideVerbatimString; }
		}
		
		public bool IsInsideCharacter
		{
			get { return currentEngine.IsInsideCharacter; }
		}
		
		public bool IsInsideString
		{
			get { return currentEngine.IsInsideString; }
		}
		
		public bool IsInsideLineComment
		{
			get { return currentEngine.IsInsideLineComment; }
		}
		
		public bool IsInsideMultiLineComment
		{
			get { return currentEngine.IsInsideMultiLineComment; }
		}
		
		public bool IsInsideDocLineComment
		{
			get { return currentEngine.IsInsideDocLineComment; }
		}
		
		public bool IsInsideComment
		{
			get { return currentEngine.IsInsideComment; }
		}
		
		public bool IsInsideOrdinaryComment
		{
			get { return currentEngine.IsInsideOrdinaryComment; }
		}
		
		public bool IsInsideOrdinaryCommentOrString
		{
			get { return currentEngine.IsInsideOrdinaryCommentOrString; }
		}
		
		public bool LineBeganInsideVerbatimString
		{
			get { return currentEngine.LineBeganInsideVerbatimString; }
		}
		
		public bool LineBeganInsideMultiLineComment
		{
			get { return currentEngine.LineBeganInsideMultiLineComment; }
		}
		
		#endregion
	}
	
	*/
}