using System;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.Text;
public record ContainerRequest(string Name, string Image, bool UsePort, int Port, bool UseVolume, string Volume, bool IsEnvironment, string Environment);
public class Containers
{
  private ProcessStartInfo? info;
  public record ContainerInfo(string Id, string Image, string Status, string Name);
  public Containers()
  {
    info = new ProcessStartInfo
    {
      FileName = "/bin/zsh",
      RedirectStandardOutput = true,
      UseShellExecute = false,
      CreateNoWindow = true
    };
  }
  public async Task<List<string>> GetAllContainers()
  {
    List<string> containers = new List<string>();
    info!.Arguments = "-c \"docker ps -a --format '{{.ID}}|{{.Image}}|{{.Status}}|{{.Names}}'\"";
    return await Task.Run(async () =>
    {
      using var process = Process.Start(info);
      while (!process!.StandardOutput.EndOfStream)
      {
        string cont = process.StandardOutput.ReadLine()!;
        if (cont != null)
        {
          containers.Add(cont);
        }
      }
      await process.WaitForExitAsync();
      return containers;
    });
  }
  public async Task<List<ContainerInfo>> GetOnlineContainers()
  {
    List<ContainerInfo> containers = new List<ContainerInfo>();
    info!.Arguments = "-c \"docker ps --format '{{.ID}}|{{.Image}}|{{.Status}}|{{.Names}}'\"";
    return await Task.Run(async () =>
    {
      using var process = Process.Start(info);
      while (!process!.StandardOutput.EndOfStream)
      {
        string line = await process.StandardOutput.ReadLineAsync() ?? "";
        if (string.IsNullOrWhiteSpace(line))
        {
          continue;
        }
        var parts = line.Split('|');
        if (parts.Length >= 4)
        {
          containers.Add(new ContainerInfo(parts[0], parts[1], parts[2], parts[3]));
        }
      }
      await process.WaitForExitAsync();
      return containers;
    });
  }
  public async Task CreateVolume(string name)
  {
    await Task.Run(async () =>
    {
      info!.Arguments = $"-c \"docker volume create {name}\"";
      using var process = Process.Start(info);
      while (!process!.StandardOutput.EndOfStream)
      {
        Console.WriteLine($"{process.StandardOutput.ReadLine()}");
      }
      await process.WaitForExitAsync();
    });
  }
  public async Task RemoveVolume(string name)
  {
    await Task.Run(async () =>
    {
      info!.Arguments = $"-c \"docker volume rm {name}\"";
      using var process = Process.Start(info);
      while (!process!.StandardOutput.EndOfStream)
      {
        Console.WriteLine($"{process.StandardOutput.ReadLine()}");
      }
      await process.WaitForExitAsync();
    });
  }
  public async Task<string> GetContainerLogs(string name)
  {
    info!.Arguments = $"-c \"docker logs --tail 100 {name}\"";
    using var process = Process.Start(info);
    string logs = await process!.StandardOutput.ReadToEndAsync();
    await process.WaitForExitAsync();
    return logs;
  }
  public async Task RestartContainer(string name)
  {
    await RunCommand($"-c \"docker restart {name}\"");
  }
  public async Task EnterDocker(string name, string command, Func<string, Task> onOutputReceived)
  {
    List<string> result = new List<string>();
    await Task.Run(async () =>
    {
      var execInfo = new ProcessStartInfo
      {
        FileName = "/bin/zsh",
        Arguments = $"-c \"docker exec -i {name} {command}\"",
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false,
        CreateNoWindow = true
      };
      using var process = new Process
      {
        StartInfo = execInfo
      };
      process.OutputDataReceived += async (sender, e) =>
      {
        if (e.Data != null)
        {
          await onOutputReceived(e.Data);
        }
      };
      process.Start();
      process.BeginOutputReadLine();
      await process.WaitForExitAsync();
    });
  }
  public async Task CreateContainer(string name, string image, bool useport, int port, bool usevolume, string volume, bool isenvironment, string envivonment)
  {
    StringBuilder sb = new StringBuilder("docker run -dit");
    sb.Append($" --name {name}");
    if (useport)
    {
      sb.Append($" -p {port}");
    }
    if (usevolume)
    {
      sb.Append($" -v {volume}");
    }
    if (isenvironment)
    {
      sb.Append($" -e {envivonment}");
    }
    sb.Append($" {name}");
    string fullCommand = $"-c \"{sb.ToString()}\"";
    await Task.Run(async () =>
    {
      info!.Arguments = fullCommand;
      using var process = Process.Start(info);
      while (!process!.StandardOutput.EndOfStream)
      {
        Console.WriteLine($"{process.StandardOutput.ReadLine()}");
      }
      await process.WaitForExitAsync();
    });
  }
  public async Task<string> GetLogs(string name, int tail = 100)
  {
    var logInfo = new ProcessStartInfo
    {
      FileName = "/bin/zsh",
      Arguments = $"-c \"docker logs --tail {tail} {name}\"",
      RedirectStandardOutput = true,
      RedirectStandardError = true,
      UseShellExecute = false,
      CreateNoWindow = true
    };
    using var process = Process.Start(logInfo);
    string output = await process!.StandardOutput.ReadToEndAsync();
    string error = await process.StandardError.ReadToEndAsync();
    await process.WaitForExitAsync();
    return string.IsNullOrEmpty(output) ? error : output;
  }
  private async Task RunCommand(string arguments)
  {
    await Task.Run(async () =>
    {
      var procInfo = new ProcessStartInfo
      {
        FileName = "/bin/zsh",
        Arguments = arguments,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false,
        CreateNoWindow = true
      };

      using var process = Process.Start(procInfo);
      string error = await process!.StandardError.ReadToEndAsync();
      if (!string.IsNullOrEmpty(error))
      {
        Console.WriteLine($"Docker Error: {error}");
      }
      await process.WaitForExitAsync();
    });
  }
  public async Task<string> GetStats(string name)
  {
    info!.Arguments = $"-c \"docker stats {name} --no-stream --format '{{.CPUPerc}}|{{.MemUsage}}'\"";
    using var process = Process.Start(info);
    string result = await process!.StandardOutput.ReadLineAsync() ?? "";
    await process.WaitForExitAsync();
    return result;
  }
  public async Task CleanSystem()
  {
    await RunCommand("-c \"docker system prune -f\"");
  }

