// Copyright (c) Cristian Civera (cristian@aspitalia.com)
// This code is distributed under the MS-PL (for details please see \doc\MS-PL.txt)

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;
using System.Windows.Media;

namespace Ricciolo.StylesExplorer.MarkupReflection
{
	public class XmlBamlReader : XmlReader, IXmlNamespaceResolver
	{
		BamlBinaryReader reader;
		Dictionary<short, string> assemblyTable = new Dictionary<short, string>();
		Dictionary<short, string> stringTable = new Dictionary<short, string>();
		Dictionary<short, TypeDeclaration> typeTable = new Dictionary<short, TypeDeclaration>();
		Dictionary<short, PropertyDeclaration> propertyTable = new Dictionary<short, PropertyDeclaration>();

		readonly ITypeResolver _resolver;

		BamlRecordType currentType;

		Stack<XmlBamlElement> elements = new Stack<XmlBamlElement>();
		Stack<XmlBamlElement> readingElements = new Stack<XmlBamlElement>();
		NodesCollection nodes = new NodesCollection();
		List<XmlPIMapping> _mappings = new List<XmlPIMapping>();
		XmlBamlNode _currentNode;

		readonly KnownInfo KnownInfo;

		int complexPropertyOpened = 0;

		bool intoAttribute = false;
		bool initialized;
		bool _eof;
		
		#region Context
		Stack<ReaderContext> layer = new Stack<ReaderContext>();
		
		class ReaderContext
		{
			public bool IsDeferred { get; set; }
			public bool IsInStaticResource { get; set; }

			public ReaderContext Previous { get; private set; }
			
			public ReaderContext()
			{
				this.Previous = this;
			}
			
			public ReaderContext(ReaderContext previous)
			{
				this.Previous = previous;
			}
		}
		
		ReaderContext Current {
			get {
				if (!layer.Any())
					layer.Push(new ReaderContext());
				
				return layer.Peek();
			}
		}
		
		int currentKey;
		List<KeyMapping> keys = new List<KeyMapping>();
		
		void LayerPop()
		{
			layer.Pop();
		}
		
		void LayerPush()
		{
			if (layer.Any())
				layer.Push(new ReaderContext(layer.Peek()));
			else
				layer.Push(new ReaderContext());
		}
		#endregion

		int bytesToSkip;

		static readonly MethodInfo staticConvertCustomBinaryToObjectMethod = Type.GetType("System.Windows.Markup.XamlPathDataSerializer,PresentationFramework, Version=3.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35").GetMethod("StaticConvertCustomBinaryToObject", BindingFlags.Static | BindingFlags.Public);
		readonly TypeDeclaration XamlTypeDeclaration;
		readonly XmlNameTable _nameTable = new NameTable();
		IDictionary<string, string> _rootNamespaces;
		
		public const string XWPFNamespace = "http://schemas.microsoft.com/winfx/2006/xaml";
		public const string DefaultWPFNamespace = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";

		public XmlBamlReader(Stream stream, ITypeResolver resolver)
		{
			if (stream == null)
				throw new ArgumentNullException("stream");
			if (resolver == null)
				throw new ArgumentNullException("resolver");

			_resolver = resolver;
			reader = new BamlBinaryReader(stream);

			XamlTypeDeclaration = new TypeDeclaration(this.Resolver, "", "System.Windows.Markup", 0);
			KnownInfo = new KnownInfo(resolver);
		}

		///<summary>
		///When overridden in a derived class, gets the value of the attribute with the specified <see cref="P:System.Xml.XmlReader.Name"></see>.
		///</summary>
		///
		///<returns>
		///The value of the specified attribute. If the attribute is not found, null is returned.
		///</returns>
		///
		///<param name="name">The qualified name of the attribute. </param>
		public override string GetAttribute(string name)
		{
			throw new NotImplementedException();
		}

		///<summary>
		///When overridden in a derived class, gets the value of the attribute with the specified <see cref="P:System.Xml.XmlReader.LocalName"></see> and <see cref="P:System.Xml.XmlReader.NamespaceURI"></see>.
		///</summary>
		///
		///<returns>
		///The value of the specified attribute. If the attribute is not found, null is returned. This method does not move the reader.
		///</returns>
		///
		///<param name="namespaceURI">The namespace URI of the attribute. </param>
		///<param name="name">The local name of the attribute. </param>
		public override string GetAttribute(string name, string namespaceURI)
		{
			throw new NotImplementedException();
		}

		///<summary>
		///When overridden in a derived class, gets the value of the attribute with the specified index.
		///</summary>
		///
		///<returns>
		///The value of the specified attribute. This method does not move the reader.
		///</returns>
		///
		///<param name="i">The index of the attribute. The index is zero-based. (The first attribute has index 0.) </param>
		public override string GetAttribute(int i)
		{
			throw new NotImplementedException();
		}

		///<summary>
		///When overridden in a derived class, moves to the attribute with the specified <see cref="P:System.Xml.XmlReader.Name"></see>.
		///</summary>
		///
		///<returns>
		///true if the attribute is found; otherwise, false. If false, the reader's position does not change.
		///</returns>
		///
		///<param name="name">The qualified name of the attribute. </param>
		public override bool MoveToAttribute(string name)
		{
			throw new NotImplementedException();
		}

		///<summary>
		///When overridden in a derived class, moves to the attribute with the specified <see cref="P:System.Xml.XmlReader.LocalName"></see> and <see cref="P:System.Xml.XmlReader.NamespaceURI"></see>.
		///</summary>
		///
		///<returns>
		///true if the attribute is found; otherwise, false. If false, the reader's position does not change.
		///</returns>
		///
		///<param name="name">The local name of the attribute. </param>
		///<param name="ns">The namespace URI of the attribute. </param>
		public override bool MoveToAttribute(string name, string ns)
		{
			throw new NotImplementedException();
		}

		///<summary>
		///When overridden in a derived class, moves to the first attribute.
		///</summary>
		///
		///<returns>
		///true if an attribute exists (the reader moves to the first attribute); otherwise, false (the position of the reader does not change).
		///</returns>
		///
		public override bool MoveToFirstAttribute()
		{
			intoAttribute = false;
			if (nodes.Count > 0 && (nodes.Peek() is XmlBamlProperty || nodes.Peek() is XmlBamlSimpleProperty))
			{
				_currentNode = nodes.Dequeue();
				return true;
			}
			return false;
		}

		///<summary>
		///When overridden in a derived class, moves to the next attribute.
		///</summary>
		///
		///<returns>
		///true if there is a next attribute; false if there are no more attributes.
		///</returns>
		///
		public override bool MoveToNextAttribute()
		{
			intoAttribute = false;
			if (nodes.Count > 0 &&  (nodes.Peek() is XmlBamlProperty || nodes.Peek() is XmlBamlSimpleProperty))
			{
				_currentNode = nodes.Dequeue();
				return true;
			}
			return false;
		}

		///<summary>
		///When overridden in a derived class, moves to the element that contains the current attribute node.
		///</summary>
		///
		///<returns>
		///true if the reader is positioned on an attribute (the reader moves to the element that owns the attribute); false if the reader is not positioned on an attribute (the position of the reader does not change).
		///</returns>
		///
		public override bool MoveToElement()
		{
			while (nodes.Peek() is XmlBamlProperty || nodes.Peek() is XmlBamlSimpleProperty)
			{
				nodes.Dequeue();
			}

			return true;
		}

		///<summary>
		///When overridden in a derived class, parses the attribute value into one or more Text, EntityReference, or EndEntity nodes.
		///</summary>
		///
		///<returns>
		///true if there are nodes to return.false if the reader is not positioned on an attribute node when the initial call is made or if all the attribute values have been read.An empty attribute, such as, misc="", returns true with a single node with a value of String.Empty.
		///</returns>
		///
		public override bool ReadAttributeValue()
		{
			if (!intoAttribute)
			{
				intoAttribute = true;
				return true;
			}
			return false;
		}

