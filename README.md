# CefDetector.Net - 检测你系统中包含多少个 Chromium 内核程序

Check how many CEFs are on your Linux/MacOS.

看看你系统 **(Linux/MacOS)** 上有多少个 [CEF (Chromium Embedded Framework)](https://bitbucket.org/chromiumembedded/cef/).

目前没有特别完善，可能还有很多情况没考虑到，欢迎发 issue ！

（测试系统：Deepin 23 Alpha、Apple Silicon MacOS；Deepin上浏览器会存在重复检测的情况，看截图你就懂了）

（也许可以起一个 Web 服务然后展示“喜报“，以后再说啦）

> 目前仅支持Linux/MacOS，Windows支持计划中...

## 截屏

Deepin：
![ScreenshotLinux](./screenshot_linux.png)

Macos:
![ScreenshotMacos](./screenshot_mac.png)

## 使用

从 [Release](https://github.com/xh321/CefDetector.Net/releases) 页面下载最新的压缩包, 解压后给予 `CefDetector.Net` 执行权限后运行即可.

## 特性

- 检测 CEF 的类型: 如 [libcef](https://bitbucket.org/chromiumembedded/cef/src/master/)、[Electron](https://www.electronjs.org/)、[NWJS](https://nwjs.io/)、[CefSharp](http://cefsharp.github.io/)、[Edge](https://www.microsoft.com/en-us/edge) 和 [Chrome](https://www.google.com/chrome/)
- 计算总数量

## 作者

xh321

创意来自 @ShirasawaSama 的 [CefDetector](https://github.com/ShirasawaSama/CefDetector) 项目。由于这个项目没有跨平台，所以我花一下午搓了一个功能差不多的。

## 协议

[MIT](./LICENSE)