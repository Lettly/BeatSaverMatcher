﻿using BeatSaverMatcher.Web.Result;
using Microsoft.Extensions.Hosting;
using Prometheus;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BeatSaverMatcher.Web
{
    public class SongMatchWorker : IHostedService
    {
        private const int _maxRunningTasks = 8;

        private readonly MatchingService _matchingService;
        private readonly WorkItemStore _itemStore;
        private readonly Gauge _runningMatchesGauge;
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();

        private readonly List<Task> _runningTasks = new List<Task>();

        public SongMatchWorker(MatchingService matchingService, WorkItemStore itemStore)
        {
            _matchingService = matchingService;
            _itemStore = itemStore;
            _runningMatchesGauge = Metrics.CreateGauge("beatsaver_running_requests", "Requests currently running");
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            Task.Factory.StartNew(() => DoWork(), TaskCreationOptions.LongRunning);
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _cts.Cancel();
            return Task.CompletedTask;
        }

        private async Task DoWork()
        {
            while (!_cts.IsCancellationRequested)
            {
                _runningMatchesGauge.Set(_runningTasks.Count);
                if (_runningTasks.Count > _maxRunningTasks)
                {
                    var task = await Task.WhenAny(_runningTasks);
                    _runningTasks.Remove(task);
                }
                else if (_itemStore.TryDequeue(out var item))
                {
                    var task = Task.Run(() => _matchingService.GetMatches(item));
                    _runningTasks.Add(task);
                }
                else
                {
                    await Task.Delay(1000);
                }
            }
        }
    }
}
