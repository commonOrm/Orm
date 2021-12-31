using Dapper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SqlSugar;

public class SqlTranExtensions
{
    private readonly ILogger<SqlTranExtensions> logger;

    private readonly IConnectionProvider conn;

    public IDbConnection connection;
    public IDbTransaction transaction;

    public SqlSugarClient db;

    public SqlTranExtensions()
    {
        this.logger = ServiceLocator.Instance.GetService(typeof(ILogger<SqlTranExtensions>)) as ILogger<SqlTranExtensions>;

        this.conn = ServiceLocator.Instance.GetService(typeof(IConnectionProvider)) as IConnectionProvider;
        this.connection = conn.GetDbConnection();
        this.db = conn.GetSqlSugarClient();

        if (this.connection != null)
        {
            //在dapper中使用事务，需要手动打开连接
            connection.Open();
            //开启一个事务
            this.transaction = connection.BeginTransaction();
        }
        else
        {
            db.BeginTran();
        }
    }
    ~SqlTranExtensions()
    {
        if (this.connection != null)
        {
            if (this.connection.State == ConnectionState.Open) this.connection.Close();
        }
        else
            try { this.db.Close(); } catch { }
    }

    /// <summary>
    /// 执行回滚
    /// </summary>
    public void Rollback()
    {
        if (this.connection != null)
        {
            this.transaction.Rollback();
            this.connection.Close();
        }
        else
        {
            this.db.RollbackTran();
            this.db.Close();
        }
    }

    /// <summary>
    /// 执行事物集合
    /// </summary>
    public async Task<bool> Commit()
    {
        return await ExecuteSqlTran();
    }
    /// <summary>
    /// 执行事物集合
    /// </summary>
    public async Task<bool> ExecuteSqlTran()
    {
        if (this.connection != null)
        {
            return await Task.Run<bool>(() =>
                 {
                     try
                     {
                         //都执行成功时提交
                         this.transaction.Commit();

                         //var successMsg = "ExecuteSqlTran Success";
                         //Console.WriteLine(successMsg);
                         //logger.LogInformation(successMsg);

                         this.connection.Close();
                         return true;
                     }
                     catch (Exception ex)
                     {
                         Rollback();

                         var errMsg = $"ExecuteSqlTran Fail：{ex.Message}";
                         Console.WriteLine(errMsg);
                         logger.LogError(ex, errMsg);

                         return false;
                     }
                 });
        }
        else
        {
            return await Task.Run<bool>(() =>
            {
                try
                {
                    //都执行成功时提交
                    this.db.CommitTran();

                    //var successMsg = "ExecuteSqlTran Success";
                    //Console.WriteLine(successMsg);
                    //logger.LogInformation(successMsg);

                    this.db.Close();
                    return true;
                }
                catch (Exception ex)
                {
                    Rollback();

                    var errMsg = $"ExecuteSqlTran Fail：{ex.Message}";
                    Console.WriteLine(errMsg);
                    logger.LogError(ex, errMsg);

                    return false;
                }
            });
        }
    }
}