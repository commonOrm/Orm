using System.Data;

public interface IConnectionProvider
{
    IDbConnection GetDbConnection();
}
