
Imports System.Collections.Concurrent
Imports System.Reflection
Imports System.Threading

Friend Class WorkItem
    Public Property Id As Integer
    Public Property [Delegate] As [Delegate]
    Public Property Args As Object()
    Public Property Thread As Thread
    Public Property Cts As CancellationTokenSource
    Public Property EnqueueTime As Date = Date.Now
    Public Property StartTime As Date?
    Public Property EndTime As Date?
    Public Property Status As String = "Queued"  ' Queued/Running/Completed/Faulted/Canceled/Aborted/TimedOut
    Public Property [Error] As Exception
    Public Property TimeoutMs As Integer = 0
    Public Property Watchdog As System.Threading.Timer
    Public Property TimedOut As Integer = 0
End Class

Public Class ThreadManager
    Private ReadOnly _queue As New ConcurrentQueue(Of WorkItem)()
    Private ReadOnly _running As New ConcurrentDictionary(Of Integer, WorkItem)()
    Private ReadOnly _tick As New AutoResetEvent(False)
    Private ReadOnly _shutdown As New ManualResetEventSlim(False)
    Private ReadOnly _dispatcher As Thread
    Private ReadOnly _logTimer As Timer
    Private _log As Action(Of String)
    Private Shared _nextId As Integer = 0

    Private _maxConcurrent As Integer
    Private _isPaused As Integer = 0 ' 0=false, 1=true
    Private _completedTotal As Long = 0
    Private _completedSinceLast As Long = 0
    Private _logTimerActive As Integer = 1

    Private _defaultSingleThreadTimeoutMs As Integer = 0

    Private Shared ReadOnly _defaultLogger As Action(Of String) = Sub(s As String)
                                                                      Try
                                                                          Debug.WriteLine(s)
                                                                          Console.WriteLine(s)
                                                                      Catch
                                                                      End Try
                                                                  End Sub

    Public Property DefaultSingleThreadTimeoutMs As Integer
        Get
            Return _defaultSingleThreadTimeoutMs
        End Get
        Set(value As Integer)
            If value < 0 Then Throw New ArgumentOutOfRangeException(NameOf(DefaultSingleThreadTimeoutMs))
            _defaultSingleThreadTimeoutMs = value
            _log("[TM] DefaultSingleThreadTimeoutMs=" & value.ToString() & " ms")
        End Set
    End Property

    Public Property MaxConcurrent As Integer
        Get
            Return _maxConcurrent
        End Get
        Set(value As Integer)
            If value < 1 Then Throw New ArgumentOutOfRangeException(NameOf(MaxConcurrent))
            _maxConcurrent = value
            _tick.Set()
            _log("[TM] MaxConcurrent set to " & value.ToString())
        End Set
    End Property

    Public Sub New(maxConcurrent As Integer, Optional logAction As Action(Of String) = Nothing)
        If maxConcurrent < 1 Then Throw New ArgumentOutOfRangeException(NameOf(maxConcurrent))
        _maxConcurrent = maxConcurrent
        _log = If(logAction, _defaultLogger)

        _dispatcher = New Thread(AddressOf DispatchLoop)
        _dispatcher.IsBackground = True
        _dispatcher.Name = "TM.Dispatcher"
        _dispatcher.Start()

        _logTimer = New Timer(AddressOf LogTick, Nothing, 5000, 5000)
        _log("[TM] Started. MaxConcurrent=" & _maxConcurrent.ToString())
    End Sub

    Private Sub LogTick(state As Object)
        Dim q As Integer = _queue.Count
        Dim r As Integer = _running.Count
        Dim done As Long = Interlocked.Exchange(_completedSinceLast, 0)
        _log("[TM] " & Date.Now.ToString("HH:mm:ss") & " Queue=" & q.ToString() & ", Running=" & r.ToString() &
             ", CompletedTotal=" & Interlocked.Read(_completedTotal).ToString() & " (+" & done.ToString() & "/5s)")
        If q = 0 AndAlso r = 0 Then
            Try
                _log("[TM] LogTimer: stopped (idle)")
                _logTimer.Change(Timeout.Infinite, Timeout.Infinite)
            Catch
            End Try
            Interlocked.Exchange(_logTimerActive, 0)
        End If
    End Sub

    Public Sub SetLogger(logAction As Action(Of String))
        If logAction IsNot Nothing Then
            _log = logAction
        End If
    End Sub

    Public Function EnqueueWithTimeout(timeoutMs As Integer, work As [Delegate], ParamArray args As Object()) As Integer
        If work Is Nothing Then Throw New ArgumentNullException(NameOf(work))
        If timeoutMs < 1 Then Throw New ArgumentOutOfRangeException(NameOf(timeoutMs))
        Dim id As Integer = Interlocked.Increment(_nextId)
        Dim wi As New WorkItem With {
            .Id = id,
            .[Delegate] = work,
            .Args = If(args, Array.Empty(Of Object)()),
            .Cts = New CancellationTokenSource(),
            .TimeoutMs = timeoutMs
        }
        _queue.Enqueue(wi)
        _log("[TM] Enqueued #" & wi.Id.ToString() & " (" & work.Method.Name & ") timeout=" & timeoutMs.ToString() & "ms")
        _tick.Set()
        If Interlocked.CompareExchange(_logTimerActive, 1, 1) = 0 Then
            Try
                _log("[TM] LogTimer: start (enqueue)")
                _logTimer.Change(5000, 5000)
            Catch
            End Try
            Interlocked.Exchange(_logTimerActive, 1)
        End If
        Return wi.Id
    End Function

    Public Function Enqueue(work As [Delegate], ParamArray args As Object()) As Integer
        If work Is Nothing Then Throw New ArgumentNullException(NameOf(work))
        Dim id As Integer = Interlocked.Increment(_nextId)
        Dim wi As New WorkItem With {
            .Id = id,
            .[Delegate] = work,
            .Args = If(args, Array.Empty(Of Object)()),
            .Cts = New CancellationTokenSource()
        }
        _queue.Enqueue(wi)
        _log("[TM] Enqueued #" & wi.Id.ToString() & " (" & work.Method.Name & ")")
        _tick.Set()
        If Interlocked.CompareExchange(_logTimerActive, 1, 1) = 0 Then
            Try
                _log("[TM] LogTimer: start (enqueue)")
                _logTimer.Change(5000, 5000)
            Catch
            End Try
            Interlocked.Exchange(_logTimerActive, 1)
        End If
        Return wi.Id
    End Function

    Public Sub Pause()
        Interlocked.Exchange(_isPaused, 1)
        _log("[TM] Paused")
    End Sub

    Public Sub [Resume]()
        Interlocked.Exchange(_isPaused, 0)
        _log("[TM] Resumed")
        _tick.Set()
    End Sub

    Public Function ClearQueue() As Integer
        Dim n As Integer = 0
        Dim tmp As WorkItem = Nothing
        While _queue.TryDequeue(tmp)
            n += 1
        End While
        If n > 0 Then
            _log("[TM] Cleared " & n.ToString() & " queued item(s).")
        End If
        Return n
    End Function

    Public Sub CancelAll(Optional reason As String = "UserCancel")
        _log("[TM] CancelAll: " & reason)
        Dim tmp As WorkItem = Nothing
        While _queue.TryDequeue(tmp)
        End While
        For Each kv As Collections.Generic.KeyValuePair(Of Integer, WorkItem) In _running
            Try
                kv.Value.Cts.Cancel()
            Catch
            End Try
        Next
        _tick.Set()
    End Sub

    Public Sub ForceStopAll(Optional tryAbortOnNetFx As Boolean = True)
        Interlocked.Exchange(_isPaused, 1)
        _log("[TM] ForceStopAll: Pause scheduling")
        _tick.Set()

        Dim dropped As Integer = 0
        Dim tmp As WorkItem = Nothing
        While _queue.TryDequeue(tmp)
            dropped += 1
        End While
        If dropped > 0 Then
            _log("[TM] ForceStopAll: dropped " & dropped.ToString() & " queued item(s)")
        End If

        _log("[TM] ForceStopAll: Cancel + Interrupt" & If(tryAbortOnNetFx, " + Abort", ""))
        For Each kv As Collections.Generic.KeyValuePair(Of Integer, WorkItem) In _running
            Dim t As Thread = kv.Value.Thread
            Try
                kv.Value.Cts.Cancel()
            Catch
            End Try
            Try
                If t IsNot Nothing AndAlso t.IsAlive Then
                    t.Interrupt()
                End If
            Catch
            End Try
