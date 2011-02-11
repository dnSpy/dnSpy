// Copyright (c) 2010 AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;

using ICSharpCode.NRefactory.Utils;

namespace ICSharpCode.NRefactory.TypeSystem.Implementation
{
	/// <summary>
	/// Simple <see cref="IProjectContent"/> implementation that stores the list of classes/namespaces.
	/// Synchronization is implemented using a <see cref="ReaderWriterLockSlim"/>.
	/// </summary>
	/// <remarks>
	/// Compared with <see cref="TypeStorage"/>, this class adds support for the IProjectContent interface,
	/// for partial classes, and for multi-threading.
	/// </remarks>
	public sealed class SimpleProjectContent : IProjectContent
	{
		// This class is sealed by design:
		// the synchronization story doesn't mix well with someone trying to extend this class.
		// If you wanted to derive from this: use delegation, not inheritance.
		
		readonly TypeStorage types = new TypeStorage();
		readonly ReaderWriterLockSlim readerWriterLock = new ReaderWriterLockSlim();
		
		#region AssemblyAttributes
		readonly List<IAttribute> assemblyAttributes = new List<IAttribute>(); // mutable assembly attribute storage
		
		volatile IAttribute[] readOnlyAssemblyAttributes = {}; // volatile field with copy for reading threads
		
		/// <inheritdoc/>
		public IList<IAttribute> AssemblyAttributes {
			get { return readOnlyAssemblyAttributes; }
		}
		
		void AddRemoveAssemblyAttributes(ICollection<IAttribute> addedAttributes, ICollection<IAttribute> removedAttributes)
		{
			// API uses ICollection instead of IEnumerable to discourage users from evaluating
			// the list inside the lock (this method is called inside the write lock)
			bool hasChanges = false;
			if (removedAttributes != null && removedAttributes.Count > 0) {
				if (assemblyAttributes.RemoveAll(removedAttributes.Contains) > 0)
					hasChanges = true;
			}
			if (addedAttributes != null) {
				assemblyAttributes.AddRange(addedAttributes);
				hasChanges = true;
			}
			
			if (hasChanges)
				readOnlyAssemblyAttributes = assemblyAttributes.ToArray();
		}
		#endregion
		
		#region AddType
		void AddType(ITypeDefinition typeDefinition)
		{
			if (typeDefinition == null)
				throw new ArgumentNullException("typeDefinition");
			typeDefinition.Freeze(); // Type definition must be frozen before it can be added to a project content
			if (typeDefinition.ProjectContent != this)
				throw new ArgumentException("Cannot add a type definition that belongs to another project content");
			
			// TODO: handle partial classes
			types.UpdateType(typeDefinition);
		}
		#endregion
		
		#region RemoveType
		void RemoveType(ITypeDefinition typeDefinition)
		{
			throw new NotImplementedException();
		}
		#endregion
		
		#region UpdateProjectContent
		/// <summary>
		/// Removes oldTypes from the project, adds newTypes.
		/// Removes oldAssemblyAttributes, adds newAssemblyAttributes.
		/// </summary>
		/// <remarks>
		/// The update is done inside a write lock; when other threads access this project content
		/// from within a <c>using (Synchronize())</c> block, they will not see intermediate (inconsistent) state.
		/// </remarks>
		public void UpdateProjectContent(ICollection<ITypeDefinition> oldTypes = null,
		                                 ICollection<ITypeDefinition> newTypes = null,
		                                 ICollection<IAttribute> oldAssemblyAttributes = null,
		                                 ICollection<IAttribute> newAssemblyAttributes = null)
		{
			readerWriterLock.EnterWriteLock();
			try {
				if (oldTypes != null) {
					foreach (var element in oldTypes) {
						RemoveType(element);
					}
				}
				if (newTypes != null) {
					foreach (var element in newTypes) {
						AddType(element);
					}
				}
				AddRemoveAssemblyAttributes(oldAssemblyAttributes, newAssemblyAttributes);
			} finally {
				readerWriterLock.ExitWriteLock();
			}
		}
		#endregion
		
		#region IProjectContent implementation
		public ITypeDefinition GetClass(string nameSpace, string name, int typeParameterCount, StringComparer nameComparer)
		{
			readerWriterLock.EnterReadLock();
			try {
				return types.GetClass(nameSpace, name, typeParameterCount, nameComparer);
			} finally {
				readerWriterLock.ExitReadLock();
			}
		}
		
		public IEnumerable<ITypeDefinition> GetClasses()
		{
			readerWriterLock.EnterReadLock();
			try {
				// make a copy with ToArray() for thread-safe access
				return types.GetClasses().ToArray();
			} finally {
				readerWriterLock.ExitReadLock();
			}
		}
		
		public IEnumerable<ITypeDefinition> GetClasses(string nameSpace, StringComparer nameComparer)
		{
			readerWriterLock.EnterReadLock();
			try {
				// make a copy with ToArray() for thread-safe access
				return types.GetClasses(nameSpace, nameComparer).ToArray();
			} finally {
				readerWriterLock.ExitReadLock();
			}
		}
		
		public IEnumerable<string> GetNamespaces()
		{
			readerWriterLock.EnterReadLock();
			try {
				// make a copy with ToArray() for thread-safe access
				return types.GetNamespaces().ToArray();
			} finally {
				readerWriterLock.ExitReadLock();
			}
		}
		
		public string GetNamespace(string nameSpace, StringComparer nameComparer)
		{
			readerWriterLock.EnterReadLock();
			try {
				return types.GetNamespace(nameSpace, nameComparer);
			} finally {
				readerWriterLock.ExitReadLock();
			}
		}
		#endregion
		
		#region Synchronization
		public CacheManager CacheManager {
			get { return null; }
		}
		
		public ISynchronizedTypeResolveContext Synchronize()
		{
			// don't acquire the lock on OutOfMemoryException etc.
			ISynchronizedTypeResolveContext sync = new ReadWriteSynchronizedTypeResolveContext(types, readerWriterLock);
			readerWriterLock.EnterReadLock();
			return sync;
		}
		
		sealed class ReadWriteSynchronizedTypeResolveContext : ProxyTypeResolveContext, ISynchronizedTypeResolveContext
		{
			ReaderWriterLockSlim readerWriterLock;
			
			public ReadWriteSynchronizedTypeResolveContext(ITypeResolveContext target, ReaderWriterLockSlim readerWriterLock)
				: base(target)
			{
				this.readerWriterLock = readerWriterLock;
			}
			
			public void Dispose()
			{
				if (readerWriterLock != null) {
					readerWriterLock.ExitReadLock();
					readerWriterLock = null;
				}
			}
			
			public override ISynchronizedTypeResolveContext Synchronize()
			{
				// nested Synchronize() calls don't need any locking
				return new ReadWriteSynchronizedTypeResolveContext(target, null);
			}
		}
		#endregion
	}
}
