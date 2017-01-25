using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace WorldSaver
{
    public struct World
    {
        Block[,] fgs;
        Block[,] bgs;

        Dictionary<string, Dictionary<string, object>> botData;
        
        public World(int width, int height)
        {
            fgs = new Block[width, height];
            bgs = new Block[width, height];
            botData = new Dictionary<string, Dictionary<string, object>>();
        }

        public World(PlayerIOClient.Message m, int width, int height)
        {
            if (m == null) throw new ArgumentNullException("m");
            if (m.Type != "init" && m.Type != "reset") throw new ArgumentException("Invalid message type.", "m");
            
            Block[,] foregrounds = new Block[width, height];
            Block[,] backgrounds = new Block[width, height];

            uint i = 0;
            while(m[i++] as string != "ws") { }
            while(m[i] as string != "we")
            {
                uint id = (uint)m[i++];
                int layer = (int)m[i++];
                byte[] xs = (byte[])m[i++];
                byte[] ys = (byte[])m[i++];
                var args = new List<object>();

                while(m[i  ] as string != "we" &&
                    !(m[i  ] is uint && 
                      m[i+1] is int &&
                      m[i+2] is byte[] &&
                      m[i+3] is byte[]))
                {
                    args.Add(m[i++]);
                }

                Block b = new Block(id, args.ToArray());
                if (layer == 0)
                {
                    for (int p = 0; p < xs.Length; p++)
                    {
                        foregrounds[xs[p], ys[p]] = b;
                    }
                }
                else if (layer == 1)
                {
                    for (int p = 0; p < xs.Length; p++)
                    {
                        backgrounds[xs[p], ys[p]] = b;
                    }
                }
            }
            
            fgs = foregrounds;
            bgs = backgrounds;
            botData = new Dictionary<string, Dictionary<string, object>>();
        }
        
        public static World Load(string fileName)
        {
            StreamReader file = new StreamReader(fileName);

            return FromJson(file.ReadToEnd());
        }

        public static World FromJson(string json)
        {
            Dictionary<string, object> jsonDict = (Dictionary<string, object>)JsonToObj(json);

            if (!jsonDict.ContainsKey("format")) throw new ArgumentException("Could not find format.");
            if (!(jsonDict["format"] is string)) throw new ArgumentException("Invalid Format.");

            WorldFormat format = new WorldFormat((string)jsonDict["format"]);

            if (format == WorldFormat.STANDARD) return FromStandardFormat(jsonDict);
            else throw new NotImplementedException("'" + format + "' format has not yet been implemented");
        }

        static World FromStandardFormat(Dictionary<string, object> json)
        {
            if (!json.ContainsKey("blist")) throw new ArgumentException("Json does not contain block list.");
            if (!json.ContainsKey("bglist")) throw new ArgumentException("Json does not contain background list.");
            if (!json.ContainsKey("blocks")) throw new ArgumentException("Json does not contain a block array.");
            if (!json.ContainsKey("backgrounds")) throw new ArgumentException("Json does not contain a background array.");
            if (!json.ContainsKey("data")) throw new ArgumentException("Json does not contain a bot data object.");

            int width = 0;
            int height = 0;
            
            // TODO: try-catch
            height = ((List<object>)json["backgrounds"]).Count > ((List<object>)json["blocks"]).Count ?
                ((List<object>)json["backgrounds"]).Count :
                ((List<object>)json["blocks"]).Count;
            foreach (object row in (List<object>)json["blocks"])
                width = ((List<object>)row).Count > width ? ((List<object>)row).Count : width;
            foreach (object row in (List<object>)json["backgrounds"])
                width = ((List<object>)row).Count > width ? ((List<object>)row).Count : width;

            List<Block> fgKeys = new List<Block> { new Block(0) };
            List<Block> bgKeys = new List<Block> { new Block(0) };
            Block[,] foreground = new Block[width, height];
            Block[,] background = new Block[width, height];

            foreach(object block in (List<object>)json["blist"])
            {
                if (block is long) fgKeys.Add(new Block((uint)(long)block));
                else fgKeys.Add(new Block((uint)(long)((List<object>)block)[0], ((List<object>)block).GetRange(1, ((List<object>)block).Count - 1).ToArray()));
            }

            foreach (object block in (List<object>)json["bglist"])
            {
                if (block is long) bgKeys.Add(new Block((uint)(long)block));
                else bgKeys.Add(new Block((uint)(long)((List<object>)block)[0], ((List<object>)block).GetRange(1, ((List<object>)block).Count - 1).ToArray()));
            }

            List<object> blocks = (List<object>)json["blocks"];
            for (int y = 0; y < height; y++)
            {
                List<object> row = (y < blocks.Count) ? (List<object>)blocks[y] : new List<object>();
                for (int x = 0; x < width; x++)
                {
                    if (x < row.Count)
                    {
                        foreground[x, y] = fgKeys[(int)(long)row[x]];
                    }
                    else
                    {
                        foreground[x, y] = fgKeys[0];
                    }
                }
            }

            blocks = (List<object>)json["backgrounds"];
            for (int y = 0; y < height; y++)
            {
                List<object> row = (y < blocks.Count) ? (List<object>)blocks[y] : new List<object>();
                for (int x = 0; x < width; x++)
                {
                    if (x < row.Count)
                    {
                        background[x, y] = bgKeys[(int)(long)row[x]];
                    }
                    else
                    {
                        background[x, y] = bgKeys[0];
                    }
                }
            }

            Dictionary<string, Dictionary<string, object>> data = ((Dictionary<string, object>)json["data"]).ToDictionary(bd => bd.Key, bd => (Dictionary<string, object>)bd.Value);

            return new World { fgs = foreground, bgs = background, botData = data };
        }

        public void Save(string fileName)
        {
            Save(fileName, WorldFormat.STANDARD);
        }

        public void Save(string fileName, WorldFormat format)
        {
            StreamWriter file = new StreamWriter(fileName);

            file.Write(ToJSON(format));

            file.Flush();
            file.Close();
        }

        public string ToJSON(WorldFormat format)
        {
            if (format == WorldFormat.STANDARD) return ToStandardFormat();
            else throw new NotImplementedException("'" + format + "' format has not yet been implemented");
        }

        string ToStandardFormat()
        {
            List<Block> fgKeys = new List<Block>();
            List<Block> bgKeys = new List<Block>();
            int[,] foreground = new int[fgs.GetLength(0), fgs.GetLength(1)];
            int[,] background = new int[fgs.GetLength(0), fgs.GetLength(1)];

            for (int x = 0; x < fgs.GetLength(0); x++)
            {
                for (int y = 0; y < fgs.GetLength(1); y++)
                {
                    if (fgs[x, y].ARGS.Length == 0 && fgs[x, y].ID == 0)
                    {
                        foreground[x, y] = 0;
                    }
                    else
                    {
                        int i = fgKeys.IndexOf(fgs[x, y]);
                        if (i < 0)
                        {
                            i = fgKeys.Count;
                            fgKeys.Add(fgs[x, y]);
                        }

                        foreground[x, y] = i + 1;
                    }

                    if (bgs[x, y].ARGS.Length == 0 && bgs[x, y].ID == 0)
                    {
                        background[x, y] = 0;
                    }
                    else
                    {
                        int i = bgKeys.IndexOf(bgs[x, y]);
                        if (i < 0)
                        {
                            i = bgKeys.Count;
                            bgKeys.Add(bgs[x, y]);
                        }

                        background[x, y] = i + 1;
                    }
                }
            }

            string json = "{\n\t\"format\": \"" + WorldFormat.STANDARD + "\",\n\t\"blist\": [";
            foreach (Block b in fgKeys)
            {
                if (json[json.Length - 1] != '[') json += ",";

                object[] bData = new object[]{ b.ID }.Concat(b.ARGS).ToArray();
                json += "\n\t\t" + ObjToJson(bData, false);
            }

            json += "\n\t],\n\t\"bglist\": [";
            foreach (Block b in bgKeys)
            {
                if (json[json.Length - 1] != '[') json += ",";

                object[] bData = new object[] { b.ID }.Concat(b.ARGS).ToArray();
                json += "\n\t\t" + ObjToJson(bData, false);
            }

            json += "\n\t],\n\t\"blocks\": [";
            for (int y = 0; y < foreground.GetLength(1); y++)
            {
                if (json[json.Length - 1] != '[') json += ",";

                object[] row = new object[foreground.GetLength(0)];
                for (int x = 0; x < foreground.GetLength(0); x++)
                {
                    row[x] = foreground[x, y];
                }
                json += "\n\t\t" + ObjToJson(row, false);
            }

            json += "\n\t],\n\t\"backgrounds\": [";
            for (int y = 0; y < background.GetLength(1); y++)
            {
                if (json[json.Length - 1] != '[') json += ",";

                object[] row = new object[background.GetLength(0)];
                for (int x = 0; x < background.GetLength(0); x++)
                {
                    row[x] = background[x, y];
                }
                json += "\n\t\t" + ObjToJson(row, false);
            }

            json += "\n\t],\n\t\"data\": ";
            json += ObjToJson(botData.ToDictionary(bd => bd.Key, bd => (object)bd.Value), true).Replace("\n", "\n\t");

            json += "\n}";

            return json;
        }

        static string ObjToJson(object obj, bool neatified)
        {
            if (obj is string)
            {
                // TODO: escaped characters
                return "\"" + obj.ToString() + "\"";
            }
            else if (obj is sbyte || obj is byte || obj is short || obj is ushort || obj is int || obj is uint || obj is long || obj is ulong)
            {
                return obj.ToString();
            }
            else if (obj is float || obj is double)
            {
                return obj.ToString();
            }
            else if (obj is IDictionary)
            {
                IDictionary dictObj = (IDictionary)obj;

                if (dictObj.GetType().GetGenericArguments()[0] != typeof(string)) throw new ArgumentException("Invalid object, must have string keys.");

                string dict = "{";
                foreach(string key in dictObj.Keys)
                {
                    if (dict != "{") dict += ",";
                    if (neatified) dict += "\n\t";
                    dict += "\"" + key + "\": " + ObjToJson(dictObj[key], neatified).Replace("\n", "\n\t");
                }
                dict += neatified ? "\n}" : "}";

                return dict;
            }
            else if (obj is IList)
            {
                IList listObj = (IList)obj;

                string list = "[";
                foreach (object item in listObj)
                {
                    if (list != "[") list += ",";
                    if (neatified) list += "\n\t";
                    list += ObjToJson(item, true).Replace("\n", "\n\t");
                }
                list += neatified ? "\n]" : "]";

                return list;
            }
            else
            {
                throw new ArgumentException("Invalid json type '" + obj.GetType() + "'");
            }
        }

        static object JsonToObj(string json)
        {
            if (json[0] == '{')
            {
                Dictionary<string, object> dict = new Dictionary<string, object>();
                foreach (string part in SplitAtComma(json))
                {
                    int i = 0;
                    bool ignore = false;
                    while (++i < part.Length)
                    {
                        if (ignore) continue;
                        ignore = part[i] == '\\';

                        if (part[i] == '"') break;
                    }

                    dict[(string)JsonToObj(part.Substring(0, i + 1))] = JsonToObj(part.Substring(i + 2));
                }
                return dict;
            }
            else if (json[0] == '[')
            {
                List<object> list = new List<object>();
                foreach(string part in SplitAtComma(json))
                {
                    list.Add(JsonToObj(part));
                }
                return list;
            }
            else if (json[0] == '"')
            {
                // TODO: escaped characters
                return json.Substring(1, json.Length - 2);
            }
            else if (json.Contains('.') || json.Contains('e') || json.Contains('E'))
            {
                return double.Parse(json);
            }
            else if (json == "null")
            {
                return null;
            }
            else
            {
                return long.Parse(json);
            }
        }

        static List<string> SplitAtComma(string json)
        {
            List<string> parts = new List<string>();
            string currentPart = "";
            bool inString = false;
            bool ignore = false;
            int inBrackets = 0;

            foreach (char c in json)
            {
                if (ignore)
                {
                    ignore = false;
                    currentPart += c;
                }
                else if (inString)
                {
                    ignore = c == '\\';
                    inString = c != '"';
                    currentPart += c;
                }
                else if (char.IsWhiteSpace(c))
                {
                    continue;
                }
                else if (inBrackets == 0)
                {
                    if (c == '{' || c == '[') inBrackets++;
                    else continue;
                }
                else if (inBrackets == 1)
                {
                    if (c == '{' || c == '[')
                    {
                        currentPart += c;
                        inBrackets++;
                    }
                    else if (c == '}' || c == ']')
                    {
                        if (currentPart != "") parts.Add(currentPart);

                        return parts;
                    }
                    else if (c == '"')
                    {
                        inString = true;
                        currentPart += c;
                    }
                    else if (c == ',')
                    {
                        parts.Add(currentPart);
                        currentPart = "";
                    }
                    else
                    {
                        currentPart += c;
                    }
                }
                else if (inBrackets > 1)
                {
                    if (c == '{' || c == '[') inBrackets++;
                    if (c == '}' || c == ']') inBrackets--;
                    if (c == '"') inString = true;

                    currentPart += c;
                }
            }

            return parts;
        }

        public Block GetBlock(int l, int x, int y)
        {
            if (x < 0) throw new ArgumentOutOfRangeException("x", x, "x cannot be less than zero.");
            else if (x >= fgs.GetLength(0)) throw new ArgumentOutOfRangeException("x", x, "x cannot be greater or equal to the world width.");
            else if (y < 0) throw new ArgumentOutOfRangeException("y", y, "y cannot be less than zero.");
            else if (y >= fgs.GetLength(1)) throw new ArgumentOutOfRangeException("y", y, "y cannot be greater or equal to the world height.");

            if (l == 0)
            {
                return fgs[x, y];
            }
            else if (l == 1)
            {
                return bgs[x, y];
            }
            else throw new ArgumentOutOfRangeException("l", l, "This version of WorldSaver only supports foreground (0) and background (1) layers.");
        }

        public void SetBlock(int l, int x, int y, uint id, object[] args = null)
        {
            SetBlock(l, x, y, new Block(id, args));
        }

        public void SetBlock(int l, int x, int y, Block b)
        {
            if (x < 0) throw new ArgumentOutOfRangeException("x", x, "x cannot be less than zero.");
            else if (x >= fgs.GetLength(0)) throw new ArgumentOutOfRangeException("x", x, "x cannot be greater or equal to the world width.");
            else if (y < 0) throw new ArgumentOutOfRangeException("y", y, "y cannot be less than zero.");
            else if (y >= fgs.GetLength(1)) throw new ArgumentOutOfRangeException("y", y, "y cannot be greater or equal to the world height.");

            if (l == 0)
            {
                fgs[x, y] = b;
            }
            else if (l == 1)
            {
                bgs[x, y] = b;
            }
            else throw new ArgumentOutOfRangeException("l", l, "This version of WorldSaver only supports foreground (0) and background (1) layers.");
        }

        public bool ContainsBotData(string botID)
        {
            return botData.ContainsKey(botID);
        }

        public object GetData(string botID, string tag)
        {
            if (!botData.ContainsKey(botID)) return null;
            if (!botData[botID].ContainsKey(tag)) return null;
            return botData[botID][tag];
        }

        public void SetData(string botID, string tag, object value)
        {
            if (!botData.ContainsKey(botID)) botData[botID] = new Dictionary<string, object>();
            botData[botID][tag] = value;
        }
    }
}
