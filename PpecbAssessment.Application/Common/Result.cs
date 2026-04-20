namespace PpecbAssessment.Application.Common
{
    public class Result
    {
        public bool Succeeded { get; set; }
        public bool IsConflict { get; set; }
        public string Message { get; set; } = string.Empty;

        public static Result Success(string message = "") =>
            new() { Succeeded = true, Message = message };

        public static Result Failure(string message) =>
            new() { Succeeded = false, Message = message };

        public static Result Conflict(string message) =>
            new() { Succeeded = false, IsConflict = true, Message = message };
    }

    public class Result<T> : Result
    {
        public T? Data { get; set; }

        public static Result<T> Success(T data, string message = "") =>
            new() { Succeeded = true, Data = data, Message = message };

        public new static Result<T> Failure(string message) =>
            new() { Succeeded = false, Message = message };

        public new static Result<T> Conflict(string message) =>
            new() { Succeeded = false, IsConflict = true, Message = message };
    }
}
