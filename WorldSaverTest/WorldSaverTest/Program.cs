using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PlayerIOClient;
using WorldSaver;

namespace WorldSaverTest
{
    class Program
    {
        static Client client;
        static Connection con;
        static World world;

        static void Main(string[] args)
        {
            Console.Write("E-Mail: ");
            string email = Console.ReadLine();
            Console.Write("Password: ");
            string password = MaskedReadLine();
            Console.Write("WorldID: ");
            string worldID = Console.ReadLine();

            client = PlayerIO.QuickConnect.SimpleConnect("everybody-edits-su9rn58o40itdbnw69plyw", email, password, null);
            con = client.Multiplayer.CreateJoinRoom(worldID, "Everybodyedits" + client.BigDB.Load("config", "config")["version"], true, null, null);

            con.OnMessage += OnMessage;

            con.Send("init");

            Console.ReadLine();
        }

        static void OnMessage(object sender, Message m)
        {
            switch (m.Type)
            {
                case "init":
                    world = new World(m, m.GetInt(18), m.GetInt(19));

                    Console.WriteLine(world.GetBlock(0, 0, 0).ID);

                    world.Save("Test.json");
                    break;
            }
        }

        static string MaskedReadLine()
        {
            string line = "";

            while (true)
            {
                ConsoleKeyInfo key = Console.ReadKey(true);

                if (key.Key == ConsoleKey.Enter)
                {
                    Console.WriteLine();
                    return line;
                }
                else if (key.Key == ConsoleKey.Backspace)
                {
                    if (line == "") continue;
                    line = line.Substring(0, (line.Length - 1));
                    Console.Write("\b \b");
                }
                else
                {
                    line += key.KeyChar;
                    Console.Write("*");
                }
            }
        }
    }
}
