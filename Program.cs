using CS2Dumper.Offsets;
using Swed64;
using System.Runtime.InteropServices;
using static CS2Dumper.Schemas.ClientDll;
using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Numerics;
using static System.Formats.Asn1.AsnWriter;

namespace CSWhy
{

    class PlantedC4
    {
        public Vector3 position { get; set; }
        public Vector2 position2d { get; set; }
    }
    class Entity
    {
        public string name { get; set; }
        public string steamID { get; set; } 
        public int health { get; set; }
        public int ping { get; set; }
        public int team { get; set; } // 3 - ct, 2 - t
        public IntPtr controller { get; set; }
        public IntPtr pawn { get; set; }
        public Vector3 position { get; set; }
        public Vector3 viewOffset { get; set; }
        public Vector2 position2d { get; set; }
        public Vector2 viewPosition2d { get; set; }
        public Vector3 head { get; set; }
        public Vector2 head2d { get; set; }
        public float distance { get; set; }
        public bool canBeAttacked { get; set; }
    }

    internal class Program
    {
        public static Vector2 CalculateAngles(Vector3 from, Vector3 destination)
        {
            float yaw;
            float pitch;

            // calculate the yaw
            float deltaX = destination.X - from.X;
            float deltaY = destination.Y - from.Y;
            yaw = (float)(Math.Atan2(deltaY, deltaX) * 180 / Math.PI);

            // calculate the pitch
            float deltaZ = destination.Z - from.Z;
            double distance = Math.Sqrt(Math.Pow(deltaX, 2) + Math.Pow(deltaY, 2));
            pitch = -(float)(Math.Atan2(deltaZ, distance) * 180 / Math.PI);

            // return calculated angles
            return new Vector2(yaw, pitch);
        }
        public static Vector2 WorldToScreen(float[] matrix, Vector3 pos, Vector2 windowSize)
        {
            float screenW = (matrix[12] * pos.X) + (matrix[13] * pos.Y) + (matrix[14] * pos.Z) + matrix[15];

            if (screenW > 0.01f)
            {
                float screenX = (matrix[0] * pos.X) + (matrix[1] * pos.Y) + (matrix[2] * pos.Z) + matrix[3];
                float screenY = (matrix[4] * pos.X) + (matrix[5] * pos.Y) + (matrix[6] * pos.Z) + matrix[7];

                float x = (windowSize.X / 2) + (windowSize.X / 2) * screenX / screenW;
                float y = (windowSize.Y / 2) - (windowSize.Y / 2) * screenY / screenW;
                return new Vector2(x, y);
            }

            return new Vector2(-99, -99);
        }
        static void shoot(Swed sw, bool ascope = false)
        {

            IntPtr clientBase = sw.GetModuleBase("client.dll");
            int dwLocalPlayerPawn = (int)ClientDll.dwLocalPlayerPawn;
            IntPtr LocalPlayerPawn = sw.ReadPointer(clientBase, dwLocalPlayerPawn);

            if (ascope && !sw.ReadBool(LocalPlayerPawn, (int)C_CSPlayerPawn.m_bIsScoped) )
            {
                var thr = new Thread(() =>
                {
                    sw.WriteInt(clientBase, (int)CS2Dumper.Buttons.attack2, 65537);
                    Thread.Sleep(10);
                    sw.WriteInt(clientBase, (int)CS2Dumper.Buttons.attack2, 256);
                });
                thr.IsBackground = true;
                thr.Start();
            }
            sw.WriteInt(clientBase, (int)CS2Dumper.Buttons.attack, 65537);
            Thread.Sleep(10);
            sw.WriteInt(clientBase, (int)CS2Dumper.Buttons.attack, 256);
        }

        static void stop(Swed sw)
        {
            sw.WriteInt(sw.GetModuleBase("client.dll"), (int)CS2Dumper.Buttons.forward, 65537);
            Thread.Sleep(10);
            sw.WriteInt(sw.GetModuleBase("client.dll"), (int)CS2Dumper.Buttons.back, 65537);
            Thread.Sleep(10);
            sw.WriteInt(sw.GetModuleBase("client.dll"), (int)CS2Dumper.Buttons.forward, 256);
            sw.WriteInt(sw.GetModuleBase("client.dll"), (int)CS2Dumper.Buttons.back, 256);
        }

        static string fix_string(string str)
        {
            int fix = 0;
            while (fix < str.Length && (!(str[fix] > (char)48 && str[fix] < (char)57) && !(str[fix] > (char)65 && str[fix] < (char)90) && !(str[fix] > (char)97 && str[fix] < (char)122)))
            {
                fix++;
            }
            return str.Substring(fix, str.Length - fix);
        }


        static public int getSelfIndex(Swed sw)
        {
            IntPtr clientBase = sw.GetModuleBase("client.dll");
            int dwEntityList = (int)ClientDll.dwEntityList;

            IntPtr entityList = sw.ReadPointer(clientBase, dwEntityList);
            IntPtr listEntry = sw.ReadPointer(entityList, 0x10);
            for (int i = 0; i < 64; i++)
            {
                if (listEntry == IntPtr.Zero) continue;
                IntPtr currentController = sw.ReadPointer(listEntry, i * 0x78);
                if (currentController == IntPtr.Zero) continue;
                if (sw.ReadBool(currentController, (int)CBasePlayerController.m_bIsLocalPlayerController))
                {
                    return i;
                }
            }
            return -1;
        }

