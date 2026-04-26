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
/// </summary>
public class CDTE
{
    public static void Main(string[] args)
    {
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
            customKey: ""
        );

        Console.WriteLine();
        Console.WriteLine("按任意键退出...");
        Console.ReadKey();

        // 清理资源
        client.Close();
    }
}
