//
// FrameworkLookup.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2013 Xamarin Inc. (http://xamarin.com)
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
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ICSharpCode.NRefactory.Semantics;
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.TypeSystem.Implementation;

namespace ICSharpCode.NRefactory.Completion
{
	/// <summary>
	/// The framework lookup provides a fast lookup where an unknow type or extension method may be defined in.
	/// </summary>
	public sealed class FrameworkLookup 
	{
		/*		 Binary format:
		 * [Header]
		 *    [Version] : [Major (byte)] [Minor (byte)] [Build  (byte)]
		 * 	  [#Types (int)]
		 *    [#Methods (int)]
		 *    [#Assemblies (int)]
		 * [AssemblyListTable] : #Assemblies x [OffsetToAssemblyLists (int)]
		 * [TypeLookupTable] : #Types x ( [NameHash (int)] [AssemblyPtrToAssemblyListTable (ushort)]
		 * [ExtMethodLookupTable] : #Methods x ( [NameHash (int)] [AssemblyPtrToAssemblyListTable (ushort)]
		 * [AssemblyLists] 
		 *    [#Count (byte)]
		 *    #Count x [AssemblyLookup] : [Package (string)] [FullName (string)] [Namespace (string)] 
		*/
		const int headerSize = 
			3 + // Version
			4 + // #Types
			4 + // #Methods
			4   // #Assembly
			/*			+ 4*/;

		public static readonly Version CurrentVersion = new Version (2, 0, 1);
		public static readonly FrameworkLookup Empty = new FrameworkLookup ();

		string fileName;
		int[] assemblyListTable;
		int[] typeLookupTable;
		int[] extLookupTable;

		/// <summary>
		/// This method tries to get a matching extension method.
		/// </summary>
		/// <returns>The extension method lookups.</returns>
		/// <param name="resolveResult">The resolve result.</param>
		public IEnumerable<AssemblyLookup> GetExtensionMethodLookups (UnknownMemberResolveResult resolveResult)
		{
			return GetLookup (resolveResult.MemberName, extLookupTable, headerSize + assemblyListTable.Length * 4 + typeLookupTable.Length * 8);
		}

		/// <summary>
		/// Tries to get a type out of an unknow identifier result.
		/// </summary>
		/// <returns>The assemblies the type may be defined (if any).</returns>
		/// <param name="resolveResult">The resolve result.</param>
		/// <param name="typeParameterCount">Type parameter count.</param>
		/// <param name="isInsideAttributeType">If set to <c>true</c> this resolve result may be inside an attribute.</param>
		public IEnumerable<AssemblyLookup> GetLookups (UnknownIdentifierResolveResult resolveResult, int typeParameterCount, bool isInsideAttributeType)
		{
			string name = isInsideAttributeType ? resolveResult.Identifier + "Attribute" : resolveResult.Identifier;

			var identifier = GetIdentifier (name, typeParameterCount);
			return GetLookup (identifier, typeLookupTable, headerSize + assemblyListTable.Length * 4);
		}

		/// <summary>
		/// Tries to get a type out of an unknow member resolve result. (In case of fully qualified names)
		/// </summary>
		/// <returns>The assemblies the type may be defined (if any).</returns>
		/// <param name="resolveResult">The resolve result.</param>
		/// <param name="fullMemberName"></param>
		/// <param name="typeParameterCount">Type parameter count.</param>
		/// <param name="isInsideAttributeType">If set to <c>true</c> this resolve result may be inside an attribute.</param>
		public IEnumerable<AssemblyLookup> GetLookups (UnknownMemberResolveResult resolveResult, string fullMemberName, int typeParameterCount, bool isInsideAttributeType)
		{
			string name = isInsideAttributeType ? resolveResult.MemberName + "Attribute" : resolveResult.MemberName;

			var identifier = GetIdentifier (name, typeParameterCount);
			foreach (var lookup in GetLookup (identifier, typeLookupTable, headerSize + assemblyListTable.Length * 4)) {
				if (fullMemberName.StartsWith (lookup.Namespace, StringComparison.Ordinal))
					yield return lookup;
			}
		}

		/// <summary>
		/// The assembly lookup determines where a type might be defined.
		/// It contains the assembly &amp; the namespace.
		/// </summary>
		public struct AssemblyLookup
		{
			readonly string nspace;

