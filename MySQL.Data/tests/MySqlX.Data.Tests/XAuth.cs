﻿// Copyright (c) 2021, 2022, Oracle and/or its affiliates.
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

using MySql.Data.Common;
using MySql.Data.MySqlClient;
using MySqlX.XDevAPI;
using NUnit.Framework;
using System;

namespace MySqlX.Data.Tests
{
  public class XAuth : BaseTest
  {

    [Test, Description("User selects DEFAULT as authentication mechanism-(default user,ssl mode none,fresh connection - ensure password is not cached")]
    public void DefaultAuthNullPlugin()
    {
      if (!Platform.IsWindows()) Assert.Ignore("Check for Linux OS");
      if (!session.Version.isAtLeast(8, 0, 11)) Assert.Ignore("Test available only to MySQL Server +8.0.11");

      string pluginName = null;//default plugin
      MySqlConnectionStringBuilder builder = new MySqlConnectionStringBuilder(ConnectionString);
      builder.UserID = "testDefaultPlugin";
      builder.Password = "test";
      CreateUser(builder.UserID, builder.Password, pluginName);
      string connectionString = null, connectionStringUri = null;
      string defaultAuthPlugin = session.SQL("SHOW VARIABLES LIKE 'default_authentication_plugin'").Execute().FetchAll()[0][1].ToString();

      //Connection String
      connectionString = $"server={Host};user={builder.UserID};port={XPort};password={builder.Password}";
      using (var session1 = MySQLX.GetSession(connectionString))
      {
        Assert.AreEqual(MySqlAuthenticationMode.PLAIN, session1.Settings.Auth);
        var result = session1.SQL("SHOW SESSION STATUS LIKE 'Mysqlx_ssl_version';").Execute().FetchAll();
        Assert.True(result[0][1].ToString().Contains("TLSv1"));
      }

      connectionString = connectionString + ";ssl-mode=none";
      using (var session1 = MySQLX.GetSession(connectionString))
        Assert.AreEqual(defaultAuthPlugin == "mysql_native_password" ? MySqlAuthenticationMode.MYSQL41 : MySqlAuthenticationMode.SHA256_MEMORY, session1.Settings.Auth);

      connectionString = $"server={Host};user={builder.UserID};port={XPort};password={builder.Password};ssl-mode=VerifyCA;ssl-ca={sslCa};ssl-ca-pwd=pass;";
      using (var session1 = MySQLX.GetSession(connectionString))
        Assert.AreEqual(MySqlAuthenticationMode.PLAIN, session1.Settings.Auth);

      //invalid values
      connectionString = $"server={Host};user={builder.UserID};port={XPort};password={builder.Password};ssl-mode=required;auth=shaa256memory";
      Assert.Throws<ArgumentException>(() => MySQLX.GetSession(connectionString));

      //Uri
      connectionStringUri = "mysqlx://" + builder.UserID + ":" + builder.Password + "@" +
          builder.Server + ":" + XPort;
      using (var session1 = MySQLX.GetSession(connectionStringUri))
      {
        Assert.AreEqual(MySqlAuthenticationMode.PLAIN, session1.Settings.Auth);
        var result = session1.SQL("SHOW SESSION STATUS LIKE 'Mysqlx_ssl_version';").Execute().FetchAll();
        Assert.True(result[0][1].ToString().Contains("TLSv1"));
      }

      connectionStringUri = connectionStringUri + "?sslmode=none";
      using (var session1 = MySQLX.GetSession(connectionStringUri))
        Assert.AreEqual(defaultAuthPlugin == "mysql_native_password" ? MySqlAuthenticationMode.MYSQL41 : MySqlAuthenticationMode.SHA256_MEMORY, session1.Settings.Auth);

      //Anonymous Object
      using (var session1 = MySQLX.GetSession(new { server = builder.Server, port = XPort, user = builder.UserID, password = builder.Password }))
      {
        Assert.AreEqual(MySqlAuthenticationMode.PLAIN, session1.Settings.Auth);
        var result = session1.SQL("SHOW SESSION STATUS LIKE 'Mysqlx_ssl_version';").Execute().FetchAll();
        Assert.True(result[0][1].ToString().Contains("TLSv1"));
      }

      using (var session1 = MySQLX.GetSession(
      new
      {
        server = builder.Server,
        port = XPort,
        user = builder.UserID,
        sslmode = MySqlSslMode.Disabled,
        password = builder.Password
      }))
        Assert.AreEqual(defaultAuthPlugin == "mysql_native_password" ? MySqlAuthenticationMode.MYSQL41 : MySqlAuthenticationMode.SHA256_MEMORY, session1.Settings.Auth);

      ExecuteSQL("flush privileges");
      connectionString = $"server={Host};user={builder.UserID};port={XPort};password={builder.Password};ssl-mode=none";
      if (defaultAuthPlugin == "mysql_native_password")
        Assert.NotNull(MySQLX.GetSession(connectionString));
      else
        Assert.Throws<MySqlException>(() => MySQLX.GetSession(connectionString));

      //URI
      connectionStringUri = $"mysqlx://{builder.UserID}:{builder.Password}@{builder.Server}:{XPort}?sslmode=none";
      if (defaultAuthPlugin == "mysql_native_password")
        Assert.NotNull(MySQLX.GetSession(connectionStringUri));
      else
        Assert.Throws<MySqlException>(() => MySQLX.GetSession(connectionStringUri));

      //Anonymous Object
      if (defaultAuthPlugin == "mysql_native_password")
        Assert.NotNull(MySQLX.GetSession(new
        {
          server = Host,
          port = XPort,
          user = builder.UserID,
          sslmode = MySqlSslMode.Disabled,
          password = builder.Password
        }));
      else
        Assert.Throws<MySqlException>(() => MySQLX.GetSession(new
        {
          server = Host,
          port = XPort,
          user = builder.UserID,
          sslmode = MySqlSslMode.Disabled,
          password = builder.Password
        }));
    }

