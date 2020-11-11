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

namespace dnSpy.Debugger.DotNet.Metadata {
	static class KnownMemberNames {
		public static readonly string[] builderFieldNames = new string[] {
			// Roslyn C#
			"<>t__builder",
			// Roslyn Visual Basic
			"$Builder",
			// Mono mcs
			"$builder",
		};

		// System.Runtime.CompilerServices.AsyncTaskMethodBuilder
		public const string AsyncTaskMethodBuilder_Builder_FieldName = "m_builder";

		// System.Runtime.CompilerServices.AsyncTaskMethodBuilder<TResult>
		public const string Builder_Task_FieldName = "m_task";

		// At least these method builders:
		// System.Runtime.CompilerServices.AsyncIteratorMethodBuilder
		// System.Runtime.CompilerServices.AsyncTaskMethodBuilder
		// System.Runtime.CompilerServices.AsyncTaskMethodBuilder<TResult>
		// System.Runtime.CompilerServices.AsyncVoidMethodBuilder
		public const string Builder_ObjectIdForDebugger_PropertyName = "ObjectIdForDebugger";

		// At least these method builders:
		// System.Runtime.CompilerServices.AsyncTaskMethodBuilder
		// System.Runtime.CompilerServices.AsyncTaskMethodBuilder<TResult>
		// System.Runtime.CompilerServices.AsyncVoidMethodBuilder
		public const string Builder_Task_PropertyName = "Task";

		// System.Threading.Tasks.ValueTask<TResult>
		public const string ValueTask_Task_FieldName = "_task";// 4.0.0-rc2-24027 - 4.5.0-preview1-26216-02
		public const string ValueTask_Obj_FieldName = "_obj";// 4.5.0-preview2-26406-04 - present

		// System.Exception
		public const string Exception_Message_FieldName = "_message";
		public const string Exception_Message_FieldName_Mono = "message";

		// System.Threading.Thread
		public const string Thread_ManagedThreadId_FieldName1 = "m_ManagedThreadId";
		public const string Thread_ManagedThreadId_FieldName2 = "_managedThreadId";// .NET since early 2019
		public const string Thread_Name_FieldName1 = "m_Name";
		public const string Thread_Name_FieldName2 = "_name";// .NET since early 2019

		// System.Nullable<T>
		public const string Nullable_HasValue_FieldName = "hasValue";
		public const string Nullable_HasValue_FieldName_Mono = "has_value";
		public const string Nullable_Value_FieldName = "value";

		// System.Decimal
		public const string Decimal_Flags_FieldName = "flags";
		public const string Decimal_Hi_FieldName = "hi";
		public const string Decimal_Lo_FieldName = "lo";
		public const string Decimal_Mid_FieldName = "mid";

		// System.DateTime
		public const string DateTime_DateData_FieldName1 = "dateData";// .NET Framework, Mono
		public const string DateTime_DateData_FieldName2 = "_dateData";// .NET
		public const string DateTime_Ticks_FieldName_Mono = "ticks";
		public const string DateTime_Kind_FieldName_Mono = "kind";

		// System.Collections.DictionaryEntry
		public const string DictionaryEntry_Key_FieldName = "_key";
		public const string DictionaryEntry_Value_FieldName = "_value";

		// System.Collections.Generic.KeyValuePair<TKey, TValue>
		public const string KeyValuePair_Key_FieldName = "key";
		public const string KeyValuePair_Value_FieldName = "value";

		// Microsoft.CSharp.RuntimeBinder.DynamicMetaObjectProviderDebugView.DynamicProperty
		public const string DynamicProperty_Name_FieldName = "name";
		public const string DynamicProperty_Value_FieldName = "value";

		// System.Linq.SystemCore_EnumerableDebugViewEmptyException, thrown by
		//		System.Linq.SystemCore_EnumerableDebugView
		//		System.Linq.SystemCore_EnumerableDebugView<T>
		public const string SystemCore_EnumerableDebugViewEmptyException_Empty_PropertyName = "Empty";
	}
}
