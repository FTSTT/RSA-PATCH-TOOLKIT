Imports System.Reflection

Public Module AppBrand
    Public Const AppDisplayName As String = "RSA PATCH TOOLKIT"
    Public AppAltName As String = CStr(Loc.T("Text.dceadb1b", "RSA 补丁工具箱"))
    Public Const AppGithubUrl As String = "https://ftstt.github.io/RSA-PATCH-TOOLKIT/" ' ← 修改为你的仓库地址
    Public AppCopyright As String = "© " & Date.Now.Year & " aCr/UpK"
End Module

Public Class AboutDialog
    Inherits Form

    Private ReadOnly lblTitle As Label
    Private ReadOnly lblVersion As Label
    Private ReadOnly lblDescription As Label
    Private ReadOnly lblThanks As Label
    Private ReadOnly lnkGithub As LinkLabel
    Private ReadOnly btnOK As Button
    Private ReadOnly lineTop As Label
    Private ReadOnly lineBottom As Label
    Private ReadOnly picApp As PictureBox

    Public Sub New()
        Me.Text = CStr(Loc.T("Text.da5499c5", "关于 ")) & AppDisplayName
        Me.StartPosition = FormStartPosition.CenterParent
        Me.FormBorderStyle = FormBorderStyle.FixedDialog
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.ShowInTaskbar = False
        Me.KeyPreview = True
        Me.Padding = New Padding(16)
        Me.Font = New Font("Microsoft YaHei UI", 9.0F, FontStyle.Regular, GraphicsUnit.Point, 134)
        Me.BackColor = Color.White
        Me.ClientSize = New Size(560, 380)

        lineTop = New Label() With {.AutoSize = False, .Height = 1, .Dock = DockStyle.Top, .BackColor = Color.FromArgb(230, 230, 230)}
        Me.Controls.Add(lineTop)

        picApp = New PictureBox() With {.Size = New Size(64, 64), .Location = New Point(24, 28), .SizeMode = PictureBoxSizeMode.CenterImage}
        Try
            Dim ico = Icon.ExtractAssociatedIcon(Application.ExecutablePath)
            If ico IsNot Nothing Then picApp.Image = ico.ToBitmap()
        Catch
        End Try
        Me.Controls.Add(picApp)

        lblTitle = New Label() With {.AutoSize = True, .Location = New Point(104, 28), .Font = New Font(Me.Font, FontStyle.Bold), .UseMnemonic = False}
        Me.Controls.Add(lblTitle)

        lblVersion = New Label() With {.AutoSize = True, .Location = New Point(104, 56), .UseMnemonic = False}
        Me.Controls.Add(lblVersion)

        lblDescription = New Label() With {.AutoSize = True, .Location = New Point(24, 108), .UseMnemonic = False, .TextAlign = ContentAlignment.TopLeft}
        Me.Controls.Add(lblDescription)

        lblThanks = New Label() With {.AutoSize = True, .Location = New Point(24, 0), .UseMnemonic = False, .ForeColor = Color.FromArgb(34, 139, 34), .TextAlign = ContentAlignment.TopLeft}
        Me.Controls.Add(lblThanks)

        ' 新增：GitHub 链接
        lnkGithub = New LinkLabel() With {
            .AutoSize = True,
            .Location = New Point(24, 0),
            .LinkBehavior = LinkBehavior.HoverUnderline,
            .TabStop = True
        }
        lnkGithub.Text = CStr(Loc.T("Text.c323fb41", "项目主页：")) & AppGithubUrl
        lnkGithub.Links.Clear()
        ' 将整个 URL 部分设为可点击链接
        Dim start As Integer = lnkGithub.Text.IndexOf(AppGithubUrl, StringComparison.Ordinal)
        If start >= 0 Then
            lnkGithub.Links.Add(start, AppGithubUrl.Length, AppGithubUrl)
        End If
        AddHandler lnkGithub.LinkClicked, AddressOf OnGithubLinkClicked
        Me.Controls.Add(lnkGithub)

        lineBottom = New Label() With {.AutoSize = False, .Height = 1, .Dock = DockStyle.Bottom, .BackColor = Color.FromArgb(230, 230, 230)}
        Me.Controls.Add(lineBottom)

        btnOK = New Button() With {.Text = CStr(Loc.T("Text.ff81add5", "确定(&O)")), .Size = New Size(96, 32), .Anchor = AnchorStyles.Bottom Or AnchorStyles.Right}
        Me.Controls.Add(btnOK)
        AddHandler btnOK.Click, Sub(sender, e) Me.Close()

        AddHandler Me.Load, AddressOf AboutDialog_Load
        AddHandler Me.KeyDown, AddressOf AboutDialog_KeyDown
        AddHandler Me.Resize, AddressOf AboutDialog_Resize
    End Sub

    Private Sub AboutDialog_Load(sender As Object, e As EventArgs)
        Dim asm = Assembly.GetEntryAssembly()
        Dim version As String = If(asm IsNot Nothing, asm.GetName().Version.ToString(), Application.ProductVersion)

        lblTitle.Text = AppDisplayName & " / " & AppAltName
        lblVersion.Text = CStr(Loc.T("Text.66915aa8", "版本：")) & version
        lblDescription.Text = CStr(Loc.T("Text.271daaea", "简介：RSA PATCH TOOLKIT 并不适合用于分解大数，")) &
                              CStr(Loc.T("Text.de7ac42f", "只是为了好玩 用于 半字节/1字节/ 固定CRC32补丁 辅助，")) &
                              CStr(Loc.T("Text.1d6aae92", "界面简洁、开箱即用。")) & Environment.NewLine &
                              AppCopyright
        lblThanks.Text = CStr(Loc.T("Text.83a035d4", "特别致谢：感谢 Shu Miao Duo 在生活中的帮助。"))

        LayoutDynamic()
    End Sub

    Private Sub OnGithubLinkClicked(sender As Object, e As LinkLabelLinkClickedEventArgs)
        Try
            Dim url As String = TryCast(e.Link.LinkData, String)
            If String.IsNullOrEmpty(url) Then url = AppGithubUrl
            Process.Start(New ProcessStartInfo(url) With {.UseShellExecute = True})
            CType(sender, LinkLabel).LinkVisited = True
        Catch ex As Exception
            MessageBox.Show(CStr(Loc.T("Text.9d2f6fff", "无法打开浏览器访问：")) & AppGithubUrl, CStr(Loc.T("Text.b25b7a81", "提示")), MessageBoxButtons.OK, MessageBoxIcon.Information)
        End Try
    End Sub

    Private Sub AboutDialog_KeyDown(sender As Object, e As KeyEventArgs)
        If e.KeyCode = Keys.Escape OrElse e.KeyCode = Keys.Enter Then Me.Close()
    End Sub

    Private Sub AboutDialog_Resize(sender As Object, e As EventArgs)
        LayoutDynamic()
    End Sub

    Private Sub LayoutDynamic()
        Dim innerWidth As Integer = Me.ClientSize.Width - 48
        lblDescription.MaximumSize = New Size(innerWidth, 0)
        lblThanks.MaximumSize = New Size(innerWidth, 0)
        lnkGithub.MaximumSize = New Size(innerWidth, 0)

        ' 自上而下排版
        lblThanks.Location = New Point(24, lblDescription.Bottom + 12)
        lnkGithub.Location = New Point(24, lblThanks.Bottom + 8)

        btnOK.Location = New Point(Me.ClientSize.Width - btnOK.Width - 24, Me.ClientSize.Height - btnOK.Height - 16)
    End Sub

    Public Shared Sub ShowAbout(owner As IWin32Window)
        Using dlg As New AboutDialog()
            dlg.ShowDialog(owner)
        End Using
    End Sub

End Class
