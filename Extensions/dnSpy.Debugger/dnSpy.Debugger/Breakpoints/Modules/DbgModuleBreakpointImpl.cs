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
using System.Text.RegularExpressions;
using dnSpy.Contracts.Debugger.Breakpoints.Modules;
using dnSpy.Debugger.Utilities;

namespace dnSpy.Debugger.Breakpoints.Modules {
	sealed class DbgModuleBreakpointImpl : DbgModuleBreakpoint {
		public override DbgModuleBreakpointSettings Settings {
			get {
				lock (lockObj)
					return settings;
			}
			set => owner.Modify(this, value);
		}

		readonly object lockObj;
		readonly DbgModuleBreakpointsServiceImpl owner;
		DbgModuleBreakpointSettings settings;

		public DbgModuleBreakpointImpl(DbgModuleBreakpointsServiceImpl owner, DbgModuleBreakpointSettings settings) {
			lockObj = new object();
			this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
			this.settings = settings;
		}

		internal void WriteSettings(DbgModuleBreakpointSettings newSettings) {
			lock (lockObj) {
				if (settings.ModuleName != newSettings.ModuleName)
					moduleNameRegexWeakRef = null;
				if (settings.AppDomainName != newSettings.AppDomainName)
					appDomainNameRegexWeakRef = null;
				if (settings.ProcessName != newSettings.ProcessName)
					processNameRegexWeakRef = null;
				settings = newSettings;
			}
		}

		internal bool IsMatch(DbgBreakpointModule module) {
			lock (lockObj) {
				if (!settings.IsEnabled)
					return false;
				if (settings.IsDynamic != null && settings.IsDynamic.Value != module.IsDynamic)
					return false;
				if (settings.IsInMemory != null && settings.IsInMemory.Value != module.IsInMemory)
					return false;
				if (settings.Order != null && settings.Order.Value != module.Order)
					return false;
				if (!WildcardsMatch(settings.ModuleName, module.ModuleName, ref moduleNameRegexWeakRef))
					return false;
				if (!WildcardsMatch(settings.AppDomainName, module.AppDomainName, ref appDomainNameRegexWeakRef))
					return false;
				if (!WildcardsMatch(settings.ProcessName, module.ProcessName, ref processNameRegexWeakRef))
					return false;
			}
			return true;
		}
		WeakReference moduleNameRegexWeakRef;
		WeakReference appDomainNameRegexWeakRef;
		WeakReference processNameRegexWeakRef;

		bool WildcardsMatch(string wildcardsString, string value, ref WeakReference regexWeakRef) {
			if (string.IsNullOrEmpty(wildcardsString))
				return true;
			var regex = regexWeakRef?.Target as Regex;
			if (regex == null)
				regexWeakRef = new WeakReference(regex = WildcardsUtils.CreateRegex(wildcardsString));
			return regex.IsMatch(value ?? string.Empty);
		}

		public override void Remove() => owner.Remove(this);
	}
}
