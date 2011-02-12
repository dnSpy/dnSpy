// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;

namespace ICSharpCode.SharpDevelop.Dom
{
	/// <summary>
	/// This class can write Dom entity into a binary file for fast loading.
	/// </summary>
	public sealed class DomPersistence
	{
		public const long FileMagic = 0x11635233ED2F428C;
		public const long IndexFileMagic = 0x11635233ED2F427D;
		public const short FileVersion = 28;
		
		ProjectContentRegistry registry;
		string cacheDirectory;
		
		internal string CacheDirectory {
			get {
				return cacheDirectory;
			}
		}
		
		internal DomPersistence(string cacheDirectory, ProjectContentRegistry registry)
		{
			this.cacheDirectory = cacheDirectory;
			this.registry = registry;
			
			cacheIndex = LoadCacheIndex();
		}
		
		#region Cache management
		public string SaveProjectContent(ReflectionProjectContent pc)
		{
			// create cache directory, if necessary
			Directory.CreateDirectory(cacheDirectory);
			
			string assemblyFullName = pc.AssemblyFullName;
			int pos = assemblyFullName.IndexOf(',');
			string fileName = Path.Combine(cacheDirectory,
			                               assemblyFullName.Substring(0, pos)
			                               + "." + pc.AssemblyLocation.GetHashCode().ToString("x", CultureInfo.InvariantCulture)
			                               + ".dat");
			AddFileNameToCacheIndex(Path.GetFileName(fileName), pc);
			using (FileStream fs = new FileStream(fileName, FileMode.Create, FileAccess.Write)) {
				WriteProjectContent(pc, fs);
			}
			return fileName;
		}
		
		public ReflectionProjectContent LoadProjectContentByAssemblyName(string assemblyName)
		{
			string cacheFileName;
			if (CacheIndex.TryGetValue(assemblyName, out cacheFileName)) {
				cacheFileName = Path.Combine(cacheDirectory, cacheFileName);
				if (File.Exists(cacheFileName)) {
					return LoadProjectContent(cacheFileName);
				}
			}
			return null;
		}
		
		public ReflectionProjectContent LoadProjectContent(string cacheFileName)
		{
			using (FileStream fs = new FileStream(cacheFileName, FileMode.Open, FileAccess.Read,
			                                      FileShare.Read | FileShare.Delete, 4096, FileOptions.SequentialScan)) {
				return LoadProjectContent(fs);
			}
		}
		#endregion
		
		#region Cache index
		string GetIndexFileName() { return Path.Combine(cacheDirectory, "index.dat"); }
		
		Dictionary<string, string> cacheIndex;
		
		Dictionary<string, string> CacheIndex {
			get {
				return cacheIndex;
			}
		}
		
		Dictionary<string, string> LoadCacheIndex()
		{
			string indexFile = GetIndexFileName();
			Dictionary<string, string> list = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			if (File.Exists(indexFile)) {
				try {
					using (FileStream fs = new FileStream(indexFile, FileMode.Open, FileAccess.Read)) {
						using (BinaryReader reader = new BinaryReader(fs)) {
							if (reader.ReadInt64() != IndexFileMagic) {
								LoggingService.Warn("Index cache has wrong file magic");
								return list;
							}
							if (reader.ReadInt16() != FileVersion) {
								LoggingService.Warn("Index cache has wrong file version");
								return list;
							}
							int count = reader.ReadInt32();
							for (int i = 0; i < count; i++) {
								string key = reader.ReadString();
								list[key] = reader.ReadString();
							}
						}
					}
				} catch (IOException ex) {
					LoggingService.Warn("Error reading DomPersistance cache index", ex);
				}
			}
			return list;
		}
		
		void SaveCacheIndex(Dictionary<string, string> cacheIndex)
		{
			string indexFile = GetIndexFileName();
			using (FileStream fs = new FileStream(indexFile, FileMode.Create, FileAccess.Write)) {
				using (BinaryWriter writer = new BinaryWriter(fs)) {
					writer.Write(IndexFileMagic);
					writer.Write(FileVersion);
					writer.Write(cacheIndex.Count);
					foreach (KeyValuePair<string, string> e in cacheIndex) {
						writer.Write(e.Key);
						writer.Write(e.Value);
					}
				}
			}
		}
		
