using DataTables.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Helper
{
    public class DataTablesHelper
    {
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="param"></param>
        /// <param name="totalRecords"></param>
        /// <param name="query"></param>
        /// <returns></returns>
        public static string GetQuery<T>(IDataTablesRequest param, int totalRecords, IQueryable<T> query)
        {
            //Global search
            if (!string.IsNullOrEmpty(param.Search.Value))
            {
                string globalSearch = param.Search.Value;
                var filteredColumns = param.Columns.GetSearchableColumns();
                if (!string.IsNullOrEmpty(globalSearch))
                {
                    IQueryable<T> temp = null;
                    foreach (var column in filteredColumns)
                    {
                        var t = query.Where<T>(column.Data, globalSearch, WhereOperation.Contains);
                        if (temp == null)
                            temp = t;
                        else
                            temp = temp.Union<T>(t);
                        //temp = temp.Concat<Material>(t);//使用union也可以查出数据
                    }
                    query = temp.Distinct<T>();//需要Distinct吗？
                }
            }

            // sorting
            var sortedColumns = param.Columns.GetSortedColumns();
            var isSorted = false;
            foreach (var column in sortedColumns)
            {
                if (!isSorted)
                {
                    // Apply first sort.
                    if (column.SortDirection == Column.OrderDirection.Ascendant)
                        query = query.OrderBy<T>(column.Data);
                    else
                        query = query.OrderByDescending<T>(column.Data);
                    isSorted = true;
                }
                else
                {
                    // Apply more sort
                    if (column.SortDirection == Column.OrderDirection.Ascendant)
                        query = query.ThenBy<T>(column.Data);
                    else
                        query = query.ThenByDescending(column.Data);
                }
            }

            // counter after filter
            var count = query.Count();
            
            //paging
            var paged = query.Skip(param.Start).Take(param.Length).ToList();
            var result = new DataTablesResponse
                        (
                          param.Draw,
                          paged,
                          count,    //filtered counter
                          totalRecords//before filter
                        );
            return Newtonsoft.Json.JsonConvert.SerializeObject(result);
        }
    }
}