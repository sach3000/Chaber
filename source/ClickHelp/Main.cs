using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
using System.Net.NetworkInformation;
using Microsoft.Win32;
using System.Reflection;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Renci.SshNet;


namespace ClickHelp
{
    public partial class frmMain : Form
    {
        
        public string host; // IP addres SSH server gateway;
        public string outputcmd,PassVNC,logf;
        public string AppData = System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
        public int port; //External port SSH on SSH server
        public uint vncportclt; //Local port for forward port VNC
        public uint nPort = 0; //Port assign client, this listen port VNC
        public int MaxnPort, MinnPort, SizePass;

        public frmMain()
        {
            InitializeComponent();
            try
            {
                foreach (Process proc in Process.GetProcessesByName("WinVNC"))
                {
                    proc.Kill();
                }
                System.Threading.Thread.Sleep(200);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                using (StreamWriter sw = File.AppendText(logf))
                { sw.WriteLine("Error: " + ex.Message); }

            }
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            Random r = new Random();
            var ini_f = new IniFile(@"files\server.ini");
            host = ini_f.Read("SSHServer");
            port = Convert.ToInt32(ini_f.Read("SSHServerPort"));
            vncportclt = Convert.ToUInt32(ini_f.Read("LocalVncPortControl"));
            MinnPort = Convert.ToInt32(ini_f.Read("MinRandomPort"));
            MaxnPort = Convert.ToInt32(ini_f.Read("MaxRandomPort"));
            SizePass = 3;
            logf = AppData + "\\files\\main.log";
            var connectionInfo = new ConnectionInfo(host, port, "vncuser",
                                        new AuthenticationMethod[]{
                                            new PrivateKeyAuthenticationMethod("vncuser",new PrivateKeyFile[]{ 
                                            new PrivateKeyFile(@"files\vncuser.pem","vnc$Uuser")
                                            }),
                                        }
            );

            var ssh = new SshClient(connectionInfo);
            //Port free
            try
            {
                using (var sshcmd = new SshClient(connectionInfo))
                {
                    sshcmd.Connect();
                    var cmd = sshcmd.RunCommand("netstat -tulpan | grep -w 'tcp' | awk '{print $4}' | cut -d ':' -f 2 | uniq");
                    outputcmd = cmd.Result;
                    sshcmd.Disconnect();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
                if (!File.Exists(logf))
                {
                    using (StreamWriter sw = File.CreateText(logf))
                    { sw.WriteLine("Error: " + ex.Message); }
                }
            }
            nPort = (uint)r.Next(MinnPort, MaxnPort);
            PassVNC = PassGenerate(SizePass) + nPort.ToString();
            if (!portav(nPort) && !outputcmd.Contains(nPort.ToString()))
            {
                        //Calculate pass with help math func
                        if (!File.Exists(logf))
                        {
                            using (StreamWriter sw = File.CreateText(logf))
                            { sw.WriteLine("Порт для соединения: " + nPort.ToString()); }
                        }
                        else
                        {
                            try
                            {
                                File.Delete(logf);
                                if (!File.Exists(logf))
                                {
                                    using (StreamWriter sw = File.CreateText(logf))
                                    { sw.WriteLine("Порт для соединения: " + nPort.ToString()); }
                                }
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show("Error: " + ex.Message);
                                if (!File.Exists(logf))
                                {
                                    using (StreamWriter sw = File.CreateText(logf))
                                    { sw.WriteLine("Error: " + ex.Message); }
                                }
                            }
                        }
                        lblPortNotUse.Text = PassVNC;
                        lblPortNotUse.Visible = false;
            }
            //Create tunnel
            try
            {
                    ssh.Connect();
                    //System.Threading.Thread.Sleep(2000);
                    if (ssh.IsConnected)
                    {
                        try
                        {
                            var vnctun = new ForwardedPortRemote(IPAddress.Loopback, nPort,IPAddress.Loopback, vncportclt);
                            ssh.AddForwardedPort(vnctun);
                            vnctun.Start();
                            if (vnctun.IsStarted)
                            {
                                lblMsg.Text = "Соединение установлено";
                                lblPortNotUse.Visible = true;
                                AddVNCReg();
                                ProcessStartInfo startvnc = new ProcessStartInfo();
                                startvnc.FileName = Path.Combine(AppData, "files\\WinVNC.exe");
                                Process proc = Process.Start(startvnc);
                            }
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(ex.Message);
                            using (StreamWriter sw = File.AppendText(logf))
                            { sw.WriteLine("Error: " + ex.Message); }
                        }
                    }
                }
            catch (Exception exConn)
               {
                    MessageBox.Show(exConn.Message);
                    lblMsg.Text = "Невозможно создать туннель";
                    using (StreamWriter sw = File.AppendText(logf))
                    { sw.WriteLine("Connection error: " + exConn.Message); }
               }
        }

        public static string GetIpv4()
        {
            IPAddress[] ipv4Addresses = Array.FindAll(
                        Dns.GetHostEntry(string.Empty).AddressList,
                        a => a.AddressFamily == AddressFamily.InterNetwork);

            return ipv4Addresses[ipv4Addresses.Length-1].ToString();
        }
        private bool portav (uint nPort)
        {
            try
            {
                IPGlobalProperties ipGlProp = IPGlobalProperties.GetIPGlobalProperties();
                TcpConnectionInformation[] tcpConnInfoArray = ipGlProp.GetActiveTcpConnections();
                foreach (TcpConnectionInformation tcpi in tcpConnInfoArray)
                {
                    if (tcpi.LocalEndPoint.Port == nPort)
                    {
                        return true;
                    }
                }
                return false;
            }
            catch (Exception exConn)
            {
                MessageBox.Show(exConn.Message);
                using (StreamWriter sw = File.AppendText(logf))
                { sw.WriteLine("Connection error: " + exConn.Message); }
                return false;
            }
        }

        public static string PassGenerate(int maxSize)
        {
            char[] chars = new char[62];
            chars =
            "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890".ToCharArray();
            byte[] data = new byte[1];
            using (RNGCryptoServiceProvider crypto = new RNGCryptoServiceProvider())
            {
                crypto.GetNonZeroBytes(data);
                data = new byte[maxSize];
                crypto.GetNonZeroBytes(data);
            }
            StringBuilder result = new StringBuilder(maxSize);
            foreach (byte b in data)
            {
                result.Append(chars[b % (chars.Length)]);
            }
            return result.ToString();
        }

        //Encrypt Password for VNC

        public static byte[] EncryptVNC(string password)
        {
            /*
            if (password.Length > 8)
            {
                password = password.Substring(0, 8);
            }
            if (password.Length < 8)
            {
                password = password.PadRight(8, '\0');
            }
            */
            byte[] key = { 23, 82, 107, 6, 35, 78, 88, 7 };
            byte[] passArr = new ASCIIEncoding().GetBytes(password);
            byte[] response = new byte[passArr.Length];
            char[] chars = { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F' };

            // reverse the byte order
            byte[] newkey = new byte[8];
            for (int i = 0; i < 8; i++)
            {
                // revert desKey[i]:
                newkey[i] = (byte)(
                    ((key[i] & 0x01) << 7) |
                    ((key[i] & 0x02) << 5) |
                    ((key[i] & 0x04) << 3) |
                    ((key[i] & 0x08) << 1) |
                    ((key[i] & 0x10) >> 1) |
                    ((key[i] & 0x20) >> 3) |
                    ((key[i] & 0x40) >> 5) |
                    ((key[i] & 0x80) >> 7)
                    );
            }
            key = newkey;
            // reverse the byte order

            DES des = new DESCryptoServiceProvider();
            des.Padding = PaddingMode.None;
            des.Mode = CipherMode.ECB;

            ICryptoTransform enc = des.CreateEncryptor(key, null);
            enc.TransformBlock(passArr, 0, passArr.Length, response, 0);
            /*
            string hexString = String.Empty;
            for (int i = 0; i < response.Length; i++)
            {
                hexString += chars[response[i] >> 4];
                hexString += chars[response[i] & 0xf];
            }
             * */
            //return hexString.Trim().ToLower();
            return response;
        }

        //Decrypt VNC password

        public static string DecryptVNC(string password)
        {
            if (password.Length < 1)
            {
                return string.Empty;
            }

            byte[] key = { 23, 82, 107, 6, 35, 78, 88, 7 };
            byte[] passArr = ToByteArray(password);
            byte[] response = new byte[passArr.Length];

            // reverse the byte order
            byte[] newkey = new byte[8];
            for (int i = 0; i < 8; i++)
            {
                // revert key[i]:
                newkey[i] = (byte)(
                    ((key[i] & 0x01) << 7) |
                    ((key[i] & 0x02) << 5) |
                    ((key[i] & 0x04) << 3) |
                    ((key[i] & 0x08) << 1) |
                    ((key[i] & 0x10) >> 1) |
                    ((key[i] & 0x20) >> 3) |
                    ((key[i] & 0x40) >> 5) |
                    ((key[i] & 0x80) >> 7)
                    );
            }
            key = newkey;
            // reverse the byte order

            DES des = new DESCryptoServiceProvider();
            des.Padding = PaddingMode.None;
            des.Mode = CipherMode.ECB;

            ICryptoTransform dec = des.CreateDecryptor(key, null);
            dec.TransformBlock(passArr, 0, passArr.Length, response, 0);

            return System.Text.ASCIIEncoding.ASCII.GetString(response);
        }

        public static byte[] ToByteArray(String HexString)
        {
            int NumberChars = HexString.Length;
            byte[] bytes = new byte[NumberChars / 2];

            for (int i = 0; i < NumberChars; i += 2)
            {
                bytes[i / 2] = Convert.ToByte(HexString.Substring(i, 2), 16);
            }

            return bytes;
        }

        bool Isx64()
        {
            string cpu = Environment.GetEnvironmentVariable("PROCESSOR_ARCHITECTURE");
            return cpu.Contains("64") ? true : false;
        }

        //for VNC
        private void AddVNCReg()
        {
            try
            {
                using (RegistryKey hcu = Registry.CurrentUser.OpenSubKey(@"Software", true))
                {
                    if (hcu.OpenSubKey("TightVNC") != null) { hcu.DeleteSubKeyTree("TightVNC"); };
                    hcu.CreateSubKey("TightVNC");
                    hcu.CreateSubKey(@"TightVNC\Server");
                }
                using (RegistryKey hcu = Registry.CurrentUser.OpenSubKey(@"Software\TightVNC\Server", true))
                {
                    hcu.SetValue("AcceptHttpConnections", 0, RegistryValueKind.DWord);
                    hcu.SetValue("AcceptRfbConnections", 1, RegistryValueKind.DWord);
                    hcu.SetValue("AllowLoopback", 1, RegistryValueKind.DWord);
                    hcu.SetValue("AlwaysShared", 1, RegistryValueKind.DWord);
                    hcu.SetValue("BlockLocalInput", 0, RegistryValueKind.DWord);
                    hcu.SetValue("BlockRemoteInput", 0, RegistryValueKind.DWord);
                    hcu.SetValue("DisconnectAction", 0, RegistryValueKind.DWord);
                    hcu.SetValue("DisconnectClients", 0, RegistryValueKind.DWord);
                    hcu.SetValue("EnableFileTransfers", 1, RegistryValueKind.DWord);
                    hcu.SetValue("EnableUrlParams", 1, RegistryValueKind.DWord);
                    hcu.SetValue("GrabTransparentWindows", 1, RegistryValueKind.DWord);
                    hcu.SetValue("GrabTransparentWindows", 1, RegistryValueKind.DWord);
                    hcu.SetValue("Password", EncryptVNC(PassVNC), RegistryValueKind.Binary);
                    hcu.SetValue("PasswordViewOnly", EncryptVNC(PassVNC), RegistryValueKind.Binary);
                    hcu.SetValue("QueryAcceptOnTimeout", 0, RegistryValueKind.DWord);
                    hcu.SetValue("QueryTimeout", 0, RegistryValueKind.DWord);
                    hcu.SetValue("RemoveWallpaper", 1, RegistryValueKind.DWord);
                    hcu.SetValue("RfbPort", vncportclt, RegistryValueKind.DWord);
                    hcu.SetValue("RunControlInterface", 1, RegistryValueKind.DWord);
                    hcu.SetValue("UseControlAuthentication", 0, RegistryValueKind.DWord);
                    hcu.SetValue("UseMirrorDriver", 1, RegistryValueKind.DWord);
                    hcu.SetValue("UseVncAuthentication", 1, RegistryValueKind.DWord);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                using (StreamWriter sw = File.AppendText(logf))
                {
                    sw.WriteLine("Error: " + ex.Message);
                    foreach (Process proc in Process.GetProcessesByName("WinVNC"))
                    {
                        proc.Kill();
                    }
                    System.Threading.Thread.Sleep(500);
                    this.Close();
                }

            }
        }

        private void frmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                foreach (Process proc in Process.GetProcessesByName("WinVNC"))
                {
                    proc.Kill();
                }
                System.Threading.Thread.Sleep(2000);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                using (StreamWriter sw = File.AppendText(logf))
                { sw.WriteLine("Error: " + ex.Message); }
                
            }
        }
    }
}