    [Test, Description("User selects DEFAULT as authentication mechanism-(default user,ssl mode none with allow public key retrieval=true,fresh connection - ensure password is not cached")]
    public void DefaultAuthPublicKeyRetrieval()
    {
      if (!session.Version.isAtLeast(8, 0, 11)) Assert.Ignore("Test available only to MySQL Server +8.0.11");

      string pluginName = "caching_sha2_password";//default plugin
      MySqlConnectionStringBuilder builder = new MySqlConnectionStringBuilder(ConnectionString);
      builder.UserID = "testDefaultPlugin";
      builder.Password = "test";
      CreateUser(builder.UserID, builder.Password, pluginName);
      string connectionString = null, connectionStringUri = null;
      //Flush Privileges
      ExecuteSQL("flush privileges");
      connectionString = "server=" + Host + ";user=" + builder.UserID + ";" + "port=" + XPort +
                         ";password=" + builder.Password + ";ssl-mode=none;allowpublickeyretrieval=true;pooling=false";
      Assert.Catch(() => MySQLX.GetSession(connectionString));
      connectionString = "server=" + Host + ";user=" + builder.UserID + ";" + "port=" + XPort +
                         ";password=" + builder.Password + ";ssl-mode=none;AllowPublicKeyRetrieval=true;pooling=false";
      Assert.Catch(() => MySQLX.GetSession(connectionString));


      ExecuteSQL("flush privileges");
      connectionStringUri = "mysqlx://" + builder.UserID + ":" + builder.Password + "@" +
           builder.Server + ":" + XPort + "?sslmode=none&allowpublickeyretrieval=true&pooling=false";
      Assert.Catch(() => MySQLX.GetSession(connectionString));
      connectionStringUri = "mysqlx://" + builder.UserID + ":" + builder.Password + "@" +
                 builder.Server + ":" + XPort + "?sslmode=none&AllowPublicKeyRetrieval=false&pooling=false";
      Assert.Catch(() => MySQLX.GetSession(connectionString));

      ExecuteSQL("flush privileges");
      Assert.Catch(() => MySQLX.GetSession(new
      {
        server = builder.Server,
        port = XPort,
        user = builder.UserID,
        sslmode = MySqlSslMode.Disabled,
        allowpublickeyretrieval = true,
        pooling = false,
        password = builder.Password
      }));
      Assert.Catch(() => MySQLX.GetSession(new
      {
        server = builder.Server,
        port = XPort,
        user = builder.UserID,
        sslmode = MySqlSslMode.Disabled,
        allowpublickeyretrieval = false,
        pooling = false,
        password = builder.Password
      }));
    }

