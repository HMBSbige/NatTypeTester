using DynamicData;
using DynamicData.Binding;
using NatTypeTester.Model;
using ReactiveUI;
using STUN.Client;
using STUN.Enums;
using STUN.Proxy;
using STUN.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Windows;

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

        private string _localEnd = NetUtils.DefaultLocalEnd;
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

        #region RFC5780

        private string _bindingTest;
        public string BindingTest
        {
            get => _bindingTest;
            set => this.RaiseAndSetIfChanged(ref _bindingTest, value);
        }

        private string _mappingBehavior;
        public string MappingBehavior
        {
            get => _mappingBehavior;
            set => this.RaiseAndSetIfChanged(ref _mappingBehavior, value);
        }

        private string _filteringBehavior;
        public string FilteringBehavior
        {
            get => _filteringBehavior;
            set => this.RaiseAndSetIfChanged(ref _filteringBehavior, value);
        }

        private string _localAddress = NetUtils.DefaultLocalEnd;
        public string LocalAddress
        {
            get => _localAddress;
            set => this.RaiseAndSetIfChanged(ref _localAddress, value);
        }

        private string _mappingAddress;
        public string MappingAddress
        {
            get => _mappingAddress;
            set => this.RaiseAndSetIfChanged(ref _mappingAddress, value);
        }

        public ReactiveCommand<Unit, Unit> DiscoveryNatType { get; }

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

        #region Proxy

        private ProxyType _proxyType = ProxyType.Socks5;
        public ProxyType ProxyType
        {
            get => _proxyType;
            set => this.RaiseAndSetIfChanged(ref _proxyType, value);
        }

        private string _proxyServer = @"127.0.0.1:1080";
        public string ProxyServer
        {
            get => _proxyServer;
            set => this.RaiseAndSetIfChanged(ref _proxyServer, value);
        }

        private string _proxyUser;
        public string ProxyUser
        {
            get => _proxyUser;
            set => this.RaiseAndSetIfChanged(ref _proxyUser, value);
        }

        private string _proxyPassword;
        public string ProxyPassword
        {
            get => _proxyPassword;
            set => this.RaiseAndSetIfChanged(ref _proxyPassword, value);
        }

        #endregion

        public MainWindowViewModel()
        {
            LoadStunServer();
            List.Connect()
                .DistinctValues(x => x)
                .ObserveOnDispatcher()
                .Bind(StunServers)
                .Subscribe();
            TestClassicNatType = ReactiveCommand.CreateFromObservable(TestClassicNatTypeImpl);
            DiscoveryNatType = ReactiveCommand.CreateFromObservable(DiscoveryNatTypeImpl);
        }

        private async void LoadStunServer()
        {
            foreach (var server in _defaultServers)
            {
                List.Add(server);
            }
            StunServer = _defaultServers.First();

            const string path = @"stun.txt";

            if (!File.Exists(path))
            {
                return;
            }

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

        private IObservable<Unit> TestClassicNatTypeImpl()
        {
            return Observable.FromAsync(async () =>
            {
                try
                {
                    var server = new StunServer();
                    if (server.Parse(StunServer))
                    {
                        using var proxy = ProxyFactory.CreateProxy(
                            ProxyType,
                            NetUtils.ParseEndpoint(LocalEnd),
                            NetUtils.ParseEndpoint(ProxyServer),
                            ProxyUser, ProxyPassword
                            );

                        using var client = new StunClient3489(server.Hostname, server.Port, NetUtils.ParseEndpoint(LocalEnd), proxy);

                        client.NatTypeChanged.ObserveOn(RxApp.MainThreadScheduler)
                                .Subscribe(t => ClassicNatType = $@"{t}");
                        client.PubChanged.ObserveOn(RxApp.MainThreadScheduler).Subscribe(t => PublicEnd = $@"{t}");
                        client.LocalChanged.ObserveOn(RxApp.MainThreadScheduler).Subscribe(t => LocalEnd = $@"{t}");
                        await client.Query3489Async();
                    }
                    else
                    {
                        throw new Exception(@"Wrong STUN Server!");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, nameof(NatTypeTester), MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }).SubscribeOn(RxApp.TaskpoolScheduler);
        }

        private IObservable<Unit> DiscoveryNatTypeImpl()
        {
            return Observable.FromAsync(async () =>
            {
                try
                {
                    var server = new StunServer();
                    if (server.Parse(StunServer))
                    {
                        using var proxy = ProxyFactory.CreateProxy(
                            ProxyType,
                            NetUtils.ParseEndpoint(LocalEnd),
                            NetUtils.ParseEndpoint(ProxyServer),
                            ProxyUser, ProxyPassword
                            );

                        using var client = new StunClient5389UDP(server.Hostname, server.Port, NetUtils.ParseEndpoint(LocalAddress), proxy);

                        client.BindingTestResultChanged
                                .ObserveOn(RxApp.MainThreadScheduler)
                                .Subscribe(t => BindingTest = $@"{t}");

                        client.MappingBehaviorChanged
                                .ObserveOn(RxApp.MainThreadScheduler)
                                .Subscribe(t => MappingBehavior = $@"{t}");

                        client.FilteringBehaviorChanged
                                .ObserveOn(RxApp.MainThreadScheduler)
                                .Subscribe(t => FilteringBehavior = $@"{t}");

                        client.PubChanged
                                .ObserveOn(RxApp.MainThreadScheduler)
                                .Subscribe(t => MappingAddress = $@"{t}");

                        client.LocalChanged
                                .ObserveOn(RxApp.MainThreadScheduler)
                                .Subscribe(t => LocalAddress = $@"{t}");

                        await client.QueryAsync();
                    }
                    else
                    {
                        throw new Exception(@"Wrong STUN Server!");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, nameof(NatTypeTester), MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }).SubscribeOn(RxApp.TaskpoolScheduler);
        }
    }
}
