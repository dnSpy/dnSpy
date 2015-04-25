//
// flowanalyis.cs: The control flow analysis code
//
// Authors:
//   Martin Baulig (martin@ximian.com)
//   Raja R Harinath (rharinath@novell.com)
//   Marek Safar (marek.safar@gmail.com)
//
// Copyright 2001, 2002, 2003 Ximian, Inc.
// Copyright 2003-2008 Novell, Inc.
// Copyright 2011 Xamarin, Inc.
//

using System;
using System.Text;
using System.Collections.Generic;

namespace Mono.CSharp
{
	// <summary>
	//   This is used by the flow analysis code to keep track of the type of local variables.
	//
	//   The flow code uses a BitVector to keep track of whether a variable has been assigned
	//   or not.  This is easy for fundamental types (int, char etc.) or reference types since
	//   you can only assign the whole variable as such.
	//
	//   For structs, we also need to keep track of all its fields.  To do this, we allocate one
	//   bit for the struct itself (it's used if you assign/access the whole struct) followed by
	//   one bit for each of its fields.
	//
	//   This class computes this `layout' for each type.
	// </summary>
	public class TypeInfo
	{
		// <summary>
		//   Total number of bits a variable of this type consumes in the flow vector.
		// </summary>
		public readonly int TotalLength;

		// <summary>
		//   Number of bits the simple fields of a variable of this type consume
		//   in the flow vector.
		// </summary>
		public readonly int Length;

		// <summary>
		//   This is only used by sub-structs.
		// </summary>
		public readonly int Offset;

		// <summary>
		//   If this is a struct.
		// </summary>
		public readonly bool IsStruct;

		// <summary>
		//   If this is a struct, all fields which are structs theirselves.
		// </summary>
		public TypeInfo[] SubStructInfo;

		readonly StructInfo struct_info;
		private static Dictionary<TypeSpec, TypeInfo> type_hash;

		static readonly TypeInfo simple_type = new TypeInfo (1);
		
		static TypeInfo ()
		{
			Reset ();
		}
		
		public static void Reset ()
		{
			type_hash = new Dictionary<TypeSpec, TypeInfo> ();
			StructInfo.field_type_hash = new Dictionary<TypeSpec, StructInfo> ();
		}

		TypeInfo (int totalLength)
		{
			this.TotalLength = totalLength;
		}
		
		TypeInfo (StructInfo struct_info, int offset)
		{
			this.struct_info = struct_info;
			this.Offset = offset;
			this.Length = struct_info.Length;
			this.TotalLength = struct_info.TotalLength;
			this.SubStructInfo = struct_info.StructFields;
			this.IsStruct = true;
		}
		
		public int GetFieldIndex (string name)
		{
			if (struct_info == null)
				return 0;

			return struct_info [name];
		}

		public TypeInfo GetStructField (string name)
		{
			if (struct_info == null)
				return null;

			return struct_info.GetStructField (name);
		}

		public static TypeInfo GetTypeInfo (TypeSpec type)
		{
			if (!type.IsStruct)
				return simple_type;

			TypeInfo info;
			if (type_hash.TryGetValue (type, out info))
				return info;

			var struct_info = StructInfo.GetStructInfo (type);
			if (struct_info != null) {
				info = new TypeInfo (struct_info, 0);
			} else {
				info = simple_type;
			}

			type_hash.Add (type, info);
			return info;
		}

		// <summary>
		//   A struct's constructor must always assign all fields.
		//   This method checks whether it actually does so.
		// </summary>
		public bool IsFullyInitialized (FlowAnalysisContext fc, VariableInfo vi, Location loc)
		{
			if (struct_info == null)
				return true;

			bool ok = true;
			for (int i = 0; i < struct_info.Count; i++) {
				var field = struct_info.Fields[i];

				if (!fc.IsStructFieldDefinitelyAssigned (vi, field.Name)) {
					var bf = field.MemberDefinition as Property.BackingField;
					if (bf != null) {
						if (bf.Initializer != null)
							continue;

						fc.Report.Error (843, loc,
							"An automatically implemented property `{0}' must be fully assigned before control leaves the constructor. Consider calling the default struct contructor from a constructor initializer",
							field.GetSignatureForError ());

						ok = false;
						continue;
					}

					fc.Report.Error (171, loc,
						"Field `{0}' must be fully assigned before control leaves the constructor",
						field.GetSignatureForError ());
					ok = false;
				}
			}

			return ok;
		}

