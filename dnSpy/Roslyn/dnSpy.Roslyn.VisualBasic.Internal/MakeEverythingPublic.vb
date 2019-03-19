'    Copyright (C) 2014-2019 de4dot@gmail.com
'
'    This file is part of dnSpy
'
'    dnSpy is free software: you can redistribute it and/or modify
'    it under the terms of the GNU General Public License as published by
'    the Free Software Foundation, either version 3 of the License, or
'    (at your option) any later version.
'
'    dnSpy is distributed in the hope that it will be useful,
'    but WITHOUT ANY WARRANTY; without even the implied warranty of
'    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
'    GNU General Public License for more details.
'
'    You should have received a copy of the GNU General Public License
'    along with dnSpy.  If not, see <http://www.gnu.org/licenses/>.

Imports System.Runtime.CompilerServices

<Assembly: IgnoresAccessChecksTo("Microsoft.CodeAnalysis")>
<Assembly: IgnoresAccessChecksTo("Microsoft.CodeAnalysis.Workspaces")>
<Assembly: IgnoresAccessChecksTo("Microsoft.CodeAnalysis.Features")>
<Assembly: IgnoresAccessChecksTo("Microsoft.CodeAnalysis.VisualBasic")>
<Assembly: IgnoresAccessChecksTo("Microsoft.CodeAnalysis.VisualBasic.Features")>
<Assembly: IgnoresAccessChecksTo("Microsoft.CodeAnalysis.VisualBasic.Workspaces")>

Namespace Global.System.Runtime.CompilerServices
	<AttributeUsage(AttributeTargets.Assembly, AllowMultiple:=True)>
	Class IgnoresAccessChecksToAttribute
		Inherits Attribute

		Public Sub New(assemblyName As String)
			Me.AssemblyName = assemblyName
		End Sub

		Public ReadOnly Property AssemblyName As String
	End Class
End Namespace