#If NETFRAMEWORK Then
            If tryAbortOnNetFx Then
                Try
                    If t Is Not Nothing AndAlso t.IsAlive Then
                        t.Abort()
                    End If
                Catch
                End Try
            End If
#End If
        Next

        _tick.Set()
    End Sub

    Public Sub [Stop]()
        _shutdown.Set()
        _tick.Set()
        Try
            _dispatcher.Join(2000)
        Catch
        End Try
        Try
            _logTimer.Dispose()
        Catch
        End Try
        _log("[TM] Stopped")
    End Sub

    Private Sub DispatchLoop()
        Do
            If _shutdown.IsSet Then Exit Do

            If Interlocked.CompareExchange(_isPaused, 0, 0) = 0 Then
                Dim started As Integer = 0
                While _running.Count < _maxConcurrent AndAlso Not _shutdown.IsSet
                    Dim wi As WorkItem = Nothing
                    If Not _queue.TryDequeue(wi) Then
                        Exit While
                    End If
                    StartWork(wi)
                    started += 1
                End While
                If started > 0 Then
                    _log("[TM] Started " & started.ToString() & " item(s). Running=" & _running.Count.ToString() & ", Queue=" & _queue.Count.ToString())
                End If
            End If

            WaitHandle.WaitAny(New WaitHandle() {_tick, _shutdown.WaitHandle}, 1000)
        Loop
    End Sub

    Private Sub StartWork(wi As WorkItem)
        wi.Status = "Running"
        wi.StartTime = Date.Now
        _running.TryAdd(wi.Id, wi)

        Dim effTimeout As Integer = wi.TimeoutMs
        If effTimeout <= 0 AndAlso _maxConcurrent = 1 AndAlso _defaultSingleThreadTimeoutMs > 0 Then
            effTimeout = _defaultSingleThreadTimeoutMs
        End If

        Dim th As New Thread(Sub() ExecuteWork(wi))
        th.IsBackground = True
        th.Name = "TM.Worker#" & wi.Id.ToString()
        wi.Thread = th

        If effTimeout > 0 Then
            Dim cb As TimerCallback = AddressOf TimeoutCallback
            wi.Watchdog = New System.Threading.Timer(cb, wi, effTimeout, System.Threading.Timeout.Infinite)
            _log("[TM] Watchdog set for #" & wi.Id.ToString() & " -> " & effTimeout.ToString() & " ms")
        End If

        th.Start()
    End Sub

    Private Sub TimeoutCallback(state As Object)
        Dim wi As WorkItem = TryCast(state, WorkItem)
        If wi Is Nothing Then Return
        If System.Threading.Interlocked.Exchange(wi.TimedOut, 1) = 1 Then Return
        _log("[TM] TIMEOUT for #" & wi.Id.ToString())
        Try
            wi.Cts.Cancel()
        Catch
        End Try
        Try
            If wi.Thread IsNot Nothing AndAlso wi.Thread.IsAlive Then
                wi.Thread.Interrupt()
            End If
        Catch
        End Try
    End Sub

    Private Sub ExecuteWork(wi As WorkItem)
        Try
            Dim work As [Delegate] = wi.[Delegate]
            Dim pars() As ParameterInfo = work.Method.GetParameters()
            Dim argsToUse() As Object

            If pars.Length > 0 AndAlso pars(pars.Length - 1).ParameterType = GetType(CancellationToken) Then
                If wi.Args.Length = pars.Length - 1 Then
                    argsToUse = New Object(wi.Args.Length) {}
                    Array.Copy(wi.Args, argsToUse, wi.Args.Length)
                    argsToUse(argsToUse.Length - 1) = wi.Cts.Token
                Else
                    argsToUse = wi.Args
                End If
            Else
                argsToUse = wi.Args
            End If

            work.DynamicInvoke(argsToUse)

            If wi.Cts.IsCancellationRequested Then
                wi.Status = "Canceled"
            Else
                wi.Status = "Completed"
            End If
        Catch ex As TargetInvocationException
            If TypeOf ex.InnerException Is ThreadInterruptedException Then
                wi.Status = "Aborted"
            ElseIf TypeOf ex.InnerException Is ThreadAbortException Then
                wi.Status = "Aborted"
