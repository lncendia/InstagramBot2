// Decompiled with JetBrains decompiler
// Type: Insta.Configuration.Configuration
// Assembly: Insta, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 618A128D-D21F-4C2F-8A52-8FDB43CCAD32
// Assembly location: C:\Users\egorl\Desktop\LikeBot2\Insta.dll

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace Insta.Configuration
{
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

    public List<long> Admins { get; set; }

    public static Configuration Initialise(string path)
    {
      using (StreamReader streamReader = new StreamReader(path))
        return (Configuration) new JsonSerializer().Deserialize((TextReader) streamReader, typeof (Configuration)) ?? throw new NullReferenceException("Файл конфигурации не найден.");
    }
  }
}
