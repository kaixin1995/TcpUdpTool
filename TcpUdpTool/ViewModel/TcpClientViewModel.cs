using System;
using System.Windows.Input;
using System.Windows.Threading;
using TcpUdpTool.Model;
using TcpUdpTool.Model.Data;
using TcpUdpTool.Model.Util;
using TcpUdpTool.ViewModel.Base;

namespace TcpUdpTool.ViewModel
{
    public class TcpClientViewModel : ObservableObject, IDisposable
    {

        #region private Members

        private TcpClient _tcpClient;
        private readonly DispatcherTimer _autoSendTimer;
     
        #endregion

        #region public Properties

        private HistoryViewModel _historyViewModel = new HistoryViewModel();
        public HistoryViewModel History
        {
            get { return _historyViewModel; }
        }

        private SendViewModel _sendViewModel = new SendViewModel();
        public SendViewModel Send
        {
            get { return _sendViewModel; }
        }

        private bool _isConnected;
        public bool IsConnected
        {
            get { return _isConnected; }
            set
            {
                _isConnected = value;
                OnPropertyChanged(nameof(IsConnected));
                UpdateAutoSendTimer();
            }
        }

        private bool _isConnecting;
        public bool IsConnecting
        {
            get { return _isConnecting; }
            set
            {
                _isConnecting = value;
                OnPropertyChanged(nameof(IsConnecting));
            }
        }

        private bool _autoSendEnabled;
        public bool AutoSendEnabled
        {
            get { return _autoSendEnabled; }
            set
            {
                if (_autoSendEnabled != value)
                {
                    _autoSendEnabled = value;
                    OnPropertyChanged(nameof(AutoSendEnabled));
                    UpdateAutoSendTimer();
                }
            }
        }

        private int? _autoSendIntervalMs;
        public int? AutoSendIntervalMs
        {
            get { return _autoSendIntervalMs; }
            set
            {
                if (_autoSendIntervalMs != value)
                {
                    _autoSendIntervalMs = value;

                    if (!_autoSendIntervalMs.HasValue || _autoSendIntervalMs.Value < 1)
                    {
                        AddError(nameof(AutoSendIntervalMs), "间隔必须大于 0 毫秒。");
                    }
                    else
                    {
                        RemoveError(nameof(AutoSendIntervalMs));
                    }

                    OnPropertyChanged(nameof(AutoSendIntervalMs));
                    UpdateAutoSendTimer();
                }
            }
        }

        private string _ipAddress;
        public string IpAddress
        {
            get { return _ipAddress; }
            set
            {
                if(_ipAddress != value)
                {
                    _ipAddress = value;
                        
                    if(String.IsNullOrWhiteSpace(_ipAddress))
                    {
                        AddError(nameof(IpAddress), "IP 地址不能为空。");
                    }
                    else
                    {
                        RemoveError(nameof(IpAddress));
                    }
                      
                    OnPropertyChanged(nameof(IpAddress));
                }
            }
        }

        private int? _port;
        public int? Port
        {
            get { return _port; }
            set
            {
                if(_port != value)
                {
                    _port = value;

                    if(!NetworkUtils.IsValidPort(_port.HasValue ? _port.Value : -1, false))
                    {
                        AddError(nameof(Port), "端口必须在 1 到 65535 之间。");
                    }
                    else
                    {
                        RemoveError(nameof(Port));
                    }
                }

                OnPropertyChanged(nameof(Port));
            }
        }

        #endregion

        #region public Commands

        public ICommand ConnectDisconnectCommand
        {
            get
            {
                return new DelegateCommand(() =>
                    {
                        if (IsConnected)
                        {
                            Disconnect();
                        }                         
                        else
                        {
                            Connect();
                        }                          
                    }                  
                );
            }
        }

        #endregion

        #region constructors

        public TcpClientViewModel()
        {
            _tcpClient = new TcpClient();
            _autoSendTimer = new DispatcherTimer();
            _autoSendTimer.Tick += (sender, args) =>
            {
                if (!IsConnected || !AutoSendEnabled)
                {
                    _autoSendTimer.Stop();
                    return;
                }

                _sendViewModel.SendNow();
            };

            _sendViewModel.SendData += OnSend;
            _sendViewModel.PropertyChanged += (sender, args) =>
            {
                if (args.PropertyName == nameof(SendViewModel.Message))
                {
                    UpdateAutoSendTimer();
                }
            };
            _tcpClient.StatusChanged += 
                (sender, arg) => 
                {
                    IsConnected = arg.Status == TcpClientStatusEventArgs.EConnectStatus.Connected;
                    IsConnecting = arg.Status == TcpClientStatusEventArgs.EConnectStatus.Connecting;

                    if(IsConnected)
                    {
                        History.Header = "已连接：< " + arg.RemoteEndPoint.ToString() + " >";
                    }
                    else
                    {
                        History.Header = "会话";
                        AutoSendEnabled = false;
                    }
                  
                };

            _tcpClient.Received += 
                (sender, arg) =>
                {
                    History.Append(arg.Message);                  
                };


            IpAddress = "127.0.0.1";
            Port = 4001;
            History.Header = "会话";
            AutoSendIntervalMs = 1000;
        }

        #endregion

        #region private Functions

        private async void Connect()
        {
            if (!ValidateConnect())
                return;

            try
            {
                await _tcpClient.ConnectAsync(IpAddress, Port.Value);
            }
            catch(Exception ex)
            {
                DialogUtils.ShowErrorDialog(ex.Message);
            }        
        }

        private void Disconnect()
        {
            _tcpClient.Disconnect();
        }

        private void UpdateAutoSendTimer()
        {
            if (!AutoSendEnabled ||
                !IsConnected ||
                HasError(nameof(AutoSendIntervalMs)) ||
                !_autoSendIntervalMs.HasValue ||
                string.IsNullOrWhiteSpace(_sendViewModel.Message))
            {
                _autoSendTimer.Stop();
                return;
            }

            _autoSendTimer.Interval = TimeSpan.FromMilliseconds(_autoSendIntervalMs.Value);
            if (!_autoSendTimer.IsEnabled)
            {
                _autoSendTimer.Start();
            }
        }

        private async void OnSend(byte[] data)
        {
            try
            {
                Transmission msg = new Transmission(data, Transmission.EType.Sent);
                History.Append(msg);
                TransmissionResult res = await _tcpClient.SendAsync(msg);
                if (res != null)
                {
                    msg.Origin = res.From;
                    msg.Destination = res.To;
                    //Send.Message = "";
                }        
            }
            catch(Exception ex)
            {
                DialogUtils.ShowErrorDialog(ex.Message);
            }
        }

        private bool ValidateConnect()
        {
            string error = null;
            if (HasError(nameof(IpAddress)))
                error = GetError(nameof(IpAddress));
            else if (HasError(nameof(Port)))
                error = GetError(nameof(Port));

            if(error != null)
            {
                DialogUtils.ShowErrorDialog(error);
                return false;
            }
           
            return true;
        }

        public void Dispose()
        {
            _autoSendTimer?.Stop();
            _tcpClient?.Dispose();
            _historyViewModel?.Dispose();
        }

        #endregion

    }
}
