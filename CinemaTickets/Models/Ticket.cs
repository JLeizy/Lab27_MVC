namespace CinemaTickets.Models
{
    /// <summary>
    /// MODEL. Проданный билет. После создания не меняется (все свойства только для чтения):
    /// если покупатель передумал, билет возвращают целиком, а не "редактируют".
    /// </summary>
    public class Ticket
    {
        public Ticket(int number, Session session, int row, int seat,
                      TicketCategory category, decimal price, DateTime soldAt)
        {
            Number = number;
            Session = session;
            Row = row;
            Seat = seat;
            Category = category;
            Price = price;
            SoldAt = soldAt;
        }

        /// <summary>Номер билета (идёт по порядку: 1, 2, 3...).</summary>
        public int Number { get; }
        public Session Session { get; }
        public int Row { get; }
        public int Seat { get; }
        public TicketCategory Category { get; }

        /// <summary>Итоговая цена, уже со скидкой и наценкой за VIP-ряд.</summary>
        public decimal Price { get; }

        /// <summary>Когда продан билет.</summary>
        public DateTime SoldAt { get; }

        public override string ToString() =>
            $"Билет №{Number}: «{Session.Movie.Title}», {Session.StartTimeText}, {Session.Hall.Name}, " +
            $"ряд {Row}, место {Seat}, {Category.Name.ToLower()}, {Price:0} руб.";
    }
}
