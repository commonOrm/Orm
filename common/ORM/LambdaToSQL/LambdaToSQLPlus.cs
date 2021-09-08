using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using System.Data;
using System.Data.SqlClient;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations.Schema;

namespace common.ORM.LambdaToSQL
{
    public class LambdaToSQLPlus
    {
        private int? sign;
        private SQLSort sqlsort { get; set; }
        private LambdaExpression func { get; set; }
        private int ColumnPrefix { get; set; }//0：自动判断（单表默认不加，多表默认加t模式）   1：加入 t.  2：加入 表名.

        private Dictionary<string, string> TAndTableName { get; set; }
        private List<Object> ListTree = new List<object>();
        ////////////////////////////////////////////////////////////////////////////////
        public string Lambda_Sql = "";
        public SqlParameter[] Lambda_SPArr = new SqlParameter[] { };
        ////////////////////////////////////////////////////////////////////////////////

        public LambdaToSQLPlus(SQLSort _sqlsort, LambdaExpression _func, int? _sign)
        {
            this.sign = _sign;
            this.sqlsort = _sqlsort;
            switch (this.sqlsort)
            {
                case SQLSort.SQLFields: this.sign = this.sign ?? 3000; break;
                case SQLSort.SQLWhere: this.sign = this.sign ?? 7000; break;
                case SQLSort.SQLOrder: this.sign = this.sign ?? 10000; break;
            }
            this.func = _func;
            this.ColumnPrefix = 0;
            if (this.func == null) throw new Exception("func is null");

            TAndTableName = new Dictionary<string, string>();
            foreach (var t in func.Parameters) { TAndTableName.Add(t.Name, t.Type.Name); }

            CreateWhereTree();
            ToSQLWithParameter();
        }

        private void CreateWhereTree()
        {
            if (func.Body is BinaryExpression)//表示包含二元运算符的表达式 比如 || &&
            {
                BinaryExpression be = ((BinaryExpression)func.Body);
                ListTree.Add(BinarExpressionProvider(be.Left, be.Right, be.NodeType));
            }
            else if (func.Body is MethodCallExpression)//表示对静态方法或实例方法的调用 比如 lb_Like
            {
                ListTree.Add(ExpressionRouter_Method((MethodCallExpression)func.Body));
            }
        }

