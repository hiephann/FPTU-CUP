namespace fptu_cup.Common
{
    /// <summary>Định dạng response thống nhất: { success, message, data }.</summary>
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }

        public static ApiResponse<T> Ok(T? data, string message = "Thành công") =>
            new() { Success = true, Message = message, Data = data };

        public static ApiResponse<T> Fail(string message, T? data = default) =>
            new() { Success = false, Message = message, Data = data };
    }
}