    [Test, Description("User selects DEFAULT as authentication mechanism-mysql_native_password user,ssl mode default,fresh connection")]
    public void MySqlNativePlugin()
    {
      if (!session.Version.isAtLeast(8, 0, 11)) Assert.Ignore("Test available only to MySQL Server +8.0.11");

      var counter = session.SQL("SELECT count(*) FROM INFORMATION_SCHEMA.PLUGINS WHERE PLUGIN_NAME = 'caching_sha2_password'").Execute().FetchOne();
      if (Convert.ToInt32(counter[0]) <= 0)
        Assert.Ignore("The caching_sha2_password plugin isn't available.");

      string pluginName = "mysql_native_password";//mysql_native_password  plugin
      MySqlConnectionStringBuilder builder = new MySqlConnectionStringBuilder(ConnectionString);
      builder.UserID = "testDefaultPlugin";
      builder.Password = "test";
      CreateUser(builder.UserID, builder.Password, pluginName);
      string connectionString = null, connectionStringUri = null;
      //Connection String
      connectionString = $"server={Host};user={builder.UserID};port={XPort};password={builder.Password}";
      using (var session1 = MySQLX.GetSession(connectionString))
        Assert.AreEqual(MySqlAuthenticationMode.PLAIN, session1.Settings.Auth);

      connectionString += ";ssl-mode=VerifyCA;ssl-ca=" + sslCa + ";ssl-ca-pwd=pass;";
      using (var session1 = MySQLX.GetSession(connectionString))
        Assert.AreEqual(MySqlAuthenticationMode.PLAIN, session1.Settings.Auth);

      connectionString = $"server={Host};user={builder.UserID};port={XPort};password={builder.Password};ssl-mode=none";
      using (var session1 = MySQLX.GetSession(connectionString))
        Assert.AreEqual(MySqlAuthenticationMode.MYSQL41, session1.Settings.Auth);

      connectionString = $"server={Host};user={builder.UserID};port={XPort};password={builder.Password};ssl-mode=Required;ssl-ca={sslCa};ssl-ca-pwd=pass;";
      using (var session1 = MySQLX.GetSession(connectionString))
        Assert.AreEqual(MySqlAuthenticationMode.PLAIN, session1.Settings.Auth);

      //URI
      ExecuteSQL("flush privileges");
      connectionStringUri = $"mysqlx://{builder.UserID}:{builder.Password}@{builder.Server}:{XPort}";
      using (var session1 = MySQLX.GetSession(connectionStringUri))
        Assert.AreEqual(MySqlAuthenticationMode.PLAIN, session1.Settings.Auth);

      connectionStringUri = connectionStringUri + "?ssl-mode=none";
      using (var session1 = MySQLX.GetSession(connectionStringUri))
        Assert.AreEqual(MySqlAuthenticationMode.MYSQL41, session1.Settings.Auth);

      //Anonymous Object
      ExecuteSQL("flush privileges");
      using (var session1 = MySQLX.GetSession(new
      {
        server = builder.Server,
        port = XPort,
        user = builder.UserID,
        password = builder.Password
      }))
        Assert.AreEqual(MySqlAuthenticationMode.PLAIN, session1.Settings.Auth);

      using (var session1 = MySQLX.GetSession(new
      {
        server = builder.Server,
        port = XPort,
        user = builder.UserID,
        sslmode = MySqlSslMode.Disabled,
        password = builder.Password
      }))
        Assert.AreEqual(MySqlAuthenticationMode.MYSQL41, session1.Settings.Auth);
    }

