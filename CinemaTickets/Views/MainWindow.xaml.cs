using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using CinemaTickets.Controllers;
using CinemaTickets.Models;

namespace CinemaTickets.Views
{
    /// <summary>
    /// VIEW — код окна. У него ровно две обязанности:
    ///
    ///   1) ПОКАЗЫВАТЬ данные модели. Метод UpdateView() читает модель и
    ///      заполняет списки, схему зала, цену, таблицу билетов и выручку.
    ///      Вызывается каждый раз, когда модель сообщает событием Changed,
    ///      что она изменилась (так же, как UpdateView() в методичке).
    ///
    ///   2) ПЕРЕДАВАТЬ действия пользователя контроллеру. Обработчики кнопок
    ///      здесь в одну строку: взять текст из полей и отдать контроллеру.
    ///      Проверок и расчётов в окне нет — это работа Controller и Model.
    ///
    /// Окно реализует ICinemaView, чтобы контроллер мог показать ошибку,
    /// сообщение или задать вопрос, не зная, что это именно WPF-окно.
    /// </summary>
    public partial class MainWindow : Window, ICinemaView
    {
        private readonly CinemaModel _model;
        private TicketController? _controller;

        // Когда UpdateView сам меняет выбранный элемент в ComboBox, WPF вызывает
        // SelectionChanged так же, как если бы это сделал пользователь.
        // Этот флаг говорит обработчикам: "это не пользователь, контроллер не зови",
        // иначе получился бы лишний (или бесконечный) круг View → Controller → Model → View.
        private bool _isUpdatingView;

        // Какой фильм и сеанс сейчас показаны на экране. Нужны, чтобы заметить их смену
        // (см. UpdateView): при смене фильма — заново заполнить список сеансов,
        // при смене сеанса — очистить поля "Ряд"/"Место".
        private Movie? _shownMovie;
        private Session? _shownSession;

        public MainWindow(CinemaModel model)
        {
            InitializeComponent();

            _model = model;

            // Подписка на модель: "когда изменишься — скажи мне, я перерисуюсь".
            _model.Changed += Model_Changed;

            _isUpdatingView = true;
            // Эти списки за время работы программы не меняются — заполняем один раз.
            MovieComboBox.ItemsSource = _model.Movies;
            CategoryComboBox.ItemsSource = TicketCategory.All;
            CategoryComboBox.SelectedItem = TicketCategory.Adult;
            _isUpdatingView = false;

            UpdateView();
        }

        /// <summary>
        /// Подключить контроллер, которому окно будет передавать действия пользователя.
        /// Вызывается в App.xaml.cs, где создаются и связываются все три части MVC.
        /// </summary>
        public void SetController(TicketController controller)
        {
            _controller = controller;
        }

        private TicketController Controller =>
            _controller ?? throw new InvalidOperationException("Контроллер не подключён к окну.");

        // =====================================================================
        //  1. ДЕЙСТВИЯ ПОЛЬЗОВАТЕЛЯ → КОНТРОЛЛЕР
        // =====================================================================