		void AddFileNameToCacheIndex(string cacheFile, ReflectionProjectContent pc)
		{
			Dictionary<string, string> l = LoadCacheIndex();
			l[pc.AssemblyLocation] = cacheFile;
			string txt = pc.AssemblyFullName;
			l[txt] = cacheFile;
			int pos = txt.LastIndexOf(',');
			do {
				txt = txt.Substring(0, pos);
				if (l.ContainsKey(txt))
					break;
				l[txt] = cacheFile;
				pos = txt.LastIndexOf(',');
			} while (pos >= 0);
			SaveCacheIndex(l);
			cacheIndex = l;
		}
		#endregion
		
		#region Saving / Loading without cache
		/// <summary>
		/// Saves the project content to the stream.
		/// </summary>
		public static void WriteProjectContent(ReflectionProjectContent pc, Stream stream)
		{
			BinaryWriter writer = new BinaryWriter(stream);
			new ReadWriteHelper(writer).WriteProjectContent(pc);
			// do not close the stream
		}
		
		/// <summary>
		/// Load a project content from a stream.
		/// </summary>
		public ReflectionProjectContent LoadProjectContent(Stream stream)
		{
			return LoadProjectContent(stream, registry);
		}
		
		public static ReflectionProjectContent LoadProjectContent(Stream stream, ProjectContentRegistry registry)
		{
			ReflectionProjectContent pc;
			BinaryReader reader = new BinaryReader(stream);
			try {
				pc = new ReadWriteHelper(reader).ReadProjectContent(registry);
				if (pc != null) {
					pc.InitializeSpecialClasses();
					pc.AssemblyCompilationUnit.Freeze();
				}
				return pc;
			} catch (EndOfStreamException) {
				LoggingService.Warn("Read dom: EndOfStreamException");
				return null;
			} catch (Exception ex) {
				HostCallback.ShowMessage("Error loading cached code-completion data.\n" +
				                         "The cached file might be corrupted and will be regenerated.\n\n" +
				                         ex.ToString());
				return null;
			}
			// do not close the stream
		}
		#endregion
		
		private struct ClassNameTypeCountPair
		{
			public readonly string ClassName;
			public readonly byte TypeParameterCount;
			
			public ClassNameTypeCountPair(IClass c) {
				this.ClassName = c.FullyQualifiedName;
				this.TypeParameterCount = (byte)c.TypeParameters.Count;
			}
			
			public ClassNameTypeCountPair(IReturnType rt) {
				this.ClassName = rt.FullyQualifiedName;
				this.TypeParameterCount = (byte)rt.TypeArgumentCount;
			}
			
			public override bool Equals(object obj) {
				if (!(obj is ClassNameTypeCountPair)) return false;
				ClassNameTypeCountPair myClassNameTypeCountPair = (ClassNameTypeCountPair)obj;
				if (ClassName != myClassNameTypeCountPair.ClassName) return false;
				if (TypeParameterCount != myClassNameTypeCountPair.TypeParameterCount) return false;
				return true;
			}
			
			public override int GetHashCode() {
				return ClassName.GetHashCode() ^ ((int)TypeParameterCount * 5);
			}
		}
		
		private sealed class ReadWriteHelper
		{
			ReflectionProjectContent pc;
			
			// for writing:
			readonly BinaryWriter writer;
			readonly Dictionary<ClassNameTypeCountPair, int> classIndices = new Dictionary<ClassNameTypeCountPair, int>();
			readonly Dictionary<string, int> stringDict = new Dictionary<string, int>();
			
			// for reading:
			readonly BinaryReader reader;
			IReturnType[] types;
			string[] stringArray;
			
			#region Write/Read ProjectContent
			public ReadWriteHelper(BinaryWriter writer)
			{
				this.writer = writer;
			}
			
			public void WriteProjectContent(ReflectionProjectContent pc)
			{
				this.pc = pc;
				writer.Write(FileMagic);
				writer.Write(FileVersion);
				writer.Write(pc.AssemblyFullName);
				writer.Write(pc.AssemblyLocation);
				long time = 0;
				try {
					time = File.GetLastWriteTimeUtc(pc.AssemblyLocation).ToFileTime();
				} catch {}
				writer.Write(time);
				writer.Write(pc.ReferencedAssemblyNames.Count);
				foreach (DomAssemblyName name in pc.ReferencedAssemblyNames) {
					writer.Write(name.FullName);
				}
				WriteClasses();
			}
			
			public ReadWriteHelper(BinaryReader reader)
			{
				this.reader = reader;
			}
			
