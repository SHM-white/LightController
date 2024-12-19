using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Ports;
using System.Threading;

namespace LightController
{
    internal class SerialHelper
    {
        public event Action<string> InfoLog;
        public event Action<string> WarnLog;
        public event Action<string, System.Exception> ErrorLog;
        public event Action<byte[]> ReceiveHandler;
        public event Action<byte[]> SendHandler;
        private SerialPort _port;
        private bool isBusy = false;
        private readonly AutoResetEvent sendIdealEvent = new AutoResetEvent(true);
        #region Properties
        /// <summary>
        /// 串口名称
        /// </summary>
        public string ComName { get; set; } = "Com1";
        /// <summary>
        /// 波特率
        /// </summary>
        public int BaudRate { get; set; } = 19200;
        /// <summary>
        /// 奇偶校验位
        /// </summary>
        public Parity Parity { get; set; } = Parity.None;
        /// <summary>
        /// 数据位
        /// </summary>
        public int DataBits { get; set; } = 8;
        /// <summary>
        /// 停止位
        /// </summary>
        public StopBits StopBits { get; set; } = StopBits.One;
        /// <summary>
        /// 是否使用RTS
        /// </summary>
        public bool RtsEnable { get; set; } = true;
        /// <summary>
        /// 读取超时
        /// </summary>
        public int ReadTimeOut { get; set; } = 5000;
        /// <summary>
        /// 是否打开串口
        /// </summary>
        public bool IsPortOpen { get { return _port != null && _port.IsOpen; } }

        /// <summary>
        /// 是否记录串口通信日志
        /// </summary>
        public bool IsWriteInfo { get; set; } = true;
        #endregion

        #region Public Functions
        /// <summary>
        /// 串口初始化
        /// </summary>
        public void InitPort(string comName, int baudRate)
        {
            InitPort(comName, baudRate, Parity, DataBits, StopBits, true, 5000);
        }
        public void InitPort(string comName, int baudRate, Parity parity, int dataBits, StopBits stopBits)
        {
            InitPort(comName, baudRate, parity, dataBits, stopBits, RtsEnable, ReadTimeOut);
        }
        public void InitPort(string comName, int baudRate, Parity parity, int dataBits, StopBits stopBits, bool rts, int readTimeOut)
        {
            InfoLog = null;
            ErrorLog = null;
            ComName = comName;
            BaudRate = baudRate;
            Parity = parity;
            DataBits = dataBits;
            StopBits = stopBits;
            RtsEnable = rts;
            ReadTimeOut = readTimeOut;
            if (_port != null && _port.IsOpen) ClosePort();
            _port = new SerialPort(ComName, BaudRate, Parity, dataBits, StopBits) { RtsEnable = this.RtsEnable, ReadTimeout = this.ReadTimeOut };
        }


        /// <summary>
        /// 打开串口
        /// </summary>
        public void OpenPort()
        {
            try
            {
                lock (this)
                {
                    _port.Open();
                    InfoLog?.Invoke($"串口打开{(IsPortOpen ? "成功" : "失败")},Com={ComName},BaudRate={BaudRate},Parity={Parity},DataBits={DataBits},StopBits={StopBits}");
                }
            }
            catch (System.Exception ex)
            {
                ErrorLog?.Invoke("串口打开异常,原因:" + ex.Message, ex);
            }
        }

        /// <summary>
        /// 关闭串口
        /// </summary>
        public void ClosePort()
        {
            try
            {
                lock (this)
                {
                    _port?.Close();
                    InfoLog?.Invoke("串口关闭成功");
                }
            }
            catch (System.Exception ex)
            {
                ErrorLog?.Invoke("串口关闭异常,原因:" + ex.Message, ex);
            }
        }

        /// <summary>
        /// 注销
        /// </summary>
        public void Dispose()
        {
            ClosePort();
            InfoLog = null;
            WarnLog = null;
            ErrorLog = null;
            ReceiveHandler = null;
            SendHandler = null;
            _port = null;
        }

        /// <summary>
        /// 发送串口消息
        /// </summary>
        /// <param name="data"></param>
        /// <param name="recLength"></param>
        /// <returns></returns>
        public byte[] Send(byte[] data, int recLength)
        {
            if (!IsPortOpen)
            {
                ErrorLog?.Invoke("串口无实例或未打开", new InvalidOperationException());
                return null;
            }
            return DoSendOrRead(false, data, recLength, 0, 0);
        }

        public void Write(string data)
        {
            if (!IsPortOpen)
            {
                ErrorLog?.Invoke("串口无实例或未打开", new InvalidOperationException());
                return;
            }
            if (isBusy) sendIdealEvent.WaitOne();
            PriWrite(data);
        }

