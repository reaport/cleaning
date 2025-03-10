namespace CleaningService.Models
{
    /// <summary>
    /// Модель ошибки запроса очистки.
    /// </summary>
    public class RequestCleaningErrorResponse
    {
        public int errorCode { get; set; }
        public string message { get; set; }
    }
}
