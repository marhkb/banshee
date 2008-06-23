//
// FilterModelProvider.cs
//
// Authors:
//   Gabriel Burt <gburt@novell.com>
//
// Copyright (C) 2008 Novell, Inc.
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Reflection;

using Hyena.Data.Sqlite;

namespace Banshee.Collection.Database
{
    public class FilterModelProvider<T> : SqliteModelProvider<T> where T : new()
    {
        private string table_name;
        
        public FilterModelProvider (HyenaSqliteConnection conn, string table_name, string pk_column, PropertyInfo pk_info, string value_column, PropertyInfo value_info) : base (conn)
        {
            this.table_name = table_name;
            PrimaryKey = pk_column;
            
            DatabaseColumnAttribute pk_attr = new DatabaseColumnAttribute ();
            pk_attr.Constraints = DatabaseColumnConstraints.PrimaryKey;
            AddColumn (new DatabaseColumn (pk_info, pk_attr), true);

            AddColumn (new DatabaseColumn (value_info, new DatabaseColumnAttribute ()), true);
            
            select = String.Format ("ifnull({0}, '') as Value", value_column);
        }
        
        public override string TableName { get { return table_name; } }
        
        private string select;
        public override string Select {
            get { return select; }
        }
    }
}