namespace CinemaTickets.Models
{
    /// <summary>
    /// MODEL. Исключение "нарушено правило кинотеатра"
    /// (место уже продано, детский билет на фильм 18+ и т.п.).
    ///
    /// Зачем отдельный класс: контроллер ловит ИМЕННО его и показывает текст
    /// пользователю. Настоящие ошибки программы (баги) этим catch не "проглатываются".
    /// </summary>
    public class CinemaRuleException : Exception
    {
        public CinemaRuleException(string message) : base(message)
        {
        }
    }
}
