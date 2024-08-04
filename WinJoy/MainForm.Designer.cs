namespace WinJoy;

partial class MainForm
{
    /// <summary>
    ///  Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    /// <summary>
    ///  Clean up any resources being used.
    /// </summary>
    /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }

        base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    /// <summary>
    ///  Required method for Designer support - do not modify
    ///  the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
        tbLog = new TextBox();
        SuspendLayout();
        // 
        // tbLog
        // 
        tbLog.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        tbLog.BorderStyle = BorderStyle.FixedSingle;
        tbLog.Location = new Point(12, 239);
        tbLog.Multiline = true;
        tbLog.Name = "tbLog";
        tbLog.ReadOnly = true;
        tbLog.Size = new Size(509, 158);
        tbLog.TabIndex = 0;
        // 
        // Form1
        // 
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(533, 409);
        Controls.Add(tbLog);
        Name = "Form1";
        Text = "WinJoy";
        ResumeLayout(false);
        PerformLayout();
    }

    #endregion

    private TextBox tbLog;
}