  public async Task StartContainer(string name)
  {
    await Task.Run(async () =>
    {
      await RunCommand($"-c \"docker start {name}\"");
    });
  }
  public async Task StopContainer(string name)
  {
    await Task.Run(async () =>
    {
      info!.Arguments = $"-c \"docker stop {name}\"";
      using var process = Process.Start(info);
      while (!process!.StandardOutput.EndOfStream)
      {
        Console.WriteLine($"{process.StandardOutput.ReadLine()}");
      }
      await process.WaitForExitAsync();
    });
  }
  public async Task RemoveContainer(string name)
  {
    await Task.Run(async () =>
    {
      info!.Arguments = $"-c \"docker rm {name}\"";
      using var process = Process.Start(info);
      while (!process!.StandardOutput.EndOfStream)
      {
        Console.WriteLine($"{process.StandardOutput.ReadLine()}");
      }
      await process.WaitForExitAsync();
    });
  }
  public async Task PullImage(string name)
  {
    await Task.Run(async () =>
    {
      info!.Arguments = $"-c \"docker pull {name}\"";
      using var process = Process.Start(info);
      while (!process!.StandardOutput.EndOfStream)
      {
        Console.WriteLine($"{process.StandardOutput.ReadLine()}");
      }
      await process.WaitForExitAsync();
    });
  }
  public async Task RemoveImage(string name)
  {
    await Task.Run(async () =>
    {
      info!.Arguments = $"-c \"docker rmi {name}\"";
      using var process = Process.Start(info);
      while (!process!.StandardOutput.EndOfStream)
      {
        Console.WriteLine($"{process.StandardOutput.ReadLine()}");
      }
      await process.WaitForExitAsync();
    });
  }
  public async Task<List<string>> GetAllImages()
  {
    List<string> images = new List<string>();
    return await Task.Run(async () =>
    {
      info!.Arguments = "-c \"docker images\"";
      using var process = Process.Start(info);
      while (!process!.StandardOutput.EndOfStream)
      {
        string cont = process.StandardOutput.ReadLine()!;
        if (cont != null && !cont.Contains("IMAGE"))
        {
          images.Add(cont);
        }
      }
      await process.WaitForExitAsync();
      return images;
    });
  }
}
