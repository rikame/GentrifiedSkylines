// Decompiled with JetBrains decompiler
// Type: AgeInfoViewPanel
// Assembly: Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 19C073A7-376D-4654-856C-851D76451F95
// Assembly location: D:\SteamLibrary\steamapps\common\Cities_Skylines\Cities_Data\Managed\Assembly-CSharp.dll

using ColossalFramework;
using ColossalFramework.UI;

public class AgeInfoViewPanel : InfoViewPanel
{
  private bool m_initialized;

  protected override void Start()
  {
    base.Start();
    this.m_Tabstrip.eventSelectedIndexChanged += (PropertyChangedEventHandler<int>) ((sender, id) =>
    {
      if (!Singleton<InfoManager>.exists)
        return;
      Singleton<InfoManager>.instance.SetCurrentMode(Singleton<InfoManager>.instance.NextMode, (InfoManager.SubInfoMode) id);
    });
  }

  protected override void UpdatePanel()
  {
    if (!this.m_initialized && Singleton<LoadingManager>.exists)
    {
      if (!Singleton<LoadingManager>.instance.m_loadingComplete)
        return;
      this.m_initialized = true;
    }
    if (!Singleton<InfoManager>.exists)
      return;
    this.m_Tabstrip.selectedIndex = (int) Singleton<InfoManager>.instance.NextSubMode;
  }
}
