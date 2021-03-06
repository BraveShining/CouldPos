﻿using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;

namespace ZlPos.Utils
{
    public class ConvertUtils
    {
        public static List<T> ToList<T>(DataTable dt) where T: class, new()
        {
            if(dt == null)
            {
                throw new Exception("datatable 数据为空");
            }
            var result = JsonConvert.DeserializeObject<List<T>>(JsonConvert.SerializeObject(dt));
            dt.Clear();dt.Dispose();dt = null;
            return result;
        }

        public static DataTable ToDataTable(IList list)
        {
            if(list == null)
            {
                throw new Exception("list 为空");
            }
            var result = JsonConvert.DeserializeObject<DataTable>(JsonConvert.SerializeObject(list));
            list.Clear(); list = null;
            return result;
        }


        //#region List convert to DataTable


        ///// <summary>
        ///// Convert a List{T} to a DataTable.
        ///// </summary>
        //public static DataTable ToDataTable<T>(List<T> items)
        //{
        //    var tb = new DataTable(typeof(T).Name);

        //    PropertyInfo[] props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        //    foreach (PropertyInfo prop in props)
        //    {
        //        Type t = GetCoreType(prop.PropertyType);
        //        tb.Columns.Add(prop.Name, t);
        //    }

        //    foreach (T item in items)
        //    {
        //        var values = new object[props.Length];

        //        for (int i = 0; i < props.Length; i++)
        //        {
        //            values[i] = props[i].GetValue(item, null);
        //        }

        //        tb.Rows.Add(values);
        //    }

        //    return tb;
        //}

        ///// <summary>
        ///// Determine of specified type is nullable
        ///// </summary>
        //public static bool IsNullable(Type t)
        //{
        //    return !t.IsValueType || (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Nullable<>));
        //}

        ///// <summary>
        ///// Return underlying type if type is Nullable otherwise return the type
        ///// </summary>
        //public static Type GetCoreType(Type t)
        //{
        //    if (t != null && IsNullable(t))
        //    {
        //        if (!t.IsValueType)
        //        {
        //            return t;
        //        }
        //        else
        //        {
        //            return Nullable.GetUnderlyingType(t);
        //        }
        //    }
        //    else
        //    {
        //        return t;
        //    }
        //}

        //public static DataTable ToDataTable<T>(IEnumerable<T> collection)
        //{
        //    var props = typeof(T).GetProperties();
        //    var dt = new DataTable();
        //    dt.Columns.AddRange(props.Select(p => new DataColumn(p.Name, p.PropertyType)).ToArray());
        //    if (collection.Count() > 0)
        //    {
        //        for (int i = 0; i < collection.Count(); i++)
        //        {
        //            ArrayList tempList = new ArrayList();
        //            foreach (PropertyInfo pi in props)
        //            {
        //                object obj = pi.GetValue(collection.ElementAt(i), null);
        //                tempList.Add(obj);
        //            }
        //            object[] array = tempList.ToArray();
        //            dt.LoadDataRow(array, true);
        //        }
        //    }
        //    return dt;
        //}
        //#endregion

        //#region DataTable convert list

        ///// <summary>
        ///// DataTable convert to List
        ///// </summary>
        ///// <typeparam name="T"></typeparam>
        ///// <param name="table"></param>
        ///// <returns></returns>
        //public static IList<T> ConvertTo<T>(DataTable table)
        //{
        //    if (table == null)
        //    {
        //        return null;
        //    }

        //    List<DataRow> rows = new List<DataRow>();

        //    foreach (DataRow row in table.Rows)
        //    {
        //        rows.Add(row);
        //    }

        //    return ConvertTo<T>(rows);
        //}

        //public static IList<T> ConvertTo<T>(IList<DataRow> rows)
        //{
        //    IList<T> list = null;

        //    if (rows != null)
        //    {
        //        list = new List<T>();

        //        foreach (DataRow row in rows)
        //        {
        //            T item = CreateItem<T>(row);
        //            list.Add(item);
        //        }
        //    }

        //    return list;
        //}

        //public static T CreateItem<T>(DataRow row)
        //{
        //    T obj = default(T);
        //    if (row != null)
        //    {
        //        obj = Activator.CreateInstance<T>();

        //        foreach (DataColumn column in row.Table.Columns)
        //        {
        //            PropertyInfo prop = obj.GetType().GetProperty(column.ColumnName);
        //            try
        //            {
        //                object value = row[column.ColumnName];
        //                prop.SetValue(obj, value, null);
        //            }
        //            catch
        //            {  //You can log something here     
        //               //throw;    
        //            }
        //        }
        //    }

        //    return obj;
        //}
        //#endregion
    }
}

