// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Reflection;

using Debugger.Interop.CorDebug;
using Debugger.MetaData;
using System.Runtime.InteropServices;

namespace Debugger
{
	public delegate Value ValueGetter(StackFrame context);
	
	/// <summary>
	/// Value class provides functions to examine value in the debuggee.
	/// It has very life-time.  In general, value dies whenever debugger is
	/// resumed (this includes method invocation and property evaluation).
	/// You can use Expressions to reobtain the value.
	/// </summary>
	public class Value: DebuggerObject
	{
		AppDomain      appDomain;
		ICorDebugValue corValue;
		PauseSession   corValue_pauseSession;
		DebugType      type;
		
		// Permanently stored as convinience so that it survives Continue
		bool           isNull;
		
		/// <summary> The appdomain that owns the value </summary>
		public AppDomain AppDomain {
			get { return appDomain; }
		}
		
		public Process Process {
			get { return appDomain.Process; }
		}
		
		[Debugger.Tests.Ignore]
		public ICorDebugValue CorValue {
			get {
				if (this.IsInvalid)
					throw new GetValueException("Value is no longer valid");
				return corValue;
			}
		}
		
		[Debugger.Tests.Ignore]
		public ICorDebugReferenceValue CorReferenceValue {
			get {
				if (IsNull) throw new GetValueException("Value is null");
				
				if (!(this.CorValue is ICorDebugReferenceValue))
					throw new DebuggerException("Reference value expected");
				
				return (ICorDebugReferenceValue)this.CorValue;
			}
		}
		
		[Debugger.Tests.Ignore]
		public ICorDebugGenericValue CorGenericValue {
			get {
				if (IsNull) throw new GetValueException("Value is null");
				
				ICorDebugValue corValue = this.CorValue;
				// Dereference and unbox if necessary
				if (corValue is ICorDebugReferenceValue)
					corValue = ((ICorDebugReferenceValue)corValue).Dereference();
				if (corValue is ICorDebugBoxValue)
					corValue = ((ICorDebugBoxValue)corValue).GetObject();
				if (!(corValue is ICorDebugGenericValue))
					throw new DebuggerException("Value is not an generic value");
				return (ICorDebugGenericValue)corValue;
			}
		}
		
		[Debugger.Tests.Ignore]
		public ICorDebugArrayValue CorArrayValue {
			get {
				if (IsNull) throw new GetValueException("Value is null");
				
				if (!this.Type.IsArray) throw new DebuggerException("Value is not an array");
				
				return (ICorDebugArrayValue)this.CorReferenceValue.Dereference();
			}
		}
		
		[Debugger.Tests.Ignore]
		public ICorDebugObjectValue CorObjectValue {
			get {
				if (IsNull) throw new GetValueException("Value is null");
				
				ICorDebugValue corValue = this.CorValue;
				// Dereference and unbox if necessary
				if (corValue is ICorDebugReferenceValue)
					corValue = ((ICorDebugReferenceValue)corValue).Dereference();
				if (corValue is ICorDebugBoxValue)
					return ((ICorDebugBoxValue)corValue).GetObject();
				if (!(corValue is ICorDebugObjectValue))
					throw new DebuggerException("Value is not an object");
				return (ICorDebugObjectValue)corValue;
			}
		}
		
		/// <summary> Returns the <see cref="Debugger.DebugType"/> of the value </summary>
		public DebugType Type {
			get { return type; }
		}
		
		/// <summary> Returns true if the Value can not be used anymore.
		/// Value is valid only until the debuggee is resummed. </summary>
		public bool IsInvalid {
			get {
				return corValue_pauseSession != this.Process.PauseSession &&
					!(corValue is ICorDebugHandleValue);
			}
		}
		
		/// <summary> Gets value indication whether the value is a reference </summary>
		/// <remarks> Value types also return true if they are boxed </remarks>
		public bool IsReference {
			get {
				return this.CorValue is ICorDebugReferenceValue;
			}
		}
		
