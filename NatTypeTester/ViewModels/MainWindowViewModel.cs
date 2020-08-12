using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using DynamicData;
using DynamicData.Binding;
using NatTypeTester.Model;
using ReactiveUI;
using STUN.Client;
using STUN.Utils;

namespace NatTypeTester.ViewModels
{
    public class MainWindowViewModel : ReactiveObject
    {
        #region RFC3489

        private string _classicNatType;
        public string ClassicNatType
        {
            get => _classicNatType;
            set => this.RaiseAndSetIfChanged(ref _classicNatType, value);
        }

        private string _localEnd;
        public string LocalEnd
        {
            get => _localEnd;
            set => this.RaiseAndSetIfChanged(ref _localEnd, value);
        }

        private string _publicEnd;
        public string PublicEnd
        {
            get => _publicEnd;
            set => this.RaiseAndSetIfChanged(ref _publicEnd, value);
        }

        public ReactiveCommand<Unit, Unit> TestClassicNatType { get; }

        #endregion

        #region Servers

        private string _stunServer;
        public string StunServer
        {
            get => _stunServer;
            set => this.RaiseAndSetIfChanged(ref _stunServer, value);
        }

        private readonly IEnumerable<string> _defaultServers = new HashSet<string>
        {
                @"stun.syncthing.net",
                @"stun.qq.com",
                @"stun.miwifi.com",
                @"stun.bige0.com",
                @"stun.stunprotocol.org"
        };

        private SourceList<string> List { get; } = new SourceList<string>();
        public readonly IObservableCollection<string> StunServers = new ObservableCollectionExtended<string>();

        #endregion

        public MainWindowViewModel()
        {
            LoadStunServer();
            List.Connect()
                .DistinctValues(x => x)
                .ObserveOnDispatcher()
                .Bind(StunServers)
                .Subscribe();
            TestClassicNatType = ReactiveCommand.CreateFromObservable(TestClassicNatTypeImp);
        }

        private async void LoadStunServer()
        {
            foreach (var server in _defaultServers)
            {
                List.Add(server);
            }
            StunServer = _defaultServers.First();

            const string path = @"stun.txt";

            if (!File.Exists(path)) return;

            using var sw = new StreamReader(path);
            string line;
            var stun = new StunServer();
            while ((line = await sw.ReadLineAsync()) != null)
            {
                if (!string.IsNullOrWhiteSpace(line) && stun.Parse(line))
                {
                    List.Add(stun.ToString());
                }
            }
        }

        private IObservable<Unit> TestClassicNatTypeImp()
        {
            return Observable.Start(() =>
            {
                var server = new StunServer();
                if (server.Parse(StunServer))
                {
                    using var client = new StunClient3489(server.Hostname, server.Port, NetUtils.ParseEndpoint(LocalEnd));
                    client.NatTypeChanged.ObserveOn(RxApp.MainThreadScheduler).Subscribe(t => ClassicNatType = $@"{t}");
                    client.PubChanged.ObserveOn(RxApp.MainThreadScheduler).Subscribe(t => PublicEnd = $@"{t}");
                    client.LocalChanged.ObserveOn(RxApp.MainThreadScheduler).Subscribe(t => LocalEnd = $@"{t}");
                    client.Query();
                }
                else
                {

                }
            });
        }
    }
}
