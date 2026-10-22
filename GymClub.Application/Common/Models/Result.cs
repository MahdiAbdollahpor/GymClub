namespace GymClub.Application.Common.Models;

public class Result
{
    public bool Succeeded { get; init; }
    public string? Error { get; init; }
    public List<string> Errors { get; init; } = new();

    public static Result Ok() => new() { Succeeded = true };
    public static Result Fail(string error) => new() { Succeeded = false, Error = error };
    public static Result Fail(IEnumerable<string> errors)
    {
        var list = errors.ToList();
        return new Result { Succeeded = false, Error = list.FirstOrDefault(), Errors = list };
    }
}

public class Result<T> : Result
{
    public T? Data { get; init; }

    public static Result<T> Ok(T data) => new() { Succeeded = true, Data = data };
    public static new Result<T> Fail(string error) => new() { Succeeded = false, Error = error };
    public static new Result<T> Fail(IEnumerable<string> errors)
    {
        var list = errors.ToList();
        return new Result<T> { Succeeded = false, Error = list.FirstOrDefault(), Errors = list };
    }
}