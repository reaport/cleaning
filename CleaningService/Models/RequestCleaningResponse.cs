namespace CleaningService.Models
{
    /// <summary>
    /// Модель успешного ответа на запрос очистки.
    /// </summary>
    public class RequestCleaningResponse
    {
        // В примере указан параметр "wait" – true означает, что запрос принят.
        public bool wait { get; set; }
    }
}