		public override string ToString ()
		{
			return String.Format ("TypeInfo ({0}:{1}:{2})",
					      Offset, Length, TotalLength);
		}

		class StructInfo
		{
			readonly List<FieldSpec> fields;
			public readonly TypeInfo[] StructFields;
			public readonly int Length;
			public readonly int TotalLength;

			public static Dictionary<TypeSpec, StructInfo> field_type_hash;
			private Dictionary<string, TypeInfo> struct_field_hash;
			private Dictionary<string, int> field_hash;

			bool InTransit;

			//
			// We only need one instance per type
			//
			StructInfo (TypeSpec type)
			{
				field_type_hash.Add (type, this);

				fields = MemberCache.GetAllFieldsForDefiniteAssignment (type);

				struct_field_hash = new Dictionary<string, TypeInfo> ();
				field_hash = new Dictionary<string, int> (fields.Count);

				StructFields = new TypeInfo[fields.Count];
				StructInfo[] sinfo = new StructInfo[fields.Count];

				InTransit = true;

				for (int i = 0; i < fields.Count; i++) {
					var field = fields [i];

					if (field.MemberType.IsStruct)
						sinfo [i] = GetStructInfo (field.MemberType);

					if (sinfo [i] == null)
						field_hash.Add (field.Name, ++Length);
					else if (sinfo [i].InTransit) {
						sinfo [i] = null;
						return;
					}
				}

				InTransit = false;

				TotalLength = Length + 1;
				for (int i = 0; i < fields.Count; i++) {
					var field = fields [i];

					if (sinfo [i] == null)
						continue;

					field_hash.Add (field.Name, TotalLength);

					StructFields [i] = new TypeInfo (sinfo [i], TotalLength);
					struct_field_hash.Add (field.Name, StructFields [i]);
					TotalLength += sinfo [i].TotalLength;
				}
			}

			public int Count {
				get {
					return fields.Count;
				}
			}

			public List<FieldSpec> Fields {
				get {
					return fields;
				}
			}

			public int this [string name] {
				get {
					int val;
					if (!field_hash.TryGetValue (name, out val))
						return 0;

					return val;
				}
			}

			public TypeInfo GetStructField (string name)
			{
				TypeInfo ti;
				if (struct_field_hash.TryGetValue (name, out ti))
					return ti;

				return null;
			}

			public static StructInfo GetStructInfo (TypeSpec type)
			{
				if (type.BuiltinType > 0)
					return null;

				StructInfo info;
				if (field_type_hash.TryGetValue (type, out info))
					return info;

				return new StructInfo (type);
			}
		}
	}

	//
	// This is used by definite assignment analysis code to store information about a local variable
	// or parameter.  Depending on the variable's type, we need to allocate one or more elements
	// in the BitVector - if it's a fundamental or reference type, we just need to know whether
	// it has been assigned or not, but for structs, we need this information for each of its fields.
	//
	public class VariableInfo
	{
		readonly string Name;

		readonly TypeInfo TypeInfo;

		// <summary>
		//   The bit offset of this variable in the flow vector.
		// </summary>
		readonly int Offset;

		// <summary>
		//   The number of bits this variable needs in the flow vector.
		//   The first bit always specifies whether the variable as such has been assigned while
		//   the remaining bits contain this information for each of a struct's fields.
		// </summary>
		readonly int Length;

		// <summary>
		//   If this is a parameter of local variable.
		// </summary>
		public bool IsParameter;

		VariableInfo[] sub_info;

		VariableInfo (string name, TypeSpec type, int offset)
		{
			this.Name = name;
			this.Offset = offset;
			this.TypeInfo = TypeInfo.GetTypeInfo (type);

			Length = TypeInfo.TotalLength;

			Initialize ();
		}

		VariableInfo (VariableInfo parent, TypeInfo type)
		{
			this.Name = parent.Name;
			this.TypeInfo = type;
			this.Offset = parent.Offset + type.Offset;
			this.Length = type.TotalLength;

			this.IsParameter = parent.IsParameter;

			Initialize ();
		}