    [Test, Description("User selects DEFAULT as authentication mechanism-sha256_password user,ssl mode none,fresh connection")]
    public void Sha256_passwordPlugin()
    {
      if (!session.Version.isAtLeast(8, 0, 11)) Assert.Ignore("Test available only to MySQL Server +8.0.11");

      var counter = session.SQL("SELECT count(*) FROM INFORMATION_SCHEMA.PLUGINS WHERE PLUGIN_NAME = 'caching_sha2_password'").Execute().FetchOne();
      if (Convert.ToInt32(counter[0]) <= 0)
        Assert.Ignore("The caching_sha2_password plugin isn't available.");
      string pluginName = "sha256_password";//sha256_password plugin
      MySqlConnectionStringBuilder builder = new MySqlConnectionStringBuilder(ConnectionString);
      builder.UserID = "testDefaultPlugin";
      builder.Password = "test";
      CreateUser(builder.UserID, builder.Password, pluginName);
      string connectionString = null, connectionStringUri = null;

      //Connection String
      connectionString = $"server={Host};user={builder.UserID};port={XPort};password={builder.Password};ssl-mode=none";
      Assert.Throws<MySqlException>(() => MySQLX.GetSession(connectionString));
      ExecuteSQL("flush privileges");
      //URI
      connectionStringUri = $"mysqlx://{builder.UserID}:{builder.Password}@{builder.Server}:{XPort}?sslmode=none";
      Assert.Throws<MySqlException>(() => MySQLX.GetSession(connectionStringUri));
      ExecuteSQL("flush privileges");
      //Anonymous Object
      Assert.Throws<MySqlException>(() => MySQLX.GetSession(new
      {
        server = builder.Server,
        port = XPort,
        user = builder.UserID,
        sslmode = MySqlSslMode.Disabled,
        password = builder.Password
      }));

      ExecuteSQL("flush privileges");
      connectionString = $"server={Host};user={builder.UserID};port={XPort};password={builder.Password}";
      using (var session1 = MySQLX.GetSession(connectionString))
        Assert.AreEqual(MySqlAuthenticationMode.PLAIN, session1.Settings.Auth);

      connectionString += $";ssl-mode=VerifyCA;ssl-ca={sslCa};ssl-ca-pwd=pass;";
      using (var session1 = MySQLX.GetSession(connectionString))
        Assert.AreEqual(MySqlAuthenticationMode.PLAIN, session1.Settings.Auth);

      builder.SslCa = sslCa;
      builder.SslCert = sslCert;
      builder.SslKey = sslKey;
      builder.CertificatePassword = sslCertificatePassword;
      builder.SslMode = MySqlSslMode.VerifyFull;
      connectionString = builder.ConnectionString;
      using (var session1 = MySQLX.GetSession(connectionString))
        Assert.AreEqual(MySqlAuthenticationMode.PLAIN, session1.Settings.Auth);
      ExecuteSQL("flush privileges");
      connectionStringUri = $"mysqlx://{builder.UserID}:{builder.Password}@{builder.Server}:{XPort}";
      using (var session1 = MySQLX.GetSession(connectionStringUri))
        Assert.AreEqual(MySqlAuthenticationMode.PLAIN, session1.Settings.Auth);

      ExecuteSQL("flush privileges");
      //Anonymous Object
      using (var session1 = MySQLX.GetSession(new
      {
        server = builder.Server,
        port = XPort,
        user = builder.UserID,
        password = builder.Password
      }))
        Assert.AreEqual(MySqlAuthenticationMode.PLAIN, session.Settings.Auth);
    }