		/// <summary> Returns true if the value is null </summary>
		public bool IsNull {
			get { return isNull; }
		}
		
		/// <summary>
		/// Gets the address in memory where this value is stored
		/// </summary>
		[Debugger.Tests.Ignore]
		public ulong Address {
			get { return corValue.GetAddress(); }
		}
		
		[Debugger.Tests.Ignore]
		public ulong PointerAddress {
			get {
				if (!(this.CorValue is ICorDebugReferenceValue))
					throw new DebuggerException("Not a pointer");
				return ((ICorDebugReferenceValue)this.CorValue).GetValue();
			}
		}
		
		/// <summary> Gets a string representation of the value </summary>
		/// <param name="maxLength">
		/// The maximum length of the result string.
		/// </param>
		public string AsString(int maxLength = int.MaxValue)
		{
			if (this.IsNull) return "null";
			if (this.Type.IsPrimitive || this.Type.FullName == typeof(string).FullName) {
				string text = PrimitiveValue.ToString();
				if (text != null && text.Length > maxLength)
					text = text.Substring(0, Math.Max(0, maxLength - 3)) + "...";
				return text;
			} else {
				string name = this.Type.FullName;
				if (name != null && name.Length > maxLength)
					return "{" + name.Substring(0, Math.Max(0, maxLength - 5)) + "...}";
				else
					return "{" + name + "}";
			}
		}
		
		internal Value(AppDomain appDomain, ICorDebugValue corValue)
		{
			if (corValue == null)
				throw new ArgumentNullException("corValue");
			this.appDomain = appDomain;
			this.corValue = corValue;
			this.corValue_pauseSession = this.Process.PauseSession;
			
			this.isNull = corValue is ICorDebugReferenceValue && ((ICorDebugReferenceValue)corValue).IsNull() != 0;
			
			if (corValue is ICorDebugReferenceValue &&
			    ((ICorDebugReferenceValue)corValue).GetValue() == 0 &&
			    ((ICorDebugValue2)corValue).GetExactType() == null)
			{
				// We were passed null reference and no metadata description
				// (happens during CreateThread callback for the thread object)
				this.type = appDomain.ObjectType;
			} else {
				ICorDebugType exactType = ((ICorDebugValue2)this.CorValue).GetExactType();
				this.type = DebugType.CreateFromCorType(appDomain, exactType);
			}
		}
		
		// Box value type
		public Value Box()
		{
			byte[] rawValue = this.CorGenericValue.GetRawValue();
			// The type must not be a primive type (always true in current design)
			ICorDebugReferenceValue corRefValue = Eval.NewObjectNoConstructor(this.Type).CorReferenceValue;
			// Make the reference to box permanent
			corRefValue = ((ICorDebugHeapValue2)corRefValue.Dereference()).CreateHandle(CorDebugHandleType.HANDLE_STRONG);
			// Create new value
			Value newValue = new Value(appDomain, corRefValue);
			// Copy the data inside the box
			newValue.CorGenericValue.SetRawValue(rawValue);
			return newValue;
		}
		
		[Debugger.Tests.Ignore]
		public Value GetPermanentReference()
		{
			if (this.CorValue is ICorDebugHandleValue) {
				return this;
			} else if (this.CorValue is ICorDebugReferenceValue) {
				if (this.IsNull)
					return this; // ("null" expression) It isn't permanent
				ICorDebugValue deRef = this.CorReferenceValue.Dereference();
				if (deRef is ICorDebugHeapValue2) {
					return new Value(appDomain, ((ICorDebugHeapValue2)deRef).CreateHandle(CorDebugHandleType.HANDLE_STRONG));
				} else {
					// For exampe int* is a refernce not pointing to heap
					// TODO: It isn't permanent
					return this;
				}
			} else {
				return this.Box();
			}
		}
		
