using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Net;
using System.Windows.Input;
using TcpUdpTool.Model.Parser;
using TcpUdpTool.Model.Util;
using TcpUdpTool.ViewModel.Item;
using TcpUdpTool.ViewModel.Base;

namespace TcpUdpTool.ViewModel
{
    public class SendViewModel : ObservableObject
    {

        #region private members

        private IParser _parser;

        #endregion

        #region public properties

        private string _ipAddress;
        public string IpAddress
        {
            get { return _ipAddress; }
            set
            {
                if (_ipAddress != value)
                {
                    _ipAddress = value;

                    if (String.IsNullOrWhiteSpace(_ipAddress))
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
                if (_port != value)
                {
                    _port = value;

                    if (!NetworkUtils.IsValidPort(_port.HasValue ? _port.Value : -1, false))
                    {
                        AddError(nameof(Port), "端口必须在 1 到 65535 之间。");
                    }
                    else
                    {
                        RemoveError(nameof(Port));
                    }

                    OnPropertyChanged(nameof(Port));
                }
            }
        }

        private string _multicastGroup;
        public string MulticastGroup
        {
            get { return _multicastGroup; }
            set
            {
                if (_multicastGroup != value)
                {
                    _multicastGroup = value;

                    try
                    {
                        var addr = IPAddress.Parse(_multicastGroup);

                        if (!NetworkUtils.IsMulticast(addr))
                        {
                            throw new Exception();
                        }
                        else
                        {
                            RemoveError(nameof(MulticastGroup));
                        }
                    }
                    catch (Exception)
                    {
                        if (String.IsNullOrWhiteSpace(_multicastGroup))
                        {
                            AddError(nameof(MulticastGroup), "组播地址不能为空。");
                        }
                        else
                        {
                            AddError(nameof(MulticastGroup),
                                String.Format("“{0}”不是有效的组播地址。", _multicastGroup));
                        }
                    }

                    OnPropertyChanged(nameof(MulticastGroup));
                }
            }
        }

        private int _multicastTtl;
        public int MulticastTtl
        {
            get { return _multicastTtl; }
            set
            {
                if (_multicastTtl != value)
                {
                    _multicastTtl = value;

                    if (_multicastTtl < 1 || _multicastTtl > 255)
                    {
                        AddError(nameof(MulticastTtl), "TTL 必须在 1 到 255 之间。");
                    }
                    else
                    {
                        RemoveError(nameof(MulticastTtl));
                    }

                    OnPropertyChanged(nameof(MulticastTtl));
                }
            }
        }

        private ObservableCollection<InterfaceAddress> _interfaces;
        public ObservableCollection<InterfaceAddress> Interfaces
        {
            get { return _interfaces; }
            set
            {
                if (_interfaces != value)
                {
                    _interfaces = value;
                    OnPropertyChanged(nameof(Interfaces));
                }
            }
        }

        private InterfaceAddress _selectedInterface;
        public InterfaceAddress SelectedInterface
        {
            get { return _selectedInterface; }
            set
            {
                if (_selectedInterface != value)
                {
                    _selectedInterface = value;
                    OnPropertyChanged(nameof(SelectedInterface));
                }
            }
        }


        private string _message;
        public string Message
        {
            get { return _message; }
            set
            {
                if(_message != value)
                {
                    _message = value;
                    OnPropertyChanged(nameof(Message));
                }
            }
        }

        private bool _plainTextSelected;
        public bool PlainTextSelected
        {
            get { return _plainTextSelected; }
            set
            {
                if (_plainTextSelected != value)
                {
                    if (FileSelected)
                        Message = "";

                    _plainTextSelected = value;
                    OnPropertyChanged(nameof(PlainTextSelected));
                }
            }
        }

        private bool _hexSelected;
        public bool HexSelected
        {
            get { return _hexSelected; }
            set
            {
                if (_hexSelected != value)
                {
                    if (FileSelected)
                        Message = "";

                    _hexSelected = value;
                    OnPropertyChanged(nameof(HexSelected));
                }
            }
        }

        private bool _fileSelected;
        public bool FileSelected
        {
            get { return _fileSelected; }
            set
            {
                if(_fileSelected != value)
                {
                    _fileSelected = value;
                    OnPropertyChanged(nameof(FileSelected));
                }
            }
        }

        #endregion

        #region public events

        public event Action<byte[]> SendData;

        #endregion

        #region public commands

        public ICommand TypeChangedCommand
        {
            get { return new DelegateCommand(TypeChanged); }
        }

        public ICommand BrowseCommand
        {
            get { return new DelegateCommand(Browse); }
        }

        public ICommand SendCommand
        {
            get { return new DelegateCommand(Send); }
        }

        public void SendNow()
        {
            Send();
        }

        #endregion

        #region constructors

        public SendViewModel()
        {
            HexSelected = true;
            Message = "";
            _parser = new HexParser();
        }

        #endregion

        #region private functions

        private void TypeChanged()
        {
            if (PlainTextSelected)
            {
                _parser = new PlainTextParser();
            }
            else if(HexSelected)
            {
                _parser = new HexParser();
            }
        }

        private void Browse()
        {
            var dialog = new OpenFileDialog();
                
            if(dialog.ShowDialog().GetValueOrDefault())
            {
                Message = dialog.FileName;
            }
        }

        private void Send()
        {
            if(FileSelected)
            {
                var filePath = Message;

                if(!File.Exists(filePath))
                {
                    DialogUtils.ShowErrorDialog("文件不存在。");
                    return;
                }

                if(new FileInfo(filePath).Length > 4096)
                {
                    DialogUtils.ShowErrorDialog("文件过大，最大允许 16 KB。");
                    return;
                }

                try
                {
                    byte[] file = File.ReadAllBytes(filePath);
                    SendData?.Invoke(file);
                }
                catch(Exception ex)
                {
                    DialogUtils.ShowErrorDialog("读取文件时出错。 " + ex.Message);
                    return;
                }
            }
            else
            {
                byte[] data = new byte[0];
                try
                {
                    data = _parser.Parse(Message, SettingsUtils.GetEncoding());
                    SendData?.Invoke(data);
                }
                catch (FormatException ex)
                {
                    DialogUtils.ShowErrorDialog(ex.Message);
                    return;
                }
            }
        }

        #endregion

    }
}
