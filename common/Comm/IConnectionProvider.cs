using SqlSugar;
using System.Data;

public interface IConnectionProvider
{
    IDbConnection GetDbConnection();

    SqlSugarClient GetSqlSugarClient();
}
