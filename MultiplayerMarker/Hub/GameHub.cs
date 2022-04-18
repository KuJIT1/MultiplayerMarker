namespace MultiplayerMarker.Hub
{
    using Microsoft.AspNetCore.SignalR;
    using System.Threading.Tasks;

    using MultiplayerMarker.Core;
    using MultiplayerMarker.Entities;
    using MultiplayerMarker.Utils;

    /// <summary>
    /// Хаб для связи пользователей и игры
    /// </summary>
    public class GameHub: Hub
    {
        private readonly GameCore gameCore;

        // TODO: выдавать токены чтобы была авторизация через куки
        private string userId => this.Context.ConnectionId;

        public GameHub(GameCore gameCore): base()
        {
            this.gameCore = gameCore;
        }

        /// <summary>
        /// Добавлена метка
        /// </summary>
        /// <param name="mark">Метка</param>
        /// <returns></returns>
        public async Task AddMark(Mark mark)
        {
            this.gameCore.AddMark(mark, this.userId);
            await this.Clients.All.SendAsync("MarkAdded", mark, this.userId);
        }

        /// <summary>
        /// Метка удалена
        /// </summary>
        /// <param name="markId">Идентификатор метки</param>
        /// <returns></returns>
        public async Task RemoveMark(int markId)
        {
            this.gameCore.RemoveMark(markId, this.userId);
            await this.Clients.All.SendAsync("MarkRemoved", markId, this.userId);
        }

        /// <summary>
        /// Синхронизация. Предоставляет полную информацию о пользователях и их метках
        /// TODO: отправлять информацию о текущих движениях
        /// </summary>
        /// <returns>Список опльзователей с метками</returns>
        public async Task<User[]> Syncronize()
        {
            return this.gameCore.Users;
        }

        /// <summary>
        /// Попытка добавить пользователя с указанным именем
        /// </summary>
        /// <param name="userName">Имя пользователя</param>
        /// <returns>Если имя занято, то сообщение об ошибке</returns>
        public async Task<TryActionResult> TryAddUser(string userName)
        {
            var result = this.gameCore.TryAddUser(userName, this.userId);
            if (result.Success)
            {
                await this.Clients.All.SendAsync("UserAdded", result.User);
            }

            return result;
        }

        /// <summary>
        /// Очистка меток у пользователя
        /// </summary>
        /// <returns></returns>
        public async Task ClearMarks()
        {
            this.gameCore.ClearMarks(this.userId);
            await this.Clients.All.SendAsync("MarksCleeared", this.userId);
        }
    }
}