    [TestCase("mysql_native_password")]
    [TestCase("sha256_password")]
    public void Sha256MemoryAuthWithDifferentPlugin(string pluginName)
    {
      if (!session.Version.isAtLeast(8, 0, 11)) Assert.Ignore("Test available only to MySQL Server +8.0.11");

      MySqlConnectionStringBuilder builder = new MySqlConnectionStringBuilder(ConnectionString);
      builder.UserID = "testDefaultPlugin";
      builder.Password = "test";
      CreateUser(builder.UserID, builder.Password, pluginName);
      string connectionString = null, connectionStringUri = null;
      //Connection string
      connectionString = $"server={Host};user={builder.UserID};port={XPort};password={builder.Password};auth = SHA256_MEMORY";
      Assert.Throws<MySqlException>(() => MySQLX.GetSession(connectionString));

      connectionString = $"server={Host};user={builder.UserID};port={XPort};password={builder.Password};sslmode=none;auth=SHA256_MEMORY";
      Assert.Throws<MySqlException>(() => MySQLX.GetSession(connectionString));
      ExecuteSQL("flush privileges");
      //Uri
      connectionStringUri = $"mysqlx://{builder.UserID}:{builder.Password}@{builder.Server}:{XPort}?auth=SHA256_MEMORY";
      Assert.Throws<MySqlException>(() => MySQLX.GetSession(connectionStringUri));

      connectionStringUri = $"mysqlx://{builder.UserID}:{builder.Password}@{builder.Server}:{XPort}?sslmode=none;auth=SHA256_MEMORY";
      Assert.Throws<ArgumentException>(() => MySQLX.GetSession(connectionStringUri));
      ExecuteSQL("flush privileges");
      //Anonymous Object
      Assert.Throws<MySqlException>(() => MySQLX.GetSession(new
      {
        server = builder.Server,
        port = XPort,
        user = builder.UserID,
        password = builder.Password,
        auth = MySqlAuthenticationMode.SHA256_MEMORY
      }));

      Assert.Throws<MySqlException>(() => MySQLX.GetSession(new
      {
        server = builder.Server,
        port = XPort,
        user = builder.UserID,
        password = builder.Password,
        sslmode = MySqlSslMode.Disabled,
        auth = MySqlAuthenticationMode.SHA256_MEMORY
      }));
    }

