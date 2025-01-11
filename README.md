
高性能异步封包服务器/客户端，和消息的分发处理，支持 TCP、Pipe 两种适配器。

在TCP和Pipe基础上的消息服务器/客户端



## 功能

- [x] TCP服务器 (TCPServer)
- [x] TCP客户端 (TCPClient)
- [x] 管道服务器 (PipeServer)
- [x] 管道客户端 (PipeClient)
- [x] 消息服务器 (MessageServer) 基于管道服务器 或 TCP服务器
- [x] 消息客户端 (MessageClient) 基于管道客户端 或 TCP客户端
- [x] 支持消息池化
- [x] 消息路由


--------------

## 测试

``` bash
Examples.exe server

Examples.exe client
Examples.exe client
```

--------------


