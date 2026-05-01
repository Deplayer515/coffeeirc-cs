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

/*
 * 
 * 此部分为Client主要运行部分
 * 
 */


using System.Net;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;

namespace io.github.deplayeris.coffeeirc.client;

/// <summary>
/// CIC 客户端核心
/// </summary>
public partial class Client
{
    private static ILogger? cml = null;
    private static StreamWriter? coreLogWriter = null;
    private static string? currentCoreLogDate = null;
    private static int coreLogSequence = 0;
    
    /// <summary>
    /// 安全记录日志（自动处理 null 检查）
    /// </summary>
    private static void LogInfo(string message)
    {
        cml?.LogInformation(message);
    }
    
    private static void LogError(string message)
    {
        cml?.LogError(message);
    }
    
    private static void LogWarning(string message)
    {
        cml?.LogWarning(message);
    }

    private string? distributionName;
    private string? ip;
    private int port;
    private int ipProtocol;
    private string? nickname;
    private string? username;
    private ShowManager showManager;
    private string sIFP = ".show";
    private bool isConnected = false;

    private StreamWriter? chatLogWriter;
    private string? currentChatLogDate;
    private string chatLogFormat = "yyyy-MM-dd HH:mm:ss";

    private HttpListener? pushServer;
    private int pushPort = 10027;

    private RSA? rsaKeyPair;
    private byte[]? aesKey;
    private bool encryptionEnabled = false;
    private string? customKey = null;

    private HttpClient? clientHttp;
    private CancellationTokenSource? cancellationTokenSource;
    private Task? pushServiceTask;
    
    /// <summary>
    /// 初始化核心日志系统
    /// </summary>
    private static void InitializeCoreLogger()
    {
        try
        {
            string today = DateTime.Now.ToString("yyyy-MM-dd");
            
            if (currentCoreLogDate != today)
            {
                currentCoreLogDate = today;
                coreLogSequence = 0;
            }
            
            string logDirectory = "./ciclogs";
            Directory.CreateDirectory(logDirectory);
            
            string logFileName;
            do
            {
                logFileName = Path.Combine(logDirectory, $"ciccore-{today}-{coreLogSequence}.log");
                coreLogSequence++;
            } while (File.Exists(logFileName) && coreLogSequence < 100);
            
            FileStream fileStream = new FileStream(logFileName, FileMode.Append, FileAccess.Write);
            coreLogWriter = new StreamWriter(fileStream, Encoding.UTF8);
            coreLogWriter.AutoFlush = true;
            
            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddProvider(new FileLoggerProvider(coreLogWriter));
                builder.SetMinimumLevel(LogLevel.Information);
            });
            
