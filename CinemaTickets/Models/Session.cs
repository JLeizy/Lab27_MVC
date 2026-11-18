namespace CinemaTickets.Models
{
    /// <summary>
    /// MODEL. Сеанс — показ конкретного фильма в конкретном зале в определённое время.
    /// Сеанс сам хранит, какие места на нём уже заняты.
    /// </summary>
    public class Session
    {
        // Занятые места храним как пары (ряд, место).
        // HashSet выбран потому, что проверка "занято ли место" в нём мгновенная
        // и одно и то же место нельзя добавить дважды.
        private readonly HashSet<(int Row, int Seat)> _takenSeats = new();

        public Session(Movie movie, Hall hall, TimeSpan startTime, decimal basePrice)
        {
            Movie = movie;
            Hall = hall;
            StartTime = startTime;
            BasePrice = basePrice;
        }

        public Movie Movie { get; }
        public Hall Hall { get; }

        /// <summary>Время начала (например, 19:00).</summary>
        public TimeSpan StartTime { get; }

        /// <summary>
        /// Базовая цена взрослого билета на обычное место.
        /// Утренние сеансы дешевле вечерних — это задаётся прямо в данных.
        /// </summary>
        public decimal BasePrice { get; }

        /// <summary>Время начала текстом "чч:мм".</summary>
        public string StartTimeText => StartTime.ToString(@"hh\:mm");

        /// <summary>Сколько мест ещё свободно.</summary>
        public int FreeSeatsCount => Hall.Capacity - _takenSeats.Count;

        public bool IsSeatTaken(int row, int seat) => _takenSeats.Contains((row, seat));

        // internal: занимать и освобождать места должна только CinemaModel
        // (в методах SellTicket / ReturnTicket), потому что именно там
        // выполняются все проверки и пересчитывается выручка.
        internal void TakeSeat(int row, int seat) => _takenSeats.Add((row, seat));
        internal void ReleaseSeat(int row, int seat) => _takenSeats.Remove((row, seat));

        /// <summary>Как сеанс выглядит в выпадающем списке.</summary>
        public override string ToString() =>
            $"{StartTimeText} · {Hall.Name} · {BasePrice:0} руб. · свободно {FreeSeatsCount}";
    }
}