        static public List<Entity> getEntities(Swed sw, bool firsthead)
        {
            IntPtr clientBase = sw.GetModuleBase("client.dll");
            int dwEntityList = (int)ClientDll.dwEntityList;
            int hPlayerPawn = (int)CCSPlayerController.m_hPlayerPawn;
            int iHealth = (int)C_BaseEntity.m_iHealth;
            int m_iszPlayerName = (int)CBasePlayerController.m_iszPlayerName;

            IntPtr entityList = sw.ReadPointer(clientBase, dwEntityList);
            IntPtr listEntry = sw.ReadPointer(entityList, 0x10);
            List<Entity> guys = new List<Entity>();
            for (int i = 0; i < 64; i++)
            {
                if (listEntry == IntPtr.Zero) continue;
                IntPtr currentController = sw.ReadPointer(listEntry, i * 0x78);
                if (currentController == IntPtr.Zero) continue;
                if(sw.ReadBool(currentController, (int)CBasePlayerController.m_bIsLocalPlayerController)) continue;
                int pawnHandle = sw.ReadInt(currentController, hPlayerPawn);
                if (pawnHandle == IntPtr.Zero) continue;
                IntPtr listEntry2 = sw.ReadPointer(entityList, 0x8 * ((pawnHandle & 0x7fff) >> 9) + 0x10);
                IntPtr currentPawn = sw.ReadPointer(listEntry2, 0x78 * (pawnHandle & 0x1ff));
                Entity e = new Entity();
                e.health = (int)sw.ReadUInt(currentPawn, iHealth);
                e.name = fix_string(sw.ReadString(currentController, m_iszPlayerName, 16).Trim());
                e.steamID = sw.ReadString(currentController, (int)CBasePlayerController.m_steamID, 32);
                e.ping = (int)sw.ReadUInt(currentController, (int)CCSPlayerController.m_iPing);
                e.team = (int)sw.ReadUInt(currentController, (int)C_BaseEntity.m_iTeamNum);
                e.pawn = currentPawn;
                e.controller = currentController;
                e.canBeAttacked = sw.ReadBool(currentPawn + (int)C_BaseEntity.m_bTakesDamage) && (int)sw.ReadUInt(currentPawn, (int)C_CSPlayerPawnBase.m_bGunGameImmunity) !=1 ;
                e.position = sw.ReadVec(currentPawn, (int)C_BasePlayerPawn.m_vOldOrigin);
                e.position2d = WorldToScreen(sw.ReadMatrix(clientBase + ClientDll.dwViewMatrix), e.position, new Vector2(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height));
                e.viewOffset = sw.ReadVec(currentPawn, (int)C_BaseModelEntity.m_vecViewOffset);
                e.viewPosition2d = WorldToScreen(sw.ReadMatrix(clientBase + ClientDll.dwViewMatrix), Vector3.Add(e.position, e.viewOffset), new Vector2(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height));
                int dwLocalPlayerPawn = (int)ClientDll.dwLocalPlayerPawn;
                IntPtr LocalPlayerPawn = sw.ReadPointer(clientBase, dwLocalPlayerPawn);
                e.distance = Vector3.Distance(e.position, sw.ReadVec(LocalPlayerPawn, (int)C_BasePlayerPawn.m_vOldOrigin));
                IntPtr node = sw.ReadPointer(currentPawn, (int)C_BaseEntity.m_pGameSceneNode);
                IntPtr bmatrix = sw.ReadPointer(node, (int)CSkeletonInstance.m_modelState + 0x80);
                var rnd = new Random();
                if (e.health > 60)
                {
                    e.head = sw.ReadVec(bmatrix,( firsthead ? 6 : ((int)rnd.NextInt64(2, 4))) * 32); // idk, torso?
                }
                else
                {
                    e.head = sw.ReadVec(bmatrix, (!firsthead ? 6 : ((int)rnd.NextInt64(2, 4))) * 32); // head
                }
                
                e.head2d = WorldToScreen(sw.ReadMatrix(clientBase + ClientDll.dwViewMatrix), e.head, new Vector2(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height));
                guys.Add(e);
                if (e.canBeAttacked)
                {
                    //Console.WriteLine(e.name);
                    //Console.WriteLine(e.steamID);
                }
            }
            return guys;
        }

        static public void simpleMode(Swed sw) // visuals 
        {
            IntPtr clientBase = sw.GetModuleBase("client.dll");
            int dwLocalPlayerPawn = (int)ClientDll.dwLocalPlayerPawn;
            int m_pCameraServices = (int)C_BasePlayerPawn.m_pCameraServices;
            int m_iFOV = (int)CCSPlayerBase_CameraServices.m_iFOV;
            IntPtr LocalPlayerPawn = sw.ReadPointer(clientBase, dwLocalPlayerPawn);
            IntPtr CameraServices = sw.ReadPointer(LocalPlayerPawn, m_pCameraServices);
            sw.WriteUInt(CameraServices + m_iFOV, 160);
        }