			public ReflectionProjectContent ReadProjectContent(ProjectContentRegistry registry)
			{
				if (reader.ReadInt64() != FileMagic) {
					LoggingService.Warn("Read dom: wrong magic");
					return null;
				}
				if (reader.ReadInt16() != FileVersion) {
					LoggingService.Warn("Read dom: wrong version");
					return null;
				}
				string assemblyName = reader.ReadString();
				string assemblyLocation = reader.ReadString();
				long time = 0;
				try {
					time = File.GetLastWriteTimeUtc(assemblyLocation).ToFileTime();
				} catch {}
				if (reader.ReadInt64() != time) {
					LoggingService.Warn("Read dom: assembly changed since cache was created");
					return null;
				}
				DomAssemblyName[] referencedAssemblies = new DomAssemblyName[reader.ReadInt32()];
				for (int i = 0; i < referencedAssemblies.Length; i++) {
					referencedAssemblies[i] = new DomAssemblyName(reader.ReadString());
				}
				this.pc = new ReflectionProjectContent(assemblyName, assemblyLocation, referencedAssemblies, registry);
				if (ReadClasses()) {
					return pc;
				} else {
					LoggingService.Warn("Read dom: error in file (invalid control mark)");
					return null;
				}
			}
			
			void WriteClasses()
			{
				ICollection<IClass> classes = pc.Classes;
				IList<IAttribute> assemblyAttributes = pc.GetAssemblyAttributes();
				
				classIndices.Clear();
				stringDict.Clear();
				int i = 0;
				foreach (IClass c in classes) {
					classIndices[new ClassNameTypeCountPair(c)] = i;
					i += 1;
				}
				
				List<ClassNameTypeCountPair> externalTypes = new List<ClassNameTypeCountPair>();
				List<string> stringList = new List<string>();
				CreateExternalTypeList(externalTypes, stringList, classes.Count, classes);
				AddStringsAndExternalTypesFromAttributes(stringList, externalTypes, classes.Count, assemblyAttributes);
				
				writer.Write(classes.Count);
				writer.Write(externalTypes.Count);
				foreach (IClass c in classes) {
					writer.Write(c.FullyQualifiedName);
				}
				foreach (ClassNameTypeCountPair type in externalTypes) {
					writer.Write(type.ClassName);
					writer.Write(type.TypeParameterCount);
				}
				writer.Write(stringList.Count);
				foreach (string text in stringList) {
					writer.Write(text);
				}
				WriteAttributes(assemblyAttributes);
				foreach (IClass c in classes) {
					WriteClass(c);
					// BinaryReader easily reads junk data when the file does not have the
					// expected format, so we put a checking byte after each class.
					writer.Write((byte)64);
				}
			}
			
			bool ReadClasses()
			{
				int classCount = reader.ReadInt32();
				int externalTypeCount = reader.ReadInt32();
				types = new IReturnType[classCount + externalTypeCount];
				DefaultClass[] classes = new DefaultClass[classCount];
				for (int i = 0; i < classes.Length; i++) {
					DefaultClass c = new DefaultClass(pc.AssemblyCompilationUnit, reader.ReadString());
					classes[i] = c;
					types[i] = c.DefaultReturnType;
				}
				for (int i = classCount; i < types.Length; i++) {
					string name = reader.ReadString();
					types[i] = new GetClassReturnType(pc, name, reader.ReadByte());
				}
				stringArray = new string[reader.ReadInt32()];
				for (int i = 0; i < stringArray.Length; i++) {
					stringArray[i] = reader.ReadString();
				}
				ReadAttributes(pc.AssemblyCompilationUnit);
				for (int i = 0; i < classes.Length; i++) {
					ReadClass(classes[i]);
					pc.AddClassToNamespaceList(classes[i]);
					if (reader.ReadByte() != 64) {
						return false;
					}
				}
				return true;
			}
			#endregion
			
			#region Write/Read Class
			IClass currentClass;
			
			void WriteClass(IClass c)
			{
				this.currentClass = c;
				WriteTemplates(c.TypeParameters);
				writer.Write(c.BaseTypes.Count);
				foreach (IReturnType type in c.BaseTypes) {
					WriteType(type);
				}
				writer.Write((int)c.Modifiers);
				if (c is DefaultClass) {
					writer.Write(((DefaultClass)c).CalculatedFlags);
				} else {
					writer.Write((byte)0);
				}
				writer.Write((byte)c.ClassType);
				WriteAttributes(c.Attributes);
				writer.Write(c.InnerClasses.Count);
				foreach (IClass innerClass in c.InnerClasses) {
					writer.Write(innerClass.FullyQualifiedName);
					WriteClass(innerClass);
				}
				this.currentClass = c;
				writer.Write(c.Methods.Count);
				foreach (IMethod method in c.Methods) {
					WriteMethod(method);
				}
				writer.Write(c.Properties.Count);
				foreach (IProperty property in c.Properties) {
					WriteProperty(property);
				}
				writer.Write(c.Events.Count);
				foreach (IEvent evt in c.Events) {
					WriteEvent(evt);
				}
				writer.Write(c.Fields.Count);
				foreach (IField field in c.Fields) {
					WriteField(field);
				}
				this.currentClass = null;
			}
			
