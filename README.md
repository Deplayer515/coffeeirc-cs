<div align="center">

# 无忧聊 - Minecraft 聊天增强模组
### 的附属项目 咖啡IRC(CoffeeIRC,CIC) 【C# Edition】
<div align="center">
  <img src="./coffeeirc.png" alt="CoffeeIRC Logo" width="96" height="54">
</div>



[![MIT License](https://img.shields.io/badge/License-MIT-red.svg?style=flat&labelColor=444444)](https://opensource.org/licenses/MIT)
[![.NET SDK](https://img.shields.io/badge/.NET-10-blue?style=flat&labelColor=444444&logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/download)
[![C#](https://img.shields.io/badge/C%23-14-cray?style=flat&labelColor=444444&logo=csharp&logoColor=white)](https://learn.microsoft.com/dotnet/csharp/)
[![Namespace](https://img.shields.io/badge/Namespace-io.github.deplayeris.coffeeirc-purple?style=flat&labelColor=444444&logo=github&logoColor=white)](https://github.com/deplayeris/coffeeirc)

<br>CoffeeChat是一个基于NeoForge的Minecraft聊天增强模组，为玩家提供更专业、更完善的IRC聊天系统体验。<br>-----本项目为其核心部分 by C#-----
</div>

## 🎈介绍

>[!WARNING]
> 1. **本项目所编写的内容不是Minecraft模组，不是Minecraft模组，不是Minecraft模组！**<br>
> 为什么这么说？因为一开始这个项目是随着一个名叫“无忧聊”（现名“无忧聊Minecraft”）的模组而出现。<br>
> 如果要在Minecraft上使用CIC，必须要借助一些mod，比如无忧聊Minecraft（直接内置CIC，无需额外安装）。<br>
> ---
> 2. **本项目现在使用“内核-发行版”开发模式，本项目作为核心**，而诸如无忧聊这些作为发行版。<br><br>
> **本项目无法独立运行，必须借助这些发行版**，而这些发行版可能会添加一些自定义的功能，可能会比较方便<br>
> 但是请一定选择**正规并且最好是开源**的发行版，**我们不知道也无法负责这些发行版的安全性**。<br><br>
> 至于CIC官方的发行版CDTE(CIC Default Test Environment)...只能用于测试开发。**能不能正常使用？别指望了**

为用户提供专业、完善的IRC聊天系统体验。<br>
提供了一些 api，让用户可以自定义聊天、外接程序提供功能。<br>
想要开发发行版本还想自定样式？当然可以，CIC特意设置了一个叫呈现器的东西，可以向外呈现信息，以供发行版使用这些信息做出相应的样式呈现。这还可以做到使用任何语言开发发行版，无需担心什么呈现API问题（因为我们压根就没有）。<br>
我们还将在不久的未来提供资源包与插件功能，还有其他实用性技术功能，敬请期待。

## 📦 安装说明

### 系统要求
- *.NET SDK版本* - 10
- *C#版本* - 14
- 如果是在Minecraft内借助特定的发行版运行，推荐1700MB的游戏运行内存空间
- 如果是借助常规运行核心内容的发行版，推荐1100MB运行内存空间

### 编译方法

```bash
# 并不推荐dotnet build，因为编译出来的产物是没法直接用的，要想用很麻烦
# 并且前面也说过本项目不能独立运行，必须依靠发行版
dotnet publish --self-contained ture -p:PublishAot=true -r <平台代名,例如win-x64、win-x86、linux-x64、linux-arm64、linux-arm、osx-x64、osx-arm64、android-x64、android-x86、android-arm64、android-arm、ios-x64、ios-arm64、maccatalyst-x64、maccatalyst-arm64、tvos-x64、tvos-arm64、watchos-x>
```

编译完成后，在 本项目目录的`/bin/Release/net10.0/<平台代名>/native` 目录下可以找到生成的编译产物文件。

### 安装步骤

#### 对于使用者
下载CoffeeIRC的可运行编译产物，他们的文件名通常为`xxx.dll`（Windows）/`xxx.so`（Linux-Like）。<br>
然后寻找一个要手动安装CIC的发行版，并按照他们给出的安装方法安装CIC。<br>
如果选择的发行版已有CIC内置的CIC，请忽略此步骤，直接使用其，无需理会本项目。

#### 对于开发者
1. 下载合适CoffeeIRC-CS版本的源码
2. 解压合并入自己的源码内，以该语言使用外部Native库的方式将CIC链接到自己的项目
3. 参考文档进行适用开发

#### 对于使用了CoffeeChat的Minecraft玩家

- 对于普通玩家<br>
  请前往[此处](https://github.com/deplayeris/coffeechat/releases)选择合适版本下载并照CoffeeChat项目的README.md中说明进行安装，无需理会本项目。
> [!WARNING]
> **请勿将本项目的可运行编译产物安装进游戏，本项目所编写的内容不是Minecraft模组，不是Minecraft模组，不是Minecraft模组！**

- 对于高技术玩家<br>
  照CoffeeChatMC项目的README.md中说明进行下载源码之后,请参考【安装步骤/对于开发者-2.】，然后操作。
> [!WARNING]
> **请谨慎考虑它与某些版本的CoffeeChatMC的兼容性，所使用的CoffeeIRC核心相对于这一某个版本是过旧版本或过新版本都可能导致兼容性问题。**

## 📚 文档

开发者可以在 [`docs`](docs/) 文件夹中查看详细的开发文档和API说明。<br>

## 🤝 贡献指南

我们欢迎任何形式的贡献！请查看详细的 [贡献规范](../coffeechat/CONTRIBUTING.md) 了解完整的提交和PR规范。

### 快速开始

1. **Fork项目** 到你的GitHub账户
2. **克隆到本地**：
   ```bash
   git clone https://github.com/yourusername/coffeeirc-cs.git
   cd coffeeirc-cs
   ```
3. **创建功能分支**：
   ```bash
   git checkout -b feature/your-feature-name
   ```
4. **进行开发** 并遵循 [贡献规范](../coffeechat/CONTRIBUTING.md)
5. **提交更改** 并推送
6. **创建Pull Request"

### 文档说明

- **外部贡献文档**：位于源代码根目录（如本README、CONTRIBUTING.md等）
- **内部开发文档**：请写入 [`docs/`](docs/) 文件夹内

### 代码规范
- 使用Java 25语法特性
- 遵循NeoForge开发最佳实践
- 确保良好的代码文档和注释

## 📄 许可证与遵循协议

本项目采用 **MIT许可证**作为开源许可证，详情请参见 [LICENSE](LICENSE) 文件。<br>
本项目采用 **DeplayerCTS 2026-0001 以开放、共享、自由、包容为目的的创作者与社区治理与发展标准**作为行为准则，详情请参见 [DeplayerCTS](CODE_OF_CONDUCT.md) 文件。

## 📞 联系方式

- **作者**: Deplayer
- **邮箱**: deplayer515@hotmail.com
- **GitHub Issues**: [提交问题报告](https://github.com/deplayeris/coffeechat/issues)

## 🙏 致谢

感谢所有为这个项目做出贡献的开发者、社区教程大佬、使用者以及社区小伙伴们！

---