public class Result_Obj
{
    /// <summary>
    /// 1：成功  0：失败
    /// </summary>
    public int Code { get; set; } = 0;

    private string _message = "";
    /// <summary>
    /// 信息
    /// </summary>
    public string Message
    {
        get
        {
            if (Code == 0 && string.IsNullOrWhiteSpace(_message)) _message = "fail";
            if (Code == 1 && string.IsNullOrWhiteSpace(_message)) _message = "success";
            return _message;
        }
        set
        {
            _message = value;
        }
    }

    /// <summary>
    /// 返回值
    /// </summary>
    public object Result { get; set; } = "";
}
