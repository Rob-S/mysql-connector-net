﻿// Copyright © 2017, 2020, Oracle and/or its affiliates. All rights reserved.
//
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License, version 2.0, as
// published by the Free Software Foundation.
//
// This program is also distributed with certain software (including
// but not limited to OpenSSL) that is licensed under separate terms,
// as designated in a particular file or component or in included license
// documentation.  The authors of MySQL hereby grant you an
// additional permission to link the program and your derivative works
// with the separately licensed software that they have included with
// MySQL.
//
// Without limiting anything contained in the foregoing, this file,
// which is part of MySQL Connector/NET, is also subject to the
// Universal FOSS Exception, version 1.0, a copy of which can be found at
// http://oss.oracle.com/licenses/universal-foss-exception.
//
// This program is distributed in the hope that it will be useful, but
// WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
// See the GNU General Public License, version 2.0, for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program; if not, write to the Free Software Foundation, Inc.,
// 51 Franklin St, Fifth Floor, Boston, MA 02110-1301  USA

using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Scaffolding.Metadata;
using MySql.Data.EntityFrameworkCore.Scaffolding.Internal;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using System.Diagnostics;
using MySql.Data.EntityFrameworkCore.Diagnostics.Internal;
using Microsoft.EntityFrameworkCore.TestUtilities;
using Microsoft.EntityFrameworkCore.Scaffolding;
using MySql.EntityFrameworkCore.Basic.Tests.Utils;
using MySql.Data.EntityFrameworkCore.Infrastructure.Internal;
using MySql.Data.EntityFrameworkCore.Internal;

namespace MySql.EntityFrameworkCore.Design.Tests
{
  public class MySQLDatabaseModelFixture : SharedStoreFixtureBase<PoolableDbContext>
  {
    protected override string StoreName { get; } = nameof(MySQLDatabaseModelFactoryTest);
    protected override ITestStoreFactory TestStoreFactory => MySQLTestStoreFactory.Instance;
    public new MySQLTestStore TestStore => (MySQLTestStore)base.TestStore;

    protected override bool ShouldLogCategory(string logCategory)
        => logCategory == DbLoggerCategory.Scaffolding.Name;

    internal static DiagnosticsLogger<DbLoggerCategory.Scaffolding> _logger;
    internal IMySQLOptions options = new MySQLOptions();

    public DatabaseModel CreateModel(string dbName, string sql, IEnumerable<string> tables, IEnumerable<string> schemas, bool executeScript = false)
    {
      if (executeScript)
        MySQLTestStore.ExecuteScript(sql);
      else
        MySQLTestStore.Execute(sql);

      _logger = new DiagnosticsLogger<DbLoggerCategory.Scaffolding>(
                        ListLoggerFactory,
                        new LoggingOptions(),
                        new DiagnosticListener("Fake"),
                        new MySQLLoggingDefinitions());

      return new MySQLDatabaseModelFactory(_logger, options).
             Create(MySQLTestStore.rootConnectionString + ";database=" + dbName + ";",
             new DatabaseModelFactoryOptions(tables, schemas));
    }
  }
}