using System.Collections.Generic;

namespace MiniProjectB2BRetailer.DTO
{
    public class Warehouse
    {
        public Location Location
        {
            get;
            set;
        }
        public List<Item> Items{
            get;
            set;
        }
    }
}
