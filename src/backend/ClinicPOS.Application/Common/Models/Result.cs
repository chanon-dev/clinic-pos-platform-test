namespace ClinicPOS.Application.Common.Models;

public class Result<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public string? Error { get; }
    public int? StatusCode { get; }

    private Result(T value) { IsSuccess = true; Value = value; }
    private Result(string error, int statusCode) { IsSuccess = false; Error = error; StatusCode = statusCode; }

    public static Result<T> Success(T value) => new(value);
    public static Result<T> Failure(string error, int statusCode = 400) => new(error, statusCode);
}