			/// <summary>
			/// The namespace the requested type is in.
			/// </summary>
			public string Namespace {
				get {
					return nspace;
				}
			}

			readonly string fullName;
			/// <summary>
			/// Gets the full name af the assembly.
			/// </summary>
			public string FullName {
				get {
					return fullName;
				}
			}

			readonly string package;
			/// <summary>
			/// Gets the package the assembly is in.
			/// </summary>
			public string Package {
				get {
					return package;
				}
			}

			/// <summary>
			/// Initializes a new instance of the <see cref="AssemblyLookup"/> struct.
			/// </summary>
			/// <param name="package">The package name.</param>
			/// <param name="fullName">The full name of the assembly.</param>
			/// <param name="nspace">The namespace the type is in.</param>
			internal AssemblyLookup (string package, string fullName, string nspace)
			{
				if (nspace == null)
					throw new ArgumentNullException ("nspace");
				if (fullName == null)
					throw new ArgumentNullException ("fullName");
				this.package = package;
				this.fullName = fullName;
				this.nspace = nspace;
			}

			public override string ToString ()
			{
				return string.Format ("[AssemblyLookup: Namespace={0}, FullName={1}, Package={2}]", Namespace, FullName, Package);
			}

			public override bool Equals (object obj)
			{
				if (obj == null)
					return false;
				//					if (ReferenceEquals (this, obj))
				//						return true;
				if (obj.GetType () != typeof(AssemblyLookup))
					return false;
				var other = (AssemblyLookup)obj;
				return Namespace == other.Namespace && FullName == other.FullName && Package == other.Package;
			}

			public override int GetHashCode ()
			{
				unchecked {
					return (Namespace != null ? Namespace.GetHashCode () : 0) ^
						(FullName != null ? FullName.GetHashCode () : 0) ^ 
						(Package != null ? Package.GetHashCode () : 0);
				}
			}
		}

		/// <summary>
		/// This method returns a new framework builder to build a new framework lookup data file.
		/// </summary>
		/// <param name="fileName">The file name of the data file.</param>
		public static FrameworkBuilder Create (string fileName)
		{
			return new FrameworkBuilder (fileName);
		}

		/// <summary>
		/// Loads a framework lookup object from a file. May return null, if the file wasn't found or has a version mismatch.
		/// </summary>
		/// <param name="fileName">File name.</param>
		public static FrameworkLookup Load (string fileName)
		{
			try {
				if (!File.Exists (fileName))
					return null;
			} catch (Exception) {
				return null;
			}
			var result = new FrameworkLookup ();
			result.fileName = fileName;
			var fs = File.OpenRead (fileName);
			using (var reader = new BinaryReader (fs, Encoding.UTF8)) {
				var major = reader.ReadByte ();
				var minor = reader.ReadByte ();
				var build = reader.ReadByte ();
				var version = new Version (major, minor, build);
				if (version != CurrentVersion)
					return null;
				int typeLookupListCount = reader.ReadInt32 ();
				int extLookupListCount = reader.ReadInt32 ();
				int assemblyLookupCount = reader.ReadInt32 ();

				result.assemblyListTable = new int[assemblyLookupCount];
				for (int i = 0; i < assemblyLookupCount; i++) {
					result.assemblyListTable[i] = reader.ReadInt32 ();
				}

				result.typeLookupTable = new int[typeLookupListCount];
				for (int i = 0; i < typeLookupListCount; i++) {
					result.typeLookupTable [i] = reader.ReadInt32 ();
					// skip list offset
					reader.ReadInt32 ();
				}

				result.extLookupTable = new int[extLookupListCount];
				for (int i = 0; i < extLookupListCount; i++) {
					result.extLookupTable [i] = reader.ReadInt32 ();
					// skip list offset
					reader.ReadInt32 ();
				}
			}
			return result;
		}

		FrameworkLookup ()
		{
		}

