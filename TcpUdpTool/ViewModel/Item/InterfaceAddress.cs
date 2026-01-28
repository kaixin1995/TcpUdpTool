using System;
using System.Net;
using System.Net.Sockets;
using TcpUdpTool.Model.Util;
using TcpUdpTool.ViewModel.Base;

namespace TcpUdpTool.ViewModel.Item
{
    public class InterfaceAddress : ObservableObject, IComparable
    {
        public enum EInterfaceType { Default, All, Any, Specific }


        private EInterfaceType _type;
        public EInterfaceType Type
        {
            get { return _type; }
            private set { _type = value; }
        }

        private IPAddress _address;
        public IPAddress Address
        {
            get { return _address; }
            private set { _address = value; }
        }

        public string Name
        {
            get { return ToString(); }
        }

        public NetworkInterface Nic { get; private set; }

        public string GroupName
        {
            get { return Nic == null ? "网络接口" : Nic.Name; }
        }


        public InterfaceAddress(EInterfaceType type, NetworkInterface nic, IPAddress address = null)
        {
            Type = type;
            Address = address;
            Nic = nic;

            if(Address == null && (Type == EInterfaceType.Any || Type == EInterfaceType.Specific))
            {
                throw new ArgumentNullException(
                    "类型为 [Specific, Any] 时地址不能为空。");
            }
        }

        public override string ToString()
        {
            if (Type == EInterfaceType.Default)
            {
                return "默认";
            }
            else if (Type == EInterfaceType.All)
            {
                return "全部";
            }
            else if (Type == EInterfaceType.Any)
            {
                if (Address.AddressFamily == AddressFamily.InterNetwork)
                {
                    return "任意 IPv4 (0.0.0.0)";
                }
                else
                {
                    return "任意 IPv6 (::)";
                }
            }
            else
            {
                return Address.ToString();
            }
        }

        public int CompareTo(object other)
        {
            InterfaceAddress o = other as InterfaceAddress;

            if (o == null)
            {
                return 0;
            }
                
            int r = this.Type.CompareTo(o.Type);

            if(r == 0)
            {
                return this.ToString().CompareTo(o.ToString());
            }             
            else
            {
                return r;
            }               
        }
    }
}
