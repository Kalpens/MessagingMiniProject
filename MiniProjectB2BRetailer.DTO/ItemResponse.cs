using System;
using System.Collections.Generic;
using System.Text;

namespace MiniProjectB2BRetailer.DTO
{
    public class ItemResponse
    {
        public int Id
        {
            get;
            set;
        }
        public string Name
        {
            get;
            set;
        }
        public float Price
        {
            get;
            set;
        }
        public string WarehouseId { get; set; }
        public int VolumeInStock { get; set; }
        public float Charge { get; set; }
        public int ShippingTime { get; set; }
        public bool ItemAvailable { get; set; }
        public string CustomerId { get; set; }
    }
}