			void WriteTemplates(IList<ITypeParameter> list)
			{
				// read code exists twice: in ReadClass and ReadMethod
				writer.Write((byte)list.Count);
				foreach (ITypeParameter typeParameter in list) {
					WriteString(typeParameter.Name);
				}
				foreach (ITypeParameter typeParameter in list) {
					writer.Write(typeParameter.Constraints.Count);
					foreach (IReturnType type in typeParameter.Constraints) {
						WriteType(type);
					}
				}
			}
			
			void ReadClass(DefaultClass c)
			{
				this.currentClass = c;
				int count;
				count = reader.ReadByte();
				for (int i = 0; i < count; i++) {
					c.TypeParameters.Add(new DefaultTypeParameter(c, ReadString(), i));
				}
				if (count > 0) {
					foreach (ITypeParameter typeParameter in c.TypeParameters) {
						count = reader.ReadInt32();
						for (int i = 0; i < count; i++) {
							typeParameter.Constraints.Add(ReadType());
						}
					}
				} else {
					c.TypeParameters = DefaultTypeParameter.EmptyTypeParameterList;
				}
				count = reader.ReadInt32();
				for (int i = 0; i < count; i++) {
					c.BaseTypes.Add(ReadType());
				}
				c.Modifiers = (ModifierEnum)reader.ReadInt32();
				c.CalculatedFlags = reader.ReadByte();
				c.ClassType = (ClassType)reader.ReadByte();
				ReadAttributes(c);
				count = reader.ReadInt32();
				for (int i = 0; i < count; i++) {
					DefaultClass innerClass = new DefaultClass(c.CompilationUnit, c);
					innerClass.FullyQualifiedName = reader.ReadString();
					c.InnerClasses.Add(innerClass);
					ReadClass(innerClass);
				}
				this.currentClass = c;
				count = reader.ReadInt32();
				for (int i = 0; i < count; i++) {
					c.Methods.Add(ReadMethod());
				}
				count = reader.ReadInt32();
				for (int i = 0; i < count; i++) {
					c.Properties.Add(ReadProperty());
				}
				count = reader.ReadInt32();
				for (int i = 0; i < count; i++) {
					c.Events.Add(ReadEvent());
				}
				count = reader.ReadInt32();
				for (int i = 0; i < count; i++) {
					c.Fields.Add(ReadField());
				}
				this.currentClass = null;
			}
			#endregion
			
			#region Write/Read return types / Collect strings
			/// <summary>
			/// Finds all return types used in the class collection and adds the unknown ones
			/// to the externalTypeIndices and externalTypes collections.
			/// </summary>
			void CreateExternalTypeList(List<ClassNameTypeCountPair> externalTypes,
			                            List<string> stringList,
			                            int classCount, ICollection<IClass> classes)
			{
				foreach (IClass c in classes) {
					CreateExternalTypeList(externalTypes, stringList, classCount, c.InnerClasses);
					AddStringsAndExternalTypesFromAttributes(stringList, externalTypes, classCount, c.Attributes);
					foreach (IReturnType returnType in c.BaseTypes) {
						AddExternalType(returnType, externalTypes, classCount);
					}
					foreach (ITypeParameter tp in c.TypeParameters) {
						AddString(stringList, tp.Name);
						foreach (IReturnType returnType in tp.Constraints) {
							AddExternalType(returnType, externalTypes, classCount);
						}
					}
					foreach (IField f in c.Fields) {
						CreateExternalTypeListMember(externalTypes, stringList, classCount, f);
					}
					foreach (IEvent f in c.Events) {
						CreateExternalTypeListMember(externalTypes, stringList, classCount, f);
					}
					foreach (IProperty p in c.Properties) {
						CreateExternalTypeListMember(externalTypes, stringList, classCount, p);
						foreach (IParameter parameter in p.Parameters) {
							AddString(stringList, parameter.Name);
							AddStringsAndExternalTypesFromAttributes(stringList, externalTypes, classCount, parameter.Attributes);
							AddExternalType(parameter.ReturnType, externalTypes, classCount);
						}
					}
					foreach (IMethod m in c.Methods) {
						CreateExternalTypeListMember(externalTypes, stringList, classCount, m);
						foreach (IParameter parameter in m.Parameters) {
							AddString(stringList, parameter.Name);
							AddStringsAndExternalTypesFromAttributes(stringList, externalTypes, classCount, parameter.Attributes);
							AddExternalType(parameter.ReturnType, externalTypes, classCount);
						}
						foreach (ITypeParameter tp in m.TypeParameters) {
							AddString(stringList, tp.Name);
							foreach (IReturnType returnType in tp.Constraints) {
								AddExternalType(returnType, externalTypes, classCount);
							}
						}
					}
				}
			}
			
