//
// QueryField.cs
//
// Authors:
//   Gabriel Burt <gburt@novell.com>
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2007-2008 Novell, Inc.
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
using System.Text;
using System.Collections.Generic;

namespace Hyena.Query
{
    public class QueryField : IAliasedObject
    {
        private bool no_custom_format;
        private bool column_lowered;

        private Type [] value_types;
        public Type [] ValueTypes {
            get { return value_types; }
        }

        private string name;
        public string Name {
            get { return name; }
            set { name = value; }
        }

        private string label;
        public string Label {
            get { return label; }
            set { label = value; }
        }

        private string [] aliases;
        public string [] Aliases {
            get { return aliases; }
        }

        public string PrimaryAlias {
            get { return aliases[0]; }
        }

        private string column;
        public string Column {
            get { return column; }
        }

        private bool is_default;
        public bool IsDefault {
            get { return is_default; }
        }

        public QueryField (string name, string label, string column, params string [] aliases)
            : this (name, label, column, false, aliases)
        {
        }

        public QueryField (string name, string label, string column, bool isDefault, params string [] aliases)
            : this (name, label, column, new Type [] {typeof(StringQueryValue)}, isDefault, aliases)
        {
        }

        public QueryField (string name, string label, string column, Type valueType, params string [] aliases)
            : this (name, label, column, new Type [] {valueType}, false, aliases)
        {
        }

        public QueryField (string name, string label, string column, Type [] valueTypes, params string [] aliases)
            : this (name, label, column, valueTypes, false, aliases)
        {
        }

        public QueryField (string name, string label, string column, Type [] valueTypes, bool isDefault, params string [] aliases)
        {
            this.name = name;
            this.label = label;
            this.column = column;
            this.value_types = valueTypes;
            this.is_default = isDefault;
            this.aliases = aliases;

            this.no_custom_format = (Column.IndexOf ("{0}") == -1 && Column.IndexOf ("{1}") == -1);
            this.column_lowered = (Column.IndexOf ("Lowered") != -1);

            if (!no_custom_format) {
                // Ensure we have parens around any custom 'columns' that may be an OR of two columns
                this.column = String.Format ("({0})", this.column);
            }

            foreach (Type value_type in valueTypes) {
                QueryValue.AddValueType (value_type);
            }
        }

        public IEnumerable<QueryValue> CreateQueryValues ()
        {
            foreach (Type type in ValueTypes) {
                yield return Activator.CreateInstance (type) as QueryValue;
            }
        }

        public string ToTermString (string op, string value)
        {
            return ToTermString (PrimaryAlias, op, value);
        }

        public string ToSql (Operator op, QueryValue qv)
        {
            return ToSql (op, qv, false);
        }
        
        public string ToSql (Operator op, QueryValue qv, bool caseSensitive)
        {
            string value = qv.ToSql () ?? String.Empty;

            if (op == null) op = qv.OperatorSet.First;

            StringBuilder sb = new StringBuilder ();

            if (no_custom_format) {
                if (qv is StringQueryValue) {
                    if (column_lowered || !caseSensitive) {
                        // The column is pre-lowered, no need to call lower() in SQL
                        sb.AppendFormat ("{0} {1}", Column, String.Format (op.SqlFormat, value.ToLower ()));
                    } else {
                        // Match string values literally and against a lower'd version.  Mostly a workaround
                        // the fact that Sqlite's lower() method only works for ASCII (meaning even with this,
                        // we're not getting 100% case-insensitive matching).
                        sb.AppendFormat ("({0} {1} {3} LOWER({0}) {2})", Column,
                            String.Format (op.SqlFormat, value),
                            String.Format (op.SqlFormat, value.ToLower ()),
                            op.IsNot ? "AND" : "OR"
                        );
                    }
                } else {
                    sb.AppendFormat ("{0} {1}", Column, String.Format (op.SqlFormat, value));
                }

                if (op.IsNot) {
                    return String.Format ("({0} OR {1} IS NULL)", sb.ToString (), Column);
                } else {
                    return String.Format ("({0} AND {1} IS NOT NULL)", sb.ToString (), Column);
                }
            } else {
                sb.AppendFormat (
                    Column, String.Format (op.SqlFormat, value),
                    value, op.IsNot ? "NOT" : null
                );
            }

            return sb.ToString ();
        }

        public static string ToTermString (string alias, string op, string value)
        {
            value = String.Format (
                "{1}{0}{1}", 
                value, value.IndexOf (" ") == -1 ? String.Empty : "\""
            );

            return String.IsNullOrEmpty (alias)
                ? value
                : String.Format ("{0}{1}{2}", alias, op, value);
        }
    }
}
