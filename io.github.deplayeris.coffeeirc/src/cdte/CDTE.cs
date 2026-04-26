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

using io.github.deplayeris.coffeeirc.client;

namespace io.github.deplayeris.coffeeirc.cdte;

/// <summary>
/// CDTE - CIC 官方测试开发发行版
/// 当你测试的时候,把生成类型改成Exe.
/// </summary>
public class CDTE
{
    private static CancellationTokenSource cancellationTokenSource = new();
    
    public static async Task Main(string[] args)
    {
        // 启动消息读取任务
        var readTask = ReadShowAsync();
        
        Console.WriteLine("CDTE - CIC 官方测试开发发行版");
        Console.WriteLine($"版本：{SwInfoc.Version}");
        Console.WriteLine($"状态：{SwInfoc.SoftwareStatus}");
        Console.WriteLine($"代号：{SwInfoc.VerCodename}");
        Console.WriteLine($"协议：{SwInfoc.Connection}");
        Console.WriteLine();

        // 创建客户端实例
        var client = new Client(
            ipProtocol: 4,
            ip: "127.0.0.1",
            port: 8080,
            nickname: "TestUser",
            username: "testuser",
            distributionName: "CDTE v1.0",
            customKey: "dsaitopeing"
        );
        
        // 启动客户端
        client.StartClient();
        
        // 发送测试消息
        if (client.IsConnected())
        {
            await client.SendMessageAsync("Hello, World!");
            Console.WriteLine("\n提示 ] 消息已发送，您可以在控制台输入消息继续聊天");
            Console.WriteLine("[提示] 输入 'quit' 或 'exit' 退出程序\n");
        }
        else
        {
            Console.WriteLine("\n错误 ] 未连接到服务器，无法发送消息");
        }
        
        // 主循环：读取用户输入
        while (!cancellationTokenSource.Token.IsCancellationRequested)
        {
            string? input = Console.ReadLine();
            if (string.IsNullOrEmpty(input))
                continue;
                
            if (input.ToLower() == "quit" || input.ToLower() == "exit")
            {
                break;
            }
            
            // 发送用户输入的消息
            if (client.IsConnected())
            {
                await client.SendMessageAsync(input);
            }
            else
            {
                Console.WriteLine("错误 ] 未连接到服务器");
            }
        }
        
        Console.WriteLine("\n正在关闭客户端...");
        
        // 取消读取任务
        cancellationTokenSource.Cancel();
        
        // 等待读取任务结束
        try
        {
            await readTask;
        }
        catch (OperationCanceledException)
        {
            // 正常取消
        }
        
        // 清理资源
        client.Close();
        
        Console.WriteLine("客户端已关闭，按任意键退出...");
        Console.ReadKey();
    }
    
    /// <summary>
    /// 异步读取 .show 文件呈现的信息
    /// </summary>
    private static async Task ReadShowAsync()
    {
        string showFilePath = ".show";
        string lastContent = "";
        DateTime lastWriteTime = DateTime.MinValue;
        
        try
        {
            while (!cancellationTokenSource.Token.IsCancellationRequested)
            {
                try
                {
                    if (File.Exists(showFilePath))
                    {
                        var fileInfo = new FileInfo(showFilePath);
                        
                        // 只有文件修改时才读取
                        if (fileInfo.LastWriteTime > lastWriteTime)
                        {
                            lastWriteTime = fileInfo.LastWriteTime;
                            string content = await File.ReadAllTextAsync(showFilePath, cancellationTokenSource.Token);
                            
                            // 调试：显示原始内容
                            // Console.WriteLine($"[DEBUG] 读取到内容: '{content}'");
                            
                            // 只处理有效的呈现内容（必须以标签开头）
                            if (!string.IsNullOrEmpty(content) && 
                                (content.StartsWith("[MSG]") || content.StartsWith("[TIP]") || 
                                 content.StartsWith("[ERR]") || content.StartsWith("[WAN]") || 
                                 content.StartsWith("[INF]") || content.StartsWith("[DBG]") || 
                                 content.StartsWith("[CHT]")))
                            {
                                // 只有内容变化时才显示
                                if (content != lastContent)
                                {
                                    lastContent = content;
                                    
                                    // 解析呈现内容
                                    if (content.StartsWith("[MSG]"))
                                    {
                                        Console.ForegroundColor = ConsoleColor.White;
                                        Console.WriteLine($"\r[消息] {content.Substring(5)}");
                                    }
                                    else if (content.StartsWith("[TIP]"))
                                    {
                                        Console.ForegroundColor = ConsoleColor.Cyan;
                                        Console.WriteLine($"\r[提示] {content.Substring(5)}");
                                    }
                                    else if (content.StartsWith("[ERR]"))
                                    {
                                        Console.ForegroundColor = ConsoleColor.Red;
                                        Console.WriteLine($"\r[错误] {content.Substring(5)}");
                                    }
                                    else if (content.StartsWith("[WAN]"))
                                    {
                                        Console.ForegroundColor = ConsoleColor.Yellow;
                                        Console.WriteLine($"\r[警告] {content.Substring(5)}");
                                    }
                                    else if (content.StartsWith("[INF]"))
                                    {
                                        Console.ForegroundColor = ConsoleColor.Green;
                                        Console.WriteLine($"\r[信息] {content.Substring(5)}");
                                    }
                                    else if (content.StartsWith("[DBG]"))
                                    {
                                        Console.ForegroundColor = ConsoleColor.DarkGray;
                                        Console.WriteLine($"\r[调试] {content.Substring(5)}");
                                    }
                                    else if (content.StartsWith("[CHT]"))
                                    {
                                        Console.ForegroundColor = ConsoleColor.Magenta;
                                        Console.WriteLine($"\r[聊天] {content.Substring(5)}");
                                    }
                                    
                                    Console.ResetColor();
                                    Console.Write("> "); // 重新显示输入提示
                                }
                            }
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch
                {
                    // 忽略文件读取错误
                }
                
                await Task.Delay(500, cancellationTokenSource.Token); // 每500ms检查一次
            }
        }
        catch (OperationCanceledException)
        {
            // 正常取消
        }
    }
}