        private object BinarExpressionProvider(Expression left, Expression right, ExpressionType type)
        {
            string Type = ExpressionTypeCast(type);//运算符
            /////////////////////////////////////////////////////////////////////////////////////////////////
            /////////////////////////////////////////////////////////////////////////////////////////////////
            //Left左边
            object obj1 = null;
            if (left is BinaryExpression)//表示包含二元运算符的表达式 比如 || &&
            {
                BinaryExpression be = ((BinaryExpression)left);
                obj1 = BinarExpressionProvider(be.Left, be.Right, be.NodeType);
            }
            else if (left is MemberExpression)//表示访问字段或属性, 那右边一定是值
            {
                return ExpressionRouter_Member((MemberExpression)left, right, Type);
            }
            else if (left is UnaryExpression)//表示访问字段或属性, 那右边一定是值 [处理==右边是比如int?这种带问号的表达式]
            {
                MemberExpression _left = (MemberExpression)(((UnaryExpression)left).Operand);
                return ExpressionRouter_Member(_left, right, Type);
            }
            else if (left is MethodCallExpression)//表示对静态方法或实例方法的调用 比如 lb_Like
            {
                obj1 = ExpressionRouter_Method((MethodCallExpression)left);
            }
            if (obj1 == null)
                throw new Exception("Lambda [Left] is Null, Check Has Type?");
            /////////////////////////////////////////////////////////////////////////////////////////////////
            /////////////////////////////////////////////////////////////////////////////////////////////////
            //Right右边
            object obj2 = null;
            if (right is BinaryExpression)//表示包含二元运算符的表达式 比如 || &&
            {
                BinaryExpression be = ((BinaryExpression)right);
                obj2 = BinarExpressionProvider(be.Left, be.Right, be.NodeType);
            }
            else if (right is MethodCallExpression)//表示对静态方法或实例方法的调用 比如 lb_Like
            {
                obj2 = ExpressionRouter_Method((MethodCallExpression)right);
            }
            return new List<Object> { obj1, Type, obj2 };
        }
        private Lambda_Column ExpressionRouter_Member(MemberExpression left, Expression right, string Type)
        {//表示访问字段或属性,那右边一定是值
            Lambda_Column column = GetColumnName(left);
            column.TypeCast = Type;

            if (right is MemberExpression)
            {//右边有可能是一个字段 比如多表联查时 on t.number = t2.number
                MemberExpression me = ((MemberExpression)right);
                string TName = me.Expression.ToString2();
                if (TAndTableName.ContainsKey(TName))
                {
                    //是一个字段
                    column.Value = GetColumnName(right);
                    column.FormatString = " \"{0}\" {1} {2} ";
                    return column;
                }
            }
            column.Value = GetColumnValue(right);
            column.FormatString = " \"{0}\" {1} {2} ";
            return column;
        }
        private Lambda_Column ExpressionRouter_Method(MethodCallExpression mce)
        {//静态方法
            string MethodName = mce.Method.Name;
            Lambda_Column column = GetColumnName(mce.Arguments[0]);
            column.TypeCast = MethodName;

            #region 字段

            if (MethodName == "lb_ColumeName")
            {
                column.FormatString = " \"{0}\" ";
            }
            if (MethodName == "lb_Sum")
            {
                column.FormatString = " Sum【\"{0}\"】 ";
            }
            if (MethodName == "lb_Avg")
            {
                column.FormatString = " Avg【\"{0}\"】 ";
            }
            if (MethodName == "lb_Min")
            {
                column.FormatString = " Min【\"{0}\"】 ";
            }
            if (MethodName == "lb_Max")
            {
                column.FormatString = " Max【\"{0}\"】 ";
            }
            if (MethodName == "lb_Distinct")
            {
                column.FormatString = " Distinct \"{0}\" ";
            }

            #endregion

            #region 排序

            if (MethodName == "lb_Desc")
            {
                column.FormatString = " \"{0}\" Desc ";
            }
            if (MethodName == "lb_Asc")
            {
                column.FormatString = " \"{0}\" Asc ";
            }
            if (MethodName == "lb_Newid")
            {
                column.FormatString = " newid【】 ";
            }
            if (MethodName == "lb_OrderByValue")
            {
                column.Value = GetColumnValue(mce.Arguments[1]);
                if (column.Value.ToInt32() == 1) column.FormatString = " \"{0}\" Asc ";
                else if (column.Value.ToInt32() == 2) column.FormatString = " \"{0}\" Desc ";
                else column.FormatString = "*=*";
            }

            #endregion

            #region 条件

            if (MethodName == "lb_Like")
            {
                column.Value = GetColumnValue(mce.Arguments[1]);
                //column.FormatString = string.IsNullOrWhiteSpace(column.Value.ToString2()) ? " 1=1 " : " {0} like '%'+{1}+'%' ";
                column.FormatString = string.IsNullOrWhiteSpace(column.Value.ToString2()) ? " 1=1 " : " \"{0}\" like concat('%',{1},'%') ";
            }
            if (MethodName == "lb_LikeR")
            {
                column.Value = GetColumnValue(mce.Arguments[1]);
                //column.FormatString = string.IsNullOrWhiteSpace(column.Value.ToString2()) ? " 1=1 " : " {0} like {1}+'%' ";
                column.FormatString = string.IsNullOrWhiteSpace(column.Value.ToString2()) ? " 1=1 " : " \"{0}\" like concat({1},'%') ";
            }
            if (MethodName == "lb_LikeL")
            {
                column.Value = GetColumnValue(mce.Arguments[1]);
                //column.FormatString = string.IsNullOrWhiteSpace(column.Value.ToString2()) ? " 1=1 " : " {0} like '%'+{1} ";
                column.FormatString = string.IsNullOrWhiteSpace(column.Value.ToString2()) ? " 1=1 " : " \"{0}\" like concat('%',{1}) ";
            }
            if (MethodName == "lb_NotLike")
            {
                column.Value = GetColumnValue(mce.Arguments[1]);
                //column.FormatString = string.IsNullOrWhiteSpace(column.Value.ToString2()) ? " 1=1 " : " {0} not like '%'+{1}+'%' ";
                column.FormatString = string.IsNullOrWhiteSpace(column.Value.ToString2()) ? " 1=1 " : " \"{0}\" not like concat('%',{1},'%') ";
            }

            if (MethodName == "lb_Like4")
            {
                column.Value = GetColumnValue(mce.Arguments[1]);
                //column.FormatString = string.IsNullOrWhiteSpace(column.Value.ToString2()) ? " 1=1 " : " ','+{0}+',' like '%,'+{1}+',%' ";
                column.FormatString = string.IsNullOrWhiteSpace(column.Value.ToString2()) ? " 1=1 " : " concat(',' , \"{0}\" , ',') like concat('%,',{1},',%') ";
            }
            if (MethodName == "lb_Like4ForInt")
            {
                column.Value = GetColumnValue(mce.Arguments[1]);
                //column.FormatString = (string.IsNullOrWhiteSpace(column.Value.ToString2()) || column.Value.ToInt32() == 0) ? " 1=1 " : " ','+{0}+',' like '%,'+{1}+',%' ";
                column.FormatString = (string.IsNullOrWhiteSpace(column.Value.ToString2()) || column.Value.ToInt32() == 0) ? " 1=1 " : " concat(',' , \"{0}\" , ',') like  concat('%,',{1},',%') ";
            }
            if (MethodName == "lb_LikeF")
            {
                object value = GetColumnValue(mce.Arguments[1]);
                if (value == null) value = new string[] { };
                string[] arr_value = (string[])value;
                column.Value = string.Join(",", arr_value);
                //column.FormatString = arr_value.Length < 1 ? " 1=1 " : " ','+{1}+',' like '%,'+{0}+',%' ";
                column.FormatString = arr_value.Length < 1 ? " 1=1 " : " concat(',',\"{1}\",',') like concat('%,',{0},',%') ";
            }


            if (MethodName == "lb_IsNotNullAndEqual")
            {
                column.Value = GetColumnValue(mce.Arguments[1]);
                column.FormatString = (column.Value == null) ? " 1=1 " : " \"{0}\" = {1} ";
            }
            if (MethodName == "lb_IsNotNullAndDo")
            {
                column.Value = GetColumnValue(mce.Arguments[1]);
                string FuHao = GetColumnValue(mce.Arguments[2]).ToString2();
                column.FormatString = (column.Value == null) ? " 1=1 " : (" \"{0}\" " + FuHao + " {1} ");
            }
            if (MethodName == "lb_IsNotNullAndEmptyAndDo")
            {
                column.Value = GetColumnValue(mce.Arguments[1]);
                string FuHao = GetColumnValue(mce.Arguments[2]).ToString2();
                column.FormatString = (string.IsNullOrWhiteSpace(column.Value.ToString2())) ? " 1=1 " : (" \"{0}\" " + FuHao + " {1} ");
            }
            if (MethodName == "lb_IsNotNullAndZeroAndDo")
            {
                column.Value = GetColumnValue(mce.Arguments[1]);
                string FuHao = GetColumnValue(mce.Arguments[2]).ToString2();
                column.FormatString = (column.Value.ToInt32() == 0) ? " 1=1 " : (" \"{0}\" " + FuHao + " {1} ");
            }
            if (MethodName == "lb_IsNotFalseAndEqual")
            {
                column.Value = GetColumnValue(mce.Arguments[1]);
                column.FormatString = (((bool)column.Value) == false) ? " 1=1 " : (" \"{0}\" = {1} ");
            }

            if (MethodName == "lb_CheckNull")
            {////0:忽略该条件 1:查询为null数据 2:查询不为null数据 3:查询为null或空数据 4:查询不为null和不为空数据
                column.Value = GetColumnValue(mce.Arguments[1]);
                if (column.Value.ToInt32() == 1)
                    column.FormatString = " \"{0}\" is null ";
                else if (column.Value.ToInt32() == 2)
                    column.FormatString = " \"{0}\" is not null ";
                else if (column.Value.ToInt32() == 3)
                    column.FormatString = " \"{0}\" is null or {0} = '' ";
                else if (column.Value.ToInt32() == 4)
                    column.FormatString = " \"{0}\" is not null and {0} <> '' ";
                else
                    column.FormatString = " 1=1 ";

            }

            #endregion

            return column;
        }

