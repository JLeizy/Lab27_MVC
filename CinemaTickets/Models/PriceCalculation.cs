namespace CinemaTickets.Models
{
    /// <summary>
    /// MODEL. Результат расчёта цены: сколько стоит билет и для какого места и типа
    /// билета это посчитано.
    ///
    /// Зачем хранить не только сумму: если кассир посчитал цену для ряда 3,
    /// а потом ввёл ряд 8, на экране должно быть видно, что сумма относится
    /// к ряду 3, а не к новому месту.
    /// </summary>
    public class PriceCalculation
    {
        public PriceCalculation(int row, int seat, TicketCategory category, decimal price)
        {
            Row = row;
            Seat = seat;
            Category = category;
            Price = price;
        }

        public int Row { get; }
        public int Seat { get; }
        public TicketCategory Category { get; }

        /// <summary>Итоговая цена в рублях.</summary>
        public decimal Price { get; }
    }
}