		void Initialize ()
		{
			TypeInfo[] sub_fields = TypeInfo.SubStructInfo;
			if (sub_fields != null) {
				sub_info = new VariableInfo [sub_fields.Length];
				for (int i = 0; i < sub_fields.Length; i++) {
					if (sub_fields [i] != null)
						sub_info [i] = new VariableInfo (this, sub_fields [i]);
				}
			} else
				sub_info = new VariableInfo [0];
		}

		public static VariableInfo Create (BlockContext bc, LocalVariable variable)
		{
			var info = new VariableInfo (variable.Name, variable.Type, bc.AssignmentInfoOffset);
			bc.AssignmentInfoOffset += info.Length;
			return info;
		}

		public static VariableInfo Create (BlockContext bc, Parameter parameter)
		{
			var info = new VariableInfo (parameter.Name, parameter.Type, bc.AssignmentInfoOffset) {
				IsParameter = true
			};

			bc.AssignmentInfoOffset += info.Length;
			return info;
		}

		public bool IsAssigned (DefiniteAssignmentBitSet vector)
		{
			if (vector == null)
				return true;

			if (vector [Offset])
				return true;

			// Unless this is a struct
			if (!TypeInfo.IsStruct)
				return false;

			//
			// Following case cannot be handled fully by SetStructFieldAssigned
			// because we may encounter following case
			// 
			// struct A { B b }
			// struct B { int value; }
			//
			// setting a.b.value is propagated only to B's vector and not upwards to possible parents
			//
			//
			// Each field must be assigned
			//
			for (int i = Offset + 1; i <= TypeInfo.Length + Offset; i++) {
				if (!vector[i])
					return false;
			}

			// Ok, now check all fields which are structs.
			for (int i = 0; i < sub_info.Length; i++) {
				VariableInfo sinfo = sub_info[i];
				if (sinfo == null)
					continue;

				if (!sinfo.IsAssigned (vector))
					return false;
			}
			
			vector.Set (Offset);
			return true;
		}

		public bool IsEverAssigned { get; set; }

		public bool IsFullyInitialized (FlowAnalysisContext fc, Location loc)
		{
			return TypeInfo.IsFullyInitialized (fc, this, loc);
		}

		public bool IsStructFieldAssigned (DefiniteAssignmentBitSet vector, string field_name)
		{
			int field_idx = TypeInfo.GetFieldIndex (field_name);

			if (field_idx == 0)
				return true;

			return vector [Offset + field_idx];
		}

		public void SetAssigned (DefiniteAssignmentBitSet vector, bool generatedAssignment)
		{
			if (Length == 1)
				vector.Set (Offset);
			else
				vector.Set (Offset, Length);

			if (!generatedAssignment)
				IsEverAssigned = true;
		}

		public void SetStructFieldAssigned (DefiniteAssignmentBitSet vector, string field_name)
		{
			if (vector [Offset])
				return;

			int field_idx = TypeInfo.GetFieldIndex (field_name);

			if (field_idx == 0)
				return;

			var complex_field = TypeInfo.GetStructField (field_name);
			if (complex_field != null) {
				vector.Set (Offset + complex_field.Offset, complex_field.TotalLength);
			} else {
				vector.Set (Offset + field_idx);
			}

			IsEverAssigned = true;

			//
			// Each field must be assigned before setting master bit
			//
			for (int i = Offset + 1; i < TypeInfo.TotalLength + Offset; i++) {
				if (!vector[i])
					return;
			}

			//
			// Set master struct flag to assigned when all tested struct
			// fields have been assigned
			//
			vector.Set (Offset);
		}

		public VariableInfo GetStructFieldInfo (string fieldName)
		{
			TypeInfo type = TypeInfo.GetStructField (fieldName);

			if (type == null)
				return null;

			return new VariableInfo (this, type);
		}

		public override string ToString ()
		{
			return String.Format ("Name={0} Offset={1} Length={2} {3})", Name, Offset, Length, TypeInfo);
		}
	}

	public struct Reachability
	{
		readonly bool unreachable;

		Reachability (bool unreachable)
		{
			this.unreachable = unreachable;
		}

		public bool IsUnreachable {
			get {
				return unreachable;
			}
		}

		public static Reachability CreateUnreachable ()
		{
			return new Reachability (true);
		}

		public static Reachability operator & (Reachability a, Reachability b)
		{
		    return new Reachability (a.unreachable && b.unreachable);
		}

		public static Reachability operator | (Reachability a, Reachability b)
		{
			return new Reachability (a.unreachable | b.unreachable);
		}
	}