			void CreateExternalTypeListMember(List<ClassNameTypeCountPair> externalTypes,
			                                  List<string> stringList, int classCount,
			                                  IMember member)
			{
				AddString(stringList, member.Name);
				AddStringsAndExternalTypesFromAttributes(stringList, externalTypes, classCount, member.Attributes);
				foreach (ExplicitInterfaceImplementation eii in member.InterfaceImplementations) {
					AddString(stringList, eii.MemberName);
					AddExternalType(eii.InterfaceReference, externalTypes, classCount);
				}
				AddExternalType(member.ReturnType, externalTypes, classCount);
			}
			
			void AddString(List<string> stringList, string text)
			{
				text = text ?? string.Empty;
				if (!stringDict.ContainsKey(text)) {
					stringDict.Add(text, stringList.Count);
					stringList.Add(text);
				}
			}
			
			void AddExternalType(IReturnType rt, List<ClassNameTypeCountPair> externalTypes, int classCount)
			{
				if (rt.IsDefaultReturnType) {
					ClassNameTypeCountPair pair = new ClassNameTypeCountPair(rt);
					if (!classIndices.ContainsKey(pair)) {
						classIndices.Add(pair, externalTypes.Count + classCount);
						externalTypes.Add(pair);
					}
				} else if (rt.IsGenericReturnType) {
					// ignore
				} else if (rt.IsArrayReturnType) {
					AddExternalType(rt.CastToArrayReturnType().ArrayElementType, externalTypes, classCount);
				} else if (rt.IsConstructedReturnType) {
					AddExternalType(rt.CastToConstructedReturnType().UnboundType, externalTypes, classCount);
					foreach (IReturnType typeArgument in rt.CastToConstructedReturnType().TypeArguments) {
						AddExternalType(typeArgument, externalTypes, classCount);
					}
				} else if (rt.IsDecoratingReturnType<PointerReturnType>()) {
					AddExternalType(rt.CastToDecoratingReturnType<PointerReturnType>().BaseType, externalTypes, classCount);
				} else {
					LoggingService.Warn("Unknown return type: " + rt.ToString());
				}
			}
			
			const int ArrayRTCode         = -1;
			const int ConstructedRTCode   = -2;
			const int TypeGenericRTCode   = -3;
			const int MethodGenericRTCode = -4;
			const int NullRTReferenceCode = -5;
			const int VoidRTCode          = -6;
			const int PointerRTCode       = -7;
			const int DynamicRTCode       = -8;
			
			void WriteType(IReturnType rt)
			{
				if (rt == null) {
					writer.Write(NullRTReferenceCode);
					return;
				}
				if (rt.IsDefaultReturnType) {
					string name = rt.FullyQualifiedName;
					if (name == "System.Void") {
						writer.Write(VoidRTCode);
					} else if (name == "dynamic") {
						writer.Write(DynamicRTCode);
					} else {
						writer.Write(classIndices[new ClassNameTypeCountPair(rt)]);
					}
				} else if (rt.IsGenericReturnType) {
					GenericReturnType grt = rt.CastToGenericReturnType();
					if (grt.TypeParameter.Method != null) {
						writer.Write(MethodGenericRTCode);
					} else {
						writer.Write(TypeGenericRTCode);
					}
					writer.Write(grt.TypeParameter.Index);
				} else if (rt.IsArrayReturnType) {
					writer.Write(ArrayRTCode);
					writer.Write(rt.CastToArrayReturnType().ArrayDimensions);
					WriteType(rt.CastToArrayReturnType().ArrayElementType);
				} else if (rt.IsConstructedReturnType) {
					ConstructedReturnType crt = rt.CastToConstructedReturnType();
					writer.Write(ConstructedRTCode);
					WriteType(crt.UnboundType);
					writer.Write((byte)crt.TypeArguments.Count);
					foreach (IReturnType typeArgument in crt.TypeArguments) {
						WriteType(typeArgument);
					}
				} else if (rt.IsDecoratingReturnType<PointerReturnType>()) {
					writer.Write(PointerRTCode);
					WriteType(rt.CastToDecoratingReturnType<PointerReturnType>().BaseType);
				} else {
					writer.Write(NullRTReferenceCode);
					LoggingService.Warn("Unknown return type: " + rt.ToString());
				}
			}
			
