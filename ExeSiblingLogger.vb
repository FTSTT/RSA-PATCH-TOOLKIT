Imports System.IO
Imports System.Runtime.InteropServices
Imports System.Text
Imports System.Threading

Module Logger

    ' 线程安全锁
    Private ReadOnly _logLock As New Object()

    ' 记住是否启用回退以及是否已提示，以免重复
    Private _fallbackPath As String = Nothing
    Private _fallbackNotified As Boolean = False

    ' P/Invoke：检测是否有控制台窗口
    <DllImport("kernel32.dll")>
    Private Function GetConsoleWindow() As IntPtr
    End Function

    ' 写入日志到与 EXE 同名的文本文件（默认 .log）
    Public Sub WriteExeLog(message As String, Optional targetExt As String = ".log")
        Try
            Dim primaryPath As String = GetExeSiblingPath(targetExt)
            Dim pathToUse As String = If(_fallbackPath, primaryPath)

            ' 先尝试当前“已选路径”（初始为主路径；若之前回退过则为回退路径）
            If Not TryAppend(pathToUse, message) Then
                ' 如果当前路径是主路径：尝试回退
                If _fallbackPath Is Nothing Then
                    Dim fbPath As String = GetFallbackLogPath(targetExt)
                    Dim firstCreate As Boolean = Not File.Exists(fbPath)
                    If TryAppend(fbPath, BuildFallbackNotice(primaryPath) & Environment.NewLine & message) Then
                        _fallbackPath = fbPath
                        If firstCreate AndAlso Not _fallbackNotified Then
                            _fallbackNotified = True
                            NotifyFallbackOnce(fbPath)
                        End If
                        Return
                    End If
                End If
                ' 回退也失败则静默
            End If

        Catch
            ' 避免日志失败影响主流程；如需调试可改为 Throw
        End Try
    End Sub

    ' ---------- 内部实现 ----------

    Private Sub NotifyFallbackOnce(fallbackPath As String)
        Dim msg As String = CStr(Loc.T("Text.bdcd1f42", "[LOG] 无法写入应用目录，日志已回退到: ")) & fallbackPath

        ' 若有控制台：只向控制台输出一次
        If HasConsoleWindow() Then
            Console.Error.WriteLine(msg)
            Return
        End If

        ' 没有控制台：弹出非阻塞提示窗（后台线程，自动消失）
        Try
            Dim uiThread As New Thread(
                Sub()
                    Try
                        Application.EnableVisualStyles()
                        Using tip As New Form()
                            tip.Text = CStr(Loc.T("Text.fbf0f7db", "日志回退"))
                            tip.TopMost = True
                            tip.FormBorderStyle = FormBorderStyle.FixedToolWindow
                            tip.ShowInTaskbar = False
                            tip.StartPosition = FormStartPosition.CenterScreen
                            tip.Width = 520
                            tip.Height = 160

                            Dim lbl As New Label()
                            lbl.Dock = DockStyle.Fill
                            lbl.Text = CStr(Loc.T("Text.1f983e67", "无法写入应用目录，日志已回退到：")) & Environment.NewLine & fallbackPath &
                                       Environment.NewLine & Environment.NewLine & CStr(Loc.T("Text.2573822c", "（此提示仅显示一次）"))
                            lbl.TextAlign = ContentAlignment.MiddleLeft
                            lbl.Padding = New Padding(12)

                            tip.Controls.Add(lbl)

                            ' 3 秒后自动关闭（用多行 Lambda，避免“单行只能一个语句”的错误）
                            Dim t As New System.Windows.Forms.Timer()
                            AddHandler t.Tick,
                                Sub(sender As Object, e As EventArgs)
                                    t.Stop()
                                    tip.Close()
                                End Sub
                            t.Interval = 3000
                            t.Start()

                            tip.Show()
                            Application.Run(New ModelessContext(tip))
                        End Using
                    Catch
                        ' UI 提示失败时忽略，避免影响主流程
                    End Try
                End Sub
            )
            uiThread.IsBackground = True
            uiThread.SetApartmentState(ApartmentState.STA)
            uiThread.Start()
        Catch
            ' 忽略 UI 线程创建失败
        End Try
    End Sub

    Private Function HasConsoleWindow() As Boolean
        Try
            Return GetConsoleWindow() <> IntPtr.Zero
        Catch
            Return False
        End Try
    End Function

    ' ApplicationContext：确保 modeless 窗体关闭时结束消息泵
    Private NotInheritable Class ModelessContext
        Inherits ApplicationContext
        Public Sub New(form As Form)
            AddHandler form.FormClosed,
                Sub(sender As Object, e As FormClosedEventArgs)
                    Me.ExitThread()
                End Sub
        End Sub
    End Class

    Private Function TryAppend(filePath As String, message As String) As Boolean
        Dim stamp As String = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")
        Dim sep As String = New String("-"c, 64)

        Dim sb As New StringBuilder(256 + If(message?.Length, 0))
        sb.AppendLine(sep)
        sb.AppendLine(stamp)
        sb.AppendLine(message)
        sb.AppendLine(sep)

        Try
            SyncLock _logLock
                ' 注意这里显式使用 System.IO.Path，且形参名为 filePath（不是 path）
                Dim dir As String = System.IO.Path.GetDirectoryName(filePath)
                If Not String.IsNullOrEmpty(dir) Then
                    Directory.CreateDirectory(dir)
                End If

                Using sw As New StreamWriter(filePath, append:=True, encoding:=New UTF8Encoding(False))
                    sw.Write(sb.ToString())
                End Using
            End SyncLock
            Return True
        Catch
            Return False
        End Try
    End Function



    Private Function BuildFallbackNotice(primaryPath As String) As String
        Dim exe = GetEntryProcessPath()
        Dim exeName = Path.GetFileName(exe)
        Dim sb As New StringBuilder()
        sb.AppendLine(CStr(Loc.T("Text.4c8d78f4", "[LOG] 注意：应用无法写入 EXE 目录，日志已回退到可写位置。")))
        sb.AppendLine(CStr(Loc.T("Text.e5f5cfb9", "  应用      : ")) & exeName)
        sb.AppendLine(CStr(Loc.T("Text.c2cb8137", "  主日志尝试: ")) & primaryPath)
        sb.AppendLine(CStr(Loc.T("Text.b77d26c6", "  说明      : 本提示仅在第一次创建回退日志文件时记录一次。")))
        Return sb.ToString().TrimEnd()
    End Function

    Private Function GetExeSiblingPath(targetExt As String) As String
        If String.IsNullOrWhiteSpace(targetExt) Then targetExt = ".log"
        If Not targetExt.StartsWith(".") Then targetExt = "." & targetExt

        Dim exePath As String = GetEntryProcessPath()
        Dim baseNoExt As String = Path.Combine(Path.GetDirectoryName(exePath),
                                               Path.GetFileNameWithoutExtension(exePath))
        Return baseNoExt & targetExt
    End Function

    Private Function GetFallbackLogPath(targetExt As String) As String
        If String.IsNullOrWhiteSpace(targetExt) Then targetExt = ".log"
        If Not targetExt.StartsWith(".") Then targetExt = "." & targetExt

        Dim exePath As String = GetEntryProcessPath()
        Dim appName As String = Path.GetFileNameWithoutExtension(exePath)
        Dim doc As String = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
        Dim dir As String = Path.Combine(doc, appName)
        Return Path.Combine(dir, appName & targetExt)
    End Function

    Private Function GetEntryProcessPath() As String
        ' 1) 当前进程主模块（最可靠）
        Try
            Dim p As Process = Process.GetCurrentProcess()
            Dim mp = p?.MainModule
            If mp IsNot Nothing AndAlso mp.FileName IsNot Nothing AndAlso mp.FileName.Length > 0 Then
                Return mp.FileName
            End If
        Catch
            ' 某些环境可能拒绝访问 MainModule，忽略
        End Try

        ' 2) 入口程序集
        Dim entryAsm As Reflection.Assembly = Nothing
        Try
            entryAsm = Reflection.Assembly.GetEntryAssembly()
        Catch
        End Try

        If entryAsm IsNot Nothing Then
            Dim loc As String = Nothing
            Try
                loc = entryAsm.Location
            Catch
            End Try
            If loc IsNot Nothing AndAlso loc.Length > 0 Then
                Return loc
            End If
        End If

        ' 3) 基目录 + 程序集名（兜底）
        Dim baseDir As String = AppContext.BaseDirectory
        Dim asmName As String = Nothing
        Try
            If entryAsm IsNot Nothing Then
                asmName = entryAsm.GetName().Name
            End If
        Catch
        End Try

        If baseDir IsNot Nothing AndAlso baseDir.Length > 0 AndAlso asmName IsNot Nothing AndAlso asmName.Length > 0 Then
            Return System.IO.Path.Combine(baseDir, asmName & ".exe")
        End If

        ' 4) 最后兜底：当前目录
        Return System.IO.Path.Combine(Directory.GetCurrentDirectory(), "app.exe")
    End Function


End Module