        /// <summary>
        /// 返回一个字段
        /// </summary>
        /// <param name="exp"></param>
        /// <returns></returns>
        private Lambda_Column GetColumnName(Expression exp)
        {
            if (exp is MemberExpression)
            {
                MemberExpression me = ((MemberExpression)exp);
                string TName = me.Expression.ToString2();
                string TTableName = TAndTableName[TName];
                string ColumnName = me.Member.Name;
                return new Lambda_Column() { TName = TName, TTableName = TTableName, Name = ColumnName };
                //ColumnAttribute[] cas = me.Member.ReflectedType.GetProperty(me.Member.Name).GetCustomAttributes(typeof(ColumnAttribute), false) as ColumnAttribute[];
                //if (cas.Length > 0)
                //{
                //    return new Lambda_Column() { TName = TName, TTableName = TTableName, Name = ColumnName, SqlDbType = cas[0].GetType() };
                //}
                //else
                //    throw new Exception("MemberExpression: LambdaToSQL " + ColumnName + " 数据库类型(SqlDbType)获取失败");
            }
            throw new Exception("not is MemberExpression");
        }
        /// <summary>
        /// 返回值
        /// </summary>
        /// <param name="exp"></param>
        /// <returns></returns>
        private object GetColumnValue(Expression exp)
        {
            if (exp is MemberExpression || exp is MethodCallExpression || exp is UnaryExpression)
            {
                try
                {
                    //获取值
                    object Result = Expression.Lambda(exp).Compile().DynamicInvoke();
                    return DealValue(Result);
                }
                catch { throw new Exception("MemberExpression|MemberExpression|UnaryExpression: 获取值出错" + exp.ToString2()); }
            }
            else if (exp is ConstantExpression)
            {
                return DealValue(((ConstantExpression)exp).Value);
            }
            else if (exp is NewArrayExpression)
            {
                NewArrayExpression ae = ((NewArrayExpression)exp);
                List<object> Values = new List<object>();
                foreach (var exp2 in ae.Expressions) { Values.Add(GetColumnValue(exp)); }
                return Values.ToArray();
            }

