using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Rendering.Composition;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace GoogleDocsInstaller;

public partial class MainWindow : Window
{
    public string drive = "";
    public string mode = "";
    public MainWindow()
    {
        InitializeComponent();
        Disable(PathSelectWindow);
        Disable(ModeSelectWindow);
        Disable(ConfirmWindow);
        Disable(ProgressWindow);
        Disable(FinishWindow);
    }

    private async void StartSetup(object? sender, RoutedEventArgs e)
    {
        AnimateOut(StartingWindow,PathSelectWindow);
        foreach (var radio in PathSelectWindow.Children)
        {
            if (radio is RadioButton rb)
            {
                rb.IsEnabled = Directory.Exists(rb.Content.ToString());
            }
        }
    }

    private async void Disable(InputElement item)
    {
        item.IsVisible = false;
        item.IsEnabled = false;
        item.Opacity = 0;
    }

    private async void AnimateOut(InputElement itemout, InputElement itemin)
    {
        itemin.IsEnabled = true;
        itemin.IsVisible = true;
        itemin.Opacity = 0;
        Anim(itemout);
        await Task.Delay(900);
        itemin.Opacity = 1;
    }


    private void NextPath(object? sender, RoutedEventArgs e)
    {
    AnimateOut(PathSelectWindow,ModeSelectWindow);
    }
    private void NextMode(object? sender, RoutedEventArgs e)
    {
         drive = "";
         mode = "";
        foreach (var radio in PathSelectWindow.Children)
        {
            if (radio is RadioButton rb)
            {
                if (rb.IsChecked == true)
                {
                    drive = rb.Content.ToString();
                }
            }
        }

        foreach (var radio in ModeSelectWindow.Children)
        {
            if (radio is RadioButton rb)
            {
                if (rb.IsChecked == true)
                {
                    mode = rb.Content.ToString();
                }
            }
        }
    AnimateOut(ModeSelectWindow,ConfirmWindow);
    DriveConfirm.Text = $"Drive: {drive}";
    ModeConfirm.Text = $"Mode: {mode}";

    }

    private void ProgressThread()
    {
        var info = new ProcessStartInfo("pwsh.exe", $"-NoProfile -WindowStyle Hidden -ExecutionPolicy Bypass -File { System.IO.Path.GetDirectoryName(System.Environment.ProcessPath)}/Install.ps1 {drive} {mode}");
        info.UseShellExecute = true;
        info.Verb = "runas";
        info.CreateNoWindow = true;
        var process = Process.Start(info);
        Dispatcher.UIThread.Post(() =>
        {
            ProgressBar.Value = 5;
        });
        var pipe = new NamedPipeServerStream("GoogleDocsInstallerPipe", PipeDirection.In, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
        Console.WriteLine("Waiting for connection");
        pipe.WaitForConnection();
        Console.WriteLine("Connected");
        var stream = new StreamReader(pipe);
        while (pipe.IsConnected)
        {
                string s = stream.ReadLine();
                if (s == null)
                {
                    Thread.Sleep(50);
                    continue;
                }

                Console.WriteLine(s);
                if (s.StartsWith("Progress: "))
                {
                    int progress = int.Parse(s.Replace("Progress: ", ""));
                    Dispatcher.UIThread.Post(() => { ProgressBar.Value = progress; });
                }


            Thread.Sleep(50);
        }

        Dispatcher.UIThread.Post(() => { AnimateOut(ProgressWindow, FinishWindow); });
    }
    private async void Confirm(object? sender, RoutedEventArgs e)
    {
        Dispatcher.UIThread.Post(() => { AnimateOut(ConfirmWindow, ProgressWindow); });
        var thread = new Thread(ProgressThread);
        thread.Start();
    }

    private async void Quit(object? sender, RoutedEventArgs e)
    {
            if (Application.Current?.ApplicationLifetime is IControlledApplicationLifetime lifetime)
            {
                lifetime.Shutdown();
            }
    }

    private void AddOffset(Visual item, Vector3 offset)
    {
        var vis = ElementComposition.GetElementVisual(item);
        var baseoffset = new Vector3((float)vis.Offset.X, (float)vis.Offset.Y, (float)vis.Offset.Z);
        vis.Offset = baseoffset + offset;
    }

    private void Anim(Visual item)
    {
        var vis = ElementComposition.GetElementVisual(item);
        var comp = vis.Compositor;
        var anim = comp.CreateVector3KeyFrameAnimation();
        anim.Duration = TimeSpan.FromMilliseconds(900);
        var prevoffset3d = vis.Offset;
        var prevoffset = new Vector3((float)prevoffset3d.X, (float)prevoffset3d.Y, (float)prevoffset3d.Z);
        anim.InsertKeyFrame(0f,prevoffset,new CubicEaseInOut());
        anim.InsertKeyFrame(1f,prevoffset + new Vector3(-1000,0,0),new CubicEaseInOut());
        vis.StartAnimation("Offset",anim);
    }
}