        object obj = new object();
        /// <summary>
        /// 主动读取串口消息
        /// </summary>
        /// <param name="lenth"></param>
        /// <param name="waitTime"></param>
        /// <returns></returns>
        public byte[] Read(int lenth, int waitTime = 5000)
        {
            lock (obj)
            {
                if (!IsPortOpen)
                {
                    ErrorLog?.Invoke("串口无实例或未打开", new InvalidOperationException());
                    return null;
                }
                return DoSendOrRead(true, null, 0, lenth, waitTime);
            }
        }
        #endregion

        #region Private Functions

        private byte[] DoSendOrRead(bool isRead, byte[] data, int reclenth, int lenth, int waitTime)
        {
            if (isBusy) sendIdealEvent.WaitOne();
            if (isRead)
                return PriRead(lenth, waitTime);
            else
                return PriSend(data, reclenth);
        }

        private byte[] PriSend(byte[] data, int recLength)
        {
            try
            {
                isBusy = true;
                sendIdealEvent.Reset();
                SendHandler?.Invoke(data);
                if (IsWriteInfo) InfoSend(data);
                _port.ReadTimeout = this.ReadTimeOut;
                byte[] buffer = new byte[recLength];
                lock (_port)
                {
                    _port.DiscardInBuffer();
                    _port.DiscardOutBuffer();
                    _port.Write(data, 0, data.Length);
                    if (recLength == 0)
                    {
                        Thread.Sleep(2);
                        isBusy = false;
                        return null;
                    }
                    int count = 0;
                    do
                    {
                        Thread.Sleep(5);
                        count += _port.Read(buffer, count, recLength - count);
                    } while (count < recLength);
                }
                ReceiveHandler?.Invoke(buffer);
                if (IsWriteInfo) InfoReceive(buffer);
                isBusy = false;
                sendIdealEvent.Set();
                return buffer;
            }
            catch (System.Exception ex)
            {
                ErrorLog?.Invoke("ComSendError: " + ex.Message, ex);
                isBusy = false;
                sendIdealEvent.Set();
                return null;
            }
        }

        private byte[] PriRead(int lenth, int waitTime)
        {
            try
            {
                isBusy = true;
                sendIdealEvent.Reset();
                byte[] buffer = new byte[lenth];
                lock (_port)
                {
                    if (!IsPortOpen) ErrorLog?.Invoke("串口无实例或未打开", new InvalidOperationException());
                    _port.ReadTimeout = Math.Max(2000, waitTime);
                    int count = 0;

                    do
                    {
                        Thread.Sleep(5);
                        count += _port.Read(buffer, count, lenth - count);
                    } while (count < lenth);
                }
                ReceiveHandler?.Invoke(buffer);
                InfoReceive(buffer);
                isBusy = false;
                sendIdealEvent.Set();
                return buffer;
            }
            catch (System.Exception ex)
            {
                ErrorLog?.Invoke($"ComReadError: Len={lenth}, waitTime={waitTime}, Err=" + ex.Message, ex);
                //读取超时
                isBusy = false;
                sendIdealEvent.Set();
                return null;
            }
        }

        private void PriWrite(string data)
        {
            try
            {
                isBusy = true;
                sendIdealEvent.Reset();
                InfoLog?.Invoke(data);
                if (!IsPortOpen) ErrorLog?.Invoke("串口无实例或未打开", new InvalidOperationException());
                lock (_port)
                {
                    _port.DiscardInBuffer();
                    _port.DiscardOutBuffer();
                    _port.Write(data);
                    Thread.Sleep(20);
                }
                isBusy = false;
                sendIdealEvent.Set();
            }
            catch (System.Exception ex)
            {
                ErrorLog?.Invoke("ComWriteError: " + ex.Message, ex);
                isBusy = false;
                sendIdealEvent.Set();
                return;
            }
        }

        /// <summary>
        /// 记录发出日志
        /// </summary>
        /// <param name="data"></param>
        private void InfoSend(byte[] data)
        {
            string send = "SendMsg:";
            for (int i = 0; i < data.Length; i++)
            {
                send += data[i].ToString("X2") + "-";
            }
            InfoLog?.Invoke(send);
        }
        /// <summary>
        /// 记录接收日志
        /// </summary>
        /// <param name="buffer"></param>
        private void InfoReceive(byte[] buffer)
        {
            string recv = "RecvMsg:";
            for (int i = 0; i < buffer.Length; i++)
            {
                recv += buffer[i].ToString("X2") + "-";
            }
            InfoLog?.Invoke(recv);
        }
        #endregion

        #region Static Functions
        public static string[] GetAllPortNames()
        {
            return SerialPort.GetPortNames();
        }
        #endregion
    }
}
