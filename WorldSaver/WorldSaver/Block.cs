using System;

namespace WorldSaver
{
    [Serializable]
    public struct Block
    {
        uint bid;
        object[] args;

        public uint ID
        {
            get { return bid; }
        }

        public object[] ARGS
        {
            get
            {
                if (args == null) args = new object[] { };
                return args;
            }
        }
        
        public Block(uint id, object[] args = null)
        {
            bid = id;

            this.args = args;
        }
    }
}
