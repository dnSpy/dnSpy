// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;

namespace ICSharpCode.NRefactory.Ast
{
	public class TypeReference : AbstractNode, INullable, ICloneable
	{
		public static readonly TypeReference StructConstraint = new TypeReference("constraint: struct");
		public static readonly TypeReference ClassConstraint = new TypeReference("constraint: class");
		public static readonly TypeReference NewConstraint = new TypeReference("constraint: new");
		
		string type = "";
		int    pointerNestingLevel;
		int[]  rankSpecifier;
		List<TypeReference> genericTypes = new List<TypeReference>();
		
		#region Static primitive type list
		static Dictionary<string, string> types   = new Dictionary<string, string>();
		static Dictionary<string, string> vbtypes = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
		static Dictionary<string, string> typesReverse   = new Dictionary<string, string>();
		static Dictionary<string, string> vbtypesReverse = new Dictionary<string, string>();
		
		static TypeReference()
		{
			// C# types
			types.Add("bool",    "System.Boolean");
			types.Add("byte",    "System.Byte");
			types.Add("char",    "System.Char");
			types.Add("decimal", "System.Decimal");
			types.Add("double",  "System.Double");
			types.Add("float",   "System.Single");
			types.Add("int",     "System.Int32");
			types.Add("long",    "System.Int64");
			types.Add("object",  "System.Object");
			types.Add("sbyte",   "System.SByte");
			types.Add("short",   "System.Int16");
			types.Add("string",  "System.String");
			types.Add("uint",    "System.UInt32");
			types.Add("ulong",   "System.UInt64");
			types.Add("ushort",  "System.UInt16");
			types.Add("void",    "System.Void");
			
			// VB.NET types
			vbtypes.Add("Boolean", "System.Boolean");
			vbtypes.Add("Byte",    "System.Byte");
			vbtypes.Add("SByte",   "System.SByte");
			vbtypes.Add("Date",	   "System.DateTime");
			vbtypes.Add("Char",    "System.Char");
			vbtypes.Add("Decimal", "System.Decimal");
			vbtypes.Add("Double",  "System.Double");
			vbtypes.Add("Single",  "System.Single");
			vbtypes.Add("Integer", "System.Int32");
			vbtypes.Add("Long",    "System.Int64");
			vbtypes.Add("UInteger","System.UInt32");
			vbtypes.Add("ULong",   "System.UInt64");
			vbtypes.Add("Object",  "System.Object");
			vbtypes.Add("Short",   "System.Int16");
			vbtypes.Add("UShort",  "System.UInt16");
			vbtypes.Add("String",  "System.String");
			
			foreach (KeyValuePair<string, string> pair in types) {
				typesReverse.Add(pair.Value, pair.Key);
			}
			foreach (KeyValuePair<string, string> pair in vbtypes) {
				vbtypesReverse.Add(pair.Value, pair.Key);
			}
		}
		
		/// <summary>
		/// Gets a shortname=>full name dictionary of C# types.
		/// </summary>
		public static IDictionary<string, string> PrimitiveTypesCSharp {
			get { return types; }
		}
		
		/// <summary>
		/// Gets a shortname=>full name dictionary of VB types.
		/// </summary>
		public static IDictionary<string, string> PrimitiveTypesVB {
			get { return vbtypes; }
		}
		
		/// <summary>
		/// Gets a full name=>shortname dictionary of C# types.
		/// </summary>
		public static IDictionary<string, string> PrimitiveTypesCSharpReverse {
			get { return typesReverse; }
		}
		
		/// <summary>
		/// Gets a full name=>shortname dictionary of VB types.
		/// </summary>
		public static IDictionary<string, string> PrimitiveTypesVBReverse {
			get { return vbtypesReverse; }
		}
		
		
		static string GetSystemType(string type)
		{
			if (types == null) return type;
			
			string systemType;
			if (types.TryGetValue(type, out systemType)) {
				return systemType;
			}
			if (vbtypes.TryGetValue(type, out systemType)) {
				return systemType;
			}
			return type;
		}
		#endregion
		
		object ICloneable.Clone()
		{
			return this.Clone();
		}
		
		public virtual TypeReference Clone()
		{
			TypeReference c = new TypeReference(type);
			CopyFields(this, c);
			return c;
		}
		