		///<summary>
		///When overridden in a derived class, reads the next node from the stream.
		///</summary>
		///
		///<returns>
		///true if the next node was read successfully; false if there are no more nodes to read.
		///</returns>
		///
		///<exception cref="T:System.Xml.XmlException">An error occurred while parsing the XML. </exception>
		public override bool Read()
		{
			return ReadInternal();
		}

		bool ReadInternal()
		{
			EnsureInit();

			if (SetNextNode())
				return true;

			try
			{
				do
				{
					ReadRecordType();
					if (currentType == BamlRecordType.DocumentEnd)
						break;

					long position = reader.BaseStream.Position;

					ComputeBytesToSkip();
					ProcessNext();

					if (bytesToSkip > 0)
						// jump to the end of the record
						reader.BaseStream.Position = position + bytesToSkip;
				}
				//while (currentType != BamlRecordType.DocumentEnd);
				while (nodes.Count == 0 || (currentType != BamlRecordType.ElementEnd) || complexPropertyOpened > 0);

				if (!SetNextNode()) {
					_eof = true;
					return false;
				}
				return true;
			}
			catch (EndOfStreamException)
			{
				_eof = true;
				return false;
			}
		}

		void ReadRecordType()
		{
			byte type = reader.ReadByte();
			if (type < 0)
				currentType = BamlRecordType.DocumentEnd;
			else
				currentType = (BamlRecordType)type;
			
			if (currentType.ToString().EndsWith("End"))
				Debug.Unindent();
			Debug.WriteLine(string.Format("{0} (0x{0:x})", currentType));
			if (currentType.ToString().EndsWith("Start"))
				Debug.Indent();
		}

		bool SetNextNode()
		{
			while (nodes.Count > 0)
			{
				_currentNode = nodes.Dequeue();

				if ((_currentNode is XmlBamlProperty)) continue;
				if ((_currentNode is XmlBamlSimpleProperty)) continue;

				if (this.NodeType == XmlNodeType.EndElement)
				{
					if (readingElements.Count == 1)
						_rootNamespaces = ((IXmlNamespaceResolver)this).GetNamespacesInScope(XmlNamespaceScope.All);
					readingElements.Pop();
				}
				else if (this.NodeType == XmlNodeType.Element)
					readingElements.Push((XmlBamlElement)_currentNode);

				return true;
			}

			return false;
		}

		void ProcessNext()
		{
			switch (currentType)
			{
				case BamlRecordType.DocumentStart:
					reader.ReadBytes(6);
					break;
				case BamlRecordType.DocumentEnd:
					break;
				case BamlRecordType.ElementStart:
					this.ReadElementStart();
					break;
				case BamlRecordType.ElementEnd:
					this.ReadElementEnd();
					break;
				case BamlRecordType.AssemblyInfo:
					this.ReadAssemblyInfo();
					break;
				case BamlRecordType.StringInfo:
					this.ReadStringInfo();
					break;
				case BamlRecordType.LineNumberAndPosition:
					reader.ReadInt32();
					reader.ReadInt32();
					break;
				case BamlRecordType.LinePosition:
					reader.ReadInt32();
					break;
				case BamlRecordType.XmlnsProperty:
					this.ReadXmlnsProperty();
					break;
				case BamlRecordType.ConnectionId:
					this.ReadConnectionId();
					break;
				case BamlRecordType.DeferableContentStart:
					Current.IsDeferred = true;
					keys = new List<KeyMapping>();
					currentKey = 0;
					reader.ReadInt32();
					break;
				case BamlRecordType.DefAttribute:
					this.ReadDefAttribute();
					break;
				case BamlRecordType.DefAttributeKeyType:
					this.ReadDefAttributeKeyType();
					break;
				case BamlRecordType.DefAttributeKeyString:
					this.ReadDefAttributeKeyString();
					break;
				case BamlRecordType.AttributeInfo:
					this.ReadAttributeInfo();
					break;
				case BamlRecordType.PropertyListStart:
					this.ReadPropertyListStart();
					break;
				case BamlRecordType.PropertyListEnd:
					this.ReadPropertyListEnd();
					break;
				case BamlRecordType.Property:
					this.ReadProperty();
					break;
				case BamlRecordType.PropertyWithConverter:
					this.ReadPropertyWithConverter();
					break;
				case BamlRecordType.PropertyWithExtension:
					this.ReadPropertyWithExtension();
					break;
				case BamlRecordType.PropertyDictionaryStart:
					this.ReadPropertyDictionaryStart();
					break;
				case BamlRecordType.PropertyCustom:
					this.ReadPropertyCustom();
					break;
				case BamlRecordType.PropertyDictionaryEnd:
					this.ReadPropertyDictionaryEnd();
					break;
				case BamlRecordType.PropertyComplexStart:
					this.ReadPropertyComplexStart();
					break;
				case BamlRecordType.PropertyComplexEnd:
					this.ReadPropertyComplexEnd();
					break;
				case BamlRecordType.PIMapping:
					this.ReadPIMapping();
					break;
				case BamlRecordType.TypeInfo:
					this.ReadTypeInfo();
					break;
				case BamlRecordType.ContentProperty:
					this.ReadContentProperty();
					break;
				case BamlRecordType.ConstructorParametersStart:
					ReadConstructorParametersStart();
					break;
				case BamlRecordType.ConstructorParametersEnd:
					ReadConstructorParametersEnd();
					break;
				case BamlRecordType.ConstructorParameterType:
					this.ReadConstructorParameterType();
					break;
				case BamlRecordType.Text:
					this.ReadText();
					break;
				case BamlRecordType.TextWithConverter:
					this.ReadTextWithConverter();
					break;
				case BamlRecordType.TextWithId:
					this.ReadTextWithId();
					break;
				case BamlRecordType.PropertyWithStaticResourceId:
					this.ReadPropertyWithStaticResourceIdentifier();
					break;
				case BamlRecordType.OptimizedStaticResource:
					this.ReadOptimizedStaticResource();
					break;
				case BamlRecordType.KeyElementStart:
					this.ReadKeyElementStart();
					break;
				case BamlRecordType.KeyElementEnd:
					this.ReadKeyElementEnd();
					break;
				case BamlRecordType.PropertyTypeReference:
					this.ReadPropertyTypeReference();
					break;
				case BamlRecordType.StaticResourceStart:
					ReadStaticResourceStart();
					break;
				case BamlRecordType.StaticResourceEnd:
					ReadStaticResourceEnd();
					break;
				case BamlRecordType.StaticResourceId:
					ReadStaticResourceId();
					break;
				case BamlRecordType.PresentationOptionsAttribute:
					this.ReadPresentationOptionsAttribute();
					break;
				default:
					throw new NotImplementedException("UnsupportedNode: " + currentType);
			}
		}
		
		void ReadConnectionId()
		{
			int id = reader.ReadInt32();
			nodes.Enqueue(new XmlBamlSimpleProperty(XWPFNamespace, "ConnectionId", id.ToString()));
		}
		
		void ReadTextWithId()
		{
			short textId = reader.ReadInt16();
			string text = stringTable[textId];
			nodes.Enqueue(new XmlBamlText(text));
		}

		void ComputeBytesToSkip()
		{
			bytesToSkip = 0;
			switch (currentType)
			{
				case BamlRecordType.PropertyWithConverter:
				case BamlRecordType.DefAttributeKeyString:
				case BamlRecordType.PresentationOptionsAttribute:
				case BamlRecordType.Property:
				case BamlRecordType.PropertyCustom:
				case BamlRecordType.Text:
				case BamlRecordType.TextWithId:
				case BamlRecordType.TextWithConverter:
				case BamlRecordType.XmlnsProperty:
				case BamlRecordType.DefAttribute:
				case BamlRecordType.PIMapping:
				case BamlRecordType.AssemblyInfo:
				case BamlRecordType.TypeInfo:
				case BamlRecordType.AttributeInfo:
				case BamlRecordType.StringInfo:
					bytesToSkip = reader.ReadCompressedInt32();
					break;
			}
		}