	//
	// Special version of bit array. Many operations can be simplified because
	// we are always dealing with arrays of same sizes
	//
	public class DefiniteAssignmentBitSet
	{
		const uint copy_on_write_flag = 1u << 31;

		uint bits;

		// Used when bits overflows
		int[] large_bits;

		public static readonly DefiniteAssignmentBitSet Empty = new DefiniteAssignmentBitSet (0);

		public DefiniteAssignmentBitSet (int length)
		{
			if (length > 31)
				large_bits = new int[(length + 31) / 32];
		}

		public DefiniteAssignmentBitSet (DefiniteAssignmentBitSet source)
		{
			if (source.large_bits != null) {
				large_bits = source.large_bits;
				bits = source.bits | copy_on_write_flag;
			} else {
				bits = source.bits & ~copy_on_write_flag;
			}
		}

		public static DefiniteAssignmentBitSet operator & (DefiniteAssignmentBitSet a, DefiniteAssignmentBitSet b)
		{
			if (AreEqual (a, b))
				return a;

			DefiniteAssignmentBitSet res;
			if (a.large_bits == null) {
				res = new DefiniteAssignmentBitSet (a);
				res.bits &= (b.bits & ~copy_on_write_flag);
				return res;
			}

			res = new DefiniteAssignmentBitSet (a);
			res.Clone ();
			var dest = res.large_bits;
			var src = b.large_bits;
			for (int i = 0; i < dest.Length; ++i) {
				dest[i] &= src[i];
			}

			return res;
		}

		public static DefiniteAssignmentBitSet operator | (DefiniteAssignmentBitSet a, DefiniteAssignmentBitSet b)
		{
			if (AreEqual (a, b))
				return a;

			DefiniteAssignmentBitSet res;
			if (a.large_bits == null) {
				res = new DefiniteAssignmentBitSet (a);
				res.bits |= b.bits;
				res.bits &= ~copy_on_write_flag;
				return res;
			}

			res = new DefiniteAssignmentBitSet (a);
			res.Clone ();
			var dest = res.large_bits;
			var src = b.large_bits;

			for (int i = 0; i < dest.Length; ++i) {
				dest[i] |= src[i];
			}

			return res;
		}

		public static DefiniteAssignmentBitSet And (List<DefiniteAssignmentBitSet> das)
		{
			if (das.Count == 0)
				throw new ArgumentException ("Empty das");

			DefiniteAssignmentBitSet res = das[0];
			for (int i = 1; i < das.Count; ++i) {
				res &= das[i];
			}

			return res;
		}

		bool CopyOnWrite {
			get {
				return (bits & copy_on_write_flag) != 0;
			}
		}

		int Length {
			get {
				return large_bits == null ? 31 : large_bits.Length * 32;
			}
		}

		public void Set (int index)
		{
			if (CopyOnWrite && !this[index])
				Clone ();

			SetBit (index);
		}

		public void Set (int index, int length)
		{
			for (int i = 0; i < length; ++i) {
				if (CopyOnWrite && !this[index + i])
					Clone ();

				SetBit (index + i);
			}
		}

		public bool this[int index] {
			get {
				return GetBit (index);
			}
		}

		public override string ToString ()
		{
			var length = Length;
			StringBuilder sb = new StringBuilder (length);
			for (int i = 0; i < length; ++i) {
				sb.Append (this[i] ? '1' : '0');
			}

			return sb.ToString ();
		}

		void Clone ()
		{
			large_bits = (int[]) large_bits.Clone ();
		}

		bool GetBit (int index)
		{
			return large_bits == null ?
				(bits & (1 << index)) != 0 :
				(large_bits[index >> 5] & (1 << (index & 31))) != 0;
		}

		void SetBit (int index)
		{
			if (large_bits == null)
				bits = (uint) ((int) bits | (1 << index));
			else
				large_bits[index >> 5] |= (1 << (index & 31));
		}

		static bool AreEqual (DefiniteAssignmentBitSet a, DefiniteAssignmentBitSet b)
		{
			if (a.large_bits == null)
				return (a.bits & ~copy_on_write_flag) == (b.bits & ~copy_on_write_flag);

			for (int i = 0; i < a.large_bits.Length; ++i) {
				if (a.large_bits[i] != b.large_bits[i])
					return false;
			}

			return true;
		}
	}
}
