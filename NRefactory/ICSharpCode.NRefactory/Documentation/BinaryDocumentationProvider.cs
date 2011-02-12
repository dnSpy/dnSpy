// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using ICSharpCode.NRefactory.TypeSystem;

namespace ICSharpCode.NRefactory.Documentation
{
	/// <summary>
	/// Provides xml documentation from a binary cache file.
	/// This allows providing XML documentation without having to read the whole documentation into memory.
	/// </summary>
	public class BinaryDocumentationProvider : IDisposable, IDocumentationProvider
	{
		struct IndexEntry
		{
			public readonly int HashCode;
			public readonly int FileLocation;
			
			public IndexEntry(int HashCode, int FileLocation)
			{
				this.HashCode = HashCode;
				this.FileLocation = FileLocation;
			}
		}
		
		#region Save binary files
		// FILE FORMAT FOR BINARY DOCUMENTATION
		// long  magic = 0x4244636f446c6d58 (identifies file type = 'XmlDocDB')
		const long magic = 0x4244636f446c6d58;
		// short version = 3              (file version)
		const short version = 3;
		// long  fileDate                 (last change date of xml file in DateTime ticks)
		// int   testHashCode = magicTestString.GetHashCode() // (check if hash-code implementation is compatible)
		const string magicTestString = "HashMe-XmlDocDB";
		// int   entryCount               (count of entries)
		// int   indexPointer             (points to location where index starts in the file)
		// {
		//   string key                   (documentation key as length-prefixed string)
		//   string docu                  (xml documentation as length-prefixed string)
		// }
		// indexPointer points to the start of the following section:
		// {
		//   int hashcode
		//   int    index           (index where the docu string starts in the file)
		// }
		
		
		/// <summary>
		/// Saves the xml documentation into a on-disk database file.
		/// </summary>
		/// <param name="fileName">Filename of the database</param>
		/// <param name="fileDate">Last-modified date of the .xml file</param>
		/// <param name="xmlDocumentation">The xml documentation that should be written to disk.</param>
		public static void Save(string fileName, DateTime fileDate, IEnumerable<KeyValuePair<string, string>> xmlDocumentation)
		{
			if (fileName == null)
				throw new ArgumentNullException("fileName");
			if (xmlDocumentation == null)
				throw new ArgumentNullException("xmlDocumentation");
			using (FileStream fs = new FileStream(fileName, FileMode.Create, FileAccess.Write, FileShare.None)) {
				using (BinaryWriter w = new BinaryWriter(fs)) {
					w.Write(magic);
					w.Write(version);
					w.Write(fileDate.Ticks);
					w.Write(magicTestString.GetHashCode());
					
					List<IndexEntry> index = new List<IndexEntry>();
					int indexLengthPos = (int)fs.Position;
					w.Write(0); // skip 4 bytes for index length
					w.Write(0); // skip 4 bytes for index pointer
					
					int i = 0;
					foreach (KeyValuePair<string, string> p in xmlDocumentation) {
						index.Add(new IndexEntry(p.Key.GetHashCode(), (int)fs.Position));
						w.Write(p.Key);
						w.Write(p.Value.Trim());
						i += 1;
					}
					
					index.Sort((a,b) => a.HashCode.CompareTo(b.HashCode));
					
					int indexStart = (int)fs.Position;
					foreach (IndexEntry entry in index) {
						w.Write(entry.HashCode);
						w.Write(entry.FileLocation);
					}
					w.Seek(indexLengthPos, SeekOrigin.Begin);
					w.Write(index.Count); // write index length
					w.Write(indexStart); // write index count
				}
			}
		}
		#endregion
		
		BinaryReader loader;
		FileStream fs;
		
		Dictionary<string, string> xmlDescription = new Dictionary<string, string>();
		IndexEntry[] index; // SORTED array of index entries
		
		const int cacheLength = 50; // number of strings to cache when working in file-mode
		Queue<string> keyCacheQueue = new Queue<string>(cacheLength);
		