    [Test, Description("Test MySQLX plugin Extern Support")]
    public void NativeAuthValidAndInvalidConnection()
    {
      if (!Platform.IsWindows()) Assert.Ignore("Check for Linux OS");
      if (!session.Version.isAtLeast(8, 0, 11)) Assert.Ignore("Test available only to MySQL Server +8.0.11");

      var user = "testNative";
      var pwd = "test";
      var cs = $"server={Host};user={user};port={XPort};password={pwd}";
      //Connection String
      using (var session1 = MySQLX.GetSession(ConnectionString))
        Assert.AreEqual(MySqlAuthenticationMode.PLAIN, session1.Settings.Auth);

      using (var session1 = MySQLX.GetSession(cs + ";auth=mysql41"))
        Assert.AreEqual(MySqlAuthenticationMode.MYSQL41, session1.Settings.Auth);

      using (var session1 = MySQLX.GetSession(cs + ";auth=mysql41;ssl-mode=none"))
        Assert.AreEqual(MySqlAuthenticationMode.MYSQL41, session1.Settings.Auth);

      using (var session1 = MySQLX.GetSession(cs + ";auth=mysql41;ssl-mode=Required"))
        Assert.AreEqual(MySqlAuthenticationMode.MYSQL41, session1.Settings.Auth);

      using (var session1 = MySQLX.GetSession(ConnectionString + ";auth=PLAIN"))
        Assert.AreEqual(MySqlAuthenticationMode.PLAIN, session1.Settings.Auth);

      using (var session1 = MySQLX.GetSession(ConnectionString + ";auth=plain;ssl-mode=Required"))
        Assert.AreEqual(MySqlAuthenticationMode.PLAIN, session1.Settings.Auth);

      using (var session1 = MySQLX.GetSession(cs + ";ssl-mode=none"))
        Assert.AreEqual(MySqlAuthenticationMode.MYSQL41, session1.Settings.Auth);

      using (var session1 = MySQLX.GetSession(ConnectionString + ";ssl-mode=Required"))
      {
        Assert.AreEqual(MySqlAuthenticationMode.PLAIN, session1.Settings.Auth);
        var result = session.SQL("SHOW SESSION STATUS LIKE 'Mysqlx_ssl_version';").Execute().FetchAll();
        Assert.True(result[0][1].ToString().Contains("TLSv1"));
      }

      Assert.Throws<MySqlException>(() => MySQLX.GetSession(ConnectionString + ";auth=PLAIN;ssl-mode=none"));
      Assert.Throws<MySqlException>(() => MySQLX.GetSession(ConnectionString + ";auth=EXTERNAL"));
      Assert.Throws<MySqlException>(() => MySQLX.GetSession(ConnectionString + ";auth=EXTERNAL;ssl-mode=none"));
      Assert.Throws<MySqlException>(() => MySQLX.GetSession(ConnectionString + ";auth=EXTERNAL;ssl-mode=Required"));
      Assert.Throws<ArgumentException>(() => MySQLX.GetSession(ConnectionString + ";auth=INVALID"));
      Assert.Throws<ArgumentException>(() => MySQLX.GetSession(ConnectionString + ";auth=INVALID;ssl-mode=none"));
      Assert.Throws<ArgumentException>(() => MySQLX.GetSession(ConnectionString + ";auth=INVALID;ssl-mode=Required"));
      //Uri
      using (var session1 = MySQLX.GetSession(ConnectionStringUriNative))
        Assert.AreEqual(MySqlAuthenticationMode.PLAIN, session1.Settings.Auth);

      using (var session1 = MySQLX.GetSession(ConnectionStringUriNative + "?auth=MySQL41"))
        Assert.AreEqual(MySqlAuthenticationMode.MYSQL41, session1.Settings.Auth);

      using (var session1 = MySQLX.GetSession(ConnectionStringUriNative + "?auth=MySQL41&ssl-mode=none"))
        Assert.AreEqual(MySqlAuthenticationMode.MYSQL41, session1.Settings.Auth);

      using (var session1 = MySQLX.GetSession(ConnectionStringUriNative + "?auth=MySQL41&ssl-mode=Required"))
        Assert.AreEqual(MySqlAuthenticationMode.MYSQL41, session1.Settings.Auth);

      using (var session1 = MySQLX.GetSession(ConnectionStringUriNative + "?auth=PLAIN"))
        Assert.AreEqual(MySqlAuthenticationMode.PLAIN, session1.Settings.Auth);

      Assert.Throws<MySqlException>(() => MySQLX.GetSession(ConnectionStringUriNative + "?auth=PLAIN&ssl-mode=none"));

      using (var session1 = MySQLX.GetSession(ConnectionStringUriNative + "?auth=PLAIN&ssl-mode=Required"))
        Assert.AreEqual(MySqlAuthenticationMode.PLAIN, session1.Settings.Auth);

      using (var session1 = MySQLX.GetSession(ConnectionStringUriNative + "?ssl-mode=none"))
        Assert.AreEqual(MySqlAuthenticationMode.MYSQL41, session1.Settings.Auth);

      using (var session1 = MySQLX.GetSession(ConnectionStringUriNative + "?ssl-mode=Required"))
      {
        Assert.AreEqual(MySqlAuthenticationMode.PLAIN, session1.Settings.Auth);
        var result = session.SQL("SHOW SESSION STATUS LIKE 'Mysqlx_ssl_version';").Execute().FetchAll();
        Assert.True(result[0][1].ToString().Contains("TLSv1"));
      }

      Assert.Throws<MySqlException>(() => MySQLX.GetSession(ConnectionStringUriNative + "?auth=EXTERNAL"));
      Assert.Throws<MySqlException>(() => MySQLX.GetSession(ConnectionStringUriNative + "?auth=EXTERNAL&ssl-mode=none"));
      Assert.Throws<MySqlException>(() => MySQLX.GetSession(ConnectionStringUriNative + "?auth=EXTERNAL&ssl-mode=Required"));
      Assert.Throws<ArgumentException>(() => MySQLX.GetSession(ConnectionStringUriNative + "?auth=INVALID"));
      Assert.Throws<ArgumentException>(() => MySQLX.GetSession(ConnectionStringUriNative + "?auth=INVALID&ssl-mode=none"));
      Assert.Throws<ArgumentException>(() => MySQLX.GetSession(ConnectionStringUriNative + "?auth=INVALID&ssl-mode=Required"));
      //Anonymous Object
      using (var session1 = MySQLX.GetSession(new
      {
        server = Host,
        port = XPort,
        user = user,
        password = pwd
      }))
        Assert.AreEqual(MySqlAuthenticationMode.PLAIN, session1.Settings.Auth);

      using (var session1 = MySQLX.GetSession(new
      {
        server = Host,
        port = XPort,
        user = user,
        password = pwd,
        auth = MySqlAuthenticationMode.MYSQL41
      }))
        Assert.AreEqual(MySqlAuthenticationMode.MYSQL41, session1.Settings.Auth);

      using (var session1 = MySQLX.GetSession(new
      {
        server = Host,
        port = XPort,
        user = user,
        password = pwd,
        auth = MySqlAuthenticationMode.MYSQL41,
        sslmode = MySqlSslMode.Disabled
      }))
        Assert.AreEqual(MySqlAuthenticationMode.MYSQL41, session1.Settings.Auth);

      using (var session1 = MySQLX.GetSession(new
      {
        server = Host,
        port = XPort,
        user = user,
        password = pwd,
        auth = MySqlAuthenticationMode.MYSQL41,
        sslmode = MySqlSslMode.Required
      }))
        Assert.AreEqual(MySqlAuthenticationMode.MYSQL41, session1.Settings.Auth);

      using (var session1 = MySQLX.GetSession(new
      {
        server = Host,
        port = XPort,
        user = user,
        password = pwd,
        auth = MySqlAuthenticationMode.PLAIN
      }))
        Assert.AreEqual(MySqlAuthenticationMode.PLAIN, session1.Settings.Auth);

      Assert.Throws<MySqlException>(() => MySQLX.GetSession(new
      {
        server = Host,
        port = XPort,
        user = user,
        password = pwd,
        auth = MySqlAuthenticationMode.PLAIN,
        sslmode = MySqlSslMode.Disabled
      }));

      using (var session1 = MySQLX.GetSession(new
      {
        server = Host,
        port = XPort,
        user = user,
        password = pwd,
        auth = MySqlAuthenticationMode.PLAIN,
        sslmode = MySqlSslMode.Required
      }))
        Assert.AreEqual(MySqlAuthenticationMode.PLAIN, session1.Settings.Auth);

      using (var session1 = MySQLX.GetSession(new
      {
        server = Host,
        port = XPort,
        user = user,
        password = pwd,
        sslmode = MySqlSslMode.Disabled
      }))
        Assert.AreEqual(MySqlAuthenticationMode.MYSQL41, session1.Settings.Auth);

      using (var session1 = MySQLX.GetSession(new
      {
        server = Host,
        port = XPort,
        user = user,
        password = pwd,
        sslmode = MySqlSslMode.Required
      }))
      {
        Assert.AreEqual(MySqlAuthenticationMode.PLAIN, session1.Settings.Auth);
        var result = session1.SQL("SHOW SESSION STATUS LIKE 'Mysqlx_ssl_version';").Execute().FetchAll();
        Assert.True(result[0][1].ToString().Contains("TLSv1"));
      }

      Assert.Throws<MySqlException>(() => MySQLX.GetSession(new
      {
        server = Host,
        port = XPort,
        user = user,
        password = pwd,
        auth = MySqlAuthenticationMode.EXTERNAL
      }));

    }

    public string CreateUser(string userName, string password, string plugin)
    {
      string host = Host == "localhost" ? Host : "%";

      ExecuteSqlAsRoot(String.Format("DROP USER IF EXISTS '{0}'@'{1}';", userName, host));
      ExecuteSqlAsRoot(
        String.Format(
          "CREATE USER '{0}'@'{1}' IDENTIFIED {2} BY '{3}'", userName, host,
          (plugin == null ? string.Empty : String.Format("WITH '{0}' ", plugin)), password));

      ExecuteSqlAsRoot(String.Format("GRANT ALL ON *.* TO '{0}'@'{1}'", userName, host));
      ExecuteSqlAsRoot("FLUSH PRIVILEGES");
      return userName;
    }
  }
}