		void EnsureInit()
		{
			if (!initialized)
			{
				int startChars = reader.ReadInt32();
				String type = new String(new BinaryReader(this.reader.BaseStream, Encoding.Unicode).ReadChars(startChars >> 1));
				if (type != "MSBAML")
					throw new NotSupportedException("Not a MS BAML");

				int r = reader.ReadInt32();
				int s = reader.ReadInt32();
				int t = reader.ReadInt32();
				if (((r != 0x600000) || (s != 0x600000)) || (t != 0x600000))
					throw new NotSupportedException();

				initialized = true;
			}
		}

		///<summary>
		///When overridden in a derived class, changes the <see cref="P:System.Xml.XmlReader.ReadState"></see> to Closed.
		///</summary>
		///
		public override void Close()
		{
			//if (reader != null)
			//    reader.Close();
			reader = null;
		}

		///<summary>
		///When overridden in a derived class, resolves a namespace prefix in the current element's scope.
		///</summary>
		///
		///<returns>
		///The namespace URI to which the prefix maps or null if no matching prefix is found.
		///</returns>
		///
		///<param name="prefix">The prefix whose namespace URI you want to resolve. To match the default namespace, pass an empty string. </param>
		public override string LookupNamespace(string prefix)
		{
			if (readingElements.Count == 0) return null;

			XmlNamespaceCollection namespaces = readingElements.Peek().Namespaces;

			for (int x = 0; x < namespaces.Count; x++)
			{
				if (String.CompareOrdinal(namespaces[x].Prefix, prefix) == 0)
					return namespaces[x].Namespace;
			}

			return null;
		}

		///<summary>
		///When overridden in a derived class, resolves the entity reference for EntityReference nodes.
		///</summary>
		///
		///<exception cref="T:System.InvalidOperationException">The reader is not positioned on an EntityReference node; this implementation of the reader cannot resolve entities (<see cref="P:System.Xml.XmlReader.CanResolveEntity"></see> returns false). </exception>
		public override void ResolveEntity()
		{
			throw new NotImplementedException();
		}

		///<summary>
		///When overridden in a derived class, gets the type of the current node.
		///</summary>
		///
		///<returns>
		///One of the <see cref="T:System.Xml.XmlNodeType"></see> values representing the type of the current node.
		///</returns>
		///
		public override XmlNodeType NodeType
		{
			get
			{
				if (intoAttribute) return XmlNodeType.Text;

				return this.CurrentNode.NodeType;
			}
		}

		///<summary>
		///When overridden in a derived class, gets the local name of the current node.
		///</summary>
		///
		///<returns>
		///The name of the current node with the prefix removed. For example, LocalName is book for the element &lt;bk:book&gt;.For node types that do not have a name (like Text, Comment, and so on), this property returns String.Empty.
		///</returns>
		///
		public override string LocalName
		{
			get
			{
				if (intoAttribute) return string.Empty;

				String localName = string.Empty;

				XmlBamlNode node = this.CurrentNode;
				if (node is XmlBamlSimpleProperty) {
					var simpleNode = (XmlBamlSimpleProperty)node;
					localName = simpleNode.LocalName;
				} else if (node is XmlBamlProperty)
				{
					PropertyDeclaration pd = ((XmlBamlProperty)node).PropertyDeclaration;
					localName = FormatPropertyDeclaration(pd, false, true, true);
				}
				else if (node is XmlBamlPropertyElement)
				{
					XmlBamlPropertyElement property = (XmlBamlPropertyElement)node;
					string typeName = property.TypeDeclaration.Name;
					
					if (property.Parent.TypeDeclaration.Type.IsSubclassOf(property.PropertyDeclaration.DeclaringType.Type))
						typeName = property.Parent.TypeDeclaration.Name;
					
					localName = String.Format("{0}.{1}", typeName, property.PropertyDeclaration.Name);
				}
				else if (node is XmlBamlElement)
					localName = ((XmlBamlElement)node).TypeDeclaration.Name;

				localName = this.NameTable.Add(localName);

				return localName;
			}
		}

		PropertyDeclaration GetPropertyDeclaration(short identifier)
		{
			PropertyDeclaration declaration;
			if (identifier >= 0)
			{
				declaration = this.propertyTable[identifier];
			}
			else
			{
				declaration = KnownInfo.KnownPropertyTable[-identifier];
			}
			if (declaration == null)
			{
				throw new NotSupportedException();
			}
			return declaration;
		}

		object GetResourceName(short identifier)
		{
			if (identifier >= 0) {
				PropertyDeclaration declaration = this.propertyTable[identifier];
				return declaration;
			} else {
				identifier = (short)-identifier;
				bool isNotKey = (identifier > 0xe8);
				if (isNotKey)
					identifier = (short)(identifier - 0xe8);
				ResourceName resource;
				if (!KnownInfo.KnownResourceTable.TryGetValue(identifier, out resource))
					throw new ArgumentException("Cannot find resource name " + identifier);
				if (!isNotKey)
					return new ResourceName(resource.Name + "Key");
				return resource;
			}
		}

		void ReadPropertyDictionaryStart()
		{
			short identifier = reader.ReadInt16();

			PropertyDeclaration pd = this.GetPropertyDeclaration(identifier);
			XmlBamlElement element = elements.Peek();
			XmlBamlPropertyElement property = new XmlBamlPropertyElement(element, PropertyType.Dictionary, pd);
			elements.Push(property);
			nodes.Enqueue(property);
		}

		void ReadPropertyDictionaryEnd()
		{
			CloseElement();
		}

		void ReadPropertyCustom()
		{
			short identifier = reader.ReadInt16();
			short serializerTypeId = reader.ReadInt16();
			bool isValueTypeId = (serializerTypeId & 0x4000) == 0x4000;
			if (isValueTypeId)
				serializerTypeId = (short)(serializerTypeId & ~0x4000);

			PropertyDeclaration pd = this.GetPropertyDeclaration(identifier);
			string value;
			switch (serializerTypeId)
			{
				case 0x2e8:
					value = new BrushConverter().ConvertToString(SolidColorBrush.DeserializeFrom(reader));
					break;
				case 0x2e9:
					value = new Int32CollectionConverter().ConvertToString(DeserializeInt32CollectionFrom(reader));
					break;
				case 0x89:

					short typeIdentifier = reader.ReadInt16();
					if (isValueTypeId)
					{
						TypeDeclaration typeDeclaration = this.GetTypeDeclaration(typeIdentifier);
						string name = reader.ReadString();
						value = FormatPropertyDeclaration(new PropertyDeclaration(name, typeDeclaration), true, false, true);
					}
					else
						value = FormatPropertyDeclaration(this.GetPropertyDeclaration(typeIdentifier), true, false, true);
					break;

				case 0x2ea:
					value = ((IFormattable)staticConvertCustomBinaryToObjectMethod.Invoke(null, new object[] { this.reader })).ToString("G", CultureInfo.InvariantCulture);
					break;
				case 0x2eb:
				case 0x2f0:
					value = Deserialize3DPoints();
					break;
				case 0x2ec:
					value = DeserializePoints();
					break;
				case 0xc3:
					// Enum
					uint num = reader.ReadUInt32();
					value = num.ToString();
					break;
				case 0x2e:
					int b = reader.ReadByte();
					value = (b == 1) ? Boolean.TrueString : Boolean.FalseString;
					break;
				default:
					return;
			}

			XmlBamlProperty property = new XmlBamlProperty(elements.Peek(), PropertyType.Value, pd);
			property.Value = value;

			nodes.Enqueue(property);
		}

