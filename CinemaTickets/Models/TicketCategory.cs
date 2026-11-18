namespace CinemaTickets.Models
{
    /// <summary>
    /// MODEL. Тип (категория) билета и его скидка.
    ///
    /// Вместо enum используется класс: так у каждой категории сразу есть
    /// и название для списка, и коэффициент цены — не нужен отдельный switch.
    /// </summary>
    public class TicketCategory
    {
        // Все возможные категории создаются один раз здесь.
        // Конструктор private, поэтому новую категорию "снаружи" не придумать.
        public static readonly TicketCategory Adult = new("Взрослый", 1.00m);
        public static readonly TicketCategory Student = new("Студенческий", 0.80m);
        public static readonly TicketCategory Pensioner = new("Пенсионный", 0.70m);
        public static readonly TicketCategory Child = new("Детский (до 12 лет)", 0.50m);

        /// <summary>Список всех категорий — его показывает выпадающий список "Тип билета".</summary>
        public static IReadOnlyList<TicketCategory> All { get; } = new[] { Adult, Student, Pensioner, Child };

        private TicketCategory(string name, decimal priceCoefficient)
        {
            Name = name;
            PriceCoefficient = priceCoefficient;
        }

        public string Name { get; }

        /// <summary>
        /// Во сколько раз умножается базовая цена: 1.00 — полная цена, 0.80 — скидка 20%.
        /// decimal (а не double) используется для денег, чтобы не было ошибок округления.
        /// </summary>
        public decimal PriceCoefficient { get; }

        public override string ToString() =>
            PriceCoefficient == 1m ? Name : $"{Name}, скидка {(1m - PriceCoefficient) * 100:0}%";
    }
}
