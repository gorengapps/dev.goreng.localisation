#nullable enable
using System;


namespace UnityEngine.Networking
{
    public class Result<T, E> where E : Exception
    {
        public T? result { get; set; }
        public E? error { get; set; }
        
        public bool hasError => error != null;
        
        public static Result<T, E> Success(T? result) { return new Result<T, E> { result = result }; }
        public static Result<T, E> Fail(E error) { return new Result<T, E> { error = error }; }
    }
}