		string DeserializePoints()
		{
			using (StringWriter writer = new StringWriter())
			{
				int num10 = reader.ReadInt32();
				for (int k = 0; k < num10; k++)
				{
					if (k != 0)
						writer.Write(" ");
					for (int m = 0; m < 2; m++)
					{
						if (m != 0)
							writer.Write(",");
						writer.Write(reader.ReadCompressedDouble().ToString());
					}
				}
				return writer.ToString();
			}
		}

		String Deserialize3DPoints()
		{
			using (StringWriter writer = new StringWriter())
			{
				int num14 = reader.ReadInt32();
				for (int i = 0; i < num14; i++)
				{
					if (i != 0)
					{
						writer.Write(" ");
					}
					for (int j = 0; j < 3; j++)
					{
						if (j != 0)
						{
							writer.Write(",");
						}
						writer.Write(reader.ReadCompressedDouble().ToString());
					}
				}
				return writer.ToString();
			}
		}

		static Int32Collection DeserializeInt32CollectionFrom(BinaryReader reader)
		{
			IntegerCollectionType type = (IntegerCollectionType)reader.ReadByte();
			int capacity = reader.ReadInt32();
			if (capacity < 0)
				throw new ArgumentException();

			Int32Collection ints = new Int32Collection(capacity);
			switch (type) {
				case IntegerCollectionType.Byte:
					for (int i = 0; i < capacity; i++)
						ints.Add(reader.ReadByte());
					return ints;
				case IntegerCollectionType.UShort:
					for (int j = 0; j < capacity; j++)
						ints.Add(reader.ReadUInt16());
					return ints;
				case IntegerCollectionType.Integer:
					for (int k = 0; k < capacity; k++)
						ints.Add(reader.ReadInt32());
					return ints;
				case IntegerCollectionType.Consecutive:
					int start = reader.ReadInt32();
					for (int m = start; m < capacity + start; m++)
						ints.Add(m);
					return ints;
			}
			throw new ArgumentException();
		}

		void ReadPropertyWithExtension()
		{
			short identifier = reader.ReadInt16();
			short x = reader.ReadInt16();
			short valueIdentifier = reader.ReadInt16();
			bool isValueType = (x & 0x4000) == 0x4000;
			bool isStaticType = (x & 0x2000) == 0x2000;
			x = (short)(x & 0xfff);

			PropertyDeclaration pd = this.GetPropertyDeclaration(identifier);
			short extensionIdentifier = (short)-(x & 0xfff);
			string value = String.Empty;

			switch (x) {
				case 0x25a:
					// StaticExtension
					object resource = this.GetResourceName(valueIdentifier);
					if (resource is ResourceName)
						value = this.GetStaticExtension(((ResourceName)resource).Name);
					else if (resource is PropertyDeclaration)
						value = this.GetStaticExtension(FormatPropertyDeclaration(((PropertyDeclaration)resource), true, false, false));
					break;
				case 0x25b: // StaticResource
				case 0xbd: // DynamicResource
					if (isValueType)
					{
						value = this.GetTypeExtension(valueIdentifier);
					}
					else if (isStaticType)
					{
						TypeDeclaration extensionDeclaration = this.GetTypeDeclaration(extensionIdentifier);
						value = GetExtension(extensionDeclaration, GetStaticExtension(GetResourceName(valueIdentifier).ToString()));
					}
					else
					{
						TypeDeclaration extensionDeclaration = this.GetTypeDeclaration(extensionIdentifier);
						value = GetExtension(extensionDeclaration, (string)this.stringTable[valueIdentifier]);
					}
					break;

				case 0x27a:
					// TemplateBinding
					PropertyDeclaration pdValue = this.GetPropertyDeclaration(valueIdentifier);
					value = GetTemplateBindingExtension(pdValue);
					break;
				default:
					throw new NotSupportedException("Unknown property with extension");
			}

			XmlBamlProperty property = new XmlBamlProperty(elements.Peek(), PropertyType.Value, pd);
			property.Value = value;

			nodes.Enqueue(property);
		}

		void ReadProperty()
		{
			short identifier = reader.ReadInt16();
			string text = reader.ReadString();

			EnqueueProperty(identifier, text);
		}

		void ReadPropertyWithConverter()
		{
			short identifier = reader.ReadInt16();
			string text = reader.ReadString();
			reader.ReadInt16();

			EnqueueProperty(identifier, text);
		}
		
		bool HaveSeenNestedElement()
		{
			XmlBamlElement element = elements.Peek();
			int elementIndex = nodes.IndexOf(element);
			for (int i = elementIndex + 1; i < nodes.Count; i++)
			{
				if (nodes[i] is XmlBamlEndElement)
					return true;
			}
			return false;
		}
		
		void EnqueueProperty(short identifier, string text)
		{
			PropertyDeclaration pd = this.GetPropertyDeclaration(identifier);
			XmlBamlElement element = FindXmlBamlElement();
			// if we've already read a nested element for the current element, this property must be a nested element as well
			if (HaveSeenNestedElement())
			{
				XmlBamlPropertyElement property = new XmlBamlPropertyElement(element, PropertyType.Complex, pd);
				
				nodes.Enqueue(property);
				nodes.Enqueue(new XmlBamlText(text));
				nodes.Enqueue(new XmlBamlEndElement(property));
			}
			else
			{
				XmlBamlProperty property = new XmlBamlProperty(element, PropertyType.Value, pd);
				property.Value = text;
				
				nodes.Enqueue(property);
			}
		}

		void ReadAttributeInfo()
		{
			short key = reader.ReadInt16();
			short identifier = reader.ReadInt16();
			reader.ReadByte();
			string name = reader.ReadString();
			TypeDeclaration declaringType = this.GetTypeDeclaration(identifier);
			PropertyDeclaration property = new PropertyDeclaration(name, declaringType);
			this.propertyTable.Add(key, property);
		}

		void ReadDefAttributeKeyType()
		{
			short typeIdentifier = reader.ReadInt16();
			reader.ReadByte();
			int position = reader.ReadInt32();
			bool shared = reader.ReadBoolean();
			bool sharedSet = reader.ReadBoolean();
			
			string extension = GetTypeExtension(typeIdentifier);
			
			keys.Add(new KeyMapping(extension) { Shared = shared, SharedSet = sharedSet, Position = position });
		}

		void ReadDefAttribute()
		{
			string text = reader.ReadString();
			short identifier = reader.ReadInt16();

			PropertyDeclaration pd;
			switch (identifier)
			{
				case -2:
					pd = new PropertyDeclaration("Uid", XamlTypeDeclaration);
					break;
				case -1:
					pd = new PropertyDeclaration("Name", XamlTypeDeclaration);
					break;
				default:
					string recordName = this.stringTable[identifier];
					if (recordName != "Key") throw new NotSupportedException(recordName);
					pd = new PropertyDeclaration(recordName, XamlTypeDeclaration);
					if (keys == null)
						keys = new List<KeyMapping>();
					keys.Add(new KeyMapping(text) { Position = -1 });
					break;
			}

			XmlBamlProperty property = new XmlBamlProperty(elements.Peek(), PropertyType.Key, pd);
			property.Value = text;

			nodes.Enqueue(property);
		}

		void ReadDefAttributeKeyString()
		{
			short stringId = reader.ReadInt16();
			int position = reader.ReadInt32();
			bool shared = reader.ReadBoolean();
			bool sharedSet = reader.ReadBoolean();
			
			string text = this.stringTable[stringId];
			Debug.Print("KeyString: " + text);
			if (text == null)
				throw new NotSupportedException();

			keys.Add(new KeyMapping(text) { Position = position });
		}

