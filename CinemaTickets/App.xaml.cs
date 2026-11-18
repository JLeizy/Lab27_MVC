using System.Windows;
using CinemaTickets.Controllers;
using CinemaTickets.Models;
using CinemaTickets.Views;

namespace CinemaTickets
{
    /// <summary>
    /// Точка входа. Здесь создаются три части MVC и связываются между собой.
    ///
    /// Схема связей:
    ///
    ///     пользователь ──нажал кнопку──► View ──передал ввод──► Controller
    ///                                     ▲                          │
    ///                                     │                  проверил и передал
    ///                    событие Changed  │                          ▼
    ///                    ("я изменилась") └──────────────────────  Model
    ///
    /// В методичке всё было в одном файле окна. Здесь каждая часть в своём файле,
    /// а соединяются они только в этом месте.
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // M — модель: данные и логика кассы.
            var model = new CinemaModel();

            // V — вид: окно. Получает модель, чтобы читать из неё данные
            //     и подписаться на её событие Changed.
            var view = new MainWindow(model);

            // C — контроллер: получает модель (кому передавать проверенные данные)
            //     и вид (кому сообщать об ошибках).
            var controller = new TicketController(model, view);

            // Окно будет передавать действия пользователя этому контроллеру.
            view.SetController(controller);

            view.Show();
        }
    }
}