        private void MovieComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isUpdatingView) return;
            Controller.SelectMovie(MovieComboBox.SelectedItem);
        }

        private void SessionComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isUpdatingView) return;
            Controller.SelectSession(SessionComboBox.SelectedItem);
        }

        /// <summary>
        /// Сменили тип билета — если ряд и место уже введены, сразу пересчитываем цену,
        /// чтобы на экране не осталась цена для старого типа билета.
        /// </summary>
        private void CategoryComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isUpdatingView) return;
            if (RowTextBox.Text.Trim().Length > 0 && SeatTextBox.Text.Trim().Length > 0)
                Controller.CalculatePrice(RowTextBox.Text, SeatTextBox.Text, CategoryComboBox.SelectedItem);
        }

        /// <summary>Кнопка "Рассчитать стоимость" — вызов контроллера, как в методичке.</summary>
        private void CalculateButton_Click(object sender, RoutedEventArgs e)
        {
            Controller.CalculatePrice(RowTextBox.Text, SeatTextBox.Text, CategoryComboBox.SelectedItem);
        }

        private void SellButton_Click(object sender, RoutedEventArgs e)
        {
            Controller.SellTicket(RowTextBox.Text, SeatTextBox.Text, CategoryComboBox.SelectedItem);
        }

        private void ReturnButton_Click(object sender, RoutedEventArgs e)
        {
            Controller.ReturnTicket(TicketsListView.SelectedItem);
        }

        /// <summary>
        /// Щелчок по месту на схеме зала. Это просто удобный способ ввода:
        /// номер ряда и места подставляются в поля, как будто их набрали руками,
        /// и дальше всё идёт обычным путём — через контроллер.
        /// </summary>
        private void Seat_Click(object sender, RoutedEventArgs e)
        {
            var (row, seat) = ((int Row, int Seat))((Button)sender).Tag;

            RowTextBox.Text = row.ToString();
            SeatTextBox.Text = seat.ToString();

            Controller.CalculatePrice(RowTextBox.Text, SeatTextBox.Text, CategoryComboBox.SelectedItem);
        }

        /// <summary>
        /// Пользователь печатает в полях "Ряд"/"Место" — перерисовываем схему,
        /// чтобы подсветить выбранное место. Данные модели при этом не меняются,
        /// это чисто внешний вид, поэтому контроллер здесь не нужен.
        /// </summary>
        private void OrderInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isUpdatingView) return;
            DrawSeatMap();
        }

        // =====================================================================
        //  2. МОДЕЛЬ ИЗМЕНИЛАСЬ → ОБНОВИТЬ ВИД
        // =====================================================================

        private void Model_Changed(object? sender, EventArgs e) => UpdateView();

        /// <summary>
        /// Перерисовать окно по текущим данным модели.
        /// Окно ничего не хранит "у себя" — всё берёт из модели, поэтому
        /// на экране всегда то, что есть на самом деле.
        /// </summary>
        public void UpdateView()
        {
            _isUpdatingView = true;
            try
            {
                // Выбранный фильм.
                MovieComboBox.SelectedItem = _model.SelectedMovie;

                // Список сеансов заполняем заново, только когда сменился фильм.
                // Если пересоздавать его при каждом обновлении, то при выборе сеанса
                // список менялся бы прямо внутри его же события SelectionChanged.
                if (_model.SelectedMovie != _shownMovie)
                {
                    SessionComboBox.ItemsSource = _model.SelectedMovie == null
                        ? null
                        : _model.GetSessions(_model.SelectedMovie);
                    _shownMovie = _model.SelectedMovie;
                }
                SessionComboBox.SelectedItem = _model.SelectedSession;

                Session? session = _model.SelectedSession;

                // Сменился сеанс — значит, и зал может быть другим (в «Зале 2» всего 6 рядов).
                // Ряд и место, введённые для прошлого сеанса, теперь ничего не значат — очищаем.
                if (session != _shownSession)
                {
                    RowTextBox.Text = "";
                    SeatTextBox.Text = "";
                    _shownSession = session;
                }

                // Подсказка о размере зала.
                HallHintTextBlock.Text = session == null
                    ? ""
                    : $"В зале {session.Hall.Rows} рядов по {session.Hall.SeatsPerRow} мест. " +
                      $"Ряд {session.Hall.Rows} — VIP (цена ×1,5). Место можно выбрать щелчком по схеме.";

                DrawSeatMap();

                // Результат последнего расчёта (в методичке: ResultTextBlock.Text = result.ToString()).
                PriceCalculation? calculation = _model.LastCalculation;
                PriceTextBlock.Text = calculation == null ? "—" : $"{calculation.Price:0} руб.";
                PriceDetailsTextBlock.Text = calculation == null
                    ? "Выберите место и нажмите «Рассчитать стоимость»"
                    : $"Ряд {calculation.Row}, место {calculation.Seat}, {calculation.Category.Name.ToLower()}";

                // Таблица билетов: новые сверху, чтобы только что проданный был виден сразу.
                TicketsListView.ItemsSource = _model.SoldTickets
                    .OrderByDescending(t => t.Number)
                    .ToList();

                TotalsTextBlock.Text =
                    $"Продано билетов: {_model.SoldTickets.Count}     Выручка: {_model.Revenue:0} руб.";
            }
            finally
            {
                // finally — чтобы флаг сбросился, даже если при отрисовке случится ошибка.
                _isUpdatingView = false;
            }
        }

        /// <summary>
        /// Нарисовать схему выбранного сеанса: ряд за рядом, место за местом.
        /// Кнопки создаются в коде, потому что число рядов и мест зависит от зала.
        /// </summary>
        private void DrawSeatMap()
        {
            SeatMapPanel.Children.Clear();

            Session? session = _model.SelectedSession;
            if (session == null)
            {
                HallGroupBox.Header = "Схема зала";
                return;
            }

            HallGroupBox.Header =
                $"Схема зала — {session.Hall.Name}, «{session.Movie.Title}» в {session.StartTimeText}. " +
                $"Свободно {session.FreeSeatsCount} из {session.Hall.Capacity}";

            // Какое место сейчас введено в полях — его подсветим синим.
            // int.TryParse здесь НЕ проверка ввода (её делает контроллер),
            // а попытка понять, что подсветить: не число — значит, ничего.
            int.TryParse(RowTextBox.Text.Trim(), out int chosenRow);
            int.TryParse(SeatTextBox.Text.Trim(), out int chosenSeat);

            var seatStyle = (Style)FindResource("SeatButtonStyle");

            for (int row = 1; row <= session.Hall.Rows; row++)
            {
                bool isVip = session.Hall.IsVipRow(row);

                var rowPanel = new StackPanel { Orientation = Orientation.Horizontal };
                rowPanel.Children.Add(new TextBlock
                {
                    Text = isVip ? $"Ряд {row} VIP" : $"Ряд {row}",
                    Width = 70,
                    FontSize = 11,
                    Foreground = Brushes.Gray,
                    VerticalAlignment = VerticalAlignment.Center
                });

                for (int seat = 1; seat <= session.Hall.SeatsPerRow; seat++)
                {
                    bool isTaken = session.IsSeatTaken(row, seat);
                    bool isChosen = row == chosenRow && seat == chosenSeat;

                    var seatButton = new Button
                    {
                        Content = seat.ToString(),
                        Style = seatStyle,
                        Tag = (row, seat), // по Tag обработчик узнает, какое место нажали
                        ToolTip = $"Ряд {row}, место {seat}: " +
                                  (isTaken ? "продано" : isVip ? "свободно, VIP-ряд" : "свободно")
                    };

                    if (isTaken)
                    {
                        seatButton.Background = (Brush)FindResource("TakenSeatBrush");
                        seatButton.Foreground = Brushes.White;
                        seatButton.IsEnabled = false; // занятое место нельзя выбрать
                    }
                    else if (isChosen)
                    {
                        seatButton.Background = (Brush)FindResource("ChosenSeatBrush");
                        seatButton.Foreground = Brushes.White;
                        seatButton.FontWeight = FontWeights.Bold;
                    }
                    else
                    {
                        seatButton.Background = (Brush)FindResource(isVip ? "VipSeatBrush" : "FreeSeatBrush");
                    }

                    seatButton.Click += Seat_Click;
                    rowPanel.Children.Add(seatButton);
                }

                SeatMapPanel.Children.Add(rowPanel);
            }
        }

        // =====================================================================
        //  3. РЕАЛИЗАЦИЯ ICinemaView (это вызывает контроллер)
        // =====================================================================

        public void ShowError(string message)
        {
            StatusTextBlock.Foreground = Brushes.DarkRed;
            StatusTextBlock.Text = "Ошибка: " + message;
            MessageBox.Show(this, message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        public void ShowInfo(string message)
        {
            StatusTextBlock.Foreground = Brushes.DarkGreen;
            StatusTextBlock.Text = message;
        }

        public bool Confirm(string question)
        {
            return MessageBox.Show(this, question, "Подтверждение",
                                   MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;
        }
    }
}
