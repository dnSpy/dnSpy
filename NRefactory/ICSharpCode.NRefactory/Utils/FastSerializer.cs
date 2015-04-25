// Copyright (c) 2011 Daniel Grunwald
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
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Serialization;

namespace ICSharpCode.NRefactory.Utils
{
	public class FastSerializer
	{
		#region Properties
		/// <summary>
		/// Gets/Sets the serialization binder that is being used.
		/// The default value is null, which will cause the FastSerializer to use the
		/// full assembly and type names.
		/// </summary>
		public SerializationBinder SerializationBinder { get; set; }
		
		/// <summary>
		/// Can be used to set several 'fixed' instances.
		/// When serializing, such instances will not be included; and any references to a fixed instance
		/// will be stored as the index in this array.
		/// When deserializing, the same (or equivalent) instances must be specified, and the deserializer
		/// will use them in place of the fixed instances.
		/// </summary>
		public object[] FixedInstances { get; set; }
		#endregion
		
		#region Constants
		const int magic = 0x71D28A5E;
		
		const byte Type_ReferenceType = 1;
		const byte Type_ValueType = 2;
		const byte Type_SZArray = 3;
		const byte Type_ParameterizedType = 4;
		#endregion
		
		#region Serialization
		sealed class SerializationType
		{
			public readonly int ID;
			public readonly Type Type;
			
			public SerializationType(int iD, Type type)
			{
				this.ID = iD;
				this.Type = type;
			}
			
			public ObjectScanner Scanner;
			public ObjectWriter Writer;
			public string TypeName;
			public int AssemblyNameID;
		}
		
		sealed class SerializationContext
		{
			readonly Dictionary<object, int> objectToID = new Dictionary<object, int>(ReferenceComparer.Instance);
			readonly List<object> instances = new List<object>(); // index: object ID
			readonly List<SerializationType> objectTypes = new List<SerializationType>(); // index: object ID
			SerializationType stringType;
			
			readonly Dictionary<Type, SerializationType> typeMap = new Dictionary<Type, SerializationType>();
			readonly List<SerializationType> types = new List<SerializationType>();
			
			readonly Dictionary<string, int> assemblyNameToID = new Dictionary<string, int>();
			readonly List<string> assemblyNames = new List<string>();
			
			readonly FastSerializer fastSerializer;
			public readonly BinaryWriter writer;
			int fixedInstanceCount;
			
			internal SerializationContext(FastSerializer fastSerializer, BinaryWriter writer)
			{
				this.fastSerializer = fastSerializer;
				this.writer = writer;
				instances.Add(null); // use object ID 0 for null
				objectTypes.Add(null);
			}
			
			#region Scanning
			public void MarkFixedInstances(object[] fixedInstances)
			{
				if (fixedInstances == null)
					return;
				foreach (object obj in fixedInstances) {
					if (!objectToID.ContainsKey(obj)) {
						objectToID.Add(obj, instances.Count);
						instances.Add(obj);
						fixedInstanceCount++;
					}
				}
			}
			
			/// <summary>
			/// Marks an instance for future scanning.
			/// </summary>
			public void Mark(object instance)
			{
				if (instance == null || objectToID.ContainsKey(instance))
					return;
				Log(" Mark {0}", instance.GetType().Name);
				
				objectToID.Add(instance, instances.Count);
				instances.Add(instance);
			}
			
			internal void Scan()
			{
				Log("Scanning...");
				// starting from 1, because index 0 is null
				// Also, do not scan any of the 'fixed instances'.
				for (int i = 1 + fixedInstanceCount; i < instances.Count; i++) {
					object instance = instances[i];
					ISerializable serializable = instance as ISerializable;
					Type type = instance.GetType();
					Log("Scan #{0}: {1}", i, type.Name);
					SerializationType sType = MarkType(type);
					objectTypes.Add(sType);
					if (serializable != null) {
						SerializationInfo info = new SerializationInfo(type, fastSerializer.formatterConverter);
						serializable.GetObjectData(info, fastSerializer.streamingContext);
						instances[i] = info;
						foreach (SerializationEntry entry in info) {
							Mark(entry.Value);
						}
						sType.Writer = serializationInfoWriter;
					} else {
						ObjectScanner objectScanner = sType.Scanner;
						if (objectScanner == null) {
							objectScanner = fastSerializer.GetScanner(type);
							sType.Scanner = objectScanner;
							sType.Writer = fastSerializer.GetWriter(type);
						}
						objectScanner(this, instance);
					}
				}
			}
			#endregion
			
			#region Scan Types
			SerializationType MarkType(Type type)
			{
				SerializationType sType;
				if (!typeMap.TryGetValue(type, out sType)) {
					string assemblyName = null;
					string typeName = null;
					if (type.HasElementType) {
						Debug.Assert(type.IsArray);
						MarkType(type.GetElementType());
					} else if (type.IsGenericType && !type.IsGenericTypeDefinition) {
						MarkType(type.GetGenericTypeDefinition());
						foreach (Type typeArg in type.GetGenericArguments())
							MarkType(typeArg);
					} else if (type.IsGenericParameter) {
						throw new NotSupportedException();
					} else {
						var serializationBinder = fastSerializer.SerializationBinder;
						if (serializationBinder != null) {
							serializationBinder.BindToName(type, out assemblyName, out typeName);
						} else {
							assemblyName = type.Assembly.FullName;
							typeName = type.FullName;
							Debug.Assert(typeName != null);
						}
					}
					
					sType = new SerializationType(typeMap.Count, type);
					sType.TypeName = typeName;
					if (assemblyName != null) {
						if (!assemblyNameToID.TryGetValue(assemblyName, out sType.AssemblyNameID)) {
							sType.AssemblyNameID = assemblyNames.Count;
							assemblyNameToID.Add(assemblyName, sType.AssemblyNameID);
							assemblyNames.Add(assemblyName);
							Log("Registered assembly #{0}: {1}", sType.AssemblyNameID, assemblyName);
						}
					}
					typeMap.Add(type, sType);
					types.Add(sType);
					Log("Registered type %{0}: {1}", sType.ID, type);
					if (type == typeof(string)) {
						stringType = sType;
					}
				}
				return sType;
			}
			
			internal void ScanTypes()
			{
				for (int i = 0; i < types.Count; i++) {
					Type type = types[i].Type;
					if (type.IsGenericTypeDefinition || type.HasElementType)
						continue;
					if (typeof(ISerializable).IsAssignableFrom(type))
						continue;
					foreach (FieldInfo field in GetSerializableFields(type)) {
						MarkType(field.FieldType);
					}
				}
			}
			#endregion
			