		/// <summary>
		/// Copies the pointerNestingLevel, RankSpecifier, GenericTypes and IsGlobal flag
		/// from <paramref name="from"/> to <paramref name="to"/>.
		/// </summary>
		/// <remarks>
		/// If <paramref name="to"/> already contains generics, the new generics are appended to the list.
		/// </remarks>
		protected static void CopyFields(TypeReference from, TypeReference to)
		{
			to.pointerNestingLevel = from.pointerNestingLevel;
			if (from.rankSpecifier != null) {
				to.rankSpecifier = (int[])from.rankSpecifier.Clone();
			}
			foreach (TypeReference r in from.genericTypes) {
				to.genericTypes.Add(r.Clone());
			}
			to.IsGlobal = from.IsGlobal;
			to.IsKeyword = from.IsKeyword;
		}
		
		public string Type {
			get {
				return type;
			}
			set {
				Debug.Assert(value != null);
				type = value ?? "?";
			}
		}
		
		/// <summary>
		/// Removes the last identifier from the type.
		/// e.g. "System.String.Length" becomes "System.String" or
		/// "System.Collections.IEnumerable(of string).Current" becomes "System.Collections.IEnumerable(of string)"
		/// This is used for explicit interface implementation in VB.
		/// </summary>
		public static string StripLastIdentifierFromType(ref TypeReference tr)
		{
			if (tr is InnerClassTypeReference && ((InnerClassTypeReference)tr).Type.IndexOf('.') < 0) {
				string ident = ((InnerClassTypeReference)tr).Type;
				tr = ((InnerClassTypeReference)tr).BaseType;
				return ident;
			} else {
				int pos = tr.Type.LastIndexOf('.');
				if (pos < 0)
					return tr.Type;
				string ident = tr.Type.Substring(pos + 1);
				tr.Type = tr.Type.Substring(0, pos);
				return ident;
			}
		}
		
		public int PointerNestingLevel {
			get {
				return pointerNestingLevel;
			}
			set {
				Debug.Assert(this.IsNull == false);
				pointerNestingLevel = value;
			}
		}
		
		/// <summary>
		/// The rank of the array type.
		/// For "object[]", this is { 0 }; for "object[,]", it is {1}.
		/// For "object[,][,,][]", it is {1, 2, 0}.
		/// For non-array types, this property is null or {}.
		/// </summary>
		public int[] RankSpecifier {
			get {
				return rankSpecifier;
			}
			set {
				Debug.Assert(this.IsNull == false);
				rankSpecifier = value;
			}
		}
		
		public List<TypeReference> GenericTypes {
			get {
				return genericTypes;
			}
		}
		
		public bool IsArrayType {
			get {
				return rankSpecifier != null && rankSpecifier.Length > 0;
			}
		}
		
		public static TypeReference CheckNull(TypeReference typeReference)
		{
			return typeReference ?? NullTypeReference.Instance;
		}
		
		public static TypeReference Null {
			get {
				return NullTypeReference.Instance;
			}
		}
		
		public virtual bool IsNull {
			get {
				return false;
			}
		}
		
		/// <summary>
		/// Gets/Sets if the type reference had a "global::" prefix.
		/// </summary>
		public bool IsGlobal {
			get; set;
		}
		
		/// <summary>
		/// Gets/Sets if the type reference was using a language keyword.
		/// </summary>
		public bool IsKeyword {
			get; set;
		}
		
		public TypeReference(string type)
		{
			this.Type = type;
		}
		
		public TypeReference(string type, bool isKeyword)
		{
			this.Type = type;
			this.IsKeyword = isKeyword;
		}
		
		public TypeReference(string type, List<TypeReference> genericTypes) : this(type)
		{
			if (genericTypes != null) {
				this.genericTypes = genericTypes;
			}
		}
		
		public TypeReference(string type, int[] rankSpecifier) : this(type, 0, rankSpecifier)
		{
		}
		
		public TypeReference(string type, int pointerNestingLevel, int[] rankSpecifier) : this(type, pointerNestingLevel, rankSpecifier, null)
		{
		}
		
		public TypeReference(string type, int pointerNestingLevel, int[] rankSpecifier, List<TypeReference> genericTypes)
		{
			Debug.Assert(type != null);
			this.type = type;
			this.pointerNestingLevel = pointerNestingLevel;
			this.rankSpecifier = rankSpecifier;
			if (genericTypes != null) {
				this.genericTypes = genericTypes;
			}
		}
		
		protected TypeReference()
		{}
		