			// outerClass and outerMethod are required for generic return types
			IReturnType ReadType()
			{
				int index = reader.ReadInt32();
				switch (index) {
					case ArrayRTCode:
						int dimensions = reader.ReadInt32();
						return new ArrayReturnType(pc, ReadType(), dimensions);
					case ConstructedRTCode:
						IReturnType baseType = ReadType();
						IReturnType[] typeArguments = new IReturnType[reader.ReadByte()];
						for (int i = 0; i < typeArguments.Length; i++) {
							typeArguments[i] = ReadType();
						}
						return new ConstructedReturnType(baseType, typeArguments);
					case TypeGenericRTCode:
						return new GenericReturnType(currentClass.TypeParameters[reader.ReadInt32()]);
					case MethodGenericRTCode:
						return new GenericReturnType(currentMethod.TypeParameters[reader.ReadInt32()]);
					case NullRTReferenceCode:
						return null;
					case VoidRTCode:
						return new VoidReturnType(pc);
					case PointerRTCode:
						return new PointerReturnType(ReadType());
					case DynamicRTCode:
						return new DynamicReturnType(pc);
					default:
						return types[index];
				}
			}
			#endregion
			
			#region Write/Read class member
			void WriteString(string text)
			{
				text = text ?? string.Empty;
				writer.Write(stringDict[text]);
			}
			
			string ReadString()
			{
				return stringArray[reader.ReadInt32()];
			}
			
			void WriteMember(IMember m)
			{
				WriteString(m.Name);
				writer.Write((int)m.Modifiers);
				WriteAttributes(m.Attributes);
				writer.Write((ushort)m.InterfaceImplementations.Count);
				foreach (ExplicitInterfaceImplementation iee in m.InterfaceImplementations) {
					WriteType(iee.InterfaceReference);
					WriteString(iee.MemberName);
				}
				if (!(m is IMethod)) {
					// method must store ReturnType AFTER Template definitions
					WriteType(m.ReturnType);
				}
			}
			
			void ReadMember(AbstractMember m)
			{
				// name is already read by the method that calls the member constructor
				m.Modifiers = (ModifierEnum)reader.ReadInt32();
				ReadAttributes(m);
				int interfaceImplCount = reader.ReadUInt16();
				for (int i = 0; i < interfaceImplCount; i++) {
					m.InterfaceImplementations.Add(new ExplicitInterfaceImplementation(ReadType(), ReadString()));
				}
				if (!(m is IMethod)) {
					m.ReturnType = ReadType();
				}
			}
			#endregion
			
			#region Write/Read attributes
			void AddStringsAndExternalTypesFromAttributes(List<string> stringList,
			                                              List<ClassNameTypeCountPair> externalTypes, int classCount,
			                                              IList<IAttribute> attributes)
			{
				foreach (IAttribute a in attributes) {
					AddExternalType(a.AttributeType, externalTypes, classCount);
					foreach (object o in a.PositionalArguments) {
						AddStringsAndExternalTypesFromAttributeArgument(stringList, attributes, externalTypes, classCount, o);
					}
					foreach (KeyValuePair<string, object> pair in a.NamedArguments) {
						AddString(stringList, pair.Key);
						AddStringsAndExternalTypesFromAttributeArgument(stringList, attributes, externalTypes, classCount, pair.Value);
					}
				}
			}
			
			void WriteAttributes(IList<IAttribute> attributes)
			{
				writer.Write((ushort)attributes.Count);
				foreach (IAttribute a in attributes) {
					WriteType(a.AttributeType);
					writer.Write((byte)a.AttributeTarget);
					writer.Write((byte)a.PositionalArguments.Count);
					foreach (object o in a.PositionalArguments) {
						WriteAttributeArgument(o);
					}
					writer.Write((byte)a.NamedArguments.Count);
					foreach (KeyValuePair<string, object> pair in a.NamedArguments) {
						WriteString(pair.Key);
						WriteAttributeArgument(pair.Value);
					}
				}
			}
			
			void ReadAttributes(ICompilationUnit cu)
			{
				int count = reader.ReadUInt16();
				ReadAttributes(cu.Attributes, count);
			}
			
			void ReadAttributes(DefaultParameter parameter)
			{
				int count = reader.ReadUInt16();
				if (count > 0) {
					ReadAttributes(parameter.Attributes, count);
				} else {
					parameter.Attributes = DefaultAttribute.EmptyAttributeList;
				}
			}
			
