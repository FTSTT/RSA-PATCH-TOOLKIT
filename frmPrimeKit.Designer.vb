<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class frmPrimeKit
    Inherits System.Windows.Forms.Form

    'Form overrides dispose to clean up the component list.
    <System.Diagnostics.DebuggerNonUserCode()>
    Protected Overrides Sub Dispose(disposing As Boolean)
        Try
            If disposing AndAlso components IsNot Nothing Then
                components.Dispose()
            End If
        Finally
            MyBase.Dispose(disposing)
        End Try
    End Sub

    'Required by the Windows Form Designer
    Private components As System.ComponentModel.IContainer

    'NOTE: The following procedure is required by the Windows Form Designer
    'It can be modified using the Windows Form Designer.  
    'Do not modify it using the code editor.
    <System.Diagnostics.DebuggerStepThrough()>
    Private Sub InitializeComponent()
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(frmPrimeKit))
        TextN = New TextBox()
        ComboFormat = New ComboBox()
        ButtonPaste = New Button()
        ButtonReverse = New Button()
        ButtonIsHexUpper = New Button()
        ButtonIsDecimal = New Button()
        ButtonIsBase64 = New Button()
        LabelSetFormat = New Label()
        LabelConvertFormat = New Label()
        PanelOption = New Panel()
        ComboBoxLanguage = New ComboBox()
        LabelLanguage = New Label()
        NumericUpDownTimeOutMs = New NumericUpDown()
        LabelTimeOutMs = New Label()
        NumericUpDownResultCount = New NumericUpDown()
        LabelResultCount = New Label()
        CheckBoxWriteResultToLog = New CheckBox()
        NumericUpDownSmallCurve = New NumericUpDown()
        CheckBoxSmallCurve = New CheckBox()
        ComboBoxSmallPrimes = New ComboBox()
        CheckBoxSmallPrimes = New CheckBox()
        NumericUpDownBrent = New NumericUpDown()
        CheckBoxBrent = New CheckBox()
        CheckBoxRsaTest = New CheckBox()
        CheckBoxDryRun = New CheckBox()
        CheckBoxIsHexString = New CheckBox()
        TabControl1 = New TabControl()
        TabPageByte = New TabPage()
        CheckBoxNibble = New CheckBox()
        LabelReplacePosition = New Label()
        NumericUpDownReplacePosition = New NumericUpDown()
        ComboBoxReplacePosition = New ComboBox()
        TabPageCrc = New TabPage()
        LabelNeedChars = New Label()
        NumericUpDownNeedChars = New NumericUpDown()
        btnEditMask = New Button()
        ComboBoxAllowedChars = New ComboBox()
        LabelAllowedChars = New Label()
        CheckBoxUnicode = New CheckBox()
        TextBoxFrozenRanges = New TextBox()
        CheckBoxAcceptPrimeN = New CheckBox()
        LabelThreads = New Label()
        NumericUpDownThreads = New NumericUpDown()
        TextE = New TextBox()
        LabelE = New Label()
        ButtonTry = New Button()
        RichTextLog = New RichTextBox()
        ButtonAbout = New Button()
        ButtonClearLog = New Button()
        ButtonHelp = New Button()
        ButtonIsHexlower = New Button()
        ButtonStop = New Button()
        LabelInfo = New Label()
        PanelOption.SuspendLayout()
        CType(NumericUpDownTimeOutMs, ComponentModel.ISupportInitialize).BeginInit()
        CType(NumericUpDownResultCount, ComponentModel.ISupportInitialize).BeginInit()
        CType(NumericUpDownSmallCurve, ComponentModel.ISupportInitialize).BeginInit()
        CType(NumericUpDownBrent, ComponentModel.ISupportInitialize).BeginInit()
        TabControl1.SuspendLayout()
        TabPageByte.SuspendLayout()
        CType(NumericUpDownReplacePosition, ComponentModel.ISupportInitialize).BeginInit()
        TabPageCrc.SuspendLayout()
        CType(NumericUpDownNeedChars, ComponentModel.ISupportInitialize).BeginInit()
        CType(NumericUpDownThreads, ComponentModel.ISupportInitialize).BeginInit()
        SuspendLayout()
        ' 
        ' TextN
        ' 
        TextN.BackColor = SystemColors.AppWorkspace
        TextN.Location = New Point(12, 21)
        TextN.Multiline = True
        TextN.Name = "TextN"
        TextN.ReadOnly = True
        TextN.ScrollBars = ScrollBars.Vertical
        TextN.Size = New Size(815, 91)
        TextN.TabIndex = 0
        ' 
        ' ComboFormat
        ' 
        ComboFormat.DropDownStyle = ComboBoxStyle.DropDownList
        ComboFormat.FormattingEnabled = True
        ComboFormat.Items.AddRange(New Object() {"HEX", "hex", "Decimal", "Base64", "unknown"})
        ComboFormat.Location = New Point(843, 80)
        ComboFormat.Name = "ComboFormat"
        ComboFormat.Size = New Size(90, 32)
        ComboFormat.TabIndex = 1
        ' 
        ' ButtonPaste
        ' 
        ButtonPaste.Location = New Point(843, 21)
        ButtonPaste.Name = "ButtonPaste"
        ButtonPaste.Size = New Size(90, 53)
        ButtonPaste.TabIndex = 2
        ButtonPaste.Text = "粘贴[&P]"
        ButtonPaste.UseVisualStyleBackColor = True
        ' 
        ' ButtonReverse
        ' 
        ButtonReverse.Enabled = False
        ButtonReverse.Location = New Point(939, 21)
        ButtonReverse.Name = "ButtonReverse"
        ButtonReverse.Size = New Size(51, 92)
        ButtonReverse.TabIndex = 3
        ButtonReverse.Text = "翻转[&R]"
        ButtonReverse.UseVisualStyleBackColor = True
        ' 
        ' ButtonIsHexUpper
        ' 
        ButtonIsHexUpper.Location = New Point(211, 133)
        ButtonIsHexUpper.Name = "ButtonIsHexUpper"
        ButtonIsHexUpper.Size = New Size(77, 34)
        ButtonIsHexUpper.TabIndex = 4
        ButtonIsHexUpper.Text = "isHEX"
        ButtonIsHexUpper.UseVisualStyleBackColor = True
        ' 
        ' ButtonIsDecimal
        ' 
        ButtonIsDecimal.Location = New Point(401, 133)
        ButtonIsDecimal.Name = "ButtonIsDecimal"
        ButtonIsDecimal.Size = New Size(112, 34)
        ButtonIsDecimal.TabIndex = 5
        ButtonIsDecimal.Text = "isDecimal"
        ButtonIsDecimal.UseVisualStyleBackColor = True
        ' 
        ' ButtonIsBase64
        ' 
        ButtonIsBase64.Location = New Point(531, 133)
        ButtonIsBase64.Name = "ButtonIsBase64"
        ButtonIsBase64.Size = New Size(112, 34)
        ButtonIsBase64.TabIndex = 6
        ButtonIsBase64.Text = "isBase64"
        ButtonIsBase64.UseVisualStyleBackColor = True
        ' 
        ' LabelSetFormat
        ' 
        LabelSetFormat.AutoSize = True
        LabelSetFormat.Location = New Point(12, 138)
        LabelSetFormat.Name = "LabelSetFormat"
        LabelSetFormat.Size = New Size(193, 24)
        LabelSetFormat.TabIndex = 7
        LabelSetFormat.Text = "更正粘贴数据的格式->"
        ' 
        ' LabelConvertFormat
        ' 
        LabelConvertFormat.AutoSize = True
        LabelConvertFormat.Location = New Point(749, 138)
        LabelConvertFormat.Name = "LabelConvertFormat"
        LabelConvertFormat.Size = New Size(241, 24)
        LabelConvertFormat.TabIndex = 8
        LabelConvertFormat.Text = "点击下拉框选项转换N的格式"
        ' 
        ' PanelOption
        ' 
        PanelOption.BackColor = SystemColors.ActiveCaption
        PanelOption.Controls.Add(ComboBoxLanguage)
        PanelOption.Controls.Add(LabelLanguage)
        PanelOption.Controls.Add(NumericUpDownTimeOutMs)
        PanelOption.Controls.Add(LabelTimeOutMs)
        PanelOption.Controls.Add(NumericUpDownResultCount)
        PanelOption.Controls.Add(LabelResultCount)
        PanelOption.Controls.Add(CheckBoxWriteResultToLog)
        PanelOption.Controls.Add(NumericUpDownSmallCurve)
        PanelOption.Controls.Add(CheckBoxSmallCurve)
        PanelOption.Controls.Add(ComboBoxSmallPrimes)
        PanelOption.Controls.Add(CheckBoxSmallPrimes)
        PanelOption.Controls.Add(NumericUpDownBrent)
        PanelOption.Controls.Add(CheckBoxBrent)
        PanelOption.Controls.Add(CheckBoxRsaTest)
        PanelOption.Controls.Add(CheckBoxDryRun)
        PanelOption.Controls.Add(CheckBoxIsHexString)
        PanelOption.Controls.Add(TabControl1)
        PanelOption.Controls.Add(CheckBoxAcceptPrimeN)
        PanelOption.Controls.Add(LabelThreads)
        PanelOption.Controls.Add(NumericUpDownThreads)
        PanelOption.Controls.Add(TextE)
        PanelOption.Controls.Add(LabelE)
        PanelOption.Location = New Point(12, 192)
        PanelOption.Name = "PanelOption"
        PanelOption.Size = New Size(815, 274)
        PanelOption.TabIndex = 9
        ' 
        ' ComboBoxLanguage
        ' 
        ComboBoxLanguage.DropDownStyle = ComboBoxStyle.DropDownList
        ComboBoxLanguage.FormattingEnabled = True
        ComboBoxLanguage.Location = New Point(488, 232)
        ComboBoxLanguage.Name = "ComboBoxLanguage"
        ComboBoxLanguage.Size = New Size(158, 32)
        ComboBoxLanguage.TabIndex = 32
        ' 
        ' LabelLanguage
        ' 
        LabelLanguage.AutoSize = True
        LabelLanguage.Location = New Point(361, 237)
        LabelLanguage.Name = "LabelLanguage"
        LabelLanguage.Size = New Size(113, 24)
        LabelLanguage.TabIndex = 31
        LabelLanguage.Text = "Language："
        ' 
        ' NumericUpDownTimeOutMs
        ' 
        NumericUpDownTimeOutMs.Location = New Point(488, 199)
        NumericUpDownTimeOutMs.Maximum = New Decimal(New Integer() {600000, 0, 0, 0})
        NumericUpDownTimeOutMs.Minimum = New Decimal(New Integer() {30000, 0, 0, 0})
        NumericUpDownTimeOutMs.Name = "NumericUpDownTimeOutMs"
        NumericUpDownTimeOutMs.Size = New Size(104, 30)
        NumericUpDownTimeOutMs.TabIndex = 30
        NumericUpDownTimeOutMs.Value = New Decimal(New Integer() {180000, 0, 0, 0})
        ' 
        ' LabelTimeOutMs
        ' 
        LabelTimeOutMs.AutoSize = True
        LabelTimeOutMs.Location = New Point(358, 201)
        LabelTimeOutMs.Name = "LabelTimeOutMs"
        LabelTimeOutMs.Size = New Size(137, 24)
        LabelTimeOutMs.TabIndex = 29
        LabelTimeOutMs.Text = "分解超时(ms)："
        ' 
        ' NumericUpDownResultCount
        ' 
        NumericUpDownResultCount.Location = New Point(488, 164)
        NumericUpDownResultCount.Minimum = New Decimal(New Integer() {1, 0, 0, 0})
        NumericUpDownResultCount.Name = "NumericUpDownResultCount"
        NumericUpDownResultCount.Size = New Size(104, 30)
        NumericUpDownResultCount.TabIndex = 28
        NumericUpDownResultCount.Value = New Decimal(New Integer() {5, 0, 0, 0})
        ' 
        ' LabelResultCount
        ' 
        LabelResultCount.AutoSize = True
        LabelResultCount.Location = New Point(358, 170)
        LabelResultCount.Name = "LabelResultCount"
        LabelResultCount.Size = New Size(136, 24)
        LabelResultCount.TabIndex = 27
        LabelResultCount.Text = "结果数量下限："
        ' 
        ' CheckBoxWriteResultToLog
        ' 
        CheckBoxWriteResultToLog.AutoSize = True
        CheckBoxWriteResultToLog.Checked = True
        CheckBoxWriteResultToLog.CheckState = CheckState.Checked
        CheckBoxWriteResultToLog.Location = New Point(614, 198)
        CheckBoxWriteResultToLog.Name = "CheckBoxWriteResultToLog"
        CheckBoxWriteResultToLog.Size = New Size(144, 28)
        CheckBoxWriteResultToLog.TabIndex = 26
        CheckBoxWriteResultToLog.Text = "结果记录日志"
        CheckBoxWriteResultToLog.UseVisualStyleBackColor = True
        ' 
        ' NumericUpDownSmallCurve
        ' 
        NumericUpDownSmallCurve.Location = New Point(199, 233)
        NumericUpDownSmallCurve.Maximum = New Decimal(New Integer() {512, 0, 0, 0})
        NumericUpDownSmallCurve.Minimum = New Decimal(New Integer() {1, 0, 0, 0})
        NumericUpDownSmallCurve.Name = "NumericUpDownSmallCurve"
        NumericUpDownSmallCurve.Size = New Size(142, 30)
        NumericUpDownSmallCurve.TabIndex = 25
        NumericUpDownSmallCurve.Value = New Decimal(New Integer() {33, 0, 0, 0})
        ' 
        ' CheckBoxSmallCurve
        ' 
        CheckBoxSmallCurve.AutoSize = True
        CheckBoxSmallCurve.Checked = True
        CheckBoxSmallCurve.CheckState = CheckState.Checked
        CheckBoxSmallCurve.Location = New Point(22, 232)
        CheckBoxSmallCurve.Name = "CheckBoxSmallCurve"
        CheckBoxSmallCurve.Size = New Size(185, 28)
        CheckBoxSmallCurve.TabIndex = 24
        CheckBoxSmallCurve.Text = "尝试小曲线 条数："
        CheckBoxSmallCurve.UseVisualStyleBackColor = True
        ' 
        ' ComboBoxSmallPrimes
        ' 
        ComboBoxSmallPrimes.FormattingEnabled = True
        ComboBoxSmallPrimes.Items.AddRange(New Object() {"2-256", "2-4096", "2-65536", "2-1048576", "2-16777216", "2-268435456"})
        ComboBoxSmallPrimes.Location = New Point(199, 198)
        ComboBoxSmallPrimes.Name = "ComboBoxSmallPrimes"
        ComboBoxSmallPrimes.Size = New Size(142, 32)
        ComboBoxSmallPrimes.TabIndex = 23
        ComboBoxSmallPrimes.Text = "2-16777216"
        ' 
        ' CheckBoxSmallPrimes
        ' 
        CheckBoxSmallPrimes.AutoSize = True
        CheckBoxSmallPrimes.Checked = True
        CheckBoxSmallPrimes.CheckState = CheckState.Checked
        CheckBoxSmallPrimes.Location = New Point(22, 200)
        CheckBoxSmallPrimes.Name = "CheckBoxSmallPrimes"
        CheckBoxSmallPrimes.Size = New Size(185, 28)
        CheckBoxSmallPrimes.TabIndex = 22
        CheckBoxSmallPrimes.Text = "小素数试除 范围："
        CheckBoxSmallPrimes.UseVisualStyleBackColor = True
        ' 
        ' NumericUpDownBrent
        ' 
        NumericUpDownBrent.Location = New Point(199, 164)
        NumericUpDownBrent.Maximum = New Decimal(New Integer() {10000000, 0, 0, 0})
        NumericUpDownBrent.Minimum = New Decimal(New Integer() {1, 0, 0, 0})
        NumericUpDownBrent.Name = "NumericUpDownBrent"
        NumericUpDownBrent.Size = New Size(142, 30)
        NumericUpDownBrent.TabIndex = 21
        NumericUpDownBrent.Value = New Decimal(New Integer() {100000, 0, 0, 0})
        ' 
        ' CheckBoxBrent
        ' 
        CheckBoxBrent.AutoSize = True
        CheckBoxBrent.Checked = True
        CheckBoxBrent.CheckState = CheckState.Checked
        CheckBoxBrent.Location = New Point(22, 166)
        CheckBoxBrent.Name = "CheckBoxBrent"
        CheckBoxBrent.Size = New Size(190, 28)
        CheckBoxBrent.TabIndex = 20
        CheckBoxBrent.Text = "Brent's 迭代次数："
        CheckBoxBrent.UseVisualStyleBackColor = True
        ' 
        ' CheckBoxRsaTest
        ' 
        CheckBoxRsaTest.AutoSize = True
        CheckBoxRsaTest.Location = New Point(358, 131)
        CheckBoxRsaTest.Name = "CheckBoxRsaTest"
        CheckBoxRsaTest.Size = New Size(250, 28)
        CheckBoxRsaTest.TabIndex = 19
        CheckBoxRsaTest.Text = "对N D进行随机加解密验证"
        CheckBoxRsaTest.UseVisualStyleBackColor = True
        ' 
        ' CheckBoxDryRun
        ' 
        CheckBoxDryRun.AutoSize = True
        CheckBoxDryRun.Location = New Point(679, 232)
        CheckBoxDryRun.Name = "CheckBoxDryRun"
        CheckBoxDryRun.Size = New Size(106, 28)
        CheckBoxDryRun.TabIndex = 18
        CheckBoxDryRun.Text = "Dry Run"
        CheckBoxDryRun.UseVisualStyleBackColor = True
        ' 
        ' CheckBoxIsHexString
        ' 
        CheckBoxIsHexString.AutoSize = True
        CheckBoxIsHexString.Location = New Point(20, 131)
        CheckBoxIsHexString.Name = "CheckBoxIsHexString"
        CheckBoxIsHexString.Size = New Size(198, 28)
        CheckBoxIsHexString.TabIndex = 17
        CheckBoxIsHexString.Text = "输入的HEX是字符串"
        CheckBoxIsHexString.UseVisualStyleBackColor = True
        ' 
        ' TabControl1
        ' 
        TabControl1.Controls.Add(TabPageByte)
        TabControl1.Controls.Add(TabPageCrc)
        TabControl1.Location = New Point(3, 3)
        TabControl1.Name = "TabControl1"
        TabControl1.SelectedIndex = 0
        TabControl1.Size = New Size(809, 122)
        TabControl1.TabIndex = 16
        ' 
        ' TabPageByte
        ' 
        TabPageByte.BackColor = SystemColors.ActiveCaption
        TabPageByte.Controls.Add(CheckBoxNibble)
        TabPageByte.Controls.Add(LabelReplacePosition)
        TabPageByte.Controls.Add(NumericUpDownReplacePosition)
        TabPageByte.Controls.Add(ComboBoxReplacePosition)
        TabPageByte.Location = New Point(4, 33)
        TabPageByte.Name = "TabPageByte"
        TabPageByte.Padding = New Padding(3)
        TabPageByte.Size = New Size(801, 85)
        TabPageByte.TabIndex = 0
        TabPageByte.Text = "微调字节模式"
        ' 
        ' CheckBoxNibble
        ' 
        CheckBoxNibble.AutoSize = True
        CheckBoxNibble.Location = New Point(13, 13)
        CheckBoxNibble.Name = "CheckBoxNibble"
        CheckBoxNibble.Size = New Size(180, 28)
        CheckBoxNibble.TabIndex = 0
        CheckBoxNibble.Text = "仅尝试半字节替换"
        CheckBoxNibble.UseVisualStyleBackColor = True
        ' 
        ' LabelReplacePosition
        ' 
        LabelReplacePosition.AutoSize = True
        LabelReplacePosition.Location = New Point(221, 14)
        LabelReplacePosition.Name = "LabelReplacePosition"
        LabelReplacePosition.Size = New Size(218, 24)
        LabelReplacePosition.TabIndex = 1
        LabelReplacePosition.Text = "尝试替换位置(Decimal)："
        ' 
        ' NumericUpDownReplacePosition
        ' 
        NumericUpDownReplacePosition.Location = New Point(440, 13)
        NumericUpDownReplacePosition.Maximum = New Decimal(New Integer() {128, 0, 0, 0})
        NumericUpDownReplacePosition.Minimum = New Decimal(New Integer() {1, 0, 0, 0})
        NumericUpDownReplacePosition.Name = "NumericUpDownReplacePosition"
        NumericUpDownReplacePosition.Size = New Size(106, 30)
        NumericUpDownReplacePosition.TabIndex = 2
        NumericUpDownReplacePosition.Value = New Decimal(New Integer() {1, 0, 0, 0})
        ' 
        ' ComboBoxReplacePosition
        ' 
        ComboBoxReplacePosition.DropDownStyle = ComboBoxStyle.DropDownList
        ComboBoxReplacePosition.FormattingEnabled = True
        ComboBoxReplacePosition.Items.AddRange(New Object() {"仅尝试当前字符", "自动向左侧尝试", "自动向右侧尝试"})
        ComboBoxReplacePosition.Location = New Point(596, 11)
        ComboBoxReplacePosition.Name = "ComboBoxReplacePosition"
        ComboBoxReplacePosition.Size = New Size(182, 32)
        ComboBoxReplacePosition.TabIndex = 3
        ' 
        ' TabPageCrc
        ' 
        TabPageCrc.BackColor = SystemColors.ActiveCaption
        TabPageCrc.Controls.Add(LabelNeedChars)
        TabPageCrc.Controls.Add(NumericUpDownNeedChars)
        TabPageCrc.Controls.Add(btnEditMask)
        TabPageCrc.Controls.Add(ComboBoxAllowedChars)
        TabPageCrc.Controls.Add(LabelAllowedChars)
        TabPageCrc.Controls.Add(CheckBoxUnicode)
        TabPageCrc.Controls.Add(TextBoxFrozenRanges)
        TabPageCrc.Location = New Point(4, 33)
        TabPageCrc.Name = "TabPageCrc"
        TabPageCrc.Padding = New Padding(3)
        TabPageCrc.Size = New Size(801, 85)
        TabPageCrc.TabIndex = 1
        TabPageCrc.Text = "固定CRC32模式"
        ' 
        ' LabelNeedChars
        ' 
        LabelNeedChars.AutoSize = True
        LabelNeedChars.Location = New Point(13, 51)
        LabelNeedChars.Name = "LabelNeedChars"
        LabelNeedChars.Size = New Size(334, 24)
        LabelNeedChars.TabIndex = 6
        LabelNeedChars.Text = "字符串类型可修改字节数（碰撞空间）："
        ' 
        ' NumericUpDownNeedChars
        ' 
        NumericUpDownNeedChars.Location = New Point(353, 49)
        NumericUpDownNeedChars.Maximum = New Decimal(New Integer() {13, 0, 0, 0})
        NumericUpDownNeedChars.Minimum = New Decimal(New Integer() {7, 0, 0, 0})
        NumericUpDownNeedChars.Name = "NumericUpDownNeedChars"
        NumericUpDownNeedChars.Size = New Size(86, 30)
        NumericUpDownNeedChars.TabIndex = 5
        NumericUpDownNeedChars.Value = New Decimal(New Integer() {7, 0, 0, 0})
        ' 
        ' btnEditMask
        ' 
        btnEditMask.Location = New Point(705, 11)
        btnEditMask.Name = "btnEditMask"
        btnEditMask.Size = New Size(90, 34)
        btnEditMask.TabIndex = 4
        btnEditMask.Text = "掩码[&M]"
        btnEditMask.UseVisualStyleBackColor = True
        ' 
        ' ComboBoxAllowedChars
        ' 
        ComboBoxAllowedChars.FormattingEnabled = True
        ComboBoxAllowedChars.Items.AddRange(New Object() {"0123456789", "0123456789abcdef", "0123456789ABCDEF", "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/"})
        ComboBoxAllowedChars.Location = New Point(443, 13)
        ComboBoxAllowedChars.Name = "ComboBoxAllowedChars"
        ComboBoxAllowedChars.Size = New Size(256, 32)
        ComboBoxAllowedChars.TabIndex = 3
        ' 
        ' LabelAllowedChars
        ' 
        LabelAllowedChars.AutoSize = True
        LabelAllowedChars.Location = New Point(350, 16)
        LabelAllowedChars.Name = "LabelAllowedChars"
        LabelAllowedChars.Size = New Size(100, 24)
        LabelAllowedChars.TabIndex = 2
        LabelAllowedChars.Text = "允许字符："
        ' 
        ' CheckBoxUnicode
        ' 
        CheckBoxUnicode.AutoSize = True
        CheckBoxUnicode.Location = New Point(240, 15)
        CheckBoxUnicode.Name = "CheckBoxUnicode"
        CheckBoxUnicode.Size = New Size(107, 28)
        CheckBoxUnicode.TabIndex = 1
        CheckBoxUnicode.Text = "Unicode"
        CheckBoxUnicode.UseVisualStyleBackColor = True
        ' 
        ' TextBoxFrozenRanges
        ' 
        TextBoxFrozenRanges.Location = New Point(13, 13)
        TextBoxFrozenRanges.Name = "TextBoxFrozenRanges"
        TextBoxFrozenRanges.PlaceholderText = "不可改变区域 1-8 6-20"
        TextBoxFrozenRanges.Size = New Size(211, 30)
        TextBoxFrozenRanges.TabIndex = 0
        ' 
        ' CheckBoxAcceptPrimeN
        ' 
        CheckBoxAcceptPrimeN.AutoSize = True
        CheckBoxAcceptPrimeN.Checked = True
        CheckBoxAcceptPrimeN.CheckState = CheckState.Checked
        CheckBoxAcceptPrimeN.Location = New Point(228, 131)
        CheckBoxAcceptPrimeN.Name = "CheckBoxAcceptPrimeN"
        CheckBoxAcceptPrimeN.Size = New Size(123, 28)
        CheckBoxAcceptPrimeN.TabIndex = 12
        CheckBoxAcceptPrimeN.Text = "接受素数N"
        CheckBoxAcceptPrimeN.UseVisualStyleBackColor = True
        ' 
        ' LabelThreads
        ' 
        LabelThreads.AutoSize = True
        LabelThreads.Location = New Point(614, 170)
        LabelThreads.Name = "LabelThreads"
        LabelThreads.Size = New Size(82, 24)
        LabelThreads.TabIndex = 11
        LabelThreads.Text = "线程数："
        ' 
        ' NumericUpDownThreads
        ' 
        NumericUpDownThreads.Location = New Point(702, 168)
        NumericUpDownThreads.Name = "NumericUpDownThreads"
        NumericUpDownThreads.Size = New Size(83, 30)
        NumericUpDownThreads.TabIndex = 10
        ' 
        ' TextE
        ' 
        TextE.Location = New Point(690, 132)
        TextE.Name = "TextE"
        TextE.Size = New Size(95, 30)
        TextE.TabIndex = 6
        TextE.Text = "10001"
        ' 
        ' LabelE
        ' 
        LabelE.AutoSize = True
        LabelE.Location = New Point(614, 132)
        LabelE.Name = "LabelE"
        LabelE.Size = New Size(85, 24)
        LabelE.TabIndex = 5
        LabelE.Text = "e (hex)："
        ' 
        ' ButtonTry
        ' 
        ButtonTry.Location = New Point(841, 192)
        ButtonTry.Name = "ButtonTry"
        ButtonTry.Size = New Size(149, 180)
        ButtonTry.TabIndex = 10
        ButtonTry.Text = "开始[&T]"
        ButtonTry.UseVisualStyleBackColor = True
        ' 
        ' RichTextLog
        ' 
        RichTextLog.BorderStyle = BorderStyle.None
        RichTextLog.Font = New Font("Courier New", 10F)
        RichTextLog.Location = New Point(12, 486)
        RichTextLog.Name = "RichTextLog"
        RichTextLog.ScrollBars = RichTextBoxScrollBars.ForcedVertical
        RichTextLog.Size = New Size(978, 416)
        RichTextLog.TabIndex = 11
        RichTextLog.Text = ""
        ' 
        ' ButtonAbout
        ' 
        ButtonAbout.Location = New Point(892, 924)
        ButtonAbout.Name = "ButtonAbout"
        ButtonAbout.Size = New Size(98, 32)
        ButtonAbout.TabIndex = 12
        ButtonAbout.Text = "关于[&A]"
        ButtonAbout.UseVisualStyleBackColor = True
        ' 
        ' ButtonClearLog
        ' 
        ButtonClearLog.Location = New Point(614, 924)
        ButtonClearLog.Name = "ButtonClearLog"
        ButtonClearLog.Size = New Size(137, 32)
        ButtonClearLog.TabIndex = 13
        ButtonClearLog.Text = "清空日志区[&C]"
        ButtonClearLog.UseVisualStyleBackColor = True
        ' 
        ' ButtonHelp
        ' 
        ButtonHelp.Location = New Point(774, 924)
        ButtonHelp.Name = "ButtonHelp"
        ButtonHelp.Size = New Size(98, 32)
        ButtonHelp.TabIndex = 14
        ButtonHelp.Text = "帮助[&H]"
        ButtonHelp.UseVisualStyleBackColor = True
        ' 
        ' ButtonIsHexlower
        ' 
        ButtonIsHexlower.Location = New Point(306, 133)
        ButtonIsHexlower.Name = "ButtonIsHexlower"
        ButtonIsHexlower.Size = New Size(77, 34)
        ButtonIsHexlower.TabIndex = 15
        ButtonIsHexlower.Text = "ishex"
        ButtonIsHexlower.UseVisualStyleBackColor = True
        ' 
        ' ButtonStop
        ' 
        ButtonStop.Location = New Point(843, 388)
        ButtonStop.Name = "ButtonStop"
        ButtonStop.Size = New Size(147, 78)
        ButtonStop.TabIndex = 16
        ButtonStop.Text = "停止[&S]"
        ButtonStop.UseVisualStyleBackColor = True
        ' 
        ' LabelInfo
        ' 
        LabelInfo.AutoSize = True
        LabelInfo.ForeColor = SystemColors.Highlight
        LabelInfo.Location = New Point(12, 924)
        LabelInfo.Name = "LabelInfo"
        LabelInfo.Size = New Size(148, 24)
        LabelInfo.TabIndex = 17
        LabelInfo.Text = "ThreadManager"
        ' 
        ' frmPrimeKit
        ' 
        AutoScaleDimensions = New SizeF(11F, 24F)
        AutoScaleMode = AutoScaleMode.Font
        ClientSize = New Size(1002, 968)
        Controls.Add(LabelInfo)
        Controls.Add(ButtonStop)
        Controls.Add(ButtonIsHexlower)
        Controls.Add(ButtonHelp)
        Controls.Add(ButtonClearLog)
        Controls.Add(ButtonAbout)
        Controls.Add(RichTextLog)
        Controls.Add(ButtonTry)
        Controls.Add(PanelOption)
        Controls.Add(LabelConvertFormat)
        Controls.Add(LabelSetFormat)
        Controls.Add(ButtonIsBase64)
        Controls.Add(ButtonIsDecimal)
        Controls.Add(ButtonIsHexUpper)
        Controls.Add(ButtonReverse)
        Controls.Add(ButtonPaste)
        Controls.Add(ComboFormat)
        Controls.Add(TextN)
        Icon = CType(resources.GetObject("$this.Icon"), Icon)
        Name = "frmPrimeKit"
        Text = "RSA 补丁工具箱   半字节/1字节/ 固定CRC32 补丁辅助工具"
        PanelOption.ResumeLayout(False)
        PanelOption.PerformLayout()
        CType(NumericUpDownTimeOutMs, ComponentModel.ISupportInitialize).EndInit()
        CType(NumericUpDownResultCount, ComponentModel.ISupportInitialize).EndInit()
        CType(NumericUpDownSmallCurve, ComponentModel.ISupportInitialize).EndInit()
        CType(NumericUpDownBrent, ComponentModel.ISupportInitialize).EndInit()
        TabControl1.ResumeLayout(False)
        TabPageByte.ResumeLayout(False)
        TabPageByte.PerformLayout()
        CType(NumericUpDownReplacePosition, ComponentModel.ISupportInitialize).EndInit()
        TabPageCrc.ResumeLayout(False)
        TabPageCrc.PerformLayout()
        CType(NumericUpDownNeedChars, ComponentModel.ISupportInitialize).EndInit()
        CType(NumericUpDownThreads, ComponentModel.ISupportInitialize).EndInit()
        ResumeLayout(False)
        PerformLayout()
    End Sub

    Friend WithEvents TextN As TextBox
    Friend WithEvents ComboFormat As ComboBox
    Friend WithEvents ButtonPaste As Button
    Friend WithEvents ButtonReverse As Button
    Friend WithEvents ButtonIsHexUpper As Button
    Friend WithEvents ButtonIsDecimal As Button
    Friend WithEvents ButtonIsBase64 As Button
    Friend WithEvents LabelSetFormat As Label
    Friend WithEvents LabelConvertFormat As Label
    Friend WithEvents PanelOption As Panel
    Friend WithEvents ButtonTry As Button
    Friend WithEvents RichTextLog As RichTextBox
    Friend WithEvents ButtonAbout As Button
    Friend WithEvents ButtonClearLog As Button
    Friend WithEvents ButtonHelp As Button
    Friend WithEvents CheckBoxNibble As CheckBox
    Friend WithEvents LabelReplacePosition As Label
    Friend WithEvents NumericUpDownReplacePosition As NumericUpDown
    Friend WithEvents ComboBoxReplacePosition As ComboBox
    Friend WithEvents LabelE As Label
    Friend WithEvents TextE As TextBox
    Friend WithEvents LabelThreads As Label
    Friend WithEvents NumericUpDownThreads As NumericUpDown
    Friend WithEvents CheckBoxAcceptPrimeN As CheckBox
    Friend WithEvents ButtonIsHexlower As Button
    Friend WithEvents TabControl1 As TabControl
    Friend WithEvents TabPageByte As TabPage
    Friend WithEvents TabPageCrc As TabPage
    Friend WithEvents TextBoxFrozenRanges As TextBox
    Friend WithEvents CheckBoxIsHexString As CheckBox
    Friend WithEvents CheckBoxUnicode As CheckBox
    Friend WithEvents LabelAllowedChars As Label
    Friend WithEvents CheckBoxDryRun As CheckBox
    Friend WithEvents ComboBoxAllowedChars As ComboBox
    Friend WithEvents btnEditMask As Button
    Friend WithEvents NumericUpDownNeedChars As NumericUpDown
    Friend WithEvents LabelNeedChars As Label
    Friend WithEvents ButtonStop As Button
    Friend WithEvents CheckBoxRsaTest As CheckBox
    Friend WithEvents NumericUpDownBrent As NumericUpDown
    Friend WithEvents CheckBoxBrent As CheckBox
    Friend WithEvents ComboBoxSmallPrimes As ComboBox
    Friend WithEvents CheckBoxSmallPrimes As CheckBox
    Friend WithEvents NumericUpDownSmallCurve As NumericUpDown
    Friend WithEvents CheckBoxSmallCurve As CheckBox
    Friend WithEvents CheckBoxWriteResultToLog As CheckBox
    Friend WithEvents LabelResultCount As Label
    Friend WithEvents LabelInfo As Label
    Friend WithEvents NumericUpDownResultCount As NumericUpDown
    Friend WithEvents NumericUpDownTimeOutMs As NumericUpDown
    Friend WithEvents LabelTimeOutMs As Label
    Friend WithEvents LabelLanguage As Label
    Friend WithEvents ComboBoxLanguage As ComboBox

End Class