		/// <summary> Dereferences a pointer type </summary>
		/// <returns> Returns null for a null pointer </returns>
		public Value Dereference()
		{
			if (!this.Type.IsPointer) throw new DebuggerException("Not a pointer");
			ICorDebugReferenceValue corRef = (ICorDebugReferenceValue)this.CorValue;
			if (corRef.GetValue() == 0 || corRef.Dereference() == null) {
				return null;
			} else {
				return new Value(this.AppDomain, corRef.Dereference());
			}
		}
		
		/// <summary> Copy the acutal value from some other Value object </summary>
		public void SetValue(Value newValue)
		{
			ICorDebugValue newCorValue = newValue.CorValue;
			
			if (this.CorValue is ICorDebugReferenceValue) {
				if (!(newCorValue is ICorDebugReferenceValue))
					newCorValue = newValue.Box().CorValue;
				((ICorDebugReferenceValue)this.CorValue).SetValue(((ICorDebugReferenceValue)newCorValue).GetValue());
			} else {
				this.CorGenericValue.SetRawValue(newValue.CorGenericValue.GetRawValue());
			}
		}
		
		#region Primitive
		
		/// <summary>
		/// Gets or sets the value of a primitive type.
		/// 
		/// If setting of a value fails, NotSupportedException is thrown.
		/// </summary>
		public object PrimitiveValue {
			get {
				if (this.Type.FullName == typeof(string).FullName) {
					if (this.IsNull) return null;
					return ((ICorDebugStringValue)this.CorReferenceValue.Dereference()).GetString();
				} else {
					if (this.Type.PrimitiveType == null)
						throw new DebuggerException("Value is not a primitive type");
					return CorGenericValue.GetValue(this.Type.PrimitiveType);
				}
			}
			set {
				if (this.Type.FullName == typeof(string).FullName) {
					this.SetValue(Eval.NewString(this.AppDomain, value.ToString()));
				} else {
					if (this.Type.PrimitiveType == null)
						throw new DebuggerException("Value is not a primitive type");
					if (value == null)
						throw new DebuggerException("Can not set primitive value to null");
					object newValue;
					try {
						newValue = Convert.ChangeType(value, this.Type.PrimitiveType);
					} catch {
						throw new NotSupportedException("Can not convert " + value.GetType().ToString() + " to " + this.Type.PrimitiveType.ToString());
					}
					CorGenericValue.SetValue(newValue);
				}
			}
		}
		
		#endregion
		
		#region Array
		
		/// <summary>
		/// Gets the number of elements in the array.
		/// eg new object[4,5] returns 20
		/// </summary>
		/// <returns> 0 for non-arrays </returns>
		public int ArrayLength {
			get {
				if (!this.Type.IsArray) return 0;
				return (int)CorArrayValue.GetCount();
			}
		}
		
		/// <summary>
		/// Gets the number of dimensions of the array.
		/// eg new object[4,5] returns 2
		/// </summary>
		/// <returns> 0 for non-arrays </returns>
		public int ArrayRank {
			get {
				if (!this.Type.IsArray) return 0;
				return (int)CorArrayValue.GetRank();
			}
		}
		
		/// <summary> Gets the dimensions of the array  </summary>
		/// <returns> null for non-arrays </returns>
		public ArrayDimensions ArrayDimensions {
			get {
				if (!this.Type.IsArray) return null;
				int rank = this.ArrayRank;
				uint[] baseIndicies;
				if (CorArrayValue.HasBaseIndicies() == 1) {
					baseIndicies = CorArrayValue.GetBaseIndicies();
				} else {
					baseIndicies = new uint[this.ArrayRank];
				}
				uint[] dimensionCounts = CorArrayValue.GetDimensions();
				
				List<ArrayDimension> dimensions = new List<ArrayDimension>();
				for(int i = 0; i < rank; i++) {
					dimensions.Add(new ArrayDimension((int)baseIndicies[i], (int)baseIndicies[i] + (int)dimensionCounts[i] - 1));
				}
				
				return new ArrayDimensions(dimensions);
			}
		}
		
		/// <summary> Returns an element of a single-dimensional array </summary>
		public Value GetArrayElement(int index)
		{
			return GetArrayElement(new int[] {index});
		}
		
