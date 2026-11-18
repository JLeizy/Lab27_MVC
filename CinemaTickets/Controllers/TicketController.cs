using System.Diagnostics.CodeAnalysis;
using CinemaTickets.Models;
using CinemaTickets.Views;

namespace CinemaTickets.Controllers
{
    /// <summary>
    /// CONTROLLER — обработчик действий пользователя.
    ///
    /// Что делает:
    ///   1) получает от вида то, что ввёл пользователь, в "сыром" виде
    ///      (строки из TextBox, выбранный элемент ComboBox/таблицы);
    ///   2) проверяет ввод (валидация): не пусто ли, число ли, в допустимых ли пределах;
    ///   3) если всё верно — передаёт уже проверенные данные модели;
    ///   4) если что-то не так — просит вид показать ошибку, а модель не трогает.
    ///
    /// Чего НЕ делает: сам ничего не рисует и цену не считает.
    /// Рисует View, считает Model.
    ///
    /// Аналогия из методички: контроллер — это кассир. Он проверяет,
    /// всё ли в порядке с заказом, и только потом передаёт его повару (модели).
    /// </summary>
    public class TicketController
    {
        private readonly CinemaModel _model;
        private readonly ICinemaView _view;

        public TicketController(CinemaModel model, ICinemaView view)
        {
            _model = model;
            _view = view;
        }

        /// <summary>Пользователь выбрал фильм в списке.</summary>
        public void SelectMovie(object? selectedItem)
        {
            // Если в списке ничего не выбрано (например, список только что
            // перезаполнили), то и делать нечего — это не ошибка пользователя.
            if (selectedItem is not Movie movie)
                return;

            _model.SelectMovie(movie);

            // Сообщение в строке статуса заодно заменяет старое (например, прошлую ошибку).
            _view.ShowInfo($"Фильм «{movie.Title}», сеанс {_model.SelectedSession?.StartTimeText}. " +
                           "Выберите место на схеме зала.");
        }

        /// <summary>Пользователь выбрал сеанс в списке.</summary>
        public void SelectSession(object? selectedItem)
        {
            if (selectedItem is not Session session)
                return;

            try
            {
                _model.SelectSession(session);
                _view.ShowInfo($"Сеанс {session.StartTimeText}, {session.Hall.Name}. Выберите место на схеме зала.");
            }
            catch (CinemaRuleException ex)
            {
                _view.ShowError(ex.Message);
            }
        }

        /// <summary>Нажата кнопка "Рассчитать стоимость".</summary>
        public void CalculatePrice(string rowText, string seatText, object? selectedCategory)
        {
            // Валидация. Если ввод неверный — ошибка уже показана, просто выходим.
            if (!TryReadOrder(rowText, seatText, selectedCategory,
                              out int row, out int seat, out TicketCategory? category))
                return;

            try
            {
                // Передача проверенных данных модели (как Calculate(num1, num2) в методичке).
                // Модель посчитает цену и сама попросит вид обновиться.
                _model.CalculatePrice(row, seat, category);
            }
            catch (CinemaRuleException ex)
            {
                // Ввод корректный, но нарушено правило кинотеатра
                // (например, детский билет на фильм 18+). Об этом знает только модель.
                _view.ShowError(ex.Message);
            }
        }

        /// <summary>Нажата кнопка "Продать билет".</summary>
        public void SellTicket(string rowText, string seatText, object? selectedCategory)
        {
            if (!TryReadOrder(rowText, seatText, selectedCategory,
                              out int row, out int seat, out TicketCategory? category))
                return;

            try
            {
                Ticket ticket = _model.SellTicket(row, seat, category);
                _view.ShowInfo($"Продажа оформлена. {ticket}");
            }
            catch (CinemaRuleException ex)
            {
                _view.ShowError(ex.Message); // например, "место уже продано"
            }
        }

        /// <summary>Нажата кнопка "Вернуть билет".</summary>
        public void ReturnTicket(object? selectedTicket)
        {
            if (selectedTicket is not Ticket ticket)
            {
                _view.ShowError("Выберите в таблице проданных билетов тот, который нужно вернуть.");
                return;
            }

            // Возврат денег — необратимое действие, поэтому сначала спрашиваем.
            if (!_view.Confirm($"Вернуть билет №{ticket.Number} и отдать покупателю {ticket.Price:0} руб.?"))
                return;

            try
            {
                _model.ReturnTicket(ticket);
                _view.ShowInfo($"Билет №{ticket.Number} возвращён. Ряд {ticket.Row}, место {ticket.Seat} снова свободно.");
            }
            catch (CinemaRuleException ex)
            {
                _view.ShowError(ex.Message);
            }
        }

        /// <summary>
        /// Общая проверка данных заказа для "Рассчитать" и "Продать".
        /// Возвращает true, если всё введено правильно; иначе показывает ошибку и возвращает false.
        ///
        /// В методичке использовался Convert.ToInt32 внутри try/catch с пустым catch —
        /// тогда при вводе "абв" программа молча считала с нулём. Здесь используется
        /// int.TryParse: он не бросает исключение, а просто сообщает, удалось ли
        /// превратить текст в число, и мы показываем пользователю понятную ошибку.
        /// </summary>
        /// <remarks>
        /// [NotNullWhen(true)] подсказывает компилятору: если метод вернул true,
        /// то category точно не null, и дальше её можно передавать в модель без проверок.
        /// </remarks>
        private bool TryReadOrder(string rowText, string seatText, object? selectedCategory,
                                  out int row, out int seat,
                                  [NotNullWhen(true)] out TicketCategory? category)
        {
            row = 0;
            seat = 0;
            category = null;

            Session? session = _model.SelectedSession;
            if (session == null)
            {
                _view.ShowError("Сначала выберите фильм и сеанс.");
                return false;
            }

            // Trim() убирает случайные пробелы: " 5 " тоже считается правильным вводом.
            rowText = rowText.Trim();
            seatText = seatText.Trim();

            if (string.IsNullOrEmpty(rowText) || string.IsNullOrEmpty(seatText))
            {
                _view.ShowError("Введите номер ряда и номер места (или щёлкните по месту на схеме зала).");
                return false;
            }

            // Границы берём из модели (размер выбранного зала), а не пишем числа в коде:
            // у разных залов разное количество рядов и мест.
            if (!int.TryParse(rowText, out row) || row < 1 || row > session.Hall.Rows)
            {
                _view.ShowError($"Ряд должен быть целым числом от 1 до {session.Hall.Rows}.");
                return false;
            }

            if (!int.TryParse(seatText, out seat) || seat < 1 || seat > session.Hall.SeatsPerRow)
            {
                _view.ShowError($"Место должно быть целым числом от 1 до {session.Hall.SeatsPerRow}.");
                return false;
            }

            if (selectedCategory is not TicketCategory selected)
            {
                _view.ShowError("Выберите тип билета.");
                return false;
            }

            category = selected;
            return true;
        }
    }
}
