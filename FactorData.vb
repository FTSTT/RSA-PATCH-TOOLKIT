Option Strict On
Option Explicit On
Option Infer On

Imports System.Collections.Concurrent
Imports System.Threading

'-------------------------
' 数据模型：FactorData
'-------------------------
Public Class FactorData
    Public Property N As String
    Public Property FactorA As String
    Public Property FactorB As String
    Public Property FactorMode As String

    Public Sub New(n As String, a As String, b As String, Optional m As String = Nothing)
        Me.N = n
        Me.FactorA = a
        Me.FactorB = b
        Me.FactorMode = m
    End Sub

    Public Overrides Function ToString() As String
        Return $"{N} = {FactorA} * {FactorB}"
    End Function
End Class

'-------------------------------------------
' 线程安全：泛型 FIFO 容器（先进先出）
'-------------------------------------------
Public NotInheritable Class FifoStack(Of T)
    Private ReadOnly _q As New ConcurrentQueue(Of T)()

    ' push：添加元素（FIFO）
    Public Sub Push(item As T)
        _q.Enqueue(item)
    End Sub

    ' pop：提取 1 个元素（取到即删除），True=成功
    Public Function TryPop(ByRef item As T) As Boolean
        Return _q.TryDequeue(item)
    End Function

    ' 只看一眼不删除（可选）
    Public Function TryPeek(ByRef item As T) As Boolean
        Return _q.TryPeek(item)
    End Function

    ' 只读快照
    Public Function ToArray() As T()
        Return _q.ToArray()
    End Function

    Public ReadOnly Property Count As Integer
        Get
            Return _q.Count
        End Get
    End Property

    Public ReadOnly Property IsEmpty As Boolean
        Get
            Return _q.IsEmpty
        End Get
    End Property

    ' 清空（逐个出队保证并发语义）
    Public Sub Clear()
        Dim tmp As T
        While _q.TryDequeue(tmp)
        End While
    End Sub
End Class

'-------------------------------------------------
' 强类型全局队列（FIFO）：Of FactorData
'-------------------------------------------------
Public Module GlobalFifo

    Private ReadOnly _queue As New ConcurrentQueue(Of FactorData)()
    ' GlobalFifo 内新增：
    Public Sub Clear()
        Dim tmp As FactorData
        While _queue.TryDequeue(tmp)
            ' 丢弃 tmp 即可（已从队列删除）
        End While
    End Sub

    ' push：添加一个元素
    Public Sub Push(item As FactorData)
        _queue.Enqueue(item)
    End Sub

    ' pop：提取 1 个元素（取到即删除）
    Public Function TryPop(ByRef item As FactorData) As Boolean
        Return _queue.TryDequeue(item)
    End Function

    ' 可选：窥视不删
    Public Function TryPeek(ByRef item As FactorData) As Boolean
        Return _queue.TryPeek(item)
    End Function

    ' 可选：只读快照
    Public Function Snapshot() As FactorData()
        Return _queue.ToArray()
    End Function

    Public ReadOnly Property Count As Integer
        Get
            Return _queue.Count
        End Get
    End Property
End Module

'-------------------------
' 最小使用示例
'-------------------------
Module DemoFD
    Private _timer As Timer

    Sub MainFD()
        ' 示例 A：本地容器
        Dim localQ As New FifoStack(Of FactorData)()
        ' localQ.Push(New FactorData("6, 2, 3))
        ' localQ.Push(New FactorData(15, 3, 5))

        Dim it As FactorData
        If localQ.TryPop(it) Then
            Console.WriteLine("[Local] " & it.ToString())
        End If

        ' 示例 B：全局队列
        'GlobalFifo.Push(New FactorData(21, 3, 7))
        'GlobalFifo.Push(New FactorData(28, 4, 7))
        'GlobalFifo.Push(New FactorData(30, 5, 6))

        ' 定时消费（FIFO；一次可多条或只取 1 条，见注释）
        _timer = New Timer(AddressOf TimerTick, Nothing, 0, 300)

        Thread.Sleep(2000)
        _timer.Change(Timeout.Infinite, Timeout.Infinite)
        _timer.Dispose()

        Console.WriteLine("Done. Press ENTER.")
        Console.ReadLine()
    End Sub

    Private Sub TimerTick(state As Object)
        ' —— 每次只取 1 个 ——（如需一次取多条，改成 While TryPop(...) 循环即可）
        Dim item As FactorData = Nothing
        If GlobalFifo.TryPop(item) Then
            Console.WriteLine("[Global] " & item.ToString() & $"  | Remaining={GlobalFifo.Count}")
        End If
    End Sub
End Module