#If NETFRAMEWORK Then
                Try
                    Thread.ResetAbort()
                Catch
                End Try
#End If
            Else
                wi.Status = "Faulted"
                wi.[Error] = ex.InnerException
            End If
        Catch ex As ThreadInterruptedException
            wi.Status = "Aborted"
        Catch ex As ThreadAbortException
            wi.Status = "Aborted"
#If NETFRAMEWORK Then
            Try
                Thread.ResetAbort()
            Catch
            End Try
#End If
        Catch ex As Exception
            wi.Status = "Faulted"
            wi.[Error] = ex
        Finally
            Dim wd As System.Threading.Timer = wi.Watchdog
            If wd IsNot Nothing Then
                Try
                    wd.Dispose()
                Catch
                End Try
            End If

            wi.EndTime = Date.Now
            Dim removed As WorkItem = Nothing
            _running.TryRemove(wi.Id, removed)
            Interlocked.Increment(_completedTotal)
            Interlocked.Increment(_completedSinceLast)

            If wi.TimedOut = 1 AndAlso wi.Status <> "Completed" Then
                wi.Status = "TimedOut"
            End If

            Dim msg As String = "[TM] Finished #" & wi.Id.ToString() & " -> " & wi.Status
            If wi.[Error] IsNot Nothing Then
                msg &= " | ex: " & wi.[Error].GetType().Name & ": " & wi.[Error].Message
            End If
            msg &= " | elapsed=" & (wi.EndTime.Value - wi.StartTime.Value).TotalMilliseconds.ToString("N0") & " ms"
            _log(msg)

            _tick.Set()
        End Try
    End Sub
    ' 放在 ThreadManager 类内部（保持与现有字段同文件）
    Public Structure WorkItemInfo
        Public Id As Integer
        Public Status As String       ' Queued/Running/Completed/Faulted/Canceled/Aborted/TimedOut
        Public EnqueueTime As Date
        Public StartTime As Date?
        Public EndTime As Date?
        Public TimeoutMs As Integer
        Public TimedOut As Integer    ' 0/1
        Public ErrorMessage As String ' 仅传文本，避免把 Exception 对象跨线程外泄
    End Structure

    Public Structure ThreadManagerStats
        Public QueueCount As Integer
        Public RunningCount As Integer
        Public CompletedTotal As Long
        Public MaxConcurrent As Integer
        Public IsPaused As Boolean
        Public DefaultSingleThreadTimeoutMs As Integer
    End Structure

    ' —— 统计快照：O(1)
    Public Function GetStatsSnapshot() As ThreadManagerStats
        Return New ThreadManagerStats With {
        .QueueCount = _queue.Count,
        .RunningCount = _running.Count,
        .CompletedTotal = Threading.Interlocked.Read(_completedTotal),
        .MaxConcurrent = _maxConcurrent,
        .IsPaused = (Threading.Interlocked.CompareExchange(_isPaused, 0, 0) = 1),
        .DefaultSingleThreadTimeoutMs = _defaultSingleThreadTimeoutMs
    }
    End Function

    ' —— 运行中条目快照：遍历 ConcurrentDictionary 是线程安全的
    Public Function GetRunningSnapshot(Optional maxItems As Integer = Integer.MaxValue) As List(Of WorkItemInfo)
        Dim list As New List(Of WorkItemInfo)
        For Each kv In _running
            If list.Count >= maxItems Then Exit For
            Dim w = kv.Value
            list.Add(Simplify(w))
        Next
        ' 为了稳定输出顺序，按 Id 排序（可选）
        list.Sort(Function(a, b) a.Id.CompareTo(b.Id))
        Return list
    End Function

    ' —— 队列快照：使用 ToArray() 拿到静态副本，避免遍历时与出队竞争
    Public Function GetQueueSnapshot(Optional maxItems As Integer = Integer.MaxValue) As List(Of WorkItemInfo)
        Dim arr = _queue.ToArray()
        Dim list As New List(Of WorkItemInfo)(Math.Min(maxItems, arr.Length))
        For i = 0 To Math.Min(maxItems, arr.Length) - 1
            list.Add(Simplify(arr(i)))
        Next
        Return list
    End Function

    ' —— 按 Id 查询（先查运行中，再在队列副本里查）
    Public Function TryGetWorkItemInfo(id As Integer, ByRef info As WorkItemInfo) As Boolean
        Dim wi As WorkItem = Nothing
        If _running.TryGetValue(id, wi) Then
            info = Simplify(wi) : Return True
        End If
        For Each q In _queue.ToArray()
            If q.Id = id Then info = Simplify(q) : Return True
        Next
        info = Nothing
        Return False
    End Function

    ' —— 将内部 WorkItem 压缩为可公开的数据
    Private Shared Function Simplify(w As WorkItem) As WorkItemInfo
        Return New WorkItemInfo With {
        .Id = w.Id,
        .Status = w.Status,
        .EnqueueTime = w.EnqueueTime,
        .StartTime = w.StartTime,
        .EndTime = w.EndTime,
        .TimeoutMs = w.TimeoutMs,
        .TimedOut = w.TimedOut,
        .ErrorMessage = If(w.Error Is Nothing, Nothing, w.Error.GetType().Name & ": " & w.Error.Message)
    }
    End Function

