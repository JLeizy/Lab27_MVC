namespace CinemaTickets.Models
{
    /// <summary>
    /// MODEL. Фильм из репертуара кинотеатра.
    ///
    /// Это простой класс-"данные": только свойства, без логики.
    /// Он ничего не знает ни об окне, ни о кнопках — поэтому его можно
    /// использовать в любом интерфейсе (WPF, консоль, сайт).
    /// </summary>
    public class Movie
    {
        public Movie(string title, int durationMinutes, int ageLimit)
        {
            Title = title;
            DurationMinutes = durationMinutes;
            AgeLimit = ageLimit;
        }

        /// <summary>Название фильма.</summary>
        public string Title { get; }

        /// <summary>Длительность в минутах.</summary>
        public int DurationMinutes { get; }

        /// <summary>Возрастное ограничение: 0, 6, 12, 16 или 18 (означает "0+", "6+" ...).</summary>
        public int AgeLimit { get; }

        /// <summary>
        /// Как фильм выглядит в выпадающем списке.
        /// ComboBox в WPF по умолчанию показывает именно ToString().
        /// </summary>
        public override string ToString() => $"{Title} ({AgeLimit}+, {DurationMinutes} мин)";
    }
}