			#region Writing
			public void WriteObjectID(object instance)
			{
				int id = (instance == null) ? 0 : objectToID[instance];
				if (instances.Count <= ushort.MaxValue)
					writer.Write((ushort)id);
				else
					writer.Write(id);
			}
			
			void WriteTypeID(Type type)
			{
				Debug.Assert(typeMap.ContainsKey(type));
				int typeID = typeMap[type].ID;
				if (types.Count <= ushort.MaxValue)
					writer.Write((ushort)typeID);
				else
					writer.Write(typeID);
			}
			
			internal void Write()
			{
				Log("Writing...");
				writer.Write(magic);
				// Write out type information
				writer.Write(instances.Count);
				writer.Write(types.Count);
				writer.Write(assemblyNames.Count);
				writer.Write(fixedInstanceCount);
				
				foreach (string assemblyName in assemblyNames) {
					writer.Write(assemblyName);
				}
				
				foreach (SerializationType sType in types) {
					Type type = sType.Type;
					if (type.HasElementType) {
						if (type.IsArray) {
							if (type.GetArrayRank() == 1)
								writer.Write(Type_SZArray);
							else
								throw new NotSupportedException();
						} else {
							throw new NotSupportedException();
						}
						WriteTypeID(type.GetElementType());
					} else if (type.IsGenericType && !type.IsGenericTypeDefinition) {
						writer.Write(Type_ParameterizedType);
						WriteTypeID(type.GetGenericTypeDefinition());
						foreach (Type typeArg in type.GetGenericArguments()) {
							WriteTypeID(typeArg);
						}
					} else {
						if (type.IsValueType) {
							writer.Write(Type_ValueType);
						} else {
							writer.Write(Type_ReferenceType);
						}
						if (assemblyNames.Count <= ushort.MaxValue)
							writer.Write((ushort)sType.AssemblyNameID);
						else
							writer.Write(sType.AssemblyNameID);
						writer.Write(sType.TypeName);
					}
				}
				foreach (SerializationType sType in types) {
					Type type = sType.Type;
					if (type.IsGenericTypeDefinition || type.HasElementType)
						continue;
					writer.Write(FastSerializerVersionAttribute.GetVersionNumber(type));
					if (type.IsPrimitive || typeof(ISerializable).IsAssignableFrom(type)) {
						writer.Write(byte.MaxValue);
					} else {
						var fields = GetSerializableFields(type);
						if (fields.Count >= byte.MaxValue)
							throw new SerializationException("Too many fields.");
						writer.Write((byte)fields.Count);
						foreach (var field in fields) {
							WriteTypeID(field.FieldType);
							writer.Write(field.Name);
						}
					}
				}
				
				// Write out information necessary to create the instances
				// starting from 1, because index 0 is null
				for (int i = 1 + fixedInstanceCount; i < instances.Count; i++) {
					SerializationType sType = objectTypes[i];
					if (types.Count <= ushort.MaxValue)
						writer.Write((ushort)sType.ID);
					else
						writer.Write(sType.ID);
					if (sType == stringType) {
						// Strings are written to the output immediately
						// - we can't create an empty string and fill it later
						writer.Write((string)instances[i]);
					} else if (sType.Type.IsArray) {
						// For arrays, write down the length, because we need that to create the array instance
						writer.Write(((Array)instances[i]).Length);
					}
				}
				// Write out information necessary to fill data into the instances
				for (int i = 1 + fixedInstanceCount; i < instances.Count; i++) {
					Log("0x{2:x6}, Write #{0}: {1}", i, objectTypes[i].Type.Name, writer.BaseStream.Position);
					objectTypes[i].Writer(this, instances[i]);
				}
				Log("Serialization done.");
			}
			#endregion
		}
		
		#region Object Scanners
		delegate void ObjectScanner(SerializationContext context, object instance);
		
		static readonly MethodInfo mark = typeof(SerializationContext).GetMethod("Mark", new[] { typeof(object) });
		static readonly FieldInfo writerField = typeof(SerializationContext).GetField("writer");
		
		Dictionary<Type, ObjectScanner> scanners = new Dictionary<Type, ObjectScanner>();
		
		ObjectScanner GetScanner(Type type)
		{
			ObjectScanner scanner;
			if (!scanners.TryGetValue(type, out scanner)) {
				scanner = CreateScanner(type);
				scanners.Add(type, scanner);
			}
			return scanner;
		}
		
		ObjectScanner CreateScanner(Type type)
		{
			bool isArray = type.IsArray;
			if (isArray) {
				if (type.GetArrayRank() != 1)
					throw new NotSupportedException();
				type = type.GetElementType();
				if (!type.IsValueType) {
					return delegate (SerializationContext context, object array) {
						foreach (object val in (object[])array) {
							context.Mark(val);
						}
					};
				}
			}
			for (Type baseType = type; baseType != null; baseType = baseType.BaseType) {
				if (!baseType.IsSerializable)
					throw new SerializationException("Type " + baseType + " is not [Serializable].");
			}
			List<FieldInfo> fields = GetSerializableFields(type);
			fields.RemoveAll(f => !IsReferenceOrContainsReferences(f.FieldType));
			if (fields.Count == 0) {
				// The scanner has nothing to do for this object.
				return delegate { };
			}
			
			DynamicMethod dynamicMethod = new DynamicMethod(
				(isArray ? "ScanArray_" : "Scan_") + type.Name,
				typeof(void), new [] { typeof(SerializationContext), typeof(object) },
				true);
			ILGenerator il = dynamicMethod.GetILGenerator();
			
			
			if (isArray) {
				var instance = il.DeclareLocal(type.MakeArrayType());
				il.Emit(OpCodes.Ldarg_1);
				il.Emit(OpCodes.Castclass, type.MakeArrayType());
				il.Emit(OpCodes.Stloc, instance); // instance = (type[])arg_1;
				
				// for (int i = 0; i < instance.Length; i++) scan instance[i];
				var loopStart = il.DefineLabel();
				var loopHead = il.DefineLabel();
				var loopVariable = il.DeclareLocal(typeof(int));
				il.Emit(OpCodes.Ldc_I4_0);
				il.Emit(OpCodes.Stloc, loopVariable); // loopVariable = 0
				il.Emit(OpCodes.Br, loopHead); // goto loopHead;
				
				il.MarkLabel(loopStart);
				
				il.Emit(OpCodes.Ldloc, instance); // instance
				il.Emit(OpCodes.Ldloc, loopVariable); // instance, loopVariable
				il.Emit(OpCodes.Ldelem, type); // &instance[loopVariable]
				EmitScanValueType(il, type);
				
				
				il.Emit(OpCodes.Ldloc, loopVariable); // loopVariable
				il.Emit(OpCodes.Ldc_I4_1); // loopVariable, 1
				il.Emit(OpCodes.Add); // loopVariable+1
				il.Emit(OpCodes.Stloc, loopVariable); // loopVariable++;
				
				il.MarkLabel(loopHead);
				il.Emit(OpCodes.Ldloc, loopVariable); // loopVariable
				il.Emit(OpCodes.Ldloc, instance); // loopVariable, instance
				il.Emit(OpCodes.Ldlen); // loopVariable, instance.Length
				il.Emit(OpCodes.Conv_I4);
				il.Emit(OpCodes.Blt, loopStart); // if (loopVariable < instance.Length) goto loopStart;
			} else if (type.IsValueType) {
				// boxed value type
				il.Emit(OpCodes.Ldarg_1);
				il.Emit(OpCodes.Unbox_Any, type);
				EmitScanValueType(il, type);
			} else {
				// reference type
				var instance = il.DeclareLocal(type);
				il.Emit(OpCodes.Ldarg_1);
				il.Emit(OpCodes.Castclass, type);
				il.Emit(OpCodes.Stloc, instance); // instance = (type)arg_1;
				
				foreach (FieldInfo field in fields) {
					EmitScanField(il, instance, field); // scan instance.Field
				}
			}
			il.Emit(OpCodes.Ret);
			return (ObjectScanner)dynamicMethod.CreateDelegate(typeof(ObjectScanner));
		}

