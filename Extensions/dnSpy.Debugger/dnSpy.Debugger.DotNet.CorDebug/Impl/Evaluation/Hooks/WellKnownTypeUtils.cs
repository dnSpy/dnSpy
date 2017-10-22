/*
    Copyright (C) 2014-2017 de4dot@gmail.com

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
using dnSpy.Debugger.DotNet.Metadata;

namespace dnSpy.Debugger.DotNet.CorDebug.Impl.Evaluation.Hooks {
	static class WellKnownTypeUtils {
		static readonly Dictionary<TypeName, DmdWellKnownType> toWellKnownType;
		static readonly TypeName[] toWellKnownTypeName;

		public static bool TryGetWellKnownType(TypeName name, out DmdWellKnownType wellKnownType) =>
			toWellKnownType.TryGetValue(name, out wellKnownType);

		public static TypeName GetTypeName(DmdWellKnownType wellKnownType) =>
			toWellKnownTypeName[(int)wellKnownType];

		static void Add(TypeName typeName, DmdWellKnownType wellKnownType) {
			toWellKnownType.Add(typeName, wellKnownType);
			toWellKnownTypeName[(int)wellKnownType] = typeName;
		}

		const int NumberOfTypes = 281;
		static WellKnownTypeUtils() {
			toWellKnownType = new Dictionary<TypeName, DmdWellKnownType>(NumberOfTypes, TypeNameEqualityComparer.Instance);
			toWellKnownTypeName = new TypeName[NumberOfTypes];

			Add(new TypeName("System", "Object"), DmdWellKnownType.System_Object);
			Add(new TypeName("System", "Enum"), DmdWellKnownType.System_Enum);
			Add(new TypeName("System", "MulticastDelegate"), DmdWellKnownType.System_MulticastDelegate);
			Add(new TypeName("System", "Delegate"), DmdWellKnownType.System_Delegate);
			Add(new TypeName("System", "ValueType"), DmdWellKnownType.System_ValueType);
			Add(new TypeName("System", "Void"), DmdWellKnownType.System_Void);
			Add(new TypeName("System", "Boolean"), DmdWellKnownType.System_Boolean);
			Add(new TypeName("System", "Char"), DmdWellKnownType.System_Char);
			Add(new TypeName("System", "SByte"), DmdWellKnownType.System_SByte);
			Add(new TypeName("System", "Byte"), DmdWellKnownType.System_Byte);
			Add(new TypeName("System", "Int16"), DmdWellKnownType.System_Int16);
			Add(new TypeName("System", "UInt16"), DmdWellKnownType.System_UInt16);
			Add(new TypeName("System", "Int32"), DmdWellKnownType.System_Int32);
			Add(new TypeName("System", "UInt32"), DmdWellKnownType.System_UInt32);
			Add(new TypeName("System", "Int64"), DmdWellKnownType.System_Int64);
			Add(new TypeName("System", "UInt64"), DmdWellKnownType.System_UInt64);
			Add(new TypeName("System", "Decimal"), DmdWellKnownType.System_Decimal);
			Add(new TypeName("System", "Single"), DmdWellKnownType.System_Single);
			Add(new TypeName("System", "Double"), DmdWellKnownType.System_Double);
			Add(new TypeName("System", "String"), DmdWellKnownType.System_String);
			Add(new TypeName("System", "IntPtr"), DmdWellKnownType.System_IntPtr);
			Add(new TypeName("System", "UIntPtr"), DmdWellKnownType.System_UIntPtr);
			Add(new TypeName("System", "Array"), DmdWellKnownType.System_Array);
			Add(new TypeName("System.Collections", "IEnumerable"), DmdWellKnownType.System_Collections_IEnumerable);
			Add(new TypeName("System.Collections.Generic", "IEnumerable`1"), DmdWellKnownType.System_Collections_Generic_IEnumerable_T);
			Add(new TypeName("System.Collections.Generic", "IList`1"), DmdWellKnownType.System_Collections_Generic_IList_T);
			Add(new TypeName("System.Collections.Generic", "ICollection`1"), DmdWellKnownType.System_Collections_Generic_ICollection_T);
			Add(new TypeName("System.Collections", "IEnumerator"), DmdWellKnownType.System_Collections_IEnumerator);
			Add(new TypeName("System.Collections.Generic", "IEnumerator`1"), DmdWellKnownType.System_Collections_Generic_IEnumerator_T);
			Add(new TypeName("System.Collections.Generic", "IReadOnlyList`1"), DmdWellKnownType.System_Collections_Generic_IReadOnlyList_T);
			Add(new TypeName("System.Collections.Generic", "IReadOnlyCollection`1"), DmdWellKnownType.System_Collections_Generic_IReadOnlyCollection_T);
			Add(new TypeName("System", "Nullable`1"), DmdWellKnownType.System_Nullable_T);
			Add(new TypeName("System", "DateTime"), DmdWellKnownType.System_DateTime);
			Add(new TypeName("System.Runtime.CompilerServices", "IsVolatile"), DmdWellKnownType.System_Runtime_CompilerServices_IsVolatile);
			Add(new TypeName("System", "IDisposable"), DmdWellKnownType.System_IDisposable);
			Add(new TypeName("System", "TypedReference"), DmdWellKnownType.System_TypedReference);
			Add(new TypeName("System", "ArgIterator"), DmdWellKnownType.System_ArgIterator);
			Add(new TypeName("System", "RuntimeArgumentHandle"), DmdWellKnownType.System_RuntimeArgumentHandle);
			Add(new TypeName("System", "RuntimeFieldHandle"), DmdWellKnownType.System_RuntimeFieldHandle);
			Add(new TypeName("System", "RuntimeMethodHandle"), DmdWellKnownType.System_RuntimeMethodHandle);
			Add(new TypeName("System", "RuntimeTypeHandle"), DmdWellKnownType.System_RuntimeTypeHandle);
			Add(new TypeName("System", "IAsyncResult"), DmdWellKnownType.System_IAsyncResult);
			Add(new TypeName("System", "AsyncCallback"), DmdWellKnownType.System_AsyncCallback);
			Add(new TypeName("System", "Math"), DmdWellKnownType.System_Math);
			Add(new TypeName("System", "Attribute"), DmdWellKnownType.System_Attribute);
			Add(new TypeName("System", "CLSCompliantAttribute"), DmdWellKnownType.System_CLSCompliantAttribute);
			Add(new TypeName("System", "Convert"), DmdWellKnownType.System_Convert);
			Add(new TypeName("System", "Exception"), DmdWellKnownType.System_Exception);
			Add(new TypeName("System", "FlagsAttribute"), DmdWellKnownType.System_FlagsAttribute);
			Add(new TypeName("System", "FormattableString"), DmdWellKnownType.System_FormattableString);
			Add(new TypeName("System", "Guid"), DmdWellKnownType.System_Guid);
			Add(new TypeName("System", "IFormattable"), DmdWellKnownType.System_IFormattable);
			Add(new TypeName("System", "MarshalByRefObject"), DmdWellKnownType.System_MarshalByRefObject);
			Add(new TypeName("System", "Type"), DmdWellKnownType.System_Type);
			Add(new TypeName("System.Reflection", "AssemblyKeyFileAttribute"), DmdWellKnownType.System_Reflection_AssemblyKeyFileAttribute);
			Add(new TypeName("System.Reflection", "AssemblyKeyNameAttribute"), DmdWellKnownType.System_Reflection_AssemblyKeyNameAttribute);
			Add(new TypeName("System.Reflection", "MethodInfo"), DmdWellKnownType.System_Reflection_MethodInfo);
			Add(new TypeName("System.Reflection", "ConstructorInfo"), DmdWellKnownType.System_Reflection_ConstructorInfo);
			Add(new TypeName("System.Reflection", "MethodBase"), DmdWellKnownType.System_Reflection_MethodBase);
			Add(new TypeName("System.Reflection", "FieldInfo"), DmdWellKnownType.System_Reflection_FieldInfo);
			Add(new TypeName("System.Reflection", "MemberInfo"), DmdWellKnownType.System_Reflection_MemberInfo);
			Add(new TypeName("System.Reflection", "Missing"), DmdWellKnownType.System_Reflection_Missing);
			Add(new TypeName("System.Runtime.CompilerServices", "FormattableStringFactory"), DmdWellKnownType.System_Runtime_CompilerServices_FormattableStringFactory);
			Add(new TypeName("System.Runtime.CompilerServices", "RuntimeHelpers"), DmdWellKnownType.System_Runtime_CompilerServices_RuntimeHelpers);
			Add(new TypeName("System.Runtime.ExceptionServices", "ExceptionDispatchInfo"), DmdWellKnownType.System_Runtime_ExceptionServices_ExceptionDispatchInfo);
			Add(new TypeName("System.Runtime.InteropServices", "StructLayoutAttribute"), DmdWellKnownType.System_Runtime_InteropServices_StructLayoutAttribute);
			Add(new TypeName("System.Runtime.InteropServices", "UnknownWrapper"), DmdWellKnownType.System_Runtime_InteropServices_UnknownWrapper);
			Add(new TypeName("System.Runtime.InteropServices", "DispatchWrapper"), DmdWellKnownType.System_Runtime_InteropServices_DispatchWrapper);
			Add(new TypeName("System.Runtime.InteropServices", "CallingConvention"), DmdWellKnownType.System_Runtime_InteropServices_CallingConvention);
			Add(new TypeName("System.Runtime.InteropServices", "ClassInterfaceAttribute"), DmdWellKnownType.System_Runtime_InteropServices_ClassInterfaceAttribute);
			Add(new TypeName("System.Runtime.InteropServices", "ClassInterfaceType"), DmdWellKnownType.System_Runtime_InteropServices_ClassInterfaceType);
			Add(new TypeName("System.Runtime.InteropServices", "CoClassAttribute"), DmdWellKnownType.System_Runtime_InteropServices_CoClassAttribute);
			Add(new TypeName("System.Runtime.InteropServices", "ComAwareEventInfo"), DmdWellKnownType.System_Runtime_InteropServices_ComAwareEventInfo);
			Add(new TypeName("System.Runtime.InteropServices", "ComEventInterfaceAttribute"), DmdWellKnownType.System_Runtime_InteropServices_ComEventInterfaceAttribute);
			Add(new TypeName("System.Runtime.InteropServices", "ComInterfaceType"), DmdWellKnownType.System_Runtime_InteropServices_ComInterfaceType);
			Add(new TypeName("System.Runtime.InteropServices", "ComSourceInterfacesAttribute"), DmdWellKnownType.System_Runtime_InteropServices_ComSourceInterfacesAttribute);
			Add(new TypeName("System.Runtime.InteropServices", "ComVisibleAttribute"), DmdWellKnownType.System_Runtime_InteropServices_ComVisibleAttribute);
			Add(new TypeName("System.Runtime.InteropServices", "DispIdAttribute"), DmdWellKnownType.System_Runtime_InteropServices_DispIdAttribute);
			Add(new TypeName("System.Runtime.InteropServices", "GuidAttribute"), DmdWellKnownType.System_Runtime_InteropServices_GuidAttribute);
			Add(new TypeName("System.Runtime.InteropServices", "InterfaceTypeAttribute"), DmdWellKnownType.System_Runtime_InteropServices_InterfaceTypeAttribute);
			Add(new TypeName("System.Runtime.InteropServices", "Marshal"), DmdWellKnownType.System_Runtime_InteropServices_Marshal);
			Add(new TypeName("System.Runtime.InteropServices", "TypeIdentifierAttribute"), DmdWellKnownType.System_Runtime_InteropServices_TypeIdentifierAttribute);
			Add(new TypeName("System.Runtime.InteropServices", "BestFitMappingAttribute"), DmdWellKnownType.System_Runtime_InteropServices_BestFitMappingAttribute);
			Add(new TypeName("System.Runtime.InteropServices", "DefaultParameterValueAttribute"), DmdWellKnownType.System_Runtime_InteropServices_DefaultParameterValueAttribute);
			Add(new TypeName("System.Runtime.InteropServices", "LCIDConversionAttribute"), DmdWellKnownType.System_Runtime_InteropServices_LCIDConversionAttribute);
			Add(new TypeName("System.Runtime.InteropServices", "UnmanagedFunctionPointerAttribute"), DmdWellKnownType.System_Runtime_InteropServices_UnmanagedFunctionPointerAttribute);
			Add(new TypeName("System", "Activator"), DmdWellKnownType.System_Activator);
			Add(new TypeName("System.Threading.Tasks", "Task"), DmdWellKnownType.System_Threading_Tasks_Task);
			Add(new TypeName("System.Threading.Tasks", "Task`1"), DmdWellKnownType.System_Threading_Tasks_Task_T);
			Add(new TypeName("System.Threading", "Interlocked"), DmdWellKnownType.System_Threading_Interlocked);
			Add(new TypeName("System.Threading", "Monitor"), DmdWellKnownType.System_Threading_Monitor);
			Add(new TypeName("System.Threading", "Thread"), DmdWellKnownType.System_Threading_Thread);
			Add(new TypeName("Microsoft.CSharp.RuntimeBinder", "Binder"), DmdWellKnownType.Microsoft_CSharp_RuntimeBinder_Binder);
			Add(new TypeName("Microsoft.CSharp.RuntimeBinder", "CSharpArgumentInfo"), DmdWellKnownType.Microsoft_CSharp_RuntimeBinder_CSharpArgumentInfo);
			Add(new TypeName("Microsoft.CSharp.RuntimeBinder", "CSharpArgumentInfoFlags"), DmdWellKnownType.Microsoft_CSharp_RuntimeBinder_CSharpArgumentInfoFlags);
			Add(new TypeName("Microsoft.CSharp.RuntimeBinder", "CSharpBinderFlags"), DmdWellKnownType.Microsoft_CSharp_RuntimeBinder_CSharpBinderFlags);
			Add(new TypeName("Microsoft.VisualBasic", "CallType"), DmdWellKnownType.Microsoft_VisualBasic_CallType);
			Add(new TypeName("Microsoft.VisualBasic", "Embedded"), DmdWellKnownType.Microsoft_VisualBasic_Embedded);
			Add(new TypeName("Microsoft.VisualBasic.CompilerServices", "Conversions"), DmdWellKnownType.Microsoft_VisualBasic_CompilerServices_Conversions);
			Add(new TypeName("Microsoft.VisualBasic.CompilerServices", "Operators"), DmdWellKnownType.Microsoft_VisualBasic_CompilerServices_Operators);
			Add(new TypeName("Microsoft.VisualBasic.CompilerServices", "NewLateBinding"), DmdWellKnownType.Microsoft_VisualBasic_CompilerServices_NewLateBinding);
			Add(new TypeName("Microsoft.VisualBasic.CompilerServices", "EmbeddedOperators"), DmdWellKnownType.Microsoft_VisualBasic_CompilerServices_EmbeddedOperators);
			Add(new TypeName("Microsoft.VisualBasic.CompilerServices", "StandardModuleAttribute"), DmdWellKnownType.Microsoft_VisualBasic_CompilerServices_StandardModuleAttribute);
			Add(new TypeName("Microsoft.VisualBasic.CompilerServices", "Utils"), DmdWellKnownType.Microsoft_VisualBasic_CompilerServices_Utils);
			Add(new TypeName("Microsoft.VisualBasic.CompilerServices", "LikeOperator"), DmdWellKnownType.Microsoft_VisualBasic_CompilerServices_LikeOperator);
			Add(new TypeName("Microsoft.VisualBasic.CompilerServices", "ProjectData"), DmdWellKnownType.Microsoft_VisualBasic_CompilerServices_ProjectData);
			Add(new TypeName("Microsoft.VisualBasic.CompilerServices", "ObjectFlowControl"), DmdWellKnownType.Microsoft_VisualBasic_CompilerServices_ObjectFlowControl);
			Add(new TypeName("Microsoft.VisualBasic.CompilerServices", "ObjectFlowControl", "ForLoopControl"), DmdWellKnownType.Microsoft_VisualBasic_CompilerServices_ObjectFlowControl_ForLoopControl);
			Add(new TypeName("Microsoft.VisualBasic.CompilerServices", "StaticLocalInitFlag"), DmdWellKnownType.Microsoft_VisualBasic_CompilerServices_StaticLocalInitFlag);
			Add(new TypeName("Microsoft.VisualBasic.CompilerServices", "StringType"), DmdWellKnownType.Microsoft_VisualBasic_CompilerServices_StringType);
			Add(new TypeName("Microsoft.VisualBasic.CompilerServices", "IncompleteInitialization"), DmdWellKnownType.Microsoft_VisualBasic_CompilerServices_IncompleteInitialization);
			Add(new TypeName("Microsoft.VisualBasic.CompilerServices", "Versioned"), DmdWellKnownType.Microsoft_VisualBasic_CompilerServices_Versioned);
			Add(new TypeName("Microsoft.VisualBasic", "CompareMethod"), DmdWellKnownType.Microsoft_VisualBasic_CompareMethod);
			Add(new TypeName("Microsoft.VisualBasic", "Strings"), DmdWellKnownType.Microsoft_VisualBasic_Strings);
			Add(new TypeName("Microsoft.VisualBasic", "ErrObject"), DmdWellKnownType.Microsoft_VisualBasic_ErrObject);
			Add(new TypeName("Microsoft.VisualBasic", "FileSystem"), DmdWellKnownType.Microsoft_VisualBasic_FileSystem);
			Add(new TypeName("Microsoft.VisualBasic.ApplicationServices", "ApplicationBase"), DmdWellKnownType.Microsoft_VisualBasic_ApplicationServices_ApplicationBase);
			Add(new TypeName("Microsoft.VisualBasic.ApplicationServices", "WindowsFormsApplicationBase"), DmdWellKnownType.Microsoft_VisualBasic_ApplicationServices_WindowsFormsApplicationBase);
			Add(new TypeName("Microsoft.VisualBasic", "Information"), DmdWellKnownType.Microsoft_VisualBasic_Information);
			Add(new TypeName("Microsoft.VisualBasic", "Interaction"), DmdWellKnownType.Microsoft_VisualBasic_Interaction);
			Add(new TypeName("System", "Func`1"), DmdWellKnownType.System_Func_T);
			Add(new TypeName("System", "Func`2"), DmdWellKnownType.System_Func_T2);
			Add(new TypeName("System", "Func`3"), DmdWellKnownType.System_Func_T3);
			Add(new TypeName("System", "Func`4"), DmdWellKnownType.System_Func_T4);
			Add(new TypeName("System", "Func`5"), DmdWellKnownType.System_Func_T5);
			Add(new TypeName("System", "Func`6"), DmdWellKnownType.System_Func_T6);
			Add(new TypeName("System", "Func`7"), DmdWellKnownType.System_Func_T7);
			Add(new TypeName("System", "Func`8"), DmdWellKnownType.System_Func_T8);
			Add(new TypeName("System", "Func`9"), DmdWellKnownType.System_Func_T9);
			Add(new TypeName("System", "Func`10"), DmdWellKnownType.System_Func_T10);
			Add(new TypeName("System", "Func`11"), DmdWellKnownType.System_Func_T11);
			Add(new TypeName("System", "Func`12"), DmdWellKnownType.System_Func_T12);
			Add(new TypeName("System", "Func`13"), DmdWellKnownType.System_Func_T13);
			Add(new TypeName("System", "Func`14"), DmdWellKnownType.System_Func_T14);
			Add(new TypeName("System", "Func`15"), DmdWellKnownType.System_Func_T15);
			Add(new TypeName("System", "Func`16"), DmdWellKnownType.System_Func_T16);
			Add(new TypeName("System", "Func`17"), DmdWellKnownType.System_Func_T17);
			Add(new TypeName("System", "Action"), DmdWellKnownType.System_Action);
			Add(new TypeName("System", "Action`1"), DmdWellKnownType.System_Action_T);
			Add(new TypeName("System", "Action`2"), DmdWellKnownType.System_Action_T2);
			Add(new TypeName("System", "Action`3"), DmdWellKnownType.System_Action_T3);
			Add(new TypeName("System", "Action`4"), DmdWellKnownType.System_Action_T4);
			Add(new TypeName("System", "Action`5"), DmdWellKnownType.System_Action_T5);
			Add(new TypeName("System", "Action`6"), DmdWellKnownType.System_Action_T6);
			Add(new TypeName("System", "Action`7"), DmdWellKnownType.System_Action_T7);
			Add(new TypeName("System", "Action`8"), DmdWellKnownType.System_Action_T8);
			Add(new TypeName("System", "Action`9"), DmdWellKnownType.System_Action_T9);
			Add(new TypeName("System", "Action`10"), DmdWellKnownType.System_Action_T10);
			Add(new TypeName("System", "Action`11"), DmdWellKnownType.System_Action_T11);
			Add(new TypeName("System", "Action`12"), DmdWellKnownType.System_Action_T12);
			Add(new TypeName("System", "Action`13"), DmdWellKnownType.System_Action_T13);
			Add(new TypeName("System", "Action`14"), DmdWellKnownType.System_Action_T14);
			Add(new TypeName("System", "Action`15"), DmdWellKnownType.System_Action_T15);
			Add(new TypeName("System", "Action`16"), DmdWellKnownType.System_Action_T16);
			Add(new TypeName("System", "AttributeUsageAttribute"), DmdWellKnownType.System_AttributeUsageAttribute);
			Add(new TypeName("System", "ParamArrayAttribute"), DmdWellKnownType.System_ParamArrayAttribute);
			Add(new TypeName("System", "NonSerializedAttribute"), DmdWellKnownType.System_NonSerializedAttribute);
			Add(new TypeName("System", "STAThreadAttribute"), DmdWellKnownType.System_STAThreadAttribute);
			Add(new TypeName("System.Reflection", "DefaultMemberAttribute"), DmdWellKnownType.System_Reflection_DefaultMemberAttribute);
			Add(new TypeName("System.Runtime.CompilerServices", "DateTimeConstantAttribute"), DmdWellKnownType.System_Runtime_CompilerServices_DateTimeConstantAttribute);
			Add(new TypeName("System.Runtime.CompilerServices", "DecimalConstantAttribute"), DmdWellKnownType.System_Runtime_CompilerServices_DecimalConstantAttribute);
			Add(new TypeName("System.Runtime.CompilerServices", "IUnknownConstantAttribute"), DmdWellKnownType.System_Runtime_CompilerServices_IUnknownConstantAttribute);
			Add(new TypeName("System.Runtime.CompilerServices", "IDispatchConstantAttribute"), DmdWellKnownType.System_Runtime_CompilerServices_IDispatchConstantAttribute);
			Add(new TypeName("System.Runtime.CompilerServices", "ExtensionAttribute"), DmdWellKnownType.System_Runtime_CompilerServices_ExtensionAttribute);
			Add(new TypeName("System.Runtime.CompilerServices", "INotifyCompletion"), DmdWellKnownType.System_Runtime_CompilerServices_INotifyCompletion);
			Add(new TypeName("System.Runtime.CompilerServices", "InternalsVisibleToAttribute"), DmdWellKnownType.System_Runtime_CompilerServices_InternalsVisibleToAttribute);
			Add(new TypeName("System.Runtime.CompilerServices", "CompilerGeneratedAttribute"), DmdWellKnownType.System_Runtime_CompilerServices_CompilerGeneratedAttribute);
			Add(new TypeName("System.Runtime.CompilerServices", "AccessedThroughPropertyAttribute"), DmdWellKnownType.System_Runtime_CompilerServices_AccessedThroughPropertyAttribute);
			Add(new TypeName("System.Runtime.CompilerServices", "CompilationRelaxationsAttribute"), DmdWellKnownType.System_Runtime_CompilerServices_CompilationRelaxationsAttribute);
			Add(new TypeName("System.Runtime.CompilerServices", "RuntimeCompatibilityAttribute"), DmdWellKnownType.System_Runtime_CompilerServices_RuntimeCompatibilityAttribute);
			Add(new TypeName("System.Runtime.CompilerServices", "UnsafeValueTypeAttribute"), DmdWellKnownType.System_Runtime_CompilerServices_UnsafeValueTypeAttribute);
			Add(new TypeName("System.Runtime.CompilerServices", "FixedBufferAttribute"), DmdWellKnownType.System_Runtime_CompilerServices_FixedBufferAttribute);
			Add(new TypeName("System.Runtime.CompilerServices", "DynamicAttribute"), DmdWellKnownType.System_Runtime_CompilerServices_DynamicAttribute);
			Add(new TypeName("System.Runtime.CompilerServices", "CallSiteBinder"), DmdWellKnownType.System_Runtime_CompilerServices_CallSiteBinder);
			Add(new TypeName("System.Runtime.CompilerServices", "CallSite"), DmdWellKnownType.System_Runtime_CompilerServices_CallSite);
			Add(new TypeName("System.Runtime.CompilerServices", "CallSite`1"), DmdWellKnownType.System_Runtime_CompilerServices_CallSite_T);
			Add(new TypeName("System.Runtime.InteropServices.WindowsRuntime", "EventRegistrationToken"), DmdWellKnownType.System_Runtime_InteropServices_WindowsRuntime_EventRegistrationToken);
			Add(new TypeName("System.Runtime.InteropServices.WindowsRuntime", "EventRegistrationTokenTable`1"), DmdWellKnownType.System_Runtime_InteropServices_WindowsRuntime_EventRegistrationTokenTable_T);
			Add(new TypeName("System.Runtime.InteropServices.WindowsRuntime", "WindowsRuntimeMarshal"), DmdWellKnownType.System_Runtime_InteropServices_WindowsRuntime_WindowsRuntimeMarshal);
			Add(new TypeName("Windows.Foundation", "IAsyncAction"), DmdWellKnownType.Windows_Foundation_IAsyncAction);
			Add(new TypeName("Windows.Foundation", "IAsyncActionWithProgress`1"), DmdWellKnownType.Windows_Foundation_IAsyncActionWithProgress_T);
			Add(new TypeName("Windows.Foundation", "IAsyncOperation`1"), DmdWellKnownType.Windows_Foundation_IAsyncOperation_T);
			Add(new TypeName("Windows.Foundation", "IAsyncOperationWithProgress`2"), DmdWellKnownType.Windows_Foundation_IAsyncOperationWithProgress_T2);
			Add(new TypeName("System.Diagnostics", "Debugger"), DmdWellKnownType.System_Diagnostics_Debugger);
			Add(new TypeName("System.Diagnostics", "DebuggerDisplayAttribute"), DmdWellKnownType.System_Diagnostics_DebuggerDisplayAttribute);
			Add(new TypeName("System.Diagnostics", "DebuggerNonUserCodeAttribute"), DmdWellKnownType.System_Diagnostics_DebuggerNonUserCodeAttribute);
			Add(new TypeName("System.Diagnostics", "DebuggerHiddenAttribute"), DmdWellKnownType.System_Diagnostics_DebuggerHiddenAttribute);
			Add(new TypeName("System.Diagnostics", "DebuggerBrowsableAttribute"), DmdWellKnownType.System_Diagnostics_DebuggerBrowsableAttribute);
			Add(new TypeName("System.Diagnostics", "DebuggerStepThroughAttribute"), DmdWellKnownType.System_Diagnostics_DebuggerStepThroughAttribute);
			Add(new TypeName("System.Diagnostics", "DebuggerBrowsableState"), DmdWellKnownType.System_Diagnostics_DebuggerBrowsableState);
			Add(new TypeName("System.Diagnostics", "DebuggableAttribute"), DmdWellKnownType.System_Diagnostics_DebuggableAttribute);
			Add(new TypeName("System.Diagnostics", "DebuggableAttribute", "DebuggingModes"), DmdWellKnownType.System_Diagnostics_DebuggableAttribute__DebuggingModes);
			Add(new TypeName("System.ComponentModel", "DesignerSerializationVisibilityAttribute"), DmdWellKnownType.System_ComponentModel_DesignerSerializationVisibilityAttribute);
			Add(new TypeName("System", "IEquatable`1"), DmdWellKnownType.System_IEquatable_T);
			Add(new TypeName("System.Collections", "IList"), DmdWellKnownType.System_Collections_IList);
			Add(new TypeName("System.Collections", "ICollection"), DmdWellKnownType.System_Collections_ICollection);
			Add(new TypeName("System.Collections.Generic", "EqualityComparer`1"), DmdWellKnownType.System_Collections_Generic_EqualityComparer_T);
			Add(new TypeName("System.Collections.Generic", "List`1"), DmdWellKnownType.System_Collections_Generic_List_T);
			Add(new TypeName("System.Collections.Generic", "IDictionary`2"), DmdWellKnownType.System_Collections_Generic_IDictionary_KV);
			Add(new TypeName("System.Collections.Generic", "IReadOnlyDictionary`2"), DmdWellKnownType.System_Collections_Generic_IReadOnlyDictionary_KV);
			Add(new TypeName("System.Collections.ObjectModel", "Collection`1"), DmdWellKnownType.System_Collections_ObjectModel_Collection_T);
			Add(new TypeName("System.Collections.ObjectModel", "ReadOnlyCollection`1"), DmdWellKnownType.System_Collections_ObjectModel_ReadOnlyCollection_T);
			Add(new TypeName("System.Collections.Specialized", "INotifyCollectionChanged"), DmdWellKnownType.System_Collections_Specialized_INotifyCollectionChanged);
			Add(new TypeName("System.ComponentModel", "INotifyPropertyChanged"), DmdWellKnownType.System_ComponentModel_INotifyPropertyChanged);
			Add(new TypeName("System.ComponentModel", "EditorBrowsableAttribute"), DmdWellKnownType.System_ComponentModel_EditorBrowsableAttribute);
			Add(new TypeName("System.ComponentModel", "EditorBrowsableState"), DmdWellKnownType.System_ComponentModel_EditorBrowsableState);
			Add(new TypeName("System.Linq", "Enumerable"), DmdWellKnownType.System_Linq_Enumerable);
			Add(new TypeName("System.Linq.Expressions", "Expression"), DmdWellKnownType.System_Linq_Expressions_Expression);
			Add(new TypeName("System.Linq.Expressions", "Expression`1"), DmdWellKnownType.System_Linq_Expressions_Expression_T);
			Add(new TypeName("System.Linq.Expressions", "ParameterExpression"), DmdWellKnownType.System_Linq_Expressions_ParameterExpression);
			Add(new TypeName("System.Linq.Expressions", "ElementInit"), DmdWellKnownType.System_Linq_Expressions_ElementInit);
			Add(new TypeName("System.Linq.Expressions", "MemberBinding"), DmdWellKnownType.System_Linq_Expressions_MemberBinding);
			Add(new TypeName("System.Linq.Expressions", "ExpressionType"), DmdWellKnownType.System_Linq_Expressions_ExpressionType);
			Add(new TypeName("System.Linq", "IQueryable"), DmdWellKnownType.System_Linq_IQueryable);
			Add(new TypeName("System.Linq", "IQueryable`1"), DmdWellKnownType.System_Linq_IQueryable_T);
			Add(new TypeName("System.Xml.Linq", "Extensions"), DmdWellKnownType.System_Xml_Linq_Extensions);
			Add(new TypeName("System.Xml.Linq", "XAttribute"), DmdWellKnownType.System_Xml_Linq_XAttribute);
			Add(new TypeName("System.Xml.Linq", "XCData"), DmdWellKnownType.System_Xml_Linq_XCData);
			Add(new TypeName("System.Xml.Linq", "XComment"), DmdWellKnownType.System_Xml_Linq_XComment);
			Add(new TypeName("System.Xml.Linq", "XContainer"), DmdWellKnownType.System_Xml_Linq_XContainer);
			Add(new TypeName("System.Xml.Linq", "XDeclaration"), DmdWellKnownType.System_Xml_Linq_XDeclaration);
			Add(new TypeName("System.Xml.Linq", "XDocument"), DmdWellKnownType.System_Xml_Linq_XDocument);
			Add(new TypeName("System.Xml.Linq", "XElement"), DmdWellKnownType.System_Xml_Linq_XElement);
			Add(new TypeName("System.Xml.Linq", "XName"), DmdWellKnownType.System_Xml_Linq_XName);
			Add(new TypeName("System.Xml.Linq", "XNamespace"), DmdWellKnownType.System_Xml_Linq_XNamespace);
			Add(new TypeName("System.Xml.Linq", "XObject"), DmdWellKnownType.System_Xml_Linq_XObject);
			Add(new TypeName("System.Xml.Linq", "XProcessingInstruction"), DmdWellKnownType.System_Xml_Linq_XProcessingInstruction);
			Add(new TypeName("System.Security", "UnverifiableCodeAttribute"), DmdWellKnownType.System_Security_UnverifiableCodeAttribute);
			Add(new TypeName("System.Security.Permissions", "SecurityAction"), DmdWellKnownType.System_Security_Permissions_SecurityAction);
			Add(new TypeName("System.Security.Permissions", "SecurityAttribute"), DmdWellKnownType.System_Security_Permissions_SecurityAttribute);
			Add(new TypeName("System.Security.Permissions", "SecurityPermissionAttribute"), DmdWellKnownType.System_Security_Permissions_SecurityPermissionAttribute);
			Add(new TypeName("System", "NotSupportedException"), DmdWellKnownType.System_NotSupportedException);
			Add(new TypeName("System.Runtime.CompilerServices", "ICriticalNotifyCompletion"), DmdWellKnownType.System_Runtime_CompilerServices_ICriticalNotifyCompletion);
			Add(new TypeName("System.Runtime.CompilerServices", "IAsyncStateMachine"), DmdWellKnownType.System_Runtime_CompilerServices_IAsyncStateMachine);
			Add(new TypeName("System.Runtime.CompilerServices", "AsyncVoidMethodBuilder"), DmdWellKnownType.System_Runtime_CompilerServices_AsyncVoidMethodBuilder);
			Add(new TypeName("System.Runtime.CompilerServices", "AsyncTaskMethodBuilder"), DmdWellKnownType.System_Runtime_CompilerServices_AsyncTaskMethodBuilder);
			Add(new TypeName("System.Runtime.CompilerServices", "AsyncTaskMethodBuilder`1"), DmdWellKnownType.System_Runtime_CompilerServices_AsyncTaskMethodBuilder_T);
			Add(new TypeName("System.Runtime.CompilerServices", "AsyncStateMachineAttribute"), DmdWellKnownType.System_Runtime_CompilerServices_AsyncStateMachineAttribute);
			Add(new TypeName("System.Runtime.CompilerServices", "IteratorStateMachineAttribute"), DmdWellKnownType.System_Runtime_CompilerServices_IteratorStateMachineAttribute);
			Add(new TypeName("System.Windows.Forms", "Form"), DmdWellKnownType.System_Windows_Forms_Form);
			Add(new TypeName("System.Windows.Forms", "Application"), DmdWellKnownType.System_Windows_Forms_Application);
			Add(new TypeName("System", "Environment"), DmdWellKnownType.System_Environment);
			Add(new TypeName("System.Runtime", "GCLatencyMode"), DmdWellKnownType.System_Runtime_GCLatencyMode);
			Add(new TypeName("System", "IFormatProvider"), DmdWellKnownType.System_IFormatProvider);
			Add(new TypeName("System", "ValueTuple`1"), DmdWellKnownType.System_ValueTuple_T1);
			Add(new TypeName("System", "ValueTuple`2"), DmdWellKnownType.System_ValueTuple_T2);
			Add(new TypeName("System", "ValueTuple`3"), DmdWellKnownType.System_ValueTuple_T3);
			Add(new TypeName("System", "ValueTuple`4"), DmdWellKnownType.System_ValueTuple_T4);
			Add(new TypeName("System", "ValueTuple`5"), DmdWellKnownType.System_ValueTuple_T5);
			Add(new TypeName("System", "ValueTuple`6"), DmdWellKnownType.System_ValueTuple_T6);
			Add(new TypeName("System", "ValueTuple`7"), DmdWellKnownType.System_ValueTuple_T7);
			Add(new TypeName("System", "ValueTuple`8"), DmdWellKnownType.System_ValueTuple_TRest);
			Add(new TypeName("System.Runtime.CompilerServices", "TupleElementNamesAttribute"), DmdWellKnownType.System_Runtime_CompilerServices_TupleElementNamesAttribute);
			Add(new TypeName("System.Runtime.CompilerServices", "ReferenceAssemblyAttribute"), DmdWellKnownType.System_Runtime_CompilerServices_ReferenceAssemblyAttribute);
			Add(new TypeName("System", "ContextBoundObject"), DmdWellKnownType.System_ContextBoundObject);
			Add(new TypeName("System.Runtime.CompilerServices", "TypeForwardedToAttribute"), DmdWellKnownType.System_Runtime_CompilerServices_TypeForwardedToAttribute);
			Add(new TypeName("System.Runtime.InteropServices", "ComImportAttribute"), DmdWellKnownType.System_Runtime_InteropServices_ComImportAttribute);
			Add(new TypeName("System.Runtime.InteropServices", "DllImportAttribute"), DmdWellKnownType.System_Runtime_InteropServices_DllImportAttribute);
			Add(new TypeName("System.Runtime.InteropServices", "FieldOffsetAttribute"), DmdWellKnownType.System_Runtime_InteropServices_FieldOffsetAttribute);
			Add(new TypeName("System.Runtime.InteropServices", "InAttribute"), DmdWellKnownType.System_Runtime_InteropServices_InAttribute);
			Add(new TypeName("System.Runtime.InteropServices", "MarshalAsAttribute"), DmdWellKnownType.System_Runtime_InteropServices_MarshalAsAttribute);
			Add(new TypeName("System.Runtime.InteropServices", "OptionalAttribute"), DmdWellKnownType.System_Runtime_InteropServices_OptionalAttribute);
			Add(new TypeName("System.Runtime.InteropServices", "OutAttribute"), DmdWellKnownType.System_Runtime_InteropServices_OutAttribute);
			Add(new TypeName("System.Runtime.InteropServices", "PreserveSigAttribute"), DmdWellKnownType.System_Runtime_InteropServices_PreserveSigAttribute);
			Add(new TypeName("System", "SerializableAttribute"), DmdWellKnownType.System_SerializableAttribute);
			Add(new TypeName("System.Runtime.InteropServices", "CharSet"), DmdWellKnownType.System_Runtime_InteropServices_CharSet);
			Add(new TypeName("System.Reflection", "Assembly"), DmdWellKnownType.System_Reflection_Assembly);
			Add(new TypeName("System", "RuntimeMethodHandleInternal"), DmdWellKnownType.System_RuntimeMethodHandleInternal);
			Add(new TypeName("System", "ByReference`1"), DmdWellKnownType.System_ByReference_T);
			Add(new TypeName("System.Runtime.InteropServices", "UnmanagedType"), DmdWellKnownType.System_Runtime_InteropServices_UnmanagedType);
			Add(new TypeName("System.Runtime.InteropServices", "VarEnum"), DmdWellKnownType.System_Runtime_InteropServices_VarEnum);
			Add(new TypeName("System", "__ComObject"), DmdWellKnownType.System___ComObject);
			Add(new TypeName("System.Runtime.InteropServices.WindowsRuntime", "RuntimeClass"), DmdWellKnownType.System_Runtime_InteropServices_WindowsRuntime_RuntimeClass);
			Add(new TypeName("System", "DBNull"), DmdWellKnownType.System_DBNull);
			Add(new TypeName("System.Security.Permissions", "PermissionSetAttribute"), DmdWellKnownType.System_Security_Permissions_PermissionSetAttribute);
			Add(new TypeName("System.Diagnostics", "Debugger", "CrossThreadDependencyNotification"), DmdWellKnownType.System_Diagnostics_Debugger_CrossThreadDependencyNotification);
			Add(new TypeName("System.Diagnostics", "DebuggerTypeProxyAttribute"), DmdWellKnownType.System_Diagnostics_DebuggerTypeProxyAttribute);
			Add(new TypeName("System.Collections.Generic", "KeyValuePair`2"), DmdWellKnownType.System_Collections_Generic_KeyValuePair_T2);
			Add(new TypeName("System.Linq", "SystemCore_EnumerableDebugView"), DmdWellKnownType.System_Linq_SystemCore_EnumerableDebugView);
			Add(new TypeName("System.Linq", "SystemCore_EnumerableDebugView`1"), DmdWellKnownType.System_Linq_SystemCore_EnumerableDebugView_T);
			Add(new TypeName("System.Linq", "SystemCore_EnumerableDebugViewEmptyException"), DmdWellKnownType.System_Linq_SystemCore_EnumerableDebugViewEmptyException);

			Debug.Assert(toWellKnownType.Count == NumberOfTypes);
#if DEBUG
			foreach (var name in toWellKnownTypeName)
				Debug.Assert(name.Name != null);
#endif
		}
	}
}