		IEnumerable<AssemblyLookup> GetLookup (string identifier, int[] lookupTable, int tableOffset)
		{
			if (lookupTable == null)
				yield break;

			int index = Array.BinarySearch (lookupTable, GetStableHashCode (identifier));
			if (index < 0)
				yield break;

			using (var reader = new BinaryReader (File.Open (fileName, FileMode.Open, FileAccess.Read, FileShare.Read), Encoding.UTF8)) {
				reader.BaseStream.Seek (tableOffset + index * 8 + 4, SeekOrigin.Begin);
				int listPtr = reader.ReadInt32 ();

				reader.BaseStream.Seek (listPtr, SeekOrigin.Begin);
				var b = reader.ReadInt32 ();
				var assemblies = new List<ushort> ();
				while (b-- > 0) {
					var assembly = reader.ReadUInt16 ();
					if (assembly < 0 || assembly >= assemblyListTable.Length)
						throw new InvalidDataException ("Assembly lookup was " + assembly + " but only " + assemblyListTable.Length + " are known.");
					assemblies.Add (assembly);
				}
				foreach (var assembly in assemblies) {
					reader.BaseStream.Seek (assemblyListTable [assembly], SeekOrigin.Begin);

					var package = reader.ReadString ();
					var fullName = reader.ReadString ();
					var ns = reader.ReadString ();
					yield return new AssemblyLookup (package, fullName, ns);
				}
			}
		}

		/// <summary>
		/// Retrieves a hash code for the specified string that is stable across
		/// .NET upgrades.
		/// 
		/// Use this method instead of the normal <c>string.GetHashCode</c> if the hash code
		/// is persisted to disk.
		/// </summary>
		static int GetStableHashCode(string text)
		{
			unchecked {
				int h = 0;
				foreach (char c in text) {
					h = (h << 5) - h + c;
				}
				return h;
			}
		}

		static string GetIdentifier (string identifier, int tc)
		{
			if (tc == 0)
				return identifier;
			return identifier + "`" + tc;
		}

		public class FrameworkBuilder : IDisposable
		{
			readonly string fileName;

			Dictionary<int, List<ushort>> typeLookup = new Dictionary<int, List<ushort>>  ();
			Dictionary<int, List<ushort>> extensionMethodLookup = new Dictionary<int, List<ushort>>  ();
			List<AssemblyLookup> assemblyLookups = new List<AssemblyLookup> ();
			Dictionary<int, string> methodCheck = new Dictionary<int, string> ();
			Dictionary<int, string> typeCheck = new Dictionary<int, string> ();

			internal FrameworkBuilder (string fileName)
			{
				this.fileName = fileName;
			}

			static int[] WriteTable (MemoryStream stream, Dictionary<int, List<ushort>> table, out List<KeyValuePair<int, List<ushort>>> list)
			{
				list = new List<KeyValuePair<int, List<ushort>>> (table);
				list.Sort ((x, y) => x.Key.CompareTo (y.Key));

				var result = new int[list.Count];
				using (var bw = new BinaryWriter (stream)) {
					for (int i = 0; i < result.Length; i++) {
						result [i] = (int)stream.Length;
						bw.Write (list [i].Value.Count);
						foreach (var ii in list [i].Value)
							bw.Write (ii);
					}
				}

				return result;
			}

			#region IDisposable implementation

			void IDisposable.Dispose ()
			{
				var typeLookupMemory = new MemoryStream ();
				List<KeyValuePair<int, List<ushort>>> typeLookupList;
				var typeTable = WriteTable (typeLookupMemory, typeLookup, out typeLookupList);

				var extMethodLookupMemory = new MemoryStream ();
				List<KeyValuePair<int, List<ushort>>> extMethodLookuplist;
				var extMethodTable = WriteTable (extMethodLookupMemory, extensionMethodLookup, out extMethodLookuplist);

				var assemblyLookupMemory = new MemoryStream ();
				var assemblyPositionTable = new int[assemblyLookups.Count];
				using (var writer = new BinaryWriter (assemblyLookupMemory, Encoding.UTF8)) {
					for (int i = 0; i < assemblyLookups.Count; i++) {
						var lookup = assemblyLookups[i];
						assemblyPositionTable[i] = (int)assemblyLookupMemory.Length;
						writer.Write (lookup.Package);
						writer.Write (lookup.FullName);
						writer.Write (lookup.Namespace);
					}
				}

				using (var stream = new BinaryWriter (File.OpenWrite (fileName), Encoding.UTF8)) {
					stream.Write ((byte)CurrentVersion.Major);
					stream.Write ((byte)CurrentVersion.Minor);
					stream.Write ((byte)CurrentVersion.Build);

					stream.Write (typeLookupList.Count);
					stream.Write (extMethodLookuplist.Count);
					stream.Write (assemblyLookups.Count);

					var typeBuffer = typeLookupMemory.ToArray ();
					var extMethodBuffer = extMethodLookupMemory.ToArray ();

					int dataOffset = 
						headerSize + 
						assemblyLookups.Count * 4 + 
						typeLookupList.Count * (4 + 4) + 
						extMethodLookuplist.Count * (4 + 4);

					for (int i = 0; i < assemblyLookups.Count; i++) {
						stream.Write ((int)(dataOffset + typeBuffer.Length + extMethodBuffer.Length + assemblyPositionTable[i]));
					}

					for (int i = 0; i < typeLookupList.Count; i++) {
						stream.Write (typeLookupList [i].Key);
						stream.Write (dataOffset + typeTable[i]);
					}

					for (int i = 0; i < extMethodLookuplist.Count; i++) {
						stream.Write (extMethodLookuplist [i].Key);
						stream.Write (dataOffset + typeBuffer.Length + extMethodTable[i]);
					}

					stream.Write (typeBuffer);
					stream.Write (extMethodBuffer);
					stream.Write (assemblyLookupMemory.ToArray ());
					stream.Flush ();
				}
			}
			#endregion

