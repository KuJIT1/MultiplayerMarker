namespace MultiplayerMarker.Core
{
    using Microsoft.AspNetCore.SignalR;
    using Microsoft.Extensions.DependencyInjection;
    using MultiplayerMarker.Entities;
    using MultiplayerMarker.Hub;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Движок игры. Запускает движения Путников по меткам
    /// TODO: проработать потокобезопасность.
    /// TODO: разделить по обязанностям, вынести связь с хабом
    /// </summary>
    public class Engine
    {
        // TODO: параллельная обработка. Куча багов
        private readonly IServiceProvider services;

        /// <summary>
        /// Скорость движения
        /// </summary>
        private readonly int speed = 10;

        private IHubContext<GameHub> HubContext => this.services.GetService<IHubContext<GameHub>>();

        public Engine(IServiceProvider services)
        {
            this.services = services;
        }

        private Dictionary<User, MovementInfo> movementDictionary = new Dictionary<User, MovementInfo>();

        // TODO: параллельность
        /// <summary>
        /// Обработчик события изменения списка меток
        /// </summary>
        /// <param name="changePathType">Тип события</param>
        /// <param name="user">Пользователь</param>
        /// <param name="mark">Добавленнаяя/удалённая метка</param>
        public void PathChanged(ChangePathType changePathType, User user, Mark mark)
        {
            if (changePathType == ChangePathType.Added)
            {
                this.HandleAddMark(user, mark);
            }
            else if (changePathType == ChangePathType.Removed)
            {
                this.HandleRemoveMark(user);
            }
            else if (changePathType == ChangePathType.RemovedAll)
            {
                this.HandleRemovedAll(user);
            }
        }

        private void HandleRemovedAll(User user)
        {
            Mutex mutex = null;

            if (this.movementDictionary.TryGetValue(user, out var movementInfo))
            {
                mutex = movementInfo.mutex;
                mutex.WaitOne();
                try
                {
                    movementInfo.cancellationToken.Cancel();
                    movementInfo.cancellationToken.Dispose();
                }
                catch (Exception)
                {

                }

                this.movementDictionary.Remove(user);
            }

            user.Wayfarer.InProgress = false;
            this.HubContext.Clients.All.SendAsync("StopMove", user.UserId);

            mutex?.ReleaseMutex();
        }

        private void HandleRemoveMark(User user)
        {
            Mutex mutex = null;
            if (user.Marks.Count < 2 && user.Wayfarer.InProgress)
            {
                if (this.movementDictionary.TryGetValue(user, out MovementInfo movementInfo))
                {
                    mutex = movementInfo.mutex;
                    mutex.WaitOne();
                    try
                    {
                        movementInfo.cancellationToken.Cancel();
                        movementInfo.cancellationToken.Dispose();
                    }
                    catch (Exception)
                    {

                    }

                    this.movementDictionary.Remove(user);
                }

                user.Wayfarer.InProgress = false;
                this.HubContext.Clients.All.SendAsync("StopMove", user.UserId);

                mutex?.ReleaseMutex();
            }
            else
            {
                // TODO: Изменить направление
            }
        }

        private void HandleAddMark(User user, Mark mark)
        {
            if (user.Marks.Count < 2)
            {
                if (user.Wayfarer.InProgress)
                {
                    user.Wayfarer.InProgress = false;
                    this.HubContext.Clients.All.SendAsync("StopMove", user.UserId);
                }

                return;
            }

            if (!user.Wayfarer.InProgress)
            {
                this.StartMove(user);
                return;
            }
        }

        private void DoStopMove(User user)
        {
            // TODO: вынести копипаст. Решить различия с мьютексами
        }

        private void StartMove(User user)
        {
            this.NextMove(user);
        }

        private void NextMove(User user)
        {
            long startId = 0;
            Mutex mutex = null;

            if (this.movementDictionary.TryGetValue(user, out var movementInfo))
            {
                mutex = movementInfo.mutex;
                mutex.WaitOne();
                try
                {
                    movementInfo.cancellationToken.Cancel();
                    movementInfo.cancellationToken.Dispose();
                }
                catch (Exception)
                {

                }

                startId = movementInfo.nextId;
            }

            if (user.Marks.Count < 2)
            {
                //marks может измениться извне
                mutex?.ReleaseMutex();
                return;
            }

            // Считаем, что метки упорядочены
            var wayFarer = user.Wayfarer;

            wayFarer.StartMark = user.Marks.Where(m => m.Id >= startId).FirstOrDefault() ?? user.Marks.First();
            wayFarer.EndMark = user.Marks.Where((m) => m.Id > wayFarer.StartMark.Id).FirstOrDefault() ?? user.Marks.First();

            var movementTime = Convert.ToInt32(Math.Sqrt(
                Math.Pow(wayFarer.StartMark.X - wayFarer.EndMark.X, 2)
                + Math.Pow(wayFarer.StartMark.Y - wayFarer.EndMark.Y, 2))) * this.speed;

            wayFarer.InProgress = true;
            wayFarer.StartTime = DateTime.Now;
            wayFarer.MovementTime = movementTime;

            var token = this.SetTImeOut(() => this.NextMove(user) , movementTime);

            this.movementDictionary[user] = new MovementInfo()
            {
                cancellationToken = token,
                nextId = wayFarer.EndMark.Id,
                mutex = mutex ?? new Mutex()
            };

            this.HubContext.Clients.All.SendAsync("StartMove", user.UserId, user.Wayfarer);
            mutex?.ReleaseMutex();
        }

        private CancellationTokenSource SetTImeOut(Action action, int timout)
        {
            var tokenSource = new CancellationTokenSource();
            var token = tokenSource.Token;

            Task.Delay(timout, token)
                .ContinueWith(
                t =>
                {
                    if (token.IsCancellationRequested)
                    {
                        return;
                    }

                    action();
                },
                token);

            return tokenSource;
        }

        private class MovementInfo
        {
            public long nextId;

            public CancellationTokenSource cancellationToken;

            public Mutex mutex;
        }
    }
}