            try
            {
                //获取值
                object Result = Expression.Lambda(exp).Compile().DynamicInvoke();
                return DealValue(Result);
            }
            catch
            {
                throw new Exception("GetColumnValue: 获取值出错");
            }
        }
        private object DealValue(object Value)
        {
            return Value;
        }

        /// <summary>
        /// 获取符号
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        private string ExpressionTypeCast(ExpressionType type)
        {
            switch (type)
            {
                case ExpressionType.And:
                case ExpressionType.AndAlso:
                    return "AND";
                case ExpressionType.Equal:
                    return "=";
                case ExpressionType.GreaterThan:
                    return ">";
                case ExpressionType.GreaterThanOrEqual:
                    return ">=";
                case ExpressionType.LessThan:
                    return "<";
                case ExpressionType.LessThanOrEqual:
                    return "<=";
                case ExpressionType.NotEqual:
                    return "<>";
                case ExpressionType.Or:
                case ExpressionType.OrElse:
                    return "Or";
                case ExpressionType.Add:
                case ExpressionType.AddChecked:
                    return "+";
                case ExpressionType.Subtract:
                case ExpressionType.SubtractChecked:
                    return "-";
                case ExpressionType.Divide:
                    return "/";
                case ExpressionType.Multiply:
                case ExpressionType.MultiplyChecked:
                    return "*";
                default:
                    throw new Exception("ExpressionTypeCast Err");
            }
        }