			struct FrameworkLookupId 
			{
				public string PackageName;
				public string AssemblyName;
				public string NameSpace;
			}

			Dictionary<FrameworkLookupId, ushort> frameworkLookupTable = new Dictionary<FrameworkLookupId, ushort> ();
			ushort GetLookup (string packageName, string assemblyName, string ns)
			{
				var id = new FrameworkLookupId {
					PackageName = packageName,
					AssemblyName = assemblyName,
					NameSpace = ns
				};
				ushort value;
				if (frameworkLookupTable.TryGetValue (id, out value))
					return value;

				var result = new AssemblyLookup (packageName, assemblyName, ns);
				assemblyLookups.Add (result);
				var index = assemblyLookups.Count - 1;
				if (index > ushort.MaxValue)
					throw new InvalidOperationException ("Assembly lookup list overflow > " + ushort.MaxValue + " assemblies.");
				frameworkLookupTable.Add (id, (ushort)index);
				return (ushort)index;
			}

			bool AddToTable (string packageName, string assemblyName, Dictionary<int, List<ushort>> table, Dictionary<int, string> checkTable, string id, string ns)
			{
				List<ushort> list;
				var hash = GetStableHashCode (id);

				if (!table.TryGetValue (hash, out list)) {
					list = new List<ushort> ();
					table [hash] = list;
				} else {
					string existingString;
					if (checkTable.TryGetValue (hash, out existingString)) {
						if (existingString != id)
							throw new InvalidOperationException ("Duplicate hash for " + existingString + " and "+ id); 
					} else {
						checkTable.Add (hash, id);
					}
				}
				var assemblyLookup = GetLookup (packageName, assemblyName, ns);
				if (!list.Any (a => a.Equals (assemblyLookup))) {
					list.Add (assemblyLookup);
					return true;
				}
				return false;
			}

			/// <summary>
			/// Add a type to the framework lookup.
			/// </summary>
			/// <param name="packageName">The package the assembly of the type is defined (can be null).</param>
			/// <param name="fullAssemblyName">The full assembly name the type is defined (needs to be != null).</param>
			/// <param name="type">The type definition  (needs to be != null).</param>
			public void AddLookup (string packageName, string fullAssemblyName, IUnresolvedTypeDefinition type)
			{
				if (fullAssemblyName == null)
					throw new ArgumentNullException ("fullAssemblyName");
				if (type == null)
					throw new ArgumentNullException ("type");
				var id = GetIdentifier (type.Name, type.TypeParameters.Count);
				if (AddToTable (packageName, fullAssemblyName, typeLookup, typeCheck, id, type.Namespace)) {
					if (type.IsSealed || type.IsStatic) {
						foreach (var method in type.Methods) {
							var m = method as DefaultUnresolvedMethod;
							if (m == null || !m.IsExtensionMethod)
								continue;
							AddToTable (packageName, fullAssemblyName, extensionMethodLookup, methodCheck, method.Name, method.DeclaringTypeDefinition.Namespace);
						}
					}
				}
			}
		}
	}
}