            cml = loggerFactory.CreateLogger<Client>();
        }
        catch (Exception e)
        {
            Console.WriteLine($"[严重错误] 初始化核心日志失败: {e.Message}");
        }
    }
    
    /// <summary>
    /// 文件日志提供者
    /// </summary>
    private class FileLoggerProvider : ILoggerProvider
    {
        private readonly StreamWriter _writer;
        
        public FileLoggerProvider(StreamWriter writer)
        {
            _writer = writer;
        }
        
        public ILogger CreateLogger(string categoryName)
        {
            return new FileLogger(_writer, categoryName);
        }
        
        public void Dispose()
        {
            _writer?.Flush();
        }
    }
    
    /// <summary>
    /// 文件日志记录器
    /// </summary>
    private class FileLogger : ILogger
    {
        private readonly StreamWriter _writer;
        private readonly string _categoryName;
        
        public FileLogger(StreamWriter writer, string categoryName)
        {
            _writer = writer;
            _categoryName = categoryName;
        }
        
        public IDisposable BeginScope<TState>(TState state) where TState : notnull
        {
            return NullDisposable.Instance;
        }
        
        public bool IsEnabled(LogLevel logLevel)
        {
            return logLevel >= LogLevel.Information;
        }
        
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
                return;
            
            string message = formatter(state, exception);
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            string level = logLevel switch
            {
                LogLevel.Trace => "TRACE",
                LogLevel.Debug => "DEBUG",
                LogLevel.Information => "INFO",
                LogLevel.Warning => "WARN",
                LogLevel.Error => "ERROR",
                LogLevel.Critical => "FATAL",
                _ => "INFO"
            };
            
            string logEntry = $"[{timestamp}] [{level}] [{_categoryName}] {message}";
            
            if (exception != null)
            {
                logEntry += $"\nException: {exception}\n{exception.StackTrace}";
            }
            
            lock (_writer)
            {
                _writer.WriteLine(logEntry);
            }
        }
        
        private class NullDisposable : IDisposable
        {
            public static readonly NullDisposable Instance = new();
            public void Dispose() { }
        }
    }
    
    /// <summary>
    /// 客户端构造函数，使用此即代表创建客户端实例并且开启客户端
    /// </summary>
    public Client(int ipProtocol, string ip, int port, string nickname, string username, string distributionName, string customKey = "", string sIFP = ".show")
    {
        this.ipProtocol = ipProtocol;
        this.ip = ip;
        this.port = port;
        this.nickname = nickname;
        this.username = username;
        this.customKey = customKey;
        this.distributionName = distributionName;
        this.sIFP = sIFP;
        this.showManager = new ShowManager(this.sIFP);

        InitializeCoreLogger();
        
        if (cml != null)
        {
            LogInfo("[客户端初始化] 开始创建客户端实例");
            LogInfo("[实例创建] 客户端实例已成功创建并配置完成");
        }

        clientHttp = new HttpClient();
    }

    public void StartClient()
    {
        LogInfo("---------------------------------------------------------------------------------");
        LogInfo("[核心信息] 正在使用的 CoffeeIRC 核心的软件信息:");
        LogInfo("        版本号：" + SwInfoc.Version);
        LogInfo("        开发状态：" + SwInfoc.SoftwareStatus);
        LogInfo("        版本代号：" + SwInfoc.VerCodename);
        LogInfo("        支持协议：" + SwInfoc.Connection);
        LogInfo("");
        LogInfo("当前运行本核心的发行版：" + distributionName);
        LogInfo("");
        LogInfo("如果遇到核心问题，请提交至：https://github.com/deplayeris/coffeeirc/issues");
        LogInfo("如在使用基于本核心的发行版 (如无忧聊) 时出现问题");
        LogInfo("请先检查是否为核心故障 (通过查看核心日志)，若非核心问题请联系发行版作者");
        LogInfo("");
        LogInfo("核心问题提交步骤:");
        LogInfo("1. 在 GitHub 上创建新的 Issue");
        LogInfo("2. 详细准确地描述遇到的问题");
        LogInfo("3. 附上出现问题时的核心日志文件");
        LogInfo("---------------------------------------------------------------------------------");
        LogInfo("[配置详情] IP 协议版本：IPv" + ipProtocol);
        LogInfo("[配置详情] 服务器地址：" + ip + ":" + port);
        LogInfo("[用户信息] 用户昵称：" + nickname);
        LogInfo("[用户信息] 用户名：" + username);
        LogInfo("[实例创建] 客户端实例已成功创建并配置完成");
        
        _ = ShowAsync(ShowManager.ShowItem.ShowInfo, "客户端已就绪");
        
        InitializeChatLog();
        StartPushService();
        InitializeEncryption(customKey ?? "");
        
        _ = ConnectAsync();
    }
    
    /// <summary>
    /// 连接到服务器
    /// </summary>
    private async Task ConnectAsync()
    {
        try
        {
            if (ip == null || nickname == null)
            {
                LogError("[连接错误] IP 或昵称为空，无法连接");
                return;
            }
            
            LogInfo("[连接] 正在连接到服务器...");
            string url = $"http://{ip}:{port}/connect";
            string requestData = $"username={nickname}&protocol={SwInfoc.Connection}";
            
            var content = new StringContent(requestData, Encoding.UTF8, "application/x-www-form-urlencoded");
            if (clientHttp != null)
            {
                HttpResponseMessage response = await clientHttp.PostAsync(url, content);
                if (response.IsSuccessStatusCode)
                {
                    isConnected = true;
                    LogInfo("[连接] 成功连接到服务器");
                    _ = ShowAsync(ShowManager.ShowItem.ShowInfo, "客户端已连接到服务器");
                    
                    _ = KeepAliveAsync();
                }
                else
                {
                    LogError($"[连接错误] 连接失败，状态码: {response.StatusCode}");
                    
                }
            }
        }
        catch (Exception e)
        {
            LogError("[连接错误] 连接服务器失败：" + e.Message);
            _ = ShowAsync(ShowManager.ShowItem.ShowError, "无法连接到服务器");
        }
    }
    
    /// <summary>
    /// 心跳保活机制
    /// </summary>
    private async Task KeepAliveAsync()
    {
        if (cancellationTokenSource == null)
        {
            cancellationTokenSource = new CancellationTokenSource();
        }
        
        while (!cancellationTokenSource.Token.IsCancellationRequested && isConnected)
        {
            try
            {
                await Task.Delay(30000, cancellationTokenSource.Token);
                
                if (isConnected && ip != null)
                {
                    string url = $"http://{ip}:{port}/heartbeat";
                    if (clientHttp != null)
                    {
                        _ = clientHttp.GetAsync(url);
                    }
                }
            }
            catch (TaskCanceledException)
            {
                break;
            }
            catch (Exception e)
            {
                LogWarning("[心跳] 心跳检测异常：" + e.Message);
            }
        }
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

            LogInfo("[聊天日志] 聊天日志系统已初始化，日志文件：" + logFileName);
        }
        catch (IOException e)
        {
            LogError("[聊天日志错误] 初始化聊天日志失败：" + e.Message);
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
            LogError("[聊天日志错误] 记录聊天消息失败：" + e.Message);
        }
    }

    /// <summary>
    /// 初始化加密系统
    /// </summary>
    private void InitializeEncryption(string? customKey)
    {
        try
        {
            LogInfo("[加密初始化] 开始初始化加密通讯系统...");

            if (customKey != null && !string.IsNullOrEmpty(customKey))
            {
                byte[] seedBytes = Encoding.UTF8.GetBytes(customKey);
                using var rng = RandomNumberGenerator.Create();
                rng.GetBytes(seedBytes);
                rsaKeyPair = RSA.Create(2048);
                LogInfo("[加密初始化] 使用自定义密钥种子初始化加密系统");
            }
            else
            {
                rsaKeyPair = RSA.Create(2048);
                LogInfo("[加密初始化] 使用随机密钥初始化加密系统");
            }

            LogInfo("[加密初始化] RSA 密钥对生成成功");
        }
        catch (Exception e)
        {
            LogError("[加密错误] 初始化加密系统失败：" + e.Message);
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
            LogError("[加密错误] 消息加密失败：" + e.Message);
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
            if (rsaKeyPair != null)
            {
                byte[] decryptedKeyBytes = rsaKeyPair.Decrypt(encryptedKeyBytes, RSAEncryptionPadding.Pkcs1);
                aesKey = decryptedKeyBytes;
                LogInfo("[密钥交换] AES 密钥解密成功");
            }
            else
            {
                LogError("[密钥交换错误] RSA 密钥对未初始化");
            }
        }
        catch (Exception e)
        {
            LogError("[密钥交换错误] AES 密钥解密失败：" + e.Message);
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
            LogError("[解密错误] 消息解密失败：" + e.Message);
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
                LogInfo("[聊天日志] 聊天日志已关闭");
            }
        }
        catch (Exception e)
        {
            LogError("[聊天日志错误] 关闭聊天日志失败：" + e.Message);
        }
    }

    /// <summary>
    /// 启动推送服务
    /// </summary>
    private void StartPushService()
    {
        try
        {
            bool started = false;
            for (int port = pushPort; port < pushPort + 10; port++)
            {
                try
                {
                    pushServer = new HttpListener();
                    pushServer.Prefixes.Add($"http://+:{port}/");
                    pushServer.Start();
                    pushPort = port;
                    started = true;
                    LogInfo("[推送服务] 推送服务已启动，监听端口：" + port);
                    break;
                }
                catch (HttpListenerException)
                {
                    pushServer?.Close();
                }
            }
            
            if (!started)
            {
                LogError("[推送服务错误] 无法启动推送服务，端口 " + pushPort + "-" + (pushPort + 9) + " 均被占用");
                return;
            }

            cancellationTokenSource = new CancellationTokenSource();
            pushServiceTask = Task.Run(() =>
            {
                while (!cancellationTokenSource.Token.IsCancellationRequested && pushServer != null && pushServer.IsListening)
                {
                    try
                    {
                        HttpListenerContext context = pushServer.GetContext();
                        HandlePushRequest(context);
                    }
                    catch (ObjectDisposedException)
                    {
                        break;
                    }
                    catch (Exception e)
                    {
                        if (!cancellationTokenSource.Token.IsCancellationRequested)
                        {
                            LogError("[推送服务错误] 处理推送请求失败：" + e.Message);
                        }
                    }
                }
            }, cancellationTokenSource.Token);
        }
        catch (Exception e)
        {
            LogError("[推送服务错误] 启动推送服务失败：" + e.Message);
        }
    }

    /// <summary>
    /// 处理推送请求
    /// </summary>
    private void HandlePushRequest(HttpListenerContext context)
    {
        HttpListenerRequest request = context.Request;
        HttpListenerResponse response = context.Response;

        string? message = request.QueryString["message"];
        if (!string.IsNullOrEmpty(message))
        {
            string decryptedMessage = DecryptMessage(message);
            string[] parts = decryptedMessage.Split('|');
            if (parts.Length >= 2)
            {
                string sender = parts[0];
                string msgContent = parts[1];
                LogChatMessage(sender, msgContent);
                LogInfo("[收到消息] " + sender + ": " + msgContent);
                _ = ShowAsync(ShowManager.ShowItem.ShowMessage, sender + ": " + msgContent);
            }
        }

        string responseString = "OK";
        byte[] buffer = Encoding.UTF8.GetBytes(responseString);
        response.ContentLength64 = buffer.Length;
        using var output = response.OutputStream;
        output.Write(buffer, 0, buffer.Length);
    }
    
    /// <summary>
    /// 发送消息
    /// </summary>
    /// <param name="message">消息内容</param>
    public async Task SendMessageAsync(string message)
    {
        if (!isConnected)
        {
            LogError("[发送错误] 未连接到服务器，无法发送消息");
            return;
        }

        try
        {
            string encryptedMessage = EncryptMessage(message);
            string url = $"http://{ip}:{port}/send";
            string requestData = $"username={nickname}&message={encryptedMessage}";

            var content = new StringContent(requestData, Encoding.UTF8, "application/x-www-form-urlencoded");
            if (clientHttp != null)
            {
                HttpResponseMessage response = await clientHttp.PostAsync(url, content);
                response.EnsureSuccessStatusCode();

                if (nickname != null)
                {
                    LogChatMessage(nickname, message);
                    LogInfo("[发送消息] " + nickname + ": " + message);
                }
            }
            else
            {
                LogError("[发送错误] HTTP 客户端未初始化");
            }
        }
        catch (Exception e)
        {
            LogError("[发送错误] 发送消息失败：" + e.Message);
        }
    }

    /// <summary>
    /// 断开连接
    /// </summary>
    public async Task DisconnectAsync()
    {
        try
        {
            if (isConnected && ip != null && nickname != null)
            {
                string url = $"http://{ip}:{port}/disconnect";
                string requestData = $"username={nickname}";

                var content = new StringContent(requestData, Encoding.UTF8, "application/x-www-form-urlencoded");
                if (clientHttp != null)
                {
                    HttpResponseMessage response = await clientHttp.PostAsync(url, content);
                    if (response.IsSuccessStatusCode)
                    {
                        LogInfo("[断开连接] 已从服务器断开连接");
                        _ = ShowAsync(ShowManager.ShowItem.ShowInfo, "已断开与服务器的连接");
                    }
                }
            }
            isConnected = false;
            
            if (cancellationTokenSource != null)
            {
                cancellationTokenSource.Cancel();
            }
        }
        catch (Exception e)
        {
            LogError("[断开连接错误] 断开连接失败：" + e.Message);
        }
    }

    /// <summary>
    /// 关闭客户端
    /// </summary>
    public void Close()
    {
        try
        {
            if (isConnected)
            {
                _ = DisconnectAsync();
            }
            
            if (pushServer != null && pushServer.IsListening)
            {
                pushServer.Stop();
                pushServer.Close();
                if (cml != null) LogInfo("[推送服务] 推送服务已关闭");
            }
            
            if (pushServiceTask != null)
            {
                pushServiceTask.Wait(TimeSpan.FromSeconds(5));
            }
            
            if (cancellationTokenSource != null)
            {
                cancellationTokenSource.Cancel();
                cancellationTokenSource.Dispose();
            }

            if (clientHttp != null)
            {
                clientHttp.Dispose();
            }
            
            if (rsaKeyPair != null)
            {
                rsaKeyPair.Dispose();
            }

            CloseChatLog();
            if (cml != null) LogInfo("[客户端] 客户端已关闭");
            _ = ShowAsync(ShowManager.ShowItem.ShowInfo, "客户端已关闭");
            
            if (coreLogWriter != null)
            {
                coreLogWriter.Flush();
                coreLogWriter.Close();
                coreLogWriter.Dispose();
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"[关闭错误] 关闭客户端失败: {e.Message}");
        }
    }

    public bool IsConnected()
    {
        return isConnected;
    }

    public string? GetNickname()
    {
        return nickname;
    }

    public string? GetUsername()
    {
        return username;
    }
    
}

