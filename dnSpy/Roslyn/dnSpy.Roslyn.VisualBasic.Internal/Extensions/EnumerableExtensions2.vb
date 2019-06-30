' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Global.dnSpy.Roslyn.Utilities2
	Module EnumerableExtensions2
		<System.Runtime.CompilerServices.Extension()>
		Public Function WhereNotNull(Of T As Class)(source As IEnumerable(Of T)) As IEnumerable(Of T)
			If source Is Nothing Then
				Return Array.Empty(Of T)()
			End If
			Return source.Where(s_notNullTest)
		End Function
		Private s_notNullTest As Func(Of Object, Boolean) = Function(x As Object) x IsNot Nothing
	End Module
End Namespace
