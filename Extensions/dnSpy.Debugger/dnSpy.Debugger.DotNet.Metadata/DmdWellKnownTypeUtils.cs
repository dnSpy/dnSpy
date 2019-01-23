/*
    Copyright (C) 2014-2019 de4dot@gmail.com

    This file is part of dnSpy

    dnSpy is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    dnSpy is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with dnSpy.  If not, see <http://www.gnu.org/licenses/>.
*/

using System.Collections.Generic;
using System.Diagnostics;

namespace dnSpy.Debugger.DotNet.Metadata {
	/// <summary>
	/// <see cref="DmdWellKnownType"/> utils
	/// </summary>
	public static class DmdWellKnownTypeUtils {
		static readonly Dictionary<DmdTypeName, DmdWellKnownType> toWellKnownType;
		static readonly DmdTypeName[] toWellKnownTypeName;

		/// <summary>
		/// Gets the well known type
		/// </summary>
		/// <param name="name">Name</param>
		/// <param name="wellKnownType">Updated with well known type if successful</param>
		/// <returns></returns>
		public static bool TryGetWellKnownType(in DmdTypeName name, out DmdWellKnownType wellKnownType) =>
			toWellKnownType.TryGetValue(name, out wellKnownType);

		/// <summary>
		/// Gets the name
		/// </summary>
		/// <param name="wellKnownType">Well known type</param>
		/// <returns></returns>
		public static DmdTypeName GetTypeName(DmdWellKnownType wellKnownType) {
			Debug.Assert(wellKnownType != DmdWellKnownType.None);
			return toWellKnownTypeName[(int)wellKnownType];
		}

		static void Add(DmdTypeName typeName, DmdWellKnownType wellKnownType) {
			Debug.Assert(wellKnownType != DmdWellKnownType.None);
			toWellKnownType.Add(typeName, wellKnownType);
			toWellKnownTypeName[(int)wellKnownType] = typeName;
		}

		/// <summary>
		/// Gets the number of well known types
		/// </summary>
		public static int WellKnownTypesCount => 306;