		/// <summary> Returns an element of an array </summary>
		public Value GetArrayElement(int[] elementIndices)
		{
			int[] indices = (int[])elementIndices.Clone();
			
			return new Value(this.AppDomain, GetCorValueOfArrayElement(indices));
		}
		
		// May be called later
		ICorDebugValue GetCorValueOfArrayElement(int[] indices)
		{
			if (indices.Length != ArrayRank) {
				throw new GetValueException("Given indicies do not have the same dimension as array.");
			}
			if (!this.ArrayDimensions.IsIndexValid(indices)) {
				throw new GetValueException("Given indices are out of range of the array");
			}
			
			return CorArrayValue.GetElement(indices);
		}
		
		public void SetArrayElement(int[] elementIndices, Value newVal)
		{
			Value elem = GetArrayElement(elementIndices);
			elem.SetValue(newVal);
		}
		
		/// <summary> Returns all elements in the array </summary>
		public Value[] GetArrayElements()
		{
			if (!this.Type.IsArray) return null;
			List<Value> values = new List<Value>();
			foreach(int[] indices in this.ArrayDimensions.Indices) {
				values.Add(GetArrayElement(indices));
			}
			return values.ToArray();
		}
		
		#endregion
		
		#region Object
		
		static void CheckObject(Value objectInstance, MemberInfo memberInfo)
		{
			if (memberInfo == null)
				throw new DebuggerException("memberInfo");
			IDebugMemberInfo debugMemberInfo = memberInfo as IDebugMemberInfo;
			if (debugMemberInfo == null)
				throw new DebuggerException("DebugMemberInfo must be used");
			if (!debugMemberInfo.IsStatic) {
				if (objectInstance == null)
					throw new DebuggerException("No target object specified");
				if (objectInstance.IsNull)
					throw new GetValueException("Null reference");
				//if (!objectInstance.IsObject) // eg Array.Length can be called
				if (!debugMemberInfo.DeclaringType.IsInstanceOfType(objectInstance))
					throw new GetValueException("Object is not of type " + debugMemberInfo.DeclaringType.FullName);
			}
		}
		
		#region Convenience overload methods
		
		/// <summary> Get a field or property of an object with a given name. </summary>
		/// <returns> Null if not found </returns>
		public Value GetMemberValue(string name)
		{
			MemberInfo memberInfo = this.Type.GetMember<MemberInfo>(name, DebugType.BindingFlagsAllInScope, DebugType.IsFieldOrNonIndexedProperty);
			if (memberInfo == null)
				return null;
			return GetMemberValue(memberInfo);
		}
		
		/// <summary> Get the value of given member. </summary>
		public Value GetMemberValue(MemberInfo memberInfo, params Value[] arguments)
		{
			return GetMemberValue(this, memberInfo, arguments);
		}
		
		#endregion
		
		/// <summary> Get the value of given member. </summary>
		/// <param name="objectInstance">null if member is static</param>
		public static Value GetMemberValue(Value objectInstance, MemberInfo memberInfo, params Value[] arguments)
		{
			if (memberInfo is FieldInfo) {
				if (arguments.Length > 0)
					throw new GetValueException("Arguments can not be used for a field");
				return GetFieldValue(objectInstance, (FieldInfo)memberInfo);
			} else if (memberInfo is PropertyInfo) {
				return GetPropertyValue(objectInstance, (PropertyInfo)memberInfo, arguments);
			} else if (memberInfo is MethodInfo) {
				return InvokeMethod(objectInstance, (MethodInfo)memberInfo, arguments);
			}
			throw new DebuggerException("Unknown member type: " + memberInfo.GetType());
		}
		
		#region Convenience overload methods
		
		/// <summary> Get the value of given field. </summary>
		public Value GetFieldValue(FieldInfo fieldInfo)
		{
			return Value.GetFieldValue(this, fieldInfo);
		}
		
		#endregion
		