		void ReadXmlnsProperty()
		{
			string prefix = reader.ReadString();
			string @namespace = reader.ReadString();
			string[] textArray = new string[(uint)reader.ReadInt16()];
			for (int i = 0; i < textArray.Length; i++)
			{
				textArray[i] = this.assemblyTable[reader.ReadInt16()];
			}

			XmlNamespaceCollection namespaces = elements.Peek().Namespaces;
			// Mapping locale, ci aggiunto l'assembly
			if (@namespace.StartsWith("clr-namespace:") && @namespace.IndexOf("assembly=") < 0)
			{
				XmlPIMapping mappingToChange = null;
				foreach (XmlPIMapping mapping in this.Mappings)
				{
					if (String.CompareOrdinal(mapping.XmlNamespace, @namespace) == 0)
					{
						mappingToChange = mapping;
						break;
					}
				}
				if (mappingToChange == null)
					throw new InvalidOperationException("Cannot find mapping");

				@namespace = String.Format("{0};assembly={1}", @namespace, mappingToChange.Assembly.Replace(" ", ""));
				mappingToChange.XmlNamespace = @namespace;
			}
			namespaces.Add(new XmlNamespace(prefix, @namespace));
		}

		void ReadElementEnd()
		{
			CloseElement();
			if (Current.IsDeferred)
				keys = null;
			LayerPop();
		}

		void ReadPropertyComplexStart()
		{
			short identifier = reader.ReadInt16();

			PropertyDeclaration pd = this.GetPropertyDeclaration(identifier);
			XmlBamlElement element = FindXmlBamlElement();

			XmlBamlPropertyElement property = new XmlBamlPropertyElement(element, PropertyType.Complex, pd);
			elements.Push(property);
			nodes.Enqueue(property);
			complexPropertyOpened++;
		}

		XmlBamlElement FindXmlBamlElement()
		{
			return elements.Peek();

			//XmlBamlElement element;
			//int x = nodes.Count - 1;
			//do
			//{
			//    element = nodes[x] as XmlBamlElement;
			//    x--;
			//} while (element == null);
			//return element;
		}

		void ReadPropertyListStart()
		{
			short identifier = reader.ReadInt16();

			PropertyDeclaration pd = this.GetPropertyDeclaration(identifier);
			XmlBamlElement element = FindXmlBamlElement();
			XmlBamlPropertyElement property = new XmlBamlPropertyElement(element, PropertyType.List, pd);
			elements.Push(property);
			nodes.Enqueue(property);
		}

		void ReadPropertyListEnd()
		{
			CloseElement();
		}

		void ReadPropertyComplexEnd()
		{
			XmlBamlPropertyElement propertyElement = (XmlBamlPropertyElement) elements.Peek();

			CloseElement();

			complexPropertyOpened--;
			// this property could be a markup extension
			// try to convert it
			int elementIndex = nodes.IndexOf(propertyElement.Parent);
			int start = nodes.IndexOf(propertyElement) + 1;
			IEnumerator<XmlBamlNode> enumerator = nodes.GetEnumerator();
			
			// move enumerator to the start of this property value
			// note whether there are any child elements before this one
			bool anyChildElement = false;
			for (int i = 0; i < start && enumerator.MoveNext(); i++)
			{
				if (i > elementIndex && i < start - 1 && (enumerator.Current is XmlBamlEndElement))
					anyChildElement = true;
			}

			if (!anyChildElement && IsExtension(enumerator) && start < nodes.Count - 1) {
				start--;
				nodes.RemoveAt(start);
				nodes.RemoveLast();

				StringBuilder sb = new StringBuilder();
				FormatElementExtension((XmlBamlElement) nodes[start], sb);

				XmlBamlProperty property =
					new XmlBamlProperty(elements.Peek(), PropertyType.Complex, propertyElement.PropertyDeclaration);
				property.Value = sb.ToString();
				nodes.Add(property);
			}
		}

		void FormatElementExtension(XmlBamlElement element, StringBuilder sb)
		{
			sb.Append("{");
			sb.Append(FormatTypeDeclaration(element.TypeDeclaration));

			int start = nodes.IndexOf(element);
			nodes.RemoveAt(start);

			string sep = " ";
			while (nodes.Count > start)
			{
				XmlBamlNode node = nodes[start];

				if (node is XmlBamlEndElement)
				{
					sb.Append("}");
					nodes.RemoveAt(start);
					break;
				}
				else if (node is XmlBamlPropertyElement)
				{
					nodes.RemoveAt(start);

					sb.Append(sep);
					XmlBamlPropertyElement property = (XmlBamlPropertyElement)node;
					sb.Append(property.PropertyDeclaration.Name);
					sb.Append("=");

					node = nodes[start];
					nodes.RemoveLast();
					FormatElementExtension((XmlBamlElement)node, sb);
				}
				else if (node is XmlBamlElement)
				{
					sb.Append(sep);
					FormatElementExtension((XmlBamlElement)node, sb);
				}
				else if (node is XmlBamlProperty)
				{
					nodes.RemoveAt(start);

					sb.Append(sep);
					XmlBamlProperty property = (XmlBamlProperty)node;
					sb.Append(property.PropertyDeclaration.Name);
					sb.Append("=");
					sb.Append(property.Value);
				}
				else if (node is XmlBamlText)
				{
					nodes.RemoveAt(start);

					sb.Append(sep);
					sb.Append(((XmlBamlText)node).Text);
				}
				sep = ", ";
			}
		}

		bool IsExtension(IEnumerator<XmlBamlNode> enumerator)
		{
			while (enumerator.MoveNext()) {
				var node = enumerator.Current;
				if (node.NodeType == XmlNodeType.Element && !((XmlBamlElement)node).TypeDeclaration.IsExtension)
					return false;
			}

			return true;
		}

		void CloseElement()
		{
			var e = elements.Pop();
			if (!e.IsImplicit)
				nodes.Enqueue(new XmlBamlEndElement(e));
		}

		void ReadElementStart()
		{
			LayerPush();
			short identifier = reader.ReadInt16();
			sbyte flags = reader.ReadSByte();
			if (flags < 0 || flags > 3)
				throw new NotImplementedException();
			Debug.Print("ElementFlags: " + flags);
			
			TypeDeclaration declaration = GetTypeDeclaration(identifier);

			XmlBamlElement element;
			XmlBamlElement parentElement = null;
			if (elements.Count > 0)
			{
				parentElement = elements.Peek();
				element = new XmlBamlElement(parentElement);
				element.Position = this.reader.BaseStream.Position;

				// Porto l'inizio del padre all'inizio del primo figlio
				if (parentElement.Position == 0 && complexPropertyOpened == 0)
					parentElement.Position = element.Position;
			}
			else
				element = new XmlBamlElement();
			
			// the type is defined in the local assembly, i.e., the main assembly
			// and this is the root element
			TypeDeclaration oldDeclaration = null;
			if (_resolver.IsLocalAssembly(declaration.Assembly) && parentElement == null) {
				oldDeclaration = declaration;
				declaration = GetKnownTypeDeclarationByName(declaration.Type.BaseType.AssemblyQualifiedName);
			}
			element.TypeDeclaration = declaration;
			element.IsImplicit = (flags & 2) == 2;
			elements.Push(element);
			if (!element.IsImplicit)
				nodes.Enqueue(element);
			
			if (oldDeclaration != null) {
				nodes.Enqueue(new XmlBamlSimpleProperty(XWPFNamespace, "Class", oldDeclaration.FullyQualifiedName.Replace('+', '.')));
			}

			if (parentElement != null && complexPropertyOpened == 0 && !Current.IsInStaticResource && Current.Previous.IsDeferred) {
				if (keys != null && keys.Count > currentKey) {
					string key = keys[currentKey].KeyString;
					AddKeyToElement(key);
					currentKey++;
				}
			}
		}