		static DmdWellKnownTypeUtils() {
			toWellKnownType = new Dictionary<DmdTypeName, DmdWellKnownType>(WellKnownTypesCount, DmdTypeNameEqualityComparer.Instance);
			toWellKnownTypeName = new DmdTypeName[WellKnownTypesCount];

			Add(new DmdTypeName("System", "Object"), DmdWellKnownType.System_Object);
			Add(new DmdTypeName("System", "Enum"), DmdWellKnownType.System_Enum);
			Add(new DmdTypeName("System", "MulticastDelegate"), DmdWellKnownType.System_MulticastDelegate);
			Add(new DmdTypeName("System", "Delegate"), DmdWellKnownType.System_Delegate);
			Add(new DmdTypeName("System", "ValueType"), DmdWellKnownType.System_ValueType);
			Add(new DmdTypeName("System", "Void"), DmdWellKnownType.System_Void);
			Add(new DmdTypeName("System", "Boolean"), DmdWellKnownType.System_Boolean);
			Add(new DmdTypeName("System", "Char"), DmdWellKnownType.System_Char);
			Add(new DmdTypeName("System", "SByte"), DmdWellKnownType.System_SByte);
			Add(new DmdTypeName("System", "Byte"), DmdWellKnownType.System_Byte);
			Add(new DmdTypeName("System", "Int16"), DmdWellKnownType.System_Int16);
			Add(new DmdTypeName("System", "UInt16"), DmdWellKnownType.System_UInt16);
			Add(new DmdTypeName("System", "Int32"), DmdWellKnownType.System_Int32);
			Add(new DmdTypeName("System", "UInt32"), DmdWellKnownType.System_UInt32);
			Add(new DmdTypeName("System", "Int64"), DmdWellKnownType.System_Int64);
			Add(new DmdTypeName("System", "UInt64"), DmdWellKnownType.System_UInt64);
			Add(new DmdTypeName("System", "Decimal"), DmdWellKnownType.System_Decimal);
			Add(new DmdTypeName("System", "Single"), DmdWellKnownType.System_Single);
			Add(new DmdTypeName("System", "Double"), DmdWellKnownType.System_Double);
			Add(new DmdTypeName("System", "String"), DmdWellKnownType.System_String);
			Add(new DmdTypeName("System", "IntPtr"), DmdWellKnownType.System_IntPtr);
			Add(new DmdTypeName("System", "UIntPtr"), DmdWellKnownType.System_UIntPtr);
			Add(new DmdTypeName("System", "Array"), DmdWellKnownType.System_Array);
			Add(new DmdTypeName("System.Collections", "IEnumerable"), DmdWellKnownType.System_Collections_IEnumerable);
			Add(new DmdTypeName("System.Collections.Generic", "IEnumerable`1"), DmdWellKnownType.System_Collections_Generic_IEnumerable_T);
			Add(new DmdTypeName("System.Collections.Generic", "IList`1"), DmdWellKnownType.System_Collections_Generic_IList_T);
			Add(new DmdTypeName("System.Collections.Generic", "ICollection`1"), DmdWellKnownType.System_Collections_Generic_ICollection_T);
			Add(new DmdTypeName("System.Collections", "IEnumerator"), DmdWellKnownType.System_Collections_IEnumerator);
			Add(new DmdTypeName("System.Collections.Generic", "IEnumerator`1"), DmdWellKnownType.System_Collections_Generic_IEnumerator_T);
			Add(new DmdTypeName("System.Collections.Generic", "IReadOnlyList`1"), DmdWellKnownType.System_Collections_Generic_IReadOnlyList_T);
			Add(new DmdTypeName("System.Collections.Generic", "IReadOnlyCollection`1"), DmdWellKnownType.System_Collections_Generic_IReadOnlyCollection_T);
			Add(new DmdTypeName("System", "Nullable`1"), DmdWellKnownType.System_Nullable_T);
			Add(new DmdTypeName("System", "DateTime"), DmdWellKnownType.System_DateTime);
			Add(new DmdTypeName("System.Runtime.CompilerServices", "IsVolatile"), DmdWellKnownType.System_Runtime_CompilerServices_IsVolatile);
			Add(new DmdTypeName("System", "IDisposable"), DmdWellKnownType.System_IDisposable);
			Add(new DmdTypeName("System", "TypedReference"), DmdWellKnownType.System_TypedReference);
			Add(new DmdTypeName("System", "ArgIterator"), DmdWellKnownType.System_ArgIterator);
			Add(new DmdTypeName("System", "RuntimeArgumentHandle"), DmdWellKnownType.System_RuntimeArgumentHandle);
			Add(new DmdTypeName("System", "RuntimeFieldHandle"), DmdWellKnownType.System_RuntimeFieldHandle);
			Add(new DmdTypeName("System", "RuntimeMethodHandle"), DmdWellKnownType.System_RuntimeMethodHandle);
			Add(new DmdTypeName("System", "RuntimeTypeHandle"), DmdWellKnownType.System_RuntimeTypeHandle);
			Add(new DmdTypeName("System", "IAsyncResult"), DmdWellKnownType.System_IAsyncResult);
			Add(new DmdTypeName("System", "AsyncCallback"), DmdWellKnownType.System_AsyncCallback);
			Add(new DmdTypeName("System", "Math"), DmdWellKnownType.System_Math);
			Add(new DmdTypeName("System", "Attribute"), DmdWellKnownType.System_Attribute);
			Add(new DmdTypeName("System", "CLSCompliantAttribute"), DmdWellKnownType.System_CLSCompliantAttribute);
			Add(new DmdTypeName("System", "Convert"), DmdWellKnownType.System_Convert);
			Add(new DmdTypeName("System", "Exception"), DmdWellKnownType.System_Exception);
			Add(new DmdTypeName("System", "FlagsAttribute"), DmdWellKnownType.System_FlagsAttribute);
			Add(new DmdTypeName("System", "FormattableString"), DmdWellKnownType.System_FormattableString);
			Add(new DmdTypeName("System", "Guid"), DmdWellKnownType.System_Guid);
			Add(new DmdTypeName("System", "IFormattable"), DmdWellKnownType.System_IFormattable);
			Add(new DmdTypeName("System", "MarshalByRefObject"), DmdWellKnownType.System_MarshalByRefObject);
			Add(new DmdTypeName("System", "Type"), DmdWellKnownType.System_Type);
			Add(new DmdTypeName("System.Reflection", "AssemblyKeyFileAttribute"), DmdWellKnownType.System_Reflection_AssemblyKeyFileAttribute);
			Add(new DmdTypeName("System.Reflection", "AssemblyKeyNameAttribute"), DmdWellKnownType.System_Reflection_AssemblyKeyNameAttribute);
			Add(new DmdTypeName("System.Reflection", "MethodInfo"), DmdWellKnownType.System_Reflection_MethodInfo);
			Add(new DmdTypeName("System.Reflection", "ConstructorInfo"), DmdWellKnownType.System_Reflection_ConstructorInfo);
			Add(new DmdTypeName("System.Reflection", "MethodBase"), DmdWellKnownType.System_Reflection_MethodBase);
			Add(new DmdTypeName("System.Reflection", "FieldInfo"), DmdWellKnownType.System_Reflection_FieldInfo);
			Add(new DmdTypeName("System.Reflection", "MemberInfo"), DmdWellKnownType.System_Reflection_MemberInfo);
			Add(new DmdTypeName("System.Reflection", "Missing"), DmdWellKnownType.System_Reflection_Missing);
			Add(new DmdTypeName("System.Runtime.CompilerServices", "FormattableStringFactory"), DmdWellKnownType.System_Runtime_CompilerServices_FormattableStringFactory);
			Add(new DmdTypeName("System.Runtime.CompilerServices", "RuntimeHelpers"), DmdWellKnownType.System_Runtime_CompilerServices_RuntimeHelpers);
			Add(new DmdTypeName("System.Runtime.ExceptionServices", "ExceptionDispatchInfo"), DmdWellKnownType.System_Runtime_ExceptionServices_ExceptionDispatchInfo);
			Add(new DmdTypeName("System.Runtime.InteropServices", "StructLayoutAttribute"), DmdWellKnownType.System_Runtime_InteropServices_StructLayoutAttribute);
			Add(new DmdTypeName("System.Runtime.InteropServices", "UnknownWrapper"), DmdWellKnownType.System_Runtime_InteropServices_UnknownWrapper);
			Add(new DmdTypeName("System.Runtime.InteropServices", "DispatchWrapper"), DmdWellKnownType.System_Runtime_InteropServices_DispatchWrapper);
			Add(new DmdTypeName("System.Runtime.InteropServices", "CallingConvention"), DmdWellKnownType.System_Runtime_InteropServices_CallingConvention);
			Add(new DmdTypeName("System.Runtime.InteropServices", "ClassInterfaceAttribute"), DmdWellKnownType.System_Runtime_InteropServices_ClassInterfaceAttribute);
			Add(new DmdTypeName("System.Runtime.InteropServices", "ClassInterfaceType"), DmdWellKnownType.System_Runtime_InteropServices_ClassInterfaceType);
			Add(new DmdTypeName("System.Runtime.InteropServices", "CoClassAttribute"), DmdWellKnownType.System_Runtime_InteropServices_CoClassAttribute);
			Add(new DmdTypeName("System.Runtime.InteropServices", "ComAwareEventInfo"), DmdWellKnownType.System_Runtime_InteropServices_ComAwareEventInfo);
			Add(new DmdTypeName("System.Runtime.InteropServices", "ComEventInterfaceAttribute"), DmdWellKnownType.System_Runtime_InteropServices_ComEventInterfaceAttribute);
			Add(new DmdTypeName("System.Runtime.InteropServices", "ComInterfaceType"), DmdWellKnownType.System_Runtime_InteropServices_ComInterfaceType);
			Add(new DmdTypeName("System.Runtime.InteropServices", "ComSourceInterfacesAttribute"), DmdWellKnownType.System_Runtime_InteropServices_ComSourceInterfacesAttribute);
			Add(new DmdTypeName("System.Runtime.InteropServices", "ComVisibleAttribute"), DmdWellKnownType.System_Runtime_InteropServices_ComVisibleAttribute);
			Add(new DmdTypeName("System.Runtime.InteropServices", "DispIdAttribute"), DmdWellKnownType.System_Runtime_InteropServices_DispIdAttribute);
			Add(new DmdTypeName("System.Runtime.InteropServices", "GuidAttribute"), DmdWellKnownType.System_Runtime_InteropServices_GuidAttribute);
			Add(new DmdTypeName("System.Runtime.InteropServices", "InterfaceTypeAttribute"), DmdWellKnownType.System_Runtime_InteropServices_InterfaceTypeAttribute);
			Add(new DmdTypeName("System.Runtime.InteropServices", "Marshal"), DmdWellKnownType.System_Runtime_InteropServices_Marshal);
			Add(new DmdTypeName("System.Runtime.InteropServices", "TypeIdentifierAttribute"), DmdWellKnownType.System_Runtime_InteropServices_TypeIdentifierAttribute);
			Add(new DmdTypeName("System.Runtime.InteropServices", "BestFitMappingAttribute"), DmdWellKnownType.System_Runtime_InteropServices_BestFitMappingAttribute);
			Add(new DmdTypeName("System.Runtime.InteropServices", "DefaultParameterValueAttribute"), DmdWellKnownType.System_Runtime_InteropServices_DefaultParameterValueAttribute);
			Add(new DmdTypeName("System.Runtime.InteropServices", "LCIDConversionAttribute"), DmdWellKnownType.System_Runtime_InteropServices_LCIDConversionAttribute);
			Add(new DmdTypeName("System.Runtime.InteropServices", "UnmanagedFunctionPointerAttribute"), DmdWellKnownType.System_Runtime_InteropServices_UnmanagedFunctionPointerAttribute);
			Add(new DmdTypeName("System", "Activator"), DmdWellKnownType.System_Activator);
			Add(new DmdTypeName("System.Threading.Tasks", "Task"), DmdWellKnownType.System_Threading_Tasks_Task);
			Add(new DmdTypeName("System.Threading.Tasks", "Task`1"), DmdWellKnownType.System_Threading_Tasks_Task_T);
			Add(new DmdTypeName("System.Threading", "Interlocked"), DmdWellKnownType.System_Threading_Interlocked);
			Add(new DmdTypeName("System.Threading", "Monitor"), DmdWellKnownType.System_Threading_Monitor);
			Add(new DmdTypeName("System.Threading", "Thread"), DmdWellKnownType.System_Threading_Thread);
			Add(new DmdTypeName("Microsoft.CSharp.RuntimeBinder", "Binder"), DmdWellKnownType.Microsoft_CSharp_RuntimeBinder_Binder);
			Add(new DmdTypeName("Microsoft.CSharp.RuntimeBinder", "CSharpArgumentInfo"), DmdWellKnownType.Microsoft_CSharp_RuntimeBinder_CSharpArgumentInfo);
			Add(new DmdTypeName("Microsoft.CSharp.RuntimeBinder", "CSharpArgumentInfoFlags"), DmdWellKnownType.Microsoft_CSharp_RuntimeBinder_CSharpArgumentInfoFlags);
			Add(new DmdTypeName("Microsoft.CSharp.RuntimeBinder", "CSharpBinderFlags"), DmdWellKnownType.Microsoft_CSharp_RuntimeBinder_CSharpBinderFlags);
			Add(new DmdTypeName("Microsoft.VisualBasic", "CallType"), DmdWellKnownType.Microsoft_VisualBasic_CallType);
			Add(new DmdTypeName("Microsoft.VisualBasic", "Embedded"), DmdWellKnownType.Microsoft_VisualBasic_Embedded);
			Add(new DmdTypeName("Microsoft.VisualBasic.CompilerServices", "Conversions"), DmdWellKnownType.Microsoft_VisualBasic_CompilerServices_Conversions);
			Add(new DmdTypeName("Microsoft.VisualBasic.CompilerServices", "Operators"), DmdWellKnownType.Microsoft_VisualBasic_CompilerServices_Operators);
			Add(new DmdTypeName("Microsoft.VisualBasic.CompilerServices", "NewLateBinding"), DmdWellKnownType.Microsoft_VisualBasic_CompilerServices_NewLateBinding);
			Add(new DmdTypeName("Microsoft.VisualBasic.CompilerServices", "EmbeddedOperators"), DmdWellKnownType.Microsoft_VisualBasic_CompilerServices_EmbeddedOperators);
			Add(new DmdTypeName("Microsoft.VisualBasic.CompilerServices", "StandardModuleAttribute"), DmdWellKnownType.Microsoft_VisualBasic_CompilerServices_StandardModuleAttribute);
			Add(new DmdTypeName("Microsoft.VisualBasic.CompilerServices", "Utils"), DmdWellKnownType.Microsoft_VisualBasic_CompilerServices_Utils);
			Add(new DmdTypeName("Microsoft.VisualBasic.CompilerServices", "LikeOperator"), DmdWellKnownType.Microsoft_VisualBasic_CompilerServices_LikeOperator);
			Add(new DmdTypeName("Microsoft.VisualBasic.CompilerServices", "ProjectData"), DmdWellKnownType.Microsoft_VisualBasic_CompilerServices_ProjectData);
			Add(new DmdTypeName("Microsoft.VisualBasic.CompilerServices", "ObjectFlowControl"), DmdWellKnownType.Microsoft_VisualBasic_CompilerServices_ObjectFlowControl);
			Add(new DmdTypeName("Microsoft.VisualBasic.CompilerServices", "ObjectFlowControl", "ForLoopControl"), DmdWellKnownType.Microsoft_VisualBasic_CompilerServices_ObjectFlowControl_ForLoopControl);
			Add(new DmdTypeName("Microsoft.VisualBasic.CompilerServices", "StaticLocalInitFlag"), DmdWellKnownType.Microsoft_VisualBasic_CompilerServices_StaticLocalInitFlag);
			Add(new DmdTypeName("Microsoft.VisualBasic.CompilerServices", "StringType"), DmdWellKnownType.Microsoft_VisualBasic_CompilerServices_StringType);
			Add(new DmdTypeName("Microsoft.VisualBasic.CompilerServices", "IncompleteInitialization"), DmdWellKnownType.Microsoft_VisualBasic_CompilerServices_IncompleteInitialization);
			Add(new DmdTypeName("Microsoft.VisualBasic.CompilerServices", "Versioned"), DmdWellKnownType.Microsoft_VisualBasic_CompilerServices_Versioned);
			Add(new DmdTypeName("Microsoft.VisualBasic", "CompareMethod"), DmdWellKnownType.Microsoft_VisualBasic_CompareMethod);
			Add(new DmdTypeName("Microsoft.VisualBasic", "Strings"), DmdWellKnownType.Microsoft_VisualBasic_Strings);
			Add(new DmdTypeName("Microsoft.VisualBasic", "ErrObject"), DmdWellKnownType.Microsoft_VisualBasic_ErrObject);
			Add(new DmdTypeName("Microsoft.VisualBasic", "FileSystem"), DmdWellKnownType.Microsoft_VisualBasic_FileSystem);
			Add(new DmdTypeName("Microsoft.VisualBasic.ApplicationServices", "ApplicationBase"), DmdWellKnownType.Microsoft_VisualBasic_ApplicationServices_ApplicationBase);
			Add(new DmdTypeName("Microsoft.VisualBasic.ApplicationServices", "WindowsFormsApplicationBase"), DmdWellKnownType.Microsoft_VisualBasic_ApplicationServices_WindowsFormsApplicationBase);
			Add(new DmdTypeName("Microsoft.VisualBasic", "Information"), DmdWellKnownType.Microsoft_VisualBasic_Information);
			Add(new DmdTypeName("Microsoft.VisualBasic", "Interaction"), DmdWellKnownType.Microsoft_VisualBasic_Interaction);
			Add(new DmdTypeName("System", "Func`1"), DmdWellKnownType.System_Func_T);
			Add(new DmdTypeName("System", "Func`2"), DmdWellKnownType.System_Func_T2);
			Add(new DmdTypeName("System", "Func`3"), DmdWellKnownType.System_Func_T3);
			Add(new DmdTypeName("System", "Func`4"), DmdWellKnownType.System_Func_T4);
			Add(new DmdTypeName("System", "Func`5"), DmdWellKnownType.System_Func_T5);
			Add(new DmdTypeName("System", "Func`6"), DmdWellKnownType.System_Func_T6);
			Add(new DmdTypeName("System", "Func`7"), DmdWellKnownType.System_Func_T7);
			Add(new DmdTypeName("System", "Func`8"), DmdWellKnownType.System_Func_T8);
			Add(new DmdTypeName("System", "Func`9"), DmdWellKnownType.System_Func_T9);
			Add(new DmdTypeName("System", "Func`10"), DmdWellKnownType.System_Func_T10);
			Add(new DmdTypeName("System", "Func`11"), DmdWellKnownType.System_Func_T11);
			Add(new DmdTypeName("System", "Func`12"), DmdWellKnownType.System_Func_T12);
			Add(new DmdTypeName("System", "Func`13"), DmdWellKnownType.System_Func_T13);
			Add(new DmdTypeName("System", "Func`14"), DmdWellKnownType.System_Func_T14);
			Add(new DmdTypeName("System", "Func`15"), DmdWellKnownType.System_Func_T15);
			Add(new DmdTypeName("System", "Func`16"), DmdWellKnownType.System_Func_T16);
			Add(new DmdTypeName("System", "Func`17"), DmdWellKnownType.System_Func_T17);
			Add(new DmdTypeName("System", "Action"), DmdWellKnownType.System_Action);
			Add(new DmdTypeName("System", "Action`1"), DmdWellKnownType.System_Action_T);
			Add(new DmdTypeName("System", "Action`2"), DmdWellKnownType.System_Action_T2);
			Add(new DmdTypeName("System", "Action`3"), DmdWellKnownType.System_Action_T3);
			Add(new DmdTypeName("System", "Action`4"), DmdWellKnownType.System_Action_T4);
			Add(new DmdTypeName("System", "Action`5"), DmdWellKnownType.System_Action_T5);
			Add(new DmdTypeName("System", "Action`6"), DmdWellKnownType.System_Action_T6);
			Add(new DmdTypeName("System", "Action`7"), DmdWellKnownType.System_Action_T7);
			Add(new DmdTypeName("System", "Action`8"), DmdWellKnownType.System_Action_T8);
			Add(new DmdTypeName("System", "Action`9"), DmdWellKnownType.System_Action_T9);
			Add(new DmdTypeName("System", "Action`10"), DmdWellKnownType.System_Action_T10);
			Add(new DmdTypeName("System", "Action`11"), DmdWellKnownType.System_Action_T11);
			Add(new DmdTypeName("System", "Action`12"), DmdWellKnownType.System_Action_T12);
			Add(new DmdTypeName("System", "Action`13"), DmdWellKnownType.System_Action_T13);
			Add(new DmdTypeName("System", "Action`14"), DmdWellKnownType.System_Action_T14);
			Add(new DmdTypeName("System", "Action`15"), DmdWellKnownType.System_Action_T15);
			Add(new DmdTypeName("System", "Action`16"), DmdWellKnownType.System_Action_T16);
			Add(new DmdTypeName("System", "AttributeUsageAttribute"), DmdWellKnownType.System_AttributeUsageAttribute);
			Add(new DmdTypeName("System", "ParamArrayAttribute"), DmdWellKnownType.System_ParamArrayAttribute);
			Add(new DmdTypeName("System", "NonSerializedAttribute"), DmdWellKnownType.System_NonSerializedAttribute);
			Add(new DmdTypeName("System", "STAThreadAttribute"), DmdWellKnownType.System_STAThreadAttribute);
			Add(new DmdTypeName("System.Reflection", "DefaultMemberAttribute"), DmdWellKnownType.System_Reflection_DefaultMemberAttribute);
			Add(new DmdTypeName("System.Runtime.CompilerServices", "DateTimeConstantAttribute"), DmdWellKnownType.System_Runtime_CompilerServices_DateTimeConstantAttribute);
			Add(new DmdTypeName("System.Runtime.CompilerServices", "DecimalConstantAttribute"), DmdWellKnownType.System_Runtime_CompilerServices_DecimalConstantAttribute);
			Add(new DmdTypeName("System.Runtime.CompilerServices", "IUnknownConstantAttribute"), DmdWellKnownType.System_Runtime_CompilerServices_IUnknownConstantAttribute);
			Add(new DmdTypeName("System.Runtime.CompilerServices", "IDispatchConstantAttribute"), DmdWellKnownType.System_Runtime_CompilerServices_IDispatchConstantAttribute);
			Add(new DmdTypeName("System.Runtime.CompilerServices", "ExtensionAttribute"), DmdWellKnownType.System_Runtime_CompilerServices_ExtensionAttribute);
			Add(new DmdTypeName("System.Runtime.CompilerServices", "INotifyCompletion"), DmdWellKnownType.System_Runtime_CompilerServices_INotifyCompletion);
			Add(new DmdTypeName("System.Runtime.CompilerServices", "InternalsVisibleToAttribute"), DmdWellKnownType.System_Runtime_CompilerServices_InternalsVisibleToAttribute);
			Add(new DmdTypeName("System.Runtime.CompilerServices", "CompilerGeneratedAttribute"), DmdWellKnownType.System_Runtime_CompilerServices_CompilerGeneratedAttribute);
			Add(new DmdTypeName("System.Runtime.CompilerServices", "AccessedThroughPropertyAttribute"), DmdWellKnownType.System_Runtime_CompilerServices_AccessedThroughPropertyAttribute);
			Add(new DmdTypeName("System.Runtime.CompilerServices", "CompilationRelaxationsAttribute"), DmdWellKnownType.System_Runtime_CompilerServices_CompilationRelaxationsAttribute);
			Add(new DmdTypeName("System.Runtime.CompilerServices", "RuntimeCompatibilityAttribute"), DmdWellKnownType.System_Runtime_CompilerServices_RuntimeCompatibilityAttribute);
			Add(new DmdTypeName("System.Runtime.CompilerServices", "UnsafeValueTypeAttribute"), DmdWellKnownType.System_Runtime_CompilerServices_UnsafeValueTypeAttribute);
			Add(new DmdTypeName("System.Runtime.CompilerServices", "FixedBufferAttribute"), DmdWellKnownType.System_Runtime_CompilerServices_FixedBufferAttribute);
			Add(new DmdTypeName("System.Runtime.CompilerServices", "DynamicAttribute"), DmdWellKnownType.System_Runtime_CompilerServices_DynamicAttribute);
			Add(new DmdTypeName("System.Runtime.CompilerServices", "CallSiteBinder"), DmdWellKnownType.System_Runtime_CompilerServices_CallSiteBinder);
			Add(new DmdTypeName("System.Runtime.CompilerServices", "CallSite"), DmdWellKnownType.System_Runtime_CompilerServices_CallSite);
			Add(new DmdTypeName("System.Runtime.CompilerServices", "CallSite`1"), DmdWellKnownType.System_Runtime_CompilerServices_CallSite_T);
			Add(new DmdTypeName("System.Runtime.InteropServices.WindowsRuntime", "EventRegistrationToken"), DmdWellKnownType.System_Runtime_InteropServices_WindowsRuntime_EventRegistrationToken);
			Add(new DmdTypeName("System.Runtime.InteropServices.WindowsRuntime", "EventRegistrationTokenTable`1"), DmdWellKnownType.System_Runtime_InteropServices_WindowsRuntime_EventRegistrationTokenTable_T);
			Add(new DmdTypeName("System.Runtime.InteropServices.WindowsRuntime", "WindowsRuntimeMarshal"), DmdWellKnownType.System_Runtime_InteropServices_WindowsRuntime_WindowsRuntimeMarshal);
			Add(new DmdTypeName("Windows.Foundation", "IAsyncAction"), DmdWellKnownType.Windows_Foundation_IAsyncAction);
			Add(new DmdTypeName("Windows.Foundation", "IAsyncActionWithProgress`1"), DmdWellKnownType.Windows_Foundation_IAsyncActionWithProgress_T);
			Add(new DmdTypeName("Windows.Foundation", "IAsyncOperation`1"), DmdWellKnownType.Windows_Foundation_IAsyncOperation_T);
			Add(new DmdTypeName("Windows.Foundation", "IAsyncOperationWithProgress`2"), DmdWellKnownType.Windows_Foundation_IAsyncOperationWithProgress_T2);
			Add(new DmdTypeName("System.Diagnostics", "Debugger"), DmdWellKnownType.System_Diagnostics_Debugger);
			Add(new DmdTypeName("System.Diagnostics", "DebuggerDisplayAttribute"), DmdWellKnownType.System_Diagnostics_DebuggerDisplayAttribute);
			Add(new DmdTypeName("System.Diagnostics", "DebuggerNonUserCodeAttribute"), DmdWellKnownType.System_Diagnostics_DebuggerNonUserCodeAttribute);
			Add(new DmdTypeName("System.Diagnostics", "DebuggerHiddenAttribute"), DmdWellKnownType.System_Diagnostics_DebuggerHiddenAttribute);
			Add(new DmdTypeName("System.Diagnostics", "DebuggerBrowsableAttribute"), DmdWellKnownType.System_Diagnostics_DebuggerBrowsableAttribute);
			Add(new DmdTypeName("System.Diagnostics", "DebuggerStepThroughAttribute"), DmdWellKnownType.System_Diagnostics_DebuggerStepThroughAttribute);
			Add(new DmdTypeName("System.Diagnostics", "DebuggerBrowsableState"), DmdWellKnownType.System_Diagnostics_DebuggerBrowsableState);
			Add(new DmdTypeName("System.Diagnostics", "DebuggableAttribute"), DmdWellKnownType.System_Diagnostics_DebuggableAttribute);
			Add(new DmdTypeName("System.Diagnostics", "DebuggableAttribute", "DebuggingModes"), DmdWellKnownType.System_Diagnostics_DebuggableAttribute__DebuggingModes);
			Add(new DmdTypeName("System.ComponentModel", "DesignerSerializationVisibilityAttribute"), DmdWellKnownType.System_ComponentModel_DesignerSerializationVisibilityAttribute);
			Add(new DmdTypeName("System", "IEquatable`1"), DmdWellKnownType.System_IEquatable_T);
			Add(new DmdTypeName("System.Collections", "IList"), DmdWellKnownType.System_Collections_IList);
			Add(new DmdTypeName("System.Collections", "ICollection"), DmdWellKnownType.System_Collections_ICollection);
			Add(new DmdTypeName("System.Collections.Generic", "EqualityComparer`1"), DmdWellKnownType.System_Collections_Generic_EqualityComparer_T);
			Add(new DmdTypeName("System.Collections.Generic", "List`1"), DmdWellKnownType.System_Collections_Generic_List_T);
			Add(new DmdTypeName("System.Collections.Generic", "IDictionary`2"), DmdWellKnownType.System_Collections_Generic_IDictionary_KV);
			Add(new DmdTypeName("System.Collections.Generic", "IReadOnlyDictionary`2"), DmdWellKnownType.System_Collections_Generic_IReadOnlyDictionary_KV);
			Add(new DmdTypeName("System.Collections.ObjectModel", "Collection`1"), DmdWellKnownType.System_Collections_ObjectModel_Collection_T);
			Add(new DmdTypeName("System.Collections.ObjectModel", "ReadOnlyCollection`1"), DmdWellKnownType.System_Collections_ObjectModel_ReadOnlyCollection_T);
			Add(new DmdTypeName("System.Collections.Specialized", "INotifyCollectionChanged"), DmdWellKnownType.System_Collections_Specialized_INotifyCollectionChanged);
			Add(new DmdTypeName("System.ComponentModel", "INotifyPropertyChanged"), DmdWellKnownType.System_ComponentModel_INotifyPropertyChanged);
			Add(new DmdTypeName("System.ComponentModel", "EditorBrowsableAttribute"), DmdWellKnownType.System_ComponentModel_EditorBrowsableAttribute);
			Add(new DmdTypeName("System.ComponentModel", "EditorBrowsableState"), DmdWellKnownType.System_ComponentModel_EditorBrowsableState);
			Add(new DmdTypeName("System.Linq", "Enumerable"), DmdWellKnownType.System_Linq_Enumerable);
			Add(new DmdTypeName("System.Linq.Expressions", "Expression"), DmdWellKnownType.System_Linq_Expressions_Expression);
			Add(new DmdTypeName("System.Linq.Expressions", "Expression`1"), DmdWellKnownType.System_Linq_Expressions_Expression_T);
			Add(new DmdTypeName("System.Linq.Expressions", "ParameterExpression"), DmdWellKnownType.System_Linq_Expressions_ParameterExpression);
			Add(new DmdTypeName("System.Linq.Expressions", "ElementInit"), DmdWellKnownType.System_Linq_Expressions_ElementInit);
			Add(new DmdTypeName("System.Linq.Expressions", "MemberBinding"), DmdWellKnownType.System_Linq_Expressions_MemberBinding);
			Add(new DmdTypeName("System.Linq.Expressions", "ExpressionType"), DmdWellKnownType.System_Linq_Expressions_ExpressionType);
			Add(new DmdTypeName("System.Linq", "IQueryable"), DmdWellKnownType.System_Linq_IQueryable);
			Add(new DmdTypeName("System.Linq", "IQueryable`1"), DmdWellKnownType.System_Linq_IQueryable_T);
			Add(new DmdTypeName("System.Xml.Linq", "Extensions"), DmdWellKnownType.System_Xml_Linq_Extensions);
			Add(new DmdTypeName("System.Xml.Linq", "XAttribute"), DmdWellKnownType.System_Xml_Linq_XAttribute);
			Add(new DmdTypeName("System.Xml.Linq", "XCData"), DmdWellKnownType.System_Xml_Linq_XCData);
			Add(new DmdTypeName("System.Xml.Linq", "XComment"), DmdWellKnownType.System_Xml_Linq_XComment);
			Add(new DmdTypeName("System.Xml.Linq", "XContainer"), DmdWellKnownType.System_Xml_Linq_XContainer);
			Add(new DmdTypeName("System.Xml.Linq", "XDeclaration"), DmdWellKnownType.System_Xml_Linq_XDeclaration);
			Add(new DmdTypeName("System.Xml.Linq", "XDocument"), DmdWellKnownType.System_Xml_Linq_XDocument);
			Add(new DmdTypeName("System.Xml.Linq", "XElement"), DmdWellKnownType.System_Xml_Linq_XElement);
			Add(new DmdTypeName("System.Xml.Linq", "XName"), DmdWellKnownType.System_Xml_Linq_XName);
			Add(new DmdTypeName("System.Xml.Linq", "XNamespace"), DmdWellKnownType.System_Xml_Linq_XNamespace);
			Add(new DmdTypeName("System.Xml.Linq", "XObject"), DmdWellKnownType.System_Xml_Linq_XObject);
			Add(new DmdTypeName("System.Xml.Linq", "XProcessingInstruction"), DmdWellKnownType.System_Xml_Linq_XProcessingInstruction);
			Add(new DmdTypeName("System.Security", "UnverifiableCodeAttribute"), DmdWellKnownType.System_Security_UnverifiableCodeAttribute);
			Add(new DmdTypeName("System.Security.Permissions", "SecurityAction"), DmdWellKnownType.System_Security_Permissions_SecurityAction);
			Add(new DmdTypeName("System.Security.Permissions", "SecurityAttribute"), DmdWellKnownType.System_Security_Permissions_SecurityAttribute);
			Add(new DmdTypeName("System.Security.Permissions", "SecurityPermissionAttribute"), DmdWellKnownType.System_Security_Permissions_SecurityPermissionAttribute);
			Add(new DmdTypeName("System", "NotSupportedException"), DmdWellKnownType.System_NotSupportedException);
			Add(new DmdTypeName("System.Runtime.CompilerServices", "ICriticalNotifyCompletion"), DmdWellKnownType.System_Runtime_CompilerServices_ICriticalNotifyCompletion);
			Add(new DmdTypeName("System.Runtime.CompilerServices", "IAsyncStateMachine"), DmdWellKnownType.System_Runtime_CompilerServices_IAsyncStateMachine);
			Add(new DmdTypeName("System.Runtime.CompilerServices", "AsyncVoidMethodBuilder"), DmdWellKnownType.System_Runtime_CompilerServices_AsyncVoidMethodBuilder);
			Add(new DmdTypeName("System.Runtime.CompilerServices", "AsyncTaskMethodBuilder"), DmdWellKnownType.System_Runtime_CompilerServices_AsyncTaskMethodBuilder);
			Add(new DmdTypeName("System.Runtime.CompilerServices", "AsyncTaskMethodBuilder`1"), DmdWellKnownType.System_Runtime_CompilerServices_AsyncTaskMethodBuilder_T);
			Add(new DmdTypeName("System.Runtime.CompilerServices", "AsyncStateMachineAttribute"), DmdWellKnownType.System_Runtime_CompilerServices_AsyncStateMachineAttribute);
			Add(new DmdTypeName("System.Runtime.CompilerServices", "IteratorStateMachineAttribute"), DmdWellKnownType.System_Runtime_CompilerServices_IteratorStateMachineAttribute);
			Add(new DmdTypeName("System.Windows.Forms", "Form"), DmdWellKnownType.System_Windows_Forms_Form);
			Add(new DmdTypeName("System.Windows.Forms", "Application"), DmdWellKnownType.System_Windows_Forms_Application);
			Add(new DmdTypeName("System", "Environment"), DmdWellKnownType.System_Environment);
			Add(new DmdTypeName("System.Runtime", "GCLatencyMode"), DmdWellKnownType.System_Runtime_GCLatencyMode);
			Add(new DmdTypeName("System", "IFormatProvider"), DmdWellKnownType.System_IFormatProvider);
			Add(new DmdTypeName("System", "ValueTuple`1"), DmdWellKnownType.System_ValueTuple_T1);
			Add(new DmdTypeName("System", "ValueTuple`2"), DmdWellKnownType.System_ValueTuple_T2);
			Add(new DmdTypeName("System", "ValueTuple`3"), DmdWellKnownType.System_ValueTuple_T3);
			Add(new DmdTypeName("System", "ValueTuple`4"), DmdWellKnownType.System_ValueTuple_T4);
			Add(new DmdTypeName("System", "ValueTuple`5"), DmdWellKnownType.System_ValueTuple_T5);
			Add(new DmdTypeName("System", "ValueTuple`6"), DmdWellKnownType.System_ValueTuple_T6);
			Add(new DmdTypeName("System", "ValueTuple`7"), DmdWellKnownType.System_ValueTuple_T7);
			Add(new DmdTypeName("System", "ValueTuple`8"), DmdWellKnownType.System_ValueTuple_TRest);
			Add(new DmdTypeName("System.Runtime.CompilerServices", "TupleElementNamesAttribute"), DmdWellKnownType.System_Runtime_CompilerServices_TupleElementNamesAttribute);
			Add(new DmdTypeName("System.Runtime.CompilerServices", "ReferenceAssemblyAttribute"), DmdWellKnownType.System_Runtime_CompilerServices_ReferenceAssemblyAttribute);
			Add(new DmdTypeName("System", "ContextBoundObject"), DmdWellKnownType.System_ContextBoundObject);
			Add(new DmdTypeName("System.Runtime.CompilerServices", "TypeForwardedToAttribute"), DmdWellKnownType.System_Runtime_CompilerServices_TypeForwardedToAttribute);
			Add(new DmdTypeName("System.Runtime.InteropServices", "ComImportAttribute"), DmdWellKnownType.System_Runtime_InteropServices_ComImportAttribute);
			Add(new DmdTypeName("System.Runtime.InteropServices", "DllImportAttribute"), DmdWellKnownType.System_Runtime_InteropServices_DllImportAttribute);
			Add(new DmdTypeName("System.Runtime.InteropServices", "FieldOffsetAttribute"), DmdWellKnownType.System_Runtime_InteropServices_FieldOffsetAttribute);
			Add(new DmdTypeName("System.Runtime.InteropServices", "InAttribute"), DmdWellKnownType.System_Runtime_InteropServices_InAttribute);
			Add(new DmdTypeName("System.Runtime.InteropServices", "MarshalAsAttribute"), DmdWellKnownType.System_Runtime_InteropServices_MarshalAsAttribute);
			Add(new DmdTypeName("System.Runtime.InteropServices", "OptionalAttribute"), DmdWellKnownType.System_Runtime_InteropServices_OptionalAttribute);
			Add(new DmdTypeName("System.Runtime.InteropServices", "OutAttribute"), DmdWellKnownType.System_Runtime_InteropServices_OutAttribute);
			Add(new DmdTypeName("System.Runtime.InteropServices", "PreserveSigAttribute"), DmdWellKnownType.System_Runtime_InteropServices_PreserveSigAttribute);
			Add(new DmdTypeName("System", "SerializableAttribute"), DmdWellKnownType.System_SerializableAttribute);
			Add(new DmdTypeName("System.Runtime.InteropServices", "CharSet"), DmdWellKnownType.System_Runtime_InteropServices_CharSet);
			Add(new DmdTypeName("System.Reflection", "Assembly"), DmdWellKnownType.System_Reflection_Assembly);
			Add(new DmdTypeName("System", "RuntimeMethodHandleInternal"), DmdWellKnownType.System_RuntimeMethodHandleInternal);
			Add(new DmdTypeName("System", "ByReference`1"), DmdWellKnownType.System_ByReference_T);
			Add(new DmdTypeName("System.Runtime.InteropServices", "UnmanagedType"), DmdWellKnownType.System_Runtime_InteropServices_UnmanagedType);
			Add(new DmdTypeName("System.Runtime.InteropServices", "VarEnum"), DmdWellKnownType.System_Runtime_InteropServices_VarEnum);
			Add(new DmdTypeName("System", "__ComObject"), DmdWellKnownType.System___ComObject);
			Add(new DmdTypeName("System.Runtime.InteropServices.WindowsRuntime", "RuntimeClass"), DmdWellKnownType.System_Runtime_InteropServices_WindowsRuntime_RuntimeClass);
			Add(new DmdTypeName("System", "DBNull"), DmdWellKnownType.System_DBNull);
			Add(new DmdTypeName("System.Security.Permissions", "PermissionSetAttribute"), DmdWellKnownType.System_Security_Permissions_PermissionSetAttribute);
			Add(new DmdTypeName("System.Diagnostics", "Debugger", "CrossThreadDependencyNotification"), DmdWellKnownType.System_Diagnostics_Debugger_CrossThreadDependencyNotification);
			Add(new DmdTypeName("System.Diagnostics", "DebuggerTypeProxyAttribute"), DmdWellKnownType.System_Diagnostics_DebuggerTypeProxyAttribute);
			Add(new DmdTypeName("System.Collections.Generic", "KeyValuePair`2"), DmdWellKnownType.System_Collections_Generic_KeyValuePair_T2);
			Add(new DmdTypeName("System.Linq", "SystemCore_EnumerableDebugView"), DmdWellKnownType.System_Linq_SystemCore_EnumerableDebugView);
			Add(new DmdTypeName("System.Linq", "SystemCore_EnumerableDebugView`1"), DmdWellKnownType.System_Linq_SystemCore_EnumerableDebugView_T);
			Add(new DmdTypeName("System.Text", "Encoding"), DmdWellKnownType.System_Text_Encoding);
			Add(new DmdTypeName("System.Runtime.CompilerServices", "IsReadOnlyAttribute"), DmdWellKnownType.System_Runtime_CompilerServices_IsReadOnlyAttribute);
			Add(new DmdTypeName("System.Runtime.CompilerServices", "IsByRefLikeAttribute"), DmdWellKnownType.System_Runtime_CompilerServices_IsByRefLikeAttribute);
			Add(new DmdTypeName("System", "ObsoleteAttribute"), DmdWellKnownType.System_ObsoleteAttribute);
			Add(new DmdTypeName("System", "Span`1"), DmdWellKnownType.System_Span_T);
			Add(new DmdTypeName("System.Runtime.InteropServices", "GCHandle"), DmdWellKnownType.System_Runtime_InteropServices_GCHandle);
			Add(new DmdTypeName("System.Runtime.CompilerServices", "NullableAttribute"), DmdWellKnownType.System_Runtime_CompilerServices_NullableAttribute);
			Add(new DmdTypeName("System", "ReadOnlySpan`1"), DmdWellKnownType.System_ReadOnlySpan_T);
			Add(new DmdTypeName("System.Runtime.CompilerServices", "IsUnmanagedAttribute"), DmdWellKnownType.System_Runtime_CompilerServices_IsUnmanagedAttribute);
			Add(new DmdTypeName("Microsoft.VisualBasic", "Conversion"), DmdWellKnownType.Microsoft_VisualBasic_Conversion);
			Add(new DmdTypeName("System", "Index"), DmdWellKnownType.System_Index);
			Add(new DmdTypeName("System", "Range"), DmdWellKnownType.System_Range);
			Add(new DmdTypeName("System.Runtime.CompilerServices", "AsyncIteratorStateMachineAttribute"), DmdWellKnownType.System_Runtime_CompilerServices_AsyncIteratorStateMachineAttribute);
			Add(new DmdTypeName("System", "IAsyncDisposable"), DmdWellKnownType.System_IAsyncDisposable);
			Add(new DmdTypeName("System.Collections.Generic", "IAsyncEnumerable`1"), DmdWellKnownType.System_Collections_Generic_IAsyncEnumerable_T);
			Add(new DmdTypeName("System.Collections.Generic", "IAsyncEnumerator`1"), DmdWellKnownType.System_Collections_Generic_IAsyncEnumerator_T);
			Add(new DmdTypeName("System.Threading.Tasks.Sources", "ManualResetValueTaskSourceCore`1"), DmdWellKnownType.System_Threading_Tasks_Sources_ManualResetValueTaskSourceCore_T);
			Add(new DmdTypeName("System.Threading.Tasks.Sources", "ValueTaskSourceStatus"), DmdWellKnownType.System_Threading_Tasks_Sources_ValueTaskSourceStatus);
			Add(new DmdTypeName("System.Threading.Tasks.Sources", "ValueTaskSourceOnCompletedFlags"), DmdWellKnownType.System_Threading_Tasks_Sources_ValueTaskSourceOnCompletedFlags);
			Add(new DmdTypeName("System.Threading.Tasks.Sources", "IValueTaskSource`1"), DmdWellKnownType.System_Threading_Tasks_Sources_IValueTaskSource_T);
			Add(new DmdTypeName("System.Threading.Tasks.Sources", "IValueTaskSource"), DmdWellKnownType.System_Threading_Tasks_Sources_IValueTaskSource);
			Add(new DmdTypeName("System.Threading.Tasks", "ValueTask`1"), DmdWellKnownType.System_Threading_Tasks_ValueTask_T);
			Add(new DmdTypeName("System.Threading.Tasks", "ValueTask"), DmdWellKnownType.System_Threading_Tasks_ValueTask);
			Add(new DmdTypeName("System.Runtime.CompilerServices", "AsyncIteratorMethodBuilder"), DmdWellKnownType.System_Runtime_CompilerServices_AsyncIteratorMethodBuilder);
			Add(new DmdTypeName("System.Threading", "CancellationToken"), DmdWellKnownType.System_Threading_CancellationToken);
			Add(new DmdTypeName("System.Collections", "DictionaryEntry"), DmdWellKnownType.System_Collections_DictionaryEntry);

			Debug.Assert(toWellKnownType.Count == WellKnownTypesCount);
#if DEBUG
			foreach (var name in toWellKnownTypeName)
				Debug.Assert(name.Name != null);
#endif
		}
	}
}
