' FrmMaskEditor.Designer.vb
<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class FrmMaskEditor
    Inherits System.Windows.Forms.Form

    <System.Diagnostics.DebuggerNonUserCode()>
    Protected Overrides Sub Dispose(ByVal disposing As Boolean)
        Try
            If disposing AndAlso components IsNot Nothing Then components.Dispose()
        Finally
            MyBase.Dispose(disposing)
        End Try
    End Sub

    Private components As System.ComponentModel.IContainer

    <System.Diagnostics.DebuggerStepThrough()>
    Private Sub InitializeComponent()
        Me.txtHint = New System.Windows.Forms.TextBox()
        Me.rtbA = New System.Windows.Forms.RichTextBox()
        Me.rtbB = New HScrollRichTextBox()
        Me.btnSave = New System.Windows.Forms.Button()
        Me.btnSaveRnd = New System.Windows.Forms.Button()
        Me.btnCancel = New System.Windows.Forms.Button()
        Me.SuspendLayout()
        '
        'txtHint
        '
        Me.txtHint.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.txtHint.Location = New System.Drawing.Point(12, 12)
        Me.txtHint.Multiline = True
        Me.txtHint.Name = "txtHint"
        Me.txtHint.ReadOnly = True
        Me.txtHint.ScrollBars = System.Windows.Forms.ScrollBars.Vertical
        Me.txtHint.Size = New System.Drawing.Size(760, 96)
        Me.txtHint.TabIndex = 0
        Me.txtHint.TabStop = False
        '
        'rtbA
        '
        Me.rtbA.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.rtbA.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.rtbA.Font = New System.Drawing.Font("Consolas", 10.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point)
        Me.rtbA.Location = New System.Drawing.Point(12, 116)
        Me.rtbA.Name = "rtbA"
        Me.rtbA.ReadOnly = True
        Me.rtbA.ScrollBars = System.Windows.Forms.RichTextBoxScrollBars.None
        Me.rtbA.Size = New System.Drawing.Size(760, 28)
        Me.rtbA.TabIndex = 1
        Me.rtbA.Text = ""
        Me.rtbA.WordWrap = False
        '
        'rtbB
        '
        Me.rtbB.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.rtbB.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.rtbB.Font = New System.Drawing.Font("Consolas", 10.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point)
        Me.rtbB.ImeMode = System.Windows.Forms.ImeMode.Off
        Me.rtbB.Location = New System.Drawing.Point(12, 150)
        Me.rtbB.Name = "rtbB"
        Me.rtbB.ScrollBars = System.Windows.Forms.RichTextBoxScrollBars.Horizontal
        Me.rtbB.Size = New System.Drawing.Size(760, 32)
        Me.rtbB.TabIndex = 2
        Me.rtbB.Text = ""
        Me.rtbB.WordWrap = False
        '
        'btnSave
        '
        Me.btnSave.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.btnSave.Location = New System.Drawing.Point(440, 196)
        Me.btnSave.Name = "btnSave"
        Me.btnSave.Size = New System.Drawing.Size(104, 28)
        Me.btnSave.TabIndex = 3
        Me.btnSave.Text = CStr(Loc.T("Text.61d5a738", "保存(&S)"))
        Me.btnSave.UseVisualStyleBackColor = True
        '
        'btnSaveRnd
        '
        Me.btnSaveRnd.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.btnSaveRnd.Location = New System.Drawing.Point(550, 196)
        Me.btnSaveRnd.Name = "btnSaveRnd"
        Me.btnSaveRnd.Size = New System.Drawing.Size(152, 28)
        Me.btnSaveRnd.TabIndex = 4
        Me.btnSaveRnd.Text = CStr(Loc.T("Text.63f70133", "保存并随机填充(&R)"))
        Me.btnSaveRnd.UseVisualStyleBackColor = True
        '
        'btnCancel
        '
        Me.btnCancel.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel
        Me.btnCancel.Location = New System.Drawing.Point(708, 196)
        Me.btnCancel.Name = "btnCancel"
        Me.btnCancel.Size = New System.Drawing.Size(64, 28)
        Me.btnCancel.TabIndex = 5
        Me.btnCancel.Text = CStr(Loc.T("Text.8f977d94", "取消(&C)"))
        Me.btnCancel.UseVisualStyleBackColor = True
        '
        'FrmMaskEditor
        '
        Me.AcceptButton = Me.btnSave
        Me.AutoScaleDimensions = New System.Drawing.SizeF(7.0!, 15.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.CancelButton = Me.btnCancel
        Me.ClientSize = New System.Drawing.Size(784, 236)
        Me.Controls.Add(Me.btnCancel)
        Me.Controls.Add(Me.btnSaveRnd)
        Me.Controls.Add(Me.btnSave)
        Me.Controls.Add(Me.rtbB)
        Me.Controls.Add(Me.rtbA)
        Me.Controls.Add(Me.txtHint)
        Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.Name = "FrmMaskEditor"
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent
        Me.Text = CStr(Loc.T("Text.d77fc83a", "掩码"))
        Me.ResumeLayout(False)
        Me.PerformLayout()
    End Sub

    Friend WithEvents txtHint As System.Windows.Forms.TextBox
    Friend WithEvents rtbA As System.Windows.Forms.RichTextBox
    Friend WithEvents rtbB As HScrollRichTextBox
    Friend WithEvents btnSave As System.Windows.Forms.Button
    Friend WithEvents btnSaveRnd As System.Windows.Forms.Button
    Friend WithEvents btnCancel As System.Windows.Forms.Button
End Class
