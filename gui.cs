using System.Numerics;
using System.Runtime.InteropServices;
using ClickableTransparentOverlay;
using ImGuiNET;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;


namespace CSWhy
{

    class gui: Overlay
    {
        [DllImport("user32.dll")]
        static extern bool SetProcessDPIAware();

        ImDrawListPtr drawList;
        public List<Entity> ents = new List<Entity>();

        public void drawESP(List<Entity> ent, PlantedC4 c4)
        {
            if (c4 != null)
            {
                Console.WriteLine(c4.position2d.X);
                Console.WriteLine(c4.position2d.Y);
                drawList.AddLine(new Vector2(Screen.PrimaryScreen.Bounds.Width / 2, Screen.PrimaryScreen.Bounds.Height), c4.position2d, ImGui.ColorConvertFloat4ToU32(new Vector4(1, 0, 0, 1)), 2.0f);
                drawList.AddText(new Vector2(c4.position2d.X, c4.position2d.Y + 20.0f), ImGui.ColorConvertFloat4ToU32(new Vector4(1, 0, 0, 1)), "Planted_C4");
            }
            
            foreach (Entity e in ent)
            {
                if (e.health >= 1)
                {
                    if (e.position2d.X == -99) { continue; }
                    var color = (e.team == 2) ? ImGui.ColorConvertFloat4ToU32(new Vector4(1, 0, 0, 1)) : ImGui.ColorConvertFloat4ToU32(new Vector4(0, 0, 1, 1));
                    float entityheight = e.position2d.Y - e.viewPosition2d.Y;
                    Vector2 rectTop = new Vector2(e.position2d.X - entityheight / 3, e.viewPosition2d.Y);
                    Vector2 rectBottom = new Vector2(e.position2d.X + entityheight / 3, e.position2d.Y);
                    if (esp)
                    {
                        drawList.AddRect(rectTop, rectBottom, color, 0, ImDrawFlags.None, 1.5f);
                        drawList.AddText(new Vector2(e.position2d.X - entityheight, rectBottom.Y + 10.0f), color, e.name.Trim());
                        drawList.AddText(new Vector2(e.position2d.X - entityheight, rectBottom.Y + 20.0f), color, e.health + " HP | " + e.ping + " ms | "+ (e.canBeAttacked ? "normal" : "immune"));
                        if(e.head2d.X == -99) { continue; }
                        drawList.AddCircle(e.head2d, 15.0f, color, 10, 2.0f);
                    }
                    if (trace)
                    {
                        drawList.AddLine(new Vector2(Screen.PrimaryScreen.Bounds.Width / 2, Screen.PrimaryScreen.Bounds.Height), e.position2d, color, 2.0f);
                    }
                    if (aimbot)
                    {
                        drawList.AddCircle(new Vector2(Screen.PrimaryScreen.Bounds.Width / 2, Screen.PrimaryScreen.Bounds.Height/2), 70, ImGui.ColorConvertFloat4ToU32(new Vector4(1, 1, 1, 1)), 10, 2.0f);
                    }
                }
            }
        }

        public bool s1mple = false;
        public bool aimbot = false;
        public bool autostop = false;
        public bool esp = false;
        public bool noflash = false;
        public bool trigger = false;
        public bool closest = false;
        public bool trace = false;
        public bool aimlock = false;
        public bool firsthead = false;
        public bool ascope = false;
        private bool _show = true;
        public bool mreadonly = false;
        public PlantedC4 c4 = null;

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool SetWindowPos(
            IntPtr hWnd,
            IntPtr hWndInsertAfter,
            int X, int Y, int cx, int cy,
            uint uFlags);

        static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        const uint SWP_SHOWWINDOW = 0x0040;


        protected override void Render()
        {

            SetProcessDPIAware();
            if (ImGui.IsKeyPressed(ImGuiKey.Insert))
                _show = !_show;
            if (_show)
            {
                SetWindowPos(
                    this.window.Handle, HWND_TOPMOST,
                    0, 0,
                    Screen.PrimaryScreen.Bounds.Width,
                    Screen.PrimaryScreen.Bounds.Height,
                    SWP_SHOWWINDOW
                );
            }


            var io = ImGui.GetIO();
            ImGui.SetNextWindowPos(Vector2.Zero, ImGuiCond.Always);
            ImGui.SetNextWindowSize(io.DisplaySize, ImGuiCond.Always);

            ImGui.Begin("overlay",
                ImGuiWindowFlags.NoDecoration
              | ImGuiWindowFlags.NoBackground
              | ImGuiWindowFlags.NoInputs
              | ImGuiWindowFlags.NoMove
              | ImGuiWindowFlags.NoScrollbar
              | ImGuiWindowFlags.NoCollapse
            );
            drawList = ImGui.GetWindowDrawList();
            drawESP(ents, c4);
            ImGui.End();

            if (!_show)
                return;


            ImGui.SetWindowSize(new Vector2(Screen.PrimaryScreen.WorkingArea.Width, Screen.PrimaryScreen.WorkingArea.Height));
            ImGui.Begin("CSWhy");
            ImGui.Checkbox("Crazy FOV", ref s1mple);
            ImGui.Checkbox("Noflash", ref noflash);
            ImGui.Checkbox("Aimbot", ref aimbot);
            ImGui.Checkbox("Triggerbot (works only with aimbot)", ref trigger);
            ImGui.Checkbox("Choose closest enemies (triggerbot)", ref closest);
            ImGui.Checkbox("Autoscope", ref ascope);
            ImGui.Checkbox("Autostop", ref autostop);
            ImGui.Checkbox("Aimlock", ref aimlock);
            ImGui.Checkbox("Prioritize headshots", ref firsthead);
            ImGui.Checkbox("ESP / Wallhack", ref esp);
            ImGui.Checkbox("Tracers", ref trace);
            ImGui.Checkbox("Never write to memory", ref mreadonly);
            ImGui.End();
        }
    }
}
