﻿using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace Insta.Configuration;

public class Configuration
{
  public string TelegramToken { get; set; }

  public string QiwiToken { get; set; }

  public int Cost { get; set; }

  public int Bonus { get; set; }

  public int LoverDuration { get; set; }

  public int UpperDuration { get; set; }

  public int Interval { get; set; }

  public string NameSupport { get; set; }
  public int BlockHours { get; set; }

  public List<long> Admins { get; set; }

  public static Configuration Initialise(string path)
  {
    using (var streamReader = new StreamReader(path))
      return (Configuration) new JsonSerializer().Deserialize(streamReader, typeof (Configuration)) ?? throw new NullReferenceException("Файл конфигурации не найден.");
  }
}