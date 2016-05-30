/*
    Copyright (C) 2014-2016 de4dot@gmail.com

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
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using dnSpy.Contracts.Command;

namespace dnSpy.Commands {
	[Export(typeof(ICommandManager))]
	sealed class CommandManager : ICommandManager {
		readonly Lazy<ICommandInfoCreator, ICommandInfoCreatorMetadata>[] commandInfoCreators;
		readonly ICommandTarget[] commandTargets;

		[ImportingConstructor]
		CommandManager([ImportMany] IEnumerable<Lazy<ICommandInfoCreator, ICommandInfoCreatorMetadata>> commandInfoCreators, [ImportMany] IEnumerable<ICommandTarget> commandTargets) {
			this.commandInfoCreators = commandInfoCreators.OrderBy(a => a.Metadata.Order).ToArray();
			this.commandTargets = commandTargets.OrderBy(a => a.Order).ToArray();
		}

		public IRegisteredCommandElement Register(UIElement sourceElement, object owner) {
			if (sourceElement == null)
				throw new ArgumentNullException(nameof(sourceElement));
			if (owner == null)
				throw new ArgumentNullException(nameof(owner));
			return new RegisteredCommandElement(this, sourceElement, owner);
		}

		internal CommandInfo? GetCommand(KeyEventArgs e, object owner) {
			if (e == null)
				throw new ArgumentNullException(nameof(e));
			if (owner == null)
				throw new ArgumentNullException(nameof(owner));

			foreach (var creator in commandInfoCreators) {
				var info = creator.Value.Create(e, owner);
				if (info != null)
					return info;
			}

			return null;
		}

		internal CommandTargetStatus CanExecute(Guid group, int cmdId) {
			foreach (var ct in commandTargets) {
				var res = ct.CanExecute(group, cmdId);
				if (res == CommandTargetStatus.Handled)
					return res;
				Debug.Assert(res == CommandTargetStatus.NotHandled);
			}
			return CommandTargetStatus.NotHandled;
		}

		internal CommandTargetStatus Execute(Guid group, int cmdId, object args, ref object result) {
			foreach (var ct in commandTargets) {
				result = null;
				var res = ct.Execute(group, cmdId, args, ref result);
				if (res == CommandTargetStatus.Handled)
					return res;
				Debug.Assert(res == CommandTargetStatus.NotHandled);
			}
			result = null;
			return CommandTargetStatus.NotHandled;
		}
	}
}
