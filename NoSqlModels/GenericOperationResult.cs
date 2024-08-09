using System.Net;

namespace NoSqlModels
{

    public interface IOperationResult
    {
        bool IsSuccess { get; set; }

        string ErrorMessage { get; set; }

        string ErrorDetail { get; set; }

        HttpStatusCode? StatusCode { get; set; }
    }

    public interface IOperationResult<T> : IOperationResult
    {
        T Result { get; set; }
    }

    public class GenericOperationResult
    {
    }

    public interface IUserInformation
    {
        string UserId { get; set; }
    }

    public class UserInformation : IUserInformation
    {
        public string UserId { get; set; }
    }
}