		public static void SetFieldValue(Value objectInstance, FieldInfo fieldInfo, Value newValue)
		{
			Value val = GetFieldValue(objectInstance, fieldInfo);
			if (!fieldInfo.FieldType.IsAssignableFrom(newValue.Type))
				throw new GetValueException("Can not assign {0} to {1}", newValue.Type.FullName, fieldInfo.FieldType.FullName);
			val.SetValue(newValue);
		}
		
		/// <summary> Get the value of given field. </summary>
		/// <param name="objectInstance">null if field is static</param>
		public static Value GetFieldValue(Value objectInstance, FieldInfo fieldInfo)
		{
			CheckObject(objectInstance, fieldInfo);
			
			if (fieldInfo.IsStatic && fieldInfo.IsLiteral) {
				return GetLiteralValue((DebugFieldInfo)fieldInfo);
			} else {
				return new Value(
					((DebugFieldInfo)fieldInfo).AppDomain,
					GetFieldCorValue(objectInstance, fieldInfo)
				);
			}
		}
		
		static ICorDebugValue GetFieldCorValue(Value objectInstance, FieldInfo fieldInfo)
		{
			Process process = ((DebugFieldInfo)fieldInfo).Process;
			
			// Current frame is used to resolve context specific static values (eg. ThreadStatic)
			ICorDebugFrame curFrame = null;
			if (process.IsPaused &&
			    process.SelectedThread != null &&
			    process.SelectedThread.MostRecentStackFrame != null &&
			    process.SelectedThread.MostRecentStackFrame.CorILFrame != null)
			{
				curFrame = process.SelectedThread.MostRecentStackFrame.CorILFrame;
			}
			
			try {
				if (fieldInfo.IsStatic) {
					return ((DebugType)fieldInfo.DeclaringType).CorType.GetStaticFieldValue((uint)fieldInfo.MetadataToken, curFrame);
				} else {
					return objectInstance.CorObjectValue.GetFieldValue(((DebugType)fieldInfo.DeclaringType).CorType.GetClass(), (uint)fieldInfo.MetadataToken);
				}
			} catch (COMException e) {
				throw new GetValueException("Can not get value of field", e);
			}
		}
		
		static Value GetLiteralValue(DebugFieldInfo fieldInfo)
		{
			CorElementType corElemType = (CorElementType)fieldInfo.FieldProps.ConstantType;
			if (corElemType == CorElementType.CLASS) {
				// Only null literals are allowed
				return Eval.CreateValue(fieldInfo.AppDomain, null);
			} else if (corElemType == CorElementType.STRING) {
				string str = Marshal.PtrToStringUni(fieldInfo.FieldProps.ConstantPtr, (int)fieldInfo.FieldProps.ConstantStringLength);
				return Eval.CreateValue(fieldInfo.AppDomain, str);
			} else {
				DebugType type = DebugType.CreateFromType(fieldInfo.AppDomain.Mscorlib, DebugType.CorElementTypeToManagedType(corElemType));
				if (fieldInfo.FieldType.IsEnum && fieldInfo.FieldType.GetEnumUnderlyingType() == type) {
					Value val = Eval.NewObjectNoConstructor((DebugType)fieldInfo.FieldType);
					Value backingField = val.GetMemberValue("value__");
					backingField.CorGenericValue.SetValue(fieldInfo.FieldProps.ConstantPtr);
					return val;
				} else {
					Value val = Eval.NewObjectNoConstructor(type);
					val.CorGenericValue.SetValue(fieldInfo.FieldProps.ConstantPtr);
					return val;
				}
			}
		}
		
		#region Convenience overload methods
		
		/// <summary> Get the value of the property using the get accessor </summary>
		public Value GetPropertyValue(PropertyInfo propertyInfo, params Value[] arguments)
		{
			return GetPropertyValue(this, propertyInfo, arguments);
		}
		
		#endregion
		
		/// <summary> Get the value of the property using the get accessor </summary>
		public static Value GetPropertyValue(Value objectInstance, PropertyInfo propertyInfo, params Value[] arguments)
		{
			CheckObject(objectInstance, propertyInfo);
			
			if (propertyInfo.GetGetMethod() == null) throw new GetValueException("Property does not have a get method");
			
			Value val = Value.InvokeMethod(objectInstance, (DebugMethodInfo)propertyInfo.GetGetMethod(), arguments);
			
			return val;
		}
		