		void AddKeyToElement(string key)
		{
			PropertyDeclaration pd = new PropertyDeclaration("Key", XamlTypeDeclaration);
			XmlBamlProperty property = new XmlBamlProperty(elements.Peek(), PropertyType.Key, pd);

			property.Value = key;

			nodes.Enqueue(property);
		}

		XmlPIMapping FindByClrNamespaceAndAssemblyId(TypeDeclaration declaration)
		{
			return FindByClrNamespaceAndAssemblyName(declaration.Namespace, declaration.Assembly);
		}
		
		XmlPIMapping FindByClrNamespaceAndAssemblyName(string clrNamespace, string assemblyName)
		{
			if (clrNamespace == XamlTypeDeclaration.Namespace && assemblyName == XamlTypeDeclaration.Assembly)
				return new XmlPIMapping(XmlPIMapping.XamlNamespace, assemblyName, clrNamespace);
			for (int x = 0; x < Mappings.Count; x++) {
				XmlPIMapping xp = Mappings[x];
				if (string.Equals(xp.Assembly, assemblyName, StringComparison.Ordinal) && string.Equals(xp.ClrNamespace, clrNamespace, StringComparison.Ordinal))
					return xp;
			}

			return null;
		}

		void ReadPIMapping()
		{
			string xmlNamespace = reader.ReadString();
			string clrNamespace = reader.ReadString();
			short assemblyId = reader.ReadInt16();

			Mappings.Add(new XmlPIMapping(xmlNamespace, GetAssembly(assemblyId), clrNamespace));
		}

		void ReadContentProperty()
		{
			reader.ReadInt16();

			// Non serve aprire niente, è il default
		}

		static void ReadConstructorParametersStart()
		{
			//this.constructorParameterTable.Add(this.elements.Peek());
			//PromoteDataToComplexProperty();
		}

		static void ReadConstructorParametersEnd()
		{
			//this.constructorParameterTable.Remove(this.elements.Peek());
			//properties.Pop();
		}

		void ReadConstructorParameterType()
		{
			short identifier = reader.ReadInt16();

			//TypeDeclaration declaration = GetTypeDeclaration(identifier);
			nodes.Enqueue(new XmlBamlText(GetTypeExtension(identifier)));
		}

		void ReadText()
		{
			string text = reader.ReadString();

			nodes.Enqueue(new XmlBamlText(text));
		}

		void ReadKeyElementStart()
		{
			short typeIdentifier = reader.ReadInt16();
			byte valueIdentifier = reader.ReadByte();
			// TODO: handle shared
			//bool shared = (valueIdentifier & 1) != 0;
			//bool sharedSet = (valueIdentifier & 2) != 0;
			int position = reader.ReadInt32();
			reader.ReadBoolean();
			reader.ReadBoolean();

			TypeDeclaration declaration = this.GetTypeDeclaration(typeIdentifier);

			XmlBamlPropertyElement property = new XmlBamlPropertyElement(elements.Peek(), PropertyType.Key, new PropertyDeclaration("Key", declaration));
			property.Position = position;
			elements.Push(property);
			nodes.Enqueue(property);
			complexPropertyOpened++;
		}

		void ReadKeyElementEnd()
		{
			XmlBamlPropertyElement propertyElement = (XmlBamlPropertyElement)elements.Peek();

			CloseElement();
			complexPropertyOpened--;
			if (complexPropertyOpened == 0) {
				int start = nodes.IndexOf(propertyElement);

				StringBuilder sb = new StringBuilder();
				FormatElementExtension((XmlBamlElement)nodes[start], sb);
				keys.Add(new KeyMapping(sb.ToString()) { Position = -1 });
			}
		}

		void ReadStaticResourceStart()
		{
			Current.IsInStaticResource = true;
			short identifier = reader.ReadInt16();
			byte flags = reader.ReadByte();
			TypeDeclaration declaration = GetTypeDeclaration(identifier);
			var lastKey = keys.LastOrDefault();
			if (lastKey == null)
				throw new InvalidOperationException("No key mapping found for StaticResourceStart!");
			lastKey.StaticResources.Add(declaration);
			XmlBamlElement element;
			if (elements.Any())
				element = new XmlBamlElement(elements.Peek());
			else
				element = new XmlBamlElement();
			element.TypeDeclaration = declaration;
			elements.Push(element);
			nodes.Enqueue(element);
		}

		void ReadStaticResourceEnd()
		{
			CloseElement();
			Current.IsInStaticResource = false;
		}

		void ReadStaticResourceId()
		{
			short identifier = reader.ReadInt16();
			object staticResource = GetStaticResource(identifier);
		}

		void ReadPresentationOptionsAttribute()
		{
			string text = reader.ReadString();
			short valueIdentifier = reader.ReadInt16();

			PropertyDeclaration pd = new PropertyDeclaration(this.stringTable[valueIdentifier].ToString());

			XmlBamlProperty property = new XmlBamlProperty(elements.Peek(), PropertyType.Value, pd);
			property.Value = text;
		}

		void ReadPropertyTypeReference()
		{
			short identifier = reader.ReadInt16();
			short typeIdentifier = reader.ReadInt16();

			PropertyDeclaration pd = this.GetPropertyDeclaration(identifier);
			string value = this.GetTypeExtension(typeIdentifier);

			XmlBamlProperty property = new XmlBamlProperty(elements.Peek(), PropertyType.Value, pd);
			property.Value = value;

			nodes.Enqueue(property);
		}

		void ReadOptimizedStaticResource()
		{
			byte flags = reader.ReadByte();
			short typeIdentifier = reader.ReadInt16();
			bool isValueType = (flags & 1) == 1;
			bool isStaticType = (flags & 2) == 2;
			object resource;

			if (isValueType)
				resource = GetTypeExtension(typeIdentifier);
			else if (isStaticType) {
				object name = GetResourceName(typeIdentifier);
				if (name == null)
					resource = null;
				else if (name is ResourceName)
					resource = GetStaticExtension(((ResourceName)name).Name);
				else if (name is PropertyDeclaration)
					resource = GetStaticExtension(FormatPropertyDeclaration(((PropertyDeclaration)name), true, false, false));
				else
					throw new InvalidOperationException("Invalid resource: " + name.GetType());
			} else {
				resource = this.stringTable[typeIdentifier];
			}
			
			var lastKey = keys.LastOrDefault();
			if (lastKey == null)
				throw new InvalidOperationException("No key mapping found for OptimizedStaticResource!");
			lastKey.StaticResources.Add(resource);
		}

		string GetTemplateBindingExtension(PropertyDeclaration propertyDeclaration)
		{
			return String.Format("{{TemplateBinding {0}}}", FormatPropertyDeclaration(propertyDeclaration, true, false, false));
		}

		string GetStaticExtension(string name)
		{
			string prefix = this.LookupPrefix(XmlPIMapping.XamlNamespace, false);
			if (String.IsNullOrEmpty(prefix))
				return String.Format("{{Static {0}}}", name);
			else
				return String.Format("{{{0}:Static {1}}}", prefix, name);
		}

		string GetExtension(TypeDeclaration declaration, string value)
		{
			return String.Format("{{{0} {1}}}", FormatTypeDeclaration(declaration), value);
		}

		string GetTypeExtension(short typeIdentifier)
		{
			string prefix = this.LookupPrefix(XmlPIMapping.XamlNamespace, false);
			if (String.IsNullOrEmpty(prefix))
				return String.Format("{{Type {0}}}", FormatTypeDeclaration(GetTypeDeclaration(typeIdentifier)));
			else
				return String.Format("{{{0}:Type {1}}}", prefix, FormatTypeDeclaration(GetTypeDeclaration(typeIdentifier)));
		}

