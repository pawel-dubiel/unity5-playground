using System;
using System.Collections.Generic;
using UnityEngine;

public class SlavsEvent
{
    public string Id;
    public string Title;
    public string Body;
    public Color ImageTint = new Color(0.5f, 0.4f, 0.3f, 1f);
    public bool AutoAdvance;
    public List<SlavsEventOption> Options = new List<SlavsEventOption>();
}

public class SlavsEventOption
{
    public string Label;
    public string ResultText;
    public Action<SlavsVillageGame> Apply;
}