        private void ToSQLWithParameter()
        {
            if (ListTree.Count < 1)
                throw new Exception("ListTree.Count is Zero");

            object Obj = ListTree[0];

            List<SqlParameter> _SPList;
            string SQL = CTree(Obj, out _SPList);
            if (_SPList == null) _SPList = new List<SqlParameter>();

            switch (this.sqlsort)
            {
                case SQLSort.SQLFields:
                case SQLSort.SQLOrder:
                    SQL = SQL.Replace("(", "").Replace(")", "").Replace("AND", ",");
                    //SQL = SQL.Replace("*=* ,", "").Replace(", *=*", "").Replace(", *=* ,", ",").Replace("*=*", "");
                    List<string> NSQL = new List<string>();
                    string[] SQL_item = SQL.ToString2().Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var sql in SQL_item)
                    {
                        if (sql.ToString2().Trim() == "*=*") continue;
                        NSQL.Add(sql);
                    }
                    SQL = string.Join(" , ", NSQL.ToArray());
                    SQL = SQL.Replace("【", "(").Replace("】", ")");
                    break;
            }

            Lambda_Sql = SQL;
            Lambda_SPArr = _SPList.ToArray();
        }
        private string CTree(object Obj, out List<SqlParameter> _SPList)
        {
            _SPList = new List<SqlParameter>();
            StringBuilder Output = new StringBuilder();
            Output.Append("(");
            if (Obj is List<Object>)
            {
                var Arr = (List<Object>)Obj;

                List<SqlParameter> _SPList0 = new List<SqlParameter>();
                Output.Append(CTree(Arr[0], out _SPList0));
                _SPList.AddRange(_SPList0);

                Output.Append(" " + Arr[1].ToString2() + " ");

                List<SqlParameter> _SPList2 = new List<SqlParameter>();
                Output.Append(CTree(Arr[2], out _SPList2));
                _SPList.AddRange(_SPList2);
            }
            else
            {
                SqlParameter _SP = null;
                Output.Append(CItem((Lambda_Column)Obj, out _SP));
                if (_SP != null) _SPList.Add(_SP);
            }
            Output.Append(")");
            return Output.ToString2();
        }
        private string CItem(Lambda_Column _Lambda_Column, out SqlParameter _SP)
        {
            try
            {
                sign++;
                string ColumnNameParameter = "@" + _Lambda_Column.Name + sign;
                //_SP = new SqlParameter(ColumnNameParameter, _Lambda_Column.SqlDbType);
                _SP = new SqlParameter(ColumnNameParameter);
                _SP.Value = _Lambda_Column.Value;

                string ColumnName = GetColumnShowName(_Lambda_Column);

                switch (_Lambda_Column.TypeCast)
                {
                    case "lb_ColumeName":
                    case "lb_Sum":
                    case "lb_Avg":
                    case "lb_Min":
                    case "lb_Max":
                    case "lb_Distinct":

                    case "lb_Desc":
                    case "lb_Asc":
                    case "lb_Newid":
                    case "lb_OrderByValue":
                        _SP = null; break;

                    case "lb_Like":
                    case "lb_LikeR":
                    case "lb_LikeL":
                    case "lb_NotLike":
                    case "lb_Like4":
                    case "lb_LikeF":
                        if (string.IsNullOrWhiteSpace(_SP.Value.ToString2())) _SP = null; break;
                    case "lb_Like4ForInt":
                        if (string.IsNullOrWhiteSpace(_SP.Value.ToString2()) || _SP.Value.ToInt32() == 0) _SP = null; break;
                    case "lb_IsNotNullAndEqual":
                    case "lb_IsNotNullAndDo":
                        if (_SP.Value == null) _SP = null; break;
                    case "lb_IsNotNullAndEmptyAndDo":
                        if (string.IsNullOrWhiteSpace(_SP.Value.ToString2())) _SP = null; break;
                    case "lb_IsNotNullAndZeroAndDo":
                        if (_SP.Value.ToInt32() == 0) _SP = null; break;
                    case "lb_IsNotFalseAndEqual":
                        if (((bool)_SP.Value) == false) _SP = null; break;
                    case "lb_CheckNull":
                        //if (_SP.Value.ToInt32() < 1 || _SP.Value.ToInt32() > 4) _SP = null; break;
                        _SP = null; break;
                    default:
                        if (_SP.Value is Lambda_Column)
                        {
                            Lambda_Column right = (Lambda_Column)_SP.Value;
                            ColumnNameParameter = GetColumnShowName(right);
                            _SP = null;
                        }
                        break;
                }
                string SQL = "";
                switch (_Lambda_Column.TypeCast)
                {
                    case "lb_ColumeName":
                    case "lb_Sum":
                    case "lb_Avg":
                    case "lb_Min":
                    case "lb_Max":
                    case "lb_Distinct":
                        SQL = string.Format(_Lambda_Column.FormatString, ColumnName); break;

                    case "lb_Desc":
                    case "lb_Asc":
                    case "lb_Newid":
                    case "lb_OrderByValue":
                        SQL = string.Format(_Lambda_Column.FormatString, ColumnName); break;

                    case "lb_Like":
                    case "lb_LikeR":
                    case "lb_LikeL":
                    case "lb_NotLike":
                    case "lb_Like4":
                    case "lb_LikeF":
                    case "lb_Like4ForInt":
                    case "lb_IsNotNullAndEqual":
                    case "lb_IsNotNullAndDo":
                    case "lb_IsNotNullAndEmptyAndDo":
                    case "lb_IsNotNullAndZeroAndDo":
                    case "lb_IsNotFalseAndEqual":
                        SQL = string.Format(_Lambda_Column.FormatString, ColumnName, ColumnNameParameter); break;
                    case "lb_CheckNull":
                        SQL = string.Format(_Lambda_Column.FormatString, ColumnName); break;
                    default:
                        SQL = string.Format(_Lambda_Column.FormatString, ColumnName, _Lambda_Column.TypeCast, ColumnNameParameter); break;
                }
                return SQL;
            }
            catch (Exception ce)
            {
                throw ce;
            }
            //_SP = null;
            //return "";
        }