			void ReadAttributes(AbstractEntity decoration)
			{
				int count = reader.ReadUInt16();
				if (count > 0) {
					ReadAttributes(decoration.Attributes, count);
				} else {
					decoration.Attributes = DefaultAttribute.EmptyAttributeList;
				}
			}
			
			void ReadAttributes(IList<IAttribute> attributes, int count)
			{
				for (int i = 0; i < count; i++) {
					IReturnType type = ReadType();
					DefaultAttribute attr = new DefaultAttribute(type, (AttributeTarget)reader.ReadByte());
					int posArgCount = reader.ReadByte();
					for (int j = 0; j < posArgCount; j++) {
						attr.PositionalArguments.Add(ReadAttributeArgument());
					}
					int namedArgCount = reader.ReadByte();
					for (int j = 0; j < namedArgCount; j++) {
						attr.NamedArguments.Add(ReadString(), ReadAttributeArgument());
					}
					attributes.Add(attr);
				}
			}
			#endregion
			
			#region Write/Read attribute arguments
			void AddStringsAndExternalTypesFromAttributeArgument(List<string> stringList, IList<IAttribute> attributes,
			                                                     List<ClassNameTypeCountPair> externalTypes, int classCount,
			                                                     object value)
			{
				if (value is string) {
					AddString(stringList, (string)value);
				} else if (value is IReturnType) {
					AddExternalType((IReturnType)value, externalTypes, classCount);
				}
			}
			
			enum AttributeType : byte
			{
				Null,
				String,
				Type,
				SByte,
				Int16,
				Int32,
				Int64,
				Byte,
				UInt16,
				UInt32,
				UInt64,
				Bool,
				Single,
				Double,
			}
			
			void WriteAttributeArgument(object o)
			{
				if (o == null) {
					writer.Write((byte)AttributeType.Null);
				} else if (o is string) {
					writer.Write((byte)AttributeType.String);
					WriteString((string)o);
				} else if (o is IReturnType) {
					writer.Write((byte)AttributeType.Type);
					WriteType((IReturnType)o);
				} else if (o is Byte) {
					writer.Write((byte)AttributeType.Byte);
					writer.Write((Byte)o);
				} else if (o is Int16) {
					writer.Write((byte)AttributeType.Int16);
					writer.Write((Int16)o);
				} else if (o is Int32) {
					writer.Write((byte)AttributeType.Int32);
					writer.Write((Int32)o);
				} else if (o is Int64) {
					writer.Write((byte)AttributeType.Int64);
					writer.Write((Int64)o);
				} else if (o is SByte) {
					writer.Write((byte)AttributeType.SByte);
					writer.Write((SByte)o);
				} else if (o is UInt16) {
					writer.Write((byte)AttributeType.UInt16);
					writer.Write((UInt16)o);
				} else if (o is UInt32) {
					writer.Write((byte)AttributeType.UInt32);
					writer.Write((UInt32)o);
				} else if (o is UInt64) {
					writer.Write((byte)AttributeType.UInt64);
					writer.Write((UInt64)o);
				} else if (o is bool) {
					writer.Write((byte)AttributeType.Bool);
					writer.Write((bool)o);
				} else if (o is Single) {
					writer.Write((byte)AttributeType.Single);
					writer.Write((Single)o);
				} else if (o is Double) {
					writer.Write((byte)AttributeType.Double);
					writer.Write((Double)o);
				} else {
					writer.Write((byte)AttributeType.Null);
					LoggingService.Warn("Cannot write attribute arguments of type " + o.GetType());
				}
			}
			
			object ReadAttributeArgument()
			{
				byte type = reader.ReadByte();
				switch ((AttributeType)type) {
					case AttributeType.Null:
						return null;
					case AttributeType.String:
						return ReadString();
					case AttributeType.Type:
						return ReadType();
					case AttributeType.SByte:
						return reader.ReadSByte();
					case AttributeType.Int16:
						return reader.ReadInt16();
					case AttributeType.Int32:
						return reader.ReadInt32();
					case AttributeType.Int64:
						return reader.ReadInt64();
					case AttributeType.Byte:
						return reader.ReadByte();
					case AttributeType.UInt16:
						return reader.ReadUInt16();
					case AttributeType.UInt32:
						return reader.ReadUInt32();
					case AttributeType.UInt64:
						return reader.ReadUInt64();
					case AttributeType.Bool:
						return reader.ReadBoolean();
					case AttributeType.Single:
						return reader.ReadSingle();
					case AttributeType.Double:
						return reader.ReadDouble();
					default:
						throw new NotSupportedException("Invalid attribute argument type code " + type);
				}
			}
			#endregion
			