		#region Load binary files
		private BinaryDocumentationProvider() {}
		
		/// <summary>
		/// Loads binary documentation.
		/// </summary>
		/// <remarks>
		/// Don't forget to dispose the BinaryDocumentationProvider.
		/// </remarks>
		/// <param name="fileName">The name of the binary cache file.</param>
		/// <param name="fileDate">The file date of the original XML file. Loading will fail if the cached data was generated
		/// from a different file date than the original XML file.</param>
		/// <returns>
		/// The BinaryDocumentationProvider representing the file's content; or null if loading failed.
		/// </returns>
		public static BinaryDocumentationProvider Load(string fileName, DateTime fileDate)
		{
			BinaryDocumentationProvider doc = new BinaryDocumentationProvider();
			try {
				doc.fs = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete);
				int len = (int)doc.fs.Length;
				BinaryReader loader = doc.loader = new BinaryReader(doc.fs);
				if (loader.ReadInt64() != magic) {
					Debug.WriteLine("Cannot load XmlDoc: wrong magic");
					return null;
				}
				if (loader.ReadInt16() != version) {
					Debug.WriteLine("Cannot load XmlDoc: wrong version");
					return null;
				}
				if (loader.ReadInt64() != fileDate.Ticks) {
					Debug.WriteLine("Not loading XmlDoc: file changed since cache was created");
					return null;
				}
				int count = loader.ReadInt32();
				int indexStartPosition = loader.ReadInt32(); // go to start of index
				if (indexStartPosition <= 0 || indexStartPosition >= len) {
					Debug.WriteLine("XmlDoc: Cannot find index, cache invalid!");
					return null;
				}
				doc.fs.Position = indexStartPosition;
				IndexEntry[] index = new IndexEntry[count];
				for (int i = 0; i < index.Length; i++) {
					index[i] = new IndexEntry(loader.ReadInt32(), loader.ReadInt32());
				}
				doc.index = index;
				return doc;
			} catch (IOException ex) {
				Debug.WriteLine("Cannot load from cache" + ex.ToString());
				return null;
			}
		}
		
		string LoadDocumentation(string key)
		{
			if (keyCacheQueue.Count > cacheLength - 1) {
				xmlDescription.Remove(keyCacheQueue.Dequeue());
			}
			
			int hashcode = key.GetHashCode();
			
			// use binary search to find the item
			string resultDocu = null;
			
			int m = Array.BinarySearch(index, new IndexEntry(hashcode, 0));
			if (m >= 0) {
				// correct hash code found.
				// possibly there are multiple items with the same hash, so go to the first.
				while (--m >= 0 && index[m].HashCode == hashcode);
				// go through all items that have the correct hash
				while (++m < index.Length && index[m].HashCode == hashcode) {
					fs.Position = index[m].FileLocation;
					string keyInFile = loader.ReadString();
					if (keyInFile == key) {
						//LoggingService.Debug("Got XML documentation for " + key);
						resultDocu = loader.ReadString();
						break;
					} else {
						// this is a harmless hash collision, just continue reading
						Debug.WriteLine("Found " + keyInFile + " instead of " + key);
					}
				}
			}
			
			keyCacheQueue.Enqueue(key);
			xmlDescription.Add(key, resultDocu);
			
			return resultDocu;
		}
		#endregion
		
		public string GetDocumentation(string key)
		{
			lock (xmlDescription) {
				if (index == null)
					throw new ObjectDisposedException("BinaryDocumentationProvider");
				string result;
				if (xmlDescription.TryGetValue(key, out result))
					return result;
				return LoadDocumentation(key);
			}
		}
		
		public string GetDocumentation(IEntity entity)
		{
			return GetDocumentation(XmlDocumentationProvider.GetDocumentationKey(entity));
		}
		
		public void Dispose()
		{
			lock (xmlDescription) {
				if (loader != null) {
					loader.Close();
					fs.Close();
				}
				xmlDescription.Clear();
				index = null;
				keyCacheQueue = null;
				loader = null;
				fs = null;
			}
		}
	}
}
