using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Helper
{
    // 排序和筛选条件
    public static class LinqExtensions
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

       /// <summary>
        /// 筛选
        /// </summary>
        /// <typeparam name="T">实体类</typeparam>
        /// <param name="query">数据源</param>
        /// <param name="column">字段名</param>
        /// <param name="value">字段值</param>
        /// <param name="operation">操作符</param>
        /// <returns></returns>
        public static IQueryable<T> Where<T>(this IQueryable<T> query, string column, object value, WhereOperation operation)
        {
            if (string.IsNullOrEmpty(column))
                return query;
            //组建一个表达式树来创建一个参数p
            ParameterExpression parameter = Expression.Parameter(query.ElementType, "p");

            MemberExpression memberAccess = null;
            foreach (var property in column.Split('.'))
                memberAccess = MemberExpression.Property(memberAccess ?? (parameter as Expression), property);

            //change param value type
            //necessary to getting bool from string
            //ConstantExpression filter = Expression.Constant
            //    (
            //        Convert.ChangeType(value, memberAccess.Type)
            //    );

            //如果数据库表字段为null（可为空），则变量filter=null，以上代码遇到null值会异常，改为如下：---------------
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
            //如果数据库表字段为null（可为空），则以上代码可以避免异常。--------------------------------------------

            //switch operation
            Expression condition = null;
            LambdaExpression lambda = null;
            switch (operation)
            {
                //等於 equal ==
                case WhereOperation.Equal:
                    condition = Expression.Equal(memberAccess, filter);
                    lambda = Expression.Lambda(condition, parameter);
                    break;

                //不等於 not equal !=
                case WhereOperation.NotEqual:
                    condition = Expression.NotEqual(memberAccess, filter);
                    lambda = Expression.Lambda(condition, parameter);
                    break;

                //小於
                case WhereOperation.LessThan:
                    condition = Expression.LessThan(memberAccess, filter);
                    lambda = Expression.Lambda(condition, parameter);
                    break;

                //小於等於
                case WhereOperation.LessThanOrEqual:
                    condition = Expression.LessThanOrEqual(memberAccess, filter);
                    lambda = Expression.Lambda(condition, parameter);
                    break;

                //大於
                case WhereOperation.GreaterThan:
                    condition = Expression.GreaterThan(memberAccess, filter);
                    lambda = Expression.Lambda(condition, parameter);
                    break;

                //大於等於
                case WhereOperation.GreaterThanOrEqual:
                    condition = Expression.GreaterThanOrEqual(memberAccess, filter);
                    lambda = Expression.Lambda(condition, parameter);
                    break;

                //開始於
                case WhereOperation.BeginsWith:
                    condition = Expression.Call(memberAccess,
                        typeof(string).GetMethod("StartsWith", new[] { typeof(string) }),
                        Expression.Constant(value));
                    lambda = Expression.Lambda(condition, parameter);
                    break;

                //不開始於
                case WhereOperation.NotBeginsWith:
                    condition = Expression.Call(memberAccess,
                        typeof(string).GetMethod("StartsWith", new[] { typeof(string) }),
                        Expression.Constant(value));
                    condition = Expression.Not(condition);
                    lambda = Expression.Lambda(condition, parameter);
                    break;

                //在其中 string.Contains()
                case WhereOperation.In:
                    condition = Expression.Call(memberAccess,
                        typeof(string).GetMethod("Contains", new[] { typeof(string) }),
                        Expression.Constant(value));
                    lambda = Expression.Lambda(condition, parameter);
                    break
                        ;
                //不在其中 string.Contains()
                case WhereOperation.NotIn:
                    condition = Expression.Call(memberAccess,
                        typeof(string).GetMethod("Contains", new[] { typeof(string) }),
                        Expression.Constant(value));
                    condition = Expression.Not(condition);
                    lambda = Expression.Lambda(condition, parameter);
                    break;

                //結束於
                case WhereOperation.EndWith:
                    condition = Expression.Call(memberAccess,
                        typeof(string).GetMethod("EndsWith", new[] { typeof(string) }),
                        Expression.Constant(value));
                    lambda = Expression.Lambda(condition, parameter);
                    break;

                //不結束於
                case WhereOperation.NotEndWith:
                    condition = Expression.Call(memberAccess,
                        typeof(string).GetMethod("EndsWith", new[] { typeof(string) }),
                        Expression.Constant(value));
                    condition = Expression.Not(condition);
                    lambda = Expression.Lambda(condition, parameter);
                    break;

                //包含 string.Contains()
                case WhereOperation.Contains:
                    condition = Expression.Call(memberAccess,
                        typeof(string).GetMethod("Contains", new[] { typeof(string) }),
                        Expression.Constant(value));
                    lambda = Expression.Lambda(condition, parameter);
                    break;

                //不包含
                case WhereOperation.NotContains:
                    condition = Expression.Call(memberAccess,
                        typeof(string).GetMethod("Contains", new[] { typeof(string) }),
                        Expression.Constant(value));
                    condition = Expression.Not(condition);
                    lambda = Expression.Lambda(condition, parameter);
                    break;

                //Null
                case WhereOperation.Null:
                    condition = Expression.Call(memberAccess,
                        typeof(string).GetMethod("IsNullOrEmpty"),
                        Expression.Constant(value));
                    lambda = Expression.Lambda(condition, parameter);
                    break;

                //不Null
                case WhereOperation.NotNull:
                    condition = Expression.Call(memberAccess,
                        typeof(string).GetMethod("IsNullOrEmpty"),
                        Expression.Constant(value));
                    condition = Expression.Not(condition);
                    lambda = Expression.Lambda(condition, parameter);
                    break;
            }

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
    #region WhereOperation 搜索条件操作符
    public  enum WhereOperation
    {
        /// <summary>
        /// jqGrid的筛选条件操作符
        /// </summary>

        //等於
        [StringValue("eq")]
        Equal,

        //不等於
        [StringValue("ne")]
        NotEqual,

        //小於
        [StringValue("lt")]
        LessThan,

        //小於等於
        [StringValue("le")]
        LessThanOrEqual,

        //大於
        [StringValue("gt")]
        GreaterThan,

        //大於等於
        [StringValue("ge")]
        GreaterThanOrEqual,

        //開始於
        [StringValue("bw")]
        BeginsWith,

        //不開始於
        [StringValue("bn")]
        NotBeginsWith,

        //在其中
        [StringValue("in")]
        In,

        //不在其中
        [StringValue("ni")]
        NotIn,

        //結束於="ew"
        [StringValue("ew")]
        EndWith,

        //不結束於
        [StringValue("en")]
        NotEndWith,

        //包含
        [StringValue("cn")]
        Contains,

        //不包含
        [StringValue("nc")]
        NotContains,

        //Null
        [StringValue("nu")]
        Null,

        //不Null
        [StringValue("nn")]
        NotNull
    }
    #endregion
    #region StringValueAttribute
    /// <summary>
    /// 自定义字符串的特性
    /// </summary>
    public class StringValueAttribute : Attribute
    {
        private string _value;

        /// <summary>
        /// Creates a new <see cref="StringValueAttribute"/> instance.
        /// </summary>
        /// <param name="value">Value.</param>
        public StringValueAttribute(string value)//这里传进的value是什么？->是操作符，如eq、gt、lt...等。
        {
            _value = value;
        }

        /// <summary>
        /// Gets the value.
        /// </summary>
        /// <value></value>
        public string Value
        {
            get { return _value; }
        }
    }
    #endregion
}