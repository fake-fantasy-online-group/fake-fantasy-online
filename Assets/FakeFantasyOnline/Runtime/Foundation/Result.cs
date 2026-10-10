using System;

namespace FakeFantasyOnline.Foundation
{

public readonly struct Error
{
    public Error(string code, string message)
    {
        Code = code ?? throw new ArgumentNullException(nameof(code));
        Message = message ?? throw new ArgumentNullException(nameof(message));
    }

    public string Code { get; }

    public string Message { get; }

    public override string ToString()
    {
        return $"{Code}: {Message}";
    }
}

public readonly struct Result
{
    private Result(bool isSuccess, Error error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    public bool IsSuccess { get; }

    public Error Error { get; }

    public static Result Success()
    {
        return new Result(true, default);
    }

    public static Result Failure(string code, string message)
    {
        return new Result(false, new Error(code, message));
    }
}

public readonly struct Result<T>
{
    private readonly T _value;

    private Result(bool isSuccess, T value, Error error)
    {
        IsSuccess = isSuccess;
        _value = value;
        Error = error;
    }

    public bool IsSuccess { get; }

    public Error Error { get; }

    public T Value => IsSuccess
        ? _value
        : throw new InvalidOperationException("A failed result has no value.");

    public static Result<T> Success(T value)
    {
        return new Result<T>(true, value, default);
    }

    public static Result<T> Failure(string code, string message)
    {
        return new Result<T>(false, default, new Error(code, message));
    }
}
}
