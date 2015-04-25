// Copyright (c) 2010-2013 AlphaSierraPapa for the SharpDevelop Team
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
using System.Linq;
using System.Threading;

namespace ICSharpCode.NRefactory
{
	/// <summary>
	/// Provides an interface to handle annotations in an object.
	/// </summary>
	public interface IAnnotatable
	{
		/// <summary>
		/// Gets all annotations stored on this IAnnotatable.
		/// </summary>
		IEnumerable<object> Annotations {
			get;
		}
		
		/// <summary>
		/// Gets the first annotation of the specified type.
		/// Returns null if no matching annotation exists.
		/// </summary>
		/// <typeparam name='T'>
		/// The type of the annotation.
		/// </typeparam>
		T Annotation<T> () where T: class;
		
		/// <summary>
		/// Gets the first annotation of the specified type.
		/// Returns null if no matching annotation exists.
		/// </summary>
		/// <param name='type'>
		/// The type of the annotation.
		/// </param>
		object Annotation (Type type);
		
		/// <summary>
		/// Adds an annotation to this instance.
		/// </summary>
		/// <param name='annotation'>
		/// The annotation to add.
		/// </param>
		void AddAnnotation (object annotation);
		
		/// <summary>
		/// Removes all annotations of the specified type.
		/// </summary>
		/// <typeparam name='T'>
		/// The type of the annotations to remove.
		/// </typeparam>
		void RemoveAnnotations<T> () where T : class;
		
		/// <summary>
		/// Removes all annotations of the specified type.
		/// </summary>
		/// <param name='type'>
		/// The type of the annotations to remove.
		/// </param>
		void RemoveAnnotations(Type type);
	}
	
	/// <summary>
	/// Base class used to implement the IAnnotatable interface.
	/// This implementation is thread-safe.
	/// </summary>
	[Serializable]
	public abstract class AbstractAnnotatable : IAnnotatable
	{
		// Annotations: points either null (no annotations), to the single annotation,
		// or to an AnnotationList.
		// Once it is pointed at an AnnotationList, it will never change (this allows thread-safety support by locking the list)
		
		object annotations;
		
		/// <summary>
		/// Clones all annotations.
		/// This method is intended to be called by Clone() implementations in derived classes.
		/// <code>
		/// AstNode copy = (AstNode)MemberwiseClone();
		/// copy.CloneAnnotations();
		/// </code>
		/// </summary>
		protected void CloneAnnotations()
		{
			ICloneable cloneable = annotations as ICloneable;
			if (cloneable != null)
				annotations = cloneable.Clone();
		}

		sealed class AnnotationList : List<object>, ICloneable
		{
			// There are two uses for this custom list type:
			// 1) it's private, and thus (unlike List<object>) cannot be confused with real annotations
			// 2) It allows us to simplify the cloning logic by making the list behave the same as a clonable annotation.
			public AnnotationList (int initialCapacity) : base(initialCapacity)
			{
			}
			
			public object Clone ()
			{
				lock (this) {
					AnnotationList copy = new AnnotationList (this.Count);
					for (int i = 0; i < this.Count; i++) {
						object obj = this [i];
						ICloneable c = obj as ICloneable;
						copy.Add (c != null ? c.Clone () : obj);
					}
					return copy;
				}
			}
		}
		
		public virtual void AddAnnotation (object annotation)
		{
			if (annotation == null)
				throw new ArgumentNullException ("annotation");
		retry: // Retry until successful
			object oldAnnotation = Interlocked.CompareExchange (ref this.annotations, annotation, null);
			if (oldAnnotation == null) {
				return; // we successfully added a single annotation
			}
			AnnotationList list = oldAnnotation as AnnotationList;
			if (list == null) {
				// we need to transform the old annotation into a list
				list = new AnnotationList (4);
				list.Add (oldAnnotation);
				list.Add (annotation);
				if (Interlocked.CompareExchange (ref this.annotations, list, oldAnnotation) != oldAnnotation) {
					// the transformation failed (some other thread wrote to this.annotations first)
					goto retry;
				}
			} else {
				// once there's a list, use simple locking
				lock (list) {
					list.Add (annotation);
				}
			}
		}
		
		public virtual void RemoveAnnotations<T> () where T : class
		{
		retry: // Retry until successful
			object oldAnnotations = this.annotations;
			AnnotationList list = oldAnnotations as AnnotationList;
			if (list != null) {
				lock (list)
					list.RemoveAll (obj => obj is T);
			} else if (oldAnnotations is T) {
				if (Interlocked.CompareExchange (ref this.annotations, null, oldAnnotations) != oldAnnotations) {
					// Operation failed (some other thread wrote to this.annotations first)
					goto retry;
				}
			}
		}
		
		public virtual void RemoveAnnotations (Type type)
		{
			if (type == null)
				throw new ArgumentNullException ("type");
		retry: // Retry until successful
			object oldAnnotations = this.annotations;
			AnnotationList list = oldAnnotations as AnnotationList;
			if (list != null) {
				lock (list)
					list.RemoveAll(type.IsInstanceOfType);
			} else if (type.IsInstanceOfType (oldAnnotations)) {
				if (Interlocked.CompareExchange (ref this.annotations, null, oldAnnotations) != oldAnnotations) {
					// Operation failed (some other thread wrote to this.annotations first)
					goto retry;
				}
			}
		}
		
		public T Annotation<T> () where T: class
		{
			object annotations = this.annotations;
			AnnotationList list = annotations as AnnotationList;
			if (list != null) {
				lock (list) {
					foreach (object obj in list) {
						T t = obj as T;
						if (t != null)
							return t;
					}
					return null;
				}
			} else {
				return annotations as T;
			}
		}
		
		public object Annotation (Type type)
		{
			if (type == null)
				throw new ArgumentNullException ("type");
			object annotations = this.annotations;
			AnnotationList list = annotations as AnnotationList;
			if (list != null) {
				lock (list) {
					foreach (object obj in list) {
						if (type.IsInstanceOfType (obj))
							return obj;
					}
				}
			} else {
				if (type.IsInstanceOfType (annotations))
					return annotations;
			}
			return null;
		}
		
		/// <summary>
		/// Gets all annotations stored on this AstNode.
		/// </summary>
		public IEnumerable<object> Annotations {
			get {
				object annotations = this.annotations;
				AnnotationList list = annotations as AnnotationList;
				if (list != null) {
					lock (list) {
						return list.ToArray ();
					}
				} else {
					if (annotations != null)
						return new object[] { annotations };
					else
						return Enumerable.Empty<object> ();
				}
			}
		}
	}
}