		public override object AcceptVisitor(IAstVisitor visitor, object data)
		{
			return visitor.VisitTypeReference(this, data);
		}
		
		public override string ToString()
		{
			StringBuilder b = new StringBuilder(type);
			if (genericTypes != null && genericTypes.Count > 0) {
				b.Append('<');
				for (int i = 0; i < genericTypes.Count; i++) {
					if (i > 0) b.Append(',');
					b.Append(genericTypes[i].ToString());
				}
				b.Append('>');
			}
			if (pointerNestingLevel > 0) {
				b.Append('*', pointerNestingLevel);
			}
			if (IsArrayType) {
				foreach (int rank in rankSpecifier) {
					b.Append('[');
					if (rank < 0)
						b.Append('`', -rank);
					else
						b.Append(',', rank);
					b.Append(']');
				}
			}
			return b.ToString();
		}
		
		public static bool AreEqualReferences(TypeReference a, TypeReference b)
		{
			if (a == b) return true;
			if (a == null || b == null) return false;
			if (a is InnerClassTypeReference) a = ((InnerClassTypeReference)a).CombineToNormalTypeReference();
			if (b is InnerClassTypeReference) b = ((InnerClassTypeReference)b).CombineToNormalTypeReference();
			if (a.type != b.type) return false;
			if (a.IsKeyword != b.IsKeyword) return false;
			if (a.IsGlobal != b.IsGlobal) return false;
			if (a.pointerNestingLevel != b.pointerNestingLevel) return false;
			if (a.IsArrayType != b.IsArrayType) return false;
			if (a.IsArrayType) {
				if (a.rankSpecifier.Length != b.rankSpecifier.Length) return false;
				for (int i = 0; i < a.rankSpecifier.Length; i++) {
					if (a.rankSpecifier[i] != b.rankSpecifier[i]) return false;
				}
			}
			if (a.genericTypes.Count != b.genericTypes.Count) return false;
			for (int i = 0; i < a.genericTypes.Count; i++) {
				if (!AreEqualReferences(a.genericTypes[i], b.genericTypes[i]))
					return false;
			}
			return true;
		}
	}

	internal sealed class NullTypeReference : TypeReference
	{
		public static readonly NullTypeReference Instance = new NullTypeReference();
		public override bool IsNull {
			get {
				return true;
			}
		}
		public override TypeReference Clone()
		{
			return this;
		}
		public override object AcceptVisitor(IAstVisitor visitor, object data)
		{
			return null;
		}
		
		public override string ToString()
		{
			return String.Format("[NullTypeReference]");
		}
	}

	/// <summary>
	/// We need this special type reference for cases like
	/// OuterClass(Of T1).InnerClass(Of T2) (in expression or type context)
	/// or Dictionary(Of String, NamespaceStruct).KeyCollection (in type context, otherwise it's a
	/// MemberReferenceExpression)
	/// </summary>
	public class InnerClassTypeReference: TypeReference
	{
		TypeReference baseType;
		
		public TypeReference BaseType {
			get { return baseType; }
			set { baseType = value; }
		}
		
		public override TypeReference Clone()
		{
			InnerClassTypeReference c = new InnerClassTypeReference(baseType.Clone(), Type, new List<TypeReference>());
			CopyFields(this, c);
			return c;
		}
		
		public InnerClassTypeReference(TypeReference outerClass, string innerType, List<TypeReference> innerGenericTypes)
			: base(innerType, innerGenericTypes)
		{
			this.baseType = outerClass;
		}
		
		public override object AcceptVisitor(IAstVisitor visitor, object data)
		{
			return visitor.VisitInnerClassTypeReference(this, data);
		}
		
		/// <summary>
		/// Creates a type reference where all type parameters are specified for the innermost class.
		/// Namespace.OuterClass(of string).InnerClass(of integer).InnerInnerClass
		/// becomes Namespace.OuterClass.InnerClass.InnerInnerClass(of string, integer)
		/// </summary>
		public TypeReference CombineToNormalTypeReference()
		{
			TypeReference tr = (baseType is InnerClassTypeReference)
				? ((InnerClassTypeReference)baseType).CombineToNormalTypeReference()
				: baseType.Clone();
			CopyFields(this, tr);
			tr.Type += "." + Type;
			return tr;
		}
		
		public override string ToString()
		{
			return baseType.ToString() + "+" + base.ToString();
		}
	}
}