		string FormatTypeDeclaration(TypeDeclaration typeDeclaration)
		{
			XmlPIMapping mapping = FindByClrNamespaceAndAssemblyName(typeDeclaration.Namespace, typeDeclaration.Assembly);
			string prefix = (mapping != null) ? this.LookupPrefix(mapping.XmlNamespace, false) : null;
			string name = typeDeclaration.Name;
			if (name.EndsWith("Extension"))
				name = name.Substring(0, name.Length - 9);
			if (String.IsNullOrEmpty(prefix))
				return name;
			else
				return String.Format("{0}:{1}", prefix, name);
		}

		string FormatPropertyDeclaration(PropertyDeclaration propertyDeclaration, bool withPrefix, bool useReading, bool checkType)
		{
			StringBuilder sb = new StringBuilder();

			TypeDeclaration elementDeclaration = (useReading) ? readingElements.Peek().TypeDeclaration : elements.Peek().TypeDeclaration;

			IDependencyPropertyDescriptor descriptor = null;
			bool areValidTypes = elementDeclaration.Type != null && propertyDeclaration.DeclaringType.Type != null;
			if (areValidTypes)
				descriptor = this.Resolver.GetDependencyPropertyDescriptor(propertyDeclaration.Name, elementDeclaration.Type, propertyDeclaration.DeclaringType.Type);

			bool isDescendant = (areValidTypes && (propertyDeclaration.DeclaringType.Type.Equals(elementDeclaration.Type) || elementDeclaration.Type.IsSubclassOf(propertyDeclaration.DeclaringType.Type)));
			bool isAttached = (descriptor != null && descriptor.IsAttached);
			bool differentType = ((propertyDeclaration.DeclaringType != propertyDeclaration.DeclaringType || !isDescendant));

			if (withPrefix) {
				XmlPIMapping mapping = FindByClrNamespaceAndAssemblyName(propertyDeclaration.DeclaringType.Namespace, propertyDeclaration.DeclaringType.Assembly);
				string prefix = (mapping != null) ? this.LookupPrefix(mapping.XmlNamespace, false) : null;

				if (!String.IsNullOrEmpty(prefix)) {
					sb.Append(prefix);
					sb.Append(":");
				}
			}
			if ((differentType || isAttached || !checkType) && propertyDeclaration.DeclaringType.Name.Length > 0) {
				sb.Append(propertyDeclaration.DeclaringType.Name);
				sb.Append(".");
			}
			sb.Append(propertyDeclaration.Name);

			return sb.ToString();
		}

		void ReadPropertyWithStaticResourceIdentifier()
		{
			short propertyId = reader.ReadInt16();
			short index = reader.ReadInt16();

			PropertyDeclaration pd = this.GetPropertyDeclaration(propertyId);
			object staticResource = GetStaticResource(index);

			string prefix = this.LookupPrefix(XmlPIMapping.PresentationNamespace, false);
			string value = String.Format("{{{0}{1}StaticResource {2}}}", prefix, (String.IsNullOrEmpty(prefix)) ? String.Empty : ":", staticResource);

			XmlBamlProperty property = new XmlBamlProperty(elements.Peek(), PropertyType.Value, pd);
			property.Value = value;

			nodes.Enqueue(property);
		}

		object GetStaticResource(short identifier)
		{
			int keyIndex = currentKey - 1;
			while (keyIndex >= 0 && !keys[keyIndex].HasStaticResources)
				keyIndex--;
			if (keyIndex >= 0 && identifier < keys[keyIndex].StaticResources.Count)
				return keys[keyIndex].StaticResources[(int)identifier];
//			Debug.WriteLine(string.Format("Cannot find StaticResource: {0}", identifier));
//			return "???" + identifier + "???";
			throw new ArgumentException("Cannot find StaticResource: " + identifier, "identifier");
		}

		void ReadTextWithConverter()
		{
			string text = reader.ReadString();
			reader.ReadInt16();

			nodes.Enqueue(new XmlBamlText(text));
		}

		void ReadTypeInfo()
		{
			short typeId = reader.ReadInt16();
			short assemblyId = reader.ReadInt16();
			string fullName = reader.ReadString();
			assemblyId = (short)(assemblyId & 0xfff);
			TypeDeclaration declaration;
			int length = fullName.LastIndexOf('.');
			if (length != -1)
			{
				string name = fullName.Substring(length + 1);
				string namespaceName = fullName.Substring(0, length);
				declaration = new TypeDeclaration(this, this.Resolver, name, namespaceName, assemblyId);
			}
			else
			{
				declaration = new TypeDeclaration(this, this.Resolver, fullName, string.Empty, assemblyId);
			}
			this.typeTable.Add(typeId, declaration);
		}

		void ReadAssemblyInfo()
		{
			short key = reader.ReadInt16();
			string text = reader.ReadString();
			this.assemblyTable.Add(key, text);
		}

		void ReadStringInfo()
		{
			short key = reader.ReadInt16();
			string text = reader.ReadString();
			this.stringTable.Add(key, text);
		}

		TypeDeclaration GetTypeDeclaration(short identifier)
		{
			TypeDeclaration declaration;
			if (identifier >= 0)
				declaration = this.typeTable[identifier];
			else
				declaration = KnownInfo.KnownTypeTable[-identifier];

			if (declaration == null)
				throw new NotSupportedException();

			return declaration;
		}
		
		TypeDeclaration GetKnownTypeDeclarationByName(string assemblyQualifiedName)
		{
			foreach (var type in KnownInfo.KnownTypeTable) {
				if (assemblyQualifiedName == type.AssemblyQualifiedName)
					return type;
			}
			return new ResolverTypeDeclaration(_resolver, assemblyQualifiedName);
		}

		internal string GetAssembly(short identifier)
		{
			return this.assemblyTable[identifier];
		}

		XmlBamlNode CurrentNode {
			get { return _currentNode; }
		}

		///<summary>
		///When overridden in a derived class, gets the namespace URI (as defined in the W3C Namespace specification) of the node on which the reader is positioned.
		///</summary>
		///<returns>
		///The namespace URI of the current node; otherwise an empty string.
		///</returns>
		public override string NamespaceURI {
			get {
				if (intoAttribute) return String.Empty;

				TypeDeclaration declaration;
				XmlBamlNode node = this.CurrentNode;
				if (node is XmlBamlSimpleProperty)
					return ((XmlBamlSimpleProperty)node).NamespaceName;
				else if (node is XmlBamlProperty) {
					declaration = ((XmlBamlProperty)node).PropertyDeclaration.DeclaringType;
					TypeDeclaration elementDeclaration = this.readingElements.Peek().TypeDeclaration;

					XmlPIMapping propertyMapping = FindByClrNamespaceAndAssemblyId(declaration) ?? XmlPIMapping.GetPresentationMapping(GetAssembly);
					XmlPIMapping elementMapping = FindByClrNamespaceAndAssemblyId(elementDeclaration) ?? XmlPIMapping.GetPresentationMapping(GetAssembly);
					
					if (((XmlBamlProperty)node).PropertyDeclaration.Name == "Name" &&
					    _resolver.IsLocalAssembly(((XmlBamlProperty)node).Parent.TypeDeclaration.Assembly))
						return XWPFNamespace;
					
					if (String.CompareOrdinal(propertyMapping.XmlNamespace, elementMapping.XmlNamespace) == 0
					    || (elementDeclaration.Type != null && declaration.Type != null && elementDeclaration.Type.IsSubclassOf(declaration.Type)))
						return String.Empty;
				}
				else if (node is XmlBamlPropertyElement)
				{
					XmlBamlPropertyElement property = (XmlBamlPropertyElement)node;
					declaration = property.TypeDeclaration;
					if (property.Parent.TypeDeclaration.Type.IsSubclassOf(property.PropertyDeclaration.DeclaringType.Type))
						declaration = property.Parent.TypeDeclaration;
				}
				else if (node is XmlBamlElement)
					declaration = ((XmlBamlElement)node).TypeDeclaration;
				else
					return String.Empty;

				XmlPIMapping mapping = FindByClrNamespaceAndAssemblyId(declaration);
				if (mapping == null)
					mapping = XmlPIMapping.GetPresentationMapping(GetAssembly);

				return mapping.XmlNamespace;
			}
		}

