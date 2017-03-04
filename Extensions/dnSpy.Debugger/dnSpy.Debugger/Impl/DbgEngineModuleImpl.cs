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
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.Engine;

namespace dnSpy.Debugger.Impl {
	sealed class DbgEngineModuleImpl : DbgEngineModule {
		public override DbgModule Module => module;
		readonly DbgModuleImpl module;

		public DbgEngineModuleImpl(DbgModuleImpl module) => this.module = module ?? throw new ArgumentNullException(nameof(module));

		public override void Remove() => module.Remove();

		public override void Update(UpdateOptions options, bool isExe, ulong address, uint size, DbgImageLayout imageLayout, string name, string filename, string realFilename, bool isDynamic, bool isInMemory, bool? isOptimized, int order, DateTime? timestamp, string version) => module.Process.DbgManager.DispatcherThread.BeginInvoke(() => {
			if (module.IsClosed)
				return;
			if ((options & UpdateOptions.IsExe) != 0)
				module.UpdateIsExe_DbgThread(isExe);
			if ((options & UpdateOptions.Address) != 0)
				module.UpdateAddress_DbgThread(address);
			if ((options & UpdateOptions.Size) != 0)
				module.UpdateSize_DbgThread(size);
			if ((options & UpdateOptions.ImageLayout) != 0)
				module.UpdateImageLayout_DbgThread(imageLayout);
			if ((options & UpdateOptions.Name) != 0)
				module.UpdateName_DbgThread(name);
			if ((options & UpdateOptions.Filename) != 0)
				module.UpdateFilename_DbgThread(filename);
			if ((options & UpdateOptions.RealFilename) != 0)
				module.UpdateRealFilename_DbgThread(realFilename);
			if ((options & UpdateOptions.IsDynamic) != 0)
				module.UpdateIsDynamic_DbgThread(isDynamic);
			if ((options & UpdateOptions.IsInMemory) != 0)
				module.UpdateIsInMemory_DbgThread(isInMemory);
			if ((options & UpdateOptions.IsOptimized) != 0)
				module.UpdateIsOptimized_DbgThread(isOptimized);
			if ((options & UpdateOptions.Order) != 0)
				module.UpdateOrder_DbgThread(order);
			if ((options & UpdateOptions.Timestamp) != 0)
				module.UpdateTimestamp_DbgThread(timestamp);
			if ((options & UpdateOptions.Version) != 0)
				module.UpdateVersion_DbgThread(version);
		});
	}
}