			#region Write/Read parameters
			void WriteParameters(IList<IParameter> parameters)
			{
				writer.Write((ushort)parameters.Count);
				foreach (IParameter p in parameters) {
					WriteString(p.Name);
					WriteType(p.ReturnType);
					writer.Write((byte)p.Modifiers);
					WriteAttributes(p.Attributes);
				}
			}
			
			void ReadParameters(DefaultMethod m)
			{
				int count = reader.ReadUInt16();
				if (count > 0) {
					ReadParameters(m.Parameters, count);
				} else {
					m.Parameters = DefaultParameter.EmptyParameterList;
				}
			}
			
			void ReadParameters(DefaultProperty m)
			{
				int count = reader.ReadUInt16();
				if (count > 0) {
					ReadParameters(m.Parameters, count);
				} else {
					m.Parameters = DefaultParameter.EmptyParameterList;
				}
			}
			
			void ReadParameters(IList<IParameter> parameters, int count)
			{
				for (int i = 0; i < count; i++) {
					string name = ReadString();
					DefaultParameter p = new DefaultParameter(name, ReadType(), DomRegion.Empty);
					p.Modifiers = (ParameterModifiers)reader.ReadByte();
					ReadAttributes(p);
					parameters.Add(p);
				}
			}
			#endregion
			
			#region Write/Read Method
			IMethod currentMethod;
			
			void WriteMethod(IMethod m)
			{
				currentMethod = m;
				WriteMember(m);
				WriteTemplates(m.TypeParameters);
				WriteType(m.ReturnType);
				writer.Write(m.IsExtensionMethod);
				WriteParameters(m.Parameters);
				currentMethod = null;
			}
			
			IMethod ReadMethod()
			{
				DefaultMethod m = new DefaultMethod(currentClass, ReadString());
				currentMethod = m;
				ReadMember(m);
				int count = reader.ReadByte();
				for (int i = 0; i < count; i++) {
					m.TypeParameters.Add(new DefaultTypeParameter(m, ReadString(), i));
				}
				if (count > 0) {
					foreach (ITypeParameter typeParameter in m.TypeParameters) {
						count = reader.ReadInt32();
						for (int i = 0; i < count; i++) {
							typeParameter.Constraints.Add(ReadType());
						}
					}
				} else {
					m.TypeParameters = DefaultTypeParameter.EmptyTypeParameterList;
				}
				m.ReturnType = ReadType();
				m.IsExtensionMethod = reader.ReadBoolean();
				ReadParameters(m);
				currentMethod = null;
				return m;
			}
			#endregion
			
			#region Write/Read Property
			void WriteProperty(IProperty p)
			{
				WriteMember(p);
				DefaultProperty dp = p as DefaultProperty;
				if (dp != null) {
					writer.Write(dp.accessFlags);
				} else {
					writer.Write((byte)0);
				}
				writer.Write((byte)((p.GetterModifiers != ModifierEnum.None ? 1 : 0) + (p.SetterModifiers != ModifierEnum.None ? 2 : 0)));
				if (p.GetterModifiers != ModifierEnum.None) {
					writer.Write((int)p.GetterModifiers);
				}
				if (p.SetterModifiers != ModifierEnum.None) {
					writer.Write((int)p.SetterModifiers);
				}
				WriteParameters(p.Parameters);
			}
			
			IProperty ReadProperty()
			{
				DefaultProperty p = new DefaultProperty(currentClass, ReadString());
				ReadMember(p);
				p.accessFlags = reader.ReadByte();
				byte b = reader.ReadByte();
				if ((b & 1) == 1) {
					p.GetterModifiers = (ModifierEnum)reader.ReadInt32();
				}
				if ((b & 2) == 2) {
					p.SetterModifiers = (ModifierEnum)reader.ReadInt32();
				}
				ReadParameters(p);
				return p;
			}
			#endregion
			
			#region Write/Read Event
			void WriteEvent(IEvent p)
			{
				WriteMember(p);
			}
			
			IEvent ReadEvent()
			{
				DefaultEvent p = new DefaultEvent(currentClass, ReadString());
				ReadMember(p);
				return p;
			}
			#endregion
			
			#region Write/Read Field
			void WriteField(IField p)
			{
				WriteMember(p);
			}
			
			IField ReadField()
			{
				DefaultField p = new DefaultField(currentClass, ReadString());
				ReadMember(p);
				return p;
			}
			#endregion
		}
	}
}