End Class

Public Module ThreadHub
    Public Property CurrentManager As ThreadManager

    Public Sub Init(maxConcurrent As Integer, Optional logAction As Action(Of String) = Nothing)
        If Not Object.ReferenceEquals(CurrentManager, Nothing) Then
            Try
                CurrentManager.Stop()
            Catch
            End Try
        End If
        CurrentManager = New ThreadManager(maxConcurrent, logAction)
    End Sub

    Public Function Enqueue(work As [Delegate], ParamArray args As Object()) As Integer
        If Object.ReferenceEquals(CurrentManager, Nothing) Then
            Init(Environment.ProcessorCount)
        End If
        Return CurrentManager.Enqueue(work, args)
    End Function

    Public Sub Pause()
        If Not Object.ReferenceEquals(CurrentManager, Nothing) Then
            CurrentManager.Pause()
        End If
    End Sub

    Public Sub [Resume]()
        If Not Object.ReferenceEquals(CurrentManager, Nothing) Then
            CurrentManager.Resume()
        End If
    End Sub

    Public Sub CancelAll(Optional reason As String = "UserCancel")
        If Not Object.ReferenceEquals(CurrentManager, Nothing) Then
            CurrentManager.CancelAll(reason)
        End If
    End Sub

    Public Sub ForceStopAll(Optional tryAbortOnNetFx As Boolean = True)
        If Not Object.ReferenceEquals(CurrentManager, Nothing) Then
            CurrentManager.ForceStopAll(tryAbortOnNetFx)
        End If
    End Sub

    Public Sub SetLogger(logAction As Action(Of String))
        If Not Object.ReferenceEquals(CurrentManager, Nothing) Then
            CurrentManager.SetLogger(logAction)
        End If
    End Sub

    Public Sub SetMaxConcurrent(n As Integer)
        If Not Object.ReferenceEquals(CurrentManager, Nothing) Then
            CurrentManager.MaxConcurrent = n
        End If
    End Sub

    Public Function GetStats() As ThreadManager.ThreadManagerStats
        If Object.ReferenceEquals(CurrentManager, Nothing) Then ThreadHub.Init(Environment.ProcessorCount)
        Return CurrentManager.GetStatsSnapshot()
    End Function

    Public Function ListRunning(Optional maxItems As Integer = Integer.MaxValue) As List(Of ThreadManager.WorkItemInfo)
        If Object.ReferenceEquals(CurrentManager, Nothing) Then ThreadHub.Init(Environment.ProcessorCount)
        Return CurrentManager.GetRunningSnapshot(maxItems)
    End Function

    Public Function ListQueued(Optional maxItems As Integer = Integer.MaxValue) As List(Of ThreadManager.WorkItemInfo)
        If Object.ReferenceEquals(CurrentManager, Nothing) Then ThreadHub.Init(Environment.ProcessorCount)
        Return CurrentManager.GetQueueSnapshot(maxItems)
    End Function

    Public Function FindWorkItem(id As Integer, ByRef info As ThreadManager.WorkItemInfo) As Boolean
        If Object.ReferenceEquals(CurrentManager, Nothing) Then ThreadHub.Init(Environment.ProcessorCount)
        Return CurrentManager.TryGetWorkItemInfo(id, info)
    End Function

End Module
