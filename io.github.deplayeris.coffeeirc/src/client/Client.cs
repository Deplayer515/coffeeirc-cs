/*MIT License

Copyright (c) 2026 Deplayer

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/

using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace io.github.deplayeris.coffeeirc.client;

/// <summary>
/// CIC 客户端核心
/// </summary>
public class Client
{
    private static readonly ILogger cml = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<Client>();

    private string distributionName;
    private string ip;
    private int port;
    private int ipProtocol;
    private string nickname;
    private string username;
    private string servername;
    private string serverdescription;
    private string clientId;
    private ShowManager showManager;
    private string sIFP = ".show";
    private bool isConnected = false;

    private StreamWriter? chatLogWriter;
    private string? currentChatLogDate;
    private string chatLogFormat = "yyyy-MM-dd HH:mm:ss";

    private HttpListener? pushServer;
    private int pushPort = 10026;

    private RSA? rsaKeyPair;
    private byte[]? aesKey;
    private byte[]? serverPublicKey;
    private bool encryptionEnabled = false;
    private string? customKey = null;

    private HttpClient? clientHttp;
    
    /// <summary>
    /// 客户端构造函数
    /// </summary>
    public Client(int ipProtocol, string ip, int port, string nickname, string username, string distributionName, string customKey = "", string sIFP = ".show")
    {
        cml.LogInformation("---------------------------------------------------------------------------------");
        cml.LogInformation("[核心信息] 正在使用的 CoffeeIRC 核心的软件信息:");
        cml.LogInformation("        版本号：" + SwInfoc.Version);
        cml.LogInformation("        开发状态：" + SwInfoc.SoftwareStatus);
        cml.LogInformation("        版本代号：" + SwInfoc.VerCodename);
        cml.LogInformation("        支持协议：" + SwInfoc.Connection);
        cml.LogInformation("");
        cml.LogInformation("当前运行本核心的发行版：" + distributionName);
        cml.LogInformation("");
        cml.LogInformation("如果遇到核心问题，请提交至：https://github.com/deplayeris/coffeeirc/issues");
        cml.LogInformation("如在使用基于本核心的发行版 (如无忧聊) 时出现问题");
        cml.LogInformation("请先检查是否为核心故障 (通过查看核心日志)，若非核心问题请联系发行版作者");
        cml.LogInformation("");
        cml.LogInformation("核心问题提交步骤:");
        cml.LogInformation("1. 在 GitHub 上创建新的 Issue");
        cml.LogInformation("2. 详细准确地描述遇到的问题");
        cml.LogInformation("3. 附上出现问题时的核心日志文件");
        cml.LogInformation("---------------------------------------------------------------------------------");

        this.ipProtocol = ipProtocol;
        this.ip = ip;
        this.port = port;
        this.nickname = nickname;
        this.username = username;
        this.customKey = customKey;
        this.distributionName = distributionName;
        this.sIFP = sIFP;

        cml.LogInformation("[客户端初始化] 开始创建客户端实例");
        cml.LogInformation("[配置详情] IP 协议版本：IPv" + ipProtocol);
        cml.LogInformation("[配置详情] 服务器地址：" + ip + ":" + port);
        cml.LogInformation("[用户信息] 用户昵称：" + nickname);
        cml.LogInformation("[用户信息] 用户名：" + username);
        cml.LogInformation("[实例创建] 客户端实例已成功创建并配置完成");

        InitializeChatLog();
        StartPushService();
        InitializeEncryption(customKey);

        clientHttp = new HttpClient();
    }
    
    

    /// <summary>
    /// 记录聊天日志所必须要使用的一个 Method
    /// </summary>
    private void InitializeChatLog()

    {
        try
        {
            currentChatLogDate = DateTime.Now.ToString("yyyy-MM-dd");
            string logFileName = "./ciclogs/chatlog-c-" + currentChatLogDate + ".log";
            
            Directory.CreateDirectory("./ciclogs");
            
            FileStream fileStream = new FileStream(logFileName, FileMode.Append, FileAccess.Write);
            chatLogWriter = new StreamWriter(fileStream, Encoding.UTF8);
            chatLogWriter.AutoFlush = true;

            cml.LogInformation("[聊天日志] 聊天日志系统已初始化，日志文件：" + logFileName);
        }
        catch (IOException e)
        {
            cml.LogError("[聊天日志错误] 初始化聊天日志失败：" + e.Message);
        }
    }

    /// <summary>
    /// 记录聊天消息
    /// </summary>
    /// <param name="username">用户名</param>
    /// <param name="message">消息</param>
    private void LogChatMessage(string username, string message)
    {
        try
        {
            string today = DateTime.Now.ToString("yyyy-MM-dd");
            if (!today.Equals(currentChatLogDate))
            {
                CloseChatLog();
                InitializeChatLog();
            }

            if (chatLogWriter != null)
            {
                string timestamp = DateTime.Now.ToString(chatLogFormat);
                string logEntry = timestamp + " [ " + username + " ] " + message;
                chatLogWriter.WriteLine(logEntry);
            }
        }
        catch (Exception e)
        {
            cml.LogError("[聊天日志错误] 记录聊天消息失败：" + e.Message);
        }
    }

    /// <summary>
    /// 初始化加密系统
    /// </summary>
    private void InitializeEncryption(string customKey)
    {
        try
        {
            cml.LogInformation("[加密初始化] 开始初始化加密通讯系统...");

            if (customKey != null && !string.IsNullOrEmpty(customKey))
            {
                // 使用自定义密钥种子
                byte[] seedBytes = Encoding.UTF8.GetBytes(customKey);
                using var rng = RandomNumberGenerator.Create();
                rng.GetBytes(seedBytes);
                rsaKeyPair = RSA.Create(2048);
                cml.LogInformation("[加密初始化] 使用自定义密钥种子初始化加密系统");
            }
            else
            {
                rsaKeyPair = RSA.Create(2048);
                cml.LogInformation("[加密初始化] 使用随机密钥初始化加密系统");
            }

            cml.LogInformation("[加密初始化] RSA 密钥对生成成功");
        }
        catch (Exception e)
        {
            cml.LogError("[加密错误] 初始化加密系统失败：" + e.Message);
        }
    }

    /// <summary>
    /// AES 加密消息
    /// </summary>
    private string EncryptMessage(string message)
    {
        try
        {
            if (!encryptionEnabled || aesKey == null)
            {
                return message;
            }

            using var aes = Aes.Create();
            aes.Key = aesKey;
            aes.Mode = CipherMode.ECB;
            aes.Padding = PaddingMode.PKCS7;

            ICryptoTransform encryptor = aes.CreateEncryptor();
            byte[] messageBytes = Encoding.UTF8.GetBytes(message);
            byte[] encryptedBytes = encryptor.TransformFinalBlock(messageBytes, 0, messageBytes.Length);
            return Convert.ToBase64String(encryptedBytes);
        }
        catch (Exception e)
        {
            cml.LogError("[加密错误] 消息加密失败：" + e.Message);
            return message;
        }
    }

    /// <summary>
    /// 解密 AES 密钥
    /// </summary>
    private void DecryptAesKey(string encryptedAesKeyStr)
    {
        try
        {
            byte[] encryptedKeyBytes = Convert.FromBase64String(encryptedAesKeyStr);
            byte[] decryptedKeyBytes = rsaKeyPair.Decrypt(encryptedKeyBytes, RSAEncryptionPadding.Pkcs1);

            aesKey = decryptedKeyBytes;
            cml.LogInformation("[密钥交换] AES 密钥解密成功");
        }
        catch (Exception e)
        {
            cml.LogError("[密钥交换错误] AES 密钥解密失败：" + e.Message);
        }
    }

    /// <summary>
    /// AES 解密消息
    /// </summary>
    private string DecryptMessage(string encryptedMessage)
    {
        try
        {
            if (!encryptionEnabled || aesKey == null)
            {
                return encryptedMessage;
            }

            using var aes = Aes.Create();
            aes.Key = aesKey;
            aes.Mode = CipherMode.ECB;
            aes.Padding = PaddingMode.PKCS7;

            ICryptoTransform decryptor = aes.CreateDecryptor();
            byte[] decodedBytes = Convert.FromBase64String(encryptedMessage);
            byte[] decryptedBytes = decryptor.TransformFinalBlock(decodedBytes, 0, decodedBytes.Length);
            return Encoding.UTF8.GetString(decryptedBytes);
        }
        catch (Exception e)
        {
            cml.LogError("[解密错误] 消息解密失败：" + e.Message);
            return encryptedMessage;
        }
    }

    /// <summary>
    /// 在使用完毕之后，必须关闭聊天日志记录
    /// </summary>
    private void CloseChatLog()
    {
        try
        {
            if (chatLogWriter != null)
            {
                chatLogWriter.Flush();
                chatLogWriter.Close();
                chatLogWriter.Dispose();
                cml.LogInformation("[聊天日志] 聊天日志已关闭");
            }
        }
        catch (Exception e)
        {
            cml.LogError("[聊天日志错误] 关闭聊天日志失败：" + e.Message);
        }
    }

    /// <summary>
    /// 启动推送服务
    /// </summary>
    private void StartPushService()
    {
        try
        {
            pushServer = new HttpListener();
            pushServer.Prefixes.Add($"http://+:{pushPort}/");
            pushServer.Start();
            cml.LogInformation("[推送服务] 推送服务已启动，监听端口：" + pushPort);

            Task.Run(() =>
            {
                while (pushServer.IsListening)
                {
                    try
                    {
                        HttpListenerContext context = pushServer.GetContext();
                        HandlePushRequest(context);
                    }
                    catch (Exception e)
                    {
                        cml.LogError("[推送服务错误] 处理推送请求失败：" + e.Message);
                    }
                }
            });
        }
        catch (Exception e)
        {
            cml.LogError("[推送服务错误] 启动推送服务失败：" + e.Message);
        }
    }

    /// <summary>
    /// 处理推送请求
    /// </summary>
    private void HandlePushRequest(HttpListenerContext context)
    {
        HttpListenerRequest request = context.Request;
        HttpListenerResponse response = context.Response;

        string message = request.QueryString["message"];
        if (!string.IsNullOrEmpty(message))
        {
            string decryptedMessage = DecryptMessage(message);
            string[] parts = decryptedMessage.Split('|');
            if (parts.Length >= 2)
            {
                string sender = parts[0];
                string msgContent = parts[1];
                LogChatMessage(sender, msgContent);
                cml.LogInformation("[收到消息] " + sender + ": " + msgContent);
                
            }
        }

        string responseString = "OK";
        byte[] buffer = Encoding.UTF8.GetBytes(responseString);
        response.ContentLength64 = buffer.Length;
        using var output = response.OutputStream;
        output.Write(buffer, 0, buffer.Length);
    }

    /// <summary>
    /// 连接到服务器
    /// </summary>
    public async Task ConnectAsync()
    {
        try
        {
            string url = $"http://{ip}:{port}/connect";
            string requestData = $"nickname={nickname}&username={username}";

            var content = new StringContent(requestData, Encoding.UTF8, "application/x-www-form-urlencoded");
            HttpResponseMessage response = await clientHttp.PostAsync(url, content);
            response.EnsureSuccessStatusCode();

            string responseBody = await response.Content.ReadAsStringAsync();
            cml.LogInformation("[连接服务器] 成功连接到服务器：" + ip + ":" + port);

            isConnected = true;
        }
        catch (Exception e)
        {
            cml.LogError("[连接错误] 连接服务器失败：" + e.Message);
            isConnected = false;
        }
    }

    /// <summary>
    /// 发送消息
    /// </summary>
    public async Task SendMessageAsync(string message)
    {
        if (!isConnected)
        {
            cml.LogError("[发送错误] 未连接到服务器，无法发送消息");
            return;
        }

        try
        {
            string encryptedMessage = EncryptMessage(message);
            string url = $"http://{ip}:{port}/send";
            string requestData = $"username={nickname}&message={encryptedMessage}";

            var content = new StringContent(requestData, Encoding.UTF8, "application/x-www-form-urlencoded");
            HttpResponseMessage response = await clientHttp.PostAsync(url, content);
            response.EnsureSuccessStatusCode();

            LogChatMessage(nickname, message);
            cml.LogInformation("[发送消息] " + nickname + ": " + message);
        }
        catch (Exception e)
        {
            cml.LogError("[发送错误] 发送消息失败：" + e.Message);
        }
    }

    /// <summary>
    /// 断开连接
    /// </summary>
    public async Task DisconnectAsync()
    {
        try
        {
            if (isConnected)
            {
                string url = $"http://{ip}:{port}/disconnect";
                string requestData = $"username={nickname}";

                var content = new StringContent(requestData, Encoding.UTF8, "application/x-www-form-urlencoded");
                HttpResponseMessage response = await clientHttp.PostAsync(url, content);
                response.EnsureSuccessStatusCode();

                cml.LogInformation("[断开连接] 已从服务器断开连接");
                isConnected = false;
            }
        }
        catch (Exception e)
        {
            cml.LogError("[断开连接错误] 断开连接失败：" + e.Message);
        }
    }

    /// <summary>
    /// 关闭客户端
    /// </summary>
    public void Close()
    {
        try
        {
            if (pushServer != null && pushServer.IsListening)
            {
                pushServer.Stop();
                pushServer.Close();
                cml.LogInformation("[推送服务] 推送服务已关闭");
            }

            if (clientHttp != null)
            {
                clientHttp.Dispose();
            }

            CloseChatLog();
            cml.LogInformation("[客户端] 客户端已关闭");
        }
        catch (Exception e)
        {
            cml.LogError("[关闭错误] 关闭客户端失败：" + e.Message);
        }
    }

    public bool IsConnected()
    {
        return isConnected;
    }

    public string GetNickname()
    {
        return nickname;
    }

    public string GetUsername()
    {
        return username;
    }
    
    /// <summary>
    /// 信息呈现管理器的工具方法，用于向外界呈现信息，ShowManager为其提供支持
    /// </summary>
    /// <param name="showItem">呈现何种内容</param>
    /// <param name="message">内容的具体详情</param>
    private async Task ShowAsync(ShowManager.ShowItem showItem, string message)
    { 
        
        
        
    }
    
    
    /// <summary>
    /// 信息呈现管理器，用于向外界呈现信息的相关管理事宜
    /// </summary>
    private class ShowManager
    {
        /// <summary>
        /// ShowInterfaceFilePath
        /// </summary>
        private string sIFP;
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="showInterfaceFilePath">传给内部成员"sIFP"，用于确定呈现器接口文件路径</param>
        public ShowManager(string showInterfaceFilePath)
        {
            sIFP = showInterfaceFilePath;

            File.Create(sIFP);
        }
        
        /// <summary>
        /// 呈现信息种类枚举
        /// </summary>
        public enum ShowItem
        {
            /// <summary>
            /// 显示消息
            /// </summary>
            ShowMessage = 0,
            /// <summary>
            /// 显示提示
            /// </summary>
            ShowTip = 1,
            /// <summary>
            /// 显示错误
            /// </summary>
            ShowError = 2,
            /// <summary>
            /// 显示警告
            /// </summary>
            ShowWarning = 3,
            /// <summary>
            /// 显示信息
            /// </summary>
            ShowInfo = 4,
            /// <summary>
            /// 显示调试信息
            /// </summary>
            ShowDebug = 5,
            /// <summary>
            /// 显示用户发送聊天信息
            /// </summary>
            ShowChatmsg = 6
        }

        public void ShowThis(ShowItem showItem, string message)
        {
            switch (showItem)
            {
                case ShowItem.ShowMessage:
                    File.WriteAllText(sIFP, "[MSG]"+message);
                    break;
                case ShowItem.ShowTip:
                    File.WriteAllText(sIFP, "[TIP]"+message);
                    break;
                case ShowItem.ShowError:
                    File.WriteAllText(sIFP, "[ERR]"+message);
                    break;
                case ShowItem.ShowWarning:
                    File.WriteAllText(sIFP, "[WAN]"+message);
                    break;
                case ShowItem.ShowInfo:
                    File.WriteAllText(sIFP, "[INF]"+message);
                    break;
                case ShowItem.ShowDebug:
                    File.WriteAllText(sIFP, "[DBG]"+message);
                    break;
                case ShowItem.ShowChatmsg:
                    File.WriteAllText(sIFP, "[CHT]"+message);
                    break;
            }
        }
    }
}