		#region Convenience overload methods
		
		/// <summary> Set the value of the property using the set accessor </summary>
		public Value SetPropertyValue(PropertyInfo propertyInfo, Value newValue)
		{
			return SetPropertyValue(this, propertyInfo, null, newValue);
		}
		
		/// <summary> Set the value of the property using the set accessor </summary>
		public Value SetPropertyValue(PropertyInfo propertyInfo, Value[] arguments, Value newValue)
		{
			return SetPropertyValue(this, propertyInfo, arguments, newValue);
		}
		
		/// <summary> Set the value of the property using the set accessor </summary>
		public static Value SetPropertyValue(Value objectInstance, PropertyInfo propertyInfo, Value newValue)
		{
			return SetPropertyValue(objectInstance, propertyInfo, null, newValue);
		}
		
		#endregion
		
		/// <summary> Set the value of the property using the set accessor </summary>
		public static Value SetPropertyValue(Value objectInstance, PropertyInfo propertyInfo, Value[] arguments, Value newValue)
		{
			CheckObject(objectInstance, propertyInfo);
			
			if (propertyInfo.GetSetMethod() == null) throw new GetValueException("Property does not have a set method");
			
			arguments = arguments ?? new Value[0];
			
			Value[] allParams = new Value[1 + arguments.Length];
			allParams[0] = newValue;
			arguments.CopyTo(allParams, 1);
			
			return Value.InvokeMethod(objectInstance, (DebugMethodInfo)propertyInfo.GetSetMethod(), allParams);
		}
		
		#region Convenience overload methods
		
		/// <summary> Synchronously invoke the method </summary>
		public Value InvokeMethod(MethodInfo methodInfo, params Value[] arguments)
		{
			return InvokeMethod(this, methodInfo, arguments);
		}
		
		#endregion
		
		/// <summary> Synchronously invoke the method </summary>
		public static Value InvokeMethod(Value objectInstance, MethodInfo methodInfo, params Value[] arguments)
		{
			CheckObject(objectInstance, methodInfo);
			
			return Eval.InvokeMethod(
				(DebugMethodInfo)methodInfo,
				methodInfo.IsStatic ? null : objectInstance,
				arguments ?? new Value[0]
			);
		}
		
		/// <summary> Invoke the ToString() method </summary>
		public string InvokeToString(int maxLength = int.MaxValue)
		{
			if (this.Type.IsPrimitive) return AsString(maxLength);
			if (this.Type.FullName == typeof(string).FullName) return AsString(maxLength);
			if (this.Type.IsPointer) return "0x" + this.PointerAddress.ToString("X");
			// if (!IsObject) // Can invoke on primitives
			DebugMethodInfo methodInfo = (DebugMethodInfo)this.AppDomain.ObjectType.GetMethod("ToString", new DebugType[] {});
			return Eval.InvokeMethod(methodInfo, this, new Value[] {}).AsString(maxLength);
		}
		
		#region Convenience overload methods
		
		/// <summary> Asynchronously invoke the method </summary>
		public Eval AsyncInvokeMethod(MethodInfo methodInfo, params Value[] arguments)
		{
			return AsyncInvokeMethod(this, methodInfo, arguments);
		}
		
		#endregion
		
		/// <summary> Asynchronously invoke the method </summary>
		public static Eval AsyncInvokeMethod(Value objectInstance, MethodInfo methodInfo, params Value[] arguments)
		{
			CheckObject(objectInstance, methodInfo);
			
			return Eval.AsyncInvokeMethod(
				(DebugMethodInfo)methodInfo,
				methodInfo.IsStatic ? null : objectInstance,
				arguments ?? new Value[0]
			);
		}
		
		#endregion
		
		public override string ToString()
		{
			return this.AsString();
		}
	}
}