        private string GetColumnShowName(Lambda_Column _Lambda_Column)
        {
            string ColumnName = "";
            int _ColumnPrefix = ColumnPrefix;
            if (_ColumnPrefix == 0 && TAndTableName.Count > 1) _ColumnPrefix = 1;
            switch (_ColumnPrefix)
            {
                //case 1: ColumnName = _Lambda_Column.TName + "." + "[" + _Lambda_Column.Name + "]"; break;//t.title
                //case 2: ColumnName = _Lambda_Column.TTableName + "." + "[" + _Lambda_Column.Name + "]"; break;//product.title
                //default: ColumnName = "[" + _Lambda_Column.Name + "]"; break;//title
                case 1: ColumnName = _Lambda_Column.TName + "." + "" + _Lambda_Column.Name + ""; break;//t.title
                case 2: ColumnName = _Lambda_Column.TTableName + "." + "" + _Lambda_Column.Name + ""; break;//product.title
                default: ColumnName = "" + _Lambda_Column.Name + ""; break;//title

            }
            return ColumnName;
        }

        public object ShowMsg()
        {
            List<object> C = new List<object>();
            foreach (var can in Lambda_SPArr) { C.Add(new { Name = can.ParameterName, Value = can.Value }); }
            return new { Tree = ListTree, SQL = Lambda_Sql, Parameter = C };
        }
    }

    public class SqlParameter
    {
        public SqlParameter(string ParameterName)
        {
            this.ParameterName = ParameterName;
            //this.DbType = DbType;
        }

        public string ParameterName { get; }
        //public SqlDbType DbType { get; }
        public object Value { get; set; }
    }

    /// <summary>
    /// Lambda_字段（子类）
    /// </summary>
    public class Lambda_Column
    {
        public string TName { get; set; }
        public string TTableName { get; set; }
        public string Name { get; set; }
        public string TypeCast { get; set; }
        public object Value { get; set; }
        public string FormatString { get; set; }
        //public SqlDbType SqlDbType { get; set; }
    }
}