        static public void noflash(Swed sw) // visuals 
        {
            IntPtr clientBase = sw.GetModuleBase("client.dll");
            int dwLocalPlayerPawn = (int)ClientDll.dwLocalPlayerPawn;
            IntPtr LocalPlayerPawn = sw.ReadPointer(clientBase, dwLocalPlayerPawn);
            sw.WriteFloat(LocalPlayerPawn + (int)C_CSPlayerPawnBase.m_flFlashDuration, 0);
        }

        static public void setNormalFOV(Swed sw) // visuals 
        {
            IntPtr clientBase = sw.GetModuleBase("client.dll");
            int dwLocalPlayerPawn = (int)ClientDll.dwLocalPlayerPawn;
            int m_pCameraServices = (int)C_BasePlayerPawn.m_pCameraServices;
            int m_iFOV = (int)CCSPlayerBase_CameraServices.m_iFOV;
            IntPtr LocalPlayerPawn = sw.ReadPointer(clientBase, dwLocalPlayerPawn);
            IntPtr CameraServices = sw.ReadPointer(LocalPlayerPawn, m_pCameraServices);
            sw.WriteUInt(CameraServices + m_iFOV, 90);
        }

        static public void aimbot(Swed sw, List<Entity> entities, bool safe = false, bool trigger = false, bool autostop = false, bool closest = false, bool ascope = false, bool aimlock = false)
        {
            int sI = (int)getSelfIndex(sw);
            IntPtr clientBase = sw.GetModuleBase("client.dll");
            int dwLocalPlayerPawn = (int)ClientDll.dwLocalPlayerPawn;
            IntPtr LocalPlayerPawn = sw.ReadPointer(clientBase, dwLocalPlayerPawn);

            foreach (var x in entities.OrderBy(e => e.distance).ToList()) 
            {
                if ((!x.canBeAttacked) || x.health <= 0 || x.head2d.X == -99) { continue; }
                if ((Vector2.Distance(x.head2d, new Vector2(Screen.PrimaryScreen.Bounds.Width / 2, Screen.PrimaryScreen.Bounds.Height / 2)) <= 140.0f) || closest) // if spinbot, we choose closest to distance
                {
                    //Console.WriteLine();
                    if ((sw.ReadUInt(x.pawn + (int)C_CSPlayerPawn.m_entitySpottedState, (int)EntitySpottedState_t.m_bSpottedByMask) & (1 << sI-1) ) !=0)
                    {
                        var a = CalculateAngles(Vector3.Add(sw.ReadVec(LocalPlayerPawn, (int)C_BasePlayerPawn.m_vOldOrigin), sw.ReadVec(LocalPlayerPawn, (int)C_BaseModelEntity.m_vecViewOffset)), x.head);
                        sw.WriteVec(clientBase, (int)ClientDll.dwViewAngles, new Vector3(a.Y, a.X, 0.0f));
                        //Console.WriteLine("Write vector " + a.X + " " + a.Y);
                        if (autostop)
                        {
                            stop(sw);
                        }
                        if (trigger)
                        {
                            shoot(sw, ascope);
                        }
                    }
                    if (!trigger && aimlock) {
                        var a = CalculateAngles(Vector3.Add(sw.ReadVec(LocalPlayerPawn, (int)C_BasePlayerPawn.m_vOldOrigin), sw.ReadVec(LocalPlayerPawn, (int)C_BaseModelEntity.m_vecViewOffset)), x.head);
                        sw.WriteVec(clientBase, (int)ClientDll.dwViewAngles, new Vector3(a.Y, a.X, 0.0f));
                    }
                }
            }
        }


        static void Main(string[] args)
        {
            Swed sw = new Swed("cs2");
            
            gui gui = new gui();
            var thr = new Thread(() =>
            {
                gui.Start().Wait();
            });
            var thr2 = new Thread(() =>
            {
                while (true)
                {
                    if ((!gui.mreadonly) && gui.aimbot)
                    {
                        aimbot(sw, gui.ents, gui.safe, gui.trigger, gui.autostop, gui.closest, gui.ascope, gui.aimlock);
                    }
                }
            });
            var thr3 = new Thread(() =>
            {
                while (true)
                {
                    if (gui.esp | gui.trace | gui.aimbot)
                    {
                        gui.ents = getEntities(sw, gui.firsthead);
                    }
                }
            });
            thr.IsBackground = true;
            thr.Start();
            thr2.IsBackground = true;
            thr2.Start();
            thr3.IsBackground = true;
            thr3.Start();
            while (true)
            {
                if (!gui.mreadonly)
                {
                    if (gui.s1mple)
                    {
                        simpleMode(sw);
                    }
                    else
                    {
                        setNormalFOV(sw);
                    }
                    if (gui.noflash)
                    {
                        noflash(sw);
                    }
                }
            }
        }
    }
}
