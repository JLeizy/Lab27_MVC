namespace CinemaTickets.Models
{
    /// <summary>
    /// MODEL. Кинозал: сколько в нём рядов и мест в каждом ряду.
    /// </summary>
    public class Hall
    {
        public Hall(string name, int rows, int seatsPerRow)
        {
            Name = name;
            Rows = rows;
            SeatsPerRow = seatsPerRow;
        }

        public string Name { get; }

        /// <summary>Количество рядов (нумерация с 1).</summary>
        public int Rows { get; }

        /// <summary>Количество мест в каждом ряду (нумерация с 1).</summary>
        public int SeatsPerRow { get; }

        /// <summary>Всего мест в зале.</summary>
        public int Capacity => Rows * SeatsPerRow;

        /// <summary>
        /// Последний ряд — VIP (диваны), билеты туда дороже.
        /// Правило хранится в модели, а не в окне: если его поменять,
        /// интерфейс и контроллер переписывать не придётся.
        /// </summary>
        public bool IsVipRow(int row) => row == Rows;

        /// <summary>Есть ли такое место в зале (защита от "ряд 100, место -5").</summary>
        public bool HasSeat(int row, int seat) =>
            row >= 1 && row <= Rows && seat >= 1 && seat <= SeatsPerRow;
    }
}