		/// <summary>
		/// Emit 'scan instance.Field'.
		/// Stack transition: ... => ...
		/// </summary>
		void EmitScanField(ILGenerator il, LocalBuilder instance, FieldInfo field)
		{
			if (field.FieldType.IsValueType) {
				il.Emit(OpCodes.Ldloc, instance); // instance
				il.Emit(OpCodes.Ldfld, field); // instance.field
				EmitScanValueType(il, field.FieldType);
			} else {
				il.Emit(OpCodes.Ldarg_0); // context
				il.Emit(OpCodes.Ldloc, instance); // context, instance
				il.Emit(OpCodes.Ldfld, field); // context, instance.field
				il.Emit(OpCodes.Call, mark); // context.Mark(instance.field);
			}
		}

		/// <summary>
		/// Stack transition: ..., value => ...
		/// </summary>
		void EmitScanValueType(ILGenerator il, Type valType)
		{
			var fieldRef = il.DeclareLocal(valType);
			il.Emit(OpCodes.Stloc, fieldRef);
			
			foreach (FieldInfo field in GetSerializableFields(valType)) {
				if (IsReferenceOrContainsReferences(field.FieldType)) {
					EmitScanField(il, fieldRef, field);
				}
			}
		}

		static List<FieldInfo> GetSerializableFields(Type type)
		{
			List<FieldInfo> fields = new List<FieldInfo>();
			for (Type baseType = type; baseType != null; baseType = baseType.BaseType) {
				FieldInfo[] declFields = baseType.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly);
				Array.Sort(declFields, (a,b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal));
				fields.AddRange(declFields);
			}
			fields.RemoveAll(f => f.IsNotSerialized);
			return fields;
		}

		static bool IsReferenceOrContainsReferences(Type type)
		{
			if (!type.IsValueType)
				return true;
			if (type.IsPrimitive)
				return false;
			foreach (FieldInfo field in GetSerializableFields(type)) {
				if (IsReferenceOrContainsReferences(field.FieldType))
					return true;
			}
			return false;
		}
		#endregion

		#region Object Writers
		delegate void ObjectWriter(SerializationContext context, object instance);

		static readonly MethodInfo writeObjectID = typeof(SerializationContext).GetMethod("WriteObjectID", new[] { typeof(object) });

		static readonly MethodInfo writeByte = typeof(BinaryWriter).GetMethod("Write", new[] { typeof(byte) });
		static readonly MethodInfo writeShort = typeof(BinaryWriter).GetMethod("Write", new[] { typeof(short) });
		static readonly MethodInfo writeInt = typeof(BinaryWriter).GetMethod("Write", new[] { typeof(int) });
		static readonly MethodInfo writeLong = typeof(BinaryWriter).GetMethod("Write", new[] { typeof(long) });
		static readonly MethodInfo writeFloat = typeof(BinaryWriter).GetMethod("Write", new[] { typeof(float) });
		static readonly MethodInfo writeDouble = typeof(BinaryWriter).GetMethod("Write", new[] { typeof(double) });
		OpCode callVirt = OpCodes.Callvirt;

		static readonly ObjectWriter serializationInfoWriter = delegate(SerializationContext context, object instance) {
			BinaryWriter writer = context.writer;
			SerializationInfo info = (SerializationInfo)instance;
			writer.Write(info.MemberCount);
			foreach (SerializationEntry entry in info) {
				writer.Write(entry.Name);
				context.WriteObjectID(entry.Value);
			}
		};

		Dictionary<Type, ObjectWriter> writers = new Dictionary<Type, ObjectWriter>();

		ObjectWriter GetWriter(Type type)
		{
			ObjectWriter writer;
			if (!writers.TryGetValue(type, out writer)) {
				writer = CreateWriter(type);
				writers.Add(type, writer);
			}
			return writer;
		}

		ObjectWriter CreateWriter(Type type)
		{
			if (type == typeof(string)) {
				// String contents are written in the object creation section,
				// not into the field value section.
				return delegate {};
			}
			bool isArray = type.IsArray;
			if (isArray) {
				if (type.GetArrayRank() != 1)
					throw new NotSupportedException();
				type = type.GetElementType();
				if (!type.IsValueType) {
					return delegate (SerializationContext context, object array) {
						foreach (object val in (object[])array) {
							context.WriteObjectID(val);
						}
					};
				} else if (type == typeof(byte)) {
					return delegate (SerializationContext context, object array) {
						context.writer.Write((byte[])array);
					};
				}
			}
			List<FieldInfo> fields = GetSerializableFields(type);
			if (fields.Count == 0) {
				// The writer has nothing to do for this object.
				return delegate { };
			}
			
			
			DynamicMethod dynamicMethod = new DynamicMethod(
				(isArray ? "WriteArray_" : "Write_") + type.Name,
				typeof(void), new [] { typeof(SerializationContext), typeof(object) },
				true);
			ILGenerator il = dynamicMethod.GetILGenerator();
			
			var writer = il.DeclareLocal(typeof(BinaryWriter));
			
			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Ldfld, writerField);
			il.Emit(OpCodes.Stloc, writer); // writer = context.writer;
			
