// Copyright (c) AlphaSierraPapa for the SharpDevelop Team
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
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Serialization;
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
	[Serializable]
	public class SimpleProjectContent : AbstractAnnotatable, IProjectContent, ISerializable, IDeserializationCallback
	{
		readonly TypeStorage types = new TypeStorage();
		readonly ReaderWriterLockSlim readerWriterLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
		readonly Dictionary<string, IParsedFile> fileDict = new Dictionary<string, IParsedFile>(Platform.FileNameComparer);
		
		#region Constructor
		/// <summary>
		/// Creates a new SimpleProjectContent instance.
		/// </summary>
		public SimpleProjectContent()
		{
		}
		#endregion
		
		public virtual string AssemblyName {
			get { return string.Empty; }
		}
		
		#region AssemblyAttributes
		readonly List<IAttribute> assemblyAttributes = new List<IAttribute>(); // mutable assembly attribute storage
		readonly List<IAttribute> moduleAttributes = new List<IAttribute>();
		
		volatile IAttribute[] readOnlyAssemblyAttributes = {}; // volatile field with copy for reading threads
		volatile IAttribute[] readOnlyModuleAttributes = {};
		
		/// <inheritdoc/>
		public IList<IAttribute> AssemblyAttributes {
			get { return readOnlyAssemblyAttributes;  }
		}
		
		/// <inheritdoc/>
		public IList<IAttribute> ModuleAttributes {
			get { return readOnlyModuleAttributes;  }
		}
		
		static bool AddRemoveAttributes(ICollection<IAttribute> removedAttributes, ICollection<IAttribute> addedAttributes,
		                                List<IAttribute> attributeStorage)
		{
			// API uses ICollection instead of IEnumerable to discourage users from evaluating
			// the list inside the lock (this method is called inside the write lock)
			// [[not an issue anymore; the user now passes IParsedFile]]
			bool hasChanges = false;
			if (removedAttributes != null && removedAttributes.Count > 0) {
				if (attributeStorage.RemoveAll(removedAttributes.Contains) > 0)
					hasChanges = true;
			}
			if (addedAttributes != null) {
				attributeStorage.AddRange(addedAttributes);
				hasChanges = true;
			}
			return hasChanges;
		}
		
		void AddRemoveAssemblyAttributes(ICollection<IAttribute> removedAttributes, ICollection<IAttribute> addedAttributes)
		{
			if (AddRemoveAttributes(removedAttributes, addedAttributes, assemblyAttributes))
				readOnlyAssemblyAttributes = assemblyAttributes.ToArray();
		}
		
		void AddRemoveModuleAttributes(ICollection<IAttribute> removedAttributes, ICollection<IAttribute> addedAttributes)
		{
			if (AddRemoveAttributes(removedAttributes, addedAttributes, moduleAttributes))
				readOnlyModuleAttributes = moduleAttributes.ToArray();
		}
		#endregion
		
		#region AddType
		void AddType(ITypeDefinition typeDefinition)
		{
			if (typeDefinition == null)
				throw new ArgumentNullException("typeDefinition");
			if (typeDefinition.ProjectContent != this)
				throw new ArgumentException("Cannot add a type definition that belongs to another project content");
			
			ITypeDefinition existingTypeDef = types.GetTypeDefinition(typeDefinition.Namespace, typeDefinition.Name, typeDefinition.TypeParameterCount, StringComparer.Ordinal);
			if (existingTypeDef != null) {
				// Add a part to a compound class
				var newParts = new List<ITypeDefinition>(existingTypeDef.GetParts());
				newParts.Add(typeDefinition);
				types.UpdateType(CompoundTypeDefinition.Create(newParts));
			} else {
				types.UpdateType(typeDefinition);
			}
		}
		#endregion
		
		#region RemoveType
		void RemoveType(ITypeDefinition typeDefinition)
		{
			var compoundTypeDef = typeDefinition.GetDefinition() as CompoundTypeDefinition;
			if (compoundTypeDef != null) {
				// Remove one part from a compound class
				var newParts = new List<ITypeDefinition>(compoundTypeDef.GetParts());
				// We cannot use newParts.Remove() because we need to use reference equality
				for (int i = 0; i < newParts.Count; i++) {
					if (newParts[i] == typeDefinition) {
						newParts.RemoveAt(i);
						((DefaultTypeDefinition)typeDefinition).SetCompoundTypeDefinition(typeDefinition);
						break;
					}
				}
				types.UpdateType(CompoundTypeDefinition.Create(newParts));
			} else {
				types.RemoveType(typeDefinition);
			}
		}
		#endregion
		
		#region UpdateProjectContent
		/// <summary>
		/// Removes types and attributes from oldFile from the project, and adds those from newFile.
		/// </summary>
		/// <remarks>
		/// The update is done inside a write lock; when other threads access this project content
		/// from within a <c>using (Synchronize())</c> block, they will not see intermediate (inconsistent) state.
		/// </remarks>
		public void UpdateProjectContent(IParsedFile oldFile, IParsedFile newFile)
		{
			if (oldFile != null && newFile != null) {
				if (!Platform.FileNameComparer.Equals(oldFile.FileName, newFile.FileName))
					throw new ArgumentException("When both oldFile and newFile are specified, they must use the same file name.");
			}
			readerWriterLock.EnterWriteLock();
			try {
				if (oldFile != null) {
					foreach (var element in oldFile.TopLevelTypeDefinitions) {
						RemoveType(element);
					}
					if (newFile == null) {
						fileDict.Remove(oldFile.FileName);
					}
				}
				if (newFile != null) {
					foreach (var element in newFile.TopLevelTypeDefinitions) {
						AddType(element);
					}
					fileDict[newFile.FileName] = newFile;
				}
				AddRemoveAssemblyAttributes(oldFile != null ? oldFile.AssemblyAttributes : null,
				                            newFile != null ? newFile.AssemblyAttributes : null);
				
				AddRemoveModuleAttributes(oldFile != null ? oldFile.ModuleAttributes : null,
				                          newFile != null ? newFile.ModuleAttributes : null);
			} finally {
				readerWriterLock.ExitWriteLock();
			}
		}
		#endregion
		
		#region IProjectContent implementation
		public ITypeDefinition GetKnownTypeDefinition(TypeCode typeCode)
		{
			readerWriterLock.EnterReadLock();
			try {
				return types.GetKnownTypeDefinition(typeCode);
			} finally {
				readerWriterLock.ExitReadLock();
			}
		}
		
		public ITypeDefinition GetTypeDefinition(string nameSpace, string name, int typeParameterCount, StringComparer nameComparer)
		{
			readerWriterLock.EnterReadLock();
			try {
				return types.GetTypeDefinition(nameSpace, name, typeParameterCount, nameComparer);
			} finally {
				readerWriterLock.ExitReadLock();
			}
		}
		
		public IEnumerable<ITypeDefinition> GetTypes()
		{
			readerWriterLock.EnterReadLock();
			try {
				// make a copy with ToArray() for thread-safe access
				return types.GetTypes().ToArray();
			} finally {
				readerWriterLock.ExitReadLock();
			}
		}
		
		public IEnumerable<ITypeDefinition> GetTypes(string nameSpace, StringComparer nameComparer)
		{
			readerWriterLock.EnterReadLock();
			try {
				// make a copy with ToArray() for thread-safe access
				return types.GetTypes(nameSpace, nameComparer).ToArray();
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
		
		public IParsedFile GetFile(string fileName)
		{
			readerWriterLock.EnterReadLock();
			try {
				IParsedFile file;
				if (fileDict.TryGetValue(fileName, out file))
					return file;
				else
					return null;
			} finally {
				readerWriterLock.ExitReadLock();
			}
		}
		
		public IEnumerable<IParsedFile> Files {
			get {
				readerWriterLock.EnterReadLock();
				try {
					return fileDict.Values.ToArray();
				} finally {
					readerWriterLock.ExitReadLock();
				}
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
		
		#region Serialization
		SerializationInfo serializationInfo;
		
		protected SimpleProjectContent(SerializationInfo info, StreamingContext context)
		{
			this.serializationInfo = info;
			assemblyAttributes.AddRange((IAttribute[])info.GetValue("AssemblyAttributes", typeof(IAttribute[])));
			readOnlyAssemblyAttributes = assemblyAttributes.ToArray();
			moduleAttributes.AddRange((IAttribute[])info.GetValue("ModuleAttributes", typeof(IAttribute[])));
			readOnlyModuleAttributes = moduleAttributes.ToArray();
		}
		
		public virtual void OnDeserialization(object sender)
		{
			// We need to do this in OnDeserialization because at the time the deserialization
			// constructor runs, type.FullName/file.FileName may not be deserialized yet.
			if (serializationInfo != null) {
				foreach (var typeDef in (ITypeDefinition[])serializationInfo.GetValue("Types", typeof(ITypeDefinition[]))) {
					types.UpdateType(typeDef);
				}
				foreach (IParsedFile file in (IParsedFile[])serializationInfo.GetValue("Files", typeof(IParsedFile[]))) {
					fileDict.Add(file.FileName, file);
				}
				serializationInfo = null;
			}
		}
		
		public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			readerWriterLock.EnterReadLock();
			try {
				info.AddValue("Types", types.GetTypes().ToArray());
				info.AddValue("AssemblyAttributes", readOnlyAssemblyAttributes);
				info.AddValue("ModuleAttributes", readOnlyModuleAttributes);
				info.AddValue("Files", fileDict.Values.ToArray());
			} finally {
				readerWriterLock.ExitReadLock();
			}
		}
		#endregion
	}
}
