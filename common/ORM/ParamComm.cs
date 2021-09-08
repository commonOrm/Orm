using System;
using System.Collections.Generic;
using System.Linq;

public interface IParamBase
{
    string GetSplitSign();
    void CreateWhere(out string _wheres, out Dictionary<string, object> _params);
}
public abstract class ParamBase : IParamBase
{
    public int Sign { get; set; } = 0;

    protected List<object[]> _vals = new List<object[]>();
    public void Add(string column, object value, string fuhao = "=")
    {
        _vals.Add(new object[] { fuhao, column, value });
    }

    public void Add(IParamBase sub)
    {
        ParamBase subObj = ((ParamBase)sub);
        subObj.Sign += 100;
        _vals.Add(new object[] { "sub", subObj });
    }

    public abstract string GetSplitSign();

    public void CreateWhere(out string _wheres, out Dictionary<string, object> _params)
    {
        List<string> _wheresList = new List<string>();
        Dictionary<string, object> _paramsDic = new Dictionary<string, object>();
        int index = Sign;
        foreach (var item in _vals)
        {
            string fuhao = item[0].ToString();
            switch (fuhao)
            {
                case "sub":
                    IParamBase sub = (IParamBase)item[1];
                    string _wheres_sub;
                    Dictionary<string, object> _params_sub;
                    sub.CreateWhere(out _wheres_sub, out _params_sub);
                    _wheresList.Add(" (" + _wheres_sub + ") ");
                    foreach (var item_sub in _params_sub)
                    { _paramsDic.Add(item_sub.Key, item_sub.Value); }
                    break;
                default:
                    string column = item[1].ToString();
                    string param_name = item[1].ToString() + index;

                    switch (fuhao.ToLower().Trim())
                    {
                        case "in":
                            List<string> inParams = new List<string>();
                            foreach (var val in ((Array)item[2]))
                            {
                                param_name = item[1].ToString() + index;
                                _paramsDic.Add(param_name, val);//装入值
                                inParams.Add("@" + param_name);
                                index++;
                            }
                            _wheresList.Add(string.Format(" \"{0}\" {1} ({2}) ", column, fuhao, string.Join(",", inParams)));
                            break;
                        case "like":
                        case "ilike": //忽略大小写
                            _paramsDic.Add(param_name, item[2]);//装入值
                            param_name = string.Format(" concat('%',{0},'%') ", "@" + param_name);
                            _wheresList.Add(string.Format(" \"{0}\" {1} {2} ", column, fuhao, param_name));
                            break;
                        default:
                            _paramsDic.Add(param_name, item[2]);//装入值
                            param_name = string.Format(" {0} ", "@" + param_name);
                            _wheresList.Add(string.Format(" \"{0}\" {1} {2} ", column, fuhao, param_name));
                            break;
                    }
                    index++;
                    break;
            }
        }

        _wheres = string.Join(GetSplitSign(), _wheresList.ToArray());
        _params = _paramsDic;
        if (string.IsNullOrWhiteSpace(_wheres.Trim()))
        {
            _wheres = " 1=1 ";
            _params = null;
        }
    }

}

public class ParamCommOr : ParamBase, IParamBase
{
    public override string GetSplitSign()
    {
        return " OR ";
    }
}

public class ParamCommAnd : ParamBase, IParamBase
{
    public override string GetSplitSign()
    {
        return " AND ";
    }
}
