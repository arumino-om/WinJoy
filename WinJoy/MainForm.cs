using WinJoy.Threads;

namespace WinJoy;

public partial class MainForm : Form
{
    public static MainForm? currentInstance;
    public static void AddLog(string text)
    {
        var logTexts = new List<string>();
        var logDateTime = $"[{DateTime.Now:u}]";
        var splited = text.Split('\n');
        var isFirstLine = true;
        foreach (var line in splited)
        {
            if (isFirstLine)
            {
                logTexts.Add($"{logDateTime} {line}");
                isFirstLine = false;
                continue;
            }
            logTexts.Add($"{logDateTime}   {line}\n");
        }
        var logText = string.Join("\r\n", logTexts) + "\r\n";

        if (currentInstance == null) return;
        if (currentInstance.InvokeRequired)
        {
            currentInstance.Invoke(new Action(() => currentInstance.tbLog.AppendText(logText)));
        }
        else
        {
           currentInstance.tbLog.AppendText(logText);
        }
    }
    
    List<Task> tasks = new();

    public MainForm()
    {
        InitializeComponent();
        tasks.Add(new(() => new PollThread()));
        tasks.Add(new(() => new ConnectionThread()));
        foreach (var task in tasks)
        {
            task.Start();
        }

        currentInstance = this;
    }

    private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
    {
        foreach (var task in tasks)
        {
            task.Dispose();
        }
    }
}