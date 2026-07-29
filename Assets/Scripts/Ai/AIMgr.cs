using LitJson;
using Luban.SimpleJSON;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;

public class AIMgr : MonoSingleton<AIMgr>
{
    private bool m_Running = false;
    private AINetwork m_AINetwork = null;
    private CancellationTokenSource m_CancellationTokenSource = null;

    protected override void Awake()
    {
        base.Awake();
        m_Running = false;
    }

    private void Start()
    {
        NetworkManager.Singleton.OnClientStarted += OnClientStarted;
        NetworkManager.Singleton.OnClientStopped += OnClientStopped;
    }

    private void OnClientStarted()
    {
        if (!NetworkManager.Singleton.IsHost) return;
        if (!IsRunning())
        {
            Run();
        }
    }

    private void OnClientStopped(bool obj)
    {
        if (!NetworkManager.Singleton.IsHost) return;
        if(IsRunning())
        {
            Stop();
        }
    }

    protected override void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientStarted -= OnClientStarted;
            NetworkManager.Singleton.OnClientStopped -= OnClientStopped;
        }

        base.OnDestroy();
    }

    /// <summary>
    /// 启动AI
    /// </summary>
    public async void Run()
    {
        if (m_Running)
        {
            Debug.LogWarning("AI已经在运行中");
            return;
        }

        m_Running = true;
        m_CancellationTokenSource = new CancellationTokenSource();
        m_AINetwork = new AINetwork();

        try
        {
            await m_AINetwork.Run(m_CancellationTokenSource.Token);
        }
        catch (Exception ex)
        {
            Debug.LogError($"AI运行异常: {ex.Message}");
        }
    }

    /// <summary>
    /// 停止AI
    /// </summary>
    public void Stop()
    {
        if (!m_Running)
        {
            return;
        }

        m_Running = false;
        m_CancellationTokenSource?.Cancel();
        m_CancellationTokenSource?.Dispose();
        m_CancellationTokenSource = null;
        m_AINetwork = null;
    }

    public bool IsRunning()
    {
        return m_Running;
    }

    /// <summary>
    /// 发送消息给AI服务器
    /// </summary>
    public void SendNetworkMessage(string message)
    {
        if (!m_Running || m_AINetwork == null)
        {
            Debug.LogWarning("AI未运行，无法发送消息");
            return;
        }

        m_AINetwork.EnqueueMessage(message);
    }

    public class AINetwork
    {
        private static readonly string SERVER_IP = "127.0.0.1";
        private static readonly int SERVER_PORT = 9999;
        private static readonly int BUFFER_SIZE = 4096;

        // 发送给AI服务器的消息队列
        private Queue<string> m_MessageQueue = new Queue<string>();
        private object m_QueueLock = new object();

        public AINetwork()
        {
            m_MessageQueue.Clear();
        }

        /// <summary>
        /// 添加消息到发送队列
        /// </summary>
        public void EnqueueMessage(string message)
        {
            lock (m_QueueLock)
            {
                m_MessageQueue.Enqueue(message);
            }
        }

        /// <summary>
        /// 运行AI网络（不阻塞主线程）
        /// </summary>
        public async Task Run(CancellationToken cancellationToken)
        {
            TcpClient client = null;
            NetworkStream stream = null;

            try
            {
                client = new TcpClient();
                await client.ConnectAsync(SERVER_IP, SERVER_PORT);
                Debug.Log($"已连接到AI服务器 {SERVER_IP}:{SERVER_PORT}");

                stream = client.GetStream();

                // 启动接收任务
                Task receiveTask = ReceiveMessagesAsync(stream, cancellationToken);

                // 发送消息循环
                while (!cancellationToken.IsCancellationRequested)
                {
                    // 尝试从消息队列中获取消息
                    string message = null;
                    lock (m_QueueLock)
                    {
                        if (m_MessageQueue.Count > 0)
                        {
                            message = m_MessageQueue.Dequeue();
                        }
                    }

                    if (message != null)
                    {
                        // 发送消息长度（4字节）+ 消息内容
                        byte[] messageBytes = Encoding.UTF8.GetBytes(message);
                        byte[] lengthBytes = BitConverter.GetBytes(messageBytes.Length);

                        await stream.WriteAsync(lengthBytes, 0, lengthBytes.Length, cancellationToken);
                        await stream.WriteAsync(messageBytes, 0, messageBytes.Length, cancellationToken);
                        await stream.FlushAsync(cancellationToken);

                        Debug.Log($"已发送消息给AI服务器: {message}");
                    }
                    else
                    {
                        // 队列为空，等待一段时间避免CPU空转
                        await Task.Delay(100, cancellationToken);
                    }
                }

                // 等待接收任务完成
                await receiveTask;
            }
            catch (OperationCanceledException)
            {
                Debug.Log("AI网络已停止");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"无法连接到AI网络: {ex.Message}");
            }
            finally
            {
                stream?.Close();
                client?.Close();
                Debug.Log("已断开AI服务器连接");
            }
        }

        /// <summary>
        /// 接收AI服务器消息（异步）
        /// </summary>
        private async Task ReceiveMessagesAsync(NetworkStream stream, CancellationToken cancellationToken)
        {
            byte[] buffer = new byte[BUFFER_SIZE];

            try
            {
                while (!cancellationToken.IsCancellationRequested && stream.CanRead)
                {
                    byte[] lengthBytes = new byte[4];
                    int lengthBytesRead = await stream.ReadAsync(lengthBytes, 0, 4, cancellationToken);

                    if (lengthBytesRead == 0) break;

                    int messageLength = BitConverter.ToInt32(lengthBytes, 0);
                    if (messageLength <= 0 || messageLength > BUFFER_SIZE)
                    {
                        Debug.LogWarning($"接收到无效的消息长度: {messageLength}");
                        continue;
                    }

                    int totalBytesRead = 0;
                    while (totalBytesRead < messageLength)
                    {
                        int bytesRead = await stream.ReadAsync(
                            buffer, totalBytesRead, messageLength - totalBytesRead, cancellationToken);
                        if (bytesRead == 0) break;
                        totalBytesRead += bytesRead;
                    }

                    if (totalBytesRead == messageLength)
                    {
                        string message = Encoding.UTF8.GetString(buffer, 0, messageLength);
                        Debug.Log($"收到AI服务器消息: {message}");
                        EventMgr.Instance.Trigger(new ReceiveAIServerMsgEvent
                        {
                            AIAction = JsonMapper.ToObject<PlayerActionData>(message)
                        });
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // 正常取消，忽略
            }
            catch (IOException ex) when (cancellationToken.IsCancellationRequested)
            {
                // ✅ Stop() 主动取消导致的 IO 中断，属于正常流程，不报错
                Debug.Log($"AI接收循环已因取消而停止: {ex.Message}");
            }
            catch (Exception ex)
            {
                // 真正的意外错误才报
                Debug.LogError($"接收消息错误: {ex.Message}");
            }
        }
    }
}