			if (isArray) {
				var instance = il.DeclareLocal(type.MakeArrayType());
				il.Emit(OpCodes.Ldarg_1);
				il.Emit(OpCodes.Castclass, type.MakeArrayType());
				il.Emit(OpCodes.Stloc, instance); // instance = (type[])arg_1;
				
				// for (int i = 0; i < instance.Length; i++) write instance[i];
				
				var loopStart = il.DefineLabel();
				var loopHead = il.DefineLabel();
				var loopVariable = il.DeclareLocal(typeof(int));
				il.Emit(OpCodes.Ldc_I4_0);
				il.Emit(OpCodes.Stloc, loopVariable); // loopVariable = 0
				il.Emit(OpCodes.Br, loopHead); // goto loopHead;
				
				il.MarkLabel(loopStart);
				
				if (type.IsEnum || type.IsPrimitive) {
					if (type.IsEnum) {
						type = type.GetEnumUnderlyingType();
					}
					Debug.Assert(type.IsPrimitive);
					il.Emit(OpCodes.Ldloc, writer); // writer
					il.Emit(OpCodes.Ldloc, instance); // writer, instance
					il.Emit(OpCodes.Ldloc, loopVariable); // writer, instance, loopVariable
					switch (Type.GetTypeCode(type)) {
						case TypeCode.Boolean:
						case TypeCode.SByte:
						case TypeCode.Byte:
							il.Emit(OpCodes.Ldelem_I1); // writer, instance[loopVariable]
							il.Emit(callVirt, writeByte); // writer.Write(instance[loopVariable]);
							break;
						case TypeCode.Char:
						case TypeCode.Int16:
						case TypeCode.UInt16:
							il.Emit(OpCodes.Ldelem_I2); // writer, instance[loopVariable]
							il.Emit(callVirt, writeShort); // writer.Write(instance[loopVariable]);
							break;
						case TypeCode.Int32:
						case TypeCode.UInt32:
							il.Emit(OpCodes.Ldelem_I4);  // writer, instance[loopVariable]
							il.Emit(callVirt, writeInt); // writer.Write(instance[loopVariable]);
							break;
						case TypeCode.Int64:
						case TypeCode.UInt64:
							il.Emit(OpCodes.Ldelem_I8);  // writer, instance[loopVariable]
							il.Emit(callVirt, writeLong); // writer.Write(instance[loopVariable]);
							break;
						case TypeCode.Single:
							il.Emit(OpCodes.Ldelem_R4);  // writer, instance[loopVariable]
							il.Emit(callVirt, writeFloat); // writer.Write(instance[loopVariable]);
							break;
						case TypeCode.Double:
							il.Emit(OpCodes.Ldelem_R8);  // writer, instance[loopVariable]
							il.Emit(callVirt, writeDouble); // writer.Write(instance[loopVariable]);
							break;
						default:
							throw new NotSupportedException("Unknown primitive type " + type);
					}
				} else {
					il.Emit(OpCodes.Ldloc, instance); // instance
					il.Emit(OpCodes.Ldloc, loopVariable); // instance, loopVariable
					il.Emit(OpCodes.Ldelem, type); // instance[loopVariable]
					EmitWriteValueType(il, writer, type);
				}
				
				il.Emit(OpCodes.Ldloc, loopVariable); // loopVariable
				il.Emit(OpCodes.Ldc_I4_1); // loopVariable, 1
				il.Emit(OpCodes.Add); // loopVariable+1
				il.Emit(OpCodes.Stloc, loopVariable); // loopVariable++;
				
				il.MarkLabel(loopHead);
				il.Emit(OpCodes.Ldloc, loopVariable); // loopVariable
				il.Emit(OpCodes.Ldloc, instance); // loopVariable, instance
				il.Emit(OpCodes.Ldlen); // loopVariable, instance.Length
				il.Emit(OpCodes.Conv_I4);
				il.Emit(OpCodes.Blt, loopStart); // if (loopVariable < instance.Length) goto loopStart;
			} else if (type.IsValueType) {
				// boxed value type
				if (type.IsEnum || type.IsPrimitive) {
					il.Emit(OpCodes.Ldloc, writer);
					il.Emit(OpCodes.Ldarg_1);
					il.Emit(OpCodes.Unbox_Any, type);
					WritePrimitiveValue(il, type);
				} else {
					il.Emit(OpCodes.Ldarg_1);
					il.Emit(OpCodes.Unbox_Any, type);
					EmitWriteValueType(il, writer, type);
				}
			} else {
				// reference type
				var instance = il.DeclareLocal(type);
				il.Emit(OpCodes.Ldarg_1);
				il.Emit(OpCodes.Castclass, type);
				il.Emit(OpCodes.Stloc, instance); // instance = (type)arg_1;
				
				foreach (FieldInfo field in fields) {
					EmitWriteField(il, writer, instance, field); // write instance.Field
				}
			}
			il.Emit(OpCodes.Ret);
			return (ObjectWriter)dynamicMethod.CreateDelegate(typeof(ObjectWriter));
		}

		/// <summary>
		/// Emit 'write instance.Field'.
		/// Stack transition: ... => ...
		/// </summary>
		void EmitWriteField(ILGenerator il, LocalBuilder writer, LocalBuilder instance, FieldInfo field)
		{
			Type fieldType = field.FieldType;
			if (fieldType.IsValueType) {
				if (fieldType.IsPrimitive || fieldType.IsEnum) {
					il.Emit(OpCodes.Ldloc, writer); // writer
					il.Emit(OpCodes.Ldloc, instance); // writer, instance
					il.Emit(OpCodes.Ldfld, field); // writer, instance.field
					WritePrimitiveValue(il, fieldType);
				} else {
					il.Emit(OpCodes.Ldloc, instance); // instance
					il.Emit(OpCodes.Ldfld, field); // instance.field
					EmitWriteValueType(il, writer, fieldType);
				}
			} else {
				il.Emit(OpCodes.Ldarg_0); // context
				il.Emit(OpCodes.Ldloc, instance); // context, instance
				il.Emit(OpCodes.Ldfld, field); // context, instance.field
				il.Emit(OpCodes.Call, writeObjectID); // context.WriteObjectID(instance.field);
			}
		}
		
		/// <summary>
		/// Writes a primitive value of the specified type.
		/// Stack transition: ..., writer, value => ...
		/// </summary>
		void WritePrimitiveValue(ILGenerator il, Type fieldType)
		{
			if (fieldType.IsEnum) {
				fieldType = fieldType.GetEnumUnderlyingType();
				Debug.Assert(fieldType.IsPrimitive);
			}
			switch (Type.GetTypeCode(fieldType)) {
				case TypeCode.Boolean:
				case TypeCode.SByte:
				case TypeCode.Byte:
					il.Emit(callVirt, writeByte); // writer.Write(value);
					break;
				case TypeCode.Char:
				case TypeCode.Int16:
				case TypeCode.UInt16:
					il.Emit(callVirt, writeShort); // writer.Write(value);
					break;
				case TypeCode.Int32:
				case TypeCode.UInt32:
					il.Emit(callVirt, writeInt); // writer.Write(value);
					break;
				case TypeCode.Int64:
				case TypeCode.UInt64:
					il.Emit(callVirt, writeLong); // writer.Write(value);
					break;
				case TypeCode.Single:
					il.Emit(callVirt, writeFloat); // writer.Write(value);
					break;
				case TypeCode.Double:
					il.Emit(callVirt, writeDouble); // writer.Write(value);
					break;
				default:
					throw new NotSupportedException("Unknown primitive type " + fieldType);
			}
		}

		/// <summary>
		/// Stack transition: ..., value => ...
		/// </summary>
		void EmitWriteValueType(ILGenerator il, LocalBuilder writer, Type valType)
		{
			Debug.Assert(valType.IsValueType);
			Debug.Assert(!(valType.IsEnum || valType.IsPrimitive));
			
			var fieldVal = il.DeclareLocal(valType);
			il.Emit(OpCodes.Stloc, fieldVal);
			
			foreach (FieldInfo field in GetSerializableFields(valType)) {
				EmitWriteField(il, writer, fieldVal, field);
			}
		}
		#endregion

		StreamingContext streamingContext = new StreamingContext(StreamingContextStates.All);
		FormatterConverter formatterConverter = new FormatterConverter();

		public void Serialize(Stream stream, object instance)
		{
			Serialize(new BinaryWriterWith7BitEncodedInts(stream), instance);
		}

		public void Serialize(BinaryWriter writer, object instance)
		{
			SerializationContext context = new SerializationContext(this, writer);
			context.MarkFixedInstances(this.FixedInstances);
			context.Mark(instance);
			context.Scan();
			context.ScanTypes();
			context.Write();
			context.WriteObjectID(instance);
		}

		delegate void TypeSerializer(object instance, SerializationContext context);
		#endregion

		#region Deserialization
		sealed class DeserializationContext
		{
			public Type[] Types; // index: type ID
			
			public object[] Objects; // index: object ID
			
			public BinaryReader Reader;
			
			public object ReadObject()
			{
				if (this.Objects.Length <= ushort.MaxValue)
					return this.Objects[Reader.ReadUInt16()];
				else
					return this.Objects[Reader.ReadInt32()];
			}
			
			#region DeserializeTypeDescriptions
			internal int ReadTypeID()
			{
				if (this.Types.Length <= ushort.MaxValue)
					return Reader.ReadUInt16();
				else
					return Reader.ReadInt32();
			}
			
			internal void DeserializeTypeDescriptions()
			{
				for (int i = 0; i < this.Types.Length; i++) {
					Type type = this.Types[i];
					if (type.IsGenericTypeDefinition || type.HasElementType)
						continue;
					int versionNumber = Reader.ReadInt32();
					if (versionNumber != FastSerializerVersionAttribute.GetVersionNumber(type))
						throw new SerializationException("Type '" + type.FullName + "' was serialized with version " + versionNumber + ", but is version " + FastSerializerVersionAttribute.GetVersionNumber(type));
					
					bool isCustomSerialization = typeof(ISerializable).IsAssignableFrom(type);
					bool typeIsSpecial = type.IsPrimitive || isCustomSerialization;
					
					byte serializedFieldCount = Reader.ReadByte();
					if (serializedFieldCount == byte.MaxValue) {
						// special type
						if (!typeIsSpecial)
							throw new SerializationException("Type '" + type.FullName + "' was serialized as special type, but isn't special now.");
					} else {
						if (typeIsSpecial)
							throw new SerializationException("Type '" + type.FullName + "' wasn't serialized as special type, but is special now.");
						
						var availableFields = GetSerializableFields(this.Types[i]);
						if (availableFields.Count != serializedFieldCount)
							throw new SerializationException("Number of fields on " + type.FullName + " has changed.");
						for (int j = 0; j < serializedFieldCount; j++) {
							int fieldTypeID = ReadTypeID();
							
							string fieldName = Reader.ReadString();
							FieldInfo fieldInfo = availableFields[j];
							if (fieldInfo.Name != fieldName)
								throw new SerializationException("Field mismatch on type " + type.FullName);
							if (fieldInfo.FieldType != this.Types[fieldTypeID])
								throw new SerializationException(type.FullName + "." + fieldName + " was serialized as " + this.Types[fieldTypeID] + ", but now is " + fieldInfo.FieldType);
						}
					}
				}
			}
			#endregion
		}
		
		delegate void ObjectReader(DeserializationContext context, object instance);
		
		public object Deserialize(Stream stream)
		{
			return Deserialize(new BinaryReaderWith7BitEncodedInts(stream));
		}
		
		public object Deserialize(BinaryReader reader)
		{
			if (reader.ReadInt32() != magic)
				throw new SerializationException("The data cannot be read by FastSerializer (unknown magic value)");
			
			DeserializationContext context = new DeserializationContext();
			context.Reader = reader;
			context.Objects = new object[reader.ReadInt32()];
			context.Types = new Type[reader.ReadInt32()];
			string[] assemblyNames = new string[reader.ReadInt32()];
			int fixedInstanceCount = reader.ReadInt32();
			
			if (fixedInstanceCount != 0) {
				if (this.FixedInstances == null || this.FixedInstances.Length != fixedInstanceCount)
					throw new SerializationException("Number of fixed instances doesn't match");
				for (int i = 0; i < fixedInstanceCount; i++) {
					context.Objects[i + 1] = this.FixedInstances[i];
				}
			}
			
			for (int i = 0; i < assemblyNames.Length; i++) {
				assemblyNames[i] = reader.ReadString();
			}
			int stringTypeID = -1;
			for (int i = 0; i < context.Types.Length; i++) {
				byte typeKind = reader.ReadByte();
				switch (typeKind) {
					case Type_ReferenceType:
					case Type_ValueType:
						int assemblyID;
						if (assemblyNames.Length <= ushort.MaxValue)
							assemblyID = reader.ReadUInt16();
						else
							assemblyID = reader.ReadInt32();
						string assemblyName = assemblyNames[assemblyID];
						string typeName = reader.ReadString();
						Type type;
						if (SerializationBinder != null) {
							type = SerializationBinder.BindToType(assemblyName, typeName);
						} else {
							type = Assembly.Load(assemblyName).GetType(typeName);
						}
						if (type == null)
							throw new SerializationException("Could not find '" + typeName + "' in '" + assemblyName + "'");
						if (typeKind == Type_ValueType && !type.IsValueType)
							throw new SerializationException("Expected '" + typeName + "' to be a value type, but it is reference type");
						if (typeKind == Type_ReferenceType && type.IsValueType)
							throw new SerializationException("Expected '" + typeName + "' to be a reference type, but it is value type");
						context.Types[i] = type;
						if (type == typeof(string))
							stringTypeID = i;
						break;
					case Type_SZArray:
						context.Types[i] = context.Types[context.ReadTypeID()].MakeArrayType();
						break;
					case Type_ParameterizedType:
						Type genericType = context.Types[context.ReadTypeID()];
						int typeParameterCount = genericType.GetGenericArguments().Length;
						Type[] typeArguments = new Type[typeParameterCount];
						for (int j = 0; j < typeArguments.Length; j++) {
							typeArguments[j] = context.Types[context.ReadTypeID()];
						}
						context.Types[i] = genericType.MakeGenericType(typeArguments);
						break;
					default:
						throw new SerializationException("Unknown type kind");
				}
			}
			context.DeserializeTypeDescriptions();
			int[] typeIDByObjectID = new int[context.Objects.Length];
			for (int i = 1 + fixedInstanceCount; i < context.Objects.Length; i++) {
				int typeID = context.ReadTypeID();
				
				object instance;
				if (typeID == stringTypeID) {
					instance = reader.ReadString();
				} else {
					Type type = context.Types[typeID];
					if (type.IsArray) {
						int length = reader.ReadInt32();
						instance = Array.CreateInstance(type.GetElementType(), length);
					} else {
						instance = FormatterServices.GetUninitializedObject(type);
					}
				}
				context.Objects[i] = instance;
				typeIDByObjectID[i] = typeID;
			}
			List<CustomDeserialization> customDeserializatons = new List<CustomDeserialization>();
			ObjectReader[] objectReaders = new ObjectReader[context.Types.Length]; // index: type ID
			for (int i = 1 + fixedInstanceCount; i < context.Objects.Length; i++) {
				object instance = context.Objects[i];
				int typeID = typeIDByObjectID[i];
				Log("0x{2:x6} Read #{0}: {1}", i, context.Types[typeID].Name, reader.BaseStream.Position);
				ISerializable serializable = instance as ISerializable;
				if (serializable != null) {
					Type type = context.Types[typeID];
					SerializationInfo info = new SerializationInfo(type, formatterConverter);
					int count = reader.ReadInt32();
					for (int j = 0; j < count; j++) {
						string name = reader.ReadString();
						object val = context.ReadObject();
						info.AddValue(name, val);
					}
					CustomDeserializationAction action = GetCustomDeserializationAction(type);
					customDeserializatons.Add(new CustomDeserialization(instance, info, action));
				} else {
					ObjectReader objectReader = objectReaders[typeID];
					if (objectReader == null) {
						objectReader = GetReader(context.Types[typeID]);
						objectReaders[typeID] = objectReader;
					}
					objectReader(context, instance);
				}
			}
			Log("File was read successfully, now running {0} custom deserializations...", customDeserializatons.Count);
			foreach (CustomDeserialization customDeserializaton in customDeserializatons) {
				customDeserializaton.Run(streamingContext);
			}
			for (int i = 1 + fixedInstanceCount; i < context.Objects.Length; i++) {
				IDeserializationCallback dc = context.Objects[i] as IDeserializationCallback;
				if (dc != null)
					dc.OnDeserialization(null);
			}
			
			return context.ReadObject();
		}
		
		#region Object Reader
		static readonly FieldInfo readerField = typeof(DeserializationContext).GetField("Reader");
		static readonly MethodInfo readObject = typeof(DeserializationContext).GetMethod("ReadObject");
		
		static readonly MethodInfo readByte = typeof(BinaryReader).GetMethod("ReadByte");
		static readonly MethodInfo readShort = typeof(BinaryReader).GetMethod("ReadInt16");
		static readonly MethodInfo readInt = typeof(BinaryReader).GetMethod("ReadInt32");
		static readonly MethodInfo readLong = typeof(BinaryReader).GetMethod("ReadInt64");
		static readonly MethodInfo readFloat = typeof(BinaryReader).GetMethod("ReadSingle");
		static readonly MethodInfo readDouble = typeof(BinaryReader).GetMethod("ReadDouble");
		
		Dictionary<Type, ObjectReader> readers = new Dictionary<Type, ObjectReader>();

		ObjectReader GetReader(Type type)
		{
			ObjectReader reader;
			if (!readers.TryGetValue(type, out reader)) {
				reader = CreateReader(type);
				readers.Add(type, reader);
			}
			return reader;
		}
		
		ObjectReader CreateReader(Type type)
		{
			if (type == typeof(string)) {
				// String contents are written in the object creation section,
				// not into the field value section; so there's nothing to read here.
				return delegate {};
			}
			bool isArray = type.IsArray;
			if (isArray) {
				if (type.GetArrayRank() != 1)
					throw new NotSupportedException();
				type = type.GetElementType();
				if (!type.IsValueType) {
					return delegate (DeserializationContext context, object arrayInstance) {
						object[] array = (object[])arrayInstance;
						for (int i = 0; i < array.Length; i++) {
							array[i] = context.ReadObject();
						}
					};
				} else if (type == typeof(byte)) {
					return delegate (DeserializationContext context, object arrayInstance) {
						byte[] array = (byte[])arrayInstance;
						BinaryReader binaryReader = context.Reader;
						int pos = 0;
						int bytesRead;
						do {
							bytesRead = binaryReader.Read(array, pos, array.Length - pos);
							pos += bytesRead;
						} while (bytesRead > 0);
						if (pos != array.Length)
							throw new EndOfStreamException();
					};
				}
			}
			var fields = GetSerializableFields(type);
			if (fields.Count == 0) {
				// The reader has nothing to do for this object.
				return delegate { };
			}
			
			DynamicMethod dynamicMethod = new DynamicMethod(
				(isArray ? "ReadArray_" : "Read_") + type.Name,
				MethodAttributes.Public | MethodAttributes.Static,
				CallingConventions.Standard,
				typeof(void), new [] { typeof(DeserializationContext), typeof(object) },
				type,
				true);
			ILGenerator il = dynamicMethod.GetILGenerator();
			
			var reader = il.DeclareLocal(typeof(BinaryReader));
			
			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Ldfld, readerField);
			il.Emit(OpCodes.Stloc, reader); // reader = context.reader;
			
			if (isArray) {
				var instance = il.DeclareLocal(type.MakeArrayType());
				il.Emit(OpCodes.Ldarg_1);
				il.Emit(OpCodes.Castclass, type.MakeArrayType());
				il.Emit(OpCodes.Stloc, instance); // instance = (type[])arg_1;
				
				// for (int i = 0; i < instance.Length; i++) read &instance[i];
				
				var loopStart = il.DefineLabel();
				var loopHead = il.DefineLabel();
				var loopVariable = il.DeclareLocal(typeof(int));
				il.Emit(OpCodes.Ldc_I4_0);
				il.Emit(OpCodes.Stloc, loopVariable); // loopVariable = 0
				il.Emit(OpCodes.Br, loopHead); // goto loopHead;
				
				il.MarkLabel(loopStart);
				
				if (type.IsEnum || type.IsPrimitive) {
					if (type.IsEnum) {
						type = type.GetEnumUnderlyingType();
					}
					Debug.Assert(type.IsPrimitive);
					il.Emit(OpCodes.Ldloc, instance); // instance
					il.Emit(OpCodes.Ldloc, loopVariable); // instance, loopVariable
					ReadPrimitiveValue(il, reader, type); // instance, loopVariable, value
					switch (Type.GetTypeCode(type)) {
						case TypeCode.Boolean:
						case TypeCode.SByte:
						case TypeCode.Byte:
							il.Emit(OpCodes.Stelem_I1); // instance[loopVariable] = value;
							break;
						case TypeCode.Char:
						case TypeCode.Int16:
						case TypeCode.UInt16:
							il.Emit(OpCodes.Stelem_I2); // instance[loopVariable] = value;
							break;
						case TypeCode.Int32:
						case TypeCode.UInt32:
							il.Emit(OpCodes.Stelem_I4); // instance[loopVariable] = value;
							break;
						case TypeCode.Int64:
						case TypeCode.UInt64:
							il.Emit(OpCodes.Stelem_I8); // instance[loopVariable] = value;
							break;
						case TypeCode.Single:
							il.Emit(OpCodes.Stelem_R4); // instance[loopVariable] = value;
							break;
						case TypeCode.Double:
							il.Emit(OpCodes.Stelem_R8); // instance[loopVariable] = value;
							break;
						default:
							throw new NotSupportedException("Unknown primitive type " + type);
					}
				} else {
					il.Emit(OpCodes.Ldloc, instance); // instance
					il.Emit(OpCodes.Ldloc, loopVariable); // instance, loopVariable
					il.Emit(OpCodes.Ldelema, type); // instance[loopVariable]
					EmitReadValueType(il, reader, type);
				}
				
				il.Emit(OpCodes.Ldloc, loopVariable); // loopVariable
				il.Emit(OpCodes.Ldc_I4_1); // loopVariable, 1
				il.Emit(OpCodes.Add); // loopVariable+1
				il.Emit(OpCodes.Stloc, loopVariable); // loopVariable++;
				
				il.MarkLabel(loopHead);
				il.Emit(OpCodes.Ldloc, loopVariable); // loopVariable
				il.Emit(OpCodes.Ldloc, instance); // loopVariable, instance
				il.Emit(OpCodes.Ldlen); // loopVariable, instance.Length
				il.Emit(OpCodes.Conv_I4);
				il.Emit(OpCodes.Blt, loopStart); // if (loopVariable < instance.Length) goto loopStart;
			} else if (type.IsValueType) {
				// boxed value type
				il.Emit(OpCodes.Ldarg_1); // instance
				il.Emit(OpCodes.Unbox, type); // &(Type)instance
				if (type.IsEnum || type.IsPrimitive) {
					if (type.IsEnum) {
						type = type.GetEnumUnderlyingType();
					}
					Debug.Assert(type.IsPrimitive);
					ReadPrimitiveValue(il, reader, type); // &(Type)instance, value
					switch (Type.GetTypeCode(type)) {
						case TypeCode.Boolean:
						case TypeCode.SByte:
						case TypeCode.Byte:
							il.Emit(OpCodes.Stind_I1);
							break;
						case TypeCode.Char:
						case TypeCode.Int16:
						case TypeCode.UInt16:
							il.Emit(OpCodes.Stind_I2);
							break;
						case TypeCode.Int32:
						case TypeCode.UInt32:
							il.Emit(OpCodes.Stind_I4);
							break;
						case TypeCode.Int64:
						case TypeCode.UInt64:
							il.Emit(OpCodes.Stind_I8);
							break;
						case TypeCode.Single:
							il.Emit(OpCodes.Stind_R4);
							break;
						case TypeCode.Double:
							il.Emit(OpCodes.Stind_R8);
							break;
						default:
							throw new NotSupportedException("Unknown primitive type " + type);
					}
				} else {
					EmitReadValueType(il, reader, type);
				}
			} else {
				// reference type
				var instance = il.DeclareLocal(type);
				il.Emit(OpCodes.Ldarg_1);
				il.Emit(OpCodes.Castclass, type);
				il.Emit(OpCodes.Stloc, instance); // instance = (type)arg_1;
				
				foreach (FieldInfo field in fields) {
					EmitReadField(il, reader, instance, field); // read instance.Field
				}
			}
			il.Emit(OpCodes.Ret);
			return (ObjectReader)dynamicMethod.CreateDelegate(typeof(ObjectReader));
		}

		void EmitReadField(ILGenerator il, LocalBuilder reader, LocalBuilder instance, FieldInfo field)
		{
			Type fieldType = field.FieldType;
			if (fieldType.IsValueType) {
				if (fieldType.IsPrimitive || fieldType.IsEnum) {
					il.Emit(OpCodes.Ldloc, instance); // instance
					ReadPrimitiveValue(il, reader, fieldType); // instance, value
					il.Emit(OpCodes.Stfld, field); // instance.field = value;
				} else {
					il.Emit(OpCodes.Ldloc, instance); // instance
					il.Emit(OpCodes.Ldflda, field); // &instance.field
					EmitReadValueType(il, reader, fieldType);
				}
			} else {
				il.Emit(OpCodes.Ldloc, instance); // instance
				il.Emit(OpCodes.Ldarg_0); // instance, context
				il.Emit(OpCodes.Call, readObject); // instance, context.ReadObject()
				il.Emit(OpCodes.Castclass, fieldType);
				il.Emit(OpCodes.Stfld, field); // instance.field = (fieldType) context.ReadObject();
			}
		}

		/// <summary>
		/// Reads a primitive value of the specified type.
		/// Stack transition: ... => ..., value
		/// </summary>
		void ReadPrimitiveValue(ILGenerator il, LocalBuilder reader, Type fieldType)
		{
			if (fieldType.IsEnum) {
				fieldType = fieldType.GetEnumUnderlyingType();
				Debug.Assert(fieldType.IsPrimitive);
			}
			il.Emit(OpCodes.Ldloc, reader);
			switch (Type.GetTypeCode(fieldType)) {
				case TypeCode.Boolean:
				case TypeCode.SByte:
				case TypeCode.Byte:
					il.Emit(callVirt, readByte);
					break;
				case TypeCode.Char:
				case TypeCode.Int16:
				case TypeCode.UInt16:
					il.Emit(callVirt, readShort);
					break;
				case TypeCode.Int32:
				case TypeCode.UInt32:
					il.Emit(callVirt, readInt);
					break;
				case TypeCode.Int64:
				case TypeCode.UInt64:
					il.Emit(callVirt, readLong);
					break;
				case TypeCode.Single:
					il.Emit(callVirt, readFloat);
					break;
				case TypeCode.Double:
					il.Emit(callVirt, readDouble);
					break;
				default:
					throw new NotSupportedException("Unknown primitive type " + fieldType);
			}
		}

		/// <summary>
		/// Stack transition: ..., field-ref => ...
		/// </summary>
		void EmitReadValueType(ILGenerator il, LocalBuilder reader, Type valType)
		{
			Debug.Assert(valType.IsValueType);
			Debug.Assert(!(valType.IsEnum || valType.IsPrimitive));
			
			var fieldRef = il.DeclareLocal(valType.MakeByRefType());
			il.Emit(OpCodes.Stloc, fieldRef);
			
			foreach (FieldInfo field in GetSerializableFields(valType)) {
				EmitReadField(il, reader, fieldRef, field);
			}
		}
		#endregion
		
		#region Custom Deserialization
		struct CustomDeserialization
		{
			readonly object instance;
			readonly SerializationInfo serializationInfo;
			readonly CustomDeserializationAction action;
			
			public CustomDeserialization(object instance, SerializationInfo serializationInfo, CustomDeserializationAction action)
			{
				this.instance = instance;
				this.serializationInfo = serializationInfo;
				this.action = action;
			}
			
			public void Run(StreamingContext context)
			{
				action(instance, serializationInfo, context);
			}
		}
		
		delegate void CustomDeserializationAction(object instance, SerializationInfo info, StreamingContext context);
		
		Dictionary<Type, CustomDeserializationAction> customDeserializationActions = new Dictionary<Type, CustomDeserializationAction>();
		
		CustomDeserializationAction GetCustomDeserializationAction(Type type)
		{
			CustomDeserializationAction action;
			if (!customDeserializationActions.TryGetValue(type, out action)) {
				action = CreateCustomDeserializationAction(type);
				customDeserializationActions.Add(type, action);
			}
			return action;
		}
		
		static CustomDeserializationAction CreateCustomDeserializationAction(Type type)
		{
			ConstructorInfo ctor = type.GetConstructor(
				BindingFlags.DeclaredOnly | BindingFlags.ExactBinding | BindingFlags.Instance
				| BindingFlags.NonPublic | BindingFlags.Public,
				null,
				new Type [] { typeof(SerializationInfo), typeof(StreamingContext) },
				null);
			if (ctor == null)
				throw new SerializationException("Could not find deserialization constructor for " + type.FullName);
			
			DynamicMethod dynamicMethod = new DynamicMethod(
				"CallCtor_" + type.Name,
				MethodAttributes.Public | MethodAttributes.Static,
				CallingConventions.Standard,
				typeof(void), new [] { typeof(object), typeof(SerializationInfo), typeof(StreamingContext) },
				type,
				true);
			ILGenerator il = dynamicMethod.GetILGenerator();
			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Ldarg_1);
			il.Emit(OpCodes.Ldarg_2);
			il.Emit(OpCodes.Call, ctor);
			il.Emit(OpCodes.Ret);
			return (CustomDeserializationAction)dynamicMethod.CreateDelegate(typeof(CustomDeserializationAction));
		}
		#endregion
		#endregion
		
		[Conditional("DEBUG_SERIALIZER")]
		static void Log(string format, params object[] args)
		{
			Debug.WriteLine(format, args);
		}
	}
	
	/// <summary>
	/// Specifies the version of the class.
	/// The <see cref="FastSerializer"/> will refuse to deserialize an instance that was stored by
	/// a different version of the class than the current one.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum)]
	public class FastSerializerVersionAttribute : Attribute
	{
		readonly int versionNumber;
		
		public FastSerializerVersionAttribute(int versionNumber)
		{
			this.versionNumber = versionNumber;
		}
		
		public int VersionNumber {
			get {
				return versionNumber;
			}
		}
		
		internal static int GetVersionNumber(Type type)
		{
			var arr = type.GetCustomAttributes(typeof(FastSerializerVersionAttribute), false);
			if (arr.Length == 0)
				return 0;
			else
				return ((FastSerializerVersionAttribute)arr[0]).VersionNumber;
		}
	}
}