		///<summary>
		///When overridden in a derived class, gets the namespace prefix associated with the current node.
		///</summary>
		///<returns>
		///The namespace prefix associated with the current node.
		///</returns>
		public override string Prefix
		{
			get
			{
				if (!intoAttribute)
					return ((IXmlNamespaceResolver)this).LookupPrefix(this.NamespaceURI) ?? String.Empty;
				return String.Empty;
			}
		}

		///<summary>
		///When overridden in a derived class, gets a value indicating whether the current node can have a <see cref="P:System.Xml.XmlReader.Value"></see>.
		///</summary>
		///<returns>
		///true if the node on which the reader is currently positioned can have a Value; otherwise, false. If false, the node has a value of String.Empty.
		///</returns>
		public override bool HasValue
		{
			get { return this.Value != null; }
		}

		/// <summary>
		/// Returns object used to resolve types
		/// </summary>
		public ITypeResolver Resolver
		{
			get { return _resolver; }
		}

		///<summary>
		///When overridden in a derived class, gets the text value of the current node.
		///</summary>
		///<returns>
		///The value returned depends on the <see cref="P:System.Xml.XmlReader.NodeType"></see> of the node. The following table lists node types that have a value to return. All other node types return String.Empty.Node type Value AttributeThe value of the attribute. CDATAThe content of the CDATA section. CommentThe content of the comment. DocumentTypeThe internal subset. ProcessingInstructionThe entire content, excluding the target. SignificantWhitespaceThe white space between markup in a mixed content model. TextThe content of the text node. WhitespaceThe white space between markup. XmlDeclarationThe content of the declaration.
		///</returns>
		public override string Value
		{
			get
			{
				XmlBamlNode node = this.CurrentNode;
				if (node is XmlBamlSimpleProperty)
					return ((XmlBamlSimpleProperty)node).Value;
				else if (node is XmlBamlProperty)
					return ((XmlBamlProperty)node).Value.ToString();
				else if (node is XmlBamlText)
					return ((XmlBamlText)node).Text;
				else if (node is XmlBamlElement)
					return String.Empty;

				return String.Empty;
			}
		}

		/// <summary>
		/// Return root namespaces
		/// </summary>
		public IDictionary<string, string> RootNamespaces
		{
			get { return _rootNamespaces; }
		}

		///<summary>
		///When overridden in a derived class, gets the depth of the current node in the XML document.
		///</summary>
		///<returns>
		///The depth of the current node in the XML document.
		///</returns>
		public override int Depth
		{
			get { return this.readingElements.Count; }
		}

		///<summary>
		///When overridden in a derived class, gets the base URI of the current node.
		///</summary>
		///<returns>
		///The base URI of the current node.
		///</returns>
		public override string BaseURI
		{
			get { return String.Empty; }
		}

		///<summary>
		///When overridden in a derived class, gets a value indicating whether the current node is an empty element (for example, &lt;MyElement/&gt;).
		///</summary>
		///<returns>
		///true if the current node is an element (<see cref="P:System.Xml.XmlReader.NodeType"></see> equals XmlNodeType.Element) that ends with /&gt;; otherwise, false.
		///</returns>
		public override bool IsEmptyElement
		{
			get { return false; }
		}

		///<summary>
		///When overridden in a derived class, gets the number of attributes on the current node.
		///</summary>
		///<returns>
		///The number of attributes on the current node.
		///</returns>
		public override int AttributeCount {
			get { throw new NotImplementedException(); }
		}

		///<summary>
		///When overridden in a derived class, gets a value indicating whether the reader is positioned at the end of the stream.
		///</summary>
		///<returns>
		///true if the reader is positioned at the end of the stream; otherwise, false.
		///</returns>
		public override bool EOF {
			get { return _eof; }
		}

		///<summary>
		///When overridden in a derived class, gets the state of the reader.
		///</summary>
		///<returns>
		///One of the <see cref="T:System.Xml.ReadState"></see> values.
		///</returns>
		public override ReadState ReadState {
			get {
				if (!initialized)
					return ReadState.Initial;
				else if (reader == null)
					return ReadState.Closed;
				else if (this.EOF)
					return ReadState.EndOfFile;
				else
					return ReadState.Interactive;
			}
		}

		public List<XmlPIMapping> Mappings
		{
			get { return _mappings; }
		}

		///<summary>
		///When overridden in a derived class, gets the <see cref="T:System.Xml.XmlNameTable"></see> associated with this implementation.
		///</summary>
		///<returns>
		///The XmlNameTable enabling you to get the atomized version of a string within the node.
		///</returns>
		public override XmlNameTable NameTable
		{
			get { return _nameTable; }
		}

		#region IXmlNamespaceResolver Members

		///<summary>
		///Gets a collection of defined prefix-namespace Mappings that are currently in scope.
		///</summary>
		///
		///<returns>
		///An <see cref="T:System.Collections.IDictionary"></see> that contains the current in-scope namespaces.
		///</returns>
		///
		///<param name="scope">An <see cref="T:System.Xml.XmlNamespaceScope"></see> value that specifies the type of namespace nodes to return.</param>
		IDictionary<string, string> IXmlNamespaceResolver.GetNamespacesInScope(XmlNamespaceScope scope)
		{
			XmlNamespaceCollection namespaces = readingElements.Peek().Namespaces;
			Dictionary<String, String> list = new Dictionary<string, string>();
			foreach (XmlNamespace ns in namespaces)
			{
				list.Add(ns.Prefix, ns.Namespace);
			}

			return list;
		}

		///<summary>
		///Gets the namespace URI mapped to the specified prefix.
		///</summary>
		///
		///<returns>
		///The namespace URI that is mapped to the prefix; null if the prefix is not mapped to a namespace URI.
		///</returns>
		///
		///<param name="prefix">The prefix whose namespace URI you wish to find.</param>
		string IXmlNamespaceResolver.LookupNamespace(string prefix)
		{
			return this.LookupNamespace(prefix);
		}

		///<summary>
		///Gets the prefix that is mapped to the specified namespace URI.
		///</summary>
		///
		///<returns>
		///The prefix that is mapped to the namespace URI; null if the namespace URI is not mapped to a prefix.
		///</returns>
		///
		///<param name="namespaceName">The namespace URI whose prefix you wish to find.</param>
		string IXmlNamespaceResolver.LookupPrefix(string namespaceName)
		{
			return this.LookupPrefix(namespaceName, true);
		}

		string LookupPrefix(string namespaceName, bool useReading)
		{
			Stack<XmlBamlElement> elements;
			if (useReading)
				elements = readingElements;
			else
				elements = this.elements;

			if (elements.Count == 0) return null;
			XmlNamespaceCollection namespaces = elements.Peek().Namespaces;

			return LookupPrefix(namespaceName, namespaces);
		}

		static string LookupPrefix(string namespaceName, XmlNamespaceCollection namespaces)
		{
			for (int x = 0; x < namespaces.Count; x++)
			{
				if (String.CompareOrdinal(namespaces[x].Namespace, namespaceName) == 0)
					return namespaces[x].Prefix;
			}

			return null;
		}

		#endregion

		#region IntegerCollectionType

		internal enum IntegerCollectionType : byte
		{
			Byte = 2,
			Consecutive = 1,
			Integer = 4,
			Unknown = 0,
			UShort = 3
		}

		#endregion
	}
}