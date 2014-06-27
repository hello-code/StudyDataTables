using DataTables.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

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
                //var filteredColumns = param.Columns.GetFilteredColumns();

                if (!string.IsNullOrEmpty(globalSearch))
                {
                    IQueryable<T> temp = null;
                    foreach (var column in filteredColumns)
                    {
                        var t = query.Where<T>(column.Data, globalSearch);
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
            /*如果选择每页显示记录数为全部，如： "lengthMenu": [[5, 10, 25, 50, -1], [5, 10, 25, 50, "All"]],则需更改var paged*/
            List<T> paged;
            if (param.Length == -1)
            {
                paged = query.ToList();
            }
            else
            {
                paged = query.Skip(param.Start).Take(param.Length).ToList();
            }
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

    // 排序和筛选条件
    public static class MyLinqExtensions
    {
        public static IOrderedQueryable<T> OrderBy<T>(this IQueryable<T> source, string property)
        {
            return ApplyOrder<T>(source, property, "OrderBy");
        }
        public static IOrderedQueryable<T> OrderByDescending<T>(this IQueryable<T> source, string property)
        {
            return ApplyOrder<T>(source, property, "OrderByDescending");
        }
        //public static IOrderedQueryable<T> ThenBy<T>(this IOrderedQueryable<T> source, string property)
        public static IOrderedQueryable<T> ThenBy<T>(this IQueryable<T> source, string property)
        {
            return ApplyOrder<T>(source, property, "ThenBy");
        }
        //public static IOrderedQueryable<T> ThenByDescending<T>(this IOrderedQueryable<T> source, string property)
        public static IOrderedQueryable<T> ThenByDescending<T>(this IQueryable<T> source, string property)
        {
            return ApplyOrder<T>(source, property, "ThenByDescending");
        }
        static IOrderedQueryable<T> ApplyOrder<T>(IQueryable<T> source, string property, string methodName)
        {
            string[] props = property.Split('.');
            Type type = typeof(T);
            ParameterExpression arg = Expression.Parameter(type, "x");
            Expression expr = arg;
            foreach (string prop in props)
            {
                // use reflection (not ComponentModel) to mirror LINQ
                PropertyInfo pi = type.GetProperty(prop);
                expr = Expression.Property(expr, pi);
                type = pi.PropertyType;
            }
            Type delegateType = typeof(Func<,>).MakeGenericType(typeof(T), type);
            LambdaExpression lambda = Expression.Lambda(delegateType, expr, arg);
            object result = typeof(Queryable).GetMethods().Single(
                    method => method.Name == methodName
                            && method.IsGenericMethodDefinition
                            && method.GetGenericArguments().Length == 2
                            && method.GetParameters().Length == 2)
                    .MakeGenericMethod(typeof(T), type)
                    .Invoke(null, new object[] { source, lambda });
            return (IOrderedQueryable<T>)result;
        }
        public static IQueryable<T> Where<T>(this IQueryable<T> query, string column, object value)
        {
            if (string.IsNullOrEmpty(column))
                return query;
            //组建一个表达式树来创建一个参数p
            ParameterExpression parameter = Expression.Parameter(query.ElementType, "p");

            MemberExpression memberAccess = null;
            foreach (var property in column.Split('.'))
                memberAccess = MemberExpression.Property(memberAccess ?? (parameter as Expression), property);

            ConstantExpression filter = null;
            Type t = memberAccess.Type;
            Type t2 = Nullable.GetUnderlyingType(memberAccess.Type);

            // 判断各种可为null的数据类型                                                                            
            if (t.IsGenericType && t.GetGenericTypeDefinition().Equals(typeof(Nullable<>)))
            {
                //数据类型=int                                                                                       
                if (t2 == typeof(int))
                {
                    int val;
                    //return int value or null int                                                                  
                    var valNull = int.TryParse(value.ToString(), out val) ? val : (int?)null;
                    filter = Expression.Constant(valNull, t);
                }
                //数据类型=DateTime                                                                                  
                if (t2 == typeof(DateTime))
                {
                    DateTime val;
                    var valNull = DateTime.TryParse(value.ToString(), out val) ? val : (DateTime?)null;
                    filter = Expression.Constant(valNull, t);
                }
                //数据类型=Decimal
                if (t2 == typeof(Decimal))
                {
                    Decimal val;
                    var valNull = Decimal.TryParse(value.ToString(), out val) ? val : (Decimal?)null;
                    filter = Expression.Constant(valNull, t);
                }
            }
            else
                //else not nullable create ContantExpresion as normal                                               
                filter = Expression.Constant(Convert.ChangeType(value, t));

            Expression condition = null;
            LambdaExpression lambda = null;

            condition = Expression.Call(memberAccess,
                typeof(string).GetMethod("Contains", new[] { typeof(string) }),
                Expression.Constant(value));
            lambda = Expression.Lambda(condition, parameter);


            //组建表达式树:Where语句
            MethodCallExpression result = Expression.Call(
                   typeof(Queryable), "Where",
                   new[] { query.ElementType },
                   query.Expression,
                   lambda);
            //使用表达式树来生成动态查询
            return query.Provider.CreateQuery<T>(result);
        }
    }
}