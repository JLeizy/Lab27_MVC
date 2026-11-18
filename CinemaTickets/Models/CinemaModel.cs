namespace CinemaTickets.Models
{
    /// <summary>
    /// MODEL — главная часть модели: "касса кинотеатра".
    ///
    /// Что делает:
    ///   * хранит данные: фильмы, сеансы, проданные билеты, выбранный сеанс;
    ///   * выполняет операции: расчёт цены, продажа и возврат билета;
    ///   * следит за правилами (место не продать дважды, детский билет не на 18+ и т.д.);
    ///   * после каждого изменения сама сообщает об этом событием <see cref="Changed"/>.
    ///
    /// Чего НЕ делает: не знает про окно, кнопки, TextBox и MessageBox.
    /// В методичке модель вызывала UpdateView() напрямую. Здесь вместо этого событие:
    /// модель просто "кричит" — "я изменилась", а вид (View) подписан на это событие
    /// и сам перерисовывается. Так модель не зависит от конкретного окна.
    ///
    /// Аналогия из методички: модель — это повар. Он готовит заказ,
    /// но не знает, как выглядит посетитель и как тот платил.
    /// </summary>
    public class CinemaModel
    {
        /// <summary>Наценка за VIP-ряд (последний ряд зала): цена × 1,5.</summary>
        public const decimal VipRowMultiplier = 1.5m;

        private readonly List<Movie> _movies = new();
        private readonly List<Session> _sessions = new();
        private readonly List<Ticket> _soldTickets = new();
        private int _nextTicketNumber = 1;

        /// <summary>
        /// Событие "данные модели изменились". На него подписывается View
        /// и в ответ вызывает свой UpdateView().
        /// </summary>
        public event EventHandler? Changed;

        public CinemaModel()
        {
            // Настоящей базы данных в лабораторной нет, поэтому заполняем
            // репертуар прямо в коде. Если позже появится БД, поменяется
            // только этот метод — View и Controller останутся прежними.
            FillRepertoire();

            // При запуске кассир сразу видит первый фильм и его первый сеанс.
            SelectedMovie = _movies[0];
            SelectedSession = GetSessions(SelectedMovie).FirstOrDefault();
        }

        // ------------------------------------------------------------------
        //  ДАННЫЕ (только для чтения снаружи)
        //  IReadOnlyList — чтобы View/Controller не могли, например, сделать
        //  _soldTickets.Clear() в обход правил. Менять данные — только методами ниже.
        // ------------------------------------------------------------------

        /// <summary>Все фильмы в прокате.</summary>
        public IReadOnlyList<Movie> Movies => _movies;

        /// <summary>Все проданные билеты (для таблицы и подсчёта выручки).</summary>
        public IReadOnlyList<Ticket> SoldTickets => _soldTickets;

        /// <summary>Фильм, который сейчас выбран в кассе.</summary>
        public Movie? SelectedMovie { get; private set; }

        /// <summary>Сеанс, на который сейчас продаются билеты.</summary>
        public Session? SelectedSession { get; private set; }

        /// <summary>
        /// Результат последнего расчёта цены (аналог поля result из методички):
        /// сумма и для какого места/типа билета она посчитана.
        /// null — цену ещё не считали для текущего сеанса.
        /// </summary>
        public PriceCalculation? LastCalculation { get; private set; }

        /// <summary>Выручка — сумма цен всех проданных билетов.</summary>
        public decimal Revenue => _soldTickets.Sum(t => t.Price);

        /// <summary>Сеансы выбранного фильма, отсортированные по времени.</summary>
        public IReadOnlyList<Session> GetSessions(Movie movie) =>
            _sessions.Where(s => s.Movie == movie)
                     .OrderBy(s => s.StartTime)
                     .ToList();

        // ------------------------------------------------------------------
        //  ОПЕРАЦИИ (их вызывает Controller после проверки ввода)
        // ------------------------------------------------------------------

        /// <summary>Выбрать фильм. Автоматически выбирается его первый сеанс.</summary>
        public void SelectMovie(Movie movie)
        {
            if (movie == SelectedMovie)
                return; // ничего не поменялось — и вид лишний раз не перерисовываем

            SelectedMovie = movie;
            SelectedSession = GetSessions(movie).FirstOrDefault();
            LastCalculation = null; // цена считалась для другого сеанса — сбрасываем
            OnChanged();
        }

        /// <summary>Выбрать сеанс выбранного фильма.</summary>
        public void SelectSession(Session session)
        {
            if (session == SelectedSession)
                return;

            if (session.Movie != SelectedMovie)
                throw new CinemaRuleException("Этот сеанс относится к другому фильму.");

            SelectedSession = session;
            LastCalculation = null;
            OnChanged();
        }

        /// <summary>
        /// Рассчитать стоимость билета на выбранный сеанс (без продажи).
        /// Результат сохраняется в <see cref="LastCalculation"/>, затем модель
        /// сообщает виду, что пора обновиться — ровно как Calculate() в методичке.
        /// </summary>
        public decimal CalculatePrice(int row, int seat, TicketCategory category)
        {
            Session session = RequireSelectedSession();
            CheckSeatExists(session, row, seat);
            CheckCategoryAllowed(session.Movie, category);

            decimal price = ComputePrice(session, row, category);
            LastCalculation = new PriceCalculation(row, seat, category, price);
            OnChanged();
            return price;
        }

        /// <summary>
        /// Продать билет на выбранный сеанс.
        /// Возвращает созданный билет, чтобы контроллер мог сообщить его номер.
        /// </summary>
        public Ticket SellTicket(int row, int seat, TicketCategory category)
        {
            Session session = RequireSelectedSession();

            // Контроллер уже проверил ввод, но модель всё равно защищает свои данные:
            // её могут вызвать и из другого места (например, из другого окна или тестов).
            CheckSeatExists(session, row, seat);
            CheckCategoryAllowed(session.Movie, category);

            if (session.IsSeatTaken(row, seat))
                throw new CinemaRuleException($"Место {seat} в ряду {row} на этот сеанс уже продано.");

            decimal price = ComputePrice(session, row, category);
            var ticket = new Ticket(_nextTicketNumber++, session, row, seat, category, price, DateTime.Now);

            session.TakeSeat(row, seat);
            _soldTickets.Add(ticket);
            LastCalculation = new PriceCalculation(row, seat, category, price);

            OnChanged(); // вид перерисует схему зала, таблицу билетов и выручку
            return ticket;
        }

        /// <summary>Вернуть билет: место снова свободно, деньги вычитаются из выручки.</summary>
        public void ReturnTicket(Ticket ticket)
        {
            if (!_soldTickets.Remove(ticket))
                throw new CinemaRuleException("Такого билета нет среди проданных.");

            ticket.Session.ReleaseSeat(ticket.Row, ticket.Seat);
            LastCalculation = null;
            OnChanged();
        }

        // ------------------------------------------------------------------
        //  ВНУТРЕННЯЯ ЛОГИКА
        // ------------------------------------------------------------------

        /// <summary>
        /// Формула цены — единственное место, где она записана:
        ///   цена = базовая цена сеанса × коэффициент категории × (1,5 если VIP-ряд),
        ///   округляется до целых рублей.
        /// </summary>
        private static decimal ComputePrice(Session session, int row, TicketCategory category)
        {
            decimal price = session.BasePrice * category.PriceCoefficient;

            if (session.Hall.IsVipRow(row))
                price *= VipRowMultiplier;

            return Math.Round(price, 0, MidpointRounding.AwayFromZero);
        }

        private Session RequireSelectedSession() =>
            SelectedSession ?? throw new CinemaRuleException("Не выбран сеанс.");

        private static void CheckSeatExists(Session session, int row, int seat)
        {
            if (!session.Hall.HasSeat(row, seat))
                throw new CinemaRuleException(
                    $"В зале «{session.Hall.Name}» нет места {seat} в ряду {row}.");
        }

        /// <summary>
        /// Бизнес-правило: детский билет (до 12 лет) нельзя продать на фильм 12+ и старше.
        /// Это правило кинотеатра, а не проверка формата ввода, поэтому оно в модели.
        /// </summary>
        private static void CheckCategoryAllowed(Movie movie, TicketCategory category)
        {
            if (category == TicketCategory.Child && movie.AgeLimit >= 12)
                throw new CinemaRuleException(
                    $"Детский билет нельзя продать на фильм «{movie.Title}» ({movie.AgeLimit}+).");
        }

        /// <summary>Оповестить подписчиков (View), что данные изменились.</summary>
        private void OnChanged() => Changed?.Invoke(this, EventArgs.Empty);

        /// <summary>Тестовый репертуар на один день: 2 зала, 4 фильма, 10 сеансов.</summary>
        private void FillRepertoire()
        {
            var hall1 = new Hall("Зал 1", rows: 8, seatsPerRow: 12);
            var hall2 = new Hall("Зал 2", rows: 6, seatsPerRow: 10);

            var interstellar = new Movie("Интерстеллар", durationMinutes: 169, ageLimit: 12);
            var lionKing = new Movie("Король Лев", durationMinutes: 118, ageLimit: 6);
            var inception = new Movie("Начало", durationMinutes: 148, ageLimit: 12);
            var joker = new Movie("Джокер", durationMinutes: 122, ageLimit: 18);

            _movies.AddRange(new[] { interstellar, lionKing, inception, joker });

            // Утром билеты дешевле, вечером дороже.
            _sessions.Add(new Session(lionKing,     hall1, new TimeSpan(10, 0, 0), 250));
            _sessions.Add(new Session(interstellar, hall1, new TimeSpan(12, 30, 0), 300));
            _sessions.Add(new Session(inception,    hall1, new TimeSpan(16, 0, 0), 350));
            _sessions.Add(new Session(interstellar, hall1, new TimeSpan(19, 0, 0), 450));
            _sessions.Add(new Session(joker,        hall1, new TimeSpan(22, 15, 0), 400));

            _sessions.Add(new Session(lionKing,     hall2, new TimeSpan(11, 0, 0), 250));
            _sessions.Add(new Session(inception,    hall2, new TimeSpan(13, 30, 0), 300));
            _sessions.Add(new Session(lionKing,     hall2, new TimeSpan(16, 30, 0), 300));
            _sessions.Add(new Session(joker,        hall2, new TimeSpan(19, 30, 0), 450));
            _sessions.Add(new Session(inception,    hall2, new TimeSpan(22, 0, 0), 400));
        }
    }
}
