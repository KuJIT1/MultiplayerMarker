namespace MultiplayerMarker.Hub
{
    using Microsoft.AspNetCore.SignalR;
    using MultiplayerMarker.Core;
    using MultiplayerMarker.Entities;
    using MultiplayerMarker.Utils;
    using System.Threading.Tasks;

    public class GameHub: Hub
    {
        private readonly GameCore gameCore;

        // TODO: выдавать токены
        private string userId => this.Context.ConnectionId;

        public GameHub(GameCore gameCore): base()
        {
            this.gameCore = gameCore;
        }

        public async Task AddMark(Mark mark)
        {
            this.gameCore.AddMark(mark, this.userId);
            await this.Clients.All.SendAsync("MarkAdded", mark, this.userId);
        }

        public async Task RemoveMark(int markId)
        {
            this.gameCore.RemoveMark(markId, this.userId);
            await this.Clients.All.SendAsync("MarkRemoved", markId, this.userId);
        }

        public async Task<User[]> Syncronize()
        {
            return this.gameCore.Users;
        }

        public async Task<TryActionResult> TryAddUser(string userName)
        {
            var result = this.gameCore.TryAddUser(userName, this.userId);
            if (result.Success)
            {
                await this.Clients.All.SendAsync("UserAdded", result.User);
            }

            return result;
        }

        public async Task ClearMarks()
        {
            this.gameCore.ClearMarks(this.userId);
            await this.Clients.All.SendAsync("MarksCleeared", this.userId);
        }
    }
}
