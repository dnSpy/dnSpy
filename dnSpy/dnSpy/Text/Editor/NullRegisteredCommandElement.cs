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

using System;
using dnSpy.Contracts.Command;

namespace dnSpy.Text.Editor {
	sealed class NullRegisteredCommandElement : IRegisteredCommandElement {
		public static readonly NullRegisteredCommandElement Instance = new NullRegisteredCommandElement();
		public ICommandTargetCollection CommandTarget => NullCommandTargetCollection.Instance;
		public void Unregister() { }
	}

	sealed class NullCommandTargetCollection : ICommandTargetCollection {
		public static readonly NullCommandTargetCollection Instance = new NullCommandTargetCollection();
		public void AddFilter(ICommandTargetFilter filter, double order) { }
		public CommandTargetStatus CanExecute(Guid group, int cmdId) => CommandTargetStatus.NotHandled;
		public CommandTargetStatus Execute(Guid group, int cmdId, object args = null) => CommandTargetStatus.NotHandled;
		public CommandTargetStatus Execute(Guid group, int cmdId, object args, ref object result) => CommandTargetStatus.NotHandled;
		public void RemoveFilter(ICommandTargetFilter filter) { }
	}
}
