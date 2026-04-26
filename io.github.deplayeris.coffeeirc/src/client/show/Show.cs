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
 * 此部分为向外呈现器部分
 * 
 */


namespace io.github.deplayeris.coffeeirc.client;

public partial class Client
{
    /// <summary>
    /// 信息呈现管理器的工具方法，用于向外界呈现信息，ShowManager为其提供支持
    /// </summary>
    /// <param name="showItem">呈现何种内容</param>
    /// <param name="message">内容的具体详情</param>
    private async Task ShowAsync(ShowManager.ShowItem showItem, string message)
    { 
        showManager.ShowThis(showItem, message);
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
        /// 内部信息呈现管理器的构造函数
        /// </summary>
        /// <param name="showInterfaceFilePath">传给内部成员"sIFP"，用于确定呈现器接口文件路径</param>
        public ShowManager(string showInterfaceFilePath)
        {
            sIFP = showInterfaceFilePath;
            using (File.Create(sIFP)) { }
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
                    File.WriteAllText(sIFP, "[MSG]" + message);
                    break;
                case ShowItem.ShowTip:
                    File.WriteAllText(sIFP, "[TIP]" + message);
                    break;
                case ShowItem.ShowError:
                    File.WriteAllText(sIFP, "[ERR]" + message);
                    break;
                case ShowItem.ShowWarning:
                    File.WriteAllText(sIFP, "[WAN]" + message);
                    break;
                case ShowItem.ShowInfo:
                    File.WriteAllText(sIFP, "[INF]" + message);
                    break;
                case ShowItem.ShowDebug:
                    File.WriteAllText(sIFP, "[DBG]" + message);
                    break;
                case ShowItem.ShowChatmsg:
                    File.WriteAllText(sIFP, "[CHT]" + message);
                    break;
            }
        }
    }
    
}