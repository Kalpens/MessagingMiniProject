using System;
namespace MiniProjectB2BRetailer.DTO
{
    public class Order
    {
        public int Qantity
        {
            get;
            set;
        }
        public Item Item
        {
            get;
            set;
        }
        public DateTime DateOfOrder
        {
            get;
            set;
        }
        public Location Location
        {
            get;
            set;
        }

        public string CustomerID
        {
            get;
            set;
        }
    }
}
